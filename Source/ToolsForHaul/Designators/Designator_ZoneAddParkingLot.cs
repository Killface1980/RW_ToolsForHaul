namespace ToolsForHaul.Designators
{
    using RimWorld;

    using Verse;

    public class Designator_ZoneAddParkingLot : Designator_ZoneAdd
    {

        protected override string NewZoneLabel
        {
            get
            {
                return "ParkingLot".Translate();
            }
        }
        public Designator_ZoneAddParkingLot()
        {
            this.zoneTypeToPlace = typeof(Zone_ParkingLot);
            this.defaultLabel = "ParkingLotZone".Translate();
            this.defaultDesc = Static.ParkingLotDesc;
            this.icon = Static.TexParkingLot;
            this.tutorTag = "ZoneAdd_ParkingLot";

            // this.hotKey = KeyBindingDefOf.Misc1;
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!base.CanDesignateCell(c).Accepted)
            {
                return false;
            }

            return true;
        }

        protected override Zone MakeNewZone()
        {
            // TODO add tutorials
      // PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.GrowingFood, KnowledgeAmount.Total);
            return new Zone_ParkingLot(Find.VisibleMap.zoneManager);
        }
    }
}
