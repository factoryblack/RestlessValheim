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
    private readonly List<Card> _recipeCards = new(), _nodes = new(), _requirementCards = new();
    private readonly List<GameObject> _lines = new();
    private readonly List<Button> _tabs = new();
    private readonly Dictionary<string, Sprite?> _icons = new();
    private readonly List<int> _trail = new();
    private IReadOnlyList<KitchenStep> _steps = Array.Empty<KitchenStep>();
    private IReadOnlyList<KitchenOrder> _orders = Array.Empty<KitchenOrder>();
    private string _recipe = "", _query = "", _stamp = "";
    private int _tab, _count = 1, _focus, _order;
    private int _detailFocus = -1;
    private bool _blocked, _confirmCancel;
    private string _category = "Feasts";
    private readonly List<Button> _filters = new();
    private Image _dish = null!;
    private GameObject _dishFrame = null!;
    private Text _dishName = null!, _dishKind = null!, _station = null!, _requirementsLabel = null!;
    private float _next, _messageUntil;
    private Text _heading = null!, _copy = null!, _status = null!, _quantity = null!, _actionText = null!, _actionHint = null!;
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
            w.Build(); RestlessQoL.HudTweaks.LookHints.SetSuppressed(w,true); w._blocked = true; GUIManager.BlockInput(true);
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
        RestlessQoL.HudTweaks.LookHints.SetSuppressed(this,false);
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
            if (_order != 0 && !_orders.Any(o => o.Id == _order)) { _order = 0; _trail.Clear(); _focus = 0; _detailFocus = -1; }
            if (_tab == 2) { PaintStations(force); return; }
            if (_tab == 3) { PaintPantry(force); return; }
            _steps = _order != 0 ? Kitchen.Live(_table, _order) : _tab == 1 ? Array.Empty<KitchenStep>() : Kitchen.Plan(_table, _recipe, _count);
            var stamp = new StringBuilder().Append(_tab).Append('|').Append(_order).Append('|').Append(_recipe).Append('|').Append(_count);
            foreach (var s in _steps) stamp.Append('|').Append(s.Output).Append(':').Append(s.Parent).Append(':').Append(s.Depth)
                .Append(':').Append(s.Need).Append(':').Append(s.Have).Append(':').Append(s.Available).Append(':').Append(s.Cooking).Append(':').Append(s.State).Append(':').Append(s.Note).Append(':').Append(s.ElapsedSeconds).Append(':').Append(s.DurationSeconds);
            foreach (var o in _orders) stamp.Append('|').Append(o.Id).Append(':').Append(o.Ready).Append(':').Append(o.Count);
            var key = stamp.ToString();
            if (force || key != _stamp)
            {
                if (_detailFocus >= _steps.Count) _detailFocus = -1;
                _stamp = key;
                if (_tab == 1) OrderList();
                if (_focus >= _steps.Count) { _focus = 0; _trail.Clear(); }
                if (_tab == 1) PaintOrderSteps(); else PaintTree(); PaintDetails();
            }
            if (Time.unscaledTime >= _messageUntil)
                _status.text = _order == 0 ? "Queue an order · missing ingredients can be supplied while it waits" : OrderStatus();
        }
        catch (Exception e) { Debug.LogException(e); _status.text = "Kitchen data unavailable. Close and try again."; }
    }
    private string OrderStatus()
    {
        var working = _steps.FirstOrDefault(s => s.State == KitchenStepState.Cooking && !Covered(s));
        if (working != null) return "Preparing " + Local(working.Name) + " · " + State(working);
        var blocked = _steps.FirstOrDefault(s => s.State == KitchenStepState.Blocked && !Covered(s));
        if (blocked != null) return Local(blocked.Name) + " · " + blocked.Note;
        var missing = _steps.FirstOrDefault(s => s.State == KitchenStepState.Missing && s.Uses.Count == 0 && !Covered(s));
        if (missing != null) return "Waiting for " + Local(missing.Name) + " · " + KitchenStock.Count(missing.Available, missing.Need) + " available";
        return "Order queued · waiting for the next preparation step";
    }
    private void Tab(int tab)
    {
        _detailFocus = -1; _tab = tab; _stamp = ""; _confirmCancel = false;
        for (var i = 0; i < _tabs.Count; i++) RestlessUi.PaperControl(_tabs[i].gameObject, i == tab ? RestlessUi.Accent : (Color?)null);
        if (tab == 0) { _order = 0; RecipeList(); }
        if (tab == 1) OrderList();
        _trail.Clear(); _focus = 0; Refresh(true);
        Size(_tree, _tree.content.rect.height, true);
        Size(_details, _details.content.rect.height, true);
        Size(_recipes, _recipes.content.rect.height, true);
    }
    private void RecipeList()
    {
        var rows = Kitchen.Rows.Where(r => Matches(r)
            && (string.IsNullOrWhiteSpace(_query) || r.Name.IndexOf(_query.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0))
            .OrderBy(r => TierOrder(r.Tier)).ThenBy(r => r.Name).ToList();
        if (!rows.Any(r => r.Id == _recipe)) { _recipe = rows.Count > 0 ? rows[0].Id : ""; _detailFocus = -1; _focus = 0; _trail.Clear(); }
        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i]; var c = GetCard(_recipeCards, i, _recipes.content);
            Position(c.Root, 0, i * 80, 228, 72);
            c.Set(r.Name, TierName(r.Tier) + " · " + r.Kind, Icon(Output(r)), r.Id == _recipe ? RestlessUi.Accent : RestlessUi.PaperMuted,
                () => { _detailFocus = -1; _recipe = r.Id; _order = 0; _focus = 0; _trail.Clear(); _confirmCancel = false; RecipeList(); Refresh(true); Size(_tree,_tree.content.rect.height,true); Size(_details,_details.content.rect.height,true); }, selected: r.Id == _recipe, listEntry: true);
        }
        HideAfter(_recipeCards, rows.Count); Size(_recipes, rows.Count * 80, false);
        if (rows.Count == 0) Message("No matching recipes.");
    }
    private void OrderList()
    {
        for (var i = 0; i < _orders.Count; i++)
        {
            var o = _orders[i]; var c = GetCard(_recipeCards, i, _recipes.content);
            Position(c.Root, 0, i * 96, 228, 90);
            c.Set(o.Name, o.Collected + o.Ready + " / " + o.Count + " finished", Icon(o.Feast), o.Id == _order ? RestlessUi.Accent : RestlessUi.PaperMuted,
                () => { _detailFocus = -1; _order = o.Id; _focus = 0; _trail.Clear(); _confirmCancel = false; Refresh(true); }, selected: o.Id == _order);
        }
        HideAfter(_recipeCards, _orders.Count); Size(_recipes, _orders.Count * 96, false);
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
        _detailFocus = -1; _trail.Add(_focus); _focus = index; if (_tab == 1) PaintOrderSteps(); else PaintTree(); PaintDetails(); Size(_tree, _tree.content.rect.height, true); Size(_details, _details.content.rect.height, true);
    }
    private void Inspect(int index)
    {
        if (_steps[index].Uses.Count > 0) { Focus(index); return; }
        _detailFocus = index; PaintDetails(); Size(_details,_details.content.rect.height,true);
    }
    private void PaintTree()
    {
        foreach (var line in _lines) line.SetActive(false);
        _back.interactable = _trail.Count > 0;
        if (_steps.Count == 0) { HideAfter(_nodes, 0); _heading.text = _tab == 1 ? (_orders.Count == 0 ? "No kitchen orders" : "Choose an order") : "Choose a recipe"; return; }
        var root = _steps[_focus]; _heading.text = _trail.Count == 0 ? "Preparation plan" : "Ingredient preparation";
        var card = GetCard(_nodes, 0, _tree.content);
        Position(card.Root, 18, 8, 492, 152);
        card.Set(Local(root.Name), State(root), Icon(root.Output), Tint(root.State), () => { _detailFocus = -1; PaintDetails(); }, selected: true, featured: true);
        card.Progress(root);
        var children = Children(_focus);
        var li = 0;
        for (var j = 0; j < children.Count; j++)
        {
            var index = children[j]; var s = _steps[index];
            float x = j == children.Count-1 && children.Count%2 == 1 ? 138 : j%2*276;
            float y = 192+j/2*144;
            Line(li++, 264, 142, 2, y - 158);
            Line(li++, Math.Min(264, x + 126), y - 16, Math.Abs(264 - (x + 126)), 2);
            Line(li++, x + 126, y - 16, 2, 16);
            var node = GetCard(_nodes, j + 1, _tree.content); Position(node.Root, x, y, 252, 116);
            node.Set(Local(s.Name), State(s), Icon(s.Output), Tint(s.State), () => Inspect(index), expandable: s.Uses.Count > 0);
            node.Progress(s);
        }
        HideAfter(_nodes, children.Count + 1);
        Size(_tree, children.Count == 0 ? 168 : 192 + ((children.Count + 1) / 2) * 144, false);
    }
    private void PaintDetails()
    {
        ShowDish();
        var detailIndex = _detailFocus >= 0 && _detailFocus < _steps.Count ? _detailFocus : _focus;
        var s = _steps.Count > detailIndex ? _steps[detailIndex] : null;
        _dish.sprite = s == null ? null : Icon(s.Output); _dish.enabled = _dish.sprite != null;
        _dishFrame.SetActive(s != null); _dishName.text = s == null ? "Select a dish" : Local(s.Name);
        _dishKind.text = s == null ? "" : State(s);
        _dishKind.color = s == null ? RestlessUi.PaperMuted : Tint(s.State);
        _station.text = s == null ? "" : s.Station == KitchenStationKind.None ? "Gather from the world"
            : StationName(s.Station) + "\nRequired level " + s.StationLevel;
        var b = new StringBuilder();
        var rowCount = s?.Uses.Count ?? 0;
        var children = s == null ? new List<int>() : Children(detailIndex);
        for (var i = 0; i < rowCount; i++)
        {
            var use = s!.Uses[i];
            var index = children.FindIndex(n => _steps[n].Output == use.Item);
            if (index >= 0) index = children[index];
            var child = index >= 0 ? _steps[index] : null;
            var onHand = child != null ? child.Available : use.Have;
            var covered = child != null && Covered(child);
            var row = GetCard(_requirementCards,i,_details.content);
            Position(row.Root,0,26+i*58,280,54);
            var count = KitchenStock.Count(onHand, use.Amount);
            row.Set(Local(use.Name), covered ? "Covered · " + count : count, Icon(use.Item),
                covered || onHand >= use.Amount ? Tint(KitchenStepState.Prepared) : RestlessUi.Accent,
                () => { if (index >= 0) Inspect(index); });
        }
        HideAfter(_requirementCards,rowCount);
        if (s != null)
        {
            if ((_order != 0 || detailIndex != 0) && !string.IsNullOrWhiteSpace(s.Note)) b.AppendLine(s.Note).AppendLine();
            var uses = Kitchen.OtherUses(s.Output);
            if (uses.Count > 0)
            {
                b.AppendLine("ALSO USED IN");
                foreach (var use in uses) b.AppendLine(use.Name);
            }
        }
        else b.Append(_tab == 1 ? "Select an order on the left to follow its progress." : "Select a recipe to explore its ingredients.");
        _requirementsLabel.text = rowCount > 0 ? "INGREDIENTS · STOCK / NEEDED" : "";
        _copy.text = b.ToString();
        var copyY = rowCount > 0 ? 34 + rowCount*58 : 0;
        Position(_copy.gameObject,0,copyY,280,Mathf.Max(60,_copy.preferredHeight));
        Size(_details,copyY+_copy.preferredHeight+16,false);
        var order = _orders.FirstOrDefault(o => o.Id == _order);
        _collect.gameObject.SetActive(order != null);
        _collect.interactable = order != null && order.Ready > 0;
        _action.gameObject.SetActive(_tab < 2);
        _action.interactable = order != null || (_tab == 0 && _steps.Count > 0 && _steps[0].Uses.Count > 0 && _orders.Count < 8);
        _actionText.text = order != null ? (_confirmCancel ? "Confirm cancellation" : "Cancel order") : "Queue " + _count;
        _actionHint.gameObject.SetActive(_tab == 0);
        _actionHint.text = _steps.Count == 0 ? "Choose a recipe to queue." : QueueReason(_steps[0]);
        _quantity.text = _count.ToString();
    }
    private void Act()
    {
        if (_tab == 3) { TakePantry(); return; }
        if (!_action.interactable) return;
        try
        {
            if (_order != 0)
            {
                if (!_confirmCancel) { _confirmCancel = true; PaintDetails(); Message("Cancel this order? Prepared food remains in the kitchen pantry."); return; }
                Kitchen.Cancel(_table, _order); _order = 0; _confirmCancel = false; Message("Cancellation requested.");
            }
            else
            {
                var previous = new HashSet<int>(Kitchen.Orders(_table).Select(o => o.Id));
                if (Kitchen.Place(_table,_recipe,_count))
                {
                    _order = Kitchen.Orders(_table).FirstOrDefault(o => !previous.Contains(o.Id))?.Id ?? 0;
                    Tab(1); Message("Order queued. Each step below shows what happens next.");
                }
                else Message("Order could not be placed. Check ownership or queue capacity.");
            }
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
        HideDish();
        var stations = Kitchen.Stations(_table);
        var text = string.Join("\n\n", stations.Select(s => Local(s.Name) + "\n" + ((s.Kind == KitchenStationKind.Cauldron || s.Kind == KitchenStationKind.MeadKettle)
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
    private bool Covered(KitchenStep step)
    {
        // The planner marks descendants fulfilled when an ancestor is already prepared/in progress.
        // These are satisfied dependencies, not a claim that the raw ingredients remain in stock.
        var parent = step.ParentIndex;
        for (var guard = 0; parent >= 0 && parent < _steps.Count && guard < _steps.Count; guard++)
        {
            var ancestor = _steps[parent];
            if (ancestor.Need > 0 && ancestor.Have + ancestor.Cooking >= ancestor.Need) return true;
            parent = ancestor.ParentIndex;
        }
        return false;
    }
    private string State(KitchenStep s)
    {
        var count = KitchenStock.Count(s.Available, s.Need);
        if (Covered(s)) return "Covered by another step";
        if (s.State == KitchenStepState.Blocked) return Local(s.Note) + " · " + count;
        if (s.Cooking > 0)
        {
            var timer = s.DurationSeconds > 0 ? " · " + Mathf.CeilToInt(Mathf.Max(0, s.DurationSeconds - s.ElapsedSeconds)) + "s" : "";
            return s.Cooking + " cooking" + timer + (string.IsNullOrEmpty(s.ActiveStation) ? "" : " · " + Local(s.ActiveStation));
        }
        if (s.Uses.Count == 0)
            return (s.Available >= s.Need ? "Available · " : "Missing · ") + count;
        if (s.Available >= s.Need) return "Prepared · " + count;
        return s.Available + " prepared · " + Math.Max(0, s.Need - s.Available) + " to make";
    }
    private static string QueueReason(KitchenStep root) => root.State switch
    {
        KitchenStepState.Ready => "Ingredients available · ready to prepare",
        KitchenStepState.Queued => "Ingredients available · waiting for a station",
        KitchenStepState.Prepared => "Preparation complete",
        _ => string.IsNullOrWhiteSpace(root.Note) ? "Queue this dish for preparation" : Local(root.Note)
    };
    private static Color Tint(KitchenStepState s) => s == KitchenStepState.Prepared ? new Color(.58f,.76f,.56f)
        : s == KitchenStepState.Blocked ? RestlessUi.HealthTint : RestlessUi.Accent;
    private static string TierName(string tier) => tier.ToLowerInvariant() switch
    {
        "blackforest" => "Black Forest", "deepnorth" => "Deep North", "" => "Kitchen",
        _ => char.ToUpperInvariant(tier[0]) + tier.Substring(1)
    };
    private static int TierOrder(string tier) => tier.ToLowerInvariant() switch
    {
        "meadows" => 0, "blackforest" => 1, "swamp" => 2, "mountains" => 3,
        "plains" => 4, "mistlands" => 5, "ashlands" => 6, "deepnorth" => 7, _ => 8
    };
    private static string StationName(KitchenStationKind s) => s == KitchenStationKind.MeadKettle ? "Mead kettle" : s == KitchenStationKind.PrepTable ? "Preparation table" : s == KitchenStationKind.Rack ? "Cooking rack" : s.ToString();
    [HarmonyPatch(typeof(Menu), "Update")]
    private static class MenuInput
    {
        private static bool Prefix() => _current == null && Time.frameCount != _closedFrame;
    }
}



