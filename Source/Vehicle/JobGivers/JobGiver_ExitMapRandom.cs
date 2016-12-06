using RimWorld;
using ToolsForHaul.Utilities;
using Verse;
using Verse.AI;

namespace ToolsForHaul.JobGivers
{
    public class JobGiver_ExitMapRandom : JobGiver_ExitMap
    {
        protected override bool TryFindGoodExitDest(Pawn pawn, bool canDig, out IntVec3 dest)
        {
            foreach (Vehicle_Cart vehicle_Cart in ToolsForHaulUtility.Cart)
            {
                if (vehicle_Cart.MountableComp.IsMounted && !vehicle_Cart.MountableComp.Driver.RaceProps.Animal && vehicle_Cart.MountableComp.Driver.ThingID == pawn.ThingID)
                {
                    vehicle_Cart.VehicleComp.despawnAtEdge = true;
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
