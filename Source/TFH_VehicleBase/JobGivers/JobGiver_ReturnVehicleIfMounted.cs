﻿namespace TFH_VehicleBase.JobGivers
{
    using System.Collections.Generic;
    using System.Linq;

    using RimWorld;

    using TFH_VehicleBase.DefOfs_TFH;

    using Verse;
    using Verse.AI;

    public class JobGiver_ReturnVehicleIfMounted : ThinkNode_JobGiver
    {
        public const float ItemsSearchRadiusInitial = 7f;

        private const float vehicleSearchRadius = 12f;

        protected override Job TryGiveJob(Pawn pawn)
        {

            if (!pawn.IsDriver(out Vehicle_Cart drivenCart))
            {
                return null;
            }
            else
            {
                Job job = pawn.DismountAtParkingLot("Jobgiver Return");
                return job;
            }

            if (pawn != null && !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return null;
            }


            List<Thing> steelVehicle = pawn.AvailableVehiclesForSteeling(20f);
            foreach (Thing thing in steelVehicle)
            {
                Vehicle_Cart cart = (Vehicle_Cart)thing;

                if (pawn.RaceProps.Animal || !pawn.RaceProps.Humanlike || !pawn.RaceProps.hasGenders)
                    break;
                if (!cart.IsBurning()
                    && cart.Position.InHorDistOf(pawn.Position, vehicleSearchRadius)
                    && !cart.MountableComp.IsMounted
                    && (float)cart.HitPoints / cart.MaxHitPoints > 0.2f
                    && cart.VehicleComp.VehicleSpeed >= pawn.GetStatValue(StatDefOf.MoveSpeed)
                    && pawn.CanReserveAndReach(cart, PathEndMode.InteractionCell, Danger.Deadly))
                {
                    steelVehicle.Add(cart);
                }

            }


            if (steelVehicle.Any())
            {
                // && !GenAI.InDangerousCombat(pawn))
                IOrderedEnumerable<Thing> orderedEnumerable =
                    steelVehicle.OrderBy(x => x.Position.DistanceToSquared(pawn.Position));
                Job job = new Job(VehicleJobDefOf.Mount);

                // orderedEnumerable.First().SetFaction(null);
                job.targetA = orderedEnumerable.First();

                return job;
            }

            return null;
        }
    }
}
