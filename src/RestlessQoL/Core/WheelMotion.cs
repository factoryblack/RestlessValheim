using System;

namespace RestlessQoL.Core;

// UI-unit wheel motion, independent of Unity so input and timing can be tested.
internal sealed class WheelMotion
{
    private const float Duration = 0.10f;
    private float _start, _elapsed;
    public float Position { get; private set; }
    public float Target { get; private set; }
    public bool Active { get; private set; }

    public void Cancel(float position)
    {
        Position = Target = _start = position;
        _elapsed = 0f;
        Active = false;
    }

    public float Push(float current, float delta, float maximum)
    {
        maximum = Math.Max(0f, maximum);
        current = Clamp(current, maximum);
        // Reversal discards outstanding travel in the old direction.
        var origin = Active && (Target - current) * delta > 0f ? Target : current;
        Target = Clamp(origin + delta, maximum);
        Position = current + (Target - current) * 0.25f;
        _start = Position;
        _elapsed = 0f;
        Active = Math.Abs(Target - Position) > 0.01f;
        return Position;
    }

    public float Advance(float seconds, float maximum)
    {
        maximum = Math.Max(0f, maximum);
        Target = Clamp(Target, maximum);
        _start = Clamp(_start, maximum);
        _elapsed = Math.Min(Duration, _elapsed + Math.Max(0f, seconds));
        var remaining = 1f - _elapsed / Duration;
        Position = Clamp(Target + (_start - Target) * remaining * remaining * remaining, maximum);
        Active = _elapsed < Duration && Math.Abs(Target - Position) > 0.01f;
        if (!Active) Position = Target;
        return Position;
    }

    private static float Clamp(float value, float maximum) => Math.Max(0f, Math.Min(value, maximum));
}
