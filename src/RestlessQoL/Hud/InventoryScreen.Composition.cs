using System.Collections.Generic;
using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

public sealed partial class InventoryScreen
{
    private static bool _craftAreaReady;
    private static Rect _craftArea;
    private static readonly Dictionary<Transform, (Transform parent, int sibling)> RequirementParents = new();
    private static string _requirementsSelection = "";

    private static void RestoreRequirementParents()
    {
        foreach (var pair in RequirementParents)
            if (pair.Key != null && pair.Value.parent != null)
            {
                pair.Key.SetParent(pair.Value.parent, false);
            }
        var ordered = new List<KeyValuePair<Transform, (Transform parent, int sibling)>>(RequirementParents);
        ordered.Sort((a, b) => a.Value.sibling.CompareTo(b.Value.sibling));
        foreach (var pair in ordered)
            if (pair.Key != null && pair.Value.parent != null) pair.Key.SetSiblingIndex(pair.Value.sibling);
        RequirementParents.Clear();
        _requirementsSelection = "";
    }

    private static void ParentRequirement(Transform child, Transform content)
    {
        if (child.parent == content) return;
        if (child is RectTransform rect) RememberCraftRect(rect);
        if (!RequirementParents.ContainsKey(child))
            RequirementParents.Add(child, (child.parent, child.GetSiblingIndex()));
        child.SetParent(content, true);
    }

    // Capture native stationary bounds once. Never derive the next layout from
    // an already moved portrait, wrapped description or scrolling recipe row.
    private static bool CraftArea(InventoryGui gui, out Rect area, out float sx, out float sy)
    {
        area = default; sx = sy = 1f;
        var craft = gui.m_crafting;
        if (craft == null) return false;
        sx = Mathf.Max(0.01f, Mathf.Abs(craft.lossyScale.x));
        sy = Mathf.Max(0.01f, Mathf.Abs(craft.lossyScale.y));
        if (!_craftAreaReady)
        {
            var desc = RestlessUi.Deep(craft, "Decription");
            if (!WorldBox(ListHost(gui), out var l, out var bottom, out _, out var top)
                || !WorldBox(desc, out _, out _, out var r, out _)) return false;
            var topic = RestlessUi.Deep(craft, "topic") ?? gui.m_craftingStationName?.transform;
            if (WorldBox(topic, out _, out _, out _, out var titleTop)) top = titleTop + 24f * sy;
            if (gui.m_craftButton != null && WorldBox(gui.m_craftButton.transform,
                out _, out var actionBottom, out _, out _)) bottom = Mathf.Min(bottom, actionBottom);
            var a = craft.InverseTransformPoint(new Vector3(l - 12f * sx, bottom - 12f * sy, craft.position.z));
            var b = craft.InverseTransformPoint(new Vector3(r + 12f * sx, top, craft.position.z));
            _craftArea = Rect.MinMaxRect(a.x, a.y, b.x, b.y);
            _craftAreaReady = _craftArea.width > 200f && _craftArea.height > 300f;
            if (!_craftAreaReady) return false;
        }
        var min = craft.TransformPoint(new Vector3(_craftArea.xMin, _craftArea.yMin));
        var max = craft.TransformPoint(new Vector3(_craftArea.xMax, _craftArea.yMax));
        area = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return true;
    }

    private static void MoveCraft(Transform? target, float l, float b, float r, float t)
    {
        if (target is not RectTransform rect) return;
        RememberCraftRect(rect);
        CraftBounds(rect, l, b, r, t);
    }

    private static void ComposeCraftHeader(InventoryGui gui)
    {
        if (!CraftArea(gui, out var a, out var sx, out var sy)) return;
        var header = EnsureStrip(gui.m_crafting, "RestlessHeader");
        Place(header.GetComponent<RectTransform>(), a.xMin + 18f * sx, a.yMax - 100f * sy,
            a.xMax - 18f * sx, a.yMax - 8f * sy, 0f, 0f, false);
        RestlessUi.CraftInset(header.gameObject);
        MoveCraft(gui.m_craftingStationIcon?.transform, a.xMin + 24f * sx, a.yMax - 64f * sy,
            a.xMin + 70f * sx, a.yMax - 18f * sy);
        MoveCraft(gui.m_craftingStationLevel?.transform.parent, a.xMax - 72f * sx, a.yMax - 64f * sy,
            a.xMax - 24f * sx, a.yMax - 16f * sy);
        MoveCraft(gui.m_tabCraft?.transform, a.xMin + 24f * sx, a.yMax - 98f * sy,
            a.xMin + 132f * sx, a.yMax - 66f * sy);
        MoveCraft(gui.m_tabUpgrade?.transform, a.xMin + 140f * sx, a.yMax - 98f * sy,
            a.xMin + 248f * sx, a.yMax - 66f * sy);
        var upgrade = RestlessUi.Deep(gui.m_crafting, "UpgradePanel");
        MoveCraft(upgrade, a.xMax - 212f * sx, a.yMax - 98f * sy,
            a.xMax - 24f * sx, a.yMax - 66f * sy);
        var list = ListHost(gui);
        // Move the stationary list host, never the ScrollRect content itself.
        if (list != gui.m_recipeListRoot)
            MoveCraft(list, a.xMin + 18f * sx, a.yMin + 18f * sy,
                a.xMin + a.width * 0.35f, a.yMax - 116f * sy);
    }

    private static void ComposeCraftDetail(InventoryGui gui)
    {
        if (!CraftArea(gui, out var a, out var sx, out var sy)) return;
        var desc = RestlessUi.Deep(gui.m_crafting, "Decription");
        if (desc == null) return;
        var left = a.xMin + a.width * 0.35f + 18f * sx;
        var right = a.xMax - 20f * sx;
        // Pack the selected recipe's visible materials. The outer sheet stays
        // fixed; only the inner reading area gives way when another row is needed.
        var station = RestlessUi.Deep(gui.m_crafting, "RestlessStationSocket");
        var hasStation = RequiredStation(gui, out _) != null;
        // Reserve an extra gutter after the non-consumable station socket.
        var available = (right - left) / sx - (hasStation ? 8f : 0f);
        var columns = Mathf.Max(1, Mathf.FloorToInt((available + 8f) / 88f));
        var count = hasStation ? 1 : 0;
        if (gui.m_recipeRequirementList != null)
            foreach (var requirement in gui.m_recipeRequirementList)
                if (LiveMaterial(requirement)) count++;
        var rows = Mathf.Max(1, Mathf.CeilToInt((float)count / columns));
        // Keep at least 260 units for identity + reader at normal canvas sizes.
        // Surplus requirement rows scroll independently instead of consuming it.
        var sheetHeight = a.height / sy;
        var rowBudget = Mathf.Max(104f, sheetHeight - 116f - 260f - 108f);
        var overflowing = rows * 112f - 8f > rowBudget;
        if (overflowing)
        {
            available -= 24f; // Native-width scrollbar and a clear content gutter.
            columns = Mathf.Max(1, Mathf.FloorToInt((available + 8f) / 88f));
            rows = Mathf.Max(1, Mathf.CeilToInt((float)count / columns));
        }
        var requirementHeight = rows * 112f - 8f;
        var viewportHeight = Mathf.Min(requirementHeight, rowBudget);
        var footer = 108f + viewportHeight;
        var view = gui.m_crafting.Find("RestlessRequirementsViewport")?.gameObject;
        if (view == null)
        {
            view = RestlessUi.Graphic(gui.m_crafting, "RestlessRequirementsViewport", Color.clear, true);
            Ours.Add(view);
            view.AddComponent<RectMask2D>();
            var reader = view.AddComponent<RestlessScrollRect>();
            reader.horizontal = false;
            reader.RowHeight = 112f;
            reader.RowsPerNotch = 1f;
            reader.viewport = view.GetComponent<RectTransform>();
            reader.content = RestlessUi.Node(view.transform, "content").GetComponent<RectTransform>();
        }
        var requirementsScroll = view.GetComponent<RestlessScrollRect>();
        var bar = RecipeScrollbar(gui, view.transform, 4f);
        if (bar != null)
        {
            requirementsScroll.verticalScrollbar = bar;
            requirementsScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            bar.transform.SetAsLastSibling();
        }
        var content = requirementsScroll.content;
        Place(view.GetComponent<RectTransform>(), left, a.yMin + 78f * sy, right,
            a.yMin + (78f + viewportHeight) * sy, 0f, 0f, false);
        var selection = gui.GetSelectedRecipeIndex(false) + ":" + gui.InCraftTab();
        var offset = selection == _requirementsSelection ? content.anchoredPosition.y : 0f;
        _requirementsSelection = selection;
        requirementsScroll.CancelWheel();
        RestlessUi.Pin(content.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
            Vector2.zero, new Vector2((right - left) / sx, requirementHeight));
        var contentTop = a.yMin + (78f + viewportHeight) * sy;
        var detail = EnsureStrip(desc, "RestlessRecipe");
        Place(detail.GetComponent<RectTransform>(), left, a.yMin + footer * sy, right,
            a.yMax - 116f * sy, 0f, 0f, false);
        RestlessUi.CraftInset(detail.gameObject);
        MoveCraft(gui.m_craftButton?.transform, left, a.yMin + 18f * sy, right, a.yMin + 64f * sy);
        var cancel = RestlessUi.Deep(gui.m_crafting, "CraftCancelButton");
        MoveCraft(cancel, left, a.yMin + 18f * sy, right, a.yMin + 64f * sy);
        var requirements = gui.m_recipeRequirementList;
        if (requirements != null)
        {
            // Snapshot every sibling before moving the first native cell.
            foreach (var requirement in requirements)
                if (requirement != null && !RequirementParents.ContainsKey(requirement.transform))
                {
                    var child = requirement.transform;
                    RequirementParents.Add(child, (child.parent, child.GetSiblingIndex()));
                    if (child is RectTransform rect) RememberCraftRect(rect);
                }
            var gap = 8f * sx;
            var cell = Mathf.Min(80f * sx, (available * sx - (columns - 1) * gap) / columns);
            if (hasStation && station != null)
            {
                ParentRequirement(station, content);
                var y = contentTop - 104f * sy;
                CraftBounds(station.GetComponent<RectTransform>(), left, y, left + cell, y + 104f * sy);
            }
            var slot = hasStation ? 1 : 0;
            foreach (var requirement in requirements)
            {
                if (!LiveMaterial(requirement)) continue;
                var x = left + (slot % columns) * (cell + gap)
                    + (hasStation && slot < columns ? 8f * sx : 0f);
                ParentRequirement(requirement.transform, content);
                var y = contentTop - (104f + slot / columns * 112f) * sy;
                MoveCraft(requirement.transform, x, y, x + cell, y + 104f * sy);
                slot++;
            }
        }
        content.anchoredPosition = new Vector2(0f, Mathf.Clamp(offset, 0f,
            Mathf.Max(0f, requirementHeight - viewportHeight)));
    }

    private static bool LiveMaterial(GameObject? requirement)
    {
        if (requirement == null || !requirement.activeSelf) return false;
        var icon = RestlessUi.Deep<Image>(requirement.transform, "res_icon");
        return icon != null && icon.gameObject.activeSelf && icon.enabled && icon.sprite != null && icon.color.a > 0.2f;
    }

    private static void ComposeCraftPaper(InventoryGui gui)
    {
        if (!CraftArea(gui, out var a, out var sx, out var sy)) return;
        var body = EnsureStrip(gui.m_crafting, "RestlessCraftPaper");
        Place(body.GetComponent<RectTransform>(), a.xMin, a.yMin, a.xMax, a.yMax, 0f, 0f, false);
        RestlessUi.PaperSurface(body.gameObject);
        var seam = body.Find("RestlessCraftSeam")?.gameObject;
        if (seam == null) seam = RestlessUi.Graphic(body, "RestlessCraftSeam", new Color(0.5f, 0.43f, 0.32f, 0.4f), false);
        RestlessUi.Stretch(seam, new Vector2(0f, 1f), Vector2.one,
            new Vector2(24f, -108f), new Vector2(-24f, -107f));
    }
}
