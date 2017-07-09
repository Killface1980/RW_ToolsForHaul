// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompProperties_Axle.cs" company="">
// </copyright>
// <summary>
//   Defines the CompProperties_Axle type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace TFH_VehicleBase.Components
{
    using UnityEngine;

    using Verse;

    public class CompProperties_Rideable : CompProperties
    {
        public Vector3 drawOffsetRotN = Vector3.zero;

        public Vector3 drawOffsetRotS = Vector3.zero;

        public CompProperties_Rideable()
        {
            this.compClass = typeof(CompRideable);
        }
    }
}