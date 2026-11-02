namespace RestlessQoL.Core;

/// <summary>Startup policy. Changing the saved setting takes effect on restart,
/// so no optional hooks, event subscriptions or ticks can leak into foundation mode.</summary>
public static class CoreFeatures
{
    public static bool OptionalEnabled { get; private set; } = true;

    internal static void Configure(bool enabled) => OptionalEnabled = enabled;

    internal static bool ShouldLoad(bool foundation, bool enabled) =>
        enabled && (foundation || OptionalEnabled);
}
