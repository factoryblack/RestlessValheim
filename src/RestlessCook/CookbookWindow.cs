using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Jotunn.Managers;
using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessCook;

// Presentation only: the Kitchen API owns planning, stock and all mutations.
internal sealed partial class CookbookWindow : MonoBehaviour
{
    private static CookbookWindow? _current;
    private static int _closedFrame = -1;
    private CraftingStation _table = null!;
    private RectTransform _sheet = null!;
    private RestlessScrollRect _recipes = null!, _tree = null!, _details = null!;
    private readonly List<Card> _recipeCards = new(), _nodes = new();
    private readonly List<GameObject> _lines = new();
    private readonly List<Button> _tabs = new();
    private readonly Dictionary<string, Sprite?> _icons = new();
    private readonly List<int> _trail = new();
    private IReadOnlyList<KitchenStep> _steps = Array.Empty<KitchenStep>();
    private IReadOnlyList<KitchenOrder> _orders = Array.Empty<KitchenOrder>();
    private string _recipe = "", _query = "", _stamp = "";
    private int _tab, _count = 1, _focus, _order;
    private bool _feasts = true, _blocked, _confirmCancel;
    private float _next, _messageUntil;
    private Text _heading = null!, _copy = null!, _status = null!, _quantity = null!, _actionText = null!;
    private Button _action = null!, _collect = null!, _back = null!;
    private Vector2 _canvas;

    internal static void Open(CraftingStation table)
    {
        if (!Plugin.CookbookEnabled.Value) { Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "Enable Cookbook in Restless settings."); return; }
        if (table == null || GUIManager.CustomGUIFront == null || SettingsUi.IsOpen || InventoryGui.IsVisible()) return;
        Close();
        var go = RestlessUi.Graphic(GUIManager.CustomGUIFront.transform, "RestlessCookbook", new Color(0, 0, 0, .28f), true);
        RestlessUi.Stretch(go, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var w = go.AddComponent<CookbookWindow>(); _current = w; w._table = table;
        try
        {
            w.Build(); w._blocked = true; GUIManager.BlockInput(true);
            w.RecipeList(); w.Refresh(true);
        }
        catch (Exception e) { Debug.LogException(e); Close(); }
    }
    internal static void Close()
    {
        if (_current == null) return;
        var w = _current; _current = null; _closedFrame = Time.frameCount;
        w.gameObject.SetActive(false); Destroy(w.gameObject);
    }
    private void OnDisable()
    {
        if (_current == this) _current = null;
        if (_blocked && !SettingsUi.IsOpen) GUIManager.BlockInput(false);
        _blocked = false;
    }
    private void Update()
    {
        var player = Player.m_localPlayer;
        if (Input.GetKeyDown(KeyCode.Escape) || !Plugin.CookbookEnabled.Value || _table == null
            || player == null || player.IsDead() || SettingsUi.IsOpen || InventoryGui.IsVisible()
            || (_table.transform.position - player.transform.position).sqrMagnitude > 25f)
        { Close(); return; }
        Fit();
        if (Time.unscaledTime >= _next) Refresh(false);
    }
    private void Refresh(bool force)
    {
        _next = Time.unscaledTime + 1f;
        try
        {
            _orders = Kitchen.Orders(_table);
            if (_order != 0 && !_orders.Any(o => o.Id == _order)) { _order = 0; _trail.Clear(); _focus = 0; }
            if (_tab == 2) { PaintStations(force); return; }
            _steps = _order != 0 ? Kitchen.Live(_table, _order) : _tab == 1 ? Array.Empty<KitchenStep>() : Kitchen.Plan(_table, _recipe, _count);
            var stamp = new StringBuilder().Append(_tab).Append('|').Append(_order).Append('|').Append(_recipe).Append('|').Append(_count);
            foreach (var s in _steps) stamp.Append('|').Append(s.Output).Append(':').Append(s.Parent).Append(':').Append(s.Depth)
                .Append(':').Append(s.Need).Append(':').Append(s.Have).Append(':').Append(s.Cooking).Append(':').Append(s.State).Append(':').Append(s.Note);
            foreach (var o in _orders) stamp.Append('|').Append(o.Id).Append(':').Append(o.Ready).Append(':').Append(o.Count);
            var key = stamp.ToString();
            if (force || key != _stamp)
            {
                _stamp = key;
                if (_tab == 1) OrderList();
                if (_focus >= _steps.Count) { _focus = 0; _trail.Clear(); }
                PaintTree(); PaintDetails();
            }
            if (Time.unscaledTime >= _messageUntil)
                _status.text = _order == 0 ? "Plan a finite order · your kitchen handles preparation" : "Live order · preparation continues after closing";
        }
        catch (Exception e) { Debug.LogException(e); _status.text = "Kitchen data unavailable. Close and try again."; }
    }
    private void Tab(int tab)
    {
        _tab = tab; _stamp = ""; _confirmCancel = false;
        for (var i = 0; i < _tabs.Count; i++) RestlessUi.PaperControl(_tabs[i].gameObject, i == tab ? RestlessUi.Accent : (Color?)null);
        if (tab == 0) { _order = 0; RecipeList(); }
        if (tab == 1) OrderList();
        _trail.Clear(); _focus = 0; Refresh(true);
    }
    private void RecipeList()
    {
        var rows = Kitchen.Rows.Where(r => (!_feasts || r.IsFeast) && (r.IsFeast || r.IsMeal || r.IsSideboard)
            && (string.IsNullOrWhiteSpace(_query) || r.Name.IndexOf(_query.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0))
            .OrderBy(r => r.Tier).ThenBy(r => r.Name).ToList();
        if (string.IsNullOrEmpty(_recipe) && rows.Count > 0) _recipe = rows[0].Id;
        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i]; var c = GetCard(_recipeCards, i, _recipes.content);
            Position(c.Root, 0, i * 90, 240, 82);
            c.Set(r.Name, r.Tier + " · " + r.Kind, Icon(Output(r)), r.Id == _recipe ? RestlessUi.Accent : RestlessUi.PaperMuted,
                () => { _recipe = r.Id; _order = 0; _focus = 0; _trail.Clear(); _confirmCancel = false; RecipeList(); Refresh(true); });
        }
        HideAfter(_recipeCards, rows.Count); Size(_recipes, rows.Count * 90, true);
        if (rows.Count == 0) Message("No matching recipes.");
    }
    private void OrderList()
    {
        for (var i = 0; i < _orders.Count; i++)
        {
            var o = _orders[i]; var c = GetCard(_recipeCards, i, _recipes.content);
            Position(c.Root, 0, i * 90, 240, 82);
            c.Set(o.Name, o.Ready + " ready · target " + o.Count, Icon(o.Feast), o.Id == _order ? RestlessUi.Accent : RestlessUi.PaperMuted,
                () => { _order = o.Id; _focus = 0; _trail.Clear(); _confirmCancel = false; Refresh(true); });
        }
        HideAfter(_recipeCards, _orders.Count); Size(_recipes, _orders.Count * 90, false);
    }
    // Plan is a preorder traversal; depth boundaries distinguish repeated ingredients in separate branches.
    private List<int> Children(int index)
    {
        var result = new List<int>();
        for (var i = index + 1; i < _steps.Count && _steps[i].Depth > _steps[index].Depth; i++)
            if (_steps[i].Depth == _steps[index].Depth + 1) result.Add(i);
        return result;
    }
    private void Focus(int index)
    {
        _trail.Add(_focus); _focus = index; PaintTree(); PaintDetails(); Size(_tree, _tree.content.rect.height, true); Size(_details, _details.content.rect.height, true);
    }
    private void PaintTree()
    {
        foreach (var line in _lines) line.SetActive(false);
        _back.interactable = _trail.Count > 0;
        if (_steps.Count == 0) { HideAfter(_nodes, 0); _heading.text = _tab == 1 ? (_orders.Count == 0 ? "No kitchen orders" : "Choose an order") : "Choose a recipe"; return; }
        var root = _steps[_focus]; _heading.text = Local(root.Name);
        var card = GetCard(_nodes, 0, _tree.content);
        Position(card.Root, 132, 12, 280, 112);
        card.Set(Local(root.Name), State(root), Icon(root.Output), Tint(root.State), () => PaintDetails());
        var children = Children(_focus);
        var li = 0;
        for (var j = 0; j < children.Count; j++)
        {
            var index = children[j]; var s = _steps[index];
            float x = j % 2 * 276, y = 174 + j / 2 * 130;
            Line(li++, 272, 126, 1, y - 142);
            Line(li++, Math.Min(272, x + 134), y - 16, Math.Abs(272 - (x + 134)), 1);
            Line(li++, x + 134, y - 16, 1, 16);
            var node = GetCard(_nodes, j + 1, _tree.content); Position(node.Root, x, y, 264, 112);
            node.Set(Local(s.Name), State(s), Icon(s.Output), Tint(s.State), () => Focus(index));
        }
        HideAfter(_nodes, children.Count + 1);
        Size(_tree, children.Count == 0 ? 160 : 174 + ((children.Count + 1) / 2) * 130, false);
    }
    private void PaintDetails()
    {
        var s = _steps.Count > _focus ? _steps[_focus] : null;
        var b = new StringBuilder();
        if (s != null)
        {
            b.AppendLine(Local(s.Name)).AppendLine().AppendLine(s.Note)
                .AppendLine("Prepared: " + s.Have + " / " + s.Need);
            if (s.Cooking > 0) b.AppendLine("Cooking: " + s.Cooking);
            b.AppendLine().AppendLine("REQUIRED STATION")
                .AppendLine(s.Station == KitchenStationKind.None ? "Gather from the world" : StationName(s.Station) + " · level " + s.StationLevel);
            if (s.Uses.Count > 0)
            {
                b.AppendLine().AppendLine("INGREDIENTS");
                foreach (var use in s.Uses) b.AppendLine(Local(use.Name) + " × " + use.Amount);
            }
            b.AppendLine().AppendLine("OTHER USES");
            var uses = Kitchen.OtherUses(s.Output);
            foreach (var use in uses) b.AppendLine(use.Name);
            if (uses.Count == 0) b.AppendLine("Final dish");
        }
        else b.Append(_tab == 1 ? "No order selected. Select an order on the left to follow its progress." : "Select a recipe to explore its ingredients.");
        _copy.text = b.ToString();
        Position(_copy.gameObject, 0, 0, 280, Mathf.Max(60, _copy.preferredHeight));
        Size(_details, _copy.preferredHeight + 16, false);
        var order = _orders.FirstOrDefault(o => o.Id == _order);
        _collect.gameObject.SetActive(order != null);
        _collect.interactable = order != null && order.Ready > 0;
        _action.gameObject.SetActive(_tab != 2);
        _action.interactable = order != null || (_tab == 0 && _steps.Count > 0 && _steps[0].Uses.Count > 0 && _orders.Count < 8);
        _actionText.text = order != null ? (_confirmCancel ? "Confirm cancellation" : "Cancel order") : "Prepare " + _count;
        _quantity.text = _count.ToString();
    }
    private void Act()
    {
        if (!_action.interactable) return;
        try
        {
            if (_order != 0)
            {
                if (!_confirmCancel) { _confirmCancel = true; PaintDetails(); Message("Cancel this order? Prepared food remains in the kitchen pantry."); return; }
                Kitchen.Cancel(_table, _order); _order = 0; _confirmCancel = false; Message("Cancellation requested.");
            }
            else Message(Kitchen.Place(_table, _recipe, _count) ? "Order placed. Open Orders to follow preparation." : "Order could not be placed. Check ownership or queue capacity.");
            Refresh(true);
        }
        catch (Exception e) { Debug.LogException(e); Message("Action failed. Check Orders before trying again."); }
    }
    private void Collect()
    {
        try { Message("Collected " + Kitchen.Collect(_table, _order) + ". Remaining food stays at the table."); Refresh(true); }
        catch (Exception e) { Debug.LogException(e); Message("Collection failed. Check your inventory."); }
    }
    private void PaintStations(bool force)
    {
        var stations = Kitchen.Stations(_table);
        var text = string.Join("\n\n", stations.Select(s => Local(s.Name) + "\n" + (s.Kind == KitchenStationKind.Cauldron
            ? "Level " + s.Level : s.Free + " free · " + s.Cooking + " cooking · " + s.Ready + " ready")
            + (s.FuelMax > 0 ? "\nFuel " + s.Fuel + " / " + s.FuelMax : "") + (string.IsNullOrEmpty(s.Block) ? "" : "\n" + s.Block)));
        if (!force && text == _stamp) return;
        _stamp = text; _heading.text = "Your kitchen";
        HideAfter(_nodes, 0); HideAfter(_recipeCards, 0); foreach (var line in _lines) line.SetActive(false);
        _copy.text = stations.Count == 0 ? "No cooking stations found nearby." : text;
        Position(_copy.gameObject, 0, 0, 280, Mathf.Max(60, _copy.preferredHeight)); Size(_details, _copy.preferredHeight + 16, false);
        var c = GetCard(_nodes, 0, _tree.content); Position(c.Root, 80, 24, 380, 112);
        c.Set("Connected kitchen", stations.Count + " stations nearby", null, RestlessUi.Accent, () => { });
        _action.gameObject.SetActive(false); _collect.gameObject.SetActive(false); _back.interactable = false;
    }
    private void Message(string message) { _status.text = message; _messageUntil = Time.unscaledTime + 5f; }
    private static string Local(string text) => Localization.instance == null ? text : Localization.instance.Localize(text);
    private static string Output(CookRow r) => r.IsFeast && r.IsAdd && !r.Prefab.EndsWith("_Material") ? r.Prefab + "_Material" : r.Prefab;
    private Sprite? Icon(string prefab)
    {
        if (_icons.TryGetValue(prefab, out var icon)) return icon;
        var item = ObjectDB.instance?.GetItemPrefab(prefab)?.GetComponent<ItemDrop>();
        var icons = item?.m_itemData?.m_shared?.m_icons;
        icon = icons != null && icons.Length > 0 ? icons[0] : null;
        _icons[prefab] = icon; return icon;
    }
    private static string State(KitchenStep s) => s.State + " · " + s.Have + " / " + s.Need + (s.Cooking > 0 ? " · " + s.Cooking + " cooking" : "");
    private static Color Tint(KitchenStepState s) => s == KitchenStepState.Prepared ? new Color(.58f,.76f,.56f)
        : s == KitchenStepState.Missing || s == KitchenStepState.Blocked ? RestlessUi.HealthTint : RestlessUi.Accent;
    private static string StationName(KitchenStationKind s) => s == KitchenStationKind.PrepTable ? "Preparation table" : s == KitchenStationKind.Rack ? "Cooking rack" : s.ToString();
    [HarmonyPatch(typeof(Menu), "Update")]
    private static class MenuInput
    {
        private static bool Prefix() => _current == null && Time.frameCount != _closedFrame;
    }
}
