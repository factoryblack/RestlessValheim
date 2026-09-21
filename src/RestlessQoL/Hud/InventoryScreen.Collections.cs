using System.Collections.Generic;
using RestlessQoL.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

public sealed partial class InventoryScreen
{
    private static void RefreshCollections(InventoryGui gui)
    {
        var texts = gui.m_textsDialog;
        if (texts != null && texts.gameObject.activeInHierarchy)
        {
            CollectionShell(texts.transform);
            if (texts.m_listRoot != null)
                foreach (var row in texts.m_listRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (!IsListRow(row, texts.m_listRoot, texts.m_elementPrefab)) continue;
                    var plate = row.Find("RestlessSlot");
                    if (plate == null) continue;
                    var selected = row.Find("selected");
                    var lit = selected != null && selected.gameObject.activeSelf;
                    RestlessUi.PaperControl(plate.gameObject, lit ? RestlessUi.Accent : (Color?)null);
                    var face = plate.Find("label")?.GetComponent<Text>();
                    if (face != null)
                    {
                        face.color = lit ? RestlessUi.Accent : RestlessUi.Text;
                        RestlessUi.BoundedLabel(face, RestlessUi.BodySize, RestlessUi.HintSize);
                        RestlessUi.Stretch(face.gameObject, Vector2.zero, Vector2.one,
                            new Vector2(16f, 6f), new Vector2(-16f, -6f));
                    }
                    var button = row.GetComponent<Button>();
                    if (button != null) BindCraftControl(button, plate.GetComponent<Image>(), face, lit);
                }
            var area = RestlessUi.Deep(texts.transform, "TextArea");
            var paper = area != null ? area.Find("RestlessRead") : null;
            if (paper != null) RestlessUi.PaperSurface(paper.gameObject);
            ReadableParagraph(texts.m_textArea);
        }
        if (gui.m_trophiesPanel != null && gui.m_trophiesPanel.activeInHierarchy)
        {
            CollectionShell(gui.m_trophiesPanel.transform);
            CollectionCards(gui.m_trophieListRoot, "TrophyElement");
        }
        var achievements = gui.m_achievementsPanel;
        if (achievements != null && achievements.gameObject.activeInHierarchy)
        {
            CollectionShell(achievements.transform, achievements.m_achievementDetails != null
                ? achievements.m_achievementDetails.transform : null);
            CollectionCards(gui.m_achievementsListRoot, "AchElement");
            if (achievements.m_achievementDetails != null && achievements.m_achievementDetails.activeInHierarchy)
            {
                CollectionShell(achievements.m_achievementDetails.transform);
                var list = achievements.m_achievementDetailsListRoot;
                if (list != null)
                    foreach (Transform row in list)
                    {
                        var plate = row.Find("RestlessSlot");
                        if (plate == null) continue;
                        RestlessUi.PaperControl(plate.gameObject);
                        foreach (var face in row.GetComponentsInChildren<Text>(true))
                            if (face.name.StartsWith("Restless") || face.transform.parent == plate)
                                RestlessUi.BoundedLabel(face, RestlessUi.BodySize, RestlessUi.HintSize);
                    }
            }
        }
    }

    // Decoration follows stationary viewports and controls, never scrolling content.
    private static void CollectionShell(Transform root, Transform? exclude = null)
    {
        var parts = new List<RectTransform>();
        foreach (var scroll in root.GetComponentsInChildren<ScrollRect>(true))
            if (scroll.gameObject.activeInHierarchy && (exclude == null || !scroll.transform.IsChildOf(exclude)))
                AddRt(parts, scroll.viewport != null ? scroll.viewport : scroll.transform);
        var hasViewport = parts.Count > 0;
        foreach (var text in root.GetComponentsInChildren<Text>(true))
        {
            var title = text.name.EndsWith("Topic");
            var summary = text.name is "Restless_achRate" or "Restless_achCheat";
            if (!text.gameObject.activeInHierarchy || !(title || summary)
                || exclude != null && text.transform.IsChildOf(exclude)) continue;
            AddRt(parts, text.transform);
            RestlessUi.BoundedLabel(text, title ? RestlessUi.TitleSize : RestlessUi.HintSize,
                title ? RestlessUi.BodySize : RestlessUi.HudMeta);
            var old = text.transform.parent.Find("RestlessModalTitle");
            if (old != null) old.gameObject.SetActive(false);
        }
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            if (!button.gameObject.activeInHierarchy || button.name.IndexOf("close", System.StringComparison.OrdinalIgnoreCase) < 0
                || exclude != null && button.transform.IsChildOf(exclude)) continue;
            AddRt(parts, button.transform);
            var chip = button.transform.Find("RestlessChip");
            if (chip == null) continue;
            RestlessUi.PaperControl(chip.gameObject);
            var face = chip.GetComponentInChildren<Text>(true);
            if (face != null) RestlessUi.BoundedLabel(face, RestlessUi.BodySize, RestlessUi.HintSize);
            BindCraftControl(button, chip.GetComponent<Image>(), face, false);
        }
        if (!hasViewport || !Union(parts, out var x0, out var y0, out var x1, out var y1)) return;
        var panel = EnsureStrip(root, "RestlessCollectionPaper");
        Place(panel.GetComponent<RectTransform>(), x0, y0, x1, y1, 20f, 16f, false);
        RestlessUi.InventorySurface(panel.gameObject);
    }

    private static void CollectionCards(RectTransform? root, string prefix)
    {
        if (root == null) return;
        foreach (var cell in root.GetComponentsInChildren<Transform>(true))
        {
            if (!cell.gameObject.activeInHierarchy || !cell.name.StartsWith(prefix)) continue;
            var icon = RestlessUi.Deep<Image>(cell, "icon");
            var host = icon != null && icon.transform.parent != null ? icon.transform.parent : cell;
            var plate = host.Find("RestlessSlot");
            if (plate != null) RestlessUi.PaperControl(plate.gameObject);
            foreach (var face in cell.GetComponentsInChildren<Text>(true))
            {
                if (face.name == "Restless_cardName")
                    RestlessUi.BoundedLabel(face, RestlessUi.BodySize, RestlessUi.HudSize);
                else if (face.name.StartsWith("Restless_card"))
                    RestlessUi.BoundedLabel(face, RestlessUi.HintSize, RestlessUi.HudMeta);
            }
            // Native icon tint/status images still communicate locked/completed state.
            var button = cell.GetComponent<Button>();
            if (button != null && plate != null)
                BindCraftControl(button, plate.GetComponent<Image>(), null, false);
        }
    }

    private static void ReadableParagraph(TMP_Text? source)
    {
        if (source == null || !source.gameObject.activeInHierarchy) return;
        Text? face = null;
        foreach (var readout in Readouts)
            if (readout.Src == source) { face = readout.Face; break; }
        if (face == null) return;
        var scroll = source.GetComponentInParent<ScrollRect>();
        // Only take ownership of a dedicated paragraph content rectangle.
        // Other prefab layouts retain their native geometry as a safe fallback.
        if (scroll == null || scroll.content == null || scroll.viewport == null) return;
        var direct = scroll.content == source.rectTransform;
        if (!direct)
        {
            if (source.transform.parent != scroll.content) return;
            foreach (Transform child in scroll.content)
                if (child != source.transform && child != face.transform) return;
            var group = scroll.content.GetComponent<LayoutGroup>();
            if (group != null && group.enabled) { Hidden.Add(group); group.enabled = false; }
        }
        RememberCraftRect(scroll.content);
        var fitter = scroll.content.GetComponent<ContentSizeFitter>();
        if (fitter != null && fitter.enabled) { Hidden.Add(fitter); fitter.enabled = false; }
        var width = Mathf.Max(80f, scroll.content.rect.width - 40f);
        face.fontSize = RestlessUi.BodySize;
        face.horizontalOverflow = HorizontalWrapMode.Wrap;
        face.verticalOverflow = VerticalWrapMode.Overflow;
        face.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        var height = Mathf.Max(RestlessUi.BodySize + 4f, face.preferredHeight);
        scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            Mathf.Max(scroll.viewport.rect.height, height + 40f));
        if (!direct)
        {
            RestlessUi.Pin(face.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -20f), new Vector2(width, height));
            return;
        }
        // The mirror is a sibling of the source; copy its scroll position, then inset.
        RestlessUi.CopyRect(face.rectTransform, source.rectTransform);
        face.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        face.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        var corners = new Vector3[4];
        source.rectTransform.GetWorldCorners(corners);
        face.rectTransform.pivot = new Vector2(0f, 1f);
        face.rectTransform.position = corners[1] + new Vector3(20f * source.transform.lossyScale.x,
            -20f * source.transform.lossyScale.y, 0f);
    }
}
