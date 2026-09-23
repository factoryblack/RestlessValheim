using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace RestlessQoL.Api;

public static partial class SettingsPageApi
{
    private static readonly Dictionary<string, IReadOnlyList<SettingsSection>> Controls = new();
    internal static int ControlsRevision { get; private set; }
    public static bool IsMenuOpen => Core.SettingsUi.IsOpen;

    /// <summary>Register existing config entries; Core only presents them. Register
    /// after binding config, on Unity's main thread, and dispose on shutdown.</summary>
    public static IDisposable RegisterSettings(string pluginGuid, params SettingsSection[] sections)
    {
        if (string.IsNullOrWhiteSpace(pluginGuid) || !pluginGuid.Contains("."))
            throw new ArgumentException("Plugin GUID required.", nameof(pluginGuid));
        if (sections == null) throw new ArgumentNullException(nameof(sections));
        if (Controls.ContainsKey(pluginGuid)) throw new ArgumentException("Settings already registered: " + pluginGuid);
        foreach (var section in sections)
            if (section == null) throw new ArgumentException("Null section.", nameof(sections));
        Controls.Add(pluginGuid, Array.AsReadOnly((SettingsSection[])sections.Clone()));
        unchecked { ControlsRevision++; }
        return new ControlsRegistration(pluginGuid);
    }

    internal static IReadOnlyList<SettingsSection>? SettingsFor(string guid) =>
        Controls.TryGetValue(guid, out var sections) ? sections : null;

    private sealed class ControlsRegistration : IDisposable
    {
        private string? _guid;
        internal ControlsRegistration(string guid) { _guid = guid; }
        public void Dispose()
        {
            if (_guid == null) return;
            Controls.Remove(_guid);
            _guid = null;
            unchecked { ControlsRevision++; }
        }
    }
}

public sealed class SettingsSection
{
    public string Title { get; }
    public IReadOnlyList<SettingsOption> Options { get; }
    public SettingsSection(string title, params SettingsOption[] options)
    {
        Title = title ?? "";
        if (options == null) throw new ArgumentNullException(nameof(options));
        foreach (var option in options)
            if (option == null) throw new ArgumentException("Null option.", nameof(options));
        Options = Array.AsReadOnly((SettingsOption[])options.Clone());
    }
}

public sealed class SettingsOption
{
    public string Label { get; }
    public ConfigEntryBase Entry { get; }
    public bool HostControlled { get; }
    public string Unit { get; }
    public bool RequiresRestart { get; }
    public Func<bool>? Visible { get; }

    /// <summary>HostControlled must match the expansion's sync policy. Core does
    /// not create RPCs or alter config metadata. Numeric entries require ranges.
    /// Visible is rechecked while the page is open; null means always shown.</summary>
    public SettingsOption(string label, ConfigEntryBase entry, bool hostControlled, string unit = "", bool requiresRestart = false)
        : this(label, entry, hostControlled, unit, requiresRestart, null) {}

    // Five-argument form stays. Plant 0.1.5 and Piles 0.1.5 on Thunderstore call it.
    public SettingsOption(string label, ConfigEntryBase entry, bool hostControlled, string unit, bool requiresRestart, Func<bool>? visible)
    {
        Label = label ?? "";
        Entry = entry ?? throw new ArgumentNullException(nameof(entry));
        HostControlled = hostControlled;
        Unit = unit ?? "";
        RequiresRestart = requiresRestart;
        Visible = visible;
        if (entry is ConfigEntry<bool> || entry is ConfigEntry<KeyboardShortcut>) return;
        if (entry is ConfigEntry<float> && entry.Description.AcceptableValues is AcceptableValueRange<float>) return;
        if (entry is ConfigEntry<int> && entry.Description.AcceptableValues is AcceptableValueRange<int>) return;
        throw new ArgumentException("Supported controls: bool, keybind, ranged int or float.", nameof(entry));
    }
}
