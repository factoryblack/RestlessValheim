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

    internal static double RemainingMinutes(Pickable pick)
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

    [HarmonyPatch(typeof(Pickable), nameof(Pickable.Awake))]
    private static class SproutWatch
    {
        [HarmonyPostfix]
        private static void After(Pickable __instance)
        {
            if (!PlantConfig.On || !Regrows(__instance))
                return;
            var sprout = __instance.GetComponent<PickSprout>() ?? __instance.gameObject.AddComponent<PickSprout>();
            sprout.Bind(__instance);
        }
    }

    [HarmonyPatch(typeof(Pickable), nameof(Pickable.SetPicked))]
    private static class SproutPick
    {
        [HarmonyPostfix]
        private static void After(Pickable __instance)
        {
            __instance.GetComponent<PickSprout>()?.Apply();
        }
    }

    [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
    private static class SproutHandsOff
    {
        // The mesh stays up while it regrows, so the use key would otherwise
        // play the pick animation on a sprout that has nothing to give.
        [HarmonyPrefix]
        private static bool LeaveIt(Pickable __instance, ref bool __result)
        {
            if (!__instance.m_picked || __instance.GetComponent<PickSprout>() == null)
                return true;
            __result = false;
            return false;
        }
    }

    private static bool Regrows(Pickable pick)
    {
        if (pick.m_respawnTimeMinutes <= 0f || pick.m_hideWhenPicked == null)
            return false;
        var name = Utils.GetPrefabName(pick.gameObject);
        return name.StartsWith("Restless_");
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

// Picked forage used to switch its mesh off, so a mushroom patch looked empty.
// A mushroom is one mesh, so that mesh grows back from a sprout. A bush hides
// only its berry cluster, and that cluster is what changes size.
internal sealed class PickSprout : MonoBehaviour
{
    private const float Tiny = 0.2f;

    private Pickable? _pick;
    private Transform? _visual;
    private Transform? _bush;
    private Vector3 _full = Vector3.one;
    private Vector3 _bushFull = Vector3.one;
    private float _next;

    public void Bind(Pickable pick)
    {
        _pick = pick;
        var hidden = pick.m_hideWhenPicked.transform;
        var fruit = FruitCluster(hidden);
        if (fruit != null)
        {
            _bush = hidden;
            _bushFull = hidden.localScale;
            _visual = fruit;
        }
        else
        {
            _bush = null;
            _visual = hidden;
        }

        _full = _visual.localScale;
        Apply();
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime < _next)
            return;
        _next = Time.unscaledTime + 0.25f;
        Apply();
    }

    public void Apply()
    {
        if (_pick == null || _visual == null || _pick.m_respawnTimeMinutes <= 0f || _pick.m_enabled == 0)
            return;

        if (_bush != null)
        {
            if (!_bush.gameObject.activeSelf)
                _bush.gameObject.SetActive(true);
            _bush.localScale = _bushFull;
        }

        if (!_pick.m_picked)
        {
            _visual.localScale = _full;
            return;
        }

        if (!_visual.gameObject.activeSelf)
            _visual.gameObject.SetActive(true);

        var left = PlantHover.RemainingMinutes(_pick);
        var grown = Mathf.Clamp01(1f - (float)(left / _pick.m_respawnTimeMinutes));
        _visual.localScale = _full * Mathf.Lerp(Tiny, 1f, grown);
    }

    // Raspberry, blueberry, and cloudberry call the cluster Berrys. Lingonberry
    // calls it Berries. The bush mesh is a sibling, so it is left alone.
    private static Transform? FruitCluster(Transform hidden)
    {
        foreach (var child in hidden.GetComponentsInChildren<Transform>(true))
        {
            if (child == hidden)
                continue;
            if (IsFruit(child.name))
                return child;
        }

        return null;
    }

    private static bool IsFruit(string name) =>
        name.Equals("Berrys", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Berries", StringComparison.OrdinalIgnoreCase);
}
