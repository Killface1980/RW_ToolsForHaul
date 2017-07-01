#define LOGGING

namespace ToolsForHaul.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using RimWorld;

    using ToolsForHaul.Components;
    using ToolsForHaul.Defs;
    using ToolsForHaul.Vehicles;

    using UnityEngine;

    using Verse;
    using Verse.AI;

    public static class TFH_Utility
    {
        private const double ValidDistance = 30;


        private const int NearbyCell = 10;

        public static bool IsMountedOnAnimalAndAvailable(this Vehicle_Cart cart)
        {
            Pawn driver = cart.MountableComp.IsMounted ? cart.MountableComp.Driver : null;
            if (driver == null)
            {
                return false;
            }

            return driver.RaceProps.Animal && driver.CanCasuallyInteractNow()
                   && driver.needs.food.CurCategory < HungerCategory.Starving
                   && driver.needs.rest.CurCategory < RestCategory.VeryTired && !HealthAIUtility.ShouldBeTendedNow(driver);
        }

        public static bool IsAllowedToRide(this Pawn pawn, ThingWithComps cart)
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



        public static Job DismountAtParkingLot(this Pawn pawn, Vehicle_Cart cart, string caller)
        {
            Log.Message(caller);

            Job job = new Job(HaulJobDefOf.DismountAtParkingLot) { targetA = cart };

            IntVec3 parkingSpace = IntVec3.Invalid;
            pawn.FindParkingSpace(cart.Position, out parkingSpace);

            job.targetB = parkingSpace;

            if (job.targetB != IntVec3.Invalid)
            {
                Trace.AppendLine("DismountAtParkingLot Job is issued - pawn: " + pawn.LabelShort + " - " + cart.Label);
                Trace.LogMessage();
                return job;
            }

            JobFailReason.Is(Static.NoEmptyPlaceForCart);
            Trace.AppendLine("No Job. Reason: " + JobFailReason.Reason);
            Trace.LogMessage();
            return null;
        }

        public static bool HasFreeCellsInParkingLot(this Map map)
        {
            bool flag = false;
            foreach (IntVec3 cell in map.AllCells)
            {
                if (cell.GetZone(map) is Zone_ParkingLot)
                {
                    if (map.thingGrid.ThingsAt(cell).Any(
                        current => current.def.passability == Traversability.PassThroughOnly
                                   || current.def.passability == Traversability.Impassable))
                    {
                        continue;
                    }

                    flag = true;
                    break;
                }
            }

            return flag;
        }

        public static bool FindParkingSpace(this Pawn pawn, IntVec3 searchPos, out IntVec3 parkingSpace)
        {
            List<IntVec3> parkingLot = new List<IntVec3>();
            foreach (IntVec3 cell in pawn.Map.AllCells)
            {
                Zone zone = pawn.Map.zoneManager.ZoneAt(cell);
                if (zone is Zone_ParkingLot)
                {
                    if (pawn.Map.thingGrid.ThingsAt(cell)
                        .Any(
                            current => current.def.passability == Traversability.PassThroughOnly
                                       || current.def.passability == Traversability.Impassable))
                    {
                        continue;
                    }

                    if (!pawn.CanReserveAndReach(cell, PathEndMode.ClosestTouch, pawn.NormalMaxDanger()))
                        continue;

                    parkingLot.Add(cell);
                }
            }

            if (parkingLot.Count > 0)
            {
                IOrderedEnumerable<IntVec3> orderedEnumerable = parkingLot.OrderBy(x => x.DistanceTo(searchPos));
                parkingSpace = orderedEnumerable.First();
                return true;
            }

            parkingSpace = IntVec3.Invalid;
            return false;
        }

        public static IntVec3 FindStorageCell(Pawn pawn, Thing haulable, List<LocalTargetInfo> targetQueue = null)
        {
            // Find closest cell in queue.
            if (!targetQueue.NullOrEmpty())
            {
                foreach (LocalTargetInfo target in targetQueue)
                {
                    foreach (IntVec3 adjCell in GenAdjFast.AdjacentCells8Way(target))
                    {
                        if (!targetQueue.Contains(adjCell) && adjCell.IsValidStorageFor(pawn.Map, haulable))
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
            foreach (SlotGroup slotGroup in pawn.Map.slotGroupManager.AllGroupsListInPriorityOrder)
            {
                if (slotGroup.Settings.Priority < currentPriority) break;
                {
                    foreach (IntVec3 cell in slotGroup.CellsList)
                    {
                        if ((!targetQueue.NullOrEmpty() && !targetQueue.Contains(cell)) || targetQueue.NullOrEmpty())
                        {
                            if (cell.GetStorable(pawn.Map) == null)
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
            List<Thing> availableVehicles = AvailableVehicles(pawn);

            foreach (Thing thing in availableVehicles)
            {
                Vehicle_Cart vehicle = (Vehicle_Cart)thing;
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

        public static List<Thing> AvailableVehicles(this Pawn pawn)
        {
            List<Thing> availableVehicles = pawn.Map.listerThings.AllThings.FindAll(
                aV => (aV is Vehicle_Cart) && !aV.IsForbidden(pawn.Faction)
                              && ((!aV.TryGetComp<CompMountable>().IsMounted && pawn.CanReserve(aV)) // Unmounted
                                  || aV.TryGetComp<CompMountable>().Driver == pawn)); // or Driver is pawn himself
            return availableVehicles;
        }

        public static List<Thing> MountedVehicles(this Map map)
        {
            List<Thing> availableVehicles =
                map.listerThings.AllThings.FindAll(
                    aV => aV is Vehicle_Cart && ((Vehicle_Cart)aV).MountableComp.IsMounted);
            return availableVehicles;
        }

        public static List<Thing> VehiclesOfPlayer(this Map map)
        {
            List<Thing> availableVehicles =
                map.listerThings.AllThings.FindAll(
                    aV => (aV is Vehicle_Cart) && aV.Faction == Faction.OfPlayer);
            return availableVehicles;
        }

        public static List<Thing> AvailableRideables(Pawn pawn)
        {
            List<Thing> availableVehicles = pawn.Map.listerThings.AllThings.FindAll(
                aV => (aV is Vehicle_Saddle) && !aV.IsForbidden(pawn.Faction)
                              && ((aV.TryGetComp<CompMountable>().IsMounted && pawn.CanReserve(aV)) // Unmounted
                                  || aV.TryGetComp<CompMountable>().Driver == pawn)); // or Driver is pawn himself
            return availableVehicles;
        }

        public static List<Thing> AvailableVehiclesForSteeling(this Pawn pawn, float distance)
        {
            List<Thing> availableVehicles = pawn.Map.listerThings.AllThings.FindAll(
                aV => (aV is Vehicle_Cart) && !((Vehicle_Cart)aV).MountableComp.IsMounted && pawn.CanReserve(aV) && aV.Position.InHorDistOf(pawn.Position, distance)); // Unmounted
            availableVehicles.OrderBy(x => x.Position.DistanceTo(pawn.Position));
            return availableVehicles;
        }

        public static Vehicle_Cart MountedVehicle(this Pawn pawn)
        {

            List<Thing> availableVehicles = pawn.AvailableVehicles();
            foreach (var thing in availableVehicles)
            {
                var vehicle = (Vehicle_Cart)thing;
                if (vehicle.MountableComp.Driver == pawn)
                {
                    return vehicle;
                }
            }

            return null;
        }

        public static Vehicle_Saddle GetSaddleByRider(Pawn pawn)
        {

            List<Thing> availableVehicles = AvailableRideables(pawn);
            foreach (var thing in availableVehicles)
            {
                var vehicle = (Vehicle_Saddle)thing;
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

        public static Job HaulWithTools(Pawn pawn, Vehicle_Cart cart, Thing haulThing = null)
        {
            Trace.StopWatchStart();
            bool forced = false;

            // Job Setting
            JobDef jobDef = null;
            LocalTargetInfo targetC;
            int maxItem;
            int thresholdItem;
            int reservedMaxItem;
            IEnumerable<Thing> remainingItems;
            bool shouldDrop;

            // Thing lastItem = TryGetBackpackLastItem(pawn);
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

            Zone zone = pawn.Map.zoneManager.ZoneAt(cart.Position);

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

            Thing lastItem = null;

            // Drop remaining item
            // if (reservedMaxItem >= Math.Ceiling(maxItem * 0.5) && shouldDrop)
            if (shouldDrop)
            {
                bool startDrop = false;
                for (int i = 0; i < remainingItems.Count(); i++)
                {
                    // if (startDrop == false)
                    // {
                    // if (remainingItems.ElementAt(i) == lastItem)
                    // {
                    // startDrop = true;
                    // }
                    // else
                    // {
                    // continue;
                    // }
                    // }
                    IntVec3 storageCell = FindStorageCell(pawn, remainingItems.ElementAt(i), job.targetQueueB);
                    if (storageCell == IntVec3.Invalid)
                    {
                        break;
                    }

                    job.targetQueueB.Add(storageCell);
                }

                if (!job.targetQueueB.NullOrEmpty())
                {
                    return job;
                }

                if (job.def == HaulJobDefOf.HaulWithCart && !(zone is Zone_ParkingLot))
                {
                    return pawn.DismountAtParkingLot(cart, "TFH U Parkin");
                }

                Log.Message("HaulWithTools Failes NoEmptyPlaceLowerTrans");
                JobFailReason.Is(Static.NoEmptyPlaceLowerTrans);
#if DEBUG
                Log.Message("No Job. Reason: " + Static.NoEmptyPlaceLowerTrans);
#endif
                return null;
            }

            // Collect item
            Trace.AppendLine("Start Collect item");

            // ClosestThing_Global_Reachable Configuration
            Predicate<Thing> predicate = item => !job.targetQueueA.Contains(item)
                                                 && !item.IsBurning() // && !deniedThings.Contains(item)
                                                 && cart.allowances.Allows(item) && pawn.CanReserveAndReach(
                                                     item,
                                                     PathEndMode.Touch,
                                                     pawn.NormalMaxDanger());

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

            // Collect and drop item
            while (reservedMaxItem < maxItem)
            {
                if (flag1 == false && !job.targetQueueA.NullOrEmpty()
                    && job.targetQueueA.First().Thing.Position != IntVec3.Invalid)
                {
                    flag1 = true;
                    searchPos = job.targetQueueA.First().Thing.Position;
                    maxDistance = NearbyCell;
                }

                // Find Haulable
                Thing closestHaulable = GenClosest.ClosestThing_Global_Reachable(
                    searchPos,
                    pawn.Map,
                    pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling(),
                    PathEndMode.Touch,
                    TraverseParms.For(pawn, Danger.Some),
                    maxDistance,
                    predicate);

                // Check it can be hauled
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

                // Find StorageCell
                IntVec3 storageCell = FindStorageCell(pawn, closestHaulable, job.targetQueueB);
                if (storageCell == IntVec3.Invalid) break;

                // Add Queue & Reserve
                job.targetQueueA.Add(closestHaulable);
                job.targetQueueB.Add(storageCell);
                reservedMaxItem++;
            }

            Trace.AppendLine("Elapsed Time");
            Trace.StopWatchStop();

            // Check job is valid
            if (!job.targetQueueA.NullOrEmpty() && reservedMaxItem + job.targetQueueA.Count > thresholdItem
                && !job.targetQueueB.NullOrEmpty())
            {
                Trace.AppendLine("Hauling Job is issued");
                Trace.LogMessage();
                return job;
            }

            if (job.def == HaulJobDefOf.HaulWithCart && !(zone is Zone_ParkingLot))
            {
                Trace.AppendLine("In DismountAtParkingLot: ");
                return pawn.DismountAtParkingLot(cart, "TFHU");
            }

            if (job.targetQueueA.NullOrEmpty())
            {
                JobFailReason.Is("NoHaulable".Translate());
            }
            else if (reservedMaxItem + job.targetQueueA.Count <= thresholdItem)
            {
                JobFailReason.Is(Static.TooLittleHaulable);
            }
            else if (job.targetQueueB.NullOrEmpty())
            {
                Log.Message("HaulWithTools NoEmptyPlaceLowerTrans");
                JobFailReason.Is(Static.NoEmptyPlaceLowerTrans);
            }

            Trace.AppendLine("No Job. Reason: " + JobFailReason.Reason);
            Trace.LogMessage();
            return null;
        }

        public static bool IsDriver(this Pawn pawn)
        {
            List<Thing> availableVehicles = AvailableVehicles(pawn);
            foreach (Vehicle_Cart vehicle in availableVehicles)
            {
                if (vehicle.MountableComp.Driver == pawn)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsDriverOfThisVehicle(this Pawn pawn, Vehicle_Cart vehicleReq)
        {
            List<Thing> availableVehicles = AvailableVehicles(pawn);
            foreach (var thing in availableVehicles)
            {
                var vehicleTurret = (Vehicle_Cart)thing;
                if (vehicleTurret.MountableComp.Driver == pawn && vehicleTurret == vehicleReq)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Selects the appropriate vehicle by worktype
        /// </summary>
        public static Vehicle_Cart GetRightVehicle(
            Pawn pawn,
            List<Thing> availableVehicles,
            WorkTypeDef worktype,
            Thing haulThing = null)
        {
            Thing cart = null;

            if (worktype.Equals(WorkTypeDefOf.Hunting))
            {
                IOrderedEnumerable<Thing> orderedEnumerable =
                    availableVehicles.OrderBy(x => pawn.Position.DistanceToSquared(x.Position));
                foreach (Thing thing in orderedEnumerable)
                {
                    Vehicle_Cart vehicleCart = (Vehicle_Cart)thing;
                    if (vehicleCart == null) continue;
                    if (!pawn.IsAllowedToRide(vehicleCart)) continue;
                    if (vehicleCart.HasGasTank())
                    {
                        if (vehicleCart.GasTankComp.tankLeaking) continue;
                    }
                    if (vehicleCart.ExplosiveComp.wickStarted) continue;
                    if (!vehicleCart.IsCurrentlyMotorized()) continue;

                    cart = vehicleCart;
                    break;
                }
            }
            else if (worktype == WorkTypeDefOf.Hauling)
            {
                IOrderedEnumerable<Thing> orderedEnumerable2 =
                    availableVehicles.OrderByDescending(x => (x as Vehicle_Cart)?.MaxItem).ThenBy(x => pawn.Position.DistanceToSquared(x.Position));

                foreach (Thing thing in orderedEnumerable2)
                {
                    Vehicle_Cart vehicleCart = (Vehicle_Cart)thing;
                    if (vehicleCart == null)
                    {
                        continue;
                    }

                    if (!pawn.IsAllowedToRide(vehicleCart)) continue;
                    if (vehicleCart.HasGasTank())
                    {
                        if (vehicleCart.GasTankComp.tankLeaking) continue;
                    }
                    if (vehicleCart.ExplosiveComp.wickStarted) continue;
                    if (!vehicleCart.IsCurrentlyMotorized()) continue;
                    if (haulThing != null)
                    {
                        if (!vehicleCart.allowances.Allows(haulThing)) continue;
                    }

                    cart = vehicleCart;
                    break;
                }
            }
            else if (worktype.Equals(WorkTypeDefOf.Construction))
            {
                IOrderedEnumerable<Thing> orderedEnumerable2 =
                    availableVehicles.OrderBy(x => pawn.Position.DistanceToSquared(x.Position)).ThenByDescending(x => (x as Vehicle_Cart).VehicleComp.VehicleSpeed);
                foreach (Thing thing in orderedEnumerable2)
                {
                    Vehicle_Cart vehicleCart = (Vehicle_Cart)thing;
                    if (vehicleCart == null)
                        continue;
                    if (!pawn.IsAllowedToRide(vehicleCart)) continue;
                    if (vehicleCart.HasGasTank())
                    {
                        if (vehicleCart.GasTankComp.tankLeaking) continue;
                    }
                    if (vehicleCart.ExplosiveComp.wickStarted) continue;
                    if (!vehicleCart.IsCurrentlyMotorized()) continue;
                    cart = vehicleCart;
                    break;
                }
            }

            return (Vehicle_Cart)cart;
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
        public static void DismountGizmoFloatMenu(Vehicle_Cart cart, Pawn driver)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            Action action_DismountInBase = () =>
                {

                    Job jobNew = driver.DismountAtParkingLot(cart, "DGFM");

                    driver.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                };

            Action action_Dismount = () =>
                {
                    if (!driver.Position.InBounds(driver.Map))
                    {
                        cart.MountableComp.DismountAt(driver.Position);
                        return;
                    }

                    cart.MountableComp.DismountAt(
                        driver.Position - cart.def.interactionCellOffset.RotatedBy(driver.Rotation));
                    driver.Position = driver.Position.RandomAdjacentCell8Way();

                    // mountableComp.DismountAt(myPawn.Position - VehicleDef.interactionCellOffset.RotatedBy(myPawn.Rotation));
                };

            options.Add(new FloatMenuOption("Dismount".Translate(driver.LabelShort), action_Dismount));
            bool flag = cart.Map.HasFreeCellsInParkingLot();

            if (flag)
            {
                options.Add(new FloatMenuOption("DismountAtParkingLot".Translate(cart.LabelShort), action_DismountInBase));
            }
            else
            {
                FloatMenuOption failer = new FloatMenuOption(
                    "NoFreeParkingSpace".Translate(cart.LabelShort),
                    null,
                    MenuOptionPriority.Default,
                    null,
                    null,
                    0f,
                    null,
                    null);
                options.Add(failer);
            }

            FloatMenu window = new FloatMenu(options, "WhereToDismount".Translate());
            Find.WindowStack.Add(window);
        }

    }
}