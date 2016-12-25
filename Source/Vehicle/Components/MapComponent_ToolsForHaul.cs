using System.Collections.Generic;
using Verse;

namespace ToolsForHaul
{
    using HugsLib.Core;
    using HugsLib.Utils;
    public class MapComponent_ToolsForHaul : UtilityWorldObject
    {


        public static Dictionary<Pawn, ThingWithComps> previousPawnWeapons = new Dictionary<Pawn, ThingWithComps>();
        public static List<Thing> AutoInventory = new List<Thing>();

        public static Dictionary<Pawn, Thing> currentVehicle = new Dictionary<Pawn, Thing>();


        public override void ExposeData()
        {
            Scribe_Collections.LookDictionary(ref previousPawnWeapons, "previousPawnWeapons", LookMode.Reference, LookMode.Reference);
            Scribe_Collections.LookDictionary(ref currentVehicle, "currentVehicle", LookMode.Reference, LookMode.Reference);
            Scribe_Collections.LookList(ref AutoInventory, "AutoInventory", LookMode.Reference);
        }
    }
}
