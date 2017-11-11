using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TFH_Tools.Components
{
    using Verse;
    public class CompInventory : ThingComp
    {
        public Pawn_InventoryTracker2 tracker;

        public CompInventory()
        {
            this.tracker = new Pawn_InventoryTracker2(this.parent as Pawn);
        }
    }
}
