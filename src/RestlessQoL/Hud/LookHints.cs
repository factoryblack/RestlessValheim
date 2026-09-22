using System.Collections.Generic;
using HarmonyLib;
using Jotunn.Managers;
using RestlessQoL.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

public sealed class LookHints : FeatureModule
{
    public override string Id => "ui.look";
    public override bool Enabled => true;

    private static RestlessUi.KeyStack? _stack;

    protected override void OnLoaded()
    {
        GUIManager.OnCustomGUIAvailable += TearDown;
    }

    public override void Tick()
    {
        if (ModConfig.LookHintsEnabled.Value)
            return;
        _stack?.Hide();
        RestoreVanilla();
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(global::Hud), "UpdateCrosshair")]
        private static void AfterCrosshair(global::Hud __instance)
        {
            if (!ModConfig.LookHintsEnabled.Value)
            {
                _stack?.Hide();
                RestoreVanilla(__instance);
                return;
            }

            var player = Player.m_localPlayer;
            if (player == null || Hud.IsUserHidden() || RestlessUi.InventoryOpen() || RestlessUi.MapOpen())
            {
                _stack?.Hide();
                return;
            }

            var raw = player.GetHoverText();
            if (string.IsNullOrWhiteSpace(raw))
                raw = Join(ReadHud(__instance, "m_hoverName"), ReadHud(__instance, "m_hoverText"));
            else
            {
                var hover = ReadHud(__instance, "m_hoverText");
                if (!string.IsNullOrWhiteSpace(hover) && raw.IndexOf('[') < 0)
                    raw = raw + "\n" + hover;
            }

            var stone = GuardianStone(player, out var power, out var lore, out var hold, out var binds);
            string title = "";
            string extra = "";
            var rows = new List<(string verb, string face)>();
            var parsed = !stone && !string.IsNullOrWhiteSpace(raw) && Read(raw, out title, out extra, out rows);
            if (!stone && !parsed)
            {
                _stack?.Hide();
                return;
            }

            QuietVanilla(__instance);
            var parent = __instance.m_rootObject != null
                ? __instance.m_rootObject.transform
                : null;
            if (parent == null)
                return;

            if (_stack != null && _stack.Root.transform.Find("plate") == null)
                TearDown();
            _stack ??= RestlessUi.KeyStack.Look(parent);
            if (stone)
                _stack.SetStory(power, lore, hold, binds);
            else
                _stack.Set(title, extra, rows);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.GetHoverText))]
        private static void AfterFerment(Fermenter __instance, ref string __result) =>
            StationTime.Append(__instance.gameObject, ref __result);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Smelter), "OnHoverAddOre")]
        private static void AfterSmeltOre(Smelter __instance, ref string __result) =>
            StationTime.Append(__instance.gameObject, ref __result);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Smelter), "OnHoverAddFuel")]
        private static void AfterSmeltFuel(Smelter __instance, ref string __result) =>
            StationTime.Append(__instance.gameObject, ref __result);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Smelter), "OnHoverEmptyOre")]
        private static void AfterSmeltEmpty(Smelter __instance, ref string __result) =>
            StationTime.Append(__instance.gameObject, ref __result);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.GetHoverText))]
        private static void AfterCook(CookingStation __instance, ref string __result) =>
            StationTime.Append(__instance.gameObject, ref __result);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Beehive), nameof(Beehive.GetHoverText))]
        private static void AfterHive(Beehive __instance, ref string __result) =>
            StationTime.Append(__instance.gameObject, ref __result);
    }

    private static bool GuardianStone(Player player, out string title, out string body, out string extra,
        out List<(string verb, string face)> rows)
    {
        title = "";
        body = "";
        extra = "";
        rows = new List<(string, string)>();
        var hover = player.GetHoverObject();
        var stand = hover != null ? hover.GetComponentInParent<ItemStand>() : null;
        if (stand?.m_guardianPower == null)
            return false;

        var power = stand.m_guardianPower;
        title = Loc(power.m_name);
        body = RestlessUi.Bare(power.m_tooltip);
        if (power.m_ttl > 0f)
            extra = Mathf.RoundToInt(power.m_ttl) + "s";
        if (stand.IsGuardianPowerActive(player))
        {
            var active = Loc("$guardianstone_hook_alreadyactive");
            extra = string.IsNullOrEmpty(extra) ? active : extra + " · " + active;
            return true;
        }

        var activate = Loc("$guardianstone_hook_activate");
        if (string.IsNullOrEmpty(activate) || activate[0] == '$')
            activate = "Activate";
        rows.Add((activate, RestlessUi.Bind("Use", "UseItem", "JoyUse")));
        return title.Length > 0 || body.Length > 0;
    }

    private static string Loc(string token)
    {
        if (string.IsNullOrEmpty(token))
            return "";
        var text = Localization.instance != null ? Localization.instance.Localize(token) : token;
        return RestlessUi.Bare(text).Trim();
    }

    private static string Join(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a))
            return b ?? "";
        if (string.IsNullOrWhiteSpace(b) || a.Contains(b))
            return a;
        return a + "\n" + b;
    }

    private static string ReadHud(global::Hud hud, string field)
    {
        var data = Traverse.Create(hud).Field(field);
        if (!data.FieldExists())
            return "";
        return TextOf(data.GetValue());
    }

    private static string TextOf(object? node) =>
        node switch
        {
            Text text => text.text ?? "",
            TMP_Text tmp => tmp.text ?? "",
            Component c => TextOf(c.GetComponent<Text>()) is { Length: > 0 } a
                ? a
                : TextOf(c.GetComponent<TMP_Text>()),
            GameObject go => TextOf(go.GetComponent<Text>()) is { Length: > 0 } b
                ? b
                : TextOf(go.GetComponent<TMP_Text>()),
            _ => ""
        };

    private static bool Read(string raw, out string title, out string extra,
        out List<(string verb, string face)> rows)
    {
        title = "";
        extra = "";
        rows = new List<(string, string)>();
        var leftovers = new List<string>();
        foreach (var chunk in RestlessUi.Bare(raw).Split('\n'))
        {
            var line = chunk.Trim();
            if (line.Length == 0)
                continue;
            if (TryRow(line, out var verb, out var face))
            {
                rows.Add((verb, face));
                continue;
            }

            var cleaned = Clean(line);
            if (cleaned.Length == 0)
                continue;

            if (title.Length == 0)
                title = cleaned;
            else
                leftovers.Add(cleaned);
        }

        if (leftovers.Count > 0)
            extra = string.Join(" · ", leftovers);
        return title.Length > 0 || rows.Count > 0;
    }

    private static string Clean(string line)
    {
        var loc = Localization.instance != null
            ? Localization.instance.Localize("$piece_container_empty")
            : "";
        if (!string.IsNullOrEmpty(loc) && loc.IndexOf("empty", System.StringComparison.OrdinalIgnoreCase) >= 0)
            line = Cut(line, loc.Trim());
        line = Cut(line, "(EMPTY)");
        line = line.Replace("()", "").Replace("( )", "");
        return line.Trim().Trim('-', '·', ':', '(', ')', ' ').Trim();
    }

    private static string Cut(string text, string token)
    {
        if (string.IsNullOrEmpty(token))
            return text;
        var i = text.IndexOf(token, System.StringComparison.OrdinalIgnoreCase);
        if (i < 0)
            return text;
        return (text.Substring(0, i) + text.Substring(i + token.Length)).Trim();
    }

    private static bool TryRow(string line, out string verb, out string face)
    {
        verb = "";
        face = "";
        if (line[0] == '[')
        {
            var close = line.IndexOf(']');
            if (close > 1)
            {
                face = RestlessUi.KeyFace(line.Substring(1, close - 1));
                verb = line.Substring(close + 1).Trim();
                return !string.IsNullOrEmpty(face);
            }
        }

        var use = Localization.instance != null ? Localization.instance.Localize("$KEY_Use") : "";
        if (!string.IsNullOrEmpty(use) && TokenAt(line, use))
        {
            face = RestlessUi.KeyFace(use);
            verb = line.Substring(use.Length).Trim();
            return !string.IsNullOrEmpty(face) && !string.IsNullOrEmpty(verb);
        }

        return false;
    }

    private static bool TokenAt(string line, string token)
    {
        if (!line.StartsWith(token, System.StringComparison.OrdinalIgnoreCase))
            return false;
        return line.Length == token.Length || !char.IsLetterOrDigit(line[token.Length]);
    }

    private static void RestoreVanilla(global::Hud? hud = null)
    {
        hud ??= global::Hud.instance;
        if (hud == null)
            return;
        var data = Traverse.Create(hud);
        foreach (var name in new[] { "m_hoverName", "m_hoverText" })
        {
            if (!data.Field(name).FieldExists())
                continue;
            Loud(data.Field(name).GetValue());
        }
    }

    private static void Loud(object? node)
    {
        switch (node)
        {
            case Text text:
                text.enabled = true;
                break;
            case TMP_Text tmp:
                tmp.enabled = true;
                break;
            case GameObject go:
                go.SetActive(true);
                foreach (var text in go.GetComponentsInChildren<Text>(true))
                    text.enabled = true;
                foreach (var tmp in go.GetComponentsInChildren<TMP_Text>(true))
                    tmp.enabled = true;
                break;
            case Component c:
                Loud(c.gameObject);
                break;
        }
    }

    private static void QuietVanilla(global::Hud hud)
    {
        var data = Traverse.Create(hud);
        foreach (var name in new[] { "m_hoverName", "m_hoverText" })
        {
            if (!data.Field(name).FieldExists())
                continue;
            Quiet(data.Field(name).GetValue());
        }
    }

    private static void Quiet(object? node)
    {
        switch (node)
        {
            case Text text:
                text.text = "";
                text.enabled = false;
                break;
            case TMP_Text tmp:
                tmp.text = "";
                tmp.enabled = false;
                break;
            case GameObject go:
                foreach (var text in go.GetComponentsInChildren<Text>(true))
                {
                    text.text = "";
                    text.enabled = false;
                }

                foreach (var tmp in go.GetComponentsInChildren<TMP_Text>(true))
                {
                    tmp.text = "";
                    tmp.enabled = false;
                }

                go.SetActive(false);
                break;
            case Component c:
                Quiet(c.gameObject);
                break;
        }
    }

    private static void TearDown()
    {
        if (_stack != null)
            Object.Destroy(_stack.Root);
        _stack = null;
    }
}
