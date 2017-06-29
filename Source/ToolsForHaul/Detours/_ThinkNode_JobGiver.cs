namespace ToolsForHaul.Detours
{
    using System;
    using System.Collections.Generic;
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
                    if (pawn.mindState.IsIdle)
                    {
                        if (TFH_Utility.IsDriver(pawn))
                        {
                            try
                            {
                                job = TFH_Utility.DismountAtParkingLot(pawn, GameComponentToolsForHaul.CurrentDrivers[pawn]);
                            }
                            catch (ArgumentNullException argumentNullException)
                            {
                                Debug.Log(argumentNullException);
                            }

                            result = new ThinkResult(job, this, null);

                        }
                    }
                }
                else
                {

                    if (pawn.Faction == Faction.OfPlayer && pawn.RaceProps.Humanlike && pawn.RaceProps.IsFlesh)
                    {
                        if (job.def == JobDefOf.LayDown || job.def == JobDefOf.Arrest || job.def == JobDefOf.DeliverFood
                            || job.def == JobDefOf.EnterCryptosleepCasket || job.def == JobDefOf.EnterTransporter
                            || job.def == JobDefOf.Ingest || job.def == JobDefOf.ManTurret
                            || job.def == JobDefOf.Slaughter || job.def == JobDefOf.VisitSickPawn || job.def == JobDefOf.WaitWander || job.def == JobDefOf.DoBill)
                        {
                            if (TFH_Utility.IsDriver(pawn))
                            {
                                job = TFH_Utility.DismountAtParkingLot(pawn, GameComponentToolsForHaul.CurrentDrivers[pawn]);
                            }
                        }

                        if (job.def == JobDefOf.FinishFrame || job.def == JobDefOf.Deconstruct || job.def == JobDefOf.Repair || job.def == JobDefOf.BuildRoof || job.def == JobDefOf.RemoveRoof || job.def == JobDefOf.RemoveFloor)
                        {
                            List<Thing> availableVehicles = TFH_Utility.AvailableVehicles(pawn);
                            if (availableVehicles.Count > 0 || availableVehicles.Count > 0)
                            {
                                Thing vehicle = TFH_Utility.GetRightVehicle(pawn, availableVehicles, WorkTypeDefOf.Construction);
                                if (vehicle != null && pawn.Position.DistanceToSquared(vehicle.Position) < pawn.Position.DistanceToSquared(job.targetA.Cell))
                                    job = GetVehicle(pawn, job, WorkTypeDefOf.Construction);
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

            return result;
        }

        private static Job GetVehicle(Pawn pawn, Job job, WorkTypeDef workType)
        {
            List<Thing> availableVehicles = TFH_Utility.AvailableVehicles(pawn);
            if (!TFH_Utility.IsDriver(pawn))
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
                    job = TFH_Utility.DismountAtParkingLot(pawn, GameComponentToolsForHaul.CurrentDrivers[pawn]);
                }
            }

            return job;
        }
    }
}
