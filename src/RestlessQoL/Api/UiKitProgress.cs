using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.Api;

/// <summary>A pooled, non-interactive progress track. Set resets visibility and both
/// layers so a previous card's progress cannot leak into the next item.</summary>
public sealed class UiKitProgress
{
    public GameObject Root { get; }
    private readonly Image _total;
    private readonly Image _completed;
    internal UiKitProgress(Transform parent, string name)
    {
        Root = RestlessUi.Graphic(parent,name,RestlessUi.Ink,false);
        _total = RestlessUi.Graphic(Root.transform,"progress",UiKitApi.Accent,false).GetComponent<Image>();
        _completed = RestlessUi.Graphic(Root.transform,"completed",UiKitApi.Accent,false).GetComponent<Image>();
        Root.SetActive(false);
    }
    /// <summary>Completed is a subset of total (completed plus running), both 0–1.</summary>
    public void Set(Rect bounds, float completed, float total, Color totalColour, Color completedColour, bool visible = true)
    {
        total = Fraction(total); completed = Mathf.Min(Fraction(completed),total);
        UiKitApi.Place(Root,bounds);
        UiKitApi.Place(_total.gameObject,new Rect(0,0,Mathf.Max(0,bounds.width)*total,Mathf.Max(0,bounds.height)));
        UiKitApi.Place(_completed.gameObject,new Rect(0,0,Mathf.Max(0,bounds.width)*completed,Mathf.Max(0,bounds.height)));
        _total.color = totalColour; _completed.color = completedColour;
        Root.SetActive(visible);
    }
    public void Hide() => Root.SetActive(false);
    private static float Fraction(float value) => float.IsNaN(value) ? 0f : Mathf.Clamp01(value);
}
