// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompDriver.cs" company="">
// </copyright>
// <summary>
//   Defines the CompDriver type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace ToolsForHaul.Components.Vehicles
{
    using Verse;

    public class CompRider : ThingComp
    {
        public Thing Vehicle { get; set; }

        public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            float hitChance = 0.25f;
            float hit = Rand.Value;

            if (hitChance <= hit)
            {
                // apply damage to vehicle here
                this.Vehicle?.TakeDamage(dinfo);

                absorbed = true;
                return;
            }

            absorbed = false;
        }
    }
}