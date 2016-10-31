using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace CommunityCoreLibrary
{

    public abstract class DefInjectionQualifierTemp
    {
        public abstract bool Test(Def def);

        public static List<ThingDef> FilteredThingDefs(Type qualifier, ref DefInjectionQualifierTemp QualifierTempInt, List<string> targetDefs)
        {
            if (!targetDefs.NullOrEmpty())
            {
                return DefDatabase<ThingDef>.AllDefs.Where(def => targetDefs.Contains(def.defName)).ToList();
            }
            if (QualifierTempInt == null)
            {
                QualifierTempInt = (DefInjectionQualifierTemp)Activator.CreateInstance(qualifier);
                if (QualifierTempInt == null)
                {
                    return null;
                }
            }
            return DefDatabase<ThingDef>.AllDefs.Where(QualifierTempInt.Test).ToList();
        }

    }

}