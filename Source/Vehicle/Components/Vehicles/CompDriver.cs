using Verse;

namespace ToolsForHaul.Components
{
    public class CompDriver : ThingComp
    {
        public Thing vehicle;

        public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            float hitChance = 0.25f;
            float hit = Rand.Value;

            if (hitChance <= hit)
            {
                //apply damage to vehicle here
                if (vehicle != null)
                    vehicle.TakeDamage(dinfo);

                absorbed = true;
                return;
            }
            absorbed = false;
        }
    }
}
