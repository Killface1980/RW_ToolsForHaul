using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ToolsForHaul
{
    internal class CompProperties_Axles : CompProperties
    {
        public List<Vector2> axles = new List<Vector2>();

        public CompProperties_Axles()
        {
            compClass = typeof(CompAxles);
        }
    }
}
