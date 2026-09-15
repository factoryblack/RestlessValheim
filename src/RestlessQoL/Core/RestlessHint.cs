using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RestlessQoL.Core;

// Small reusable explanatory hint for interactive emblems and controls.
public sealed class RestlessHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string Copy = "";
    private GameObject? _popup;
    public void OnPointerEnter(PointerEventData data)
    {
        Close();
        var canvas = GetComponentInParent<Canvas>()?.rootCanvas;
        if (canvas == null || string.IsNullOrWhiteSpace(Copy)) return;
        var bounds = canvas.GetComponent<RectTransform>();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds, data.position, data.enterEventCamera, out var pos)) return;
        _popup = RestlessUi.Chip(bounds, "RestlessHintPopup");
        RestlessUi.ForgedSurface(_popup);
        var face = RestlessUi.Label(_popup.transform, Copy, RestlessUi.HudMeta, RestlessUi.Text, TextAnchor.MiddleCenter);
        RestlessUi.Stretch(face.gameObject, Vector2.zero, Vector2.one, new Vector2(16f, 8f), new Vector2(-16f, -8f));
        face.horizontalOverflow = HorizontalWrapMode.Wrap;
        var x = Mathf.Clamp(pos.x + 12f, bounds.rect.xMin + 8f, bounds.rect.xMax - 208f);
        var y = Mathf.Clamp(pos.y - 12f, bounds.rect.yMin + 60f, bounds.rect.yMax - 8f);
        RestlessUi.Pin(_popup, bounds.pivot, new Vector2(0f, 1f), new Vector2(x, y), new Vector2(200f, 52f));
        _popup.transform.SetAsLastSibling();
    }
    public void OnPointerExit(PointerEventData data) => Close();
    private void OnDisable() => Close();
    private void OnDestroy() => Close();
    private void Close() { if (_popup != null) Destroy(_popup); _popup = null; }
}
