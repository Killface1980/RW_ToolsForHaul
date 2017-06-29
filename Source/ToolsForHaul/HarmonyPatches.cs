namespace ToolsForHaul
{
    using System.Reflection;

    using Harmony;

    using RimWorld;

    using ToolsForHaul.Vehicles;

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

    [HarmonyPatch(typeof(ListerBuildingsRepairable), "Notify_BuildingRepaired")]
    static class Notify_VehicleRepaired_Postfix
    {
        [HarmonyPostfix]
        public static void Notify_VehicleRepaired(Building b)
        {
            var cart = b as Vehicle_Cart;
            if (cart== null)
                return;

            if (cart.VehicleComp.tankLeaking)
            {
                cart.VehicleComp.tankLeaking = false;
                cart.VehicleComp._tankHitPos = 1f;
            }
        }
    }
}
