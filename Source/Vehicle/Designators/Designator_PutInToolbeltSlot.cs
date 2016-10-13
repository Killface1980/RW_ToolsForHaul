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
    public class Designator_PutInToolbeltSlot : Designator
    {
        public static readonly Texture2D texPutInArrow = ContentFinder<Texture2D>.Get("UI/Gizmoes/PutIn");
        private static readonly Texture2D texOccupiedSlotBG = SolidColorMaterials.NewSolidColorTexture(1f, 1f, 1f, 0.1f);

        public CompSlotsToolbelt SlotsToolbeltComp;
        private static readonly Texture2D MeleeWeaponTex = SolidColorMaterials.NewSolidColorTexture(0.6f, 0.15f, 0.15f, 0.75f);
        private static readonly Texture2D RangedWeaponTex = SolidColorMaterials.NewSolidColorTexture(0.09f, 0.25f, 0.6f, 0.75f);

        public int MaxItem;

        // overall gizmo width
        // Height = 75f
        //      public override float Width => slotsComp.Properties.maxSlots * Height + Height;

        public Designator_PutInToolbeltSlot()
        {
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.DesignateHaul;
            soundDragSustain = SoundDefOf.DesignateDragStandard;
            soundDragChanged = SoundDefOf.DesignateDragStandardChanged;
            activateSound = SoundDef.Named("Click");
        }

        //EquipmentSlot Properties
        private const int numOfRow = 2;

        private float curWidth
        {
            get
            {
                return Height;
            }
        }

        public override float Width
        {
            get
            {

                return Height;
            }
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft)
        {
            Rect gizmoRect = new Rect(topLeft.x, topLeft.y, Width, Height);

            Rect designatorRect = new Rect(gizmoRect.x, gizmoRect.y, Height, Height);
            GizmoResult result = DrawDesignator(designatorRect);
            GUI.DrawTexture(designatorRect, texPutInArrow);

            //Rect inventoryRect = new Rect(gizmoRect.x + designatorRect.width, gizmoRect.y, curWidth, Height);
            //Widgets.DrawWindowBackground(inventoryRect);
            //     DrawSlots(inventoryRect);

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
            Widgets.ThingIcon(rect, SlotsToolbeltComp.parent);

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


        // determine conditions of hiding similar gizmos
        public override bool GroupsWith(Gizmo other) { return false; }

        // allows rectangular selection
        public override int DraggableDimensions => 2;

        // draws number of selected objects
        public override bool DragDrawMeasurements => true;

        private int numOfContents;

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

            numOfContents = SlotsToolbeltComp.slots.Count;

            int designationsTotalStackCount = 0;
            foreach (Thing designation in SlotsToolbeltComp.designatedThings)
                designationsTotalStackCount += designation.stackCount;

            //No Item space or no stack space
            if (SlotsToolbeltComp.designatedThings.Count + numOfContents >= MaxItem
                || designationsTotalStackCount + SlotsToolbeltComp.slots.TotalStackCount >= SlotsToolbeltComp.MaxStack)
                return new AcceptanceReport("BackpackIsFull".Translate());


            foreach (Thing designation in SlotsToolbeltComp.designatedThings)
            {
                if (designation.def.category == ThingCategory.Item && !Find.Reservations.IsReserved(designation, Faction.OfPlayer))
                    return true;
            }
            return new AcceptanceReport("InvalidPutInTarget".Translate());

            //Thing firstItem = cell.GetFirstItem();
            //if (firstItem != null && CanDesignateThing(firstItem).Accepted)
            //{
            //    return true;
            //}
            //
            //return false;
        }

        public override AcceptanceReport CanDesignateThing(Thing thing)
        {
            // fast check if already added to "designations"
            if (SlotsToolbeltComp.designatedThings.Contains(thing))
            {
                return true;
            }

            // corpse has no icon\texture to render in the slot
            if (thing is Corpse)
            {
                return false;
            }

            if (!SlotsToolbeltComp.owner.CanReserveAndReach(thing, PathEndMode.ClosestTouch, SlotsToolbeltComp.owner.NormalMaxDanger()))
            {
                return false;
            }

            // if there are free slots
            if (SlotsToolbeltComp.designatedThings.Count + SlotsToolbeltComp.slots.Count >= MaxItem)
            {
                return false;
            }

            // if any of the thing's categories is allowed and not forbidden
            if (thing.def.thingCategories.Exists(category => SlotsToolbeltComp.Properties.allowedThingCategoryDefs.Exists(subCategory => subCategory.ThisAndChildCategoryDefs.Contains(category)) && !SlotsToolbeltComp.Properties.forbiddenSubThingCategoryDefs.Exists(subCategory => subCategory.ThisAndChildCategoryDefs.Contains(category))))
            {
                SlotsToolbeltComp.designatedThings.Add(thing);
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

            Job job = new Job(DefDatabase<JobDef>.GetNamed("PutInToolbeltSlot"))
            {
                targetQueueA = new List<TargetInfo>(),
                numToBringList = new List<int>(),
                targetB = SlotsToolbeltComp.parent
            };

            foreach (Thing thing in SlotsToolbeltComp.designatedThings)
            {
                if (thing.IsForbidden(SlotsToolbeltComp.owner))
                {
                    thing.SetForbidden(false);
                }

                job.targetQueueA.Add(thing);
                job.numToBringList.Add(thing.def.stackLimit);
            }
            SlotsToolbeltComp.designatedThings.Clear();

            if (!job.targetQueueA.NullOrEmpty())
                SlotsToolbeltComp.owner.drafter.TakeOrderedJob(job);

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