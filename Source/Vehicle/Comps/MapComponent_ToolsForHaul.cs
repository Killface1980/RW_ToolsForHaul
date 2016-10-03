using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ToolsForHaul
{

    public class MapComponent_ToolsForHaul : MapComponent
    {


        public static Dictionary<Pawn, ThingWithComps> wasAutoEquipped = new Dictionary<Pawn, ThingWithComps>();
        public static Dictionary<ThingWithComps, Pawn> AutoInventory = new Dictionary<ThingWithComps, Pawn>();



        public override void ExposeData()
        {
            Scribe_Collections.LookDictionary(ref wasAutoEquipped, "wasAutoEquipped", LookMode.Value,LookMode.DefReference);
            Scribe_Collections.LookDictionary(ref AutoInventory, "AutoInventory", LookMode.DefReference, LookMode.Value);

        }
    }
}
