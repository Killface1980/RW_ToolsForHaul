namespace ToolsForHaul.JobGivers
{
    using System.Collections.Generic;
    using System.Linq;

    using RimWorld;

    using ToolsForHaul.Defs;
    using ToolsForHaul.Utilities;
    using ToolsForHaul.Vehicles;

    using Verse;
    using Verse.AI;

    public class JobGiver_MountSpawnedFactionVehicle : ThinkNode_JobGiver
    {
        private float vehicleSearchRadius = 10f;

        protected override Job TryGiveJob(Pawn pawn)
        {

            if (pawn != null && !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return null;
            }

            if (pawn.Faction.IsPlayer)
            {
                return null;
            }
            if (pawn.RaceProps.Animal || !pawn.RaceProps.Humanlike || !pawn.RaceProps.hasGenders)
            {
                return null;
            }

            if (pawn.IsDriver())
            {
                return null;
            }

            List<Thing> availableVehicles = pawn.AvailableVehicleAt();

            if (!availableVehicles.NullOrEmpty())
            {
                // && !GenAI.InDangerousCombat(pawn))
                Job job = new Job(HaulJobDefOf.Mount) { targetA = availableVehicles.First() };

                // orderedEnumerable.First().SetFaction(null);

                return job;
            }

            return null;
        }
    }
}
