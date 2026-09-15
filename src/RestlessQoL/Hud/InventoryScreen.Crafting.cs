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
    private static string _recipeIdentity = "";
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
                var icon = RestlessUi.Deep<Image>(node, "Icon") ?? RestlessUi.Deep<Image>(node, "Image")
                    ?? RestlessUi.Deep<Image>(node, "Checked");
                CompactStationIcon(icon, 42f);
                var plate = node!.Find("RestlessNavPaper");
                if (plate == null)
                {
                    var go = RestlessUi.Chip(node, "RestlessNavPaper");
                    Ours.Add(go);
                    plate = go.transform;
                    RestlessUi.Stretch(go, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    plate.SetAsFirstSibling();
                }
                RestlessUi.ForgedTab(plate.gameObject, false);
                BindCraftControl(button, plate.GetComponent<Image>(), null, false);
            }

        PaperCraftButton(gui.m_tabCraft, gui.InCraftTab());
        PaperCraftButton(gui.m_tabUpgrade, gui.InUpradeTab());
        PaperCraftButton(gui.m_craftButton, true);
        var action = gui.m_craftButton != null ? gui.m_craftButton.transform.Find("RestlessChip") : null;
        if (action != null)
        {
            RestlessUi.ForgedSurface(action.gameObject, action: true, interactive: true);
            var marker = action.Find("RestlessTabMarker");
            if (marker != null) marker.gameObject.SetActive(false);
        }
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

    private static void CompactStationIcon(Image? icon, float limit = 56f)
    {
        if (icon == null || CraftIconScales.ContainsKey(icon.rectTransform)) return;
        var size = Mathf.Max(icon.rectTransform.rect.width, icon.rectTransform.rect.height);
        // Only scale an isolated icon, never a panel if a prefab uses different nesting.
        if (size <= limit || size > 128f) return;
        CraftIconScales.Add(icon.rectTransform, icon.rectTransform.localScale);
        icon.rectTransform.localScale *= limit / size;
    }

    private static void DressStructuredRecipe(InventoryGui gui)
    {
        var source = gui.m_recipeDecription;
        if (source == null || source.transform.parent == null) return;
        var oldFace = source.transform.parent.Find("Restless_recipeBody");
        if (oldFace != null) oldFace.gameObject.SetActive(false);
        var desc = RestlessUi.Deep(gui.m_crafting, "Decription");
        var host = desc != null ? desc.Find("RestlessRecipe") : null;
        if (host == null || !WorldBox(host, out var x0, out var y0, out var x1, out var y1)) return;
        var scale = host.lossyScale;
        var sx = Mathf.Abs(scale.x);
        var sy = Mathf.Abs(scale.y);
        var identityBottom = y1 - 78f * sy;
        if (WorldBox(gui.m_recipeIcon != null ? gui.m_recipeIcon.transform : null, out _, out var iconBottom, out _, out _))
            identityBottom = Mathf.Min(identityBottom, iconBottom - 8f * sy);
        if (WorldBox(gui.m_recipeName != null ? gui.m_recipeName.transform : null, out _, out var nameBottom, out _, out _))
            identityBottom = Mathf.Min(identityBottom, nameBottom - 8f * sy);
        var view = host.Find("RestlessRecipeBody")?.gameObject;
        if (view == null)
        {
            view = RestlessUi.Graphic(host, "RestlessRecipeBody", Color.clear, true);
            view.AddComponent<RectMask2D>();
            var scroll = view.AddComponent<RestlessScrollRect>();
            scroll.horizontal = false;
            scroll.inertia = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.viewport = view.GetComponent<RectTransform>();
        }

        var scroller = view.GetComponent<RestlessScrollRect>();
        var bar = RecipeScrollbar(gui, host);
        if (bar != null)
        {
            scroller.verticalScrollbar = bar;
            scroller.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }
        view.SetActive(source.gameObject.activeInHierarchy);
        Place(view.GetComponent<RectTransform>(), x0 + 18f * sx, y0 + 18f * sy,
            x1 - 26f * sx, Mathf.Max(y0 + 50f * sy, identityBottom), 0f, 0f, false);
        var width = Mathf.Max(80f, view.GetComponent<RectTransform>().rect.width);
        var copy = ItemTooltip.RecipeCopy(source.text ?? "");
        var identity = gui.GetSelectedRecipeIndex(false) + ":" + gui.InCraftTab() + ":" + gui.m_recipeName?.text;
        var changedSelection = identity != _recipeIdentity;
        if (!changedSelection && scroller.content != null && copy == _recipeCopy && Mathf.Abs(width - _recipeWidth) < 0.5f) return;
        var offset = !changedSelection && scroller.content != null ? scroller.content.anchoredPosition.y : 0f;
        foreach (Transform child in view.transform)
        {
            child.gameObject.SetActive(false);
            Object.Destroy(child.gameObject);
        }
        var content = RestlessUi.Node(view.transform, "content");
        RestlessUi.Stretch(content, new Vector2(0f, 1f), Vector2.one, Vector2.zero, Vector2.zero);
        var rect = content.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0f, 1f);
        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 5f;
        layout.childControlHeight = layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ItemTooltip.RecipeBody(content.transform, copy, width);
        scroller.content = rect;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        scroller.StopMovement();
        rect.anchoredPosition = new Vector2(0f, Mathf.Clamp(offset, 0f, Mathf.Max(0f, rect.rect.height - scroller.viewport.rect.height)));
        _recipeCopy = copy;
        _recipeWidth = width;
        _recipeIdentity = identity;
    }

    // Same wood slider as the recipe list — not the forged thumb. Wheel step lives on RestlessScrollRect.
    private static Scrollbar RecipeScrollbar(InventoryGui gui, Transform host)
    {
        var forged = host.Find("RestlessScrollTrack");
        if (forged != null)
            Object.Destroy(forged.gameObject);
        var existing = host.Find("RestlessRecipeScroll")?.GetComponent<Scrollbar>();
        if (existing != null)
            return existing;
        var src = gui.m_recipeListScroll;
        if (src == null)
            return null;
        var go = Object.Instantiate(src.gameObject, host, false);
        go.name = "RestlessRecipeScroll";
        Ours.Add(go);
        var srcRt = src.transform as RectTransform;
        var width = srcRt != null && srcRt.rect.width > 1f ? srcRt.rect.width : 16f;
        RestlessUi.Stretch(go, new Vector2(1f, 0f), Vector2.one,
            new Vector2(-width - 6f, 18f), new Vector2(-4f, -92f));
        return go.GetComponent<Scrollbar>();
    }

    private static void DressStationRequirement(InventoryGui gui)
    {
        var source = gui.m_minStationLevelText;
        if (source == null || source.transform.parent == null) return;
        var host = source.transform.parent;
        var face = host.Find("Restless_minStation")?.GetComponent<Text>();
        if (face == null) return;
        var copy = RestlessUi.Bare(source.text);
        face.text = copy;
        face.fontSize = RestlessUi.HudMeta;
        face.alignment = TextAnchor.LowerCenter;
        face.horizontalOverflow = HorizontalWrapMode.Overflow;
        var colour = source.color;
        face.color = colour.r > colour.g * 1.3f && colour.r > colour.b * 1.3f
            ? RestlessUi.HealthTint : RestlessUi.Accent;
        if (gui.m_minStationLevelIcon != null) Hide(gui.m_minStationLevelIcon);
        var oldBackground = host.GetComponent<Image>();
        if (oldBackground != null && oldBackground.GetComponent<Mask>() == null) Hide(oldBackground);
        var medal = host.Find("RestlessStationMedal")?.gameObject;
        if (medal == null)
        {
            medal = RestlessUi.Picture(host, "RestlessStationMedal", "station-medallion");
            Ours.Add(medal);
            RestlessUi.Pin(medal, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 52f));
            medal.GetComponent<Image>().raycastTarget = true;
            medal.AddComponent<RestlessHint>();
            var glyph = RestlessUi.Picture(medal.transform, "glyph", "glyph-station");
            RestlessUi.Pin(glyph, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -7f), new Vector2(24f, 24f));
            glyph.GetComponent<Image>().color = RestlessUi.PaperMuted;
            glyph.GetComponent<Image>().raycastTarget = false;
        }
        medal.SetActive(copy.Length > 0 && source.gameObject.activeInHierarchy);
        medal.GetComponent<RestlessHint>().Copy = "Requires station level " + copy;
        RestlessUi.Pin(face.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(44f, 22f));
        face.transform.SetAsLastSibling();
    }

    private static void PaperCraftButton(Button? button, bool primary)
    {
        var chip = button != null ? button.transform.Find("RestlessChip") : null;
        if (button == null || chip == null) return;
        RestlessUi.ForgedTab(chip.gameObject, primary);
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
            if (pair.Key == null) continue;
            var nav = pair.Key.transform.Find("RestlessNavPaper");
            if (nav != null)
            {
                var selected = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject == pair.Key.gameObject;
                selected |= pair.Key.name switch
                {
                    "Texts" => gui.m_textsDialog != null && gui.m_textsDialog.gameObject.activeInHierarchy,
                    "Skills" => gui.m_skillsDialog != null && gui.m_skillsDialog.gameObject.activeInHierarchy,
                    "Trophies" => gui.m_trophiesPanel != null && gui.m_trophiesPanel.activeInHierarchy,
                    "Achievements" => gui.m_achievementsPanel != null && gui.m_achievementsPanel.gameObject.activeInHierarchy,
                    _ => false
                };
                var marker = nav.Find("RestlessTabMarker");
                if (marker != null) marker.gameObject.SetActive(selected);
            }
            if (pair.Value.Face == null) continue;
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
        _recipeIdentity = "";
        _recipeWidth = 0f;
    }
}
