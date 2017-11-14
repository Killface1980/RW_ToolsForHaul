namespace TFH_Tools.Components
{
    using System.Collections.Generic;

    using RimWorld;

    using UnityEngine;

    using Verse;
    using Verse.AI;

    public class CompSlotsBackpack : ThingComp, IThingHolder
    {
        #region Public Fields

        public List<Thing> designatedThings = new List<Thing>();

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
        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            this.innerContainer.TryDropAll(base.parent.Position, map, ThingPlaceMode.Near);
        }
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
        }
        public ThingOwner<Thing> innerContainer;

        public ThingOwner GetDirectlyHeldThings()
        {
            return this.innerContainer;
        }

        public override void CompTick()
        {
            base.CompTick();
            this.innerContainer.ThingOwnerTick(true);
        }
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

        // gets access to the comp properties
        public CompSlotsBackpack_Properties Properties => (CompSlotsBackpack_Properties)this.props;

        public bool Spawned => this.parent.Spawned;

        #endregion Public Properties

        #region Public Methods

        public Pawn Owner => (parent as Apparel_ToolBelt).Wearer;

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



        public void InventoryTrackerTickRare()
        {
            this.innerContainer.ThingOwnerTickRare(true);
        }
        public void InventoryTrackerTick()
        {
            this.innerContainer.ThingOwnerTick(true);
           // if (this.unloadEverything && !this.HasAnyUnloadableThing)
           // {
           //     this.unloadEverything = false;
           // }
        }
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

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (this.innerContainer.Count > 0)
            {
                this.innerContainer.TryDropAll(this.parent.Position, this.parent.Map, ThingPlaceMode.Near);
            }
        }

        // swap selected equipment and primary equipment
        public void SwapEquipment(ThingWithComps thing)
        {
            // if Owner has equipped weapon
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
            if (this.Owner?.CurJob != null)
            {
                this.Owner.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }

        #endregion Public Methods
    }
}