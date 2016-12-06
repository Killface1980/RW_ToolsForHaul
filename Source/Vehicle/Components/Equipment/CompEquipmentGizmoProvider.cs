using System.Linq;
using RimWorld;
using Verse;

namespace ToolsForHaul.Components
{
    public class CompEquipmentGizmoProvider : ThingComp
    {
        public Pawn owner;

        public bool ParentIsEquipped
        {
            get
            {
                if (this.owner != null
                    && (this.owner.equipment.AllEquipment.Contains(this.parent) || this.owner.apparel.WornApparel.Contains(this.parent as Apparel))) return true;

                return false;
            }
        }
    }
}