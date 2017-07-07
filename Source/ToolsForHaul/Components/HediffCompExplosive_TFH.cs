namespace ToolsForHaul.Components
{
    using RimWorld;

    using ToolsForHaul.Vehicles;

    using UnityEngine;

    using Verse;
    using Verse.Sound;

    public class HediffCompExplosive_TFH : HediffComp
    {

        protected int wickTicksLeft;

        private Thing instigator;

        public bool detonated;

        protected Sustainer wickSoundSustainer;

        private Vehicle_Cart cart;

        private float parentPartHitPoints
        {
            get
            {
                return this.parent.SummaryHealthPercentImpact;
                return 100;
                // Hediff hediff = ((Vehicle_Cart)this.parent).health.hediffSet.hediffs.Find(
                //     x => x.def == DefDatabase<HediffDef>.GetNamed("CarBomb"));
                //
                // var bomb = DefDatabase<HediffDef>.GetNamed("CarBomb");
                //
                // HediffSet hediffSet = ((Vehicle_Cart)this.parent).health.hediffSet;
                // Hediff firstHediffOfDef = hediffSet.hediffs<bomb>;
                //
                // hediff.SummaryHealthPercentImpact


            }
        }

        public HediffCompProperties_Explosive_TFH Props
        {
            get
            {
                return (HediffCompProperties_Explosive_TFH)this.props;
            }
        }

        protected int StartWickThreshold
        {
            get
            {
                return Mathf.RoundToInt(this.Props.startWickHitPointsPercent);
            }
        }

        private bool CanEverExplodeFromDamage
        {
            get
            {
                if (this.Props.chanceNeverExplodeFromDamage < 1E-05f)
                {
                    return true;
                }
                Rand.PushState();
                Rand.Seed = this.Pawn.thingIDNumber.GetHashCode();
                bool result = Rand.Value < this.Props.chanceNeverExplodeFromDamage;
                Rand.PopState();
                return result;
            }
        }
        public void Command_Detonate()
        {
            StartWick();
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look<Thing>(ref this.instigator, "instigator", false);
            Scribe_Values.Look<Vehicle_Cart>(ref this.cart, "cart");
            Scribe_Values.Look<int>(ref this.wickTicksLeft, "wickTicksLeft", 0, false);
            Scribe_Values.Look<bool>(ref this.detonated, "detonated", false, false);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (cart.wickStarted)
            {
                if (this.wickSoundSustainer == null)
                {
                    this.StartWickSustainer();
                }
                else
                {
                    this.wickSoundSustainer.Maintain();
                }
                this.wickTicksLeft--;
                if (this.wickTicksLeft <= 0)
                {
                    this.Detonate(this.Pawn.MapHeld);
                }
            }
        }

        private void StartWickSustainer()
        {
            SoundDefOf.MetalHitImportant.PlayOneShot(new TargetInfo(this.Pawn.Position, this.Pawn.Map, false));
            SoundInfo info = SoundInfo.InMap(this.Pawn, MaintenanceType.PerTick);
            this.wickSoundSustainer = SoundDefOf.HissSmall.TrySpawnSustainer(info);
        }


        public void PostPostApplyDamage(DamageInfo dinfo)
        {
            if (!this.CanEverExplodeFromDamage)
            {
                return;
            }

            if (!this.Pawn.Destroyed)
            {
                if (dinfo.Def.externalViolence && this.parentPartHitPoints < 0.01f)
                {
                    if (this.Pawn.MapHeld != null)
                    {
                        this.Detonate(this.Pawn.MapHeld);
                    }
                }
                else if (!this.cart.wickStarted && this.Props.startWickOnDamageTaken != null && dinfo.Def == this.Props.startWickOnDamageTaken)
                {
                    this.StartWick(dinfo.Instigator);
                }
                else if (this.cart.wickStarted && dinfo.Def == DamageDefOf.Stun)
                {
                    this.StopWick();
                }
                else if (!this.cart.wickStarted && this.parentPartHitPoints <= this.StartWickThreshold && dinfo.Def.externalViolence)
                {
                    this.StartWick(dinfo.Instigator);
                }
            }
        }

        public override void CompPostMake()
        {
            base.CompPostMake();

            this.cart = this.parent.pawn as Vehicle_Cart;

            this.cart?.ExplosiveTickers.Add(this);
        }

        public void StartWick(Thing instigator = null)
        {
            if (this.cart.wickStarted)
            {
                return;
            }
            this.instigator = instigator;
            this.cart.wickStarted = true;
            this.wickTicksLeft = this.Props.wickTicks.RandomInRange;
            this.StartWickSustainer();
            GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this.Pawn, this.Props.explosiveDamageType, null);
        }

        public override void CompTended(float quality, int batchPosition = 0)
        {
            base.CompTended(quality, batchPosition);
            this.cart.ExplosiveTickers.Remove(this);
        }

        public void StopWick()
        {
            this.cart.wickStarted = false;
            this.instigator = null;
        }

        protected void Detonate(Map map)
        {
            if (this.detonated)
            {
                return;
            }
            this.detonated = true;
            if (!this.Pawn.SpawnedOrAnyParentSpawned)
            {
                return;
            }
            if (!this.Pawn.Destroyed)
            {
                this.Pawn.Kill(null);
            }
            if (map == null)
            {
                Log.Warning("Tried to detonate CompExplosive_TFH in a null map.");
                return;
            }
            HediffCompProperties_Explosive_TFH props = this.Props;
            float num = props.explosiveRadius;
            if (this.Pawn.stackCount > 1 && props.explosiveExpandPerStackcount > 0f)
            {
                num += Mathf.Sqrt((float)(this.Pawn.stackCount - 1) * props.explosiveExpandPerStackcount);
            }
            if (props.explosionEffect != null)
            {
                Effecter effecter = props.explosionEffect.Spawn();
                effecter.Trigger(new TargetInfo(this.Pawn.PositionHeld, map, false), new TargetInfo(this.Pawn.PositionHeld, map, false));
                effecter.Cleanup();
            }
            ThingDef postExplosionSpawnThingDef = props.postExplosionSpawnThingDef;
            float postExplosionSpawnChance = props.postExplosionSpawnChance;
            int postExplosionSpawnThingCount = props.postExplosionSpawnThingCount;
            GenExplosion.DoExplosion(this.Pawn.PositionHeld, map, num, props.explosiveDamageType, this.instigator ?? this.Pawn, null, null, null, postExplosionSpawnThingDef, postExplosionSpawnChance, postExplosionSpawnThingCount, props.applyDamageToExplosionCellsNeighbors, props.preExplosionSpawnThingDef, props.preExplosionSpawnChance, props.preExplosionSpawnThingCount);
        }
    }
}
