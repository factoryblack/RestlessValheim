using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RestlessQoL.Core;

public sealed class SettingsUi : FeatureModule
{
    public override string Id => "ui.settings";
    public override bool Enabled => true;
    public override bool TickInMenus => true;

    internal static bool IsOpen { get; private set; }

    private static GameObject? _root;
    private static GameObject? _body;
    private static RectTransform? _sheetCard;
    private static CanvasGroup? _fade;
    private static ScrollRect? _scroll;
    private static RectMask2D? _mask;
    private static Text? _hint;
    private static Text? _lock;
    private static string _hintNow = "";
    private const int ListFade = 18;
    private static readonly List<Text> TabLabels = new();
    private static readonly List<GameObject> TabGlows = new();
    private static int _tab;
    private static bool _capturing;
    private static bool _closing;
    private static float _alpha;
    private static ConfigEntry<KeyboardShortcut>? _captureEntry;
    private static Text? _captureLabel;

    private static Transform? _tabRow;

    private static readonly (string Id, string Name)[] AllTabs =
    {
        ("storage", "Storage"),
        ("building", "Building"),
        ("player", "Player"),
        ("world", "World"),
        ("hud", "Hud"),
        ("character", "Character"),
    };

    private static List<(string Id, string Name)> CurrentTabs()
    {
        var list = new List<(string, string)>(AllTabs.Length);
        foreach (var tab in AllTabs)
        {
            if (tab.Id == "character" && (ModConfig.LedgerEnabled == null || !ModConfig.LedgerEnabled.Value))
                continue;
            list.Add(tab);
        }

        return list;
    }

    protected override void OnLoaded()
    {
        GUIManager.OnCustomGUIAvailable += () =>
        {
            _root = null;
            _body = null;
            _fade = null;
            _scroll = null;
            _mask = null;
            _hint = null;
            _lock = null;
            _hintNow = "";
            TabLabels.Clear();
            TabGlows.Clear();
            _tabRow = null;
            IsOpen = false;
            _closing = false;
            _alpha = 0f;
        };
    }

    public override void Tick()
    {
        if (_capturing)
            PollCapture();
        else if (!_closing && !EditingText())
        {
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
                Close();
            else if (IsOpen && Input.GetKeyDown(KeyCode.Q))
                ShiftTab(-1);
            else if (IsOpen && Input.GetKeyDown(KeyCode.E))
                ShiftTab(1);
            else if (ModConfig.SettingsHotkey.Value.IsDown())
                Toggle();
        }

        StepFade();
        if (IsOpen)
        {
            FitSheet();
            UpdateListFades();
        }
    }

    private static void OpenFromMenu()
    {
        Menu.instance?.Hide();
        Open();
    }

    internal static void Toggle()
    {
        if (IsOpen && !_closing)
            Close();
        else
            Open();
    }

    internal static void Open()
    {
        if (GUIManager.CustomGUIFront == null)
            return;
        if (_root == null)
            Build();
        if (_root == null)
            return;
        Highlight();
        Rebuild();
        UpdateLock();
        RestlessUi.Paint(_root);
        _closing = false;
        _root.SetActive(true);
        IsOpen = true;
        GUIManager.BlockInput(true);
    }

    internal static void Close()
    {
        _capturing = false;
        _captureEntry = null;
        _captureLabel = null;
        if (_root == null || _fade == null)
        {
            IsOpen = false;
            _closing = false;
            GUIManager.BlockInput(false);
            return;
        }

        _closing = true;
    }

    private static void StepFade()
    {
        if (_fade == null)
            return;

        var target = IsOpen && !_closing ? 1f : 0f;
        if (Mathf.Approximately(_alpha, target) && !_closing)
        {
            _fade.alpha = _alpha;
            return;
        }

        _alpha = Mathf.MoveTowards(_alpha, target, Time.unscaledDeltaTime / RestlessUi.FadeSeconds);
        _fade.alpha = _alpha;
        _fade.blocksRaycasts = _alpha > 0.05f;
        if (_closing && _alpha <= 0.01f)
        {
            _closing = false;
            _alpha = 0f;
            _fade.alpha = 0f;
            IsOpen = false;
            _root!.SetActive(false);
            GUIManager.BlockInput(false);
        }
    }

    private static void ShiftTab(int delta)
    {
        var tabs = CurrentTabs();
        if (tabs.Count == 0)
            return;
        _tab = (_tab + delta + tabs.Count) % tabs.Count;
        Highlight();
        Rebuild();
    }

    private static bool CanEditGameplay()
    {
        if (!ModConfig.LockConfiguration.Value)
            return true;
        if (ZNet.instance == null || ZNet.instance.IsServer())
            return true;
        return SynchronizationManager.Instance != null && SynchronizationManager.Instance.PlayerIsAdmin;
    }

    private static void Build()
    {
        var front = GUIManager.CustomGUIFront.transform;
        _root = RestlessUi.Node(front, "RestlessCoreSettings");
        RestlessUi.Stretch(_root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        _root.SetActive(false);

        _fade = _root.AddComponent<CanvasGroup>();
        _fade.alpha = 0f;
        _alpha = 0f;

        var dim = RestlessUi.Graphic(_root.transform, "dim", new Color(0f, 0f, 0f, 0.56f));
        RestlessUi.Stretch(dim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        dim.AddComponent<Button>().onClick.AddListener(Close);

        var card = RestlessUi.Node(_root.transform, "card");
        RestlessUi.Pin(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(RestlessUi.PanelWidth, RestlessUi.PanelHeight));

        _sheetCard = card.GetComponent<RectTransform>();
        var tray = RestlessUi.Tray(card.transform, "tray");
        RestlessUi.PaperSurface(tray);
        tray.GetComponent<Image>().raycastTarget = true;
        RestlessUi.Stretch(tray, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        BuildBar(tray.transform);
        BuildList(card.transform);
        BuildHint(card.transform);
        FitSheet();
    }

    private static void FitSheet()
    {
        if (_sheetCard == null || _root == null) return;
        var bounds = _root.GetComponent<RectTransform>().rect;
        var scale = Mathf.Min(1f, Mathf.Min((bounds.width - 32f) / 1120f, (bounds.height - 32f) / 768f));
        _sheetCard.localScale = Vector3.one * Mathf.Max(0.1f, scale);
    }

    private static bool EditingText()
    {
        var selected = EventSystem.current?.currentSelectedGameObject;
        return selected != null && selected.GetComponent<InputField>()?.isFocused == true;
    }

    private static void BuildBar(Transform tray)
    {
        var bar = RestlessUi.PaperSettingsHeader(tray.gameObject);
        var title = RestlessUi.Label(bar.transform, "Restless", RestlessUi.TitleSize, RestlessUi.Text, TextAnchor.MiddleLeft);
        RestlessUi.Pin(title.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(66f, 0f), new Vector2(190f, 46f));

        var esc = RestlessUi.Chip(bar.transform, "esc");
        RestlessUi.PaperControl(esc);
        RestlessUi.Pin(esc, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-62f, 0f), new Vector2(72f, 36f));
        var escLabel = RestlessUi.Label(esc.transform, "ESC", RestlessUi.HintSize, RestlessUi.Text, TextAnchor.MiddleCenter);
        RestlessUi.Stretch(escLabel.gameObject, Vector2.zero, Vector2.one, new Vector2(16f, 0f), Vector2.zero);
        var closeMark = RestlessUi.Picture(esc.transform, "RestlessClose", "utility-close");
        RestlessUi.Pin(closeMark, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(5f, 0f), new Vector2(16f, 16f));
        var escBtn = esc.AddComponent<Button>();
        escBtn.targetGraphic = esc.GetComponent<Image>();
        RestlessUi.PaperSelectable(escBtn);
        escBtn.onClick.AddListener(Close);

        var row = RestlessUi.Node(bar.transform, "tabs");
        _tabRow = row.transform;
        RestlessUi.Pin(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(36f, 0f), new Vector2(650f, 38f));
        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        FillTabs();
    }

    private static void FillTabs()
    {
        if (_tabRow == null)
            return;
        RestlessUi.Wipe(_tabRow);
        TabLabels.Clear();
        TabGlows.Clear();
        Chip(_tabRow, "Q");
        var tabs = CurrentTabs();
        if (_tab >= tabs.Count)
            _tab = Math.Max(0, tabs.Count - 1);
        for (var i = 0; i < tabs.Count; i++)
        {
            var index = i;
            var tab = RestlessUi.Graphic(_tabRow, tabs[i].Id, Color.clear);
            var tabLe = tab.AddComponent<LayoutElement>();
            tabLe.preferredWidth = tabs[i].Id == "character" ? 98f : 82f;
            tabLe.minWidth = 56f;
            tabLe.preferredHeight = 28f;

            var glow = RestlessUi.Chip(tab.transform, "selected");
            RestlessUi.ForgedTab(glow, true);
            glow.GetComponent<Image>().raycastTarget = false;
            RestlessUi.Stretch(glow, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            glow.SetActive(false);
            TabGlows.Add(glow);

            var label = RestlessUi.Label(tab.transform, tabs[i].Name, RestlessUi.HintSize, RestlessUi.PaperMuted, TextAnchor.MiddleCenter);
            RestlessUi.Stretch(label.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TabLabels.Add(label);

            var button = tab.AddComponent<Button>();
            button.targetGraphic = label;
            RestlessUi.PaperSelectable(button);
            button.onClick.AddListener(() =>
            {
                _tab = index;
                Highlight();
                Rebuild();
            });
        }

        Chip(_tabRow, "E");
        Highlight();
    }

    private static void BuildHint(Transform card)
    {
        var inset = RestlessUi.TrayGutter;
        _hint = RestlessUi.Label(card, "", RestlessUi.HintSize, RestlessUi.PaperMuted, TextAnchor.MiddleLeft);
        RestlessUi.Stretch(_hint.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(inset, 12f), new Vector2(-inset - 200f, 56f));
        _hint.horizontalOverflow = HorizontalWrapMode.Wrap;
        _lock = RestlessUi.Label(card, "", RestlessUi.HudSize, RestlessUi.Accent, TextAnchor.MiddleRight);
        RestlessUi.Stretch(_lock.gameObject, Vector2.zero, new Vector2(1f, 0f),
            new Vector2(740f, 12f), new Vector2(-inset, 56f));
        var lockIcon = RestlessUi.Picture(_lock.transform, "RestlessLock", "utility-lock");
        RestlessUi.Pin(lockIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(-22f, 0f), new Vector2(18f, 18f));
        lockIcon.GetComponent<Image>().raycastTarget = false;
    }

    private static void UpdateLock()
    {
        if (_lock == null)
            return;
        _lock.text = CanEditGameplay() ? "" : "Host locked";
        var icon = _lock.transform.Find("RestlessLock");
        if (icon != null) icon.gameObject.SetActive(_lock.text.Length > 0);
    }

    private static void ShowHint(string text)
    {
        _hintNow = text;
        if (_hint != null)
            _hint.text = text;
    }

    private static void HideHint(string text)
    {
        if (_hintNow != text)
            return;
        _hintNow = "";
        if (_hint != null)
            _hint.text = "";
    }

    private static void Chip(Transform parent, string key)
    {
        var plate = RestlessUi.Chip(parent, "chip-" + key);
        RestlessUi.PaperControl(plate);
        var le = plate.AddComponent<LayoutElement>();
        le.preferredWidth = 40f;
        le.minWidth = 40f;
        le.preferredHeight = 30f;
        le.flexibleWidth = 0f;
        var label = RestlessUi.Label(plate.transform, key, RestlessUi.HintSize, RestlessUi.Text, TextAnchor.MiddleCenter);
        RestlessUi.Stretch(label.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var button = plate.AddComponent<Button>();
        button.targetGraphic = plate.GetComponent<Image>();
        RestlessUi.PaperSelectable(button);
        button.onClick.AddListener(() => ShiftTab(key == "Q" ? -1 : 1));
    }

    private static void BuildList(Transform card)
    {
        var inset = RestlessUi.TrayGutter;
        var top = 78f;
        var sheet = RestlessUi.Node(card, "sheet");
        RestlessUi.Stretch(sheet, Vector2.zero, Vector2.one,
            new Vector2(inset, 70f),
            new Vector2(-inset, -top));

        var scroll = RestlessUi.Graphic(sheet.transform, "scroll", Color.clear);
        RestlessUi.Stretch(scroll, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-14f, 0f));
        _mask = scroll.AddComponent<RectMask2D>();
        _mask.softness = new Vector2Int(0, ListFade);
        _scroll = scroll.AddComponent<ScrollRect>();
        _scroll.horizontal = false;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
        _scroll.scrollSensitivity = (RestlessUi.RowHeight + RestlessUi.RowGap) * 4f;
        _scroll.inertia = false;
        _scroll.elasticity = 0f;

        _body = RestlessUi.Node(scroll.transform, "body");
        var bodyRt = _body.GetComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0f, 1f);
        bodyRt.anchorMax = Vector2.one;
        bodyRt.pivot = new Vector2(0.5f, 1f);
        bodyRt.offsetMin = Vector2.zero;
        bodyRt.offsetMax = Vector2.zero;
        var layout = _body.AddComponent<VerticalLayoutGroup>();
        layout.spacing = RestlessUi.RowGap;
        layout.padding = new RectOffset(0, 0, 4, 4);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childAlignment = TextAnchor.UpperCenter;
        _body.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _scroll.content = bodyRt;
        _scroll.viewport = scroll.GetComponent<RectTransform>();

        var bar = RestlessUi.Graphic(sheet.transform, "scroll-bar", new Color(0.05f, 0.04f, 0.03f, 0.9f));
        var barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(1f, 0f);
        barRt.anchorMax = Vector2.one;
        barRt.pivot = new Vector2(1f, 0.5f);
        barRt.sizeDelta = new Vector2(6f, 0f);
        barRt.anchoredPosition = Vector2.zero;

        var handle = RestlessUi.Graphic(bar.transform, "handle", RestlessUi.Accent);
        RestlessUi.Stretch(handle, new Vector2(0f, 0f), Vector2.one, new Vector2(1f, 4f), new Vector2(-1f, -4f));
        var scrollbar = bar.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handle.GetComponent<RectTransform>();
        scrollbar.targetGraphic = handle.GetComponent<Image>();
        _scroll.verticalScrollbar = scrollbar;
        _scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        _scroll.onValueChanged.AddListener(_ => UpdateListFades());
        UpdateListFades();
    }

    private static void UpdateListFades()
    {
        if (_scroll == null || _mask == null)
            return;
        var view = _scroll.viewport != null ? _scroll.viewport.rect.height : 0f;
        var content = _scroll.content != null ? _scroll.content.rect.height : 0f;
        var overflow = content - view;
        if (overflow <= 4f)
        {
            _mask.softness = Vector2Int.zero;
            _mask.padding = Vector4.zero;
            return;
        }

        _mask.softness = new Vector2Int(0, ListFade);
        var n = _scroll.verticalNormalizedPosition;
        var padTop = n > 0.97f ? -ListFade : 0f;
        var padBot = n < 0.03f ? -ListFade : 0f;
        _mask.padding = new Vector4(0f, padBot, 0f, padTop);
    }

    private static void Highlight()
    {
        for (var i = 0; i < TabLabels.Count; i++)
        {
            TabLabels[i].color = i == _tab ? RestlessUi.Accent : RestlessUi.PaperMuted;
            if (i < TabGlows.Count)
                TabGlows[i].SetActive(i == _tab);
        }
    }

    private static void Rebuild()
    {
        if (_body == null)
            return;
        _hintNow = "";
        if (_hint != null)
            _hint.text = "";
        RestlessUi.Wipe(_body.transform);

        var locked = !CanEditGameplay();
        var tabs = CurrentTabs();
        var id = _tab >= 0 && _tab < tabs.Count ? tabs[_tab].Id : "hud";
        switch (id)
        {
            case "storage":
                Head("Chests");
                Bunch(
                    t => Bool("Use nearby chests", ModConfig.StorageEnabled, locked, t),
                    t => Step("Search range", "m", ModConfig.StorageRange, locked, t, true),
                    t => Bool("Leave one when pulling", ModConfig.LeaveOne, locked, t, true));
                Bool("Craft from chests", ModConfig.CraftFromStorageEnabled, locked);
                Bool("Build from chests", ModConfig.BuildFromStorageEnabled, locked);
                Head("Carry");
                Step("Stack size multiplier", "×", ModConfig.StackSizeMultiplier, locked);
                Keyed("Quick stack", ModConfig.QuickStackEnabled, ModConfig.QuickStackHotkey, locked);
                Keyed("Restock", ModConfig.RestockEnabled, ModConfig.RestockHotkey, locked);
                Bunch(
                    t => Bool("Ground piles into matching chests", ModConfig.VacuumEnabled, locked, t),
                    t => Step("Vacuum interval", "s", ModConfig.VacuumInterval, locked, t, true));
                Bool("Tames eat from chests", ModConfig.PetPantryEnabled, locked);
                break;
            case "building":
                Head("Hammer");
                Bunch(
                    t => Bool("Area repair", ModConfig.AreaRepairEnabled, locked, t),
                    t => Step("Repair radius", "m", ModConfig.AreaRepairRadius, locked, t, true));
                Head("Stations");
                Bunch(
                    t => Bool("Workbench range override", ModConfig.WorkbenchTweaksEnabled, locked, t),
                    t => Step("Station range", "m", ModConfig.WorkbenchRange, locked, t, true));
                Bool("Stations pull ore and fuel", ModConfig.StationPullEnabled, locked);
                Bool("Fires and held torches stay lit", ModConfig.EternalFireEnabled, locked);
                Bool("Repair on station use", ModConfig.AutoRepairEnabled, locked);
                break;
            case "player":
                Head("Combat");
                Bool("Keep tools in water", ModConfig.SwimWieldEnabled, locked);
                Bool("Keep a loaded crossbow loaded", ModConfig.CrossbowStateEnabled, locked);
                Bool("No friendly fire on tames", ModConfig.FriendlyFireEnabled, locked);
                Bool("Axe combo while chopping", ModConfig.AxeComboEnabled, locked);
                Head("Camp");
                Bool("Clear death pin when the tomb is empty", ModConfig.DeathPinsEnabled, locked);
                break;
            case "world":
                Head("Terrain");
                Bunch(
                    t => Bool("Dig and raise past the vanilla cap", ModConfig.DigDeeperEnabled, locked, t),
                    t => Step("Max terrain delta", "m", ModConfig.DigMaxDelta, locked, t, true));
                Head("Water");
                Bunch(
                    t => Bool("Floating items", ModConfig.FloatingItemsEnabled, locked, t),
                    t => Bool("Float everything except the sink list", ModConfig.FloatingEverything, locked, t, true),
                    t => Words("Always sink", ModConfig.FloatingSinkList, locked, t, true),
                    t => Words("Also float", ModConfig.FloatingExtraList, locked, t, true));
                Head("Host");
                Bool("Uncap network send", ModConfig.NetworkUncapEnabled, locked);
                Bool("Host-lock gameplay config", ModConfig.LockConfiguration, locked);
                KeyOnly("Open this panel", ModConfig.SettingsHotkey);
                break;
            case "character":
                PaintCharacter();
                break;
            default:
                Head("Buffs");
                Bunch(
                    t => Bool("Buff list under the map", ModConfig.BuffListEnabled, false, t),
                    t => Bool("Include food", ModConfig.BuffListFood, false, t, true),
                    t => Bool("Empty food slots", ModConfig.BuffListFoodEmpty, false, t, true));
                Head("Map");
                Bunch(
                    t => Bool("Restless minimap", ModConfig.MapChromeEnabled, false, t),
                    t => Bool("Torn edge", ModConfig.MapTearEnabled, false, t, true),
                    t => Bool("Biome name plate", ModConfig.MapBiomePlate, false, t, true),
                    t => Bool("Wind arrow on the biome bar", ModConfig.MapWindPlate, false, t, true));
                Bunch(
                    t => Bool("Restless map (M)", ModConfig.MapScreenEnabled, false, t),
                    t => Bool("Player dots on the map", ModConfig.MapPlayerDots, false, t, true));
                Head("Inventory");
                Bunch(
                    t => Bool("Restless inventory (Tab)", ModConfig.InventoryScreenEnabled, false, t),
                    t => Bool("Restless item tooltip", ModConfig.TooltipEnabled, false, t, true),
                    t => Bool("Middle-click lock slots", ModConfig.SlotLockEnabled, false, t, true));
                Bunch(
                    t => Bool("Armor and quick slots", ModConfig.ExtraSlotsEnabled, false, t),
                    t => KeyOnly("Quick slot 1", ModConfig.QuickSlot1, t, true),
                    t => KeyOnly("Quick slot 2", ModConfig.QuickSlot2, t, true),
                    t => KeyOnly("Quick slot 3", ModConfig.QuickSlot3, t, true));
                Bool("Character ledger tab", ModConfig.LedgerEnabled, false, null, false, () =>
                {
                    FillTabs();
                    Rebuild();
                });
                Head("Menu");
                Bool("Restless pause / logout / exit", ModConfig.MenuScreenEnabled, false);
                Head("Notices");
                Bunch(
                    t => Bool("Message toasts", ModConfig.NoticesEnabled, false, t),
                    t => Bool("Merge matching lines", ModConfig.NoticesMerge, false, t, true),
                    t => Bool("Duration fill", ModConfig.NoticesFill, false, t, true),
                    t => Step("Toast sit time", "s", ModConfig.NoticesHold, false, t, true));
                Head("Bar");
                Bunch(
                    t => Bool("Restless hotbar", ModConfig.HotbarEnabled, false, t),
                    t => Bool("Equipped piece helper", ModConfig.EquipHintEnabled, false, t, true),
                    t => Bool("Restless build menu", ModConfig.BuildMenuEnabled, false, t, true),
                    t => Bool("Action hints", ModConfig.ActionHintsEnabled, false, t, true),
                    t => Bool("World hover prompts", ModConfig.LookHintsEnabled, false, t, true));
                Bunch(
                    t => Bool("Health and stamina on the hotbar", ModConfig.VitalsEnabled, false, t),
                    t => Bool("Always show stamina", ModConfig.VitalsStaminaAlways, false, t, true),
                    t => Bool("Numbers on the bars", ModConfig.VitalsNumbers, false, t, true));
                Head("Debug");
                Bunch(
                    t => Bool("Jötunn debug overlay", ModConfig.JotunnDebug, false, t),
                    t => Bool("Hammer piece hover (health / stability)", ModConfig.PieceHealthEnabled, false, t, true));
                break;
        }

        UpdateLock();
        if (_root != null)
            RestlessUi.Paint(_root);
        if (_body != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_body.GetComponent<RectTransform>());
        if (_scroll != null)
            _scroll.verticalNormalizedPosition = 1f;
        UpdateListFades();
    }

    private static void Head(string title)
    {
        var go = RestlessUi.Node(_body!.transform, "head");
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 48f;
        le.preferredHeight = 48f;
        var rule = RestlessUi.Node(go.transform, "rule");
        RestlessUi.Stretch(rule, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -14f), Vector2.zero);
        RestlessUi.PaperDivider(rule);
        le.preferredWidth = RestlessUi.RowWidth;
        le.flexibleHeight = 0f;

        var gem = RestlessUi.Picture(go.transform, "diamond", "diamond");
        RestlessUi.Pin(gem, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, -8f),
            new Vector2(8f, 8f));

        var label = RestlessUi.Label(go.transform, title, RestlessUi.BodySize + 2, RestlessUi.Accent,
            TextAnchor.MiddleLeft);
        RestlessUi.Stretch(label.gameObject, Vector2.zero, Vector2.one, new Vector2(30f, 0f), new Vector2(0f, -16f));
    }

    private static void Bunch(params Action<Transform>[] rows)
    {
        var go = RestlessUi.Node(_body!.transform, "bunch");
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = RestlessUi.RowWidth;
        le.flexibleHeight = 0f;
        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 2f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        foreach (var row in rows)
            row(go.transform);
    }

    private static GameObject Row(Transform? parent = null, bool nested = false)
    {
        parent ??= _body!.transform;
        var shell = RestlessUi.Node(parent, "row");
        var height = nested ? 44f : 48f;
        var le = shell.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
        le.preferredWidth = RestlessUi.RowWidth;
        le.flexibleHeight = 0f;
        var go = RestlessUi.Strip(shell.transform, "plate", nested ? RestlessUi.Nested : RestlessUi.RowTint, true);
        RestlessUi.PaperControl(go);
        if (nested)
            RestlessUi.Stretch(go, Vector2.zero, Vector2.one, new Vector2(16f, 1f), Vector2.zero);
        else
            RestlessUi.Stretch(go, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var edge = go.transform.Find("paperAccent")?.GetComponent<Image>();
        var trigger = go.AddComponent<EventTrigger>();
        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => { if (edge != null) edge.color = RestlessUi.Accent * new Color(1f, 1f, 1f, 0.6f); });
        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(data =>
        {
            if (data is PointerEventData ev && !ev.fullyExited)
                return;
            if (edge != null) edge.color = new Color(0.18f, 0.16f, 0.13f, 0.88f);
        });
        trigger.triggers.Add(enter);
        trigger.triggers.Add(exit);
        ScrollRelay.Bind(go, _scroll);
        return go;
    }

    private static void Titles(GameObject row, string title, string hint, float gutter)
    {
        var t = RestlessUi.Label(row.transform, title, RestlessUi.BodySize, RestlessUi.PaperMuted, TextAnchor.MiddleLeft);
        RestlessUi.Stretch(t.gameObject, Vector2.zero, Vector2.one, new Vector2(20f, 6f), new Vector2(-gutter, -6f));
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        t.gameObject.name = hint;
        var trigger = row.GetComponent<EventTrigger>();
        if (trigger == null)
            return;
        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => ShowHint(hint));
        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(data =>
        {
            if (data is PointerEventData ev && !ev.fullyExited)
                return;
            HideHint(hint);
        });
        trigger.triggers.Add(enter);
        trigger.triggers.Add(exit);
    }

    private static string Hint(ConfigEntryBase entry) => entry.Description.Description;

    private static void Bool(string title, ConfigEntry<bool> entry, bool locked, Transform? parent = null,
        bool nested = false, Action? after = null)
    {
        var row = Row(parent, nested);
        Titles(row, title, Hint(entry), 88f);
        var toggle = RestlessUi.Switch(row.transform, entry.Value);
        RestlessUi.PaperSwitch(toggle, entry.Value);
        RestlessUi.Pin(toggle.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f),
            new Vector2(RestlessUi.ToggleWidth, RestlessUi.ToggleHeight));
        ScrollRelay.Bind(toggle.gameObject, _scroll);
        toggle.interactable = !locked;
        toggle.onClick.AddListener(() =>
        {
            entry.Value = !entry.Value;
            RestlessUi.PaperSwitch(toggle, entry.Value);
            after?.Invoke();
        });
    }

    private static void PaintCharacter()
    {
        Head("This save");
        Stat("Deaths", Count(PlayerStatType.Deaths));
        Stat("Jumps", Count(PlayerStatType.Jumps));
        Stat("Food eaten", Count(PlayerStatType.FoodEaten));
        Stat("Portals used", Count(PlayerStatType.PortalsUsed));
        Stat("Walked", Distance(PlayerStatType.DistanceWalk));
        Stat("Sailed", Distance(PlayerStatType.DistanceSail));
        Stat("Items crafted", Count(PlayerStatType.Crafts));
        Stat("Picked up", Count(PlayerStatType.ItemsPickedUp));
        Stat("Boss kills", Count(PlayerStatType.BossKills));

        Head("Hunts");
        var hunts = 0;
        foreach (var pair in Ledger.Hunts())
        {
            Stat(Ledger.EnemyName(pair.Key), CountValue(pair.Value));
            hunts++;
            if (hunts >= 8)
                break;
        }

        Head("Extras");
        foreach (var pair in Ledger.Extras())
            Stat(pair.Key, CountValue(pair.Value));
    }

    private static void Stat(string title, string value)
    {
        var row = Row();
        var label = RestlessUi.Label(row.transform, title, RestlessUi.BodySize, RestlessUi.PaperMuted,
            TextAnchor.MiddleLeft);
        RestlessUi.Stretch(label.gameObject, Vector2.zero, Vector2.one, new Vector2(20f, 6f), new Vector2(-160f, -6f));
        var face = RestlessUi.Label(row.transform, value, RestlessUi.BodySize, RestlessUi.Accent,
            TextAnchor.MiddleRight);
        RestlessUi.Pin(face.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f),
            new Vector2(140f, 24f));
    }

    private static string Count(PlayerStatType stat) => CountValue(Ledger.Get(stat));

    private static string CountValue(float value)
    {
        if (Mathf.Abs(value - Mathf.Round(value)) < 0.05f)
            return Mathf.RoundToInt(value).ToString();
        return value.ToString("0.#");
    }

    private static string Distance(PlayerStatType stat)
    {
        var metres = Ledger.Get(stat);
        if (metres >= 1000f)
            return (metres / 1000f).ToString("0.0") + " km";
        return Mathf.RoundToInt(metres) + " m";
    }

    private static void Step(string title, string unit, ConfigEntry<float> entry, bool locked, Transform? parent = null, bool nested = false)
    {
        var row = Row(parent, nested);
        Titles(row, title, Hint(entry), 320f);
        var value = RestlessUi.Label(row.transform, Format(entry.Value, unit, entry), RestlessUi.BodySize, RestlessUi.Accent, TextAnchor.MiddleRight);
        RestlessUi.Pin(value.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(72f, 24f));

        var slider = RestlessUi.PaperSlider(row.transform);
        slider.interactable = !locked;
        if (entry.Description.AcceptableValues is AcceptableValueRange<float> range)
        {
            slider.minValue = range.MinValue;
            slider.maxValue = range.MaxValue;
        }
        else
        {
            slider.minValue = 0f;
            slider.maxValue = Mathf.Max(entry.Value, 1f);
        }

        ScrollRelay.Bind(slider.gameObject, _scroll);
        slider.wholeNumbers = StepSize(entry) >= 1f;
        slider.SetValueWithoutNotify(Snap(entry, entry.Value));
        slider.onValueChanged.AddListener(v =>
        {
            var snapped = Snap(entry, v);
            if (!Mathf.Approximately(slider.value, snapped))
                slider.SetValueWithoutNotify(snapped);
            entry.Value = snapped;
            value.text = Format(snapped, unit, entry);
        });
    }

    private static void Keyed(string title, ConfigEntry<bool> enabled, ConfigEntry<KeyboardShortcut> key, bool locked)
    {
        var row = Row();
        Titles(row, title, Hint(enabled), 250f);
        AddKey(row.transform, key, false, -14f - RestlessUi.ToggleWidth - 10f);
        var toggle = RestlessUi.Switch(row.transform, enabled.Value);
        RestlessUi.PaperSwitch(toggle, enabled.Value);
        RestlessUi.Pin(toggle.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f),
            new Vector2(RestlessUi.ToggleWidth, RestlessUi.ToggleHeight));
        ScrollRelay.Bind(toggle.gameObject, _scroll);
        toggle.interactable = !locked;
        toggle.onClick.AddListener(() =>
        {
            enabled.Value = !enabled.Value;
            RestlessUi.PaperSwitch(toggle, enabled.Value);
        });
    }

    private static void KeyOnly(string title, ConfigEntry<KeyboardShortcut> key, Transform? parent = null,
        bool nested = false)
    {
        var row = Row(parent, nested);
        Titles(row, title, Hint(key), 160f);
        AddKey(row.transform, key, false);
    }

    private static void Words(string title, ConfigEntry<string> entry, bool locked, Transform? parent = null,
        bool nested = false)
    {
        var row = Row(parent, nested);
        Titles(row, title, Hint(entry), 300f);
        var plate = RestlessUi.Chip(row.transform, "words");
        RestlessUi.PaperControl(plate);
        RestlessUi.Pin(plate, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f),
            new Vector2(240f, 28f));
        ScrollRelay.Bind(plate, _scroll);
        var label = RestlessUi.Label(plate.transform, entry.Value, RestlessUi.MetaSize, RestlessUi.Text,
            TextAnchor.MiddleLeft);
        RestlessUi.Stretch(label.gameObject, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        var field = plate.AddComponent<InputField>();
        field.textComponent = label;
        field.text = entry.Value;
        RestlessUi.PaperSelectable(field);
        field.interactable = !locked;
        field.caretColor = RestlessUi.Accent;
        field.selectionColor = new Color(RestlessUi.Accent.r, RestlessUi.Accent.g, RestlessUi.Accent.b, 0.25f);
        field.onEndEdit.AddListener(value => entry.Value = value);
    }

    private static void AddKey(Transform parent, ConfigEntry<KeyboardShortcut> entry, bool locked, float fromRight = -14f)
    {
        var plate = RestlessUi.Chip(parent, "key");
        RestlessUi.PaperControl(plate);
        RestlessUi.Pin(plate, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(fromRight, 0f),
            new Vector2(RestlessUi.KeyWidth, RestlessUi.KeyHeight));
        ScrollRelay.Bind(plate, _scroll);
        var label = RestlessUi.Label(plate.transform, Pretty(entry.Value), RestlessUi.MetaSize, RestlessUi.Text, TextAnchor.MiddleCenter);
        RestlessUi.Stretch(label.gameObject, Vector2.zero, Vector2.one, new Vector2(6f, 0f), new Vector2(-6f, 0f));
        var go = plate;
        var button = go.AddComponent<Button>();
        button.targetGraphic = plate.GetComponent<Image>();
        RestlessUi.PaperSelectable(button);
        button.interactable = !locked;
        button.onClick.AddListener(() =>
        {
            _capturing = true;
            _captureEntry = entry;
            _captureLabel = label;
            label.text = "Press a key";
            label.color = RestlessUi.Accent;
        });
    }

    private static void PollCapture()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_captureLabel != null && _captureEntry != null)
            {
                _captureLabel.text = Pretty(_captureEntry.Value);
                _captureLabel.color = RestlessUi.PaperMuted;
            }

            _capturing = false;
            _captureEntry = null;
            _captureLabel = null;
            return;
        }

        foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
        {
            if (!Input.GetKeyDown(key) || IsModifier(key) || IsPointer(key) || key == KeyCode.Escape)
                continue;
            var mods = new List<KeyCode>();
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                mods.Add(KeyCode.LeftShift);
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                mods.Add(KeyCode.LeftControl);
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                mods.Add(KeyCode.LeftAlt);
            _captureEntry!.Value = new KeyboardShortcut(key, mods.ToArray());
            if (_captureLabel != null)
            {
                _captureLabel.text = Pretty(_captureEntry.Value);
                _captureLabel.color = RestlessUi.PaperMuted;
            }

            _capturing = false;
            _captureEntry = null;
            _captureLabel = null;
            return;
        }
    }

    private static string Pretty(KeyboardShortcut shortcut) =>
        shortcut.ToString()
            .Replace("BackQuote", "`")
            .Replace("LeftShift", "Shift")
            .Replace("RightShift", "Shift")
            .Replace("LeftControl", "Ctrl")
            .Replace("RightControl", "Ctrl")
            .Replace("LeftAlt", "Alt")
            .Replace("RightAlt", "Alt");

    private static bool IsModifier(KeyCode key) =>
        key is KeyCode.LeftShift or KeyCode.RightShift or KeyCode.LeftControl or KeyCode.RightControl
            or KeyCode.LeftAlt or KeyCode.RightAlt or KeyCode.LeftCommand or KeyCode.RightCommand;

    private static bool IsPointer(KeyCode key) =>
        key is KeyCode.Mouse0 or KeyCode.Mouse1 or KeyCode.Mouse2 or KeyCode.Mouse3 or KeyCode.Mouse4
            or KeyCode.Mouse5 or KeyCode.Mouse6;

    private static float StepSize(ConfigEntry<float> entry) =>
        entry.Description.AcceptableValues is AcceptableValueRange<float> range && range.MaxValue - range.MinValue <= 8f
            ? 0.05f
            : 1f;

    private static float Clamp(ConfigEntry<float> entry, float value) =>
        entry.Description.AcceptableValues is AcceptableValueRange<float> range
            ? Mathf.Clamp(value, range.MinValue, range.MaxValue)
            : value;

    private static float Snap(ConfigEntry<float> entry, float value)
    {
        var step = StepSize(entry);
        return Clamp(entry, Mathf.Round(value / step) * step);
    }

    private static string Format(float value, string unit, ConfigEntry<float> entry)
    {
        var step = StepSize(entry);
        var snapped = Snap(entry, value);
        var number = step >= 1f
            ? Mathf.RoundToInt(snapped).ToString()
            : snapped.ToString(step >= 0.1f ? "0.#" : "0.##");
        return number + " " + unit;
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Menu), nameof(Menu.Start))]
        private static void MenuStart(Menu __instance)
        {
            var src = __instance.m_settingsButton;
            if (src == null)
                return;
            var parent = src.transform.parent;
            if (parent.Find("RestlessCoreMenuButton") != null)
                return;
            var clone = UnityEngine.Object.Instantiate(src, parent);
            clone.name = "RestlessCoreMenuButton";
            clone.transform.SetSiblingIndex(src.transform.GetSiblingIndex() + 1);
            foreach (var component in clone.GetComponentsInChildren<Component>(true))
            {
                if (component != null && component.GetType().Name is "Localize" or "LocalizedText")
                    UnityEngine.Object.Destroy(component);
            }

            foreach (var text in clone.GetComponentsInChildren<Text>(true))
                text.text = "Restless";
            foreach (var tmp in clone.GetComponentsInChildren<TextMeshProUGUI>(true))
                tmp.text = "Restless";
            clone.onClick = new Button.ButtonClickedEvent();
            clone.onClick.AddListener(OpenFromMenu);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Menu), nameof(Menu.Hide))]
        private static void MenuHide()
        {
            if (IsOpen)
                Close();
        }
    }
}
