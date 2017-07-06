using System;
using System.Linq;

using RimWorld;
using Verse;
using Verse.AI;

using Harmony;

namespace ToolsForHaul
{
    using System.Collections.Generic;

    using ToolsForHaul.Components;
    using ToolsForHaul.Defs;
    using ToolsForHaul.Utilities;
    using ToolsForHaul.Vehicles;

    [HarmonyPatch(typeof(ThinkNode_JobGiver))]
    [HarmonyPatch("TryIssueJobPackage")]
    [HarmonyPatch(new Type[] { typeof(Pawn), typeof(JobIssueParams) })]
    public static class ThinkNode_JobGiver_Patch
    {

        [HarmonyPostfix]
        public static void ThinkNode_JobGiver_Postfix(ref ThinkResult __result, ThinkNode_JobGiver __instance, Pawn pawn, JobIssueParams jobParams)
        {
            VehicleThinker.VehicleThinkResult(ref __result, __instance, pawn);
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
            VehicleThinker.VehicleThinkResult(ref __result, __instance, pawn);
        }
    }

    public static class VehicleThinker
    {
        private static float vehicleSearchRadius = 12f;

        public static void VehicleThinkResult(ref ThinkResult __result, ThinkNode_JobGiver __instance, Pawn pawn)
        {
            Job job = null;

            Job requestJob = __result.Job;

            if (requestJob == null)
            {
                return;
            }

            if (pawn == null || !pawn.RaceProps.Humanlike || !pawn.RaceProps.IsFlesh)
            {
                return;
            }

            if (pawn.Faction.IsPlayer)
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
                                TFH_Utility.GetRightVehicle(pawn, availableVehicles, WorkTypeDefOf.Construction);

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
                            Vehicle_Cart vehicle = TFH_Utility.GetRightVehicle(pawn, availableVehicles, WorkTypeDefOf.Hunting);
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
                            Vehicle_Cart vehicle = TFH_Utility.GetRightVehicle(pawn, availableVehicles, WorkTypeDefOf.Doctor);
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

                if (!pawn.IsDriver())
                {
                    if (requestJob.def == JobDefOf.Flee || requestJob.def == JobDefOf.FleeAndCower
                        || requestJob.def == JobDefOf.Steal || requestJob.def == JobDefOf.Kidnap
                        || requestJob.def == JobDefOf.CarryDownedPawnToExit || requestJob.def == JobDefOf.WaitCombat
                        || requestJob.def == JobDefOf.AttackMelee || requestJob.def == JobDefOf.AttackStatic
                        || requestJob.def == JobDefOf.Goto && pawn.CurJob.exitMapOnArrival)
                    {
                        List<Thing> availableVehicles;

                        if (pawn.Faction.HostileTo(Faction.OfPlayer))
                        {
                            availableVehicles = pawn.AvailableVehiclesForSteeling(vehicleSearchRadius);
                        }
                        else
                        {
                            availableVehicles = pawn.AvailableVehiclesForAllFactions(vehicleSearchRadius);
                        }

                        if (!availableVehicles.NullOrEmpty())
                        {
                            job = new Job(HaulJobDefOf.Mount) { targetA = availableVehicles.FirstOrDefault(), };
                        }
                    }
                }
            }

            if (job != null)
            {
                Log.Message("Thinknode accessed " + pawn + "\nNew thinknode, " + __result.Job + " -> " + job);
                __result = new ThinkResult(job, __instance, null);
            }
        }

        private static Job MountOnOrReturnVehicle(Pawn pawn, Job job, Vehicle_Cart cart)
        {
            if (!pawn.IsDriver())
            {
                job = new Job(HaulJobDefOf.Mount)
                {
                    targetA = cart
                };
            }
            else
            {
                if (!TFH_Utility.IsDriverOfThisVehicle(pawn, cart))
                {
                    job = pawn.DismountAtParkingLot("TNJ 99");
                }
            }

            return job;
        }

    }

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
}