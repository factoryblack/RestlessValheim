using RestlessQoL.Core;
using UnityEngine;

namespace RestlessQoL.HudTweaks;

// Remaining time on workshop stations. LookHints already paints the extra line.
internal static class StationTime
{
    public static string? Describe(GameObject? go)
    {
        if (go == null || !ModConfig.LookStationTime.Value)
            return null;
        var ferment = go.GetComponentInParent<Fermenter>();
        if (ferment != null)
            return Ferment(ferment);
        var cook = go.GetComponentInParent<CookingStation>();
        if (cook != null)
            return Cook(cook);
        var smelt = go.GetComponentInParent<Smelter>();
        if (smelt != null)
            return Melt(smelt);
        var hive = go.GetComponentInParent<Beehive>();
        return hive != null ? Honey(hive) : null;
    }

    private static string? Ferment(Fermenter station)
    {
        var elapsed = station.GetFermentationTime();
        if (elapsed <= 0d)
            return null;
        var left = station.m_fermentationDuration - (float)elapsed;
        return left <= 0f ? null : "Ready in " + Format(left);
    }

    private static string? Melt(Smelter station)
    {
        var queue = station.GetQueueSize();
        if (queue <= 0)
            return null;
        var left = queue * station.m_secPerProduct - station.GetBakeTimer();
        return left <= 0.5f ? null : "Ready in " + Format(left);
    }

    private static string? Cook(CookingStation station)
    {
        var soonest = float.MaxValue;
        var n = 0;
        var slots = station.m_slots;
        var count = slots != null ? slots.Length : 0;
        for (var i = 0; i < count; i++)
        {
            station.GetSlot(i, out var name, out var cooked, out var status, out _);
            if (string.IsNullOrEmpty(name) || status != CookingStation.Status.NotDone)
                continue;
            var conv = station.GetItemConversion(name);
            if (conv == null)
                continue;
            n++;
            var left = conv.m_cookTime - cooked;
            if (left > 0f && left < soonest)
                soonest = left;
        }

        if (n == 0 || soonest >= float.MaxValue)
            return null;
        return "Ready in " + Format(soonest);
    }

    private static string? Honey(Beehive hive)
    {
        if (hive.GetHoneyLevel() >= hive.m_maxHoney)
            return null;
        var left = hive.m_secPerUnit - hive.GetTimeSinceLastUpdate();
        if (left <= 0.5f || left > hive.m_secPerUnit + 1f)
            return null;
        return "Honey in " + Format(left);
    }

    private static string Format(float seconds)
    {
        if (seconds < 60f)
            return "soon";
        var minutes = seconds / 60f;
        if (minutes < 60f)
            return Mathf.CeilToInt(minutes) + " min";
        var hours = (int)(minutes / 60f);
        var mins = Mathf.CeilToInt(minutes % 60f);
        if (mins >= 60)
        {
            hours++;
            mins = 0;
        }

        return mins > 0 ? hours + "h " + mins + "m" : hours + "h";
    }

    public static void Append(GameObject go, ref string text)
    {
        var line = Describe(go);
        if (string.IsNullOrEmpty(line))
            return;
        if (!string.IsNullOrEmpty(text) && text.IndexOf(line, System.StringComparison.Ordinal) >= 0)
            return;
        text = string.IsNullOrEmpty(text) ? line! : text + "\n" + line;
    }
}
