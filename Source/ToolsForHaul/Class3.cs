using System;
using Verse;

namespace ToolsForHaul
{
    using RimWorld;

    public class CompProperties_Explosive_TFH : CompProperties
    {
        public float explosiveRadius = 1.9f;

        public DamageDef explosiveDamageType = DamageDefOf.Bomb;

        public ThingDef postExplosionSpawnThingDef;

        public float postExplosionSpawnChance;

        public int postExplosionSpawnThingCount = 1;

        public bool applyDamageToExplosionCellsNeighbors;

        public ThingDef preExplosionSpawnThingDef;

        public float preExplosionSpawnChance;

        public int preExplosionSpawnThingCount = 1;

        public float explosiveExpandPerStackcount;

        public EffecterDef explosionEffect;

        public DamageDef startWickOnDamageTaken;

        public float startWickHitPointsPercent = 0.2f;

        public IntRange wickTicks = new IntRange(140, 150);

        public float wickScale = 1f;

        public float chanceNeverExplodeFromDamage;

        public CompProperties_Explosive_TFH()
        {
            this.compClass = typeof(CompExplosive_TFH);
        }
    }
}
