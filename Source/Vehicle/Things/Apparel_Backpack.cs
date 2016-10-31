using System;
using System.Collections.Generic;
using System.Threading;
using RimWorld;
using ToolsForHaul.Components;
using ToolsForHaul.Designators;
using ToolsForHaul.Gizmos;
using ToolsForHaul.StatDefs;
using UnityEngine;
using Verse;
using Object = UnityEngine.Object;

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

        public int numOfSavedItems;
        public Pawn postWearer;

        public int MaxItem;
        public int MaxStack { get { return MaxItem * 20; } }

        public Apparel_Backpack()
        {
            postWearer = null;
            numOfSavedItems = 0;
        }


        public CompSlotsBackpack slotsComp
        {
            get
            {
                return GetComp<CompSlotsBackpack>();
            }
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            MaxItem = Mathf.RoundToInt(this.GetStatValue(HaulStatDefOf.BackpackMaxItem));
        }

        public override void ExposeData()
        {
			base.ExposeData();
            Scribe_Values.LookValue(ref numOfSavedItems, "numOfSavedItems", 0);
            Scribe_Values.LookValue(ref MaxItem, "MaxItem");
        }

        public override void Draw()
        {
            base.Draw();
        }



      //public override void Tick()
      //{
      //    base.Tick();
      //    //Put on backpack
      //    if (postWearer == null && wearer != null)
      //    {
      //        postWearer = wearer;
      //    }
      //
      //    //Put off backpack. Should drop all from postWearer
      //    else if (postWearer != null && wearer == null)
      //    {
      //
      //        slotsComp.slots.TryDropAll(postWearer.Position, ThingPlaceMode.Near);
      //        postWearer = null;
      //        numOfSavedItems = 0;
      //    }
      //    if (wearer != null && numOfSavedItems > slotsComp.slots.Count)
      //        numOfSavedItems = slotsComp.slots.Count;
      //}

        public override IEnumerable<Gizmo> GetWornGizmos()
        {
            // Designator_PutInInventory designator = new Designator_PutInInventory();
            //
            // designator.backpack = this;
            // designator.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconPutIn");
            // designator.defaultLabel = DesignatorPutInInventoryDefaultLabel + "(" + wearer.inventory.container.Count + "/" + MaxItem + ")";
            // designator.defaultDesc = DesignatorPutInInventoryDefaultDesc + wearer.inventory.container.Count + "/" + MaxItem;
            // designator.hotKey = KeyBindingDef.Named("CommandPutInInventory");
            // designator.activateSound = SoundDef.Named("Click");
            //
            // yield return designator;

            Designator_PutInBackpackSlot designator2 = new Designator_PutInBackpackSlot();
            designator2.SlotsBackpackComp = slotsComp;
            designator2.defaultLabel = string.Format("Put in ({0}/{1})", slotsComp.slots.Count, MaxItem);
            designator2.defaultDesc = string.Format("Put thing in {0}.", Label);
            designator2.hotKey = KeyBindingDef.Named("CommandPutInInventory");
            designator2.activateSound = SoundDef.Named("Click");
            // not used, but need to be defined, so that gizmo could accept actions
            designator2.icon = def.uiIcon;
            designator2.MaxItem = MaxItem;
            yield return designator2;


            Gizmo_BackpackEquipment gizmo = new Gizmo_BackpackEquipment();

            gizmo.backpack = this;
            yield return gizmo;
        }
    }
}