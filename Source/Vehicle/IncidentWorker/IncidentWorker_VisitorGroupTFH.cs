using System.Collections.Generic;
using System.Linq;
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
            if (!this.TryResolveParms(parms))
            {
                return false;
            }
            List<Pawn> list = base.SpawnPawns(parms);
            if (list.Count == 0)
            {
                return false;
            }
            IntVec3 chillSpot;
            RCellFinder.TryFindRandomSpotJustOutsideColony(list[0], out chillSpot);
            LordJob_VisitColony lordJob = new LordJob_VisitColony(parms.faction, chillSpot);
            LordMaker.MakeNewLord(parms.faction, lordJob, list);
            bool flag = false;
            if (Rand.Value < TraderChance)
            {
                flag = this.TryConvertOnePawnToSmallTrader(list, parms.faction);
            }
            Pawn pawn = list.Find((Pawn x) => parms.faction.leader == x);
            string label;
            string text3;
            if (list.Count == 1)
            {
                string text = (!flag) ? string.Empty : "SingleVisitorArrivesTraderInfo".Translate();
                string text2 = (pawn == null) ? string.Empty : "SingleVisitorArrivesLeaderInfo".Translate();
                label = "LetterLabelSingleVisitorArrives".Translate();
                text3 = "SingleVisitorArrives".Translate(new object[]
                {
                    list[0].story.adulthood.title.ToLower(),
                    parms.faction.Name,
                    list[0].Name,
                    text,
                    text2
                });
                text3 = text3.AdjustedFor(list[0]);
            }
            else
            {
                string text4 = (!flag) ? string.Empty : "GroupVisitorsArriveTraderInfo".Translate();
                string text5 = (pawn == null) ? string.Empty : "GroupVisitorsArriveLeaderInfo".Translate(new object[]
                {
                    pawn.LabelShort
                });
                label = "LetterLabelGroupVisitorsArrive".Translate();
                text3 = "GroupVisitorsArrive".Translate(new object[]
                {
                    parms.faction.Name,
                    text4,
                    text5
                });
            }
            Find.LetterStack.ReceiveLetter(label, text3, LetterType.Good, list[0]);
            return true;
        }

        private bool TryConvertOnePawnToSmallTrader(List<Pawn> pawns, Faction faction)
        {
            if (faction.def.visitorTraderKinds.NullOrEmpty<TraderKindDef>())
            {
                return false;
            }
            Pawn pawn = pawns.RandomElement<Pawn>();
            Lord lord = pawn.GetLord();
            pawn.mindState.wantsToTradeWithColony = true;
            PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn, true);
            TraderKindDef traderKindDef = faction.def.visitorTraderKinds.RandomElement<TraderKindDef>();
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
                    IntVec3 loc = CellFinder.RandomClosewalkCellNear(pawn.Position, 5);
                    GenSpawn.Spawn(pawn2, loc);
                    lord.AddPawn(pawn2);
                }
                else if (!pawn.inventory.container.TryAdd(current))
                {
                    current.Destroy();
                }
            }
            if (!pawn.inventory.container.Any((Thing x) => x.def.IsNutritionGivingIngestible && x.def.ingestible.preferability >= FoodPreferability.MealAwful))
            {
                PawnInventoryGenerator.GiveRandomFood(pawn);
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
