using System;
using System.Collections.Generic;
using Jotunn.Managers;
using RestlessQoL.Api;
using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.Ui;

internal sealed partial class StorageWindow : MonoBehaviour
{
    private static StorageWindow? _current;
    private static int _closedFrame = -1;
    internal static bool IsOpen => _current != null && _current.gameObject.activeSelf;
    internal static bool BlocksMenu => IsOpen || Time.frameCount == _closedFrame;
    private IStorageWindowSource _source = null!;
    private StorageSnapshot? _snapshot;
    private readonly List<StorageResource> _filtered = new();
    private readonly List<Cell> _cells = new();
    private readonly List<Button> _tabs = new();
    private readonly string[] _categories = { "All", "Materials", "Food", "Equipment", "Other" };
    private string _category = "All", _query = "", _selected = "";
    private int _sort, _amount = 1, _firstRow = -1;
    private bool _pending, _blocked;
    private float _nextSnapshot;
    private Vector2 _canvasSize;
    private RectTransform _sheet = null!;
    private RestlessScrollRect _grid = null!, _detail = null!;
    private Text _title = null!, _scope = null!, _stores = null!, _footer = null!, _weight = null!;
    private Text _empty = null!, _name = null!, _categoryLabel = null!, _description = null!, _total = null!;
    private Text _status = null!, _takeLabel = null!, _addedWeight = null!;
    private readonly List<(Text name, Text count)> _sourceRows = new();
    private Text _sourcesHeading = null!;
    private Image _portrait = null!;
    private InputField _quantity = null!;
    private Button _take = null!, _minus = null!, _plus = null!;
    private readonly List<Button> _presets = new();
    private bool _paintingAmount;
    private const int Columns = 6, PoolRows = 5;
    private const float PitchX = 132f, PitchY = 140f;

    internal static bool Open(IStorageWindowSource source)
    {
        if (source == null || !source.IsAvailable || GUIManager.CustomGUIFront == null
            || SettingsUi.IsOpen || InventoryGui.IsVisible()) return false;
        _current?.Dismiss();
        var go = RestlessUi.Graphic(GUIManager.CustomGUIFront.transform, "RestlessStorageWindow",
            new Color(0f, 0f, 0f, 0.28f), true);
        RestlessUi.Stretch(go, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var window = go.AddComponent<StorageWindow>();
        _current = window;
        window._source = source;
        try
        {
            window.Build();
            window._blocked = true;
            GUIManager.BlockInput(true);
            window.RefreshSnapshot(true);
            return IsOpen;
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            window.Dismiss();
            return false;
        }
    }

    internal static void Close(IStorageWindowSource source)
    {
        if (_current != null && ReferenceEquals(_current._source, source)) _current.Dismiss();
    }

    private void Update()
    {
        if (_source == null) return;
        if (Input.GetKeyDown(KeyCode.Escape) || !_source.IsAvailable || SettingsUi.IsOpen
            || Player.m_localPlayer == null || InventoryGui.IsVisible()) { Dismiss(); return; }
        Fit();
        if (Time.unscaledTime >= _nextSnapshot) RefreshSnapshot(false);
    }

    private void RefreshSnapshot(bool force)
    {
        _nextSnapshot = Time.unscaledTime + 1f;
        try
        {
            var snapshot = _source.Capture();
            var changed = force || _snapshot == null || snapshot.Revision != _snapshot.Revision;
            _snapshot = snapshot;
            _title.text = snapshot.Title;
            _scope.text = snapshot.Scope;
            _stores.text = snapshot.StoreCount + " nearby stores";
            _weight.text = "Carry weight  " + snapshot.CarryWeight.ToString("0.#") + " / " + snapshot.CarryLimit.ToString("0.#");
            if (!_pending) _status.text = snapshot.Status;
            if (changed) Filter(false);
            else PaintAmount();
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "Storage is unavailable. Please try again.");
            Dismiss();
        }
    }

    private void Filter(bool resetScroll)
    {
        var previousSelection = _selected;
        _filtered.Clear();
        if (_snapshot != null)
            foreach (var item in _snapshot.Resources)
                if (item.Count > 0 && (_category == "All" || item.Category == _category)
                    && (string.IsNullOrWhiteSpace(_query) || item.Name.IndexOf(_query.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0))
                    _filtered.Add(item);
        _filtered.Sort((a, b) =>
        {
            var order = _sort == 1 ? b.Count.CompareTo(a.Count) : _sort == 2 ? b.UnitWeight.CompareTo(a.UnitWeight) : 0;
            if (order == 0) order = StringComparer.CurrentCultureIgnoreCase.Compare(a.Name, b.Name);
            return order != 0 ? order : StringComparer.Ordinal.Compare(a.Id, b.Id);
        });
        if (!_filtered.Exists(r => r.Id == _selected)) _selected = _filtered.Count > 0 ? _filtered[0].Id : "";
        var height = Mathf.Max(_grid.viewport.rect.height, ((_filtered.Count + Columns - 1) / Columns) * PitchY);
        _grid.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        var y = resetScroll ? 0f : Mathf.Clamp(_grid.content.anchoredPosition.y, 0f, height - _grid.viewport.rect.height);
        // A count-only refresh must not interrupt an active wheel animation.
        if (resetScroll || !Mathf.Approximately(y, _grid.content.anchoredPosition.y))
        {
            _grid.CancelWheel();
            _grid.content.anchoredPosition = new Vector2(0f, y);
        }
        _empty.text = _snapshot?.StoreCount == 0 ? "No accessible stores nearby."
            : _snapshot?.Resources.Count == 0 ? "These stores are empty." : "No matching resources.";
        _empty.gameObject.SetActive(_filtered.Count == 0);
        _footer.text = _filtered.Count + " entries shown";
        for (var i = 0; i < _tabs.Count; i++) RestlessUi.PaperControl(_tabs[i].gameObject,
            _categories[i] == _category ? RestlessUi.Accent : (Color?)null);
        BindCells(true);
        PaintDetail(resetScroll || previousSelection != _selected);
    }

    private void BindCells(bool force)
    {
        var first = Mathf.Max(0, Mathf.FloorToInt(_grid.content.anchoredPosition.y / PitchY));
        if (!force && first == _firstRow) return;
        _firstRow = first;
        for (var i = 0; i < _cells.Count; i++)
        {
            var index = first * Columns + i;
            var cell = _cells[i];
            cell.Button.gameObject.SetActive(index < _filtered.Count);
            if (index >= _filtered.Count) { cell.Item = null; continue; }
            var item = _filtered[index]; cell.Item = item;
            At(cell.Button.gameObject, index % Columns * PitchX, index / Columns * PitchY, 122f, 128f);
            cell.Name.text = item.Name; cell.Count.text = item.Count.ToString("N0");
            cell.Icon.sprite = item.Icon; cell.Icon.enabled = item.Icon != null;
            RestlessUi.PaperControl(cell.Button.gameObject, item.Id == _selected ? RestlessUi.Accent : (Color?)null);
        }
    }

    private StorageResource? Selected() => _filtered.Find(r => r.Id == _selected);

    private void Select(Cell cell)
    {
        if (cell.Item == null) return;
        _selected = cell.Item.Id;
        _amount = Math.Max(1, Math.Min(cell.Item.StackSize, (int)Math.Min(int.MaxValue, cell.Item.Count)));
        BindCells(true);
        PaintDetail(true);
    }

    private void PaintDetail(bool resetScroll)
    {
        var item = Selected();
        _portrait.sprite = item?.Icon; _portrait.enabled = item?.Icon != null;
        _name.text = item?.Name ?? "Select a resource";
        _categoryLabel.text = item?.Category ?? "";
        _description.text = item == null ? "Choose an item to see its total and storage locations." : RestlessUi.Bare(item.Description);
        var descriptionHeight = Mathf.Max(24f, _description.preferredHeight);
        At(_description.gameObject, 0f, 108f, 284f, descriptionHeight);
        var y = 124f + descriptionHeight;
        _total.text = item == null ? "" : item.Count.ToString("N0") + " in storage";
        At(_total.gameObject, 0f, y, 284f, 30f);
        _sourcesHeading.text = item == null ? "" : "Stored in · " + item.Locations.Count + " stores";
        At(_sourcesHeading.gameObject, 0f, y + 44f, 284f, 26f);
        var sourceY = y + 78f;
        var count = item?.Locations.Count ?? 0;
        for (var i = 0; i < count; i++)
        {
            if (i == _sourceRows.Count)
            {
                var name = Label(_detail.content, "", 16, 0, sourceY, 206, 24, true);
                var value = Label(_detail.content, "", 16, 214, sourceY, 70, 24);
                value.alignment = TextAnchor.UpperRight;
                name.alignment = TextAnchor.UpperLeft;
                _sourceRows.Add((name, value));
            }
            var row = _sourceRows[i]; var location = item!.Locations[i];
            row.name.gameObject.SetActive(true); row.count.gameObject.SetActive(true);
            row.name.text = location.Name; row.count.text = location.Count.ToString("N0");
            var rowHeight = Mathf.Max(24f, Mathf.Max(row.name.preferredHeight, row.count.preferredHeight));
            At(row.name.gameObject, 0f, sourceY, 206f, rowHeight);
            At(row.count.gameObject, 214f, sourceY, 70f, rowHeight);
            sourceY += rowHeight + 8f;
        }
        for (var i = count; i < _sourceRows.Count; i++)
        { _sourceRows[i].name.gameObject.SetActive(false); _sourceRows[i].count.gameObject.SetActive(false); }
        _detail.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            Mathf.Max(_detail.viewport.rect.height, sourceY + 8f));
        var detailY = resetScroll ? 0f : Mathf.Clamp(_detail.content.anchoredPosition.y, 0f,
            Mathf.Max(0f, _detail.content.rect.height - _detail.viewport.rect.height));
        if (resetScroll || !Mathf.Approximately(detailY, _detail.content.anchoredPosition.y))
        {
            _detail.CancelWheel();
            _detail.content.anchoredPosition = new Vector2(0f, detailY);
        }
        PaintAmount();
    }

    private void SetAmount(int amount)
    {
        var item = Selected();
        var max = item == null ? 1 : (int)Math.Min(int.MaxValue, Math.Max(1L, item.Count));
        _amount = Math.Max(1, Math.Min(max, amount));
        // onEndEdit can run before InputField has cleared isFocused.
        _quantity.SetTextWithoutNotify(_amount.ToString());
        PaintAmount();
    }

    private void PaintAmount()
    {
        var item = Selected();
        var max = item == null ? 1 : (int)Math.Min(int.MaxValue, Math.Max(1L, item.Count));
        _amount = Math.Max(1, Math.Min(max, _amount));
        if (!_quantity.isFocused)
        {
            _paintingAmount = true; _quantity.SetTextWithoutNotify(_amount.ToString()); _paintingAmount = false;
        }
        _quantity.interactable = item != null && !_pending;
        _minus.interactable = item != null && _amount > 1 && !_pending;
        _plus.interactable = item != null && _amount < max && !_pending;
        foreach (var button in _presets) button.interactable = item != null && !_pending;
        var added = (item?.UnitWeight ?? 0f) * _amount;
        _addedWeight.text = "Added weight  " + added.ToString("0.#");
        _addedWeight.color = _snapshot != null && _snapshot.CarryWeight + added > _snapshot.CarryLimit
            ? RestlessUi.HealthTint : RestlessUi.PaperMuted;
        _take.interactable = item != null && item.Count > 0 && _snapshot?.CanWithdraw == true && !_pending;
        _takeLabel.text = _pending ? "Taking…" : "Take " + _amount.ToString("N0");
    }

    private void Withdraw()
    {
        var item = Selected();
        if (item == null || _pending || _snapshot?.CanWithdraw != true || !_source.IsAvailable) return;
        _pending = true; _status.text = "Taking items…"; PaintAmount();
        try
        {
            _source.Withdraw(item.Id, _amount, message =>
            {
                if (this == null || !isActiveAndEnabled) return;
                _pending = false; RefreshSnapshot(true); _status.text = message;
                _nextSnapshot = Time.unscaledTime + 3f;
            });
        }
        catch (Exception error)
        {
            // An ambiguous transport error is not safe to retry automatically.
            Debug.LogException(error);
            _pending = false;
            _status.text = "Transfer status unknown. Close and check your inventory.";
            PaintAmount();
        }
    }

    private void Fit()
    {
        var parent = transform as RectTransform;
        if (parent == null || parent.rect.size == _canvasSize) return;
        _canvasSize = parent.rect.size;
        _sheet.localScale = Vector3.one * Mathf.Min(1f, Mathf.Max(0.1f, (_canvasSize.x - 32f) / 1200f),
            Mathf.Max(0.1f, (_canvasSize.y - 32f) / 800f));
    }

    private void Dismiss()
    {
        _closedFrame = Time.frameCount;
        gameObject.SetActive(false);
        if (_current == this) _current = null;
        Destroy(gameObject);
    }
    private void OnDisable()
    {
        if (_current == this) _current = null;
        if (!_blocked) return;
        _blocked = false;
        if (!SettingsUi.IsOpen) GUIManager.BlockInput(false);
    }
    private sealed class Cell
    {
        internal Button Button = null!;
        internal Image Icon = null!;
        internal Text Name = null!, Count = null!;
        internal StorageResource? Item;
    }
}
