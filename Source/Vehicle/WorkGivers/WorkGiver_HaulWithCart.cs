using System.Collections.Generic;
using RimWorld;
using ToolsForHaul.Utilities;
using Verse;
using Verse.AI;

namespace ToolsForHaul.WorkGivers
{
    public class WorkGiver_HaulWithCart : WorkGiver_Scanner
    {


        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {

            // return ToolsForHaulUtility.Cart();
            return pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling();
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            Trace.DebugWriteHaulingPawn(pawn);
            if (RightTools.GetRightVehicle(pawn, DefDatabase<WorkTypeDef>.GetNamed("Hauling")) == null)
                return true;

            if (pawn.RaceProps.Animal || !pawn.RaceProps.Humanlike || !pawn.RaceProps.hasGenders)
                return true;

            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            Vehicle_Cart cart = null;

            if (!HaulAIUtility.PawnCanAutomaticallyHaul(pawn, t))
            {
                return null;
            }

            // Vehicle selection
          if (ToolsForHaulUtility.IsDriver(pawn))
          {
              cart = ToolsForHaulUtility.GetCartByDriver(pawn);
          
              if (cart ==null)
              {
                    // JobFailReason.Is("Can't haul with military vehicle");
                   return ToolsForHaulUtility.DismountInBase(pawn, MapComponent_ToolsForHaul.currentVehicle[pawn]);
                }
          }


            if (cart == null)
            {
                cart = RightTools.GetRightVehicle(pawn, DefDatabase<WorkTypeDef>.GetNamed("Hauling"), t) as Vehicle_Cart;

                if (cart == null)
                    return null;
            }

           

            if (cart.IsBurning())
            {
                JobFailReason.Is(ToolsForHaulUtility.BurningLowerTrans);
                return null;
            }
            
            if (!cart.allowances.Allows(t))
            {
                JobFailReason.Is("Cart does not allow that thing");
                return null;
            }

            if (cart.Map.listerHaulables.ThingsPotentiallyNeedingHauling().Count == 0 && cart.Storage.Count == 0)
            {
                JobFailReason.Is("NoHaulable".Translate());
                return null;
            }

            if (cart.Map.slotGroupManager.AllGroupsListInPriorityOrder.Count == 0)
            {
                JobFailReason.Is(ToolsForHaulUtility.NoEmptyPlaceLowerTrans);
                return null;
            }

            if (ToolsForHaulUtility.AvailableAnimalCart(cart) || ToolsForHaulUtility.AvailableVehicle(pawn, cart))
                return ToolsForHaulUtility.HaulWithTools(pawn,pawn.Map, cart, t);
            JobFailReason.Is(ToolsForHaulUtility.NoAvailableCart);
            return null;
        }

    }

}