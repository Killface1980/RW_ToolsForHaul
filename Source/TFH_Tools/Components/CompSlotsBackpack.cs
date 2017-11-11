namespace TFH_Tools.Components
{
    using System.Collections.Generic;

    using RimWorld;

    using UnityEngine;

    using Verse;
    using Verse.AI;

    public class CompSlotsBackpack : CompEquipmentGizmoProvider, IThingHolder
    {
        #region Public Fields

        public List<Thing> designatedThings = new List<Thing>();

        public ThingOwner<Thing> innerContainer;

        #endregion Public Fields

        #region Public Constructors

        // initialises ThingContainer owner and restricts the max innerContainer range
        public CompSlotsBackpack()
        {
            this.innerContainer = new ThingOwner<Thing>(this);

            // if (Properties.MaxSlots > 6)
            //     Properties.MaxSlots = 6;
            //
            // if (Properties.maxSlots < 1)
            //     Properties.maxSlots = 1;
        }

        #endregion Public Constructors

        #region Public Properties

        public float encumberPenalty
        {
            get
            {
                float penalty = 0f;
                if (this.innerContainer != null && this.innerContainer.Count > 0)
                {
                    penalty = this.innerContainer.Count / (this.parent as Apparel_Backpack).MaxItem;
                }

                return penalty;
            }
        }

        public int MaxStack => (this.parent as Apparel_Backpack).MaxItem * 50;

        public float moveSpeedFactor
        {
            get
            {
                if (this.innerContainer != null && this.innerContainer.Count > 0)
                    return Mathf.Lerp(1f, 0.75f, this.innerContainer.Count / (this.parent as Apparel_Backpack).MaxItem);
                return 1f;
            }
        }

        public Apparel_Backpack backpack;
        public IThingHolder ParentHolder
        {
            get
            {
                return this.backpack.Wearer;
            }
        }

        // gets access to the comp properties
        public CompSlotsBackpack_Properties Properties => (CompSlotsBackpack_Properties)this.props;

        public bool Spawned => this.parent.Spawned;

        #endregion Public Properties

        #region Public Methods

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

        public override void CompTick()
        {
            base.CompTick();
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return this.innerContainer;
        }
        // IThingContainerOwner requirement
        public IntVec3 GetPosition()
        {
            return this.parent.Position;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();

            // NOTE: check if not "new object[]{ this });"
            Scribe_Deep.Look(ref this.innerContainer, "innerContainer", this);
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
            if (this.Owner?.CurJob != null) this.Owner.jobs.EndCurrentJob(JobCondition.InterruptForced);
        }

        #endregion Public Methods
    }
}