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

        public static bool IsAllowedToRide(this Pawn pawn, Vehicle_Cart cart)
        {
            if (cart.Faction != Faction.OfPlayer) return false;
            if (cart.IsForbidden(pawn.Faction)) return false;
            if (cart.Position.IsForbidden(pawn)) return false;
            if (cart.IsBurning()) return false;
            if (!pawn.CanReserveAndReach(cart, PathEndMode.ClosestTouch, Danger.Some)) return false;

            if (!cart.MountableComp.IsMounted) return true;
            if (cart.MountableComp.Driver == pawn) return true;
            if (cart.MountableComp.IsMounted && cart.MountableComp.Driver.RaceProps.Animal) return true;
            return false;
        }



        public static Job DismountAtParkingLot(this Pawn pawn, string caller, Vehicle_Cart cart = null)
        {
            if (cart == null)
            {
                cart = pawn.MountedVehicle();
            }

            Job job = new Job(HaulJobDefOf.DismountAtParkingLot) { targetA = cart };

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
            foreach (IntVec3 cell in pawn.Map.AllCells)
            {
                Zone zone = pawn.Map.zoneManager.ZoneAt(cell);
                if (zone is Zone_ParkingLot)
                {
                    if (pawn.Map.thingGrid.ThingsAt(cell)
                        .Any(
                            current => current.def.passability == Traversability.PassThroughOnly
                                       || current.def.passability == Traversability.Impassable || current is Vehicle_Cart))
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
        private static List<IntVec3> candidates = new List<IntVec3>();
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
                aV => aV is Vehicle_Cart && !aV.IsForbidden(pawn.Faction)
                      && (!((Vehicle_Cart)aV).MountableComp.IsMounted && pawn.CanReserve(aV) // Unmounted
                          || ((Vehicle_Cart)aV).MountableComp.Driver == pawn)); // or Driver is pawn himself
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
                      && pawn.CanReserveAndReach(aV, PathEndMode.InteractionCell, Danger.Deadly)
                      && aV.Position.InHorDistOf(pawn.Position, distance)
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
                      && pawn.CanReserveAndReach(aV, PathEndMode.InteractionCell, Danger.Deadly)
                      && aV.Position.InHorDistOf(pawn.Position, distance)
                      && (aV.Faction == pawn.Faction || aV.Faction == null || pawn.Faction.HostileTo(aV.Faction))
                      && !((Vehicle_Cart)aV).IsAboutToBlowUp()); // Unmounted
            if (!availableVehicles.NullOrEmpty())
            {
                availableVehicles.OrderBy(x => x.Position.DistanceTo(pawn.Position));
            }
            return availableVehicles;
        }

        public static List<Thing> AvailableVehiclesForPawnFaction(this Pawn pawn, float distance)
        {
            List<Thing> availableVehicles = pawn.Map.listerThings.AllThings.FindAll(
                aV => aV is Vehicle_Cart && !((Vehicle_Cart)aV).MountableComp.IsMounted
                      && pawn.CanReserveAndReach(aV, PathEndMode.InteractionCell, Danger.Some)
                      && aV.Position.InHorDistOf(pawn.Position, distance)
                      && aV.Faction == pawn.Faction
                      && !((Vehicle_Cart)aV).IsAboutToBlowUp()); // Unmounted
            if (!availableVehicles.NullOrEmpty())
            {
                availableVehicles.OrderBy(x => x.Position.DistanceTo(pawn.Position));
            }
            return availableVehicles;
        }

        public static List<Thing> AvailableVehicleAt(this Pawn pawn)
        {
            List<Thing> availableVehicles = pawn.Map.listerThings.AllThings.FindAll(
                aV => aV is Vehicle_Cart && !((Vehicle_Cart)aV).MountableComp.IsMounted
                      && aV.Position == pawn.Position
                      && pawn.CanReserveAndReach(aV, PathEndMode.InteractionCell, Danger.Deadly)
                      && !((Vehicle_Cart)aV).IsAboutToBlowUp()); // Unmounted
            return availableVehicles;
        }

        public static Vehicle_Cart MountedVehicle(this Pawn pawn)
        {

            List<Thing> availableVehicles = pawn.AvailableVehicles();
            return availableVehicles.Cast<Vehicle_Cart>().FirstOrDefault(vehicle => vehicle.MountableComp.Driver == pawn);
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

        public static Job HaulWithToolsToCell(Pawn pawn, Vehicle_Cart cart, Thing haulThing = null)
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

            if (job.def == HaulJobDefOf.HaulWithCart && !(zone is Zone_ParkingLot))
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

        public static Job HaulWithToolsToContainer(Pawn pawn, Vehicle_Cart cart, Job jobHaul, Thing haulThing = null)
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

            if (job.def == HaulJobDefOf.HaulWithCart && !(zone is Zone_ParkingLot))
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

        public static bool IsDriver(this Pawn pawn)
        {
            List<Thing> mountedVehicles = pawn.Map.MountedVehicles();
            foreach (var thing in mountedVehicles)
            {
                var vehicle = (Vehicle_Cart)thing;
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
                IOrderedEnumerable<Thing> armoured =
                    availableVehicles.Where(x => x is Vehicle_Cart).OrderBy(x => ((Vehicle_Cart)x).HitPoints);

                IOrderedEnumerable<Thing> orderedEnumerable =
                    availableVehicles.OrderBy(x => ((Vehicle_Cart)x).HitPoints);

                foreach (Thing thing in armoured)
                {
                    Vehicle_Cart vehicleCart = (Vehicle_Cart)thing;
                    if (vehicleCart == null) continue;
                    if (!(vehicleCart.Position.GetZone(vehicleCart.Map) is Zone_ParkingLot)) continue;
                    if (!pawn.IsAllowedToRide(vehicleCart)) continue;
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
                    if (!pawn.IsAllowedToRide(vehicleCart)) continue;
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

                    if (!pawn.IsAllowedToRide(vehicleCart)) continue;
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
                    if (!pawn.IsAllowedToRide(vehicleCart)) continue;
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
                    if (!pawn.IsAllowedToRide(vehicleCart)) continue;
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



        public static void DismountGizmoFloatMenu(Pawn pawn)
        {

            var pawnCart = pawn.MountedVehicle();

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

            var allPawnsDriving = new List<Pawn>();


            if (Find.Selector.SelectedObjects.Count > 1)
            {
                foreach (var selectedObject in Find.Selector.SelectedObjects)
                {
                    Pawn selPawn = selectedObject as Pawn;
                    if (selPawn != null)
                    {
                        if (selPawn.IsDriver())
                        {
                            allPawnsDriving.Add(selPawn);
                        }

                    }
                }
                if (allPawnsDriving.Count > 1)
                {
                    foreach (Pawn driverPawn in allPawnsDriving)
                    {
                        options.Add(
                            new FloatMenuOption(
                                "Dismount".Translate(driverPawn.LabelShort),
                                delegate
                                    {
                                        if (!driverPawn.Position.InBounds(driverPawn.Map))
                                        {
                                            driverPawn.MountedVehicle().MountableComp.DismountAt(driverPawn.Position);
                                            return;
                                        }

                                        driverPawn.MountedVehicle().MountableComp
                                            .DismountAt(
                                                driverPawn.Position
                                                - driverPawn.MountedVehicle().def.interactionCellOffset
                                                    .RotatedBy(driverPawn.Rotation));
                                        driverPawn.Position = driverPawn.Position.RandomAdjacentCell8Way();
                                    }));

                        options.Add(new FloatMenuOption("DismountAtParkingLot".Translate(driverPawn.MountedVehicle().LabelShort), new Action(
                            delegate
                                {
                                    Job jobNew = driverPawn.DismountAtParkingLot("DGFM");

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
                                        if (!driverPawn.Position.InBounds(driverPawn.Map))
                                        {
                                            driverPawn.MountedVehicle().MountableComp.DismountAt(driverPawn.Position);
                                            return;
                                        }

                                        driverPawn.MountedVehicle().MountableComp
                                            .DismountAt(
                                                driverPawn.Position
                                                - driverPawn.MountedVehicle().def.interactionCellOffset
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
                                            Job jobNew = driverPawn.DismountAtParkingLot("DGFM");
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