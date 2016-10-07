using Verse;

namespace ToolsForHaul
{
    internal class CompVehicles : ThingComp
    {
        public CompProperties_Vehicles Props
        {
            get
            {
                return (CompProperties_Vehicles)props;
            }
        }

        public bool ShowsStorage()
        {
            return Props.showsStorage;
        }

        public bool AnimalsCanDrive()
        {
            return Props.animalsCanDrive;
        }

        public bool MotorizedWithoutFuel()
        {
            return Props.motorizedWithoutFuel;
        }

        public float FuelCatchesFireHitPointsPercent()
        {
            return Props.fuelCatchesFireHitPointsPercent;
        }
    }
}
