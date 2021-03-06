﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompDriver.cs" company="">
// </copyright>
// <summary>
//   Defines the CompDriver type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace TFH_VehicleBase.Components
{
    using System;
    using System.Collections.Generic;

    using RimWorld;

    using Verse;
    using Verse.AI;

    public class CompDriver : ThingComp
    {
        public BasicVehicle Vehicle { get; set; }

        public Pawn Pawn;

        public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {

            if (this.Pawn == null || this.Vehicle == null)
            {
                absorbed = false;
                return;
            }

            if (this.Pawn.RaceProps.Animal)
            {
                absorbed = false;
                return;
            }

            float hitChance = 0.65f;
            float hit = Rand.Value;

            if (hitChance <= hit)
            {
                // apply damage to vehicle here
                this.Vehicle?.TakeDamage(dinfo);

                absorbed = true;
                return;
            }

            absorbed = false;
        }

        // IMPORTANT: THE parent IS THE PAWN, NOT THE VEHICLE!!!!!!!
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (selPawn == null || this.Vehicle == null || selPawn.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            Action action_DismountInBase = () =>
                {
                    Job jobNew = selPawn.DismountAtParkingLot("CFMO driver");

                    selPawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                };

            Action action_Dismount = () =>
                {
                    if (!selPawn.Position.InBounds(selPawn.Map))
                    {
                        this.Vehicle.MountableComp.DismountAt(selPawn.Position);
                        return;
                    }

                    this.Vehicle.MountableComp.DismountAt(
                        selPawn.Position - this.Vehicle.def.interactionCellOffset.RotatedBy(selPawn.Rotation));
                    selPawn.Position = selPawn.Position.RandomAdjacentCell8Way();

                    // mountableComp.DismountAt(myPawn.Position - VehicleDef.interactionCellOffset.RotatedBy(myPawn.Rotation));
                };
            {
                // if (cart.MountableComp.IsMounted && selPawn == cart.MountableComp.Driver)
                // && !myPawn.health.hediffSet.HasHediff(HediffDef.Named("HediffWheelChair")))
                yield return new FloatMenuOption("Dismount".Translate(selPawn.LabelShort), action_Dismount);
                yield return new FloatMenuOption("DismountAtParkingLot".Translate(selPawn.LabelShort), action_DismountInBase);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo c in base.CompGetGizmosExtra())
            {
                yield return c;
            }

            if (this.Pawn == null)
            {
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = Static.TxtCommandDismountLabel.Translate(),
                defaultDesc = Static.TxtCommandDismountDesc.Translate(),
                icon = Static.IconUnmount,
                activateSound = Static.ClickSound,
                action = delegate { TFH_BaseUtility.DismountGizmoFloatMenu(this.parent as Pawn); }
            };
        }

    }
}