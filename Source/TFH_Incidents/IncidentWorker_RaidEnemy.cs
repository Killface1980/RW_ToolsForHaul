namespace TFH_Incidents
{
    using System.Collections.Generic;
    using System.Linq;

    using RimWorld;

    using Verse;

    // Vanilla copy with edited base class
    public class IncidentWorker_RaidEnemy : IncidentWorker_Raid_TFH
    {
        protected override bool FactionCanBeGroupSource(Faction f, Map map, bool desperate = false)
        {
            return base.FactionCanBeGroupSource(f, map, desperate) && f.HostileTo(Faction.OfPlayer) && (desperate || GenDate.DaysPassed >= f.def.earliestRaidDays);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!base.TryExecuteWorker(parms))
            {
                return false;
            }

            Find.TickManager.slower.SignalForceNormalSpeedShort();
            Find.StoryWatcher.statsRecord.numRaidsEnemy++;
            return true;
        }

        // RimWorld.IncidentWorker_RaidEnemy
        protected override bool TryResolveRaidFaction(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            bool result;
            if (parms.faction != null)
            {
                result = true;
            }
            else
            {
                float num = parms.points;
                if (num <= 0f)
                {
                    num = 999999f;
                }
                if (!PawnGroupMakerUtility.TryGetRandomFactionForNormalPawnGroup(num, out parms.faction, (Faction f) => this.FactionCanBeGroupSource(f, map, false), true, true, true, true))
                {
                    if (!PawnGroupMakerUtility.TryGetRandomFactionForNormalPawnGroup(num, out parms.faction, (Faction f) => this.FactionCanBeGroupSource(f, map, true), true, true, true, true))
                    {
                        result = false;
                        return result;
                    }
                }
                result = true;
            }
            return result;
        }

        protected override void ResolveRaidPoints(IncidentParms parms)
        {
            if (parms.points <= 0f)
            {
                Log.Error("RaidEnemy is resolving raid points. They should always be set before initiating the incident.");
                parms.points = Rand.Range(50, 300);
            }
        }

        protected override void ResolveRaidStrategy(IncidentParms parms)
        {
            if (parms.raidStrategy != null)
            {
                return;
            }

            Map map = (Map)parms.target;
            parms.raidStrategy = (from d in DefDatabase<RaidStrategyDef>.AllDefs
                                  where d.Worker.CanUseWith(parms)
                                  select d).RandomElementByWeight(d => d.Worker.SelectionChance(map));
        }

        protected override string GetLetterLabel(IncidentParms parms)
        {
            return parms.raidStrategy.letterLabelEnemy;
        }

        protected override string GetLetterText(IncidentParms parms, List<Pawn> pawns)
        {
            string text = null;
            switch (parms.raidArrivalMode)
            {
                case PawnsArriveMode.EdgeWalkIn:
                    text = "EnemyRaidWalkIn".Translate(
                        parms.faction.def.pawnsPlural,
                        parms.faction.Name);
                    break;
                case PawnsArriveMode.EdgeDrop:
                    text = "EnemyRaidEdgeDrop".Translate(
                        parms.faction.def.pawnsPlural,
                        parms.faction.Name);
                    break;
                case PawnsArriveMode.CenterDrop:
                    text = "EnemyRaidCenterDrop".Translate(
                        parms.faction.def.pawnsPlural,
                        parms.faction.Name);
                    break;
            }
            text += "\n\n";
            text += parms.raidStrategy.arrivalTextEnemy;
            Pawn pawn = pawns.Find(x => x.Faction.leader == x);
            if (pawn != null)
            {
                text += "\n\n";
                text += "EnemyRaidLeaderPresent".Translate(
                    pawn.Faction.def.pawnsPlural,
                    pawn.LabelShort);
            }

            return text;
        }

        protected override LetterDef GetLetterDef()
        {
            return LetterDefOf.ThreatBig;
        }

        protected override string GetRelatedPawnsInfoLetterText(IncidentParms parms)
        {
            return "LetterRelatedPawnsRaidEnemy".Translate(parms.faction.def.pawnsPlural);
        }
    }
}
