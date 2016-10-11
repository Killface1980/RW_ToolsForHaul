using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public class WorkGiver_RepairVehicles : WorkGiver_Scanner
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
            return ListerVehiclesRepairable.RepairableVehicles(pawn.Faction);
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            return ListerVehiclesRepairable.RepairableVehicles(pawn.Faction).Count == 0;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t)
        {
            if (t.Faction != pawn.Faction)
            {
                return false;
            }
            if (pawn.Faction == Faction.OfPlayer && !Find.AreaHome[t.Position])
            {
                return false;
            }
            if (!t.def.useHitPoints || t.HitPoints == t.MaxHitPoints)
            {
                return false;
            }
            Vehicle_Cart vehicle = t as Vehicle_Cart;
            if (vehicle != null && vehicle.repairable && pawn.CanReserve(vehicle, 1) && Find.DesignationManager.DesignationOn(vehicle, DesignationDefOf.Deconstruct) == null && !vehicle.IsBurning()) return true;

            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            return new Job(JobDefOf.Repair, t);
        }
    }
}
