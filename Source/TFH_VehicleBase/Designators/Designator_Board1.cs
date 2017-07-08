using System.Collections.Generic;
using RimWorld;
using ToolsForHaul.JobDefs;
using Verse;
using Verse.AI;

namespace ToolsForHaul.Designators
{
    class Designator_Board : Designator
    {
        private const string txtCannotBoard = "CannotBoard";

        public Thing vehicle;

        public Designator_Board()
        {
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Click;
        }

        public override int DraggableDimensions { get { return 2; } }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            List<Thing> thingList = loc.GetThingList();

            foreach (Thing thing in thingList)
            {
                Pawn pawn = thing as Pawn;
                if (pawn != null && pawn.Faction == Faction.OfPlayer && (pawn.RaceProps.IsMechanoid || pawn.RaceProps.Humanlike))
                    return true;
            }

            return new AcceptanceReport(txtCannotBoard.Translate());
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            List<Thing> thingList = c.GetThingList();
            foreach (Thing thing in thingList)
            {
                Pawn pawn = thing as Pawn;
                if (pawn != null && pawn.Faction == Faction.OfPlayer && (pawn.RaceProps.IsMechanoid || pawn.RaceProps.Humanlike))
                {
                    Pawn crew = pawn;
                    Job jobNew = new Job(HaulJobDefOf.Board);
                    Find.Reservations.ReleaseAllForTarget(vehicle);
                    jobNew.targetA = vehicle;
                    crew.jobs.TryTakeOrderedJob(jobNew);
                    break;
                }
            }

            Find.DesignatorManager.Deselect();
        }
    }
}