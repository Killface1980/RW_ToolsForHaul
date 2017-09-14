namespace TFH_Motes
{
    using Verse;

    public class MoteCounter_TFH : MapComponent
    {
        // vanilla = 250 motes + 250 TFH  => PC should handle this?
        private const int SaturatedCount = 1500;

        private  int moteCount = 0;

        public  bool Saturated
        {
            get
            {
                return this.Saturation > 1f;
            }
        }

        public bool SaturatedLowPriority
        {
            get
            {
                return this.Saturation > 0.8f;
            }
        }

        public  float Saturation
        {
            get
            {
                return this.moteCount / SaturatedCount;
            }
        }

        // [Detour(typeof(Verse.MoteCounter), bindingFlags = BindingFlags.Instance | BindingFlags.Public)]
        public void Notify_MoteDespawned()
        {
            this.moteCount--;
        }

        // [Detour(typeof(Verse.MoteCounter), bindingFlags = BindingFlags.Instance | BindingFlags.Public)]
        public void Notify_MoteSpawned()
        {
            this.moteCount++;
        }

        public MoteCounter_TFH(Map map)
            : base(map)
        {
        }
    }
}