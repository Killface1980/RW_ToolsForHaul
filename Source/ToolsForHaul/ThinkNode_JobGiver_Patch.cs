using System;
using System.Linq;

using RimWorld;
using Verse;
using Verse.AI;

using Harmony;

namespace ToolsForHaul
{
    using System.Collections.Generic;

    using ToolsForHaul.Defs;
    using ToolsForHaul.Utilities;

    [HarmonyPatch(typeof(ThinkNode_JobGiver))]
    [HarmonyPatch("TryIssueJobPackage")]
    [HarmonyPatch(new Type[] { typeof(Pawn), typeof(JobIssueParams) })]
    public static class ThinkNode_JobGiver_Patch
    {
        private static float vehicleSearchRadius = 20f;

        static void Postfix(Pawn pawn, JobIssueParams jobParams)
        {
            Job job = pawn.CurJob;
            Job oldJob = pawn.CurJob;
            bool newjob = false;

            if (pawn != null && pawn.RaceProps.Humanlike && pawn.RaceProps.IsFlesh)
            {
                if (pawn.Faction.IsPlayer)
                {
                    if (job != null)
                    {

                        if (job.def == JobDefOf.LayDown || job.def == JobDefOf.Arrest || job.def == JobDefOf.DeliverFood
                            || job.def == JobDefOf.EnterCryptosleepCasket || job.def == JobDefOf.EnterTransporter
                            || job.def == JobDefOf.Ingest || job.def == JobDefOf.ManTurret
                            || job.def == JobDefOf.Slaughter || job.def == JobDefOf.VisitSickPawn
                            || job.def == JobDefOf.WaitWander || job.def == JobDefOf.DoBill)
                        {
                            if (pawn.IsDriver())
                            {
                                job = pawn.DismountAtParkingLot(pawn.MountedVehicle(), "TN #1");
                                newjob = true;
                            }
                        }

                        if (job.def == JobDefOf.FinishFrame || job.def == JobDefOf.Deconstruct || job.def == JobDefOf.Repair || job.def == JobDefOf.BuildRoof || job.def == JobDefOf.RemoveRoof || job.def == JobDefOf.RemoveFloor)
                        {
                            List<Thing> availableVehicles = pawn.AvailableVehicles();
                            if (availableVehicles.Count > 0 || availableVehicles.Count > 0)
                            {
                                Thing vehicle = TFH_Utility.GetRightVehicle(pawn, availableVehicles, WorkTypeDefOf.Construction);
                                if (vehicle != null && pawn.Position.DistanceToSquared(vehicle.Position)
                                    < pawn.Position.DistanceToSquared(job.targetA.Cell))
                                {
                                    job = GetVehicle(pawn, job, WorkTypeDefOf.Construction);
                                    newjob = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (pawn.IsDriver() && !pawn.Drafted && pawn.mindState != null && pawn.mindState.IsIdle)
                        {
                            job = pawn.DismountAtParkingLot(pawn.MountedVehicle(), "TN #1a");
                            newjob = true;
                        }
                    }
                }
                else
                {
                    if (!pawn.IsDriver())
                    {
                        if (job != null)
                        {
                            if (job.def == JobDefOf.Flee || job.def == JobDefOf.FleeAndCower
                                || job.def == JobDefOf.Steal || job.def == JobDefOf.Kidnap
                                || job.def == JobDefOf.CarryDownedPawnToExit || job.def == JobDefOf.WaitCombat
                                || job.def == JobDefOf.AttackMelee || job.def == JobDefOf.AttackStatic
                                || job.def == JobDefOf.Goto && pawn.CurJob.exitMapOnArrival)
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
                                    newjob = true;
                                }
                            }
                        }
                    }
                }
                if (newjob)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);

                    pawn.jobs.TryTakeOrderedJob(job);
                    if (oldJob != null)
                    {
                        //   Log.Message("Job replaced " + pawn);
                        pawn.jobs.jobQueue.EnqueueFirst(oldJob);
                    }
                }
            }

        }

        private static Job GetVehicle(Pawn pawn, Job job, WorkTypeDef workType)
        {
            List<Thing> availableVehicles = pawn.AvailableVehicles();
            if (!pawn.IsDriver())
            {
                if (availableVehicles.Count > 0)
                {
                    Thing vehicle = TFH_Utility.GetRightVehicle(pawn, availableVehicles, workType);
                    if (vehicle != null)
                    {
                        job = new Job(HaulJobDefOf.Mount)
                        {
                            targetA = vehicle
                        };
                    }
                }
            }
            else
            {
                if (!TFH_Utility.IsDriverOfThisVehicle(pawn, TFH_Utility.GetRightVehicle(pawn, availableVehicles, workType)))
                {
                    job = pawn.DismountAtParkingLot(pawn.MountedVehicle(), "TNJ 99");
                }
            }

            return job;
        }

    }
}