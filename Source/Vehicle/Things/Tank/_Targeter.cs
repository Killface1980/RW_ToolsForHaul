using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ToolsForHaul
{
    public static class _Targeter
    {
        internal static FieldInfo _targetingVerb;
        internal static FieldInfo _targetingVerbAdditionalPawns;

        internal static Verb GetVerb(this Targeter _this)
        {
            if (_targetingVerb == null)
            {
                _targetingVerb = typeof(Targeter).GetField("targetingVerb", BindingFlags.Instance | BindingFlags.Public);
                if (_targetingVerb == null)
                {
                    Log.ErrorOnce("Unable to reflect Targeter.targetingVerb!", 0x12348765);
                }
            }
            return (Verb)_targetingVerb.GetValue(_this);
        }

        internal static List<Pawn> GetVerbAdditional(this Targeter _this)
        {
            if (_targetingVerbAdditionalPawns == null)
            {
                _targetingVerbAdditionalPawns = typeof(Targeter).GetField("targetingVerbAdditionalPawns", BindingFlags.Instance | BindingFlags.Public);
                if (_targetingVerbAdditionalPawns == null)
                {
                    Log.ErrorOnce("Unable to reflect Targeter.targetingVerbAdditionalPawns!", 0x12348765);
                }
            }
            return (List<Pawn>)_targetingVerbAdditionalPawns.GetValue(_this);
        }


        [Detour(typeof(Targeter), bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic)]
        private static void CastVerb(this Targeter _this)
        {
            Verb targetingVerb = _this.GetVerb();
            List<Pawn> targetingVerbAdditionalPawns = _this.GetVerbAdditional();

            if (targetingVerb.CasterIsPawn)
            {
                CastPawnVerb(targetingVerb);
                for (int i = 0; i < targetingVerbAdditionalPawns.Count; i++)
                {
                    Verb verb = (from x in targetingVerbAdditionalPawns[i].equipment.AllEquipmentVerbs
                                 where x.verbProps == targetingVerb.verbProps
                                 select x).FirstOrDefault<Verb>();
                    if (verb != null)
                    {
                        CastPawnVerb(verb);
                    }
                }
            }
            else if (targetingVerb.caster is Building_Turret)
            {
                TargetInfo targ = TargetInfo.Invalid;
                if (targetingVerb.CanTargetCell)
                {
                    targ = Gen.MouseCell();
                    if (!targ.Cell.InBounds())
                    {
                        targ = TargetInfo.Invalid;
                    }
                }
                using (IEnumerator<TargetInfo> enumerator = GenUI.TargetsAtMouse(targetingVerb.verbProps.targetParams, false).GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        TargetInfo current = enumerator.Current;
                        targ = current;
                    }
                }

                ((Building_Turret)targetingVerb.caster).OrderAttack(targ);
            }
            else if (targetingVerb.caster is Vehicle_Turret)
            {
                TargetInfo targ = TargetInfo.Invalid;
                if (targetingVerb.CanTargetCell)
                {
                    targ = Gen.MouseCell();
                    if (!targ.Cell.InBounds())
                    {
                        targ = TargetInfo.Invalid;
                    }
                }
                using (IEnumerator<TargetInfo> enumerator = GenUI.TargetsAtMouse(targetingVerb.verbProps.targetParams, false).GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        TargetInfo current = enumerator.Current;
                        targ = current;
                    }
                }

                ((Vehicle_Turret)targetingVerb.caster).OrderAttack(targ);
            }
        }

        // RimWorld.Targeter
        private static void CastPawnVerb(Verb verb)
        {
            foreach (TargetInfo current in GenUI.TargetsAtMouse(verb.verbProps.targetParams, false))
            {
                TargetInfo targetA = current;
                if (verb.verbProps.MeleeRange)
                {
                    Job job = new Job(JobDefOf.AttackMelee, targetA);
                    job.playerForced = true;
                    Pawn pawn = targetA.Thing as Pawn;
                    if (pawn != null)
                    {
                        job.killIncappedTarget = pawn.Downed;
                    }
                    verb.CasterPawn.drafter.TakeOrderedJob(job);
                }
                else
                {
                    JobDef jDef;
                    if (verb.verbProps.ai_IsWeapon)
                    {
                        jDef = JobDefOf.AttackStatic;
                    }
                    else
                    {
                        jDef = JobDefOf.UseVerbOnThing;
                    }
                    Job job2 = new Job(jDef);
                    job2.verbToUse = verb;
                    job2.targetA = targetA;
                    verb.CasterPawn.drafter.TakeOrderedJob(job2);
                }
            }
        }

    }
}
