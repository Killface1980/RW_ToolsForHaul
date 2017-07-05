namespace ToolsForHaul
{
    using RimWorld;

    using UnityEngine;

    using Verse;
    using Verse.Sound;

    public class HediffCompExplosive : HediffComp
    {
        public bool wickStarted;

        protected int wickTicksLeft;

        private Thing instigator;

        public bool detonated;

        protected Sustainer wickSoundSustainer;

        public HediffCompProperties_Explosive Props
        {
            get
            {
                return (HediffCompProperties_Explosive)this.props;
            }
        }

        protected int StartWickThreshold
        {
            get
            {
                return Mathf.RoundToInt(this.Props.startWickHitPointsPercent * (float)this.parent.pawn.MaxHitPoints);
            }
        }

     // private bool CanEverExplodeFromDamage
     // {
     //     get
     //     {
     //         if (this.Props.chanceNeverExplodeFromDamage < 1E-05f)
     //         {
     //             return true;
     //         }
     //         Rand.PushState();
     //         Rand.Seed = this.parent.thingIDNumber.GetHashCode();
     //         bool result = Rand.Value < this.Props.chanceNeverExplodeFromDamage;
     //         Rand.PopState();
     //         return result;
     //     }
     // }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look<Thing>(ref this.instigator, "instigator", false);
            Scribe_Values.Look<bool>(ref this.wickStarted, "wickStarted", false, false);
            Scribe_Values.Look<int>(ref this.wickTicksLeft, "wickTicksLeft", 0, false);
            Scribe_Values.Look<bool>(ref this.detonated, "detonated", false, false);
        }


        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.wickStarted)
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
                    this.Detonate(this.parent.pawn.MapHeld);
                }
            }
        }

        private void StartWickSustainer()
        {
            SoundDefOf.MetalHitImportant.PlayOneShot(new TargetInfo(this.parent.pawn.Position, this.parent.pawn.Map, false));
            SoundInfo info = SoundInfo.InMap(this.parent.pawn, MaintenanceType.PerTick);
            this.wickSoundSustainer = SoundDefOf.HissSmall.TrySpawnSustainer(info);
        }

    //  public override void PostDraw()
    //  {
    //      if (this.wickStarted)
    //      {
    //          this.parent.Map.overlayDrawer.DrawOverlay(this.parent, OverlayTypes.BurningWick);
    //      }
    //  }

    //  public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
    //  {
    //      absorbed = false;
    //      if (this.CanEverExplodeFromDamage)
    //      {
    //          if (dinfo.Def.externalViolence && dinfo.Amount >= this.parent.HitPoints)
    //          {
    //              if (this.parent.MapHeld != null)
    //              {
    //                  this.Detonate(this.parent.MapHeld);
    //                  absorbed = true;
    //              }
    //          }
    //          else if (!this.wickStarted && this.Props.startWickOnDamageTaken != null && dinfo.Def == this.Props.startWickOnDamageTaken)
    //          {
    //              this.StartWick(dinfo.Instigator);
    //          }
    //      }
    //  }
    //
    //  public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
    //  {
    //      if (!this.CanEverExplodeFromDamage)
    //      {
    //          return;
    //      }
    //      if (!this.parent.Destroyed)
    //      {
    //          if (this.wickStarted && dinfo.Def == DamageDefOf.Stun)
    //          {
    //              this.StopWick();
    //          }
    //          else if (!this.wickStarted && this.parent.HitPoints <= this.StartWickThreshold && dinfo.Def.externalViolence)
    //          {
    //              this.StartWick(dinfo.Instigator);
    //          }
    //      }
    //  }

        public void StartWick(Thing instigator = null)
        {
            if (this.wickStarted)
            {
                return;
            }
            this.instigator = instigator;
            this.wickStarted = true;
            this.wickTicksLeft = this.Props.wickTicks.RandomInRange;
            this.StartWickSustainer();
            GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this.parent.pawn, this.Props.explosiveDamageType, null);
        }

        public void StopWick()
        {
            this.wickStarted = false;
            this.instigator = null;
        }

        protected void Detonate(Map map)
        {
            if (this.detonated)
            {
                return;
            }
            this.detonated = true;
            if (!this.parent.pawn.SpawnedOrAnyParentSpawned)
            {
                return;
            }
            if (!this.parent.pawn.Destroyed)
            {
                this.parent.pawn.Kill(null);
            }
            if (map == null)
            {
                Log.Warning("Tried to detonate CompExplosive_TFH in a null map.");
                return;
            }
            HediffCompProperties_Explosive props = this.Props;
            float num = props.explosiveRadius;
            if (this.parent.pawn.stackCount > 1 && props.explosiveExpandPerStackcount > 0f)
            {
                num += Mathf.Sqrt((float)(this.parent.pawn.stackCount - 1) * props.explosiveExpandPerStackcount);
            }
            if (props.explosionEffect != null)
            {
                Effecter effecter = props.explosionEffect.Spawn();
                effecter.Trigger(new TargetInfo(this.parent.pawn.PositionHeld, map, false), new TargetInfo(this.parent.pawn.PositionHeld, map, false));
                effecter.Cleanup();
            }
            ThingDef postExplosionSpawnThingDef = props.postExplosionSpawnThingDef;
            float postExplosionSpawnChance = props.postExplosionSpawnChance;
            int postExplosionSpawnThingCount = props.postExplosionSpawnThingCount;
            GenExplosion.DoExplosion(this.parent.pawn.PositionHeld, map, num, props.explosiveDamageType, this.instigator ?? this.parent.pawn, null, null, null, postExplosionSpawnThingDef, postExplosionSpawnChance, postExplosionSpawnThingCount, props.applyDamageToExplosionCellsNeighbors, props.preExplosionSpawnThingDef, props.preExplosionSpawnChance, props.preExplosionSpawnThingCount);
        }
    }
}
