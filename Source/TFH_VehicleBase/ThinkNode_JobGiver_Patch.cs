namespace TFH_VehicleBase
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Harmony;

    using RimWorld;

    using TFH_VehicleBase.Recipes;

    using Verse;
    using Verse.AI;

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
                    if (pawn.IsDriver())
                    {
                        if (requestJob.def == JobDefOf.LayDown || requestJob.def == JobDefOf.WaitWander
                            || requestJob.def == JobDefOf.EnterCryptosleepCasket || requestJob.def == JobDefOf.FeedPatient
                            || requestJob.def == JobDefOf.TendPatient)
                        {
                            job = pawn.DismountAtParkingLot("TN #1a");
                        }
                    }
                    else
                    {
                        if (requestJob.def == JobDefOf.FinishFrame || requestJob.def == JobDefOf.Deconstruct
                            || requestJob.def == JobDefOf.Repair || requestJob.def == JobDefOf.BuildRoof
                            || requestJob.def == JobDefOf.RemoveRoof || requestJob.def == JobDefOf.RemoveFloor)
                        {
                            List<Thing> availableVehicles = pawn.AvailableVehicles();
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
                            List<Thing> availableVehicles = pawn.AvailableVehicles();
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

                        if (requestJob.def == JobDefOf.Capture || requestJob.def == JobDefOf.Rescue)
                        {
                            List<Thing> availableVehicles = pawn.AvailableVehicles();
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

                if (!pawn.IsDriver())
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
            if (!pawn.IsDriver())
            {
                job = new Job(VehicleJobDefOf.Mount)
                {
                    targetA = cart
                };
            }
            else
            {
                if (!pawn.IsDriverOfThisVehicle(cart))
                {
                    job = pawn.DismountAtParkingLot("TNJ 99");
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

    [HarmonyPatch(typeof(Recipe_VehicleSurgery), "CheckSurgeryFail")]
    public static class CheckSurgeryFail_Patch
    {
        [HarmonyPostfix]
        public static void CheckSurgeryFail_Postfix(ref bool __result, Recipe_VehicleSurgery __instance, Pawn surgeon, Pawn patient, List<Thing> ingredients, BodyPartRecord part)
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