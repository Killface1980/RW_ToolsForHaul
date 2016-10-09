using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public class Designator_ApplyMedicine : Designator
    {
        private const string txtNoNeedTreatment = "NoNeedTreatment";

        public Thing medicine;
        public Pawn doctor;
        public CompSlots slotsComp;

        public Designation designation;

        public Designator_ApplyMedicine()
        {
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Click;
        }

        public override int DraggableDimensions { get { return 1; } }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            List<Thing> thingList = loc.GetThingList();

            foreach (Thing thing in thingList)
            {
                Pawn pawn = thing as Pawn;
                if (pawn != null && pawn.health.ShouldBeTendedNow)
                    return true;
            }
            return new AcceptanceReport(txtNoNeedTreatment.Translate());
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            List<Thing> thingList = c.GetThingList();
            foreach (Thing thing in thingList)
            {
                Pawn pawn = thing as Pawn;
                if (pawn != null && pawn.health.ShouldBeTendedNow)
                {
                    Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("ApplyMedicine"));
                    jobNew.targetA = pawn;

                    Thing dummy;
                    slotsComp.slots.TryDrop(medicine, doctor.Position, ThingPlaceMode.Direct, Medicine.GetMedicineCountToFullyHeal(jobNew.targetA.Thing as Pawn), out dummy);

                    jobNew.targetB = dummy;

                    jobNew.maxNumToCarry = Medicine.GetMedicineCountToFullyHeal(jobNew.targetA.Thing as Pawn);
                    doctor.drafter.TakeOrderedJob(jobNew);
                    break;
                }
            }
            DesignatorManager.Deselect();
        }
    }
}