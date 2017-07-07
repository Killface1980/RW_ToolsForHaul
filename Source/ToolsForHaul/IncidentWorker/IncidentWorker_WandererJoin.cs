namespace ToolsForHaul.IncidentWorker
{
    using System.Collections.Generic;

    using RimWorld;

    using ToolsForHaul.Components;
    using ToolsForHaul.Defs;

    using UnityEngine;

    using Verse;
    using Verse.AI;
    using Verse.Sound;

    // Vanilla copy with spawning vehicles
    public class IncidentWorker_WandererJoin : IncidentWorker
    {
        private const float RelationWithColonistWeight = 20f;

        public override bool TryExecute(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 loc;
            if (!CellFinder.TryFindRandomEdgeCellWith(c => map.reachability.CanReachColony(c), map, CellFinder.EdgeRoadChance_Neutral, out loc))
            {
                return false;
            }

            PawnKindDef pawnKindDef = new List<PawnKindDef>
                                          {
                                              PawnKindDefOf.Villager
                                          }.RandomElement();
            PawnGenerationRequest request = new PawnGenerationRequest(pawnKindDef, Faction.OfPlayer, PawnGenerationContext.NonPlayer, -1, false, false, false, false, true, false, 20f, false, true, true, false, false, null, null, null, null, null);
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            GenSpawn.Spawn(pawn, loc, map);

            // Vehicle
            if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                if (parms.faction.def.techLevel >= TechLevel.Industrial && pawn.RaceProps.FleshType != FleshTypeDefOf.Mechanoid && pawn.RaceProps.ToolUser)
                {
                    float value = Rand.Value;

                    if (value >= 0.35f)
                    {
                        CellFinder.RandomClosewalkCellNear(pawn.Position, pawn.Map, 5);

                        Pawn cart = PawnGenerator.GeneratePawn(VehicleKindDefOf.ATV, parms.faction);

                        if (value >= 0.7f)
                        {
                            cart = PawnGenerator.GeneratePawn(VehicleKindDefOf.Speeder, parms.faction);
                        }
                        GenSpawn.Spawn(cart, pawn.Position, map, Rot4.Random, false);
                        pawn.Reserve(cart);

                        Job job = new Job(HaulJobDefOf.Mount);
                        pawn.Map.reservationManager.ReleaseAllForTarget(cart);
                        pawn.Reserve(cart);
                        job.targetA = cart;
                        pawn.jobs.StartJob(job, JobCondition.InterruptForced, null, true);

                    }
                }}



            string text = "WandererJoin".Translate(
                pawnKindDef.label,
                pawn.story.Title.ToLower());
            text = text.AdjustedFor(pawn);
            string label = "LetterLabelWandererJoin".Translate();
            PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref text, ref label, pawn);
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.Good, pawn);
            return true;
        }

            
        
    }
}
