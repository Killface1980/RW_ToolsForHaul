namespace TFH_Tools
{
    using System.Collections.Generic;

    using RimWorld;

    using TFH_VehicleBase;

    using Verse;

    public class MapComponent_ToolsForHaul : MapComponent
    {
        public struct Entry
        {
            public Pawn pawn;

            public Thing tool;

            public StatDef stat;

            public float workStat;

            public Entry(Pawn pawn, Thing tool, StatDef stat, float workStat)
            {
                this.pawn = pawn;
                this.tool = tool;
                this.stat = stat;
                this.workStat = workStat;
            }
        }

        private static List<Entry> _cachedToolEntries = new List<Entry>();

        public static Dictionary<Pawn, ThingWithComps> PreviousPawnWeapon = new Dictionary<Pawn, ThingWithComps>();



        public static List<Thing> AutoInventory = new List<Thing>();

        public static Dictionary<Pawn, BasicVehicle> currentVehicle = new Dictionary<Pawn, BasicVehicle>();

        public static List<Entry> CachedToolEntries
        {
            get
            {
                return _cachedToolEntries;
            }

            set
            {
                _cachedToolEntries = value;
            }
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref PreviousPawnWeapon, "previousPawnWeapons", LookMode.Reference, LookMode.Reference);
            Scribe_Collections.Look(ref currentVehicle, "currentVehicle", LookMode.Reference, LookMode.Reference);
            Scribe_Collections.Look(ref AutoInventory, "AutoInventory", LookMode.Reference);
            Scribe_Collections.Look(ref _cachedToolEntries, "_cachedToolEntries", LookMode.Reference);
        }

        public MapComponent_ToolsForHaul(Map map)
            : base(map)
        {
            this.map = map;
        }
    }
}
