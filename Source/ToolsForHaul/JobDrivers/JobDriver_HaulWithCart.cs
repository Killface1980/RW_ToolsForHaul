namespace ToolsForHaul.JobDrivers
{
    using System.Collections.Generic;

    using RimWorld;

    using ToolsForHaul.Components;
    using ToolsForHaul.Toils;
    using ToolsForHaul.Vehicles;

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
            hauledThing = TargetThingA;
            if (TargetThingA == null)  // Haul Cart
                hauledThing = CurJob.targetC.Thing;
            IntVec3 destLoc = IntVec3.Invalid;
            string destName = null;
            SlotGroup destGroup = null;

            if (pawn.jobs.curJob.targetB != null)
            {
                destLoc = pawn.jobs.curJob.targetB.Cell;
                destGroup = destLoc.GetSlotGroup(Map);
            }

            if (destGroup != null)
                destName = destGroup.parent.SlotYielderLabel();

            string repString;
            if (destName != null)
                repString = "ReportHaulingTo".Translate(hauledThing.LabelCap, destName);
            else
                repString = "ReportHauling".Translate(hauledThing.LabelCap);

            return repString;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Vehicle_Cart cart = this.CurJob.GetTarget(CartInd).Thing as Vehicle_Cart;

            ///
            // Set fail conditions
            ///

            this.FailOnDestroyedNullOrForbidden(CartInd);

            // Note we only fail on forbidden if the target doesn't start that way
            // This helps haul-aside jobs on forbidden items
            if (!TargetThingA.IsForbidden(pawn.Faction))
            {
                this.FailOnForbidden(CartInd);
            }

            ///
            // Define Toil
            ///

            Toil findParkingSpaceForCart = Toils_Cart.FindParkingSpaceForCartForCart(CartInd);

            Toil checkStoreCellEmpty = Toils_Jump.JumpIf(
                findParkingSpaceForCart,
                () => CurJob.GetTargetQueue(StoreCellInd).NullOrEmpty());
            Toil checkHaulableEmpty = Toils_Jump.JumpIf(
                checkStoreCellEmpty,
                () => CurJob.GetTargetQueue(HaulableInd).NullOrEmpty());

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
                () => { return (cart.GetComp<CompMountable>().Driver == pawn) ? true : false; });

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

            if (this.pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling().NullOrEmpty())
            {
                yield return findParkingSpaceForCart;

                yield return Toils_Goto.GotoCell(StoreCellInd, PathEndMode.OnCell);

                yield return Toils_Cart.DismountAt(CartInd, StoreCellInd);
            }
        }
    }
}
