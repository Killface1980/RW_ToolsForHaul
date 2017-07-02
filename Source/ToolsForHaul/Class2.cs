using System;
using System.Text;
using Verse;

namespace ToolsForHaul
{
    using System.Collections.Generic;

    using RimWorld;

    using ToolsForHaul.Defs;
    using ToolsForHaul.Utilities;

    using UnityEngine;

    using Verse.AI;

    [StaticConstructorOnStartup]
    public class Building_BattleSpot : Building
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo c in base.GetGizmos())
            {
                yield return c;
            }

            Command_Action draft = new Command_Action();
            draft.hotKey = KeyBindingDefOf.CommandColonistDraft;
            draft.defaultLabel = "CommandDraftLabel".Translate();
            draft.defaultDesc = "CommandToggleDraftDesc".Translate();
            draft.icon = TexCommand.Draft;
            draft.activateSound = SoundDefOf.DraftOn;

            //     pris.isActive = (() => this.<> f__this.ForPrisoners);
            draft.action = delegate
                {
                    foreach (Pawn pawn in Find.VisibleMap.mapPawns.FreeColonistsSpawned)
                    {
                        if (pawn.mindState == null)
                            continue;
                        if (pawn.InMentalState)
                            continue;
                        if (pawn.Dead || pawn.Downed)
                            continue;


                        pawn.jobs.StopAll();

                        Thing vehicle = TFH_Utility.GetRightVehicle(pawn, pawn.AvailableVehiclesForPawnFaction(120f), WorkTypeDefOf.Hunting);


                        Job jobby = new Job(HaulJobDefOf.MountAndDraft)
                        {
                            targetA = vehicle,
                            targetB = this.Position,
                            locomotionUrgency = LocomotionUrgency.Sprint
                        };
                        pawn.jobs.TryTakeOrderedJob(jobby);

                    }
                    this.DeSpawn();
                };
            yield return draft;
        }
    }
}
