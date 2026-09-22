using System.Collections;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Jotunn.Managers;
using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

// Tab loadout totals. Hover a row for origins. Live getters only.
public sealed class StatSheet : FeatureModule
{
    public override string Id => "ui.sheet";
    public override bool Enabled => true;
    public override bool TickInMenus => true;

    private const float SheetWidth = 440f;
    private const float SheetHeight = 480f;
    private const float RowH = 36f;

    private static GameObject? _root;
    private static GameObject? _body;
    private static GameObject? _origin;
    private static Text? _originFace;
    private static bool _open;
    private static string _stamp = "";
    private static string _hover = "";

    protected override void OnLoaded() => GUIManager.OnCustomGUIAvailable += TearDown;

    public override void Tick()
    {
        if (!ModConfig.SheetEnabled.Value || !InventoryGui.IsVisible())
        {
            Hide();
            return;
        }

        var gui = InventoryGui.instance;
        if (gui == null)
        {
            Hide();
            return;
        }

        if (OtherModal(gui))
            Hide();
        if (_open)
            Paint();
    }

    internal static void Hook(Transform host)
    {
        if (host == null || !ModConfig.SheetEnabled.Value)
            return;
        var plate = host.Find("RestlessStat") ?? host;
        if (plate.GetComponent<SheetOpener>() != null)
            return;
        var image = plate.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = true;
        var button = plate.gameObject.GetComponent<Button>() ?? plate.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(Toggle);
        var hint = plate.gameObject.GetComponent<RestlessHint>() ?? plate.gameObject.AddComponent<RestlessHint>();
        hint.Copy = "Open loadout totals";
        plate.gameObject.AddComponent<SheetOpener>();
    }

    private static void Toggle()
    {
        if (_open)
            Hide();
        else
            Show();
    }

    private static void Show()
    {
        if (!ModConfig.SheetEnabled.Value)
            return;
        var gui = InventoryGui.instance;
        var player = Player.m_localPlayer;
        if (gui == null || player == null)
            return;
        CloseOthers(gui);
        Ensure(gui);
        _open = true;
        _stamp = "";
        _hover = "";
        if (_root != null)
            _root.SetActive(true);
        Paint();
    }

    internal static void Hide()
    {
        _open = false;
        _hover = "";
        if (_origin != null)
            _origin.SetActive(false);
        if (_root != null)
            _root.SetActive(false);
    }

    private static void TearDown()
    {
        Hide();
        if (_root != null)
            Object.Destroy(_root);
        _root = _body = _origin = null;
        _originFace = null;
        _stamp = "";
    }

    private static bool OtherModal(InventoryGui gui) =>
        gui.m_textsDialog != null && gui.m_textsDialog.gameObject.activeInHierarchy
        || gui.m_skillsDialog != null && gui.m_skillsDialog.gameObject.activeInHierarchy
        || gui.m_trophiesPanel != null && gui.m_trophiesPanel.activeInHierarchy
        || gui.m_achievementsPanel != null && gui.m_achievementsPanel.gameObject.activeInHierarchy;

    private static void CloseOthers(InventoryGui gui)
    {
        if (gui.m_textsDialog != null)
            gui.m_textsDialog.gameObject.SetActive(false);
        if (gui.m_skillsDialog != null)
            gui.m_skillsDialog.gameObject.SetActive(false);
        if (gui.m_trophiesPanel != null)
            gui.m_trophiesPanel.SetActive(false);
        if (gui.m_achievementsPanel != null)
            gui.m_achievementsPanel.gameObject.SetActive(false);
    }

    private static void Ensure(InventoryGui gui)
    {
        if (_root != null)
            return;
        var parent = gui.m_inventoryRoot != null ? gui.m_inventoryRoot.parent : gui.transform;
        _root = RestlessUi.Node(parent, "RestlessSheet");
        var rt = _root.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(SheetWidth, SheetHeight);
        rt.anchoredPosition = new Vector2(80f, 10f);
        RestlessUi.PaperSurface(_root);
        var face = _root.GetComponent<Image>();
        if (face != null)
            face.raycastTarget = true;
        RestlessUi.PaperCorner(_root);

        var head = RestlessUi.Chip(_root.transform, "head");
        RestlessUi.PaperControl(head);
        RestlessUi.Stretch(head, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(18f, -56f), new Vector2(-18f, -16f));
        var title = RestlessUi.Label(head.transform, "Loadout", RestlessUi.TitleSize, RestlessUi.Text,
            TextAnchor.MiddleLeft);
        RestlessUi.Stretch(title.gameObject, Vector2.zero, Vector2.one, new Vector2(16f, 0f), new Vector2(-72f, 0f));

        var close = RestlessUi.Chip(head.transform, "close");
        RestlessUi.PaperControl(close);
        RestlessUi.Pin(close, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-10f, 0f), new Vector2(56f, 28f));
        var closeLabel = RestlessUi.Label(close.transform, "ESC", RestlessUi.HintSize, RestlessUi.Text,
            TextAnchor.MiddleCenter);
        RestlessUi.Stretch(closeLabel.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var closeBtn = close.AddComponent<Button>();
        closeBtn.targetGraphic = close.GetComponent<Image>();
        RestlessUi.PaperSelectable(closeBtn);
        closeBtn.onClick.AddListener(Hide);

        var view = RestlessUi.Node(_root.transform, "view");
        RestlessUi.Stretch(view, Vector2.zero, Vector2.one, new Vector2(16f, 16f), new Vector2(-16f, -64f));
        var mask = view.AddComponent<RectMask2D>();
        _ = mask;
        var scroll = view.AddComponent<RestlessScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = false;
        scroll.viewport = view.GetComponent<RectTransform>();

        _body = RestlessUi.Node(view.transform, "body");
        var bodyRt = _body.GetComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0f, 1f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.pivot = new Vector2(0.5f, 1f);
        bodyRt.sizeDelta = new Vector2(0f, 0f);
        var layout = _body.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 2f;
        _body.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = bodyRt;

        _origin = RestlessUi.Chip(_root.transform, "origin");
        RestlessUi.PaperControl(_origin);
        RestlessUi.Pin(_origin, new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(12f, -64f), new Vector2(240f, 80f));
        _originFace = RestlessUi.Label(_origin.transform, "", RestlessUi.HintSize, RestlessUi.Text,
            TextAnchor.UpperLeft);
        RestlessUi.Stretch(_originFace.gameObject, Vector2.zero, Vector2.one, new Vector2(14f, 10f),
            new Vector2(-14f, -10f));
        _originFace.horizontalOverflow = HorizontalWrapMode.Wrap;
        _originFace.verticalOverflow = VerticalWrapMode.Overflow;
        _origin.SetActive(false);
    }

    private static void Paint()
    {
        var player = Player.m_localPlayer;
        if (player == null || _body == null || _root == null)
            return;
        var rows = Collect(player);
        var stamp = Signature(rows);
        if (stamp == _stamp)
        {
            PlaceOrigin();
            return;
        }

        _stamp = stamp;
        foreach (Transform child in _body.transform)
            Object.Destroy(child.gameObject);

        string? group = null;
        foreach (var row in rows)
        {
            if (row.Group != group)
            {
                group = row.Group;
                Head(group);
            }

            Line(row);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_body.GetComponent<RectTransform>());
        PlaceOrigin();
    }

    private static void Head(string title)
    {
        var row = RestlessUi.Node(_body!.transform, "head");
        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 28f;
        le.minHeight = 28f;
        var face = RestlessUi.Label(row.transform, title, RestlessUi.HintSize, RestlessUi.PaperMuted,
            TextAnchor.MiddleLeft);
        RestlessUi.Stretch(face.gameObject, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
        RestlessUi.PaperDivider(row);
    }

    private static void Line(StatRow row)
    {
        var go = RestlessUi.Node(_body!.transform, "row");
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = RowH;
        le.minHeight = RowH;
        RestlessUi.Metric(go.transform, row.Label, row.Value);
        var hit = go.AddComponent<Image>();
        hit.color = Color.clear;
        hit.raycastTarget = true;
        var hold = go.AddComponent<SheetHover>();
        hold.Copy = Origins(row);
        var trigger = go.AddComponent<EventTrigger>();
        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => Hover(hold.Copy));
        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => Hover(""));
        trigger.triggers.Add(enter);
        trigger.triggers.Add(exit);
    }

    private static void Hover(string copy)
    {
        _hover = copy;
        PlaceOrigin();
    }

    private static void PlaceOrigin()
    {
        if (_origin == null || _originFace == null)
            return;
        if (string.IsNullOrEmpty(_hover))
        {
            _origin.SetActive(false);
            return;
        }

        _originFace.text = _hover;
        var lines = Mathf.Max(1, _hover.Split('\n').Length);
        var height = Mathf.Clamp(20f + lines * 18f, 56f, 220f);
        var rt = _origin.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(240f, height);
        _origin.SetActive(true);
    }

    private static string Signature(List<StatRow> rows)
    {
        var sb = new StringBuilder(rows.Count * 24);
        foreach (var row in rows)
        {
            sb.Append(row.Label);
            sb.Append('=');
            sb.Append(row.Value);
            sb.Append(';');
        }

        return sb.ToString();
    }

    private static string Origins(StatRow row)
    {
        if (row.Parts.Count == 0)
            return row.Label + "\n" + row.Value;
        var sb = new StringBuilder();
        sb.Append(row.Label);
        foreach (var part in row.Parts)
        {
            sb.Append('\n');
            sb.Append(part.Name);
            sb.Append("  ");
            sb.Append(part.Value);
        }

        return sb.ToString();
    }

    private static List<StatRow> Collect(Player player)
    {
        var rows = new List<StatRow>();
        var foods = player.GetFoods();
        var equipped = Equipped(player);
        var effects = Effects(player);

        Pool(rows, "Pools", "Health", player.GetHealth(), player.GetMaxHealth(), foods, f => f.m_health);
        Pool(rows, "Pools", "Stamina", player.GetStamina(), player.GetMaxStamina(), foods, f => f.m_stamina);
        if (player.GetMaxEitr() > 0.01f)
            Pool(rows, "Pools", "Eitr", player.GetEitr(), player.GetMaxEitr(), foods, f => f.m_eitr);
        var adrMax = player.GetMaxAdrenaline();
        if (adrMax > 0.01f)
            Add(rows, "Pools", "Adrenaline", Pair(player.GetAdrenaline(), adrMax));

        var regen = 0f;
        var regenParts = new List<Part>();
        if (foods != null)
        {
            foreach (var food in foods)
            {
                var item = food?.m_item;
                if (item?.m_shared == null)
                    continue;
                var tick = item.m_shared.m_foodRegen;
                if (Mathf.Abs(tick) < 0.01f)
                    continue;
                regen += tick;
                regenParts.Add(new Part(Name(item), Signed(tick)));
            }
        }

        if (regenParts.Count > 0)
            Add(rows, "Pools", "Regen", Signed(regen), regenParts);

        var armor = player.GetBodyArmor();
        var armorParts = new List<Part>();
        foreach (var item in equipped)
        {
            var piece = item.GetArmor();
            if (piece >= 0.05f)
                armorParts.Add(new Part(Name(item), N(piece)));
        }

        foreach (var effect in effects)
        {
            if (effect is not SE_Stats stats)
                continue;
            if (Mathf.Abs(stats.m_addArmor) >= 0.05f)
                armorParts.Add(new Part(SeName(effect), Signed(stats.m_addArmor)));
            if (Mathf.Abs(stats.m_armorMultiplier - 1f) >= 0.001f && stats.m_armorMultiplier > 0f)
                armorParts.Add(new Part(SeName(effect), "×" + N(stats.m_armorMultiplier)));
        }

        Add(rows, "Defense", "Armor", N(armor), armorParts);

        var blocker = player.GetCurrentBlocker();
        if (blocker?.m_shared != null && blocker.m_shared.m_blockable)
        {
            var block = blocker.GetBaseBlockPower();
            if (block > 0.05f)
                Add(rows, "Defense", "Block", N(block), new List<Part> { new(Name(blocker), N(block)) });
        }

        var bag = player.GetInventory();
        var carryMax = player.GetMaxCarryWeight();
        var carryNow = bag != null ? bag.GetTotalWeight() : 0f;
        var carryParts = new List<Part> { new("Bag", N(carryNow) + " / " + N(carryMax)) };
        foreach (var effect in effects)
        {
            if (effect is not SE_Stats stats || Mathf.Abs(stats.m_addMaxCarryWeight) < 0.5f)
                continue;
            carryParts.Add(new Part(SeName(effect), Signed(stats.m_addMaxCarryWeight)));
        }

        Add(rows, "Carry and move", "Weight", Pair(carryNow, carryMax), carryParts);

        var move = player.GetEquipmentMovementModifier();
        var moveParts = new List<Part>();
        foreach (var item in equipped)
        {
            var piece = item.m_shared.m_movementModifier;
            if (Mathf.Abs(piece) >= 0.001f)
                moveParts.Add(new Part(Name(item), Pct(piece)));
        }

        foreach (var effect in effects)
        {
            if (effect is not SE_Stats stats || Mathf.Abs(stats.m_speedModifier) < 0.001f)
                continue;
            move += stats.m_speedModifier;
            moveParts.Add(new Part(SeName(effect), Pct(stats.m_speedModifier)));
        }

        Add(rows, "Carry and move", "Move", Pct(move), moveParts);

        var attack = new Dictionary<string, StatRow>();
        foreach (var effect in effects)
        {
            if (effect is not SE_Stats stats)
                continue;
            var origin = Source(effect, equipped);
            AddDamage(attack, origin, stats.m_percentigeDamageModifiers);
            if (Mathf.Abs(stats.m_damageModifier) >= 0.001f)
                Merge(attack, "Attack", "Damage", stats.m_damageModifier, origin, Pct);
        }

        foreach (var row in attack.Values)
        {
            if (Mathf.Abs(row.Amount) < 0.01f)
                continue;
            row.Value = row.Format(row.Amount);
            rows.Add(row);
        }

        foreach (var type in DamageTypes)
        {
            var mod = player.GetDamageModifier(type);
            if (mod == HitData.DamageModifier.Normal)
                continue;
            var parts = new List<Part>();
            foreach (var item in equipped)
            {
                var mods = item.m_shared.m_damageModifiers;
                if (mods == null)
                    continue;
                foreach (var pair in mods)
                {
                    if (pair.m_type != type || pair.m_modifier == HitData.DamageModifier.Normal)
                        continue;
                    parts.Add(new Part(Name(item), ModName(pair.m_modifier)));
                }
            }

            foreach (var effect in effects)
            {
                if (effect is not SE_Stats stats || stats.m_mods == null)
                    continue;
                foreach (var pair in stats.m_mods)
                {
                    if (pair.m_type != type || pair.m_modifier == HitData.DamageModifier.Normal)
                        continue;
                    parts.Add(new Part(Source(effect, equipped), ModName(pair.m_modifier)));
                }
            }

            Add(rows, "Resist", TypeName(type), ModName(mod), parts);
        }

        return rows;
    }

    private static void Pool(List<StatRow> rows, string group, string label, float now, float max,
        List<Player.Food>? foods, System.Func<Player.Food, float> take)
    {
        var parts = new List<Part>();
        var food = 0f;
        if (foods != null)
        {
            foreach (var plate in foods)
            {
                if (plate?.m_item?.m_shared == null)
                    continue;
                var amount = take(plate);
                if (Mathf.Abs(amount) < 0.05f)
                    continue;
                food += amount;
                parts.Add(new Part(Name(plate.m_item), Signed(amount)));
            }
        }

        var rest = max - food;
        if (rest > 0.05f)
            parts.Insert(0, new Part("Base", Signed(rest)));
        Add(rows, group, label, Pair(now, max), parts);
    }

    private static void AddDamage(Dictionary<string, StatRow> dest, string origin, HitData.DamageTypes damage)
    {
        Merge(dest, "Attack", "Slash", damage.m_slash, origin, Pct);
        Merge(dest, "Attack", "Pierce", damage.m_pierce, origin, Pct);
        Merge(dest, "Attack", "Blunt", damage.m_blunt, origin, Pct);
        Merge(dest, "Attack", "Chop", damage.m_chop, origin, Pct);
        Merge(dest, "Attack", "Pickaxe", damage.m_pickaxe, origin, Pct);
        Merge(dest, "Attack", "Fire", damage.m_fire, origin, Pct);
        Merge(dest, "Attack", "Frost", damage.m_frost, origin, Pct);
        Merge(dest, "Attack", "Lightning", damage.m_lightning, origin, Pct);
        Merge(dest, "Attack", "Poison", damage.m_poison, origin, Pct);
        Merge(dest, "Attack", "Spirit", damage.m_spirit, origin, Pct);
    }

    private static void Merge(Dictionary<string, StatRow> dest, string group, string label, float amount,
        string origin, System.Func<float, string> format)
    {
        if (Mathf.Abs(amount) < 0.01f)
            return;
        if (!dest.TryGetValue(label, out var row))
        {
            row = new StatRow { Group = group, Label = label, Format = format };
            dest[label] = row;
        }

        row.Amount += amount;
        row.Parts.Add(new Part(origin, format(amount)));
    }

    private static void Add(List<StatRow> rows, string group, string label, string value, List<Part>? parts = null)
    {
        rows.Add(new StatRow
        {
            Group = group,
            Label = label,
            Value = value,
            Parts = parts ?? new List<Part>()
        });
    }

    private static List<ItemDrop.ItemData> Equipped(Player player)
    {
        var list = new List<ItemDrop.ItemData>();
        var bag = player.GetInventory();
        if (bag == null)
            return list;
        foreach (var item in bag.GetAllItems())
        {
            if (item is { m_equipped: true, m_shared: not null })
                list.Add(item);
        }

        return list;
    }

    private static List<StatusEffect> Effects(Player player)
    {
        var list = new List<StatusEffect>();
        var seen = new HashSet<int>();
        var field = AccessTools.Field(typeof(SEMan), "m_statusEffects");
        if (field?.GetValue(player.GetSEMan()) is IList raw)
        {
            foreach (var entry in raw)
            {
                if (entry is not StatusEffect effect || effect == null)
                    continue;
                if (!seen.Add(effect.NameHash()))
                    continue;
                list.Add(effect);
            }

            return list;
        }

        player.GetSEMan().GetHUDStatusEffects(list);
        return list;
    }

    private static string Source(StatusEffect effect, List<ItemDrop.ItemData> equipped)
    {
        var hash = effect.NameHash();
        foreach (var item in equipped)
        {
            var shared = item.m_shared;
            if (shared.m_equipStatusEffect != null && shared.m_equipStatusEffect.NameHash() == hash)
                return Name(item);
            if (shared.m_setStatusEffect != null && shared.m_setStatusEffect.NameHash() == hash)
                return Name(item) + " set";
        }

        return SeName(effect);
    }

    private static string Name(ItemDrop.ItemData item)
    {
        var raw = item.m_shared.m_name;
        return Localization.instance != null ? Localization.instance.Localize(raw) : raw;
    }

    private static string SeName(StatusEffect effect)
    {
        var raw = effect.m_name;
        return Localization.instance != null ? Localization.instance.Localize(raw) : raw;
    }

    private static string Pair(float now, float max) => N(now) + " / " + N(max);

    private static string N(float value)
    {
        if (Mathf.Abs(value - Mathf.Round(value)) < 0.05f)
            return Mathf.RoundToInt(value).ToString();
        return value.ToString("0.#");
    }

    private static string Signed(float value)
    {
        var text = N(value);
        return value > 0.001f ? "+" + text : text;
    }

    private static string Pct(float value)
    {
        var n = value * 100f;
        var text = Mathf.Abs(n - Mathf.Round(n)) < 0.05f ? Mathf.RoundToInt(n).ToString() : n.ToString("0.#");
        return (n > 0.001f ? "+" : "") + text + "%";
    }

    private static string ModName(HitData.DamageModifier mod) => mod switch
    {
        HitData.DamageModifier.Resistant => "Resistant",
        HitData.DamageModifier.VeryResistant => "Very resistant",
        HitData.DamageModifier.Weak => "Weak",
        HitData.DamageModifier.VeryWeak => "Very weak",
        HitData.DamageModifier.Immune => "Immune",
        HitData.DamageModifier.Ignore => "Ignore",
        _ => mod.ToString()
    };

    private static string TypeName(HitData.DamageType type) => type switch
    {
        HitData.DamageType.Slash => "Slash",
        HitData.DamageType.Pierce => "Pierce",
        HitData.DamageType.Blunt => "Blunt",
        HitData.DamageType.Chop => "Chop",
        HitData.DamageType.Pickaxe => "Pickaxe",
        HitData.DamageType.Fire => "Fire",
        HitData.DamageType.Frost => "Frost",
        HitData.DamageType.Lightning => "Lightning",
        HitData.DamageType.Poison => "Poison",
        HitData.DamageType.Spirit => "Spirit",
        _ => type.ToString()
    };

    private static readonly HitData.DamageType[] DamageTypes =
    {
        HitData.DamageType.Slash, HitData.DamageType.Pierce, HitData.DamageType.Blunt,
        HitData.DamageType.Chop, HitData.DamageType.Pickaxe, HitData.DamageType.Fire,
        HitData.DamageType.Frost, HitData.DamageType.Lightning, HitData.DamageType.Poison,
        HitData.DamageType.Spirit
    };

    private sealed class StatRow
    {
        public string Group = "";
        public string Label = "";
        public string Value = "";
        public float Amount;
        public System.Func<float, string> Format = Signed;
        public List<Part> Parts = new();
    }

    private readonly struct Part
    {
        public readonly string Name;
        public readonly string Value;
        public Part(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }

    private sealed class SheetOpener : MonoBehaviour { }

    private sealed class SheetHover : MonoBehaviour
    {
        public string Copy = "";
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
        private static void AfterHide() => Hide();
    }
}
