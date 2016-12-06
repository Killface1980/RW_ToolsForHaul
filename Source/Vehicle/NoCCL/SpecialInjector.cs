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
        private static Assembly Assembly => Assembly.GetAssembly(typeof(DetourInjector));

        private static string AssemblyName => Assembly.FullName.Split(',').First();

        public static BindingFlags universalFlags = GenGeneric.BindingFlagsAll;
        static DetourInjector()
        {
            LongEventHandler.QueueLongEvent(Inject, "Initializing", true, null);
        }

        private static void Inject()
        {

            // Pawn_ApparelTracker
            MethodInfo tryDrop3Source = typeof(Pawn_ApparelTracker).GetMethod(
                "TryDrop",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(Apparel), typeof(Apparel).MakeByRefType(), typeof(IntVec3), typeof(bool) },
                null);

            MethodInfo tryDrop3Dest = typeof(_Pawn_ApparelTracker).GetMethod(
                "TryDrop",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[] { typeof(Pawn_ApparelTracker), typeof(Apparel), typeof(Apparel).MakeByRefType(), typeof(IntVec3), typeof(bool) },
                null);

            if (!Detours.TryDetourFromTo(tryDrop3Source, tryDrop3Dest))
                Log.Message("Failed detour Pawn_ApparelTracker TryDrop");
            else
            {
                Log.Message("TFH detoured Pawn_ApparelTracker TryDrop");
            }


            // CCL code for backpack injection on races
            // ToDo Remove for A16
            CompInjectionSet injectionSet = new CompInjectionSet
            {
                targetDefs = new List<string>(),
                compProps = new CompProperties()
            };

            injectionSet.targetDefs.Add("Human");
            injectionSet.targetDefs.Add("Jaffa");
            injectionSet.targetDefs.Add("Orassans");

            injectionSet.compProps.compClass = typeof(CompEquipmentGizmoUser);
            List<ThingDef> thingDefs = DefInjectionQualifier.FilteredThingDefs(injectionSet.qualifier, ref injectionSet.qualifierInt, injectionSet.targetDefs);


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