using System.Collections.Generic;
using RimWorld;
using ToolsForHaul.Components;
using ToolsForHaul.Utilities;
using Verse;
using Verse.AI;

namespace ToolsForHaul.JoyGivers
{
    public class JoyGiver_GoForRide : JoyGiver_InteractBuilding
    {
        protected override bool CanDoDuringParty
        {
            get
            {
                return false;
            }
        }

        protected override Job TryGivePlayJob(Pawn pawn, Thing t)
        {
            if ((t as ThingWithComps).TryGetComp<CompMountable>().IsMounted && !ToolsForHaulUtility.IsDriverOfThisVehicle(pawn, t))
            {
                return null;
            }

            if (!JoyUtility.EnjoyableOutsideNow(pawn, null))
            {
                return null;
            }
            if (PawnUtility.WillSoonHaveBasicNeed(pawn))
            {
                return null;
            }
            Region reg;
            if (!CellFinder.TryFindClosestRegionWith(pawn.Position.GetRegion(), TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), (Region r) => r.Room.PsychologicallyOutdoors && !r.IsForbiddenEntirely(pawn), 100, out reg))
            {
                return null;
            }
            IntVec3 root;
            if (!reg.TryFindRandomCellInRegionUnforbidden(pawn, null, out root))
            {
                return null;
            }
            List<IntVec3> list;
            if (!WalkPathFinder.TryFindWalkPath(pawn, root, out list))
            {
                return null;
            }
            Job job = new Job(this.def.jobDef, list[0])
            {
                targetQueueA = new List<TargetInfo>(),
                targetB = t,
            };

            for (int i = 1; i < list.Count; i++)
            {
                job.targetQueueA.Add(list[i]);
            }

            job.locomotionUrgency = LocomotionUrgency.Jog;
            return job;
        }
    }
}
