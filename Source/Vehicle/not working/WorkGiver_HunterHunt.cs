using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RimWorld;
using ToolsForHaul.Toils;
using ToolsForHaul.Utilities;
using Verse;
using Verse.AI;

namespace ToolsForHaul.WorkGivers
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
            foreach (var animal in Find.DesignationManager.DesignationsOfDef(DesignationDefOf.Hunt))
            {
                yield return animal.target.Thing;
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
            Job Hunting = new Job(JobDefOf.Hunt, t);
            Thing cart = null;

            List<Thing> things = ToolsForHaulUtility.Cart;
            things.AddRange(ToolsForHaulUtility.CartTurret);
            if (things != null)
            {
                bool skip = false;
                things.OrderBy(x => pawn.Position.DistanceToSquared(x.Position));
                foreach (Vehicle_Turret vehicleTurret in things)
                {
                    if (vehicleTurret != null && (vehicleTurret.Faction == Faction.OfPlayer && !vehicleTurret.IsForbidden(Faction.OfPlayer) && ToolsForHaulUtility.AvailableTurretCart(vehicleTurret, pawn) && vehicleTurret.IsCurrentlyMotorized() && !vehicleTurret.tankLeaking && vehicleTurret.VehicleSpeed >= pawn.GetStatValue(StatDefOf.MoveSpeed)))
                    {
                        cart = vehicleTurret;
                        skip = true;
                        break;
                    }
                }
                if (!skip)
                {
                    foreach (Vehicle_Cart vehicleCart in things)
                    {
                        if (vehicleCart != null && (vehicleCart.Faction == Faction.OfPlayer && !vehicleCart.IsForbidden(Faction.OfPlayer) && ToolsForHaulUtility.AvailableCart(vehicleCart, pawn) && vehicleCart.IsCurrentlyMotorized() && !vehicleCart.tankLeaking && vehicleCart.VehicleSpeed >= pawn.GetStatValue(StatDefOf.MoveSpeed)))
                        {
                            cart = vehicleCart;
                            break;
                        }
                    }
                }

                Hunting.targetC = cart;
            }
            return Hunting;
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
