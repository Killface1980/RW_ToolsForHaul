using UnityEngine;
using Verse;

namespace ToolsForHaul.Components
{
    public class CompProperties_Mountable : CompProperties
    {
        public CompProperties_Mountable()
        {
            compClass = typeof(CompMountable);
        }

        public IntVec3 driverPosOffset = IntVec3.Zero;

    }
}
