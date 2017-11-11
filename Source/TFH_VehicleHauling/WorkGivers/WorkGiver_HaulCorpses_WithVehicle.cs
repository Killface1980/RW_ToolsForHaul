namespace TFH_VehicleHauling.WorkGivers
{
    using System.Collections.Generic;

    using RimWorld;

    using TFH_VehicleBase;

    using Verse;
    using Verse.AI;

    public class WorkGiver_HaulCorpses_WithVehicle : WorkGiver_Haul
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling();
        }

        public override bool ShouldSkip(Pawn pawn)
        {

            if (pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling().Count == 0)
            {
                return true;
            }

            pawn.AvailableVehicles(out List<Thing> availableVehicles);

            if (availableVehicles.NullOrEmpty())
            {
                return true;
            }
            Trace.DebugWriteHaulingPawn(pawn);

            if (TFH_BaseUtility.GetRightVehicle(pawn, availableVehicles) == null)
            {
                return true;
            }

            if (pawn.RaceProps.Animal)
            {
                return true;
            }

            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Corpse))
            {
                return null;
            }

            if (!HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t, forced))
            {
                return null;
            }

            Vehicle_Cart cart = null;

            if (!HaulAIUtility.PawnCanAutomaticallyHaul(pawn, t, forced))
            {
                return null;
            }

            // Vehicle selection
            if (pawn.IsDriver(out Vehicle_Cart drivenCart) && drivenCart is Vehicle_Cart)
            {
                cart = drivenCart as Vehicle_Cart;
            }

            if (cart == null)
            {
                pawn.AvailableVehicles(out List<Thing> availableVehicles);
                cart = TFH_BaseUtility.GetRightVehicle(pawn, availableVehicles, null, t) as Vehicle_Cart;

                if (cart == null)
                {
                    return null;
                }
            }

            ThingOwner storage = cart.GetDirectlyHeldThings();

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
            if (!StoreUtility.TryFindBestBetterStoreCellFor(t, pawn, pawn.Map, currentPriority, pawn.Faction, out storeCell))
            {
                Log.Message("WorkGiver_HaulCorpses NoEmptyPlaceLowerTrans");
                JobFailReason.Is(Static.NoEmptyPlaceLowerTrans);
                return null;
            }

            if (cart.IsMountedOnAnimalAndAvailable() || TFH_Utility.IsPlayerAllowedToRide(pawn, cart))
            {
                return TFH_Utility.HaulWithCartToCell(pawn, cart, t);
            }

            JobFailReason.Is(Static.NoAvailableCart);

            return null;
        }
    }
}
