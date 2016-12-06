using System;
using System.Collections.Generic;

using RimWorld;

using Verse;

namespace ToolsForHaul.JTBetterHauling
{
    static class AreaFinder
    {
        // Returns Home area, then any areas that contain "haul" in alphabetical order
        public static List<Area> getHaulAreas()
        {
            List<string> areaName = new List<string>();
            foreach (Area a in Find.AreaManager.AllAreas)
            {
                if (a is Area_Allowed && a.Label.ToLower().Contains("haul"))
                {
                    areaName.Add(a.Label);
                }
            }

            areaName.Sort();

            List<Area> areas = new List<Area>();
            foreach (string s in areaName)
            {
                areas.Add(Find.AreaManager.GetLabeled(s));
            }

            areas.Add(Find.AreaHome);
            return areas;
        }

        // Returns things that are in area
        public static IEnumerable<Thing> searcher(Area area, List<Thing> things)
        {
            List<Thing> results = new List<Thing>();
            foreach (Thing thing in things)
            {
                if (area.GetCellBool(CellIndices.CellToIndex(thing.Position)))
                {
                    yield return thing;
                }
            }

            yield break;
        }
    }
}
