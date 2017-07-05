namespace ToolsForHaul
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    using Harmony;

    using RimWorld;

    using ToolsForHaul.Components;
    using ToolsForHaul.Utilities;
    using ToolsForHaul.Vehicles;

    using UnityEngine;

    using Verse;
    using Verse.AI;

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

    /*
     [HarmonyPatch(typeof(ListerBuildingsRepairable), "Notify_BuildingRepaired")]
     static class Notify_VehicleRepaired_Postfix
     {
         [HarmonyPostfix]
         public static void Notify_VehicleRepaired(Building b)
         {
             var cart = (Vehicle_Cart)b;
             if (cart == null)
                 return;
    
             if (!cart.HasGasTank())
                 return;
             if (cart.GasTankComp.tankLeaking)
             {
                 cart.GasTankComp.tankLeaking = false;
                 cart.GasTankComp._tankHitPos = 1f;
             }
         }
     }
    */


    // Alloes Ordered Attacks
    [HarmonyPatch(typeof(Targeter), "OrderVerbForceTarget")]
    static class Targeter_Postfix
    {
        private static LocalTargetInfo target;

     // [HarmonyPostfix]
     // public static void Targeter(Targeter __instance)
     // {
     //     if (!__instance.targetingVerb.CasterIsPawn)
     //     {
     //         int numSelected = Find.Selector.NumSelected;
     //         List<object> selectedObjects = Find.Selector.SelectedObjects;
     //         for (int j = 0; j < numSelected; j++)
     //         {
     //             Vehicle_CartTurretGun cartTurretGun = selectedObjects[j] as Vehicle_CartTurretGun;
     //             if (cartTurretGun != null && cartTurretGun.Map == Find.VisibleMap)
     //             {
     //                 LocalTargetInfo targ = CurrentTargetUnderMouse(__instance, true);
     //                 cartTurretGun.OrderAttack(targ);
     //             }
     //         }
     //     }
     // }

        // RimWorld.Targeter
        private static LocalTargetInfo CurrentTargetUnderMouse(Targeter __instance, bool mustBeHittableNowIfNotMelee)
        {
            if (!__instance.IsTargeting)
            {
                return LocalTargetInfo.Invalid;
            }
            TargetingParameters clickParams = __instance.targetingVerb.verbProps.targetParams;
            LocalTargetInfo localTargetInfo = LocalTargetInfo.Invalid;
            using (IEnumerator<LocalTargetInfo> enumerator = GenUI.TargetsAtMouse(clickParams, false).GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    LocalTargetInfo current = enumerator.Current;
                    localTargetInfo = current;
                }
            }
            if (localTargetInfo.IsValid && mustBeHittableNowIfNotMelee && !(localTargetInfo.Thing is Pawn) && __instance.targetingVerb != null && !__instance.targetingVerb.verbProps.MeleeRange)
            {
                if (!__instance.targetingVerb.CanHitTarget(localTargetInfo))
                {
                    localTargetInfo = LocalTargetInfo.Invalid;
                }
            }
            return localTargetInfo;
        }

    }


    // Faster movement for vehicles
    [HarmonyPatch(typeof(Pawn_PathFollower), "SetupMoveIntoNextCell")]
    static class SetupMoveIntoNextCell_Postfix
    {
        static FieldInfo PawnField = AccessTools.Field(typeof(Pawn_PathFollower), "pawn");

        [HarmonyPostfix]
        public static void SetupMoveIntoNextCell(Pawn_PathFollower __instance)
        {
            Pawn pawn = (Pawn)PawnField?.GetValue(__instance);
            //Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();

            Vehicle_Cart cart = pawn.MountedVehicle();
            if (cart != null)
            {

                // TODO create own formula, wheel size??
                //      Log.Message("Old cell cost: " + +__instance.nextCellCostLeft + " / " + __instance.nextCellCostTotal);
                float newCost = Mathf.Min(__instance.nextCellCostTotal, 20f);

                __instance.nextCellCostTotal = newCost;
                __instance.nextCellCostLeft = newCost;
            }

            //  int num;
            //  if (c.x == this.pawn.Position.x || c.z == this.pawn.Position.z)
            //  {
            //      num = this.pawn.TicksPerMoveCardinal;
            //  }
            //  else
            //  {
            //      num = this.pawn.TicksPerMoveDiagonal;
            //  }
            //  num += this.pawn.Map.pathGrid.CalculatedCostAt(c, false, this.pawn.Position);
            //  Building edifice = c.GetEdifice(this.pawn.Map);
            //  if (edifice != null)
            //  {
            //      num += (int)edifice.PathWalkCostFor(this.pawn);
            //  }
            //  if (num > 450)
            //  {
            //      num = 450;
            //  }
            //  if (this.pawn.jobs.curJob != null)
            //  {
            //      switch (this.pawn.jobs.curJob.locomotionUrgency)
            //      {
            //          case LocomotionUrgency.Amble:
            //              num *= 3;
            //              if (num < 60)
            //              {
            //                  num = 60;
            //              }
            //              break;
            //          case LocomotionUrgency.Walk:
            //              num *= 2;
            //              if (num < 50)
            //              {
            //                  num = 50;
            //              }
            //              break;
            //          case LocomotionUrgency.Jog:
            //              num *= 1;
            //              break;
            //          case LocomotionUrgency.Sprint:
            //              num = Mathf.RoundToInt((float)num * 0.75f);
            //              break;
            //      }
            //  }
            //  return Mathf.Max(num, 1);
        }
    }


}
