// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompDriver.cs" company="">
// </copyright>
// <summary>
//   Defines the CompDriver type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace ToolsForHaul.Components.Vehicles
{
    using System;
    using System.Collections.Generic;

    using RimWorld;

    using ToolsForHaul.Components.Vehicle;
    using ToolsForHaul.JobDefs;
    using ToolsForHaul.Utilities;

    using UnityEngine;

    using Verse;
    using Verse.AI;

    public class CompDriver : ThingComp
    {
        public Thing Vehicle { get; set; }

        public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            var pawn = parent as Pawn;

            if (pawn.RaceProps.Animal)
            {
                absorbed = false;
                return;
            }

            float hitChance = 0.25f;
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
            if (selPawn.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            Vehicle_Cart cart = ToolsForHaulUtility.GetCartByDriver(selPawn);
            if (cart == null)
                yield break;

            Action action_DismountInBase = () =>
                {
                    Job jobNew = ToolsForHaulUtility.DismountAtParkingLot(selPawn, cart);

                    selPawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                };

            Action action_Dismount = () =>
                {
                    if (!selPawn.Position.InBounds(selPawn.Map))
                    {
                        cart.MountableComp.DismountAt(selPawn.Position);
                        return;
                    }

                    cart.MountableComp.DismountAt(
                        selPawn.Position - this.parent.def.interactionCellOffset.RotatedBy(selPawn.Rotation));
                    selPawn.Position = selPawn.Position.RandomAdjacentCell8Way();

                    // mountableComp.DismountAt(myPawn.Position - VehicleDef.interactionCellOffset.RotatedBy(myPawn.Rotation));
                };

        //    if (cart.MountableComp.IsMounted && selPawn == cart.MountableComp.Driver)
            {
                // && !myPawn.health.hediffSet.HasHediff(HediffDef.Named("HediffWheelChair")))
                yield return new FloatMenuOption("Dismount".Translate(this.parent.LabelShort), action_Dismount);
                yield return new FloatMenuOption("DismountAtParkingLot".Translate(this.parent.LabelShort), action_DismountInBase);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo c in base.CompGetGizmosExtra())
            {
                yield return c;
            }
            yield return new Command_Action
            {
                defaultLabel = Static.TxtCommandDismountLabel.Translate(),
                defaultDesc = Static.TxtCommandDismountDesc.Translate(),
                icon = Static.IconUnmount,
                activateSound = Static.ClickSound,
                action = ToolsForHaulUtility.GetCartByDriver(this.parent as Pawn).MountableComp.Dismount
            };

            var saddle = parent as Vehicle_Saddle;

            if (saddle != null)
            {
                if (saddle.mountableComp.Driver.RaceProps.Animal)
                {
                    Designator_Board designatorBoard =
                        new Designator_Board
                        {
                            vehicle = this.parent,
                            defaultLabel = "CommandRideLabel".Translate(),
                            defaultDesc = "CommandRideDesc".Translate(),
                            icon = Static.IconBoard,
                            activateSound = Static.ClickSound
                        };


                    yield return designatorBoard;
                    // if (mountableComp.IsMounted && this.innerContainer.Count(x => x is Pawn) >= maxNumBoarding)
                    // {
                    //     Command_Action commandUnboardAll = new Command_Action();

                    // commandUnboardAll.defaultLabel = "CommandGetOffLabel".Translate();
                    // commandUnboardAll.defaultDesc = "CommandGetOffDesc".Translate();
                    // commandUnboardAll.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconUnboardAll");
                    // commandUnboardAll.activateSound = Static.ClickSound;
                    // commandUnboardAll.action = () => { this.UnboardAll(); };
                    //
                    // yield return commandUnboardAll;

                }

            }
        }

    }
}