using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public class Designator_Mount : Designator
    {
        public Thing vehicle;

        public Designator_Mount()
        {
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Click;
        }

        public override int DraggableDimensions { get { return 2; } }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            Pawn pawn = loc.GetThingList().Find(t => t is Pawn) as Pawn;
            if (pawn == null)
                return new AcceptanceReport("CannotMount".Translate() + ": " + "NotPawn".Translate());
            if (pawn.Faction != Faction.OfPlayer)
                return new AcceptanceReport("CannotMount".Translate() + ": " + "NotColonyFaction".Translate());
#if Saddle
            if (!pawn.RaceProps.Animal && vehicle is Vehicle_Saddle)
                return new AcceptanceReport("CannotMount".Translate() + ": " + "NotHumanlikeOrMechanoid".Translate());
#endif
            if (pawn.RaceProps.Animal && !pawn.training.IsCompleted(TrainableDefOf.Obedience))
                return new AcceptanceReport("CannotMount".Translate() + ": " + "NotTrainedAnimal".Translate());
            if (pawn.RaceProps.Animal && !(pawn.RaceProps.baseBodySize >= 1.0))
                return new AcceptanceReport("CannotMount".Translate() + ": " + "TooSmallAnimal".Translate());
            return true;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            List<Thing> thingList = c.GetThingList();
            foreach (Thing thing in thingList)
            {
                Pawn pawn = thing as Pawn;

                bool alreadyMounted = false;
                foreach (Vehicle_Cart cart in ToolsForHaulUtility.Cart())
                    if (cart.mountableComp.Driver == pawn)
                        alreadyMounted = true;

                if (pawn != null && pawn.Faction == Faction.OfPlayer && (pawn.RaceProps.IsMechanoid || pawn.RaceProps.Humanlike) && !alreadyMounted)
                {
                    Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("Mount"));
                    Find.Reservations.ReleaseAllForTarget(vehicle);
                    jobNew.targetA = vehicle;
                    pawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                    break;
                }
                if (pawn != null && (pawn.Faction == Faction.OfPlayer && pawn.RaceProps.Animal) && pawn.training.IsCompleted(TrainableDefOf.Obedience) && pawn.RaceProps.baseBodySize >= 1.0)
                {
                    Pawn worker = null;
                    Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("MakeMount"));
                    Find.Reservations.ReleaseAllForTarget(vehicle);
                    jobNew.maxNumToCarry = 1;
                    jobNew.targetA = vehicle;
                    jobNew.targetB = pawn;
                    foreach (Pawn colonyPawn in Find.MapPawns.FreeColonistsSpawned)
                        if (colonyPawn.CurJob.def != jobNew.def && (worker == null || (worker.Position - pawn.Position).LengthHorizontal > (colonyPawn.Position - pawn.Position).LengthHorizontal))
                            worker = colonyPawn;
                    if (worker == null)
                    {
                        Messages.Message("NoWorkForMakeMount".Translate(), MessageSound.RejectInput);
                        break;
                    }
                    worker.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                    break;
                }
            }
            DesignatorManager.Deselect();
        }
    }
}