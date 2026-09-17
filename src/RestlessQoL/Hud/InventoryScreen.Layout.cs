using System.Collections.Generic;
using System.Reflection;
using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

public sealed partial class InventoryScreen
{
    private sealed class CraftRect
    {
        public Vector2 Min, Max, Pivot, Size;
        public Vector3 Position, Scale;
        public CraftRect(RectTransform r)
        {
            Min = r.anchorMin; Max = r.anchorMax; Pivot = r.pivot;
            Size = r.sizeDelta; Position = r.anchoredPosition3D; Scale = r.localScale;
        }
        public void Restore(RectTransform r)
        {
            r.anchorMin = Min; r.anchorMax = Max; r.pivot = Pivot;
            r.sizeDelta = Size; r.anchoredPosition3D = Position; r.localScale = Scale;
        }
    }

    private static readonly Dictionary<RectTransform, CraftRect> CraftRects = new();
    private static void RememberCraftRect(RectTransform r)
    {
        if (!CraftRects.ContainsKey(r)) CraftRects.Add(r, new CraftRect(r));
    }

    // Unlike Place, this also accepts a plain RectTransform or Text without an Image.
    private static void CraftBounds(RectTransform r, float left, float bottom, float right, float top)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.position = new Vector3((left + right) * 0.5f, (bottom + top) * 0.5f, r.position.z);
        r.sizeDelta = new Vector2((right - left) / Mathf.Max(0.01f, Mathf.Abs(r.lossyScale.x)),
            (top - bottom) / Mathf.Max(0.01f, Mathf.Abs(r.lossyScale.y)));
    }

    private static float LayoutCraftIdentity(InventoryGui gui, float left, float top, float right, float sx, float sy)
    {
        const float inset = 24f, portrait = 64f, gap = 18f;
        var height = portrait;
        if (gui.m_recipeIcon != null)
        {
            var r = gui.m_recipeIcon.rectTransform;
            RememberCraftRect(r);
            CraftBounds(r, left + inset * sx, top - (inset + portrait) * sy,
                left + (inset + portrait) * sx, top - inset * sy);
        }
        var name = gui.m_recipeName != null
            ? gui.m_recipeName.transform.parent.Find("Restless_recipeName")?.GetComponent<Text>() : null;
        if (name != null)
        {
            name.fontSize = 24;
            name.alignment = TextAnchor.UpperLeft;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Overflow;
            CraftBounds(name.rectTransform, left + (inset + portrait + gap) * sx,
                top - (inset + portrait) * sy, right - inset * sx, top - inset * sy);
            height = Mathf.Max(portrait, name.preferredHeight);
            CraftBounds(name.rectTransform, left + (inset + portrait + gap) * sx,
                top - (inset + height) * sy, right - inset * sx, top - inset * sy);
        }
        return top - (inset + height + gap) * sy;
    }

    private static void LayoutMaterialCell(Transform plate, Image? icon, Text face)
    {
        if (icon == null || !WorldBox(plate, out var x0, out var y0, out var x1, out var y1)) return;
        var sx = Mathf.Abs(plate.lossyScale.x);
        var sy = Mathf.Abs(plate.lossyScale.y);
        RememberCraftRect(icon.rectTransform);
        // Native item image stays above its own count strip, never behind the number.
        CraftBounds(icon.rectTransform, x0 + 8f * sx, y0 + 27f * sy, x1 - 8f * sx, y1 - 5f * sy);
        var strip = plate.Find("RestlessCountStrip")?.gameObject;
        if (strip == null)
        {
            strip = RestlessUi.Graphic(plate, "RestlessCountStrip", new Color(0.08f, 0.07f, 0.06f, 0.8f), false);
            strip.transform.SetAsFirstSibling();
        }
        RestlessUi.Stretch(strip, Vector2.zero, new Vector2(1f, 0f), new Vector2(4f, 3f), new Vector2(-4f, 27f));
        face.alignment = TextAnchor.MiddleCenter;
        RestlessUi.BoundedLabel(face, 20, RestlessUi.HintSize);
        RestlessUi.Stretch(face.gameObject, Vector2.zero, new Vector2(1f, 0f), new Vector2(4f, 3f), new Vector2(-4f, 27f));
    }

    private static void LayoutStationBadge(InventoryGui gui)
    {
        var src = gui.m_craftingStationLevel;
        if (src == null || src.transform.parent == null) return;
        var face = src.transform.parent.Find("Restless_stationLevel")?.GetComponent<Text>();
        if (face == null) return;
        face.fontSize = 22;
        face.alignment = TextAnchor.MiddleCenter;
        face.horizontalOverflow = HorizontalWrapMode.Overflow;
        RestlessUi.Stretch(face.gameObject, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));
    }

    private static void DressPvpArtwork(Transform node)
    {
        if (RestlessUi.Deep(node, "RestlessPvpArtwork") != null) return;
        var toggle = node.GetComponent<Toggle>() ?? node.GetComponentInChildren<Toggle>(true);
        var input = toggle != null ? toggle.transform
            : node.GetComponentInChildren<Button>(true)?.transform ?? node;
        // Preserve native hit targets and toggle state while suppressing only their paint.
        foreach (var image in node.GetComponentsInChildren<Image>(true))
            if (!image.transform.name.StartsWith("Restless")) Ghost(image);
        var go = RestlessUi.Graphic(node, "RestlessPvpArtwork", Color.clear, true);
        Ours.Add(go);
        RestlessUi.Stretch(go, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        // If a prefab nests its control, raycasts must still reach that native target.
        if (input != node)
        {
            go.transform.SetParent(input, false);
            RestlessUi.Stretch(go, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }
        var icon = RestlessUi.Picture(go.transform, "RestlessSwords", "nav-swords");
        RestlessUi.Pin(icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 44f));
        var slash = RestlessUi.Graphic(go.transform, "RestlessOffSlash", RestlessUi.PaperMuted, false);
        RestlessUi.Pin(slash, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 3f));
        slash.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
        var state = go.AddComponent<RestlessPvpArtwork>();
        state.Toggle = toggle;
        state.Icon = icon.GetComponent<Image>();
        state.Slash = slash;
    }

    private static void RestoreCraftLayout()
    {
        foreach (var pair in CraftRects)
            if (pair.Key != null) pair.Value.Restore(pair.Key);
        CraftRects.Clear();
    }
}

internal sealed class RestlessPvpArtwork : MonoBehaviour
{
    public Toggle? Toggle;
    public Image Icon = null!;
    public GameObject Slash = null!;
    private static readonly MethodInfo? PvpState = typeof(Player).GetMethod("IsPVPEnabled",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, System.Type.EmptyTypes, null);
    private void LateUpdate()
    {
        // Read the native toggle where present; older prefabs expose the player state.
        var on = Toggle != null ? Toggle.isOn
            : Player.m_localPlayer != null && PvpState != null && PvpState.Invoke(Player.m_localPlayer, null) is bool active && active;
        Icon.color = on ? Color.white : new Color(0.42f, 0.47f, 0.52f, 0.65f);
        Slash.SetActive(!on);
    }
}
