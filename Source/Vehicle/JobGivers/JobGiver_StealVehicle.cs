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


            List<Thing> steelVehicle = new List<Thing>();
            foreach (Vehicle_Cart vehicleTurret in ToolsForHaulUtility.Cart)
            {
                if (ToolsForHaulUtility.IsDriver(pawn))
                    break;
                if (pawn.RaceProps.Animal || !pawn.RaceProps.Humanlike || !pawn.RaceProps.hasGenders)
                    break;
                if (!vehicleTurret.IsBurning()
                    && vehicleTurret.Position.InHorDistOf(pawn.Position, ItemsSearchRadiusOngoing)
                    && !vehicleTurret.MountableComp.IsMounted
                    && (float)vehicleTurret.HitPoints / vehicleTurret.MaxHitPoints > 0.2f
                    && vehicleTurret.VehicleComp.VehicleSpeed >= pawn.GetStatValue(StatDefOf.MoveSpeed)
                    && pawn.CanReserveAndReach(vehicleTurret, PathEndMode.InteractionCell, Danger.Deadly))
                {
                    steelVehicle.Add(vehicleTurret);
                }

            }


            if (steelVehicle.Any() )//&& !GenAI.InDangerousCombat(pawn))
            {
                IOrderedEnumerable<Thing> orderedEnumerable = steelVehicle.OrderBy(x => x.Position.DistanceToSquared(pawn.Position));
                Job job = new Job(HaulJobDefOf.Mount);
           //     orderedEnumerable.First().SetFaction(null);
                job.targetA = orderedEnumerable.First();

                return job;
            }

            return null;
        }
    }
}
