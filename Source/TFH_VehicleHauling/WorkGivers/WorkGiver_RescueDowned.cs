namespace TFH_VehicleHauling.WorkGivers
{
    using System.Collections.Generic;
    using System.Linq;

    using RimWorld;

    using TFH_VehicleBase;

    using Verse;
    using Verse.AI;

    public class WorkGiver_RescueVehicleDowned : WorkGiver_TakeToBed
    {
        private const float MinDistFromEnemy = 40f;

        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.OnCell;
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
            Trace.DebugWriteHaulingPawn(pawn);

            pawn.AvailableVehicles(out List<Thing> availableVehicles);
            if (availableVehicles.NullOrEmpty())
            {
                return true;
            }

            if (TFH_BaseUtility.GetRightVehicle(pawn, availableVehicles, WorkTypeDefOf.Hauling) == null)
            {
                return true;
            }

            if (pawn.RaceProps.Animal)
            {
                return true;
            }

            return false;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Pawn pawn2 = t as Pawn;
            if (pawn2 == null || !pawn2.Downed || pawn2.Faction != pawn.Faction || pawn2.InBed() || !pawn.CanReserve(pawn2, 1, -1, null, forced) || GenAI.EnemyIsNear(pawn2, 40f))
            {
                return false;
            }
            Thing thing = base.FindBed(pawn, pawn2);

            var distance = pawn.Position.DistanceTo(pawn2.Position)/2;
            pawn.AvailableVehicles(out List<Thing> availableVehicles, null, distance);

            return thing != null && pawn2.CanReserve(thing, 1, -1, null, false) && !availableVehicles.NullOrEmpty();
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Pawn pawn2 = t as Pawn;
            Thing t2 = base.FindBed(pawn, pawn2);


            return TFH_BaseUtility.HaulDowneesToBed(pawn, pawn2, t2);
        }
    }
}
