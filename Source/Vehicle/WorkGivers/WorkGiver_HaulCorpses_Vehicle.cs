using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ToolsForHaul.WorkGivers
{
    using RimWorld;

    using ToolsForHaul.Utilities;

    public class WorkGiver_HaulCorpses_Vehicle : WorkGiver_Haul
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling();
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            Trace.DebugWriteHaulingPawn(pawn);

            List<Thing> availableVehicles = ToolsForHaulUtility.AvailableVehicles(pawn);

            if (availableVehicles.Count == 0) return true;

            if (RightVehicle.GetRightVehicle(pawn, availableVehicles, DefDatabase<WorkTypeDef>.GetNamed("Hauling")) == null)
                return true;

            if (pawn.RaceProps.Animal || !pawn.RaceProps.Humanlike || !pawn.RaceProps.hasGenders)
                return true;

            return pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling().Count == 0;
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
            if (ToolsForHaulUtility.IsDriver(pawn))
            {
                cart = ToolsForHaulUtility.GetCartByDriver(pawn);

                if (cart == null)
                {
                    // JobFailReason.Is("Can't haul with military vehicle");
                    return ToolsForHaulUtility.DismountAtParkingLot(pawn, cart);
                }
            }

            if (cart == null)
            {
                List<Thing> availableVehicles = ToolsForHaulUtility.AvailableVehicles(pawn);

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
                Log.Message("WorkGiver_HaulCorpses NoEmptyPlaceLowerTrans");
                JobFailReason.Is(ToolsForHaulUtility.NoEmptyPlaceLowerTrans);
                return null;
            }

            if (ToolsForHaulUtility.AvailableAnimalCart(cart) || ToolsForHaulUtility.IsVehicleAvailable(pawn, cart))
            {
                return ToolsForHaulUtility.HaulWithTools(pawn, cart, t);
            }
            JobFailReason.Is(ToolsForHaulUtility.NoAvailableCart);

            return null;
        }
    }
}
