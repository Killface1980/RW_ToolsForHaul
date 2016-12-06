using System.Linq;
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
            Apparel_Toolbelt toolbelt = ToolsForHaulUtility.TryGetToolbelt(pawn);

            bool flag = toolbelt != null;
            if (flag)
            {
                ThingWithComps thingWithComps = pawn.equipment.Primary;
                float stat = GetMaxStat(pawn.equipment.Primary, def);

                foreach (ThingWithComps slot in toolbelt.slotsComp.slots)
                {
                    ThingWithComps thingWithComps2 = slot;
                    bool flag2 = !thingWithComps2.def.IsRangedWeapon && !thingWithComps2.def.IsMeleeWeapon;
                    if (!flag2)
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

                // using (IEnumerator<Thing> enumerator = pawn.inventory.container.GetEnumerator())
                // {
                // while (enumerator.MoveNext())
                // {
                // ThingWithComps thingWithComps2 = (ThingWithComps)enumerator.Current;
                // bool flag2 = !thingWithComps2.def.IsRangedWeapon && !thingWithComps2.def.IsMeleeWeapon;
                // if (!flag2)
                // {
                // float maxStat = GetMaxStat(thingWithComps2, def);
                // bool flag3 = stat < maxStat;
                // if (flag3)
                // {
                // stat = maxStat;
                // thingWithComps = thingWithComps2;
                // }
                // }
                // }
                // }
                bool unEquipped = thingWithComps != pawn.equipment.Primary;
                if (unEquipped)
                {
                    if (!MapComponent_ToolsForHaul.previousPawnWeapons.ContainsKey(pawn)) MapComponent_ToolsForHaul.previousPawnWeapons.Add(pawn, pawn.equipment.Primary);

                    toolbelt.slotsComp.SwapEquipment(thingWithComps);

                    // pawn.equipment.TryTransferEquipmentToContainer(pawn.equipment.Primary, pawn.inventory.container, out dummy);
                    // pawn.equipment.AddEquipment(thingWithComps);
                    // pawn.inventory.container.Remove(thingWithComps);
                }

                // else
                // {
                // bool flag5 = stat == 0f && def != StatDefOf.WorkSpeedGlobal;
                // if (flag5)
                // {
                // EquipRigthTool(pawn, StatDefOf.WorkSpeedGlobal);
                // }
                // }
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
                    if (!vehicleTurret.vehicleComp.IsCurrentlyMotorized()) continue;
                    if (vehicleTurret.vehicleComp.tankLeaking) continue;
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
                        if (!vehicleCart.VehicleComp.IsCurrentlyMotorized()) continue;
                        if (vehicleCart.VehicleComp.tankLeaking) continue;
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
                    if (vehicleCart.VehicleComp.tankLeaking) continue;
                    cart = vehicleCart;
                    break;
                }
            }

            if (worktype.Equals(WorkTypeDefOf.Construction))
            {
                IOrderedEnumerable<Thing> orderedEnumerable2 =
                      ToolsForHaulUtility.Cart.OrderBy(x => pawn.Position.DistanceToSquared(x.Position)).ThenByDescending(x => (x as Vehicle_Cart).VehicleComp.VehicleSpeed);
                foreach (Thing thing in orderedEnumerable2)
                {
                    Vehicle_Cart vehicleCart = (Vehicle_Cart)thing;
                    if (vehicleCart == null)
                        continue;
                    if (!ToolsForHaulUtility.AvailableVehicle(pawn, vehicleCart)) continue;
                    if (!vehicleCart.VehicleComp.IsCurrentlyMotorized()) continue;
                    if (vehicleCart.VehicleComp.tankLeaking) continue;
                    cart = vehicleCart;
                    break;
                }
            }

            return cart;
        }
    }
}
