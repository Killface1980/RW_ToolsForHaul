using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommunityCoreLibrary;
using RW_FacialStuff;
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

        public static object GetHiddenValue(Type type, object instance, string fieldName, FieldInfo info)
        {
            if (info == null)
            {
                info = type.GetField(fieldName, universalFlags);
            }

            return info?.GetValue(instance);
        }

        public static void SetHiddenValue(object value, Type type, object instance, string fieldName, FieldInfo info)
        {
            if (info == null)
            {
                info = type.GetField(fieldName, universalFlags);
            }

            info?.SetValue(instance, value);
        }

        private static void Inject()
        {
            MethodInfo method = typeof(ThinkNode_JobGiver).GetMethod("TryIssueJobPackage", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo method2 = typeof(_ThinkNode_JobGiver).GetMethod("TryIssueJobPackage", BindingFlags.Instance | BindingFlags.Public);

          //// draws hands on equipment, if corresponding Comp is specified
          //MethodInfo method3 = typeof(PawnRenderer).GetMethod("DrawEquipment", BindingFlags.Instance | BindingFlags.NonPublic);
          //MethodInfo method4 = typeof(RA_PawnRenderer).GetMethod("DrawEquipment", BindingFlags.Instance | BindingFlags.NonPublic);


            if (!Detours.TryDetourFromTo(method, method2))
            {
                Log.Message("Failed detour RightTools JobPackage");
            }
            //if (!Detours.TryDetourFromTo(method3, method4))
            //{
            //    Log.Message("Failed detour RightTools PawnRenderer DrawEquipment");
            //}


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