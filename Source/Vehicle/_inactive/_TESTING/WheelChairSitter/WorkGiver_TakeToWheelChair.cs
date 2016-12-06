using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public class WorkGiver_TakeToWheelChair : WorkGiver_TakeToVehicle
    {

        private const float MinDistFromEnemy = 40f;

        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.InteractionCell;
            }
        }

        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForGroup(ThingRequestGroup.Pawn);
            }
        }
        public override bool ShouldSkip(Pawn pawn)
        {
            return !InteractionUtility.CanInitiateInteraction(pawn);
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            Pawn patient = t as Pawn;
            if (patient == null)
            {
                return null;
            }

            if (!SickPawnVisitUtility.CanVisit(pawn, patient, JoyCategory.High))
            {
                return null;
            }

            if (patient.health.capacities.GetEfficiency(PawnCapacityDefOf.Moving) > 0.6f)
                return null;
            if (patient.health.capacities.GetEfficiency(PawnCapacityDefOf.Consciousness) < 0.6f)
                return null;

            Thing t2 = FindWheelChair(pawn, patient);
            if (t2 != null)
                return new Job(DefDatabase<JobDef>.GetNamed("TakeToWheelChair"), patient, t2)
                {
                    maxNumToCarry = 1
                };
            return null;
        }
    }
}
