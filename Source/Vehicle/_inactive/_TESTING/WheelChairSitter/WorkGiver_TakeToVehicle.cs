using RimWorld;
using Verse;

namespace ToolsForHaul
{
    public abstract class WorkGiver_TakeToVehicle : WorkGiver_Scanner
    {
        protected Vehicle_Cart FindWheelChair(Pawn pawn, Pawn patient)
        {
            return ToolsForHaulUtility.FindWheelChair(patient, pawn);
        }
    }
}
