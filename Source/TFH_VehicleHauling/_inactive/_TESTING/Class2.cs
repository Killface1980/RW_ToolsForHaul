using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ppumkin.LEDTechnology.GlowFlooders;

using RimWorld;

using UnityEngine;

using Verse;

namespace ppumkin.LEDTechnology.Managers
{
    public static class CustomGlowFloodManager
    {
        public static List<IGlowFlooder> customFlooders; // = new List<IGlowFlooder>();

        static CustomGlowFloodManager()
        {
            customFlooders = new List<IGlowFlooder>();
        }

        public static void RegisterFlooder(IGlowFlooder theFlooder)
        {
            // Log.Message("Registered new customGlower:");
            // Log.Message(theFlooder.ToString());
            customFlooders.Add(theFlooder);
        }

        public static void DeRegisterGlower(IGlowFlooder theFlooder)
        {
            theFlooder.Clear();
            customFlooders.Remove(theFlooder);
        }

        public static bool IsGlowerRegistered(IGlowFlooder theFlooder)
        {
            return customFlooders.Contains(theFlooder);
        }

        public static void RefreshGlowFlooders()
        {
            // Log.Message("CustomGlowFlooder: Refreshing all registered glowFlooders");
            foreach (var customFlooder in customFlooders)
            {
                customFlooder.CalculateGlowFlood();
            }
        }
    }
}