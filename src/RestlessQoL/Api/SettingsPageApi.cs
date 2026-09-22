using System;
using System.Collections.Generic;
using UnityEngine;

namespace RestlessQoL.Api;

/// <summary>Optional ecosystem pages. Register on Unity's main thread at startup;
/// dispose at shutdown. Gameplay settings remain owned/synchronised by each mod.</summary>
public static class SettingsPageApi
{
    private static readonly Dictionary<string, SettingsPage> Pages = new();

    public static IDisposable Register(SettingsPage page)
    {
        if (page == null) throw new ArgumentNullException(nameof(page));
        if (Pages.ContainsKey(page.PluginGuid)) throw new ArgumentException("Page already registered: " + page.PluginGuid);
        Pages.Add(page.PluginGuid, page);
        return new Registration(page.PluginGuid);
    }

    internal static SettingsPage[] Snapshot()
    {
        var pages = new List<SettingsPage>(Pages.Values);
        pages.Sort((a, b) => StringComparer.Ordinal.Compare(a.PluginGuid, b.PluginGuid));
        return pages.ToArray();
    }

    private sealed class Registration : IDisposable
    {
        private string? _id;
        internal Registration(string id) { _id = id; }
        public void Dispose()
        {
            if (_id == null) return;
            Pages.Remove(_id);
            _id = null;
        }
    }
}

/// <summary>Display data, with an optional explicit action to open the mod's own
/// settings. Icons are borrowed: the registering mod retains ownership.</summary>
public sealed class SettingsPage
{
    public string PluginGuid { get; }
    public string Name { get; }
    public string Summary { get; }
    public string Details { get; }
    public Sprite? Icon { get; }
    public string? PackageUrl { get; }
    public Action? OpenSettings { get; }

    public SettingsPage(string pluginGuid, string name, string summary, string details,
        Sprite? icon = null, string? packageUrl = null, Action? openSettings = null)
    {
        if (string.IsNullOrWhiteSpace(pluginGuid) || !pluginGuid.Contains(".")) throw new ArgumentException("Plugin GUID required.", nameof(pluginGuid));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Page name required.", nameof(name));
        if (packageUrl != null && (!Uri.TryCreate(packageUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https"))
            throw new ArgumentException("Package links must use HTTPS.", nameof(packageUrl));
        PluginGuid = pluginGuid; Name = name; Summary = summary ?? "";
        Details = details ?? ""; Icon = icon; PackageUrl = packageUrl; OpenSettings = openSettings;
    }
}
