namespace TFH_Incidents
{
    using System.Collections.Generic;

    using RimWorld;

    using TFH_VehicleBase;
    using TFH_VehicleBase.DefOfs_TFH;

    using TFH_Vehicles;
    using TFH_Vehicles.DefOfs_TFH;

    using Verse;
    using Verse.AI;

    // Vanilla copy with spawning vehicles
    public class IncidentWorker_WandererJoin : IncidentWorker
    {
        private const float RelationWithColonistWeight = 20f;


        // RimWorld.IncidentWorker_WandererJoin
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 loc;
            bool result;
            if (!CellFinder.TryFindRandomEdgeCellWith((IntVec3 c) => map.reachability.CanReachColony(c), map, CellFinder.EdgeRoadChance_Neutral, out loc))
            {
                result = false;
            }
            else
            {
                PawnKindDef pawnKindDef = new List<PawnKindDef>
                                              {
                                                  PawnKindDefOf.Villager
                                              }.RandomElement<PawnKindDef>();
                PawnGenerationRequest request = new PawnGenerationRequest(pawnKindDef, Faction.OfPlayer, PawnGenerationContext.NonPlayer, -1, false, false, false, false, true, false, 20f, false, true, true, false, false, false, false, null, null, null, null, null, null, null);
                Pawn pawn = PawnGenerator.GeneratePawn(request);
                GenSpawn.Spawn(pawn, loc, map);
                string text = "WandererJoin".Translate(new object[]
                                                           {
                                                               pawnKindDef.label,
                                                               pawn.story.Title.ToLower()
                                                           });
                text = text.AdjustedFor(pawn);
                string label = "LetterLabelWandererJoin".Translate();
                PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref text, ref label, pawn);
                Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, pawn, null);

                if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                {
                    if (parms.faction.def.techLevel >= TechLevel.Industrial && pawn.RaceProps.FleshType != FleshTypeDefOf.Mechanoid && pawn.RaceProps.ToolUser)
                    {
                        float value = Rand.Value;

                        if (value >= 0.35f)
                        {
                            CellFinder.RandomClosewalkCellNear(pawn.Position, pawn.Map, 5);

                            Pawn cart = PawnGenerator.GeneratePawn(VehicleKindDefOf.TFH_ATV, parms.faction);

                            if (value >= 0.7f)
                            {
                                cart = PawnGenerator.GeneratePawn(VehicleKindDefOf.TFH_Speeder, parms.faction);
                            }

                            GenSpawn.Spawn(cart, pawn.Position, map, Rot4.Random, false);

                            pawn.Map.reservationManager.ReleaseAllForTarget(cart);
                            Job job = new Job(VehicleJobDefOf.Mount) { targetA = cart };
                            pawn.Reserve(cart, job);
                            pawn.jobs.jobQueue.EnqueueFirst(job);
                        }
                    }
                }




                result = true;
            }
            return result;
        }

            
        
    }
}
