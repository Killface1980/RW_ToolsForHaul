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

    public class WorkGiver_Repair : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerBuildingsRepairable.RepairableBuildings(pawn.Faction);
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            return pawn.Map.listerBuildingsRepairable.RepairableBuildings(pawn.Faction).Count == 0;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t)
        {
            Building building = t as Building;
            if (building == null)
            {
                return false;
            }
            if (!building.def.building.repairable)
            {
                return false;
            }
            if (t.Faction != pawn.Faction)
            {
                return false;
            }

            if (pawn.Faction == Faction.OfPlayer && !pawn.Map.areaManager.Home[t.Position])
            {
                return false;
            }

            if (!t.def.useHitPoints || t.HitPoints == t.MaxHitPoints)
            {
                return false;
            }

            CompMountable compMountable = t.TryGetComp<CompMountable>();
            if (compMountable != null && compMountable.IsMounted)
            {
                return false;
            }

            return t.def.useHitPoints && t.HitPoints != t.MaxHitPoints && pawn.CanReserve(building, 1) && building.Map.designationManager.DesignationOn(building, DesignationDefOf.Deconstruct) == null && !building.IsBurning();
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            return new Job(JobDefOf.Repair, t);
        }
    }
}
