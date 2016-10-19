//#define DEBUG

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public class WorkGiver_HaulWithBackpack : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            var list = new List<Thing>();
            var backpack = ToolsForHaulUtility.TryGetBackpack(pawn);
            foreach (var thing in ListerHaulables.ThingsPotentiallyNeedingHauling())
            {
                if (thing.def.thingCategories.Exists(category => backpack.slotsComp.Properties.allowedThingCategoryDefs.Exists(subCategory => subCategory.ThisAndChildCategoryDefs.Contains(category)) && !backpack.slotsComp.Properties.forbiddenSubThingCategoryDefs.Exists(subCategory => subCategory.ThisAndChildCategoryDefs.Contains(category))))
                    list.Add(thing);
                //return ToolsForHaulUtility.Cart();
            }
            return list;
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            Trace.DebugWriteHaulingPawn(pawn);

            Apparel_Backpack backpack = ToolsForHaulUtility.TryGetBackpack(pawn);

            //Should skip pawn that don't have backpack.
            if (backpack == null)
                return true;
            if (backpack.MaxItem - backpack.slotsComp.slots.Count == 0)
            {
                return true;
            }
            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            if (t is Corpse)
            {
                return null;
            }

            if (!HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t))
            {
                return null;
            }
            Apparel_Backpack backpack = ToolsForHaulUtility.TryGetBackpack(pawn);
            if (backpack != null)
            {
                if (
                    !t.def.thingCategories.Exists(
                        category =>
                            backpack.slotsComp.Properties.allowedThingCategoryDefs.Exists(
                                subCategory => subCategory.ThisAndChildCategoryDefs.Contains(category)) &&
                            !backpack.slotsComp.Properties.forbiddenSubThingCategoryDefs.Exists(
                                subCategory => subCategory.ThisAndChildCategoryDefs.Contains(category))))
                {
                    JobFailReason.Is("Backpack can't hold that thing");
                    return null;
                }
                else
                {

                    return ToolsForHaulUtility.HaulWithTools(pawn);
                }
            }

            JobFailReason.Is("NoBackpack".Translate());
            return null;
        }
    }
}