using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public class JobGiver_TakeWoundedGuest : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            using (List<Thing>.Enumerator enumerator = ToolsForHaulUtility.Cart().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Vehicle_Cart vehicle_Cart = (Vehicle_Cart)enumerator.Current;
                    if (vehicle_Cart.mountableComp.IsMounted && !vehicle_Cart.mountableComp.Driver.RaceProps.Animal && vehicle_Cart.mountableComp.Driver.ThingID == pawn.ThingID)
                    {
                        vehicle_Cart.despawnAtEdge = true;
                    }
                }
            }
            IntVec3 vec;
            if (!RCellFinder.TryFindBestExitSpot(pawn, out vec, TraverseMode.ByPawn))
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
                targetB = vec,
                maxNumToCarry = 1
            };
        }
    }
}