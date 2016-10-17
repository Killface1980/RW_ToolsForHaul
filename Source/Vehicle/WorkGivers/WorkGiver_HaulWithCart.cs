using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public class WorkGiver_HaulWithCart : WorkGiver_Scanner
    {


        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            //return ToolsForHaulUtility.Cart();
            return ListerHaulables.ThingsPotentiallyNeedingHauling();
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            Trace.DebugWriteHaulingPawn(pawn);
            if (ToolsForHaulUtility.Cart().Count == 0)
                return true;
            //int countForbidden = 0;
            //foreach (var actualCart in cart)
            //{
            //    if (actualCart.IsForbidden(pawn.Faction))
            //        countForbidden+=1;
            //}
            //if (cart.Count== countForbidden)
            //{
            //    return true;
            //}
            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            Vehicle_Cart cart = null;

            if (!HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t))
            {
                return null;
            }

            foreach (Vehicle_Cart thing in ToolsForHaulUtility.Cart())
            {
                if (ToolsForHaulUtility.AvailableAnimalCart(thing) || ToolsForHaulUtility.AvailableCart(thing, pawn))
                {
                    cart = thing;
                    break;
                }
            }
            if (cart == null)
                return null;
            if (cart.IsForbidden(pawn.Faction) || !pawn.CanReserveAndReach(cart, PathEndMode.ClosestTouch, pawn.NormalMaxDanger()))
                return null;
            if (cart.IsBurning())
            {
                JobFailReason.Is(ToolsForHaulUtility.BurningLowerTrans);
                return null;
            }
            if (ListerHaulables.ThingsPotentiallyNeedingHauling().Count == 0 && cart.storage.Count == 0)
            {
                JobFailReason.Is("NoHaulable".Translate());
                return null;
            }
            if (Find.SlotGroupManager.AllGroupsListInPriorityOrder.Count == 0)
            {
                JobFailReason.Is(ToolsForHaulUtility.NoEmptyPlaceLowerTrans);
                return null;
            }
            if (ToolsForHaulUtility.AvailableAnimalCart(cart) || ToolsForHaulUtility.AvailableCart(cart, pawn))
                return ToolsForHaulUtility.HaulWithTools(pawn, cart);
            JobFailReason.Is(ToolsForHaulUtility.NoAvailableCart);
            return null;
        }

    }

}