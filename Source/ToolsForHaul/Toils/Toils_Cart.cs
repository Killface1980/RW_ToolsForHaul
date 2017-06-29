namespace ToolsForHaul.Toils
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using RimWorld;

    using ToolsForHaul.Components;
    using ToolsForHaul.Defs;
    using ToolsForHaul.Utilities;
    using ToolsForHaul.Vehicles;

    using Verse;
    using Verse.AI;

    public static class Toils_Cart
    {
        public static Toil MountOn(TargetIndex CartInd)
        {
            Toil toil = new Toil();
            toil.initAction = () =>
            {
                Pawn actor = toil.GetActor();
                ThingWithComps cart = toil.actor.jobs.curJob.GetTarget(CartInd).Thing as ThingWithComps;
                if (cart == null)
                {
                    Log.Error(actor.LabelCap + " Report: Cart is invalid.");
                    toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                }

                cart.TryGetComp<CompMountable>().MountOn(actor);
            };
            return toil;
        }

        public static Toil MountOtherOn(TargetIndex CartInd, Pawn patient)
        {
            Toil toil = new Toil();
            toil.initAction = () =>
            {
                Pawn actor = toil.GetActor();
                Vehicle_Cart cart = toil.actor.jobs.curJob.GetTarget(CartInd).Thing as Vehicle_Cart;
                if (cart == null)
                {
                    Log.Error(actor.LabelCap + " Report: Wheel chair is invalid.");
                    toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                }

                cart.MountableComp.MountOn(patient);
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

                cart.MountableComp.DismountAt(toil.actor.jobs.curJob.GetTarget(StoreCellInd).Cell);
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
                    ThingWithComps vehicleCart = toil.actor.jobs.curJob.GetTarget(CartInd).Thing as ThingWithComps;
                    if (vehicleCart == null)
                    {
                        Log.Error(actor.LabelCap + " Report: Cart is invalid.");
                        toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                    }

                    // Find Valid Storage
                    foreach (IntVec3 cell in GenRadial.RadialCellsAround(vehicleCart.Position, NearbyCell, false))
                    {
                        if (cell.IsValidStorageFor(actor.Map, vehicleCart )
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
                        // Regionwise Flood-fill cellFinder
                        int regionInd = 0;
                        List<Region> regions = new List<Region> { vehicleCart.Position.GetRegion(actor.Map) };
#if DEBUG
                    stringBuilder.AppendLine(actor.LabelCap + " Report");
#endif
                        bool flag1 = false;
                        while (regionInd < regions.Count)
                        {
#if DEBUG
                        stringBuilder.AppendLine("Region id: " + regions[regionInd].id);
#endif
                            if (regions[regionInd].extentsClose.CenterCell.InHorDistOf(
                                vehicleCart.Position,
                                NearbyCell + RegionCellOffset))
                            {
                                IntVec3 foundCell = IntVec3.Invalid;
                                IntVec3 distCell = regionInd > 0
                                                       ? regions[regionInd - 1].extentsClose.CenterCell
                                                       : vehicleCart.Position;
                                float distFoundCell = float.MaxValue;
                                foreach (IntVec3 cell in regions[regionInd].Cells)
                                {
                                    // Find best cell for placing cart
                                    if (cell.GetEdifice(actor.Map) == null && cell.GetZone(actor.Map) == null && cell.Standable(actor.Map)
                                        && !GenAdj.CellsAdjacentCardinal(cell, Rot4.North, IntVec2.One)
                                            .Any(cardinal => cardinal.GetEdifice(actor.Map) is Building_Door)
                                        && actor.CanReserveAndReach(
                                            cell,
                                            PathEndMode.ClosestTouch,
                                            actor.NormalMaxDanger()))
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
                                    if (regions.Contains(link.RegionA) == false) regions.Add(link.RegionA);
                                    if (regions.Contains(link.RegionB) == false) regions.Add(link.RegionB);
                                }
                            }

                            regionInd++;
                        }
                    }

                    // Log.Message(stringBuilder.ToString());
                    /*
                    //Home Area
                    if (storeCell == IntVec3.Invalid)
                        foreach (IntVec3 cell in Find.AreaHome.ActiveCells.Where(cell => (cell.GetZone() == null || cell.IsValidStorageFor(cart)) && cell.Standable() && cell.GetEdifice() == null))
                            if (cell.DistanceToSquared(cart.Position) < NearbyCell)
                                storeCell = cell;
                    */
                    actor.Reserve(storeCell);
                    toil.actor.jobs.curJob.targetB = storeCell != invalid && storeCell != IntVec3.Invalid
                                                         ? storeCell
                                                         : vehicleCart.Position;
                };
            return toil;
        }

        public static Toil FindParkingSpaceForCartForCart(TargetIndex CartInd)
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

                    // Find Valid Storage
                    if (!TFH_Utility.FindParkingSpace(actor, actor.Position, out storeCell))
                    {
                        Log.Error(actor.LabelCap + " Report: No parking space for cart.");
                        toil.actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                    }

                    // Log.Message(stringBuilder.ToString());
                    /*
                    //Home Area
                    if (storeCell == IntVec3.Invalid)
                        foreach (IntVec3 cell in Find.AreaHome.ActiveCells.Where(cell => (cell.GetZone() == null || cell.IsValidStorageFor(cart)) && cell.Standable() && cell.GetEdifice() == null))
                            if (cell.DistanceToSquared(cart.Position) < NearbyCell)
                                storeCell = cell;
                    */
                    actor.Reserve(storeCell);
                    toil.actor.jobs.curJob.targetB = storeCell != invalid && storeCell != IntVec3.Invalid
                                                         ? storeCell
                                                         : vehicleCart.Position;
                };
            return toil;
        }



        ///////////////
        // Animal Cart//
        ///////////////
        private const int defaultWaitWorker = 2056;
        private const int tickCheckInterval = 64;

        public static Toil CallAnimalCart(TargetIndex CartInd, TargetIndex Ind, Pawn pawn = null)
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

                Job job;
                if (pawn != null)
                    job = new Job(HaulJobDefOf.StandBy, pawn.Position, defaultWaitWorker);
                else
                    job = new Job(HaulJobDefOf.StandBy, toil.actor.jobs.curJob.GetTarget(Ind), defaultWaitWorker);

                cart.MountableComp.Driver.jobs.StartJob(job, JobCondition.InterruptForced);
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

                if (cart.MountableComp.IsMounted && cart.MountableComp.Driver.CurJob.def == HaulJobDefOf.StandBy)
                    cart.MountableComp.Driver.jobs.curDriver.EndJobWith(JobCondition.Succeeded);
            };
            return toil;
        }

        public static Toil WaitForAnimalCart(TargetIndex CartInd, TargetIndex HaulableInd)
        {
            // Log.Message("WaitForAnimalCart");
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
                if (cart.MountableComp.IsMounted)
                {
                    // Worker has arrived and Animal cart is coming
                    if (cart.MountableComp.Driver.CurJob.def == HaulJobDefOf.StandBy
                        && (!actor.Position.InHorDistOf(cart.Position, 1f)
                            || !actor.Position.InHorDistOf(cart.MountableComp.Driver.Position, 1f)))
                    {
                        tickTime = 0;
                    }

                    // Worker has arrived and Animal cart has arrived
                    else if (cart.MountableComp.Driver.CurJob.def == HaulJobDefOf.StandBy
                             && actor.Position.InHorDistOf(cart.MountableComp.Driver.Position, 1f)) toil.actor.jobs.curDriver.ReadyForNextToil();

                    // Worker has arrived but Animal cart is missing
                    else
                    {
                        Job job = new Job(HaulJobDefOf.StandBy, actor, defaultWaitWorker);
                        cart.MountableComp.Driver.jobs.StartJob(job, JobCondition.InterruptForced);
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
                    if (cart.MountableComp.IsMounted)
                    {
                        // Animal cart has arrived
                        if (cart.MountableComp.Driver.CurJob.def == HaulJobDefOf.StandBy
                            && (actor.Position.AdjacentTo8WayOrInside(cart.MountableComp.Driver)
                                || actor.Position.AdjacentTo8WayOrInside(cart)))
                        {
                            toil.actor.jobs.curDriver.ReadyForNextToil();
                        }

                        // Animal cart would never come. Imcompletable.
                        else if (cart.MountableComp.Driver.CurJob.def != HaulJobDefOf.StandBy
                                 || tickTime >= defaultWaitWorker)
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
