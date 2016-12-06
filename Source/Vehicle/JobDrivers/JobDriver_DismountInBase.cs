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
    using ToolsForHaul.Components.Vehicles;

    public class JobDriver_DismountInBase : JobDriver
    {
        // Constants
        private const TargetIndex CartInd = TargetIndex.A;
        private const TargetIndex StoreCellInd = TargetIndex.B;

        public override string GetReport()
        {
            ThingWithComps cart = this.TargetThingA as ThingWithComps;

            IntVec3 destLoc = new IntVec3(-1000, -1000, -1000);
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

            // Note we only fail on forbidden if the target doesn't start that way
            // This helps haul-aside jobs on forbidden items
            if (!this.TargetThingA.IsForbidden(this.pawn.Faction)) this.FailOnForbidden(CartInd);

            ThingWithComps cart = this.TargetThingA as ThingWithComps;

            if (ToolsForHaulUtility.FindStorageCell(this.pawn, cart) == IntVec3.Invalid)
            {
                JobFailReason.Is(ToolsForHaulUtility.NoEmptyPlaceForCart);
            }

            if (cart.TryGetComp<CompMountable>().Driver != null)
            {
                this.FailOnSomeonePhysicallyInteracting(CartInd);
            }

            ///
            // Define Toil
            ///

            Toil toilGoToCell = Toils_Goto.GotoCell(StoreCellInd, PathEndMode.ClosestTouch);

            ///
            // Toils Start
            ///

            // Reserve thing to be stored and storage cell 
            yield return Toils_Reserve.Reserve(CartInd);
            yield return Toils_Reserve.Reserve(StoreCellInd);

            // JumpIf already mounted
            yield return
                Toils_Jump.JumpIf(
                    toilGoToCell,
                    () => { return cart.TryGetComp<CompMountable>().Driver == this.pawn ? true : false; });

            // Mount on Target
            yield return Toils_Goto.GotoThing(CartInd, PathEndMode.ClosestTouch).FailOnDestroyedOrNull(CartInd);
            yield return Toils_Cart.MountOn(CartInd);

            // Dismount
            yield return toilGoToCell;

            yield return Toils_Cart.DismountAt(CartInd, StoreCellInd);
        }
    }
}
