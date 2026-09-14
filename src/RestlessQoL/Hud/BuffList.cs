using System.Collections.Generic;
using HarmonyLib;
using RestlessQoL.Core;
using UnityEngine;

namespace RestlessQoL.HudTweaks;

public sealed class BuffList : FeatureModule
{
    public override string Id => "ui.buffs";
    public override bool Enabled => true;

    private static readonly List<StatusEffect> Effects = new();
    private static readonly Dictionary<string, Row> Rows = new();
    private static readonly Dictionary<string, float> FoodSpan = new();
    private static GameObject? _root;
    private static bool _vanillaHidden;

    protected override void OnLoaded()
    {
        Jotunn.Managers.GUIManager.OnCustomGUIAvailable += TearDown;
    }

    public override void Tick()
    {
        if (ModConfig.BuffListEnabled.Value)
            return;
        if (_root != null)
            _root.SetActive(false);
        RestoreVanilla();
    }

    private static void HideVanilla(global::Hud hud)
    {
        if (hud.m_statusEffectListRoot == null)
            return;
        hud.m_statusEffectListRoot.gameObject.SetActive(false);
        if (hud.m_gpRoot != null)
            hud.m_gpRoot.gameObject.SetActive(false);
        _vanillaHidden = true;
    }

    private static void RestoreVanilla()
    {
        if (!_vanillaHidden || global::Hud.instance?.m_statusEffectListRoot == null)
            return;
        global::Hud.instance.m_statusEffectListRoot.gameObject.SetActive(true);
        if (global::Hud.instance.m_gpRoot != null)
            global::Hud.instance.m_gpRoot.gameObject.SetActive(true);
        _vanillaHidden = false;
    }

    private static RectTransform? MapImage() =>
        Minimap.instance?.m_mapImageSmall != null ? Minimap.instance.m_mapImageSmall.rectTransform : null;

    private static Transform HudRoot(global::Hud hud) =>
        hud.m_rootObject != null ? hud.m_rootObject.transform : hud.transform;

    private static void TearDown()
    {
        foreach (var pair in Rows)
            UnityEngine.Object.Destroy(pair.Value.Root);
        Rows.Clear();
        FoodSpan.Clear();
        if (_root != null)
            UnityEngine.Object.Destroy(_root);
        _root = null;
    }

    private static bool IsCurrentLayout() =>
        _root != null && (_root.transform.childCount == 0 || _root.transform.GetChild(0).Find("strip") != null);

    private static void EnsureRoot(global::Hud hud)
    {
        if (_root != null && !IsCurrentLayout())
            TearDown();

        if (_root != null)
            return;

        _root = RestlessUi.Node(HudRoot(hud), "RestlessBuffs");
        var rt = _root.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(RestlessUi.HudRow.BuffWidth, 0f);
    }

    private static void PlaceUnderMap(global::Hud hud)
    {
        if (_root == null)
            return;
        var map = MapImage();
        if (map == null)
            return;

        var parent = HudRoot(hud);
        if (_root.transform.parent != parent)
            _root.transform.SetParent(parent, false);

        var rt = _root.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        RestlessUi.HangUnder(rt, map, new Vector2(0f, -8f));
    }

    private static void Sync(Player player)
    {
        if (_root == null)
            return;

        var wanted = new HashSet<string>();
        var order = new List<string>();
        BindGuardian(player, wanted, order);

        if (ModConfig.BuffListFood.Value)
            BindFood(player, wanted, order);

        Effects.Clear();
        player.GetSEMan().GetHUDStatusEffects(Effects);
        player.GetGuardianPowerHUD(out var gp, out _);
        var gpHash = gp != null ? gp.NameHash() : 0;
        foreach (var effect in Effects)
        {
            if (effect == null || (gpHash != 0 && effect.NameHash() == gpHash))
                continue;
            var key = "se:" + effect.NameHash();
            wanted.Add(key);
            order.Add(key);
            var seName = Localization.instance != null
                ? Localization.instance.Localize(effect.m_name)
                : effect.m_name;
            GetOrCreate(key).Bind(effect.m_icon, seName, Extra(effect), FillOf(effect));
        }

        var stale = new List<string>();
        foreach (var pair in Rows)
        {
            if (!wanted.Contains(pair.Key))
                stale.Add(pair.Key);
        }

        foreach (var key in stale)
        {
            UnityEngine.Object.Destroy(Rows[key].Root);
            Rows.Remove(key);
            FoodSpan.Remove(key);
        }

        var y = 0f;
        for (var i = 0; i < order.Count; i++)
        {
            Rows[order[i]].Place(y);
            y -= RestlessUi.HudRow.BuffHeight + 4f;
        }

        _root.GetComponent<RectTransform>().sizeDelta = new Vector2(RestlessUi.HudRow.BuffWidth, -y);
    }

    private static void BindFood(Player player, HashSet<string> wanted, List<string> order)
    {
        var foods = player.GetFoods();
        var slots = FoodSlots();
        if (!ModConfig.BuffListFoodEmpty.Value)
            slots = foods?.Count ?? 0;

        for (var i = 0; i < slots; i++)
        {
            var key = "food:" + i;
            wanted.Add(key);
            order.Add(key);
            var food = foods != null && i < foods.Count ? foods[i] : null;
            if (food?.m_item?.m_shared == null)
            {
                FoodSpan.Remove(key);
                GetOrCreate(key).Bind(RestlessUi.BowlIcon(), "Eat", null, 0f, RestlessUi.Muted);
                continue;
            }

            if (!FoodSpan.TryGetValue(key, out var span) || food.m_time > span)
                FoodSpan[key] = span = food.m_time;
            var fill = span > 1f ? food.m_time / span : 0f;
            var foodName = Localization.instance != null
                ? Localization.instance.Localize(food.m_item.m_shared.m_name)
                : food.m_item.m_shared.m_name;
            GetOrCreate(key).Bind(RestlessUi.IconOf(food.m_item), foodName, Clock(food.m_time), fill, RestlessUi.Text);
        }
    }

    private static int FoodSlots()
    {
        var icons = global::Hud.instance?.m_foodIcons;
        return icons != null && icons.Length > 0 ? icons.Length : 3;
    }

    private static void BindGuardian(Player player, HashSet<string> wanted, List<string> order)
    {
        player.GetGuardianPowerHUD(out var se, out var cooldown);
        if (se == null)
            return;

        const string key = "gp";
        wanted.Add(key);
        order.Add(key);

        var live = player.GetSEMan().GetStatusEffect(se.NameHash());
        var name = Localization.instance != null ? Localization.instance.Localize(se.m_name) : se.m_name;
        var fill = 0f;
        string extra;
        if (live != null && live.m_ttl > 0.5f)
        {
            var left = live.GetRemaningTime();
            fill = Mathf.Clamp01(left / live.m_ttl);
            extra = Clock(left);
        }
        else if (cooldown > 0.5f)
        {
            extra = Clock(cooldown);
        }
        else
        {
            extra = "Ready";
        }

        GetOrCreate(key).Bind(se.m_icon, name, extra, fill);
    }

    private static Row GetOrCreate(string key)
    {
        if (Rows.TryGetValue(key, out var row))
            return row;
        row = Row.Create(_root!.transform);
        Rows[key] = row;
        return row;
    }

    private static float FillOf(StatusEffect effect)
    {
        if (effect.m_ttl > 0.5f)
            return Mathf.Clamp01(effect.GetRemaningTime() / effect.m_ttl);
        return 0f;
    }

    private static string Extra(StatusEffect effect)
    {
        var text = effect.GetIconText();
        if (!string.IsNullOrWhiteSpace(text))
            return text.Trim();
        if (effect is SE_Stats stats && Mathf.Abs(stats.m_addMaxCarryWeight) >= 1f)
            return "+" + Mathf.RoundToInt(stats.m_addMaxCarryWeight);
        return "";
    }

    private static string Clock(float seconds)
    {
        if (seconds <= 0f)
            return "";
        if (seconds >= 90f)
            return Mathf.CeilToInt(seconds / 60f) + "m";
        return Mathf.CeilToInt(seconds) + "s";
    }

    private sealed class Row
    {
        public GameObject Root => _row.Root;
        private RestlessUi.HudRow _row = null!;

        public static Row Create(Transform parent) =>
            new() { _row = RestlessUi.HudRow.Buff(parent) };

        public void Place(float y)
        {
            _row.Rect.anchoredPosition = new Vector2(0f, y);
            _row.Rect.sizeDelta = new Vector2(RestlessUi.HudRow.BuffWidth, RestlessUi.HudRow.BuffHeight);
        }

        public void Bind(Sprite? icon, string name, string? extra, float fill = 0f, Color? title = null)
        {
            _row.SetIcon(icon);
            _row.Icon.color = Color.white;
            _row.SetCopy(name, extra);
            _row.SetFill(fill);
            _row.Title.color = title ?? RestlessUi.Text;
        }
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(global::Hud), nameof(global::Hud.UpdateStatusEffects))]
        private static bool UpdateStatusEffects(global::Hud __instance)
        {
            if (!ModConfig.BuffListEnabled.Value)
            {
                if (_root != null)
                    _root.SetActive(false);
                RestoreVanilla();
                return true;
            }

            var player = Player.m_localPlayer;
            if (player == null || __instance.m_rootObject == null || !__instance.m_rootObject.activeInHierarchy)
            {
                if (_root != null)
                    _root.SetActive(false);
                return false;
            }

            if (RestlessUi.MapOpen())
            {
                if (_root != null)
                    _root.SetActive(false);
                HideVanilla(__instance);
                return false;
            }

            RestlessUi.HudTuck.Tick();
            HideVanilla(__instance);
            EnsureRoot(__instance);
            PlaceUnderMap(__instance);
            Sync(player);
            RestlessUi.HudTuck.Fade(_root);
            _root!.SetActive(RestlessUi.HudTuck.Amount < 0.98f);
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(global::Hud), "UpdateGuardianPower")]
        private static void AfterGp(global::Hud __instance)
        {
            if (ModConfig.BuffListEnabled.Value && __instance.m_gpRoot != null)
                __instance.m_gpRoot.gameObject.SetActive(false);
        }
    }
}
