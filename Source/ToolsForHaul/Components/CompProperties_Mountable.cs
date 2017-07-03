// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompProperties_Axle.cs" company="">
// </copyright>
// <summary>
//   Defines the CompProperties_Axle type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace ToolsForHaul.Components
{
    using System.Collections.Generic;

    using UnityEngine;

    using Verse;

    public class CompProperties_Mountable : CompProperties
    {
        public Vector3 drawOffsetRotN = Vector3.zero;

        public Vector3 drawOffsetRotS = Vector3.zero;

        public CompProperties_Mountable()
        {
            this.compClass = typeof(CompMountable);
        }
    }
}