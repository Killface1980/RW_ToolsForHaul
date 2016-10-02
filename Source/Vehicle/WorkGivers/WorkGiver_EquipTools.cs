using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public class WorkGiver_EquipTools : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            var statdict = new Dictionary<Thing, float>();

            foreach (Thing thing in Find.ListerThings.AllThings)
            {
                float statfloat = 0;
                if (!thing.def.IsMeleeWeapon)
                    continue;
                foreach (var stat in pawn.GetWeightedWorkStats())
                {
                    statfloat += RightTools.GetMaxStat(thing as ThingWithComps, stat.Key);
                }
                if (statfloat > 0)
                {
                    statdict.Add(thing, statfloat);
                }
            }

            return statdict.Keys;
        }


        public override bool ShouldSkip(Pawn pawn)
        {
            var backpack = ToolsForHaulUtility.TryGetBackpack(pawn);
            //Should skip pawn that don't have backpack.
            if (backpack == null)
                return true;

            if (backpack.MaxItem / 2 < backpack.numOfSavedItems)
            {
                return true;
            }

            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            var backpack = ToolsForHaulUtility.TryGetBackpack(pawn);
            if (backpack != null)
            {
                Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("PutInInventory"));
                jobNew.maxNumToCarry = 1;
                jobNew.targetA = backpack;
                jobNew.targetQueueB = new List<TargetInfo>();
                jobNew.targetQueueB.Add(t);

                return jobNew;
                //if (backpack.wearer.drafter.CanTakePlayerJob())
                    backpack.wearer.drafter.TakeOrderedJob(jobNew);
                //else
                //    backpack.wearer.drafter.QueueJob(jobNew);
            }

            JobFailReason.Is("NoBackpack".Translate());
            return null;
        }
    }
}
