using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using static ToolsForHaul.MapComponent_ToolsForHaul;

namespace ToolsForHaul
{
    public abstract class _ThinkNode_JobGiver : ThinkNode
    {
        protected abstract Job TryGiveTerminalJob(Pawn pawn);


        public override ThinkResult TryIssueJobPackage(Pawn pawn)
        {
            Job job = this.TryGiveTerminalJob(pawn);
            bool jobNull = job == null;
            ThinkResult result;
            if (jobNull)
            {
                result = ThinkResult.NoJob;
              if (wasAutoEquipped.ContainsKey(pawn) && pawn.mindState.IsIdle)
              {
                  ThingWithComps dummy;
                  Apparel_Backpack backpack = ToolsForHaulUtility.TryGetBackpack(pawn);
              
                  Pawn wearer = backpack.wearer;
                  if (wearer.equipment.Primary != null)
                      wearer.equipment.TryTransferEquipmentToContainer(wearer.equipment.Primary, wearer.inventory.container, out dummy);
                  else
                      backpack.numOfSavedItems--;
                  wearer.equipment.AddEquipment(wasAutoEquipped[pawn]);
                  wearer.inventory.container.Remove(wasAutoEquipped[pawn]);
                  wasAutoEquipped.Remove(pawn);
              }
            }
            else
            {
                bool flag2 = pawn.Faction == Faction.OfPlayer;
                if (flag2)
                {
                    if (job.def == JobDefOf.DoBill)
                    {
                        RightTools.EquipRigthTool(pawn, job.RecipeDef.workSpeedStat);
                    }

                    if (job.def == JobDefOf.Hunt)
                    {
                        RightTools.EquipRigthTool(pawn, StatDefOf.AccuracyLong);
                    }

                    if (job.def == JobDefOf.FinishFrame || job.def == JobDefOf.Deconstruct || job.def == JobDefOf.Repair)
                    {
                        RightTools.EquipRigthTool(pawn, StatDefOf.ConstructionSpeed);
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
    }
}
