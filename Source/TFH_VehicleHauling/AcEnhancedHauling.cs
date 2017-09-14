namespace ToolsForHaul
{
    using System.Collections.Generic;

    using RimWorld;

    using ToolsForHaul.Utilities;
    using ToolsForHaul.Vehicles;

    using Verse;
    using Verse.AI;

    public static class AcEnhancedHauling
    {
        public const float NoneHaulDistance = 16f;

        public static Job SmartBuild(Pawn pawn, Job job)
        {
            if (pawn.story.WorkTagIsDisabled(WorkTags.Hauling))
            {
                return job;
            }

            Vehicle_Cart cart = null;

            Thing thing = job.targetA.Thing;

            // Vehicle selection
            if (pawn.IsDriver())
            {
                cart = pawn.MountedVehicle();
            }

            if (cart == null)
            {
                List<Thing> availableVehicles = pawn.AvailableVehicles();
                if (availableVehicles.Count == 0) return null;

                cart = TFH_Utility.GetRightVehicle(pawn, availableVehicles, WorkTypeDefOf.Hauling, thing) as Vehicle_Cart;

                if (cart == null)
                    return null;
            }

            if (cart.IsBurning())
            {
                JobFailReason.Is(Static.BurningLowerTrans);
                return null;
            }

            if (!cart.allowances.Allows(thing))
            {
                JobFailReason.Is("Cart does not allow that thing");
                return null;
            }

            if (cart.Map.listerHaulables.ThingsPotentiallyNeedingHauling().Count == 0 && cart.innerContainer.Count == 0)
            {
                JobFailReason.Is("NoHaulable".Translate());
                return null;
            }

            StoragePriority currentPriority = HaulAIUtility.StoragePriorityAtFor(thing.Position, thing);
            IntVec3 storeCell;
            if (!StoreUtility.TryFindBestBetterStoreCellFor(thing, pawn, pawn.Map, currentPriority, pawn.Faction, out storeCell))
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
            IntVec3 targetPos = thing.Position;
            IntVec3 destPos = job.targetB.Thing.Position;

            if ((targetPos - destPos).LengthHorizontalSquared > (targetPos - storeCell).LengthHorizontalSquared)
            {
                if (cart.IsMountedOnAnimalAndAvailable() || pawn.IsAllowedToRide(cart))
                {
                    return TFH_Utility.HaulWithToolsToContainer(pawn, cart, thing, job);
                }
            }


            return null;
        }
    }
}