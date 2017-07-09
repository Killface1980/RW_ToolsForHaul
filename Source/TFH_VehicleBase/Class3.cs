using System;
using Verse;
using Verse.AI;

namespace TFH_VehicleBase
{
    using RimWorld;

    public class ThinkNode_ConditionalShouldBeIdle : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            return ShouldBeIdle(pawn);
        }

        public static bool ShouldBeIdle(Pawn pawn)
        {
            var vehicle = pawn as Vehicle_Cart;
            if (vehicle.MountableComp.IsMounted)
            {
                return true;
            }
                return false;
         }
    }
}
