namespace TFH_VehicleHauling.WorkGivers
{
    using System.Collections.Generic;

    using RimWorld;

    using TFH_VehicleBase;

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
            if (pawn.RaceProps.Animal || !pawn.RaceProps.Humanlike || !pawn.RaceProps.hasGenders)
                return true;


            pawn.AvailableVehicles(out List<Thing> availableVehicles);

            if (availableVehicles.NullOrEmpty())
            {
                return true;
            }

            Trace.DebugWriteHaulingPawn(pawn);

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

            pawn.AvailableVehicles(out List<Thing> availableVehicles);

            if (availableVehicles.NullOrEmpty())
            {
                return null;
            }
            Vehicle_Cart drivenCart;
            if (!pawn.IsDriver(out drivenCart))
            {
                cart = TFH_BaseUtility.GetRightVehicle(
                           pawn,
                           availableVehicles,
                           DefDatabase<WorkTypeDef>.GetNamed("Hauling"),
                           t) as Vehicle_Cart;
            }

            if (cart == null) return null;

            var storage = cart.GetContainer();

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

            if (cart.Map.listerHaulables.ThingsPotentiallyNeedingHauling().Count == 0 && storage.Count == 0)
            {
                JobFailReason.Is("NoHaulable".Translate());
                return null;
            }

            StoragePriority currentPriority = HaulAIUtility.StoragePriorityAtFor(t.Position, t);
            IntVec3 storeCell;
            if (!StoreUtility.TryFindBestBetterStoreCellFor(
                    t,
                    pawn,
                    pawn.Map,
                    currentPriority,
                    pawn.Faction,
                    out storeCell))
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
            if (cart.IsMountedOnAnimalAndAvailable() || TFH_Utility.IsPlayerAllowedToRide(pawn, cart))
            {
                return TFH_Utility.HaulWithToolsToCell(pawn, cart, t);
            }

            JobFailReason.Is(Static.NoAvailableCart);
            return null;
        }
    }

}