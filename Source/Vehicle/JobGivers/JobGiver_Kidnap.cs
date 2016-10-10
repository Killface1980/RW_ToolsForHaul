using System.Collections.Generic;
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
