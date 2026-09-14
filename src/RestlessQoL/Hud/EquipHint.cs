using System.Collections.Generic;
using HarmonyLib;
using Jotunn.Managers;
using RestlessQoL.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

public sealed class EquipHint : FeatureModule
{
    public override string Id => "ui.equiphint";
    public override bool Enabled => true;

    internal static bool Showing => _root != null && _root.activeSelf;

    private static readonly HashSet<Behaviour> Hidden = new();
    private static GameObject? _root;
    private static Image? _icon;
    private static Text? _name;

    protected override void OnLoaded()
    {
        GUIManager.OnCustomGUIAvailable += TearDown;
    }

    public override void Tick()
    {
        if (ModConfig.EquipHintEnabled.Value)
            return;
        if (_root != null)
            _root.SetActive(false);
        Restore();
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(global::Hud), "UpdateBuild")]
        private static void AfterBuild(global::Hud __instance)
        {
            if (!ModConfig.EquipHintEnabled.Value)
            {
                Restore();
                if (_root != null)
                    _root.SetActive(false);
                return;
            }

            var helper = __instance.m_buildHud != null && __instance.m_buildHud.activeSelf
                && !global::Hud.IsPieceSelectionVisible();
            if (!helper)
            {
                Restore();
                if (_root != null)
                    _root.SetActive(false);
                return;
            }

            HideVanilla(__instance);
            HidePieceHealth(__instance);
            KeepQuiet();
            Ensure();
            Paint(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(global::Hud), "UpdateCrosshair")]
        private static void AfterCrosshair(global::Hud __instance)
        {
            HidePieceHealth(__instance);
        }
    }

    private static void HidePieceHealth(global::Hud hud)
    {
        if (ModConfig.PieceHealthEnabled.Value)
            return;
        void Hide(object? node)
        {
            if (node is GameObject go)
                go.SetActive(false);
            else if (node is Component c && c.gameObject != null)
                c.gameObject.SetActive(false);
        }

        Hide(hud.m_pieceHealthRoot);
        var data = Traverse.Create(hud);
        foreach (var name in new[] { "m_pieceHealthRoot", "m_pieceHealthBar" })
        {
            if (!data.Field(name).FieldExists())
                continue;
            Hide(data.Field(name).GetValue());
        }
    }

    // Selected-piece name/icon only. m_buildHud also parents 1.0 BuildUi
    // (search + right-click piece menu). A CanvasGroup on that root blanks
    // the menu and can leave the search field focused, which kills TakeInput.
    // Hide the leftover vanilla plate behind "Repair" — do not Ghost it.
    private static void HideVanilla(global::Hud hud)
    {
        LoudBuildHud(hud);
        Hide(hud.m_buildSelection);
        Hide(hud.m_buildIcon);
        Hide(hud.m_pieceDescription);
        HideHelperChrome(hud);
    }

    private static void HideHelperChrome(global::Hud hud)
    {
        var root = hud.m_buildHud;
        if (root == null)
            return;
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (KeepBuildUi(image.transform, hud))
                continue;
            Hide(image);
        }

        foreach (var raw in root.GetComponentsInChildren<RawImage>(true))
        {
            if (KeepBuildUi(raw.transform, hud))
                continue;
            Hide(raw);
        }

        foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (KeepBuildUi(tmp.transform, hud))
                continue;
            Hide(tmp);
        }
    }

    private static bool KeepBuildUi(Transform node, global::Hud hud)
    {
        if (hud.m_buildUi != null && node.IsChildOf(hud.m_buildUi.transform))
            return true;
        return hud.m_pieceSelectionWindow != null
               && node.IsChildOf(hud.m_pieceSelectionWindow.transform);
    }

    private static void Restore()
    {
        var hud = global::Hud.instance;
        if (hud == null)
            return;
        LoudBuildHud(hud);
        foreach (var part in Hidden)
        {
            if (part != null)
                part.enabled = true;
        }

        Hidden.Clear();
        Show(hud.m_buildSelection);
        Show(hud.m_buildIcon);
        Show(hud.m_pieceDescription);
    }

    private static void KeepQuiet()
    {
        foreach (var part in Hidden)
        {
            if (part != null)
                part.enabled = false;
        }
    }

    private static void LoudBuildHud(global::Hud hud)
    {
        if (hud.m_buildHud == null)
            return;
        var group = hud.m_buildHud.GetComponent<CanvasGroup>();
        if (group == null)
            return;
        group.alpha = 1f;
        group.blocksRaycasts = true;
        group.interactable = true;
    }

    private static void Hide(Behaviour? part)
    {
        if (part == null)
            return;
        Hidden.Add(part);
        part.enabled = false;
    }

    private static void Show(Behaviour? part)
    {
        if (part != null)
            part.enabled = true;
    }

    private static void TearDown()
    {
        Restore();
        if (_root != null)
            Object.Destroy(_root);
        _root = null;
        _icon = null;
        _name = null;
    }

    private static Transform? HudRoot() =>
        global::Hud.instance?.m_rootObject != null
            ? global::Hud.instance.m_rootObject.transform
            : null;

    private static void Ensure()
    {
        if (_root != null && _root.transform.Find("snap") != null)
            TearDown();
        if (_root != null)
            return;
        var parent = HudRoot();
        if (parent == null)
            return;

        _root = RestlessUi.Node(parent, "RestlessEquipHint");
        RestlessUi.Place(_root, RestlessUi.HudDock.Bar, new Vector2(300f, 44f), new Vector2(0f, 80f));

        var strip = RestlessUi.Strip(_root.transform, "strip");
        RestlessUi.Pin(strip, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 40f));

        var iconGo = RestlessUi.Graphic(_root.transform, "icon", Color.white, false);
        RestlessUi.Pin(iconGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-110f, 0f),
            new Vector2(28f, 28f));
        _icon = iconGo.GetComponent<Image>();
        _icon.preserveAspect = true;

        _name = RestlessUi.Label(_root.transform, "", RestlessUi.BodySize, RestlessUi.Text, TextAnchor.MiddleLeft);
        RestlessUi.Pin(_name.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(16f, 0f),
            new Vector2(180f, 32f));
    }

    private static void Paint(global::Hud hud)
    {
        if (_root == null || _name == null || _icon == null)
            return;
        _root.SetActive(true);

        var title = hud.m_buildSelection != null ? hud.m_buildSelection.text : "";
        if (Localization.instance != null)
            title = Localization.instance.Localize(title);
        _name.text = title;
        var sprite = hud.m_buildIcon != null ? hud.m_buildIcon.sprite : null;
        _icon.enabled = sprite != null;
        _icon.sprite = sprite;
    }
}
