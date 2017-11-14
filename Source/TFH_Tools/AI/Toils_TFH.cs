using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TFH_Tools.JobDrivers
{
    using RimWorld;

    using UnityEngine;

    using Verse;
    using Verse.AI;

    public static class Toils_TFH
    {

        public static Toil CollectInBackpack(TargetIndex HaulableInd)
        {

            Toil toil = new Toil();
            toil.initAction = () =>
                {
                    Pawn actor = toil.actor;
                    Job curJob = actor.jobs.curJob;
                    Thing haulThing = curJob.GetTarget(HaulableInd).Thing;

                    actor.GetInventoryContainer().TryAdd(haulThing.SplitOff(haulThing.stackCount));

                };

            toil.FailOn(() =>
                {
                    Pawn actor = toil.actor;
                    Job curJob = actor.jobs.curJob;
                    Thing haulThing = curJob.GetTarget(HaulableInd).Thing;

                    if (!actor.GetInventoryContainer().CanAcceptAnyOf(haulThing))
                    {
                        return true;
                    }

                    return false;
                });
            return toil;
        }

        public static Toil PutCarriedThingInBackpack()
        {
            Toil toil = new Toil();
            toil.initAction = () =>
                {
                    var actor = toil.GetActor();
                    if (actor.carryTracker.CarriedThing != null)
                    {
                        // Try transfer to inventory
                        if (!actor.carryTracker.innerContainer.TryTransferToContainer(actor.carryTracker.CarriedThing, actor.GetInventoryContainer()))
                        {
                            // Failed: try drop
                            Thing unused;
                            actor.carryTracker.TryDropCarriedThing(actor.Position, actor.carryTracker.CarriedThing.stackCount, ThingPlaceMode.Near, out unused);
                        }
                    }
                };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;

            return toil;
        }

        public static Toil DepositCarriedThingInBackpack(TargetIndex containerInd, TargetIndex reserveForContainerInd)
        {
            Toil toil = new Toil();
            void Action()
            {

                Pawn actor = toil.actor;
                var backpack = actor.jobs.curJob.GetTarget(containerInd).Thing as Apparel_Backpack;

                if (actor.carryTracker.CarriedThing != null)
                {


                    if (actor.carryTracker.CarriedThing.CanStackWith(toil.actor.jobs.curJob.targetA.Thing)
                        && !actor.carryTracker.Full)
                    {

                        if (!actor.carryTracker.innerContainer.TryTransferToContainer(actor.carryTracker.CarriedThing, backpack.slotsComp.innerContainer, true))
                        {
                            Thing thing = default(Thing);
                            actor.carryTracker.TryDropCarriedThing(actor.Position, actor.carryTracker.CarriedThing.stackCount, ThingPlaceMode.Near, out thing, (Action<Thing, int>)null);
                            // actor.TryGetBackpack().toDrop.RemoveLast();
                        }
                    }
                }
            }
            // toil.initAction = action;

            toil.AddFinishAction(Action);

            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = PlaceInInventoryDuration;
            toil.WithProgressBarToilDelay(reserveForContainerInd);
            return toil;
        }
        // Verse.AI.Toils_General

        private const int PlaceInInventoryDuration = 25;

        public static Toil PutThingFromBackpackIntoCarrytracker(TargetIndex containerInd)
        {
            Toil toil = new Toil();

            void Action()
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                var backpack = curJob.GetTarget(containerInd).Thing as Apparel_Backpack;
                if (actor.carryTracker.CarriedThing == null)
                {
                    ThingOwner thingOwner = actor.carryTracker.innerContainer;
                    if (thingOwner != null && backpack.slotsComp.innerContainer.Any)
                    {
                        //  Apparel_Backpack backpack = actor.TryGetBackpack();

                        backpack.slotsComp.innerContainer.TryTransferToContainer(
                            backpack.slotsComp.innerContainer.FirstOrDefault(),
                            thingOwner,
                            true);
                        {
                            //       backpack.toDrop.RemoveAt(0);
                        }
                        // for (int i = backpack.toDrop.Count - 1; i >= 0; i--)
                        // {
                        //     if (!actor.inventory.Contains(backpack.toDrop[i]))
                        //     {
                        //         backpack.toDrop.RemoveAt(i);
                        //     }
                        // }
                    }
                }
                else
                {
                    Log.Error(
                        "Could not deposit hauled thing in carrytracker: " + curJob.GetTarget(containerInd).Thing);
                }
                // else
                // {
                //     Log.Error(
                //         actor + " tried to place contained thing in carrytracker but is already hauling something.");
                // }
            }

            toil.initAction = Action;
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = PlaceInInventoryDuration;
            toil.WithProgressBarToilDelay(containerInd);

            return toil;
        }

        public static Toil DropTheCarriedInCell(TargetIndex StoreCellInd, ThingPlaceMode placeMode)
        {
            Toil toil = new Toil();
            toil.initAction = () =>
                {
                    Pawn actor = toil.actor;
                    Job curJob = actor.jobs.curJob;
                    // ThingOwner<Thing> innerContainer = actor.inventory.innerContainer;
                    ThingOwner<Thing> innerContainer = actor.TryGetBackpack().slotsComp.innerContainer;

                    if (innerContainer.Count <= 0)
                        return;
                    toil.actor.jobs.curJob.SetTarget(TargetIndex.A, innerContainer.First());
                    Thing dropThing = toil.actor.jobs.curJob.targetA.Thing;
                    IntVec3 destLoc = actor.jobs.curJob.GetTarget(StoreCellInd).Cell;
                    Thing dummy;

                    if (destLoc.GetStorable(actor.Map) == null)
                    {
                        actor.Map.designationManager.RemoveAllDesignationsOn(dropThing);
                        innerContainer.TryDrop(dropThing, destLoc, actor.Map, placeMode, out dummy);
                    }

                    //Check cell queue is adjacent
                    List<LocalTargetInfo> cells = curJob.GetTargetQueue(StoreCellInd);
                    for (int i = 0; i < cells.Count && i < innerContainer.Count; i++)
                        if (destLoc.AdjacentTo8Way(cells[i].Cell) && cells[i].Cell.GetStorable(actor.Map) == null)
                        {
                            actor.Map.designationManager.RemoveAllDesignationsOn(innerContainer[i]);
                            innerContainer.TryDrop(innerContainer[i], cells[i].Cell, actor.Map, ThingPlaceMode.Direct, out dummy);
                            cells.RemoveAt(i);
                            i--;
                        }
                    //Check item queue is valid storage for adjacent cell
                    foreach (IntVec3 adjCell in GenAdj.CellsAdjacent8Way(destLoc, Rot4.North, IntVec2.One))
                        if (innerContainer.Count > 0 && adjCell.GetStorable(actor.Map) == null && StoreUtility.IsValidStorageFor(adjCell, actor.Map, innerContainer.First()))
                        {
                            actor.Map.designationManager.RemoveAllDesignationsOn(innerContainer.First());
                            innerContainer.TryDrop(innerContainer.First(), adjCell, actor.Map, ThingPlaceMode.Direct, out dummy);
                        }
                };
            return toil;
        }
    }
}
