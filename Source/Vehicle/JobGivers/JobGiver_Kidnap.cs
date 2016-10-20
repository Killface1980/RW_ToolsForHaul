using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public class JobGiver_Kidnap : ThinkNode_JobGiver
    {
        public const float LordStateChangeSearchRadius = 8f;

        private const float VictimSearchRadius = 20f;

        protected override Job TryGiveJob(Pawn pawn)
        {
            List<Thing> steelVehicle = new List<Thing>();
            foreach (Vehicle_Cart vehicle_Cart in ToolsForHaulUtility.Cart())
            {
                if (vehicle_Cart.mountableComp.IsMounted && !vehicle_Cart.mountableComp.Driver.RaceProps.Animal && vehicle_Cart.mountableComp.Driver.ThingID == pawn.ThingID)
                {
                    vehicle_Cart.despawnAtEdge = true;
                    break;
                }
                if (pawn.RaceProps.Animal || !pawn.RaceProps.Humanlike || !pawn.RaceProps.hasGenders)
                    break;

                if (!vehicle_Cart.IsBurning() && vehicle_Cart.Position.InHorDistOf(pawn.Position, 20f) && !vehicle_Cart.mountableComp.IsMounted && (float)vehicle_Cart.HitPoints / vehicle_Cart.MaxHitPoints > 0.2f && vehicle_Cart.VehicleSpeed >= pawn.GetStatValue(StatDefOf.MoveSpeed) && pawn.CanReserveAndReach(vehicle_Cart, PathEndMode.InteractionCell, Danger.Deadly))
                {
                    steelVehicle.Add(vehicle_Cart);
                }
            }

            foreach (Vehicle_Turret vehicle_Cart in ToolsForHaulUtility.CartTurret())
            {
                if (vehicle_Cart.mountableComp.IsMounted && !vehicle_Cart.mountableComp.Driver.RaceProps.Animal && vehicle_Cart.mountableComp.Driver.ThingID == pawn.ThingID)
                {
                    vehicle_Cart.despawnAtEdge = true;
                    break;
                }
                if (pawn.RaceProps.Animal || !pawn.RaceProps.Humanlike || !pawn.RaceProps.hasGenders)
                    break;

                if (!vehicle_Cart.IsBurning() && vehicle_Cart.Position.InHorDistOf(pawn.Position, 20f) && !vehicle_Cart.mountableComp.IsMounted && (float)vehicle_Cart.HitPoints / vehicle_Cart.MaxHitPoints > 0.2f && vehicle_Cart.VehicleSpeed >= pawn.GetStatValue(StatDefOf.MoveSpeed) && pawn.CanReserveAndReach(vehicle_Cart, PathEndMode.InteractionCell, Danger.Deadly))
                {
                    steelVehicle.Add(vehicle_Cart);
                }
            }

            if (steelVehicle.Any())
            {
                IOrderedEnumerable<Thing> orderedEnumerable = steelVehicle.OrderBy(x => x.Position.DistanceToSquared(pawn.Position));
                Job job = new Job(DefDatabase<JobDef>.GetNamed("Mount"));
                orderedEnumerable.First().SetFaction(null);
                job.targetA = orderedEnumerable.First();

                return job;
            }


            IntVec3 intVec;
            if (!RCellFinder.TryFindBestExitSpot(pawn, out intVec))
            {
                return null;
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
                maxNumToCarry = 1
            };
        }
    }
}
