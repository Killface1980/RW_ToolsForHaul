//#define DEBUG

namespace TFH_Tools.WorkGivers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using RimWorld;

    using TFH_Tools;

    using TFH_VehicleBase;

    using Verse;
    using Verse.AI;

    public class WorkGiver_EmptyBackpack : WorkGiver
    {

        public override bool ShouldSkip(Pawn pawn)
        {

            Apparel_Backpack backpack = pawn.TryGetBackpack();

            // Should skip pawn that don't have backpack.
            if (backpack == null)
            {
                return true;
            }
            if (backpack.slotsComp.innerContainer.Count > 0)
            {
                return false;
            }
            // if (backpack.MaxItem - pawn.inventory.innerContainer.Count == 0)
            // {
            //     return true;
            // }

            Trace.DebugWriteHaulingPawn(pawn);
            return true;
        }

        public override Job NonScanJob(Pawn pawn)
        {

            Apparel_Backpack backpack = pawn.TryGetBackpack();
            if (backpack != null && backpack.slotsComp.innerContainer.Count>0)
            {
                    return ToolsForHaulUtility.HaulWithTools(pawn);
            }

            JobFailReason.Is("NoBackpackWithStuff".Translate());
            return null;
        }
    }
}