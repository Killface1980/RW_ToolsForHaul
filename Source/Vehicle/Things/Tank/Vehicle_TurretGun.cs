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

namespace ToolsForHaul
{
    [StaticConstructorOnStartup]
    public class Vehicle_TurretGun : Vehicle_Turret
    {
        private Thing gunInt;

        protected VehicleTurretTop top;

        protected CompPowerTrader powerComp;

        protected CompMannable mannableComp;

        public bool loaded;

        private bool holdFire;

        protected TargetInfo currentTargetInt = TargetInfo.Invalid;

        protected int burstWarmupTicksLeft;

        protected int burstCooldownTicksLeft;

        public static Material ForcedTargetLineMat = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 0.5f, 0.5f));

        public CompEquippable GunCompEq
        {
            get
            {
                return Gun.TryGetComp<CompEquippable>();
            }
        }



        public override TargetInfo CurrentTarget
        {
            get
            {
                return currentTargetInt;
            }
        }

        private bool WarmingUp
        {
            get
            {
                return burstWarmupTicksLeft > 0;
            }
        }

        public Thing Gun
        {
            get
            {
                if (gunInt == null)
                {
                    gunInt = ThingMaker.MakeThing(def.building.turretGunDef, null);
                    foreach (Verb verb in GunCompEq.AllVerbs)
                    {
                        verb.caster = this;
                        verb.castCompleteCallback = BurstComplete;
                    }
                }
                return gunInt;
            }
        }

        public override Verb AttackVerb
        {
            get
            {
                return GunCompEq.verbTracker.PrimaryVerb;
            }
        }

        private bool MannedByColonist
        {
            get
            {
                return mannableComp != null && mannableComp.ManningPawn != null && mannableComp.ManningPawn.Faction == Faction.OfPlayer;
            }
        }

        private bool CanSetForcedTarget
        {
            get
            {
                return MannedByColonist;
            }
        }

        private bool CanToggleHoldFire
        {
            get
            {
                return Faction == Faction.OfPlayer || MannedByColonist;
            }
        }

        public Vehicle_TurretGun()
        {
            top = new VehicleTurretTop(this);
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            powerComp = GetComp<CompPowerTrader>();
            mannableComp = GetComp<CompMannable>();
            currentTargetInt = TargetInfo.Invalid;
            burstWarmupTicksLeft = 0;
            burstCooldownTicksLeft = 0;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue(ref burstCooldownTicksLeft, "burstCooldownTicksLeft", 0, false);
            Scribe_Values.LookValue(ref loaded, "loaded", false, false);
            Scribe_Values.LookValue(ref holdFire, "holdFire", false, false);
        }

        public override void OrderAttack(TargetInfo targ)
        {
            if ((targ.Cell - Position).LengthHorizontal < GunCompEq.PrimaryVerb.verbProps.minRange)
            {
                Messages.Message("MessageTargetBelowMinimumRange".Translate(), this, MessageSound.RejectInput);
                return;
            }
            if ((targ.Cell - Position).LengthHorizontal > GunCompEq.PrimaryVerb.verbProps.range)
            {
                Messages.Message("MessageTargetBeyondMaximumRange".Translate(), this, MessageSound.RejectInput);
                return;
            }
            if (forcedTarget != targ)
            {
                forcedTarget = targ;
                if (burstCooldownTicksLeft <= 0)
                {
                    TryStartShootSomething();
                }
            }
        }

        public override void Tick()
        {
            base.Tick();


            if (!mountableComp.IsMounted)
            {
                return;
            }

            if (powerComp != null && !powerComp.PowerOn)
            {
                return;
            }
            if (mannableComp != null && !mannableComp.MannedNow)
            {
                return;
            }
            if (!CanSetForcedTarget && forcedTarget.IsValid)
            {
                ResetForcedTarget();
            }
            if (!CanToggleHoldFire)
            {
                holdFire = false;
            }
            GunCompEq.verbTracker.VerbsTick();
            if (stunner.Stunned)
            {
                return;
            }
            if (GunCompEq.PrimaryVerb.state == VerbState.Bursting)
            {
                return;
            }
            if (WarmingUp)
            {
                burstWarmupTicksLeft--;
                if (burstWarmupTicksLeft == 0)
                {
                    BeginBurst();
                }
            }
            else
            {
                if (burstCooldownTicksLeft > 0)
                {
                    burstCooldownTicksLeft--;
                }
                if (burstCooldownTicksLeft == 0)
                {
                    TryStartShootSomething();
                }
            }
            top.TurretTopTick();
        }

        protected void TryStartShootSomething()
        {
            if (forcedTarget.ThingDestroyed)
            {
                forcedTarget = null;
            }
            if (holdFire && CanToggleHoldFire)
            {
                return;
            }
            if (GunCompEq.PrimaryVerb.verbProps.projectileDef.projectile.flyOverhead && Find.RoofGrid.Roofed(Position))
            {
                return;
            }
            bool isValid = currentTargetInt.IsValid;
            if (forcedTarget.IsValid)
            {
                currentTargetInt = forcedTarget;
            }
            else
            {
                currentTargetInt = TryFindNewTarget();
            }
            if (!isValid && currentTargetInt.IsValid)
            {
                SoundDefOf.TurretAcquireTarget.PlayOneShot(Position);
            }
            if (currentTargetInt.IsValid)
            {
                if (def.building.turretBurstWarmupTicks > 0)
                {
                    burstWarmupTicksLeft = def.building.turretBurstWarmupTicks;
                }
                else
                {
                    BeginBurst();
                }
            }
        }

        protected TargetInfo TryFindNewTarget()
        {
            Thing thing = TargSearcher();
            Faction faction = thing.Faction;
            float range = GunCompEq.PrimaryVerb.verbProps.range;
            float minRange = GunCompEq.PrimaryVerb.verbProps.minRange;
            Building t;
            if (Rand.Value < 0.5f && GunCompEq.PrimaryVerb.verbProps.projectileDef.projectile.flyOverhead && faction.HostileTo(Faction.OfPlayer) && Find.ListerBuildings.allBuildingsColonist.Where(delegate (Building x)
            {
                float num = x.Position.DistanceToSquared(Position);
                return num > minRange * minRange && num < range * range;
            }).TryRandomElement(out t))
            {
                return t;
            }
            TargetScanFlags targetScanFlags = TargetScanFlags.NeedThreat;
            if (!GunCompEq.PrimaryVerb.verbProps.projectileDef.projectile.flyOverhead)
            {
                targetScanFlags |= TargetScanFlags.NeedLOSToAll;
            }
            if (GunCompEq.PrimaryVerb.verbProps.ai_IsIncendiary)
            {
                targetScanFlags |= TargetScanFlags.NeedNonBurning;
            }
            return AttackTargetFinder.BestShootTargetFromCurrentPosition(thing, IsValidTarget, range, minRange, targetScanFlags);
        }

        private Thing TargSearcher()
        {
            if (mannableComp != null && mannableComp.MannedNow)
            {
                return mannableComp.ManningPawn;
            }
            return this;
        }

        private bool IsValidTarget(Thing t)
        {
            Pawn pawn = t as Pawn;
            if (pawn != null)
            {
                if (GunCompEq.PrimaryVerb.verbProps.projectileDef.projectile.flyOverhead)
                {
                    RoofDef roofDef = Find.RoofGrid.RoofAt(t.Position);
                    if (roofDef != null && roofDef.isThickRoof)
                    {
                        return false;
                    }
                }
                if (mannableComp == null)
                {
                    return !GenAI.MachinesLike(Faction, pawn);
                }
                if (pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer)
                {
                    return false;
                }
            }
            return true;
        }

        protected void BeginBurst()
        {
            GunCompEq.PrimaryVerb.TryStartCastOn(CurrentTarget, false, true);
        }

        protected void BurstComplete()
        {
            if (def.building.turretBurstCooldownTicks >= 0)
            {
                burstCooldownTicksLeft = def.building.turretBurstCooldownTicks;
            }
            else
            {
                burstCooldownTicksLeft = GunCompEq.PrimaryVerb.verbProps.defaultCooldownTicks;
            }
            loaded = false;
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string inspectString = base.GetInspectString();
            if (!inspectString.NullOrEmpty())
            {
                stringBuilder.AppendLine(inspectString);
            }
            stringBuilder.AppendLine("GunInstalled".Translate() + ": " + Gun.Label);
            if (GunCompEq.PrimaryVerb.verbProps.minRange > 0f)
            {
                stringBuilder.AppendLine("MinimumRange".Translate() + ": " + GunCompEq.PrimaryVerb.verbProps.minRange.ToString("F0"));
            }
            if (burstCooldownTicksLeft > 0)
            {
                stringBuilder.AppendLine("CanFireIn".Translate() + ": " + burstCooldownTicksLeft.TickstoSecondsString());
            }
            if (def.building.turretShellDef != null)
            {
                if (loaded)
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

        public override void Draw()
        {
            top.DrawTurret();
            base.Draw();
        }

        public override void DrawExtraSelectionOverlays()
        {
            float range = GunCompEq.PrimaryVerb.verbProps.range;
            if (range < 90f)
            {
                GenDraw.DrawRadiusRing(Position, range);
            }
            float minRange = GunCompEq.PrimaryVerb.verbProps.minRange;
            if (minRange < 90f && minRange > 0.1f)
            {
                GenDraw.DrawRadiusRing(Position, minRange);
            }
            if (burstWarmupTicksLeft > 0)
            {
                int degreesWide = (int)((float)burstWarmupTicksLeft * 0.5f);
                GenDraw.DrawAimPie(this, CurrentTarget, degreesWide, (float)def.size.x * 0.5f);
            }
            if (forcedTarget.IsValid && (!forcedTarget.HasThing || forcedTarget.Thing.Spawned))
            {
                Vector3 b;
                if (forcedTarget.HasThing)
                {
                    b = forcedTarget.Thing.TrueCenter();
                }
                else
                {
                    b = forcedTarget.Cell.ToVector3Shifted();
                }
                Vector3 a = this.TrueCenter();
                b.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);
                a.y = b.y;
                GenDraw.DrawLineBetween(a, b, ForcedTargetLineMat);
            }
        }

        [DebuggerHidden]
        public override IEnumerable<Gizmo> GetGizmos()
        {
            IEnumerator<Gizmo> enumerator = base.GetGizmos().GetEnumerator();
            while (enumerator.MoveNext())
            {
                Gizmo current = enumerator.Current;
                yield return current;
            }
            if (CanSetForcedTarget)
            {
                yield return new Command_VerbTarget
                {
                    defaultLabel = "CommandSetForceAttackTarget".Translate(),
                    defaultDesc = "CommandSetForceAttackTargetDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true),
                    verb = GunCompEq.PrimaryVerb,
                    hotKey = KeyBindingDefOf.Misc4
                };
            }
            if (CanToggleHoldFire)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "CommandHoldFire".Translate(),
                    defaultDesc = "CommandHoldFireDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/HoldFire", true),
                    hotKey = KeyBindingDefOf.Misc6,
                    toggleAction = delegate
                    {
                        holdFire = !holdFire;
                        if (holdFire)
                        {
                            currentTargetInt = TargetInfo.Invalid;
                            burstWarmupTicksLeft = 0;
                        }
                    },
                    isActive = (() => holdFire)
                };
            }
            yield break;
        }

        private void ResetForcedTarget()
        {
            forcedTarget = TargetInfo.Invalid;
            if (burstCooldownTicksLeft <= 0)
            {
                TryStartShootSomething();
            }
        }
    }
}
