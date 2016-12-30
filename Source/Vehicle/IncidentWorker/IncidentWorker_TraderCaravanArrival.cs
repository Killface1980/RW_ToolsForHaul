namespace ToolsForHaul.IncidentWorker
{
    using System.Collections.Generic;
    using System.Linq;

    using RimWorld;

    using ToolsForHaul.Components;
    using ToolsForHaul.Components.Vehicle;
    using ToolsForHaul.JobDefs;

    using UnityEngine;

    using Verse;
    using Verse.AI;
    using Verse.AI.Group;
    using Verse.Sound;

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

        // RimWorld.IncidentWorker_TraderCaravanArrival
        public override bool TryExecute(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!this.TryResolveParms(parms))
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
            foreach (var current in list)
            {
                if (current.TraderKind != null)
                {
                    traderKindDef = current.TraderKind;

                    float value = Rand.Value;

                    Thing thing = null;
                    if (current.RaceProps.Animal)
                    {
                        if (current.RaceProps.packAnimal)
                        {
                            thing = ThingMaker.MakeThing(ThingDef.Named("VehicleCart"));
                        }
                    }
                    else if (current.Faction.def.techLevel >= TechLevel.Industrial && value >= 0.75f)
                    {
                        thing = ThingMaker.MakeThing(ThingDef.Named("VehicleTruck"));
                        Thing fuel =
                            ThingMaker.MakeThing(
                                thing.TryGetComp<CompRefuelable>().Props.fuelFilter.AllowedThingDefs.FirstOrDefault());
                        fuel.stackCount += Mathf.FloorToInt(5 + Rand.Value * 15f);
                        thing.TryGetComp<CompRefuelable>().Refuel(fuel);
                    }
                    else
                    {
                        thing = ThingMaker.MakeThing(ThingDef.Named("VehicleCart"));
                    }

                    if (thing != null)
                    {
                        GenSpawn.Spawn(thing, current.Position, map);

                        Job job = new Job(HaulJobDefOf.Mount);
                        map.reservationManager.ReleaseAllForTarget(thing);
                        job.targetA = thing;
                        current.jobs.StartJob(job, JobCondition.InterruptForced, null, true);

                        SoundInfo info = SoundInfo.InMap(thing);
                        thing.TryGetComp<CompMountable>().SustainerAmbient =
                            thing.TryGetComp<CompVehicle>().compProps.soundAmbient.TrySpawnSustainer(info);
                    }

                    break;
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
            Find.LetterStack.ReceiveLetter(label, text, LetterType.Good, list[0], null);
            IntVec3 chillSpot;
            RCellFinder.TryFindRandomSpotJustOutsideColony(list[0], out chillSpot);
            LordJob_TradeWithColony lordJob = new LordJob_TradeWithColony(parms.faction, chillSpot);
            LordMaker.MakeNewLord(parms.faction, lordJob, map, list);
            return true;
        }



    }
}