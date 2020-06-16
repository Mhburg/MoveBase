using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace MoveBase
{
    public class PerfProfile : Attribute
    {
        private static Dictionary<MethodBase, Stopwatch> _watches = new Dictionary<MethodBase, Stopwatch>();

        private static Dictionary<MethodBase, StatModel> _totalTime = new Dictionary<MethodBase, StatModel>();

        private static MethodInfo _prefix = typeof(PerfProfile).GetMethod(nameof(PerfProfile.Prefix), BindingFlags.Static | BindingFlags.NonPublic);

        private static MethodInfo _postfix = typeof(PerfProfile).GetMethod(nameof(PerfProfile.Postfix), BindingFlags.Static | BindingFlags.NonPublic);

        private static bool _initialized = false;

        static PerfProfile()
        {
#if DEBUG
            if (!_initialized)
                Init();
#endif
        }

        public static void Init()
        {
            foreach (Type type in Assembly.GetExecutingAssembly().DefinedTypes)
            {
                if (type.HasAttribute<PerfProfile>())
                {
                    foreach (MethodInfo methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                    {
                        HarmonyUtility.Instance.Patch(methodInfo, new HarmonyMethod(_prefix), new HarmonyMethod(_postfix));
                    }
                }
            }

            _initialized = true;
        }

        public static void OutputLog()
        {
#if DEBUG
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var pair in _totalTime.OrderByDescending(pair => pair.Value.TotalTime))
                stringBuilder.AppendLine($"{pair.Key}: {pair.Value}");

            Log.Warning(stringBuilder.ToString(), true);
#endif
        }

        private static bool Prefix(MethodBase __originalMethod)
        {
            if (!_watches.TryGetValue(__originalMethod, out Stopwatch stopwatch))
                stopwatch = _watches[__originalMethod] = new Stopwatch();

            stopwatch.Restart();
            return true;
        }

        private static void Postfix(MethodBase __originalMethod)
        {
            Stopwatch stopwatch = _watches[__originalMethod];
            stopwatch.Stop();

            if (_totalTime.TryGetValue(__originalMethod, out StatModel model))
                model.Update(stopwatch.Elapsed.TotalMilliseconds);
            else
                (_totalTime[__originalMethod] = new StatModel()).Update(stopwatch.Elapsed.TotalMilliseconds);
        }

        private class StatModel
        {
            public double TotalTime = 0;
            public double InvokedTimes = 0;
            public double LongestExecutionTime = 0;

            public void Update(double duration)
            {
                this.TotalTime += duration;
                this.InvokedTimes++;
                this.LongestExecutionTime = this.LongestExecutionTime > duration ? this.LongestExecutionTime : duration;
            }

            public override string ToString()
            {
                return $"{nameof(TotalTime)}: {TotalTime: 0000.0000}ms - {nameof(InvokedTimes)}: {InvokedTimes} - {nameof(LongestExecutionTime)}: {LongestExecutionTime: 0000.0000}ms";
            }
        }
    }
}
