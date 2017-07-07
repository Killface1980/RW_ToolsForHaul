namespace ToolsForHaul.WorkGivers
{
    using System.Collections.Generic;

    using RimWorld;

    using ToolsForHaul.Utilities;
    using ToolsForHaul.Vehicles;

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
            Trace.DebugWriteHaulingPawn(pawn);

            if (pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling().Count == 0)
            {
                return true;
            }

            List<Thing> availableVehicles = pawn.AvailableVehicles();

            if (availableVehicles.Count < 1)
            {
                return true;
            }

            if (TFH_Utility.GetRightVehicle(pawn, availableVehicles, WorkTypeDefOf.Hauling)
                == null)
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
            if (pawn.IsDriver())
            {
                cart = pawn.MountedVehicle();
            }

            if (cart == null)
            {
                List<Thing> availableVehicles = pawn.AvailableVehicles();

                cart = TFH_Utility.GetRightVehicle(pawn, availableVehicles, WorkTypeDefOf.Hauling, t) as Vehicle_Cart;

                if (cart == null)
                    return null;
            }
            var storage = cart.TryGetInnerInteractableThingOwner();

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

            if (cart.IsMountedOnAnimalAndAvailable() || pawn.IsPlayerAllowedToRide(cart))
            {
                return TFH_Utility.HaulWithToolsToCell(pawn, cart, t);
            }

            JobFailReason.Is(Static.NoAvailableCart);

            return null;
        }
    }
}
