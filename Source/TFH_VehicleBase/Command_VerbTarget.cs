namespace TFH_VehicleBase
{
    using System;

    using RimWorld;

    using UnityEngine;

    using Verse;
    using Verse.Sound;

    internal class Command_VerbTarget : Command
    {
        public Verb verb;

        public Action action;

        public override Color IconDrawColor
        {
            get
            {
                if (this.verb.ownerEquipment != null)
                {
                    return this.verb.ownerEquipment.DrawColor;
                }
                return base.IconDrawColor;
            }
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            SoundDefOf.TickTiny.PlayOneShotOnCamera(null);
            Targeter targeter = Find.Targeter;
            if (this.verb.CasterIsPawn && targeter.targetingVerb != null && targeter.targetingVerb.verbProps == this.verb.verbProps)
            {
                Pawn casterPawn = this.verb.CasterPawn;
                if (!targeter.IsPawnTargeting(casterPawn))
                {
                    targeter.targetingVerbAdditionalPawns.Add(casterPawn);
                }
            }
            else
            {
                Find.Targeter.BeginTargeting(this.verb);
            }
          
        }
    }
}
