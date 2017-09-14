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
    using Verse.AI.Group;

    // Vanilla copy with edited base class
    public class IncidentWorker_TravelerGroup : IncidentWorker_NeutralGroup
    {
        public override bool TryExecute(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!this.TryResolveParms(parms))
            {
                return false;
            }

            IntVec3 travelDest;
            if (!RCellFinder.TryFindTravelDestFrom(parms.spawnCenter, map, out travelDest))
            {
                Log.Warning("Failed to do traveler incident from " + parms.spawnCenter + ": couldn't find anywhere for the traveler to go.");
                return false;
            }

            List<Pawn> list = this.SpawnPawns(parms);
            if (list.Count == 0)
            {
                return false;
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
                    Pawn cart = PawnGenerator.GeneratePawn(VehicleKindDefOf.TFH_ATV, parms.faction);

                    if (Rand.Value >= 0.9f)
                    {
                        cart = PawnGenerator.GeneratePawn(VehicleKindDefOf.TFH_CombatATV, parms.faction);
                    }

                    GenSpawn.Spawn(cart, current.Position, map, Rot4.Random, false);

                    current.Map.reservationManager.ReleaseAllForTarget(cart);
                    Job job = new Job(VehicleJobDefOf.Mount) { targetA = cart };
                    current.Reserve(cart);
                    current.jobs.jobQueue.EnqueueFirst(job);
                }
            }

            string text;
            if (list.Count == 1)
            {
                text = "SingleTravelerPassing".Translate(new object[]
                                                             {
                                                                 list[0].story.Title.ToLower(),
                                                                 parms.faction.Name,
                                                                 list[0].Name
                                                             });
                text = text.AdjustedFor(list[0]);
            }
            else
            {
                text = "GroupTravelersPassing".Translate(new object[]
                                                             {
                                                                 parms.faction.Name
                                                             });
            }

            Messages.Message(text, list[0], MessageSound.Standard);
            LordJob_TravelAndExit lordJob = new LordJob_TravelAndExit(travelDest);
            LordMaker.MakeNewLord(parms.faction, lordJob, map, list);
            string empty = string.Empty;
            string empty2 = string.Empty;
            PawnRelationUtility.Notify_PawnsSeenByPlayer(list, ref empty, ref empty2, "LetterRelatedPawnsNeutralGroup".Translate(), true);
            if (!empty2.NullOrEmpty())
            {
                Find.LetterStack.ReceiveLetter(empty, empty2, LetterDefOf.Good, list[0], null);
            }

            return true;
        }
    }
}
