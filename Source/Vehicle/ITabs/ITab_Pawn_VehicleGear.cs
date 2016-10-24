using System.Collections.Generic;
using RimWorld;
using ToolsForHaul.Components;
using UnityEngine;
using Verse;

// Always needed
//using VerseBase;         // Material/Graphics handling functions are found here
// RimWorld universal objects are here (like 'Building')
// Needed when you do something with the AI
//using Verse.Sound;       // Needed when you do something with Sound
//using Verse.Noise;       // Needed when you do something with Noises

// RimWorld specific functions are found here (like 'Building_Battery')
//using RimWorld.Planet;   // RimWorld specific functions for world creation
//using RimWorld.SquadAI;  // RimWorld specific functions for squad brains 

namespace ToolsForHaul.ITabs
{
    class Itab_Pawn_VehicleGear : ITab_Pawn_Gear
    {
        private const string txtTabVehicleGear = "TabVehicleGear";
        private const string txtDriver = "Driver";
        private const string txtStorage = "Storage";
        private const string txtDropIt = "DropIt";

        public Itab_Pawn_VehicleGear()
        {
            labelKey = txtTabVehicleGear;
            size = new Vector2(300f, 450f);
        }
        public override bool IsVisible
        {
            get { return true; }
        }
        protected override void FillTab()
        {
            float fieldHeight = 30.0f;


            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.PrisonerTab, KnowledgeAmount.FrameDisplayed);
            Text.Font = GameFont.Small;
            Rect innerRect1 = new Rect(0.0f, 0.0f, size.x, size.y).ContractedBy(10f);
            GUI.BeginGroup(innerRect1);
            Rect mountedRect = new Rect(0.0f, 30.0f, innerRect1.width, fieldHeight);
            float mountedRectY = mountedRect.y;
            Widgets.ListSeparator(ref mountedRectY, innerRect1.width, txtDriver.Translate());
            mountedRect.y += fieldHeight;
            Rect thingIconRect = new Rect(mountedRect.x, mountedRect.y, 30f, fieldHeight);
            Rect thingLabelRect = new Rect(mountedRect.x + 35f, mountedRect.y + 5.0f, innerRect1.width - 35f, fieldHeight);
            Rect thingButtonRect = new Rect(mountedRect.x, mountedRect.y, innerRect1.width, fieldHeight);

            var compMountable = SelThing.TryGetComp<CompMountable>();

            if (compMountable.IsMounted)
            {
                Pawn driver = compMountable.Driver;
                Widgets.ThingIcon(thingIconRect, driver);
                Widgets.Label(thingLabelRect, driver.Label);
                if (Event.current.button == 1 && Widgets.ButtonInvisible(thingButtonRect))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    FloatMenuOption dismount = new FloatMenuOption("Dismount".Translate(driver.LabelShort), () =>
                    {
                        compMountable.Dismount();
                    });
                    options.Add(dismount);

                    Find.WindowStack.Add(new FloatMenu(options));
                }
                thingIconRect.y += fieldHeight;
                thingLabelRect.y += fieldHeight;
            }
            Rect storageRect = new Rect(0.0f, thingIconRect.y, innerRect1.width, fieldHeight);
            float storageRectY = storageRect.y;
            Widgets.ListSeparator(ref storageRectY, innerRect1.width, txtStorage.Translate());
            storageRect.y += fieldHeight;
            thingIconRect.y = storageRect.y;
            thingLabelRect.y = storageRect.y;
            thingButtonRect.y = storageRect.y;

            #region Cart
            var cart = SelThing as Vehicle_Cart;
            if (cart != null)
            {
                foreach (Thing thing in cart.storage)
                {
                    if (thing.ThingID.IndexOf("Human_Corpse") > -1)
                        Widgets.DrawTextureFitted(thingIconRect, ContentFinder<Texture2D>.Get("Things/Pawn/IconHuman_Corpse"), 1.0f);
                    else if (thing.ThingID.IndexOf("Corpse") > -1)
                    {
                        Corpse corpse = thing as Corpse;
                        Widgets.ThingIcon(thingIconRect, corpse.innerPawn.def);
                    }
                    else
                        Widgets.ThingIcon(thingIconRect, thing);
                    Widgets.Label(thingLabelRect, thing.LabelCap);
                    if (Event.current.button == 1 && Widgets.ButtonInvisible(thingButtonRect))
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();
                        options.Add(new FloatMenuOption("ThingInfo".Translate(), () =>
                        {
                            Find.WindowStack.Add(new Dialog_InfoCard(thing));
                        }));
                        options.Add(new FloatMenuOption("DropThing".Translate(), () =>
                        {
                            Thing dummy;
                            cart.storage.TryDrop(thing, SelThing.Position, ThingPlaceMode.Near, out dummy);
                        }));

                        Find.WindowStack.Add(new FloatMenu(options, thing.LabelCap));
                    }
                    if (Mouse.IsOver(thingButtonRect))
                    {
                        GUI.DrawTexture(thingButtonRect, TexUI.HighlightTex);
                    }
                    TooltipHandler.TipRegion(thingIconRect, thing.def.LabelCap);
                    thingIconRect.y += fieldHeight;
                    thingLabelRect.y += fieldHeight;
                }
                if (Widgets.ButtonText(new Rect(180f, 400f, 100f, 30f), "Drop All"))
                    cart.storage.TryDropAll(SelThing.Position, ThingPlaceMode.Near);
            }
            #endregion

            #region CartTurret
            var cartTurret = SelThing as Vehicle_Turret;
            if (cartTurret != null)
            {
                foreach (Thing thing in cartTurret.storage)
                {
                    if (thing.ThingID.IndexOf("Human_Corpse") > -1)
                        Widgets.DrawTextureFitted(thingIconRect, ContentFinder<Texture2D>.Get("Things/Pawn/IconHuman_Corpse"), 1.0f);
                    else if (thing.ThingID.IndexOf("Corpse") > -1)
                    {
                        Corpse corpse = thing as Corpse;
                        Widgets.ThingIcon(thingIconRect, corpse.innerPawn.def);
                    }
                    else
                        Widgets.ThingIcon(thingIconRect, thing);
                    Widgets.Label(thingLabelRect, thing.LabelCap);
                    if (Event.current.button == 1 && Widgets.ButtonInvisible(thingButtonRect))
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();
                        options.Add(new FloatMenuOption("ThingInfo".Translate(), () =>
                        {
                            Find.WindowStack.Add(new Dialog_InfoCard(thing));
                        }));
                        options.Add(new FloatMenuOption("DropThing".Translate(), () =>
                        {
                            Thing dummy;
                            cartTurret.storage.TryDrop(thing, SelThing.Position, ThingPlaceMode.Near, out dummy);
                        }));

                        Find.WindowStack.Add(new FloatMenu(options, thing.LabelCap));
                    }
                    if (Mouse.IsOver(thingButtonRect))
                    {
                        GUI.DrawTexture(thingButtonRect, TexUI.HighlightTex);
                    }
                    TooltipHandler.TipRegion(thingIconRect, thing.def.LabelCap);
                    thingIconRect.y += fieldHeight;
                    thingLabelRect.y += fieldHeight;
                }
                if (Widgets.ButtonText(new Rect(180f, 400f, 100f, 30f), "Drop All"))
                    cartTurret.storage.TryDropAll(SelThing.Position, ThingPlaceMode.Near);
            }
            #endregion


            GUI.EndGroup();
        }
    }
}