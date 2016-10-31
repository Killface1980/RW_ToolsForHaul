using Verse;

namespace ToolsForHaul
{
    public class IncidentWorker_VisitorGroup_Sanity : IncidentWorker_VisitorGroupTFH
    {
        protected override bool CanFireNowSub()
        {
            return base.CanFireNowSub() && GenTemperature.OutdoorTemp < 38.0 && GenTemperature.OutdoorTemp > -38.0;
        }
    }
}
