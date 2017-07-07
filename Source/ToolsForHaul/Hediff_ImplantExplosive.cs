namespace ToolsForHaul
{
    using System.Text;

    using ToolsForHaul.Vehicles;

    using Verse;

    public class Hediff_ImplantExplosive : Hediff_Implant
    {
        public override bool ShouldRemove
        {
            get
            {
                return false;
            }
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);

            if (base.Part == null)
            {
                Log.Error("Part is null. It should be set before PostAdd for " + this.def + ".");
                return;
            }

         // this.pawn.health.RestorePart(base.Part, this, false);
         // for (int i = 0; i < base.Part.parts.Count; i++)
         // {
         //     Hediff_MissingPart hediff_MissingPart = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, this.pawn, null);
         //     hediff_MissingPart.IsFresh = true;
         //     hediff_MissingPart.lastInjury = HediffDefOf.SurgicalCut;
         //     hediff_MissingPart.Part = base.Part.parts[i];
         //     this.pawn.health.hediffSet.AddDirect(hediff_MissingPart, null);
         // }
        }
    }
}
