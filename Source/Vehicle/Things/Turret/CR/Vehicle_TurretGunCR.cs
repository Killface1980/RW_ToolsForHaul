#if CR
using System;
using System.Collections.Generic;
using System.Text;
using Combat_Realism;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Command_VerbTarget = Verse.Command_VerbTarget;

namespace ToolsForHaul
{
    [StaticConstructorOnStartup]
    public class Vehicle_TurretGun : Vehicle_Turret
    {
        private const int minTicksBeforeAutoReload = 1800;              // This much time must pass before haulers will try to automatically reload an auto-turret

        #region Fields

        protected CompPowerTrader powerComp;
        protected Vehicle_TurretTop top;

        #endregion

        #region Properties

        #endregion

        #region Methods

        public override void Draw()
        {
            top.DrawTurret();
            base.Draw();
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string inspectString = base.GetInspectString();
            if (!inspectString.NullOrEmpty())
            {
                stringBuilder.AppendLine(inspectString);
            }
            stringBuilder.AppendLine("GunInstalled".Translate() + ": " + gun.LabelCap);
            if (GunCompEq.PrimaryVerb.verbProps.minRange > 0f)
            {
                stringBuilder.AppendLine("MinimumRange".Translate() + ": " + GunCompEq.PrimaryVerb.verbProps.minRange.ToString("F0"));
            }

            if (isReloading)
            {
                stringBuilder.AppendLine("CR_TurretReloading".Translate());
            }
            else if (burstCooldownTicksLeft > 0)
            {
                stringBuilder.AppendLine("CanFireIn".Translate() + ": " + burstCooldownTicksLeft.TickstoSecondsString());
            }

            if (compAmmo != null && compAmmo.Props.ammoSet != null)
            {
                stringBuilder.AppendLine("CR_AmmoSet".Translate() + ": " + compAmmo.Props.ammoSet.LabelCap);
            }
            /*
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
            */
            return stringBuilder.ToString();
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            powerComp = GetComp<CompPowerTrader>();
            mannableComp = GetComp<CompMannable>();
            if (gun == null)
            {
                gun = ThingMaker.MakeThing(def.building.turretGunDef, null);
            }
            for (int i = 0; i < GunCompEq.AllVerbs.Count; i++)
            {
                Verb verb = GunCompEq.AllVerbs[i];
                verb.caster = this;
                verb.castCompleteCallback = new Action(BurstComplete);
            }
            top = new Vehicle_TurretTop(this);

            // Callback for ammo comp
            if (compAmmo != null)
            {
                compAmmo.turret = this;
                if (def.building.turretShellDef != null && def.building.turretShellDef is AmmoDef) compAmmo.selectedAmmo = (AmmoDef)def.building.turretShellDef;
            }
        }

        public override void Tick()
        {
            base.Tick();

            ticksSinceLastBurst++;

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

        // New methods

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (Faction == Faction.OfPlayer)
            {
                // Ammo gizmos
                if (compAmmo != null)
                {
                    foreach (Command com in compAmmo.CompGetGizmosExtra())
                    {
                        yield return com;
                    }
                }
                if (mountableComp.IsMounted)
                {      // Fire mode gizmos
                    if (compFireModes != null)
                    {
                        foreach (Command com in compFireModes.GenerateGizmos())
                        {
                            yield return com;
                        }
                    }

                    // Stop forced attack gizmo
                    Gizmo stop = new Command_Action()
                    {
                        defaultLabel = "CommandStopForceAttack".Translate(),
                        defaultDesc = "CommandStopForceAttackDesc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt", true),
                        action = new Action(delegate
                        {
                            forcedTarget = TargetInfo.Invalid;
                            SoundDefOf.TickLow.PlayOneShotOnCamera();
                        }),
                        hotKey = KeyBindingDefOf.Misc5
                    };
                    yield return stop;
                    // Set forced target gizmo
                    if ((mannableComp != null && mannableComp.MannedNow && mannableComp.ManningPawn.Faction == Faction.OfPlayer)
                        || (mannableComp == null && Faction == Faction.OfPlayer))
                    {
                        Gizmo attack = new Command_VerbTarget()
                        {
                            defaultLabel = "CommandSetForceAttackTarget".Translate(),
                            defaultDesc = "CommandSetForceAttackTargetDesc".Translate(),
                            icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true),
                            verb = GunCompEq.PrimaryVerb,
                            hotKey = KeyBindingDefOf.Misc4
                        };
                        yield return attack;
                    }
                }
            }

            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
        }

        #endregion
    }
}
#endif