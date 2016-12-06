using System;
using System.Linq;
using System.Reflection;
using RimWorld;
using ToolsForHaul.JobDefs;
using ToolsForHaul.Utilities;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public abstract class _ThinkNode_JobGiver : ThinkNode
    {
        protected abstract Job TryGiveTerminalJob(Pawn pawn);

        [Detour(typeof(ThinkNode_JobGiver), bindingFlags = BindingFlags.Instance | BindingFlags.Public)]
        public override ThinkResult TryIssueJobPackage(Pawn pawn)
        {
            Job job = this.TryGiveTerminalJob(pawn);
            bool jobNull = job == null;
            ThinkResult result;

            if (pawn.mindState.IsIdle)
            {
                // if (previousPawnWeapons.ContainsKey(pawn))
                // {
                // Apparel_Toolbelt toolbelt = ToolsForHaulUtility.TryGetToolbelt(pawn);
                // Pawn wearer = toolbelt.wearer;
                // if (wearer.equipment.Primary != null)
                // toolbelt.slotsComp.SwapEquipment(previousPawnWeapons[pawn]);
                // else
                // {
                // wearer.equipment.AddEquipment(previousPawnWeapons[pawn]);
                // toolbelt.slotsComp.slots.Remove(previousPawnWeapons[pawn]);
                // }
                // previousPawnWeapons.Remove(pawn);
                // }
                if (ToolsForHaulUtility.IsDriver(pawn))
                {
                    job = ToolsForHaulUtility.DismountInBase(pawn, MapComponent_ToolsForHaul.currentVehicle[pawn]);
                }
            }



            if (jobNull)
                result = ThinkResult.NoJob;
            else
            {
                if (pawn.Faction == Faction.OfPlayer && pawn.RaceProps.Humanlike && pawn.RaceProps.IsFlesh)
                {
                    if (job.def == JobDefOf.DoBill)
                    {
                        RightTools.EquipRigthTool(pawn, job.RecipeDef.workSpeedStat);
                    }

                    if (job.def == JobDefOf.Hunt)
                    {
                        RightTools.EquipRigthTool(pawn, StatDefOf.AccuracyLong);
                        job = GetVehicle(pawn, job, WorkTypeDefOf.Hunting);
                    }

                    if (job.def == JobDefOf.FinishFrame || job.def == JobDefOf.Deconstruct || job.def == JobDefOf.Repair || job.def == JobDefOf.BuildRoof || job.def == JobDefOf.RemoveRoof || job.def == JobDefOf.RemoveFloor)
                    {
                        RightTools.EquipRigthTool(pawn, StatDefOf.ConstructionSpeed);
                        if (ToolsForHaulUtility.Cart.Count > 0 || ToolsForHaulUtility.CartTurret.Count > 0)
                        {
                            Thing vehicle = RightTools.GetRightVehicle(pawn, WorkTypeDefOf.Construction);
                            if (vehicle != null && pawn.Position.DistanceToSquared(vehicle.Position) < pawn.Position.DistanceToSquared(job.targetA.Cell))
                                job = GetVehicle(pawn, job, WorkTypeDefOf.Construction);
                        }
                    }

                    if (job.def == JobDefOf.CutPlant || job.def == JobDefOf.Harvest)
                    {
                        RightTools.EquipRigthTool(pawn, StatDefOf.PlantWorkSpeed);
                    }

                    if (job.def == JobDefOf.Mine)
                    {
                        RightTools.EquipRigthTool(pawn, StatDefOf.MiningSpeed);
                    }

                    if (job.def == JobDefOf.TendPatient)
                    {
                        RightTools.EquipRigthTool(pawn, StatDefOf.BaseHealingQuality);
                    }


                }

                result = new ThinkResult(job, this);
            }

            return result;
        }

        private static Job GetVehicle(Pawn pawn, Job job, WorkTypeDef worktag)
        {
            if (!ToolsForHaulUtility.IsDriver(pawn))
            {
                if (ToolsForHaulUtility.Cart.Count > 0 || ToolsForHaulUtility.CartTurret.Count > 0)
                {
                    Thing vehicle = RightTools.GetRightVehicle(pawn, worktag);
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
                if (!ToolsForHaulUtility.IsDriverOfThisVehicle(pawn, RightTools.GetRightVehicle(pawn, worktag)))
                {
                    job = ToolsForHaulUtility.DismountInBase(pawn, MapComponent_ToolsForHaul.currentVehicle[pawn]);
                }
            }

            return job;
        }
    }
}
