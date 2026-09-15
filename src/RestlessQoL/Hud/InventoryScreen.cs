using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Jotunn.Managers;
using RestlessQoL.Core;
using RestlessQoL.PlayerTweaks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

public sealed partial class InventoryScreen : FeatureModule
{
    public override string Id => "ui.inventory";
    public override bool Enabled => true;
    public override bool TickInMenus => true;

    private static readonly HashSet<GameObject> Ours = new();
    private static readonly List<Behaviour> Hidden = new();
    private static readonly List<GameObject> Silenced = new();
    private static readonly Dictionary<Image, Color> Ghosted = new();
    private static readonly Dictionary<TMP_Text, float> TmpAlpha = new();
    private static readonly List<Readout> Readouts = new();
    private static bool _dressed;
    private static bool _dumped;
    private static int _syncStamp;

    private sealed class Readout
    {
        public TMP_Text? Src;
        public Text Face = null!;
        public bool Wrap;
    }

    protected override void OnLoaded()
    {
        GUIManager.OnCustomGUIAvailable += TearDown;
    }

    public override void Tick()
    {
        if (ModConfig.InventoryScreenEnabled.Value || !_dressed)
            return;
        var gui = InventoryGui.instance;
        if (gui != null)
            Undress(gui);
    }

    internal static void KeepQuiet()
    {
        foreach (var go in Silenced)
        {
            if (go == null)
                continue;
            var tmp = go.GetComponent<TMP_Text>();
            if (tmp != null)
                SilenceTmp(tmp);
        }

        foreach (var behaviour in Hidden)
        {
            if (behaviour == null)
                continue;
            behaviour.enabled = false;
            if (behaviour is GuiBar)
                behaviour.gameObject.SetActive(false);
        }

        foreach (var pair in Ghosted)
        {
            if (pair.Key == null)
                continue;
            pair.Key.color = Color.clear;
            pair.Key.raycastTarget = true;
            pair.Key.enabled = true;
        }
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
        private static void AfterShow(InventoryGui __instance)
        {
            _syncStamp = int.MinValue;
            DumpOnce(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
        private static void AfterUpdate(InventoryGui __instance)
        {
            RestlessUi.HudTuck.Tick();
            RestlessUi.HudTuck.Map();

            if (!ModConfig.InventoryScreenEnabled.Value)
            {
                if (_dressed)
                    Undress(__instance);
                return;
            }

            if (__instance.m_inventoryRoot == null || !__instance.m_inventoryRoot.gameObject.activeInHierarchy)
                return;

            if (!_dressed)
                Dress(__instance);
            if (NeedsSync(__instance))
                Sync(__instance);
            KeepQuiet();
            RefreshCraftFeedback(__instance);
        }
    }

    // Live names/sizes before plates. Do not invent a second tree.
    private static void DumpOnce(InventoryGui gui)
    {
        if (_dumped || gui.m_inventoryRoot == null)
            return;
        _dumped = true;

        var root = gui.m_inventoryRoot;
        var sb = new StringBuilder();
        sb.AppendLine("ui.inventory dump");
        InventoryElement? sample = null;
        foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            var slot = graphic.GetComponentInParent<InventoryElement>();
            if (slot != null)
            {
                sample ??= slot;
                if (slot != sample)
                    continue;
            }

            var rt = graphic.rectTransform;
            sb.Append("  ").Append(graphic.GetType().Name).Append(' ')
                .Append(Trail(graphic.transform, root))
                .Append(' ').Append(rt.rect.width.ToString("0"))
                .Append('x').Append(rt.rect.height.ToString("0"))
                .AppendLine();
        }

        Plugin.Log.LogInfo(sb.ToString());
    }

    private static string Trail(Transform node, Transform root)
    {
        var parts = new List<string>();
        for (var t = node; t != null; t = t.parent)
        {
            parts.Add(t.name);
            if (t == root)
                break;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    private static void Dress(InventoryGui gui)
    {
        QuietChrome(gui);
        DropWell(gui.m_crafting);
        HideTitle(gui.m_info);
        HideTitle(gui.m_crafting);
        DropNamed(gui.m_info, "RestlessPlate");
        DropNamed(ListHost(gui), "RestlessPlate");
        DropNamed(RestlessUi.Deep(gui.m_crafting, "Decription"), "RestlessPlate");
        DressIcons(gui);
        DressHeader(gui);
        DressRecipeChrome(gui);
        _dressed = true;
    }

    private static void Sync(InventoryGui gui)
    {
        KeepQuiet();
        HideTitle(gui.m_info);
        HideTitle(gui.m_crafting);
        DropNamed(gui.m_info, "RestlessPlate");
        DropNamed(ListHost(gui), "RestlessPlate");
        DropNamed(RestlessUi.Deep(gui.m_crafting, "Decription"), "RestlessPlate");
        DressIcons(gui);
        DressGrid(gui.m_playerGrid);
        DressGrid(gui.ContainerGrid);
        DressButton(gui.m_tabCraft, "Craft", gui.InCraftTab());
        DressButton(gui.m_tabUpgrade, "Upgrade", gui.InUpradeTab());
        DressHeader(gui);
        DressRecipeChrome(gui);
        DressButton(gui.m_takeAllButton, "Take all", false);
        DressButton(gui.m_stackAllButton, "Stack", false);
        DressButton(gui.m_craftButton, "Craft", false);
        DressCraftExtras(gui);
        HideRepairChrome(gui);
        if (AutoRepair.HidesButton)
            AutoRepair.HideChrome(gui);
        else
            DressIconButton(gui.m_repairButton, gui.HaveRepairableItems());
        StripDrop(gui.m_dropButton);
        DressRecipes(gui);
        DressRequirements(gui);
        DressStat(gui.m_armor, "armor", "armor_icon", ExtraSlots.HoldsPlayerStats);
        DressStat(gui.m_weight, "weight", "weight_icon", ExtraSlots.HoldsPlayerStats);
        DressStat(gui.m_containerWeight, "containerWeight", "weight_icon");
        DressChestName(gui);
        var leftover = RestlessUi.Deep(gui.m_crafting, "OLD_QualityPanel");
        if (leftover != null)
            leftover.gameObject.SetActive(false);
        if (ModalOpen(gui))
            QuietOverlays(gui);
        DressOverlays(gui);
        var topic = gui.m_crafting != null
            ? RestlessUi.Deep<TMP_Text>(gui.m_crafting, "topic")
            : null;
        var station = topic != null ? topic : gui.m_craftingStationName;
        Face(station, "station", RestlessUi.Text, RestlessUi.TitleSize);
        if (gui.m_craftingStationName != null && gui.m_craftingStationName != station)
            SilenceTmp(gui.m_craftingStationName);
        var extra = gui.m_crafting != null ? RestlessUi.Deep(gui.m_crafting, "Restless_stationTopic") : null;
        if (extra != null)
            extra.gameObject.SetActive(false);
        Face(gui.m_craftingStationLevel, "stationLevel", RestlessUi.Accent, RestlessUi.HudMeta);
        Face(gui.m_recipeName, "recipeName", RestlessUi.Text, RestlessUi.BodySize);
        Face(gui.m_recipeDecription, "recipeBody", RestlessUi.Text, RestlessUi.MetaSize, true);
        Face(gui.m_itemCraftType, "craftType", RestlessUi.Muted, RestlessUi.HudMeta);
        Face(gui.m_minStationLevelText, "minStation", RestlessUi.Muted, RestlessUi.HudMeta);
        Face(gui.m_upgradeItemName, "upgradeName", RestlessUi.Text, RestlessUi.HudSize);
        Face(gui.m_upgradeItemQuality, "upgradeQuality", RestlessUi.Accent, RestlessUi.HudMeta);
        foreach (var row in Readouts)
            PaintReadout(row);
        ParkStationTitle(gui);
        DressCraftMaterials(gui);
    }

    // Vanilla writes TMP / GuiBar / selection back every Update. Re-dress only
    // when a stack, wear, lock, tab, recipe, or readout actually changed.
    private static bool NeedsSync(InventoryGui gui)
    {
        var stamp = Stamp(gui);
        if (stamp == _syncStamp)
            return false;
        _syncStamp = stamp;
        return true;
    }

    private static int Stamp(InventoryGui gui)
    {
        unchecked
        {
            var h = 17;
            h = h * 31 + (gui.InCraftTab() ? 1 : 0);
            h = h * 31 + (gui.InUpradeTab() ? 1 : 0);
            h = h * 31 + gui.GetSelectedRecipeIndex(false);
            h = h * 31 + (ModalOpen(gui) ? 1 : 0);
            h = h * 31 + (gui.HaveRepairableItems() ? 1 : 0);
            h = MixGrid(h, gui.m_playerGrid);
            h = MixGrid(h, gui.ContainerGrid);
            h = MixTmp(h, gui.m_armor);
            h = MixTmp(h, gui.m_weight);
            h = MixTmp(h, gui.m_containerWeight);
            h = MixTmp(h, gui.m_containerName);
            h = MixTmp(h, gui.m_recipeName);
            h = MixTmp(h, gui.m_recipeDecription);
            h = MixTmp(h, gui.m_itemCraftType);
            h = MixTmp(h, gui.m_craftingStationName);
            h = MixTmp(h, gui.m_minStationLevelText);
            h = MixTmp(h, gui.m_upgradeItemName);
            h = MixTmp(h, gui.m_upgradeItemQuality);
            if (gui.m_recipeRequirementList != null)
                foreach (var requirement in gui.m_recipeRequirementList)
                {
                    if (requirement == null) continue;
                    h = h * 31 + (requirement.activeSelf ? 1 : 0);
                    h = MixTmp(h, RestlessUi.Deep<TMP_Text>(requirement.transform, "res_amount"));
                    h = MixTmp(h, RestlessUi.Deep<TMP_Text>(requirement.transform, "res_name"));
                }
            h = MixOverlay(h, gui);
            var recipes = gui.m_recipeListRoot;
            if (recipes != null)
            {
                h = h * 31 + recipes.childCount;
                var live = 0;
                for (var i = 0; i < recipes.childCount; i++)
                {
                    if (recipes.GetChild(i).gameObject.activeSelf)
                        live++;
                }

                h = h * 31 + live;
            }

            h = MixItem(h, gui.m_dragItem);
            return h;
        }
    }

    private static int MixGrid(int h, InventoryGrid? grid)
    {
        if (grid == null)
            return h * 31;
        var inv = grid.GetInventory();
        var elements = Elements(grid);
        if (elements != null)
        {
            h = h * 31 + elements.Count;
            foreach (var element in elements)
            {
                if (element == null || ExtraSlots.IsExtraCell(element.Position))
                    continue;
                var item = inv?.GetItemAt(element.Position.x, element.Position.y);
                h = MixItem(h, item);
                h = h * 31 + (SlotLock.Held(element.Position) ? 1 : 0);
                var lit = item is { m_equipped: true }
                    || element.m_equiped != null && element.m_equiped.gameObject.activeSelf
                    || element.m_selected != null && element.m_selected.activeSelf;
                h = h * 31 + (lit ? 1 : 0);
            }

            return h;
        }

        if (inv == null)
            return h * 31;
        var w = inv.GetWidth();
        var height = inv.GetHeight();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var pos = new Vector2i(x, y);
                if (ExtraSlots.IsExtraCell(pos))
                    continue;
                h = MixItem(h, inv.GetItemAt(x, y));
                h = h * 31 + (SlotLock.Held(pos) ? 1 : 0);
            }
        }

        return h;
    }

    private static int MixItem(int h, ItemDrop.ItemData? item)
    {
        if (item == null)
            return h * 31;
        h = h * 31 + item.m_stack;
        h = h * 31 + item.m_quality;
        h = h * 31 + (item.m_shared != null ? item.m_shared.m_name.GetHashCode() : 0);
        h = h * 31 + (int)(item.GetDurabilityPercentage() * 20f);
        h = h * 31 + (item.m_equipped ? 1 : 0);
        return h;
    }

    private static int MixTmp(int h, TMP_Text? tmp)
    {
        if (tmp == null || !tmp.gameObject.activeInHierarchy)
            return h * 31;
        return h * 31 + tmp.text.GetHashCode();
    }

    private static List<InventoryElement>? Elements(InventoryGrid? grid)
    {
        if (grid == null)
            return null;
        try
        {
            return Traverse.Create(grid).Field("m_elements").GetValue<List<InventoryElement>>();
        }
        catch
        {
            return null;
        }
    }

    private static void DressGrid(InventoryGrid? grid)
    {
        if (grid == null)
            return;
        var inv = grid.GetInventory();
        var elements = Elements(grid);
        if (elements == null)
        {
            foreach (var element in grid.GetComponentsInChildren<InventoryElement>(true))
                PaintCell(inv, element);
            return;
        }

        foreach (var element in elements)
            PaintCell(inv, element);
    }

    private static void PaintCell(Inventory? inv, InventoryElement? element)
    {
        if (element == null || ExtraSlots.IsExtraCell(element.Position))
            return;
        try
        {
            var item = inv?.GetItemAt(element.Position.x, element.Position.y);
            var lit = item is { m_equipped: true }
                || element.m_equiped != null && element.m_equiped.gameObject.activeSelf
                || element.m_selected != null && element.m_selected.activeSelf;
            Ours.Add(RestlessUi.DressSlot(element.gameObject, element.m_icon, lit, item, Hidden, true,
                SlotLock.Held(element.Position)));
        }
        catch (System.Exception e)
        {
            Plugin.Log.LogWarning("ui.inventory slot: " + e.Message);
        }
    }

    private static void DressIcons(InventoryGui gui)
    {
        if (gui.m_info == null)
            return;
        if (gui.m_info.Find("RestlessIcons") != null)
            return;
        var parts = new List<RectTransform>();
        foreach (var name in new[] { "Texts", "Skills", "Trophies", "Achievements", "PVP" })
        {
            var node = gui.m_info.Find(name) ?? RestlessUi.Deep(gui.m_info, name);
            if (node == null || !node.gameObject.activeInHierarchy)
                continue;
            foreach (var staleName in new[] { "RestlessChip", "RestlessSlot" })
            {
                var stale = node.Find(staleName);
                if (stale == null)
                    continue;
                Ours.Remove(stale.gameObject);
                Object.Destroy(stale.gameObject);
            }

            var icon = RestlessUi.Deep<Image>(node, "Icon")
                ?? RestlessUi.Deep<Image>(node, "Image")
                ?? RestlessUi.Deep<Image>(node, "Checked");
            foreach (var image in node.GetComponentsInChildren<Image>(true))
            {
                if (image == icon || image.transform.name.StartsWith("Restless"))
                    continue;
                var n = image.gameObject.name.ToLowerInvariant();
                if (n is "bkg" or "background" || n.Contains("selected") || n.Contains("border"))
                {
                    Hide(image);
                    image.gameObject.SetActive(false);
                }
            }

            var wood = node.GetComponent<Image>();
            if (wood != null && wood != icon)
                Ghost(wood);
            foreach (var tmp in node.GetComponentsInChildren<TMP_Text>(true))
                SilenceTmp(tmp);
            AddRt(parts, node);
        }

        StripCover(gui.m_info, "RestlessIcons", parts, 10f, 8f);
    }

    // Header is one tall Strip over the two columns. Title sits at the top;
    // Craft / Upgrade sit inside the bottom with air.
    private static void DressHeader(InventoryGui gui)
    {
        if (gui.m_crafting == null)
            return;
        var craft = gui.m_crafting;
        DropNamed(craft, "RestlessCraftTray");
        DropNamed(craft, "RestlessCraftHead");
        var list = ListHost(gui);
        var desc = RestlessUi.Deep(craft, "Decription");
        if (!WorldBox(list, out var lx0, out _, out var lx1, out var ly1)
            && !WorldBox(gui.m_recipeListRoot, out lx0, out _, out lx1, out ly1))
            return;
        if (!WorldBox(desc, out var dx0, out _, out var dx1, out var dy1))
        {
            dx0 = lx1;
            dx1 = lx1;
            dy1 = ly1;
        }

        var left = Mathf.Min(lx0, dx0);
        var right = Mathf.Max(lx1, dx1);
        var topic = RestlessUi.Deep(craft, "topic");
        if (!WorldBox(topic, out _, out var titleBot, out _, out var titleTop)
            && gui.m_craftingStationName != null
            && !WorldBox(gui.m_craftingStationName.rectTransform, out _, out titleBot, out _,
                out titleTop))
        {
            titleTop = Mathf.Max(ly1, dy1) + 30f;
            titleBot = titleTop - 30f;
        }

        const float topPad = 24f;
        const float mid = 12f;
        const float tabBand = 36f;
        const float botPad = 8f;
        var headerTop = titleTop + topPad;
        var headerBot = titleBot - mid - tabBand - botPad;
        var header = EnsureStrip(craft, "RestlessHeader");
        Place(header.GetComponent<RectTransform>(), left, headerBot, right, headerTop, 0f, 0f);
        ParkCraftTabs(gui, header.GetComponent<RectTransform>());
    }

    // Craft / Upgrade sit on the header's bottom edge, inset, not on the lip.
    private static void ParkCraftTabs(InventoryGui gui, RectTransform header)
    {
        if (!WorldBox(header, out _, out var hy0, out _, out _))
            return;
        var tabs = new List<RectTransform>();
        if (gui.m_tabCraft != null)
            AddRt(tabs, gui.m_tabCraft.GetComponent<RectTransform>());
        if (gui.m_tabUpgrade != null)
            AddRt(tabs, gui.m_tabUpgrade.GetComponent<RectTransform>());
        if (!Union(tabs, out _, out var ty0, out _, out var ty1))
            return;
        var want = hy0 + 6f + (ty1 - ty0) * 0.5f;
        var have = (ty0 + ty1) * 0.5f;
        var dy = want - have;
        if (Mathf.Abs(dy) < 0.5f)
            return;
        foreach (var tab in tabs)
        {
            if (!CraftTabPositions.ContainsKey(tab)) CraftTabPositions.Add(tab, tab.anchoredPosition3D);
            tab.position = new Vector3(tab.position.x, tab.position.y + dy, tab.position.z);
        }
    }

    private static void DressRecipeChrome(InventoryGui gui)
    {
        var desc = RestlessUi.Deep(gui.m_crafting, "Decription");
        if (desc == null)
            return;
        DropNamed(desc, "RestlessRecipeFoot");
        DropNamed(desc, "RestlessRecipeTitle");

        var parts = new List<RectTransform>();
        if (gui.m_recipeIcon != null)
            AddRt(parts, gui.m_recipeIcon.rectTransform);
        if (gui.m_recipeName != null)
            AddRt(parts, gui.m_recipeName.rectTransform);
        if (gui.m_recipeDecription != null)
            AddRt(parts, gui.m_recipeDecription.rectTransform);
        if (gui.m_itemCraftType != null)
            AddRt(parts, gui.m_itemCraftType.rectTransform);
        if (!Union(parts, out var minX, out var minY, out var maxX, out var maxY))
            return;
        if (WorldBox(desc, out var dx0, out _, out var dx1, out _))
        {
            minX = dx0;
            maxX = dx1;
        }

        var gap = RestlessUi.RowGap;
        var list = ListHost(gui);
        if (WorldBox(list, out _, out _, out var listRight, out _)
            || WorldBox(gui.m_recipeListRoot, out _, out _, out listRight, out _))
            minX = Mathf.Max(minX, listRight + gap);
        var header = gui.m_crafting != null ? gui.m_crafting.Find("RestlessHeader") : null;
        if (WorldBox(header, out _, out var headerBot, out _, out _))
            maxY = Mathf.Min(maxY, headerBot - gap);

        minY -= gap;
        TallBox(desc, "RestlessRecipe", minX, minY, maxX, maxY, 0f, 0f);

        var level = desc.Find("requirements")?.Find("level");
        if (level != null)
        {
            var copy = RestlessUi.Bare(gui.m_minStationLevelText != null ? gui.m_minStationLevelText.text : "");
            if (string.IsNullOrEmpty(copy))
                RestlessUi.Quiet(level.gameObject);
            else
                RestlessUi.Loud(level.gameObject);
        }
    }

    private static void ParkStationTitle(InventoryGui gui)
    {
        var header = gui.m_crafting != null ? gui.m_crafting.Find("RestlessHeader") : null;
        var node = gui.m_crafting != null ? RestlessUi.Deep(gui.m_crafting, "Restless_station") : null;
        if (header == null || node == null)
            return;
        var face = node.GetComponent<Text>();
        if (face == null)
            return;
        var from = header.GetComponent<RectTransform>();
        var rt = face.GetComponent<RectTransform>();
        RestlessUi.CopyRect(rt, from);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -12f);
        rt.sizeDelta = new Vector2(Mathf.Max(from.rect.width - 24f, 80f), 52f);
        face.alignment = TextAnchor.UpperCenter;
        face.fontSize = RestlessUi.TitleSize;
        face.font = RestlessUi.Face(true);
        face.horizontalOverflow = HorizontalWrapMode.Overflow;
        face.verticalOverflow = VerticalWrapMode.Overflow;
        node.SetAsLastSibling();
    }

    private static void DressChestName(InventoryGui gui)
    {
        var src = gui.m_containerName;
        if (src == null || !src.gameObject.activeInHierarchy || src.transform.parent == null)
            return;
        var host = src.transform.parent;
        if (!WorldBox(src.rectTransform, out var minX, out var minY, out var maxX, out var maxY))
            return;
        minX -= 18f;
        maxX += 18f;
        minY -= 12f;
        maxY += 12f;
        var gap = RestlessUi.RowGap;
        if (WorldBox(gui.m_takeAllButton != null ? gui.m_takeAllButton.transform : null,
                out _, out _, out var takeRight, out _))
            minX = Mathf.Max(minX, takeRight + gap);
        if (WorldBox(gui.m_stackAllButton != null ? gui.m_stackAllButton.transform : null,
                out var stackLeft, out _, out _, out _))
            maxX = Mathf.Min(maxX, stackLeft - gap);
        StripBox(host, "RestlessChestName", minX, minY, maxX, maxY, 0f, 0f);
        Face(src, "containerName", RestlessUi.Text, RestlessUi.BodySize);
    }

    // Chip + vanilla icon + Averia. Same language as Take all / Craft tabs.
    // Not HudMeter (that's the 18px hotbar track) and not HudRow (notices / buffs).
    private static void DressStat(TMP_Text? src, string tag, string iconName, bool extraHolds = false)
    {
        if (src == null || src.transform.parent == null || !src.gameObject.activeInHierarchy)
            return;
        var host = src.transform.parent;
        var icon = RestlessUi.Deep<Image>(host, iconName);
        var parts = new List<RectTransform>();
        AddRt(parts, src.rectTransform);
        if (icon != null)
            AddRt(parts, icon.rectTransform);
        if (parts.Count == 0)
            return;
        var plate = host.Find("RestlessStat");
        if (plate == null)
        {
            var go = RestlessUi.Chip(host, "RestlessStat");
            Ours.Add(go);
            plate = go.transform;
        }

        plate.SetAsFirstSibling();
        if (!extraHolds)
            Cover(plate.GetComponent<RectTransform>(), parts, 12f, 8f);
        var rootImg = host.GetComponent<Image>();
        if (rootImg != null)
            Hide(rootImg);
        var bkg = RestlessUi.Deep<Image>(host, "bkg") ?? RestlessUi.Deep<Image>(host, "background");
        if (bkg != null && bkg != icon)
            Hide(bkg);
        Face(src, tag, RestlessUi.Text, RestlessUi.HudSize);
    }

    private static bool ModalOpen(InventoryGui gui) =>
        gui.m_textsDialog != null && gui.m_textsDialog.gameObject.activeInHierarchy
        || gui.m_skillsDialog != null && gui.m_skillsDialog.gameObject.activeInHierarchy
        || gui.m_trophiesPanel != null && gui.m_trophiesPanel.activeInHierarchy
        || gui.m_achievementsPanel != null && gui.m_achievementsPanel.gameObject.activeInHierarchy;

    private static void DressOverlays(InventoryGui gui)
    {
        DressCompendium(gui.m_textsDialog);
        DressSkills(gui.m_skillsDialog);
        DressTrophies(gui);
        DressAchievements(gui);
    }

    private static void DressCompendium(TextsDialog? dialog)
    {
        if (dialog == null || !dialog.gameObject.activeInHierarchy)
            return;
        DressModalShell(dialog.transform, "textsTopic");
        DressListChildren(dialog.m_listRoot, dialog.m_elementPrefab, false, row =>
        {
            var sel = row.Find("selected");
            return sel != null && sel.gameObject.activeSelf;
        });

        var area = RestlessUi.Deep(dialog.transform, "TextArea");
        if (area == null)
            return;
        var parts = new List<RectTransform>();
        if (dialog.m_textAreaTopic != null)
            AddRt(parts, dialog.m_textAreaTopic.rectTransform);
        if (dialog.m_textArea != null)
            AddRt(parts, dialog.m_textArea.rectTransform);
        if (!Union(parts, out var minX, out var minY, out var maxX, out var maxY))
            return;
        if (WorldBox(area, out var ax0, out _, out var ax1, out _))
        {
            minX = ax0;
            maxX = ax1;
        }

        var list = RestlessUi.Deep(dialog.transform, "TextList");
        if (WorldBox(list, out _, out _, out var listRight, out _))
            minX = Mathf.Max(minX, listRight + RestlessUi.RowGap);
        TallBox(area, "RestlessRead", minX, minY, maxX, maxY, 0f, 0f);
        Face(dialog.m_textAreaTopic, "textsName", RestlessUi.Text, RestlessUi.BodySize);
        Face(dialog.m_textArea, "textsBody", RestlessUi.Text, RestlessUi.MetaSize, true);
    }

    private static void DressSkills(SkillsDialog? dialog)
    {
        if (dialog == null || !dialog.gameObject.activeInHierarchy)
            return;
        DressModalShell(dialog.transform, "skillsTopic");
        var total = dialog.m_totalSkillText;
        var label = RestlessUi.Deep<TMP_Text>(dialog.transform, "totalskills_topic");
        var host = total != null ? total.transform.parent : label != null ? label.transform.parent : null;
        if (host != null)
        {
            var parts = new List<RectTransform>();
            if (label != null)
                AddRt(parts, label.rectTransform);
            if (total != null)
                AddRt(parts, total.rectTransform);
            StripCover(host, "RestlessTotals", parts, 12f, 8f);
        }

        Face(label, "skillsTotalLabel", RestlessUi.Muted, RestlessUi.HudMeta);
        Face(total, "skillsTotal", RestlessUi.Text, RestlessUi.HudSize);
        DressListChildren(dialog.m_listRoot, dialog.m_elementPrefab, true, _ => false);
        if (dialog.m_listRoot == null)
            return;
        foreach (var row in dialog.m_listRoot.GetComponentsInChildren<Transform>(true))
        {
            if (!IsListRow(row, dialog.m_listRoot, dialog.m_elementPrefab))
                continue;
            foreach (var tmp in row.GetComponentsInChildren<TMP_Text>(true))
            {
                var n = tmp.gameObject.name.ToLowerInvariant();
                if (n.Contains("level"))
                    Face(tmp, "skillLevel", RestlessUi.Text, RestlessUi.HudMeta);
                else if (n.Contains("bonus"))
                    Face(tmp, "skillBonus", RestlessUi.Accent, RestlessUi.HudMeta);
            }
        }
    }

    private static void DressTrophies(InventoryGui gui)
    {
        if (gui.m_trophiesPanel == null || !gui.m_trophiesPanel.activeInHierarchy)
            return;
        DressModalShell(gui.m_trophiesPanel.transform, "trophiesTopic");
        DressOverlayCards(gui.m_trophieListRoot, "TrophyElement");
    }

    private static void DressAchievements(InventoryGui gui)
    {
        var panel = gui.m_achievementsPanel;
        if (panel == null || !panel.gameObject.activeInHierarchy)
            return;
        DressModalShell(panel.transform, "achTopic");
        Face(gui.m_achievementsCompletionRateText, "achRate", RestlessUi.Muted, RestlessUi.HudSize);
        Face(gui.m_achievementsCheatedText, "achCheat", RestlessUi.HealthTint, RestlessUi.HudMeta);
        DressOverlayCards(gui.m_achievementsListRoot, "AchElement");
        var details = panel.m_achievementDetails;
        if (details == null || !details.activeInHierarchy)
            return;
        DressModalShell(details.transform, "achDetailsTopic");
        if (panel.m_achievementDetailsListRoot == null)
            return;
        foreach (var row in panel.m_achievementDetailsListRoot.GetComponentsInChildren<Transform>(true))
        {
            if (!row.name.StartsWith("UnlockConditionElement") || !row.gameObject.activeInHierarchy)
                continue;
            DressListRow(row, false, false);
            var progress = RestlessUi.Deep<TMP_Text>(row, "progress");
            Face(progress, "achProgress", RestlessUi.Accent, RestlessUi.HudMeta);
        }
    }

    private static void QuietOverlays(InventoryGui gui)
    {
        if (gui.m_textsDialog != null && gui.m_textsDialog.gameObject.activeInHierarchy)
            QuietOverlay(gui.m_textsDialog.transform);
        if (gui.m_skillsDialog != null && gui.m_skillsDialog.gameObject.activeInHierarchy)
            QuietOverlay(gui.m_skillsDialog.transform, true);
        if (gui.m_trophiesPanel != null && gui.m_trophiesPanel.activeInHierarchy)
            QuietOverlay(gui.m_trophiesPanel.transform);
        var ach = gui.m_achievementsPanel;
        if (ach != null && ach.gameObject.activeInHierarchy)
        {
            QuietOverlay(ach.transform);
            if (ach.m_achievementDetails != null && ach.m_achievementDetails.activeInHierarchy)
                QuietOverlay(ach.m_achievementDetails.transform);
        }
    }

    private static void QuietOverlay(Transform root, bool keepBars = false)
    {
        HideTitle(root);
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.transform.name.StartsWith("Restless"))
                continue;
            var n = image.gameObject.name.ToLowerInvariant();
            if (n.Contains("icon"))
                continue;
            if (keepBars && (n is "bar" || n.Contains("levelbar") || n.Contains("currentlevel")))
                continue;
            if (image.GetComponent<Mask>() != null)
            {
                Ghost(image);
                continue;
            }

            if (image.GetComponentInParent<Scrollbar>() != null)
            {
                if (n.Contains("handle"))
                    continue;
                Hide(image);
                continue;
            }

            var button = image.GetComponent<Button>();
            if (button != null)
            {
                var rt = image.rectTransform;
                if (rt.rect.width > 800f || rt.rect.height > 800f)
                    Ghost(image);
                continue;
            }

            if (n is "darken" || n is "blur" || n.Contains("border") || n.Contains("selected")
                || IsChromeName(n) || n.Contains("frame") || n.Contains("wood") || n.Contains("well"))
            {
                Hide(image);
                continue;
            }

            var size = image.rectTransform.rect;
            if (size.width > 500f && size.height > 280f)
                Hide(image);
        }
    }

    private static void DressListChildren(RectTransform? root, GameObject? prefab, bool keepBars,
        System.Func<Transform, bool> lit)
    {
        if (root == null)
            return;
        foreach (var row in root.GetComponentsInChildren<Transform>(true))
        {
            if (!IsListRow(row, root, prefab))
                continue;
            DressListRow(row, lit(row), keepBars);
        }
    }

    private static bool IsListRow(Transform row, RectTransform root, GameObject? prefab)
    {
        if (!row.gameObject.activeInHierarchy || row.name.StartsWith("Restless"))
            return false;
        if (prefab != null)
        {
            var prefix = prefab.name.Replace("(Clone)", "");
            return row.name.StartsWith(prefix);
        }

        return row.parent == root;
    }

    private static int MixOverlay(int h, InventoryGui gui)
    {
        if (gui.m_textsDialog != null && gui.m_textsDialog.gameObject.activeInHierarchy)
        {
            h = MixTmp(h, gui.m_textsDialog.m_textAreaTopic);
            h = MixTmp(h, gui.m_textsDialog.m_textArea);
            h = h * 31 + 3;
        }

        if (gui.m_skillsDialog != null && gui.m_skillsDialog.gameObject.activeInHierarchy)
            h = h * 31 + 5;
        if (gui.m_trophiesPanel != null && gui.m_trophiesPanel.activeInHierarchy)
            h = h * 31 + 7;
        var ach = gui.m_achievementsPanel;
        if (ach != null && ach.gameObject.activeInHierarchy)
        {
            h = MixTmp(h, gui.m_achievementsCompletionRateText);
            h = h * 31 + (ach.m_achievementDetails != null && ach.m_achievementDetails.activeInHierarchy ? 11 : 13);
        }

        return h;
    }

    private static void DressModalShell(Transform root, string topicTag)
    {
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            if (button.name.IndexOf("close", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            DressButton(button, "Close", false);
        }

        TMP_Text? topic = null;
        foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp.name != "topic" || !tmp.gameObject.activeInHierarchy)
                continue;
            topic = tmp;
            break;
        }

        if (topic == null)
            return;
        var host = topic.transform.parent;
        if (host == null)
            return;
        var parts = new List<RectTransform>();
        AddRt(parts, topic.rectTransform);
        StripCover(host, "RestlessModalTitle", parts, 18f, 12f);
        Face(topic, topicTag, RestlessUi.Text, RestlessUi.TitleSize);
    }

    private static void DressOverlayCards(RectTransform? root, string prefix)
    {
        if (root == null)
            return;
        foreach (var node in root.GetComponentsInChildren<Transform>(true))
        {
            if (!node.name.StartsWith(prefix) || !node.gameObject.activeInHierarchy)
                continue;
            DressOverlayCard(node);
        }
    }

    private static void DressOverlayCard(Transform cell)
    {
        foreach (var image in cell.GetComponentsInChildren<Image>(true))
        {
            if (image.transform.name.StartsWith("Restless"))
                continue;
            var n = image.gameObject.name.ToLowerInvariant();
            if (n.Contains("icon"))
                continue;
            if (n is "bkg" or "selected" || n.Contains("selected"))
                Hide(image);
        }

        var icon = RestlessUi.Deep<Image>(cell, "icon");
        var host = icon != null && icon.transform.parent != null ? icon.transform.parent.gameObject : cell.gameObject;
        Ours.Add(RestlessUi.DressSlot(host, icon, false, null, Hidden, true));
        foreach (var tmp in cell.GetComponentsInChildren<TMP_Text>(true))
        {
            var n = tmp.gameObject.name.ToLowerInvariant();
            if (n is "name")
                Face(tmp, "cardName", RestlessUi.Text, RestlessUi.HudSize);
            else if (n.Contains("desc"))
                Face(tmp, "cardBody", RestlessUi.Text, RestlessUi.HudMeta, true);
            else
                SilenceTmp(tmp);
        }
    }

    private static void DressListRow(Transform row, bool lit, bool keepBars)
    {
        foreach (var image in row.GetComponents<Image>())
            Ghost(image);
        foreach (var image in row.GetComponentsInChildren<Image>(true))
        {
            if (image.transform.name.StartsWith("Restless") || image.transform == row)
                continue;
            var n = image.gameObject.name.ToLowerInvariant();
            if (n.Contains("icon"))
                continue;
            if (keepBars && (n is "bar" || n.Contains("levelbar") || n.Contains("currentlevel")))
                continue;
            if (n is "bkg")
            {
                Ghost(image);
                continue;
            }

            if (n is "selected" || n.Contains("durab"))
                Hide(image);
        }

        var title = "";
        foreach (var tmp in row.GetComponentsInChildren<TMP_Text>(true))
        {
            var n = tmp.gameObject.name.ToLowerInvariant();
            if (n is "name" || string.IsNullOrEmpty(title) && !n.Contains("level") && !n.Contains("bonus")
                && !n.Contains("progress") && !n.Contains("desc"))
                title = RestlessUi.Bare(tmp.text);
            if (n is "name" || n.Contains("desc"))
                SilenceTmp(tmp);
        }

        var plate = row.Find("RestlessSlot");
        if (plate == null)
        {
            var go = RestlessUi.Strip(row, "RestlessSlot");
            Ours.Add(go);
            plate = go.transform;
        }

        plate.SetAsFirstSibling();
        RestlessUi.Stretch(plate.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        plate.GetComponent<Image>().raycastTarget = true;
        RestlessUi.PaintSlot(plate.gameObject, lit);
        var label = plate.Find("label")?.GetComponent<Text>();
        if (label == null)
        {
            label = RestlessUi.Label(plate, title, RestlessUi.HudSize, RestlessUi.Text, TextAnchor.MiddleLeft);
            RestlessUi.Stretch(label.gameObject, Vector2.zero, Vector2.one, new Vector2(40f, 1f),
                new Vector2(keepBars ? -172f : -28f, -1f));
        }

        label.text = title;
        label.color = lit ? RestlessUi.Accent : RestlessUi.Text;
    }

    private static void DressCraftExtras(InventoryGui gui)
    {
        var leftover = RestlessUi.Deep(gui.m_crafting, "OLD_QualityPanel");
        if (leftover != null)
            leftover.gameObject.SetActive(false);
        var upgrade = RestlessUi.Deep(gui.m_crafting, "UpgradePanel");
        DressQualityStep(upgrade, "LevelDown", "–");
        DressQualityStep(upgrade, "LevelUp", "+");
        var cancel = gui.m_crafting != null
            ? RestlessUi.Deep<Button>(gui.m_crafting, "CraftCancelButton")
            : null;
        DressButton(cancel, "Cancel", false);
    }

    private static void DressQualityStep(Transform? root, string name, string face)
    {
        if (root == null)
            return;
        var node = root.Find(name) ?? RestlessUi.Deep(root, name);
        var button = node != null ? node.GetComponent<Button>() : null;
        if (button == null)
            return;
        DressButton(button, face, false, new Vector2(40f, 32f));
    }

    private static void StripCover(Transform host, string name, List<RectTransform> parts, float padX,
        float padY)
    {
        if (!Union(parts, out var minX, out var minY, out var maxX, out var maxY))
            return;
        var plate = host.Find(name);
        if (plate == null)
        {
            var go = RestlessUi.Strip(host, name, RestlessUi.RowTint, true);
            Ours.Add(go);
            plate = go.transform;
        }

        plate.SetAsFirstSibling();
        Place(plate.GetComponent<RectTransform>(), minX, minY, maxX, maxY, padX, padY);
    }

    private static void StripBox(Transform host, string name, float minX, float minY, float maxX, float maxY,
        float padX, float padY)
    {
        var plate = host.Find(name);
        if (plate == null)
        {
            var go = RestlessUi.Strip(host, name, RestlessUi.RowTint, true);
            Ours.Add(go);
            plate = go.transform;
        }

        plate.SetAsFirstSibling();
        Place(plate.GetComponent<RectTransform>(), minX, minY, maxX, maxY, padX, padY);
    }

    // One tall Strip. Drops a leftover Card (fill + lip bars) if the last pass left one.
    private static void TallBox(Transform host, string name, float minX, float minY, float maxX, float maxY,
        float padX, float padY)
    {
        var plate = EnsureStrip(host, name);
        Place(plate.GetComponent<RectTransform>(), minX, minY, maxX, maxY, padX, padY);
    }

    private static Transform EnsureStrip(Transform host, string name)
    {
        var plate = host.Find(name);
        if (plate != null && (plate.Find("fill") != null || plate.Find("edgeTop") != null))
        {
            Ours.Remove(plate.gameObject);
            Object.DestroyImmediate(plate.gameObject);
            plate = null;
        }

        if (plate == null)
        {
            var go = RestlessUi.TallStrip(host, name, RestlessUi.RowTint, true);
            Ours.Add(go);
            plate = go.transform;
        }

        plate.SetAsFirstSibling();
        return plate;
    }

    private static Transform? ListHost(InventoryGui gui)
    {
        var list = gui.m_recipeListRoot;
        if (list != null && list.parent != null && list.parent.name == "Recipes")
            return list.parent;
        return list;
    }

    private static void AddRt(List<RectTransform> parts, Transform? node)
    {
        var rt = node != null ? node.GetComponent<RectTransform>() : null;
        if (rt != null)
            parts.Add(rt);
    }

    private static void Cover(RectTransform plate, List<RectTransform> parts, float padX, float padY)
    {
        if (!Union(parts, out var minX, out var minY, out var maxX, out var maxY))
            return;
        Place(plate, minX, minY, maxX, maxY, padX, padY);
    }

    private static bool Union(List<RectTransform> parts, out float minX, out float minY, out float maxX,
        out float maxY)
    {
        minX = float.PositiveInfinity;
        minY = float.PositiveInfinity;
        maxX = float.NegativeInfinity;
        maxY = float.NegativeInfinity;
        var box = new Vector3[4];
        foreach (var rt in parts)
        {
            if (rt == null || !rt.gameObject.activeInHierarchy)
                continue;
            rt.GetWorldCorners(box);
            minX = Mathf.Min(minX, box[0].x);
            minY = Mathf.Min(minY, box[0].y);
            maxX = Mathf.Max(maxX, box[2].x);
            maxY = Mathf.Max(maxY, box[1].y);
        }

        return !float.IsInfinity(minX) && maxX > minX;
    }

    private static bool WorldBox(Transform? node, out float minX, out float minY, out float maxX,
        out float maxY)
    {
        minX = minY = maxX = maxY = 0f;
        if (node is not RectTransform rt || !rt.gameObject.activeInHierarchy)
            return false;
        var box = new Vector3[4];
        rt.GetWorldCorners(box);
        minX = box[0].x;
        minY = box[0].y;
        maxX = box[2].x;
        maxY = box[1].y;
        return maxX > minX;
    }

    private static void Place(RectTransform plate, float minX, float minY, float maxX, float maxY,
        float padX, float padY, bool paint = true)
    {
        var z = plate.position.z;
        var scale = plate.lossyScale;
        var sx = Mathf.Abs(scale.x) < 0.01f ? 1f : Mathf.Abs(scale.x);
        var sy = Mathf.Abs(scale.y) < 0.01f ? 1f : Mathf.Abs(scale.y);
        plate.anchorMin = plate.anchorMax = new Vector2(0.5f, 0.5f);
        plate.pivot = new Vector2(0.5f, 0.5f);
        plate.position = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, z);
        plate.sizeDelta = new Vector2((maxX - minX) / sx + padX * 2f,
            (maxY - minY) / sy + padY * 2f);
        var img = plate.GetComponent<Image>();
        img.raycastTarget = true;
        if (paint)
            img.color = RestlessUi.RowTint;
    }

    private static void DropNamed(Transform? root, string name)
    {
        if (root == null)
            return;
        var node = root.Find(name);
        if (node == null)
            return;
        Ours.Remove(node.gameObject);
        Object.Destroy(node.gameObject);
    }

    private static void DropWell(Transform? root)
    {
        if (root == null)
            return;
        var well = root.Find("RestlessWell");
        if (well == null)
            return;
        Ours.Remove(well.gameObject);
        Object.Destroy(well.gameObject);
    }

    private static void HideTitle(Transform? root)
    {
        if (root == null)
            return;
        var title = root.Find("TitlePanel") ?? RestlessUi.Deep(root, "TitlePanel");
        if (title == null)
            return;
        RestlessUi.Quiet(title.gameObject);
        foreach (var tmp in title.GetComponentsInChildren<TMP_Text>(true))
            SilenceTmp(tmp);
        var face = title.Find("Restless_playerName");
        if (face != null)
            face.gameObject.SetActive(false);
    }

    private static void DropPlate(Transform? node)
    {
        if (node == null)
            return;
        var plate = node.Find("RestlessPlate");
        if (plate == null)
            return;
        Ours.Remove(plate.gameObject);
        Object.Destroy(plate.gameObject);
    }

    private static void StripDrop(Button? button)
    {
        if (button == null)
            return;
        var stale = button.transform.Find("RestlessChip");
        if (stale != null)
            Object.Destroy(stale.gameObject);
        var image = button.GetComponent<Image>();
        if (image != null)
            Ghost(image);
    }

    private static void HideRepairChrome(InventoryGui gui)
    {
        foreach (var root in new Transform?[] { gui.m_repairPanel, gui.m_repairPanelSelection })
        {
            if (root == null)
                continue;
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.transform.name.StartsWith("Restless"))
                    continue;
                var n = image.gameObject.name.ToLowerInvariant();
                if (n.Contains("icon") && image.rectTransform.rect.width <= 80f)
                    continue;
                Hide(image);
            }
        }

        if (gui.m_repairButtonGlow != null)
            Hide(gui.m_repairButtonGlow);
        if (gui.m_repairButton != null)
        {
            foreach (var image in gui.m_repairButton.GetComponentsInChildren<Image>(true))
            {
                if (image.transform.name.StartsWith("Restless"))
                    continue;
                if (image.rectTransform.rect.width > 80f || image.rectTransform.rect.height > 80f)
                    Hide(image);
            }
        }
    }

    private static void DressIconButton(Button? button, bool lit)
    {
        if (button == null)
            return;
        var stale = button.transform.Find("RestlessChip");
        if (stale != null)
            Object.Destroy(stale.gameObject);
        var image = button.GetComponent<Image>();
        if (image != null)
        {
            if (image.rectTransform.rect.width > 80f || image.rectTransform.rect.height > 80f)
                Hide(image);
            else
                Ghost(image);
        }
        foreach (var child in button.GetComponentsInChildren<Image>(true))
        {
            if (child == image || child.transform.name.StartsWith("Restless"))
                continue;
            var n = child.gameObject.name.ToLowerInvariant();
            if (n.Contains("glow") || n.Contains("selected") || n is "bkg"
                || child.rectTransform.rect.width > 80f)
                Hide(child);
        }

        foreach (var tmp in button.GetComponentsInChildren<TMP_Text>(true))
            SilenceTmp(tmp);
        if (image != null)
            image.color = lit ? new Color(1f, 1f, 1f, 0.12f) : Color.clear;
    }

    private static void DressButton(Button? button, string fallback, bool lit, Vector2? chipSize = null)
    {
        if (button == null)
            return;
        if (HasChip(button))
        {
            var ready = button.GetComponent<RectTransform>();
            var holdReady = chipSize ?? (ready != null && ready.rect.width > 8f && ready.rect.height > 8f
                ? ready.rect.size
                : (Vector2?)null);
            Ours.Add(RestlessUi.ChipButton(button, RestlessUi.ButtonCopy(button, fallback), lit, holdReady));
            return;
        }

        var rt = button.GetComponent<RectTransform>();
        if (rt != null && (rt.rect.width > 800f || rt.rect.height > 800f))
        {
            StripDrop(button);
            return;
        }

        var hold = chipSize ?? (rt != null && rt.rect.width > 8f && rt.rect.height > 8f
            ? rt.rect.size
            : (Vector2?)null);
        if (chipSize == null && hold is not { x: > 80f } && button.transform.Find("RestlessChip") == null)
        {
            DressIconButton(button, lit);
            return;
        }

        var image = button.GetComponent<Image>();
        if (image != null)
            Ghost(image);

        foreach (var child in button.GetComponentsInChildren<Transform>(true))
        {
            var n = child.name.ToLowerInvariant();
            if (n.Contains("selected") || n.Contains("gamepad"))
            {
                RestlessUi.Quiet(child.gameObject);
                if (n.Contains("gamepad"))
                    child.gameObject.SetActive(false);
            }
        }

        foreach (var child in button.GetComponentsInChildren<Image>(true))
        {
            if (child == image || child.transform.name.StartsWith("Restless"))
                continue;
            if (child.GetComponent<Button>() != null && child.gameObject != button.gameObject)
                continue;
            var n = child.gameObject.name.ToLowerInvariant();
            if (n.Contains("selected") || n.Contains("glow") || n is "bkg")
                Hide(child);
        }

        foreach (var tmp in button.GetComponentsInChildren<TMP_Text>(true))
            SilenceTmp(tmp);

        var title = RestlessUi.ButtonCopy(button, fallback);
        Ours.Add(RestlessUi.ChipButton(button, title, lit, hold));
    }

    private static bool HasChip(Button button) => button.transform.Find("RestlessChip") != null;

    private static void DressRecipes(InventoryGui gui)
    {
        if (gui.m_recipeListRoot == null)
            return;
        var picked = gui.GetSelectedRecipeIndex(false);
        WalkRecipes(gui, gui.m_recipeListRoot, picked);
    }

    private static void WalkRecipes(InventoryGui gui, Transform root, int picked)
    {
        for (var i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (child.name.StartsWith("RecipeElement"))
            {
                if (child.gameObject.activeInHierarchy)
                {
                    var lit = picked >= 0 && gui.FindSelectedRecipe(child.gameObject) == picked;
                    DressRecipeRow(child, lit);
                }

                continue;
            }

            WalkRecipes(gui, child, picked);
        }
    }

    private static bool PaintRecipeRow(Transform row, bool lit)
    {
        var plate = row.Find("RestlessSlot");
        if (plate == null)
            return false;
        RestlessUi.PaperControl(plate.gameObject, lit ? RestlessUi.Accent : null);
        var nameTmp = row.Find("name")?.GetComponent<TMP_Text>();
        var qualityTmp = row.Find("QualityLevel")?.GetComponent<TMP_Text>();
        var title = RestlessUi.Bare(nameTmp != null ? nameTmp.text : "");
        int.TryParse(RestlessUi.Bare(qualityTmp != null ? qualityTmp.text : ""), out var quality);
        var label = plate.Find("label")?.GetComponent<Text>();
        if (label != null)
        {
            label.text = title;
            label.color = lit ? RestlessUi.Accent : RestlessUi.Text;
        }

        SilenceTmp(qualityTmp);
        if (qualityTmp != null)
            qualityTmp.gameObject.SetActive(false);
        RestlessUi.HideVanillaSlotText(row.gameObject);
        RestlessUi.DressQuality(row, quality, false);
        return true;
    }

    private static void DressRecipeRow(Transform row, bool lit)
    {
        if (PaintRecipeRow(row, lit))
            return;

        foreach (var image in row.GetComponents<Image>())
            Ghost(image);

        foreach (var image in row.GetComponentsInChildren<Image>(true))
        {
            if (image.transform.name.StartsWith("Restless") || image.transform == row)
                continue;
            var n = image.gameObject.name.ToLowerInvariant();
            if (n.Contains("icon") || n.Contains("item"))
                continue;
            if (n is "bkg")
            {
                Ghost(image);
                continue;
            }

            if (n is "selected" || n.Contains("durab"))
                Hide(image);
        }

        var title = "";
        var quality = 0;
        foreach (var tmp in row.GetComponentsInChildren<TMP_Text>(true))
        {
            var n = tmp.gameObject.name.ToLowerInvariant();
            if (n.Contains("quality") || n.Contains("level"))
                int.TryParse(RestlessUi.Bare(tmp.text), out quality);
            else if (n is "name" || string.IsNullOrEmpty(title))
                title = RestlessUi.Bare(tmp.text);
            SilenceTmp(tmp);
        }

        foreach (var leftover in row.GetComponentsInChildren<Text>(true))
        {
            if (leftover.transform.name.StartsWith("Restless")
                || leftover.transform.parent != null && leftover.transform.parent.name.StartsWith("Restless"))
                continue;
            leftover.enabled = false;
        }

        var plate = row.Find("RestlessSlot");
        if (plate == null)
        {
            var go = RestlessUi.Strip(row, "RestlessSlot");
            Ours.Add(go);
            plate = go.transform;
        }

        plate.SetAsFirstSibling();
        RestlessUi.Stretch(plate.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        plate.GetComponent<Image>().raycastTarget = true;
        RestlessUi.PaperControl(plate.gameObject, lit ? RestlessUi.Accent : null);

        var label = plate.Find("label")?.GetComponent<Text>();
        if (label == null)
        {
            label = RestlessUi.Label(plate, title, RestlessUi.HudSize, RestlessUi.Text, TextAnchor.MiddleLeft);
            RestlessUi.Stretch(label.gameObject, Vector2.zero, Vector2.one, new Vector2(40f, 1f),
                new Vector2(-28f, -1f));
        }

        label.text = title;
        label.color = lit ? RestlessUi.Accent : RestlessUi.Text;
        RestlessUi.HideVanillaSlotText(row.gameObject);
        RestlessUi.DressQuality(row, quality, false);
    }

    private static void DressRequirements(InventoryGui gui)
    {
        if (gui.m_recipeRequirementList == null)
            return;
        foreach (var go in gui.m_recipeRequirementList)
        {
            if (go == null)
                continue;
            var icon = RestlessUi.Deep<Image>(go.transform, "res_icon")
                ?? go.transform.Find("res_icon")?.GetComponent<Image>();
            var amountTmp = RestlessUi.Deep<TMP_Text>(go.transform, "res_amount");
            var nameTmp = RestlessUi.Deep<TMP_Text>(go.transform, "res_name");
            var count = RestlessUi.Bare(amountTmp != null ? amountTmp.text : "");
            var name = RestlessUi.Bare(nameTmp != null ? nameTmp.text : "");
            var live = go.activeSelf && icon != null && icon.sprite != null && icon.enabled
                && icon.gameObject.activeInHierarchy && icon.color.a > 0.2f && count.Length > 0 && count != "0"
                && name.Length > 0;
            if (!live)
            {
                RestlessUi.Quiet(go);
                var stale = go.transform.Find("RestlessSlot");
                if (stale != null)
                {
                    var staleCount = stale.Find("RestlessMaterialCount")?.GetComponent<Text>();
                    if (staleCount != null) staleCount.text = "";
                    stale.gameObject.SetActive(false);
                }
                SilenceTmp(amountTmp);
                SilenceTmp(nameTmp);
                continue;
            }

            RestlessUi.Loud(go);
            var plate = RestlessUi.DressSlot(go, icon, false, null, Hidden, true);
            plate.SetActive(true);
            if (!Ours.Contains(plate))
                Ours.Add(plate);
            foreach (var tmp in go.GetComponentsInChildren<TMP_Text>(true))
                SilenceTmp(tmp);
            var nameFace = go.transform.Find("Restless_resName");
            if (nameFace != null)
                nameFace.gameObject.SetActive(false);
            RestlessUi.HideVanillaSlotText(go);
            RestlessUi.PaperSurface(plate, accent: RestlessUi.PaperMuted * 0.5f);
            plate.GetComponent<Image>().raycastTarget = true;
            // Keep the original count format and shortage colour, including mod-provided have/need.
            PaintRequirement(go, amountTmp);
        }
    }

    private static void Face(TMP_Text? src, string tag, Color color, int size, bool wrap = false)
    {
        if (src == null)
            return;
        if (RestlessUi.HintNode(src.transform))
        {
            SilenceTmp(src);
            src.gameObject.SetActive(false);
            return;
        }

        SilenceTmp(src);
        foreach (var row in Readouts)
        {
            if (row.Src != src)
                continue;
            row.Face.color = color;
            row.Face.fontSize = size;
            row.Wrap = wrap;
            return;
        }

        var host = src.transform.parent;
        if (host == null)
            return;
        var existing = host.Find("Restless_" + tag);
        Text face;
        if (existing != null)
            face = existing.GetComponent<Text>();
        else
        {
            face = RestlessUi.Label(host, "", size, color,
                wrap ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft);
            face.gameObject.name = "Restless_" + tag;
            Ours.Add(face.gameObject);
            var dest = face.GetComponent<RectTransform>();
            var from = src.rectTransform;
            RestlessUi.CopyRect(dest, from);
            dest.SetSiblingIndex(src.transform.GetSiblingIndex() + 1);
        }

        face.color = color;
        face.fontSize = size;
        face.alignment = wrap
            ? TextAnchor.UpperLeft
            : src.alignment switch
            {
                TextAlignmentOptions.Center or TextAlignmentOptions.Midline => TextAnchor.MiddleCenter,
                TextAlignmentOptions.Right or TextAlignmentOptions.MidlineRight
                    or TextAlignmentOptions.BottomRight => TextAnchor.MiddleRight,
                _ => TextAnchor.MiddleLeft
            };
        Readouts.Add(new Readout { Src = src, Face = face, Wrap = wrap });
    }

    private static void PaintReadout(Readout row)
    {
        if (row.Face == null)
            return;
        if (row.Src == null || !row.Src.gameObject.activeInHierarchy)
        {
            row.Face.gameObject.SetActive(false);
            return;
        }

        for (var t = row.Face.transform; t != null; t = t.parent)
        {
            var fade = t.GetComponent<CanvasGroup>();
            if (fade != null && fade.alpha < 0.02f)
            {
                row.Face.gameObject.SetActive(false);
                return;
            }
        }

        row.Face.gameObject.SetActive(true);
        if (!row.Face.gameObject.name.EndsWith("_station")
            && !(ExtraSlots.HoldsPlayerStats && row.Face.gameObject.name is "Restless_armor" or "Restless_weight"))
            RestlessUi.CopyRect(row.Face.rectTransform, row.Src.rectTransform);
        var copy = RestlessUi.Bare(row.Src.text);
        if (copy.Length == 0)
        {
            row.Face.gameObject.SetActive(false);
            return;
        }

        row.Face.text = copy;
        if (row.Face.gameObject.name.IndexOf("weight", System.StringComparison.OrdinalIgnoreCase) >= 0
            && Overweight(copy))
            row.Face.color = RestlessUi.HealthTint;
        else if (row.Face.gameObject.name.IndexOf("armor", System.StringComparison.OrdinalIgnoreCase) >= 0
            || row.Face.gameObject.name.IndexOf("weight", System.StringComparison.OrdinalIgnoreCase) >= 0)
            row.Face.color = RestlessUi.Text;
        row.Face.horizontalOverflow = row.Wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
        row.Face.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private static void QuietChrome(InventoryGui gui)
    {
        var root = gui.m_inventoryRoot;
        if (root == null)
            return;

        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.transform.name.StartsWith("Restless"))
                continue;
            if (image.GetComponentInParent<InventoryElement>() != null)
                continue;
            if (UnderRecipeRow(image.transform))
                continue;
            if (KeepIcon(image, gui))
                continue;
            if (UnderRequirements(image, gui))
                continue;
            if (UnderSkip(image.transform))
                continue;
            if (UnderModal(image.transform))
            {
                QuietModalChrome(image);
                continue;
            }
            if (image.GetComponent<Button>() != null)
                continue;
            var n = image.gameObject.name.ToLowerInvariant();
            if (n == "panel-back" || IsChromeName(n))
                Hide(image);
        }

        foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp.transform.name.StartsWith("Restless") || UnderSkip(tmp.transform))
                continue;
            SilenceTmp(tmp);
            if (RestlessUi.HintNode(tmp.transform)
                || tmp.gameObject.name.Equals("help_Text", System.StringComparison.OrdinalIgnoreCase))
                tmp.gameObject.SetActive(false);
        }
    }

    private static bool UnderSkip(Transform node)
    {
        for (var t = node; t != null; t = t.parent)
        {
            if (t.name is "SplitDialog" or "VariantDialog")
                return true;
        }

        return false;
    }

    private static bool UnderModal(Transform node)
    {
        for (var t = node; t != null; t = t.parent)
        {
            if (t.name is "Skills" or "Texts" or "Trophies" or "Achievements"
                or "TextsDialog" or "SkillsDialog")
                return true;
        }

        return false;
    }

    private static void QuietModalChrome(Image image)
    {
        var n = image.gameObject.name.ToLowerInvariant();
        if (n.Contains("icon") || n is "bar")
            return;
        if (image.GetComponent<Button>() != null)
        {
            var rt = image.rectTransform;
            if (rt.rect.width > 800f || rt.rect.height > 800f)
                Ghost(image);
            return;
        }

        if (n is "darken" or "blur" || n.Contains("border") || n.Contains("selected")
            || IsChromeName(n) || n.Contains("frame"))
            Hide(image);
    }

    private static bool UnderRecipeRow(Transform node)
    {
        for (var t = node; t != null; t = t.parent)
        {
            if (t.name.StartsWith("RecipeElement"))
                return true;
        }

        return false;
    }

    private static bool Overweight(string copy)
    {
        var slash = copy.IndexOf('/');
        if (slash <= 0)
            return false;
        return float.TryParse(copy.Substring(0, slash).Trim(),
                   System.Globalization.NumberStyles.Float,
                   System.Globalization.CultureInfo.InvariantCulture, out var cur)
            && float.TryParse(copy.Substring(slash + 1).Trim(),
                   System.Globalization.NumberStyles.Float,
                   System.Globalization.CultureInfo.InvariantCulture, out var max)
            && max > 0f && cur > max;
    }

    private static bool KeepIcon(Image image, InventoryGui gui) =>
        image == gui.m_craftingStationIcon
        || image == gui.m_recipeIcon
        || image == gui.m_upgradeItemIcon
        || image == gui.m_minStationLevelIcon
        || image.gameObject.name.IndexOf("armor_icon", System.StringComparison.OrdinalIgnoreCase) >= 0
        || image.gameObject.name.IndexOf("weight_icon", System.StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool UnderRequirements(Image image, InventoryGui gui)
    {
        if (gui.m_recipeRequirementList == null)
            return false;
        foreach (var go in gui.m_recipeRequirementList)
        {
            if (go != null && image.transform.IsChildOf(go.transform))
                return true;
        }

        return false;
    }

    private static bool IsChromeName(string n)
    {
            if (n is "bkg" or "background" or "panel-back" or "darken" or "sunken" or "sep"
            or "inventory" or "tabborder" or "repairsimple" or "selected_frame"
            or "decription" or "requirements" or "craft_button_panel" or "recipes"
            or "recipelist" or "textarea" or "textlist" or "list" or "win" or "window")
            return true;
        if (n.StartsWith("selected (") || n.StartsWith("braidline"))
            return true;
        return n.Contains("bkg") || n.Contains("background");
    }

    private static void Hide(Image image)
    {
        if (image == null || !image.enabled)
            return;
        Hidden.Add(image);
        image.enabled = false;
    }

    private static void Ghost(Image image)
    {
        if (image == null)
            return;
        if (!Ghosted.ContainsKey(image))
            Ghosted[image] = image.color;
        image.color = Color.clear;
        image.raycastTarget = true;
        image.enabled = true;
    }

    private static void SilenceTmp(TMP_Text? tmp)
    {
        if (tmp == null || tmp.transform.name.StartsWith("Restless"))
            return;
        if (!TmpAlpha.ContainsKey(tmp))
            TmpAlpha[tmp] = tmp.alpha;
        RestlessUi.SilenceTmp(tmp);
        if (!Silenced.Contains(tmp.gameObject))
            Silenced.Add(tmp.gameObject);
    }

    private static void Undress(InventoryGui gui)
    {
        RestoreCraftControls();
        foreach (var go in Ours)
        {
            if (go != null)
                Object.Destroy(go);
        }

        Ours.Clear();
        foreach (var behaviour in Hidden)
        {
            if (behaviour != null)
            {
                behaviour.enabled = true;
                if (behaviour is GuiBar)
                    behaviour.gameObject.SetActive(true);
            }
        }

        Hidden.Clear();
        foreach (var pair in Ghosted)
        {
            if (pair.Key != null)
                pair.Key.color = pair.Value;
        }

        Ghosted.Clear();
        foreach (var pair in TmpAlpha)
        {
            if (pair.Key != null)
            {
                pair.Key.alpha = pair.Value;
                pair.Key.enabled = true;
                pair.Key.maxVisibleCharacters = int.MaxValue;
            }
        }

        TmpAlpha.Clear();
        Silenced.Clear();
        Readouts.Clear();
        _dressed = false;
        _syncStamp = 0;
    }

    private static void TearDown()
    {
        var gui = InventoryGui.instance;
        if (gui != null)
            Undress(gui);
        else
        {
            RestoreCraftControls();
            foreach (var go in Ours)
            {
                if (go != null)
                    Object.Destroy(go);
            }

            Ours.Clear();
            Hidden.Clear();
            Ghosted.Clear();
            TmpAlpha.Clear();
            Silenced.Clear();
            Readouts.Clear();
            _dressed = false;
            _syncStamp = 0;
        }
    }
}
