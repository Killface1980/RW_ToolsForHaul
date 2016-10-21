using System.Linq;
using RimWorld;
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
    class Itab_Pawn_Vehicle_Storage : ITab_Storage
    {
        private const float TopAreaHeight = 35f;
        private Vector2 scrollPosition;
        private static readonly Vector2 WinSize;

        static Itab_Pawn_Vehicle_Storage()
        {
            WinSize = new Vector2(300f, 480f);
        }

		public override bool IsVisible
		{
			get 
            {
                Vehicle_Cart cart = Find.Selector.SelectedObjects.First() as Vehicle_Cart;
                return cart != null;
            }
		}

        protected override void FillTab()
        {
            ThingFilter allowances;
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.StorageTab, KnowledgeAmount.FrameDisplayed);
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.StorageTab, OpportunityType.Critical);
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.Stockpiles, OpportunityType.Critical);
            allowances = ((Vehicle_Cart)Find.Selector.SelectedObjects.First()).allowances;
            Rect position = new Rect(0.0f, 0.0f, WinSize.x, WinSize.y).ContractedBy(10f);
            GUI.BeginGroup(position);

            ThingFilterUI.DoThingFilterConfigWindow(new Rect(0.0f, 35f, position.width, position.height - 35f), ref scrollPosition, allowances);
            GUI.EndGroup();
        }
    }
}