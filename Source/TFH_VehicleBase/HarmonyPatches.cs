namespace TFH_VehicleBase
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Harmony;

    using RimWorld;

    using TFH_VehicleBase.DefOfs_TFH;

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

            if (pawn.IsDriver(out Vehicle_Cart cart))
            {

                // TODO create own formula, wheel size??
                //      Log.Message("Old cell cost: " + +__instance.nextCellCostLeft + " / " + __instance.nextCellCostTotal);
                float newCost = Mathf.Min(__instance.nextCellCostTotal, 15f);

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

    [HarmonyPatch(typeof(ThinkNode_JobGiver))]
    [HarmonyPatch("TryIssueJobPackage")]
    [HarmonyPatch(new Type[] { typeof(Pawn), typeof(JobIssueParams) })]
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
    [HarmonyPatch(new Type[] { typeof(Pawn), typeof(JobIssueParams) })]
    public static class ThinkNode_Priority_Patch
    {
        [HarmonyPostfix]
        public static void ThinkNode_Priority_Postfix(ref ThinkResult __result, ThinkNode_JobGiver __instance, Pawn pawn, JobIssueParams jobParams)
        {
            __result = VehicleThinker.VehicleThinkResult(__result, __instance, pawn);
        }
    }

    public static class VehicleThinker
    {
        private static Type PawnRendererType;
        private static MethodInfo PawnFieldInfo;
        private static float vehicleSearchRadius = 12f;

        private static void GetReflections()
        {
            if (PawnRendererType == null)
            {
                PawnRendererType = typeof(ThinkNode_JobGiver);
                PawnFieldInfo = PawnRendererType.GetMethod("TryGiveJob", BindingFlags.NonPublic | BindingFlags.Instance);
            }
        }

        public static ThinkResult VehicleThinkResult(ThinkResult __result, ThinkNode_JobGiver __instance, Pawn pawn)
        {

            Job job = null;

            if (__result.Job == null)
            {
                return __result;
            }

            Job requestJob = __result.Job;

            if (!pawn.RaceProps.Humanlike || !pawn.RaceProps.IsFlesh)
            {
                return __result;
            }

            if (pawn.Faction == Faction.OfPlayer)
            {
                if (!pawn.Drafted)
                {
                    // if (job.def == JobDefOf.LayDown || job.def == JobDefOf.Arrest || job.def == JobDefOf.DeliverFood
                    //     || job.def == JobDefOf.EnterCryptosleepCasket || job.def == JobDefOf.EnterTransporter
                    //     || job.def == JobDefOf.Ingest || job.def == JobDefOf.ManTurret
                    //     || job.def == JobDefOf.Slaughter || job.def == JobDefOf.VisitSickPawn
                    //     || job.def == JobDefOf.WaitWander || job.def == JobDefOf.DoBill)
                    // {
                    //     if (pawn.IsDriver())
                    //     {
                    //         job = pawn.DismountAtParkingLot("TN #1");
                    //         newjob = true;
                    //     }
                    // }
                    if (pawn.IsDriver(out Vehicle_Cart drivenCart))
                    {
                        if (requestJob.def == JobDefOf.LayDown || requestJob.def == JobDefOf.WaitWander
                            || requestJob.def == JobDefOf.EnterCryptosleepCasket || requestJob.def == JobDefOf.FeedPatient
                            || requestJob.def == JobDefOf.TendPatient)
                        {
                            job = pawn.DismountAtParkingLot("TN #1a", drivenCart);
                        }
                    }
                    else
                    {
                        if (requestJob.def == JobDefOf.FinishFrame || requestJob.def == JobDefOf.Deconstruct
                            || requestJob.def == JobDefOf.Repair || requestJob.def == JobDefOf.BuildRoof
                            || requestJob.def == JobDefOf.RemoveRoof || requestJob.def == JobDefOf.RemoveFloor)
                        {
                            pawn.AvailableVehicles(out List<Thing> availableVehicles);
                            Vehicle_Cart vehicle =
                                TFH_BaseUtility.GetRightVehicle(pawn, availableVehicles, WorkTypeDefOf.Construction);

                            if (vehicle != null)
                            {
                                if (pawn.Position.DistanceToSquared(vehicle.Position)
                                    < pawn.Position.DistanceToSquared(requestJob.targetA.Cell))
                                {
                                    job = MountOnOrReturnVehicle(pawn, requestJob, vehicle);
                                }
                            }
                        }
                        if (requestJob.def == JobDefOf.Hunt)
                        {
                            pawn.AvailableVehicles(out List<Thing> availableVehicles);

                            Vehicle_Cart vehicle = TFH_BaseUtility.GetRightVehicle(pawn, availableVehicles, WorkTypeDefOf.Hunting);
                            {
                                if (vehicle != null)
                                {
                                    if (pawn.Position.DistanceToSquared(vehicle.Position)
                                        < pawn.Position.DistanceToSquared(requestJob.targetA.Cell))
                                    {
                                        job = MountOnOrReturnVehicle(pawn, requestJob, vehicle);
                                    }
                                }
                            }
                        }

                        if (requestJob.def == JobDefOf.Capture)
                        {
                            pawn.AvailableVehicles(out List<Thing> availableVehicles);
                            Vehicle_Cart vehicle = TFH_BaseUtility.GetRightVehicle(pawn, availableVehicles, WorkTypeDefOf.Doctor);
                            if (vehicle != null)
                            {
                                if (pawn.Position.DistanceToSquared(vehicle.Position)
                                    < pawn.Position.DistanceToSquared(requestJob.targetA.Cell))
                                {
                                    job = MountOnOrReturnVehicle(pawn, requestJob, vehicle);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // Enemies & other

                //    Log.Message("Non-player faction");

                if (!pawn.IsDriver(out Vehicle_Cart drivenCart))
                {
                    // Log.Message("no driver");
                    // Log.Message("job " + __result + " - " + requestJob);

                    if (requestJob.def == JobDefOf.Flee || requestJob.def == JobDefOf.FleeAndCower
                        || requestJob.def == JobDefOf.Steal || requestJob.def == JobDefOf.Kidnap
                        || requestJob.def == JobDefOf.CarryDownedPawnToExit || requestJob.def == JobDefOf.WaitCombat
                        || requestJob.def == JobDefOf.AttackMelee || requestJob.def == JobDefOf.AttackStatic
                        || requestJob.def == JobDefOf.Goto && (requestJob.exitMapOnArrival || pawn.Position.InNoBuildEdgeArea(pawn.Map)))
                    {
                        //   Log.Message("job " + requestJob.def);
                        List<Thing> availableVehicles;

                        if (pawn.Faction.HostileTo(Faction.OfPlayer))
                        {
                            availableVehicles = pawn.AvailableVehiclesForSteeling(vehicleSearchRadius);
                        }
                        else
                        {
                            availableVehicles = pawn.AvailableVehiclesForAllFactions(vehicleSearchRadius);
                        }
                        //      Log.Message("vehicles " + availableVehicles.ToList());

                        if (!availableVehicles.NullOrEmpty())
                        {
                            var cart = availableVehicles.FirstOrDefault();
                            job = new Job(VehicleJobDefOf.Mount) { targetA = cart };
                        }
                    }
                }
            }

            if (job != null)
            {
                Log.Message("Thinknode accessed " + pawn + "\nNew thinknode, " + __result.Job + " -> " + job);
                __result = new ThinkResult(job, __instance, null);
            }

            return __result;
        }

        private static Job MountOnOrReturnVehicle(Pawn pawn, Job job, Vehicle_Cart cart)
        {
            if (!pawn.IsDriver(out Vehicle_Cart drivenCart))
            {
                job = new Job(VehicleJobDefOf.Mount)
                {
                    targetA = cart
                };
            }
            else
            {
                if (!pawn.IsDriver(out drivenCart, cart))
                {
                    job = pawn.DismountAtParkingLot("TNJ 99", drivenCart);
                }
            }

            return job;
        }

    }

    // Makes vehicles available for recipes - medical
    [HarmonyPatch(typeof(Pawn), "CurrentlyUsable")]
    public static class CurrentlyUsable_Patch
    {
        [HarmonyPostfix]
        public static void CurrentlyUsable_Postfix(ref bool __result, Pawn __instance)
        {
            Vehicle_Cart vehicleCart = __instance as Vehicle_Cart;
            if (vehicleCart == null)
            {
                return;
            }
            if (vehicleCart.InParkingLot)
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(Recipe_Surgery), "CheckSurgeryFail")]
    public static class CheckSurgeryFail_Patch
    {
        [HarmonyPostfix]
        public static void CheckSurgeryFail_Postfix(ref bool __result, Recipe_Surgery __instance, Pawn surgeon, Pawn patient, List<Thing> ingredients, BodyPartRecord part)
        {
            Vehicle_Cart vehicleCart = patient as Vehicle_Cart;
            if (vehicleCart == null)
            {
                return;
            }
            //  if (vehicleCart.InParkingLot)
            {
                __result = false;
            }
        }
    }
}
