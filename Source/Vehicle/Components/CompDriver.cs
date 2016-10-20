using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ToolsForHaul
{
    public class CompDriver : ThingComp
    {
        public Vehicle_Cart vehicleCart;
        public Vehicle_Turret vehicleTurret;

        public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            float hitChance = 0.25f;
            float hit = Rand.Value;

            if (hitChance <= hit)
            {
                //apply damage to vehicle here
                if (vehicleCart != null)
                    vehicleCart.TakeDamage(dinfo);
                if (vehicleTurret != null)
                    vehicleTurret.TakeDamage(dinfo);
                absorbed = true;
                return;
            }
            absorbed = false;
        }
    }
}
