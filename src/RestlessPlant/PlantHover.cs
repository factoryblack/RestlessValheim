using System;
using HarmonyLib;
using UnityEngine;

namespace RestlessPlant;

internal static class PlantHover
{
    [HarmonyPatch(typeof(Pickable), nameof(Pickable.GetHoverText))]
    private static class PickableHover
    {
        [HarmonyPostfix]
        private static void After(Pickable __instance, ref string __result)
        {
            if (!PlantConfig.On)
                return;

            if (PlantConfig.RegrowHint.Value && __instance.m_picked && __instance.m_respawnTimeMinutes > 0f)
            {
                var left = RemainingMinutes(__instance);
                if (left > 0.05)
                {
                    var name = __instance.GetHoverName();
                    __result = string.IsNullOrEmpty(name)
                        ? $"Regrows in {Format(left)}"
                        : $"{name}\nRegrows in {Format(left)}";
                }
            }

            if (!PlantConfig.Replant.Value || __instance.m_respawnTimeMinutes > 0f || !PlantHarvest.Grown(__instance))
                return;
            var seed = PlantHarvest.Hint();
            if (seed == null)
                return;
            var label = string.IsNullOrEmpty(seed.m_name) ? seed.name : Localization.instance.Localize(seed.m_name);
            if (string.IsNullOrEmpty(__result))
                __result = "Replant " + label;
            else if (__result.IndexOf("Replant", StringComparison.OrdinalIgnoreCase) < 0)
                __result += "\nReplant " + label;
        }
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.GetHoverText))]
    private static class SaplingHover
    {
        [HarmonyPostfix]
        private static void After(Plant __instance, ref string __result)
        {
            if (!PlantConfig.On || !PlantConfig.RegrowHint.Value)
                return;
            var left = (__instance.GetGrowTime() - __instance.TimeSincePlanted()) / 60d;
            if (left <= 0.05)
                return;
            var line = "Grows in " + Format(left);
            if (string.IsNullOrEmpty(__result))
                __result = line;
            else if (__result.IndexOf("Grows in", StringComparison.OrdinalIgnoreCase) < 0)
                __result += "\n" + line;
        }
    }

    private static double RemainingMinutes(Pickable pick)
    {
        var pickedTime = PickedTime(pick);
        if (pickedTime <= 1)
            return pick.m_respawnTimeMinutes;

        if (ZNet.instance == null)
            return pick.m_respawnTimeMinutes;

        var elapsed = (ZNet.instance.GetTime() - new DateTime(pickedTime)).TotalMinutes;
        return pick.m_respawnTimeMinutes - elapsed;
    }

    private static long PickedTime(Pickable pick)
    {
        if (pick.m_nview != null && pick.m_nview.IsValid())
            return pick.m_nview.GetZDO().GetLong(ZDOVars.s_pickedTime, 0L);
        return pick.m_pickedTime;
    }

    private static string Format(double minutes)
    {
        if (minutes < 1d)
            return "soon";

        if (minutes < 60d)
            return $"{Mathf.CeilToInt((float)minutes)} min";

        var hours = (int)(minutes / 60d);
        var mins = Mathf.CeilToInt((float)(minutes % 60d));
        if (mins >= 60)
        {
            hours++;
            mins = 0;
        }

        return mins > 0 ? $"{hours}h {mins}m" : $"{hours}h";
    }
}
