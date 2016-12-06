using System;
using System.Collections.Generic;

using RimWorld;

using Verse;
using Verse.AI;

namespace ToolsForHaul.JTBetterHauling
{
    public static class GenClosest_JT
    {
        public static Thing ClosestThingReachable_JT(IntVec3 root, ThingRequest thingReq, PathEndMode peMode, TraverseParms traverseParams, float maxDistance = 9999f, Predicate<Thing> validator = null, IEnumerable<Thing> customGlobalSearchSet = null, int searchRegionsMax = -1, bool forceGlobalSearch = false)
        {
            // customGlobalSearchSet in context of hauling is list of things to be hauled
            // forceGlobalSearch is true when customGlobalSearchSet is not null
            // searchRegionMax is only used for cleaning
            ProfilerThreadCheck.BeginSample("ClosestThingReachable");

            // Guessing if searchRegionsMax is > 0, customGlobalSearchSet is not suppose to be used.
            if (searchRegionsMax > 0 && customGlobalSearchSet != null && !forceGlobalSearch)
            {
                Log.ErrorOnce(
                    "searchRegionsMax > 0 && customGlobalSearchSet != null && !forceGlobalSearch. customGlobalSearchSet will never be used.",
                    634984);
            }

            // There is no null check for thingReq, always not null?
            // if thingReq group == nothing || 
            // if there is thingReq and customGlobalSearchSet is null and there are things matching thingReq
            if (EarlyOutSearch_JT(root, thingReq, customGlobalSearchSet))
            {
                ProfilerThreadCheck.EndSample();
                return null;
            }

            // Use either searchRegionsMax or customGlobalSearchSet
            // We're interested in thingReq.group == HaulableEver, HaulableAlways, HaulableEverOrMinifiable
            // This means customGlobalSearch has to have something (when there are such items on map) --> correct
            // Actual stuff begins?
            Thing thing = null;

            // IsUndefined == singleDef == null && thingReq group == Undefined
            if (!thingReq.IsUndefined)
            {
                // The debug bellow only resuted in group == Pawn, 7 times with 3 pawns all set only to haul, perhaps dogs loving?
                // Therefore probably ignore this if
                // Log.Message("First if was called. " + thingReq.group.ToString());
                // Hauling: searchRegionsMax should be -1 --> maxRegions = 30
                int maxRegions = (searchRegionsMax <= 0) ? 30 : searchRegionsMax;
                thing = GenClosest.RegionwiseBFSWorker(
                    root,
                    thingReq,
                    peMode,
                    traverseParams,
                    validator,
                    null,
                    0,
                    maxRegions,
                    maxDistance);
            }

            if (thing == null && (searchRegionsMax < 0 || forceGlobalSearch))
            {
                // validator = (Thing t) => !t.IsForbidden(pawn) && scanner.HasJobOnThing(pawn, t);
                // Debug bellow gives thingReq group to be Undefined, skips first 
                // Log.Message("First if resulted in null " + thingReq.group.ToString());
                Predicate<Thing> validator2 =
                    (Thing t) => root.CanReach(t, peMode, traverseParams) && (validator == null || validator(t));
                IEnumerable<Thing> searchSet = customGlobalSearchSet ?? Find.ListerThings.ThingsMatching(thingReq);

                // Main start of my code
                List<Thing> degrade = new List<Thing>();
                List<Thing> undegrade = new List<Thing>();

                // Seperate into degrade or not
                foreach (Thing t in searchSet)
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
                    thing = GenClosest.ClosestThing_Global(
                        root,
                        AreaFinder.searcher(a, degrade),
                        maxDistance,
                        validator2);
                    if (thing != null)
                    {
                        break;
                    }

                    // Check if got undegradable item
                    thing = GenClosest.ClosestThing_Global(
                        root,
                        AreaFinder.searcher(a, undegrade),
                        maxDistance,
                        validator2);
                    if (thing != null)
                    {
                        break;
                    }
                }

                if (thing == null)
                {
                    thing = GenClosest.ClosestThing_Global(root, degrade, maxDistance, validator2);
                    if (thing == null)
                    {
                        thing = GenClosest.ClosestThing_Global(root, undegrade, maxDistance, validator2);
                    }
                }

                /* old 50 cell code
                                thing = GenClosest.ClosestThing_Global(root, degrade, 50f, validator2); //If there is degradable with 50 cells
                                if (thing == null)
                                {
                                    thing = GenClosest.ClosestThing_Global(root, undegrade, 50f, validator2);//If there is undegradable with 50 cells
                                    if (thing == null)
                                    {
                                        thing = GenClosest.ClosestThing_Global(root, degrade, maxDistance, validator2); //If there is degradable
                                        if (thing == null)
                                        {
                                            thing = GenClosest.ClosestThing_Global(root, undegrade, maxDistance, validator2); //If there is undegradable
                                        }
                                    }
                                }
                                */
                // Main end of my code
            }

            /*
                        if (thing != null)
                        {
                            Log.Message(thing.def.defName);
                        }
                        else {
                            Log.Message("Resulted in null");
                        }*/
            ProfilerThreadCheck.EndSample();
            return thing;
        }

        /*public static Thing ClosestThing_Global_JT(IntVec3 center, IEnumerable searchSet, float maxDistance = 99999f, Predicate<Thing> validator = null)
        {
            ProfilerThreadCheck.BeginSample("ClosestThing_Global");
            if (searchSet == null)
            {
                return null;
            }
            float num = 2.14748365E+09f;
            Thing result = null;
            float num2 = maxDistance * maxDistance;
            foreach (Thing thing in searchSet)
            {
                float lengthHorizontalSquared = (center - thing.Position).LengthHorizontalSquared;
                if (lengthHorizontalSquared < num && lengthHorizontalSquared <= num2)
                {
                    ProfilerThreadCheck.BeginSample("validator");
                    if (validator != null && !validator(thing))
                    {
                        ProfilerThreadCheck.EndSample();
                    }
                    else
                    {
                        ProfilerThreadCheck.EndSample();
                        if (thing.Spawned)
                        {
                            result = thing;
                            num = lengthHorizontalSquared;
                        }
                    }
                }
            }
            ProfilerThreadCheck.EndSample();
            return result;
        }*/
        public static bool EarlyOutSearch_JT(IntVec3 start, ThingRequest thingReq, IEnumerable<Thing> customGlobalSearchSet)
        {
            if (thingReq.group == ThingRequestGroup.Everything)
            {
                Log.Error("Cannot do ClosestThingReachable searching everything without restriction.");
                return true;
            }

            if (!start.InBounds())
            {
                Log.Error(string.Concat(new object[]
                {
            "Did FindClosestThing with start out of bounds (",
            start,
            "), thingReq=",
            thingReq
                }));
                return true;
            }

            return thingReq.group == ThingRequestGroup.Nothing || (customGlobalSearchSet == null && !thingReq.IsUndefined && Find.ListerThings.ThingsMatching(thingReq).Count == 0);
        }

    }
}
