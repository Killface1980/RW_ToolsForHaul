using System.Linq;
using Combat_Realism;
using RimWorld;
using ToolsForHaul.Utilities;
using Verse;

namespace ToolsForHaul
{
    [StaticConstructorOnStartup]
    public static class RightTools
    {
        static RightTools()
        {
            PreLoad();
        }

        private static void PreLoad()
        {
        }

        public static float GetMaxStat(ThingWithComps thing, StatDef def)
        {
            bool flag = thing == null || thing.def.equippedStatOffsets == null;
            float result;
            if (flag)
            {
                result = 0f;
            }
            else
            {
                foreach (StatModifier current in thing.def.equippedStatOffsets)
                {
                    bool flag2 = current.stat == def;
                    if (flag2)
                    {
                        result = current.value;
                        return result;
                    }
                }
                result = 0f;
            }
            return result;
        }

        /// <summary>
        /// Equips tools
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="def"></param>
        public static void EquipRigthTool(Pawn pawn, StatDef def)
        {
            CompInventory compInventory = ThingCompUtility.TryGetComp<CompInventory>(pawn);
            bool flag = compInventory != null;
            if (flag)
            {
                ThingWithComps thingWithComps = pawn.equipment.Primary;
                float stat = GetMaxStat(pawn.equipment.Primary, def);

                foreach (ThingWithComps slot in compInventory.container)
                {
                    ThingWithComps thingWithComps2 = slot;
                    bool flag2 = thingWithComps2.def.IsWeapon;
                    if (flag2)
                    {
                        float maxStat = GetMaxStat(thingWithComps2, def);
                        bool flag3 = stat < maxStat;
                        if (flag3)
                        {
                            stat = maxStat;
                            thingWithComps = thingWithComps2;
                        }
                    }
                }

                bool unEquipped = thingWithComps != pawn.equipment.Primary;
                if (unEquipped)
                {
                    compInventory.TrySwitchToWeapon(thingWithComps);
                }
                else
                {
                    bool flag5 = stat == 0f && def != StatDefOf.WorkSpeedGlobal;
                    if (flag5)
                    {
                        RightTools.EquipRigthTool(pawn, StatDefOf.WorkSpeedGlobal);
                    }
                }

            }
        }

        /// <summary>
        /// Selects the appropriate vehicle by worktype
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="worktype"></param>
        /// <returns></returns>
        public static Thing GetRightVehicle(Pawn pawn, WorkTypeDef worktype, Thing t = null)
        {
            Thing cart = null;
            if (worktype.Equals(WorkTypeDefOf.Hunting))
            {
                bool skip = false;
                IOrderedEnumerable<Thing> orderedEnumerable =
                    ToolsForHaulUtility.CartTurret.OrderBy(x => pawn.Position.DistanceToSquared(x.Position));
                foreach (Thing thing in orderedEnumerable)
                {
                    Vehicle_Turret vehicleTurret = (Vehicle_Turret)thing;
                    if (vehicleTurret == null) continue;
                    if (!ToolsForHaulUtility.AvailableVehicle(pawn, vehicleTurret)) continue;
                    if (!vehicleTurret.IsCurrentlyMotorized()) continue;
                    if (vehicleTurret.vehiclesComp.tankLeaking) continue;
                    cart = vehicleTurret;
                    skip = true;
                    break;
                }
                if (!skip)
                {
                    IOrderedEnumerable<Thing> orderedEnumerable2 =
                          ToolsForHaulUtility.Cart.OrderBy(x => pawn.Position.DistanceToSquared(x.Position));
                    foreach (Thing thing in orderedEnumerable2)
                    {
                        Vehicle_Cart vehicleCart = (Vehicle_Cart)thing;
                        if (vehicleCart == null)
                            continue;
                        if (!ToolsForHaulUtility.AvailableVehicle(pawn, vehicleCart)) continue;
                        if (!vehicleCart.IsCurrentlyMotorized()) continue;
                        if (vehicleCart.vehiclesComp.tankLeaking) continue;
                        cart = vehicleCart;
                        break;
                    }
                }
            }
            if (worktype.Equals(WorkTypeDefOf.Hauling))
            {
                IOrderedEnumerable<Thing> orderedEnumerable2 =
                      ToolsForHaulUtility.Cart.OrderByDescending(x => (x as Vehicle_Cart).MaxItem).ThenBy(x=>pawn.Position.DistanceToSquared(x.Position));
                
                foreach (Thing thing in orderedEnumerable2)
                {
                    Vehicle_Cart vehicleCart = (Vehicle_Cart)thing;
                    if (vehicleCart == null)
                        continue;
                    if (!ToolsForHaulUtility.AvailableVehicle(pawn, vehicleCart)) continue;
                    if (vehicleCart.vehiclesComp.tankLeaking) continue;
                    cart = vehicleCart;
                    break;
                }
            }
            if (worktype.Equals(WorkTypeDefOf.Construction))
            {
                IOrderedEnumerable<Thing> orderedEnumerable2 =
                      ToolsForHaulUtility.Cart.OrderBy(x => pawn.Position.DistanceToSquared(x.Position)).ThenByDescending(x => (x as Vehicle_Cart).VehicleSpeed);
                foreach (Thing thing in orderedEnumerable2)
                {
                    Vehicle_Cart vehicleCart = (Vehicle_Cart)thing;
                    if (vehicleCart == null)
                        continue;
                    if (!ToolsForHaulUtility.AvailableVehicle(pawn, vehicleCart)) continue;
                    if (!vehicleCart.IsCurrentlyMotorized()) continue;
                    if (vehicleCart.vehiclesComp.tankLeaking) continue;
                    cart = vehicleCart;
                    break;
                }
            }
            return cart;
        }
    }
}
