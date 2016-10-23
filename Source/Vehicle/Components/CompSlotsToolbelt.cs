using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ToolsForHaul.Components
{
    public class CompSlotsToolbelt : CompEquipmentGizmoProvider, IThingContainerOwner
    {
        public ThingContainer slots;
        public List<Thing> designatedThings = new List<Thing>();

        // gets access to the comp properties
        public CompSlotsToolbelt_Properties Properties => (CompSlotsToolbelt_Properties)props;

        public bool Spawned => parent.Spawned;

        // IThingContainerOwner requirement
        public ThingContainer GetContainer()
        {
            return slots;
        }


        public override void CompTick()
        {
            base.CompTick();

 
        }

#if CR            
                public float moveSpeedFactor
        {
            get
            {
                return Mathf.Lerp(1f, 0.75f, currentWeight / this.parentPawn.GetStatValue(StatDef.Named("CarryWeight")));
            }
        }

        public float encumberPenalty
        {
            get
            {
                float penalty = 0f;
                if (availableWeight < 0)
                {
                    penalty = currentWeight / this.parentPawn.GetStatValue(StatDef.Named("CarryWeight")) - 1;
                }
                return penalty;
            }
        }
#else
        public float moveSpeedFactor
        {
            get
            {
                return Mathf.Lerp(1f, 0.75f, slots.Count / (parent as Apparel_Toolbelt).MaxItem);
            }
        }

        public float encumberPenalty
        {
            get
            {
                float penalty = 0f;
                if (slots.Count != 0)
                {
                    penalty = slots.Count / (parent as Apparel_Toolbelt).MaxItem;
                }
                return penalty;
            }
        }
#endif


        // IThingContainerOwner requirement
        public IntVec3 GetPosition()
        {
            return parent.Position;
        }

        public int MaxStack { get { return (parent as Apparel_Toolbelt).MaxItem * 50; } }

        public int AvailableStackSpace(ThingDef td, Thing CarriedThing = null)
        {
            int b = Mathf.RoundToInt(owner.GetStatValue(StatDefOf.CarryingCapacity) / td.VolumePerUnit);
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
            slots = new ThingContainer(this);
        }

        // apply remaining damage and scatter things in slots, if holder is destroyed
        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            if (parent.HitPoints < 0)
            {
                foreach (Thing thing in slots)
                    thing.HitPoints -= (int)totalDamageDealt - parent.HitPoints;

                slots.TryDropAll(parent.Position, ThingPlaceMode.Near);
            }
        }

        // swap selected equipment and primary equipment
        public void SwapEquipment(ThingWithComps thing)
        {
            // if pawn has equipped weapon
            if (owner.equipment.Primary != null)
            {
                ThingWithComps resultThing;
                // put weapon in slotter
                owner.equipment.TryTransferEquipmentToContainer(owner.equipment.Primary, slots, out resultThing);
            }
            // equip new weapon
            owner.equipment.AddEquipment(thing);
            // remove that equipment from slotter
            slots.Remove(thing);

            // interrupt current jobs to prevent random errors
            if (owner?.jobs.curJob != null)
                owner.jobs.EndCurrentJob(JobCondition.InterruptForced);
        }


        public override void PostExposeData()
        {
            base.PostExposeData();

            // NOTE: check if not "new object[]{ this });"
            Scribe_Deep.LookDeep(ref slots, "slots", this);
        }
    }

    public class CompSlotsToolbelt_Properties : CompProperties
    {
        public List<ThingCategoryDef> allowedThingCategoryDefs = new List<ThingCategoryDef>();
        public List<ThingCategoryDef> forbiddenSubThingCategoryDefs = new List<ThingCategoryDef>();



        public CompSlotsToolbelt_Properties()
        {
            compClass = typeof(CompSlotsToolbelt);
        }
    }
}