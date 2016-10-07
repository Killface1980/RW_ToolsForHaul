
using System;
using ToolsForHaul;
using UnityEngine;
using Verse;

namespace RW_FacialStuff       // Replace with yours.
{       // This code is mostly borrowed from Pawn State Icons mod by Dan Sadler, which has open source and no license I could find, so...
    public class MapComponentInjector : MonoBehaviour
    {
        private static Type toolsForHaul = typeof(MapComponent_ToolsForHaul);

        public void FixedUpdate()
        {
            if (Current.ProgramState != ProgramState.MapPlaying)
            {
                return;
            }

            if (Find.Map.components.FindAll(c => c.GetType() == toolsForHaul).Count == 0)
            {
                Find.Map.components.Add((MapComponent)Activator.CreateInstance(toolsForHaul));

                Log.Message("ToolsForHaul :: Added an TFH to the map.");
            }

            Destroy(this);
        }
    }


}