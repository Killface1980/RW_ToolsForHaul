using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ToolsForHaul
{
    using System.Reflection;

    using Harmony;

    using Verse;

    [StaticConstructorOnStartup]
    class HarmonyPatches
    {

        static HarmonyPatches()
        {
            var harmony = HarmonyInstance.Create("com.toolsforhaul.rimworld.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.Message("Tools For Haul: Adding Harmony Patches.");

        }
    }
}
