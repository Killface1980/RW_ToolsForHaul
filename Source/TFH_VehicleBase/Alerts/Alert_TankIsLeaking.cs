// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Alert_TankIsLeaking.cs" company="">
// </copyright>
// <summary>
//   Defines the Alert_TankIsLeaking type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace TFH_VehicleBase.Alerts
{
    using System.Collections.Generic;

    using RimWorld;

    using Verse;

    public class Alert_TankIsLeaking : Alert_Critical
    {
        public Alert_TankIsLeaking()
        {
            this.defaultLabel = "VehicleTankLeakingLabel".Translate();
            this.defaultExplanation = "VehicleTankLeakingExplanation".Translate();
        }

        public override AlertReport GetReport()
        {
            List<Map> maps = Find.Maps;
            foreach (Map currentMap in maps)
            {
                foreach (Thing thing in currentMap.VehiclesOfPlayer())
                {
                    Vehicle_Cart cart = thing as Vehicle_Cart;
                    if (cart.HasGasTank())
                    {
                        if (cart.GasTankComp.tankLeaking)
                        {
                            return cart;
                        }
                    }
                }
            }

            return false;
        }
    }
}