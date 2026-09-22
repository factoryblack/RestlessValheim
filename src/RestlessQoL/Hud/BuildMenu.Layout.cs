using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

public sealed partial class BuildMenu
{
    private static Vector2 _fitSize;
    private static float _fitFooter, _fitBottom, _fitWidth;

    private static float FitBuildGrid(global::Hud hud, RectTransform host, float footerTop, out float gridWidth)
    {
        if (_fitWidth > 1f && host.rect.size == _fitSize && Mathf.Approximately(footerTop, _fitFooter))
        {
            gridWidth = _fitWidth;
            return _fitBottom;
        }
        gridWidth = 0f;
        var ui = hud.m_buildUi;
        if (ui == null || ui.transform is not RectTransform root) return footerTop;
        if (!MenuGeometry.ContainsKey(root)) MenuGeometry.Add(root, (root.localScale, root.localPosition));
        var original = MenuGeometry[root];
        root.localScale = original.scale;
        root.localPosition = original.position;
        var parts = new List<RectTransform>();
        if (ui.m_tabContainer != null)
            foreach (var button in ui.m_tabContainer.GetComponentsInChildren<Button>(false))
                parts.Add(button.GetComponent<RectTransform>());
        if (ui.m_searchField is Component field) parts.Add(field.GetComponent<RectTransform>());
        foreach (var scroll in ui.GetComponentsInChildren<ScrollRect>(false))
            if (scroll.viewport != null) parts.Add(scroll.viewport);
        foreach (var bar in ui.GetComponentsInChildren<Scrollbar>(false))
        {
            var rect = bar.GetComponent<RectTransform>();
            if (!BarGeometry.ContainsKey(rect))
                BarGeometry.Add(rect, (rect.anchorMin, rect.anchorMax, rect.pivot, rect.sizeDelta, rect.anchoredPosition3D));
            // Keep native sprites and handle length/value; only narrow the track.
            if (bar.direction is Scrollbar.Direction.BottomToTop or Scrollbar.Direction.TopToBottom)
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 10f);
            else rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 10f);
            parts.Add(rect);
        }
        if (!GridBounds(host, parts, out var min, out var max)) return footerTop;
        var available = new Vector2(host.rect.width - 72f, host.rect.height - footerTop - 54f);
        var size = max - min;
        var scale = Mathf.Min(1f, Mathf.Min(available.x / Mathf.Max(size.x, 1f), available.y / Mathf.Max(size.y, 1f)));
        root.localScale = original.scale * Mathf.Max(0.1f, scale);
        GridBounds(host, parts, out min, out max);
        gridWidth = max.x - min.x;
        // Centre horizontally, then sit the reserved card+hotbar envelope on the
        // hotbar so a short BuildUi is not glued to the top of the HUD.
        var target = new Vector2(host.rect.center.x, host.rect.yMax - 42f);
        var shift = new Vector3(target.x - (min.x + max.x) * 0.5f, target.y - max.y, 0f);
        root.position += host.TransformVector(shift);
        var gridBottom = min.y + shift.y - host.rect.yMin;
        if (gridBottom > footerTop)
        {
            var drop = footerTop - gridBottom;
            root.position += host.TransformVector(new Vector3(0f, drop, 0f));
            gridBottom = footerTop;
        }
        _fitSize = host.rect.size;
        _fitFooter = footerTop;
        _fitBottom = gridBottom;
        _fitWidth = gridWidth;
        return gridBottom;
    }

    private static bool GridBounds(RectTransform host, List<RectTransform> parts, out Vector2 min, out Vector2 max)
    {
        min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        var corners = new Vector3[4];
        foreach (var rect in parts)
        {
            if (rect == null || !rect.gameObject.activeInHierarchy) continue;
            rect.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var point = (Vector2)host.InverseTransformPoint(corner);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
        }
        return !float.IsInfinity(min.x) && max.x > min.x && max.y > min.y;
    }
}
