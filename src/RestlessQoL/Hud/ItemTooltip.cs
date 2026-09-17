using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Jotunn.Managers;
using RestlessQoL.Api;
using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

// The inspect surface owns layout; extensions supply data through TooltipApi.
// Uses the shared Tray / TitleTear / DressSlot / Chip kit, including the F8 toggle.
public sealed class ItemTooltip : FeatureModule
{
    public override string Id => "ui.tooltip";
    public override bool Enabled => true;
    public override bool TickInMenus => true;

    private const float CardWidth = 440f;
    private const float Pad = 18f;
    private const int CopySize = RestlessUi.MetaSize;
    private static GameObject? _card;
    private static GameObject? _body;
    private static GameObject? _viewport;
    private static GameObject? _footer;
    private static ItemDrop.ItemData? _item;
    private static string _signature = "";
    private static float _nextRefresh;
    private static int _revision;
    private static float _width;
    private static float _overflow;
    private static float _scroll;
    private static bool _warned;

    private static readonly string[] DamageWords =
    {
        "slash", "pierce", "blunt", "chop", "pickaxe", "fire", "frost",
        "lightning", "poison", "spirit"
    };

    protected override void OnLoaded() => GUIManager.OnCustomGUIAvailable += TearDown;

    public override void Tick()
    {
        if (!ModConfig.TooltipEnabled.Value || !InventoryGui.IsVisible())
            HideCard();
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
        return _card != null && _card.activeSelf && gui != null && Hovered(gui) != null;
    }

    private static bool Ours(UITooltip tip) =>
        Hovering()
        && tip != null && tip.GetComponentInParent<InventoryElement>() != null;

    private static void Dress(InventoryGui gui)
    {
        var item = Hovered(gui);
        if (item?.m_shared == null)
        {
            HideCard();
            return;
        }

        try
        {
            Bind(gui, item);
            Park();
            if (_card == null || !_card.activeSelf) return;
            UITooltip.HideTooltip();
            if (UITooltip.m_tooltip != null)
                UITooltip.m_tooltip.SetActive(false);
        }
        catch (System.Exception error)
        {
            HideCard();
            if (!_warned)
            {
                _warned = true;
                Plugin.Log.LogWarning("item tooltip: " + error);
            }
        }
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
        var parent = host as RectTransform;
        if (parent == null) return;
        if (_card == null || _card.transform.parent != host)
        {
            DropCard();
            _card = RestlessUi.Tray(host, "RestlessTooltip", false);
            var input = _card.AddComponent<CanvasGroup>();
            input.blocksRaycasts = false;
            input.interactable = false;
        }

        var width = Mathf.Min(CardWidth, Mathf.Max(180f, parent.rect.width - 36f - RestlessUi.HeadOverhang));
        var maxHeight = Mathf.Max(100f, parent.rect.height - 32f - RestlessUi.HeadLift);
        var changedItem = !ReferenceEquals(_item, item);
        if (changedItem || !_card.activeSelf || TooltipApi.Revision != _revision
            || Time.unscaledTime >= _nextRefresh || !Mathf.Approximately(width, _width))
        {
            _nextRefresh = Time.unscaledTime + 0.25f;
            _revision = TooltipApi.Revision;
            var raw = item.GetTooltip();
            var contributions = TooltipApi.Collect(item);
            var signature = Signature(item, raw, contributions, width, maxHeight);
            if (changedItem || !_card.activeSelf || signature != _signature)
            {
                _signature = signature;
                _item = item;
                _width = width;
                if (changedItem) _scroll = 0f;
                _card.SetActive(true);
                RestlessUi.Wipe(_card.transform);
                RestlessUi.Rim(_card, RimTint(contributions));
                RestlessUi.PaperSurface(_card, accent: RimTint(contributions));
                Rebuild(item, raw, contributions, maxHeight);
            }
        }

        // The tooltip never intercepts pointer raycasts from the inventory.
        // Page keys scroll long detail sections without scrolling the bag beneath it.
        if (_overflow > 0f && _viewport != null)
        {
            var page = _viewport.GetComponent<RectTransform>().rect.height * 0.8f;
            if (Input.GetKeyDown(KeyCode.PageDown)) _scroll += page;
            if (Input.GetKeyDown(KeyCode.PageUp)) _scroll -= page;
        }
        Scroll();
        _card.transform.SetAsLastSibling();
    }

    private static void Rebuild(ItemDrop.ItemData item, string raw,
        List<TooltipContribution> contributions, float maxHeight)
    {
        var inner = _width - Pad * 2f;
        var title = Soft(item.m_shared.m_name);
        var blurb = Soft(item.m_shared.m_description).Trim();
        Parse(raw, title, blurb, out var stats, out var chips, out var notes);
        var category = TypeName(item.m_shared.m_itemType);
        if (category == title) category = "";
        notes.RemoveAll(line => line == category || Handedness(line));
        stats.RemoveAll(stat => stat.label == Phrase(Soft("$item_quality")));
        var supporting = stats.FindAll(stat => SupportingLabel(stat.label));
        stats.RemoveAll(stat => SupportingLabel(stat.label));

        RestlessUi.Pin(_card!, new Vector2(0.5f, 0.5f), new Vector2(0f, 1f),
            Vector2.zero, new Vector2(_width, maxHeight));
        var header = RestlessUi.Node(_card!.transform, "head");
        Place(header, Pad, Pad, inner, 100f);
        var cell = RestlessUi.Node(header.transform, "slot");
        Place(cell, 0f, 0f, 72f, 72f);
        var icon = RestlessUi.Graphic(cell.transform, "icon", Color.white, false).GetComponent<Image>();
        icon.sprite = RestlessUi.IconOf(item);
        icon.preserveAspect = true;
        RestlessUi.Pin(icon.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(52f, 52f));
        var slot = RestlessUi.Strip(cell.transform, "portrait");
        slot.transform.SetAsFirstSibling();
        RestlessUi.PortraitFrame(slot);
        RestlessUi.Pin(slot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(72f, 72f));

        var metaWidth = Mathf.Max(100f, inner - 86f - 116f);
        var name = RestlessUi.Label(header.transform, title, RestlessUi.TitleSize,
            RestlessUi.Text, TextAnchor.UpperLeft);
        name.gameObject.name = "title";
        var nameHeight = Measure(name, metaWidth);
        Place(name.gameObject, 86f, 0f, metaWidth, nameHeight);
        var hasQuality = item.m_shared.m_maxQuality > 1 || item.m_quality > 1;
        var qualityWidth = hasQuality ? Mathf.Min(metaWidth, item.m_quality > 5 ? 122f : item.m_quality * 18f) : 0f;
        var wrapQuality = hasQuality && category.Length > 0 && metaWidth - qualityWidth - 8f < 100f;
        var typeWidth = wrapQuality ? metaWidth : Mathf.Max(56f, metaWidth - qualityWidth - (hasQuality ? 8f : 0f));
        var ribbon = RestlessUi.Picture(header.transform, "category", "category-strip");
        RestlessUi.CategoryStrip(ribbon);
        var type = RestlessUi.Label(ribbon.transform, category,
            RestlessUi.HudMeta, RestlessUi.Accent, TextAnchor.MiddleLeft);
        var typeHeight = Mathf.Max(24f, Measure(type, typeWidth - 16f) + 8f);
        Place(ribbon, 86f, nameHeight + 4f, typeWidth, typeHeight);
        RestlessUi.Stretch(type.gameObject, Vector2.zero, Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -4f));
        ribbon.GetComponent<Image>().raycastTarget = false;
        ribbon.SetActive(category.Length > 0);
        var headerHeight = Mathf.Max(72f, nameHeight + typeHeight + 4f + (wrapQuality ? 24f : 0f));
        if (hasQuality)
        {
            var quality = RestlessUi.Node(header.transform, "quality");
            Place(quality, 86f + (wrapQuality || category.Length == 0 ? 0f : typeWidth + 8f),
                nameHeight + 4f + (wrapQuality ? typeHeight : 0f), qualityWidth, wrapQuality ? 24f : typeHeight);
            RestlessUi.QualityMarks(quality.transform, item.m_quality, qualityWidth);
        }
        Place(header, Pad, Pad, inner, headerHeight);

        _viewport = RestlessUi.Node(_card.transform, "viewport");
        _viewport.AddComponent<RectMask2D>();
        _body = RestlessUi.Node(_viewport.transform, "body");
        Place(_body, 0f, 0f, inner, 0f);
        var layout = _body.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 7f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperLeft;

        // Extensions contribute rarity/set badges; actual quality lives in the fixed header.
        foreach (var contribution in contributions)
            foreach (var badge in contribution.Badges)
                if (!string.IsNullOrWhiteSpace(badge.Text))
                    Badge(_body.transform, Soft(badge.Text), badge.Tint ?? RestlessUi.Accent, inner);

        if (blurb.Length > 0)
            Paragraph(_body.transform, blurb, inner, RestlessUi.PaperMuted);
        if (stats.Count > 0)
        {
            Divider(_body.transform);
            foreach (var stat in stats)
                StatRow(_body.transform, stat.label, stat.value, inner);
        }

        if (chips.Count > 0)
        {
            Divider(_body.transform);
            // Two generous cells per row; measured text may wrap instead of clipping.
            var columns = inner >= 280f ? 2 : 1;
            for (var i = 0; i < chips.Count; i += columns)
            {
                var row = RestlessUi.Node(_body.transform, "damage");
                var cellWidth = (inner - (columns - 1) * 8f) / columns;
                var leftHeight = DamageChip(row.transform, chips[i].label, chips[i].value, 0f, cellWidth);
                var rightHeight = columns == 2 && i + 1 < chips.Count
                    ? DamageChip(row.transform, chips[i + 1].label, chips[i + 1].value, cellWidth + 8f, cellWidth)
                    : 0f;
                Hold(row, Mathf.Max(leftHeight, rightHeight));
            }
        }

        if (supporting.Count > 0)
        {
            Divider(_body.transform);
            foreach (var stat in supporting)
                StatRow(_body.transform, stat.label, stat.value, inner, true);
        }

        // Preserve unfamiliar vanilla/mod lines, including short set/effect lines.
        if (notes.Count > 0)
        {
            Divider(_body.transform);
            Paragraph(_body.transform, string.Join("\n", notes), inner, RestlessUi.Text);
        }
        foreach (var contribution in contributions)
        {
            foreach (var section in contribution.Sections)
            {
                if (string.IsNullOrWhiteSpace(section.Title) && string.IsNullOrWhiteSpace(section.Body)
                    && section.Rows.Count == 0) continue;
                Divider(_body.transform);
                if (!string.IsNullOrWhiteSpace(section.Title))
                    Paragraph(_body.transform, Soft(section.Title), inner, RestlessUi.Accent);
                foreach (var stat in section.Rows)
                    StatRow(_body.transform, Soft(stat.Label), Soft(stat.Value), inner);
                if (!string.IsNullOrWhiteSpace(section.Body))
                    Paragraph(_body.transform, Soft(section.Body), inner, RestlessUi.PaperMuted);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_body.GetComponent<RectTransform>());
        var contentHeight = LayoutUtility.GetPreferredHeight(_body.GetComponent<RectTransform>());
        var top = Pad + headerHeight + 14f;
        var available = Mathf.Max(32f, maxHeight - top - Pad);
        var needsScroll = contentHeight > available;
        var viewportHeight = Mathf.Min(contentHeight, needsScroll ? Mathf.Max(32f, available - 25f) : available);
        _overflow = Mathf.Max(0f, contentHeight - viewportHeight);
        Place(_viewport, Pad, top, inner, viewportHeight);
        Place(_body, 0f, 0f, inner, contentHeight);
        var height = top + viewportHeight + Pad + (needsScroll ? 25f : 0f);
        // Extremely small canvases still keep the entire surface within the screen.
        _card.GetComponent<RectTransform>().sizeDelta = new Vector2(_width, height);
        _card.transform.localScale = Vector3.one * Mathf.Min(1f, maxHeight / height);
        if (needsScroll)
        {
            _footer = RestlessUi.Node(_card.transform, "paging");
            Place(_footer, Pad, height - Pad - 18f, inner, 18f);
            var hint = RestlessUi.Label(_footer.transform, "", RestlessUi.HudMeta,
                RestlessUi.PaperMuted, TextAnchor.MiddleRight);
            RestlessUi.Stretch(hint.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }
        else _footer = null;
        RestlessUi.PaperCorner(_card);
    }

    private static void Scroll()
    {
        if (_body == null) return;
        _scroll = Mathf.Clamp(_scroll, 0f, _overflow);
        _body.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, _scroll);
        if (_footer != null)
        {
            var hint = _footer.GetComponentInChildren<Text>();
            hint.text = "PgUp / PgDn  ·  " + Mathf.RoundToInt(_overflow > 0f ? _scroll / _overflow * 100f : 0f) + "%";
        }
    }

    private static Transform? Host(InventoryGui gui)
    {
        var canvas = gui.GetComponentInParent<Canvas>();
        if (canvas != null) return canvas.transform;
        return gui.m_inventoryRoot != null ? gui.m_inventoryRoot.parent : gui.transform;
    }

    private static void Park()
    {
        if (_card == null || !_card.activeSelf) return;
        var plate = _card.GetComponent<RectTransform>();
        var parent = plate.parent as RectTransform;
        if (parent == null) return;
        var canvas = _card.GetComponentInParent<Canvas>();
        var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Input.mousePosition, cam, out var local)) return;
        var size = plate.sizeDelta * plate.localScale.x;
        var box = parent.rect;
        var hang = RestlessUi.HeadOverhang * plate.localScale.x;
        var lift = RestlessUi.HeadLift * plate.localScale.x;
        var pos = local + new Vector2(18f, -12f);
        if (pos.x + size.x + hang > box.xMax - 8f) pos.x = local.x - size.x - 18f;
        if (pos.y - size.y < box.yMin + 8f) pos.y = local.y + 12f;
        pos.x = Mathf.Clamp(pos.x, box.xMin + 8f, Mathf.Max(box.xMin + 8f, box.xMax - size.x - 8f - hang));
        pos.y = Mathf.Clamp(pos.y, box.yMin + size.y + 8f, Mathf.Max(box.yMin + size.y + 8f, box.yMax - 8f - lift));
        plate.anchorMin = plate.anchorMax = parent.pivot;
        plate.pivot = new Vector2(0f, 1f);
        plate.anchoredPosition = pos;
    }

    private static float Measure(Text text, float width)
    {
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        return Mathf.Ceil(Mathf.Max(text.fontSize + 3f, text.preferredHeight));
    }

    private static void Place(GameObject go, float x, float y, float width, float height) =>
        RestlessUi.Pin(go, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(width, height));

    private static void Paragraph(Transform parent, string copy, float width, Color tint, int size = CopySize)
    {
        var text = RestlessUi.Label(parent, copy, size, tint, TextAnchor.UpperLeft);
        Hold(text.gameObject, Measure(text, width));
    }

    private static void Divider(Transform parent)
    {
        var row = RestlessUi.Node(parent, "divider");
        Hold(row, 16f);
        RestlessUi.PaperDivider(row);
    }

    private static void Badge(Transform parent, string copy, Color tint, float width)
    {
        var row = RestlessUi.Node(parent, "badge");
        var chip = RestlessUi.Chip(row.transform, "chip");
        RestlessUi.ForgedSurface(chip);
        chip.GetComponent<Image>().raycastTarget = false;
        var text = RestlessUi.Label(chip.transform, copy, RestlessUi.HudMeta, tint, TextAnchor.MiddleLeft);
        var inset = RestlessUi.PlateInset;
        var chipWidth = Mathf.Min(width, Mathf.Max(72f, text.preferredWidth + inset * 2f));
        var height = Measure(text, chipWidth - inset * 2f) + 8f;
        Place(chip, 0f, 0f, chipWidth, height);
        RestlessUi.Stretch(text.gameObject, Vector2.zero, Vector2.one, new Vector2(inset, 4f), new Vector2(-inset, -4f));
        Hold(row, height);
    }

    private static void StatRow(Transform parent, string label, string value, float width, bool supporting = false, int size = CopySize)
    {
        var row = RestlessUi.Node(parent, "stat");
        var labelWidth = (width - 14f) * 0.52f;
        var valueWidth = width - 14f - labelWidth;
        var left = RestlessUi.Label(row.transform, label, size, RestlessUi.PaperMuted, TextAnchor.UpperLeft);
        var right = RestlessUi.Label(row.transform, value, supporting ? RestlessUi.HudMeta : size,
            supporting ? RestlessUi.PaperMuted : RestlessUi.Accent, TextAnchor.UpperRight);
        if (supporting) left.fontSize = RestlessUi.HudMeta;
        var height = Mathf.Max(Measure(left, labelWidth), Measure(right, valueWidth));
        Place(left.gameObject, 0f, 0f, labelWidth, height);
        Place(right.gameObject, labelWidth + 14f, 0f, valueWidth, height);
        Hold(row, height);
    }

    private static float DamageChip(Transform parent, string label, string value, float x, float width)
    {
        var chip = RestlessUi.Chip(parent, "damageChip");
        var tint = DamageTint(label);
        RestlessUi.ForgedSurface(chip);
        chip.GetComponent<Image>().color = Color.Lerp(Color.white, tint, 0.2f);
        chip.GetComponent<Image>().raycastTarget = false;
        var face = RestlessUi.Label(chip.transform, label + " " + value, CopySize,
            tint, TextAnchor.MiddleLeft);
        const float inset = 14f;
        var height = Mathf.Max(38f, Measure(face, width - inset * 2f - 26f) + 16f);
        Place(chip, x, 0f, width, height);
        var glyph = RestlessUi.Picture(chip.transform, "glyph", "glyph-" + DamageKey(label));
        glyph.GetComponent<Image>().color = tint;
        glyph.GetComponent<Image>().raycastTarget = false;
        RestlessUi.Pin(glyph, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(inset, 0f), new Vector2(21f, 24f));
        RestlessUi.Stretch(face.gameObject, Vector2.zero, Vector2.one, new Vector2(inset + 26f, 8f), new Vector2(-inset, -8f));
        return height;
    }

    private static Color DamageTint(string label)
    {
        var words = new[] { "fire", "frost", "poison", "lightning", "spirit" };
        var colours = new[] { RestlessUi.Hex(0xF4A05E), RestlessUi.Hex(0x9FCFDF),
            RestlessUi.Hex(0xB2C982), RestlessUi.Hex(0xC2B0E5), RestlessUi.Hex(0xE9DDB0) };
        for (var i = 0; i < words.Length; i++)
            if (string.Equals(label, words[i], System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(label, Soft("$inventory_" + words[i]), System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(label, Soft("$item_" + words[i]), System.StringComparison.OrdinalIgnoreCase))
                return colours[i];
        return RestlessUi.PaperMuted;
    }

    private static bool SupportingLabel(string label) =>
        label == Phrase(Soft("$item_crafter"))
        || label == Phrase(Soft("$item_repairlevel"))
        || label == Phrase(Soft("$item_repairstationlevel"));

    private static Color? RimTint(List<TooltipContribution> contributions)
    {
        foreach (var contribution in contributions)
            if (contribution.RimTint.HasValue) return contribution.RimTint;
        return null;
    }

    // Crafting consumes the same text parser and measured rows as inspect.
    // Unknown mod lines are retained, and the caller supplies a clipped scroll area.
    internal static void RecipeBody(Transform parent, string raw, float width)
    {
        Parse(raw, "", "", out var stats, out var chips, out var notes);
        if (notes.Count > 0) Paragraph(parent, string.Join("\n", notes), width, RestlessUi.PaperMuted, CopySize + 2);
        if (stats.Count > 0)
        {
            Divider(parent);
            foreach (var stat in stats) StatRow(parent, stat.label, stat.value, width, size: CopySize + 2);
        }
        if (chips.Count > 0)
        {
            Divider(parent);
            foreach (var chip in chips)
            {
                var row = RestlessUi.Node(parent, "damage");
                Hold(row, DamageChip(row.transform, chip.label, chip.value, 0f, width));
            }
        }
    }

    internal static string RecipeCopy(string raw) => Soft(raw);

    private static string Signature(ItemDrop.ItemData item, string raw,
        List<TooltipContribution> contributions, float width, float maxHeight)
    {
        var text = new StringBuilder();
        // Length-prefix fields so arbitrary extension text cannot collide at separators.
        void Add(string value) { text.Append(value.Length).Append(':').Append(value); }
        Add(raw); Add(Soft(item.m_shared.m_name)); Add(Soft(item.m_shared.m_description));
        Add(item.m_quality.ToString()); Add(item.m_stack.ToString()); Add(item.m_durability.ToString("R"));
        Add(SlotLock.Held(item).ToString()); Add(width.ToString("R")); Add(maxHeight.ToString("R"));
        Add(RestlessUi.IconOf(item)?.GetInstanceID().ToString() ?? "");
        foreach (var contribution in contributions)
        {
            Add("contribution"); Add(contribution.RimTint?.ToString() ?? "");
            foreach (var badge in contribution.Badges) { Add("badge"); Add(Soft(badge.Text)); Add(badge.Tint?.ToString() ?? ""); }
            foreach (var section in contribution.Sections)
            {
                Add("section"); Add(Soft(section.Title)); Add(Soft(section.Body));
                foreach (var row in section.Rows) { Add("row"); Add(Soft(row.Label)); Add(Soft(row.Value)); }
            }
        }
        return text.ToString();
    }

    private static void Parse(string raw, string title, string description, out List<(string label, string value)> stats,
        out List<(string label, string value)> chips, out List<string> notes)
    {
        stats = new List<(string, string)>();
        chips = new List<(string, string)>();
        notes = new List<string>();
        if (string.IsNullOrWhiteSpace(raw)) return;
        var seen = new HashSet<string>();
        foreach (var line in description.Replace("\r", "").Split('\n')) seen.Add(line.Trim());
        foreach (var chunk in Soft(raw).Replace("\r", "").Split('\n'))
        {
            var line = chunk.Trim();
            if (line.Length == 0 || line == title || !seen.Add(line)) continue;
            var colon = line.IndexOf(':');
            if (colon > 0 && colon < line.Length - 1)
            {
                var label = Phrase(line.Substring(0, colon).Trim());
                var value = line.Substring(colon + 1).Trim();
                if (IsDamage(label)) chips.Add((label, value));
                else stats.Add((label, value));
            }
            else notes.Add(line);
        }
    }

    private static bool IsDamage(string label)
    {
        // Exact localised labels: "fire resistance" must never become fire damage.
        foreach (var word in DamageWords)
            if (string.Equals(label, word, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(label, Soft("$inventory_" + word), System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(label, Soft("$item_" + word), System.StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static string DamageKey(string label)
    {
        foreach (var word in DamageWords)
            if (string.Equals(label, word, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(label, Soft("$inventory_" + word), System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(label, Soft("$item_" + word), System.StringComparison.OrdinalIgnoreCase)) return word;
        return "blunt";
    }

    private static bool Handedness(string line) => line == "One-handed" || line == "Two-handed"
        || line == Soft("$item_onehanded") || line == Soft("$item_twohanded");

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
                if (name.TrimEnd('/').Trim().Equals("br", System.StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append('\n');
                    afterTag = false;
                    continue;
                }
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
            if (!GoodPhrase(loc) || Compact(loc) != compact)
                continue;
            return loc;
        }

        return label;
    }

    private static bool GoodPhrase(string loc) =>
        loc.Length > 0 && loc.IndexOf('$') < 0;

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

    private static string TypeName(ItemDrop.ItemData.ItemType type)
    {
        var raw = type.ToString();
        switch (raw)
        {
            case "Torch":
                return "Utility";
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


    private static void Hold(GameObject go, float height)
    {
        var hold = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        hold.minHeight = hold.preferredHeight = height;
        hold.flexibleHeight = 0f;
    }

    private static void HideCard()
    {
        if (_card != null) _card.SetActive(false);
        _item = null;
        _signature = "";
        _scroll = 0f;
    }

    private static void DropCard()
    {
        if (_card != null) Object.Destroy(_card);
        _card = _body = _viewport = _footer = null;
        _item = null;
        _signature = "";
        _scroll = 0f;
    }

    private static void TearDown() => DropCard();
}
