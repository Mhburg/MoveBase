using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace MoveBase
{
    [StaticConstructorOnStartup]
    public static class RoofGrid_SetRoof_Patch
    {
        static RoofGrid_SetRoof_Patch()
        {
            MethodInfo original = typeof(RoofGrid).GetMethod("SetRoof", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo postfix = typeof(RoofGrid_SetRoof_Patch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public);
            HarmonyUtility.Instance.Patch(original, postfix: new HarmonyMethod(postfix));
        }

        public static void Postfix(IntVec3 c, RoofDef def)
        {
            if (def == null)
                DesignatorMoveBase.SetNoRoofFalse(c);
        }
    }
}
