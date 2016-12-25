using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public class JobGiver_TakeToWheelChair : ThinkNode_JobGiver
    {
        private const float MinDistFromEnemy = 25f;

        private float radius = 30f;

        public override ThinkNode DeepCopy()
        {
            JobGiver_TakeToWheelChair jobGiverTakeToWheelChair = (JobGiver_TakeToWheelChair)base.DeepCopy();
            jobGiverTakeToWheelChair.radius = radius;
            return jobGiverTakeToWheelChair;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!InteractionUtility.CanInitiateInteraction(pawn))
            {
                return null;
            }

            Pawn patient = SickPawnVisitUtility.FindRandomSickPawn(pawn, JoyCategory.High);
            if (patient == null)
            {
                return null;
            }

            Thing wheelChair = ToolsForHaulUtility.FindWheelChair(patient, pawn);
            if (wheelChair == null || !pawn.CanReserve(wheelChair))
            {
                return null;
            }

            return new Job(DefDatabase<JobDef>.GetNamed("TakeToWheelChair"), patient, wheelChair)
            {
                count = 1
            };
        }
    }
}
