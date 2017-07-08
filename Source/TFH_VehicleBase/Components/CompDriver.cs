// --------------------------------------------------------------------------------------------------------------------
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
        public Vehicle_Cart Cart { get; set; }

        public Pawn Pawn;

        public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {

            if (this.Pawn == null || this.Cart == null)
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
                this.Cart?.TakeDamage(dinfo);

                absorbed = true;
                return;
            }

            absorbed = false;
        }

        // IMPORTANT: THE parent IS THE PAWN, NOT THE VEHICLE!!!!!!!
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (selPawn == null || this.Cart == null || selPawn.Faction != Faction.OfPlayer)
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
                        this.Cart.MountableComp.DismountAt(selPawn.Position);
                        return;
                    }

                    this.Cart.MountableComp.DismountAt(
                        selPawn.Position - this.Cart.def.interactionCellOffset.RotatedBy(selPawn.Rotation));
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


            var saddle = TFH_BaseUtility.GetSaddleByRider((Pawn)this.parent);

            if (saddle != null)
            {
                if (saddle.MountableComp.Driver.RaceProps.Animal)
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
                    // Command_Action commandUnboardAll = new Command_Action();

                    // commandUnboardAll.defaultLabel = "CommandGetOffLabel".Translate();
                    // commandUnboardAll.defaultDesc = "CommandGetOffDesc".Translate();
                    // commandUnboardAll.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconUnboardAll");
                    // commandUnboardAll.activateSound = Static.ClickSound;
                    // commandUnboardAll.action = () => { this.UnboardAll(); };
                    // yield return commandUnboardAll;
                }
            }
        }

    }
}