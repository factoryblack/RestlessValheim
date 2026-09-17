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
    private static readonly Dictionary<Image, (Sprite? Sprite, bool Aspect, Color Colour, string Asset)> CraftIconArt = new();
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
            var rule = header.Find("RestlessPaperRule");
            if (rule != null) rule.gameObject.SetActive(false);
        }
        if (detail != null) RestlessUi.PaperSurface(detail.gameObject);
        DressStructuredRecipe(gui);
        if (gui.m_recipeIcon != null && gui.m_recipeIcon.transform.Find("RestlessPortrait") == null)
        {
            var frame = RestlessUi.Picture(gui.m_recipeIcon.transform, "RestlessPortrait", "portrait-frame");
            Ours.Add(frame);
            RestlessUi.PortraitFrame(frame);
            RestlessUi.Stretch(frame, Vector2.zero, Vector2.one, new Vector2(-4f, -4f), new Vector2(4f, 4f));
        }
        DressStationRequirement(gui);
        if (gui.m_repairButton != null)
            CraftArtwork(RestlessUi.Deep<Image>(gui.m_repairButton.transform, "Icon"), "utility-repair");
        CompactStationIcon(gui.m_craftingStationIcon);
        CraftArtwork(gui.m_craftingStationIcon, "craft-hammer");
        if (gui.m_craftingStationLevel != null && gui.m_craftingStationLevel.transform.parent != null)
        {
            CompactStationIcon(gui.m_craftingStationLevel.transform.parent.GetComponent<Image>());
            CraftArtwork(gui.m_craftingStationLevel.transform.parent.GetComponent<Image>(), "station-socket");
        }

        // Replace icon artwork, preserving the native nodes, active states and click targets.
        var navigation = gui.m_info != null ? gui.m_info.Find("RestlessIcons") : null;
        if (navigation != null) RestlessUi.PaperSurface(navigation.gameObject);
        if (gui.m_info != null)
            foreach (var name in new[] { "Texts", "Skills", "Trophies", "Achievements", "PVP" })
            {
                var node = gui.m_info.Find(name) ?? RestlessUi.Deep(gui.m_info, name);
                if (name == "PVP" && node != null)
                {
                    DressPvpArtwork(node);
                    continue;
                }
                var button = node != null ? node.GetComponent<Button>() : null;
                if (button == null) continue;
                var icon = RestlessUi.Deep<Image>(node, "Icon") ?? RestlessUi.Deep<Image>(node, "Image")
                    ?? RestlessUi.Deep<Image>(node, "Checked");
                CompactStationIcon(icon, 42f);
                var asset = name switch
                {
                    "Texts" => "nav-raven", "Skills" => "nav-knot",
                    "Trophies" => "nav-trophy", "Achievements" => "nav-shield", _ => "nav-swords"
                };
                CraftArtwork(icon, asset);
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
                RestlessUi.Stretch(plate.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(6f, -23f), new Vector2(-6f, 23f));
                plate.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);
                BindCraftControl(button, plate.GetComponent<Image>(), null, false);
                RestlessUi.ControlFeedback(button).Icon = icon;
            }

        PaperCraftButton(gui.m_tabCraft, gui.InCraftTab());
        PaperCraftButton(gui.m_tabUpgrade, gui.InUpradeTab());
        PaperCraftButton(gui.m_craftButton, true);
        var action = gui.m_craftButton != null ? gui.m_craftButton.transform.Find("RestlessChip") : null;
        if (action != null)
        {
            RestlessUi.ForgedSurface(action.gameObject, action: true, interactive: true);
            var label = action.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.fontSize = 24;
                label.alignment = TextAnchor.MiddleCenter;
                RestlessUi.Stretch(label.gameObject, Vector2.zero, Vector2.one,
                    new Vector2(30f, 8f), new Vector2(-30f, -8f));
            }
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

    private static void CompactStationIcon(Image? icon, float limit = 56f)
    {
        if (icon == null || CraftIconScales.ContainsKey(icon.rectTransform)) return;
        var size = Mathf.Max(icon.rectTransform.rect.width, icon.rectTransform.rect.height);
        // Only scale an isolated icon, never a panel if a prefab uses different nesting.
        if (size <= limit || size > 128f) return;
        CraftIconScales.Add(icon.rectTransform, icon.rectTransform.localScale);
        icon.rectTransform.localScale *= limit / size;
    }

    private static void CraftArtwork(Image? image, string asset)
    {
        if (image == null) return;
        var sprite = Kit.Sprite(asset);
        if (sprite == null) return;
        if (!CraftIconArt.ContainsKey(image))
            CraftIconArt.Add(image, (image.sprite, image.preserveAspect, image.color, asset));
        image.sprite = sprite;
        image.preserveAspect = true;
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
        var identityBottom = LayoutCraftIdentity(gui, x0, y1, x1, sx, sy);
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
        var bar = RecipeScrollbar(gui, host, (y1 - identityBottom) / sy);
        if (bar != null)
        {
            scroller.verticalScrollbar = bar;
            scroller.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }
        view.SetActive(source.gameObject.activeInHierarchy);
        var barWidth = bar != null ? bar.GetComponent<RectTransform>().rect.width : 0f;
        Place(view.GetComponent<RectTransform>(), x0 + 24f * sx, y0 + 24f * sy,
            x1 - (barWidth + 36f) * sx, Mathf.Max(y0 + 50f * sy, identityBottom), 0f, 0f, false);
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
        layout.spacing = 8f;
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
    private static Scrollbar RecipeScrollbar(InventoryGui gui, Transform host, float topInset)
    {
        var forged = host.Find("RestlessScrollTrack");
        if (forged != null)
            Object.Destroy(forged.gameObject);
        var existing = host.Find("RestlessRecipeScroll")?.GetComponent<Scrollbar>();
        var src = gui.m_recipeListScroll;
        if (src == null)
            return null;
        var go = existing != null ? existing.gameObject : Object.Instantiate(src.gameObject, host, false);
        if (existing == null)
        {
            go.name = "RestlessRecipeScroll";
            Ours.Add(go);
        }
        var srcRt = src.transform as RectTransform;
        var width = srcRt != null && srcRt.rect.width > 1f ? srcRt.rect.width : 16f;
        RestlessUi.Stretch(go, new Vector2(1f, 0f), Vector2.one,
            new Vector2(-width - 12f, 24f), new Vector2(-12f, -topInset));
        return go.GetComponent<Scrollbar>();
    }

    private static void DressStationRequirement(InventoryGui gui)
    {
        var source = gui.m_minStationLevelText;
        if (source == null || source.transform.parent == null) return;
        var host = source.transform.parent;
        var face = host.Find("Restless_minStation")?.GetComponent<Text>();
        if (face == null)
        {
            face = RestlessUi.Label(host, "", 20, RestlessUi.Accent, TextAnchor.MiddleCenter);
            face.gameObject.name = "Restless_minStation";
            Ours.Add(face.gameObject);
        }
        var copy = RestlessUi.Bare(source.text);
        face.text = copy;
        face.fontSize = 20;
        face.alignment = TextAnchor.MiddleCenter;
        face.horizontalOverflow = HorizontalWrapMode.Overflow;
        var colour = source.color;
        face.color = colour.r > colour.g * 1.3f && colour.r > colour.b * 1.3f
            ? RestlessUi.HealthTint : RestlessUi.Accent;
        if (gui.m_minStationLevelIcon != null) Hide(gui.m_minStationLevelIcon);
        var oldBackground = host.GetComponent<Image>();
        if (oldBackground != null && oldBackground.GetComponent<Mask>() == null) Hide(oldBackground);
        var oldSlot = host.Find("RestlessSlot");
        if (oldSlot != null) oldSlot.gameObject.SetActive(false);
        var medal = host.Find("RestlessStationMedal")?.gameObject;
        if (medal == null)
        {
            medal = RestlessUi.Picture(host, "RestlessStationMedal", "station-socket");
            Ours.Add(medal);
            RestlessUi.Pin(medal, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 52f));
            medal.GetComponent<Image>().raycastTarget = true;
            medal.AddComponent<RestlessHint>();
            var glyph = RestlessUi.Picture(medal.transform, "glyph", "craft-hammer");
            RestlessUi.Pin(glyph, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -5f), new Vector2(28f, 28f));
            glyph.GetComponent<Image>().preserveAspect = true;
            glyph.GetComponent<Image>().color = Color.white;
            glyph.GetComponent<Image>().raycastTarget = false;
        }
        medal.SetActive(copy.Length > 0 && source.gameObject.activeInHierarchy);
        face.gameObject.SetActive(medal.activeSelf);
        medal.GetComponent<RestlessHint>().Copy = "Requires station level " + copy;
        RestlessUi.Pin(face.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(44f, 22f));
        face.transform.SetAsLastSibling();
    }

    private static void PaperCraftButton(Button? button, bool primary)
    {
        var chip = button != null ? button.transform.Find("RestlessChip") : null;
        if (button == null || chip == null) return;
        RestlessUi.ForgedTab(chip.gameObject, primary);
        var face = chip.GetComponentInChildren<Text>(true);
        if (face != null)
        {
            RestlessUi.BoundedLabel(face, RestlessUi.BodySize, RestlessUi.HintSize);
            face.alignment = TextAnchor.MiddleCenter;
            RestlessUi.Stretch(face.gameObject, Vector2.zero, Vector2.one,
                new Vector2(12f, 4f), new Vector2(-12f, -4f));
        }
        BindCraftControl(button, chip.GetComponent<Image>(), face, primary);
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
        // Readout mirroring happens before this pass; reapply owned geometry afterwards.
        DressStructuredRecipe(gui);
        LayoutStationBadge(gui);
        foreach (var pair in CraftIconArt)
            if (pair.Key != null) pair.Key.sprite = Kit.Sprite(pair.Value.Asset);
        DressStationRequirement(gui);
        foreach (var pair in CraftControls)
        {
            if (pair.Key == null) continue;
            var nav = pair.Key.transform.Find("RestlessNavPaper");
            if (nav != null)
            {
                var selected = pair.Key.name switch
                {
                    "Texts" => gui.m_textsDialog != null && gui.m_textsDialog.gameObject.activeInHierarchy,
                    "Skills" => gui.m_skillsDialog != null && gui.m_skillsDialog.gameObject.activeInHierarchy,
                    "Trophies" => gui.m_trophiesPanel != null && gui.m_trophiesPanel.activeInHierarchy,
                    "Achievements" => gui.m_achievementsPanel != null && gui.m_achievementsPanel.gameObject.activeInHierarchy,
                    _ => false
                };
                var marker = nav.Find("RestlessTabMarker");
                if (marker != null) marker.gameObject.SetActive(selected);
                var feedback = pair.Key.GetComponent<RestlessControlFeedback>();
                if (feedback != null) feedback.Selected = selected;
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
        LayoutMaterialCell(plate, icon, face);
        face.gameObject.SetActive(face.text.Length > 0 && face.text != "0");
        var colour = source.color;
        colour.a = 1f; // TMP alpha was suppressed by the dresser, not by the game state.
        face.color = colour.r > colour.g * 1.3f && colour.r > colour.b * 1.3f
            ? RestlessUi.HealthTint : RestlessUi.Accent;
        var missing = plate.Find("RestlessMissing")?.gameObject;
        if (missing == null)
        {
            missing = RestlessUi.Picture(plate, "RestlessMissing", "utility-missing");
            RestlessUi.Pin(missing, Vector2.one, Vector2.one, new Vector2(-3f, -3f), new Vector2(16f, 16f));
            missing.GetComponent<Image>().raycastTarget = false;
        }
        missing.GetComponent<Image>().color = RestlessUi.HealthTint;
        missing.SetActive(face.gameObject.activeSelf && face.color == RestlessUi.HealthTint);
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
            var feedback = pair.Key.GetComponent<RestlessControlFeedback>();
            if (feedback != null) { feedback.enabled = false; Object.Destroy(feedback); }
        }
        CraftControls.Clear();
        RestoreCraftLayout();
        RestoreSkillsMaterials();
        foreach (var pair in CraftIconArt)
        {
            if (pair.Key == null) continue;
            pair.Key.sprite = pair.Value.Sprite;
            pair.Key.preserveAspect = pair.Value.Aspect;
            pair.Key.color = pair.Value.Colour;
        }
        CraftIconArt.Clear();
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

