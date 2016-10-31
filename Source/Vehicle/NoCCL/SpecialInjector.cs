using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommunityCoreLibrary;
using RimWorld;
using ToolsForHaul.Components;
using ToolsForHaul.Detoured;
using UnityEngine;
using Verse;
using Verse.AI;
using Object = UnityEngine.Object;

namespace ToolsForHaul
{
    public class SpecialInjector
    {

        public virtual bool Inject()
        {
            Log.Error("This should never be called.");
            return false;
        }
    }

    [StaticConstructorOnStartup]
    internal static class DetourInjector
    {
        private static Assembly Assembly { get { return Assembly.GetAssembly(typeof(DetourInjector)); } }
        private static string AssemblyName { get { return Assembly.FullName.Split(',').First(); } }
        public static BindingFlags universalFlags = GenGeneric.BindingFlagsAll;
        static DetourInjector()
        {
            LongEventHandler.QueueLongEvent(Inject, "Initializing", true, null);
        }

        private static void Inject()
        {
            MethodInfo method = typeof(ThinkNode_JobGiver).GetMethod("TryIssueJobPackage", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo method2 = typeof(_ThinkNode_JobGiver).GetMethod("TryIssueJobPackage", BindingFlags.Instance | BindingFlags.Public);

            if (!Detours.TryDetourFromTo(method, method2))
            {
                Log.Message("Failed detour RightTools JobPackage");
            }
            else
            {
                Log.Message("TFH detoured RightTools JobPackage");

            }
            // Pawn_ApparelTracker

            MethodInfo tryDrop3Source = typeof(Pawn_ApparelTracker).GetMethod("TryDrop",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new Type[] { typeof(Apparel), typeof(Apparel).MakeByRefType(), typeof(IntVec3), typeof(bool) },
                null);

            MethodInfo tryDrop3Dest = typeof(_Pawn_ApparelTracker).GetMethod("TryDrop",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(Pawn_ApparelTracker), typeof(Apparel), typeof(Apparel).MakeByRefType(), typeof(IntVec3), typeof(bool) },
                null);

            if (!Detours.TryDetourFromTo(tryDrop3Source, tryDrop3Dest))
                Log.Message("Failed detour Pawn_ApparelTracker TryDrop");
            else
            {
                Log.Message("TFH detoured Pawn_ApparelTracker TryDrop");

            }


            // CCL code for backpack injection on races

            CompInjectionSet injectionSet = new CompInjectionSet
            {
                targetDefs = new List<string>(),
                compProps = new CompProperties()
            };

            injectionSet.targetDefs.Add("Human");
            injectionSet.targetDefs.Add("Jaffa");
            injectionSet.targetDefs.Add("Orassans");

            injectionSet.compProps.compClass = typeof(CompEquipmentGizmoUser);
            List<ThingDef> thingDefs = DefInjectionQualifierTemp.FilteredThingDefs(injectionSet.qualifier, ref injectionSet.QualifierTempInt, injectionSet.targetDefs);


            if (!thingDefs.NullOrEmpty())
            {
                foreach (ThingDef thingDef in thingDefs)
                {
                    // TODO:  Make a full copy using the comp in this def as a template
                    // Currently adds the comp in this def so all target use the same def
                    if (!thingDef.HasComp(injectionSet.compProps.compClass))
                    {
                        thingDef.comps.Add(injectionSet.compProps);
                    }
                }
            }



            GameObject initializer = new GameObject("TFHMapComponentInjector");
            initializer.AddComponent<MapComponentInjector>();
            Object.DontDestroyOnLoad(initializer);
        }
    }
}