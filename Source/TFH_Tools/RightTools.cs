namespace TFH_Tools
{
    using System.Linq;

    using RimWorld;

    using TFH_VehicleBase;

    using Verse;

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
            float result;
            if (thing == null || thing.def.equippedStatOffsets == null)
            {
                result = 0f;
            }
            else
            {
                foreach (StatModifier current in thing.def.equippedStatOffsets)
                {
                    if (current.stat == def)
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
            if (pawn.story != null && pawn.story.WorkTagIsDisabled(WorkTags.Violent))
                return;

            Apparel_ToolBelt toolbelt = ToolsForHaulUtility.TryGetToolbelt(pawn);

            if (toolbelt != null)
            {
                ThingWithComps thingWithComps = pawn.equipment.Primary;
                float currentStat = GetMaxStat(thingWithComps, def);

                foreach (Thing slot in toolbelt.slotsComp.slots)
                {
                    ThingWithComps thingWithComps2 = slot as ThingWithComps;
                    if (thingWithComps2 != null)
                    {
                        if (thingWithComps2.def.IsRangedWeapon || thingWithComps2.def.IsMeleeWeapon)
                        {
                            float candidateStat = GetMaxStat(thingWithComps2, def);
                            if (candidateStat > currentStat)
                            {
                                currentStat = candidateStat;
                                thingWithComps = thingWithComps2;
                            }
                        }
                    }
                }

                bool unEquipped = thingWithComps != pawn.equipment.Primary;
                if (unEquipped)
                {
                    if (!MapComponent_ToolsForHaul.PreviousPawnWeapon.ContainsKey(pawn))
                    {
                        MapComponent_ToolsForHaul.PreviousPawnWeapon.Add(pawn, pawn.equipment.Primary);
                    }
                    else
                    {
                        MapComponent_ToolsForHaul.PreviousPawnWeapon[pawn] = pawn.equipment.Primary;
                    }

                    toolbelt.slotsComp.SwapEquipment(thingWithComps);

                    // pawn.equipment.TryTransferEquipmentToContainer(pawn.equipment.Primary, pawn.inventory.innerContainer, out dummy);
                    // pawn.equipment.AddEquipment(thingWithComps);
                    // pawn.inventory.innerContainer.Remove(thingWithComps);
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
        public static Thing GetRightVehicle(
            Pawn pawn,
            WorkTypeDef worktype = null,
            Thing t = null)
        {
            Thing cart = null;
            if (worktype.Equals(WorkTypeDefOf.Hunting))
            {
                bool skip = false;
                IOrderedEnumerable<Thing> orderedEnumerable =
                    ToolsForHaulUtility.CartTurret.OrderBy(x => pawn.Position.DistanceToSquared(x.Position));


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
                        if (vehicleCart.GasTankComp.tankLeaking) continue;
                        cart = vehicleCart;
                        break;
                    }
                }
            }

            if (worktype == DefDatabase<WorkTypeDef>.GetNamed("Hauling"))
            {
                IOrderedEnumerable<Thing> orderedEnumerable2 =
                      ToolsForHaulUtility.Cart.OrderByDescending(x => (x as Vehicle_Cart).MaxItem).ThenBy(x => pawn.Position.DistanceToSquared(x.Position));

                foreach (Thing thing in orderedEnumerable2)
                {
                    Vehicle_Cart vehicleCart = (Vehicle_Cart)thing;
                    if (vehicleCart == null)
                        continue;
                    if (!ToolsForHaulUtility.AvailableVehicle(pawn, vehicleCart)) continue;
                    if (vehicleCart.GasTankComp.tankLeaking) continue;
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
                    if (!vehicleCart.IsCurrentlyMotorized()) continue;
                    if (vehicleCart.GasTankComp.tankLeaking) continue;
                    cart = vehicleCart;
                    break;
                }
            }

            return cart;
        }
    }
}
