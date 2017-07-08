// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompProperties_Axle.cs" company="">
// </copyright>
// <summary>
//   Defines the CompProperties_Axle type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace TFH_VehicleBase.Components
{
    using System.Collections.Generic;

    using UnityEngine;

    using Verse;

    public class CompProperties_Axle : CompProperties
    {
        public List<Vector2> axles = new List<Vector2>();


        public CompProperties_Axle()
        {
            this.compClass = typeof(CompAxles);
        }
    }
}