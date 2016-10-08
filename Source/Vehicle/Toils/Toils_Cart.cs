﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public static class Toils_Cart
    {
        public static Toil MountOn(TargetIndex CartInd)
        {
            Toil toil = new Toil();
            toil.initAction = () =>
            {
                Pawn actor = toil.GetActor();
                Vehicle_Cart cart = toil.actor.jobs.curJob.GetTarget(CartInd).Thing as Vehicle_Cart;
                if (cart == null)
                {
                    Log.Error(actor.LabelCap + " Report: Cart is invalid.");
                    toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                }
                cart.GetComp<CompMountable>().MountOn(actor);
            };
            return toil;
        }

        public static Toil DismountAt(TargetIndex CartInd, TargetIndex StoreCellInd)
        {
            Toil toil = new Toil();
            toil.initAction = () =>
            {
                Pawn actor = toil.GetActor();
                Vehicle_Cart cart = toil.actor.jobs.curJob.GetTarget(CartInd).Thing as Vehicle_Cart;
                if (cart == null)
                {
                    Log.Error(actor.LabelCap + " Report: Cart is invalid.");
                    toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                }
                cart.GetComp<CompMountable>().DismountAt(toil.actor.jobs.curJob.GetTarget(StoreCellInd).Cell);
            };
            return toil;
        }

        public static Toil FindStoreCellForCart(TargetIndex CartInd)
        {
            const int NearbyCell = 8;
            const int RegionCellOffset = 16;
            IntVec3 invalid = new IntVec3(0, 0, 0);
#if DEBUG
            StringBuilder stringBuilder = new StringBuilder();
#endif
            Toil toil = new Toil();
            toil.initAction = () =>
            {
                IntVec3 storeCell = IntVec3.Invalid;
                Pawn actor = toil.GetActor();
                Vehicle_Cart vehicleCart = toil.actor.jobs.curJob.GetTarget(CartInd).Thing as Vehicle_Cart;
                if (vehicleCart == null)
                {
                    Log.Error(actor.LabelCap + " Report: Cart is invalid.");
                    toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                }
                //Find Valid Storage
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(vehicleCart.Position, NearbyCell, false))
                {
                    if (cell.IsValidStorageFor(vehicleCart)
                        && actor.CanReserveAndReach(cell, PathEndMode.ClosestTouch, actor.NormalMaxDanger()))
                    {
                        storeCell = cell;
#if DEBUG
                        stringBuilder.AppendLine("Found cell: " + storeCell);
#endif
                    }
                }

                if (storeCell == IntVec3.Invalid)
                {
                    //Regionwise Flood-fill cellFinder
                    int regionInd = 0;
                    List<Region> regions = new List<Region>();
                    regions.Add(vehicleCart.Position.GetRegion());
#if DEBUG
                    stringBuilder.AppendLine(actor.LabelCap + " Report");
#endif
                    bool flag1 = false;
                    while (regionInd < regions.Count)
                    {
#if DEBUG
                        stringBuilder.AppendLine("Region id: " + regions[regionInd].id);
#endif
                        if (regions[regionInd].extentsClose.CenterCell.InHorDistOf(vehicleCart.Position, NearbyCell + RegionCellOffset))
                        {
                            IntVec3 foundCell = IntVec3.Invalid;
                            IntVec3 distCell = regionInd > 0 ? regions[regionInd - 1].extentsClose.CenterCell : vehicleCart.Position;
                            float distFoundCell = float.MaxValue;
                            foreach (IntVec3 cell in regions[regionInd].Cells)
                            {
                                //Find best cell for placing cart
                                if (cell.GetEdifice() == null && cell.GetZone() == null && cell.Standable()
                                && !GenAdj.CellsAdjacentCardinal(cell, Rot4.North, IntVec2.One).Any(cardinal => cardinal.GetEdifice() is Building_Door)
                                && actor.CanReserveAndReach(cell, PathEndMode.ClosestTouch, actor.NormalMaxDanger()))
                                {
                                    if (distCell.DistanceToSquared(cell) < distFoundCell)
                                    {
                                        foundCell = cell;
                                        distFoundCell = distCell.DistanceToSquared(cell);
                                        flag1 = true;
                                    }
                                }
                            }
                            if (flag1)
                            {
                                storeCell = foundCell;
#if DEBUG
                                stringBuilder.AppendLine("Found cell: " + storeCell);
#endif
                                break;
                            }
                            foreach (RegionLink link in regions[regionInd].links)
                            {
                                if (regions.Contains(link.RegionA) == false)
                                    regions.Add(link.RegionA);
                                if (regions.Contains(link.RegionB) == false)
                                    regions.Add(link.RegionB);
                            }
                        }
                        regionInd++;
                    }
                }
                //Log.Message(stringBuilder.ToString());
                /*
                //Home Area
                if (storeCell == IntVec3.Invalid)
                    foreach (IntVec3 cell in Find.AreaHome.ActiveCells.Where(cell => (cell.GetZone() == null || cell.IsValidStorageFor(cart)) && cell.Standable() && cell.GetEdifice() == null))
                        if (cell.DistanceToSquared(cart.Position) < NearbyCell)
                            storeCell = cell;
                */
                actor.Reserve(storeCell);
                toil.actor.jobs.curJob.targetB = storeCell != invalid && storeCell != IntVec3.Invalid ? storeCell : vehicleCart.Position;
            };
            return toil;
        }



        ///////////////
        //Animal Cart//
        ///////////////

        private const int defaultWaitWorker = 2056;
        private const int tickCheckInterval = 64;

        public static Toil CallAnimalCart(TargetIndex CartInd, TargetIndex Ind)
        {
            Toil toil = new Toil();
            toil.initAction = () =>
            {
                Pawn actor = toil.GetActor();
                Vehicle_Cart cart = toil.actor.jobs.curJob.GetTarget(CartInd).Thing as Vehicle_Cart;
                if (cart == null)
                {
                    Log.Error(actor.LabelCap + " Report: Cart is invalid.");
                    toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                }
                Job job = new Job(DefDatabase<JobDef>.GetNamed("Standby"), toil.actor.jobs.curJob.GetTarget(Ind), defaultWaitWorker);
                cart.mountableComp.Driver.jobs.StartJob(job, JobCondition.InterruptForced);
            };
            return toil;
        }

        public static Toil ReleaseAnimalCart(TargetIndex CartInd)
        {
            Toil toil = new Toil();
            toil.initAction = () =>
            {
                Pawn actor = toil.GetActor();
                Vehicle_Cart cart = toil.actor.jobs.curJob.GetTarget(CartInd).Thing as Vehicle_Cart;
                if (cart == null)
                {
                    Log.Error(actor.LabelCap + " Report: Cart is invalid.");
                    toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                }
                if (cart.mountableComp.IsMounted && cart.mountableComp.Driver.CurJob.def == DefDatabase<JobDef>.GetNamed("Standby"))
                    cart.mountableComp.Driver.jobs.curDriver.EndJobWith(JobCondition.Succeeded);
            };
            return toil;
        }

        public static Toil WaitForAnimalCart(TargetIndex CartInd, TargetIndex HaulableInd)
        {
            //       Log.Message("WaitForAnimalCart");
            Toil toil = new Toil();
            int tickTime = 0;
            toil.initAction = () =>
            {
                Pawn actor = toil.GetActor();
                Vehicle_Cart cart = toil.actor.jobs.curJob.GetTarget(CartInd).Thing as Vehicle_Cart;
                if (cart == null)
                {
                    Log.Error(actor.LabelCap + " Report: Cart is invalid.");
                    toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                }
                tickTime = 0;
                if (cart.mountableComp.IsMounted)
                {
                    //Worker has arrived and Animal cart is coming
                    if (cart.mountableComp.Driver.CurJob.def == DefDatabase<JobDef>.GetNamed("Standby") &&
                        (!actor.Position.AdjacentTo8WayOrInside(cart) ||
                         !actor.Position.AdjacentTo8WayOrInside(cart.mountableComp.Driver)))
                    {
                        tickTime = 0;

                    }
                    //Worker has arrived and Animal cart has arrived
                    else if (cart.mountableComp.Driver.CurJob.def == DefDatabase<JobDef>.GetNamed("Standby") && (actor.Position.AdjacentTo8WayOrInside(cart) || actor.Position.AdjacentTo8WayOrInside(cart.mountableComp.Driver)))
                        toil.actor.jobs.curDriver.ReadyForNextToil();
                    //Worker has arrived but Animal cart is missing
                    else
                    {
                        Job job = new Job(DefDatabase<JobDef>.GetNamed("Standby"), actor, defaultWaitWorker);
                        cart.mountableComp.Driver.jobs.StartJob(job, JobCondition.InterruptForced);
                    }
                }
                else
                    toil.actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
            };
            toil.tickAction = () =>
            {
                Pawn actor = toil.GetActor();
                Vehicle_Cart cart = toil.actor.jobs.curJob.GetTarget(CartInd).Thing as Vehicle_Cart;
                if (cart == null)
                {
                    Log.Error(actor.LabelCap + " Report: Cart is invalid.");
                    toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                }
                if (Find.TickManager.TicksGame % tickCheckInterval == 0)
                    if (cart.mountableComp.IsMounted)
                    {
                        //Animal cart has arrived
                        if (cart.mountableComp.Driver.CurJob.def == DefDatabase<JobDef>.GetNamed("Standby") &&
                            (actor.Position.AdjacentTo8WayOrInside(cart.mountableComp.Driver) || actor.Position.AdjacentTo8WayOrInside(cart)))
                        {
                            toil.actor.jobs.curDriver.ReadyForNextToil();
                        }
                        //Animal cart would never come. Imcompletable.
                        else if (cart.mountableComp.Driver.CurJob.def != DefDatabase<JobDef>.GetNamed("Standby") ||
                                 tickTime >= defaultWaitWorker)
                        {
                            toil.actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
                        }
                    }
                    else
                        toil.actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            return toil;
        }


    }
}
