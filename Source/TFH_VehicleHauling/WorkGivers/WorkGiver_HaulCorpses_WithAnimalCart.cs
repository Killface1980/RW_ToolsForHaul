namespace TFH_VehicleHauling.WorkGivers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using RimWorld;

    using TFH_VehicleBase;
    using TFH_VehicleBase.Components;

    using Verse;
    using Verse.AI;

    public class WorkGiver_HaulCorpses_WithAnimalCart : WorkGiver_Scanner
    {
        private static List<Thing> availableVehicle;

        private static IntVec3 invalidCell = new IntVec3(0, 0, 0);

        public WorkGiver_HaulCorpses_WithAnimalCart()
            : base()
        {
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced)
        {
            // TODO Corpses with animal carts?
            return null;

            if (!(t is Corpse))
            {
                return null;
            }
            var carrier=new Vehicle_Cart();

            var storage = carrier.GetContainer();

            IEnumerable<Thing> remainingItems = storage;
            int reservedMaxItem = storage.Count;
            Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("HaulWithAnimalCart"));

            // jobNew.maxNumToCarry = 99999;
            // jobNew.haulMode = HaulMode.ToCellStorage;
            jobNew.targetQueueA = new List<LocalTargetInfo>();
            jobNew.targetQueueB = new List<LocalTargetInfo>();

            // Set carrier
            jobNew.targetC = carrier;
            pawn.Reserve(carrier);

            // Drop remaining item
            foreach (var remainingItem in remainingItems)
            {
                IntVec3 storageCell = this.FindStorageCell(pawn, remainingItem, jobNew.targetQueueB);
                if (!storageCell.IsValid)
                {
                    break;
                }

                pawn.Reserve(storageCell);
                jobNew.targetQueueB.Add(storageCell);
            }

            if (!jobNew.targetQueueB.NullOrEmpty())
            {
                return jobNew;
            }

            // collectThing Predicate
            Predicate<Thing> predicate = item => !jobNew.targetQueueA.Contains(item) && pawn.CanReserve(item)
                                                 && !item.IsInValidBestStorage();

            // Collect and drop item
            while (reservedMaxItem < carrier.MaxItem)
            {
                IntVec3 storageCell = IntVec3.Invalid;
                Thing closestHaulable = null;

                IntVec3 searchPos;
                searchPos = (!jobNew.targetQueueA.NullOrEmpty() && jobNew.targetQueueA.First() != IntVec3.Invalid)
                                ? jobNew.targetQueueA.First().Thing.Position
                                : searchPos = carrier.Position;

                closestHaulable = GenClosest.ClosestThing_Global_Reachable(
                    searchPos,
                    pawn.Map,
                    pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling(),
                    PathEndMode.Touch,
                    TraverseParms.For(pawn, Danger.Some),
                    9999,
                    predicate);
                if (closestHaulable == null)
                {
                    break;
                }

                storageCell = this.FindStorageCell(pawn, closestHaulable, jobNew.targetQueueB);
                if (storageCell == IntVec3.Invalid)
                {
                    break;
                }

                jobNew.targetQueueA.Add(closestHaulable);
                jobNew.targetQueueB.Add(storageCell);
                pawn.Reserve(closestHaulable);
                pawn.Reserve(storageCell);
                reservedMaxItem++;
            }

            // Has job?
            if (!jobNew.targetQueueA.NullOrEmpty() && !jobNew.targetQueueB.NullOrEmpty())
            {
                return jobNew;
            }

            // No haulables or zone. Release everything
            pawn.Map.reservationManager.ReleaseAllClaimedBy(pawn);
            return null;
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            availableVehicle = pawn.Map.listerThings.AllThings.FindAll(
                (Thing aV) => ((aV is Vehicle_Cart) && !aV.IsForbidden(pawn.Faction)
                               && pawn.CanReserveAndReach(aV, PathEndMode.Touch, Danger.Some)
                               && (aV.TryGetComp<CompMountable>().IsMounted
                                   && aV.TryGetComp<CompMountable>().Rider.RaceProps.Animal
                                   && aV.TryGetComp<CompMountable>().Rider.needs.food.CurCategory
                                   != HungerCategory.Hungry
                                   && aV.TryGetComp<CompMountable>().Rider.needs.rest.CurCategory
                                   != RestCategory.Tired) // Driver is animal not hungry and restless
                ));

#if DEBUG

            // Log.Message("Number of Reservation:" + Find.Reservations.AllReservedThings().Count().ToString());
            // Log.Message("availableVehicle Count: " + availableVehicle.Count);
#endif
            return availableVehicle as IEnumerable<Thing>;
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            availableVehicle = this.PotentialWorkThingsGlobal(pawn) as List<Thing>;

            return availableVehicle.Find(aV => ((Vehicle_Cart)aV).GetContainer().TotalStackCount > 0)
                    == null // Need to drop
                    && pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling().Count == 0; // No Haulable
        }

        private IntVec3 FindStorageCell(Pawn pawn, Thing closestHaulable, List<LocalTargetInfo> targetQueue)
        {
            if (!targetQueue.NullOrEmpty())
            {
                foreach (LocalTargetInfo target in targetQueue)
                {
                    foreach (var adjCell in GenAdjFast.AdjacentCells8Way(target))
                    {
                        if (!targetQueue.Contains(adjCell) && adjCell.IsValidStorageFor(pawn.Map, closestHaulable)
                            && pawn.CanReserve(adjCell))
                        {
                            return adjCell;
                        }
                    }
                }
            }

            foreach (var slotGroup in pawn.Map.slotGroupManager.AllGroupsListInPriorityOrder)
            {
                foreach (var cell in slotGroup.CellsList.Where(
                    cell => !targetQueue.Contains(cell)
                            && cell.IsValidStorageFor(pawn.Map, closestHaulable)
                            && pawn.CanReserve(cell)))
                {
                    if (cell != invalidCell && cell != IntVec3.Invalid)
                    {
                        return cell;
                    }
                }
            }

            return IntVec3.Invalid;
        }
    }
}