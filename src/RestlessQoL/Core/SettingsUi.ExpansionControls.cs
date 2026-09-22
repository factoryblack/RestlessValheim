using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using Jotunn.Managers;
using RestlessQoL.Api;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.Core;

public sealed partial class SettingsUi
{
    private static readonly List<Action> SettingUnsubscribe = new();
    private static readonly List<Action> SettingRefresh = new();
    private static volatile bool _settingsDirty;
    private static bool _settingsHost;
    private static bool _expHasHost;
    private static int _settingsRevision;
    private static Func<bool>? _captureEditable;

    // Addons are independently marked AdminOnly by Jotunn. Core's optional
    // LockConfiguration switch must not unlock another plugin's host settings.
    private static bool ExpansionHostCanEdit() => ZNet.instance == null || ZNet.instance.IsServer()
        || SynchronizationManager.Instance != null && SynchronizationManager.Instance.PlayerIsAdmin;

    private static void ClearSettingBindings()
    {
        foreach (var unsubscribe in SettingUnsubscribe) unsubscribe();
        SettingUnsubscribe.Clear();
        SettingRefresh.Clear();
        _settingsDirty = false;
        _expHasHost = false;
        _capturing = false;
        _captureEntry = null;
        _captureLabel = null;
        _captureEditable = null;
    }

    private static void ObserveSetting<T>(ConfigEntry<T> entry, Selectable control,
        Func<bool>? editable, Action repaint)
    {
        if (editable == null) return; // Core controls retain their existing behaviour.
        void Refresh()
        {
            if (control == null) return;
            control.interactable = editable();
            repaint();
        }
        EventHandler changed = (_, _) => _settingsDirty = true;
        entry.SettingChanged += changed;
        SettingUnsubscribe.Add(() => entry.SettingChanged -= changed);
        SettingRefresh.Add(Refresh);
        Refresh();
    }

    private static void RefreshSettingBindings()
    {
        if (_closing) return;
        if (_settingsRevision != SettingsPageApi.ControlsRevision)
        {
            Rebuild(); // Module registration/disposal, never routine value edits.
            return;
        }
        if (SettingRefresh.Count == 0) return;
        var host = ExpansionHostCanEdit();
        if (!_settingsDirty && host == _settingsHost) return;
        _settingsHost = host;
        _settingsDirty = false;
        foreach (var refresh in SettingRefresh) refresh();
        UpdateLock();
    }

    private static bool PaintExpansionSettings(SettingsPage page)
    {
        var sections = SettingsPageApi.SettingsFor(page.PluginGuid);
        if (sections == null || sections.Count == 0) return false;
        foreach (var section in sections)
        {
            var host = false;
            var local = false;
            var restart = false;
            foreach (var option in section.Options)
                if (option.HostControlled) host = true; else local = true;
            foreach (var option in section.Options) restart |= option.RequiresRestart;
            Head(section.Title);
            if (host)
            {
                _expHasHost = true;
                EcosystemCopy("Host-controlled gameplay · only the host or an admin can change these settings.");
            }
            if (local) EcosystemCopy("Personal settings · saved for you.");
            if (restart) EcosystemCopy("Settings marked (restart) apply fully after restarting the game/server.");
            foreach (var option in section.Options)
            {
                Func<bool> editable = () => !_closing && Loaded(page)
                    && ReferenceEquals(sections, SettingsPageApi.SettingsFor(page.PluginGuid))
                    && (!option.HostControlled || ExpansionHostCanEdit());
                var label = option.Label + (option.RequiresRestart ? " (restart)" : "");
                var before = _body!.transform.childCount;
                switch (option.Entry)
                {
                    case ConfigEntry<bool> entry:
                        Bool(label, entry, !editable(), editable: editable);
                        break;
                    case ConfigEntry<float> entry:
                        Step(label, option.Unit, entry, !editable(), editable: editable);
                        break;
                    case ConfigEntry<int> entry:
                        IntegerStep(label, option.Unit, entry, editable);
                        break;
                    case ConfigEntry<KeyboardShortcut> entry:
                        KeyOnly(label, entry, editable: editable);
                        break;
                }
                if (option.Visible != null && _body.transform.childCount > before)
                    TrackVisible(_body.transform.GetChild(_body.transform.childCount - 1).gameObject, option.Visible);
            }
        }
        return true;
    }

    private static void TrackVisible(GameObject shell, Func<bool> visible)
    {
        void Apply()
        {
            if (shell == null) return;
            var show = visible();
            if (shell.activeSelf == show) return;
            shell.SetActive(show);
            if (_body != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_body.GetComponent<RectTransform>());
        }
        SettingRefresh.Add(Apply);
        Apply();
    }

    private static void IntegerStep(string title, string unit, ConfigEntry<int> entry, Func<bool> editable)
    {
        var range = (AcceptableValueRange<int>)entry.Description.AcceptableValues;
        var row = Row();
        Titles(row, title, Hint(entry), 320f);
        var value = RestlessUi.Label(row.transform, "", RestlessUi.BodySize, RestlessUi.Accent, TextAnchor.MiddleRight);
        RestlessUi.Pin(value.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(72f, 24f));
        var slider = RestlessUi.PaperSlider(row.transform);
        slider.minValue = range.MinValue;
        slider.maxValue = range.MaxValue;
        slider.wholeNumbers = true;
        ScrollRelay.Bind(slider.gameObject, _scroll);
        void Repaint()
        {
            slider.SetValueWithoutNotify(entry.Value);
            value.text = (entry.Value + " " + unit).TrimEnd();
        }
        slider.onValueChanged.AddListener(v =>
        {
            if (editable()) entry.Value = Mathf.Clamp(Mathf.RoundToInt(v), range.MinValue, range.MaxValue);
            Repaint();
        });
        ObserveSetting(entry, slider, editable, Repaint);
    }
}
