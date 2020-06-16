using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MoveBase
{
    /// <summary>
    /// Fix door tick exception when a door is minified.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class MinifyUtility_MakeMinified
    {
        private static MethodInfo _bucketOf = typeof(TickList).GetMethod("BucketOf", BindingFlags.NonPublic | BindingFlags.Instance);

        private static MethodInfo _tickListFor = typeof(TickManager).GetMethod("TickListFor", BindingFlags.NonPublic | BindingFlags.Instance);

        static MinifyUtility_MakeMinified()
        {
            MethodInfo original = typeof(MinifyUtility).GetMethod(nameof(MinifyUtility.MakeMinified), BindingFlags.Public | BindingFlags.Static);
            MethodInfo prefix = typeof(MinifyUtility_MakeMinified).GetMethod("Prefix", BindingFlags.Public | BindingFlags.Static);
            MethodInfo postfix = typeof(MinifyUtility_MakeMinified).GetMethod("Postfix", BindingFlags.Public | BindingFlags.Static);
            HarmonyUtility.Instance.Patch(original, new HarmonyMethod(prefix), new HarmonyMethod(postfix));
        }

        public static bool Prefix(out bool __state, Thing thing)
        {
            __state = thing?.MapHeld?.designationManager?.DesignationOn(thing, MoveBaseDefOf.MoveBase) != null;
            return true;
        }

        public static void Postfix(bool __state, Thing thing)
        {
            if (__state)
            {
                TickList tickList = _tickListFor.Invoke(Find.TickManager, new[] { thing }) as TickList;
                if (tickList != null)
                    (_bucketOf.Invoke(tickList, new[] { thing }) as List<Thing>).Remove(thing);
            }
        }
    }
}
