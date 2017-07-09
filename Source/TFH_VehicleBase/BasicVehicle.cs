using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TFH_VehicleBase
{
    using RimWorld;

    using TFH_VehicleBase.Components;
    using TFH_VehicleBase.Designators;

    using Verse;
    using Verse.AI;

    public class BasicVehicle : Pawn
    {



        public virtual bool ClaimableBy(Faction claimee)
        {
            // No vehicles if enemy near


            if (base.Faction != null)
            {
                if (claimee != base.Faction)
                {
                    if (base.Faction.HostileTo(claimee))
                    {
                        foreach (IAttackTarget attackTarget in this.Map.attackTargetsCache.TargetsHostileToFaction(claimee))
                        {
                            if (attackTarget.Thing.Position.InHorDistOf(this.Position, 20f))
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;

            // CompPowerTrader comp = this.GetComp<CompPowerTrader>();
            // if (comp == null || !comp.PowerOn)
            // {
            // CompMannable comp2 = this.GetComp<CompMannable>();
            // if (comp2 == null || !comp2.MannedNow)
            // {
            // return true;
            // }
            // }
        }



    }
}
