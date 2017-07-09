namespace TFH_VehicleBase.JobGivers
{
    using System.Collections.Generic;

    using RimWorld;

    using Verse;
    using Verse.AI;

    public class JoyGiver_GoForRide : JoyGiver_InteractBuilding
    {
        protected override bool CanDoDuringParty => true;

        protected override Job TryGivePlayJob(Pawn pawn, Thing t)
        {
            var cart = t as Vehicle_Cart;

            if (cart == null)
            {
                return null;
            }

            if (cart.MountableComp.IsMounted && !pawn.IsDriver(out Vehicle_Cart drivenCart, cart))
            {
                return null;
            }

            if (!cart.RefuelableComp.HasFuel)
            {
                return null;
            }

            if (t.IsForbidden(Faction.OfPlayer))
            {
                return null;
            }

            if (!JoyUtility.EnjoyableOutsideNow(pawn))
            {
                return null;
            }

            if (PawnUtility.WillSoonHaveBasicNeed(pawn))
            {
                return null;
            }

            Region reg;
            if (!CellFinder.TryFindClosestRegionWith(pawn.Position.GetRegion(pawn.Map), TraverseParms.For(pawn), r => r.Room.PsychologicallyOutdoors && !r.IsForbiddenEntirely(pawn), 100, out reg))
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
                targetQueueA = new List<LocalTargetInfo>(),
                targetB = t
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
