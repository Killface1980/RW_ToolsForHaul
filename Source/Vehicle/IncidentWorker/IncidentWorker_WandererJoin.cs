using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using ToolsForHaul.Components;
using ToolsForHaul.JobDefs;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ToolsForHaul.IncidentWorkers
{
    public class IncidentWorker_WandererJoin : IncidentWorker
    {
        private const float RelationWithColonistWeight = 20f;

        public override bool TryExecute(IncidentParms parms)
        {
            IntVec3 loc;
            if (!CellFinder.TryFindRandomEdgeCellWith((IntVec3 c) => c.CanReachColony(), out loc))
            {
                return false;
            }
            PawnKindDef pawnKindDef = new List<PawnKindDef>
            {
                PawnKindDefOf.Villager
            }.RandomElement<PawnKindDef>();
            PawnGenerationRequest request = new PawnGenerationRequest(pawnKindDef, Faction.OfPlayer, PawnGenerationContext.NonPlayer, false, false, false, false, true, false, RelationWithColonistWeight, false, true, true, null, null, null, null, null, null);
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            GenSpawn.Spawn(pawn, loc);

            // vehicle generation
            // Vehicles for raiders
            // lowered probability for shield users as they are overpowered


            if (pawn.RaceProps.ToolUser)
            {
                float value = Rand.Value;
                if (value >= 0.5f)
                {
                    CellFinder.RandomClosewalkCellNear(pawn.Position, 5);
                    Thing thing;

                    if (value >= 0.95f)
                    {
                        thing = ThingMaker.MakeThing(ThingDef.Named("VehicleCombatATV"));
                    }
                    else
                    {
                        thing = ThingMaker.MakeThing(ThingDef.Named("VehicleATV"));
                    }

                    GenSpawn.Spawn(thing, pawn.Position);

                    Thing fuel = ThingMaker.MakeThing(thing.TryGetComp<CompRefuelable>().Props.fuelFilter.AllowedThingDefs.FirstOrDefault());
                    fuel.stackCount += Mathf.FloorToInt(5 + Rand.Value * 15f);
                    thing.TryGetComp<CompRefuelable>().Refuel(fuel);
                    int num2 = Mathf.FloorToInt(Rand.Value * 0.2f * thing.MaxHitPoints);
                    thing.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, num2, null, null));
                    thing.SetFaction(Faction.OfPlayer);

                    Job job = new Job(HaulJobDefOf.Mount);
                    Find.Reservations.ReleaseAllForTarget(thing);
                    job.targetA = thing;
                    pawn.jobs.StartJob(job, JobCondition.InterruptForced, null, true);

                    SoundInfo info = SoundInfo.InWorld(thing);
                    thing.TryGetComp<CompMountable>().sustainerAmbient = thing.TryGetComp<CompVehicle>().compProps.soundAmbient.TrySpawnSustainer(info);
                }
            }


            string text = "WandererJoin".Translate(new object[]
            {
                pawnKindDef.label,
                pawn.story.adulthood.title.ToLower()
            });
            text = text.AdjustedFor(pawn);
            string label = "LetterLabelWandererJoin".Translate();
            PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref text, ref label, pawn);
            Find.LetterStack.ReceiveLetter(label, text, LetterType.Good, pawn, null);
            return true;
        }
    }
}
