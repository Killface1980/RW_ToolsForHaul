using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using ToolsForHaul.JobDefs;
using ToolsForHaul.Utilities;
using Verse;
using Verse.AI;

namespace ToolsForHaul.JobGivers
{
    public class _JobGiver_Kidnap : ThinkNode_JobGiver
    {
        public const float LordStateChangeSearchRadius = 8f;

        private const float VictimSearchRadius = 20f;

          [Detour(typeof(RimWorld.JobDriver_Kidnap), bindingFlags = (BindingFlags.Instance | BindingFlags.NonPublic))]
        protected override Job TryGiveJob(Pawn pawn)
        {
            IntVec3 intVec;
            if (!RCellFinder.TryFindBestExitSpot(pawn, out intVec))
            {
                return null;
            }


            List<Thing> steelVehicle = new List<Thing>();
            foreach (Vehicle_Cart vehicle_Cart in ToolsForHaulUtility.Cart)
            {
                if (vehicle_Cart.MountableComp.IsMounted && !vehicle_Cart.MountableComp.Driver.RaceProps.Animal && vehicle_Cart.MountableComp.Driver.ThingID == pawn.ThingID)
                {
                    vehicle_Cart.VehicleComp.despawnAtEdge = true;
                    break;
                }

                if (pawn.RaceProps.Animal || !pawn.RaceProps.Humanlike || !pawn.RaceProps.hasGenders)
                    break;

                if (!vehicle_Cart.IsBurning() && vehicle_Cart.Position.InHorDistOf(pawn.Position, 20f) && !vehicle_Cart.MountableComp.IsMounted && (float)vehicle_Cart.HitPoints / vehicle_Cart.MaxHitPoints > 0.2f && vehicle_Cart.VehicleComp.VehicleSpeed >= pawn.GetStatValue(StatDefOf.MoveSpeed) && pawn.CanReserveAndReach(vehicle_Cart, PathEndMode.InteractionCell, Danger.Deadly))
                {
                    steelVehicle.Add(vehicle_Cart);
                }
            }

            if (steelVehicle.Any())
            {
                IOrderedEnumerable<Thing> orderedEnumerable = steelVehicle.OrderBy(x => x.Position.DistanceToSquared(pawn.Position));
                Job job = new Job(HaulJobDefOf.Mount);
                orderedEnumerable.First().SetFaction(null);
                job.targetA = orderedEnumerable.First();

                return job;
            }




            Pawn pawn2;
            KidnapAIUtility.TryFindGoodKidnapVictim(pawn, 20f, out pawn2);
            if (pawn2 == null)
            {
                return null;
            }

            return new Job(JobDefOf.Kidnap)
            {
                targetA = pawn2,
                targetB = intVec,
                count = 1
            };
        }
    }
}
