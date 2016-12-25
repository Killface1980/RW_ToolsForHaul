using RimWorld;
using ToolsForHaul.Utilities;
using Verse;
using Verse.AI;

namespace ToolsForHaul.JobGivers
{
    public class JobGiver_TakeWoundedGuest : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            foreach (Vehicle_Cart cart in ToolsForHaulUtility.Cart)
            {
                if (cart.MountableComp.IsMounted && !cart.MountableComp.Driver.RaceProps.Animal && cart.MountableComp.Driver.ThingID == pawn.ThingID)
                {
                    cart.VehicleComp.despawnAtEdge = true;
                }
            }

            foreach (Vehicle_Turret cart in ToolsForHaulUtility.CartTurret)
            {
                if (cart.mountableComp.IsMounted && !cart.mountableComp.Driver.RaceProps.Animal && cart.mountableComp.Driver.ThingID == pawn.ThingID)
                {
                    cart.vehicleComp.despawnAtEdge = true;
                }
            }

            IntVec3 intVec;
            if (!RCellFinder.TryFindBestExitSpot(pawn, out intVec))
            {
                return null;
            }

            Pawn pawn2 = KidnapAIUtility.ReachableWoundedGuest(pawn);
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
