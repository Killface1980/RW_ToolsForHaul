using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using ToolsForHaul.Components;
using ToolsForHaul.JobDefs;
using ToolsForHaul.Toils;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ToolsForHaul.JobDrivers
{
    public class JobDriver_GoForRide : JobDriver
    {
        private const TargetIndex DrivePathInd = TargetIndex.A;
        private const TargetIndex MountableInd = TargetIndex.B;

        private const int tickCheckInterval = 64;

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.EndOnDespawnedOrNull(MountableInd);
            if (TargetThingB.IsForbidden(pawn.Faction))
                this.FailOnForbidden(MountableInd);

            yield return Toils_Reserve.Reserve(MountableInd, CurJob.def.joyMaxParticipants);

            Toil toil = Toils_Goto.GotoCell(DrivePathInd, PathEndMode.OnCell);
            toil.tickAction = delegate
            {
                if (Find.TickManager.TicksGame > startTick + CurJob.def.joyDuration)
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }
                JoyUtility.JoyTickCheckEnd(pawn, JoyTickFullJoyAction.EndJob, 1f);
            };
            ThingWithComps cart = CurJob.GetTarget(MountableInd).Thing as ThingWithComps;

            //JumpIf already mounted
            yield return Toils_Jump.JumpIf(toil, () =>
            {
                if (cart.TryGetComp<CompMountable>().Driver == pawn) return true;
                return false;
            });

            //Mount on Target
            yield return Toils_Goto.GotoThing(MountableInd, PathEndMode.ClosestTouch)
                                        .FailOnDestroyedOrNull(MountableInd);
            yield return Toils_Cart.MountOn(MountableInd);



            yield return toil;
            yield return new Toil
            {
                initAction = delegate
                {
                    if (CurJob.targetQueueA.Count > 0)
                    {
                        TargetInfo targetA = CurJob.targetQueueA[0];
                        CurJob.targetQueueA.RemoveAt(0);
                        CurJob.targetA = targetA;
                        JumpToToil(toil);
                        return;
                    }
                }
            };
            yield break;
        }

    }
}
