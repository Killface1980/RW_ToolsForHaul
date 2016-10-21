using RimWorld;
using ToolsForHaul.JobDefs;
using Verse;
using Verse.AI;

namespace ToolsForHaul.Designators
{
    public class Designator_Move : Designator
    {
        private const string txtCannotMove = "CannotMove";

        public Pawn driver;

        public Designator_Move()
        {
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Click;
        }

        public override int DraggableDimensions { get { return 0; } }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            if (loc.CanReach(driver, PathEndMode.OnCell, TraverseMode.ByPawn, Danger.Deadly))
                return true;
            return new AcceptanceReport(txtCannotMove.Translate());
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            Job jobNew = new Job(HaulJobDefOf.StandBy, c, 4800);
            driver.jobs.StartJob(jobNew, JobCondition.Incompletable);

            DesignatorManager.Deselect();
        }
    }
}