namespace TFH_VehicleHauling.JobDrivers
{
    using System.Collections.Generic;

    using RimWorld;

    using TFH_VehicleBase;
    using TFH_VehicleBase.Components;

    using TFH_VehicleHauling.Toils;

    using Verse;
    using Verse.AI;

    public class JobDriver_HaulWithCart : JobDriver
    {
        // Constants
        private const TargetIndex HaulableInd = TargetIndex.A;

        private const TargetIndex StoreCellInd = TargetIndex.B;

        private const TargetIndex CartInd = TargetIndex.C;

        public override string GetReport()
        {
            Thing hauledThing = null;
            hauledThing = this.TargetThingA;
            if (this.TargetThingA == null)  // Haul Cart
            {
                hauledThing = this.job.targetC.Thing;
            }

            IntVec3 destLoc = IntVec3.Invalid;
            string destName = null;
            SlotGroup destGroup = null;

            if (this.pawn.jobs.curJob.targetB != null)
            {
                destLoc = this.pawn.jobs.curJob.targetB.Cell;
                destGroup = destLoc.GetSlotGroup(this.Map);
            }

            if (destGroup != null)
            {
                destName = destGroup.parent.SlotYielderLabel();
            }

            string repString;
            if (destName != null)
            {
                repString = "ReportHaulingTo".Translate(hauledThing.LabelCap, destName);
            }
            else
            {
                repString = "ReportHauling".Translate(hauledThing.LabelCap);
            }

            return repString;
        }

        public override bool TryMakePreToilReservations()
        {
            return true;// this.pawn.Reserve(this.job.GetTarget(CartInd).Thing, this.job);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Vehicle_Cart cart = (Vehicle_Cart)this.job.GetTarget(CartInd).Thing;

            ///
            // Set fail conditions
            ///

            this.FailOnDestroyedNullOrForbidden(CartInd);

            // Note we only fail on forbidden if the target doesn't start that way
            // This helps haul-aside jobs on forbidden items
            if (!this.TargetThingA.IsForbidden(this.pawn.Faction))
            {
                this.FailOnForbidden(CartInd);
            }

            ///
            // Define Toil
            ///

            Toil findParkingSpaceForCart = Toils_Cart.FindParkingSpaceForCart(CartInd);

            Toil findStoreCellFor = Toils_Cart.FindStoreCellForCart(CartInd);
            Toil checkStoreCellEmpty = Toils_Jump.JumpIf(findStoreCellFor, () => job.GetTargetQueue(StoreCellInd).NullOrEmpty());


            Toil checkParkingCellEmpty = Toils_Jump.JumpIf(
                findParkingSpaceForCart,
                () => this.job.GetTargetQueue(StoreCellInd).NullOrEmpty());
            Toil checkHaulableEmpty = Toils_Jump.JumpIf(
                checkStoreCellEmpty,
                () => this.job.GetTargetQueue(HaulableInd).NullOrEmpty());

            ///
            // Toils Start
            ///

            // Reserve thing to be stored and storage cell 
            yield return Toils_Reserve.Reserve(CartInd);
            yield return Toils_Reserve.ReserveQueue(HaulableInd);
            yield return Toils_Reserve.ReserveQueue(StoreCellInd);

            // JumpIf already mounted
            yield return Toils_Jump.JumpIf(
                checkHaulableEmpty,
                () => { return (cart.MountableComp.Rider == this.pawn) ? true : false; });

            // Mount on Target
            yield return Toils_Goto.GotoThing(CartInd, PathEndMode.ClosestTouch)
                .FailOnDestroyedNullOrForbidden(CartInd);
            yield return Toils_Cart.MountOn(CartInd);

            // JumpIf checkStoreCellEmpty
            yield return checkHaulableEmpty;

            // Collect TargetQueue
            Toil extractA = Toils_Collect.Extract(HaulableInd);
            yield return extractA;

            yield return Toils_Goto.GotoThing(HaulableInd, PathEndMode.ClosestTouch)
                .FailOnDestroyedNullOrForbidden(HaulableInd);

            yield return Toils_Collect.CollectInCarrier(CartInd, HaulableInd);

            yield return Toils_Collect.CheckDuplicates(extractA, CartInd, HaulableInd);

            yield return Toils_Jump.JumpIfHaveTargetInQueue(HaulableInd, extractA);

            // JumpIf findStoreCellForCart
            yield return checkStoreCellEmpty;
            {
                // Drop TargetQueue
                Toil extractB = Toils_Collect.Extract(StoreCellInd);
                yield return extractB;

                yield return Toils_Goto.GotoCell(StoreCellInd, PathEndMode.ClosestTouch);

                yield return Toils_Collect.DropTheCarriedInCell(StoreCellInd, ThingPlaceMode.Direct, CartInd);

                yield return Toils_Jump.JumpIfHaveTargetInQueue(StoreCellInd, extractB);
            }

            // Keep the cart if haulables
            if (this.pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling().NullOrEmpty())
            {
              //  yield return checkParkingCellEmpty;

                yield return findParkingSpaceForCart;

                yield return Toils_Goto.GotoCell(StoreCellInd, PathEndMode.OnCell);

                yield return Toils_Cart.DismountAt(CartInd, StoreCellInd);
            }
        }
    }
}
