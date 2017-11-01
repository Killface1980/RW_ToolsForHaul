namespace TFH_Tools.JobDrivers
{
    using System.Collections.Generic;
    using System.Linq;

    using RimWorld;

    using TFH_VehicleHauling.Toils;

    using Verse;
    using Verse.AI;

    public class JobDriver_HaulWithBackpack : JobDriver
    {
        private const TargetIndex BackpackInd = TargetIndex.C;

        // Constants
        private const TargetIndex HaulableInd = TargetIndex.A;

        private const TargetIndex StoreCellInd = TargetIndex.B;

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
            return true; // this.pawn.Reserve(this.TargetThingA, this.job);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Apparel_Backpack backpack = this.job.GetTarget(BackpackInd).Thing as Apparel_Backpack;

            ///
            // Set fail conditions
            ///
            // no free slots
            this.FailOn(() => backpack.slotsComp.slots.Count >= backpack.MaxItem);

            //// hauling stuff not allowed
            // foreach (ThingCategoryDef category in CurJob.targetA.Thing.def.thingCategories)
            // {
            // this.FailOn(() => !backpack.slotsComp.Properties.allowedThingCategoryDefs.Contains(category));
            // this.FailOn(() => backpack.slotsComp.Properties.forbiddenSubThingCategoryDefs.Contains(category));
            // }

            ///
            // Define Toil
            ///

            Toil endOfJob = new Toil();
            endOfJob.initAction = () => { this.EndJobWith(JobCondition.Succeeded); };
            Toil checkStoreCellEmpty = Toils_Jump.JumpIf(
                endOfJob,
                () => this.job.GetTargetQueue(StoreCellInd).NullOrEmpty());
            Toil checkHaulableEmpty = Toils_Jump.JumpIf(
                checkStoreCellEmpty,
                () => this.job.GetTargetQueue(HaulableInd).NullOrEmpty());

            Toil checkBackpackEmpty = Toils_Jump.JumpIf(endOfJob, () => backpack.slotsComp.slots.Count <= 0);

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
                Toil extractA = Toils_Collect.Extract(HaulableInd);
                yield return extractA;

                Toil gotoThing = Toils_Goto.GotoThing(HaulableInd, PathEndMode.ClosestTouch)
                    .FailOnDestroyedOrNull(HaulableInd);
                yield return gotoThing;

                // yield return Toils_Collect.CollectInBackpack(HaulableInd, backpack);
                Toil pickUpThingIntoSlot = new Toil
                {
                    initAction = () =>
                        {
                            if (!backpack.slotsComp.slots.TryAdd(
                                    this.job.targetA.Thing))
                            {
                                this.EndJobWith(JobCondition.Incompletable);
                            }
                        }
                };
                yield return pickUpThingIntoSlot;

                yield return Toils_Collect.CheckDuplicates(gotoThing, BackpackInd, HaulableInd);

                yield return Toils_Jump.JumpIfHaveTargetInQueue(HaulableInd, extractA);
            }

            // JumpIf toilEnd
            yield return checkStoreCellEmpty;
            {
                // Drop TargetQueue
                yield return checkBackpackEmpty;

                Toil extractB = Toils_Collect.Extract(StoreCellInd);
                yield return extractB;

                Toil gotoCell = Toils_Goto.GotoCell(StoreCellInd, PathEndMode.ClosestTouch);
                yield return gotoCell;

                yield return DropTheCarriedFromBackpackInCell(StoreCellInd, ThingPlaceMode.Direct, backpack);

                yield return Toils_Jump.JumpIfHaveTargetInQueue(StoreCellInd, checkBackpackEmpty);

                yield return Toils_Collect.CheckNeedStorageCell(gotoCell, BackpackInd, StoreCellInd);
            }

            yield return endOfJob;
        }

        private static Toil DropTheCarriedFromBackpackInCell(
            TargetIndex StoreCellInd,
            ThingPlaceMode placeMode,
            Apparel_Backpack backpack)
        {
            Toil toil = new Toil();
            toil.initAction = () =>
                {
                    Pawn actor = toil.actor;
                    Job curJob = actor.jobs.curJob;
                    if (backpack.slotsComp.slots.Count <= 0)
                    {
                        return;
                    }

                    // Check dropThing is last item that should not be dropped
                    Thing dropThing = null;

                    dropThing = backpack.slotsComp.slots.First();

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
                        backpack.slotsComp.slots.TryDrop(dropThing, destLoc, actor.Map, placeMode, out dummy);
                    }
                };
            return toil;
        }
    }
}