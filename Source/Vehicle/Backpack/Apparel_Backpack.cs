using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ToolsForHaul
{
    public class Apparel_Backpack : Apparel
    {
        /*
        public Pawn wearer;

        public Apparel();

        public virtual bool AllowVerbCast(IntVec3 root, TargetInfo targ);
        public virtual bool CheckPreAbsorbDamage(DamageInfo dinfo);
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish);
        public virtual void DrawWornExtras();
        public virtual IEnumerable<Gizmo> GetWornGizmos();
        */

        private static string DesignatorPutInInventoryDefaultLabel = "DesignatorPutInDefaultLabel".Translate();
        private static string DesignatorPutInInventoryDefaultDesc = "DesignatorPutInDefaultDesc".Translate();
        private static readonly StatDef backpackMaxItem = DefDatabase<StatDef>.GetNamed("BackpackMaxItem");
 
        public int maxItem; //obsoleted
        public int numOfSavedItems;
        public Pawn postWearer;

        public int MaxItem { get { return (int)this.GetStatValue(backpackMaxItem); } }
        public int MaxStack { get { return maxItem * 20; } }
        
        public override void DrawWornExtras()
        {
            if (wearer == null || !wearer.Spawned)
            {
                return;
            }
            Vector3 drawPos = wearer.Drawer.DrawPos;
            Building_Bed buildingBed = wearer.CurrentBed();
            if (buildingBed != null)
            {
                if (!buildingBed.def.building.bed_showSleeperBody)
                {
                    return;
                }
                drawPos.y = Altitudes.AltitudeFor(buildingBed.def.altitudeLayer);
            }
            else
            {
                drawPos.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn);
            }
            Vector3 s = new Vector3(1.5f, 1.5f, 1.5f);

            // Get the graphic path
            string path = def.graphicData.texPath + "_" + wearer?.story.BodyType;
            Graphic graphic = GraphicDatabase.Get<Graphic_Multi>(path, ShaderDatabase.Cutout, def.graphicData.drawSize, DrawColor);
            ApparelGraphicRecord apparelGraphic = new ApparelGraphicRecord(graphic, this);


            Rot4 rot = wearer.Rotation;
            float angle = 0f;
            if (wearer.GetPosture() > 0)
            {
                rot = LayingFacing();
                if (buildingBed != null)
                {
                    Rot4 rotation = buildingBed.Rotation;
                    rotation.AsInt = (rotation.AsInt + 2);
                    angle = rotation.AsAngle;
                }
                else if (wearer.Downed || wearer.Dead)
                {
                    Pawn_DrawTracker drawer = wearer.Drawer;
                    float? num;
                    if (drawer == null)
                    {
                        num = null;
                    }
                    else
                    {
                        PawnRenderer renderer = drawer.renderer;
                        if (renderer == null)
                        {
                            num = null;
                        }
                        else
                        {
                            PawnDownedWiggler wiggler = renderer.wiggler;
                            num = ((wiggler != null) ? new float?(wiggler.downedAngle) : null);
                        }
                    }
                    float? num2 = num;
                    if (num2.HasValue)
                    {
                        angle = num2.Value;
                    }
                }
                else
                {
                    angle = rot.FacingCell.AngleFlat;
                }
            }
            drawPos.y += GetAltitudeOffset(rot);
            Material material = apparelGraphic.graphic.MatAt(rot, null);
            material.shader = ShaderDatabase.Cutout;
            material.color = DrawColor;

            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(drawPos, Quaternion.AngleAxis(angle, Vector3.up), s);
            Graphics.DrawMesh((rot == Rot4.West) ? MeshPool.plane10Flip : MeshPool.plane10, matrix, material, 0);
        }

        // Copied from PawnRenderer
        private Rot4 LayingFacing()
        {
            if (wearer == null)
            {
                return Rot4.Random;
            }
            if (wearer.GetPosture() == PawnPosture.LayingFaceUp)
            {
                return Rot4.South;
            }
            if (wearer.RaceProps.Humanlike)
            {
                switch (wearer.thingIDNumber % 4)
                {
                    case 0:
                        return Rot4.South;
                    case 1:
                        return Rot4.South;
                    case 2:
                        return Rot4.East;
                    case 3:
                        return Rot4.West;
                }
            }
            else
            {
                switch (wearer.thingIDNumber % 4)
                {
                    case 0:
                        return Rot4.South;
                    case 1:
                        return Rot4.East;
                    case 2:
                        return Rot4.West;
                    case 3:
                        return Rot4.West;
                }
            }
            return Rot4.Random;
        }


        private float GetAltitudeOffset(Rot4 rotation)
        {
            if (rotation == Rot4.North)
            {
                return 0.1f;
            }
            float offset = 0.0375f;
            bool hasOnSkin = false;
            bool hasMiddle = false;
            bool hasShell = false;
            for (int i = 0; i < wearer.apparel.WornApparel.Count && !(hasOnSkin && hasMiddle && hasShell); i++)
            {
                switch (wearer.apparel.WornApparel[i].def.apparel.LastLayer)
                {
                    case ApparelLayer.OnSkin:
                        if (!hasOnSkin)
                        {
                            offset += 0.005f;
                            hasOnSkin = true;
                        }
                        break;
                    case ApparelLayer.Middle:
                        if (!hasMiddle)
                        {
                            offset += 0.005f;
                            hasMiddle = true;
                        }
                        break;
                    case ApparelLayer.Shell:
                        if (!hasShell)
                        {
                            offset += 0.005f;
                            hasShell = true;
                        }
                        break;
                }
            }
            return offset;
        }

        public Apparel_Backpack()
        {
            postWearer = null;
            numOfSavedItems = 0;
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            maxItem = (int)this.GetStatValue(backpackMaxItem);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue(ref maxItem, "maxItem", (int)this.GetStatValue(backpackMaxItem));
            Scribe_Values.LookValue(ref numOfSavedItems, "numOfSavedItems", 0);
        }

        public override void Draw()
        {
            base.Draw();
        }

        public override void Tick()
        {
            base.Tick();
            //Put on backpack
            if (postWearer == null && wearer != null)
            {
                postWearer = wearer;
            }

            //Put off backpack. Should drop all from postWearer
            else if (postWearer != null && wearer == null)
            {
                postWearer.inventory?.container?.TryDropAll(postWearer.Position, ThingPlaceMode.Near);
                postWearer = null;
                numOfSavedItems = 0;
            }
            if (wearer != null && numOfSavedItems > wearer.inventory.container.Count)
                numOfSavedItems = wearer.inventory.container.Count;
        }

        public override IEnumerable<Gizmo> GetWornGizmos()
        {
            Designator_PutInInventory designator = new Designator_PutInInventory();

            designator.backpack = this;
            designator.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconPutIn");
            designator.defaultLabel = DesignatorPutInInventoryDefaultLabel + "(" + wearer.inventory.container.Count + "/" + MaxItem + ")";
            designator.defaultDesc = DesignatorPutInInventoryDefaultDesc + wearer.inventory.container.Count + "/" + MaxItem;
            designator.hotKey = KeyBindingDef.Named("CommandPutInInventory");
            designator.activateSound = SoundDef.Named("Click");

            yield return designator;

            Designator_PutInSlot designator2 = new Designator_PutInSlot();
            designator2.slotsComp = GetComp<CompSlots>();
            designator2.defaultLabel = string.Format("Put in ({0}/{1})", GetComp<CompSlots>().slots.Count, MaxItem);
            designator2.defaultDesc = string.Format("Put thing in {0}.", Label);
            designator2.hotKey = KeyBindingDef.Named("CommandPutInInventory");
            designator.activateSound = SoundDef.Named("Click");
            // not used, but need to be defined, so that gizmo could accept actions
            designator2.icon = def.uiIcon;
            yield return designator2;


            Gizmo_BackpackEquipment gizmo = new Gizmo_BackpackEquipment();

            gizmo.backpack = this;
            yield return gizmo;
        }
    }
}