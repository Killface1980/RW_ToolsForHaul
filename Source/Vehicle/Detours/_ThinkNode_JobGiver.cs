using System;
using System.Linq;
using System.Reflection;
using RimWorld;
using ToolsForHaul;
using ToolsForHaul.JobDefs;
using ToolsForHaul.Utilities;
using UnityEngine;
using Verse;
using Verse.AI;
using static ToolsForHaul.GameComponentToolsForHaul;

namespace Verse.AI
{
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

                    //Modded
                    if (pawn.mindState.IsIdle)
                    {
                        if (ToolsForHaulUtility.IsDriver(pawn))
                        {
                            try
                            {
                                job = ToolsForHaulUtility.DismountInBase(pawn, CurrentVehicle[pawn]);
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
                            if (ToolsForHaulUtility.IsDriver(pawn))
                            {
                                job = ToolsForHaulUtility.DismountInBase(pawn, CurrentVehicle[pawn]);
                            }
                        }
                        if (job.def == JobDefOf.FinishFrame || job.def == JobDefOf.Deconstruct || job.def == JobDefOf.Repair || job.def == JobDefOf.BuildRoof || job.def == JobDefOf.RemoveRoof || job.def == JobDefOf.RemoveFloor)
                        {
                            if (ToolsForHaulUtility.Cart.Count > 0 || ToolsForHaulUtility.Cart.Count > 0)
                            {
                                Thing vehicle = RightVehicle.GetRightVehicle(pawn, WorkTypeDefOf.Construction);
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
            if (!ToolsForHaulUtility.IsDriver(pawn))
            {
                if (ToolsForHaulUtility.Cart.Count > 0 || ToolsForHaulUtility.Cart.Count > 0)
                {
                    Thing vehicle = RightVehicle.GetRightVehicle(pawn, workType);
                    if (vehicle != null)
                    {
                        job = new Job(HaulJobDefOf.Mount)
                        {
                            targetA = vehicle,
                        };
                    }
                }
            }
            else
            {
                if (!ToolsForHaulUtility.IsDriverOfThisVehicle(pawn, RightVehicle.GetRightVehicle(pawn, workType)))
                {
                    job = ToolsForHaulUtility.DismountInBase(pawn, CurrentVehicle[pawn]);
                }
            }

            return job;
        }
    }
}
