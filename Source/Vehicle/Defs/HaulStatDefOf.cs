using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace ToolsForHaul.StatDefs
{
    public class HaulStatDefOf
    {
        public static readonly StatDef VehicleMaxItem = DefDatabase<StatDef>.GetNamed("VehicleMaxItem");
        public static readonly StatDef VehicleSpeed = DefDatabase<StatDef>.GetNamed("VehicleSpeed");
        public static readonly StatDef MaxItem = DefDatabase<StatDef>.GetNamed("TFHMaxItem");

    }
}
