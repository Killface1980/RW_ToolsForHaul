namespace TFH_Tools.JobDrivers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using RimWorld;

    using TFH_VehicleHauling.Toils;

    using Verse;
    using Verse.AI;

    public class JobDriver_EmptyBackpack : JobDriver
    {

        // Constants
        private const TargetIndex ToHaulInd = TargetIndex.A;

        private const TargetIndex StoreCellInd = TargetIndex.B;

        private const TargetIndex BackpackInd = TargetIndex.C;
        public override string GetReport()
        {
            Thing hauledThing = null;
            hauledThing = this.TargetThingA;
            IntVec3 destLoc = IntVec3.Invalid;
            string destName = null;
            SlotGroup destGroup = null;

            if (this.pawn.jobs.curJob.targetB != null)
            {
                destLoc = this.pawn.jobs.curJob.targetB.Cell;
                destGroup = destLoc.GetSlotGroup(this.Map);
            }

            this.FailOn(() => !this.pawn.CanReserveAndReach(this.TargetThingA, PathEndMode.ClosestTouch, Danger.Some));

            if (destGroup != null)
            {
                destName = destGroup.parent.SlotYielderLabel();
            }

            string repString;
            if (destName != null && hauledThing != null)
            {
                repString = "ReportHaulingTo".Translate(hauledThing.LabelCap, destName);
            }
            else if (hauledThing != null)
            {
                repString = "ReportHauling".Translate(hauledThing.LabelCap);
            }
            else
            {
                repString = "ReportHauling".Translate();
            }

            return repString;
        }

        public override bool TryMakePreToilReservations()
        {
            base.pawn.ReserveAsManyAsPossible(base.job.GetTargetQueue(TargetIndex.A), base.job, 1, -1, null);
            base.pawn.ReserveAsManyAsPossible(base.job.GetTargetQueue(TargetIndex.B), base.job, 1, -1, null);

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            ///
            // Set fail conditions
            ///

            ///
            // Define Toil
            ///

            //Reserve thing to be stored and storage cell 
            yield return Toils_Reserve.ReserveQueue(ToHaulInd);
            yield return Toils_Reserve.ReserveQueue(StoreCellInd);

            //   .FailOnDestroyedNullOrForbidden(ToHaulInd);

            //Reserve thing to be stored
            //This is redundant relative to MakePreToilReservations(), but the redundancy doesn't hurt, and if we end up looping and grabbing more things, it's necessary

            ///
            // Toils Start
            ///

            Toil startExtractionA = Toils_JobTransforms.ExtractNextTargetFromQueue(ToHaulInd, false);
            Toil startExtractionB = Toils_JobTransforms.ExtractNextTargetFromQueue(StoreCellInd, false);
            // JumpIf checkStoreCellEmpty

            // JumpIf toilEnd
            Toil endOfJob = new Toil { initAction = () => { this.EndJobWith(JobCondition.Succeeded); } };

            {
                Toil checkStoreCellEmpty = Toils_Jump.JumpIf(
                    endOfJob,
                    () => this.job.GetTargetQueue(StoreCellInd).NullOrEmpty());
                yield return checkStoreCellEmpty;
            }

            // JumpIf emptying backpack

            yield return startExtractionA;
            {
                yield return startExtractionB;
                Toil reserve = Toils_Reserve.Reserve(StoreCellInd);
                yield return reserve;
                Toil reserve2 = Toils_Reserve.Reserve(ToHaulInd);
                yield return reserve2;
                //    yield return Toils_Jump.JumpIf(goToStoreCell, () => actor.carryTracker.CarriedThing != null);
                Toil goTo = Toils_Goto.GotoCell(StoreCellInd, PathEndMode.ClosestTouch)
                    .FailOnBurningImmobile(StoreCellInd);
                yield return Toils_TFH.PutThingFromBackpackIntoCarrytracker(BackpackInd);
                //    yield return new Toil { initAction = () => { actor.jobs.curJob.targetA = this.TargetB; } };

                //  yield return Toils_Haul.CarryHauledThingToCell(StoreCellInd).FailOnBurningImmobile(StoreCellInd);

                yield return goTo;

                yield return Toils_Haul.PlaceHauledThingInCell(StoreCellInd, reserve, true);
            }

            yield return Toils_Jump.JumpIfHaveTargetInQueue(StoreCellInd, startExtractionA);

            yield return endOfJob;
        }
    }
}