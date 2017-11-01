namespace TFH_Tools.Components
{
    using System.Collections.Generic;

    using Verse;

    public class CompSlotsToolbelt_Properties : CompProperties
    {
        public List<ThingCategoryDef> allowedThingCategoryDefs = new List<ThingCategoryDef>();
        public List<ThingCategoryDef> forbiddenSubThingCategoryDefs = new List<ThingCategoryDef>();



        public CompSlotsToolbelt_Properties()
        {
            this.compClass = typeof(CompSlotsToolbelt);
        }
    }
}