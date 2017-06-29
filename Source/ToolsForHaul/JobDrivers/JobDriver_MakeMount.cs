using System.Collections.Generic;
using RimWorld;
using ToolsForHaul.Components;
using ToolsForHaul.JobDefs;
using Verse;
using Verse.AI;

namespace ToolsForHaul.JobDrivers
{
    using ToolsForHaul.Components.Vehicle;
    using ToolsForHaul.Components.Vehicles;

    public class JobDriver_MakeMount : JobDriver
    {
        // Constants
        private const TargetIndex MountableInd = TargetIndex.A;
        private const TargetIndex DriverInd = TargetIndex.B;

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
            this.FailOnDowned(DriverInd);

            // Note we only fail on forbidden if the target doesn't start that way
            // This helps haul-aside jobs on forbidden items
            if (!this.TargetThingA.IsForbidden(this.pawn.Faction)) this.FailOnForbidden(MountableInd);

            ///
            // Define Toil
            ///

            Toil toilMakeStandby = new Toil();
            toilMakeStandby.initAction = () =>
                {
                    Pawn driver = this.CurJob.GetTarget(DriverInd).Thing as Pawn;
                    driver.jobs.StartJob(
                        new Job(
                            HaulJobDefOf.StandBy,
                            driver.Position,
                            2400 + (int)((this.pawn.Position - driver.Position).LengthHorizontal * 120)),
                        JobCondition.InterruptForced);
                };

            Toil toilGoto = null;
            toilGoto = Toils_Goto.GotoThing(MountableInd, PathEndMode.ClosestTouch).FailOn(
                () =>
                    {
                        // Note we don't fail on losing hauling designation
                        // Because that's a special case anyway

                        // While hauling to cell storage, ensure storage dest is still valid
                        Pawn actor = toilGoto.actor;
                        Job curJob = actor.jobs.curJob;
                        if (curJob.haulMode == HaulMode.ToCellStorage)
                        {
                            Thing haulThing = curJob.GetTarget(MountableInd).Thing;

                            IntVec3 destLoc = actor.jobs.curJob.GetTarget(TargetIndex.B).Cell;
                            if (!destLoc.IsValidStorageFor(actor.Map,haulThing)) return true;
                        }

                        return false;
                    });

            Toil toilMountOn = new Toil();
            toilMountOn.initAction = () =>
                {
                    Pawn driver = this.TargetB.Thing as Pawn;
                    if (driver != null && this.TargetThingA.TryGetComp<CompMountable>() != null)
                    {
                        this.TargetThingA.TryGetComp<CompMountable>().MountOn(driver);
                    }
                    else
                    {
                        Log.Error(this.GetActor().LabelCap + ": Try make mount without target B Driver");
                        this.EndJobWith(JobCondition.Errored);
                    }
                };

            Toil toilEnd = new Toil();
            toilEnd.initAction = () =>
                {
                    Vehicle_Cart cart = this.CurJob.GetTarget(MountableInd).Thing as Vehicle_Cart;

                    // Vehicle_Saddle saddle = CurJob.GetTarget(MountableInd).Thing as Vehicle_Saddle;
                    if (cart == null)
                    {
                        // && saddle == null)
                        Log.Error(this.GetActor().LabelCap + ": MakeMount get TargetA not cart or saddle.");
                        this.EndJobWith(JobCondition.Errored);
                        return;
                    }

                    if (cart != null && cart.MountableComp.IsMounted
                        && cart.MountableComp.Driver.CurJob.def == HaulJobDefOf.StandBy) cart.MountableComp.Driver.jobs.curDriver.EndJobWith(JobCondition.Succeeded);
                    this.EndJobWith(JobCondition.Succeeded);
                };

            ///
            // Toils Start
            ///

            // Reserve thing to be stored and storage cell 
            yield return Toils_Reserve.Reserve(MountableInd);

            yield return Toils_Reserve.Reserve(DriverInd);

            yield return toilMakeStandby;

            yield return toilGoto;

            yield return Toils_Haul.StartCarryThing(MountableInd);

            yield return Toils_Haul.CarryHauledThingToCell(DriverInd);

            yield return toilMountOn;

            yield return toilEnd;
        }
    }
}
