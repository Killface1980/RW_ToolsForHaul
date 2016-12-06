using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ToolsForHaul
{
    public abstract class IncidentWorker_Raid_Sanity : IncidentWorker_PawnsArrive
    {

        protected abstract bool TryResolveRaidFaction(IncidentParms parms);

        protected abstract void ResolveRaidStrategy(IncidentParms parms);

        protected abstract string GetLetterLabel(IncidentParms parms);

        protected abstract string GetLetterText(IncidentParms parms, List<Pawn> pawns);

        protected abstract LetterType GetLetterType();

        protected abstract string GetRelatedPawnsInfoLetterText(IncidentParms parms);

        protected virtual void ResolveRaidPoints(IncidentParms parms)
        {
            if (parms.points > 0f)
            {
                return;
            }

            parms.points = (float)Rand.Range(50, 300);
        }

        protected virtual void ResolveRaidArriveMode(IncidentParms parms)
        {
            if (parms.raidArrivalMode != PawnsArriveMode.Undecided)
            {
                return;
            }

            if (parms.faction.def.techLevel < TechLevel.Spacer || parms.points < 240f)
            {
                parms.raidArrivalMode = PawnsArriveMode.EdgeWalkIn;
            }
            else
            {
                parms.raidArrivalMode = parms.raidStrategy.arriveModes.RandomElementByWeight(delegate (PawnsArriveMode am)
                {
                    switch (am)
                    {
                        case PawnsArriveMode.EdgeWalkIn:
                            return 70f;
                        case PawnsArriveMode.EdgeDrop:
                            return 20f;
                        case PawnsArriveMode.CenterDrop:
                            return 10f;
                        default:
                            throw new NotImplementedException();
                    }
                });
            }
        }

        protected virtual void ResolveRaidSpawnCenter(IncidentParms parms)
        {
            if (parms.spawnCenter.IsValid)
            {
                return;
            }

            if (parms.raidArrivalMode == PawnsArriveMode.CenterDrop || parms.raidArrivalMode == PawnsArriveMode.EdgeDrop)
            {
                if (parms.raidArrivalMode == PawnsArriveMode.CenterDrop)
                {
                    parms.raidPodOpenDelay = 520;
                    if (Rand.Value < 0.4f && Find.ListerBuildings.ColonistsHaveBuildingWithPowerOn(ThingDefOf.OrbitalTradeBeacon))
                    {
                        parms.spawnCenter = DropCellFinder.TradeDropSpot();
                    }
                    else if (!DropCellFinder.TryFindRaiderDropCenterClose(out parms.spawnCenter))
                    {
                        parms.raidArrivalMode = PawnsArriveMode.EdgeDrop;
                    }
                }

                if (parms.raidArrivalMode == PawnsArriveMode.EdgeDrop)
                {
                    parms.raidPodOpenDelay = 140;
                    parms.spawnCenter = DropCellFinder.FindRaiderDropCenterDistant();
                }
            }
            else
            {
                RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter);
            }
        }

        public override bool TryExecute(IncidentParms parms)
        {
            ResolveRaidPoints(parms);
            if (!TryResolveRaidFaction(parms))
            {
                return false;
            }

            ResolveRaidStrategy(parms);
            ResolveRaidArriveMode(parms);
            ResolveRaidSpawnCenter(parms);
            PawnGroupMakerUtility.AdjustPointsForGroupArrivalParams(parms);
            List<Pawn> list = PawnGroupMakerUtility.GenerateArrivingPawns(parms).ToList();
            if (list.Count == 0)
            {
                Log.Error("Got no pawns spawning raid from parms " + parms);
                return false;
            }

            TargetInfo letterLookTarget = TargetInfo.Invalid;
            if (parms.raidArrivalMode == PawnsArriveMode.CenterDrop || parms.raidArrivalMode == PawnsArriveMode.EdgeDrop)
            {
                DropPodUtility.DropThingsNear(parms.spawnCenter, list.Cast<Thing>(), parms.raidPodOpenDelay, true, true);
                letterLookTarget = parms.spawnCenter;
            }
            else
            {
                foreach (Pawn current in list)
                {
                    float value = Rand.Value;
                    IntVec3 intVec = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, 8);
                    GenSpawn.Spawn(current, intVec);

                    letterLookTarget = current;

                    if (parms.faction.def.techLevel >= TechLevel.Industrial && value >= 0.5f && current.RaceProps.fleshType != FleshType.Mechanoid)
                    {
                        CellFinder.RandomClosewalkCellNear(current.Position, 5);
                        Thing thing = ThingMaker.MakeThing(ThingDef.Named("VehicleATV"));
                        thing.SetFaction(parms.faction);
                        GenSpawn.Spawn(thing, current.Position);

                        Job job = new Job(HaulJobDefOf.Mount);
                        Find.Reservations.ReleaseAllForTarget(thing);
                        job.targetA = thing;
                        current.jobs.StartJob(job, JobCondition.InterruptForced);
                    }

                }
            }

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Points = " + parms.points.ToString("F0"));

            foreach (Pawn current2 in list)
            {
                string str = (current2.equipment == null || current2.equipment.Primary == null) ? "unarmed" : current2.equipment.Primary.LabelCap;
                stringBuilder.AppendLine(current2.KindLabel + " - " + str);
            }

            Find.LetterStack.ReceiveLetter(this.GetLetterLabel(parms), this.GetLetterText(parms, list), this.GetLetterType(), letterLookTarget, stringBuilder.ToString());
            if (this.GetLetterType() == LetterType.BadUrgent)
            {
                TaleRecorder.RecordTale(TaleDefOf.RaidArrived, new object[0]);
            }

            PawnRelationUtility.Notify_PawnsSeenByPlayer(list, GetRelatedPawnsInfoLetterText(parms), true);
            Lord lord = LordMaker.MakeNewLord(parms.faction, parms.raidStrategy.Worker.MakeLordJob(ref parms), list);
            AvoidGridMaker.RegenerateAvoidGridsFor(parms.faction);
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.EquippingWeapons, OpportunityType.Critical);
            if (!PlayerKnowledgeDatabase.IsComplete(ConceptDefOf.PersonalShields))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Pawn pawn = list[i];
                    if (pawn.apparel.WornApparel.Any((Apparel ap) => ap is PersonalShield))
                    {
                        LessonAutoActivator.TeachOpportunity(ConceptDefOf.PersonalShields, OpportunityType.Critical);
                        break;
                    }
                }
            }

            if (DebugViewSettings.drawStealDebug && parms.faction.HostileTo(Faction.OfPlayer))
            {
                Log.Message(string.Concat(new object[]
                {
            "Market value threshold to start stealing: ",
            StealAIUtility.StartStealingMarketValueThreshold(lord),
            " (colony wealth = ",
            Find.StoryWatcher.watcherWealth.WealthTotal,
            ")"
                }));
            }

            return true;
        }
    }
}
