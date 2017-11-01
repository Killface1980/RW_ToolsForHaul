// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MoteMakerTFH.cs" company="">
//   
// </copyright>
// <summary>
//   Slightly changed vanilla MoteMaker to allow more motes and use its own counter.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace TFH_Tools
{
    using RimWorld;

    using UnityEngine;

    using Verse;

    public static class MoteMakerTFH
    {

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
    }
}