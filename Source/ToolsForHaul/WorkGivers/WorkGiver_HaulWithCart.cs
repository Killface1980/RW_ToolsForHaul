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
            List<Thing> availabeVehicles = ToolsForHaulUtility.AvailableVehicles(pawn);

            Trace.DebugWriteHaulingPawn(pawn);
            if (RightVehicle.GetRightVehicle(pawn, availabeVehicles, DefDatabase<WorkTypeDef>.GetNamed("Hauling")) == null)
                return true;

            if (pawn.RaceProps.Animal || !pawn.RaceProps.Humanlike || !pawn.RaceProps.hasGenders)
                return true;

            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced)
        {
            Vehicle_Cart cart = null;

            if (!HaulAIUtility.PawnCanAutomaticallyHaul(pawn, t, forced))
            {
                return null;
            }

            // Vehicle selection
            if (ToolsForHaulUtility.IsDriver(pawn))
            {
                cart = ToolsForHaulUtility.GetCartByDriver(pawn);

                if (cart == null)
                {
                    // JobFailReason.Is("Can't haul with military vehicle");
                    return ToolsForHaulUtility.DismountAtParkingLot(pawn, GameComponentToolsForHaul.CurrentDrivers[pawn]);
                }
            }

            if (cart == null)
            {
                List<Thing> availableVehicles = ToolsForHaulUtility.AvailableVehicles(pawn);
                if (availableVehicles.Count == 0) return null;

                cart = RightVehicle.GetRightVehicle(pawn, availableVehicles, DefDatabase<WorkTypeDef>.GetNamed("Hauling"), t) as Vehicle_Cart;

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

            if (cart.Map.listerHaulables.ThingsPotentiallyNeedingHauling().Count == 0 && cart.innerContainer.Count == 0)
            {
                JobFailReason.Is("NoHaulable".Translate());
                return null;
            }

            StoragePriority currentPriority = HaulAIUtility.StoragePriorityAtFor(t.Position, t);
            IntVec3 storeCell;
            if (!StoreUtility.TryFindBestBetterStoreCellFor(t, pawn, pawn.Map, currentPriority, pawn.Faction, out storeCell, true))
            {
                Log.Message("WorkGiver_HaulWithCart " + ToolsForHaulUtility.NoEmptyPlaceLowerTrans);
                JobFailReason.Is(ToolsForHaulUtility.NoEmptyPlaceLowerTrans);
                return null;
            }

            // if (cart.Map.slotGroupManager.AllGroupsListInPriorityOrder.Count == 0)
            // {
            //     JobFailReason.Is(ToolsForHaulUtility.NoEmptyPlaceLowerTrans);
            //     return null;
            // }

            if (ToolsForHaulUtility.AvailableAnimalCart(cart) || ToolsForHaulUtility.IsVehicleAvailable(pawn, cart))
            {
                return ToolsForHaulUtility.HaulWithTools(pawn, cart, t);
            }
            JobFailReason.Is(ToolsForHaulUtility.NoAvailableCart);
            return null;
        }

    }

}