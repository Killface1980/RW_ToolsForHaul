namespace TFH_Tools.Components
{
    using System.Collections.Generic;

    using Verse;

    public class CompSlotsBackpack_Properties : CompProperties
    {
        public List<ThingCategoryDef> allowedThingCategoryDefs = new List<ThingCategoryDef>();

        public List<ThingCategoryDef> forbiddenSubThingCategoryDefs = new List<ThingCategoryDef>();

        // public int maxSlots = 3;
        public CompSlotsBackpack_Properties()
        {
            this.compClass = typeof(CompSlotsBackpack);
        }
    }
}