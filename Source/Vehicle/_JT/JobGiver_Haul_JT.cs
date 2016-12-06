using System;
using System.Collections.Generic;
using System.Reflection;

using RimWorld;

using Verse;
using Verse.AI;

namespace ToolsForHaul.JTBetterHauling
{
    public class JobGiver_Haul_JT : ThinkNode_JobGiver
    {
        // This is for animal haul
        [Detour(typeof(JobGiver_Haul), bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic)]
        protected override Job TryGiveJob(Pawn pawn)
        {
            Predicate<Thing> validator =
                (Thing t) => !t.IsForbidden(pawn) && HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t);

            // Start my code
            Thing thing = null;
            List<Thing> things = ListerHaulables.ThingsPotentiallyNeedingHauling();
            List<Thing> degrade = new List<Thing>();
            List<Thing> undegrade = new List<Thing>();
            foreach (Thing t in things)
            {
                if (t.GetStatValue(StatDefOf.DeteriorationRate) > 0)
                {
                    degrade.Add(t);
                }
                else
                {
                    undegrade.Add(t);
                }
            }

            // Loop through all haul areas in order
            foreach (Area a in AreaFinder.getHaulAreas())
            {
                // Check if got degradable item
                thing = GenClosest.ClosestThing_Global_Reachable(
                    pawn.Position,
                    AreaFinder.searcher(a, degrade),
                    PathEndMode.OnCell,
                    TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false),
                    9999f,
                    validator,
                    null);
                if (thing != null)
                {
                    break;
                }

                // Check if got undegradable item
                thing = GenClosest.ClosestThing_Global_Reachable(
                    pawn.Position,
                    AreaFinder.searcher(a, undegrade),
                    PathEndMode.OnCell,
                    TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false),
                    9999f,
                    validator,
                    null);
                if (thing != null)
                {
                    break;
                }
            }

            if (thing == null)
            {
                thing = GenClosest.ClosestThing_Global_Reachable(
                    pawn.Position,
                    degrade,
                    PathEndMode.OnCell,
                    TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false),
                    9999f,
                    validator,
                    null);
                if (thing == null)
                {
                    thing = GenClosest.ClosestThing_Global_Reachable(
                        pawn.Position,
                        undegrade,
                        PathEndMode.OnCell,
                        TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false),
                        9999f,
                        validator,
                        null);
                }
            }

            /* old 50 cell code
                        Thing thing = GenClosest.ClosestThing_Global_Reachable(pawn.Position, degrade, PathEndMode.OnCell, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), 50f, validator, null);
                        if (thing == null)
                        {
                            thing = GenClosest.ClosestThing_Global_Reachable(pawn.Position, undegrade, PathEndMode.OnCell, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), 50f, validator, null);
                            if (thing == null)
                            {
                                thing = GenClosest.ClosestThing_Global_Reachable(pawn.Position, degrade, PathEndMode.OnCell, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, validator, null);
                                if (thing == null)
                                {
                                    thing = GenClosest.ClosestThing_Global_Reachable(pawn.Position, undegrade, PathEndMode.OnCell, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, validator, null);
                                }
                            }
                        }*/
            // End my code
            if (thing != null)
            {
                return HaulAIUtility.HaulToStorageJob(pawn, thing);
            }

            return null;
        }
    }
}
