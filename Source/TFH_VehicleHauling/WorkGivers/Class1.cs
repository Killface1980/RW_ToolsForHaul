namespace ToolsForHaul.WorkGivers
{
    using System;
    using System.Collections.Generic;

    using RimWorld;

    using Verse;
    using Verse.AI;

    public class WorkGiver_WithVehicle_ConstructDeliverResourcesToFrames : WorkGiver_ConstructDeliverResources
    {
        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForGroup(ThingRequestGroup.BuildingFrame);
            }
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.Faction != pawn.Faction)
            {
                return null;
            }
            Frame frame = t as Frame;
            if (frame == null)
            {
                return null;
            }
            if (!GenConstruct.CanConstruct(frame, pawn, forced))
            {
                return null;
            }
            return base.ResourceDeliverWithVehicleJobFor(pawn, frame, true);
        }

        // RimWorld.WorkGiver_ConstructDeliverResources
        protected Job ResourceDeliverWithVehicleJobFor(Pawn pawn, IConstructible c, bool canRemoveExistingFloorUnderNearbyNeeders = true)
        {
            Blueprint_Install blueprint_Install = c as Blueprint_Install;
            if (blueprint_Install != null)
            {
                return this.InstallJob(pawn, blueprint_Install);
            }
            bool flag = false;
            List<ThingCountClass> list = c.MaterialsNeeded();
            int count = list.Count;
            int i = 0;
            while (i < count)
            {
                WorkGiver_ConstructDeliverResources.< ResourceDeliverJobFor > c__AnonStorey2AF < ResourceDeliverJobFor > c__AnonStorey2AF = new WorkGiver_ConstructDeliverResources.< ResourceDeliverJobFor > c__AnonStorey2AF();

                    < ResourceDeliverJobFor > c__AnonStorey2AF.<> f__ref$686 = < ResourceDeliverJobFor > c__AnonStorey2AE;

                    < ResourceDeliverJobFor > c__AnonStorey2AF.need = list[i];
                if (!pawn.Map.itemAvailability.ThingsAvailableAnywhere(< ResourceDeliverJobFor > c__AnonStorey2AF.need, pawn))
                {
                    flag = true;
                    break;
                }
                WorkGiver_ConstructDeliverResources.< ResourceDeliverJobFor > c__AnonStorey2AF arg_EE_0 = < ResourceDeliverJobFor > c__AnonStorey2AF;
                Predicate<Thing> validator = (Thing r) => WorkGiver_ConstructDeliverResources.ResourceValidator(< ResourceDeliverJobFor > c__AnonStorey2AF.<> f__ref$686.pawn, < ResourceDeliverJobFor > c__AnonStorey2AF.need, r);
                arg_EE_0.foundRes = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(< ResourceDeliverJobFor > c__AnonStorey2AF.need.thingDef), PathEndMode.ClosestTouch, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, validator, null, 0, -1, false, RegionType.Set_Passable, false);
                if (< ResourceDeliverJobFor > c__AnonStorey2AF.foundRes != null)
                {
                    int resTotalAvailable;
                    this.FindAvailableNearbyResources(< ResourceDeliverJobFor > c__AnonStorey2AF.foundRes, pawn, out resTotalAvailable);
                    int num;
                    Job job;
                    HashSet<Thing> hashSet = this.FindNearbyNeeders(pawn, < ResourceDeliverJobFor > c__AnonStorey2AF.need, c, resTotalAvailable, canRemoveExistingFloorUnderNearbyNeeders, out num, out job);
                    if (job != null)
                    {
                        return job;
                    }
                    hashSet.Add((Thing)c);
                    Thing thing = hashSet.MinBy((Thing nee) => IntVec3Utility.ManhattanDistanceFlat(< ResourceDeliverJobFor > c__AnonStorey2AF.foundRes.Position, nee.Position));
                    hashSet.Remove(thing);
                    int num2 = 0;
                    int j = 0;
                    do
                    {
                        num2 += WorkGiver_ConstructDeliverResources.resourcesAvailable[j].stackCount;
                        j++;
                    }
                    while (num2 < num && j < WorkGiver_ConstructDeliverResources.resourcesAvailable.Count);
                    WorkGiver_ConstructDeliverResources.resourcesAvailable.RemoveRange(j, WorkGiver_ConstructDeliverResources.resourcesAvailable.Count - j);
                    WorkGiver_ConstructDeliverResources.resourcesAvailable.Remove(< ResourceDeliverJobFor > c__AnonStorey2AF.foundRes);
                    Job job2 = new Job(JobDefOf.HaulToContainer);
                    job2.targetA = < ResourceDeliverJobFor > c__AnonStorey2AF.foundRes;
                    job2.targetQueueA = new List<LocalTargetInfo>();
                    for (j = 0; j < WorkGiver_ConstructDeliverResources.resourcesAvailable.Count; j++)
                    {
                        job2.targetQueueA.Add(WorkGiver_ConstructDeliverResources.resourcesAvailable[j]);
                    }
                    job2.targetB = thing;
                    if (hashSet.Count > 0)
                    {
                        job2.targetQueueB = new List<LocalTargetInfo>();
                        foreach (Thing current in hashSet)
                        {
                            job2.targetQueueB.Add(current);
                        }
                    }
                    job2.targetC = (Thing)c;
                    job2.count = num;
                    job2.haulMode = HaulMode.ToContainer;
                    return job2;
                }
                else
                {
                    flag = true;
                    i++;
                }
            }
            if (flag)
            {
                JobFailReason.Is(WorkGiver_ConstructDeliverResources.MissingMaterialsTranslated);
            }
            return null;
        }

        // RimWorld.WorkGiver_ConstructDeliverResources
        private Job InstallJob(Pawn pawn, Blueprint_Install install)
        {
            Thing miniToInstallOrBuildingToReinstall = install.MiniToInstallOrBuildingToReinstall;
            if (miniToInstallOrBuildingToReinstall.IsForbidden(pawn) || !pawn.CanReserveAndReach(miniToInstallOrBuildingToReinstall, PathEndMode.OnCell, pawn.NormalMaxDanger(), 1, -1, null, false))
            {
                return null;
            }
            return new Job(JobDefOf.HaulToContainer)
                       {
                           targetA = miniToInstallOrBuildingToReinstall,
                           targetB = install,
                           count = 1,
                           haulMode = HaulMode.ToContainer
                       };
        }

    }
}
