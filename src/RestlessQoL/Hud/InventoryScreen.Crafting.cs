using System.Collections.Generic;
using RestlessQoL.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

public sealed partial class InventoryScreen
{
    // The game's controls still own selection, focus, crafting and upgrade behaviour.
    private sealed class CraftControl
    {
        public Graphic Graphic = null!;
        public ColorBlock Colours;
        public Selectable.Transition Transition;
        public Text? Face;
        public bool Primary;
    }

    private static readonly Dictionary<Button, CraftControl> CraftControls = new();

    private static void DressCraftMaterials(InventoryGui gui)
    {
        var craft = gui.m_crafting;
        if (craft == null) return;
        var header = craft.Find("RestlessHeader");
        var desc = RestlessUi.Deep(craft, "Decription");
        var detail = desc != null ? desc.Find("RestlessRecipe") : null;
        var parts = new List<RectTransform>();
        AddRt(parts, header);
        AddRt(parts, ListHost(gui));
        AddRt(parts, detail);
        AddRt(parts, gui.m_craftButton != null ? gui.m_craftButton.transform : null);
        AddRt(parts, RestlessUi.Deep(craft, "UpgradePanel"));
        if (gui.m_recipeRequirementList != null)
            foreach (var requirement in gui.m_recipeRequirementList)
                if (requirement != null) AddRt(parts, requirement.transform);
        if (Union(parts, out var x0, out var y0, out var x1, out var y1))
        {
            var body = EnsureStrip(craft, "RestlessCraftPaper");
            Place(body.GetComponent<RectTransform>(), x0, y0, x1, y1, 14f, 14f);
            RestlessUi.PaperSurface(body.gameObject);
        }
        if (header != null)
        {
            RestlessUi.PaperSurface(header.gameObject);
            PaperHeaderRule(header);
        }
        if (detail != null) RestlessUi.PaperSurface(detail.gameObject);

        // Keep the real navigation icons and their existing controller/click targets.
        var navigation = gui.m_info != null ? gui.m_info.Find("RestlessIcons") : null;
        if (navigation != null) RestlessUi.PaperSurface(navigation.gameObject);
        if (gui.m_info != null)
            foreach (var name in new[] { "Texts", "Skills", "Trophies", "Achievements", "PVP" })
            {
                var node = gui.m_info.Find(name) ?? RestlessUi.Deep(gui.m_info, name);
                var button = node != null ? node.GetComponent<Button>() : null;
                if (button == null) continue;
                var plate = node!.Find("RestlessNavPaper");
                if (plate == null)
                {
                    var go = RestlessUi.Chip(node, "RestlessNavPaper");
                    Ours.Add(go);
                    plate = go.transform;
                    RestlessUi.Stretch(go, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    plate.SetAsFirstSibling();
                }
                RestlessUi.PaperControl(plate.gameObject);
                BindCraftControl(button, plate.GetComponent<Image>(), null, false);
            }

        PaperCraftButton(gui.m_tabCraft, gui.InCraftTab());
        PaperCraftButton(gui.m_tabUpgrade, gui.InUpradeTab());
        PaperCraftButton(gui.m_craftButton, true);
        PaperCraftButton(RestlessUi.Deep<Button>(craft, "CraftCancelButton"), false);
        var upgrade = RestlessUi.Deep(craft, "UpgradePanel");
        if (upgrade != null)
        {
            PaperCraftButton(RestlessUi.Deep<Button>(upgrade, "LevelDown"), false);
            PaperCraftButton(RestlessUi.Deep<Button>(upgrade, "LevelUp"), false);
        }
    }

    private static void PaperHeaderRule(Transform header)
    {
        if (header.Find("RestlessPaperRule") != null) return;
        var rule = RestlessUi.Node(header, "RestlessPaperRule");
        RestlessUi.Stretch(rule, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(18f, 1f), new Vector2(-18f, 17f));
        RestlessUi.PaperDivider(rule);
    }

    private static void PaperCraftButton(Button? button, bool primary)
    {
        var chip = button != null ? button.transform.Find("RestlessChip") : null;
        if (button == null || chip == null) return;
        RestlessUi.PaperControl(chip.gameObject, primary ? RestlessUi.Accent : null);
        BindCraftControl(button, chip.GetComponent<Image>(), chip.GetComponentInChildren<Text>(true), primary);
    }

    private static void BindCraftControl(Button button, Image image, Text? face, bool primary)
    {
        if (!CraftControls.TryGetValue(button, out var state))
        {
            state = new CraftControl
            {
                Graphic = button.targetGraphic,
                Colours = button.colors,
                Transition = button.transition
            };
            CraftControls.Add(button, state);
            button.targetGraphic = image;
            RestlessUi.PaperSelectable(button);
        }
        state.Face = face;
        state.Primary = primary;
    }

    private static void RefreshCraftFeedback(InventoryGui gui)
    {
        foreach (var pair in CraftControls)
        {
            if (pair.Key == null || pair.Value.Face == null) continue;
            pair.Value.Face.color = !pair.Key.IsInteractable() ? RestlessUi.PaperMuted * 0.65f
                : pair.Value.Primary ? RestlessUi.Accent : RestlessUi.Text;
        }
        // Requirements can change from nearby storage without the player's grid changing.
        if (gui.m_recipeRequirementList == null) return;
        foreach (var go in gui.m_recipeRequirementList)
        {
            if (go == null || !go.activeInHierarchy) continue;
            PaintRequirement(go, RestlessUi.Deep<TMP_Text>(go.transform, "res_amount"));
        }
    }

    private static void PaintRequirement(GameObject go, TMP_Text? source)
    {
        var plate = go.transform.Find("RestlessSlot");
        if (source == null || plate == null) return;
        var face = plate.Find("RestlessMaterialCount")?.GetComponent<Text>();
        if (face == null)
        {
            face = RestlessUi.Label(plate, "", RestlessUi.HudSize, RestlessUi.Accent, TextAnchor.LowerCenter);
            face.gameObject.name = "RestlessMaterialCount";
            RestlessUi.Stretch(face.gameObject, Vector2.zero, Vector2.one,
                new Vector2(3f, 3f), new Vector2(-3f, -3f));
        }
        face.text = RestlessUi.Bare(source.text);
        var colour = source.color;
        colour.a = 1f; // TMP alpha was suppressed by the dresser, not by the game state.
        face.color = colour.r > colour.g * 1.3f && colour.r > colour.b * 1.3f
            ? RestlessUi.HealthTint : RestlessUi.Accent;
        face.transform.SetAsLastSibling();
    }

    private static void RestoreCraftControls()
    {
        foreach (var pair in CraftControls)
        {
            if (pair.Key == null) continue;
            pair.Key.targetGraphic = pair.Value.Graphic;
            pair.Key.colors = pair.Value.Colours;
            pair.Key.transition = pair.Value.Transition;
        }
        CraftControls.Clear();
    }
}
