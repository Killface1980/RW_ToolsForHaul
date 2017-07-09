namespace TFH_VehicleBase.JobDrivers
{
    using System.Collections.Generic;

    using TFH_VehicleBase.Components;

    using Verse;
    using Verse.AI;

    public class JobDriver_MountAnimal : JobDriver
    {
        // Constants
        private const TargetIndex MountableInd = TargetIndex.A;

        public override string GetReport()
        {

            string repString;
            repString = "ReportMounting".Translate(this.TargetThingA.LabelCap);

            return repString;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            ///
            // Set fail conditions
            ///

            this.FailOnDestroyedOrNull(MountableInd);

            ///
            // Define Toil
            ///

            ///
            // Toils Start
            ///

            // Reserve tvehicle 
            yield return Toils_Reserve.Reserve(MountableInd);

            // Mount on Target
            yield return Toils_Goto.GotoThing(MountableInd, PathEndMode.InteractionCell);

            Toil toilMountOn = new Toil();
            toilMountOn.initAction = () =>
                {
                    Pawn actor = toilMountOn.actor;
                    this.TargetThingA.TryGetComp<CompRideable>().MountOn(actor);
                };

            yield return toilMountOn;

        }
    }
}
