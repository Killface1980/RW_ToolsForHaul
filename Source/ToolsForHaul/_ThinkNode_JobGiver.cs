namespace ToolsForHaul.Detours
{
    // Not working

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using RimWorld;

    using ToolsForHaul.Components;
    using ToolsForHaul.Defs;
    using ToolsForHaul.NoCCL;
    using ToolsForHaul.Utilities;

    using UnityEngine;

    using Verse;
    using Verse.AI;

    public abstract class _ThinkNode_JobGiver : ThinkNode
    {

        protected abstract Job TryGiveJob(Pawn pawn);

        [Detour(typeof(ThinkNode_JobGiver), bindingFlags = BindingFlags.Instance | BindingFlags.Public)]
        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            Log.Message("Test - detour working");
            ThinkResult result;
            try
            {
                if (jobParams.maxDistToSquadFlag > 0f)
                {
                    if (pawn.mindState.maxDistToSquadFlag > 0f)
                    {
                        Log.Error("Squad flag was not reset properly; raiders may behave strangely");
                    }

                    pawn.mindState.maxDistToSquadFlag = jobParams.maxDistToSquadFlag;
                }

                Job job = this.TryGiveJob(pawn);

                if (job == null)
                {
                    result = ThinkResult.NoJob;

                    // Modded
                    //      if (pawn.mindState.IsIdle)
                    //      {
                    //          if (pawn.IsDriver())
                    //          {
                    //              try
                    //              {
                    //                  job = pawn.DismountAtParkingLot(pawn.MountedVehicle());
                    //              }
                    //              catch (ArgumentNullException argumentNullException)
                    //              {
                    //                  Debug.Log(argumentNullException);
                    //              }
                    //
                    //              result = new ThinkResult(job, this, null);
                    //
                    //          }
                    //      }
                }
                else
                {
                    if (pawn.Faction.IsPlayer && pawn.RaceProps.Humanlike && pawn.RaceProps.IsFlesh)
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
                                    var oldJob = job;
                                    pawn.jobs.jobQueue.EnqueueFirst(oldJob);

                                    job = new Job(HaulJobDefOf.Mount)
                                    {
                                        targetA = availableVehiclesForSteeling
                                                      .FirstOrDefault()
                                    };
                                }
                            }
                        }
                    }
                    result = new ThinkResult(job, this, null);
                }
            }
            finally
            {
                pawn.mindState.maxDistToSquadFlag = -1f;
            }
            Log.Message("TNJ #1");
            return result;
        }

    }
}
