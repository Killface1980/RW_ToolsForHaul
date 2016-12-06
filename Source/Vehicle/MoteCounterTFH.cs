namespace ToolsForHaul
{
    public static class MoteCounterTFH
    {
        // vanilla = 250 motes + 250 TFH  => PC should handle this?
        private const int SaturatedCount = 250;

        private static int moteCount;

        public static bool Saturated
        {
            get
            {
                return Saturation > 1f;
            }
        }

        public static bool SaturatedLowPriority
        {
            get
            {
                return Saturation > 0.8f;
            }
        }

        // ToDo Detour the properties to get the higher motes count to work
        public static float Saturation
        {
            get
            {
                return moteCount / SaturatedCount;
            }
        }

        // [Detour(typeof(Verse.MoteCounter), bindingFlags = BindingFlags.Instance | BindingFlags.Public)]
        public static void Notify_MoteDespawned()
        {
            moteCount--;
        }

        // [Detour(typeof(Verse.MoteCounter), bindingFlags = BindingFlags.Instance | BindingFlags.Public)]
        public static void Notify_MoteSpawned()
        {
            moteCount++;
        }

        // [Detour(typeof(Verse.MoteCounter), bindingFlags = BindingFlags.Instance | BindingFlags.Public)]
        public static void Reinit()
        {
            moteCount = 0;
        }
    }
}