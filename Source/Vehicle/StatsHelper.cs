using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace ToolsForHaul
{
    public static class StatsHelper
    {
        public static Dictionary<StatDef, float> GetWeightedWorkStats(this Pawn pawn)
        {
            Dictionary<StatDef, float> dict = new Dictionary<StatDef, float>();

                // add weights for all worktypes, multiplied by job priority
                foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(def => pawn.workSettings.WorkIsActive(def)))
                {
                    foreach (KeyValuePair<StatDef, float> stat in GetStatsOfWorkType(pawn, workType))
                    {
                        int priority = pawn.workSettings.GetPriority(workType);

                        float priorityAdjust;
                        switch (priority)
                        {
                            case 1:
                                priorityAdjust = 1f;
                                break;
                            case 2:
                                priorityAdjust = 0.5f;
                                break;
                            case 3:
                                priorityAdjust = 0.25f;
                                break;
                            case 4:
                                priorityAdjust = 0.125f;
                                break;
                            default:
                                priorityAdjust = 0.5f;
                                break;
                        }

                        float weight = stat.Value * priorityAdjust;

                        if (dict.ContainsKey(stat.Key))
                        {
                            dict[stat.Key] += weight;
                        }
                        else
                        {
                            dict.Add(stat.Key, weight);
                        }
                    }
                }

                foreach (StatDef key in new List<StatDef>(dict.Keys))
                {
                    if (key == StatDefOf.MoveSpeed)
                    {
                        switch (pawn.story.traits.DegreeOfTrait(TraitDef.Named("SpeedOffset")))
                        {
                            case -1:
                                dict[key] *= 1.5f;
                                break;
                            case 1:
                                dict[key] *= 0.5f;
                                break;
                            case 2:
                                dict[key] *= 0.25f;
                                break;
                        }
                    }

                    if (key == StatDefOf.WorkSpeedGlobal)
                    {
                        switch (pawn.story.traits.DegreeOfTrait(TraitDef.Named("Industriousness")))
                        {
                            case -2:
                                dict[key] *= 2f;
                                break;
                            case -1:
                                dict[key] *= 1.5f;
                                break;
                            case 1:
                                dict[key] *= 0.5f;
                                break;
                            case 2:
                                dict[key] *= 0.25f;
                                break;
                        }
                    }
                }
            

            if (dict.Count > 0)
            {
                // normalize weights
                float max = dict.Values.Select(Math.Abs).Max();
                foreach (StatDef key in new List<StatDef>(dict.Keys))
                {
                    // normalize max of absolute weigths to be 1.5
                    dict[key] /= max / 1.5f;
                }
            }

            return dict;
        }


        public static IEnumerable<KeyValuePair<StatDef, float>> GetStatsOfWorkType(Pawn pawn, WorkTypeDef worktype)
        {
            switch (worktype.defName)
            {
                case "Doctor":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MedicalOperationSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SurgerySuccessChance"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("BaseHealingQuality"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("HealingSpeed"), 0.5f);
                    yield break;

                case "PatientBedRest":
                    yield break;

                case "Flicker":
                    yield break;

                case "Warden":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.SocialImpact, 0.5f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.RecruitPrisonerChance, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.GiftImpact, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.TradePriceImprovement, 0.2f);
                    yield break;

                case "Handling":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.3f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, 0.1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.TameAnimalChance, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.TrainAnimalChance, 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MeleeDPS"), 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeHitChance, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 0.25f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 0.25f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyTouch, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_Cooldown, -0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_DamageAmount, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeHitChance, 0.2f);
                    //         yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryWeight"), 0.25f); // CR
                    //         yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryBulk"), 0.25f); // CR
                    yield break;

                case "Cooking":
                    //    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.05f);
                    //     yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CookSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.FoodPoisonChance, -0.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("BrewingSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ButcheryFleshSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ButcheryFleshEfficiency"), 1f);
                    yield break;

                case "Hunting":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.5f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AimingDelayFactor, -1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ShootingAccuracy, 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MeleeDPS"), 0.25f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeHitChance, 0.25f);
                    //   yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("AimingAccuracy"), 1f); // CR
                    //   yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ReloadSpeed"), 0.25f); // CR
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 0.25f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 0.25f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyShort, 0.4f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyMedium, 0.4f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyLong, 0.4f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.RangedWeapon_Cooldown, -0.8f);
                    yield break;

                case "Construction":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ConstructionSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.SmoothingSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, 0.25f);
                    yield break;

                case "Repair":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.FixBrokenDownBuildingFailChance, -1f);
                    yield break;

                case "Growing":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.PlantWorkSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.HarvestFailChance, -1f);
                    yield break;

                case "Mining":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MiningSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, 0.25f);
                    yield break;

                case "PlantCutting":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.PlantWorkSpeed, 0.5f);
                    yield break;

                case "Smithing":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SmithingSpeed"), 1f);
                    yield break;

                case "Tailoring":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("TailoringSpeed"), 1f);
                    yield break;

                case "Art":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SculptingSpeed"), 1f);
                    yield break;

                case "Crafting":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("StonecuttingSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SmeltingSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ButcheryMechanoidSpeed"), 0.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ButcheryMechanoidEfficiency"), 0.5f);
                    yield break;

                case "Hauling":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, 0.25f);
                    yield break;

                case "Cleaning":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.5f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.25f);
                    yield break;

                case "Research":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ResearchSpeed, 1f);
                    yield break;

                case "Managing":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.SocialImpact, 0.25f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ManagingSpeed"), 0.5f);
                    yield break;

                default:
                    yield break;
            }
        }

    }
}
