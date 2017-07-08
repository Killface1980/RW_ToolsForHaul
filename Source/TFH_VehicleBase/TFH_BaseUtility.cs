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
            if (!pawn.CanReserveAndReach(cart, PathEndMode.InteractionCell, Danger.Some)) return false;

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

        public static List<Thing> AvailableVehicles(this Pawn pawn)
        {
            List<Thing> availableVehicles = pawn.Map.listerThings.AllThings.FindAll(
                aV => aV is Vehicle_Cart && ((Vehicle_Cart)aV).ClaimableBy(pawn.Faction)
                      && (!((Vehicle_Cart)aV).MountableComp.IsMounted && pawn.CanReserveAndReach(aV, PathEndMode.InteractionCell, Danger.Some) // Unmounted
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
                      && pawn.CanReserveAndReach(aV, PathEndMode.InteractionCell, Danger.Deadly)
                      && aV.Position.InHorDistOf(pawn.Position, distance)
                      && (aV.Faction == pawn.Faction || aV.Faction == null || pawn.Faction.HostileTo(aV.Faction))
                      && ((Vehicle_Cart)aV).ClaimableBy(pawn.Faction)
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

        public static Job DismountAtParkingLot(this Pawn pawn, string caller, Vehicle_Cart cart = null)
        {
            if (cart == null)
            {
                cart = pawn.MountedVehicle();
            }

            Job job = new Job(VehicleJobDefOf.DismountAtParkingLot) { targetA = cart };

            IntVec3 parkingSpace = IntVec3.Invalid;
            pawn.FindParkingSpace(cart.Position, out parkingSpace);

            job.targetB = parkingSpace;

            if (job.targetB != IntVec3.Invalid)
            {
             // Trace.AppendLine("DismountAtParkingLot Job is issued - pawn: " + pawn.LabelShort + " - " + cart.Label + " - " + caller);
             // Trace.LogMessage();
                return job;
            }

            JobFailReason.Is(Static.NoEmptyPlaceForCart);
         // Trace.AppendLine("No Job. Reason: " + JobFailReason.Reason);
         // Trace.LogMessage();
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

        public static bool IsDriver(this Pawn pawn)
        {
            List<Thing> mountedVehicles = pawn.Map.MountedVehicles();
            if (!mountedVehicles.NullOrEmpty())
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