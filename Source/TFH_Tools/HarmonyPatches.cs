namespace TFH_Tools
{
    using System.Reflection;

    using Harmony;

    using RimWorld;

    using TFH_VehicleBase;

    using Verse;
    using Verse.AI;

    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("com.toolsforhaul.rimworld.mod.Tools");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // Log.Message("Tools For Haul: Adding Harmony Patches.");
            //
            // harmony.Patch(
            // AccessTools.Method(typeof(Pawn), nameof(Pawn.ThreatDisabled)),
            //     null,
            // new HarmonyMethod(typeof(HarmonyPatches), nameof(ThreatDisabled_Postfix)));
            //
            // harmony.Patch(
            //     AccessTools.Method(typeof(Pawn), nameof(Pawn.ExitMap)),
            //     null,
            //     new HarmonyMethod(typeof(HarmonyPatches), nameof(ExitMap_Postfix)));
            //
            // harmony.Patch(
            //     AccessTools.Method(typeof(Pawn), nameof(Pawn.SpawnSetup)),
            //     null,
            //     new HarmonyMethod(typeof(HarmonyPatches), nameof(SpawnSetup_Postfix)));



        }
        [HarmonyPatch(typeof(ThinkNode_JobGiver))]
        [HarmonyPatch("TryIssueJobPackage")]
        [HarmonyPatch(new[] { typeof(Pawn), typeof(JobIssueParams) })]
        public static class ThinkNode_JobGiver_Patch
        {

            [HarmonyPostfix]
            public static void ThinkNode_JobGiver_Postfix(ref ThinkResult __result, ThinkNode_JobGiver __instance, Pawn pawn, JobIssueParams jobParams)
            {
                __result = VehicleThinker.VehicleThinkResult(__result, __instance, pawn);
            }
        }


        [HarmonyPatch(typeof(ThinkNode_Priority))]
        [HarmonyPatch("TryIssueJobPackage")]
        [HarmonyPatch(new[] { typeof(Pawn), typeof(JobIssueParams) })]
        public static class ThinkNode_Priority_Patch
        {
            [HarmonyPostfix]
            public static void ThinkNode_Priority_Postfix(ref ThinkResult __result, ThinkNode_JobGiver __instance, Pawn pawn, JobIssueParams jobParams)
            {
                __result = VehicleThinker.VehicleThinkResult(__result, __instance, pawn);
            }
        }
        //  [Detour(typeof(ThinkNode_JobGiver), bindingFlags = BindingFlags.Instance | BindingFlags.Public)]
        public static ThinkResult VehicleThinkResult(ThinkResult __result, ThinkNode_JobGiver __instance, Pawn pawn)
        {
            if (__result.Job == null)
            {
                return __result;
            }

            Job job = __result.Job;

            if (!pawn.RaceProps.Humanlike || !pawn.RaceProps.IsFlesh)
            {
                return __result;
            }

            bool jobNull = job == null;
            ThinkResult result;

            Apparel_ToolBelt toolbelt = ToolsForHaulUtility.TryGetToolbelt(pawn);
            if (toolbelt != null)
            {
                if (MapComponent_ToolsForHaul.PreviousPawnWeapon.ContainsKey(pawn) && MapComponent_ToolsForHaul.PreviousPawnWeapon[pawn] != null)
                {
                    Pawn wearer = toolbelt.Wearer;
                    ThingWithComps previousWeapon = MapComponent_ToolsForHaul.PreviousPawnWeapon[pawn];
                    if (previousWeapon != null && toolbelt.slotsComp.slots.Contains(previousWeapon))
                    {
                        for (int i = toolbelt.slotsComp.slots.Count-1; i >=0 ; i--)
                        {
                            var thing = toolbelt.slotsComp.slots[i];
                            ThingWithComps item = (ThingWithComps)thing;
                            if (item == previousWeapon)
                            {
                                if (wearer.equipment.Primary != null)
                                {
                                    toolbelt.slotsComp.SwapEquipment(item);
                                }
                                else
                                {
                                    wearer.equipment.AddEquipment(item);
                                    toolbelt.slotsComp.slots.Remove(item);
                                }
                                break;
                            }
                        }
                    }
                }
                MapComponent_ToolsForHaul.PreviousPawnWeapon[pawn] = null;
            }

            if (pawn.mindState.IsIdle)
            {
                if (ToolsForHaulUtility.IsDriver(pawn))
                {
                    job = TFH_BaseUtility.DismountAtParkingLot(pawn,"VehicleThinkResult", MapComponent_ToolsForHaul.currentVehicle[pawn]);
                }
            }




            if (jobNull)
            {
                result = ThinkResult.NoJob;
            }
            else
            {
                if (pawn.Faction == Faction.OfPlayer && pawn.RaceProps.Humanlike && pawn.RaceProps.IsFlesh)
                {
                    if (job.def == JobDefOf.DoBill)
                    {
                        RightTools.EquipRigthTool(pawn, job.RecipeDef.workSpeedStat);
                    }

                    if (job.def == JobDefOf.FinishFrame || job.def == JobDefOf.Deconstruct || job.def == JobDefOf.Repair || job.def == JobDefOf.BuildRoof || job.def == JobDefOf.RemoveRoof || job.def == JobDefOf.RemoveFloor)
                    {
                        RightTools.EquipRigthTool(pawn, StatDefOf.ConstructionSpeed);
                    }
                    if (job.def == JobDefOf.SmoothFloor)
                    {
                        RightTools.EquipRigthTool(pawn, StatDefOf.SmoothingSpeed);
                    }

                    if (job.def == JobDefOf.Harvest)
                    {
                        RightTools.EquipRigthTool(pawn, StatDefOf.PlantHarvestYield);
                    }
                    if (job.def == JobDefOf.CutPlant || job.def == JobDefOf.Sow)
                    {
                        RightTools.EquipRigthTool(pawn, StatDefOf.PlantWorkSpeed);
                    }

                    if (job.def == JobDefOf.Mine)
                    {
                        RightTools.EquipRigthTool(pawn, StatDefOf.MiningSpeed);
                    }




                }

                result = new ThinkResult(job, __instance, null);
            }

            return result;
        }
    }
}
