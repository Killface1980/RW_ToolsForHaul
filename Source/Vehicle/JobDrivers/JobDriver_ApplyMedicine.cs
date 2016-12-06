using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul.JobDrivers
{
    public class JobDriver_ApplyMedicine : JobDriver
    {
        private const int BaseTreatmentDuration = 600;

        protected Thing Medicine => this.CurJob.targetB.Thing;

        protected Pawn Deliveree => (Pawn)this.CurJob.targetA.Thing;

        public override string GetReport()
        {
            string repString;
            repString = "TreatPatient.reportString".Translate(this.TargetThingA.LabelCap);

            return repString;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            ///
            // Set fail conditions
            ///

            this.FailOnDestroyedOrNull(TargetIndex.A);
            AddEndCondition(
                () =>
                    {
                        return this.Deliveree.health.ShouldBeTendedNow ? JobCondition.Ongoing : JobCondition.Succeeded;
                    });

            // Note we only fail on forbidden if the target doesn't start that way
            // This helps haul-aside jobs on forbidden items

            ///
            // Define Toil
            ///

            ///
            // Toils Start
            ///

            // Reserve thing to be stored and storage cell 
            yield return Toils_Reserve.Reserve(TargetIndex.A);

            StatWorker statWorker = new StatWorker();
            statWorker.InitSetStat(StatDefOf.BaseHealingQuality);

            Toil toilApplyMedicine = new Toil();
            toilApplyMedicine.initAction = () =>
                {
                    Thing dummy;
                    this.Medicine.holder.TryDrop(
                        this.Medicine,
                        this.pawn.Position + IntVec3.North.RotatedBy(this.pawn.Rotation),
                        ThingPlaceMode.Direct,
                        out dummy);
                };
            yield return toilApplyMedicine;

            yield return Toils_Tend.PickupMedicine(TargetIndex.B, this.Deliveree);

            Toil toilGoTodeliveree = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return toilGoTodeliveree;

            int duration = (int)(1.0 / this.pawn.GetStatValue(StatDefOf.HealingSpeed) * 600.0);
            Toil toilDelivereeWait = new Toil();
            toilDelivereeWait.initAction =
                () => { this.Deliveree.drafter.TakeOrderedJob(new Job(JobDefOf.Wait, duration)); };

            yield return Toils_General.Wait(duration);

            yield return Toils_Tend.FinalizeTend(this.Deliveree);

            yield return Toils_Jump.Jump(toilGoTodeliveree);
        }
    }
}
