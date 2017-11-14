namespace TFH_Tools.Components
{
    using System.Collections.Generic;

    using RimWorld;

    using UnityEngine;

    using Verse;
    using Verse.AI;

    public class CompSlotsToolbelt : ThingComp, IThingHolder
    {
        public ThingOwner GetDirectlyHeldThings()
        {
            return this.innerContainer;
        }

        public Pawn Owner => (parent as Apparel_ToolBelt).Wearer;

        public ThingOwner<Thing> innerContainer;

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
        }

        public List<Thing> designatedThings = new List<Thing>();

        // gets access to the comp properties
        public CompSlotsToolbelt_Properties Properties => (CompSlotsToolbelt_Properties)this.props;

        public bool Spawned => this.parent.Spawned;

        // IThingContainerOwner requirement
        public Map GetMap()
        {
            return this.parent.MapHeld;
        }

        public override void CompTick()
        {
            base.CompTick();
        }

        public float moveSpeedFactor => Mathf.Lerp(1f, 0.75f, this.innerContainer.Count / (this.parent as Apparel_ToolBelt).MaxItem);

        public float encumberPenalty
        {
            get
            {
                float penalty = 0f;
                if (this.innerContainer.Count != 0)
                {
                    penalty = this.innerContainer.Count / (this.parent as Apparel_ToolBelt).MaxItem;
                }

                return penalty;
            }
        }

        // IThingContainerOwner requirement
        public IntVec3 GetPosition()
        {
            return this.parent.Position;
        }

        public int MaxStack => (this.parent as Apparel_ToolBelt).MaxItem * 50;

        public int AvailableStackSpace(ThingDef td, Thing CarriedThing = null)
        {
            int b = Mathf.RoundToInt(this.Owner.GetStatValue(StatDefOf.CarryingCapacity) / td.VolumePerUnit);
            int num = Mathf.Min(td.stackLimit, b);
            if (CarriedThing != null)
            {
                num -= CarriedThing.stackCount;
            }

            return num;
        }

        // initialises ThingContainer owner and restricts the max innerContainer range
        public CompSlotsToolbelt()
        {
            this.innerContainer = new ThingOwner<Thing>(this);
        }

        // apply remaining damage and scatter things in innerContainer, if holdingContainer is destroyed
        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            if (this.parent.HitPoints < 0)
            {
                foreach (Thing thing in this.innerContainer)
                    thing.HitPoints -= (int)totalDamageDealt - this.parent.HitPoints;

                this.innerContainer.TryDropAll(this.parent.Position, this.parent.Map, ThingPlaceMode.Near);
            }
        }

        // swap selected equipment and primary equipment
        public void SwapEquipment(ThingWithComps thing)
        {
            // if pawn has equipped weapon
            if (this.Owner.equipment.Primary != null)
            {
                ThingWithComps resultThing;

                // put weapon in slotter
                this.Owner.equipment.TryTransferEquipmentToContainer(
                    this.Owner.equipment.Primary,
                    this.innerContainer);
            }

            // equip new weapon
            this.Owner.equipment.AddEquipment(thing);

            // remove that equipment from slotter
            this.innerContainer.Remove(thing);

            // interrupt current jobs to prevent random errors
            if (this.Owner?.jobs.curJob != null)
            {
                this.Owner.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            // NOTE: check if not "new object[]{ this });"
            Scribe_Deep.Look(ref this.innerContainer, "innerContainer", this);
        }
    }
}