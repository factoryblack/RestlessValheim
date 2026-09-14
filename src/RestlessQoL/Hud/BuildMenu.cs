using System.Collections.Generic;
using HarmonyLib;
using Jotunn.Managers;
using RestlessQoL.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

// 1.0 hammer / hoe / serving-tray menu (BuildUIV2). Dress in place.
// Do not CanvasGroup m_buildHud, Ghost Masks, or ChipButton (LayoutElement
// stretches the four category tabs into pillars and blanks the grid).
public sealed class BuildMenu : FeatureModule
{
    public override string Id => "ui.build";
    public override bool Enabled => true;

    private static readonly List<GameObject> Ours = new();
    private static readonly List<Behaviour> Hidden = new();
    private static readonly HashSet<GameObject> Silenced = new();
    private static bool _dressed;
    private static bool _dumped;

    protected override void OnLoaded()
    {
        GUIManager.OnCustomGUIAvailable += TearDown;
    }

    public override void Tick()
    {
        if (ModConfig.BuildMenuEnabled.Value)
            return;
        if (_dressed)
            Undress();
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(global::Hud), "UpdateBuild")]
        private static void AfterBuild(global::Hud __instance)
        {
            if (!ModConfig.BuildMenuEnabled.Value)
            {
                if (_dressed)
                    Undress();
                return;
            }

            if (!Open(__instance))
                return;

            DumpOnce(__instance);
            if (!_dressed)
            {
                Scrub(RootOf(__instance.m_buildUi));
                Scrub(RootOf(__instance.m_pieceSelectionWindow));
                _dressed = true;
            }

            Sync(__instance);
            KeepQuiet();
        }
    }

    private static bool Open(global::Hud hud) =>
        global::Hud.IsPieceSelectionVisible()
        || Live(hud.m_buildUi)
        || Live(hud.m_pieceSelectionWindow);

    private static bool Live(object? node) =>
        node is GameObject go && go.activeInHierarchy
        || node is Component c && c.gameObject.activeInHierarchy;

    private static Transform? RootOf(object? node) =>
        node as Transform
        ?? (node as Component)?.transform
        ?? (node as GameObject)?.transform;

    private static void DumpOnce(global::Hud hud)
    {
        if (_dumped)
            return;
        var root = RootOf(hud.m_buildUi) ?? RootOf(hud.m_pieceSelectionWindow);
        if (root == null)
            return;
        _dumped = true;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("ui.build dump");
        foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            var rt = graphic.rectTransform;
            sb.Append("  ").Append(graphic.GetType().Name).Append(' ')
                .Append(Trail(graphic.transform, root))
                .Append(' ').Append(rt.rect.width.ToString("0"))
                .Append('x').Append(rt.rect.height.ToString("0"))
                .AppendLine();
        }

        Plugin.Log.LogInfo(sb.ToString());
    }

    private static string Trail(Transform node, Transform root)
    {
        var parts = new List<string>();
        for (var t = node; t != null; t = t.parent)
        {
            parts.Add(t.name);
            if (t == root)
                break;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    private static void Sync(global::Hud hud)
    {
        var root = RootOf(hud.m_buildUi);
        Quiet(root);
        Quiet(RootOf(hud.m_pieceSelectionWindow));
        QuietScroll(root);
        HideHover(root);
        var ui = hud.m_buildUi;
        if (ui == null)
            return;
        DressPieces(ui);
        DressTags(ui);
        DressTabs(ui);
        DressSearch(ui);
        DressKeys(ui);
    }

    private static void Scrub(Transform? root)
    {
        if (root == null)
            return;
        foreach (var chip in root.GetComponentsInChildren<Transform>(true))
        {
            if (chip.name is not ("RestlessChip" or "RestlessSlot"))
                continue;
            var rt = chip.GetComponent<RectTransform>();
            if (rt != null && rt.rect.height > 48f)
                Object.Destroy(chip.gameObject);
        }

        foreach (var hold in root.GetComponentsInChildren<LayoutElement>(true))
        {
            if (hold.transform.Find("RestlessChip") == null)
                continue;
            Object.Destroy(hold);
        }
    }

    private static void KeepQuiet()
    {
        foreach (var behaviour in Hidden)
        {
            if (behaviour != null)
                behaviour.enabled = false;
        }

        foreach (var go in Silenced)
        {
            if (go == null)
                continue;
            RestlessUi.SilenceTmp(go.GetComponent<TMP_Text>());
        }
    }

    private static void Quiet(Transform? root)
    {
        if (root == null)
            return;
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.transform.name.StartsWith("Restless"))
                continue;
            if (LeaveMask(image.transform))
                continue;
            var n = image.gameObject.name.ToLowerInvariant();
            var size = image.rectTransform.rect;
            if (size.width >= 800f && size.height >= 800f)
            {
                Hide(image);
                continue;
            }

            if (n is "background" or "tabborder" || n.Contains("wood") || n is "darken" or "blur"
                || n is "panel-back" or "bkg" || n.Contains("bkg"))
                Hide(image);
        }
    }

    private static void QuietScroll(Transform? root)
    {
        if (root == null)
            return;
        foreach (var scroll in root.GetComponentsInChildren<ScrollRect>(true))
        {
            var image = scroll.GetComponent<Image>();
            if (image != null && image.GetComponent<Mask>() == null
                && image.GetComponent<RectMask2D>() == null)
                Hide(image);
        }

        foreach (var bar in root.GetComponentsInChildren<Scrollbar>(true))
        {
            foreach (var image in bar.GetComponentsInChildren<Image>(true))
                Hide(image);
        }
    }

    private static void HideHover(Transform? root)
    {
        if (root == null)
            return;
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.transform.name.StartsWith("Restless"))
                continue;
            if (LeaveMask(image.transform) && image.GetComponentInParent<BuildUiPieceButton>() == null
                && image.GetComponentInParent<BuildUiTagButton>() == null)
                continue;
            var n = image.gameObject.name.ToLowerInvariant();
            if (n.Contains("icon") || n.Contains("star") || n.Contains("arrow"))
                continue;
            if (n.Contains("selected") || n.Contains("hover") || n.Contains("highlight")
                || n.Contains("glow"))
                Hide(image);
        }
    }

    // Viewport mask, typed search, and the piece grid stay vanilla.
    private static bool LeaveMask(Transform node)
    {
        for (var t = node; t != null; t = t.parent)
        {
            if (t.GetComponent<TMP_InputField>() != null || t.GetComponent<InputField>() != null)
                return true;
            if (t.GetComponent<Mask>() != null || t.GetComponent<RectMask2D>() != null)
                return true;
            if (t.GetComponent<BuildUiPieceButton>() != null)
                return true;
            var n = t.name.ToLowerInvariant();
            if (n.Contains("viewport") || n is "content")
                return true;
        }

        return false;
    }

    private static void DressPieces(BuildUi ui)
    {
        if (ui.m_pieceButtons == null)
            return;
        foreach (var piece in ui.m_pieceButtons)
        {
            if (piece == null || !piece.gameObject.activeInHierarchy)
                continue;
            var rt = piece.GetComponent<RectTransform>();
            if (rt != null && (rt.rect.width > 80f || rt.rect.height > 80f
                || rt.rect.width < 24f || rt.rect.height < 24f))
                continue;
            var icon = piece.m_icon ?? RestlessUi.Deep<Image>(piece.transform, "Piece Icon");
            var lit = piece == ui.m_lastSelectedPieceBtn || piece == ui.m_currentHoveredPieceButton;
            var plate = RestlessUi.DressSlot(piece.gameObject, icon, lit, null, Hidden, true);
            if (!Ours.Contains(plate))
                Ours.Add(plate);
            KeepMarks(piece.transform);
        }

        if (ui.m_specialPieceButton != null && ui.m_specialPieceButton.gameObject.activeInHierarchy)
        {
            var special = ui.m_specialPieceButton;
            var rt = special.GetComponent<RectTransform>();
            if (rt != null && rt.rect.width <= 80f && rt.rect.height <= 80f)
            {
                var icon = special.m_icon ?? RestlessUi.Deep<Image>(special.transform, "Piece Icon");
                var plate = RestlessUi.DressSlot(special.gameObject, icon,
                    special == ui.m_lastSelectedPieceBtn || special == ui.m_currentHoveredPieceButton,
                    null, Hidden, true);
                if (!Ours.Contains(plate))
                    Ours.Add(plate);
                KeepMarks(special.transform);
            }
        }
    }

    private static void KeepMarks(Transform piece)
    {
        foreach (var image in piece.GetComponentsInChildren<Image>(true))
        {
            var n = image.gameObject.name.ToLowerInvariant();
            if (!n.Contains("star") && !n.Contains("arrow") && !n.Contains("icon"))
                continue;
            Hidden.Remove(image);
            image.enabled = true;
        }
    }

    private static void DressTags(BuildUi ui)
    {
        if (ui.m_showAllTagsButton != null)
            DressTag(ui.m_showAllTagsButton, "Show all",
                Equals(ui.m_showAllTagsButton.m_tagId, ui.m_currentTagId));
        if (ui.m_tagButtons == null)
            return;
        foreach (var tag in ui.m_tagButtons)
            DressTag(tag, tag != null ? tag.gameObject.name : "Tag",
                tag != null && Equals(tag.m_tagId, ui.m_currentTagId));
    }

    private static void DressTag(BuildUiTagButton? tag, string fallback, bool lit)
    {
        if (tag == null || !tag.gameObject.activeInHierarchy)
            return;
        var button = tag.GetComponent<Button>() ?? tag.GetComponentInChildren<Button>(true);
        if (button == null)
            return;
        var rt = button.GetComponent<RectTransform>();
        if (rt == null || rt.rect.width < 40f || rt.rect.height > 40f || rt.rect.height < 16f)
            return;
        Plate(button, RestlessUi.ButtonCopy(button, fallback), lit,
            new Vector2(Mathf.Min(rt.rect.width, 200f), 30f));
    }

    private static void DressTabs(BuildUi ui)
    {
        if (ui.m_tabContainer == null)
            return;
        foreach (var button in ui.m_tabContainer.GetComponentsInChildren<Button>(true))
        {
            if (!button.gameObject.activeInHierarchy)
                continue;
            if (button.GetComponentInParent<BuildUiPieceButton>() != null)
                continue;
            if (button.GetComponentInParent<BuildUiTagButton>() != null)
                continue;
            if (HintHost(button.transform))
                continue;
            var rt = button.GetComponent<RectTransform>();
            if (rt == null || rt.rect.height > 40f || rt.rect.width < 40f)
                continue;
            var selected = button.transform.Find("Selected");
            var lit = selected != null && selected.gameObject.activeInHierarchy;
            Plate(button, RestlessUi.ButtonCopy(button, button.gameObject.name), lit,
                new Vector2(Mathf.Min(rt.rect.width, 260f), 36f));
        }
    }

    private static void DressSearch(BuildUi ui)
    {
        var field = ui.m_searchField as Component;
        if (field == null)
            return;
        var bar = field.transform;
        for (var t = field.transform; t != null; t = t.parent)
        {
            if (t.name.ToLowerInvariant().Contains("search"))
            {
                bar = t;
                break;
            }
        }

        foreach (var image in bar.GetComponentsInChildren<Image>(true))
        {
            if (image.transform.name.StartsWith("Restless"))
                continue;
            if (image.GetComponent<TMP_InputField>() != null || image.GetComponent<InputField>() != null)
            {
                image.color = Color.clear;
                image.raycastTarget = true;
                image.enabled = true;
                continue;
            }

            if (HintHost(image.transform))
            {
                Hide(image);
                continue;
            }

            var n = image.gameObject.name.ToLowerInvariant();
            if (n.Contains("icon"))
                continue;
            Hide(image);
        }

        foreach (var tmp in bar.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp.transform.name.StartsWith("Restless"))
                continue;
            if (tmp.GetComponentInParent<TMP_InputField>()?.textComponent == tmp)
            {
                tmp.color = RestlessUi.Text;
                continue;
            }

            MuteTmp(tmp);
        }

        var plate = bar.Find("RestlessSearch");
        if (plate == null)
        {
            var go = RestlessUi.Chip(bar, "RestlessSearch");
            go.GetComponent<Image>().raycastTarget = false;
            Ours.Add(go);
            plate = go.transform;
            plate.SetAsFirstSibling();
        }

        var host = bar.GetComponent<RectTransform>();
        if (host != null)
            RestlessUi.Pin(plate.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(Mathf.Max(host.rect.width, 154f), 36f));

        var input = bar.GetComponentInChildren<TMP_InputField>(true);
        var typed = input != null && !string.IsNullOrEmpty(input.text);
        var ph = bar.Find("RestlessSearchFace")?.GetComponent<Text>();
        if (ph == null)
        {
            ph = RestlessUi.Label(bar, "Filter", RestlessUi.HudSize, RestlessUi.Muted,
                TextAnchor.MiddleLeft);
            ph.gameObject.name = "RestlessSearchFace";
            Ours.Add(ph.gameObject);
        }

        if (host != null)
            RestlessUi.Pin(ph.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(36f, 1f), new Vector2(host.rect.width - 44f, 24f));
        ph.alignment = TextAnchor.MiddleLeft;
        ph.verticalOverflow = VerticalWrapMode.Overflow;
        ph.gameObject.SetActive(!typed);
    }

    private static void DressKeys(BuildUi ui)
    {
        var root = ui.transform;
        foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (!HintHost(tmp.transform) || !tmp.gameObject.activeInHierarchy)
                continue;
            if (tmp.transform.name.StartsWith("Restless"))
                continue;
            var letter = RestlessUi.Bare(tmp.text);
            if (letter.Length == 0 || letter.Length > 3)
                continue;
            var host = tmp.transform.parent;
            if (host == null)
                continue;
            foreach (var image in host.GetComponentsInChildren<Image>(true))
            {
                if (!image.transform.name.StartsWith("Restless"))
                    Hide(image);
            }

            MuteTmp(tmp);
            KeyPlate(host, letter);
        }
    }

    private static void KeyPlate(Transform host, string letter)
    {
        var chip = host.Find("RestlessKey");
        if (chip == null)
        {
            var go = RestlessUi.Chip(host, "RestlessKey");
            var face = RestlessUi.Label(go.transform, letter, RestlessUi.HudSize, RestlessUi.Text,
                TextAnchor.MiddleCenter);
            face.gameObject.name = "RestlessKeyFace";
            face.verticalOverflow = VerticalWrapMode.Overflow;
            RestlessUi.Stretch(face.gameObject, Vector2.zero, Vector2.one, new Vector2(4f, 6f),
                new Vector2(-4f, -2f));
            chip = go.transform;
            Ours.Add(go);
        }

        var from = host.GetComponent<RectTransform>();
        var w = from != null && from.rect.width > 8f ? Mathf.Max(from.rect.width, 28f) : 32f;
        var h = from != null && from.rect.height > 8f ? Mathf.Max(from.rect.height, 28f) : 28f;
        RestlessUi.Pin(chip.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(w, h));
        var label = chip.Find("RestlessKeyFace")?.GetComponent<Text>();
        if (label != null)
        {
            label.text = letter;
            label.alignment = TextAnchor.MiddleCenter;
        }
    }

    private static bool HintHost(Transform node)
    {
        for (var t = node; t != null; t = t.parent)
        {
            var n = t.name.ToLowerInvariant();
            if (n.Contains("hint") || n.Contains("gamepad") || n.Contains("inputhelp"))
                return true;
        }

        return false;
    }

    // Chip + Averia. No LayoutElement — ChipButton's hold blows the tab row up.
    private static void Plate(Button button, string title, bool lit, Vector2 size)
    {
        var image = button.GetComponent<Image>();
        if (image != null)
            Hide(image);
        foreach (var child in button.GetComponentsInChildren<Image>(true))
        {
            if (child.transform.name.StartsWith("Restless"))
                continue;
            var n = child.gameObject.name.ToLowerInvariant();
            if (n.Contains("selected") || n.Contains("hover") || n is "bkg" or "background")
                Hide(child);
        }

        foreach (var tmp in button.GetComponentsInChildren<TMP_Text>(true))
        {
            if (HintHost(tmp.transform))
                continue;
            MuteTmp(tmp);
        }

        var chip = button.transform.Find("RestlessChip");
        if (chip == null)
        {
            var go = RestlessUi.Chip(button.transform, "RestlessChip");
            var label = RestlessUi.Label(go.transform, title, RestlessUi.HudSize, RestlessUi.Text,
                TextAnchor.MiddleCenter);
            label.gameObject.name = "RestlessChipFace";
            label.verticalOverflow = VerticalWrapMode.Overflow;
            RestlessUi.Stretch(label.gameObject, Vector2.zero, Vector2.one, new Vector2(10f, 8f),
                new Vector2(-10f, -2f));
            chip = go.transform;
            Ours.Add(go);
        }

        RestlessUi.Pin(chip.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        RestlessUi.PaintSlot(chip.gameObject, lit);
        var face = chip.Find("RestlessChipFace")?.GetComponent<Text>();
        if (face != null)
        {
            face.text = title;
            face.alignment = TextAnchor.MiddleCenter;
            face.verticalOverflow = VerticalWrapMode.Overflow;
            face.color = lit ? RestlessUi.Accent : RestlessUi.Text;
        }
    }

    private static void MuteTmp(TMP_Text tmp)
    {
        RestlessUi.SilenceTmp(tmp);
        Silenced.Add(tmp.gameObject);
    }

    private static void Hide(Image image)
    {
        if (image == null)
            return;
        if (!Hidden.Contains(image))
            Hidden.Add(image);
        image.enabled = false;
    }

    private static void TearDown()
    {
        Undress();
        _dumped = false;
    }

    private static void Undress()
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
        foreach (var go in Silenced)
        {
            if (go == null)
                continue;
            var tmp = go.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.enabled = true;
                tmp.maxVisibleCharacters = int.MaxValue;
            }
        }

        Silenced.Clear();
        _dressed = false;
    }
}
