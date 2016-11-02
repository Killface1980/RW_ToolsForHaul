using System;
using CommunityCoreLibrary;

namespace ToolsForHaul
{
    public static class MoteCounter
    {
        // changed the max motes to 512, vanilla = 250
        private const int SaturatedCount = 512;

        private static int moteCount;

        public static float Saturation
        {
            get
            {
                return moteCount / SaturatedCount;
            }
        }

        [DetourClassProperty(typeof(MoteCounter), "Saturated", InjectionSequence.DLLLoad, InjectionTiming.Priority_23)]
        public static bool Saturated
        {
            get
            {
                return Saturation > 1f;
            }
        }

        [DetourClassProperty(typeof(MoteCounter), "SaturatedLowPriority", InjectionSequence.DLLLoad, InjectionTiming.Priority_23)]
        public static bool SaturatedLowPriority
        {
            get
            {
                return Saturation > 0.8f;
            }
        }

        [DetourClassMethod(typeof(MoteCounter), "Reinit", InjectionSequence.DLLLoad, InjectionTiming.Priority_23)]
        public static void Reinit()
        {
            moteCount = 0;
        }

        [DetourClassMethod(typeof(MoteCounter), "Notify_MoteSpawned", InjectionSequence.DLLLoad, InjectionTiming.Priority_23)]
        public static void Notify_MoteSpawned()
        {
            moteCount++;
        }

        [DetourClassMethod(typeof(MoteCounter), "Notify_MoteDespawned", InjectionSequence.DLLLoad, InjectionTiming.Priority_23)]
        public static void Notify_MoteDespawned()
        {
            moteCount--;
        }
    }
}
