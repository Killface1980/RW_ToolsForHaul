#define LOGGING

namespace TFH_VehicleHauling
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    using RimWorld;

    using TFH_VehicleBase;
    using TFH_VehicleBase.DefOfs_TFH;

    using UnityEngine;

    using Verse;
    using Verse.AI;

    public static class TFH_Utility
    {
        private const double ValidDistance = 30;


        private const int NearbyCell = 10;

        public static bool IsPlayerAllowedToRide(this Pawn pawn, Vehicle_Cart cart)
        {
            if (cart.Faction != Faction.OfPlayer)
            {
                return false;
            }

            if (cart.IsForbidden(pawn.Faction))
            {
                return false;
            }

            if (cart.Position.IsForbidden(pawn))
            {
                return false;
            }

            if (cart.IsBurning())
            {
                return false;
            }

            if (!pawn.CanReserveAndReach(cart, PathEndMode.InteractionCell, Danger.Some))
            {
                return false;
            }

            if (!cart.MountableComp.IsMounted)
            {
                return true;
            }

            if (cart.MountableComp.Rider == pawn)
            {
                return true;
            }

            if (cart.MountableComp.IsMounted && cart.MountableComp.Rider.RaceProps.Animal)
            {
                return true;
            }

            return false;
        }




        public static Vehicle_Cart FindWheelChair(Pawn patient, Pawn pawn)
        {
            pawn.AvailableVehicles(out List<Thing> availableVehicles);
            foreach (Thing thing in availableVehicles)
            {
                Vehicle_Cart vehicle = (Vehicle_Cart)thing;
                if (vehicle.VehicleComp.IsMedical() && vehicle.MountableComp.Rider == null
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
    
        public static Job HaulWithCartToCell([NotNull] Pawn pawn, Vehicle_Cart cart, Thing haulThing = null)
        {
            Trace.StopWatchStart();
            bool forced = false;

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
                jobDef = cart.MountableComp.Rider.RaceProps.Animal
                             ? VehicleJobDefOf.HaulWithAnimalCart
                             : VehicleJobDefOf.HaulWithCart;
            }
            else
            {
                jobDef = VehicleJobDefOf.HaulWithCart;
            }

            Zone zone = pawn.Map.zoneManager.ZoneAt(cart.Position);

            targetCart = cart;
            ThingOwner storage = cart.GetDirectlyHeldThings();

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
                    IntVec3 storageCell = TFH_BaseUtility.FindStorageCell(pawn, remainingItems.ElementAt(i), job.targetQueueB);
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
                IntVec3 storageCell = TFH_BaseUtility.FindStorageCell(pawn, closestHaulable, job.targetQueueB);
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
                jobDef = cart.MountableComp.Rider.RaceProps.Animal
                             ? VehicleJobDefOf.HaulWithAnimalCart
                             : VehicleJobDefOf.HaulWithCart;
            }
            else
            {
                jobDef = VehicleJobDefOf.HaulWithCart;
            }

            Zone zone = pawn.Map.zoneManager.ZoneAt(cart.Position);

            targetC = cart;
            ThingOwner storage = cart.GetDirectlyHeldThings();

            maxItem = cart.MaxItem;
            thresholdItem = (int)Math.Ceiling(maxItem * 0.25);
            reservedMaxItem = storage.Count;
            remainingItems = storage;

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
                    IntVec3 storageCell = TFH_BaseUtility.FindStorageCell(pawn, remainingItems.ElementAt(i), job.targetQueueB);
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
                IntVec3 storageCell = TFH_BaseUtility.FindStorageCell(pawn, closestHaulable, job.targetQueueB);
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
                return pawn.DismountAtParkingLot("TFHU-xyz", cart);
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
    }
}