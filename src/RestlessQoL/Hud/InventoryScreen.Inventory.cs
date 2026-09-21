using System.Collections.Generic;
using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

public sealed partial class InventoryScreen
{
    // Fit the material to real cells. Never move cells away from grid hit testing.
    internal static void RefreshInventoryMaterials(InventoryGui gui)
    {
        if (!ModConfig.InventoryScreenEnabled.Value || !_dressed || gui.m_player == null) return;
        var grid = gui.m_playerGrid;
        var elements = Elements(grid);
        if (elements == null) return;
        var bag = new List<RectTransform>();
        var hotbar = new List<RectTransform>();
        var extras = new List<RectTransform>();
        foreach (var element in elements)
        {
            if (element == null || !element.gameObject.activeInHierarchy) continue;
            var rt = element.transform.Find("RestlessSlot") as RectTransform;
            if (rt == null) continue;
            if (ExtraSlots.HoldsPlayerStats && ExtraSlots.IsExtraCell(element.Position)) extras.Add(rt);
            else
            {
                bag.Add(rt);
                if (element.Position.y == 0) hotbar.Add(rt);
            }
        }
        if (!Union(bag, out var left, out var bottom, out var right, out var top)) return;
        if (!PaperOn())
        {
            DropNamed(gui.m_player, "RestlessInventoryPaper");
            if (gui.m_weight != null && gui.m_weight.transform.parent != null)
                CraftArtwork(RestlessUi.Deep<Image>(gui.m_weight.transform.parent, "weight_icon"), "carry-weight");
            return;
        }
        var fullRight = right;
        var sx = Mathf.Max(0.01f, Mathf.Abs(gui.m_player.lossyScale.x));
        var sy = Mathf.Max(0.01f, Mathf.Abs(gui.m_player.lossyScale.y));
        var hasExtras = Union(extras, out var extraLeft, out _, out var extraRight, out _);
        if (hasExtras) fullRight = Mathf.Max(fullRight, extraRight);
        var panel = EnsureStrip(gui.m_player, "RestlessInventoryPaper");
        if (FrameMoved(ref _invL, ref _invB, ref _invR, ref _invT, left, bottom, fullRight, top))
        {
            Place(panel.GetComponent<RectTransform>(), left, bottom, fullRight, top, 10f, 10f, false);
            RestlessUi.InventorySurface(panel.gameObject);
        }
        // Plain rules use the existing gaps. No new header, rune or corner decoration.
        var split = InventoryRule(panel, "RestlessEquipmentRule");
        split.gameObject.SetActive(hasExtras);
        if (hasExtras)
        {
            var x = (right + extraLeft) * 0.5f;
            CraftBounds(split, x - 0.5f * sx, bottom + 5f * sy, x + 0.5f * sx, top - 5f * sy);
        }
        var seam = InventoryRule(panel, "RestlessHotbarRule");
        var hasHotbar = Union(hotbar, out _, out var hotbarBottom, out _, out _);
        seam.gameObject.SetActive(hasHotbar && bag.Count > hotbar.Count);
        if (hasHotbar && bag.Count > hotbar.Count)
        {
            var nextTop = float.MinValue;
            foreach (var cell in bag)
                if (!hotbar.Contains(cell) && WorldBox(cell, out _, out _, out _, out var cellTop))
                    nextTop = Mathf.Max(nextTop, cellTop);
            var y = (hotbarBottom + nextTop) * 0.5f;
            CraftBounds(seam, left + 4f * sx, y - 0.5f * sy, right - 4f * sx, y + 0.5f * sy);
        }

        foreach (var source in new[] { gui.m_armor, gui.m_weight })
        {
            if (source == null || source.transform.parent == null) continue;
            var stat = source.transform.parent.Find("RestlessStat");
            if (stat != null) stat.GetComponent<Image>().color = Color.clear;
        }
        if (gui.m_weight != null && gui.m_weight.transform.parent != null)
            CraftArtwork(RestlessUi.Deep<Image>(gui.m_weight.transform.parent, "weight_icon"), "carry-weight");
    }

    private static RectTransform InventoryRule(Transform parent, string name)
    {
        var rule = parent.Find(name);
        if (rule == null)
            rule = RestlessUi.Graphic(parent, name, new Color(0.64f, 0.56f, 0.42f, 0.3f), false).transform;
        return (RectTransform)rule;
    }
}
