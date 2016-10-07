using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public class WorkGiver_EquipTools : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            List<Thing> potentialWorkThingsGlobal = new List<Thing>();

            foreach (Thing thing in Find.ListerThings.AllThings)
            {
                float statfloat = 0;
                if (!thing.def.IsMeleeWeapon || !pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, Danger.Deadly))
                    continue;
                foreach (KeyValuePair<StatDef, float> stat in pawn.GetWeightedWorkStats())
                {
                    statfloat += RightTools.GetMaxStat(thing as ThingWithComps, stat.Key);
                    if (statfloat > 0)
                    {
                        potentialWorkThingsGlobal.Add(thing);
                    }
                }

            }


            return potentialWorkThingsGlobal;
        }


        public override bool ShouldSkip(Pawn pawn)
        {
            Apparel_Backpack backpack = ToolsForHaulUtility.TryGetBackpack(pawn);
            //Should skip pawn that don't have backpack.
            if (backpack == null)
                return true;

            if (backpack.MaxItem - backpack.numOfSavedItems < 2)
            {
                return true;
            }

            return false;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t)
        {
            if (!pawn.inventory.container.Contains(t.def) || pawn.equipment.Primary.def.Equals(t.def))
                return true;
            return false;

        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            Apparel_Backpack backpack = ToolsForHaulUtility.TryGetBackpack(pawn);

            if (backpack != null)
            {
                Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("PutInInventory"));
                jobNew.maxNumToCarry = 1;
                jobNew.targetA = backpack;
                jobNew.targetQueueB = new List<TargetInfo>();
                jobNew.targetQueueB.Add(t);
                pawn.Reserve(t);

                return jobNew;

            }

            JobFailReason.Is("NoBackpack".Translate());
            return null;
        }
    }
}
