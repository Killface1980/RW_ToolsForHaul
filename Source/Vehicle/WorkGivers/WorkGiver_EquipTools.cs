using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using ToolsForHaul.JobDefs;
using ToolsForHaul.Utilities;
using Verse;
using Verse.AI;

namespace ToolsForHaul.WorkGivers
{
    public class WorkGiver_EquipTools : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            List<Thing> potentialWorkThingsGlobal = new List<Thing>();

            foreach (Thing thing in pawn.Map.listerThings.AllThings)
            {
                float statfloat = 0;
                if (!thing.def.IsWeapon)
                {
                    continue;
                }
                if (!HaulAIUtility.PawnCanAutomaticallyHaul(pawn, thing))
                {
                    continue;
                }
                bool grabNewTool = false;
                foreach (KeyValuePair<StatDef, float> stat in pawn.GetWeightedWorkStats())
                {
                    statfloat = RightTools.GetMaxStat(thing as ThingWithComps, stat.Key);
                    if (statfloat > 0)
                    {
                        grabNewTool = true;
                        // Should skip if already better tool in inventory
                        foreach (var entry in MapComponent_ToolsForHaul.CachedToolEntries.Where(x => x.pawn != null && x.pawn == pawn))
                        {
                            if (entry.stat == stat.Key)
                            {
                                if (entry.workStat > statfloat)
                                {
                                    grabNewTool = false;
                                }
                            }
                        }
                    }
                }
                if (grabNewTool)
                {
                    potentialWorkThingsGlobal.Add(thing);
                }
            }
            return potentialWorkThingsGlobal;
        }


        public override bool ShouldSkip(Pawn pawn)
        {
            Apparel_Toolbelt toolbelt = ToolsForHaulUtility.TryGetToolbelt(pawn);

            // Pacifists are incapable of using tools/weapons
            if (pawn.story != null && pawn.story.WorkTagIsDisabled(WorkTags.Violent))
                return true;

            // Should skip pawn that don't have a toolbelt.
            if (toolbelt == null) return true;

            // Skip it toolbelt full
            if (toolbelt.MaxItem <= toolbelt.slotsComp.slots.Count)
            {
                return true;
            }

            return false;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t)
        {
            Apparel_Toolbelt toolbelt = ToolsForHaulUtility.TryGetToolbelt(pawn);
            if (toolbelt == null)
                return false;

            if (!HaulAIUtility.PawnCanAutomaticallyHaul(pawn, t))
                return false;

            if (toolbelt.slotsComp.slots.Contains(t.def))
                return false;

            if (pawn.equipment.Primary != null && pawn.equipment.Primary.def.Equals(t.def))
                return false;


            return true;

        }

        public override Job JobOnThing(Pawn pawn, Thing thing)
        {
            Apparel_Toolbelt toolbelt = ToolsForHaulUtility.TryGetToolbelt(pawn);

            if (toolbelt != null)
            {
                Job jobNew = new Job(HaulJobDefOf.PutInToolbeltSlot);
                jobNew.targetQueueA = new List<LocalTargetInfo>();
                jobNew.countQueue = new List<int>();
                jobNew.targetB = toolbelt;
                jobNew.targetQueueA.Add(thing);
                jobNew.countQueue.Add(thing.def.stackLimit);
                pawn.Reserve(thing);

                return jobNew;
            }

            JobFailReason.Is("NoToolbelt".Translate());
            return null;
        }
    }
}
