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

    public class JobGiver_StealVehicle : ThinkNode_JobGiver
    {
        public const float ItemsSearchRadiusInitial = 7f;

        private const float ItemsSearchRadiusOngoing = 12f;

        protected override Job TryGiveJob(Pawn pawn)
        {

            if (pawn != null && !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return null;
            }

            if (pawn.IsDriver()) return null;

            List<Thing> steelVehicle = pawn.AvailableVehiclesForSteeling(20f);
            foreach (var thing in steelVehicle)
            {
                var cart = (Vehicle_Cart)thing;

                if (pawn.RaceProps.Animal || !pawn.RaceProps.Humanlike || !pawn.RaceProps.hasGenders)
                    break;
                if (!cart.IsBurning()
                    && cart.Position.InHorDistOf(pawn.Position, ItemsSearchRadiusOngoing)
                    && !cart.MountableComp.IsMounted
                    && (float)cart.HitPoints / cart.MaxHitPoints > 0.2f
                    && cart.VehicleComp.VehicleSpeed >= pawn.GetStatValue(StatDefOf.MoveSpeed)
                    && pawn.CanReserveAndReach(cart, PathEndMode.InteractionCell, Danger.Deadly))
                {
                    steelVehicle.Add(cart);
                }

            }


            if (steelVehicle.Any() )
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
