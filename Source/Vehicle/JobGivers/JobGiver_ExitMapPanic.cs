using System.Collections.Generic;
using System.Linq;
using RimWorld;
using ToolsForHaul.JobDefs;
using ToolsForHaul.Utilities;
using Verse;
using Verse.AI;

namespace ToolsForHaul.JobGivers
{
    public class JobGiver_ExitMapPanic : JobGiver_ExitMapBest
    {
        public JobGiver_ExitMapPanic()
        {
            canBash = true;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            List<Thing> steelVehicle = new List<Thing>();

            foreach (Vehicle_Cart vehicle_Cart in ToolsForHaulUtility.Cart)
            {
                if (ToolsForHaulUtility.IsDriver(pawn))
                    break;
                if (pawn.RaceProps.Animal|| !pawn.RaceProps.Humanlike|| !pawn.RaceProps.hasGenders)
                    break;
                if (!vehicle_Cart.IsBurning() && vehicle_Cart.Position.InHorDistOf(pawn.Position, 20f) && !vehicle_Cart.mountableComp.IsMounted && (float)vehicle_Cart.HitPoints / vehicle_Cart.MaxHitPoints > 0.2f && vehicle_Cart.vehicleComp.VehicleSpeed>=pawn.GetStatValue(StatDefOf.MoveSpeed) && pawn.CanReserveAndReach(vehicle_Cart, PathEndMode.InteractionCell, Danger.Deadly))
                {
                    steelVehicle.Add(vehicle_Cart);
                }
            }

            foreach (Vehicle_Turret vehicle_Cart in ToolsForHaulUtility.CartTurret)
            {
                if (ToolsForHaulUtility.IsDriver(pawn))
                    break;
                if (pawn.RaceProps.Animal || !pawn.RaceProps.Humanlike || !pawn.RaceProps.hasGenders)
                    break;
                if (!vehicle_Cart.IsBurning() && vehicle_Cart.Position.InHorDistOf(pawn.Position, 20f) && !vehicle_Cart.mountableComp.IsMounted && (float)vehicle_Cart.HitPoints / vehicle_Cart.MaxHitPoints > 0.2f && vehicle_Cart.vehicleComp.VehicleSpeed >= pawn.GetStatValue(StatDefOf.MoveSpeed) && pawn.CanReserveAndReach(vehicle_Cart, PathEndMode.InteractionCell, Danger.Deadly))
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

            bool flag = false;
            if (this.forceCanDig || (pawn.mindState.duty != null && pawn.mindState.duty.canDig))
            {
                flag = true;
            }
            IntVec3 vec;
            if (!this.TryFindGoodExitDest(pawn, flag, out vec))
            {
                return null;
            }
            if (flag)
            {
                using (PawnPath pawnPath = PathFinder.FindPath(pawn.Position, vec, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassAnything)))
                {
                    IntVec3 cellBeforeBlocker;
                    Thing thing = pawnPath.FirstBlockingBuilding(out cellBeforeBlocker, pawn);
                    if (thing != null)
                    {
                        Job job = DigUtility.PassBlockerJob(pawn, thing, cellBeforeBlocker, true);
                        if (job != null)
                        {
                            return job;
                        }
                    }
                }
            }
            return new Job(JobDefOf.Goto, vec)
            {
                exitMapOnArrival = true,
                locomotionUrgency = PawnUtility.ResolveLocomotion(pawn, this.defaultLocomotion, LocomotionUrgency.Jog),
                expiryInterval = this.jobMaxDuration,
                canBash = this.canBash
            };
        }

        protected override bool TryFindGoodExitDest(Pawn pawn, bool canDig, out IntVec3 dest)
        {

            foreach (Vehicle_Cart vehicle_Cart in ToolsForHaulUtility.Cart)
            {

                if (vehicle_Cart.mountableComp.IsMounted && !vehicle_Cart.mountableComp.Driver.RaceProps.Animal && vehicle_Cart.mountableComp.Driver.ThingID == pawn.ThingID)
                {
                    vehicle_Cart.vehicleComp.despawnAtEdge = true;
                }
            }
            foreach (Vehicle_Turret vehicle_Cart in ToolsForHaulUtility.CartTurret)
            {

                if (vehicle_Cart.mountableComp.IsMounted && !vehicle_Cart.mountableComp.Driver.RaceProps.Animal && vehicle_Cart.mountableComp.Driver.ThingID == pawn.ThingID)
                {
                    vehicle_Cart.vehicleComp.despawnAtEdge = true;
                }
            }
            TraverseMode mode = canDig ? TraverseMode.PassAnything : TraverseMode.ByPawn;
            return RCellFinder.TryFindBestExitSpot(pawn, out dest, mode);
        }

    }
}
