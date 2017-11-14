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

          //  harmony.Patch(
          //      AccessTools.Method(
          //          typeof(Verse.AI.Job),
          //          nameof(Verse.AI.Job.JobIsSameAs)),
          //      null,
          //      new HarmonyMethod(typeof(HarmonyPatches), nameof(JobIsSameAs)),
          //      null);
           
         //   harmony.Patch(
         //       AccessTools.Method(
         //           typeof(Pawn_InventoryTracker),
         //           nameof(Pawn_InventoryTracker.InventoryTrackerTick)),
         //       new HarmonyMethod(typeof(HarmonyPatches), nameof(ThingOwnerTick)),
         //       null);
         //  
         //   harmony.Patch(
         //       AccessTools.Method(
         //           typeof(Pawn_InventoryTracker),
         //           nameof(Pawn_InventoryTracker.InventoryTrackerTickRare)),
         //       new HarmonyMethod(typeof(HarmonyPatches), nameof(ThingOwnerTickRare)),
         //       null);
        }

      //  private static void JobIsSameAs(Verse.AI.Job __instance, ref bool __result, Job other)
      //  {
      //      if (__instance == other)
      //      {
      //          if (__instance.def == HaulJobDefOf.HaulWithBackpack)
      //          {
      //              __result = true;
      //          }
      //      }
      //  }
        private static void ThingOwnerTick(Pawn_InventoryTracker __instance)
        {
            Apparel_Backpack backpack = __instance.pawn.TryGetBackpack();
            backpack?.slotsComp.InventoryTrackerTick();
        }
       
        private static void ThingOwnerTickRare(Pawn_InventoryTracker __instance)
        {
            Apparel_Backpack backpack = __instance.pawn.TryGetBackpack();
            backpack?.slotsComp.InventoryTrackerTickRare();
        }
    }
}
