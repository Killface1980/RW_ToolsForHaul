using RimWorld;
using ToolsForHaul.Utilities;
using Verse;

namespace ToolsForHaul.Alerts
{
    public class Alert_TankIsLeaking : Alert_High
    {
        public override AlertReport Report
        {
            get
            {
                foreach (Vehicle_Cart vehicleCart in ToolsForHaulUtility.Cart)
                    if (vehicleCart.Faction == Faction.OfPlayer && vehicleCart.tankLeaking)
                        return vehicleCart;

                foreach (Vehicle_Turret vehicleTurret in ToolsForHaulUtility.CartTurret)
                    if (vehicleTurret.Faction == Faction.OfPlayer && vehicleTurret.tankLeaking)
                        return vehicleTurret;

                return false;
            }
        }

        public Alert_TankIsLeaking()
        {
            baseLabel = "VehicleTankLeaking".Translate();
            baseExplanation = "VehicleTankLeaking".Translate();
        }
    }
}
