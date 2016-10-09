using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul
{
    public class JobDriver_PutInInventory : JobDriver
    {
        //Constants
        private const TargetIndex HaulableInd = TargetIndex.A;
        private const TargetIndex BackpackInd = TargetIndex.B;

        public override string GetReport()
        {
            Thing hauledThing = TargetThingA;

            string repString;
            if (hauledThing != null)
                repString = "ReportPutInInventory".Translate(hauledThing.LabelCap, CurJob.GetTarget(BackpackInd).Thing.LabelCap);
            else
                repString = "ReportPutSomethingInInventory".Translate(CurJob.GetTarget(BackpackInd).Thing.LabelCap);

            return repString;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Apparel_Backpack backpack = CurJob.GetTarget(BackpackInd).Thing as Apparel_Backpack;

            ///
            //Set fail conditions
            ///


            //Backpack is full.
            this.FailOn(() =>{ return pawn.inventory.container.Count < backpack.MaxItem ? false : true; });

            this.FailOn(() => { return pawn == backpack.wearer ? false : true; });
            ///
            //Define Toil
            ///

            Toil extractA = new Toil();
            extractA.initAction = () =>
            {
                if (!CurJob.targetQueueA.NullOrEmpty())
                {
                    CurJob.targetA = CurJob.targetQueueA.First();
                    CurJob.targetQueueA.RemoveAt(0);
                    this.FailOnDestroyedOrNull(HaulableInd);
                }
                else
                    EndJobWith(JobCondition.Succeeded);
            };

            Toil toilGoToThing = Toils_Goto.GotoThing(HaulableInd, PathEndMode.ClosestTouch)
                                    .FailOnDestroyedOrNull(HaulableInd);

            ///
            //Toils Start
            ///

            //Reserve thing to be stored and storage cell 
            yield return Toils_Reserve.Reserve(BackpackInd);
            yield return Toils_Reserve.Reserve(HaulableInd);
            yield return Toils_Reserve.ReserveQueue(HaulableInd);


            yield return Toils_Jump.JumpIf(toilGoToThing, () => { return CurJob.targetA.HasThing ? true : false;  });

            //Collect TargetQueue
            {

                //Extract an haulable into TargetA
                yield return extractA;

                yield return toilGoToThing;

                //CollectIntoCarrier
                Toil toilPutInInventory = new Toil();
                toilPutInInventory.initAction = () =>
                {
                    if (pawn.inventory.container.Count < backpack.MaxItem
                        && backpack.wearer.inventory.container.TotalStackCount < backpack.MaxStack)
                    {
                        if (CurJob.targetA.Thing.TryGetComp<CompForbiddable>() != null && CurJob.targetA.Thing.TryGetComp<CompForbiddable>().Forbidden)
                            CurJob.targetA.Thing.TryGetComp<CompForbiddable>().Forbidden = false;
                        if (pawn.inventory.container.TryAdd(CurJob.targetA.Thing, CurJob.maxNumToCarry))
                        {
                            CurJob.targetA.Thing.holder = pawn.inventory.GetContainer();
                            CurJob.targetA.Thing.holder.owner = pawn.inventory;
                            backpack.numOfSavedItems++;
                        }
                    }
                    else
                        EndJobWith(JobCondition.Incompletable);
                };
                yield return toilPutInInventory;
                yield return Toils_Jump.JumpIfHaveTargetInQueue(HaulableInd, extractA);
            }
        }

    }
}
