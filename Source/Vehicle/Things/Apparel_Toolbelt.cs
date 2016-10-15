using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ToolsForHaul
{
    public class Apparel_Toolbelt : Apparel
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
        private static readonly StatDef toolbeltMaxItem = DefDatabase<StatDef>.GetNamed("TFHMaxItem");

        public int maxItem; //obsoleted
        public Pawn postWearer;

        public int MaxItem { get { return (int)this.GetStatValue(toolbeltMaxItem); } }
        public int MaxStack { get { return maxItem * 20; } }


        public Apparel_Toolbelt()
        {
            postWearer = null;
    //        numOfSavedItems = 0;
        }

        public CompSlotsToolbelt slotsComp
        {
            get
            {
                return GetComp<CompSlotsToolbelt>();
            }
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            maxItem = (int)this.GetStatValue(toolbeltMaxItem);

        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue(ref maxItem, "maxItem");
            //     Scribe_Values.LookValue(ref numOfSavedItems, "numOfSavedItems", 0);
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

            //TODO make them drop the current container stuff, not the inventory
            //Put off backpack. Should drop all from postWearer
            else if (postWearer != null && wearer == null)
            {
                slotsComp.slots.TryDropAll(postWearer.Position, ThingPlaceMode.Near);
                postWearer = null;
            }

        }

        public override IEnumerable<Gizmo> GetWornGizmos()
        {
            Designator_PutInToolbeltSlot designator2 = new Designator_PutInToolbeltSlot();
            designator2.SlotsToolbeltComp = slotsComp;
            designator2.defaultLabel = string.Format("Put in ({0}/{1})", slotsComp.slots.Count, MaxItem);
            designator2.defaultDesc = string.Format("Put thing in {0}.", Label);
            designator2.hotKey = KeyBindingDef.Named("CommandPutInInventory");
            designator2.activateSound = SoundDef.Named("Click");
            // not used, but need to be defined, so that gizmo could accept actions
            designator2.icon = def.uiIcon;
            designator2.MaxItem = MaxItem;
            yield return designator2;

            Gizmo_ToolbeltEquipment gizmo = new Gizmo_ToolbeltEquipment();
           
            gizmo.toolbelt = this;
            
            yield return gizmo;

        }
    }
}