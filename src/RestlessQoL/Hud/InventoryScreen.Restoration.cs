using System.Collections.Generic;
using RestlessQoL.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

public sealed partial class InventoryScreen
{
    private sealed class NativePaint
    {
        public Color Colour;
        public bool Enabled, Raycast;
        public int Visible;
        public readonly Dictionary<CanvasRenderer, float> Alphas = new();
        public NativePaint(Graphic graphic)
        {
            Colour = graphic.color;
            Enabled = graphic.enabled;
            Raycast = graphic.raycastTarget;
            if (graphic is TMP_Text text) Visible = text.maxVisibleCharacters;
            foreach (var renderer in graphic.GetComponentsInChildren<CanvasRenderer>(true))
                if (!RestlessUi.Owned(renderer.transform)) Alphas[renderer] = renderer.GetAlpha();
        }
        public void Restore(Graphic graphic)
        {
            graphic.color = Colour;
            graphic.enabled = Enabled;
            graphic.raycastTarget = Raycast;
            if (graphic is TMP_Text text) text.maxVisibleCharacters = Visible;
            foreach (var pair in Alphas)
                if (pair.Key != null) pair.Key.SetAlpha(pair.Value);
        }
    }

    private static readonly Dictionary<Graphic, NativePaint> NativePaints = new();
    private static readonly Dictionary<GameObject, bool> NativeActive = new();

    private static readonly HashSet<GameObject> NativeCells = new();
    private static readonly Dictionary<Behaviour, bool> NativeEnabled = new();

    private static void RememberCell(GameObject cell, Image? icon)
    {
        if (!NativeCells.Add(cell)) return;
        foreach (var graphic in cell.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic == icon || RestlessUi.Owned(graphic.transform)) continue;
            RememberPaint(graphic);
            if (!NativeActive.ContainsKey(graphic.gameObject)) NativeActive.Add(graphic.gameObject, graphic.gameObject.activeSelf);
        }
        foreach (var bar in cell.GetComponentsInChildren<GuiBar>(true))
        {
            if (!NativeEnabled.ContainsKey(bar)) NativeEnabled.Add(bar, bar.enabled);
            if (!NativeActive.ContainsKey(bar.gameObject)) NativeActive.Add(bar.gameObject, bar.gameObject.activeSelf);
        }
    }

    private static readonly Dictionary<GameObject, (CanvasGroup? group, float alpha, bool blocks, bool interactable)> NativeGroups = new();
    private static void RememberGroup(GameObject go)
    {
        if (!RestlessUi.Owned(go.transform) && !NativeGroups.ContainsKey(go))
        {
            var group = go.GetComponent<CanvasGroup>();
            NativeGroups.Add(go, (group, group != null ? group.alpha : 1f,
                group == null || group.blocksRaycasts, group == null || group.interactable));
        }
    }

    private static void QuietNative(GameObject go)
    {
        RememberGroup(go);
        RestlessUi.Quiet(go);
    }

    private static void LoudNative(GameObject go)
    {
        RememberGroup(go);
        RestlessUi.Loud(go);
    }

    private static void RememberPaint(Graphic graphic)
    {
        if (!RestlessUi.Owned(graphic.transform) && !NativePaints.ContainsKey(graphic))
            NativePaints.Add(graphic, new NativePaint(graphic));
    }

    private static void ShelfNative(GameObject go)
    {
        if (!RestlessUi.Owned(go.transform) && !NativeActive.ContainsKey(go))
            NativeActive.Add(go, go.activeSelf);
        go.SetActive(false);
    }

    private static void RestoreNativePaint()
    {
        foreach (var pair in NativePaints)
            if (pair.Key != null) pair.Value.Restore(pair.Key);
        foreach (var pair in NativeActive)
            if (pair.Key != null) pair.Key.SetActive(pair.Value);
        foreach (var pair in NativeEnabled)
            if (pair.Key != null) pair.Key.enabled = pair.Value;
        foreach (var pair in NativeGroups)
        {
            if (pair.Key == null) continue;
            var state = pair.Value;
            var group = pair.Key.GetComponent<CanvasGroup>();
            if (group == null) continue;
            group.alpha = state.alpha;
            group.blocksRaycasts = state.blocks;
            group.interactable = state.interactable;
            if (state.group == null) Object.Destroy(group);
        }
        NativeGroups.Clear();
        NativeCells.Clear();
        NativeEnabled.Clear();
        NativePaints.Clear();
        NativeActive.Clear();
    }
}
