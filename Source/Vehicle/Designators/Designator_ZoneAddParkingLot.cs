using System;
using UnityEngine;
using Verse;

namespace ToolsForHaul
{
    using RimWorld;

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

            this.defaultLabel = "ParkingLot".Translate();
            this.defaultDesc = "DesignatorParkingLotDesc".Translate();
            this.icon = ContentFinder<Texture2D>.Get("UI/Designators/ZoneCreate_Stockpile", true);
       //     this.hotKey = KeyBindingDefOf.Misc1;
            this.tutorTag = "ZoneAdd_ParkingLot";
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
      //      PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.GrowingFood, KnowledgeAmount.Total);
            return new Zone_ParkingLot(Find.VisibleMap.zoneManager);
        }
    }
}
