namespace TFH_VehicleBase.JobDrivers
{
    using System.Collections.Generic;

    using TFH_VehicleBase.Components;
    using TFH_VehicleBase.DefOfs_TFH;

    using Verse;
    using Verse.AI;

    public class JobDriver_Mount : JobDriver
    {
        // Constants
        private const TargetIndex MountableInd = TargetIndex.A;

        public override string GetReport()
        {

            string repString;
            repString = "ReportMounting".Translate(this.TargetThingA.LabelCap);

            return repString;
        }

        public override bool TryMakePreToilReservations()
        {
            return this.pawn.Reserve(this.TargetThingA, this.job);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            ///
            // Set fail conditions
            ///

            this.FailOnDestroyedOrNull(MountableInd);

            // // Note we only fail on forbidden if the target doesn't start that way
            // // This helps haul-aside jobs on forbidden items
            // if (!((Vehicle_Cart)this.TargetThingA).ClaimableBy(this.pawn.Faction))
            // {
            // this.FailOnForbidden(MountableInd);
            // }

            ///
            // Define Toil
            ///
            Toil toilMakeStandby = new Toil
                                       {
                                           initAction = () =>
                                               {
                                                   BasicVehicle vehicle =
                                                       this.job.GetTarget(MountableInd).Thing as BasicVehicle;
                                                   vehicle.jobs.StartJob(
                                                       new Job(
                                                           VehicleJobDefOf.StandBy,
                                                           vehicle.Position,
                                                           300 + (int)((this.pawn.Position - vehicle.Position)
                                                                       .LengthHorizontal * 60)),
                                                       JobCondition.InterruptForced);
                                               }
                                       };

            Toil toilMountOn = new Toil();
            toilMountOn.initAction = () =>
                {
                    Pawn actor = toilMountOn.actor;
                    this.TargetThingA.TryGetComp<CompMountable>().MountOn(actor);
                };

            Toil toilEnd = new Toil();
            toilEnd.initAction = () =>
                {
                    BasicVehicle cart = job.GetTarget(MountableInd).Thing as BasicVehicle;
                    if (cart == null)
                    {
                        Log.Error(GetActor().LabelCap + ": MakeMount get TargetA not cart or saddle.");
                        EndJobWith(JobCondition.Errored);
                        return;
                    }

                    if (cart.MountableComp.IsMounted)
                    {
                        cart.MountableComp.Rider.jobs.curDriver.EndJobWith(JobCondition.Succeeded);
                    }

                    EndJobWith(JobCondition.Succeeded);
                };
      

            ///
            // Toils Start
            ///

            // Reserve tvehicle 
            yield return Toils_Reserve.Reserve(MountableInd);

            if ((this.job.GetTarget(MountableInd).Thing as BasicVehicle).RaceProps.Animal)
            {
                yield return toilMakeStandby;
            }

            // Mount on Target
            yield return Toils_Goto.GotoThing(MountableInd, PathEndMode.InteractionCell);



            yield return toilMountOn;

            yield return toilEnd;

        }
    }
}
