namespace TFH_VehicleBase.Recipes
{
    using System.Collections.Generic;

    using RimWorld;

    using Verse;

    public class Recipe_VehicleSurgery : RecipeWorker
    {
        private const float CatastrophicFailChance = 0.5f;

        private const float RidiculousFailChanceFromCatastrophic = 0.1f;

        protected bool CheckSurgeryFail(Pawn surgeon, Pawn patient, List<Thing> ingredients, BodyPartRecord part, Bill bill)
        {
            return false;
        }

        private void TryGainBotchedSurgeryThought(Pawn patient, Pawn surgeon)
        {
            if (!patient.RaceProps.Humanlike)
            {
                return;
            }

            patient.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.BotchedMySurgery, surgeon);
        }

        private float GetAverageMedicalPotency(List<Thing> ingredients)
        {
            if (ingredients.NullOrEmpty<Thing>())
            {
                return 1f;
            }

            int num = 0;
            float num2 = 0f;
            for (int i = 0; i < ingredients.Count; i++)
            {
                Medicine medicine = ingredients[i] as Medicine;
                if (medicine != null)
                {
                    num += medicine.stackCount;
                    num2 += medicine.GetStatValue(StatDefOf.MedicalPotency, true) * (float)medicine.stackCount;
                }
            }

            if (num == 0)
            {
                return 1f;
            }

            return num2 / (float)num;
        }
    }
}
