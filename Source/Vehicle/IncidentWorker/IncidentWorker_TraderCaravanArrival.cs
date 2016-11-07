using System.Collections.Generic;
using System.Linq;
using RimWorld;
using ToolsForHaul.Components;
using ToolsForHaul.JobDefs;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace ToolsForHaul.IncidentWorkers
{
    public class IncidentWorker_TraderCaravanArrival : IncidentWorker_NeutralGroup
    {
        protected override bool FactionCanBeGroupSource(Faction f, bool desperate = false)
        {
            return base.FactionCanBeGroupSource(f, desperate) && f.def.caravanTraderKinds.Any();
        }

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
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].needs != null && list[i].needs.food != null)
                {
                    list[i].needs.food.CurLevel = list[i].needs.food.MaxLevel;
                }
            }
            TraderKindDef traderKindDef = null;
            foreach (Pawn current in list)
            {
                bool spawnCart = false;
                if (current.TraderKind != null)
                {
                    traderKindDef = current.TraderKind;
                    spawnCart = true;
                }
                float value = Rand.Value;

                Thing thing = null;
                if (current.RaceProps.Animal)
                {
                    if (current.kindDef.carrier)
                    {
                        thing = ThingMaker.MakeThing(ThingDef.Named("VehicleCart"));
                        spawnCart = true;
                    }
                }
                else if (current.Faction.def.techLevel >= TechLevel.Industrial && value >= 0.75f)
                {
                    thing = ThingMaker.MakeThing(ThingDef.Named("VehicleTruck"));
                    Thing fuel = ThingMaker.MakeThing(thing.TryGetComp<CompRefuelable>().Props.fuelFilter.AllowedThingDefs.FirstOrDefault());
                    fuel.stackCount += Mathf.FloorToInt(5 + Rand.Value * 15f);
                    thing.TryGetComp<CompRefuelable>().Refuel(fuel);
                }
                else
                {
                    thing = ThingMaker.MakeThing(ThingDef.Named("VehicleCart"));
                }



                if (spawnCart)
                {
                    GenSpawn.Spawn(thing, current.Position);

                    Job job = new Job(HaulJobDefOf.Mount);
                    Find.Reservations.ReleaseAllForTarget(thing);
                    job.targetA = thing;
                    current.jobs.StartJob(job, JobCondition.InterruptForced, null, true);

                    SoundInfo info = SoundInfo.InWorld(thing);
                    thing.TryGetComp<CompMountable>().sustainerAmbient = thing.TryGetComp<CompVehicle>().compProps.soundAmbient.TrySpawnSustainer(info);
                }

            }
            Find.LetterStack.ReceiveLetter("LetterLabelTraderCaravanArrival".Translate(parms.faction.Name, traderKindDef.label).CapitalizeFirst(), "LetterTraderCaravanArrival".Translate(parms.faction.Name, traderKindDef.label).CapitalizeFirst(), LetterType.Good, list[0], null);
            IntVec3 chillSpot;
            RCellFinder.TryFindRandomSpotJustOutsideColony(list[0], out chillSpot);
            LordJob_TradeWithColony lordJob = new LordJob_TradeWithColony(parms.faction, chillSpot);
            LordMaker.MakeNewLord(parms.faction, lordJob, list);
            return true;
        }

        protected override bool TryResolveParms(IncidentParms parms)
        {
            if (!base.TryResolveParms(parms))
            {
                return false;
            }
            parms.traderCaravan = true;
            return true;
        }
    }
}
