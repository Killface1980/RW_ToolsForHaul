using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ToolsForHaul
{

    public class MapComponent_ToolsForHaul : MapComponent
    {


        public static Dictionary<Pawn, ThingWithComps> previousPawnWeapons = new Dictionary<Pawn, ThingWithComps>();
        public static List<Thing> AutoInventory = new List<Thing>();



        public override void ExposeData()
        {
            Scribe_Collections.LookDictionary(ref previousPawnWeapons, "previousPawnWeapons", LookMode.MapReference,LookMode.MapReference);
            Scribe_Collections.LookList(ref AutoInventory, "AutoInventory", LookMode.DefReference);

        }
    }
}
