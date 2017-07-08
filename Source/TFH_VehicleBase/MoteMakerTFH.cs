// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MoteMakerTFH.cs" company="">
//   
// </copyright>
// <summary>
//   Slightly changed vanilla MoteMaker to allow more motes and use its own counter.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace TFH_VehicleBase
{
    using UnityEngine;

    using Verse;

    public static class MoteMakerTFH
    {

        public static void PlaceTireTrack(Vector3 loc, Map map, float rot, Vector3 pos)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.GetComponent<MoteCounterTFH>().SaturatedLowPriority)
            {
                return;
            }
            MoteThrown_TFH moteThrown = (MoteThrown_TFH)ThingMaker.MakeThing(MoteDefOf.Mote_Track_ATV);
            moteThrown.exactRotation = rot;
            moteThrown.exactPosition = loc;
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowDustPuff(Vector3 loc, Map map, float scale)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.GetComponent<MoteCounterTFH>().SaturatedLowPriority)
            {
                return;
            }

            MoteThrown_TFH moteThrown = (MoteThrown_TFH)ThingMaker.MakeThing(MoteDefOf.Mote_DustPuffTFH);
            moteThrown.Scale = 1.9f * scale;
            moteThrown.rotationRate = Rand.Range(-60, 60);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity(Rand.Range(0, 360), Rand.Range(0.6f, 0.75f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }

        public static void ThrowMicroSparks(Vector3 loc, Map map)
        {
            if (!loc.ShouldSpawnMotesAt(map) || map.GetComponent<MoteCounterTFH>().SaturatedLowPriority)
            {
                return;
            }

            MoteThrown_TFH moteThrown = (MoteThrown_TFH)ThingMaker.MakeThing(MoteDefOf.Mote_MicroSparksTFH);
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
            if (!loc.ShouldSpawnMotesAt(map) || map.GetComponent<MoteCounterTFH>().SaturatedLowPriority)
            {
                return;
            }

            MoteThrown_TFH moteThrown = (MoteThrown_TFH)ThingMaker.MakeThing(MoteDefOf.Mote_SmokeTFH);
            moteThrown.Scale = Rand.Range(1.5f, 2.5f) * size;
            moteThrown.rotationRate = Rand.Range(-30f, 30f);
            moteThrown.exactPosition = loc;
            moteThrown.SetVelocity(Rand.Range(30, 40), Rand.Range(0.5f, 0.7f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }
    }
}