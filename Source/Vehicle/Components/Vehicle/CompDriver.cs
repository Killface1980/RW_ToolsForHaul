// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompDriver.cs" company="">
// </copyright>
// <summary>
//   Defines the CompDriver type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace ToolsForHaul.Components.Vehicles
{
    using System.Collections.Generic;

    using ToolsForHaul.Components.Vehicle;
    using ToolsForHaul.Utilities;

    using UnityEngine;

    using Verse;

    public class CompDriver : ThingComp
    {
        public Thing Vehicle { get; set; }

        public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            var pawn = parent as Pawn;

            if (pawn.RaceProps.Animal)
            {
                absorbed = false;
                return;
            }

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

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo c in base.CompGetGizmosExtra())
            {
                yield return c;
            }
            yield return new Command_Action
            {
                defaultLabel = Strings.TxtCommandDismountLabel.Translate(),
                defaultDesc = Strings.TxtCommandDismountDesc.Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/IconUnmount"),
                activateSound = SoundDef.Named("Click"),
                action = ToolsForHaulUtility.GetCartByDriver(this.parent as Pawn).MountableComp.Dismount
            };
            var pawn = parent as Pawn;

            if (pawn != null)
            {
                if (pawn.RaceProps.Animal)
                {
                    Designator_Board designatorBoard =
                        new Designator_Board
                        {
                            vehicle = this.parent,
                            defaultLabel = "CommandRideLabel".Translate(),
                            defaultDesc = "CommandRideDesc".Translate(),
                            icon = ContentFinder<Texture2D>.Get("UI/Commands/IconBoard"),
                            activateSound = SoundDef.Named("Click")
                        };


                    yield return designatorBoard;
                    // if (mountableComp.IsMounted && this.innerContainer.Count(x => x is Pawn) >= maxNumBoarding)
                    // {
                    //     Command_Action commandUnboardAll = new Command_Action();

                    // commandUnboardAll.defaultLabel = "CommandGetOffLabel".Translate();
                    // commandUnboardAll.defaultDesc = "CommandGetOffDesc".Translate();
                    // commandUnboardAll.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconUnboardAll");
                    // commandUnboardAll.activateSound = SoundDef.Named("Click");
                    // commandUnboardAll.action = () => { this.UnboardAll(); };
                    //
                    // yield return commandUnboardAll;

                }

            }
        }

    }
}