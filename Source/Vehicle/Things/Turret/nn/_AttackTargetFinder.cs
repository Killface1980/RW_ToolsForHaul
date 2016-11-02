using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public static class _AttackTargetFinder
    {
        // Verse.AI.AttackTargetFinder
        [Detour(typeof(AttackTargetFinder), bindingFlags = BindingFlags.Static | BindingFlags.NonPublic)]
        private static Verb GetAttackVerb(Thing attacker)
        {
            Pawn pawn = attacker as Pawn;
            if (pawn != null)
            {
                return pawn.TryGetAttackVerb(!pawn.IsColonist);
            }

            Building_Turret building_Turret = attacker as Building_Turret;
            if (building_Turret != null)
            {
                return building_Turret.AttackVerb;
            }

            Vehicle_Turret vehicle_Turret= attacker as Vehicle_Turret;
            if (vehicle_Turret != null)
            {
                return vehicle_Turret.AttackVerb;
            }
            return null;
        }

    }
}
