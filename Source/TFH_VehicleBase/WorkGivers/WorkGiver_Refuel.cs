namespace TFH_VehicleBase.WorkGivers
{
    using System;

    using RimWorld;

    using TFH_VehicleBase.Components;

    using Verse;
    using Verse.AI;

    public class WorkGiver_Refuel_Vehicle : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Refuelable);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced)
        {
            return this.CanRefuel(pawn, t, true) && pawn.Faction == t.Faction;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced)
        {
            Thing t2 = this.FindBestFuel(pawn, t);
            return new Job(JobDefOf.Refuel, t, t2)
            {
                count = t.TryGetComp<CompRefuelable>().GetFuelCountToFullyRefuel()
            };
        }

        private bool CanRefuel(Pawn pawn, Thing t, bool mustBeAutoRefuelable)
        {
            var cart = t as Vehicle_Cart;
            if (cart == null) return false;

            CompRefuelable compRefuelable = t.TryGetComp<CompRefuelable>();
            if (compRefuelable == null || compRefuelable.IsFull)
            {
                return false;
            }

            if (mustBeAutoRefuelable && !compRefuelable.ShouldAutoRefuelNow)
            {
                return false;
            }

            if (t.IsForbidden(pawn) || !pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger()))
            {
                return false;
            }

            if (this.FindBestFuel(pawn, t) == null)
            {
                ThingFilter fuelFilter = t.TryGetComp<CompRefuelable>().Props.fuelFilter;
                JobFailReason.Is("NoFuelToRefuel".Translate(fuelFilter.Summary));
                return false;
            }

            CompMountable compMountable = t.TryGetComp<CompMountable>();
            if (compMountable != null && compMountable.IsMounted)
            {
                return false;
            }

            return true;
        }

        private Thing FindBestFuel(Pawn pawn, Thing refuelable)
        {
            ThingFilter filter = refuelable.TryGetComp<CompRefuelable>().Props.fuelFilter;
            Predicate<Thing> predicate = x => !x.IsForbidden(pawn) && pawn.CanReserve(x) && filter.Allows(x);
            Predicate<Thing> validator = predicate;
            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, filter.BestThingRequest, PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, validator);
        }
    }
}
