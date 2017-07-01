// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MoteMakerTFH.cs" company="">
//   
// </copyright>
// <summary>
//   Slightly changed vanilla MoteMaker to allow more motes and use its own counter.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ToolsForHaul
{
    using RimWorld;

    using UnityEngine;

    using Verse;

    public static class MoteMakerTFH
    {

        public static void PlaceTireTrack(Vector3 loc, Map map, float rot, Vector3 pos)
        {
                if (loc.ShouldSpawnMotesAt(map) && !MoteCounterTFH.SaturatedLowPriority)
                {
                    MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(TFH_ThingDefOf.Mote_Track_ATV);
                    moteThrown.exactRotation = rot;
                    moteThrown.exactPosition = loc;
                    GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
                }
        }

        public static void ThrowDustPuff(Vector3 loc, Map map, float scale)
        {
            if (!loc.ShouldSpawnMotesAt(map) || MoteCounterTFH.SaturatedLowPriority)
            {
                return;
            }

            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDefOf.Mote_DustPuff);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = Rand.Range(-60, 60);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity(Rand.Range(0, 360), Rand.Range(0.6f, 0.75f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowMetaPuffs(TargetInfo targ, Map map)
        {
            Vector3 a = (!targ.HasThing) ? targ.Cell.ToVector3Shifted() : targ.Thing.TrueCenter();
            int num = Rand.RangeInclusive(4, 6);
            for (int i = 0; i < num; i++)
            {
                Vector3 loc = a + new Vector3(Rand.Range(-0.5f, 0.5f), 0f, Rand.Range(-0.5f, 0.5f));
                MoteMaker.ThrowMetaPuff(loc, map);
            }
        }

        public static void ThrowMicroSparks(Vector3 loc, Map map)
        {
            if (!loc.ShouldSpawnMotesAt(map) || MoteCounterTFH.SaturatedLowPriority)
            {
                return;
            }

            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDefOf.Mote_MicroSparks);
            moteThrown.Scale = Rand.Range(0.8f, 1.2f);
            moteThrown.rotationRate = Rand.Range(-12f, 12f);
            moteThrown.exactPosition = loc;
            moteThrown.exactPosition -= new Vector3(0.5f, 0f, 0.5f);
            moteThrown.exactPosition += new Vector3(Rand.Value, 0f, Rand.Value);
            moteThrown.SetVelocity(Rand.Range(35, 45), 1.2f);
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowSmoke(Vector3 loc, Map map, float size)
        {
            if (!loc.ShouldSpawnMotesAt(map) || MoteCounterTFH.SaturatedLowPriority)
            {
                return;
            }

            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDefOf.Mote_Smoke);
            moteThrown.Scale = Rand.Range(1.5f, 2.5f) * size;
            moteThrown.rotationRate = Rand.Range(-30f, 30f);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity(Rand.Range(30, 40), Rand.Range(0.5f, 0.7f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }
    }
}