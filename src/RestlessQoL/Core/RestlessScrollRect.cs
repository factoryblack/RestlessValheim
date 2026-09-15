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
        if (overflow <= 0f || Mathf.Abs(eventData.scrollDelta.y) < 0.01f) return;
        StopMovement();
        var pos = content.anchoredPosition;
        // One notch ≈ one viewport, not a fraction of a long recipe.
        var travel = Mathf.Max(viewport.rect.height, 120f);
        pos.y = Mathf.Clamp(pos.y - Mathf.Sign(eventData.scrollDelta.y) * travel, 0f, overflow);
        content.anchoredPosition = pos;
        eventData.Use();
    }
}
