using System.Collections.Generic;
using RimWorld;
using ToolsForHaul.Toils;
using Verse;
using Verse.AI;

namespace ToolsForHaul.JobDrivers
{
    public class JobDriver_HaulWithCart : JobDriver
    {
        // Constants
        private const TargetIndex HaulableInd = TargetIndex.A;
        private const TargetIndex StoreCellInd = TargetIndex.B;
        private const TargetIndex CartInd = TargetIndex.C;

        public override string GetReport()
        {
            Thing hauledThing = this.TargetThingA;
            if (this.TargetThingA == null)  // Haul Cart
                hauledThing = this.CurJob.targetC.Thing;
            this.FailOn(() => !this.pawn.CanReach(hauledThing, PathEndMode.ClosestTouch, Danger.Some));
            IntVec3 destLoc = IntVec3.Invalid;
            string destName = null;
            SlotGroup destGroup = null;

            if (this.pawn.jobs.curJob.targetB != null)
            {
                destLoc = this.pawn.jobs.curJob.targetB.Cell;
                destGroup = destLoc.GetSlotGroup();
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

            this.FailOnDestroyedOrNull(CartInd);

            // Note we only fail on forbidden if the target doesn't start that way
            // This helps haul-aside jobs on forbidden items
            if (!this.TargetThingA.IsForbidden(this.pawn.Faction)) this.FailOnForbidden(CartInd);

            this.FailOn(() => !this.pawn.RaceProps.IsFlesh || !this.pawn.RaceProps.Humanlike);

            ///
            // Define Toil
            ///

            Toil findStoreCellForCart = Toils_Cart.FindStoreCellForCart(CartInd);
            Toil checkCartEmpty = Toils_Jump.JumpIf(findStoreCellForCart, () => cart.Storage.Count <= 0);

            Toil checkStoreCellEmpty = Toils_Jump.JumpIf(
                findStoreCellForCart,
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

            // JumpIf already mounted
            yield return Toils_Jump.JumpIf(
                checkHaulableEmpty,
                () =>
                    {
                        if (cart.MountableComp.Driver == this.pawn) return true;
                        return false;
                    });

            // Mount on Target
            yield return Toils_Goto.GotoThing(CartInd, PathEndMode.ClosestTouch).FailOnDestroyedOrNull(CartInd);
            yield return Toils_Cart.MountOn(CartInd);

            // JumpIf checkStoreCellEmpty
            yield return checkHaulableEmpty;
            {
                // Collect TargetQueue
                Toil extractA = Toils_Collect.Extract(HaulableInd);
                yield return extractA;

                Toil gotoThing =
                    Toils_Goto.GotoThing(HaulableInd, PathEndMode.ClosestTouch).FailOnDestroyedOrNull(HaulableInd);
                yield return gotoThing;

                yield return Toils_Collect.CollectInCarrier(CartInd, HaulableInd);

                yield return Toils_Collect.CheckDuplicates(gotoThing, CartInd, HaulableInd);

                yield return Toils_Jump.JumpIfHaveTargetInQueue(HaulableInd, extractA);
            }

            // JumpIf findStoreCellForCart
            yield return checkStoreCellEmpty;
            {
                // Drop TargetQueue
                yield return checkCartEmpty;

                Toil extractB = Toils_Collect.Extract(StoreCellInd);
                yield return extractB;

                Toil gotoCell = Toils_Goto.GotoCell(StoreCellInd, PathEndMode.ClosestTouch);
                yield return gotoCell;

                yield return Toils_Collect.DropTheCarriedInCell(StoreCellInd, ThingPlaceMode.Direct, CartInd);

                yield return Toils_Jump.JumpIfHaveTargetInQueue(StoreCellInd, checkCartEmpty);

                yield return Toils_Collect.CheckNeedStorageCell(gotoCell, CartInd, StoreCellInd);
            }

            yield return findStoreCellForCart;

            yield return Toils_Goto.GotoCell(StoreCellInd, PathEndMode.OnCell);

            yield return Toils_Cart.DismountAt(CartInd, StoreCellInd);
        }
    }
}
