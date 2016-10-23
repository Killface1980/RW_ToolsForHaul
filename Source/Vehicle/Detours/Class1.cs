using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;

namespace ToolsForHaul.Detoured
{
    public class _Pawn_ApparelTracker
    {
        public bool _TryDrop(Apparel ap, out Apparel resultingAp)
        {
            
            return ap.wearer.apparel.TryDrop(ap, out resultingAp, ap.wearer.Position, true);
        }
    }
}
