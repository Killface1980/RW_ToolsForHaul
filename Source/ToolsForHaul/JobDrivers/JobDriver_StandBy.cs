namespace ToolsForHaul.JobDrivers
{
    using System.Collections.Generic;

    using Verse;
    using Verse.AI;

    class JobDriver_StandBy : JobDriver
    {
        // Constants
        private const TargetIndex DestInd = TargetIndex.A;

        public override string GetReport()
        {
            string repString;
            repString = "ReportStandby".Translate();

            return repString;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            ///
            // Set fail conditions
            ///

            this.FailOn(() => !this.TargetA.Cell.IsValid);
            this.FailOnBurningImmobile(DestInd);

            ///
            // Define Toil
            ///



            ///
            // Toils Start
            ///

            yield return Toils_Goto.GotoCell(DestInd, PathEndMode.ClosestTouch);

            yield return Toils_General.Wait(this.CurJob.expiryInterval);

        }

    }
}
