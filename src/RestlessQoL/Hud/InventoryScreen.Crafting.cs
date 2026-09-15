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
    private static readonly Dictionary<RectTransform, Vector3> CraftTabPositions = new();
    private static readonly Dictionary<RectTransform, Vector3> CraftIconScales = new();
    private static string _recipeCopy = "";
    private static float _recipeWidth;

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
        DressStructuredRecipe(gui);
        DressStationRequirement(gui);
        CompactStationIcon(gui.m_craftingStationIcon);
        if (gui.m_craftingStationLevel != null && gui.m_craftingStationLevel.transform.parent != null)
            CompactStationIcon(gui.m_craftingStationLevel.transform.parent.GetComponent<Image>());

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

    private static void CompactStationIcon(Image? icon)
    {
        if (icon == null || CraftIconScales.ContainsKey(icon.rectTransform)) return;
        var size = Mathf.Max(icon.rectTransform.rect.width, icon.rectTransform.rect.height);
        // Only scale an isolated icon, never a panel if a prefab uses different nesting.
        if (size <= 56f || size > 128f) return;
        CraftIconScales.Add(icon.rectTransform, icon.rectTransform.localScale);
        icon.rectTransform.localScale *= 56f / size;
    }

    private static void DressStructuredRecipe(InventoryGui gui)
    {
        var source = gui.m_recipeDecription;
        if (source == null || source.transform.parent == null) return;
        var host = source.transform.parent;
        var oldFace = host.Find("Restless_recipeBody");
        if (oldFace != null) oldFace.gameObject.SetActive(false);
        var view = host.Find("RestlessRecipeBody")?.gameObject;
        if (view == null)
        {
            view = RestlessUi.Graphic(host, "RestlessRecipeBody", Color.clear, true);
            Ours.Add(view);
            view.AddComponent<RectMask2D>();
            var scroll = view.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;
            scroll.viewport = view.GetComponent<RectTransform>();
        }
        view.SetActive(source.gameObject.activeInHierarchy);
        RestlessUi.CopyRect(view.GetComponent<RectTransform>(), source.rectTransform);
        var width = Mathf.Max(80f, source.rectTransform.rect.width - 12f);
        var copy = source.text ?? "";
        if (view.transform.childCount > 0 && copy == _recipeCopy && Mathf.Abs(width - _recipeWidth) < 0.5f) return;
        foreach (Transform child in view.transform)
        {
            child.gameObject.SetActive(false);
            Object.Destroy(child.gameObject);
        }
        var content = RestlessUi.Node(view.transform, "content");
        RestlessUi.Stretch(content, new Vector2(0f, 1f), Vector2.one,
            new Vector2(0f, 0f), new Vector2(-12f, 0f));
        var rect = content.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0f, 1f);
        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 5f;
        layout.childControlHeight = layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ItemTooltip.RecipeBody(content.transform, copy, width);
        var scroller = view.GetComponent<ScrollRect>();
        scroller.content = rect;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        scroller.verticalNormalizedPosition = 1f;
        _recipeCopy = copy;
        _recipeWidth = width;
    }

    private static void DressStationRequirement(InventoryGui gui)
    {
        var source = gui.m_minStationLevelText;
        if (source == null || source.transform.parent == null) return;
        var host = source.transform.parent;
        var face = host.Find("Restless_minStation")?.GetComponent<Text>();
        if (face == null) return;
        var copy = RestlessUi.Bare(source.text);
        face.text = copy.Length == 0 ? "" : "Station\n" + copy;
        face.fontSize = RestlessUi.HudMeta;
        face.alignment = TextAnchor.MiddleCenter;
        face.horizontalOverflow = HorizontalWrapMode.Wrap;
        var colour = source.color;
        face.color = colour.r > colour.g * 1.3f && colour.r > colour.b * 1.3f
            ? RestlessUi.HealthTint : RestlessUi.Accent;
        // Stretch relative to its own cell, not the previous resource count column.
        RestlessUi.Stretch(face.gameObject, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
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
        DressStationRequirement(gui);
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
        var icon = RestlessUi.Deep<Image>(go.transform, "res_icon");
        var live = go.activeInHierarchy && icon != null && icon.gameObject.activeInHierarchy
            && icon.enabled && icon.sprite != null && icon.color.a > 0.2f;
        if (!live)
        {
            if (face != null) { face.text = ""; face.gameObject.SetActive(false); }
            plate.gameObject.SetActive(false);
            return;
        }
        plate.gameObject.SetActive(true);
        if (face == null)
        {
            face = RestlessUi.Label(plate, "", RestlessUi.HudSize, RestlessUi.Accent, TextAnchor.LowerCenter);
            face.gameObject.name = "RestlessMaterialCount";
            RestlessUi.Stretch(face.gameObject, Vector2.zero, Vector2.one,
                new Vector2(3f, 3f), new Vector2(-3f, -3f));
        }
        face.text = RestlessUi.Bare(source.text);
        face.gameObject.SetActive(face.text.Length > 0 && face.text != "0");
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
        foreach (var pair in CraftTabPositions)
            if (pair.Key != null) pair.Key.anchoredPosition3D = pair.Value;
        CraftTabPositions.Clear();
        foreach (var pair in CraftIconScales)
            if (pair.Key != null) pair.Key.localScale = pair.Value;
        CraftIconScales.Clear();
        _recipeCopy = "";
        _recipeWidth = 0f;
    }
}
