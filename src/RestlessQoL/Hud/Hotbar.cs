using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Jotunn.Managers;
using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

public sealed class Hotbar : FeatureModule
{
    public override string Id => "ui.hotbar";
    public override bool Enabled => true;

    private static readonly List<Behaviour> Hidden = new();
    private static readonly List<GameObject> Ours = new();
    private static Transform? _home;
    private static int _homeIndex;
    private static Vector2 _homeMin;
    private static Vector2 _homeMax;
    private static Vector2 _homePivot;
    private static Vector2 _homePos;
    private static Vector2 _homeSize;
    private static bool _moved;
    private static HotkeyBar? _bar;
    private static float _spanLeft;
    private static float _spanRight;
    private static float _spanMidY;
    private static float _spanHeight;
    private static bool _spanOk;

    protected override void OnLoaded()
    {
        GUIManager.OnCustomGUIAvailable += TearDown;
    }

    public override void Tick()
    {
        if (ModConfig.HotbarEnabled.Value)
            return;
        TearDown();
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HotkeyBar), "UpdateIcons")]
        private static void UpdateIcons(HotkeyBar __instance)
        {
            if (!ModConfig.HotbarEnabled.Value)
            {
                TearDown();
                return;
            }

            Dress(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(global::Hud), nameof(global::Hud.Update))]
        private static void HudTick()
        {
            if (!ModConfig.HotbarEnabled.Value || _bar == null)
                return;
            Park(_bar);
        }
    }

    private static void TearDown()
    {
        foreach (var go in Ours)
        {
            if (go != null)
                Object.Destroy(go);
        }

        Ours.Clear();
        foreach (var behaviour in Hidden)
        {
            if (behaviour != null)
                behaviour.enabled = true;
        }

        Hidden.Clear();

        var bar = _bar;
        if (_moved && bar != null)
        {
            var rt = bar.GetComponent<RectTransform>();
            if (_home != null)
                bar.transform.SetParent(_home, false);
            if (rt != null)
            {
                rt.anchorMin = _homeMin;
                rt.anchorMax = _homeMax;
                rt.pivot = _homePivot;
                rt.anchoredPosition = _homePos;
                rt.sizeDelta = _homeSize;
                if (_home != null)
                    bar.transform.SetSiblingIndex(Mathf.Clamp(_homeIndex, 0, _home.childCount - 1));
            }
        }

        _moved = false;
        _home = null;
        _bar = null;
        _spanOk = false;
    }

    // World span of the Restless hotbar. Vitals dock to this. Extra quick slots
    // hang off the right edge — do not reparent a second bar.
    internal static bool TrySpan(out float left, out float right, out float midY, out float height)
    {
        left = _spanLeft;
        right = _spanRight;
        midY = _spanMidY;
        height = _spanHeight;
        return _spanOk;
    }

    // Torn plate of an unlit hotbar slot. No cell fallback — that is smaller than the overflow plate.
    internal static bool TryPlateLocal(out float size)
    {
        size = 0f;
        if (!TrySlotLocal(out _, out var plate))
            return false;
        size = Mathf.Min(plate.x, plate.y);
        return size > 8f;
    }

    // Unlit hotbar cell + plate. Skip the held slot so Z/X/C do not inherit
    // slot 1's selected overflow when 1 is active.
    internal static bool TrySlotLocal(out Vector2 cell, out Vector2 plate)
    {
        cell = plate = default;
        if (_bar == null)
            return false;
        var elements = Traverse.Create(_bar).Field("m_elements").GetValue<IList>();
        if (elements == null || elements.Count == 0)
            return false;
        GameObject? pick = null;
        GameObject? fallback = null;
        for (var i = 0; i < elements.Count; i++)
        {
            var go = GoOf(elements[i]!);
            if (go == null || go.transform.Find("RestlessSlot") == null)
                continue;
            fallback ??= go;
            if (!Held(elements[i]!, i))
            {
                pick = go;
                break;
            }
        }

        var use = pick != null ? pick : fallback;
        if (use == null)
            return false;
        var rt = use.GetComponent<RectTransform>();
        var dressed = use.transform.Find("RestlessSlot") as RectTransform;
        if (rt == null || dressed == null)
            return false;
        cell = new Vector2(rt.rect.width, rt.rect.height);
        plate = dressed.sizeDelta;
        return cell.x > 8f && plate.x > 8f;
    }

    // World pitch between slot 1 and 2, and slot 8's center. Z/X/C continue this row.
    internal static bool TryPitch(out float step, out float lastX, out float midY)
    {
        step = lastX = midY = 0f;
        if (_bar == null)
            return false;
        var elements = Traverse.Create(_bar).Field("m_elements").GetValue<IList>();
        if (elements == null || elements.Count < 2)
            return false;

        RectTransform? first = null;
        RectTransform? second = null;
        RectTransform? last = null;
        for (var i = 0; i < elements.Count; i++)
        {
            var go = Traverse.Create(elements[i]!).Field("m_go").GetValue<GameObject>();
            var plate = go != null ? go.transform.Find("RestlessSlot") as RectTransform : null;
            var slot = plate != null ? plate : go != null ? go.GetComponent<RectTransform>() : null;
            if (slot == null || !slot.gameObject.activeInHierarchy)
                continue;
            first ??= slot;
            if (i == 1)
                second = slot;
            last = slot;
        }

        if (first == null || last == null)
            return false;
        if (second != null)
            step = second.position.x - first.position.x;
        if (Mathf.Abs(step) < 4f)
        {
            var space = Traverse.Create(_bar).Field("m_elementSpace").GetValue<float>();
            step = space * Mathf.Abs(first.lossyScale.x);
        }

        lastX = last.position.x;
        midY = last.position.y;
        return Mathf.Abs(step) > 4f;
    }

    internal static bool Measure(HotkeyBar? bar, out float left, out float right, out float midY, out float height)
    {
        left = right = midY = height = 0f;
        if (bar == null)
            return false;
        var elements = Traverse.Create(bar).Field("m_elements").GetValue<IList>();
        if (elements == null || elements.Count == 0)
            return false;

        var min = float.PositiveInfinity;
        var max = float.NegativeInfinity;
        var bottom = float.PositiveInfinity;
        var top = float.NegativeInfinity;
        foreach (var item in elements)
        {
            var go = Traverse.Create(item).Field("m_go").GetValue<GameObject>();
            var slot = go != null ? go.GetComponent<RectTransform>() : null;
            if (slot == null || !slot.gameObject.activeInHierarchy)
                continue;
            var box = new Vector3[4];
            slot.GetWorldCorners(box);
            min = Mathf.Min(min, box[0].x);
            max = Mathf.Max(max, box[2].x);
            bottom = Mathf.Min(bottom, box[0].y);
            top = Mathf.Max(top, box[1].y);
        }

        if (float.IsInfinity(min) || max <= min)
            return false;
        left = min;
        right = max;
        midY = (bottom + top) * 0.5f;
        height = top - bottom;
        return true;
    }

    private static void Dress(HotkeyBar bar)
    {
        Park(bar);
        var elements = Traverse.Create(bar).Field("m_elements").GetValue<IList>();
        if (elements == null || elements.Count == 0)
            return;
        for (var i = 0; i < elements.Count; i++)
        {
            try
            {
                Slot(elements[i]!, i);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning("ui.hotbar slot " + i + ": " + e.Message);
            }
        }
    }

    private static void Park(HotkeyBar bar)
    {
        _bar = bar;
        var hud = global::Hud.instance?.m_rootObject != null
            ? global::Hud.instance.m_rootObject.transform
            : null;
        if (hud == null)
            return;

        var rt = bar.GetComponent<RectTransform>();
        if (rt == null)
            return;

        if (!_moved)
        {
            _home = bar.transform.parent;
            _homeIndex = bar.transform.GetSiblingIndex();
            _homeMin = rt.anchorMin;
            _homeMax = rt.anchorMax;
            _homePivot = rt.pivot;
            _homePos = rt.anchoredPosition;
            _homeSize = _homeMin != _homeMax
                ? new Vector2(rt.rect.width, rt.rect.height)
                : rt.sizeDelta;
            _moved = true;
        }

        var space = Traverse.Create(bar).Field("m_elementSpace").GetValue<float>();
        var count = Traverse.Create(bar).Field("m_elements").GetValue<IList>()?.Count ?? 0;
        var width = space > 1f && count > 0 ? space * count : Mathf.Max(rt.rect.width, 552f);
        var height = Mathf.Max(rt.rect.height, 64f);

        // Restless owns this HotkeyBar while dressed. TearDown puts it back.
        if (bar.transform.parent != hud)
            bar.transform.SetParent(hud, false);

        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(0f, 96f);
        NudgeCenter(bar, rt);
    }

    private static void NudgeCenter(HotkeyBar bar, RectTransform rt)
    {
        if (!Measure(bar, out var min, out var max, out var midY, out var height))
            return;
        _spanLeft = min;
        _spanRight = max;
        _spanMidY = midY;
        _spanHeight = height;
        _spanOk = true;
        if (rt.parent is not RectTransform parent)
            return;

        var screen = new Vector3[4];
        parent.GetWorldCorners(screen);
        var want = (screen[0].x + screen[2].x) * 0.5f;
        var have = (min + max) * 0.5f + ExtraSlots.HudPad(height) * 0.5f;
        var scale = rt.lossyScale.x;
        if (scale < 0.01f)
            return;
        rt.anchoredPosition += new Vector2((want - have) / scale, 0f);
        if (Measure(bar, out min, out max, out midY, out height))
        {
            _spanLeft = min;
            _spanRight = max;
            _spanMidY = midY;
            _spanHeight = height;
        }
    }

    private static void Slot(object element, int index)
    {
        var go = GoOf(element);
        if (go == null)
            return;

        var icon = IconOf(element);
        var lit = Held(element, index);
        var item = Player.m_localPlayer?.GetInventory()?.GetItemAt(index, 0)
            ?? Read<ItemDrop.ItemData>(element, "m_item");
        var plate = RestlessUi.DressSlot(go, icon, lit, item, Hidden, false, SlotLock.Held(new Vector2i(index, 0)));
        if (!Ours.Contains(plate))
            Ours.Add(plate);

        var key = go.transform.Find("RestlessKey");
        if (key == null)
        {
            var chip = RestlessUi.Chip(go.transform, "RestlessKey");
            var label = RestlessUi.Label(chip.transform, Key(index), RestlessUi.HudMeta, RestlessUi.Text,
                TextAnchor.MiddleCenter);
            RestlessUi.Stretch(label.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Ours.Add(chip);
            key = chip.transform;
        }

        RestlessUi.Pin(key.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f),
            new Vector2(22f, 18f));
        key.GetComponent<Image>().color = lit
            ? Color.Lerp(RestlessUi.ChipTint, RestlessUi.Accent, 0.45f)
            : RestlessUi.ChipTint;

        var text = key.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = Key(index);
            text.color = lit ? RestlessUi.Accent : RestlessUi.Text;
        }

        key.gameObject.SetActive(true);
    }

    private static string Key(int index) => index >= 9 ? "0" : (index + 1).ToString();

    private static GameObject? GoOf(object element) => Read<GameObject>(element, "m_go");

    private static Image? IconOf(object element) => Read<Image>(element, "m_icon");

    private static T? Read<T>(object element, string field) where T : class
    {
        try
        {
            var t = Traverse.Create(element).Field(field);
            if (!t.FieldExists())
                return null;
            return t.GetValue() as T;
        }
        catch
        {
            return null;
        }
    }

    private static bool Held(object element, int index)
    {
        var player = Player.m_localPlayer;
        if (player == null)
            return false;
        var item = player.GetInventory()?.GetItemAt(index, 0)
            ?? Read<ItemDrop.ItemData>(element, "m_item");
        if (item == null)
            return false;
        return item.m_equipped
            || player.GetRightItem() == item
            || player.GetLeftItem() == item;
    }
}
