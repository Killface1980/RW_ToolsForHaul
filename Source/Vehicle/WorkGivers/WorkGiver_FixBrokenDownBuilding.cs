using System;
using System.Collections.Generic;
using RimWorld;
using ToolsForHaul.Components;
using Verse;
using Verse.AI;

namespace ToolsForHaul.WorkGivers
{
    using ToolsForHaul.Components.Vehicle;
    using ToolsForHaul.Components.Vehicles;

    public class WorkGiver_FixBrokenDownBuilding : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

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

            Building building = t as Building;
            if (building == null)
            {
                return false;
            }

            if (!building.def.building.repairable)
            {
                return false;
            }

            if (!pawn.CanReserve(building, 1))
            {
                return false;
            }

            if (Find.DesignationManager.DesignationOn(building, DesignationDefOf.Deconstruct) != null)
            {
                return false;
            }

            if (building.IsBurning())
            {
                return false;
            }

            if (this.FindClosestComponent(pawn) == null)
            {
                JobFailReason.Is("NoComponentsToRepair".Translate());
                return false;
            }

            if (t.TryGetComp<CompMountable>().IsMounted)
            {
                return false;
            }

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            Thing t2 = this.FindClosestComponent(pawn);
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
