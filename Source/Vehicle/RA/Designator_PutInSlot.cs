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
    public class Designator_PutInSlot : Designator
    {
        public static readonly Texture2D texPutInArrow = ContentFinder<Texture2D>.Get("UI/Gizmoes/PutIn");
        private static readonly Texture2D texOccupiedSlotBG = SolidColorMaterials.NewSolidColorTexture(1f, 1f, 1f, 0.1f);

        public CompSlots slotsComp;
        private static readonly Texture2D MeleeWeaponTex = SolidColorMaterials.NewSolidColorTexture(0.6f, 0.15f, 0.15f, 0.75f);
        private static readonly Texture2D RangedWeaponTex = SolidColorMaterials.NewSolidColorTexture(0.09f, 0.25f, 0.6f, 0.75f);

        // overall gizmo width
        // Height = 75f
        //      public override float Width => slotsComp.Properties.maxSlots * Height + Height;

        public Designator_PutInSlot()
        {
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.DesignateHaul;
            soundDragSustain = SoundDefOf.DesignateDragStandard;
            soundDragChanged = SoundDefOf.DesignateDragStandardChanged;
            activateSound = SoundDef.Named("Click");
        }

        //EquipmentSlot Properties
        private const int numOfRow = 2;
        private int numOfMaxItemsPerRow
        {
            get
            {
                switch (slotsComp.Properties.maxSlots)
                {
                    case 8:
                        return 4;
                    default:
                        return 2;
                }
            }
        }
        private float curWidth
        {
            get
            {
                switch (slotsComp.Properties.maxSlots)
                {
                    case 8:
                        return Height * 2;
                    default:
                        return Height;
                }
            }
        }
        public override float Width
        {
            get
            {
                switch (slotsComp.Properties.maxSlots)
                {
                    case 8:
                        return Height * 3;
                    default:
                        return Height * 2;

                }
            }
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft)
        {
            Rect gizmoRect = new Rect(topLeft.x, topLeft.y, Width, Height);

            Rect designatorRect = new Rect(gizmoRect.x, gizmoRect.y, Height, Height);
            GizmoResult result = DrawDesignator(designatorRect);
            GUI.DrawTexture(designatorRect, texPutInArrow);

            Rect inventoryRect = new Rect(gizmoRect.x + designatorRect.width, gizmoRect.y, curWidth, Height);
            Widgets.DrawWindowBackground(inventoryRect);
            DrawSlots(inventoryRect);

            return result;
        }

        // same as base.GizmoOnGUI(), but in specified rect size
        public GizmoResult DrawDesignator(Rect rect)
        {
            bool mouseOver = false;
            if (Mouse.IsOver(rect))
            {
                mouseOver = true;
                GUI.color = GenUI.MouseoverColor;
            }
            GUI.DrawTexture(rect, BGTex);
            MouseoverSounds.DoRegion(rect, SoundDefOf.MouseoverCommand);

            // draw thing's texture
            Widgets.ThingIcon(rect, slotsComp.parent);

            bool gizmoActivated = false;
            KeyCode keyCode = hotKey?.MainKey ?? KeyCode.None;
            if (keyCode != KeyCode.None && !GizmoGridDrawer.drawnHotKeys.Contains(keyCode))
            {
                Rect rect2 = new Rect(rect.x + 5f, rect.y + 5f, 16f, 18f);
                Widgets.Label(rect2, keyCode.ToString());
                GizmoGridDrawer.drawnHotKeys.Add(keyCode);
                if (hotKey.KeyDownEvent)
                {
                    gizmoActivated = true;
                    Event.current.Use();
                }
            }
            if (Widgets.ButtonInvisible(rect))
            {
                gizmoActivated = true;
            }
            string labelCap = LabelCap;
            if (!labelCap.NullOrEmpty())
            {
                float num = Text.CalcHeight(labelCap, rect.width);
                num -= 2f;
                Rect rect3 = new Rect(rect.x, rect.yMax - num + 12f, rect.width, num);
                GUI.DrawTexture(rect3, TexUI.GrayTextBG);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(rect3, labelCap);
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
            GUI.color = Color.white;
            if (DoTooltip)
            {
                TipSignal tip = Desc;
                if (disabled && !disabledReason.NullOrEmpty())
                {
                    tip.text = tip.text + "\n\nDISABLED: " + disabledReason;
                }
                TooltipHandler.TipRegion(rect, tip);
            }
            if (!HighlightTag.NullOrEmpty() &&
                (Find.WindowStack.FloatMenu == null || !Find.WindowStack.FloatMenu.windowRect.Overlaps(rect)))
            {
                UIHighlighter.HighlightOpportunity(rect, HighlightTag);
            }
            if (gizmoActivated)
            {
                if (disabled)
                {
                    if (!disabledReason.NullOrEmpty())
                    {
                        Messages.Message(disabledReason, MessageSound.RejectInput);
                    }
                    return new GizmoResult(GizmoState.Mouseover, null);
                }
                return new GizmoResult(GizmoState.Interacted, Event.current);
            }
            if (mouseOver)
            {
                return new GizmoResult(GizmoState.Mouseover, null);
            }
            return new GizmoResult(GizmoState.Clear, null);
        }

        public void DrawSlots(Rect inventoryRect)
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
                        slotRect.x = inventoryRect.x + Height/2 * (currentSlotInd % numOfMaxItemsPerRow);
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
                                if (currentThing != null && currentThing.def.equipmentType == EquipmentType.Primary && (currentThing.def.IsRangedWeapon|| currentThing.def.IsMeleeWeapon))
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
                                        slotsComp.slots.TryDrop(currentThing, slotsComp.owner.Position,
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
                                    if (slotsComp.owner.workSettings != null && slotsComp.owner.workSettings.WorkIsActive(WorkTypeDefOf.Doctor))
                                        options.Add(new FloatMenuOption("TreatPatientWithMedicine".Translate(), () =>
                                        {
                                            Designator_ApplyMedicine designator = new Designator_ApplyMedicine();
                                            designator.medicine = currentThing;
                                            designator.doctor = slotsComp.owner;
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
                                        Job jobNew = new Job(JobDefOf.Ingest, currentThing);
                                        jobNew.maxNumToCarry = currentThing.def.ingestible.maxNumToIngestAtOnce;
                                        jobNew.ignoreForbidden = true;
                                        slotsComp.owner.drafter.TakeOrderedJob(jobNew);
                                    }));
                                }
                                // Drugs
                                if (currentThing.def.ingestible?.drugCategory >= DrugCategory.Any)
                                    options.Add(new FloatMenuOption("ConsumeThing".Translate(), () =>
                                    {
                                        Job jobNew = new Job(JobDefOf.Ingest, currentThing);
                                        jobNew.maxNumToCarry = currentThing.def.ingestible.maxNumToIngestAtOnce;
                                        jobNew.ignoreForbidden = true;
                                        slotsComp.owner.drafter.TakeOrderedJob(jobNew);
                                    }));
                               


                                Find.WindowStack.Add(new FloatMenu(options, currentThing.LabelCap));
                            }

                            // plays click sound on each click
                            SoundDefOf.Click.PlayOneShotOnCamera();
                        }
                    }
                    slotRect.x = inventoryRect.x + Height/2 * (currentSlotInd % numOfMaxItemsPerRow);
                    slotRect.y = inventoryRect.y + Height / 2 * (currentSlotInd / numOfMaxItemsPerRow);
                    //      slotRect.x += Height;
                }
            }
        }

        // determine conditions of hiding similar gizmos
        public override bool GroupsWith(Gizmo other) { return false; }

        // allows rectangular selection
        public override int DraggableDimensions => 2;

        // draws number of selected objects
        public override bool DragDrawMeasurements => true;

        // returning string text assigning false reason to AcceptanceReport
        public override AcceptanceReport CanDesignateCell(IntVec3 cell)
        {
            if (!cell.InBounds() || cell.Fogged() || cell.ContainsStaticFire())
            {
                return false;
            }

            Thing firstItem = cell.GetFirstItem();
            if (firstItem != null && CanDesignateThing(firstItem).Accepted)
            {
                return true;
            }

            return false;
        }

        public override AcceptanceReport CanDesignateThing(Thing thing)
        {
            // fast check if already added to "designations"
            if (slotsComp.designatedThings.Contains(thing))
            {
                return true;
            }

            // corpse has no icon\texture to render in the slot
            if (thing is Corpse)
            {
                return false;
            }

            if (!slotsComp.owner.CanReserveAndReach(thing, PathEndMode.ClosestTouch, slotsComp.owner.NormalMaxDanger()))
            {
                return false;
            }

            // if there are free slots
            if (slotsComp.designatedThings.Count + slotsComp.slots.Count >= slotsComp.Properties.maxSlots)
            {
                return false;
            }

            // if any of the thing's categories is allowed and not forbidden
            if (thing.def.thingCategories.Exists(category => slotsComp.Properties.allowedThingCategoryDefs.Exists(subCategory => subCategory.ThisAndChildCategoryDefs.Contains(category)) && !slotsComp.Properties.forbiddenSubThingCategoryDefs.Exists(subCategory => subCategory.ThisAndChildCategoryDefs.Contains(category))))
            {
                slotsComp.designatedThings.Add(thing);
                return true;
            }

            return false;
        }

        public override void DesignateSingleCell(IntVec3 cell)
        {
            DesignateThing(cell.GetFirstItem());
        }

        public override void DesignateThing(Thing thing)
        {
            // throws puffs to indicate that thigns were selected
            MoteMaker.ThrowMetaPuffs(thing);
        }

        // called when designator successfuly selects at least one thing
        protected override void FinalizeDesignationSucceeded()
        {
            // plays corresponding sound
            base.FinalizeDesignationSucceeded();

            Job job = new Job(DefDatabase<JobDef>.GetNamed("PutInSlot"))
            {
                targetQueueA = new List<TargetInfo>(),
                numToBringList = new List<int>(),
                targetB = slotsComp.parent
            };

            foreach (Thing thing in slotsComp.designatedThings)
            {
                job.targetQueueA.Add(thing);
                job.numToBringList.Add(thing.def.stackLimit);
            }
            slotsComp.designatedThings.Clear();

            if (!job.targetQueueA.NullOrEmpty())
                slotsComp.owner.drafter.TakeOrderedJob(job);

            // remove active selection after click
            DesignatorManager.Deselect();
        }

        // draws selection brackets over designated things
        public override void SelectedUpdate()
        {
            GenUI.RenderMouseoverBracket();
        }
    }
}