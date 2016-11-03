using Verse;

namespace ToolsForHaul.Components
{
    public class CompVehicle : ThingComp
    {
        public CompProperties_Vehicle compProps
        {
            get
            {
                return (CompProperties_Vehicle)props;
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

        public bool LeaveTrail()
        {
            return compProps.leaveTrail;
        }

        public bool tankLeaking;

    }
}
