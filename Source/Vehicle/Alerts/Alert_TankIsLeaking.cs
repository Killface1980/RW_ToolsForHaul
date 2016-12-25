// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Alert_TankIsLeaking.cs" company="">
// </copyright>
// <summary>
//   Defines the Alert_TankIsLeaking type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace ToolsForHaul.Alerts
{
    using RimWorld;

    using ToolsForHaul.Utilities;

    using Verse;

    public class Alert_TankIsLeaking : Alert_Critical
    {
        public Alert_TankIsLeaking()
        {
            this.defaultLabel = "VehicleTankLeaking".Translate();
            this.defaultExplanation = "VehicleTankLeaking".Translate();
        }

        public override AlertReport GetReport()
        {
            {
                foreach (Thing thing in ToolsForHaulUtility.Cart)
                {
                    Vehicle_Cart vehicleCart = (Vehicle_Cart)thing;
                    if (vehicleCart.Faction == Faction.OfPlayer && vehicleCart.VehicleComp.tankLeaking)
                    {
                        return vehicleCart;
                    }
                }

                foreach (Thing thing in ToolsForHaulUtility.CartTurret)
                {
                    Vehicle_Turret vehicleTurret = (Vehicle_Turret)thing;
                    if (vehicleTurret.Faction == Faction.OfPlayer && vehicleTurret.vehicleComp.tankLeaking)
                    {
                        return vehicleTurret;
                    }
                }

                return false;
            }
        }

    }
}