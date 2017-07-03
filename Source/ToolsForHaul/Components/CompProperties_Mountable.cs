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
        public List<Vector3> seats = new List<Vector3>();

        public CompProperties_Mountable()
        {
            this.compClass = typeof(CompMountable);
        }
    }
}