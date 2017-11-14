namespace TFH_Tools.JobDrivers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using RimWorld;

    using TFH_VehicleHauling.Toils;

    using Verse;
    using Verse.AI;

    public class JobDriver_HaulWithBackpack : JobDriver
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
            Toil endOfJob = new Toil { initAction = () => { this.EndJobWith(JobCondition.Succeeded); } };

            Toil checkStoreCellEmpty = Toils_Jump.JumpIf(endOfJob, () => this.job.GetTargetQueue(StoreCellInd).NullOrEmpty());

            Toil checkHaulableEmpty = Toils_Jump.JumpIf(checkStoreCellEmpty, () => this.job.GetTargetQueue(ToHaulInd).NullOrEmpty());

            //Reserve thing to be stored and storage cell 
            yield return Toils_Reserve.ReserveQueue(ToHaulInd);
            yield return Toils_Reserve.ReserveQueue(StoreCellInd);

            //   .FailOnDestroyedNullOrForbidden(ToHaulInd);

            //Reserve thing to be stored
            //This is redundant relative to MakePreToilReservations(), but the redundancy doesn't hurt, and if we end up looping and grabbing more things, it's necessary

            ///
            // Toils Start
            ///

            // JumpIf checkStoreCellEmpty
            yield return checkHaulableEmpty;
            Pawn actor = this.GetActor();
            {
                // Collect TargetQueue
                Toil extractA = Toils_JobTransforms.ExtractNextTargetFromQueue(ToHaulInd, false);
                yield return extractA;

                //  yield return Toils_Jump.JumpIf(gotoThing, () => );

                {
                    Toil reserve = Toils_Reserve.Reserve(ToHaulInd);
                    yield return reserve;

                    yield return Toils_Goto.GotoThing(ToHaulInd, PathEndMode.ClosestTouch)
                        .FailOnSomeonePhysicallyInteracting(ToHaulInd).FailOnForbidden(ToHaulInd);


                    yield return Toils_TFH.DepositCarriedThingInBackpack(BackpackInd, ToHaulInd);

                    yield return Toils_Haul.StartCarryThing(ToHaulInd, true);
                    yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserve, ToHaulInd, TargetIndex.None);

                }
                yield return Toils_Jump.JumpIfHaveTargetInQueue(ToHaulInd, extractA);
            }

            // JumpIf toilEnd
            yield return checkStoreCellEmpty;
            {
                // Drop TargetQueue

                Toil extractB = Toils_JobTransforms.ExtractNextTargetFromQueue(StoreCellInd, false);
                yield return extractB;


                yield return Toils_Goto.GotoCell(StoreCellInd, PathEndMode.ClosestTouch).FailOnBurningImmobile(StoreCellInd);


                yield return Toils_TFH.PutThingFromBackpackIntoCarrytracker(BackpackInd);

                yield return Toils_Haul.PlaceHauledThingInCell(StoreCellInd, extractB, true);

                yield return Toils_Jump.JumpIfHaveTargetInQueue(StoreCellInd, extractB);

            }

            yield return endOfJob;
        }


    }
}