using System;

using RimWorld;

using Verse;

namespace TFH_RideableAnimals
{
    using TFH_VehicleBase;

    using Verse.AI;
    public class JobGiver_ProvideRide : ThinkNode_JobGiver
    {
        public int ticks = 300;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (((Vehicle_Animal)pawn).RideableComp.IsMounted)
            {
                return new Job(JobDefOf.Wait)
                {
                    expiryInterval = this.ticks
                };
            }
            return null;
        }
    }
}
