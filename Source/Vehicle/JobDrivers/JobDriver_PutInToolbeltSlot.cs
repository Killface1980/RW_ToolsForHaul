using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace ToolsForHaul.JobDrivers
{
    public class JobDriver_PutInToolbeltSlot : JobDriver
    {
        // Constants
        public const TargetIndex HaulableInd = TargetIndex.A;
        public const TargetIndex SlotterInd = TargetIndex.B;

        public override string GetReport()
        {
            Thing hauledThing = this.TargetThingA;

            string repString;
            if (hauledThing != null)
                repString = "ReportPutInInventory".Translate(hauledThing.LabelCap, this.CurJob.GetTarget(SlotterInd).Thing.LabelCap);
            else
                repString = "ReportPutSomethingInInventory".Translate(this.CurJob.GetTarget(SlotterInd).Thing.LabelCap);

            return repString;
        }


        protected override IEnumerable<Toil> MakeNewToils()
        {
            Apparel_Toolbelt toolbelt = this.CurJob.GetTarget(SlotterInd).Thing as Apparel_Toolbelt;

            // no free slots
            this.FailOn(() => toolbelt.slotsComp.slots.Count >= toolbelt.MaxItem);


            // reserve resources
            yield return Toils_Reserve.ReserveQueue(HaulableInd);

            // extract next target thing from targetQueue
            Toil toilExtractNextTarget = Toils_JobTransforms.ExtractNextTargetFromQueue(HaulableInd);
            yield return toilExtractNextTarget;

            Toil toilGoToThing = Toils_Goto.GotoThing(HaulableInd, PathEndMode.ClosestTouch)
                .FailOnDespawnedOrNull(HaulableInd);
            yield return toilGoToThing;

            Toil pickUpThingIntoSlot = new Toil
            {
                initAction = () =>
                {
                    if (!toolbelt.slotsComp.slots.TryAdd(this.CurJob.targetA.Thing))
                    {
                        this.EndJobWith(JobCondition.Incompletable);
                    }
                    else
                    {
                        float statfloat = 0;
                        Thing thing = this.TargetThingA;
                        // add stats to pawn inventory
                        foreach (KeyValuePair<StatDef, float> stat in pawn.GetWeightedWorkStats())
                        {
                            statfloat = RightTools.GetMaxStat(thing as ThingWithComps, stat.Key);
                            if (statfloat > 0)
                            {
                                MapComponent_ToolsForHaul.CachedToolEntries.Add(new MapComponent_ToolsForHaul.Entry(pawn, thing, stat.Key, statfloat));
                            }
                            for (int i = toolbelt.slotsComp.slots.Count - 1; i >= 0; i--)
                            {
                                var tool = toolbelt.slotsComp.slots[i];
                                var checkstat = RightTools.GetMaxStat(tool as ThingWithComps, stat.Key);
                                if (checkstat > 0 && checkstat < statfloat)
                                {
                                    Thing dropTool;
                                    toolbelt.slotsComp.slots.TryDrop(tool, pawn.Position, pawn.Map, ThingPlaceMode.Near, out dropTool, null);
                                    for (int j = MapComponent_ToolsForHaul.CachedToolEntries.Count - 1;
                                        j >= 0;
                                        j--)
                                    {
                                        var entry = MapComponent_ToolsForHaul.CachedToolEntries[j];
                                        if (entry.tool == tool)
                                            MapComponent_ToolsForHaul.CachedToolEntries.RemoveAt(j);
                                    }
                                }
                            }
                        }
                    }
                }
            };
            yield return pickUpThingIntoSlot;

            yield return Toils_Jump.JumpIfHaveTargetInQueue(HaulableInd, toilExtractNextTarget);
        }
    }
}