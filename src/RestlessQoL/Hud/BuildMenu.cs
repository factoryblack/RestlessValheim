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
public sealed partial class BuildMenu : FeatureModule
{
    public override string Id => "ui.build";
    public override bool Enabled => true;

    private static readonly List<GameObject> Ours = new();
    private static readonly List<Behaviour> Hidden = new();
    private static readonly HashSet<GameObject> Silenced = new();
    private static readonly HashSet<Object> Remembered = new();
    private static readonly List<System.Action> Restore = new();
    private static readonly List<RestlessControlFeedback> Feedback = new();
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
            {
                ClearHover();
                return;
            }

            DumpOnce(__instance);
            if (!_dressed)
            {
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
        DressScroll(root);
        var ui = hud.m_buildUi;
        if (ui == null)
            return;
        DressPieces(ui);
        DressTags(ui);
        DressTabs(ui);
        DressSearch(ui);
        DressKeys(ui);
        DressHover(hud);
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
            if (Owned(image.transform))
                continue;
            if (LeaveMask(image.transform) || image.GetComponentInParent<Scrollbar>() != null)
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

    private static void DressScroll(Transform? root)
    {
        if (root == null) return;
        foreach (var scroll in root.GetComponentsInChildren<ScrollRect>(true))
        {
            var image = scroll.GetComponent<Image>();
            if (image != null && image.GetComponent<Mask>() == null
                && image.GetComponent<RectMask2D>() == null) Hide(image);
            // Fit the viewport, never the scrolling content or fullscreen build HUD.
            var viewport = scroll.viewport;
            if (viewport == null || viewport.parent == null) continue;
            var rect = viewport.rect;
            if (rect.width < 100f || rect.height < 80f || rect.width > 1400f || rect.height > 1000f) continue;
            var name = "RestlessBuildWell" + scroll.GetInstanceID();
            var plate = viewport.parent.Find(name);
            if (plate == null)
            {
                var go = RestlessUi.Graphic(viewport.parent, name, Color.white, false);
                go.AddComponent<LayoutElement>().ignoreLayout = true;
                RestlessUi.InventorySurface(go);
                Ours.Add(go);
                plate = go.transform;
            }
            plate.gameObject.SetActive(viewport.gameObject.activeInHierarchy);
            RestlessUi.CopyRect(plate.GetComponent<RectTransform>(), viewport);
            // Keep the backing inside the viewport; its torn lip must not touch
            // neighbouring category controls or the native scrollbar.
            var plateRect = plate.GetComponent<RectTransform>();
            plateRect.offsetMin += new Vector2(3f, 3f);
            plateRect.offsetMax -= new Vector2(3f, 3f);
            var before = plate.GetSiblingIndex() < viewport.GetSiblingIndex();
            plate.SetSiblingIndex(viewport.GetSiblingIndex() - (before ? 1 : 0));
        }
        // Scrollbars retain their native art, colours, geometry and feedback.
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
        if (ui.m_pieceButtons != null)
            foreach (var piece in ui.m_pieceButtons) DressPiece(ui, piece);
        DressPiece(ui, ui.m_specialPieceButton);
    }

    private static void DressPiece(BuildUi ui, BuildUiPieceButton? piece)
    {
        if (piece == null || !piece.gameObject.activeInHierarchy) return;
        var rect = piece.GetComponent<RectTransform>();
        if (rect == null || rect.rect.width < 24f || rect.rect.height < 24f) return;
        var icon = piece.m_icon ?? RestlessUi.Deep<Image>(piece.transform, "Piece Icon");
        var selected = piece == ui.m_lastSelectedPieceBtn;
        var hovered = piece == ui.m_currentHoveredPieceButton;
        // Piece buttons are not ItemData slots: keep all counts, stars, arrows,
        // availability tint and future mod status children under native control.
        foreach (var image in piece.GetComponentsInChildren<Image>(true))
        {
            if (Owned(image.transform) || image == icon || image.GetComponent<Mask>() != null) continue;
            if (image.GetComponent<Button>() != null)
            {
                RememberImage(image);
                image.color = Color.clear;
                continue;
            }
            var name = image.name.ToLowerInvariant();
            if (name is "background" or "bkg" or "selected" or "hover" or "highlight" or "glow")
                Hide(image);
        }
        var plate = piece.transform.Find("RestlessBuildPiece");
        if (plate == null)
        {
            var go = RestlessUi.Chip(piece.transform, "RestlessBuildPiece");
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            Ours.Add(go);
            plate = go.transform;
            plate.SetAsFirstSibling();
        }
        RestlessUi.Stretch(plate.gameObject, Vector2.zero, Vector2.one,
            new Vector2(2f, 2f), new Vector2(-2f, -2f));
        RestlessUi.PaperControl(plate.gameObject,
            selected ? RestlessUi.Accent : hovered ? RestlessUi.PaperMuted : (Color?)null);
        // Existing Button and BuildUiPieceButton receive pointer/controller events.
        var button = piece.GetComponent<Button>();
        if (button != null) AddFeedback(button);
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
        if (rt == null || rt.rect.width < 40f || rt.rect.height > 80f || rt.rect.height < 16f)
            return;
        Plate(button, RestlessUi.ButtonCopy(button, fallback), lit,
            rt.rect.size);
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
            if (rt == null || rt.rect.height > 100f || rt.rect.width < 40f)
                continue;
            var selected = button.transform.Find("Selected");
            var lit = selected != null && selected.gameObject.activeInHierarchy;
            Plate(button, RestlessUi.ButtonCopy(button, button.gameObject.name), lit,
                rt.rect.size);
        }
    }

    private static void DressSearch(BuildUi ui)
    {
        var field = ui.m_searchField as Component;
        if (field == null) return;
        var rect = field.GetComponent<RectTransform>();
        if (rect == null) return;
        var background = field.GetComponent<Image>();
        if (background != null)
        {
            RememberImage(background);
            background.color = Color.clear;
        }
        var plate = field.transform.Find("RestlessSearch");
        if (plate == null)
        {
            var go = RestlessUi.Chip(field.transform, "RestlessSearch");
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            RestlessUi.PaperSurface(go, small: true);
            Ours.Add(go);
            plate = go.transform;
            plate.SetAsFirstSibling();
        }
        RestlessUi.Stretch(plate.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        // Preserve placeholder localization, caret, selection, IME and focus.
        var tmp = field.GetComponent<TMP_InputField>();
        if (tmp != null)
        {
            if (tmp.textComponent != null)
            {
                RememberTmp(tmp.textComponent);
                tmp.textComponent.color = RestlessUi.Text;
            }
            if (tmp.placeholder is TMP_Text placeholder)
            {
                RememberTmp(placeholder);
                placeholder.color = RestlessUi.PaperMuted;
            }
        }
        var legacy = field.GetComponent<InputField>();
        if (legacy != null)
        {
            if (legacy.textComponent != null) StyleInputText(legacy.textComponent, RestlessUi.Text);
            if (legacy.placeholder is Text placeholder) StyleInputText(placeholder, RestlessUi.PaperMuted);
        }
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
                if (Owned(image.transform)) continue;
                var name = image.name.ToLowerInvariant();
                if (name is "background" or "bkg" or "border") Hide(image);
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
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            RestlessUi.PaperSurface(go, small: true);
            var face = RestlessUi.Label(go.transform, letter, RestlessUi.HudSize, RestlessUi.Text,
                TextAnchor.MiddleCenter);
            face.gameObject.name = "RestlessKeyFace";
            face.verticalOverflow = VerticalWrapMode.Overflow;
            RestlessUi.Stretch(face.gameObject, Vector2.zero, Vector2.one, new Vector2(4f, 2f),
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

    // Owned child ignores layout; never add ChipButton sizing to the native tab.
    private static void Plate(Button button, string title, bool lit, Vector2 size)
    {
        var image = button.GetComponent<Image>();
        if (image != null)
            Hide(image);
        foreach (var child in button.GetComponentsInChildren<Image>(true))
        {
            if (Owned(child.transform))
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
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            var label = RestlessUi.Label(go.transform, title, RestlessUi.BodySize, RestlessUi.Text,
                TextAnchor.MiddleCenter);
            label.gameObject.name = "RestlessChipFace";
            label.verticalOverflow = VerticalWrapMode.Overflow;
            RestlessUi.Stretch(label.gameObject, Vector2.zero, Vector2.one, new Vector2(10f, 2f),
                new Vector2(-10f, -2f));
            chip = go.transform;
            Ours.Add(go);
        }

        RestlessUi.Pin(chip.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        RestlessUi.PaperControl(chip.gameObject, lit ? RestlessUi.Accent : (Color?)null);
        AddFeedback(button);
        var face = chip.Find("RestlessChipFace")?.GetComponent<Text>();
        if (face != null)
        {
            face.text = title;
            face.alignment = TextAnchor.MiddleCenter;
            RestlessUi.BoundedLabel(face, RestlessUi.BodySize, RestlessUi.HudSize);
            face.color = !button.IsInteractable() ? RestlessUi.PaperMuted : lit ? RestlessUi.Accent : RestlessUi.Text;
        }
    }

    private static void MuteTmp(TMP_Text tmp)
    {
        RememberTmp(tmp);
        RestlessUi.SilenceTmp(tmp);
        Silenced.Add(tmp.gameObject);
    }

    private static void Hide(Image image)
    {
        if (image == null)
            return;
        RememberImage(image);
        if (!Hidden.Contains(image))
            Hidden.Add(image);
        image.enabled = false;
    }

    private static bool Owned(Transform node)
    {
        for (var t = node; t != null; t = t.parent)
            if (t.name.StartsWith("Restless")) return true;
        return false;
    }

    private static void AddFeedback(Selectable control)
    {
        if (control.GetComponent<RestlessControlFeedback>() == null)
            Feedback.Add(RestlessUi.ControlFeedback(control));
    }

    private static void RememberImage(Image image)
    {
        if (!Remembered.Add(image)) return;
        var enabled = image.enabled;
        var color = image.color;
        var sprite = image.sprite;
        var type = image.type;
        Restore.Add(() =>
        {
            if (image == null) return;
            image.enabled = enabled;
            image.color = color;
            image.sprite = sprite;
            image.type = type;
        });
    }

    private static void RememberTmp(TMP_Text text)
    {
        if (!Remembered.Add(text)) return;
        var enabled = text.enabled;
        var color = text.color;
        var visible = text.maxVisibleCharacters;
        var raycast = text.raycastTarget;
        Restore.Add(() =>
        {
            if (text == null) return;
            text.enabled = enabled;
            text.color = color;
            text.maxVisibleCharacters = visible;
            text.raycastTarget = raycast;
        });
    }

    private static void StyleInputText(Text text, Color color)
    {
        if (Remembered.Add(text))
        {
            var font = text.font;
            var oldColor = text.color;
            Restore.Add(() => { if (text != null) { text.font = font; text.color = oldColor; } });
        }
        text.font = RestlessUi.Face();
        text.color = color;
    }

    private static void TearDown()
    {
        Undress();
        _dumped = false;
    }

    private static void Undress()
    {
        ClearHover();
        foreach (var go in Ours)
        {
            if (go != null)
                Object.Destroy(go);
        }

        Ours.Clear();
        foreach (var feedback in Feedback)
            if (feedback != null) Object.Destroy(feedback);
        Feedback.Clear();
        for (var i = Restore.Count - 1; i >= 0; i--) Restore[i]();
        Restore.Clear();
        Remembered.Clear();
        Hidden.Clear();
        Silenced.Clear();
        _dressed = false;
    }
}


