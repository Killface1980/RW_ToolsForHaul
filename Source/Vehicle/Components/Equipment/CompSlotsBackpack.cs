using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ToolsForHaul.Components
{
    public class CompSlotsBackpack : CompEquipmentGizmoProvider, IThingContainerOwner
    {
        public ThingContainer slots;
        public List<Thing> designatedThings = new List<Thing>();

        // gets access to the comp properties
        public CompSlotsBackpack_Properties Properties => (CompSlotsBackpack_Properties)this.props;

        public bool Spawned => this.parent.Spawned;

        // IThingContainerOwner requirement
        public Map GetMap()
        {
            return this.parent.MapHeld;
        }

        public ThingContainer GetInnerContainer()
        {
            return this.slots;
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
            int b = Mathf.RoundToInt(this.owner.GetStatValue(StatDefOf.CarryingCapacity) / td.VolumePerUnit);
            int num = Mathf.Min(td.stackLimit, b);
            if (CarriedThing != null)
            {
                num -= CarriedThing.stackCount;
            }

            return num;
        }

        // initialises ThingContainer owner and restricts the max slots range
        public override void PostSpawnSetup()
        {
            this.slots = new ThingContainer(this);
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
            if (this.owner.equipment.Primary != null)
            {
                ThingWithComps resultThing;

                // put weapon in slotter
                this.owner.equipment.TryTransferEquipmentToContainer(
                    this.owner.equipment.Primary,
                    this.slots,
                    out resultThing);
            }

            // equip new weapon
            this.owner.equipment.AddEquipment(thing);

            // remove that equipment from slotter
            this.slots.Remove(thing);

            // interrupt current jobs to prevent random errors
            if (this.owner?.jobs.curJob != null) this.owner.jobs.EndCurrentJob(JobCondition.InterruptForced);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            // NOTE: check if not "new object[]{ this });"
            Scribe_Deep.LookDeep(ref this.slots, "slots", this);
        }
    }

    public class CompSlotsBackpack_Properties : CompProperties
    {
        public List<ThingCategoryDef> allowedThingCategoryDefs = new List<ThingCategoryDef>();

        public List<ThingCategoryDef> forbiddenSubThingCategoryDefs = new List<ThingCategoryDef>();

        // public int maxSlots = 3;
        public CompSlotsBackpack_Properties()
        {
            this.compClass = typeof(CompSlotsBackpack);
        }
    }
}