using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RestlessQoL.Core;

// Wheel travel in UI units, without inertial drift or dependency on content length.
public sealed class RestlessScrollRect : ScrollRect
{
    public override void OnScroll(PointerEventData eventData)
    {
        if (!IsActive() || content == null || viewport == null) return;
        var overflow = Mathf.Max(0f, content.rect.height - viewport.rect.height);
        StopMovement();
        var pos = content.anchoredPosition;
        pos.y = Mathf.Clamp(pos.y - eventData.scrollDelta.y * 56f, 0f, overflow);
        content.anchoredPosition = pos;
        eventData.Use();
    }
}
