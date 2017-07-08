namespace TFH_Incidents
{
    using System.Collections.Generic;

    using RimWorld;

    using TFH_VehicleBase;

    using TFH_VehicleHauling.DefOf_TFH;

    using Verse;
    using Verse.AI;
    using Verse.AI.Group;

    public class IncidentWorker_TraderCaravanArrival : IncidentWorker_NeutralGroup
    {
        protected override PawnGroupKindDef PawnGroupKindDef
        {
            get
            {
                return PawnGroupKindDefOf.Trader;
            }
        }

        protected override bool FactionCanBeGroupSource(Faction f, Map map, bool desperate = false)
        {
            return base.FactionCanBeGroupSource(f, map, desperate) && f.def.caravanTraderKinds.Any<TraderKindDef>();
        }

        public override bool TryExecute(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!base.TryResolveParms(parms))
            {
                return false;
            }
            if (parms.faction.HostileTo(Faction.OfPlayer))
            {
                return false;
            }
            List<Pawn> list = base.SpawnPawns(parms);
            if (list.Count == 0)
            {
                return false;
            }
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].needs != null && list[i].needs.food != null)
                {
                    list[i].needs.food.CurLevel = list[i].needs.food.MaxLevel;
                }
            }
            TraderKindDef traderKindDef = null;
            for (int j = 0; j < list.Count; j++)
            {
                Pawn pawn = list[j];
                if (pawn.TraderKind != null)
                {
                    traderKindDef = pawn.TraderKind;
                    break;
                }
            }

            // Add vehicles
            foreach (Pawn current in list)
            {
                // Make vehicles
                if (current.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
                    && parms.faction.def.techLevel >= TechLevel.Industrial
                    && current.RaceProps.FleshType != FleshTypeDefOf.Mechanoid && current.RaceProps.ToolUser && Rand.Value > 0.5f)
                {
                    CellFinder.RandomClosewalkCellNear(current.Position, current.Map, 5);
                    var rand = Rand.Value;

                    Pawn cart = PawnGenerator.GeneratePawn(VehicleKindDefOf.TFH_ATV, parms.faction);

                    if (rand >= 0.9f)
                    {
                        cart = PawnGenerator.GeneratePawn(VehicleKindDefOf.TFH_Cart, parms.faction);
                    }
                    else if (rand >= 0.8f)
                    {
                        cart = PawnGenerator.GeneratePawn(VehicleKindDefOf.TFH_Truck, parms.faction);
                    }
                    GenSpawn.Spawn(cart, current.Position, map, Rot4.Random, false);
                   // current.Map.reservationManager.ReleaseAllForTarget(cart);
                    Job job = new Job(VehicleJobDefOf.Mount) { targetA = cart };
                    current.jobs.StartJob(job, JobCondition.InterruptForced, null, true);
                }
            }

            string label = "LetterLabelTraderCaravanArrival".Translate(new object[]
                                                                           {
                                                                               parms.faction.Name,
                                                                               traderKindDef.label
                                                                           }).CapitalizeFirst();
            string text = "LetterTraderCaravanArrival".Translate(new object[]
                                                                     {
                                                                         parms.faction.Name,
                                                                         traderKindDef.label
                                                                     }).CapitalizeFirst();
            PawnRelationUtility.Notify_PawnsSeenByPlayer(list, ref label, ref text, "LetterRelatedPawnsNeutralGroup".Translate(), true);
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.Good, list[0], null);
            IntVec3 chillSpot;
            RCellFinder.TryFindRandomSpotJustOutsideColony(list[0], out chillSpot);
            LordJob_TradeWithColony lordJob = new LordJob_TradeWithColony(parms.faction, chillSpot);
            LordMaker.MakeNewLord(parms.faction, lordJob, map, list);
            return true;
        }

        protected override void ResolveParmsPoints(IncidentParms parms)
        {
            parms.points = TraderCaravanUtility.GenerateGuardPoints();
            IncidentParmsUtility.AdjustPointsForGroupArrivalParams(parms);
        }
    }
}
