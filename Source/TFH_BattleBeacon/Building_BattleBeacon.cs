namespace TFH_BattleBeacon
{
    using System.Collections.Generic;

    using RimWorld;

    using TFH_VehicleBase;
    using TFH_VehicleBase.DefOfs_TFH;

    using Verse;
    using Verse.AI;

    [StaticConstructorOnStartup]
    public class Building_BattleBeacon : Building
    {
        private int ticksToDespawn;

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo c in base.GetGizmos())
            {
                yield return c;
            }

            Command_Action draft = new Command_Action
                                       {
                                           hotKey = KeyBindingDefOf.CommandColonistDraft,
                                           defaultLabel = "CommandDraftLabel".Translate(),
                                           defaultDesc = "CommandToggleDraftDesc".Translate(),
                                           icon = TexCommand.Draft,
                                           activateSound = SoundDefOf.DraftOn,
                                           action = delegate
                                               {
                                                   foreach (Pawn pawn in Find.VisibleMap.mapPawns
                                                       .FreeColonistsSpawned)
                                                   {
                                                       if (pawn.mindState == null)
                                                       {
                                                           continue;
                                                       }

                                                       if (pawn.InMentalState)
                                                       {
                                                           continue;
                                                       }

                                                       if (pawn.Dead || pawn.Downed)
                                                       {
                                                           continue;
                                                       }

                                                       pawn.jobs.StopAll();

                                                       Thing vehicle = TFH_BaseUtility.GetRightVehicle(
                                                           pawn,
                                                           pawn.AvailableVehiclesForPawnFaction(120f),
                                                           WorkTypeDefOf.Hunting);

                                                       Job jobby =
                                                           new Job(VehicleJobDefOf.MountAndDraft)
                                                               {
                                                                   targetA
                                                                       = vehicle,
                                                                   targetB
                                                                       = this
                                                                           .Position,
                                                                   locomotionUrgency
                                                                       = LocomotionUrgency
                                                                           .Sprint
                                                               };
                                                       pawn.jobs.TryTakeOrderedJob(jobby);
                                                   }

                                                   this.DeSpawn();
                                               }
                                       };

            // pris.isActive = (() => this.<> f__this.ForPrisoners);
            yield return draft;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            this.ticksToDespawn = 3000;
        }

        public override void Tick()
        {
            base.Tick();

            this.ticksToDespawn--;

            if (this.ticksToDespawn == 0)
            {
                this.Destroy(DestroyMode.Deconstruct);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.ticksToDespawn, "ticksToDespawn");
        }
    }
}
