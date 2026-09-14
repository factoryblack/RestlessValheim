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
// fullscreen darken, quiet the wood, Chip the buttons, Averia on the copy.
public sealed class MenuScreen : FeatureModule
{
    public override string Id => "ui.menu";
    public override bool Enabled => true;

    private static readonly List<GameObject> Ours = new();
    private static readonly List<GameObject> Shelved = new();
    private static readonly List<Behaviour> Hidden = new();
    private static readonly Dictionary<Image, Color> Ghosted = new();
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
            Undress(menu);
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Menu), nameof(Menu.Show))]
        private static void AfterShow(Menu __instance)
        {
            DumpOnce(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Menu), nameof(Menu.Update))]
        private static void AfterUpdate(Menu __instance)
        {
            if (!ModConfig.MenuScreenEnabled.Value)
            {
                if (_dressed)
                    Undress(__instance);
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
        if (_dumped || menu.m_root == null)
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
        MuteCopy(menu, menu.m_root);
        MuteCopy(menu, menu.m_menuDialog);
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
        foreach (var button in Buttons(menu.m_menuDialog))
            DressButton(button, button.gameObject.name);
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

        TMP_Text? topic = null;
        TMP_Text? body = null;
        foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (!tmp.gameObject.activeInHierarchy || tmp.transform.name.StartsWith("Restless"))
                continue;
            if (tmp.GetComponentInParent<Button>() != null)
                continue;
            var n = tmp.gameObject.name.ToLowerInvariant();
            if (topic == null && (n is "topic" or "title" or "header" || n.Contains("topic")))
                topic = tmp;
            else if (body == null)
                body = tmp;
        }

        if (topic == null)
        {
            foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp.gameObject.activeInHierarchy && tmp.GetComponentInParent<Button>() == null
                    && !tmp.transform.name.StartsWith("Restless"))
                {
                    topic = tmp;
                    break;
                }
            }
        }

        if (topic != null && topic.transform.parent != null)
        {
            var parts = new List<RectTransform> { topic.rectTransform };
            Hug(topic.transform.parent, "RestlessTitle", parts, 18f, 10f);
            Face(topic, tag + "Topic", RestlessUi.Text, RestlessUi.TitleSize);
        }

        if (body != null)
            Face(body, tag + "Body", RestlessUi.Text, RestlessUi.BodySize, true);
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

        var hold = rt != null && rt.rect.width > 8f && rt.rect.height > 8f
            ? rt.rect.size
            : (Vector2?)null;

        var image = button.GetComponent<Image>();
        if (image != null)
            Ghost(image);

        foreach (var child in button.GetComponentsInChildren<Transform>(true))
        {
            var n = child.name.ToLowerInvariant();
            if (n.Contains("selected") || n.Contains("gamepad") || n.Contains("glow"))
                RestlessUi.Quiet(child.gameObject);
        }

        foreach (var child in button.GetComponentsInChildren<Image>(true))
        {
            if (child == image || child.transform.name.StartsWith("Restless"))
                continue;
            var n = child.gameObject.name.ToLowerInvariant();
            if (n.Contains("selected") || n.Contains("glow") || n is "bkg" or "background")
                Hide(child);
        }

        foreach (var tmp in button.GetComponentsInChildren<TMP_Text>(true))
            RestlessUi.SilenceTmp(tmp);
        foreach (var text in button.GetComponentsInChildren<Text>(true))
        {
            if (text.transform.name.StartsWith("Restless")
                || text.transform.parent != null && text.transform.parent.name.StartsWith("Restless"))
                continue;
            text.enabled = false;
        }

        var title = RestlessUi.ButtonCopy(button, fallback);
        var chip = RestlessUi.ChipButton(button, title, false, hold);
        if (!Ours.Contains(chip))
            Ours.Add(chip);
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
                Hidden.Add(raw);
                raw.enabled = false;
            }
        }
    }

    private static void MuteCopy(Menu menu, Transform? root)
    {
        if (root == null)
            return;
        foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp.transform.name.StartsWith("Restless") || UnderSkip(tmp.transform, menu)
                || UnderConfirm(tmp.transform, menu))
                continue;
            RestlessUi.SilenceTmp(tmp);
            Shelf(tmp.gameObject);
        }
    }

    private static void HideOrnaments(Menu menu, Transform? root)
    {
        if (root == null)
            return;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == root || UnderConfirm(t, menu))
                continue;
            var n = t.name.ToLowerInvariant();
            if (n.Contains("knot") || n is "ornament")
                Shelf(t.gameObject);
        }
    }

    private static void DropLegacy(Menu menu, Transform? root)
    {
        if (root == null)
            return;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (UnderConfirm(t, menu))
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
        Silence(src);
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
        row.Face.text = RestlessUi.Bare(row.Src.text);
        row.Face.horizontalOverflow = row.Wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
        row.Face.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private static void Hug(Transform host, string name, List<RectTransform> parts, float padX, float padY)
    {
        if (!Union(parts, out var minX, out var minY, out var maxX, out var maxY))
            return;
        var plate = host.Find(name);
        if (plate == null)
        {
            var go = RestlessUi.Strip(host, name, RestlessUi.RowTint, true);
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
        img.color = RestlessUi.RowTint;
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

    private static void Silence(TMP_Text tmp) => RestlessUi.SilenceTmp(tmp);

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
            Ghosted[image] = image.color;
        image.color = Color.clear;
        image.raycastTarget = true;
        image.enabled = true;
    }

    private static void Undress(Menu menu)
    {
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
            if (pair.Key != null)
                pair.Key.color = pair.Value;
        }

        Ghosted.Clear();
        foreach (var row in Readouts)
        {
            if (row.Src != null)
            {
                row.Src.enabled = true;
                row.Src.maxVisibleCharacters = int.MaxValue;
            }
        }

        Readouts.Clear();
        if (menu.m_root != null)
        {
            foreach (var tmp in menu.m_root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp.transform.name.StartsWith("Restless"))
                    continue;
                tmp.enabled = true;
                tmp.maxVisibleCharacters = int.MaxValue;
                tmp.raycastTarget = true;
            }
        }

        _dressed = false;
    }

    private static void TearDown()
    {
        var menu = Menu.instance;
        if (menu != null)
            Undress(menu);
        else
        {
            foreach (var go in Ours)
            {
                if (go != null)
                    Object.Destroy(go);
            }

            Ours.Clear();
            Shelved.Clear();
            Hidden.Clear();
            Ghosted.Clear();
            Readouts.Clear();
            _dressed = false;
        }
    }
}
