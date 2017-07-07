using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ToolsForHaul
{
    internal class VehicleRecipesUtility
    {
        public static bool IsCleanAndDroppable(Pawn pawn, BodyPartRecord part)
        {
            return true;
            return !pawn.Dead && !pawn.RaceProps.Animal && part.def.spawnThingOnRemoved != null && IsClean(pawn, part);
        }

        public static bool IsClean(Pawn pawn, BodyPartRecord part)
        {
            return true;
            return !pawn.Dead && !(from x in pawn.health.hediffSet.hediffs
                                   where x.Part == part
                                   select x).Any<Hediff>();
        }

        public static void RestorePartAndSpawnAllPreviousParts(Pawn pawn, BodyPartRecord part, IntVec3 pos, Map map)
        {
            SpawnNaturalPartIfClean(pawn, part, pos, map);
            SpawnThingsFromHediffs(pawn, part, pos, map);
            pawn.health.RestorePart(part);
        }

        public static Thing SpawnNaturalPartIfClean(Pawn pawn, BodyPartRecord part, IntVec3 pos, Map map)
        {
            if (IsCleanAndDroppable(pawn, part))
            {
                return GenSpawn.Spawn(part.def.spawnThingOnRemoved, pos, map);
            }

            return null;
        }

        public static void SpawnThingsFromHediffs(Pawn pawn, BodyPartRecord part, IntVec3 pos, Map map)
        {
            if (!pawn.health.hediffSet.GetNotMissingParts().Contains(part))
            {
                return;
            }

            IEnumerable<Hediff> enumerable = from x in pawn.health.hediffSet.hediffs
                                             where x.Part == part
                                             select x;
            foreach (Hediff current in enumerable)
            {
                if (current.def.spawnThingOnRemoved != null)
                {
                    GenSpawn.Spawn(current.def.spawnThingOnRemoved, pos, map);
                }
            }

            for (int i = 0; i < part.parts.Count; i++)
            {
                SpawnThingsFromHediffs(pawn, part.parts[i], pos, map);
            }
        }
    }
}
