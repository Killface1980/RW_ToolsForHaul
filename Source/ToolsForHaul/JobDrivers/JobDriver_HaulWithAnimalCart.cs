namespace ToolsForHaul.JobDrivers
{
    using System.Collections.Generic;

    using RimWorld;

    using ToolsForHaul.Vehicles;
    using ToolsForHaul.Toils;

    using Verse;
    using Verse.AI;

    public class JobDriver_HaulWithAnimalCart : JobDriver
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
                hauledThing = this.CurJob.targetC.Thing;
            IntVec3 destLoc = IntVec3.Invalid;
            string destName = null;
            SlotGroup destGroup = null;

            if (this.pawn.jobs.curJob.targetB != null)
            {
                destLoc = this.pawn.jobs.curJob.targetB.Cell;
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

        private Thing HaulThingie => (Pawn)this.CurJob.GetTarget(TargetIndex.A).Thing;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Vehicle_Cart cart = this.CurJob.GetTarget(CartInd).Thing as Vehicle_Cart;
            Job jobNew = new Job();

            ///
            // Set fail conditions
            ///

            this.FailOnDestroyedOrNull(CartInd);
            this.FailOn(() => !cart.MountableComp.IsMounted);

            // Note we only fail on forbidden if the target doesn't start that way
            // This helps haul-aside jobs on forbidden items
            if (!this.TargetThingA.IsForbidden(this.pawn.Faction)) this.FailOnForbidden(CartInd);

            ///
            // Define Toil
            ///

            Toil releaseAnimalCart = Toils_Cart.ReleaseAnimalCart(CartInd);
            Toil checkStoreCellEmpty = Toils_Jump.JumpIf(
                releaseAnimalCart,
                () => this.CurJob.GetTargetQueue(StoreCellInd).NullOrEmpty());
            Toil checkHaulableEmpty = Toils_Jump.JumpIf(
                checkStoreCellEmpty,
                () => this.CurJob.GetTargetQueue(HaulableInd).NullOrEmpty());

            ///
            // Toils Start
            ///

            // Reserve thing to be stored and storage cell 
            yield return Toils_Reserve.Reserve(CartInd);
            yield return Toils_Reserve.ReserveQueue(HaulableInd);
            yield return Toils_Reserve.ReserveQueue(StoreCellInd);

            yield return
                Toils_Goto.GotoThing(CartInd, PathEndMode.Touch)
                    .FailOn(() => cart.Destroyed || !cart.MountableComp.IsMounted);

            // JumpIf toilCheckStoreCellEmpty
            yield return checkHaulableEmpty;
            {
                // Collect TargetQueue
                Toil extractA = Toils_Collect.Extract(HaulableInd);
                yield return extractA;

                yield return Toils_Cart.CallAnimalCart(CartInd, HaulableInd).FailOnDestroyedOrNull(HaulableInd);

                yield return
                    Toils_Goto.GotoThing(HaulableInd, PathEndMode.ClosestTouch).FailOnDestroyedOrNull(HaulableInd);

                yield return
                    Toils_Cart.CallAnimalCart(CartInd, HaulableInd, this.pawn).FailOnDestroyedOrNull(HaulableInd);

                yield return Toils_Cart.WaitForAnimalCart(CartInd, HaulableInd);

                yield return Toils_Collect.CollectInCarrier(CartInd, HaulableInd);

                yield return Toils_Collect.CheckDuplicates(extractA, CartInd, HaulableInd);

                yield return Toils_Jump.JumpIfHaveTargetInQueue(HaulableInd, extractA);
            }

            // JumpIf releaseAnimalCart
            yield return checkStoreCellEmpty;
            {
                // Drop TargetQueue
                Toil extractB = Toils_Collect.Extract(StoreCellInd);
                yield return extractB;

                yield return Toils_Cart.CallAnimalCart(CartInd, StoreCellInd);

                yield return
                    Toils_Goto.GotoCell(StoreCellInd, PathEndMode.ClosestTouch).FailOnBurningImmobile(StoreCellInd);

                yield return Toils_Cart.CallAnimalCart(CartInd, HaulableInd, this.pawn);

                yield return Toils_Cart.WaitForAnimalCart(CartInd, HaulableInd);

                yield return Toils_Collect.DropTheCarriedInCell(StoreCellInd, ThingPlaceMode.Direct, CartInd);

                yield return Toils_Jump.JumpIfHaveTargetInQueue(StoreCellInd, extractB);
            }

            yield return releaseAnimalCart;
        }
    }
}