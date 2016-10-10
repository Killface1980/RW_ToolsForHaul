using Verse;

namespace ToolsForHaul
{
    internal class CompProperties_Vehicles : CompProperties
    {
        public bool showsStorage;

        public bool animalsCanDrive;

        public float fuelCatchesFireHitPointsPercent;

        public bool motorizedWithoutFuel;

        public CompProperties_Vehicles()
        {
            compClass = typeof(CompVehicles);
        }
    }
}
