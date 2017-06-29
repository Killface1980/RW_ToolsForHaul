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

            List<Thing> availableVehicles = TFH_Utility.AvailableVehicles(pawn);

            if (availableVehicles.Count < 1)
            {
                return true;
            }

            if (TFH_Utility.GetRightVehicle(pawn, availableVehicles, WorkTypeDefOf.Hauling, new Corpse())
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
            if (TFH_Utility.IsDriver(pawn))
            {
                cart = TFH_Utility.GetCartByDriver(pawn);
            }

            if (cart == null)
            {
                List<Thing> availableVehicles = TFH_Utility.AvailableVehicles(pawn);

                cart = TFH_Utility.GetRightVehicle(pawn, availableVehicles, WorkTypeDefOf.Hauling, t) as Vehicle_Cart;

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
                Log.Message("WorkGiver_HaulCorpses NoEmptyPlaceLowerTrans");
                JobFailReason.Is(TFH_Utility.NoEmptyPlaceLowerTrans);
                return null;
            }

            if (TFH_Utility.AvailableAnimalCart(cart) || TFH_Utility.IsVehicleAvailable(pawn, cart))
            {
                return TFH_Utility.HaulWithTools(pawn, cart, t);
            }

            JobFailReason.Is(TFH_Utility.NoAvailableCart);

            return null;
        }
    }
}
