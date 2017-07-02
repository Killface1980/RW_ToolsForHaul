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
    using ToolsForHaul.Vehicles;

    [HarmonyPatch(typeof(ThinkNode_JobGiver))]
    [HarmonyPatch("TryIssueJobPackage")]
    [HarmonyPatch(new Type[] { typeof(Pawn), typeof(JobIssueParams) })]
    public static class ThinkNode_JobGiver_Patch
    {
        private static float vehicleSearchRadius = 12f;

        static void Postfix(Pawn pawn, JobIssueParams jobParams)
        {
            Job job = pawn.CurJob;
            Job oldJob = pawn.CurJob;
            bool newjob = false;

            if (pawn != null && pawn.RaceProps.Humanlike && pawn.RaceProps.IsFlesh)
            {
                if (pawn.Faction.IsPlayer)
                {
                    if (job != null && !pawn.Drafted)
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
                            if (job.def == JobDefOf.LayDown || job.def == JobDefOf.WaitWander
                                || job.def == JobDefOf.EnterCryptosleepCasket || job.def == JobDefOf.FeedPatient
                                || job.def == JobDefOf.TendPatient)
                            {
                                job = pawn.DismountAtParkingLot("TN #1a");
                                newjob = true;
                            }
                        }
                        else
                        {
                            if (job.def == JobDefOf.FinishFrame || job.def == JobDefOf.Deconstruct || job.def == JobDefOf.Repair || job.def == JobDefOf.BuildRoof || job.def == JobDefOf.RemoveRoof || job.def == JobDefOf.RemoveFloor)
                            {
                                List<Thing> availableVehicles = pawn.AvailableVehicles();
                                Vehicle_Cart vehicle = TFH_Utility.GetRightVehicle(pawn, availableVehicles, WorkTypeDefOf.Construction);

                                if (vehicle != null)
                                {
                                    if (pawn.Position.DistanceToSquared(vehicle.Position)
                                        < pawn.Position.DistanceToSquared(job.targetA.Cell))
                                    {
                                        job = MountOnOrReturnVehicle(pawn, job, vehicle);
                                        newjob = true;
                                    }
                                }
                            }
                            if (job.def == JobDefOf.Hunt)
                            {
                                List<Thing> availableVehicles = pawn.AvailableVehicles();
                                Vehicle_Cart vehicle = TFH_Utility.GetRightVehicle(pawn, availableVehicles, WorkTypeDefOf.Hunting);
                                {
                                    if (vehicle != null)
                                    {
                                        if (pawn.Position.DistanceToSquared(vehicle.Position)
                                            < pawn.Position.DistanceToSquared(job.targetA.Cell))
                                        {
                                            job = MountOnOrReturnVehicle(pawn, job, vehicle);
                                            newjob = true;
                                        }
                                    }
                                }
                            }

                            if (job.def == JobDefOf.Capture || job.def == JobDefOf.Rescue)
                            {
                                List<Thing> availableVehicles = pawn.AvailableVehicles();
                                Vehicle_Cart vehicle =
                                    TFH_Utility.GetRightVehicle(pawn, availableVehicles, WorkTypeDefOf.Doctor);
                                if (vehicle != null)
                                {
                                    if (pawn.Position.DistanceToSquared(vehicle.Position)
                                        < pawn.Position.DistanceToSquared(job.targetA.Cell))
                                    {
                                        job = MountOnOrReturnVehicle(pawn, job, vehicle);
                                        newjob = true;
                                        oldJob = null;
                                    }
                                }
                            }
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
}