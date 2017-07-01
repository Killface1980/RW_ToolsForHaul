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
        static void Postfix(Pawn pawn, JobIssueParams jobParams)
        {
            Job job = pawn.CurJob;
            Job oldJob = pawn.CurJob;

            if (pawn != null && pawn.RaceProps.Humanlike && pawn.RaceProps.IsFlesh)
            {
                if (pawn.Faction.IsPlayer)
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
                        }
                    }

                    if (job.def == JobDefOf.FinishFrame || job.def == JobDefOf.Deconstruct || job.def == JobDefOf.Repair || job.def == JobDefOf.BuildRoof || job.def == JobDefOf.RemoveRoof || job.def == JobDefOf.RemoveFloor)
                    {
                        List<Thing> availableVehicles = pawn.AvailableVehicles();
                        if (availableVehicles.Count > 0 || availableVehicles.Count > 0)
                        {
                            Thing vehicle = TFH_Utility.GetRightVehicle(pawn, availableVehicles, WorkTypeDefOf.Construction);
                            if (vehicle != null && pawn.Position.DistanceToSquared(vehicle.Position) < pawn.Position.DistanceToSquared(job.targetA.Cell))
                                job = GetVehicle(pawn, job, WorkTypeDefOf.Construction);
                        }
                    }
                }
                else if (pawn.Faction.HostileTo(Faction.OfPlayer))
                {
                    if (!pawn.IsDriver())
                    {
                        if (job.def == JobDefOf.Flee || job.def == JobDefOf.FleeAndCower
                            || job.def == JobDefOf.Steal || job.def == JobDefOf.Kidnap
                            || job.def == JobDefOf.CarryDownedPawnToExit || job.def == JobDefOf.Goto
                            && pawn.CurJob.targetA.Cell.OnEdge(pawn.Map))
                        {
                            Log.Message(pawn.LabelShort + " no driver. " + job.def);
                            List<Thing> availableVehiclesForSteeling = pawn.AvailableVehiclesForSteeling(20f);

                            Log.Message("Shiny cars " + availableVehiclesForSteeling);

                            if (!availableVehiclesForSteeling.NullOrEmpty())
                            {

                                job = new Job(HaulJobDefOf.Mount)
                                {
                                    targetA = availableVehiclesForSteeling
                                                  .FirstOrDefault()
                                };
                            }
                        }
                    }
                }
                if (job != oldJob)
                {
                    pawn.jobs.StartJob(job);
                    if (oldJob != null)
                    {
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