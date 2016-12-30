using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HugsLib.Source.Detour;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ToolsForHaul
{
    public static class _TendUtility
    {
        // RimWorld._TendUtility
        private const float ChanceToDevelopBondRelationOnTended = 0.004f;

        private static List<Hediff_MissingPart> bleedingStumps = new List<Hediff_MissingPart>();

        private static List<Hediff> otherHediffs = new List<Hediff>();

        [DetourMethod(typeof(TendUtility), "DoTend")]
        public static void DoTend(Pawn doctor, Pawn patient, Medicine medicine)
        {
            if (!patient.health.HasHediffsNeedingTend(false))
            {
                return;
            }
            if (medicine != null && medicine.Destroyed)
            {
                Log.Warning("Tried to use destroyed medicine.");
                medicine = null;
            }
            float num = (medicine == null) ? 0.2f : medicine.def.GetStatValueAbstract(StatDefOf.MedicalPotency, null);
            float quality = num;
            Building_Bed building_Bed = patient.CurrentBed();
            if (building_Bed != null)
            {
                quality += building_Bed.GetStatValue(StatDefOf.MedicalTendQualityOffset, true);
            }
            if (doctor != null)
            {
                quality *= doctor.GetStatValue(StatDefOf.HealingQuality, true);
            }
            quality = Mathf.Clamp01(quality);
            if (patient.health.hediffSet.GetInjuriesTendable().Any<Hediff_Injury>())
            {
                float maxSeverity = 0f;
                int batchPosition = 0;

                // added prio by bleeding, everything else vanilla
                foreach (Hediff_Injury current in from x in patient.health.hediffSet.GetInjuriesTendable().Where(x => x.BleedRate > 0)
                                                  orderby x.BleedRate descending
                                                  select x)
                {
                    float severity = Mathf.Min(current.Severity, 20f);
                    if (maxSeverity + severity > 20f)
                    {
                        break;
                    }
                    maxSeverity += severity;
                    current.Tended(quality, batchPosition);
                    if (medicine == null)
                    {
                        break;
                    }
                    batchPosition++;
                }

                foreach (Hediff_Injury current in from x in patient.health.hediffSet.GetInjuriesTendable()
                                                  orderby x.Severity descending
                                                  select x)
                {
                    float severity = Mathf.Min(current.Severity, 20f);
                    if (maxSeverity + severity > 20f)
                    {
                        break;
                    }
                    maxSeverity += severity;
                    current.Tended(quality, batchPosition);
                    if (medicine == null)
                    {
                        break;
                    }
                    batchPosition++;
                }
            }
            else
            {
                _TendUtility.bleedingStumps.Clear();
                List<Hediff_MissingPart> missingPartsCommonAncestors = patient.health.hediffSet.GetMissingPartsCommonAncestors();
                for (int i = 0; i < missingPartsCommonAncestors.Count; i++)
                {
                    if (missingPartsCommonAncestors[i].IsFresh)
                    {
                        _TendUtility.bleedingStumps.Add(missingPartsCommonAncestors[i]);
                    }
                }
                if (_TendUtility.bleedingStumps.Count > 0)
                {
                    _TendUtility.bleedingStumps.RandomElement<Hediff_MissingPart>().IsFresh = false;
                    _TendUtility.bleedingStumps.Clear();
                }
                else
                {
                    _TendUtility.otherHediffs.Clear();
                    _TendUtility.otherHediffs.AddRange(patient.health.hediffSet.GetTendableNonInjuryNonMissingPartHediffs());
                    Hediff hediff;
                    if (_TendUtility.otherHediffs.TryRandomElement(out hediff))
                    {
                        HediffCompProperties_TendDuration hediffCompProperties_TendDuration = hediff.def.CompProps<HediffCompProperties_TendDuration>();
                        if (hediffCompProperties_TendDuration != null && hediffCompProperties_TendDuration.tendAllAtOnce)
                        {
                            int num6 = 0;
                            for (int j = 0; j < _TendUtility.otherHediffs.Count; j++)
                            {
                                if (_TendUtility.otherHediffs[j].def == hediff.def)
                                {
                                    _TendUtility.otherHediffs[j].Tended(quality, num6);
                                    num6++;
                                }
                            }
                        }
                        else
                        {
                            hediff.Tended(quality, 0);
                        }
                    }
                    _TendUtility.otherHediffs.Clear();
                }
            }
            if (doctor != null && patient.HostFaction == null && patient.Faction != null && patient.Faction != doctor.Faction)
            {
                patient.Faction.AffectGoodwillWith(doctor.Faction, 0.3f);
            }
            if (doctor != null && doctor.RaceProps.Humanlike && patient.RaceProps.Animal && RelationsUtility.TryDevelopBondRelation(doctor, patient, ChanceToDevelopBondRelationOnTended) && doctor.Faction != null && doctor.Faction != patient.Faction)
            {
                InteractionWorker_RecruitAttempt.DoRecruit(doctor, patient, 1f, false);
            }
            patient.records.Increment(RecordDefOf.TimesTendedTo);
            if (doctor != null)
            {
                doctor.records.Increment(RecordDefOf.TimesTendedOther);
            }
            if (medicine != null)
            {
                if ((patient.Spawned || (doctor != null && doctor.Spawned)) && num > 1f)
                {
                    SoundDef.Named("TechMedicineUsed").PlayOneShot(new TargetInfo(patient.Position, patient.Map, false));
                }
                if (medicine.stackCount > 1)
                {
                    medicine.stackCount--;
                }
                else if (!medicine.Destroyed)
                {
                    medicine.Destroy(DestroyMode.Vanish);
                }
            }
        }

    }
}
