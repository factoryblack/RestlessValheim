using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RestlessQoL.Api;

/// <summary>Client-side tooltip contributions. Call on Unity's main thread.</summary>
public static class TooltipApi
{
    public const int Version = 1;
    private static readonly Dictionary<string, Registration> Providers = new();
    internal static int Revision { get; private set; }

    /// <summary>Register once during plugin startup; dispose during plugin shutdown.
    /// Providers must be cheap, read-only, and return null for unrelated items.</summary>
    public static IDisposable Register(string id, Func<ItemDrop.ItemData, TooltipContribution?> provider,
        int order = 0)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A namespaced provider ID is required.", nameof(id));
        if (provider == null) throw new ArgumentNullException(nameof(provider));
        if (Providers.ContainsKey(id)) throw new ArgumentException("Tooltip provider already registered: " + id, nameof(id));
        var registration = new Registration(id, provider, order);
        Providers.Add(id, registration);
        Invalidate();
        return registration;
    }

    /// <summary>Request a refresh after your item's presentation data changes.</summary>
    public static void Invalidate() { unchecked { Revision++; } }

    internal static List<TooltipContribution> Collect(ItemDrop.ItemData item)
    {
        var result = new List<TooltipContribution>();
        // Snapshot permits providers to unregister during a callback.
        foreach (var registration in Providers.Values.OrderBy(p => p.Order)
                     .ThenBy(p => p.Id, StringComparer.Ordinal).ToArray())
        {
            if (registration.Disposed) continue;
            try
            {
                var contribution = registration.Provider(item);
                if (contribution != null) result.Add(contribution);
            }
            catch (Exception error)
            {
                // A broken extension must not hide the base item's information or flood the log.
                if (!registration.Warned)
                {
                    registration.Warned = true;
                    Plugin.Log.LogWarning("tooltip provider " + registration.Id + ": " + error);
                }
            }
        }
        return result;
    }

    private sealed class Registration : IDisposable
    {
        internal readonly string Id;
        internal readonly Func<ItemDrop.ItemData, TooltipContribution?> Provider;
        internal readonly int Order;
        internal bool Warned;
        internal bool Disposed;
        internal Registration(string id, Func<ItemDrop.ItemData, TooltipContribution?> provider, int order)
        { Id = id; Provider = provider; Order = order; }
        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            Providers.Remove(Id);
            Invalidate();
        }
    }
}

/// <summary>Optional presentation only; this API does not implement rarity or gameplay rules.
/// Strings may contain Valheim localisation tokens. Rich text is not rendered.</summary>
public sealed class TooltipContribution
{
    public IReadOnlyList<TooltipBadge> Badges { get; }
    public IReadOnlyList<TooltipSection> Sections { get; }
    /// <summary>Optional inspect rim tint. First non-null tint in provider order wins.</summary>
    public Color? RimTint { get; }
    public TooltipContribution(IEnumerable<TooltipBadge>? badges = null,
        IEnumerable<TooltipSection>? sections = null, Color? rimTint = null)
    {
        Badges = Array.AsReadOnly((badges ?? Array.Empty<TooltipBadge>()).Where(x => x != null).ToArray());
        Sections = Array.AsReadOnly((sections ?? Array.Empty<TooltipSection>()).Where(x => x != null).ToArray());
        RimTint = rimTint;
    }
}

public sealed class TooltipBadge
{
    public string Text { get; }
    public Color? Tint { get; }
    public TooltipBadge(string text, Color? tint = null) { Text = text ?? ""; Tint = tint; }
}

public sealed class TooltipSection
{
    public string Title { get; }
    public string Body { get; }
    public IReadOnlyList<TooltipStat> Rows { get; }
    public TooltipSection(string title, IEnumerable<TooltipStat>? rows = null, string body = "")
    {
        Title = title ?? "";
        Body = body ?? "";
        Rows = Array.AsReadOnly((rows ?? Array.Empty<TooltipStat>()).Where(x => x != null).ToArray());
    }
}

public sealed class TooltipStat
{
    public string Label { get; }
    public string Value { get; }
    public TooltipStat(string label, string value) { Label = label ?? ""; Value = value ?? ""; }
}
