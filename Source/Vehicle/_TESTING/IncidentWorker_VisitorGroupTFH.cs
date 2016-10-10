using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ToolsForHaul
{
    public class IncidentWorker_VisitorGroupTFH : IncidentWorker_NeutralGroup
    {
        private const float TraderChance = 0.8f;

        public override bool TryExecute(IncidentParms parms)
        {
            if (!TryResolveParms(parms))
            {
                return false;
            }
            List<Pawn> list = SpawnPawns(parms);
            if (list.Count == 0)
            {
                return false;
            }
            IntVec3 intVec;
            RCellFinder.TryFindRandomSpotJustOutsideColony(list[0], out intVec);
            LordJob_VisitColony lordJob_VisitColony = new LordJob_VisitColony(parms.faction, intVec);
            LordMaker.MakeNewLord(parms.faction, lordJob_VisitColony, list);
            bool flag = false;
            if (Rand.Value < 0.800000011920929)
            {
                flag = TryConvertOnePawnToSmallTrader(list, parms.faction);
            }
            string text2;
            string text3;
            if (list.Count == 1)
            {
                string text = (!flag) ? string.Empty : "SingleVisitorArrivesTraderInfo".Translate();
                text2 = "LetterLabelSingleVisitorArrives".Translate();
                text3 = "SingleVisitorArrives".Translate(list[0].story.adulthood.title.ToLower(), parms.faction, list[0].Name, text).AdjustedFor(list[0]);
            }
            else
            {
                string text4 = (!flag) ? string.Empty : "GroupVisitorsArriveTraderInfo".Translate();
                text2 = "LetterLabelGroupVisitorsArrive".Translate();
                text3 = "GroupVisitorsArrive".Translate(parms.faction, text4);
            }
            Find.LetterStack.ReceiveLetter(text2, text3, 0, list[0]);
            return true;
        }

        private bool TryConvertOnePawnToSmallTrader(List<Pawn> pawns, Faction faction)
        {
            if (faction.def.visitorTraderKinds.NullOrEmpty())
            {
                return false;
            }
            Pawn pawn = pawns.RandomElement();
            Lord lord = pawn.GetLord();
            pawn.mindState.wantsToTradeWithColony = true;
            PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn, true);
            TraderKindDef traderKindDef = faction.def.visitorTraderKinds.RandomElement();
            pawn.trader.traderKind = traderKindDef;
            pawn.inventory.DestroyAll();
            foreach (Thing current in TraderStockGenerator.GenerateTraderThings(traderKindDef))
            {
                Pawn pawn2 = current as Pawn;
                if (pawn2 != null)
                {
                    if (pawn2.Faction != pawn.Faction)
                    {
                        pawn2.SetFaction(pawn.Faction);
                    }
                    IntVec3 intVec = CellFinder.RandomClosewalkCellNear(pawn.Position, 5);
                    GenSpawn.Spawn(pawn2, intVec);
                    lord.AddPawn(pawn2);
                }
                else if (!pawn.inventory.container.TryAdd(current))
                {
                    current.Destroy();
                }
            }
            CellFinder.RandomClosewalkCellNear(pawn.Position, 5);
            Thing thing = ThingMaker.MakeThing(ThingDef.Named("VehicleCart"));
            GenSpawn.Spawn(thing, pawn.Position);
            Job job = new Job(DefDatabase<JobDef>.GetNamed("Mount"));
            Find.Reservations.ReleaseAllForTarget(thing);
            job.targetA = thing;
            pawn.jobs.StartJob(job, JobCondition.InterruptForced);
            return true;
        }
    }
}
