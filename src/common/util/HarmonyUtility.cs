﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace MoveBase
{
    public static class HarmonyUtility
    {
        public static Harmony Instance = new Harmony("NotooShabby.MoveBase");

        public static bool Compare(this CodeInstruction codeInstruction, CodeInstruction pattern)
        {
            if (codeInstruction.opcode != pattern.opcode
                || (pattern.opcode == OpCodes.Ldloc_S
                    && (int)pattern.operand != (codeInstruction.operand as LocalVariableInfo)?.LocalIndex)
                || (pattern.opcode == OpCodes.Callvirt
                    && pattern.operand != null
                    && pattern.operand != codeInstruction.operand))
                return false;

            return true;
        }

        public static bool MatchPattern(List<CodeInstruction> instructions, List<CodeInstruction> pattern, int index, Action action)
        {
            for (int i = index, j = 0; j < pattern.Count; i++, j++)
            {
                if (!instructions[i].Compare(pattern[j]))
                {
                    return false;
                }
            }

            action?.Invoke();
            return true;
        }

        public static List<CodeInstruction> ReturnPatternMatchedInstruction(List<CodeInstruction> pattern, List<CodeInstruction> original, ref int index)
        {
            List<CodeInstruction> instructions = new List<CodeInstruction>();
            for (int j = 1; j < pattern.Count; j++)
            {
                instructions.Add(original[index + j]);
            }

            index += (pattern.Count - 1);

            return instructions;
        }
    }
}
