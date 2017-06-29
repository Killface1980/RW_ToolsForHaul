namespace ToolsForHaul.WorkGivers
{
    using System.Collections.Generic;

    using RimWorld;

    using ToolsForHaul.Components;
    using ToolsForHaul.Utilities;
    using ToolsForHaul.Vehicles;

    using Verse;
    using Verse.AI;

    public class WorkGiver_Haul_WithCart : WorkGiver_Scanner
    {


        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {

            // return TFH_Utility.Cart();
            return pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling();
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            List<Thing> availabeVehicles = TFH_Utility.AvailableVehicles(pawn);

            Trace.DebugWriteHaulingPawn(pawn);
            if (TFH_Utility.GetRightVehicle(pawn, availabeVehicles, WorkTypeDefOf.Hauling) == null)
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
            if (TFH_Utility.IsDriver(pawn))
            {
                cart = TFH_Utility.GetCartByDriver(pawn);

                if (cart == null)
                {
                    // JobFailReason.Is("Can't haul with military vehicle");
                    return TFH_Utility.DismountAtParkingLot(pawn, GameComponentToolsForHaul.CurrentDrivers[pawn]);
                }
            }

            if (cart == null)
            {
                List<Thing> availableVehicles = TFH_Utility.AvailableVehicles(pawn);
                if (availableVehicles.Count == 0) return null;

                cart = TFH_Utility.GetRightVehicle(pawn, availableVehicles, DefDatabase<WorkTypeDef>.GetNamed("Hauling"), t) as Vehicle_Cart;

                if (cart == null)
                    return null;
            }

            if (cart.IsBurning())
            {
                JobFailReason.Is(TFH_Utility.BurningLowerTrans);
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
            if (!StoreUtility.TryFindBestBetterStoreCellFor(t, pawn, pawn.Map, currentPriority, pawn.Faction, out storeCell))
            {
                Log.Message("WorkGiver_Haul_WithCart " + TFH_Utility.NoEmptyPlaceLowerTrans);
                JobFailReason.Is(TFH_Utility.NoEmptyPlaceLowerTrans);
                return null;
            }

            // if (cart.Map.slotGroupManager.AllGroupsListInPriorityOrder.Count == 0)
            // {
            // JobFailReason.Is(TFH_Utility.NoEmptyPlaceLowerTrans);
            // return null;
            // }
            if (TFH_Utility.AvailableAnimalCart(cart) || TFH_Utility.IsVehicleAvailable(pawn, cart))
            {
                return TFH_Utility.HaulWithTools(pawn, cart, t);
            }

            JobFailReason.Is(TFH_Utility.NoAvailableCart);
            return null;
        }

    }

}