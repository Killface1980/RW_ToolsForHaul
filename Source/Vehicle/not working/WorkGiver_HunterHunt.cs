using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public class WorkGiver_HunterHunt : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.OnCell;
            }
        }

        [DebuggerHidden]
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            IEnumerator<Designation> enumerator = Find.DesignationManager.DesignationsOfDef(DesignationDefOf.Hunt).GetEnumerator();
            while (enumerator.MoveNext())
            {
                Designation current = enumerator.Current;
                yield return current.target.Thing;
            }
            yield break;
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            return !HasHuntingWeapon(pawn) || HasShieldAndRangedWeapon(pawn);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t)
        {
            Pawn pawn2 = t as Pawn;
            return pawn2 != null && pawn2.RaceProps.Animal && pawn.CanReserve(t, 1) && Find.DesignationManager.DesignationOn(t, DesignationDefOf.Hunt) != null;
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            return new Job(JobDefOf.Hunt, t);
        }

        public static bool HasHuntingWeapon(Pawn p)
        {
            if (p.equipment.Primary != null && p.equipment.Primary.def.IsRangedWeapon) return true;

            if (MapComponent_ToolsForHaul.previousPawnWeapons.ContainsKey(p))
            {
                Apparel_Toolbelt toolbelt = ToolsForHaulUtility.TryGetToolbelt(p);
                if (toolbelt != null && toolbelt.slotsComp.slots.Any())
                {
                    foreach (Thing slot in toolbelt.slotsComp.slots)
                    {
                        if (slot.Equals(MapComponent_ToolsForHaul.previousPawnWeapons[p]))
                        {
                            return true;
                        }
                    }
                }

            }

            return false;
        }

        public static bool HasShieldAndRangedWeapon(Pawn p)
        {
            if (p.equipment.Primary != null && !p.equipment.Primary.def.Verbs[0].MeleeRange)
            {
                List<Apparel> wornApparel = p.apparel.WornApparel;
                for (int i = 0; i < wornApparel.Count; i++)
                {
                    if (wornApparel[i] is PersonalShield)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
