namespace TFH_Tools
{
    using System.Collections.Generic;

    using RimWorld;

    using TFH_Tools.Components;

    using UnityEngine;

    using Verse;

    [StaticConstructorOnStartup]
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

        public int numOfSavedItems = 0;
        public Pawn postWearer = null;

       // public List<Thing> toDrop = new List<Thing>();

        public int MaxItem;
        public int MaxStack => this.MaxItem * 20;


        public override string GetInspectString()
        {
            string inspectString = base.GetInspectString();
            string text = inspectString;
            return text + "\n" + HaulStatDefOf.InventoryMaxItem.LabelCap + ": "
                   + HaulStatDefOf.InventoryMaxItem.ValueToString(
                       Mathf.RoundToInt(this.GetStatValue(HaulStatDefOf.InventoryMaxItem)),
                       ToStringNumberSense.Absolute);
        }

        public CompSlotsBackpack slotsComp => this.GetComp<CompSlotsBackpack>();

        public override void SpawnSetup(Map map, bool respawn)
        {
            base.SpawnSetup(map, respawn);
            this.MaxItem = Mathf.RoundToInt(this.GetStatValue(HaulStatDefOf.InventoryMaxItem));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.numOfSavedItems, "numOfSavedItems", 0);
            Scribe_Values.Look(ref this.MaxItem, "MaxItem");
          //  Scribe_Collections.Look(ref this.toDrop, "toDrop");
        }

        public override void Draw()
        {
            base.Draw();
        }



        // public override void Tick()
        // {
        // base.Tick();
        // //Put on backpack
        // if (postWearer == null && wearer != null)
        // {
        // postWearer = wearer;
        // }
        // //Put off backpack. Should drop all from postWearer
        // else if (postWearer != null && wearer == null)
        // {
        // slotsComp.innerContainer.TryDropAll(postWearer.Position, ThingPlaceMode.Near);
        // postWearer = null;
        // numOfSavedItems = 0;
        // }
        // if (wearer != null && numOfSavedItems > slotsComp.innerContainer.Count)
        // numOfSavedItems = slotsComp.innerContainer.Count;
        // }
        public override IEnumerable<Gizmo> GetWornGizmos()
        {
            // Designator_PutInInventory designator = new Designator_PutInInventory();
            // designator.backpack = this;
            // designator.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconPutIn");
            // designator.defaultLabel = DesignatorPutInInventoryDefaultLabel + "(" + wearer.inventory.innerContainer.Count + "/" + MaxItem + ")";
            // designator.defaultDesc = DesignatorPutInInventoryDefaultDesc + wearer.inventory.innerContainer.Count + "/" + MaxItem;
            // designator.hotKey = KeyBindingDef.Named("CommandPutInInventory");
            // designator.activateSound = SoundDef.Named("Click");
            // yield return designator;
            Designator_PutInBackpackSlot designator2 =
                new Designator_PutInBackpackSlot
                    {
                        SlotsBackpackComp = this.slotsComp,
                        pawn = this.Wearer,
                        defaultLabel =
                            string.Format(
                                "Put in ({0}/{1})",
                                this.slotsComp.innerContainer.Count,
                                this.MaxItem),
                        defaultDesc = string.Format("Put thing in {0}.", this.Label),
                        hotKey = KeyBindingDef.Named("CommandPutInInventory"),
                        activateSound = SoundDef.Named("Click"),
                        icon = this.def.uiIcon,
                        MaxItem = this.MaxItem
                    };

            // not used, but need to be defined, so that gizmo could accept actions
            yield return designator2;

            Gizmo_BackpackEquipment gizmo = new Gizmo_BackpackEquipment { backpack = this };

            yield return gizmo;
        }
    }
}