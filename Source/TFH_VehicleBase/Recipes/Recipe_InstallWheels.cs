﻿namespace TFH_VehicleBase.Recipes
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using RimWorld;

    using Verse;

    public class Recipe_InstallWheels : Recipe_VehicleSurgery
    {
        [DebuggerHidden]
        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            for (int i = 0; i < recipe.appliedOnFixedBodyParts.Count; i++)
            {
                BodyPartDef recipePart = recipe.appliedOnFixedBodyParts[i];
                List<BodyPartRecord> bpList = pawn.RaceProps.body.AllParts;
                for (int j = 0; j < bpList.Count; j++)
                {
                    BodyPartRecord record = bpList[j];
                    if (record.def == recipePart)
                    {
                        if (pawn.health.hediffSet.hediffs.Any((Hediff x) => x.Part == record))
                        {
                            if (record.parent == null || pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined).Contains(record.parent))
                            {
                                if (!pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(record) || pawn.health.hediffSet.HasDirectlyAddedPartFor(record))
                                {
                                    yield return record;
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients)
        {
            if (billDoer != null)
            {
                if (this.CheckSurgeryFail(billDoer, pawn, ingredients, part))
                {
                    return;
                }

                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, new object[]
                                                                  {
                                                                      billDoer,
                                                                      pawn
                                                                  });
                for (int i = 0; i < part; i++)
                {
                    
                }

                VehicleRecipesUtility.RestorePartAndSpawnAllPreviousParts(pawn, part, billDoer.Position, billDoer.Map);
            }
        }
    }
}
