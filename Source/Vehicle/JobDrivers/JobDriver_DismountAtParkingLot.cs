using System.Collections.Generic;
using RimWorld;
using ToolsForHaul.Components;
using ToolsForHaul.Toils;
using ToolsForHaul.Utilities;
using Verse;
using Verse.AI;

namespace ToolsForHaul.JobDrivers
{
    using ToolsForHaul.Components.Vehicle;

    public class JobDriver_DismountAtParkingLot : JobDriver
    {
        // Constants
        private const TargetIndex CartInd = TargetIndex.A;
        private const TargetIndex ParkingLotCellInd = TargetIndex.B;

        public override string GetReport()
        {
            ThingWithComps cart = this.TargetThingA as ThingWithComps;

            IntVec3 destLoc = IntVec3.Invalid;
            string destName = null;
            Zone destZone = null;


            if (this.pawn.jobs.curJob.targetB != null)
            {
                destLoc = this.pawn.jobs.curJob.targetB.Cell;
                destZone = destLoc.GetZone(cart.Map);
            }

            if (destZone != null)
                destName = destZone.label;

            string repString;
            if (destName != null)
                repString = "ReportDismountingOn".Translate(cart.LabelCap, destName);
            else
                repString = "ReportDismounting".Translate(cart.LabelCap);

            return repString;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            ///
            // Set fail conditions
            ///

            this.FailOnDestroyedOrNull(CartInd);

          //  this.FailOnDestroyedNullOrForbidden(ParkingLotCellInd);

            // Note we only fail on forbidden if the target doesn't start that way
            // This helps haul-aside jobs on forbidden items
            if (!this.TargetThingA.IsForbidden(this.pawn.Faction))
            {
                this.FailOnForbidden(CartInd);
            }

            Vehicle_Cart cart = this.TargetThingA as Vehicle_Cart;

         // IntVec3 parkingSpace = IntVec3.Invalid;
         // if (!ToolsForHaulUtility.FindParkingSpace(this.pawn.Map, cart.Position, out parkingSpace))
         // {
         //     JobFailReason.Is(ToolsForHaulUtility.NoEmptyPlaceForCart);
         // }



            ///
            // Define Toil
            ///

            Toil toilGoToCell = Toils_Goto.GotoCell(ParkingLotCellInd, PathEndMode.ClosestTouch);

            ///
            // Toils Start
            ///

            // Reserve thing to be stored and storage cell 
            yield return Toils_Reserve.Reserve(CartInd);
            yield return Toils_Reserve.Reserve(ParkingLotCellInd);

            // JumpIf already mounted
            yield return Toils_Jump.JumpIf(
                toilGoToCell,
                () => { return cart.MountableComp.Driver == this.pawn ? true : false; });

            // Mount on Target
            yield return Toils_Goto.GotoThing(CartInd, PathEndMode.ClosestTouch).FailOnDestroyedOrNull(CartInd);
            yield return Toils_Cart.MountOn(CartInd);

            // Dismount
            yield return toilGoToCell;

            yield return Toils_Cart.DismountAt(CartInd, ParkingLotCellInd);
        }
    }
}
