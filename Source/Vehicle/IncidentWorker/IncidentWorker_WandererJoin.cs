using System;
using System.Collections.Generic;
using Verse;

namespace ToolsForHaul.IncidentWorker
{
    using System.Linq;

    using RimWorld;

    using ToolsForHaul.Components;
    using ToolsForHaul.Components.Vehicle;
    using ToolsForHaul.JobDefs;

    using UnityEngine;

    using Verse.AI;
    using Verse.Sound;

    public class IncidentWorker_WandererJoin : IncidentWorker
    {
        private const float RelationWithColonistWeight = 20f;

        public override bool TryExecute(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 loc;
            if (!CellFinder.TryFindRandomEdgeCellWith((IntVec3 c) => map.reachability.CanReachColony(c), map, out loc))
            {
                return false;
            }
            PawnKindDef pawnKindDef = new List<PawnKindDef>
            {
                PawnKindDefOf.Villager
            }.RandomElement<PawnKindDef>();
            PawnGenerationRequest request = new PawnGenerationRequest(pawnKindDef, Faction.OfPlayer, PawnGenerationContext.NonPlayer, null, false, false, false, false, true, false, 20f, false, true, true, null, null, null, null, null, null);
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            GenSpawn.Spawn(pawn, loc, map);

            // vehicle generation
            if (pawn.RaceProps.ToolUser)
            {
                float value = Rand.Value;
                if (value >= 0.5f)
                {
                    CellFinder.RandomClosewalkCellNear(pawn.Position,map, 5);
                    Thing thing;

                    if (value >= 0.95f)
                    {
                        thing = ThingMaker.MakeThing(ThingDef.Named("VehicleCombatATV"));
                    }
                    else if (value >= 0.75f)
                    {
                        thing = ThingMaker.MakeThing(ThingDef.Named("VehicleSpeeder"));
                    }
                    else
                    {
                        thing = ThingMaker.MakeThing(ThingDef.Named("VehicleATV"));
                    }

                    GenSpawn.Spawn(thing, pawn.Position, pawn.Map);

                    Thing fuel = ThingMaker.MakeThing(thing.TryGetComp<CompRefuelable>().Props.fuelFilter.AllowedThingDefs.FirstOrDefault());
                    fuel.stackCount += Mathf.FloorToInt(5 + Rand.Value * 15f);
                    thing.TryGetComp<CompRefuelable>().Refuel(fuel);
                    int num2 = Mathf.FloorToInt(Rand.Value * 0.2f * thing.MaxHitPoints);
                    thing.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, num2,-1f, null, null));
                    thing.SetFaction(Faction.OfPlayer);

                    Job job = new Job(HaulJobDefOf.Mount);
                    map.reservationManager.ReleaseAllForTarget(thing);
                    job.targetA = thing;
                    pawn.jobs.StartJob(job, JobCondition.InterruptForced, null, true);

                    SoundInfo info = SoundInfo.InMap(thing);
                    thing.TryGetComp<CompMountable>().SustainerAmbient = thing.TryGetComp<CompVehicle>().compProps.soundAmbient.TrySpawnSustainer(info);
                }
            }


            string text = "WandererJoin".Translate(new object[]
            {
                pawnKindDef.label,
                pawn.story.Title.ToLower()
            });
            text = text.AdjustedFor(pawn);
            string label = "LetterLabelWandererJoin".Translate();
            PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref text, ref label, pawn);
            Find.LetterStack.ReceiveLetter(label, text, LetterType.Good, pawn, null);
            return true;
        }
    }
}

     