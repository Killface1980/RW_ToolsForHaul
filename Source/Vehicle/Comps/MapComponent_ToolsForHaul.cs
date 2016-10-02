using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ToolsForHaul
{

    public class MapComponent_ToolsForHaul : MapComponent
    {


        public static Dictionary<Pawn, ThingWithComps> wasAutoEquipped = new Dictionary<Pawn, ThingWithComps>();



        public override void ExposeData()
        {
            Scribe_Collections.LookDictionary(ref wasAutoEquipped, "wasAutoEquipped", LookMode.Deep,LookMode.Deep);
        }
    }
}
