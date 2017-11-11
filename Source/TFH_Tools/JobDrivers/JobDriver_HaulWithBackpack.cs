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
        private const TargetIndex HaulableInd = TargetIndex.A;

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
            //  base.pawn.ReserveAsManyAsPossible(base.job.GetTargetQueue(TargetIndex.A), base.job, 1, -1, null);
            //  base.pawn.ReserveAsManyAsPossible(base.job.GetTargetQueue(TargetIndex.B), base.job, 1, -1, null);

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

            Toil checkHaulableEmpty = Toils_Jump.JumpIf(checkStoreCellEmpty, () => this.job.GetTargetQueue(HaulableInd).NullOrEmpty());
            Toil extractA = Toils_Collect.Extract(HaulableInd);
            Toil extractB = Toils_Collect.Extract(StoreCellInd);

            Toil gotoThing = Toils_Goto.GotoThing(HaulableInd, PathEndMode.ClosestTouch)
                .FailOnSomeonePhysicallyInteracting(HaulableInd)
                .FailOnDestroyedNullOrForbidden(HaulableInd);

            ///
            // Toils Start
            ///

            // Reserve thing to be stored and storage cell 
            yield return Toils_Reserve.ReserveQueue(HaulableInd);
            yield return Toils_Reserve.ReserveQueue(StoreCellInd);

            // JumpIf checkStoreCellEmpty
            yield return checkHaulableEmpty;
            {
                // Collect TargetQueue
                yield return extractA;
                yield return gotoThing;
                yield return Toils_General.WaitWith(HaulableInd, 60, true);
                yield return Toils_Haul.StartCarryThing(HaulableInd);
                yield return PutCarriedThingInBackpack();

                //   yield return CollectInBackpack(HaulableInd);
                yield return Toils_Collect.CheckDuplicates(gotoThing, BackpackInd, HaulableInd);
                yield return Toils_Jump.JumpIfHaveTargetInQueue(HaulableInd, extractA);
            }

            // JumpIf toilEnd
            yield return checkStoreCellEmpty;
            {
                // Drop TargetQueue
                yield return extractB;

                yield return Toils_Goto.GotoCell(StoreCellInd, PathEndMode.ClosestTouch)
                    .FailOnBurningImmobile(StoreCellInd);

                yield return DropTheCarriedFromBackpackInCell(StoreCellInd, ThingPlaceMode.Direct);

                yield return Toils_Jump.JumpIfHaveTargetInQueue(StoreCellInd, extractB);
            }

            yield return endOfJob;
        }

        private static Toil DropTheCarriedFromBackpackInCell(
            TargetIndex StoreCellInd,
            ThingPlaceMode placeMode)
        {
            Toil toil = new Toil();
            toil.initAction = () =>
                {
                    Pawn actor = toil.actor;
                    Job curJob = actor.jobs.curJob;
                    if (actor.inventory.innerContainer.Count <= 0)
                    {
                        return;
                    }

                    // Check dropThing is last item that should not be dropped
                    Thing dropThing = null;

                    dropThing = actor.inventory.innerContainer.First();

                    if (dropThing == null)
                    {
                        Log.Error(
                            toil.actor + " tried to drop null thing in "
                            + actor.jobs.curJob.GetTarget(StoreCellInd).Cell);
                        return;
                    }

                    IntVec3 destLoc = actor.jobs.curJob.GetTarget(StoreCellInd).Cell;
                    Thing dummy;

                    if (destLoc.GetStorable(actor.Map) == null)
                    {
                        actor.Map.designationManager.RemoveAllDesignationsOn(dropThing);
                        actor.inventory.innerContainer.TryDrop(dropThing, destLoc, actor.Map, placeMode, out dummy);
                    }
                };
            return toil;
        }

        public static Toil CollectInBackpack(TargetIndex HaulableInd)
        {

            Toil toil = new Toil();
            toil.initAction = () =>
                {
                    Pawn actor = toil.actor;
                    Job curJob = actor.jobs.curJob;
                    Thing haulThing = curJob.GetTarget(HaulableInd).Thing;

                    actor.TryGetBackpack().slotsComp.innerContainer.TryAdd(haulThing.SplitOff(haulThing.stackCount));

                };

            toil.FailOn(() =>
                {
                    Pawn actor = toil.actor;
                    Job curJob = actor.jobs.curJob;
                    Thing haulThing = curJob.GetTarget(HaulableInd).Thing;

                    if (!actor.TryGetBackpack().slotsComp.innerContainer.CanAcceptAnyOf(haulThing))
                    {
                        return true;
                    }

                    return false;
                });
            return toil;
        }

        public static Toil PutCarriedThingInBackpack()
        {
            Toil toil = new Toil();
            toil.initAction = () =>
                {
                    var actor = toil.GetActor();
                    if (actor.carryTracker.CarriedThing != null)
                    {
                        //Try transfer to inventory
                        if (!actor.carryTracker.innerContainer.TryTransferToContainer(actor.carryTracker.CarriedThing, actor.TryGetBackpack().slotsComp.innerContainer))
                        {
                            //Failed: try drop
                            Thing unused;
                            actor.carryTracker.TryDropCarriedThing(actor.Position, actor.carryTracker.CarriedThing.stackCount, ThingPlaceMode.Near, out unused);
                        }
                    }
                };
            return toil;
        }

    }
}