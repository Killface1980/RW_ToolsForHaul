using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;

namespace ToolsForHaul
{
    class Designator_Board : Designator
    {
        private const string txtCannotBoard = "CannotBoard";

        public Thing vehicle;

        public Designator_Board()
            : base()
        {
            useMouseIcon = true;
            this.soundSucceeded = SoundDefOf.Click;
        }

        public override int DraggableDimensions { get { return 2; } }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            List<Thing> thingList = loc.GetThingList(this.Map);

            foreach (var thing in thingList)
            {
                Pawn pawn = thing as Pawn;
                if (pawn != null && (pawn.Faction == Faction.OfPlayer && (pawn.RaceProps.IsMechanoid || pawn.RaceProps.Humanlike)))
                    return true;
            }
            return new AcceptanceReport(txtCannotBoard.Translate());
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            List<Thing> thingList = c.GetThingList(this.Map);
            foreach (var thing in thingList)
            {
                Pawn pawn = thing as Pawn;
                if (pawn != null && (pawn.Faction == Faction.OfPlayer && (pawn.RaceProps.IsMechanoid || pawn.RaceProps.Humanlike)))
                {
                    Pawn crew = pawn;
                    Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("Board"));
                    this.Map.reservationManager.ReleaseAllForTarget(vehicle);
                    jobNew.targetA = vehicle;
                    crew.jobs.TryTakeOrderedJob(jobNew);
                    break;
                }
            }
            Find.DesignatorManager.Deselect();
        }
    }
}