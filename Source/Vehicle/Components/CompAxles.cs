using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ToolsForHaul.Components
{
    internal class CompAxles : ThingComp
    {
        public CompProperties_Axles Props
        {
            get
            {
                return (CompProperties_Axles)props;
            }
        }

        public bool HasAxles()
        {
            return Props.axles.Count > 0;
        }

        public bool GetAxleLocations(Vector2 drawSize, int flip, out List<Vector3> axleVecs)
        {
            axleVecs = new List<Vector3>();
            if (Props.axles.Count <= 0)
            {
                return false;
            }
            foreach (Vector2 current in Props.axles)
            {
                Vector3 item = new Vector3(current.x / 192f * drawSize.x * flip, 0f, current.y / 192f * drawSize.y);
                axleVecs.Add(item);
            }
            return true;
        }
    }
}
