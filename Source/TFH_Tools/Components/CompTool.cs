namespace TFH_Tools.Components
{
    using System.Collections.Generic;

    using RimWorld;

    using UnityEngine;

    using Verse;

    public class CompTool : CompEquipmentGizmoProvider
    {
        public int wearTicks;
        public bool wasAutoEquipped;

        public CompTool_Properties Props => (CompTool_Properties)this.props;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);

            this.wearTicks = this.Props.workTicksPerHealthPercent;
        }

        public bool Allows(string workType)
        {
            return this.Props.workTypes.Contains(workType) || workType == "Hunting" && this.Props.usedForHunting;
        }

        public void ToolUseTick()
        {
            if (this.wearTicks-- <= 0)
            {
                this.parent.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, 1));
                this.wearTicks = this.Props.workTicksPerHealthPercent;
            }
        }

        // toggle allowance to use this type of weapon as hunting tool
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Toggle
            {
                defaultLabel = "Use for hunting",
                defaultDesc = "Make your colonists automatically use " + this.parent.def.label + " for hunting.",
                icon = ContentFinder<Texture2D>.Get("UI/Designators/Hunt"),
                isActive = () => this.Props.usedForHunting,
                toggleAction = () => { this.Props.usedForHunting = !this.Props.usedForHunting; }
            };
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref this.wearTicks, "wearTicks");
            Scribe_Values.Look(ref this.Props.usedForHunting, "usedForHunting");
            Scribe_Values.Look(ref this.wasAutoEquipped, "wasAutoEquipped");
        }
    }

    public class CompTool_Properties : CompProperties
    {
        public List<string> workTypes = new List<string>();
        public int workTicksPerHealthPercent;
        public bool usedForHunting;

        public CompTool_Properties()
        {
            this.compClass = typeof(CompTool);
        }
    }
}