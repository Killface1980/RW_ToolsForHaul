namespace TFH_Tools
{
    using System;
    using System.Linq;
    using System.Reflection;

    using Harmony;

    using RimWorld;

    using TFH_Tools.Components;

    using TFH_VehicleBase;

    using Verse;
    using Verse.AI;

    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("com.toolsforhaul.rimworld.mod.tools");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            harmony.Patch(
                AccessTools.Method(
                    typeof(Pawn_ApparelTracker),
                    nameof(Pawn_ApparelTracker.TryDrop),
                    new[] { typeof(Apparel), typeof(Apparel), typeof(IntVec3), typeof(bool) }),
                new HarmonyMethod(typeof(HarmonyPatches), nameof(TryDrop_Prefix)),
                null);
        }

        private static void TryDrop_Prefix(Apparel ap)
        {
            Log.Message("DEBUG: Trydrop detours working.");
            var bp = ap as Apparel_Backpack;
            var tb = ap as Apparel_ToolBelt;
            bp?.slotsComp.innerContainer.TryDropAll(bp.Wearer.Position, bp.Wearer.Map, ThingPlaceMode.Near);
            tb?.slotsComp.innerContainer.TryDropAll(tb.Wearer.Position, tb.Wearer.Map, ThingPlaceMode.Near);
        }
    }
}
