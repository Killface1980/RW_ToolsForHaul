using System;
using System.Linq;
using System.Reflection;
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

            GameObject initializer = new GameObject("TFHMapComponentInjector");
            initializer.AddComponent<MapComponentInjector>();
            Object.DontDestroyOnLoad(initializer);
        }
    }
}