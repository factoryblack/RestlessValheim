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
            if (!PlantConfig.On || !PlantConfig.RegrowHint.Value || !__instance.m_picked)
                return;
            if (__instance.m_respawnTimeMinutes <= 0f)
                return;

            var left = RemainingMinutes(__instance);
            if (left <= 0.05)
                return;

            var name = __instance.GetHoverName();
            __result = string.IsNullOrEmpty(name)
                ? $"Regrows in {Format(left)}"
                : $"{name}\nRegrows in {Format(left)}";
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
