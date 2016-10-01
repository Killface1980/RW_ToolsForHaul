using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ToolsForHaul
{

    public class MapComponent_FacialStuff : MapComponent
    {


        public static Dictionary<Pawn, ThingWithComps> wasAutoEquipped = new Dictionary<Pawn, ThingWithComps>();



        public override void ExposeData()
        {
            Scribe_Collections.LookDictionary(ref wasAutoEquipped, "Pawns", LookMode.Deep);
        }
    }
}
