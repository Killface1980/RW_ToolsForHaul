//#define DEBUG
namespace TFH_Tools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    using RimWorld;

    using TFH_Tools.Components;

    using TFH_VehicleBase;
    using TFH_VehicleBase.Components;
    using TFH_VehicleBase.DefOfs_TFH;

    using UnityEngine;

    using Verse;
    using Verse.AI;

    public static class ToolsForHaulUtility
    {
        private const double ValidDistance = 30;

        public static readonly string BurningLowerTrans = "BurningLower".Translate();

        public static readonly string NoAvailableCart = "NoAvailableCart".Translate();

        public static readonly string NoEmptyPlaceForCart = "NoEmptyPlaceForCart".Translate();

        public static readonly string NoEmptyPlaceLowerTrans = "NoEmptyPlaceLower".Translate();

        public static readonly string TooLittleHaulable = "TooLittleHaulable".Translate();

        public static readonly string NoHaulable = "NoHaulable".Translate();

        public static List<Thing> Cart = new List<Thing>();

        public static List<Thing> CartTurret = new List<Thing>();

        public static bool AvailableVehicle(Pawn pawn, ThingWithComps cart)
        {
            if (cart.Faction != Faction.OfPlayer) return false;
            if (cart.IsForbidden(pawn.Faction)) return false;
            if (cart.Position.IsForbidden(pawn)) return false;
            if (cart.IsBurning()) return false;
            if (!pawn.CanReserveAndReach(cart, PathEndMode.ClosestTouch, Danger.Some)) return false;

            if (!cart.TryGetComp<CompMountable>().IsMounted) return true;
            if (cart.TryGetComp<CompMountable>().Rider == pawn) return true;
            if (cart.TryGetComp<CompMountable>().IsMounted && cart.TryGetComp<CompMountable>().Rider.RaceProps.Animal) return true;
            return false;
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

        [CanBeNull]
        public static Job HaulWithTools([NotNull] Pawn pawn, [CanBeNull] Thing haulThing = null)
        {
            Trace.StopWatchStart();
            var map = pawn.Map;
            // Job Setting
            JobDef jobDef  = HaulJobDefOf.HaulWithBackpack;
            //       Thing lastItem = TryGetBackpackLastItem(pawn);
            Apparel_Backpack backpack = TryGetBackpack(pawn);
            List<Thing> remainingItems = backpack.slotsComp.innerContainer.InnerListForReading;
            bool shouldDrop = !remainingItems.NullOrEmpty();

            int maxItem = backpack.MaxItem;
            // thresholdItem = (int)Math.Ceiling(maxItem * 0.5);
            int thresholdItem = 2;

            int reservedMaxItem = 0;


            //   shouldDrop = false;
            // var lastItem = pawn.TryGetBackpackLastItem();
            //
            // if (lastItem != null)
            // {
            //     for (int i = 0; i < backpack.slotsComp.innerContainer.Count; i++)
            //     {
            //         if (backpack.slotsComp.innerContainer[i] == lastItem && reservedMaxItem - (i + 1) <= 0)
            //         {
            //             shouldDrop = false;
            //             break;
            //         }
            //     }
            // }

            if (shouldDrop)
            {
                jobDef = HaulJobDefOf.EmptyBackpack;
            }
            Job job = new Job(jobDef)
            {
                targetQueueA = new List<LocalTargetInfo>(),
                targetQueueB = new List<LocalTargetInfo>(),
                targetC = backpack,
                countQueue = new List<int>(),
                haulOpportunisticDuplicates = true,
                haulMode = HaulMode.ToCellStorage
            };



            Trace.AppendLine(
                pawn.LabelCap + " In HaulWithTools: " + jobDef.defName + "\n" + "MaxItem: " + maxItem
                + " reservedMaxItem: " + reservedMaxItem);

            // Drop remaining item
            if (shouldDrop)
            {
                Trace.AppendLine("Start Drop remaining item");
                //    bool startDrop = false;
                for (int i = 0; i < remainingItems.Count; i++)
                {
                    IntVec3 storageCell =
                        TFH_BaseUtility.FindStorageCell(pawn, remainingItems.ElementAt(i), job.targetQueueB);
                    if (storageCell == IntVec3.Invalid)
                    {
                        break;
                    }
                    Trace.AppendLine("Dropping " + remainingItems.ElementAt(i) + " at: " + storageCell);
                   
                    job.targetQueueA.Add(remainingItems[i]);
                    job.targetQueueB.Add(storageCell);
                    job.countQueue.Add(remainingItems[i].stackCount);
                    job.countQueue.Add(remainingItems[i].stackCount);
                }

                if (!job.targetQueueB.NullOrEmpty())
                {
                    Trace.AppendLine("Dropping Job is issued");
                    Trace.LogMessage();
                    return job;
                }

                JobFailReason.Is(NoEmptyPlaceLowerTrans);
                Trace.AppendLine("End Drop remaining item");
                Trace.AppendLine("No Job. Reason: " + JobFailReason.Reason);
                Trace.LogMessage();
                return null;
            }

            // Collect item
            Trace.AppendLine("Start Collect item");
            IntVec3 searchPos;
            IEnumerable<SlotGroup> slotGroups;
            if (haulThing != null)
            {
                searchPos = haulThing.Position;
                slotGroups = map.slotGroupManager.AllGroupsListInPriorityOrder
                    .Where(slotGroup => slotGroup.Settings.AllowedToAccept(haulThing));
            }
            else
            {
                searchPos = pawn.Position;
                slotGroups = map.slotGroupManager.AllGroupsListInPriorityOrder;
            }

            foreach (SlotGroup slotGroup in slotGroups)
            {
                Trace.AppendLine("Start searching slotGroup " + (slotGroup.CellsList.Count - slotGroup.HeldThings.Count()));
                // Not enough space in store
                if (slotGroup.CellsList.Count - slotGroup.HeldThings.Count() < maxItem)
                {
                    continue;
                }

                // Counting valid items
                Trace.AppendLine("Start Counting valid items");
                List<Thing> needingHauling = map.listerHaulables.ThingsPotentiallyNeedingHauling()
                    .FindAll(item => slotGroup.Settings.AllowedToAccept(item) && HaulAIUtility.PawnCanAutomaticallyHaul(pawn, item,false));

                int thingsCount = needingHauling.Count;

                // Finding valid items
                Trace.AppendLine("Start Finding valid items, valid: " + thingsCount);

                if (thingsCount > thresholdItem)
                {
                    if (haulThing == null)
                    {
                        // ClosestThing_Global_Reachable Configuration
                        Predicate<Thing> predicate =
                            item => !job.targetQueueA.Contains(item) && !item.IsBurning() && !item.IsInAnyStorage()
                                    && item.def.thingCategories.Exists(
                                        category => backpack.slotsComp.Properties.allowedThingCategoryDefs.Exists(
                                                        subCategory => subCategory.ThisAndChildCategoryDefs.Contains(
                                                            category)) && !backpack.slotsComp.Properties
                                                        .forbiddenSubThingCategoryDefs
                                                        .Exists(
                                                            subCategory => subCategory.ThisAndChildCategoryDefs
                                                                .Contains(category)))
                                    && slotGroup.Settings.AllowedToAccept(item)
                                    && HaulAIUtility.PawnCanAutomaticallyHaul(pawn, item, false);

                        // && !(item is UnfinishedThing && ((UnfinishedThing)item).BoundBill != null)
                        // && (item.def.IsNutritionSource && !SocialProperness.IsSociallyProper(item, pawn, false, false));
                        haulThing = GenClosest.ClosestThing_Global_Reachable(
                            searchPos,
                            map,
                            needingHauling,
                            PathEndMode.ClosestTouch,
                            TraverseParms.For(pawn, pawn.NormalMaxDanger()),
                            9999,
                            predicate);
                        if (haulThing == null)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        job.targetQueueA.Add(haulThing);
                        job.countQueue.Add(haulThing.def.stackLimit);
                    }

                    IntVec3 center = haulThing.Position;

                    // Enqueue other items in valid distance
                    Trace.AppendLine("Start Enqueuing other items in valid distance");
                    if (reservedMaxItem + job.targetQueueA.Count < maxItem)
                    {
                        Trace.AppendLine("Start Enqueuing items in valid distance :" + reservedMaxItem + " / " + job.targetQueueA.Count + " / " + maxItem);

                        foreach (Thing item in needingHauling.Where(
                            item => !job.targetQueueA.Contains(item)
                                    && item.def.thingCategories.Exists(
                                        category => backpack.slotsComp.Properties.allowedThingCategoryDefs.Exists(
                                                        subCategory => subCategory.ThisAndChildCategoryDefs.Contains(
                                                            category)) && !backpack.slotsComp.Properties
                                                        .forbiddenSubThingCategoryDefs
                                                        .Exists(
                                                            subCategory => subCategory.ThisAndChildCategoryDefs
                                                                .Contains(category)))
                                    && center.DistanceToSquared(item.Position) <= ValidDistance).OrderBy(x=>x.Position.DistanceTo(haulThing.Position)))
                        {
                            job.targetQueueA.Add(item);
                            job.countQueue.Add(item.def.stackLimit);
                            Trace.AppendLine("Added " + item + ", jobqueue: " + job.targetQueueA.Count + ", maxItem " + reservedMaxItem);

                            if (reservedMaxItem + job.targetQueueA.Count >= maxItem +1)
                            {
                                Trace.AppendLine("Need a break " + reservedMaxItem + " >= " + maxItem);
                                break;
                            }
                        }
                    }

                    // Find storage cell
                    Trace.AppendLine("Start Finding storage cell");

                    if (reservedMaxItem + job.targetQueueA.Count > thresholdItem)
                    {
                        for (int i = 0; i < job.targetQueueA.Count; i++)
                        {
                            IntVec3 storageCell =
                                TFH_BaseUtility.FindStorageCell(pawn, job.targetQueueA[i].Thing, job.targetQueueB);
                            if (storageCell == IntVec3.Invalid)
                            {
                                Trace.AppendLine("Invalid storage cell");
                                break;
                            }
                            {
                                Trace.AppendLine("Adding storage cell: " + storageCell);
                                job.targetQueueB.Add(storageCell);
                                job.countQueue.Add(job.targetQueueA[i].Thing.def.stackLimit);
                              //  job.countQueue.Add(job.targetQueueA[i].Thing.stackCount);
                            }
                        }

                        // foreach (IntVec3 cell in slotGroup.CellsList.Where(
                        //     cell => pawn.CanReserve(cell) && cell.Standable(pawn.Map)
                        //             && cell.GetStorable(pawn.Map) == null))
                        // {
                        //
                        //     job.targetQueueB.Add(cell);
                        //     if (job.targetQueueB.Count >= job.targetQueueA.Count)
                        //     {
                        //         break;
                        //     }
                        // }
                        // break;
                    }
                    else
                    {
                        job.targetQueueA.Clear();
                    }
                }
            }

            Trace.AppendLine("Elapsed Time");
            Trace.StopWatchStop();
            Trace.AppendLine("JobQueue count: " + job.targetQueueA.Count + " - " + job.targetQueueB.Count);

            // Check job is valid
            if (!job.targetQueueA.NullOrEmpty() && reservedMaxItem + job.targetQueueA.Count > thresholdItem
                && !job.targetQueueB.NullOrEmpty())
            {
                Trace.AppendLine("Hauling Job is issued");
                string shit = reservedMaxItem.ToString();
                for (int i = 0; i < job.targetQueueA.Count; i++)
                {
                    shit += "\n" + job.targetQueueA[i].Thing + " - " + job.targetQueueB[i].Cell;
                }
                Trace.AppendLine(shit);
                Trace.LogMessage();


                return job;
            }

            if (job.targetQueueA.NullOrEmpty())
            {
                JobFailReason.Is(NoHaulable);
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
            foreach (Vehicle_Cart vehicle in Cart) if (vehicle.MountableComp.Rider == pawn) return true;
            // foreach (Vehicle_Turret vehicle in CartTurret) if (vehicle.mountableComp.Rider == pawn) return true;
            return false;
        }

        public static bool IsDriverOfThisVehicle(Pawn pawn, Thing vehicleReq)
        {
            foreach (Vehicle_Cart vehicle in Cart) if (vehicle.MountableComp.Rider == pawn && vehicle == vehicleReq) return true;
            //  foreach (Vehicle_Turret vehicleTurret in CartTurret) if (vehicleTurret.mountableComp.Rider == pawn && vehicleTurret == vehicleReq) return true;
            return false;
        }

        [CanBeNull]
        public static ThingOwner<Thing> GetInventoryContainer([NotNull] this Pawn pawn)
        {
            return pawn.inventory.innerContainer;
            if (!pawn.RaceProps.Humanlike)
            {
                return null;
            }

            return null;
        }

        [CanBeNull]
        public static Apparel_Backpack TryGetBackpack([NotNull] this Pawn pawn)
        {
            if (!pawn.RaceProps.Humanlike)
            {
                return null;
            }

            foreach (Apparel apparel in pawn.apparel.WornApparel)
            {
                if (apparel is Apparel_Backpack)
                {
                    return apparel as Apparel_Backpack;
                }
            }
            return null;
        }

        [CanBeNull]
        public static Thing TryGetBackpackLastItem(this Pawn pawn)
        {
            Apparel_Backpack backpack = TryGetBackpack(pawn);
            if (backpack == null) return null;

            Thing lastItem = null;
            int lastItemInd = -1;
            Thing foodInInventory = FoodUtility.BestFoodInInventory(pawn);

            //   ThingOwner<Thing> innerContainer = backpack.slotsComp.innerContainer;
            ThingOwner<Thing> innerContainer = pawn.inventory.innerContainer;

            if (innerContainer.Count > 0)
            {
                if (backpack.numOfSavedItems > 0)
                {
                    lastItemInd = (backpack.numOfSavedItems >= backpack.MaxItem
                                       ? innerContainer.Count
                                       : backpack.numOfSavedItems) - 1;
                    lastItem = innerContainer[lastItemInd];
                }

                if (foodInInventory != null && backpack.numOfSavedItems < innerContainer.Count
                    && innerContainer[lastItemInd + 1] == foodInInventory)
                {
                    lastItem = foodInInventory;
                }
            }

            return lastItem;
        }

        public static Apparel_ToolBelt TryGetToolbelt(Pawn pawn)
        {
            if (!pawn.RaceProps.Humanlike)
            {
                return null;
            }

            for (int i = 0; i < pawn.apparel.WornApparel.Count; i++)
            {
                Apparel apparel = pawn.apparel.WornApparel[i];
                if (apparel is Apparel_ToolBelt)
                {
                    return apparel as Apparel_ToolBelt;
                }
            }
            return null;
        }
    }
}