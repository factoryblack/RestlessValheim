using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RestlessQoL.Core;
using UnityEngine;

namespace RestlessQoL.World;

public sealed class DigDeeper : FeatureModule
{
    public override string Id => "world.digdeeper";
    public override bool Enabled => true;

    [HarmonyPatch]
    private static class Patches
    {
        // Vanilla AtMaxWorldLevelDepth treats (const - 0.05) as "at cap".
        private const float CapEpsilon = 0.05f;

        private static float VanillaCap() => Heightmap.c_LevelMaxDelta;

        private static float MaxDelta() =>
            ModConfig.DigDeeperEnabled.Value ? ModConfig.DigMaxDelta.Value : VanillaCap();

        private static float MinDelta() => -MaxDelta();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.AtMaxWorldLevelDepth))]
        private static bool AtMaxWorldLevelDepth(Heightmap __instance, Vector3 worldPos, ref bool __result)
        {
            if (!ModConfig.DigDeeperEnabled.Value)
                return true;
            __instance.GetWorldHeight(worldPos, out var current);
            __instance.GetWorldBaseHeight(worldPos, out var baseline);
            __result = Mathf.Max(baseline - current, 0f) >= MaxDelta() - CapEpsilon;
            return false;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.RaiseTerrain))]
        private static IEnumerable<CodeInstruction> RaiseTerrain(IEnumerable<CodeInstruction> instructions) =>
            ReplaceLevelClamp(instructions, nameof(TerrainComp.RaiseTerrain));

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.LevelTerrain))]
        private static IEnumerable<CodeInstruction> LevelTerrain(IEnumerable<CodeInstruction> instructions) =>
            ReplaceLevelClamp(instructions, nameof(TerrainComp.LevelTerrain));

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.ApplyToHeightmap))]
        private static IEnumerable<CodeInstruction> ApplyToHeightmap(IEnumerable<CodeInstruction> instructions) =>
            ReplaceLevelClamp(instructions, nameof(TerrainComp.ApplyToHeightmap));

        private static IEnumerable<CodeInstruction> ReplaceLevelClamp(IEnumerable<CodeInstruction> instructions, string method)
        {
            var vanilla = VanillaCap();
            var replaced = 0;
            foreach (var ins in instructions)
            {
                if (ins.opcode == OpCodes.Ldc_R4 && ins.operand is float value)
                {
                    if (Math.Abs(value - vanilla) < 0.001f)
                    {
                        replaced++;
                        yield return Retarget(ins, nameof(MaxDelta));
                        continue;
                    }

                    if (Math.Abs(value + vanilla) < 0.001f)
                    {
                        replaced++;
                        yield return Retarget(ins, nameof(MinDelta));
                        continue;
                    }
                }

                yield return ins;
            }

            if (replaced == 0)
                Plugin.Log.LogWarning($"world.digdeeper: {method} has no ±{vanilla} clamps — 1.0 moved the cap, this feature is a no-op there.");
        }

        private static CodeInstruction Retarget(CodeInstruction from, string method)
        {
            var next = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patches), method));
            next.labels.AddRange(from.labels);
            next.blocks.AddRange(from.blocks);
            return next;
        }
    }
}
