namespace ToolsForHaul.Components
{
    using System.Collections.Generic;

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

        private float parentPartHitPoints
        {
            get
            {
                return this.parent.SummaryHealthPercentImpact;
                return 100;

                // Hediff hediff = ((Vehicle_Cart)this.parent).health.hediffSet.hediffs.Find(
                // x => x.def == DefDatabase<HediffDef>.GetNamed("CarBomb"));
                // var bomb = DefDatabase<HediffDef>.GetNamed("CarBomb");
                // HediffSet hediffSet = ((Vehicle_Cart)this.parent).health.hediffSet;
                // Hediff firstHediffOfDef = hediffSet.hediffs<bomb>;
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
                Rand.Seed = base.Pawn.thingIDNumber.GetHashCode();
                bool result = Rand.Value < this.Props.chanceNeverExplodeFromDamage;
                Rand.PopState();
                return result;
            }
        }

        public IEnumerable<Gizmo> ActionExplode()
        {
           Command_Action commandExplode = new Command_Action
                                                {
                                                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Detonate"),
                                                    defaultDesc =
                                                        "CommandDetonateDesc".Translate() + parent.Part,
                                                    action = Command_Detonate
                                                };

            if (((Vehicle_Cart)this.Pawn).wickStarted)
            {
                commandExplode.Disable();
            }

            commandExplode.defaultLabel = "CommandDetonateLabel".Translate();
           yield return commandExplode;
        }

        public void Command_Detonate()
        {
            this.StartWick();
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look<Thing>(ref this.instigator, "instigator", false);
            Scribe_Values.Look<int>(ref this.wickTicksLeft, "wickTicksLeft", 0, false);
            Scribe_Values.Look<bool>(ref this.detonated, "detonated", false, false);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (((Vehicle_Cart)this.Pawn).wickStarted)
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
                    this.Detonate(base.Pawn.MapHeld);
                }
            }
        }

        private void StartWickSustainer()
        {
            SoundDefOf.MetalHitImportant.PlayOneShot(new TargetInfo(base.Pawn.Position, base.Pawn.Map, false));
            SoundInfo info = SoundInfo.InMap(base.Pawn, MaintenanceType.PerTick);
            this.wickSoundSustainer = SoundDefOf.HissSmall.TrySpawnSustainer(info);
        }


        public void PostPostApplyDamage(DamageInfo dinfo)
        {
            if (!this.CanEverExplodeFromDamage)
            {
                return;
            }

            if (!base.Pawn.Destroyed)
            {
                if (dinfo.Def.externalViolence && dinfo.Def != DamageDefOf.SurgicalCut && this.parentPartHitPoints < 0.01f)
                {
                    if (base.Pawn.MapHeld != null)
                    {
                        this.Detonate(base.Pawn.MapHeld);
                    }
                }
                else if (!((Vehicle_Cart)this.Pawn).wickStarted && this.Props.startWickOnDamageTaken != null && dinfo.Def == this.Props.startWickOnDamageTaken)
                {
                    this.StartWick(dinfo.Instigator);
                }
                else if (((Vehicle_Cart)this.Pawn).wickStarted && dinfo.Def == DamageDefOf.Stun)
                {
                    this.StopWick();
                }
                else if (!((Vehicle_Cart)this.Pawn).wickStarted && this.parentPartHitPoints <= this.StartWickThreshold && dinfo.Def.externalViolence)
                {
                    this.StartWick(dinfo.Instigator);
                }
            }
        }

        public override void CompPostMake()
        {
            base.CompPostMake();

            if (((Vehicle_Cart)this.Pawn).ExplosiveTickers.NullOrEmpty())
            {
                ((Vehicle_Cart)this.Pawn).ExplosiveTickers = new List<HediffCompExplosive_TFH>();
            }

            ((Vehicle_Cart)this.Pawn).ExplosiveTickers.Add(this);
        }

        public void StartWick(Thing instigator = null)
        {
            if (((Vehicle_Cart)this.Pawn).wickStarted)
            {
                return;
            }

            this.instigator = instigator;
            ((Vehicle_Cart)this.Pawn).wickStarted = true;
            this.wickTicksLeft = this.Props.wickTicks.RandomInRange;
            this.StartWickSustainer();
            GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(base.Pawn, this.Props.explosiveDamageType, null);
        }

        public override void CompTended(float quality, int batchPosition = 0)
        {
            base.CompTended(quality, batchPosition);
            ((Vehicle_Cart)this.Pawn).ExplosiveTickers.Remove(this);
        }

        public void StopWick()
        {
            ((Vehicle_Cart)this.Pawn).wickStarted = false;
            this.instigator = null;
        }

        protected void Detonate(Map map)
        {
            if (this.detonated)
            {
                return;
            }

            this.detonated = true;
            if (!base.Pawn.SpawnedOrAnyParentSpawned)
            {
                return;
            }

            if (!base.Pawn.Destroyed)
            {
                base.Pawn.Kill(null);
            }

            if (map == null)
            {
                Log.Warning("Tried to detonate CompExplosive_TFH in a null map.");
                return;
            }

            HediffCompProperties_Explosive_TFH props = this.Props;
            float num = props.explosiveRadius;
            if (base.Pawn.stackCount > 1 && props.explosiveExpandPerStackcount > 0f)
            {
                num += Mathf.Sqrt((float)(base.Pawn.stackCount - 1) * props.explosiveExpandPerStackcount);
            }

            if (props.explosionEffect != null)
            {
                Effecter effecter = props.explosionEffect.Spawn();
                effecter.Trigger(new TargetInfo(base.Pawn.PositionHeld, map, false), new TargetInfo(base.Pawn.PositionHeld, map, false));
                effecter.Cleanup();
            }

            ThingDef postExplosionSpawnThingDef = props.postExplosionSpawnThingDef;
            float postExplosionSpawnChance = props.postExplosionSpawnChance;
            int postExplosionSpawnThingCount = props.postExplosionSpawnThingCount;
            GenExplosion.DoExplosion(base.Pawn.PositionHeld, map, num, props.explosiveDamageType, this.instigator ?? base.Pawn, null, null, null, postExplosionSpawnThingDef, postExplosionSpawnChance, postExplosionSpawnThingCount, props.applyDamageToExplosionCellsNeighbors, props.preExplosionSpawnThingDef, props.preExplosionSpawnChance, props.preExplosionSpawnThingCount);
        }
    }
}
