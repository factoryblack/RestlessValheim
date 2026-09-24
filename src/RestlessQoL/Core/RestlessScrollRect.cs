using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RestlessQoL.Core;

// Opt-in for owned vertical readers. Native bars and direct dragging are retained.
public sealed class RestlessScrollRect : ScrollRect
{
    public float RowHeight = 32f;
    public float RowsPerNotch = 4f;
    private readonly WheelMotion _wheel = new();
    private Vector2 _lastPosition;
    public bool IsWheelMoving => _wheel.Active;

    protected override void Awake()
    {
        base.Awake();
        inertia = false;
        movementType = MovementType.Clamped;
    }

    public override void OnScroll(PointerEventData eventData)
    {
        if (eventData.used || !IsActive() || content == null || viewport == null) return;
        if (!vertical || horizontal) { base.OnScroll(eventData); return; }
        var delta = eventData.scrollDelta.y;
        if (Mathf.Approximately(delta, 0f)) return;
        var maximum = Mathf.Max(0f, content.rect.height - viewport.rect.height);
        StopMovement();
        if (_wheel.Active && (content.anchoredPosition - _lastPosition).sqrMagnitude > 0.01f)
            CancelWheel();
        // Preserve fractional trackpad input and multiple notches in one event.
        var travel = Mathf.Min(Mathf.Max(1f, RowHeight * RowsPerNotch), Mathf.Max(1f, viewport.rect.height * 0.85f));
        SetWheelPosition(_wheel.Push(content.anchoredPosition.y, -delta * travel, maximum));
        // The innermost reader owns the event, including at its boundaries.
        eventData.Use();
    }

    protected override void LateUpdate()
    {
        if (_wheel.Active && content != null && viewport != null)
        {
            // Scrollbar dragging, focus navigation and content resets take priority.
            if ((content.anchoredPosition - _lastPosition).sqrMagnitude > 0.01f)
                CancelWheel();
            else
                SetWheelPosition(_wheel.Advance(Time.unscaledDeltaTime,
                    Mathf.Max(0f, content.rect.height - viewport.rect.height)));
        }
        base.LateUpdate();
    }

    public void CancelWheel()
    {
        _wheel.Cancel(content != null ? content.anchoredPosition.y : 0f);
        StopMovement();
    }

    private void SetWheelPosition(float y)
    {
        var position = content.anchoredPosition;
        position.y = y;
        SetContentAnchoredPosition(position);
        _lastPosition = content.anchoredPosition;
    }

    public override void OnInitializePotentialDrag(PointerEventData eventData)
    {
        CancelWheel();
        base.OnInitializePotentialDrag(eventData);
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        CancelWheel();
        base.OnBeginDrag(eventData);
    }

    protected override void OnDisable()
    {
        CancelWheel();
        base.OnDisable();
    }
}
