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
    using System.Linq;

    using RimWorld;

    using ToolsForHaul.Components;
    using ToolsForHaul.Utilities;
    using ToolsForHaul.Vehicles;

    using Verse;
    using Verse.AI;

    public class Alert_NoParkingLot : Alert_Critical
    {
        private int count;

        private int blocked;

        public Alert_NoParkingLot()
        {
            this.defaultLabel = "NoParkingLotLabel".Translate();
            this.defaultExplanation = "NoParkingLotExplanation".Translate();
        }

        public override AlertReport GetReport()
        {
            count = 0;
            blocked = 0;

            List<Map> maps = Find.Maps;

            if (!Find.VisibleMap.IsPlayerHome)
            {
                return false;
            }

            var currentMap = Find.VisibleMap;
            {

                List<Zone> zonesList = currentMap.zoneManager.AllZones;
                foreach (Zone zone in zonesList)
                {
                    Zone_ParkingLot parkingLot = zone as Zone_ParkingLot;
                    if (parkingLot != null)
                    {
                        this.count += parkingLot.Cells.Count;

                        foreach (IntVec3 cell in parkingLot.Cells)
                        {
                            foreach (Thing current in currentMap.thingGrid.ThingsAt(cell))
                            {
                                if (current is Vehicle_Cart)
                                {
                                    continue;
                                }
                                if (current.def.passability == Traversability.PassThroughOnly
                                    || current.def.passability == Traversability.Impassable)
                                {
                                    this.blocked++;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (this.count - this.blocked < currentMap.VehiclesOfPlayer().Count)
                {
                    return true;
                }
            }



            return false;
        }
    }
}