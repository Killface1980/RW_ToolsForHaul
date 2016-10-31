using System.Collections.Generic;
using System.Linq;
using RimWorld;
using ToolsForHaul.JobDefs;
using ToolsForHaul.Utilities;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ToolsForHaul.JobGivers
{
    public class JobGiver_Steal : ThinkNode_JobGiver
    {
        public const float ItemsSearchRadiusInitial = 7f;

        private const float ItemsSearchRadiusOngoing = 12f;

        protected override Job TryGiveJob(Pawn pawn)
        {
            IntVec3 vec;
            if (!RCellFinder.TryFindBestExitSpot(pawn, out vec))
            {
                return null;
            }

            List<Thing> steelVehicle = new List<Thing>();
            foreach (Vehicle_Turret vehicle_Cart in ToolsForHaulUtility.CartTurret)
            {
                if (ToolsForHaulUtility.IsDriver(pawn))
                    break;
                if (pawn.RaceProps.Animal || !pawn.RaceProps.Humanlike || !pawn.RaceProps.hasGenders)
                    break;
                if (!vehicle_Cart.IsBurning() && vehicle_Cart.Position.InHorDistOf(pawn.Position, 12f) && !vehicle_Cart.mountableComp.IsMounted && (float)vehicle_Cart.HitPoints / vehicle_Cart.MaxHitPoints > 0.2f && vehicle_Cart.VehicleSpeed >= pawn.GetStatValue(StatDefOf.MoveSpeed) && pawn.CanReserveAndReach(vehicle_Cart, PathEndMode.InteractionCell, Danger.Deadly))
                {
                    steelVehicle.Add(vehicle_Cart);
                }
            }
            foreach (Vehicle_Cart vehicle_Cart in ToolsForHaulUtility.Cart)
            {
                if (ToolsForHaulUtility.IsDriver(pawn))
                    break;
                if (pawn.RaceProps.Animal || !pawn.RaceProps.Humanlike || !pawn.RaceProps.hasGenders)
                    break;
                if (!vehicle_Cart.IsBurning() && vehicle_Cart.Position.InHorDistOf(pawn.Position, 12f) && !vehicle_Cart.mountableComp.IsMounted && (float)vehicle_Cart.HitPoints / vehicle_Cart.MaxHitPoints > 0.2f && vehicle_Cart.VehicleSpeed >= pawn.GetStatValue(StatDefOf.MoveSpeed) && pawn.CanReserveAndReach(vehicle_Cart, PathEndMode.InteractionCell, Danger.Deadly))
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
            if (StealAIUtility.TryFindBestItemToSteal(pawn.Position, 12f, out thing, pawn) && !GenAI.InDangerousCombat(pawn))
            {
                return new Job(JobDefOf.Steal)
                {
                    targetA = thing,
                    targetB = vec,
                    maxNumToCarry = Mathf.Min(thing.stackCount, (int)(pawn.GetStatValue(StatDefOf.CarryingCapacity) / thing.def.VolumePerUnit))
                };
            }
            return null;
        }
    }
}
