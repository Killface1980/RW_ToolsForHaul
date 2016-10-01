using System.Linq;
using System.Reflection;
using RW_FacialStuff;
using UnityEngine;
using Verse;
using Verse.AI;

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
        static DetourInjector()
        {
            LongEventHandler.QueueLongEvent(Inject, "Initializing", true, null);
        }

        private static void Inject()
        {
            MethodInfo method = typeof(ThinkNode_JobGiver).GetMethod("TryIssueJobPackage", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo method2 = typeof(_ThinkNode_JobGiver).GetMethod("TryIssueJobPackage", BindingFlags.Instance | BindingFlags.Public);
            bool flag = !Detours.TryDetourFromTo(method, method2);
            if (flag)
            {
                Log.Message("Failed detour RightTools");
            }
            GameObject initializer = new GameObject("TFHMapComponentInjector");
            initializer.AddComponent<MapComponentInjector>();
            Object.DontDestroyOnLoad(initializer);
        }
    }
}