using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public class JobGiver_ExitMapNearest : JobGiver_ExitMap
    {
        protected override bool TryFindGoodExitDest(Pawn pawn, bool canDig, out IntVec3 dest)
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
            return RCellFinder.TryFindBestExitSpot(pawn, out dest);
        }

    }
}
