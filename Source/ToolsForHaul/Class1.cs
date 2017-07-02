using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul.WorkGivers
{
    using ToolsForHaul;


    public class WorkGiver_WithVehicle_ConstructDeliverResourcesToFrames : RimWorld.WorkGiver_ConstructDeliverResourcesToFrames
    {
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced)
        {
            var job = base.JobOnThing(pawn, t, forced);
            if (job == null)
            {
                return null;
            }
            if (job.def != JobDefOf.HaulToContainer)
            {
                return job;
            }
            return AcEnhancedHauling.SmartBuild(pawn, job);
        }
    }

    internal class WorkGiver_WithVehicle_ConstructDeliverResourcesToBlueprints : RimWorld.WorkGiver_ConstructDeliverResourcesToBlueprints
    {
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced)
        {
            var job = base.JobOnThing(pawn, t, forced);
            if (job == null)
            {
                return null;
            }

            if ((job.def != JobDefOf.HaulToContainer) || !(t is Blueprint))
            {
                return job;
            }
            return AcEnhancedHauling.SmartBuild(pawn, job);
        }
    }
}