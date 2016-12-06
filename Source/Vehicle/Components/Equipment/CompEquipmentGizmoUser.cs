using System.Collections.Generic;
using Verse;

namespace ToolsForHaul.Components
{
    public class CompEquipmentGizmoUser : ThingComp
    {
        public Pawn Owner => this.parent as Pawn;

        public bool SpawnedAndWell(Pawn pawn)
        {
            if (pawn.Spawned && !pawn.Downed)
                return true;
            return false;
        }

        /*
        public IEnumerable<CompSlotsBackpack> EquippedSlottersComps()
        {
            // iterate through all apparels
            foreach (ThingWithComps thing in Owner.apparel.WornApparel)
            // NOTE: check what type to return (If it's designator, command_action, command or gizmo)
            {
                foreach (ThingComp thingComp in thing.AllComps.FindAll(comp => comp.GetType() == typeof(CompSlotsBackpack)))
                {
                    CompSlotsBackpack comp = (CompSlotsBackpack)thingComp;
                    // reassign pawn owner in comp, if not already set
                    comp.owner = comp.owner != Owner ? Owner : comp.owner;

                    yield return comp;
                }
            }

            // iterate through all equipment (weapons)
            foreach (ThingWithComps thing in Owner.equipment.AllEquipment)
            {
                foreach (ThingComp thingComp in thing.AllComps.FindAll(comp => comp.GetType() == typeof(CompSlotsBackpack)))
                {
                    CompSlotsBackpack comp = (CompSlotsBackpack)thingComp;
                    // reassign pawn owner in comp, if not already set
                    comp.owner = comp.owner != Owner ? Owner : comp.owner;

                    yield return comp;
                }
            }
        }
        */
        public override IEnumerable<Command> CompGetGizmosExtra()
        {
            foreach (Command current in base.CompGetGizmosExtra())
            {
                
                yield return current;
            }

            if (this.SpawnedAndWell(this.Owner))
            {
                // NOTE: should use concatenation of both enumerables, but can't cast to the same type without error
                // iterate through all apparels
                foreach (ThingWithComps thing in this.Owner.apparel.WornApparel)
                {
                    // NOTE: check what type to return (If it's designator, command_action, command or gizmo)
                    foreach (ThingComp thingComp in thing.AllComps.FindAll(comp => comp.GetType().IsSubclassOf(typeof(CompEquipmentGizmoProvider))))
                    {
                        CompEquipmentGizmoProvider comp = (CompEquipmentGizmoProvider)thingComp;

                        // reassign pawn owner in comp, if not already set
                        comp.owner = comp.owner != this.Owner ? this.Owner : comp.owner;

                        foreach (Command gizmo in comp.CompGetGizmosExtra())
                        {
                            yield return gizmo;
                        }
                    }
                }

                // iterate through all equipment (weapons)
                foreach (ThingWithComps thing in this.Owner.equipment.AllEquipment)
                {
                    foreach (ThingComp thingComp in thing.AllComps.FindAll(comp => comp.GetType().IsSubclassOf(typeof(CompEquipmentGizmoProvider))))
                    {
                        CompEquipmentGizmoProvider comp = (CompEquipmentGizmoProvider)thingComp;

                        // reassign pawn owner in comp, if not already set
                        comp.owner = comp.owner != this.Owner ? this.Owner : comp.owner;

                        foreach (Command gizmo in comp.CompGetGizmosExtra())
                        {
                            yield return gizmo;
                        }
                    }
                }
            }
        }
    }
}