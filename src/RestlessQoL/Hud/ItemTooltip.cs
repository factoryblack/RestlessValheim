using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Jotunn.Managers;
using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

// Tab + chest inspect. Tray at the cursor. Title sits in the plate; a solid
// TitleTear may escape the top-right corner. Slot is DressSlot.
// Vanilla UITooltip LateUpdate is skipped while a bag/chest item is hovered.
public sealed class ItemTooltip : FeatureModule
{
    public override string Id => "ui.tooltip";
    public override bool Enabled => true;
    public override bool TickInMenus => true;

    private const float CardWidth = 320f;
    private const float Pad = 16f;
    private static GameObject? _card;
    private static GameObject? _body;
    private static int _stamp;

    private static readonly string[] DamageWords =
    {
        "slash", "pierce", "blunt", "chop", "pickaxe", "fire", "frost",
        "lightning", "poison", "spirit"
    };

    protected override void OnLoaded()
    {
        GUIManager.OnCustomGUIAvailable += TearDown;
    }

    public override void Tick()
    {
        if (ModConfig.TooltipEnabled.Value)
            return;
        TearDown();
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
        private static void AfterUpdate(InventoryGui __instance)
        {
            if (!ModConfig.TooltipEnabled.Value)
            {
                TearDown();
                return;
            }

            if (!InventoryGui.IsVisible())
            {
                HideCard();
                return;
            }

            Dress(__instance);
        }

        // Skip the vanilla item tip so wood never paints a frame.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UITooltip), nameof(UITooltip.OnPointerEnter))]
        private static bool SkipEnter(UITooltip __instance) => !Ours(__instance);

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UITooltip), nameof(UITooltip.OnHoverStart))]
        private static bool SkipHover(UITooltip __instance) => !Ours(__instance);

        // LateUpdate / Anchor run after InventoryGui.Update and put the wood
        // back. Skip the whole tick while our plate is up.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UITooltip), "LateUpdate")]
        private static bool SkipLate() => !Hovering();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UITooltip), "AnchorTooltip")]
        private static bool SkipAnchor() => !Hovering();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UITooltip), "UpdateTextElements")]
        private static bool SkipText() => !Hovering();
    }

    private static bool Hovering()
    {
        if (!ModConfig.TooltipEnabled.Value || !InventoryGui.IsVisible())
            return false;
        var gui = InventoryGui.instance;
        return gui != null && Hovered(gui) != null;
    }

    private static bool Ours(UITooltip tip) =>
        ModConfig.TooltipEnabled.Value && InventoryGui.IsVisible()
        && tip != null && tip.GetComponentInParent<InventoryElement>() != null;

    private static void Dress(InventoryGui gui)
    {
        var item = Hovered(gui);
        if (item?.m_shared == null)
        {
            HideCard();
            return;
        }

        UITooltip.HideTooltip();
        if (UITooltip.m_tooltip != null)
            UITooltip.m_tooltip.SetActive(false);
        Bind(gui, item);
        Park();
    }

    private static ItemDrop.ItemData? Hovered(InventoryGui gui)
    {
        var hit = FromGrid(gui.m_playerGrid);
        return hit ?? FromGrid(gui.m_containerGrid);
    }

    private static ItemDrop.ItemData? FromGrid(InventoryGrid? grid)
    {
        if (grid == null)
            return null;
        var el = grid.GetHoveredElement();
        if (el == null)
            return null;
        var inv = grid.GetInventory();
        return inv?.GetItemAt(el.Position.x, el.Position.y);
    }

    private static void Bind(InventoryGui gui, ItemDrop.ItemData item)
    {
        var host = Host(gui);
        if (host == null)
            return;
        if (_card == null || _card.transform.parent != host || _card.transform.Find("edgeTop") != null)
        {
            DropCard();
            _card = Plate(host);
            _body = RestlessUi.Node(_card.transform, "body");
            RestlessUi.Stretch(_body, Vector2.zero, Vector2.one, new Vector2(Pad, Pad), new Vector2(-Pad, -Pad));
            var layout = _body.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            _body.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        var stamp = Stamp(item);
        if (stamp != _stamp || !_card.activeSelf)
        {
            _stamp = stamp;
            _card.SetActive(true);
            RestlessUi.Wipe(_body!.transform);
            var title = Soft(item.m_shared.m_name);
            if (string.IsNullOrEmpty(title))
                title = "Item";
            Fill(_body.transform, item, title);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_body.GetComponent<RectTransform>());
            var h = Mathf.Clamp(_body.GetComponent<RectTransform>().rect.height + Pad * 2f, 140f, 560f);
            RestlessUi.Pin(_card, new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), Vector2.zero,
                new Vector2(CardWidth, h));
            var stale = _card.transform.Find("lid");
            if (stale != null)
                Object.Destroy(stale.gameObject);
            var name = _body.transform.Find("head/meta/title") as RectTransform;
            RestlessUi.TitleTear(_card, name);
        }

        _card.transform.SetAsLastSibling();
    }

    private static Transform? Host(InventoryGui gui)
    {
        var canvas = gui.GetComponentInParent<Canvas>();
        if (canvas != null)
            return canvas.transform;
        if (gui.m_inventoryRoot != null)
            return gui.m_inventoryRoot.parent != null ? gui.m_inventoryRoot.parent : gui.m_inventoryRoot;
        return gui.transform;
    }

    private static void Park()
    {
        if (_card == null || !_card.activeSelf)
            return;
        var plate = _card.GetComponent<RectTransform>();
        var parent = plate.parent as RectTransform;
        if (parent == null)
            return;
        var canvas = _card.GetComponentInParent<Canvas>();
        var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Input.mousePosition, cam, out var local))
            return;

        var size = plate.sizeDelta;
        var hang = RestlessUi.HeadOverhang;
        var lift = RestlessUi.HeadLift;
        var box = parent.rect;
        var pos = local + new Vector2(18f, -12f);
        if (pos.x + size.x + hang > box.xMax - 8f)
            pos.x = local.x - size.x - 18f;
        if (pos.y - size.y < box.yMin + 8f)
            pos.y = local.y + 12f;
        pos.x = Mathf.Clamp(pos.x, box.xMin + 8f, box.xMax - size.x - 8f - hang);
        pos.y = Mathf.Clamp(pos.y, box.yMin + size.y + 8f, box.yMax - 8f - lift);
        plate.anchorMin = plate.anchorMax = new Vector2(0.5f, 0.5f);
        plate.pivot = new Vector2(0f, 1f);
        plate.anchoredPosition = pos;
    }

    private static void Fill(Transform body, ItemDrop.ItemData item, string title)
    {
        Parse(item.GetTooltip(), title, out var stats, out var chips, out var flavor);
        var blurb = Soft(item.m_shared.m_description);
        if (blurb.Length > 0)
            flavor = new List<string> { blurb };

        var head = RestlessUi.Node(body, "head");
        var row = head.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 10f;
        row.childAlignment = TextAnchor.UpperLeft;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;
        var headHold = head.AddComponent<LayoutElement>();
        headHold.minHeight = 72f;
        headHold.flexibleHeight = 0f;

        var cell = RestlessUi.Node(head.transform, "slot");
        var cellHold = cell.AddComponent<LayoutElement>();
        cellHold.minWidth = cellHold.preferredWidth = 64f;
        cellHold.minHeight = cellHold.preferredHeight = 64f;
        cellHold.flexibleWidth = 0f;
        var icon = RestlessUi.Graphic(cell.transform, "icon", Color.white, false);
        var iconImg = icon.GetComponent<Image>();
        iconImg.sprite = RestlessUi.IconOf(item);
        iconImg.preserveAspect = true;
        RestlessUi.Pin(icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(44f, 44f));
        var plate = RestlessUi.DressSlot(cell, iconImg, false, item, null, true, SlotLock.Held(item));
        RestlessUi.Pin(plate, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(64f, 64f));

        var meta = RestlessUi.Node(head.transform, "meta");
        var metaHold = meta.AddComponent<LayoutElement>();
        metaHold.flexibleWidth = 1f;
        metaHold.minHeight = 64f;
        var metaCol = meta.AddComponent<VerticalLayoutGroup>();
        metaCol.spacing = 4f;
        metaCol.childAlignment = TextAnchor.UpperLeft;
        metaCol.childControlWidth = true;
        metaCol.childControlHeight = true;
        metaCol.childForceExpandWidth = true;
        metaCol.childForceExpandHeight = false;

        var name = RestlessUi.Label(meta.transform, title, RestlessUi.TitleSize, RestlessUi.Text,
            TextAnchor.UpperLeft);
        name.gameObject.name = "title";
        name.horizontalOverflow = HorizontalWrapMode.Wrap;
        name.verticalOverflow = VerticalWrapMode.Overflow;
        name.lineSpacing = 0.85f;
        var nameHold = name.gameObject.AddComponent<LayoutElement>();
        nameHold.minHeight = 36f;
        nameHold.preferredHeight = title.Length > 16 ? 64f : 36f;
        nameHold.flexibleWidth = 1f;

        var line = RestlessUi.Node(meta.transform, "line");
        Hold(line, 26f);
        var lineRow = line.AddComponent<HorizontalLayoutGroup>();
        lineRow.spacing = 6f;
        lineRow.childAlignment = TextAnchor.MiddleLeft;
        lineRow.childControlWidth = true;
        lineRow.childControlHeight = true;
        lineRow.childForceExpandWidth = false;
        lineRow.childForceExpandHeight = false;
        TypeChip(line.transform, TypeName(item.m_shared.m_itemType));

        foreach (var stat in stats)
            StatRow(body, stat.label, stat.value);

        if (chips.Count > 0)
        {
            var tray = RestlessUi.Node(body, "chips");
            var grid = tray.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(88f, 24f);
            grid.spacing = new Vector2(6f, 4f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            Hold(tray, ((chips.Count + 2) / 3) * 28f);
            foreach (var chip in chips)
                DamageChip(tray.transform, chip.label, chip.value);
        }

        if (flavor.Count == 0)
            return;
        var note = RestlessUi.Label(body, string.Join("\n", flavor), RestlessUi.HudSize, RestlessUi.Muted,
            TextAnchor.UpperLeft);
        note.horizontalOverflow = HorizontalWrapMode.Wrap;
        note.verticalOverflow = VerticalWrapMode.Overflow;
        var noteHold = note.gameObject.AddComponent<LayoutElement>();
        noteHold.minHeight = 18f;
        noteHold.preferredHeight = 20f + flavor.Count * 16f;
        noteHold.flexibleWidth = 1f;
    }

    private static void TypeChip(Transform parent, string copy)
    {
        if (string.IsNullOrEmpty(copy))
            return;
        var chip = RestlessUi.Chip(parent, "type");
        var hold = chip.AddComponent<LayoutElement>();
        hold.preferredWidth = Mathf.Clamp(copy.Length * 8f + 20f, 72f, 140f);
        hold.preferredHeight = 24f;
        hold.flexibleWidth = 0f;
        var face = RestlessUi.Label(chip.transform, copy, RestlessUi.HudMeta, RestlessUi.Accent,
            TextAnchor.MiddleCenter);
        RestlessUi.Stretch(face.gameObject, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
    }

    private static void StatRow(Transform parent, string label, string value)
    {
        var go = RestlessUi.Node(parent, "stat");
        Hold(go, 18f);
        var left = RestlessUi.Label(go.transform, label, RestlessUi.HudSize, RestlessUi.Muted,
            TextAnchor.MiddleLeft);
        RestlessUi.Stretch(left.gameObject, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-100f, 0f));
        var right = RestlessUi.Label(go.transform, value, RestlessUi.HudSize, RestlessUi.Accent,
            TextAnchor.MiddleRight);
        RestlessUi.Stretch(right.gameObject, Vector2.zero, Vector2.one, new Vector2(120f, 0f), Vector2.zero);
    }

    private static void DamageChip(Transform parent, string label, string value)
    {
        var chip = RestlessUi.Chip(parent, "dmg");
        chip.GetComponent<Image>().color = DamageTint(label);
        var copy = string.IsNullOrEmpty(value) ? label : label + " " + value;
        var face = RestlessUi.Label(chip.transform, copy, RestlessUi.HudMeta, RestlessUi.Text,
            TextAnchor.MiddleCenter);
        RestlessUi.Stretch(face.gameObject, Vector2.zero, Vector2.one, new Vector2(4f, 0f), new Vector2(-4f, 0f));
    }

    private static Color DamageTint(string label)
    {
        var n = label.ToLowerInvariant();
        if (n.Contains("fire") || n.Contains("poison"))
            return Color.Lerp(RestlessUi.ChipTint, RestlessUi.HealthTint, 0.55f);
        if (n.Contains("frost"))
            return Color.Lerp(RestlessUi.ChipTint, RestlessUi.Muted, 0.4f);
        if (n.Contains("lightning") || n.Contains("spirit"))
            return Color.Lerp(RestlessUi.ChipTint, RestlessUi.Accent, 0.45f);
        if (n.Contains("slash") || n.Contains("pierce") || n.Contains("blunt")
            || n.Contains("chop") || n.Contains("pickaxe"))
            return Color.Lerp(RestlessUi.ChipTint, RestlessUi.StaminaTint, 0.25f);
        return RestlessUi.ChipTint;
    }

    private static void Parse(string raw, string title, out List<(string label, string value)> stats,
        out List<(string label, string value)> chips, out List<string> flavor)
    {
        stats = new List<(string, string)>();
        chips = new List<(string, string)>();
        flavor = new List<string>();
        if (string.IsNullOrWhiteSpace(raw))
            return;

        var seen = new HashSet<string>();
        foreach (var chunk in raw.Replace("\r", "").Split('\n'))
        {
            var line = Soft(chunk).Trim();
            if (line.Length == 0)
                continue;
            if (title.Length > 0 && string.Equals(line, title, System.StringComparison.OrdinalIgnoreCase))
                continue;
            var colon = line.IndexOf(':');
            if (colon > 0 && colon < line.Length - 1)
            {
                var label = Phrase(line.Substring(0, colon).Trim());
                var value = line.Substring(colon + 1).Trim();
                if (label.Length == 0 || value.Length == 0)
                    continue;
                var key = label.ToLowerInvariant();
                if (key.Contains("quality") || key.Contains("upgrade"))
                    continue;
                if (!seen.Add(key + "=" + value))
                    continue;
                if (IsDamage(key))
                    chips.Add((label, value));
                else
                    stats.Add((label, value));
                continue;
            }

            if (line.Length < 12 || IsDamage(line.ToLowerInvariant()))
                continue;
            if (seen.Add("f:" + line))
                flavor.Add(Phrase(line));
        }
    }

    // Localize each line, then strip tags. Bare() on the whole blob was
    // gluing "Crafted by" into "Craftedby" and hiding spaces in (4 parts).
    private static string Soft(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "";
        if (raw.IndexOf('$') >= 0 && Localization.instance != null)
            raw = Localization.instance.Localize(raw);
        var sb = new StringBuilder(raw.Length);
        var hide = false;
        var tag = new StringBuilder();
        var afterTag = false;
        foreach (var c in raw)
        {
            if (c == '<')
            {
                hide = true;
                tag.Length = 0;
                continue;
            }

            if (c == '>')
            {
                hide = false;
                var name = tag.ToString();
                if (name.StartsWith("space", System.StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("/space", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (sb.Length == 0 || sb[sb.Length - 1] != ' ')
                        sb.Append(' ');
                    afterTag = false;
                }
                else
                    afterTag = sb.Length > 0 && (char.IsLetterOrDigit(sb[sb.Length - 1]) || sb[sb.Length - 1] == '%');
                continue;
            }

            if (hide)
            {
                tag.Append(c);
                continue;
            }

            if (c == '\u200B' || c == '\u2060' || c == '\uFEFF' || c == '\u00AD')
            {
                if (sb.Length > 0 && sb[sb.Length - 1] != ' ' && sb[sb.Length - 1] != '\n')
                    sb.Append(' ');
                afterTag = false;
                continue;
            }

            var ch = c == '\u00A0' || c == '\u202F' || c == '\u2009' ? ' ' : c;
            if (ch == '\r')
                continue;
            if (ch == '(' && sb.Length > 0 && (char.IsDigit(sb[sb.Length - 1]) || sb[sb.Length - 1] == '%'))
                sb.Append(' ');
            if (afterTag && (char.IsLetterOrDigit(ch) || ch == '(') && sb.Length > 0 && sb[sb.Length - 1] != ' '
                && sb[sb.Length - 1] != '\n')
                sb.Append(' ');
            afterTag = false;
            sb.Append(ch);
        }

        return sb.ToString();
    }

    // Compact labels back to the localized $item_* phrase (Craftedby → Crafted by).
    private static readonly string[] ItemTokens =
    {
        "item_crafter", "item_weight", "item_durability", "item_quality",
        "item_repairlevel", "item_repairstationlevel", "item_armor", "item_armour",
        "item_blockarmor", "item_blockarmour", "item_blockforce", "item_deflection",
        "item_parrybonus", "item_parryadrenaline", "item_knockback", "item_backstab",
        "item_stamina", "item_staminause", "item_staminahold", "item_drawstamina",
        "item_use_stamina", "item_movementmodifier", "item_movement_modifier",
        "item_movementspeed", "item_seteffect", "item_set", "item_style",
        "item_value", "item_onehanded", "item_twohanded"
    };

    private static string Phrase(string label)
    {
        if (string.IsNullOrEmpty(label) || label.IndexOf(' ') >= 0)
            return label;
        var compact = Compact(label);
        foreach (var token in ItemTokens)
        {
            var loc = Soft("$" + token);
            if (!GoodPhrase(loc) || Compact(loc) != compact && !Near(compact, Compact(loc)))
                continue;
            return loc;
        }

        return label;
    }

    private static bool GoodPhrase(string loc) =>
        loc.Length > 0 && loc.IndexOf('$') < 0;

    private static bool Near(string a, string b)
    {
        if (a.Length < 4 || b.Length < 4 || Mathf.Abs(a.Length - b.Length) > 2)
            return false;
        if (a.Contains(b) || b.Contains(a))
            return true;
        var longer = a.Length >= b.Length ? a : b;
        var shorter = a.Length >= b.Length ? b : a;
        var j = 0;
        var skip = 0;
        for (var i = 0; i < longer.Length; i++)
        {
            if (j < shorter.Length && longer[i] == shorter[j])
            {
                j++;
                continue;
            }

            skip++;
            if (skip > 2)
                return false;
        }

        return j == shorter.Length;
    }

    private static string Compact(string raw)
    {
        var sb = new StringBuilder(raw.Length);
        foreach (var c in raw)
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }

    private static GameObject Plate(Transform host)
    {
        return RestlessUi.Tray(host, "RestlessTooltip", false);
    }

    private static bool IsDamage(string label)
    {
        foreach (var word in DamageWords)
        {
            if (label == word || label.Contains(word))
                return true;
        }

        return false;
    }

    private static string TypeName(ItemDrop.ItemData.ItemType type)
    {
        var raw = type.ToString();
        switch (raw)
        {
            case "OneHandedWeapon":
                return "One-handed";
            case "TwoHandedWeapon":
            case "TwoHandedWeaponLeft":
                return "Two-handed";
            case "Attach_Atgeir":
                return "Atgeir";
            case "AmmoNonEquipable":
                return "Ammo";
            default:
                return SplitCamel(raw);
        }
    }

    private static string SplitCamel(string raw)
    {
        if (string.IsNullOrEmpty(raw) || raw == "None")
            return "";
        var sb = new StringBuilder(raw.Length + 4);
        for (var i = 0; i < raw.Length; i++)
        {
            if (i > 0 && char.IsUpper(raw[i]) && !char.IsUpper(raw[i - 1]))
                sb.Append('-');
            sb.Append(raw[i]);
        }

        return sb.ToString();
    }

    private static int Stamp(ItemDrop.ItemData item) =>
        item.m_shared.m_name.GetHashCode() * 31
        + item.m_quality * 17
        + item.m_stack * 13
        + (int)item.m_durability
        + item.m_gridPos.x
        + item.m_gridPos.y * 100;

    private static void Hold(GameObject go, float height)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
        le.flexibleHeight = 0f;
    }

    private static void HideCard()
    {
        if (_card != null)
            _card.SetActive(false);
        _stamp = 0;
    }

    private static void DropCard()
    {
        if (_card != null)
            Object.Destroy(_card);
        _card = null;
        _body = null;
        _stamp = 0;
    }

    private static void TearDown()
    {
        DropCard();
    }
}
