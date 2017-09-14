namespace TFH_VehicleHauling.JobDrivers
{
    using System.Collections.Generic;
    using System.Diagnostics;

    using RimWorld;

    using Verse;
    using Verse.AI;

    public class JobDriver_HaulToContainer_WithVehicle : JobDriver
    {
        private const TargetIndex CarryThingIndex = TargetIndex.A;

        private const TargetIndex DestIndex = TargetIndex.B;

        private const TargetIndex PrimaryDestIndex = TargetIndex.C;

        private Thing Container
        {
            get
            {
                return (Thing)base.CurJob.GetTarget(DestIndex);
            }
        }

        public override string GetReport()
        {
            Thing thing;
            if (this.pawn.carryTracker.CarriedThing != null)
            {
                thing = this.pawn.carryTracker.CarriedThing;
            }
            else
            {
                thing = base.TargetThingA;
            }

            return "ReportHaulingTo".Translate(new object[]
                                                   {
                                                       thing.LabelCap,
                                                       base.CurJob.targetB.Thing.LabelShort
                                                   });
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(CarryThingIndex);
            this.FailOnDestroyedNullOrForbidden(DestIndex);
            this.FailOn(() => TransporterUtility.WasLoadingCanceled(this.Container));
            yield return Toils_Reserve.Reserve(CarryThingIndex, 1, -1, null);
            yield return Toils_Reserve.ReserveQueue(CarryThingIndex, 1, -1, null);
            yield return Toils_Reserve.Reserve(DestIndex, 1, -1, null);
            yield return Toils_Reserve.ReserveQueue(DestIndex, 1, -1, null);
            Toil getToHaulTarget = Toils_Goto.GotoThing(CarryThingIndex, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(CarryThingIndex);
            yield return getToHaulTarget;
            yield return Toils_Construct.UninstallIfMinifiable(CarryThingIndex).FailOnSomeonePhysicallyInteracting(CarryThingIndex);
            yield return Toils_Haul.StartCarryThing(CarryThingIndex, false, true);
            yield return Toils_Haul.JumpIfAlsoCollectingNextTargetInQueue(getToHaulTarget, CarryThingIndex);
            Toil carryToContainer = Toils_Haul.CarryHauledThingToContainer();
            yield return carryToContainer;
            yield return Toils_Goto.MoveOffTargetBlueprint(DestIndex);
            yield return Toils_Construct.MakeSolidThingFromBlueprintIfNecessary(DestIndex, PrimaryDestIndex);
            yield return Toils_Haul.DepositHauledThingInContainer(DestIndex, PrimaryDestIndex);
            yield return Toils_Haul.JumpToCarryToNextContainerIfPossible(carryToContainer, PrimaryDestIndex);
        }
    }
}
