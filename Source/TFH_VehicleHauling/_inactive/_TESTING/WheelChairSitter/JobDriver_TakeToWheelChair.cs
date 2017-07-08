using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    using TFH_VehicleBaseThings;

    public class JobDriver_TakeToWheelChair : JobDriver
    {
        private const TargetIndex TakeeIndex = TargetIndex.A;

        private const TargetIndex WheelChairIndex = TargetIndex.B;

        protected Pawn Patient
        {
            get
            {
                return (Pawn)CurJob.GetTarget(TakeeIndex).Thing;
            }
        }

        protected Vehicle_Cart WheelChair
        {
            get
            {
                return (Vehicle_Cart)CurJob.GetTarget(WheelChairIndex).Thing;
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {

            // this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            // this.FailOn(() => !this.Patient.InBed() || !this.Patient.Awake());
            // if (WheelChair != null)
            // {
            // this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
            // }
            // yield return Toils_Reserve.Reserve(TargetIndex.A, 1);
            // if (WheelChair != null)
            // {
            // yield return Toils_Reserve.Reserve(TargetIndex.B, 1);
            // yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.OnCell);
            // }
            // else
            // {
            // yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            // }
            // yield return Toils_Interpersonal.WaitToBeAbleToInteract(this.pawn);
            // yield return new Toil
            // {
            // tickAction = delegate
            // {
            // this.Patient.needs.joy.GainJoy(this.CurJob.def.joyGainRate * 0.000144f, this.CurJob.def.joyKind);
            // if (this.pawn.IsHashIntervalTick(320))
            // {
            // InteractionDef intDef = (Rand.Value >= 0.8f) ? InteractionDefOf.DeepTalk : InteractionDefOf.Chitchat;
            // this.pawn.interactions.TryInteractWith(this.Patient, intDef);
            // }
            // this.pawn.Drawer.rotator.FaceCell(this.Patient.Position);
            // this.pawn.GainComfortFromCellIfPossible();
            // JoyUtility.JoyTickCheckEnd(this.pawn, JoyTickFullJoyAction.None, 1f);
            // if (this.pawn.needs.joy.CurLevelPercentage > 0.9999f && this.Patient.needs.joy.CurLevelPercentage > 0.9999f)
            // {
            // this.pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
            // }
            // },
            // socialMode = RandomSocialMode.Off,
            // defaultCompleteMode = ToilCompleteMode.Delay,
            // defaultDuration = base.CurJob.def.joyDuration
            // };
            // yield break;
            this.FailOn(() => !Patient.InBed() || !Patient.Awake());

            this.FailOnDestroyedOrNull(TakeeIndex);
            this.FailOnDestroyedOrNull(WheelChairIndex);
            this.FailOnAggroMentalState(TakeeIndex);
            yield return Toils_Reserve.Reserve(TakeeIndex);
            yield return Toils_Reserve.Reserve(WheelChairIndex);


            yield return Toils_Goto.GotoThing(WheelChairIndex, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(WheelChairIndex).FailOnDespawnedNullOrForbidden(TakeeIndex).FailOn(() => CurJob.def == JobDefOf.Arrest && !Patient.CanBeArrested()).FailOn(() => !pawn.CanReach(WheelChair, PathEndMode.OnCell, Danger.Some)).FailOnSomeonePhysicallyInteracting(WheelChairIndex);

            yield return Toils_Haul.StartCarryThing(WheelChairIndex);
            yield return Toils_Goto.GotoThing(TakeeIndex, PathEndMode.InteractionCell);


            yield return Toils_Reserve.Release(WheelChairIndex);

            yield return new Toil
            {
                initAction = delegate
                {
                    IntVec3 position = WheelChair.InteractionCell;
                    Thing thing;
                    pawn.carrier.TryDropCarriedThing(position, ThingPlaceMode.Direct, out thing);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            yield return Toils_Cart.MountOtherOn(WheelChairIndex, Patient);

            yield return new Toil
            {
                initAction = delegate
                {
                    foreach (var missingPart in Patient.health.hediffSet.GetMissingPartsCommonAncestors())
                    {
                        if (missingPart.Part.def == BodyPartDefOf.LeftLeg ||
                            missingPart.Part.def == BodyPartDefOf.RightLeg)
                        {
                            Patient.health.RestorePart(missingPart.Part);
                            Patient.health.AddHediff(HediffDef.Named("HediffWheelChair"), missingPart.Part);
                            break;
                        }
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Delay
            };


            yield break;
        }
    }
}
