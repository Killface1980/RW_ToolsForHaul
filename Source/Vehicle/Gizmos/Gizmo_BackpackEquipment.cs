
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ToolsForHaul
{
    [StaticConstructorOnStartup]
    public class Gizmo_BackpackEquipment : Gizmo
    {
        private static string txtNoItem;
        private static string txtThingInfo;
        private static string txtDropThing;

        //Links
        public Apparel_Backpack backpack;

        //Constants
        private static readonly Texture2D FilledTex = SolidColorMaterials.NewSolidColorTexture(1f, 1f, 1f, 0.10f);
        private static readonly Texture2D EmptyTex = SolidColorMaterials.NewSolidColorTexture(0.4f, 0.4f, 0.4f, 0.15f);
        private static readonly Texture2D NoAvailableTex = SolidColorMaterials.NewSolidColorTexture(0.0f, 0.0f, 0.0f, 1.0f);

        private static readonly Texture2D MeleeWeaponTex = SolidColorMaterials.NewSolidColorTexture(0.6f, 0.15f, 0.15f, 0.75f);
        private static readonly Texture2D RangedWeaponTex = SolidColorMaterials.NewSolidColorTexture(0.09f, 0.25f, 0.6f, 0.75f);

        //private static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        //private static readonly ThingCategoryDef weaponMelee = DefDatabase<ThingCategoryDef>.GetNamed("WeaponsMelee");
        //private static readonly ThingCategoryDef weaponRanged = DefDatabase<ThingCategoryDef>.GetNamed("WeaponsRanged");
        private static readonly ThingCategoryDef medicine = DefDatabase<ThingCategoryDef>.GetNamed("Medicine");
        private static readonly ThingCategoryDef foodMeals = DefDatabase<ThingCategoryDef>.GetNamed("FoodMeals");

        //Properties
        private const int textHeight = 30;
        private const int textWidth = 60;

        //EquipmentSlot Properties
        private const int numOfRow = 2;
        private const int numOfMaxItemsPerRow = 4;
        private const float widthPerItem = 140.0f / 4.0f;
        private const float curWidth = widthPerItem * numOfMaxItemsPerRow;
        public override float Width { get { return curWidth; } }

        //IconClickSound
        private SoundDef thingIconSound;

        public Gizmo_BackpackEquipment()
        {
            txtNoItem = "NoItem".Translate();
            txtThingInfo = "ThingInfo".Translate();
            txtDropThing = "DropThing".Translate();
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft)
        {
            Rect overRect = new Rect(topLeft.x, topLeft.y, curWidth, Height);
            Widgets.DrawWindowBackground(overRect);

            //Equipment slot
            Pawn wearer = backpack.wearer;
            ThingWithComps dummy;

            Rect thingIconRect = new Rect(topLeft.x, topLeft.y, widthPerItem, Height / 2);
            int numOfCurItem = 0;

            List<Thing> things = wearer.inventory.container.ToList();

            //Draw Gizmo
            for (int i = 0; i < numOfMaxItemsPerRow * numOfRow; i++)
            {
                if (i >= backpack.MaxItem)
                {
                    thingIconRect.x = topLeft.x + widthPerItem * (i % numOfMaxItemsPerRow);
                    thingIconRect.y = topLeft.y + (Height / 2) * (i / numOfMaxItemsPerRow);
                    Widgets.DrawTextureFitted(thingIconRect, NoAvailableTex, 1.0f);
                    continue;
                }

                if (i >= things.Count)
                {
                    thingIconRect.x = topLeft.x + widthPerItem * (i % numOfMaxItemsPerRow);
                    thingIconRect.y = topLeft.y + (Height / 2) * (i / numOfMaxItemsPerRow);
                    Widgets.DrawTextureFitted(thingIconRect, EmptyTex, 1.0f);


                    continue;
                }

                Thing item = things[i];

                if (item == null)
                    continue;

                if (item.def.IsMeleeWeapon)
                {
                    Widgets.DrawTextureFitted(thingIconRect, MeleeWeaponTex, 1.0f);
                }
                if (item.def.IsRangedWeapon)
                {
                    Widgets.DrawTextureFitted(thingIconRect, RangedWeaponTex, 1.0f);
                }
                Widgets.DrawBox(thingIconRect);
                Widgets.ThingIcon(thingIconRect, item);
                if (thingIconRect.Contains(Event.current.mousePosition))
                {
                    Widgets.DrawTextureFitted(thingIconRect, FilledTex, 1.0f);
                }
                //Interaction with item
                if (Widgets.ButtonInvisible(thingIconRect))
                {
                    thingIconSound = SoundDefOf.Click;
                    if (Event.current.button == 0)
                    {
                        //Weapon
                        if (item != null && item.def.equipmentType == EquipmentType.Primary && item.def.IsRangedWeapon)
                            {
                                if (wearer.equipment.Primary != null)
                                    wearer.equipment.TryTransferEquipmentToContainer(wearer.equipment.Primary, wearer.inventory.container, out dummy);
                                else
                                    backpack.numOfSavedItems--;
                                wearer.equipment.AddEquipment(item as ThingWithComps);
                                wearer.inventory.container.Remove(item as ThingWithComps);
                                if (wearer.jobs.curJob != null)
                                    wearer.jobs.EndCurrentJob(JobCondition.InterruptForced);
                            }
                    }
                    if (Event.current.button == 1)
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();

                        options.Add(new FloatMenuOption(txtThingInfo, () =>
                        {
                            Find.WindowStack.Add(new Dialog_InfoCard(item));
                        }));

                        options.Add(new FloatMenuOption(txtDropThing, () =>
                        {
                            Thing dummy1;
                            wearer.inventory.container.TryDrop(item, wearer.Position, ThingPlaceMode.Near, out dummy1);
                        }));

                        //Weapon
                        if (item != null && item.def.equipmentType == EquipmentType.Primary)
                            options.Add(new FloatMenuOption("Equip".Translate(item.LabelCap), () =>
                            {
                                if (wearer.equipment.Primary != null)
                                    wearer.equipment.TryTransferEquipmentToContainer(wearer.equipment.Primary, wearer.inventory.container, out dummy);
                                else
                                    backpack.numOfSavedItems--;
                                wearer.equipment.AddEquipment(item as ThingWithComps);
                                wearer.inventory.container.Remove(item as ThingWithComps);
                                if (wearer.jobs.curJob != null)
                                    wearer.jobs.EndCurrentJob(JobCondition.InterruptForced);
                            }));


                        //Medicine
                        if (item.def.thingCategories.Contains(medicine))
                        {
                            if (wearer.workSettings != null && wearer.workSettings.WorkIsActive(WorkTypeDefOf.Doctor))
                                options.Add(new FloatMenuOption("TreatPatientWithMedicine".Translate(), () =>
                                {
                                    Designator_ApplyMedicine designator = new Designator_ApplyMedicine();
                                    designator.medicine = item;
                                    designator.doctor = wearer;
                                    designator.icon = item.def.uiIcon;
                                    designator.activateSound = SoundDef.Named("Click");
                        
                                    DesignatorManager.Select(designator);
                                }));
                        }
                        //Food
                        if (item.def.thingCategories.Contains(foodMeals))
                        {
                            //  if (wearer.needs.food.CurCategory != HungerCategory.Fed)
                            options.Add(new FloatMenuOption("ConsumeThing".Translate(item.def.LabelCap), () =>
                            {
                                Job jobNew = new Job(JobDefOf.Ingest, item);
                                jobNew.maxNumToCarry = item.def.ingestible.maxNumToIngestAtOnce;
                                jobNew.ignoreForbidden = true;
                                wearer.drafter.TakeOrderedJob(jobNew);
                            }));
                        }
                        // Drugs
                        if (item.def.ingestible?.drugCategory >= DrugCategory.Any)
                            options.Add(new FloatMenuOption("ConsumeThing".Translate(), () =>
                            {
                                Job jobNew = new Job(JobDefOf.Ingest, item);
                                jobNew.maxNumToCarry = item.def.ingestible.maxNumToIngestAtOnce;
                                jobNew.ignoreForbidden = true;
                                wearer.drafter.TakeOrderedJob(jobNew);
                            }));
                        //Apparel
                        if (item is Apparel)
                            options.Add(new FloatMenuOption("Wear".Translate(item.def.LabelCap), () =>
                            {
                                //if (!wearer.apparel.CanWearWithoutDroppingAnything(item.def))
                                //    wearer.apparel.WornApparel.Find(apparel => apparel.def.apparel.layers.Any);
                                for (int index = wearer.apparel.WornApparel.Count - 1; index >= 0; --index)
                                {
                                    Apparel ap = wearer.apparel.WornApparel[index];
                                    if (ApparelUtility.CanWearTogether(item.def, ap.def)) continue;
                                    Apparel resultingAp;
                                    wearer.apparel.TryDrop(ap, out resultingAp, wearer.Position, false);
                                    wearer.inventory.container.TryAdd(resultingAp);
                                    backpack.numOfSavedItems++;
                                }
                                wearer.apparel.Wear(item as Apparel);
                                wearer.inventory.container.Remove(item as ThingWithComps);
                                backpack.numOfSavedItems--;
                            }));

                        Find.WindowStack.Add(new FloatMenu(options, item.LabelCap));
                    }

                    thingIconSound.PlayOneShotOnCamera();
                }
                if (Mouse.IsOver(thingIconRect))
                {
                    GUI.DrawTexture(thingIconRect, TexUI.HighlightTex);
                }
                TooltipHandler.TipRegion(thingIconRect, item.def.LabelCap);

                numOfCurItem++;
                thingIconRect.x = topLeft.x + widthPerItem * (numOfCurItem % numOfMaxItemsPerRow);
                thingIconRect.y = topLeft.y + (Height / 2) * (numOfCurItem / numOfMaxItemsPerRow);
            }

            if (numOfCurItem == 0)
            {
                Rect textRect = new Rect(topLeft.x + Width / 2 - textWidth / 2, topLeft.y + Height / 2 - textHeight / 2, textWidth, textHeight);
                Widgets.Label(textRect, txtNoItem.Translate());
            }
            return new GizmoResult(GizmoState.Clear);
        }


    }

}