#define LOGGING

namespace TFH_VehicleBase
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using RimWorld;

    using TFH_VehicleBase.Components;
    using TFH_VehicleBase.DefOfs_TFH;

    using UnityEngine;

    using Verse;
    using Verse.AI;

    public static class TFH_BaseUtility
    {
        private static List<IntVec3> candidates = new List<IntVec3>();

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

        public static bool IsPlayerAllowedToRide(this Pawn pawn, Vehicle_Cart cart)
        {
            if (cart.Faction != Faction.OfPlayer) return false;
            if (cart.IsForbidden(pawn.Faction)) return false;
            if (cart.Position.IsForbidden(pawn)) return false;
            if (cart.IsBurning()) return false;
            if (!pawn.CanReserve(cart)) return false;

            if (!cart.MountableComp.IsMounted) return true;
            if (cart.MountableComp.Driver == pawn) return true;
            if (cart.MountableComp.IsMounted && cart.MountableComp.Driver.RaceProps.Animal) return true;
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
            if (availableVehicles.NullOrEmpty())
            {
                return null;
            }

            Thing cart = null;

            if (worktype.Equals(WorkTypeDefOf.Hunting))
            {
                IOrderedEnumerable<Thing> armoured =
                    availableVehicles.OrderBy(x => ((Vehicle_Cart)x).health.summaryHealth);

                IOrderedEnumerable<Thing> orderedEnumerable =
                    availableVehicles.OrderBy(x => ((Vehicle_Cart)x).health.summaryHealth);

                foreach (Thing thing in armoured)
                {
                    Vehicle_Cart vehicleCart = (Vehicle_Cart)thing;
                    if (vehicleCart == null) continue;
                    if (!(vehicleCart.Position.GetZone(vehicleCart.Map) is Zone_ParkingLot)) continue;
                    if (!pawn.IsPlayerAllowedToRide(vehicleCart)) continue;
                    if (vehicleCart.HasGasTank())
                    {
                        if (vehicleCart.GasTankComp.tankLeaking) continue;
                    }

                    if (vehicleCart.IsAboutToBlowUp())
                    {
                        continue;
                    }

                    if (!vehicleCart.IsCurrentlyMotorized())
                    {
                        continue;
                    }

                    return vehicleCart;
                }


                foreach (Thing thing in orderedEnumerable)
                {
                    Vehicle_Cart vehicleCart = (Vehicle_Cart)thing;
                    if (vehicleCart == null) continue;
                    if (!(vehicleCart.Position.GetZone(vehicleCart.Map) is Zone_ParkingLot)) continue;
                    if (!pawn.IsPlayerAllowedToRide(vehicleCart)) continue;
                    if (vehicleCart.HasGasTank())
                    {
                        if (vehicleCart.GasTankComp.tankLeaking) continue;
                    }

                    if (vehicleCart.IsAboutToBlowUp())
                    {
                        continue;
                    }

                    if (!vehicleCart.IsCurrentlyMotorized())
                    {
                        continue;
                    }

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

                    if (!(vehicleCart.Position.GetZone(vehicleCart.Map) is Zone_ParkingLot)) continue;

                    if (!pawn.IsPlayerAllowedToRide(vehicleCart)) continue;
                    if (vehicleCart.HasGasTank())
                    {
                        if (vehicleCart.GasTankComp.tankLeaking) continue;
                    }

                    if (vehicleCart.IsAboutToBlowUp())
                    {
                        continue;
                    }

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
                    if (!(vehicleCart.Position.GetZone(vehicleCart.Map) is Zone_ParkingLot)) continue;
                    if (!pawn.IsPlayerAllowedToRide(vehicleCart)) continue;
                    if (vehicleCart.HasGasTank())
                    {
                        if (vehicleCart.GasTankComp.tankLeaking) continue;
                    }

                    if (vehicleCart.IsAboutToBlowUp())
                    {
                        continue;
                    }

                    if (!vehicleCart.IsCurrentlyMotorized()) continue;
                    cart = vehicleCart;
                    break;
                }
            }
            else if (worktype.Equals(WorkTypeDefOf.Doctor))
            {
                IOrderedEnumerable<Thing> orderedEnumerable2 =
                    availableVehicles.OrderBy(x => pawn.Position.DistanceToSquared(x.Position))
                        .ThenByDescending(x => ((Vehicle_Cart)x).VehicleComp.VehicleSpeed);
                foreach (Thing thing in orderedEnumerable2)
                {
                    Vehicle_Cart vehicleCart = (Vehicle_Cart)thing;
                    if (vehicleCart == null)
                        continue;
                    if (!(vehicleCart.Position.GetZone(vehicleCart.Map) is Zone_ParkingLot)) continue;
                    if (!pawn.IsPlayerAllowedToRide(vehicleCart)) continue;
                    if (vehicleCart.HasGasTank())
                    {
                        if (vehicleCart.GasTankComp.tankLeaking) continue;
                    }

                    if (vehicleCart.IsAboutToBlowUp())
                    {
                        continue;
                    }

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
        public static void AvailableVehicles(this Pawn pawn, out List<Thing> availableVehicles, Thing allowedThing = null, float distance = 999f)
        {

            availableVehicles = pawn.Map.listerThings.AllThings.FindAll(
                cart => cart is Vehicle_Cart && !cart.IsForbidden(pawn.Faction)
                        && pawn.Position.InHorDistOf(cart.Position, distance) && pawn.CanReserve(cart)
                        && (!(cart as Vehicle_Cart).MountableComp.IsMounted
                            || ((cart as Vehicle_Cart).MountableComp.Driver == pawn
                                || (cart as Vehicle_Cart).MountableComp.Driver.RaceProps.Animal))
                        && (allowedThing == null || (cart as Vehicle_Cart).allowances.Allows(allowedThing))
                        && !(cart as Vehicle_Cart).IsAboutToBlowUp());

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
                    aV => aV is Vehicle_Cart && aV.Faction == Faction.OfPlayer);
            return availableVehicles;
        }

        public static List<Thing> AvailableRideables(Pawn pawn)
        {
            List<Thing> availableVehicles = pawn.Map.listerThings.AllThings.FindAll(
                aV => aV is Vehicle_Saddle && !aV.IsForbidden(pawn.Faction)
                              && (aV.TryGetComp<CompMountable>().IsMounted && pawn.CanReserve(aV) // Unmounted
                                  || aV.TryGetComp<CompMountable>().Driver == pawn)); // or Driver is pawn himself
            return availableVehicles;
        }

        public static List<Thing> AvailableVehiclesForSteeling(this Pawn pawn, float distance)
        {
            List<Thing> availableVehicles = pawn.Map.listerThings.AllThings.FindAll(
                aV => aV is Vehicle_Cart && !((Vehicle_Cart)aV).MountableComp.IsMounted
                      && pawn.CanReserve(aV)
                      && aV.Position.InHorDistOf(pawn.Position, distance)
                      && ((Vehicle_Cart)aV).ClaimableBy(pawn.Faction)
                      && !((Vehicle_Cart)aV).IsAboutToBlowUp()); // Unmounted
            if (!availableVehicles.NullOrEmpty())
            {
                availableVehicles.OrderBy(x => x.Position.DistanceTo(pawn.Position));
            }

            return availableVehicles;
        }

        public static List<Thing> AvailableVehiclesForAllFactions(this Pawn pawn, float distance)
        {
            List<Thing> availableVehicles = pawn.Map.listerThings.AllThings.FindAll(
                aV => aV is Vehicle_Cart && !((Vehicle_Cart)aV).MountableComp.IsMounted
                      && pawn.CanReserve(aV)
                      && aV.Position.InHorDistOf(pawn.Position, distance)
                      && (aV.Faction == pawn.Faction || aV.Faction == null || pawn.Faction.HostileTo(aV.Faction))
                      && ((Vehicle_Cart)aV).ClaimableBy(pawn.Faction)
                      && !((Vehicle_Cart)aV).IsAboutToBlowUp());
            if (!availableVehicles.NullOrEmpty())
            {
                availableVehicles.OrderBy(x => x.Position.DistanceTo(pawn.Position));
            }

            return availableVehicles;
        }

        public static List<Thing> AvailableVehiclesForPawnFaction(this Pawn pawn, float distance)
        {
            List<Thing> availableVehicles = new List<Thing>();

            IEnumerable<Thing> things = pawn.Map.listerThings.AllThings.FindAll(
                cart => (cart is Vehicle_Cart) && !cart.IsForbidden(pawn.Faction)
                        && pawn.Position.InHorDistOf(cart.Position, distance));

            foreach (Thing thing in things)
            {
                Vehicle_Cart cart = thing as Vehicle_Cart;
                if (!cart.MountableComp.IsMounted && !pawn.CanReserve(cart)) { continue; }
                if (cart.MountableComp.IsMounted && !pawn.IsDriver(out Vehicle_Cart drivenCart, cart)) { continue; }
                if (cart.Faction != pawn.Faction) { continue; }
                if (cart.IsAboutToBlowUp()) { continue; }
                availableVehicles.Add(cart);
            }

            if (!availableVehicles.NullOrEmpty())
            {
                availableVehicles.OrderBy(x => x.Position.DistanceTo(pawn.Position));
            }

            return availableVehicles;
        }

     // public static List<Thing> AvailableVehicleAt(this Pawn pawn)
     // {
     //     List<Thing> availableVehicles = pawn.Map.listerThings.AllThings.FindAll(
     //         aV => aV is Vehicle_Cart && !((Vehicle_Cart)aV).MountableComp.IsMounted
     //               && aV.Position == pawn.Position
     //               && pawn.CanReserveAndReach(aV, PathEndMode.InteractionCell, Danger.Deadly)
     //               && !((Vehicle_Cart)aV).IsAboutToBlowUp()); // Unmounted
     //     return availableVehicles;
     // }

        public static Vehicle_Cart MountedVehicle(this Pawn pawn)
        {
            return pawn.Map.listerThings.AllThings.FindAll(x => x is Vehicle_Cart && ((Vehicle_Cart)x).MountableComp.Driver == pawn).FirstOrDefault() as Vehicle_Cart;
        }

        public static void MountedVehicle(this Pawn pawn, out Vehicle_Cart cart)
        {
            cart = pawn.Map.listerThings.AllThings.FindAll(x => x is BasicVehicle && ((Vehicle_Cart)x).MountableComp.Driver == pawn).FirstOrDefault() as Vehicle_Cart;
        }

        public static Job DismountAtParkingLot(this Pawn pawn, string caller, Vehicle_Cart cart = null)
        {
            if (cart == null)
            {
                pawn.MountedVehicle(out cart);
            }

            Job job = new Job(VehicleJobDefOf.DismountAtParkingLot) { targetA = cart };

            IntVec3 parkingSpace = IntVec3.Invalid;
            pawn.FindParkingSpace(cart.Position, out parkingSpace);

            job.targetB = parkingSpace;

            if (job.targetB != IntVec3.Invalid)
            {
                Trace.AppendLine("DismountAtParkingLot Job is issued - pawn: " + pawn.LabelShort + " - " + cart.Label + " - " + caller);
                Trace.LogMessage();
                return job;
            }

            JobFailReason.Is(Static.NoEmptyPlaceForCart);
            Trace.AppendLine("No Job. Reason: " + JobFailReason.Reason);
            Trace.LogMessage();
            return null;
        }

        public static bool HasFreeCellsInParkingLot(this Map map, int vehicles = 1)
        {
            int count = 0;
            bool flag = false;
            foreach (IntVec3 cell in map.AllCells)
            {
                if (cell.GetZone(map) is Zone_ParkingLot)
                {
                    if (map.thingGrid.ThingsAt(cell).Any(
                        current => current.def.passability == Traversability.PassThroughOnly
                                   || current.def.passability == Traversability.Impassable || current is Vehicle_Cart))
                    {
                        continue;
                    }

                    count++;

                    if (count > vehicles)
                    {
                        flag = true;
                        break;
                    }
                }
            }

            return flag;
        }

        public static bool FindParkingSpace(this Pawn pawn, IntVec3 searchPos, out IntVec3 parkingSpace)
        {
            List<IntVec3> parkingLot = new List<IntVec3>();
            List<IntVec3> blockers = new List<IntVec3>();

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

                    if (pawn.Map.thingGrid.ThingsAt(cell)
                        .Any(current => current is Vehicle_Cart))
                    {
                        IEnumerable<IntVec3> test = cell.GetThingList(pawn.Map).Find(x => x is Vehicle_Cart).OccupiedRect().Cells;
                        foreach (IntVec3 vec3 in test)
                        {
                            blockers.Add(vec3);
                        }

                        continue;
                    }

                    if (!pawn.CanReserveAndReach(cell, PathEndMode.ClosestTouch, pawn.NormalMaxDanger()))
                        continue;

                    parkingLot.Add(cell);
                }
            }

            foreach (IntVec3 blocker in blockers)
            {
                if (parkingLot.Contains(blocker))
                {
                    parkingLot.Remove(blocker);
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

        public static Job HaulDowneesToBed(Pawn pawn, Pawn pawn2Downee, Thing bedThing)
        {
            Trace.StopWatchStart();
            bool forced = false;

            pawn.AvailableVehicles(out List<Thing> availableVehicles);

            Vehicle_Cart cart = GetRightVehicle(pawn, availableVehicles, WorkTypeDefOf.Hauling);

            // Job Setting
            JobDef jobDef = null;
            LocalTargetInfo targetCart;
            int maxItem;
            int thresholdItem;
            int reservedMaxItem;
            IEnumerable<Thing> remainingItems;
            bool shouldDrop;

            // Thing lastItem = TryGetBackpackLastItem(pawn);
            if (cart.MountableComp.IsMounted)
            {
                jobDef = cart.MountableComp.Driver.RaceProps.Animal
                             ? VehicleJobDefOf.HaulWithAnimalCart
                             : VehicleJobDefOf.HaulWithCart;
            }
            else
            {
                jobDef = VehicleJobDefOf.HaulWithCart;
            }

            Zone zone = pawn.Map.zoneManager.ZoneAt(cart.Position);

            targetCart = cart;
            var storage = cart.GetContainer();

            maxItem = cart.MaxItem;
            thresholdItem = (int)Math.Ceiling(maxItem * 0.25);
            reservedMaxItem = storage.Count;
            remainingItems = storage;

            shouldDrop = reservedMaxItem > 0 ? true : false;

            Job job = new Job(jobDef)
            {
                targetQueueA = new List<LocalTargetInfo>(),
                targetQueueB = new List<LocalTargetInfo>(),
                targetC = targetCart
            };

            Trace.AppendLine(
                pawn.LabelCap + " In HaulWithToolsToCell: " + jobDef.defName + "\n" + "MaxItem: " + maxItem
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
                    Thing t2 = RestUtility.FindBedFor(pawn, pawn2Downee, pawn2Downee.HostFaction == pawn.Faction, false, false); ;

                    if (t2 == null) break;


                    job.targetQueueB.Add(t2);
                }

                if (!job.targetQueueB.NullOrEmpty())
                {
                    return job;
                }

                if (job.def == VehicleJobDefOf.HaulWithCart && !(zone is Zone_ParkingLot))
                {
                    return pawn.DismountAtParkingLot("TFH U Parkin", cart);
                }

                Log.Message("HaulWithToolsToCell Failes" + Static.NoEmptyPlaceLowerTrans);
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
                                                 && pawn.CanReserveAndReach(
                                                     item,
                                                     PathEndMode.Touch,
                                                     pawn.NormalMaxDanger());

            IntVec3 searchPos;
            searchPos = pawn2Downee.Position;


            bool flag1 = false;
            int maxDistance = 99999;

            job.targetQueueA.Add(pawn2Downee);
            job.targetQueueB.Add(bedThing);
            reservedMaxItem++;

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
                if (storageCell == IntVec3.Invalid)
                {
                    break;
                }

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

            if (job.def == VehicleJobDefOf.HaulWithCart && !(zone is Zone_ParkingLot))
            {
                Trace.AppendLine("In DismountAtParkingLot: ");
                return pawn.DismountAtParkingLot("TFHU", cart);
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
                Log.Message("HaulWithToolsToCell NoEmptyPlaceLowerTrans");
                JobFailReason.Is(Static.NoEmptyPlaceLowerTrans);
            }

            Trace.AppendLine("No Job. Reason: " + JobFailReason.Reason);
            Trace.LogMessage();
            return null;
        }

        public static IntVec3 FindStorageCell(Pawn pawn, Thing haulable, List<LocalTargetInfo> targetQueue = null)
        {
            // Find closest cell in queue.
            if (!targetQueue.NullOrEmpty())
            {
                foreach (LocalTargetInfo target in targetQueue)
                {
                    IntVec3 place = IntVec3.Invalid;

                    if (TryFindSpotToPlaceHaulableCloseTo(haulable, pawn, target.Cell, out place))
                    {
                        return place;
                    }
                    continue;
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


            StoragePriority currentPriority = HaulAIUtility.StoragePriorityAtFor(haulable.Position, haulable);
            IntVec3 foundCell;

            if (StoreUtility.TryFindBestBetterStoreCellFor(
                haulable,
                pawn,
                pawn.Map,
                currentPriority,
                pawn.Faction,
                out foundCell,
                true))
            {
                return foundCell;
            }


            // // Vanilla code is not worked item on container.
            // StoragePriority currentPriority = HaulAIUtility.StoragePriorityAtFor(haulable.Position, haulable);
            // foreach (SlotGroup slotGroup in pawn.Map.slotGroupManager.AllGroupsListInPriorityOrder)
            // {
            //     if (slotGroup.Settings.Priority < currentPriority) break;
            //     {
            //         foreach (IntVec3 cell in slotGroup.CellsList)
            //         {
            //             if (!targetQueue.NullOrEmpty() && !targetQueue.Contains(cell) || targetQueue.NullOrEmpty())
            //             {
            //                 if (cell.GetStorable(pawn.Map) == null)
            //                 {
            //                     if (slotGroup.Settings.AllowedToAccept(haulable) && pawn.CanReserveAndReach(cell, PathEndMode.ClosestTouch, Danger.Deadly))
            //                     {
            //                         return cell;
            //                     }
            //                 }
            //             }
            //         }
            //     }
            // }

            return IntVec3.Invalid;
        }

        // Verse.AI.HaulAIUtility

        private static bool TryFindSpotToPlaceHaulableCloseTo(Thing haulable, Pawn worker, IntVec3 center, out IntVec3 spot)
        {
            Region region = center.GetRegion(worker.Map, RegionType.Set_Passable);
            if (region == null)
            {
                spot = center;
                return false;
            }
            TraverseParms traverseParms = TraverseParms.For(worker, Danger.Deadly, TraverseMode.ByPawn, false);
            IntVec3 foundCell = IntVec3.Invalid;
            RegionTraverser.BreadthFirstTraverse(region, (Region from, Region r) => r.Allows(traverseParms, false), delegate (Region r)
                {
                    candidates.Clear();
                    candidates.AddRange(r.Cells);
                    candidates.Sort((IntVec3 a, IntVec3 b) => a.DistanceToSquared(center).CompareTo(b.DistanceToSquared(center)));
                    for (int i = 0; i < candidates.Count; i++)
                    {
                        IntVec3 intVec = candidates[i];
                        if (HaulablePlaceValidator(haulable, worker, intVec))
                        {
                            foundCell = intVec;
                            return true;
                        }
                    }
                    return false;
                }, 100, RegionType.Set_Passable);
            if (foundCell.IsValid)
            {
                spot = foundCell;
                return true;
            }
            spot = center;
            return false;
        }

        // Verse.AI.HaulAIUtility

        private static bool HaulablePlaceValidator(Thing haulable, Pawn worker, IntVec3 c)
        {
            if (!worker.CanReserveAndReach(c, PathEndMode.OnCell, worker.NormalMaxDanger(), 1, -1, null, false))
            {
                return false;
            }
            if (GenPlace.HaulPlaceBlockerIn(haulable, c, worker.Map, true) != null)
            {
                return false;
            }
            if (!c.Standable(worker.Map))
            {
                return false;
            }
            if (c == haulable.Position && haulable.Spawned)
            {
                return false;
            }
            if (c.ContainsStaticFire(worker.Map))
            {
                return false;
            }
            if (haulable != null && haulable.def.BlockPlanting)
            {
                Zone zone = worker.Map.zoneManager.ZoneAt(c);
                if (zone is Zone_Growing)
                {
                    return false;
                }
            }
            if (haulable.def.passability != Traversability.Standable)
            {
                for (int i = 0; i < 8; i++)
                {
                    IntVec3 c2 = c + GenAdj.AdjacentCells[i];
                    if (worker.Map.designationManager.DesignationAt(c2, DesignationDefOf.Mine) != null)
                    {
                        return false;
                    }
                }
            }
            Building edifice = c.GetEdifice(worker.Map);
            if (edifice != null)
            {
                Building_Trap building_Trap = edifice as Building_Trap;
                if (building_Trap != null)
                {
                    return false;
                }
            }
            return true;
        }

        public static Vehicle_Saddle GetSaddleByRider(Pawn pawn)
        {

            List<Thing> availableVehicles = AvailableRideables(pawn);
            foreach (Thing thing in availableVehicles)
            {
                Vehicle_Saddle vehicle = (Vehicle_Saddle)thing;
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

        public static bool IsDriver(this Pawn pawn, Vehicle_Cart cart = null)
        {
            if (cart != null)
            {
                return cart.MountableComp.Driver == pawn;
            }

            List<Thing> mountedVehicles = pawn.Map.MountedVehicles();
            if (!mountedVehicles.NullOrEmpty())
            {
                foreach (Thing thing in mountedVehicles)
                {
                    Vehicle_Cart vehicle = (Vehicle_Cart)thing;
                    if (vehicle.MountableComp.Driver == pawn)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsDriver(this Pawn pawn, out Vehicle_Cart mountedCart, Vehicle_Cart cart = null)
        {
            if (cart != null)
            {
                mountedCart = cart;
                return cart.MountableComp.Driver == pawn;
            }

            List<Thing> mountedVehicles = pawn.Map.MountedVehicles();
            if (!mountedVehicles.NullOrEmpty())
            {
                foreach (Thing thing in mountedVehicles)
                {
                    Vehicle_Cart vehicle = (Vehicle_Cart)thing;
                    if (vehicle.MountableComp.Driver == pawn)
                    {
                        mountedCart = vehicle;
                        return true;
                    }
                }
            }

            mountedCart = null;
            return false;
        }

        public static bool IsDriver(this Pawn pawn)
        {

            List<Thing> mountedVehicles = pawn.Map.MountedVehicles();
            if (!mountedVehicles.NullOrEmpty())
            {
                foreach (Thing thing in mountedVehicles)
                {
                    Vehicle_Cart vehicle = (Vehicle_Cart)thing;
                    if (vehicle.MountableComp.Driver == pawn)
                    {
                        return true;
                    }
                }
            }

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


        public static void DismountGizmoFloatMenu(Pawn pawn)
        {
            if (!pawn.IsDriver(out Vehicle_Cart pawnCart))
            {
                return;

            }


            List<FloatMenuOption> options = new List<FloatMenuOption>();

            Action action_DismountInBase = () =>
                {

                    Job jobNew = pawn.DismountAtParkingLot("DGFM", pawnCart);

                    pawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                };

            Action action_Dismount = () =>
                {
                    if (!pawn.Position.InBounds(pawn.Map))
                    {
                        pawnCart.MountableComp.DismountAt(pawn.Position);
                        return;
                    }

                    pawnCart.MountableComp.DismountAt(
                        pawn.Position - pawnCart.def.interactionCellOffset.RotatedBy(pawn.Rotation));
                    pawn.Position = pawn.Position.RandomAdjacentCell8Way();

                    // mountableComp.DismountAt(myPawn.Position - VehicleDef.interactionCellOffset.RotatedBy(myPawn.Rotation));
                };

            List<Pawn> allPawnsDriving = new List<Pawn>();


            if (Find.Selector.SelectedObjects.Count > 1)
            {
                foreach (object selectedObject in Find.Selector.SelectedObjects)
                {
                    Pawn selPawn = selectedObject as Pawn;
                    if (selPawn != null)
                    {
                        if (selPawn.IsDriver(out Vehicle_Cart drivenCart))
                        {
                            allPawnsDriving.Add(selPawn);
                        }

                    }
                }

                if (allPawnsDriving.Count > 1)
                {
                    foreach (Pawn driverPawn in allPawnsDriving)
                    {
                        driverPawn.MountedVehicle(out Vehicle_Cart pawnCart2);

                        options.Add(
                            new FloatMenuOption(
                                "Dismount".Translate(driverPawn.LabelShort),
                                delegate
                                    {

                                        if (!driverPawn.Position.InBounds(driverPawn.Map))
                                        {
                                            pawnCart2.MountableComp.DismountAt(driverPawn.Position);
                                            return;
                                        }

                                        pawnCart2.MountableComp.DismountAt(
                                                driverPawn.Position
                                                - pawnCart2.def.interactionCellOffset
                                                    .RotatedBy(driverPawn.Rotation));
                                        driverPawn.Position = driverPawn.Position.RandomAdjacentCell8Way();
                                    }));

                        options.Add(new FloatMenuOption("DismountAtParkingLot".Translate(pawnCart2.LabelShort), new Action(
                            delegate
                                {
                                    Job jobNew = driverPawn.DismountAtParkingLot("DGFM-2");

                                    driverPawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                                })));
                    }

                    options.Add(
                        new FloatMenuOption(
                            "DismountAll".Translate(allPawnsDriving.ToString()),
                            delegate
                                {
                                    foreach (Pawn driverPawn in allPawnsDriving)
                                    {
                                        driverPawn.MountedVehicle(out Vehicle_Cart pawnCart3);
                                        if (!driverPawn.Position.InBounds(driverPawn.Map))
                                        {
                                            pawnCart3.MountableComp.DismountAt(driverPawn.Position);
                                            return;
                                        }

                                        pawnCart3.MountableComp
                                            .DismountAt(
                                                driverPawn.Position
                                                - pawnCart3.def.interactionCellOffset
                                                    .RotatedBy(driverPawn.Rotation));
                                        driverPawn.Position = driverPawn.Position.RandomAdjacentCell8Way();
                                    }
                                }));

                    if (pawn.Map.HasFreeCellsInParkingLot(allPawnsDriving.Count))
                    {
                        options.Add(
                            new FloatMenuOption(
                                "DismountAllAtParkingLot".Translate(allPawnsDriving.ToString()),
                                delegate
                                    {
                                        foreach (Pawn driverPawn in allPawnsDriving)
                                        {
                                            Job jobNew = driverPawn.DismountAtParkingLot("DGFM-3");
                                            driverPawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                                        }

                                    }));
                    }
                    else
                    {
                        FloatMenuOption failer =
                            new FloatMenuOption("NoFreeParkingSpaceForCount".Translate(allPawnsDriving.Count), null);
                        options.Add(failer);
                    }
                }
            }
            else
            {
                bool flag = pawnCart.Map.HasFreeCellsInParkingLot();

                options.Add(new FloatMenuOption("Dismount".Translate(pawn.LabelShort), action_Dismount));

                if (flag)
                {
                    options.Add(new FloatMenuOption("DismountAtParkingLot".Translate(pawnCart.LabelShort), action_DismountInBase));
                }
                else
                {
                    FloatMenuOption failer = new FloatMenuOption(
                        "NoFreeParkingSpace".Translate(pawnCart.LabelShort),
                        null);
                    options.Add(failer);
                }
            }

            FloatMenu window = new FloatMenu(options, "WhereToDismount".Translate());
            Find.WindowStack.Add(window);
        }
    }
}