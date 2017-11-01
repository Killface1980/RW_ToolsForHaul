using Verse;

namespace TFH_VehicleBase
{
    using RimWorld;

    using Verse.AI;

    public class ThinkNode_ConditionalShouldBeIdle : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            return ShouldBeIdle(pawn);
        }

        public static bool ShouldBeIdle(Pawn pawn)
        {
            BasicVehicle vehicle = pawn as BasicVehicle;
            if (vehicle.MountableComp.IsMounted)
            {
                return true;
            }

                return false;
         }
    }
}
