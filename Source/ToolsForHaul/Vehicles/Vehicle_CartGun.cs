namespace ToolsForHaul.Vehicles
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    using RimWorld;

    using UnityEngine;

    using Verse;
    using Verse.AI;
    using Verse.Sound;

    [StaticConstructorOnStartup]
    public class Vehicle_CartGun : Vehicle_Cart
    {
        public static Material ForcedTargetLineMat = MaterialPool.MatFrom(
            GenDraw.LineTexPath,
            ShaderDatabase.Transparent,
            new Color(1f, 0.5f, 0.5f));

        public bool loaded;

        protected int burstCooldownTicksLeft;

        protected int burstWarmupTicksLeft;

        protected LocalTargetInfo currentTargetInt = LocalTargetInfo.Invalid;

        protected CompMannable mannableComp;

        protected CompPowerTrader powerComp;

        protected Vehicle_CartGunTop top;

        private Thing gunInt;

        private bool holdFire;

        public Vehicle_CartGun()
        {
            this.top = new Vehicle_CartGunTop(this);
        }

        public override Verb AttackVerb => this.GunCompEq.verbTracker.PrimaryVerb;

        public override LocalTargetInfo CurrentTarget => this.currentTargetInt;

        public Thing Gun
        {
            get
            {
                if (this.gunInt == null)
                {
                    this.gunInt = ThingMaker.MakeThing(this.def.building.turretGunDef);
                    foreach (Verb verb in this.GunCompEq.AllVerbs)
                    {
                        verb.caster = this;
                        verb.castCompleteCallback = this.BurstComplete;
                    }
                }

                return this.gunInt;
            }
        }

        public CompEquippable GunCompEq => this.Gun.TryGetComp<CompEquippable>();

        private bool CanSetForcedTarget
        {
            get
            {
                return (base.Faction == Faction.OfPlayer || this.MannedByColonist) && !this.MannedByNonColonist;
            }
        }
        private bool CanToggleHoldFire
        {
            get
            {
                return (base.Faction == Faction.OfPlayer || this.MannedByColonist) && !this.MannedByNonColonist;
            }
        }
        private bool MannedByColonist
        {
            get
            {
                return this.mannableComp != null && this.mannableComp.ManningPawn != null && this.mannableComp.ManningPawn.Faction == Faction.OfPlayer;
            }
        }
        private bool MannedByNonColonist
        {
            get
            {
                return this.mannableComp != null && this.mannableComp.ManningPawn != null && this.mannableComp.ManningPawn.Faction != Faction.OfPlayer;
            }
        }
        private bool WarmingUp
        {
            get
            {
                return this.burstWarmupTicksLeft > 0;
            }
        }
        public override void Draw()
        {
            this.top.DrawLightTurret();
            base.Draw();
        }

        public override void DrawExtraSelectionOverlays()
        {
            float range = this.GunCompEq.PrimaryVerb.verbProps.range;
            if (range < 90f)
            {
                GenDraw.DrawRadiusRing(this.Position, range);
            }

            float minRange = this.GunCompEq.PrimaryVerb.verbProps.minRange;
            if (minRange < 90f && minRange > 0.1f)
            {
                GenDraw.DrawRadiusRing(this.Position, minRange);
            }

            if (this.burstWarmupTicksLeft > 0)
            {
                int degreesWide = (int)(this.burstWarmupTicksLeft * 0.5f);
                GenDraw.DrawAimPie(this, this.CurrentTarget, degreesWide, this.def.size.x * 0.5f);
            }

            if (this.forcedTarget.IsValid && (!this.forcedTarget.HasThing || this.forcedTarget.Thing.Spawned))
            {
                Vector3 b;
                if (this.forcedTarget.HasThing)
                {
                    b = this.forcedTarget.Thing.TrueCenter();
                }
                else
                {
                    b = this.forcedTarget.Cell.ToVector3Shifted();
                }

                Vector3 a = this.TrueCenter();
                b.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);
                a.y = b.y;
                GenDraw.DrawLineBetween(a, b, ForcedTargetLineMat);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.burstCooldownTicksLeft, "burstCooldownTicksLeft", 0, false);
            Scribe_Values.Look<int>(ref this.burstWarmupTicksLeft, "burstWarmupTicksLeft", 0, false);
            Scribe_TargetInfo.Look(ref this.currentTargetInt, "currentTarget");
            Scribe_Values.Look<bool>(ref this.loaded, "loaded", false, false);
            Scribe_Values.Look<bool>(ref this.holdFire, "holdFire", false, false);
        }

        [DebuggerHidden]
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            if (this.CanSetForcedTarget)
            {
                yield return
                    new Command_VerbTarget
                    {
                        defaultLabel = "CommandSetForceAttackTarget".Translate(),
                        defaultDesc = "CommandSetForceAttackTargetDesc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack"),
                        verb = this.GunCompEq.PrimaryVerb,
                        hotKey = KeyBindingDefOf.Misc4
                    };
            }
            if (this.forcedTarget.IsValid)
            {
                Command_Action stop = new Command_Action();
                stop.defaultLabel = "CommandStopForceAttack".Translate();
                stop.defaultDesc = "CommandStopForceAttackDesc".Translate();
                stop.icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt", true);
                stop.action = delegate
                    {
                        this.ResetForcedTarget();
                        SoundDefOf.TickLow.PlayOneShotOnCamera(null);
                    };
                if (!this.forcedTarget.IsValid)
                {
                    stop.Disable("CommandStopAttackFailNotForceAttacking".Translate());
                }
                stop.hotKey = KeyBindingDefOf.Misc5;
                yield return stop;
            }
            if (this.CanToggleHoldFire)
            {
                yield return
                    new Command_Toggle
                    {
                        defaultLabel = "CommandHoldFire".Translate(),
                        defaultDesc = "CommandHoldFireDesc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/HoldFire"),
                        hotKey = KeyBindingDefOf.Misc6,
                        toggleAction = delegate
                            {
                                this.holdFire = !this.holdFire;
                                if (this.holdFire)
                                {
                                    this.ResetForcedTarget();
                                }
                            },
                        isActive = () => this.holdFire
                    };
            }
        }

        // RimWorld.Building_TurretGun
        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string inspectString = base.GetInspectString();
            if (!inspectString.NullOrEmpty())
            {
                stringBuilder.AppendLine(inspectString);
            }

            stringBuilder.AppendLine("GunInstalled".Translate() + ": " + this.Gun.Label);
            if (this.GunCompEq.PrimaryVerb.verbProps.minRange > 0f)
            {
                stringBuilder.AppendLine("MinimumRange".Translate() + ": " + this.GunCompEq.PrimaryVerb.verbProps.minRange.ToString("F0"));
            }

            if (this.burstCooldownTicksLeft > 0)
            {
                stringBuilder.AppendLine("CanFireIn".Translate() + ": " + this.burstCooldownTicksLeft.TicksToSecondsString());
            }

            if (this.def.building.turretShellDef != null)
            {
                if (this.loaded)
                {
                    stringBuilder.AppendLine("ShellLoaded".Translate());
                }
                else
                {
                    stringBuilder.AppendLine("ShellNotLoaded".Translate());
                }
            }

            return stringBuilder.ToString();
        }

        // RimWorld.Building_TurretGun
        public override void OrderAttack(LocalTargetInfo targ)
        {
            if (!targ.IsValid)
            {
                if (this.forcedTarget.IsValid)
                {
                    this.ResetForcedTarget();
                }
                return;
            }
            if ((targ.Cell - base.Position).LengthHorizontal < this.GunCompEq.PrimaryVerb.verbProps.minRange)
            {
                Messages.Message("MessageTargetBelowMinimumRange".Translate(), this, MessageSound.RejectInput);
                return;
            }
            if ((targ.Cell - base.Position).LengthHorizontal > this.GunCompEq.PrimaryVerb.verbProps.range)
            {
                Messages.Message("MessageTargetBeyondMaximumRange".Translate(), this, MessageSound.RejectInput);
                return;
            }
            if (this.forcedTarget != targ)
            {
                this.forcedTarget = targ;
                if (this.burstCooldownTicksLeft <= 0)
                {
                    this.TryStartShootSomething(false);
                }
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.powerComp = this.GetComp<CompPowerTrader>();
            this.mannableComp = this.GetComp<CompMannable>();
            this.currentTargetInt = LocalTargetInfo.Invalid;
            this.burstWarmupTicksLeft = 0;
            this.burstCooldownTicksLeft = 0;
        }

        // RimWorld.Building_TurretGun
        public override void Tick()
        {
            base.Tick();

            // Best way to assume firing only when pawn mounted.
            if (!this.MountableComp.IsMounted)
            {
                return;
            }

            if (this.forcedTarget.IsValid && !this.CanSetForcedTarget)
            {
                this.ResetForcedTarget();
            }
            if (!this.CanToggleHoldFire)
            {
                this.holdFire = false;
            }
            if (this.forcedTarget.ThingDestroyed)
            {
                this.ResetForcedTarget();
            }
            bool flag = (this.powerComp == null || this.powerComp.PowerOn) && (this.mannableComp == null || this.mannableComp.MannedNow);
            if (flag && base.Spawned)
            {
                this.GunCompEq.verbTracker.VerbsTick();
                if (!this.stunner.Stunned && this.GunCompEq.PrimaryVerb.state != VerbState.Bursting)
                {
                    if (this.WarmingUp)
                    {
                        this.burstWarmupTicksLeft--;
                        if (this.burstWarmupTicksLeft == 0)
                        {
                            this.BeginBurst();
                        }
                    }
                    else
                    {
                        if (this.burstCooldownTicksLeft > 0)
                        {
                            this.burstCooldownTicksLeft--;
                        }
                        if (this.burstCooldownTicksLeft <= 0 && this.IsHashIntervalTick(10))
                        {
                            this.TryStartShootSomething(true);
                        }
                    }
                    this.top.CartTopTick();
                }
            }
            else
            {
                this.ResetCurrentTarget();
            }
        }

        protected void BeginBurst()
        {
            this.GunCompEq.PrimaryVerb.TryStartCastOn(this.CurrentTarget);
        }

        // RimWorld.Building_TurretGun
        protected void BurstComplete()
        {
            if (this.def.building.turretBurstCooldownTime >= 0f)
            {
                this.burstCooldownTicksLeft = this.def.building.turretBurstCooldownTime.SecondsToTicks();
            }
            else
            {
                this.burstCooldownTicksLeft = this.GunCompEq.PrimaryVerb.verbProps.defaultCooldownTime.SecondsToTicks();
            }

            this.loaded = false;
        }


        protected LocalTargetInfo TryFindNewTarget()
        {
            IAttackTargetSearcher attackTargetSearcher = this.TargSearcher();
            Faction faction = attackTargetSearcher.Thing.Faction;
            float range = this.GunCompEq.PrimaryVerb.verbProps.range;
            float minRange = this.GunCompEq.PrimaryVerb.verbProps.minRange;
            Building t;
            if (Rand.Value < 0.5f && this.GunCompEq.PrimaryVerb.verbProps.projectileDef.projectile.flyOverhead && faction.HostileTo(Faction.OfPlayer) && base.Map.listerBuildings.allBuildingsColonist.Where(delegate (Building x)
                {
                    float num = (float)x.Position.DistanceToSquared(this.Position);
                    return num > minRange * minRange && num < range * range;
                }).TryRandomElement(out t))
            {
                return t;
            }
            TargetScanFlags targetScanFlags = TargetScanFlags.NeedThreat;
            if (!this.GunCompEq.PrimaryVerb.verbProps.projectileDef.projectile.flyOverhead)
            {
                targetScanFlags |= TargetScanFlags.NeedLOSToAll;
                targetScanFlags |= TargetScanFlags.LOSBlockableByGas;
            }
            if (this.GunCompEq.PrimaryVerb.verbProps.ai_IsIncendiary)
            {
                targetScanFlags |= TargetScanFlags.NeedNonBurning;
            }
            return (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(attackTargetSearcher, new Predicate<Thing>(this.IsValidTarget), range, minRange, targetScanFlags);
        }


        // RimWorld.Building_TurretGun
        protected void TryStartShootSomething(bool canBeginBurstImmediately)
        {
            if (!base.Spawned || (this.holdFire && this.CanToggleHoldFire) || (this.GunCompEq.PrimaryVerb.verbProps.projectileDef.projectile.flyOverhead && base.Map.roofGrid.Roofed(base.Position)))
            {
                this.ResetCurrentTarget();
                return;
            }
            bool isValid = this.currentTargetInt.IsValid;
            if (this.forcedTarget.IsValid)
            {
                this.currentTargetInt = this.forcedTarget;
            }
            else
            {
                this.currentTargetInt = this.TryFindNewTarget();
            }
            if (!isValid && this.currentTargetInt.IsValid)
            {
                SoundDefOf.TurretAcquireTarget.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
            }
            if (this.currentTargetInt.IsValid)
            {
                if (this.def.building.turretBurstWarmupTime > 0f)
                {
                    this.burstWarmupTicksLeft = this.def.building.turretBurstWarmupTime.SecondsToTicks();
                }
                else if (canBeginBurstImmediately)
                {
                    this.BeginBurst();
                }
                else
                {
                    this.burstWarmupTicksLeft = 1;
                }
            }
            else
            {
                this.ResetCurrentTarget();
            }
        }

        private void ResetCurrentTarget()
        {
            this.currentTargetInt = LocalTargetInfo.Invalid;
            this.burstWarmupTicksLeft = 0;
        }

        // RimWorld.Building_TurretGun
        private bool IsValidTarget(Thing t)
        {
            Pawn pawn = t as Pawn;
            if (pawn != null)
            {
                if (this.GunCompEq.PrimaryVerb.verbProps.projectileDef.projectile.flyOverhead)
                {
                    RoofDef roofDef = this.Map.roofGrid.RoofAt(t.Position);
                    if (roofDef != null && roofDef.isThickRoof)
                    {
                        return false;
                    }
                }

                if (this.mannableComp == null)
                {
                    return !GenAI.MachinesLike(this.Faction, pawn);
                }

                if (pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer)
                {
                    return false;
                }
            }
            // Don't attack fellow prison breakers
            if (this.MountableComp.IsPrisonBreaking && !t.Faction.IsPlayer)
            {
                return false;
            }

            return true;
        }

        private void ResetForcedTarget()
        {
            this.forcedTarget = LocalTargetInfo.Invalid;
            this.burstWarmupTicksLeft = 0;
            if (this.burstCooldownTicksLeft <= 0)
            {
                this.TryStartShootSomething(false);
            }
        }


        private IAttackTargetSearcher TargSearcher()
        {
             if (this.mannableComp != null && this.mannableComp.MannedNow)
            {
                return this.mannableComp.ManningPawn;
            }
            if (this.MountableComp.IsMounted)
            {
                return this.MountableComp.Driver;
            }
            return this;
        }
    }
}