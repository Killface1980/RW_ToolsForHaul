using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RimWorld;
using ToolsForHaul.Toils;
using ToolsForHaul.Utilities;
using Verse;
using Verse.AI;

namespace ToolsForHaul.JobDrivers
{
    public class JobDriver_Hunt : JobDriver
    {
        private const TargetIndex VictimInd = TargetIndex.A;

        private const TargetIndex CorpseInd = TargetIndex.A;

        private const TargetIndex StoreCellInd = TargetIndex.B;

        private const TargetIndex VehicleInd = TargetIndex.C;

        private const int MaxHuntTicks = 5000;

        private int jobStartTick = -1;

        public Pawn Victim
        {
            get
            {
                Corpse corpse = Corpse;
                if (corpse != null)
                {
                    return corpse.innerPawn;
                }
                return (Pawn)CurJob.GetTarget(VictimInd).Thing;
            }
        }

        private Corpse Corpse
        {
            get
            {
                return CurJob.GetTarget(CorpseInd).Thing as Corpse;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue<int>(ref jobStartTick, "jobStartTick", 0, false);
        }

        public override string GetReport()
        {
            return CurJob.def.reportString.Replace("TargetA", Victim.LabelShort);
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(delegate
            {
                if (!pawn.equipment.Primary.def.IsRangedWeapon)
                {
                    if (MapComponent_ToolsForHaul.previousPawnWeapons.ContainsKey(pawn))
                    {
                        Apparel_Toolbelt toolbelt = ToolsForHaulUtility.TryGetToolbelt(pawn);
                        if (toolbelt != null)
                        {
                            foreach (Thing slot in toolbelt.slotsComp.slots)
                            {
                                if (slot == MapComponent_ToolsForHaul.previousPawnWeapons[pawn])
                                {
                                    toolbelt.slotsComp.SwapEquipment(slot as ThingWithComps);
                                    MapComponent_ToolsForHaul.previousPawnWeapons.Remove(pawn);
                                    break;
                                }
                            }
                        }
                        MapComponent_ToolsForHaul.previousPawnWeapons.Remove(pawn);
                        return false;
                    }
                }
                return true;
            });

            if (CurJob.GetTarget(VehicleInd).Thing is Vehicle_Cart || CurJob.GetTarget(VehicleInd).Thing is Vehicle_Turret)
            {

                yield return Toils_Reserve.Reserve(VehicleInd);
                //Mount on Target
                yield return Toils_Goto.GotoThing(VehicleInd, PathEndMode.ClosestTouch)
                                            .FailOnDestroyedOrNull(VehicleInd);
                yield return Toils_Cart.MountOn(VehicleInd);
            }

            this.FailOn(delegate
            {
                if (!CurJob.ignoreDesignations)
                {
                    Pawn victim = Victim;
                    if (victim != null && !victim.Dead && Find.DesignationManager.DesignationOn(victim, DesignationDefOf.Hunt) == null)
                    {
                        return true;
                    }
                }
                return false;
            });
            yield return Toils_Reserve.Reserve(VictimInd, 1);
            yield return new Toil
            {
                initAction = delegate
                {
                    jobStartTick = Find.TickManager.TicksGame;
                }
            };
            yield return Toils_Combat.TrySetJobToUseAttackVerb();
            Toil startCollectCorpseToil = StartCollectCorpseToil();
            Toil goToCastPosition = Toils_Combat.GotoCastPosition(VictimInd, true).JumpIfDespawnedOrNull(VictimInd, startCollectCorpseToil).FailOn(() => Find.TickManager.TicksGame > jobStartTick + MaxHuntTicks);
            yield return goToCastPosition;
            Toil toil3 = Toils_Jump.JumpIfTargetNotHittable(VictimInd, goToCastPosition);
            yield return toil3;
            yield return Toils_Jump.JumpIfTargetDownedDistant(VictimInd, goToCastPosition);
            yield return Toils_Combat.CastVerb(VictimInd, false).JumpIfDespawnedOrNull(VictimInd, startCollectCorpseToil).FailOn(() => Find.TickManager.TicksGame > jobStartTick + MaxHuntTicks);
            yield return Toils_Jump.JumpIfTargetDespawnedOrNull(VictimInd, startCollectCorpseToil);
            yield return Toils_Jump.Jump(toil3);
            yield return startCollectCorpseToil;
            yield return Toils_Goto.GotoCell(CorpseInd, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(CorpseInd).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(CorpseInd);
            Toil toil4 = Toils_Haul.CarryHauledThingToCell(StoreCellInd);
            yield return toil4;
            yield return Toils_Haul.PlaceHauledThingInCell(StoreCellInd, toil4, true);
            yield break;
        }

        private Toil StartCollectCorpseToil()
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                if (Victim == null)
                {
                    toil.actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
                TaleRecorder.RecordTale(TaleDefOf.Hunted, new object[]
                {
                    pawn,
                    Victim
                });
                Corpse corpse = HuntJobUtility.TryFindCorpse(Victim);
                if (corpse == null || !pawn.CanReserveAndReach(corpse, PathEndMode.ClosestTouch, Danger.Deadly, 1))
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
                corpse.SetForbidden(false, true);
                IntVec3 vec;
                if (StoreUtility.TryFindBestBetterStoreCellFor(corpse, pawn, StoragePriority.Unstored, pawn.Faction, out vec, true))
                {
                    Find.Reservations.Reserve(pawn, corpse, 1);
                    Find.Reservations.Reserve(pawn, vec, 1);
                    pawn.CurJob.SetTarget(StoreCellInd, vec);
                    pawn.CurJob.SetTarget(CorpseInd, corpse);
                    pawn.CurJob.maxNumToCarry = 1;
                    pawn.CurJob.haulMode = HaulMode.ToCellStorage;
                    return;
                }
                pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
            };
            return toil;
        }
    }
}
