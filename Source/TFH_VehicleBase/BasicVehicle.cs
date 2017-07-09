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
    using Verse.Sound;

    public class BasicVehicle : Pawn
    {
        public CompMountable MountableComp;

        public CompDriver DriverComp;

        public List<HediffCompExplosive_TFH> ExplosiveTickers;

        public bool wickStarted;

        private Sustainer sustainerAmbient;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            this.MountableComp = this.TryGetComp<CompMountable>();

            // Get Exploder info
            List<Hediff> hediffSetHediffs = this.health?.hediffSet?.hediffs;
            if (!hediffSetHediffs.NullOrEmpty())
            {
                this.ExplosiveTickers = new List<HediffCompExplosive_TFH>();
                foreach (var hediff in hediffSetHediffs)
                {
                    HediffCompExplosive_TFH exploder = hediff.TryGetComp<HediffCompExplosive_TFH>();

                    if (exploder != null)
                    {
                        this.ExplosiveTickers.Add(exploder);
                    }
                }
            }

            // Reload textures to get colored versions
            if (this.RaceProps.IsMechanoid)
            {
                LongEventHandler.ExecuteWhenFinished(
                    delegate
                        {
                            PawnKindLifeStage curKindLifeStage = this.ageTracker.CurKindLifeStage;

                            this.Drawer.renderer.graphics.nakedGraphic =
                                curKindLifeStage.bodyGraphicData.Graphic.GetColoredVersion(
                                    ShaderDatabase.Cutout,
                                    this.DrawColor,
                                    this.DrawColorTwo);
                        });
            }

        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
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
                yield return new Command_Action
                                 {
                                     defaultLabel = Static.TxtCommandDismountLabel.Translate(),
                                     defaultDesc = Static.TxtCommandDismountDesc.Translate(),
                                     icon = Static.IconUnmount,
                                     activateSound = Static.ClickSound,
                                     action = delegate
                                         {
                                             TFH_BaseUtility.DismountGizmoFloatMenu(
                                                 this.MountableComp.Rider);
                                         }
                                 };
            }
        }

        public void AddDriverComp()
        {
            this.DriverComp = new CompDriver
                                  {
                                      Vehicle = this,
                                      Pawn = this.MountableComp.Rider,
                                      parent = this.MountableComp.Rider
                                  };
            this.MountableComp.Rider.AllComps.Add(this.DriverComp);
        }


        public void StartSustainerVehicleIfInactive()
        {
            CompVehicle vehicleComp = this.TryGetComp<CompVehicle>();
            if (vehicleComp != null && (!vehicleComp.compProps.soundAmbient.NullOrUndefined()
                                        && this.sustainerAmbient == null))
            {
                SoundInfo info = SoundInfo.InMap(this, MaintenanceType.None);
                this.sustainerAmbient = vehicleComp.compProps.soundAmbient.TrySpawnSustainer(info);
            }
        }

        public void EndSustainerVehicleIfActive()
        {
            if (this.sustainerAmbient != null)
            {
                this.sustainerAmbient.End();
                this.sustainerAmbient = null;
            }
        }
    }
}
