using System.Collections.Generic;
using RimWorld;
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


        public static void EquipRigthTool(Pawn pawn, StatDef def)
        {
            Apparel_Toolbelt toolbelt = ToolsForHaulUtility.TryGetToolbelt(pawn);
            CompSlotsToolbelt slotsToolbelt = toolbelt.GetComp<CompSlotsToolbelt>();

            bool flag = toolbelt != null;
            if (flag)
            {
                ThingWithComps thingWithComps = pawn.equipment.Primary;
                float stat = GetMaxStat(pawn.equipment.Primary, def);

                foreach (ThingWithComps slot in slotsToolbelt.slots)
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

              //using (IEnumerator<Thing> enumerator = pawn.inventory.container.GetEnumerator())
              //{
              //    while (enumerator.MoveNext())
              //    {
              //        ThingWithComps thingWithComps2 = (ThingWithComps)enumerator.Current;
              //        bool flag2 = !thingWithComps2.def.IsRangedWeapon && !thingWithComps2.def.IsMeleeWeapon;
              //        if (!flag2)
              //        {
              //            float maxStat = GetMaxStat(thingWithComps2, def);
              //            bool flag3 = stat < maxStat;
              //            if (flag3)
              //            {
              //                stat = maxStat;
              //                thingWithComps = thingWithComps2;
              //            }
              //        }
              //    }
              //}
                bool unEquipped = thingWithComps != pawn.equipment.Primary;
                if (unEquipped)
                {
                    ThingWithComps dummy;

                    if (!MapComponent_ToolsForHaul.previousPawnWeapons.ContainsKey(pawn))
                        MapComponent_ToolsForHaul.previousPawnWeapons.Add(pawn, pawn.equipment.Primary);

                    slotsToolbelt.SwapEquipment(thingWithComps);


                  //pawn.equipment.TryTransferEquipmentToContainer(pawn.equipment.Primary, pawn.inventory.container, out dummy);
                  //pawn.equipment.AddEquipment(thingWithComps);
                  //pawn.inventory.container.Remove(thingWithComps);
                }
                else
                {
                    bool flag5 = stat == 0f && def != StatDefOf.WorkSpeedGlobal;
                    if (flag5)
                    {
                        EquipRigthTool(pawn, StatDefOf.WorkSpeedGlobal);
                    }
                }
            }
        }
    }
}
