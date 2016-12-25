using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using ToolsForHaul.JobDefs;
using ToolsForHaul.Utilities;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ToolsForHaul.JobGivers
{
    public class _JobGiver_Steal : ThinkNode_JobGiver
    {
        public const float ItemsSearchRadiusInitial = 7f;

        private const float ItemsSearchRadiusOngoing = 12f;

          [Detour(typeof(RimWorld.JobGiver_Steal), bindingFlags = (BindingFlags.Instance | BindingFlags.NonPublic))]
        protected override Job TryGiveJob(Pawn pawn)
        {
            IntVec3 vec;
            if (!RCellFinder.TryFindBestExitSpot(pawn, out vec))
            {
                return null;
            }

            List<Thing> steelVehicle = new List<Thing>();
            foreach (Vehicle_Turret vehicleTurret in ToolsForHaulUtility.CartTurret)
            {
                if (ToolsForHaulUtility.IsDriver(pawn))
                    break;
                if (pawn.RaceProps.Animal || !pawn.RaceProps.Humanlike || !pawn.RaceProps.hasGenders)
                    break;
                if (!vehicleTurret.IsBurning() && vehicleTurret.Position.InHorDistOf(pawn.Position, ItemsSearchRadiusOngoing) && !vehicleTurret.mountableComp.IsMounted && (float)vehicleTurret.HitPoints / vehicleTurret.MaxHitPoints > 0.2f && vehicleTurret.vehicleComp.VehicleSpeed >= pawn.GetStatValue(StatDefOf.MoveSpeed) && pawn.CanReserveAndReach(vehicleTurret, PathEndMode.InteractionCell, Danger.Deadly))
                {
                    steelVehicle.Add(vehicleTurret);
                }
            }

            foreach (Vehicle_Cart vehicle_Cart in ToolsForHaulUtility.Cart)
            {
                if (ToolsForHaulUtility.IsDriver(pawn))
                    break;
                if (pawn.RaceProps.Animal || !pawn.RaceProps.Humanlike || !pawn.RaceProps.hasGenders)
                    break;
                if (!vehicle_Cart.IsBurning() && vehicle_Cart.Position.InHorDistOf(pawn.Position, ItemsSearchRadiusOngoing) && !vehicle_Cart.MountableComp.IsMounted && (float)vehicle_Cart.HitPoints / vehicle_Cart.MaxHitPoints > 0.2f && vehicle_Cart.VehicleComp.VehicleSpeed >= pawn.GetStatValue(StatDefOf.MoveSpeed) && pawn.CanReserveAndReach(vehicle_Cart, PathEndMode.InteractionCell, Danger.Deadly))
                {
                    steelVehicle.Add(vehicle_Cart);
                }
            }

            if (steelVehicle.Any() && !GenAI.InDangerousCombat(pawn))
            {
                IOrderedEnumerable<Thing> orderedEnumerable = steelVehicle.OrderBy(x => x.Position.DistanceToSquared(pawn.Position));
                Job job = new Job(HaulJobDefOf.Mount);
                orderedEnumerable.First().SetFaction(null);
                job.targetA = orderedEnumerable.First();

                return job;
            }

            Thing thing;
            if (StealAIUtility.TryFindBestItemToSteal(pawn.Position, pawn.Map, ItemsSearchRadiusOngoing, out thing, pawn) && !GenAI.InDangerousCombat(pawn))
            {
                return new Job(JobDefOf.Steal)
                {
                    targetA = thing,
                    targetB = vec,
                    count = Mathf.Min(thing.stackCount, (int)(pawn.GetStatValue(StatDefOf.CarryingCapacity) / thing.def.VolumePerUnit))
                };
            }

            return null;
        }
    }
}
