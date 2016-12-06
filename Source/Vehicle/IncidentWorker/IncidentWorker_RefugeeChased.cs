using System.Linq;
using RimWorld;
using ToolsForHaul.Components;
using ToolsForHaul.JobDefs;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ToolsForHaul.IncidentWorker
{
    using ToolsForHaul.Components.Vehicle;
    using ToolsForHaul.Components.Vehicles;

    using IncidentWorker = RimWorld.IncidentWorker;

    public class IncidentWorker_RefugeeChased : IncidentWorker
    {
        private const float RaidPointsFactor = 1.35f;

        private const float RelationWithColonistWeight = 20f;

        private static readonly IntRange RaidDelay = new IntRange(1000, 2500);

        public override bool TryExecute(IncidentParms parms)
        {
            IntVec3 spawnSpot;
            if (!CellFinder.TryFindRandomEdgeCellWith(c => c.CanReachColony(), out spawnSpot))
            {
                return false;
            }

            Faction faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Spacer);
            PawnGenerationRequest request = new PawnGenerationRequest(PawnKindDefOf.SpaceRefugee, faction, PawnGenerationContext.NonPlayer, false, false, false, false, true, false, RelationWithColonistWeight, false, true, true, null, null, null, null, null);
            Pawn refugee = PawnGenerator.GeneratePawn(request);
            refugee.relations.everSeenByPlayer = true;
            Faction enemyFac;
            if (!(from f in Find.FactionManager.AllFactions
                  where !f.def.hidden && f.HostileTo(Faction.OfPlayer)
                  select f).TryRandomElement(out enemyFac))
            {
                return false;
            }

            string text = "RefugeeChasedInitial".Translate(refugee.Name.ToStringFull, refugee.story.adulthood.title.ToLower(), enemyFac.def.pawnsPlural, enemyFac.Name, refugee.ageTracker.AgeBiologicalYears);
            text = text.AdjustedFor(refugee);
            PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref text, refugee);
            DiaNode diaNode = new DiaNode(text);
            DiaOption diaOption = new DiaOption("RefugeeChasedInitial_Accept".Translate());
            diaOption.action = delegate
            {
                GenSpawn.Spawn(refugee, spawnSpot);
                refugee.SetFaction(Faction.OfPlayer);

                // Refugee stole vehicle?
                float value = Rand.Value;
                if (enemyFac.def.techLevel >= TechLevel.Industrial && value >= 0.33f)
                {
                    CellFinder.RandomClosewalkCellNear(spawnSpot, 5);
                    Thing thing;
                    if (value >= 0.8f)
                    {
                        thing = ThingMaker.MakeThing(ThingDef.Named("VehicleCombatATV"));
                    }
                    else
                    {
                        thing = ThingMaker.MakeThing(ThingDef.Named("VehicleATV"));
                    }

                    GenSpawn.Spawn(thing, spawnSpot);

                    Thing fuel = ThingMaker.MakeThing(thing.TryGetComp<CompRefuelable>().Props.fuelFilter.AllowedThingDefs.FirstOrDefault());
                    fuel.stackCount += Mathf.FloorToInt(5 + Rand.Value * 15f);
                    thing.TryGetComp<CompRefuelable>().Refuel(fuel);
                    int num2 = Mathf.FloorToInt(Rand.Value * 0.3f * thing.MaxHitPoints);
                    thing.TakeDamage(new DamageInfo(DamageDefOf.Bullet, num2, null, null));
                    thing.SetFaction(Faction.OfPlayer);

                    Job job = new Job(HaulJobDefOf.Mount);
                    Find.Reservations.ReleaseAllForTarget(thing);
                    job.targetA = thing;
                    refugee.jobs.StartJob(job, JobCondition.InterruptForced);

                    SoundInfo info = SoundInfo.InWorld(thing);
                    thing.TryGetComp<CompMountable>().SustainerAmbient = thing.TryGetComp<CompVehicle>().compProps.soundAmbient.TrySpawnSustainer(info);
                }


                Find.CameraDriver.JumpTo(spawnSpot);
                IncidentParms incidentParms = StorytellerUtility.DefaultParmsNow(Find.Storyteller.def, IncidentCategory.ThreatBig);
                incidentParms.forced = true;
                incidentParms.faction = enemyFac;
                incidentParms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                incidentParms.raidArrivalMode = PawnsArriveMode.EdgeWalkIn;
                incidentParms.spawnCenter = spawnSpot;
                incidentParms.points *= RaidPointsFactor;
                QueuedIncident qi = new QueuedIncident(new FiringIncident(IncidentDefOf.RaidEnemy, null, incidentParms), Find.TickManager.TicksGame + RaidDelay.RandomInRange);
                Find.Storyteller.incidentQueue.Add(qi);
            };
            diaOption.resolveTree = true;
            diaNode.options.Add(diaOption);
            string text2 = "RefugeeChasedRejected".Translate(refugee.NameStringShort);
            DiaNode diaNode2 = new DiaNode(text2);
            DiaOption diaOption2 = new DiaOption("OK".Translate());
            diaOption2.resolveTree = true;
            diaNode2.options.Add(diaOption2);
            DiaOption diaOption3 = new DiaOption("RefugeeChasedInitial_Reject".Translate());
            diaOption3.action = delegate
            {
                Find.WorldPawns.PassToWorld(refugee);
            };
            diaOption3.link = diaNode2;
            diaNode.options.Add(diaOption3);
            Find.WindowStack.Add(new Dialog_NodeTree(diaNode, true, true));
            return true;
        }
    }
}
