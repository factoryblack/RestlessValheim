using System.Collections.Generic;
using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

public sealed partial class InventoryScreen
{
    private static GameObject? _containerPaper;

    private static void RefreshContainerMaterials(InventoryGui gui)
    {
        var grid = gui.ContainerGrid;
        var inv = grid != null ? grid.GetInventory() : null;
        if (grid == null || inv == null || !grid.gameObject.activeInHierarchy
            || grid.transform.parent == null)
        {
            if (_containerPaper != null) _containerPaper.SetActive(false);
            return;
        }
        var elements = Elements(grid);
        if (elements == null)
        {
            if (_containerPaper != null) _containerPaper.SetActive(false);
            return;
        }
        var parts = new List<RectTransform>();
        foreach (var element in elements)
            if (element != null && element.gameObject.activeInHierarchy)
                AddRt(parts, element.transform);
        if (parts.Count == 0)
        {
            if (_containerPaper != null) _containerPaper.SetActive(false);
            return;
        }
        foreach (var button in new[] { gui.m_takeAllButton, gui.m_stackAllButton })
        {
            if (button == null || !button.gameObject.activeInHierarchy) continue;
            AddRt(parts, button.transform);
            var chip = button.transform.Find("RestlessChip");
            if (chip == null) continue;
            RestlessUi.PaperControl(chip.gameObject);
            var face = chip.GetComponentInChildren<Text>(true);
            if (face != null)
            {
                RestlessUi.BoundedLabel(face, RestlessUi.BodySize, RestlessUi.MetaSize);
                face.color = button.IsInteractable() ? RestlessUi.Text : RestlessUi.PaperMuted * 0.65f;
                RestlessUi.Stretch(face.gameObject, Vector2.zero, Vector2.one,
                    new Vector2(12f, 5f), new Vector2(-12f, -5f));
            }
            BindCraftControl(button, chip.GetComponent<Image>(), face, false);
        }
        if (gui.m_containerName != null && gui.m_containerName.gameObject.activeInHierarchy)
        {
            AddRt(parts, gui.m_containerName.transform);
            var host = gui.m_containerName.transform.parent;
            var namePlate = host != null ? host.Find("RestlessChestName") : null;
            // The shared chest frame owns the surface; its name needs no separate plaque.
            if (namePlate != null) namePlate.gameObject.SetActive(false);
            var title = host != null ? host.Find("Restless_containerName")?.GetComponent<Text>() : null;
            if (title != null)
                RestlessUi.BoundedLabel(title, RestlessUi.BodySize + 2, RestlessUi.HintSize);
        }
        if (gui.m_containerWeight != null && gui.m_containerWeight.gameObject.activeInHierarchy)
        {
            var host = gui.m_containerWeight.transform.parent;
            if (host != null)
            {
                AddRt(parts, host);
                var stat = host.Find("RestlessStat");
                if (stat != null) stat.GetComponent<Image>().color = Color.clear;
                CraftArtwork(RestlessUi.Deep<Image>(host, "weight_icon"), "carry-weight");
                var face = host.Find("Restless_containerWeight")?.GetComponent<Text>();
                if (face != null) RestlessUi.BoundedLabel(face, RestlessUi.HintSize, RestlessUi.HudMeta);
            }
        }
        if (!Union(parts, out var left, out var bottom, out var right, out var top)) return;
        var hostRoot = grid.transform.parent;
        var panel = EnsureStrip(hostRoot, "RestlessContainerPaper");
        if (_containerPaper != null && _containerPaper != panel.gameObject) _containerPaper.SetActive(false);
        _containerPaper = panel.gameObject;
        _containerPaper.SetActive(true);
        var sy = Mathf.Max(0.01f, Mathf.Abs(panel.lossyScale.y));
        var frameBottom = bottom - 24f * sy;
        if (FrameMoved(ref _chestL, ref _chestB, ref _chestR, ref _chestT, left, frameBottom, right, top))
        {
            Place(panel.GetComponent<RectTransform>(), left, frameBottom, right, top, 10f, 10f, false);
            RestlessUi.InventorySurface(panel.gameObject);
        }
        var capacity = panel.Find("RestlessCapacity")?.GetComponent<Text>();
        if (capacity == null)
        {
            capacity = RestlessUi.Label(panel, "", RestlessUi.HintSize, RestlessUi.PaperMuted, TextAnchor.MiddleLeft);
            capacity.name = "RestlessCapacity";
            RestlessUi.Stretch(capacity.gameObject, Vector2.zero, new Vector2(1f, 0f),
                new Vector2(16f, 8f), new Vector2(-16f, 30f));
        }
        RestlessUi.BoundedLabel(capacity, RestlessUi.HintSize, RestlessUi.HudMeta);
        capacity.text = inv.NrOfItems() + " / " + (inv.GetWidth() * inv.GetHeight()) + " slots";
    }
}
