//#define DEBUG

using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public class WorkGiver_HaulWithBackpack : WorkGiver
    {

        public override bool ShouldSkip(Pawn pawn)
        {
            Trace.DebugWriteHaulingPawn(pawn);
            //Don't have haulables.
            if (ListerHaulables.ThingsPotentiallyNeedingHauling().Count == 0)
                return true;

            //Should skip pawn that don't have backpack.
            if (ToolsForHaulUtility.TryGetBackpack(pawn) == null)
                    return true;
            return false;
        }

        public override Job NonScanJob(Pawn pawn)
        {
            if (ToolsForHaulUtility.TryGetBackpack(pawn) != null)
                return ToolsForHaulUtility.HaulWithTools(pawn);
            JobFailReason.Is("NoBackpack".Translate());
            return null;
        }
    }
}