using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
	public class JobGiver_ExitMapRandom : JobGiver_ExitMap
	{
		protected override bool TryFindGoodExitDest(Pawn pawn, out IntVec3 spot)
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
			return ExitUtility.TryFindRandomExitSpot(pawn, ref spot, 1);
		}
	}
}
