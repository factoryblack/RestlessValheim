using System;

namespace RestlessQoL.Core;

// Pure arithmetic shared by the collector and the regression checks. All inputs
// are live values; none of these methods changes a player or status effect.
internal static class LoadoutMath
{
    internal static float Armor(float current, float flat, float bonus) => (current + flat) * (1f + bonus);
    internal static float Damage(float current, float multiplier, float typeBonus) => current * multiplier * (1f + typeBonus);
    internal static float Regen(float current, float multiplier) => multiplier > 1f
        ? current + multiplier - 1f : current * multiplier;
    internal static float Residual(float actual, float accounted) => actual - accounted;
    internal static bool Changed(float value) => Math.Abs(value) > 0.0001f;
}
