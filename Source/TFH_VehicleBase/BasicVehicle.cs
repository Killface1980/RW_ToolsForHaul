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
        public CompMountable MountableComp { get; set; }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.MountableComp = this.TryGetComp<CompMountable>();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo c in base.GetGizmos())
            {
                yield return c;
            }

            if (this.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            if (!this.MountableComp.IsMounted)
            {
                Designator_Mount designator =
                    new Designator_Mount
                        {
                            vehicle = this,
                            defaultLabel = Static.TxtCommandMountLabel.Translate(),
                            defaultDesc = Static.TxtCommandMountDesc.Translate(),
                            icon = Static.IconMount,
                            activateSound = Static.ClickSound
                        };
                yield return designator;
            }
            else
            {
                if (this.MountableComp.Driver != null)
                {
                    yield return new Command_Action
                                     {
                                         defaultLabel = Static.TxtCommandDismountLabel.Translate(),
                                         defaultDesc = Static.TxtCommandDismountDesc.Translate(),
                                         icon = Static.IconUnmount,
                                         activateSound = Static.ClickSound,
                                         action = delegate
                                             {
                                                 TFH_BaseUtility.DismountGizmoFloatMenu(
                                                     this.MountableComp.Driver);
                                             }
                                     };
                }
            }


        }

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
