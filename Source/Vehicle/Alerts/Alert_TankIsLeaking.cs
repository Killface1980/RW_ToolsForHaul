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
                foreach (Vehicle_Cart cart in ToolsForHaulUtility.Cart())
                    if (cart.Faction == Faction.OfPlayer && cart.tankLeaking)
                        return cart;

                foreach (Vehicle_Turret cart in ToolsForHaulUtility.CartTurret())
                    if (cart.Faction == Faction.OfPlayer && cart.tankLeaking)
                        return cart;

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
