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

        // RimWorld.WorkGiver_Refuel
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Vehicle_Cart cart = t as Vehicle_Cart;
            if (cart == null)
            {
                return false;
            }
            CompMountable compMountable = t.TryGetComp<CompMountable>();
            if (compMountable != null && compMountable.IsMounted)
            {
                return false;
            }
            CompRefuelable compRefuelable = t.TryGetComp<CompRefuelable>();
            bool result;
            if (compRefuelable == null || compRefuelable.IsFull)
            {
                result = false;
            }
            else
            {
                bool flag = !forced;
                if (flag && !compRefuelable.ShouldAutoRefuelNow)
                {
                    result = false;
                }
                else
                {
                    if (!t.IsForbidden(pawn))
                    {
                        LocalTargetInfo target = t;
                        if (pawn.CanReserve(target, 1, -1, null, forced))
                        {
                            if (t.Faction != pawn.Faction)
                            {
                                result = false;
                                return result;
                            }
                            ThingWithComps thingWithComps = t as ThingWithComps;
                            if (thingWithComps != null)
                            {
                                CompFlickable comp = thingWithComps.GetComp<CompFlickable>();
                                if (comp != null && !comp.SwitchIsOn)
                                {
                                    result = false;
                                    return result;
                                }
                            }
                            if (this.FindBestFuel(pawn, t) == null)
                            {
                                ThingFilter fuelFilter = t.TryGetComp<CompRefuelable>().Props.fuelFilter;
                                JobFailReason.Is("NoFuelToRefuel".Translate(new object[]
                                                                                {
                                                                                    fuelFilter.Summary
                                                                                }));
                                result = false;
                                return result;
                            }
                            result = true;
                            return result;
                        }
                    }
                    result = false;
                }
            }
            return result;
        }


        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Thing t2 = this.FindBestFuel(pawn, t);
            return new Job(JobDefOf.Refuel, t, t2);
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
