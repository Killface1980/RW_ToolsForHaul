using Verse;

namespace ToolsForHaul.Components
{
    public class CompProperties_Vehicle : CompProperties
    {
        public bool showsStorage;

        public bool animalsCanDrive;

        public float fuelCatchesFireHitPointsPercent;

        public bool motorizedWithoutFuel;

        public SoundDef soundAmbient;

        public CompProperties_Vehicle()
        {
            compClass = typeof(CompVehicle);
        }

        public bool isMedical;

        public ShadowData specialShadowData;

        public bool leaveTrail = true;
    }
}
