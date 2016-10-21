using Verse;

namespace ToolsForHaul.Components
{
    public class CompVehicles : ThingComp
    {
        public CompProperties_Vehicles compProps
        {
            get
            {
                return (CompProperties_Vehicles)props;
            }
        }

        public bool ShowsStorage()
        {
            return compProps.showsStorage;
        }

        public bool AnimalsCanDrive()
        {
            return compProps.animalsCanDrive;
        }

        public bool IsMedical()
        {
            return compProps.isMedical;
        }

        public bool MotorizedWithoutFuel()
        {
            return compProps.motorizedWithoutFuel;
        }

        public float FuelCatchesFireHitPointsPercent()
        {
            return compProps.fuelCatchesFireHitPointsPercent;
        }
    }
}
