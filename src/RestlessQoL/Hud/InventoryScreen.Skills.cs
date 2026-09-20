using System.Collections.Generic;
using RestlessQoL.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

public sealed partial class InventoryScreen
{
    private static readonly Dictionary<Image, Color> SkillColours = new();

    private static void RefreshSkillsMaterials(InventoryGui gui)
    {
        var dialog = gui.m_skillsDialog;
        if (dialog == null || !dialog.gameObject.activeInHierarchy) return;
        var parts = new List<RectTransform>();
        var scroll = dialog.GetComponentInChildren<ScrollRect>(true);
        if (scroll != null) AddRt(parts, scroll.viewport != null ? scroll.viewport : scroll.transform);
        var topic = RestlessUi.Deep<TMP_Text>(dialog.transform, "topic");
        if (topic != null)
        {
            AddRt(parts, topic.transform);
            var title = topic.transform.parent.Find("Restless_skillsTopic")?.GetComponent<Text>();
            if (title != null) RestlessUi.BoundedLabel(title, RestlessUi.TitleSize, RestlessUi.BodySize);
            var old = topic.transform.parent.Find("RestlessModalTitle");
            if (old != null) old.gameObject.SetActive(false);
        }
        if (dialog.m_totalSkillText != null)
        {
            var host = dialog.m_totalSkillText.transform.parent;
            AddRt(parts, host);
            var totalPlate = host.Find("RestlessTotals");
            if (totalPlate != null) RestlessUi.PaperSurface(totalPlate.gameObject, small: true);
            foreach (var text in host.GetComponentsInChildren<Text>(true))
                if (text.name.StartsWith("Restless_skillsTotal"))
                    RestlessUi.BoundedLabel(text, RestlessUi.BodySize, RestlessUi.HintSize);
        }
        foreach (var button in dialog.GetComponentsInChildren<Button>(true))
        {
            if (!button.gameObject.activeInHierarchy || button.name.IndexOf("close", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            AddRt(parts, button.transform);
            var chip = button.transform.Find("RestlessChip");
            if (chip == null) continue;
            RestlessUi.PaperControl(chip.gameObject);
            var face = chip.GetComponentInChildren<Text>(true);
            if (face != null) RestlessUi.BoundedLabel(face, RestlessUi.BodySize, RestlessUi.HintSize);
            BindCraftControl(button, chip.GetComponent<Image>(), face, false);
        }
        // Fit the viewport, not scrolling content, so the frame stays still while scrolling.
        if (!PaperOn())
            DropNamed(dialog.transform, "RestlessSkillsPaper");
        else if (scroll != null && Union(parts, out var x0, out var y0, out var x1, out var y1)
            && FrameMoved(ref _skillL, ref _skillB, ref _skillR, ref _skillT, x0, y0, x1, y1))
        {
            var panel = EnsureStrip(dialog.transform, "RestlessSkillsPaper");
            Place(panel.GetComponent<RectTransform>(), x0, y0, x1, y1, 20f, 16f, false);
            RestlessUi.InventorySurface(panel.gameObject);
        }
        if (dialog.m_listRoot == null) return;
        foreach (var row in dialog.m_listRoot.GetComponentsInChildren<Transform>(true))
        {
            if (!IsListRow(row, dialog.m_listRoot, dialog.m_elementPrefab)) continue;
            var plate = row.Find("RestlessSlot");
            if (plate == null) continue;
            if (plate.Find("paperAccent") == null)
                RestlessUi.PaperControl(plate.gameObject);
            var name = plate.Find("label")?.GetComponent<Text>();
            if (name != null)
            {
                RestlessUi.BoundedLabel(name, RestlessUi.BodySize, RestlessUi.HintSize);
                name.alignment = TextAnchor.MiddleLeft;
                RestlessUi.Stretch(name.gameObject, Vector2.zero, Vector2.one,
                    new Vector2(48f, 6f), new Vector2(-172f, -6f));
            }
            foreach (var face in row.GetComponentsInChildren<Text>(true))
            {
                if (face.name == "Restless_skillLevel")
                {
                    face.color = RestlessUi.Accent;
                    RestlessUi.BoundedLabel(face, RestlessUi.BodySize + 2, RestlessUi.HudMeta);
                }
                else if (face.name == "Restless_skillBonus")
                    RestlessUi.BoundedLabel(face, RestlessUi.HintSize, RestlessUi.HudMeta);
            }
            // Preserve native bar geometry, value updates, icons and skill hover descriptions.
            foreach (var bar in row.GetComponentsInChildren<GuiBar>(true))
                foreach (var image in bar.GetComponentsInChildren<Image>(true))
                {
                    if (!SkillColours.ContainsKey(image)) SkillColours.Add(image, image.color);
                    var n = image.name.ToLowerInvariant();
                    var colour = n.Contains("bkg") || n.Contains("background")
                        ? RestlessUi.Ink : RestlessUi.Accent;
                    colour.a = SkillColours[image].a;
                    image.color = colour;
                }
        }
    }

    private static void RestoreSkillsMaterials()
    {
        foreach (var pair in SkillColours)
            if (pair.Key != null) pair.Key.color = pair.Value;
        SkillColours.Clear();
    }
}
