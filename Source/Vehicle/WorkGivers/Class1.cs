using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using static ToolsForHaul.MapComponent_ToolsForHaul;

namespace ToolsForHaul
{
    public enum ToolRequirementLevel : byte
    {
        NoToolRequired,
        ToolPreferable,
        ToolRequired
    }

    public class WorkGiver_EquipTools : WorkGiver
    {
        public ThingWithComps closestAvailableTool;

        public virtual List<TargetInfo> Targets(Pawn pawn) => default(List<TargetInfo>);
        public virtual Job JobWithTool(TargetInfo target) => default(Job);

        // NonScanJob performed everytime previous(current) job is completed
        public override Job NonScanJob(Pawn pawn)
        {
            TryEquipFreeTool(pawn);

            // drop tool and haul it to stockpile, if necessary
            if (pawn.equipment.Primary != null && !ShouldKeepTool(pawn))
            {
                return TryReturnTool(pawn);
            }

            if (PawnCarriedWeaponBefore(pawn))
            {
                EquipPreviousWeapon(pawn);
            }

            return null;
        }

        public IntVec3 ClosestTargetCell(Pawn pawn)
        {
            float searchRange = 9999f;
            IntVec3 result = default(IntVec3);
            foreach (IntVec3 cell in Targets(pawn))
            {
                float lengthHorizontalSquared = (cell - pawn.Position).LengthHorizontalSquared;
                if (lengthHorizontalSquared < searchRange)
                {
                    result = cell;
                    searchRange = lengthHorizontalSquared;
                }
            }
            return result;
        }

        // keep tool if it could be used for other jobs
        public bool ShouldKeepTool(Pawn pawn)
        {
            CompTool toolComp = pawn.equipment.Primary.TryGetComp<CompTool>();

            if (toolComp == null)
                return true;

            if (toolComp.wasAutoEquipped)
            {
                if (toolComp.Allows("Hunting") && pawn.workSettings.WorkIsActive(WorkTypeDefOf.Hunting))
                {
                    return true;
                }
                if (toolComp.Allows("Construction") && pawn.workSettings.WorkIsActive(WorkTypeDefOf.Construction) ||
                    (pawn.workSettings.WorkIsActive(WorkTypeDefOf.Repair) ))
                {
                    return true;
                }
                if (toolComp.Allows("Mining") && pawn.workSettings.WorkIsActive(WorkTypeDefOf.Mining))
                {
                    return true;
                }
                if (toolComp.Allows("PlantCutting") && pawn.workSettings.WorkIsActive(WorkTypeDefOf.PlantCutting))
                {
                    return true;
                }

            }
            return false;
        }

        public void EquipPreviousWeapon(Pawn pawn)
        {
            SwapOrEquipPreviousWeapon(previousPawnWeapons[pawn], pawn);
            previousPawnWeapons.Remove(pawn);
        }

        // swap weapon in inventory with equipped one, or just equips it
        public void SwapOrEquipPreviousWeapon(Thing thing, Pawn pawn)
        {
            // if pawn has equipped weapon, put it in inventory
            if (pawn.equipment.Primary != null)
            {
                ThingWithComps leftover;
                pawn.equipment.TryTransferEquipmentToContainer(pawn.equipment.Primary, pawn.inventory.container,
                    out leftover);
            }
            // equip new required weapon
            pawn.equipment.AddEquipment(thing as ThingWithComps);
            // remove that equipment from inventory
            pawn.inventory.container.Remove(thing);
        }

        public Job TryEquipFreeTool(Pawn pawn)
        {
            // find proper tools of the specific work type
            IEnumerable<Thing> availableTools =
                Find.ListerThings.AllThings.FindAll(
                    tool =>
                        !tool.IsForbidden(pawn.Faction) &&
                        pawn.CanReserveAndReach(tool, PathEndMode.ClosestTouch, pawn.NormalMaxDanger()));

            if (availableTools.Any())
            {
                // find closest reachable tool of the specific work type
                closestAvailableTool = GenClosest.ClosestThing_Global(pawn.Position, availableTools) as ThingWithComps;

                if (closestAvailableTool != null)
                {
                    // if pawn has equipped weapon, put it in inventory
                    if (pawn.equipment.Primary != null)
                    {
                        previousPawnWeapons.Add(pawn, pawn.equipment.Primary);
                        ThingWithComps leftover;
                        pawn.equipment.TryTransferEquipmentToContainer(pawn.equipment.Primary, pawn.inventory.container,
                            out leftover);
                    }

                    // reserve and set as auto equipped
                    pawn.Reserve(closestAvailableTool);
                    closestAvailableTool.TryGetComp<CompTool>().wasAutoEquipped = true;

                    return new Job(JobDefOf.Equip, closestAvailableTool);
                }
            }

            return null;
        }

        public bool PawnCarriedWeaponBefore(Pawn pawn) =>
            pawn.equipment.Primary != null
            && previousPawnWeapons.ContainsKey(pawn)
            && previousPawnWeapons[pawn] == pawn;

        // drop tool and haul it to stockpile, if necessary
        public Job TryReturnTool(Pawn pawn)
        {
            ThingWithComps tool;
            // drops primary equipment (weapon) as forbidden
            pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out tool, pawn.Position);
            // making it non forbidden
            tool.SetForbidden(false);

            // tr to haul to the stockpile
            return HaulAIUtility.HaulToStorageJob(pawn, tool);
        }
    }
}