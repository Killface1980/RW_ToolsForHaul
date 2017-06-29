namespace ToolsForHaul
{
    using System.Linq;
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

        public override string GetInspectString()
        {
            string text = string.Empty;

            text += "MaximumLots".Translate(this.Cells.Count);
            text += "\n";

            int blocked = 0;
            foreach (IntVec3 cell in this.Cells)
            {
                if (this.Map.thingGrid.ThingsAt(cell).Any(
                    current => current.def.passability == Traversability.PassThroughOnly
                               || current.def.passability == Traversability.Impassable))
                {
                    blocked++;
                }
            }

            text += "OccupiedLots".Translate(blocked);

            return text;
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
