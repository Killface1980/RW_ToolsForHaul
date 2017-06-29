using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ToolsForHaul
{
    using UnityEngine;

    using Verse;

    class Zone_ParkingLot : Zone
    {
        public Zone_ParkingLot()
        {
        }

        public Zone_ParkingLot(ZoneManager zoneManager) : base("ParkingLotZone".Translate(), zoneManager)
        {
        }

        public override bool IsMultiselectable
        {
            get
            {
                return true;
            }
        }
        protected override Color NextZoneColor
        {
            get
            {
                return Static.ParkingLotColour;  // ZoneColorUtility.NextGrowingZoneColor();
            }
        }
    }
}
