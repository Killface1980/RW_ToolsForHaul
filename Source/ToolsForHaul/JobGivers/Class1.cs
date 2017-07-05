namespace ToolsForHaul.JobGivers
{
    using RimWorld;

    using Verse;
    using Verse.AI;

    public class JobGiver_AIDefendPoint_TFH : JobGiver_AIFightEnemy
    {
        protected override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest)
        {
            Verb verb = pawn.TryGetAttackVerb(!pawn.IsColonist);
            if (verb == null)
            {
                dest = IntVec3.Invalid;
                return false;
            }
            return CastPositionFinder.TryFindCastPosition(new CastPositionRequest
                                                              {
                                                                  caster = pawn,
                                                                  target = pawn.mindState.enemyTarget,
                                                                  verb = verb,
                                                                  maxRangeFromTarget = 9999f,
                                                                  locus = (IntVec3)pawn.mindState.duty.focus,
                                                                  maxRangeFromLocus = pawn.mindState.duty.radius,
                                                                  wantCoverFromTarget = (verb.verbProps.range > 7f)
                                                              }, out dest);
        }
    }
}
