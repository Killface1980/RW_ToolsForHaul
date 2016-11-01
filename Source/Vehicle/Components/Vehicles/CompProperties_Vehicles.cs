using Verse;

namespace ToolsForHaul.Components
{
    public class CompProperties_Vehicles : CompProperties
    {
        public bool showsStorage;

        public bool animalsCanDrive;

        public float fuelCatchesFireHitPointsPercent;

        public bool motorizedWithoutFuel;

        public SoundDef soundAmbient;

        public CompProperties_Vehicles()
        {
            compClass = typeof(CompVehicles);
        }

        public bool isMedical;

        public ShadowData specialShadowData;

    }
}
