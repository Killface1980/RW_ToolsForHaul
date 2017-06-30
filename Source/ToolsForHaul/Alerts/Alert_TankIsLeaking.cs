// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Alert_TankIsLeaking.cs" company="">
// </copyright>
// <summary>
//   Defines the Alert_TankIsLeaking type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace ToolsForHaul.Alerts
{
    using System.Collections.Generic;

    using RimWorld;

    using ToolsForHaul.Components;
    using ToolsForHaul.Vehicles;

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
            List<Map> maps = Find.Maps;
            foreach (Map currentMap in maps)
            {
                List<Thing> list = currentMap.listerThings.AllThings.FindAll(
                    aV => (aV is Vehicle_Cart) && aV.Faction == Faction.OfPlayer);

                foreach (Thing thing in list)
                {
                    Vehicle_Cart cart = thing as Vehicle_Cart;
                    if (cart.TryGetComp<CompGasTank>() != null)
                    {
                        if (cart.TryGetComp<CompGasTank>().tankLeaking)
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