using System;

namespace RestlessCook;

// Bounded active simulation time: unloaded kitchens never finish via wall-clock catch-up.
internal static class PreparationClock
{
    internal static float Advance(float elapsed, float duration, float delta, bool available)
    {
        if (duration <= 0 || float.IsNaN(duration) || float.IsInfinity(duration)) throw new ArgumentOutOfRangeException(nameof(duration));
        if (float.IsNaN(elapsed) || float.IsInfinity(elapsed)) elapsed = 0;
        elapsed = Math.Max(0, Math.Min(duration, elapsed));
        if (!available || delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return elapsed;
        return Math.Min(duration, elapsed + Math.Min(2f, delta));
    }
}
