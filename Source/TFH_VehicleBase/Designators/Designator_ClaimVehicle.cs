namespace TFH_VehicleBase.Designators
{
    using RimWorld;

    using UnityEngine;

    using Verse;

    public class Designator_ClaimVehicle : Designator_Claim
    {
        public override int DraggableDimensions
        {
            get
            {
                return 2;
            }
        }

        public Designator_ClaimVehicle()
        {
            this.defaultLabel = "DesignatorClaim".Translate();
            this.defaultDesc = "DesignatorClaimDesc".Translate();
            this.icon = ContentFinder<Texture2D>.Get("UI/Designators/Claim", true);
            this.soundDragSustain = SoundDefOf.DesignateDragStandard;
            this.soundDragChanged = SoundDefOf.DesignateDragStandardChanged;
            this.useMouseIcon = true;
            this.soundSucceeded = SoundDefOf.DesignateClaim;
            this.hotKey = KeyBindingDefOf.Misc4;
        }


        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            Vehicle_Cart cart = t as Vehicle_Cart;
            return cart != null && cart.Faction != Faction.OfPlayer && cart.ClaimableBy(Faction.OfPlayer);
        }

        public override void DesignateThing(Thing t)
        {
            t.SetFaction(Faction.OfPlayer, null);
            CellRect.CellRectIterator iterator = t.OccupiedRect().GetIterator();
            while (!iterator.Done())
            {
                MoteMaker.ThrowMetaPuffs(new TargetInfo(iterator.Current, base.Map, false));
                iterator.MoveNext();
            }
        }
    }
}
