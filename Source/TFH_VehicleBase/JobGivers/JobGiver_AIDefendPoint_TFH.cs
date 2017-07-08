namespace TFH_VehicleBase.JobGivers
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
                wantCoverFromTarget = false
            }, out dest);
        }

        // RimWorld.JobGiver_AIFightEnemy
        protected override Job TryGiveJob(Pawn pawn)
        {
            var vehicleCart = pawn as Vehicle_Cart;

            if (!vehicleCart.MountableComp.IsMounted)
            {
                return null;
            }

            this.UpdateEnemyTarget(pawn);
            Thing enemyTarget = pawn.mindState.enemyTarget;
            if (enemyTarget == null)
            {
                return null;
            }

            bool allowManualCastWeapons = vehicleCart != null;
            //    bool allowManualCastWeapons = !pawn.IsColonist;
            Verb verb = pawn.TryGetAttackVerb(allowManualCastWeapons);
            if (verb == null)
            {
                return null;
            }
            if (verb.verbProps.MeleeRange)
            {
                return this.MeleeAttackJob(enemyTarget);
            }
            bool flag3 = verb.CanHitTarget(enemyTarget);
            if ((flag3))
            {
                return new Job(JobDefOf.WaitCombat, JobGiver_AIFightEnemy.ExpiryInterval_ShooterSucceeded.RandomInRange, true);
            }

            IntVec3 intVec;
            if (!this.TryFindShootingPosition(pawn, out intVec))
            {
                return null;
            }
            if (intVec == pawn.Position)
            {
                return new Job(JobDefOf.WaitCombat, JobGiver_AIFightEnemy.ExpiryInterval_ShooterSucceeded.RandomInRange, true);
            }
            pawn.Map.pawnDestinationManager.ReserveDestinationFor(pawn, intVec);
            return new Job(JobDefOf.Goto, intVec)
            {
                expiryInterval = JobGiver_AIFightEnemy.ExpiryInterval_ShooterSucceeded.RandomInRange,
                checkOverrideOnExpire = true
            };
        }

    }
}
