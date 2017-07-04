namespace ToolsForHaul
{
    using Verse;
    public class MoteCounterTFH : MapComponent
    {
        // vanilla = 250 motes + 250 TFH  => PC should handle this?
        private const int SaturatedCount = 1500;

        private  int moteCount = 0;

        public  bool Saturated
        {
            get
            {
                return Saturation > 1f;
            }
        }

        public bool SaturatedLowPriority
        {
            get
            {
                return Saturation > 0.8f;
            }
        }

        public  float Saturation
        {
            get
            {
                return moteCount / SaturatedCount;
            }
        }

        // [Detour(typeof(Verse.MoteCounter), bindingFlags = BindingFlags.Instance | BindingFlags.Public)]
        public void Notify_MoteDespawned()
        {
            moteCount--;
        }

        // [Detour(typeof(Verse.MoteCounter), bindingFlags = BindingFlags.Instance | BindingFlags.Public)]
        public void Notify_MoteSpawned()
        {
            moteCount++;
        }

        public MoteCounterTFH(Map map)
            : base(map)
        {
            //
        }
    }
}