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
        public int MaxStack { get { return maxItem * 50; } }

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

            Gizmo_BackpackEquipment gizmo = new Gizmo_BackpackEquipment();

            gizmo.backpack = this;
            yield return gizmo;
        }
    }
}