namespace TFH_Tools.Components
{
    using RimWorld;

    using Verse;

    public class CompEquipmentGizmoProvider : ThingComp
    {
        public Pawn Owner => this.owner;
        private Pawn owner;

        public void SetOwner(Pawn p)
        {
            this.owner = p;
        }

        public bool ParentIsEquipped
        {
            get
            {
                if (this.Owner != null && (this.owner.equipment.AllEquipmentListForReading.Contains(this.parent)
                                           || this.owner.apparel.WornApparel.Contains(this.parent as Apparel)))
                {
                    return true;
                }

                return false;
            }
        }
    }
}