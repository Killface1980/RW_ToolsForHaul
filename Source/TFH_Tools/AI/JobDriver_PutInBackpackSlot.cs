namespace TFH_Tools.JobDrivers
{
    using System.Collections.Generic;

    using Verse;
    using Verse.AI;

    public class JobDriver_PutInBackpackSlot : JobDriver
    {
        // Constants
        public const TargetIndex HaulableInd = TargetIndex.A;

        public const TargetIndex BackpackInd = TargetIndex.B;

        public override string GetReport()
        {
            Thing hauledThing = this.TargetThingA;

            string repString;
            if (hauledThing != null)
            {
                repString = "ReportPutInInventory".Translate(
                    hauledThing.LabelCap,
                    this.job.GetTarget(BackpackInd).Thing.LabelCap);
            }
            else
            {
                repString = "ReportPutSomethingInInventory".Translate(this.job.GetTarget(BackpackInd).Thing.LabelCap);
            }

            return repString;
        }

        public override bool TryMakePreToilReservations()
        {
            return this.pawn.Reserve(this.TargetThingA, this.job);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Apparel_Backpack backpack = this.job.GetTarget(BackpackInd).Thing as Apparel_Backpack;

            // no free innerContainer
            this.FailOn(() => backpack.slotsComp.innerContainer.Count >= backpack.MaxItem);

            // reserve resources
            yield return Toils_Reserve.ReserveQueue(HaulableInd);

            // extract next target thing from targetQueue
            Toil toilExtractNextTarget = Toils_JobTransforms.ExtractNextTargetFromQueue(HaulableInd);
            yield return toilExtractNextTarget;

            Toil toilGoToThing = Toils_Goto.GotoThing(HaulableInd, PathEndMode.ClosestTouch)
                .FailOnDespawnedOrNull(HaulableInd);
            yield return toilGoToThing;

            Toil pickUpThingIntoSlot = new Toil
            {
                initAction = () =>
                    {
                        if (!backpack.slotsComp.innerContainer.TryAddOrTransfer(this.TargetThingA.SplitOff(this.TargetThingA.stackCount)))
                        {
                            this.EndJobWith(JobCondition.Incompletable);
                        }
                    }
            };
            yield return pickUpThingIntoSlot;

            yield return Toils_Jump.JumpIfHaveTargetInQueue(HaulableInd, toilExtractNextTarget);
        }
    }
}