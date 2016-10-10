using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public class JobGiver_TakeWoundedGuest : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            foreach (Vehicle_Cart cart in ToolsForHaulUtility.Cart())

            {
                    if (cart.mountableComp.IsMounted && !cart.mountableComp.Driver.RaceProps.Animal && cart.mountableComp.Driver.ThingID == pawn.ThingID)
                    {
                        cart.despawnAtEdge = true;
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
                maxNumToCarry = 1
            };
        }


    }
}
