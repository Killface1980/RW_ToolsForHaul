namespace TFH_VehicleHauling.Toils
{
    using System.Collections.Generic;
    using System.Linq;

    using RimWorld;

    using TFH_VehicleBase;

    using Verse;
    using Verse.AI;

    public static class Toils_Collect
    {
        private const int NearbyCell = 30;

        public static Toil Extract(TargetIndex ind)
        {
            Toil toil = new Toil();
            toil.initAction = () =>
                {
                    List<LocalTargetInfo> targetQueue = toil.actor.jobs.curJob.GetTargetQueue(ind);
                    if (!targetQueue.NullOrEmpty())
                    {
                        toil.actor.jobs.curJob.SetTarget(ind, targetQueue.First());
                        targetQueue.RemoveAt(0);
                    }
                };
            return toil;
        }

        #region Toil Collect

        public static Toil CheckDuplicates(Toil jumpToil, TargetIndex CarrierInd, TargetIndex HaulableInd)
        {
            Toil toil = new Toil();
            toil.initAction = () =>
                {
                    IntVec3 storeCell = IntVec3.Invalid;
                    Pawn actor = toil.GetActor();

                    LocalTargetInfo target = toil.actor.jobs.curJob.GetTarget(HaulableInd);
                    if (target.Thing.def.stackLimit <= 1) return;
                    List<LocalTargetInfo> targetQueue = toil.actor.jobs.curJob.GetTargetQueue(HaulableInd);
                    if (!targetQueue.NullOrEmpty() && target.Thing.def.defName == targetQueue.First().Thing.def.defName)
                    {
                        toil.actor.jobs.curJob.SetTarget(HaulableInd, targetQueue.First());
                        actor.Map.reservationManager.Reserve(actor, targetQueue.First());
                        targetQueue.RemoveAt(0);
                        toil.actor.jobs.curDriver.JumpToToil(jumpToil);
                        return;
                    }

                    Vehicle_Cart cart = toil.actor.jobs.curJob.GetTarget(CarrierInd).Thing as Vehicle_Cart;

                    if (cart == null)
                    {
                        Log.Error(actor.LabelCap + " Report: Don't have Carrier");
                        toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                        return;
                    }
                    var storage = cart.GetContainer();

                    int curItemCount = storage.Count
                                       + targetQueue.Count;
                    int curItemStack = storage.TotalStackCount
                                       + targetQueue.Sum(item => item.Thing.stackCount);
                    int maxItem = cart.MaxItem;
                    int maxStack = cart.MaxStack;
                    if (curItemCount >= maxItem || curItemStack >= maxStack) return;

                    // Check target's nearby
                    Thing thing = GenClosest.ClosestThing_Global_Reachable(
                        actor.Position,
                        actor.Map,
                        actor.Map.listerHaulables.ThingsPotentiallyNeedingHauling(),
                        PathEndMode.Touch,
                        TraverseParms.For(actor, Danger.Some),
                        NearbyCell,
                        item =>
                            !targetQueue.Contains(item) && item.def.defName == target.Thing.def.defName
                            && !item.IsBurning() && actor.Map.reservationManager.CanReserve(actor, item));
                    if (thing != null)
                    {
                        toil.actor.jobs.curJob.SetTarget(HaulableInd, thing);
                        actor.Map.reservationManager.Reserve(actor, thing);
                        toil.actor.jobs.curDriver.JumpToToil(jumpToil);
                    }
                };
            return toil;
        }

        // OLD
        /* public static Toil CollectInInventory(TargetIndex HaulableInd)
         {
 
             Toil toil = new Toil();
             toil.initAction = () =>
             {
                 Pawn actor = toil.actor;
                 Job curJob = actor.jobs.curJob;
                 Thing haulThing = curJob.GetTarget(HaulableInd).Thing;
 
                 //Check haulThing is human_corpse. If other race has apparel, It need to change
                 if (haulThing.ThingID.IndexOf("Human_Corpse") <= -1 ? false : true)
                 {
                     Corpse corpse = (Corpse)haulThing;
                     List<Apparel> wornApparel = corpse.innerPawn.apparel.WornApparel;
 
                     //Drop wornApparel. wornApparel cannot Add to container directly because it will be duplicated.
                     corpse.innerPawn.apparel.DropAll(corpse.innerPawn.Position, false);
 
                     //Transfer in container
                     foreach (Thing apparel in wornApparel)
                     {
                         if (actor.inventory.innerContainer.TryAdd(apparel))
                         {
                             apparel.holdingContainer = actor.inventory.GetContainer();
                             apparel.holdingContainer.owner = actor.inventory;
                         }
                     }
                 }
                 //Collecting TargetIndex ind
                 if (actor.inventory.innerContainer.TryAdd(haulThing))
                 {
                     haulThing.holdingContainer = actor.inventory.GetContainer();
                     haulThing.holdingContainer.owner = actor.inventory;
                 }
 
             };
             toil.FailOn(() =>
             {
                 Pawn actor = toil.actor;
                 Job curJob = actor.jobs.curJob;
                 Thing haulThing = curJob.GetTarget(HaulableInd).Thing;
 
                 if (!actor.inventory.innerContainer.CanAcceptAnyOf(haulThing))
                     return true;
 
 
 
                 return false;
             });
             return toil;
         }
         */
        public static Toil CollectInCarrier(TargetIndex CarrierInd, TargetIndex HaulableInd)
        {
            Toil toil = new Toil();
            toil.initAction = () =>
                {
                    Pawn actor = toil.actor;
                    Job curJob = actor.jobs.curJob;
                    Thing haulThing = curJob.GetTarget(HaulableInd).Thing;
                    Vehicle_Cart carrier = curJob.GetTarget(CarrierInd).Thing as Vehicle_Cart;

                    // ThingOwner thingOwner = haulThing.TryGetInnerInteractableThingOwner();


                    // Check haulThing is human_corpse. If other race has apparel, It need to change
                    actor.Map.designationManager.RemoveAllDesignationsOn(haulThing);

                    // if (haulThing.ThingID.IndexOf("Human_Corpse") <= -1 ? false : true)
                    // {
                    // Corpse corpse = (Corpse)haulThing;
                    // List<Apparel> wornApparel = corpse.innerPawn.apparel.WornApparel;
                    // //Drop wornApparel. wornApparel cannot Add to container directly because it will be duplicated.
                    // corpse.innerPawn.apparel.DropAll(corpse.innerPawn.Position, false);
                    // //Transfer in container
                    // foreach (Thing apparel in wornApparel)
                    // {
                    // if (carrier.storage.TryAdd(apparel))
                    // {
                    // apparel.holdingContainer = carrier.GetContainer();
                    // apparel.holdingContainer.owner = carrier;
                    // }
                    {
                        // }
                        // }
                        // Collecting TargetIndex ind
                        // if
                        // haulThing.DeSpawn();
                        var storage = carrier.GetContainer();

                        haulThing.holdingOwner.TryTransferToContainer(haulThing, storage, haulThing.stackCount);// carrier.innerContainer.TryAdd(haulThing);
                    }
                    {
                        // haulThing.holdingOwner = carrier.innerContainer;
                    }

                    List<LocalTargetInfo> thingList = curJob.GetTargetQueue(HaulableInd);
                    for (int i = 0; i < thingList.Count; i++)
                        if (actor.Position.AdjacentTo8Way(thingList[i].Thing.Position))
                        {
                            // thingList[i].Thing.DeSpawn();
                            var storage = carrier.GetContainer();
                            thingList[i].Thing.holdingOwner
                                .TryTransferToContainer(thingList[i].Thing, storage, thingList[i].Thing.stackCount);
                            {
                                // carrier.innerContainer.TryAdd(thingList[i].Thing);
                                // thingList[i].Thing.holdingOwner = carrier.innerContainer;
                            }

                            thingList.RemoveAt(i);
                            i--;
                        }
                };
            toil.FailOn(
                () =>
                    {
                        Pawn actor = toil.actor;
                        Job curJob = actor.jobs.curJob;
                        Thing haulThing = curJob.GetTarget(HaulableInd).Thing;
                        Vehicle_Cart carrier = curJob.GetTarget(CarrierInd).Thing as Vehicle_Cart;

                        if (!carrier.allowances.Allows(haulThing)
                            && actor.Position.InHorDistOf(
                                haulThing.Position, 1f))
                            return true;
                        return false;
                    });
            toil.FailOnDestroyedOrNull(CarrierInd);
            return toil;
        }

        #endregion

        #region Toil Drop

        public static Toil CheckNeedStorageCell(Toil jumpToil, TargetIndex CarrierInd, TargetIndex StoreCellInd)
        {
            Toil toil = new Toil();
            toil.initAction = () =>
                {
                    Pawn actor = toil.actor;

                    Vehicle_Cart cart = toil.actor.jobs.curJob.GetTarget(CarrierInd).Thing as Vehicle_Cart;
                    if (cart == null)
                    {
                        Log.Error(actor.LabelCap + " Report: Don't have Carrier");
                        toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                    }
                    var storage = cart.GetContainer();

                    if (storage.Count == 0)
                    {
                        return;
                    }

                    IntVec3 cell = TFH_BaseUtility.FindStorageCell(actor, storage.First());
                    if (cell != IntVec3.Invalid)
                    {
                        toil.actor.jobs.curJob.SetTarget(StoreCellInd, cell);
                        actor.Map.reservationManager.Reserve(actor, cell);
                        toil.actor.jobs.curDriver.JumpToToil(jumpToil);
                    }
                };
            return toil;
        }

        // OLD
        /*
                public static Toil DropTheCarriedInCell(TargetIndex StoreCellInd, ThingPlaceMode placeMode)
                {
                    Toil toil = new Toil();
                    toil.initAction = () =>
                    {
                        Pawn actor = toil.actor;
                        Job curJob = actor.jobs.curJob;
                        if (actor.inventory.innerContainer.Count <= 0)
                        {
                            return;
                        }
                        toil.actor.jobs.curJob.SetTarget(TargetIndex.A, actor.inventory.innerContainer.First());
                        Thing dropThing = toil.actor.jobs.curJob.targetA.Thing;
                        IntVec3 destLoc = actor.jobs.curJob.GetTarget(StoreCellInd).Cell;
                        Thing dummy;
        
                        SlotGroup slotGroup = actor.Map.slotGroupManager.SlotGroupAt(destLoc);
                        //    if (destLoc.GetStorable() == null)
                        if (slotGroup != null && slotGroup.Settings.AllowedToAccept(dropThing))
                        {
                            actor.Map.designationManager.RemoveAllDesignationsOn(dropThing);
                            actor.inventory.innerContainer.TryDrop(dropThing, destLoc, placeMode, out dummy);
                        }
        
                        //Check cell queue is adjacent
                        List<TargetInfo> cells = curJob.GetTargetQueue(StoreCellInd);
                        for (int i = 0; i < cells.Count && i < actor.inventory.innerContainer.Count; i++)
                            if (destLoc.AdjacentTo8Way(cells[i].Cell) && cells[i].Cell.GetStorable() == null)
                            {
                                actor.Map.designationManager.RemoveAllDesignationsOn(actor.inventory.innerContainer[i]);
                                actor.inventory.innerContainer.TryDrop(actor.inventory.innerContainer[i], cells[i].Cell, ThingPlaceMode.Direct, out dummy);
                                cells.RemoveAt(i);
                                i--;
                            }
                        //Check item queue is valid storage for adjacent cell
                        foreach (IntVec3 adjCell in GenAdj.CellsAdjacent8Way(destLoc))
                            if (actor.inventory.innerContainer.Count > 0 && adjCell.GetStorable() == null && adjCell.IsValidStorageFor(actor.inventory.innerContainer.First()))
                            {
                                actor.Map.designationManager.RemoveAllDesignationsOn(actor.inventory.innerContainer.First());
                                actor.inventory.innerContainer.TryDrop(actor.inventory.innerContainer.First(), adjCell, ThingPlaceMode.Direct, out dummy);
                            }
                    };
                    return toil;
                }
        
                public static Toil DropTheCarriedInCell(TargetIndex StoreCellInd, ThingPlaceMode placeMode, Thing lastItem)
                {
                    Toil toil = new Toil();
                    toil.initAction = () =>
                    {
                        Pawn actor = toil.actor;
                        Job curJob = actor.jobs.curJob;
                        if (actor.inventory.innerContainer.Count <= 0)
                            return;
        
                        //Check dropThing is last item that should not be dropped
                        Thing dropThing = null;
                        if (lastItem != null)
                            for (int i = 0; i + 1 < actor.inventory.innerContainer.Count; i++)
                                if (actor.inventory.innerContainer[i] == lastItem)
                                    dropThing = actor.inventory.innerContainer[i + 1];
                                else if (lastItem == null && actor.inventory.innerContainer.Count > 0)
                                    dropThing = actor.inventory.innerContainer.First();
        
                        if (dropThing == null)
                        {
                            Log.Error(toil.actor + " tried to drop null thing in " + actor.jobs.curJob.GetTarget(StoreCellInd).Cell);
                            return;
                        }
                        IntVec3 destLoc = actor.jobs.curJob.GetTarget(StoreCellInd).Cell;
                        Thing dummy;
        
                        if (destLoc.GetStorable() == null)
                        {
                            actor.Map.designationManager.RemoveAllDesignationsOn(dropThing);
                            actor.inventory.innerContainer.TryDrop(dropThing, destLoc, placeMode, out dummy);
                        }
                    };
                    return toil;
                }
        */
        public static Toil DropTheCarriedInCell(
            TargetIndex StoreCellInd,
            ThingPlaceMode placeMode,
            TargetIndex CarrierInd)
        {
            Toil toil = new Toil();
            toil.initAction = () =>
                {
                    Pawn actor = toil.actor;
                    Job curJob = actor.jobs.curJob;
                    Vehicle_Cart carrier = actor.jobs.curJob.GetTarget(CarrierInd).Thing as Vehicle_Cart;
                    ThingOwner cartStorage = carrier.GetContainer();
                    if (cartStorage.Count <= 0)
                    {
                        return;
                    }

                    toil.actor.jobs.curJob.SetTarget(TargetIndex.A, cartStorage.First());
                    Thing dropThing = toil.actor.jobs.curJob.targetA.Thing;
                    IntVec3 destLoc = actor.jobs.curJob.GetTarget(StoreCellInd).Cell;
                    Thing dummy;

                    // Possible fix for container
                    ThingOwner bedContainer = actor.jobs.curJob.GetTarget(StoreCellInd).Thing.TryGetInnerInteractableThingOwner();
                    if (bedContainer != null)
                    {
                        actor.Map.designationManager.RemoveAllDesignationsOn(dropThing);
                        cartStorage.TryTransferToContainer(dropThing, bedContainer);
                    }
                    else if (destLoc.GetStorable(actor.Map) == null)
                    {
                        actor.Map.designationManager.RemoveAllDesignationsOn(dropThing);
                        cartStorage.TryDrop(dropThing, destLoc, actor.Map, placeMode, out dummy);
                    }


                    // Check cell queue is adjacent
                    List<LocalTargetInfo> cells = curJob.GetTargetQueue(StoreCellInd);
                    for (int i = 0; i < cells.Count && i < cartStorage.Count; i++)
                    {
                        if (destLoc.AdjacentTo8Way(cells[i].Cell) && cells[i].Cell.GetStorable(actor.Map) == null)
                        {
                            IntVec3 b;
                            if (cells[i].HasThing)
                            {
                                b = cells[i].Thing.OccupiedRect().ClosestCellTo(actor.Position);
                            }
                            else
                            {
                                b = cells[i].Cell;
                            }
                            IntVec3 dropLoc = actor.Position + PawnRotator.RotFromAngleBiased((actor.Position - b).AngleFlat).FacingCell;

                            // if (actor.CanReserve(dropLoc, 1, -1, null, false))
                            // {
                            //     actor.Reserve(dropLoc, 1, -1, null);
                            // }

                            actor.Map.designationManager.RemoveAllDesignationsOn(cartStorage[i]);
                            cartStorage.TryDrop(cartStorage[i], dropLoc, actor.Map, ThingPlaceMode.Direct, out dummy);
                            cells.RemoveAt(i);
                            i--;
                        }
                    }

                    // Check item queue is valid storage for adjacent cell
                    foreach (IntVec3 adjCell in GenAdj.CellsAdjacent8Way(destLoc, Rot4.Random, new IntVec2()))
                    {
                        if (cartStorage.Count > 0 && adjCell.GetStorable(actor.Map) == null
                            && adjCell.IsValidStorageFor(actor.Map, cartStorage.First()))
                        {
                            actor.Map.designationManager.RemoveAllDesignationsOn(cartStorage.First());
                            // if (actor.CanReserve(adjCell, 1, -1, null, false))
                            // {
                            //     actor.Reserve(adjCell, 1, -1, null);
                            // }

                            cartStorage.TryDrop(cartStorage.First(), adjCell, actor.Map, ThingPlaceMode.Direct, out dummy);
                        }
                    }
                };
            toil.FailOnDestroyedOrNull(CarrierInd);
            return toil;
        }

        // OLD
        /*
        public static Toil DropAllInCell(TargetIndex StoreCellInd, ThingPlaceMode placeMode)
        {
            Toil toil = new Toil();
            toil.initAction = () =>
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                IntVec3 destLoc = actor.jobs.curJob.GetTarget(StoreCellInd).Cell;

                actor.inventory.innerContainer.TryDropAll(destLoc, placeMode);
            };
            return toil;
        }
        */
        #endregion
    }
}

