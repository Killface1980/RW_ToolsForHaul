namespace TFH_Tools
{
    using System.Collections.Generic;

    using RimWorld;

    using TFH_Tools.Components;

    using UnityEngine;

    using Verse;

    public class Apparel_ToolBelt : Apparel
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
        public Pawn postWearer;

        public int MaxItem;

        public Apparel_ToolBelt()
        {
            this.postWearer = null;

            // numOfSavedItems = 0;
        }

        public CompSlotsToolbelt slotsComp => this.GetComp<CompSlotsToolbelt>();

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.MaxItem = Mathf.RoundToInt(this.GetStatValue(HaulStatDefOf.InventoryMaxItem));
        }

        public override void ExposeData()
        {
            base.ExposeData();

            // Scribe_Values.LookValue(ref MaxItem, "maxItem");
            Scribe_Values.Look(ref this.MaxItem, "MaxItem");
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
        // }
        // }
        public override IEnumerable<Gizmo> GetWornGizmos()
        {
            if (!this.Wearer.story?.WorkTagIsDisabled(WorkTags.Violent) == true)
            {
                Designator_PutInToolbeltSlot designator2 = new Designator_PutInToolbeltSlot();
                designator2.SlotsToolbeltComp = this.slotsComp;
                designator2.defaultLabel = string.Format("Put in ({0}/{1})", this.slotsComp.innerContainer.Count, this.MaxItem);
                designator2.defaultDesc = string.Format("Put thing in {0}.", this.Label);
                designator2.hotKey = KeyBindingDef.Named("CommandPutInInventory");
                designator2.activateSound = SoundDef.Named("Click");

                // not used, but need to be defined, so that gizmo could accept actions
                designator2.icon = this.def.uiIcon;
                designator2.MaxItem = this.MaxItem;
                yield return designator2;

                Gizmo_ToolbeltEquipment gizmo = new Gizmo_ToolbeltEquipment();

                gizmo.toolbelt = this;

                yield return gizmo;
            }
        }
    }
}