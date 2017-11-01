namespace TFH_Tools.Components
{
    using System.Collections.Generic;

    using RimWorld;

    using UnityEngine;

    using Verse;
    using Verse.AI;

    public class CompSlotsBackpack : CompEquipmentGizmoProvider, IThingHolder
    {
        public ThingOwner GetDirectlyHeldThings()
        {
            return this.slots;
        }

        public ThingOwner slots;

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
        }

        public List<Thing> designatedThings = new List<Thing>();

        // gets access to the comp properties
        public CompSlotsBackpack_Properties Properties => (CompSlotsBackpack_Properties)this.props;

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

        public float moveSpeedFactor
        {
            get
            {
                if (this.slots != null && this.slots.Count > 0)
                    return Mathf.Lerp(1f, 0.75f, this.slots.Count / (this.parent as Apparel_Backpack).MaxItem);
                return 1f;
            }
        }

        public float encumberPenalty
        {
            get
            {
                float penalty = 0f;
                if (this.slots != null && this.slots.Count > 0)
                {
                    penalty = this.slots.Count / (this.parent as Apparel_Backpack).MaxItem;
                }

                return penalty;
            }
        }

        // IThingContainerOwner requirement
        public IntVec3 GetPosition()
        {
            return this.parent.Position;
        }

        public int MaxStack => (this.parent as Apparel_Backpack).MaxItem * 50;

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

        // initialises ThingContainer owner and restricts the max slots range
        public CompSlotsBackpack()
        {
            this.slots = new ThingOwner<Thing>(this, true, LookMode.Deep);
        }

        // apply remaining damage and scatter things in slots, if holdingContainer is destroyed
        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            if (this.parent.HitPoints < 0)
            {
                foreach (Thing thing in this.slots)
                    thing.HitPoints -= (int)totalDamageDealt - this.parent.HitPoints;

                this.slots.TryDropAll(this.parent.Position, this.parent.Map, ThingPlaceMode.Near);
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
                    this.slots);
            }

            // equip new weapon
            this.Owner.equipment.AddEquipment(thing);

            // remove that equipment from slotter
            this.slots.Remove(thing);

            // interrupt current jobs to prevent random errors
            if (this.Owner?.jobs.curJob != null) this.Owner.jobs.EndCurrentJob(JobCondition.InterruptForced);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            // NOTE: check if not "new object[]{ this });"
            Scribe_Deep.Look(ref this.slots, "slots", this);
        }
    }
}