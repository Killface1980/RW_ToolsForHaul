namespace ToolsForHaul.WorkGivers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using RimWorld;

    using ToolsForHaul.Vehicles;

    using UnityEngine;

    using Verse;
    using Verse.AI;

    public class WorkGiver_DoBillVehicle : WorkGiver_Scanner
    {
        private class DefCountList
        {
            private List<ThingDef> defs = new List<ThingDef>();

            private List<float> counts = new List<float>();

            public int Count
            {
                get
                {
                    return this.defs.Count;
                }
            }

            public float this[ThingDef def]
            {
                get
                {
                    int num = this.defs.IndexOf(def);
                    if (num < 0)
                    {
                        return 0f;
                    }

                    return this.counts[num];
                }

                set
                {
                    int num = this.defs.IndexOf(def);
                    if (num < 0)
                    {
                        this.defs.Add(def);
                        this.counts.Add(value);
                        num = this.defs.Count - 1;
                    }
                    else
                    {
                        this.counts[num] = value;
                    }

                    this.CheckRemove(num);
                }
            }

            public float GetCount(int index)
            {
                return this.counts[index];
            }

            public void SetCount(int index, float val)
            {
                this.counts[index] = val;
                this.CheckRemove(index);
            }

            public ThingDef GetDef(int index)
            {
                return this.defs[index];
            }

            private void CheckRemove(int index)
            {
                if (this.counts[index] == 0f)
                {
                    this.counts.RemoveAt(index);
                    this.defs.RemoveAt(index);
                }
            }

            public void Clear()
            {
                this.defs.Clear();
                this.counts.Clear();
            }

            public void GenerateFrom(List<Thing> things)
            {
                this.Clear();
                for (int i = 0; i < things.Count; i++)
                {
                    ThingDef def;
                    ThingDef expr_1C = def = things[i].def;
                    float num = this[def];
                    this[expr_1C] = num + (float)things[i].stackCount;
                }
            }
        }

        private List<ThingAmount> chosenIngThings = new List<ThingAmount>();

        private static readonly IntRange ReCheckFailedBillTicksRange = new IntRange(500, 600);

        private static string MissingMaterialsTranslated;

        private static string MissingSkillTranslated;

        private static List<Thing> relevantThings = new List<Thing>();

        private static HashSet<Thing> processedThings = new HashSet<Thing>();

        private static List<Thing> newRelevantThings = new List<Thing>();

        private static List<IngredientCount> ingredientsOrdered = new List<IngredientCount>();

        private static List<Thing> tmpMedicine = new List<Thing>();

        private static DefCountList availableCounts = new DefCountList();

        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.InteractionCell;
            }
        }

        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForGroup(ThingRequestGroup.Pawn);
            }
        }

        public WorkGiver_DoBillVehicle()
        {
            if (MissingSkillTranslated == null)
            {
                MissingSkillTranslated = "MissingSkill".Translate();
            }

            if (MissingMaterialsTranslated == null)
            {
                MissingMaterialsTranslated = "MissingMaterials".Translate();
            }
        }

        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            IBillGiver billGiver = thing as IBillGiver;
            Vehicle_Cart cart = thing as Vehicle_Cart;
            if (billGiver == null || !this.ThingIsUsableBillGiver(thing) || !cart.InParkingLot || !billGiver.BillStack.AnyShouldDoNow || !pawn.CanReserve(thing, 1, -1, null, forced) || thing.IsBurning() || thing.IsForbidden(pawn))
            {
                return null;
            }

            if (!pawn.CanReach(thing.InteractionCell, PathEndMode.OnCell, Danger.Some, false, TraverseMode.ByPawn))
            {
                return null;
            }

            billGiver.BillStack.RemoveIncompletableBills();
            return this.StartOrResumeBillJob(pawn, billGiver);
        }

        private Job StartOrResumeBillJob(Pawn pawn, IBillGiver giver)
        {
            foreach (Bill bill in giver.BillStack)
            {
                if (bill.recipe.requiredGiverWorkType == null || bill.recipe.requiredGiverWorkType == this.def.workType)
                {
                    if (Find.TickManager.TicksGame >= bill.lastIngredientSearchFailTicks + ReCheckFailedBillTicksRange.RandomInRange || FloatMenuMakerMap.making)
                    {
                        bill.lastIngredientSearchFailTicks = 0;
                        if (bill.ShouldDoNow())
                        {
                            if (bill.PawnAllowedToStartAnew(pawn))
                            {
                                if (!bill.recipe.PawnSatisfiesSkillRequirements(pawn))
                                {
                                    JobFailReason.Is(MissingSkillTranslated);
                                }
                                else
                                {

                                    if (TryFindBestBillIngredients(bill, pawn, (Thing)giver, this.chosenIngThings))
                                    {
                                        return this.TryStartNewDoBillJob(pawn, bill, giver);
                                    }

                                    if (!FloatMenuMakerMap.making)
                                    {
                                        bill.lastIngredientSearchFailTicks = Find.TickManager.TicksGame;
                                    }
                                    else
                                    {
                                        JobFailReason.Is(MissingMaterialsTranslated);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        private Job TryStartNewDoBillJob(Pawn pawn, Bill bill, IBillGiver giver)
        {
            Job job = WorkGiverUtility.HaulStuffOffBillGiverJob(pawn, giver, null);
            if (job != null)
            {
                return job;
            }

            Job job2 = new Job(JobDefOf.DoBill, (Thing)giver)
                           {
                               targetQueueB =
                                   new List<LocalTargetInfo>(
                                       this.chosenIngThings.Count),
                               countQueue = new List<int>(this.chosenIngThings.Count)
                           };
            for (int i = 0; i < this.chosenIngThings.Count; i++)
            {
                job2.targetQueueB.Add(this.chosenIngThings[i].thing);
                job2.countQueue.Add(this.chosenIngThings[i].count);
            }

            job2.haulMode = HaulMode.ToCellNonStorage;
            job2.bill = bill;
            return job2;
        }

        private bool ThingIsUsableBillGiver(Thing thing)
        {
            Vehicle_Cart cart = thing as Vehicle_Cart;
            Corpse corpse = thing as Corpse;
            Pawn pawn2 = null;

            if (corpse != null)
            {
                pawn2 = corpse.InnerPawn;
            }

            if (this.def.fixedBillGiverDefs != null && this.def.fixedBillGiverDefs.Contains(thing.def))
            {
                return true;
            }

            if (cart != null)
            {
                if (this.def.billGiversAllMechanoids && cart.RaceProps.IsMechanoid)
                {
                    return true;
                }
            }

            if (corpse != null && pawn2 != null)
            {
                if (this.def.billGiversAllMechanoidsCorpses && pawn2.RaceProps.IsMechanoid)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindBestBillIngredients(Bill bill, Pawn pawn, Thing billGiver, List<ThingAmount> chosen)
        {
            chosen.Clear();
            newRelevantThings.Clear();
            if (bill.recipe.ingredients.Count == 0)
            {
                return true;
            }

            IntVec3 rootCell = GetBillGiverRootCell(billGiver, pawn);
            Region rootReg = rootCell.GetRegion(pawn.Map, RegionType.Set_Passable);
            if (rootReg == null)
            {
                return false;
            }

            MakeIngredientsListInProcessingOrder(ingredientsOrdered, bill);
            relevantThings.Clear();
            processedThings.Clear();
            bool foundAll = false;
            Predicate<Thing> baseValidator = (Thing t) => t.Spawned && !t.IsForbidden(pawn) && (float)(t.Position - billGiver.Position).LengthHorizontalSquared < bill.ingredientSearchRadius * bill.ingredientSearchRadius && bill.IsFixedOrAllowedIngredient(t) && bill.recipe.ingredients.Any((IngredientCount ingNeed) => ingNeed.filter.Allows(t)) && pawn.CanReserve(t, 1, -1, null, false);
            bool billGiverIsPawn = billGiver is Vehicle_Cart;
            if (billGiverIsPawn)
            {
                AddEveryMedicineToRelevantThings(pawn, billGiver, relevantThings, baseValidator, pawn.Map);
                if (TryFindBestBillIngredientsInSet(relevantThings, bill, chosen))
                {
                    return true;
                }
            }

            TraverseParms traverseParams = TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false);
            RegionEntryPredicate entryCondition = (Region from, Region r) => r.Allows(traverseParams, false);
            int adjacentRegionsAvailable = rootReg.Neighbors.Count((Region region) => entryCondition(rootReg, region));
            int regionsProcessed = 0;
            processedThings.AddRange(relevantThings);
            RegionProcessor regionProcessor = delegate (Region r)
                {
                    List<Thing> list = r.ListerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.HaulableEver));
                    foreach (Thing thing in list)
                    {
                        if (!processedThings.Contains(thing))
                        {
                            if (ReachabilityWithinRegion.ThingFromRegionListerReachable(thing, r, PathEndMode.ClosestTouch, pawn))
                            {
                                if (baseValidator(thing) && (!thing.def.IsMedicine || !billGiverIsPawn))
                                {
                                    newRelevantThings.Add(thing);
                                    processedThings.Add(thing);
                                }
                            }
                        }
                    }

                    regionsProcessed++;
                    if (newRelevantThings.Count > 0 && regionsProcessed > adjacentRegionsAvailable)
                    {
                        Comparison<Thing> comparison = delegate (Thing t1, Thing t2)
                            {
                                float num = (float)(t1.Position - rootCell).LengthHorizontalSquared;
                                float value = (float)(t2.Position - rootCell).LengthHorizontalSquared;
                                return num.CompareTo(value);
                            };
                        newRelevantThings.Sort(comparison);
                        relevantThings.AddRange(newRelevantThings);
                        newRelevantThings.Clear();
                        if (TryFindBestBillIngredientsInSet(relevantThings, bill, chosen))
                        {
                            foundAll = true;
                            return true;
                        }
                    }

                    return false;
                };
            RegionTraverser.BreadthFirstTraverse(rootReg, entryCondition, regionProcessor, 99999, RegionType.Set_Passable);
            relevantThings.Clear();
            newRelevantThings.Clear();
            return foundAll;
        }

        private static IntVec3 GetBillGiverRootCell(Thing billGiver, Pawn forPawn)
        {
            Vehicle_Cart cart = billGiver as Vehicle_Cart;
            
            if (cart.def.hasInteractionCell)
            {
                return cart.InteractionCell;
            }
                return billGiver.Position;
            Log.Error("Tried to find bill ingredients for " + billGiver + " which has no interaction cell.");
            return forPawn.Position;
        }

        private static void AddEveryMedicineToRelevantThings(Pawn pawn, Thing billGiver, List<Thing> relevantThings, Predicate<Thing> baseValidator, Map map)
        {
            MedicalCareCategory medicalCareCategory = GetMedicalCareCategory(billGiver);
            List<Thing> list = map.listerThings.ThingsInGroup(ThingRequestGroup.Medicine);
            tmpMedicine.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                Thing thing = list[i];
                if (medicalCareCategory.AllowsMedicine(thing.def) && baseValidator(thing) && pawn.CanReach(thing, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                {
                    tmpMedicine.Add(thing);
                }
            }

            tmpMedicine.SortBy((Thing x) => -x.GetStatValue(StatDefOf.MedicalPotency, true), (Thing x) => x.Position.DistanceToSquared(billGiver.Position));
            relevantThings.AddRange(tmpMedicine);
            tmpMedicine.Clear();
        }

        private static MedicalCareCategory GetMedicalCareCategory(Thing billGiver)
        {
            Vehicle_Cart pawn = billGiver as Vehicle_Cart;
            if (pawn != null && pawn.playerSettings != null)
            {
                return pawn.playerSettings.medCare;
            }

            return MedicalCareCategory.Best;
        }

        private static void MakeIngredientsListInProcessingOrder(List<IngredientCount> ingredientsOrdered, Bill bill)
        {
            ingredientsOrdered.Clear();
            if (bill.recipe.productHasIngredientStuff)
            {
                ingredientsOrdered.Add(bill.recipe.ingredients[0]);
            }

            for (int i = 0; i < bill.recipe.ingredients.Count; i++)
            {
                if (!bill.recipe.productHasIngredientStuff || i != 0)
                {
                    IngredientCount ingredientCount = bill.recipe.ingredients[i];
                    if (ingredientCount.IsFixedIngredient)
                    {
                        ingredientsOrdered.Add(ingredientCount);
                    }
                }
            }

            for (int j = 0; j < bill.recipe.ingredients.Count; j++)
            {
                IngredientCount item = bill.recipe.ingredients[j];
                if (!ingredientsOrdered.Contains(item))
                {
                    ingredientsOrdered.Add(item);
                }
            }
        }

        private static bool TryFindBestBillIngredientsInSet(List<Thing> availableThings, Bill bill, List<ThingAmount> chosen)
        {
            if (bill.recipe.allowMixingIngredients)
            {
                return TryFindBestBillIngredientsInSet_AllowMix(availableThings, bill, chosen);
            }

            return TryFindBestBillIngredientsInSet_NoMix(availableThings, bill, chosen);
        }

        private static bool TryFindBestBillIngredientsInSet_NoMix(List<Thing> availableThings, Bill bill, List<ThingAmount> chosen)
        {
            RecipeDef recipe = bill.recipe;
            chosen.Clear();
            availableCounts.Clear();
            availableCounts.GenerateFrom(availableThings);
            for (int i = 0; i < ingredientsOrdered.Count; i++)
            {
                IngredientCount ingredientCount = recipe.ingredients[i];
                bool flag = false;
                for (int j = 0; j < availableCounts.Count; j++)
                {
                    float num = (float)ingredientCount.CountRequiredOfFor(availableCounts.GetDef(j), bill.recipe);
                    if (num <= availableCounts.GetCount(j))
                    {
                        if (ingredientCount.filter.Allows(availableCounts.GetDef(j)))
                        {
                            if (ingredientCount.IsFixedIngredient || bill.ingredientFilter.Allows(availableCounts.GetDef(j)))
                            {
                                for (int k = 0; k < availableThings.Count; k++)
                                {
                                    if (availableThings[k].def == availableCounts.GetDef(j))
                                    {
                                        int num2 = availableThings[k].stackCount - ThingAmount.CountUsed(chosen, availableThings[k]);
                                        if (num2 > 0)
                                        {
                                            int num3 = Mathf.Min(Mathf.FloorToInt(num), num2);
                                            ThingAmount.AddToList(chosen, availableThings[k], num3);
                                            num -= (float)num3;
                                            if (num < 0.001f)
                                            {
                                                flag = true;
                                                float num4 = availableCounts.GetCount(j);
                                                num4 -= (float)ingredientCount.CountRequiredOfFor(availableCounts.GetDef(j), bill.recipe);
                                                availableCounts.SetCount(j, num4);
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (flag)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

                if (!flag)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryFindBestBillIngredientsInSet_AllowMix(List<Thing> availableThings, Bill bill, List<ThingAmount> chosen)
        {
            chosen.Clear();
            for (int i = 0; i < bill.recipe.ingredients.Count; i++)
            {
                IngredientCount ingredientCount = bill.recipe.ingredients[i];
                float num = ingredientCount.GetBaseCount();
                for (int j = 0; j < availableThings.Count; j++)
                {
                    Thing thing = availableThings[j];
                    if (ingredientCount.filter.Allows(thing))
                    {
                        if (ingredientCount.IsFixedIngredient || bill.ingredientFilter.Allows(thing))
                        {
                            float num2 = bill.recipe.IngredientValueGetter.ValuePerUnitOf(thing.def);
                            int num3 = Mathf.Min(Mathf.CeilToInt(num / num2), thing.stackCount);
                            ThingAmount.AddToList(chosen, thing, num3);
                            num -= (float)num3 * num2;
                            if (num <= 0.0001f)
                            {
                                break;
                            }
                        }
                    }
                }

                if (num > 0.0001f)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
