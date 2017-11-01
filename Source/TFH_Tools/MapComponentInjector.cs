namespace TFH_Tools
{
    using System;

    using UnityEngine;

    using Verse;

    // This code is mostly borrowed from Pawn State Icons mod by Dan Sadler, which has open source and no license I could find, so...
    // Replace with yours.
    public class MapComponentInjector : MonoBehaviour
    {
        private static Type toolsForHaul = typeof(MapComponent_ToolsForHaul);

        public void FixedUpdate()
        {
            if (Current.ProgramState != ProgramState.Playing)
            {
                return;
            }

            if (Find.VisibleMap.components.FindAll(c => c.GetType() == toolsForHaul).Count == 0)
            {
                Find.VisibleMap.components.Add((MapComponent)Activator.CreateInstance(toolsForHaul));

                Log.Message("ToolsForHaul :: Added TFH to the map.");
            }

            Destroy(this);
        }
    }
}