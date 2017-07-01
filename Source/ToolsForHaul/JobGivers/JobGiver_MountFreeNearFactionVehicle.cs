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

    public class JobGiver_MountFreeNearFactionVehicle : ThinkNode_JobGiver
    {
        public const float ItemsSearchRadiusInitial = 7f;

        private float vehicleSearchRadius = 24f;

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

            if (pawn.IsDriver())
            {
                return null;
            }

            List<Thing> steelVehicle;

            if (pawn.Faction.HostileTo(Faction.OfPlayer))
            {
                steelVehicle = pawn.AvailableVehiclesForFaction(this.vehicleSearchRadius);
            }
            else
            {
                
                steelVehicle = pawn.AvailableVehiclesForSteeling(this.vehicleSearchRadius);
            }

            foreach (var thing in steelVehicle)
            {
                var cart = (Vehicle_Cart)thing;

                if (pawn.RaceProps.Animal || !pawn.RaceProps.Humanlike || !pawn.RaceProps.hasGenders)
                    break;
                if ((float)cart.HitPoints / cart.MaxHitPoints > 0.2f
                    && cart.VehicleComp.VehicleSpeed >= pawn.GetStatValue(StatDefOf.MoveSpeed))
                {
                    steelVehicle.Add(cart);
                }

            }


            if (steelVehicle.Any())
            {
                // && !GenAI.InDangerousCombat(pawn))
                IOrderedEnumerable<Thing> orderedEnumerable =
                    steelVehicle.OrderBy(x => x.Position.DistanceToSquared(pawn.Position));
                Job job = new Job(HaulJobDefOf.Mount);

                // orderedEnumerable.First().SetFaction(null);
                job.targetA = orderedEnumerable.First();

                return job;
            }

            return null;
        }
    }
}
