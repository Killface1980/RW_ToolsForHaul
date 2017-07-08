namespace TFH_VehicleBase.JobDrivers
{
    using System.Collections.Generic;

    using TFH_VehicleBase.Components;

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

        protected override IEnumerable<Toil> MakeNewToils()
        {
            ///
            // Set fail conditions
            ///

            this.FailOnDestroyedOrNull(MountableInd);

            // Note we only fail on forbidden if the target doesn't start that way
            // This helps haul-aside jobs on forbidden items
            if (!((Vehicle_Cart)this.TargetThingA).ClaimableBy(this.pawn.Faction))
            {
                this.FailOnForbidden(MountableInd);
            }

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
                    this.TargetThingA.TryGetComp<CompMountable>().MountOn(actor);
                };

            yield return toilMountOn;

        }
    }
}
