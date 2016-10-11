using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public class WorkGiver_FixBrokenDownVehicle : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForGroup(ThingRequestGroup.Everything);
            }
        }

        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.Touch;
            }
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return Find.Map.GetComponent<BreakdownManager>().brokenDownThings;
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            return Find.Map.GetComponent<BreakdownManager>().brokenDownThings.Count == 0;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t)
        {
            if (t.Faction != pawn.Faction)
            {
                return false;
            }
            if (t.IsForbidden(pawn))
            {
                return false;
            }
            if (pawn.Faction == Faction.OfPlayer && !Find.AreaHome[t.Position])
            {
                return false;
            }
            if (!t.IsBrokenDown())
            {
                return false;
            }
            Vehicle_Cart vehicleCart = t as Vehicle_Cart;
            if (vehicleCart == null)
            {
                return false;
            }
            if (!vehicleCart.repairable)
            {
                return false;
            }
            if (!pawn.CanReserve(vehicleCart, 1))
            {
                return false;
            }
            if (Find.DesignationManager.DesignationOn(vehicleCart, DesignationDefOf.Deconstruct) != null)
            {
                return false;
            }
            if (vehicleCart.IsBurning())
            {
                return false;
            }
            if (FindClosestComponent(pawn) == null)
            {
                JobFailReason.Is("NoComponentsToRepair".Translate());
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            Thing t2 = FindClosestComponent(pawn);
            return new Job(JobDefOf.FixBrokenDownBuilding, t, t2)
            {
                maxNumToCarry = 1
            };
        }

        private Thing FindClosestComponent(Pawn pawn)
        {
            Predicate<Thing> validator = (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x, 1);
            return GenClosest.ClosestThingReachable(pawn.Position, ThingRequest.ForDef(ThingDefOf.Component), PathEndMode.InteractionCell, TraverseParms.For(pawn, pawn.NormalMaxDanger(), TraverseMode.ByPawn, false), 9999f, validator, null, -1, false);
        }
    }
}
