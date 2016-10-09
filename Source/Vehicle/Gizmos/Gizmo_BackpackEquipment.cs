
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

        public static readonly Texture2D texPutInArrow = ContentFinder<Texture2D>.Get("UI/Gizmoes/PutIn");
        private static readonly Texture2D texOccupiedSlotBG = SolidColorMaterials.NewSolidColorTexture(1f, 1f, 1f, 0.1f);


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
        private float curWidth
        {
            get
            {
                switch (backpack.GetComp<CompSlots>().Properties.maxSlots)
                {
                    case 8:
                        return Height * 2;
                    default:
                        return Height;
                }
            }
        }
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
                    thingIconRect.y = topLeft.y + Height / 2 * (i / numOfMaxItemsPerRow);
                    Widgets.DrawTextureFitted(thingIconRect, NoAvailableTex, 1.0f);
                    continue;
                }

                if (i >= things.Count)
                {
                    thingIconRect.x = topLeft.x + widthPerItem * (i % numOfMaxItemsPerRow);
                    thingIconRect.y = topLeft.y + Height / 2 * (i / numOfMaxItemsPerRow);
                    Widgets.DrawTextureFitted(thingIconRect, EmptyTex, 1.0f);


                    continue;
                }

                Thing currentThing = things[i];

                if (currentThing == null)
                    continue;

                if (currentThing.def.IsMeleeWeapon)
                {
                    Widgets.DrawTextureFitted(thingIconRect, MeleeWeaponTex, 1.0f);
                }
                if (currentThing.def.IsRangedWeapon)
                {
                    Widgets.DrawTextureFitted(thingIconRect, RangedWeaponTex, 1.0f);
                }
                Widgets.DrawBox(thingIconRect);
                Widgets.ThingIcon(thingIconRect, currentThing);
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
                        if (currentThing != null && currentThing.def.equipmentType == EquipmentType.Primary && currentThing.def.IsRangedWeapon)
                            {
                                if (wearer.equipment.Primary != null)
                                    wearer.equipment.TryTransferEquipmentToContainer(wearer.equipment.Primary, wearer.inventory.container, out dummy);
                                else
                                    backpack.numOfSavedItems--;
                                wearer.equipment.AddEquipment(currentThing as ThingWithComps);
                                wearer.inventory.container.Remove(currentThing as ThingWithComps);
                                if (wearer.jobs.curJob != null)
                                    wearer.jobs.EndCurrentJob(JobCondition.InterruptForced);
                            }
                    }
                    if (Event.current.button == 1)
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();

                        options.Add(new FloatMenuOption(txtThingInfo, () =>
                        {
                            Find.WindowStack.Add(new Dialog_InfoCard(currentThing));
                        }));

                        options.Add(new FloatMenuOption(txtDropThing, () =>
                        {
                            Thing dummy1;
                            wearer.inventory.container.TryDrop(currentThing, wearer.Position, ThingPlaceMode.Near, out dummy1);
                        }));

                        //Weapon
                        if (currentThing != null && currentThing.def.equipmentType == EquipmentType.Primary)
                            options.Add(new FloatMenuOption("Equip".Translate(currentThing.LabelCap), () =>
                            {
                                if (wearer.equipment.Primary != null)
                                    wearer.equipment.TryTransferEquipmentToContainer(wearer.equipment.Primary, wearer.inventory.container, out dummy);
                                else
                                    backpack.numOfSavedItems--;
                                wearer.equipment.AddEquipment(currentThing as ThingWithComps);
                                wearer.inventory.container.Remove(currentThing as ThingWithComps);
                                if (wearer.jobs.curJob != null)
                                    wearer.jobs.EndCurrentJob(JobCondition.InterruptForced);
                            }));


                        //Medicine
                        if (currentThing.def.thingCategories.Contains(medicine))
                        {
                            if (wearer.workSettings != null && wearer.workSettings.WorkIsActive(WorkTypeDefOf.Doctor))
                                options.Add(new FloatMenuOption("TreatPatientWithMedicine".Translate(), () =>
                                {
                                    Designator_ApplyMedicine designator = new Designator_ApplyMedicine();
                                    designator.medicine = currentThing;
                                    designator.doctor = wearer;
                                    designator.icon = currentThing.def.uiIcon;
                                    designator.activateSound = SoundDef.Named("Click");
                        
                                    DesignatorManager.Select(designator);
                                }));
                        }
                        //Food
                        if (currentThing.def.thingCategories.Contains(foodMeals))
                        {
                            //  if (wearer.needs.food.CurCategory != HungerCategory.Fed)
                            options.Add(new FloatMenuOption("ConsumeThing".Translate(currentThing.def.LabelCap), () =>
                            {
                                Job jobNew = new Job(JobDefOf.Ingest, currentThing);
                                jobNew.maxNumToCarry = currentThing.def.ingestible.maxNumToIngestAtOnce;
                                jobNew.ignoreForbidden = true;
                                wearer.drafter.TakeOrderedJob(jobNew);
                            }));
                        }
                        // Drugs
                        if (currentThing.def.ingestible?.drugCategory >= DrugCategory.Any)
                            options.Add(new FloatMenuOption("ConsumeThing".Translate(), () =>
                            {
                                Job jobNew = new Job(JobDefOf.Ingest, currentThing);
                                jobNew.maxNumToCarry = currentThing.def.ingestible.maxNumToIngestAtOnce;
                                jobNew.ignoreForbidden = true;
                                wearer.drafter.TakeOrderedJob(jobNew);
                            }));
                        //Apparel
                        if (currentThing is Apparel)
                            options.Add(new FloatMenuOption("Wear".Translate(currentThing.def.LabelCap), () =>
                            {
                                //if (!wearer.apparel.CanWearWithoutDroppingAnything(item.def))
                                //    wearer.apparel.WornApparel.Find(apparel => apparel.def.apparel.layers.Any);
                                for (int index = wearer.apparel.WornApparel.Count - 1; index >= 0; --index)
                                {
                                    Apparel ap = wearer.apparel.WornApparel[index];
                                    if (ApparelUtility.CanWearTogether(currentThing.def, ap.def)) continue;
                                    Apparel resultingAp;
                                    wearer.apparel.TryDrop(ap, out resultingAp, wearer.Position, false);
                                    wearer.inventory.container.TryAdd(resultingAp);
                                    backpack.numOfSavedItems++;
                                }
                                wearer.apparel.Wear(currentThing as Apparel);
                                wearer.inventory.container.Remove(currentThing as ThingWithComps);
                                backpack.numOfSavedItems--;
                            }));

                        Find.WindowStack.Add(new FloatMenu(options, currentThing.LabelCap));
                    }

                    thingIconSound.PlayOneShotOnCamera();
                }
                if (Mouse.IsOver(thingIconRect))
                {
                    GUI.DrawTexture(thingIconRect, TexUI.HighlightTex);
                }
                TooltipHandler.TipRegion(thingIconRect, currentThing.def.LabelCap);

                numOfCurItem++;
                thingIconRect.x = topLeft.x + widthPerItem * (numOfCurItem % numOfMaxItemsPerRow);
                thingIconRect.y = topLeft.y + Height / 2 * (numOfCurItem / numOfMaxItemsPerRow);
            }

            if (numOfCurItem == 0)
            {
                Rect textRect = new Rect(topLeft.x + Width / 2 - textWidth / 2, topLeft.y + Height / 2 - textHeight / 2, textWidth, textHeight);
                Widgets.Label(textRect, txtNoItem.Translate());
            }

            // Slots CompSlots
            {
                Rect gizmoRect = new Rect(topLeft.x, topLeft.y, Width, Height);

                

                Rect inventoryRect = new Rect(gizmoRect.x + overRect.width, gizmoRect.y, curWidth, Height);
                Widgets.DrawWindowBackground( inventoryRect);
                CompSlots backpackSlots = backpack.GetComp<CompSlots>();
                DrawSlots(wearer, backpackSlots, inventoryRect);
            }

            return new GizmoResult(GizmoState.Clear);
        }

        public void DrawSlots(Pawn wearer, CompSlots slotsComp, Rect inventoryRect)
        {
            // draw text message if no contents inside
            if (slotsComp.slots.Count == 0)
            {
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(inventoryRect, "No Items");
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Tiny;
            }
            // draw slots
            else
            {
                Rect slotRect = new Rect(inventoryRect.x, inventoryRect.y, Height / 2, Height / 2);
                for (int currentSlotInd = 0; currentSlotInd < numOfMaxItemsPerRow * numOfRow; currentSlotInd++)
                {
                    // draw occupied slots
                    if (currentSlotInd < slotsComp.slots.Count)
                    {
                        slotRect.x = inventoryRect.x + Height / 2 * (currentSlotInd % numOfMaxItemsPerRow);
                        slotRect.y = inventoryRect.y + Height / 2 * (currentSlotInd / numOfMaxItemsPerRow);

                        Thing currentThing = slotsComp.slots[currentSlotInd];

                        // draws greyish slot background
                        Widgets.DrawTextureFitted(slotRect.ContractedBy(3f), texOccupiedSlotBG, 1f);

                        if (currentThing.def.IsMeleeWeapon)
                        {
                            Widgets.DrawTextureFitted(slotRect, MeleeWeaponTex, 1.0f);
                        }
                        if (currentThing.def.IsRangedWeapon)
                        {
                            Widgets.DrawTextureFitted(slotRect, RangedWeaponTex, 1.0f);
                        }
                        Widgets.DrawBox(slotRect);

                        // highlights slot if mouse over
                        Widgets.DrawHighlightIfMouseover(slotRect.ContractedBy(3f));

                        // tooltip if mouse over
                        TooltipHandler.TipRegion(slotRect, currentThing.def.LabelCap);

                        // draw thing texture
                        Widgets.ThingIcon(slotRect, currentThing);

                        // interaction with slots
                        if (Widgets.ButtonInvisible(slotRect))
                        {
                            // mouse button pressed
                            if (Event.current.button == 0)
                            {
                                //Weapon
                                if (currentThing != null && currentThing.def.equipmentType == EquipmentType.Primary && (currentThing.def.IsRangedWeapon || currentThing.def.IsMeleeWeapon))
                                {
                                    slotsComp.SwapEquipment(currentThing as ThingWithComps);
                                }

                                //// equip weapon in slot
                                //if (currentThing.def.equipmentType == EquipmentType.Primary)
                                //{
                                //    slotsComp.SwapEquipment(currentThing as ThingWithComps);
                                //}
                            }
                            // mouse button released
                            else if (Event.current.button == 1)
                            {
                                List<FloatMenuOption> options = new List<FloatMenuOption>
                                {
                                    new FloatMenuOption("Info",
                                        () => { Find.WindowStack.Add(new Dialog_InfoCard(currentThing)); }),
                                    new FloatMenuOption("Drop", () =>
                                    {
                                        Thing resultThing;
                                        slotsComp.slots.TryDrop(currentThing, wearer.Position,
                                            ThingPlaceMode.Near, out resultThing);
                                    })
                                };
                                // get thing info card
                                // drop thing

                                // Else

                                //Weapon
                                if (currentThing != null && currentThing.def.equipmentType == EquipmentType.Primary)
                                    options.Add(new FloatMenuOption("Equip".Translate(currentThing.LabelCap), () =>
                                    {
                                        slotsComp.SwapEquipment(currentThing as ThingWithComps);

                                    }));


                                //Medicine
                                if (currentThing.def.IsMedicine)
                                {
                                    if (wearer.workSettings != null && wearer.workSettings.WorkIsActive(WorkTypeDefOf.Doctor))
                                        options.Add(new FloatMenuOption("TreatPatientWithMedicine".Translate(), () =>
                                        {
                                            Designator_ApplyMedicine designator = new Designator_ApplyMedicine();
                                            designator.slotsComp = slotsComp;
                                            designator.medicine = currentThing;
                                            designator.doctor = wearer;
                                            designator.icon = currentThing.def.uiIcon;
                                            designator.activateSound = SoundDef.Named("Click");

                                            DesignatorManager.Select(designator);
                                        }));
                                }
                                //Food
                                if (currentThing.def.thingCategories.Contains(DefDatabase<ThingCategoryDef>.GetNamed("FoodMeals")))
                                {
                                    //  if (compSlots.owner.needs.food.CurCategory != HungerCategory.Fed)
                                    options.Add(new FloatMenuOption("ConsumeThing".Translate(currentThing.def.LabelCap), () =>
                                    {
                                        Thing dummy;
                                        slotsComp.slots.TryDrop(currentThing, wearer.Position, ThingPlaceMode.Direct, currentThing.def.ingestible.maxNumToIngestAtOnce, out dummy);

                                        Job jobNew = new Job(JobDefOf.Ingest, dummy);
                                        jobNew.maxNumToCarry = currentThing.def.ingestible.maxNumToIngestAtOnce;
                                        jobNew.ignoreForbidden = true;
                                        wearer.drafter.TakeOrderedJob(jobNew);
                                    }));
                                }
                                // Drugs
                                if (currentThing.def.ingestible?.drugCategory >= DrugCategory.Any)
                                    options.Add(new FloatMenuOption("ConsumeThing".Translate(), () =>
                                    {
                                        Thing dummy;
                                        slotsComp.slots.TryDrop(currentThing, wearer.Position, ThingPlaceMode.Direct, currentThing.def.ingestible.maxNumToIngestAtOnce, out dummy);

                                        Job jobNew = new Job(JobDefOf.Ingest, dummy);
                                        jobNew.maxNumToCarry = dummy.def.ingestible.maxNumToIngestAtOnce;
                                        jobNew.ignoreForbidden = true;
                                        wearer.drafter.TakeOrderedJob(jobNew);
                                    }));



                                Find.WindowStack.Add(new FloatMenu(options, currentThing.LabelCap));
                            }

                            // plays click sound on each click
                            SoundDefOf.Click.PlayOneShotOnCamera();
                        }
                    }
                    slotRect.x = inventoryRect.x + Height / 2 * (currentSlotInd % numOfMaxItemsPerRow);
                    slotRect.y = inventoryRect.y + Height / 2 * (currentSlotInd / numOfMaxItemsPerRow);
                    //      slotRect.x += Height;
                }
            }
        }



    }

}