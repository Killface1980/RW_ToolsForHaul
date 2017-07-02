namespace ToolsForHaul.WorkGivers
{
    using System.Collections.Generic;
    using System.Linq;

    using RimWorld;

    using ToolsForHaul.Components;
    using ToolsForHaul.Utilities;
    using ToolsForHaul.Vehicles;

    using Verse;
    using Verse.AI;


    public class WorkGiver_Haul_ReturnCart : WorkGiver_Scanner
    {


        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            // return TFH_Utility.Cart();
            // noParking.SortBy(x => pawn.Position.DistanceTo(x.Position));
            return pawn.AvailableVehiclesForPawnFaction(999f)
                .Where(vehicle => !(vehicle.Position.GetZone(pawn.Map) is Zone_ParkingLot)).ToList();

            // pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling();
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            if (pawn.IsDriver())
            {
                return true;
            }

            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced)
        {
            Vehicle_Cart cart = t as Vehicle_Cart;

            return pawn.DismountAtParkingLot(cart, "WG Haul");

            // Vehicle selection
            if (pawn.IsDriver())
            {
                cart = pawn.MountedVehicle();

                if (cart == null)
                {
                    // JobFailReason.Is("Can't haul with military vehicle");
                }
            }

            if (cart == null)
            {
                List<Thing> availableVehicles = pawn.AvailableVehicles();
                if (availableVehicles.Count == 0) return null;

                cart = TFH_Utility.GetRightVehicle(pawn, availableVehicles, DefDatabase<WorkTypeDef>.GetNamed("Hauling"), t) as Vehicle_Cart;

                if (cart == null)
                    return null;
            }

            if (cart.IsBurning())
            {
                JobFailReason.Is(Static.BurningLowerTrans);
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
                Log.Message("WorkGiver_Haul_WithCart " + Static.NoEmptyPlaceLowerTrans);
                JobFailReason.Is(Static.NoEmptyPlaceLowerTrans);
                return null;
            }

            // if (cart.Map.slotGroupManager.AllGroupsListInPriorityOrder.Count == 0)
            // {
            // JobFailReason.Is(TFH_Utility.NoEmptyPlaceLowerTrans);
            // return null;
            // }
            if (cart.IsMountedOnAnimalAndAvailable() || pawn.IsAllowedToRide(cart))
            {
                return TFH_Utility.HaulWithTools(pawn, cart, t);
            }

            JobFailReason.Is(Static.NoAvailableCart);
            return null;
        }

    }

}