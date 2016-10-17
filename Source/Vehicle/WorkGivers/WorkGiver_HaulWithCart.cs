using System.Collections.Generic;
using System.Linq;
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

            if (pawn.RaceProps.Animal || !pawn.RaceProps.Humanlike || !pawn.RaceProps.hasGenders)
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
            List<Vehicle_Cart> cartsAvailable = new List<Vehicle_Cart>();
            foreach (Vehicle_Cart thing in ToolsForHaulUtility.Cart())
            {
                if (ToolsForHaulUtility.IsDriverOfThisVehicle(pawn, thing))
                {
                    cart = thing;
                    break;
                }
                else if (ToolsForHaulUtility.AvailableAnimalCart(thing) || ToolsForHaulUtility.AvailableCart(thing, pawn))
                {
                    cartsAvailable.Add(thing);
                }
            }
            if (cart == null && cartsAvailable.Any())
            {
                cartsAvailable.OrderBy(x => pawn.Position.DistanceToSquared(x.Position)).ThenBy(x => x.DefaultMaxItem);
                cart = cartsAvailable.First();
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