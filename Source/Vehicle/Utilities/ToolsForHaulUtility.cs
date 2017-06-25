//#define DEBUG
namespace ToolsForHaul.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using RimWorld;

    using ToolsForHaul.Components.Vehicle;
    using ToolsForHaul.JobDefs;

    using UnityEngine;

    using Verse;
    using Verse.AI;

    public static class ToolsForHaulUtility
    {
        private const double ValidDistance = 30;

        public static readonly string BurningLowerTrans;

        public static readonly string NoAvailableCart;

        public static readonly string NoEmptyPlaceForCart;

        public static readonly string NoEmptyPlaceLowerTrans;

        public static readonly string TooLittleHaulable;

        public static List<Thing> Cart = new List<Thing>();

        private const int NearbyCell = 10;

        static ToolsForHaulUtility()
        {
            TooLittleHaulable = "TooLittleHaulable".Translate();
            NoEmptyPlaceForCart = "NoEmptyPlaceForCart".Translate();
            NoEmptyPlaceLowerTrans = "NoEmptyPlaceLower".Translate();
            NoAvailableCart = "NoAvailableCart".Translate();
            BurningLowerTrans = "BurningLower".Translate();
        }

        public static bool AvailableAnimalCart(ThingWithComps cart)
        {
            Pawn Driver = cart.GetComp<CompMountable>().IsMounted ? cart.GetComp<CompMountable>().Driver : null;
            if (Driver == null) return false;

            return Driver.RaceProps.Animal && Driver.CanCasuallyInteractNow()
                   && Driver.needs.food.CurCategory < HungerCategory.Starving
                   && Driver.needs.rest.CurCategory < RestCategory.VeryTired && !HealthAIUtility.ShouldBeTendedNow(Driver);
        }

        public static bool AvailableVehicle(Pawn pawn, ThingWithComps cart)
        {
            if (cart.Faction != Faction.OfPlayer) return false;
            if (cart.IsForbidden(pawn.Faction)) return false;
            if (cart.Position.IsForbidden(pawn)) return false;
            if (cart.IsBurning()) return false;
            if (!pawn.CanReserveAndReach(cart, PathEndMode.ClosestTouch, Danger.Some)) return false;

            if (!cart.TryGetComp<CompMountable>().IsMounted) return true;
            if (cart.TryGetComp<CompMountable>().Driver == pawn) return true;
            if (cart.TryGetComp<CompMountable>().IsMounted && cart.TryGetComp<CompMountable>().Driver.RaceProps.Animal) return true;
            return false;
        }

        public static Job DismountInBase(Pawn pawn, Thing cart)
        {
            Job job = new Job(HaulJobDefOf.DismountInBase);
            job.targetA = cart;
            job.targetB = FindStorageCell(pawn, cart, pawn.Map);

            if (job.targetB != IntVec3.Invalid)
            {
                Trace.AppendLine("DismountInBase Job is issued");
                Trace.LogMessage();
                return job;
            }

            JobFailReason.Is(NoEmptyPlaceForCart);
            Trace.AppendLine("No Job. Reason: " + JobFailReason.Reason);
            Trace.LogMessage();
            return null;
        }

        public static IntVec3 FindStorageCell(Pawn pawn, Thing haulable, Map map, List<LocalTargetInfo> targetQueue = null)
        {
            // Find closest cell in queue.
            if (!targetQueue.NullOrEmpty())
            {
                foreach (LocalTargetInfo target in targetQueue)
                {
                    foreach (IntVec3 adjCell in GenAdjFast.AdjacentCells8Way(target))
                    {
                        if (!targetQueue.Contains(adjCell) && adjCell.IsValidStorageFor(map, haulable))
                        {
                            if (pawn.CanReserveAndReach(adjCell, PathEndMode.ClosestTouch, Danger.Some))
                            {
                                return adjCell;
                            }
                        }
                    }
                }
            }

            /*
                        StoragePriority currentPriority = HaulAIUtility.StoragePriorityAtFor(closestHaulable.Position, closestHaulable);
                        IntVec3 foundCell;
                        if (StoreUtility.TryFindBestBetterStoreCellFor(closestHaulable, pawn, currentPriority, pawn.Faction, out foundCell, true))
                            return foundCell;
                        */
            // Vanilla code is not worked item on container.
            StoragePriority currentPriority = HaulAIUtility.StoragePriorityAtFor(haulable.Position, haulable);
            foreach (SlotGroup slotGroup in map.slotGroupManager.AllGroupsListInPriorityOrder)
            {
                if (slotGroup.Settings.Priority < currentPriority) break;
                {
                    foreach (IntVec3 cell in slotGroup.CellsList)
                    {
                        if ((!targetQueue.NullOrEmpty() && !targetQueue.Contains(cell)) || targetQueue.NullOrEmpty())
                        {
                            if (cell.GetStorable(map) == null)
                            {
                                if (slotGroup.Settings.AllowedToAccept(haulable) && pawn.CanReserveAndReach(cell, PathEndMode.ClosestTouch, Danger.Deadly))
                                {
                                    return cell;
                                }
                            }
                        }
                    }
                }
            }
            return IntVec3.Invalid;
        }

        public static Vehicle_Cart FindWheelChair(Pawn patient, Pawn pawn)
        {
            foreach (Vehicle_Cart vehicle in Cart)
            {
                if (vehicle.VehicleComp.IsMedical() && vehicle.MountableComp.Driver == null
                    && pawn.CanReserveAndReach(vehicle.InteractionCell, PathEndMode.ClosestTouch, Danger.Some)
                    && vehicle.Faction == pawn.Faction)
                {
                    Debug.Log("Wheel chair found");
                    return vehicle;
                }
            }

            Debug.Log("No wheel chair found");
            return null;
        }

        public static Vehicle_Cart GetCartByDriver(Pawn pawn)
        {
            foreach (Vehicle_Cart vehicle in Cart)
            {
                if (vehicle.MountableComp.Driver == pawn)
                {
                    return vehicle;
                }
            }
            return null;
        }

        /// <summary>
        ///     Calculates the actual current movement speed of a pawn
        /// </summary>
        /// <param name="pawn">Pawn to calculate speed of</param>
        /// <returns>Move speed in cells per second</returns>
        public static float GetMoveSpeed(Pawn pawn)
        {
            Map map = pawn.Map;
            float movePerTick = 60 / pawn.GetStatValue(StatDefOf.MoveSpeed, false); // Movement per tick
            movePerTick += pawn.Map.pathGrid.CalculatedCostAt(pawn.Position, false, pawn.Position);
            Building edifice = pawn.Position.GetEdifice(map);
            if (edifice != null)
            {
                movePerTick += edifice.PathWalkCostFor(pawn);
            }

            // Case switch to handle walking, jogging, etc.
            if (pawn.CurJob != null)
            {
                switch (pawn.CurJob.locomotionUrgency)
                {
                    case LocomotionUrgency.Amble:
                        movePerTick *= 3;
                        if (movePerTick < 60)
                        {
                            movePerTick = 60;
                        }

                        break;
                    case LocomotionUrgency.Walk:
                        movePerTick *= 2;
                        if (movePerTick < 50)
                        {
                            movePerTick = 50;
                        }

                        break;
                    case LocomotionUrgency.Jog:
                        break;
                    case LocomotionUrgency.Sprint:
                        movePerTick = Mathf.RoundToInt(movePerTick * 0.75f);
                        break;
                }
            }

            return 60 / movePerTick;
        }

        public static Vehicle_Cart GetTurretByDriver(Pawn pawn)
        {
            foreach (Vehicle_Cart vehicleTurret in Cart)
                if (vehicleTurret.MountableComp.Driver == pawn)
                {
                    return vehicleTurret;
                }

            return null;
        }

        public static Job HaulWithTools(Pawn pawn, Vehicle_Cart cart = null, Thing haulThing = null)
        {
            Trace.stopWatchStart();
            bool forced = false;
            // Job Setting
            JobDef jobDef = null;
            LocalTargetInfo targetC;
            int maxItem;
            int thresholdItem;
            int reservedMaxItem;
            IEnumerable<Thing> remainingItems;
            bool shouldDrop;
            //       Thing lastItem = TryGetBackpackLastItem(pawn);

            if (cart.MountableComp.IsMounted)
            {
                jobDef = cart.MountableComp.Driver.RaceProps.Animal
                    ? HaulJobDefOf.HaulWithAnimalCart
                    : HaulJobDefOf.HaulWithCart;
            }
            else
            {
                jobDef = HaulJobDefOf.HaulWithCart;
            }

            targetC = cart;

            maxItem = cart.MaxItem;
            thresholdItem = (int)Math.Ceiling(maxItem * 0.25);
            reservedMaxItem = cart.innerContainer.Count;
            remainingItems = cart.innerContainer;

            shouldDrop = reservedMaxItem > 0 ? true : false;

            Job job = new Job(jobDef)
            {
                targetQueueA = new List<LocalTargetInfo>(),
                targetQueueB = new List<LocalTargetInfo>(),
                targetC = targetC
            };


            Trace.AppendLine(
                pawn.LabelCap + " In HaulWithTools: " + jobDef.defName + "\n" + "MaxItem: " + maxItem
                + " reservedMaxItem: " + reservedMaxItem);

            bool ShouldDrop = true;
            Thing lastItem = null;
            //Drop remaining item
            if (reservedMaxItem >= Math.Ceiling(maxItem * 0.5) && ShouldDrop)
            {
                bool startDrop = false;
                for (int i = 0; i < remainingItems.Count(); i++)
                {
                    if (startDrop == false)
                        if (remainingItems.ElementAt(i) == lastItem) startDrop = true;
                        else continue;
                    IntVec3 storageCell = FindStorageCell(pawn, remainingItems.ElementAt(i), pawn.Map, job.targetQueueB);
                    if (storageCell == IntVec3.Invalid) break;
                    job.targetQueueB.Add(storageCell);
                }
                if (!job.targetQueueB.NullOrEmpty()) return job;
                if (cart != null && job.def == HaulJobDefOf.HaulWithCart && !cart.IsInValidStorage())
                    return DismountInBase(pawn, cart);
                JobFailReason.Is(ToolsForHaulUtility.NoEmptyPlaceLowerTrans);
#if DEBUG
                Log.Message("No Job. Reason: " + ToolsForHaulUtility.NoEmptyPlaceLowerTrans);
#endif
                return (Job)null;
            }
            // Collect item
                Trace.AppendLine("Start Collect item");

            //ClosestThing_Global_Reachable Configuration
            Predicate<Thing> predicate = item
                => !job.targetQueueA.Contains(item) && !FireUtility.IsBurning(item) //&& !deniedThings.Contains(item)
                   && (cart != null && cart.allowances.Allows(item))
                   && pawn.CanReserveAndReach(item, PathEndMode.Touch, DangerUtility.NormalMaxDanger(pawn));

            IntVec3 searchPos;
            if (haulThing != null)
            {
                searchPos = haulThing.Position;
            }
            else
            {
                searchPos = cart.Position;
            }

            bool flag1 = false;
            int maxDistance = 99999;

            //Collect and drop item
            while (reservedMaxItem < maxItem)
            {
                if (flag1 == false && !job.targetQueueA.NullOrEmpty() && job.targetQueueA.First().Thing.Position != IntVec3.Invalid)
                {
                    flag1 = true;
                    searchPos = job.targetQueueA.First().Thing.Position;
                    maxDistance = NearbyCell;
                }

                //Find Haulable
                Thing closestHaulable = GenClosest.ClosestThing_Global_Reachable(searchPos, pawn.Map,
                    pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling(),
                    PathEndMode.Touch,
                    TraverseParms.For(pawn, Danger.Some),
                    maxDistance,
                    predicate);

                //Check it can be hauled
                /*
                if ((closestHaulable is UnfinishedThing && ((UnfinishedThing)closestHaulable).BoundBill != null)
                    || (closestHaulable.def.IsNutritionSource && !SocialProperness.IsSociallyProper(closestHaulable, pawn, false, true)))
                {
                    deniedThings.Add(closestHaulable);
                    continue;
                }*/
                if (closestHaulable == null)
                {
                    break;
                }

                //Find StorageCell
                IntVec3 storageCell = FindStorageCell(pawn, closestHaulable, pawn.Map, job.targetQueueB);
                if (storageCell == IntVec3.Invalid)
                {
                    break;
                }

                //Add Queue & Reserve
                job.targetQueueA.Add(closestHaulable);
                job.targetQueueB.Add(storageCell);
                reservedMaxItem++;
            }

            Trace.AppendLine("Elapsed Time");
            Trace.stopWatchStop();

            // Check job is valid
            if (!job.targetQueueA.NullOrEmpty() && reservedMaxItem + job.targetQueueA.Count > thresholdItem
                && !job.targetQueueB.NullOrEmpty())
            {
                Trace.AppendLine("Hauling Job is issued");
                Trace.LogMessage();
                return job;
            }

            if (cart != null && job.def == HaulJobDefOf.HaulWithCart && !cart.IsInValidStorage())
            {
                Trace.AppendLine("In DismountInBase: ");
                return DismountInBase(pawn, cart);
            }

            if (job.targetQueueA.NullOrEmpty())
            {
                JobFailReason.Is("NoHaulable".Translate());
            }
            else if (reservedMaxItem + job.targetQueueA.Count <= thresholdItem)
            {
                JobFailReason.Is(TooLittleHaulable);
            }
            else if (job.targetQueueB.NullOrEmpty())
            {
                JobFailReason.Is(NoEmptyPlaceLowerTrans);
            }
            Trace.AppendLine("No Job. Reason: " + JobFailReason.Reason);
            Trace.LogMessage();
            return null;
        }

        public static bool IsDriver(Pawn pawn)
        {
            foreach (Vehicle_Cart vehicle in Cart) if (vehicle.MountableComp.Driver == pawn) return true;
            return false;
        }

        public static bool IsDriverOfThisVehicle(Pawn pawn, Thing vehicleReq)
        {
            foreach (Vehicle_Cart vehicleTurret in Cart) if (vehicleTurret.MountableComp.Driver == pawn && vehicleTurret == vehicleReq) return true;
            return false;
        }

        /*
        public static Thing TryGetBackpackLastItem(Pawn pawn)
        {
            Apparel_Backpack backpack = TryGetBackpack(pawn);
            if (backpack == null) return null;
            Thing lastItem = null;
            int lastItemInd = -1;
            Thing foodInInventory = FoodUtility.BestFoodInInventory(pawn);
            if (backpack.slotsComp.slots.Count > 0)
            {
                if (backpack.numOfSavedItems > 0)
                {
                    lastItemInd = (backpack.numOfSavedItems >= backpack.MaxItem
                                       ? backpack.slotsComp.slots.Count
                                       : backpack.numOfSavedItems) - 1;
                    lastItem = backpack.slotsComp.slots[lastItemInd];
                }

                if (foodInInventory != null && backpack.numOfSavedItems < backpack.slotsComp.slots.Count
                    && backpack.slotsComp.slots[lastItemInd + 1] == foodInInventory) lastItem = foodInInventory;
            }

            return lastItem;
        }
        */
    }
}