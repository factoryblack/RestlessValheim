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
        _popup.AddComponent<CanvasGroup>().blocksRaycasts = false;
        var face = RestlessUi.Label(_popup.transform, Copy, RestlessUi.HudMeta, RestlessUi.Text, TextAnchor.MiddleCenter);
        RestlessUi.Stretch(face.gameObject, Vector2.zero, Vector2.one, new Vector2(16f, 8f), new Vector2(-16f, -8f));
        face.horizontalOverflow = HorizontalWrapMode.Wrap;
        face.verticalOverflow = VerticalWrapMode.Overflow;
        // Measure after assigning a bounded text width, then clamp the actual plate.
        var width = Mathf.Min(300f, Mathf.Max(48f, bounds.rect.width - 16f));
        face.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width - 32f);
        var height = Mathf.Max(52f, face.preferredHeight + 16f);
        var scale = Mathf.Min(1f, Mathf.Max(1f, bounds.rect.height - 16f) / height);
        var visualWidth = width * scale;
        var visualHeight = height * scale;
        var x = Mathf.Clamp(pos.x + 12f, bounds.rect.xMin + 8f,
            Mathf.Max(bounds.rect.xMin + 8f, bounds.rect.xMax - visualWidth - 8f));
        var y = Mathf.Clamp(pos.y - 12f, bounds.rect.yMin + visualHeight + 8f,
            Mathf.Max(bounds.rect.yMin + visualHeight + 8f, bounds.rect.yMax - 8f));
        RestlessUi.Pin(_popup, bounds.pivot, new Vector2(0f, 1f), new Vector2(x, y), new Vector2(width, height));
        RestlessUi.Stretch(face.gameObject, Vector2.zero, Vector2.one, new Vector2(16f, 8f), new Vector2(-16f, -8f));
        _popup.transform.localScale = Vector3.one * scale;
        _popup.transform.SetAsLastSibling();
    }
    public void OnPointerExit(PointerEventData data) => Close();
    private void OnDisable() => Close();
    private void OnDestroy() => Close();
    private void Close() { if (_popup != null) Destroy(_popup); _popup = null; }
}
