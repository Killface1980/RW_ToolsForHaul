namespace ToolsForHaul.JobDrivers
{
    using System.Collections.Generic;

    using RimWorld;
    using RimWorld.Planet;

    using ToolsForHaul.Components;

    using Verse;
    using Verse.AI;

    public class JobDriver_MountAndDraft : JobDriver
    {
        // Constants
        private const TargetIndex MountableInd = TargetIndex.A;

        private const TargetIndex PlaceToGoTo = TargetIndex.B;

        public override string GetReport()
        {

            string repString;
            repString = "ReportMounting".Translate(this.TargetThingA.LabelCap);

            return repString;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            ///
            // Set fail conditions
            ///

            this.FailOnDestroyedOrNull(MountableInd);

            // Note we only fail on forbidden if the target doesn't start that way
            // This helps haul-aside jobs on forbidden items
            if (this.TargetThingA.IsForbidden(this.pawn.Faction))
            {
                this.FailOnForbidden(MountableInd);
            }

            ///
            // Define Toil
            ///

            ///
            // Toils Start
            ///

            // Reserve thing to be stored and storage cell 
            yield return Toils_Reserve.Reserve(MountableInd);

            // Mount on Target
            yield return Toils_Goto.GotoThing(MountableInd, PathEndMode.InteractionCell);

            Toil toilMountOn = new Toil();
            toilMountOn.initAction = () =>
                {
                    Pawn actor = toilMountOn.actor;
                    this.TargetThingA.TryGetComp<CompMountable>().MountOn(actor);
                };
            yield return toilMountOn;

            yield return Toils_Goto.GotoCell(PlaceToGoTo, PathEndMode.OnCell);

            Toil arrive = new Toil();
            arrive.initAction = () =>
                {
                    if (CurJob.exitMapOnArrival && pawn.Map.exitMapGrid.IsExitCell(this.pawn.Position))
                    {
                        this.TryExitMap();
                    }
                };
            arrive.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return arrive;

            Toil scatter = new Toil();
            scatter.initAction = () =>
                {
                    List<Thing> thingsHere = this.pawn.Map.thingGrid.ThingsListAt(pawn.Position);
                    bool foundOtherPawnHere = false;
                    for (int i = 0; i < thingsHere.Count; i++)
                    {
                        Pawn p = thingsHere[i] as Pawn;
                        if (p != null && p != pawn)
                        {
                            foundOtherPawnHere = true;
                            break;
                        }
                    }

                    LocalTargetInfo tp;
                    if (foundOtherPawnHere)
                    {
                        IntVec3 freeCell = CellFinder.RandomClosewalkCellNear(pawn.Position, this.pawn.Map, 2);
                        tp = new LocalTargetInfo(freeCell);
                    }
                    else
                        tp = new LocalTargetInfo(pawn.Position);

                    pawn.pather.StartPath(tp, PathEndMode.OnCell);
                };
            scatter.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            yield return scatter;

            // Set playerController to drafted
            Toil arrivalDraft = new Toil();
            arrivalDraft.initAction = () =>
                {
                    pawn.drafter.Drafted = true;
                };
            arrivalDraft.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return arrivalDraft;

        }

        private void TryExitMap()
        {
            if (base.CurJob.failIfCantJoinOrCreateCaravan && !CaravanExitMapUtility.CanExitMapAndJoinOrCreateCaravanNow(this.pawn))
            {
                return;
            }
            this.pawn.ExitMap(true);
        }
    }
}
