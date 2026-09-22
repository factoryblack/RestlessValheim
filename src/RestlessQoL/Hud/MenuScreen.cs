using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Jotunn.Managers;
using RestlessQoL.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

// ESC pause column plus logout / quit confirms. Dress in place: keep the
// fullscreen darken and native actions; shared paper surfaces and Averia copy.
public sealed class MenuScreen : FeatureModule
{
    public override string Id => "ui.menu";
    public override bool Enabled => true;

    private static readonly List<GameObject> Ours = new();
    private static readonly List<GameObject> Shelved = new();
    private static readonly List<Behaviour> Hidden = new();
    private static readonly Dictionary<Image, (Color color, bool enabled, bool raycast)> Ghosted = new();
    private static readonly Dictionary<TMP_Text, (bool enabled, float alpha, int visible, bool raycast)> CopyState = new();
    private static readonly Dictionary<Button, (Graphic graphic, ColorBlock colors, Selectable.Transition transition)> Controls = new();
    private static readonly Dictionary<Button, (bool added, float minW, float prefW, float minH, float prefH, float flexW, float flexH, bool ignore)> Holds = new();
    private static readonly List<RestlessControlFeedback> Feedback = new();
    private static readonly List<Readout> Readouts = new();
    private static bool _dressed;
    private static bool _dumped;

    private sealed class Readout
    {
        public TMP_Text? Src;
        public Text Face = null!;
        public bool Wrap;
    }

    protected override void OnLoaded()
    {
        GUIManager.OnCustomGUIAvailable += TearDown;
    }

    public override void Tick()
    {
        if (ModConfig.MenuScreenEnabled.Value)
            return;
        var menu = Menu.instance;
        if (menu != null && _dressed)
            Undress();
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Menu), nameof(Menu.Show))]
        private static void AfterShow(Menu __instance)
        {
            DumpOnce(__instance);
            if (ModConfig.MenuScreenEnabled.Value)
                Dress(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Menu), nameof(Menu.Update))]
        private static void AfterUpdate(Menu __instance)
        {
            if (!ModConfig.MenuScreenEnabled.Value)
            {
                if (_dressed)
                    Undress();
                return;
            }

            if (!Menu.IsVisible() || __instance.m_root == null
                || !__instance.m_root.gameObject.activeInHierarchy)
                return;

            Dress(__instance);
        }
    }

    private static void DumpOnce(Menu menu)
    {
        if (_dumped || !ModConfig.JotunnDebug.Value || menu.m_root == null)
            return;
        _dumped = true;
        var root = menu.m_root;
        var sb = new StringBuilder();
        sb.AppendLine("ui.menu dump");
        foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            if (UnderSkip(graphic.transform, menu))
                continue;
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

    private static void Dress(Menu menu)
    {
        DropLegacy(menu, menu.m_root);
        DropLegacy(menu, menu.m_menuDialog);
        HideOrnaments(menu, menu.m_root);
        HideOrnaments(menu, menu.m_menuDialog);
        Quiet(menu, menu.m_root);
        Quiet(menu, menu.m_menuDialog);
        Quiet(menu, menu.m_logoutDialog);
        Quiet(menu, menu.m_quitDialog);
        DimCopy(menu, menu.m_root);
        DimCopy(menu, menu.m_menuDialog);
        DressButton(menu.m_continueButton, "Continue");
        DressButton(menu.m_skipButton, "Skip");
        DressButton(menu.m_saveButton, "Save");
        DressButton(menu.m_playerListButton, "Players");
        DressButton(menu.m_inviteButton, "Invite");
        DressButton(menu.m_settingsButton, "Settings");
        DressButton(menu.m_logoutButton, "Logout");
        DressButton(menu.m_quitButton, "Exit");
        var restless = menu.m_settingsButton != null
            ? menu.m_settingsButton.transform.parent.Find("RestlessCoreMenuButton")
            : null;
        if (restless != null)
            DressButton(restless.GetComponent<Button>(), "Restless");
        var column = new List<RectTransform>();
        foreach (var button in Buttons(menu.m_menuDialog))
        {
            if (UnderSkip(button.transform, menu) || UnderConfirm(button.transform, menu)) continue;
            DressButton(button, button.gameObject.name);
            var rect = button.GetComponent<RectTransform>();
            if (rect != null && rect.rect.width < 800f && rect.rect.height < 800f)
                column.Add(rect);
        }
        if (menu.m_menuDialog != null)
        {
            foreach (var tmp in menu.m_menuDialog.GetComponentsInChildren<TMP_Text>(true))
            {
                if (UnderSkip(tmp.transform, menu) || UnderConfirm(tmp.transform, menu)
                    || RestlessUi.Owned(tmp.transform) || tmp.GetComponentInParent<Button>() != null)
                    continue;
                Face(tmp, "menu" + tmp.GetInstanceID(), RestlessUi.Muted, RestlessUi.HintSize + 2);
                column.Add(tmp.rectTransform);
            }
            Hug(menu.m_menuDialog, "RestlessMenuPaper", column, 24f, 24f);
        }
        DressConfirm(menu.m_logoutDialog, "logout");
        DressConfirm(menu.m_quitDialog, "quit");
        foreach (var row in Readouts)
            Paint(row);
        _dressed = true;
    }

    private static void DressConfirm(Transform? root, string tag)
    {
        if (root == null || !root.gameObject.activeInHierarchy)
            return;
        Quiet(Menu.instance, root);
        foreach (var button in Buttons(root))
            DressButton(button, button.gameObject.name);

        var parts = new List<RectTransform>();
        foreach (var button in Buttons(root))
        {
            var rect = button.GetComponent<RectTransform>();
            if (rect != null && rect.rect.width < 800f && rect.rect.height < 800f)
                parts.Add(rect);
        }
        // Each live source is mirrored once; a single question is body copy,
        // not both a guessed title and the body of the confirmation.
        foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (!tmp.gameObject.activeInHierarchy || tmp.transform.name.StartsWith("Restless")
                || tmp.GetComponentInParent<Button>() != null) continue;
            var n = tmp.gameObject.name.ToLowerInvariant();
            var heading = n is "topic" or "title" or "header" || n.Contains("topic");
            Face(tmp, tag + tmp.GetInstanceID(), RestlessUi.Text,
                heading ? RestlessUi.TitleSize : RestlessUi.BodySize + 2, true);
            parts.Add(tmp.rectTransform);
        }
        Hug(root, "RestlessConfirmPaper", parts, 28f, 24f);
    }

    private static IEnumerable<Button> Buttons(Transform? root)
    {
        if (root == null)
            yield break;
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            if (button.gameObject.activeInHierarchy)
                yield return button;
        }
    }

    private static void DressButton(Button? button, string fallback)
    {
        if (button == null || !button.gameObject.activeInHierarchy)
            return;
        var rt = button.GetComponent<RectTransform>();
        if (rt != null && (rt.rect.width > 800f || rt.rect.height > 800f))
        {
            var big = button.GetComponent<Image>();
            if (big != null)
                Ghost(big);
            return;
        }

        var title = RestlessUi.ButtonCopy(button, fallback);
        if (!Controls.ContainsKey(button))
            Controls.Add(button, (button.targetGraphic, button.colors, button.transition));
        var image = button.GetComponent<Image>();
        if (image != null) Ghost(image);
        foreach (var child in button.GetComponentsInChildren<Image>(true))
        {
            if (child == image || child.transform.name.StartsWith("Restless")) continue;
            var n = child.gameObject.name.ToLowerInvariant();
            if (n.Contains("selected") || n.Contains("glow") || n is "bkg" or "background") Hide(child);
        }
        foreach (var tmp in button.GetComponentsInChildren<TMP_Text>(true)) Dim(tmp);
        foreach (var text in button.GetComponentsInChildren<Text>(true))
        {
            if (RestlessUi.Owned(text.transform) || !text.enabled) continue;
            Hidden.Add(text);
            text.enabled = false;
        }
        Hold(button);
        var chip = button.transform.Find("RestlessMenuButton");
        if (chip == null)
        {
            var go = RestlessUi.Chip(button.transform, "RestlessMenuButton");
            Ours.Add(go);
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            RestlessUi.Stretch(go, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RestlessUi.PaperControl(go);
            var face = RestlessUi.Label(go.transform, title, RestlessUi.BodySize + 2,
                RestlessUi.Text, TextAnchor.MiddleCenter);
            face.name = "RestlessMenuFace";
            RestlessUi.Stretch(face.gameObject, Vector2.zero, Vector2.one,
                new Vector2(14f, 3f), new Vector2(-14f, -3f));
            RestlessUi.BoundedLabel(face, RestlessUi.BodySize + 2, RestlessUi.HintSize);
            chip = go.transform;
            var existingFeedback = button.GetComponent<RestlessControlFeedback>();
            RestlessUi.PaperSelectable(button);
            if (existingFeedback == null) Feedback.Add(button.GetComponent<RestlessControlFeedback>());
        }
        var label = chip.GetComponentInChildren<Text>(true);
        if (label != null)
        {
            label.enabled = true;
            label.text = title.Length > 0 ? title : fallback;
            label.color = button.IsInteractable() ? RestlessUi.Text : RestlessUi.PaperMuted;
            var cr = label.GetComponent<CanvasRenderer>();
            if (cr != null) cr.SetAlpha(1f);
        }
        button.targetGraphic = chip.GetComponent<Image>();
    }

    private static void Hold(Button button)
    {
        var hold = button.GetComponent<LayoutElement>();
        if (!Holds.ContainsKey(button))
        {
            if (hold == null)
            {
                Holds.Add(button, (true, 0f, 0f, 0f, 0f, 0f, 0f, false));
                hold = button.gameObject.AddComponent<LayoutElement>();
            }
            else
            {
                Holds.Add(button, (false, hold.minWidth, hold.preferredWidth, hold.minHeight,
                    hold.preferredHeight, hold.flexibleWidth, hold.flexibleHeight, hold.ignoreLayout));
            }
        }
        else if (hold == null)
            hold = button.gameObject.AddComponent<LayoutElement>();
        var pin = RestlessUi.ButtonSize(button);
        hold.minWidth = hold.preferredWidth = pin.x;
        hold.minHeight = hold.preferredHeight = pin.y;
    }

    private static void Quiet(Menu menu, Transform? root)
    {
        if (root == null)
            return;
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.transform.name.StartsWith("Restless") || UnderSkip(image.transform, menu))
                continue;
            var n = image.gameObject.name.ToLowerInvariant();
            if (n is "darken")
                continue;
            if (n.Contains("icon"))
                continue;
            if (image.GetComponent<Button>() != null)
                continue;
            if (n.Contains("knot") || n is "bkg" or "background" or "panel-back" or "blur"
                or "border" or "sunken" or "ornament"
                || n.Contains("bkg") || n.Contains("background") || n.Contains("border")
                || n.Contains("wood") || n.Contains("ornament") || n.Contains("frame"))
                Hide(image);
        }

        foreach (var raw in root.GetComponentsInChildren<RawImage>(true))
        {
            if (raw.transform.name.StartsWith("Restless") || UnderSkip(raw.transform, menu))
                continue;
            var n = raw.gameObject.name.ToLowerInvariant();
            if (n.Contains("knot") || n.Contains("ornament") || n.Contains("border") || n is "blur")
            {
                if (raw.enabled) Hidden.Add(raw);
                raw.enabled = false;
            }
        }
    }

    // Dim only. MenuEntries sizes each row from the live TMP and knot
    // children; deactivating them lets the layout rebuild to zero height
    // and clips the Averia after a frame.
    private static void DimCopy(Menu menu, Transform? root)
    {
        if (root == null)
            return;
        foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (RestlessUi.Owned(tmp.transform) || UnderSkip(tmp.transform, menu)
                || UnderConfirm(tmp.transform, menu))
                continue;
            Dim(tmp);
        }
    }

    private static void HideOrnaments(Menu menu, Transform? root)
    {
        if (root == null)
            return;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == root || UnderSkip(t, menu) || UnderConfirm(t, menu))
                continue;
            var n = t.name.ToLowerInvariant();
            if (n.Contains("knot") && t.GetComponentInParent<Button>() != null)
                continue;
            if (n is "ornament")
                Shelf(t.gameObject);
        }
    }

    private static void DropLegacy(Menu menu, Transform? root)
    {
        if (root == null)
            return;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (UnderSkip(t, menu) || UnderConfirm(t, menu))
                continue;
            if (t.name == "OLD_menu" || t.name.StartsWith("Button_"))
                Shelf(t.gameObject);
        }
    }

    private static void Shelf(GameObject go)
    {
        if (go == null || !go.activeSelf)
            return;
        Shelved.Add(go);
        go.SetActive(false);
    }

    private static bool UnderConfirm(Transform node, Menu menu)
    {
        if (menu.m_logoutDialog != null && node.IsChildOf(menu.m_logoutDialog))
            return true;
        if (menu.m_quitDialog != null && node.IsChildOf(menu.m_quitDialog))
            return true;
        return false;
    }

    private static bool UnderSkip(Transform node, Menu menu)
    {
        if (menu.m_settingsInstance != null && node.IsChildOf(menu.m_settingsInstance.transform))
            return true;
        if (menu.m_currentPlayersInstance != null && node.IsChildOf(menu.m_currentPlayersInstance.transform))
            return true;
        if (menu.m_cloudStorageWarning != null && node.IsChildOf(menu.m_cloudStorageWarning.transform))
            return true;
        if (menu.m_cloudStorageWarningNextSave != null
            && node.IsChildOf(menu.m_cloudStorageWarningNextSave.transform))
            return true;
        return false;
    }

    private static void Face(TMP_Text? src, string tag, Color color, int size, bool wrap = false)
    {
        if (src == null)
            return;
        Dim(src);
        foreach (var row in Readouts)
        {
            if (row.Src != src)
                continue;
            row.Face.color = color;
            row.Face.fontSize = size;
            row.Wrap = wrap;
            return;
        }

        var host = src.transform.parent;
        if (host == null)
            return;
        var existing = host.Find("Restless_" + tag);
        Text face;
        if (existing != null)
            face = existing.GetComponent<Text>();
        else
        {
            face = RestlessUi.Label(host, "", size, color,
                wrap ? TextAnchor.UpperCenter : TextAnchor.MiddleCenter);
            face.gameObject.name = "Restless_" + tag;
            Ours.Add(face.gameObject);
            face.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            RestlessUi.CopyRect(face.rectTransform, src.rectTransform);
            face.rectTransform.SetSiblingIndex(src.transform.GetSiblingIndex() + 1);
        }

        face.color = color;
        face.fontSize = size;
        Readouts.Add(new Readout { Src = src, Face = face, Wrap = wrap });
    }

    private static void Paint(Readout row)
    {
        if (row.Face == null)
            return;
        if (row.Src == null || !row.Src.gameObject.activeInHierarchy)
        {
            row.Face.gameObject.SetActive(false);
            return;
        }

        row.Face.gameObject.SetActive(true);
        RestlessUi.CopyRect(row.Face.rectTransform, row.Src.rectTransform);
        if (row.Face.rectTransform.rect.height < 8f)
        {
            var rt = row.Face.rectTransform;
            rt.sizeDelta = new Vector2(Mathf.Max(rt.rect.width, 180f), 22f);
        }
        row.Face.enabled = true;
        row.Face.text = RestlessUi.Bare(row.Src.text);
        RestlessUi.BoundedLabel(row.Face, row.Face.fontSize, RestlessUi.HintSize);
        row.Face.raycastTarget = false;
        var cr = row.Face.GetComponent<CanvasRenderer>();
        if (cr != null) cr.SetAlpha(1f);
    }

    private static void Hug(Transform host, string name, List<RectTransform> parts, float padX, float padY)
    {
        if (!Union(parts, out var minX, out var minY, out var maxX, out var maxY))
            return;
        var plate = host.Find(name);
        if (plate == null)
        {
            var go = RestlessUi.Graphic(host, name, Color.white, false);
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            RestlessUi.PaperSurface(go);
            Ours.Add(go);
            plate = go.transform;
        }

        plate.SetAsFirstSibling();
        var rt = plate.GetComponent<RectTransform>();
        var z = rt.position.z;
        var scale = rt.lossyScale;
        var sx = Mathf.Abs(scale.x) < 0.01f ? 1f : Mathf.Abs(scale.x);
        var sy = Mathf.Abs(scale.y) < 0.01f ? 1f : Mathf.Abs(scale.y);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.position = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, z);
        rt.sizeDelta = new Vector2((maxX - minX) / sx + padX * 2f, (maxY - minY) / sy + padY * 2f);
        var img = rt.GetComponent<Image>();
        img.raycastTarget = false;
        img.color = Color.white;
    }

    private static bool Union(List<RectTransform> parts, out float minX, out float minY, out float maxX,
        out float maxY)
    {
        minX = float.PositiveInfinity;
        minY = float.PositiveInfinity;
        maxX = float.NegativeInfinity;
        maxY = float.NegativeInfinity;
        var box = new Vector3[4];
        foreach (var rt in parts)
        {
            if (rt == null || !rt.gameObject.activeInHierarchy)
                continue;
            rt.GetWorldCorners(box);
            minX = Mathf.Min(minX, box[0].x);
            minY = Mathf.Min(minY, box[0].y);
            maxX = Mathf.Max(maxX, box[2].x);
            maxY = Mathf.Max(maxY, box[1].y);
        }

        return !float.IsInfinity(minX) && maxX > minX;
    }

    private static void Dim(TMP_Text tmp)
    {
        if (RestlessUi.Owned(tmp.transform))
            return;
        if (!CopyState.ContainsKey(tmp))
            CopyState.Add(tmp, (tmp.enabled, tmp.alpha, tmp.maxVisibleCharacters, tmp.raycastTarget));
        tmp.alpha = 0f;
        var color = tmp.color;
        color.a = 0f;
        tmp.color = color;
        tmp.raycastTarget = false;
        var cr = tmp.GetComponent<CanvasRenderer>();
        if (cr != null)
            cr.SetAlpha(0f);
    }

    private static void Hide(Image image)
    {
        if (image == null || !image.enabled)
            return;
        Hidden.Add(image);
        image.enabled = false;
    }

    private static void Ghost(Image image)
    {
        if (image == null)
            return;
        if (!Ghosted.ContainsKey(image))
            Ghosted[image] = (image.color, image.enabled, image.raycastTarget);
        image.color = Color.clear;
        image.raycastTarget = true;
        image.enabled = true;
    }

    private static void Undress()
    {
        foreach (var feedback in Feedback)
            if (feedback != null) Object.Destroy(feedback);
        Feedback.Clear();
        foreach (var pair in Controls)
        {
            if (pair.Key == null) continue;
            pair.Key.targetGraphic = pair.Value.graphic;
            pair.Key.colors = pair.Value.colors;
            pair.Key.transition = pair.Value.transition;
        }
        Controls.Clear();
        foreach (var pair in Holds)
        {
            if (pair.Key == null) continue;
            var hold = pair.Key.GetComponent<LayoutElement>();
            if (hold == null) continue;
            if (pair.Value.added)
                Object.Destroy(hold);
            else
            {
                hold.minWidth = pair.Value.minW;
                hold.preferredWidth = pair.Value.prefW;
                hold.minHeight = pair.Value.minH;
                hold.preferredHeight = pair.Value.prefH;
                hold.flexibleWidth = pair.Value.flexW;
                hold.flexibleHeight = pair.Value.flexH;
                hold.ignoreLayout = pair.Value.ignore;
            }
        }
        Holds.Clear();
        foreach (var go in Ours)
        {
            if (go != null)
                Object.Destroy(go);
        }

        Ours.Clear();
        foreach (var go in Shelved)
        {
            if (go != null)
                go.SetActive(true);
        }

        Shelved.Clear();
        foreach (var behaviour in Hidden)
        {
            if (behaviour != null)
                behaviour.enabled = true;
        }

        Hidden.Clear();
        foreach (var pair in Ghosted)
        {
            if (pair.Key == null) continue;
            pair.Key.color = pair.Value.color;
            pair.Key.enabled = pair.Value.enabled;
            pair.Key.raycastTarget = pair.Value.raycast;
        }

        Ghosted.Clear();
        foreach (var pair in CopyState)
        {
            if (pair.Key == null) continue;
            pair.Key.enabled = pair.Value.enabled;
            pair.Key.alpha = pair.Value.alpha;
            pair.Key.maxVisibleCharacters = pair.Value.visible;
            pair.Key.raycastTarget = pair.Value.raycast;
        }
        CopyState.Clear();
        Readouts.Clear();
        _dressed = false;
    }

    private static void TearDown() => Undress();
}
