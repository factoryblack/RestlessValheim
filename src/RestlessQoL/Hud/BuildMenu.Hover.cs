using System.Collections.Generic;
using RestlessQoL.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

public sealed partial class BuildMenu
{
    private sealed class HoverCopy
    {
        public TMP_Text Source = null!;
        public Text Face = null!;
        public float Alpha;
    }

    private static readonly List<HoverCopy> HoverCopies = new();
    private static readonly List<GameObject> HoverOwned = new();
    private static GameObject? _hoverPaper;

    // Read the native HUD output, including resource counts supplied by other
    // systems. Do not recalculate recipe costs or manufacture a station badge.
    private static void DressHover(global::Hud hud)
    {
        if (!global::Hud.IsPieceSelectionVisible() || hud.m_buildHud == null
            || !hud.m_buildHud.activeInHierarchy || hud.m_buildSelection == null
            || !hud.m_buildSelection.gameObject.activeInHierarchy
            || string.IsNullOrWhiteSpace(hud.m_buildSelection.text))
        {
            ClearHover();
            return;
        }

        var root = hud.m_buildHud.transform;
        var bounds = new List<RectTransform>();
        var live = new HashSet<TMP_Text>();
        AddHoverText(hud.m_buildSelection, RestlessUi.TitleSize, TextAnchor.MiddleCenter, live, bounds);
        AddHoverText(hud.m_pieceDescription, RestlessUi.BodySize + 2, TextAnchor.UpperLeft, live, bounds);
        if (hud.m_buildIcon != null && hud.m_buildIcon.gameObject.activeInHierarchy && hud.m_buildIcon.enabled)
            bounds.Add(hud.m_buildIcon.rectTransform);

        // Requirements are outside BuildUi's grid. Preserve the original icons,
        // amounts, station names, visibility and shortage colours in their slots.
        foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (Owned(tmp.transform) || !tmp.gameObject.activeInHierarchy) continue;
            if (hud.m_buildUi != null && tmp.transform.IsChildOf(hud.m_buildUi.transform)) continue;
            var name = tmp.name.ToLowerInvariant();
            if (name is not ("res_name" or "res_amount")) continue;
            AddHoverText(tmp, name == "res_amount" ? RestlessUi.BodySize : RestlessUi.HintSize,
                TextAnchor.MiddleCenter, live, bounds);
            // Include the whole native requirement slot so its icon is framed too.
            if (tmp.transform.parent is RectTransform slot && slot.rect.width < 220f && slot.rect.height < 180f)
                bounds.Add(slot);
        }

        foreach (var row in HoverCopies)
        {
            if (row.Source == null || row.Face == null) continue;
            var show = live.Contains(row.Source);
            row.Face.gameObject.SetActive(show);
            if (!show) row.Source.alpha = row.Alpha;
        }

        if (_hoverPaper == null)
        {
            _hoverPaper = RestlessUi.Graphic(root, "RestlessBuildHoverPaper", Color.white, false);
            _hoverPaper.AddComponent<LayoutElement>().ignoreLayout = true;
            RestlessUi.PaperSurface(_hoverPaper);
            HoverOwned.Add(_hoverPaper);
        }
        _hoverPaper.transform.SetAsFirstSibling();
        FitHoverPaper(_hoverPaper.GetComponent<RectTransform>(), root, bounds);
    }

    private static void AddHoverText(TMP_Text? source, int size, TextAnchor alignment,
        HashSet<TMP_Text> live, List<RectTransform> bounds)
    {
        if (source == null || !source.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(source.text)) return;
        live.Add(source);
        HoverCopy? copy = null;
        foreach (var row in HoverCopies)
            if (row.Source == source) { copy = row; break; }
        if (copy == null)
        {
            var face = RestlessUi.Label(source.transform.parent, "", size, RestlessUi.Text, alignment);
            face.name = "RestlessBuildHover" + source.GetInstanceID();
            face.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            HoverOwned.Add(face.gameObject);
            copy = new HoverCopy { Source = source, Face = face, Alpha = source.alpha };
            HoverCopies.Add(copy);
        }
        var label = copy.Face;
        label.gameObject.SetActive(true);
        RestlessUi.CopyRect(label.rectTransform, source.rectTransform);
        label.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);
        label.alignment = alignment;
        label.text = source.text; // Keep rich-text shortage colours and live localization.
        label.supportRichText = true;
        var color = source.color;
        // Neutral vanilla copy becomes cream. Preserve red/green shortage feedback.
        var chroma = Mathf.Max(color.r, Mathf.Max(color.g, color.b)) - Mathf.Min(color.r, Mathf.Min(color.g, color.b));
        label.color = chroma < 0.15f || source == global::Hud.instance?.m_buildSelection
            ? RestlessUi.Text : new Color(color.r, color.g, color.b, copy.Alpha);
        RestlessUi.BoundedLabel(label, size, RestlessUi.HintSize);
        label.raycastTarget = false;
        source.alpha = 0f;
        bounds.Add(label.rectTransform);
    }

    private static void FitHoverPaper(RectTransform plate, Transform root, List<RectTransform> parts)
    {
        var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        var corners = new Vector3[4];
        foreach (var rect in parts)
        {
            rect.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var p = (Vector2)root.InverseTransformPoint(corner);
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }
        }
        if (float.IsInfinity(min.x)) { plate.gameObject.SetActive(false); return; }
        plate.gameObject.SetActive(true);
        plate.anchorMin = plate.anchorMax = new Vector2(0.5f, 0.5f);
        plate.pivot = new Vector2(0.5f, 0.5f);
        plate.localPosition = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, 0f);
        plate.sizeDelta = max - min + new Vector2(36f, 28f);
    }

    private static void ClearHover()
    {
        foreach (var row in HoverCopies)
            if (row.Source != null) row.Source.alpha = row.Alpha;
        HoverCopies.Clear();
        foreach (var go in HoverOwned)
            if (go != null) Object.Destroy(go);
        HoverOwned.Clear();
        _hoverPaper = null;
    }
}
