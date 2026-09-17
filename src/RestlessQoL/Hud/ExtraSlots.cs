using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using RestlessQoL.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RestlessQoL.HudTweaks;

// Worn + Z/X/C live in two hidden rows on the player inventory (same pattern
// as EquipmentAndQuickSlots). UpdateGui relocates those real cells into a well
// to the right of the bag. HUD quick slots match hotbar 1–8 size.
public sealed class ExtraSlots : FeatureModule
{
    public override string Id => "inventory.equipment_slots";
    public override bool Enabled => true;
    public override bool TickInMenus => true;

    private const string EquipKey = "restless.equip";
    private const string QuickKey = "restless.quick";
    private const string HeightKey = "restless.inv.visible";
    private const int ExtraRows = 2;
    private const int EquipCount = 6;
    private const int QuickCount = 3;
    private const int ExtraCount = EquipCount + QuickCount;

    private static readonly List<Behaviour> Hidden = new();
    private static readonly HashSet<GameObject> Ours = new();
    private static readonly long[] ExtraPaint = new long[ExtraCount];
    private static readonly ItemDrop.ItemData.ItemType[,] Worn =
    {
        { ItemDrop.ItemData.ItemType.Helmet, ItemDrop.ItemData.ItemType.Shoulder },
        { ItemDrop.ItemData.ItemType.Chest, ItemDrop.ItemData.ItemType.Utility },
        { ItemDrop.ItemData.ItemType.Legs, ItemDrop.ItemData.ItemType.Trinket }
    };

    private static readonly string[,] WornName =
    {
        { "Head", "Cape" },
        { "Chest", "Utility" },
        { "Legs", "Trinket" }
    };

    private static RectTransform? _slotRoot;
    private static RectTransform? _hiddenRoot;
    private static GameObject? _hud;
    private static readonly Cell[] HudSlots = new Cell[3];
    private static float _hudRight;
    private static int _visibleHeight = -1;
    private static bool _wasOn = true;
    private static bool _busy;
    private static bool _doingEquip;
    private static int _leaveWorn;
    private static int _craftPark;
    private static int _dragExtra = -1;
    private static ItemDrop.ItemData? _dragItem;
    private static bool _dragMoved;
    private static readonly List<ItemDrop.ItemData> GraveLoot = new();

    private sealed class Cell
    {
        public GameObject Go = null!;
        public Image Icon = null!;
        public Text? Key;
    }

    protected override void OnLoaded()
    {
        GUIManager.OnCustomGUIAvailable += TearDownUi;
        // Valheim has Load(ZPackage) and Load(ZPackage, bool). PatchAll + nameof(Load)
        // is AmbiguousMatch and FeatureModule then unpatches the whole extras class.
        var prefix = new HarmonyMethod(typeof(ExtraSlots), nameof(BeforeInvLoad));
        var postfix = new HarmonyMethod(typeof(ExtraSlots), nameof(AfterInvLoad));
        foreach (var method in AccessTools.GetDeclaredMethods(typeof(Inventory)))
        {
            if (method.Name != nameof(Inventory.Load))
                continue;
            Harmony!.Patch(method, prefix: prefix, postfix: postfix);
        }
    }

    private static void BeforeInvLoad(Inventory __instance)
    {
        if (IsLocal(__instance))
            Grow(__instance, Player.m_localPlayer);
    }

    private static void AfterInvLoad(Inventory __instance)
    {
        if (IsLocal(__instance))
            Grow(__instance, Player.m_localPlayer);
    }

    public override void Tick()
    {
        var player = Player.m_localPlayer;
        if (player == null)
            return;

        if (!ModConfig.ExtraSlotsEnabled.Value)
        {
            if (_wasOn)
            {
                Shrink(player);
                TearDownUi();
                _wasOn = false;
            }

            return;
        }

        _wasOn = true;
        Grow(player.GetInventory(), player);
        PullLegacy(player);
        UseHotkeys(player);
    }

    internal static bool HoldsPlayerStats => ModConfig.ExtraSlotsEnabled.Value;

    internal static bool IsExtraCell(Vector2i pos) =>
        _visibleHeight >= 0 && pos.y >= _visibleHeight;

    internal static bool SkipDeposit(ItemDrop.ItemData? item)
    {
        if (item == null)
            return true;
        var pos = item.m_gridPos;
        if (pos.y == 0 && pos.x >= 0)
            return true;
        return IsExtraCell(pos);
    }

    internal static void Widen(ref float right)
    {
        if (ModConfig.ExtraSlotsEnabled.Value && _hud != null && _hud.activeSelf && _hudRight > right)
            right = _hudRight;
    }

    internal static float HudPad(float slotHeight)
    {
        if (!ModConfig.ExtraSlotsEnabled.Value)
            return 0f;
        if (Hotbar.TryPitch(out var step, out _, out _))
            return Mathf.Abs(step) * 3f + RestlessUi.RowGap;
        var size = Hotbar.TryPlateLocal(out var local) ? local : Mathf.Min(slotHeight, 64f);
        return RestlessUi.RowGap + size * 3f + 8f;
    }

    private static void Grow(Inventory? inv, Player? player = null)
    {
        if (inv == null)
            return;
        player ??= Player.m_localPlayer;
        if (_visibleHeight < 0)
        {
            var h = inv.GetHeight();
            if (player != null && player.m_customData.TryGetValue(HeightKey, out var raw)
                && int.TryParse(raw, out var saved) && saved >= 4)
            {
                _visibleHeight = saved;
                if (AlreadyGrown(inv) && h == saved)
                    _visibleHeight = Mathf.Max(4, saved - ExtraRows);
            }
            else
                _visibleHeight = AlreadyGrown(inv) ? h - ExtraRows : h;

            if (player != null)
                player.m_customData[HeightKey] = _visibleHeight.ToString();
        }

        var want = _visibleHeight + ExtraRows;
        if (inv.GetHeight() < want)
            inv.SetHeight(want);
    }

    private static bool AlreadyGrown(Inventory inv)
    {
        var h = inv.GetHeight();
        var w = inv.GetWidth();
        if (h < 4 + ExtraRows || w < 1)
            return false;
        var startY = h - ExtraRows;
        for (var i = ExtraCount; i < ExtraRows * w; i++)
        {
            var x = i % w;
            var y = startY + i / w;
            if (inv.GetItemAt(x, y) != null)
                return false;
        }

        return true;
    }

    private static void Shrink(Player player)
    {
        var inv = player.GetInventory();
        if (inv == null || _visibleHeight < 0)
            return;
        var home = inv;
        _busy = true;
        try
        {
            for (var i = 0; i < ExtraRows * inv.GetWidth(); i++)
            {
                var pos = ExtraPos(inv, i);
                var item = inv.GetItemAt(pos.x, pos.y);
                if (item == null)
                    continue;
                inv.RemoveItem(item);
                if (!TryAddVisible(home, item))
                    ItemDrop.DropItem(item, item.m_stack, player.transform.position + Vector3.up,
                        player.transform.rotation);
            }

            inv.SetHeight(_visibleHeight);
        }
        finally
        {
            _busy = false;
            _visibleHeight = -1;
        }
    }

    private static bool TryAddVisible(Inventory inv, ItemDrop.ItemData item)
    {
        for (var y = 1; y < _visibleHeight; y++)
        {
            for (var x = 0; x < inv.GetWidth(); x++)
            {
                if (SlotLock.Held(new Vector2i(x, y)))
                    continue;
                if (inv.GetItemAt(x, y) != null)
                    continue;
                return inv.AddItem(item, item.m_stack, x, y, false);
            }
        }

        return false;
    }

    private static Vector2i ExtraPos(Inventory inv, int extra)
    {
        var w = inv.GetWidth();
        var i = _visibleHeight * w + extra;
        return new Vector2i(i % w, i / w);
    }

    private static bool SlotOf(ItemDrop.ItemData.ItemType type, out int extra)
    {
        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < 2; x++)
            {
                if (Worn[y, x] != type)
                    continue;
                extra = y * 2 + x;
                return true;
            }
        }

        extra = -1;
        return false;
    }

    private static bool ExtraIndex(Inventory inv, int x, int y, out int extra)
    {
        extra = -1;
        if (_visibleHeight < 0 || y < _visibleHeight)
            return false;
        extra = (y - _visibleHeight) * inv.GetWidth() + x;
        return extra >= 0 && extra < ExtraRows * inv.GetWidth();
    }

    private static bool IsHiddenExtra(Inventory inv, int extra) =>
        extra >= ExtraCount && extra < ExtraRows * inv.GetWidth();

    private static Vector2i VisibleEmpty(Inventory inv, bool topFirst, bool skipHotbar = false)
    {
        var w = inv.GetWidth();
        var h = Mathf.Max(_visibleHeight, 0);
        var first = skipHotbar ? 1 : 0;
        if (topFirst)
        {
            for (var y = first; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    if (SlotLock.Held(new Vector2i(x, y)))
                        continue;
                    if (inv.GetItemAt(x, y) == null)
                        return new Vector2i(x, y);
                }
            }
        }
        else
        {
            for (var y = h - 1; y >= first; y--)
            {
                for (var x = 0; x < w; x++)
                {
                    if (SlotLock.Held(new Vector2i(x, y)))
                        continue;
                    if (inv.GetItemAt(x, y) == null)
                        return new Vector2i(x, y);
                }
            }
        }

        return new Vector2i(-1, -1);
    }

    private static int CountVisibleEmpty(Inventory inv)
    {
        var n = 0;
        var w = inv.GetWidth();
        for (var y = 0; y < _visibleHeight; y++)
        {
            for (var x = 0; x < w; x++)
            {
                if (inv.GetItemAt(x, y) == null)
                    n++;
            }
        }

        return n;
    }

    private static bool IsLocal(Inventory? inv) =>
        inv != null && Player.m_localPlayer != null && Player.m_localPlayer.GetInventory() == inv;

    private static bool IsOwnedPlayer(Player? player) =>
        player != null && player.m_nview != null && player.m_nview.IsValid() && player.m_nview.IsOwner();

    private static bool CraftingNow()
    {
        var gui = InventoryGui.instance;
        return gui != null && gui.m_craftTimer >= 0f;
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateInventory))]
        private static void BeforeInv(InventoryGrid __instance)
        {
            if (!ModConfig.ExtraSlotsEnabled.Value || Player.m_localPlayer == null)
                return;
            var gui = InventoryGui.instance;
            if (gui == null || __instance != gui.m_playerGrid)
                return;
            Grow(Player.m_localPlayer.GetInventory(), Player.m_localPlayer);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
        private static void AfterGrid(InventoryGrid __instance)
        {
            if (!ModConfig.ExtraSlotsEnabled.Value)
                return;
            var gui = InventoryGui.instance;
            if (gui == null || __instance != gui.m_playerGrid || Player.m_localPlayer == null)
                return;
            if (__instance.GetInventory() != Player.m_localPlayer.GetInventory())
                return;
            Grow(Player.m_localPlayer.GetInventory(), Player.m_localPlayer);
            Relocate(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
        [HarmonyPriority(Priority.Last)]
        private static void AfterInvGui()
        {
            var gui = InventoryGui.instance;
            if (gui == null || gui.m_playerGrid == null || !ModConfig.ExtraSlotsEnabled.Value)
                return;
            if (gui.m_inventoryRoot == null || !gui.m_inventoryRoot.gameObject.activeInHierarchy)
                return;
            Relocate(gui.m_playerGrid);
            InventoryScreen.RefreshInventoryMaterials(gui);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(global::Hud), nameof(global::Hud.Update))]
        private static void AfterHud()
        {
            PaintHud();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), nameof(Player.Load))]
        private static void BeforeLoad(Player __instance)
        {
            if (!IsOwnedPlayer(__instance))
                return;
            _visibleHeight = -1;
            Grow(__instance.GetInventory(), __instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.SetHeight))]
        private static void BeforeHeight(Inventory __instance, ref int height)
        {
            if (!ModConfig.ExtraSlotsEnabled.Value || !IsLocal(__instance))
                return;
            if (_visibleHeight < 0)
                Grow(__instance, Player.m_localPlayer);
            var want = _visibleHeight + ExtraRows;
            if (_visibleHeight >= 0 && height < want)
                height = want;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.Load))]
        private static void AfterLoad(Player __instance)
        {
            if (!IsOwnedPlayer(__instance))
                return;
            Grow(__instance.GetInventory(), __instance);
            PullLegacy(__instance);
            SeatAllWorn(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
        private static void BeforeCraft() => _craftPark++;

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
        private static void AfterCraft()
        {
            if (_craftPark > 0)
                _craftPark--;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(string), typeof(int), typeof(int),
            typeof(int), typeof(long), typeof(string), typeof(Vector2i), typeof(bool), typeof(bool), typeof(bool))]
        private static void ParkCrafted(Inventory __instance, ref Vector2i position)
        {
            if (_craftPark <= 0 || !IsLocal(__instance) || _visibleHeight < 0)
                return;
            if (!ExtraIndex(__instance, position.x, position.y, out _))
                return;
            var dest = VisibleEmpty(__instance, topFirst: false, skipHotbar: true);
            if (dest.x >= 0)
                position = dest;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupDragItem))]
        private static bool BeforeDrag(ItemDrop.ItemData item, Inventory inventory)
        {
            if (item == null)
            {
                FinishDrag();
                return true;
            }

            if (!TryDragExtra(inventory, item, out var extra))
                return true;
            if (extra < EquipCount)
                return false;
            _dragExtra = extra;
            _dragItem = item;
            _dragMoved = false;
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.EquipInventoryItems))]
        private static void AfterEquipAll(Player __instance)
        {
            if (!IsOwnedPlayer(__instance) || CraftingNow())
                return;
            SeatAllWorn(__instance);
            WearExtras(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData), typeof(Vector2i))]
        private static bool AddAt(Inventory __instance, ItemDrop.ItemData item, Vector2i pos, ref bool __result)
        {
            MarkDragMove(__instance, item, pos.x, pos.y);
            return AllowExtra(__instance, item, pos.x, pos.y, ref __result);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData), typeof(int), typeof(int),
            typeof(int), typeof(bool))]
        private static bool AddXY(Inventory __instance, ItemDrop.ItemData item, int x, int y, ref bool __result)
        {
            MarkDragMove(__instance, item, x, y);
            return AllowExtra(__instance, item, x, y, ref __result);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveItemToThis), typeof(Inventory), typeof(ItemDrop.ItemData),
            typeof(int), typeof(int), typeof(int))]
        private static bool MoveAt(Inventory __instance, ItemDrop.ItemData item, int x, int y, ref bool __result)
        {
            MarkDragMove(__instance, item, x, y);
            return AllowExtra(__instance, item, x, y, ref __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.DropItem))]
        private static void AfterWorldDrop()
        {
            if (_dragExtra >= 0)
                _dragMoved = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.FindEmptySlot))]
        private static void AfterFindEmpty(Inventory __instance, bool topFirst, ref Vector2i __result)
        {
            if (!IsLocal(__instance) || _visibleHeight < 0 || __result.x < 0)
                return;
            if (!ExtraIndex(__instance, __result.x, __result.y, out _))
                return;
            __result = VisibleEmpty(__instance, topFirst);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.HaveEmptySlot))]
        private static void AfterHaveEmpty(Inventory __instance, ref bool __result)
        {
            if (!IsLocal(__instance) || _visibleHeight < 0)
                return;
            __result = CountVisibleEmpty(__instance) > 0;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetEmptySlots))]
        private static void AfterEmptyCount(Inventory __instance, ref int __result)
        {
            if (!IsLocal(__instance) || _visibleHeight < 0)
                return;
            __result = CountVisibleEmpty(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.CanAddItem), typeof(ItemDrop.ItemData), typeof(int))]
        private static void AfterCanAdd(Inventory __instance, ItemDrop.ItemData item, ref bool __result)
        {
            if (!IsLocal(__instance) || _visibleHeight < 0)
                return;
            if (CountVisibleEmpty(__instance) > 0 || CanStackVisible(__instance, item))
                return;
            __result = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        private static void AfterEquip(Humanoid __instance, ItemDrop.ItemData item, bool __result)
        {
            if (_doingEquip || !__result || __instance is not Player player || item?.m_shared == null)
                return;
            if (!IsOwnedPlayer(player) || CraftingNow())
                return;
            SeatWorn(player, item);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnSelectedItem))]
        private static void BeforeSelected(InventoryGrid.Modifier mod)
        {
            if (mod is InventoryGrid.Modifier.Move or InventoryGrid.Modifier.Drop)
                _leaveWorn++;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnSelectedItem))]
        private static void AfterSelected(InventoryGrid.Modifier mod)
        {
            if ((mod is InventoryGrid.Modifier.Move or InventoryGrid.Modifier.Drop) && _leaveWorn > 0)
                _leaveWorn--;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.DropItem))]
        private static void BeforeDrop() => _leaveWorn++;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.DropItem))]
        private static void AfterDrop()
        {
            if (_leaveWorn > 0)
                _leaveWorn--;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
        private static void AfterUnequip(Humanoid __instance, ItemDrop.ItemData item)
        {
            if (_doingEquip || _busy || _leaveWorn > 0 || __instance is not Player player || item?.m_shared == null)
                return;
            if (!IsOwnedPlayer(player) || CraftingNow())
                return;
            UnseatWorn(player, item);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Inventory), "Changed")]
        private static void AfterChanged(Inventory __instance)
        {
            if (_busy || _doingEquip || !IsLocal(__instance))
                return;
            FinishDrag();
            if (!CraftingNow())
                WearExtras(Player.m_localPlayer);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), nameof(Player.CreateTombStone))]
        private static void BeforeTomb(Player __instance) => TakeGraveLoot(__instance);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.CreateTombStone))]
        private static void AfterTomb(Player __instance) => DropGraveLoot(__instance);
    }

    private static bool TryDragExtra(Inventory? source, ItemDrop.ItemData item, out int extra)
    {
        extra = -1;
        var player = Player.m_localPlayer;
        var inv = player?.GetInventory();
        if (source == null || inv == null || source != inv)
            return false;
        return ExtraIndex(inv, item.m_gridPos.x, item.m_gridPos.y, out extra);
    }

    private static void MarkDragMove(Inventory inv, ItemDrop.ItemData? item, int x, int y)
    {
        if (_dragExtra < 0 || item == null || !IsLocal(inv))
            return;
        var src = ExtraPos(inv, _dragExtra);
        if (x == src.x && y == src.y)
            return;
        if (item == _dragItem)
            _dragMoved = true;
    }

    private static void FinishDrag()
    {
        var extra = _dragExtra;
        var dragged = _dragItem;
        var moved = _dragMoved;
        _dragExtra = -1;
        _dragItem = null;
        _dragMoved = false;
        if (!moved || extra < 0 || dragged == null)
            return;
        var inv = Player.m_localPlayer?.GetInventory();
        if (inv == null || _visibleHeight < 0)
            return;
        var pos = ExtraPos(inv, extra);
        var left = inv.GetItemAt(pos.x, pos.y);
        if (left == null || left != dragged)
            return;
        _busy = true;
        try
        {
            inv.RemoveItem(left);
        }
        finally
        {
            _busy = false;
        }
    }

    private static bool AllowExtra(Inventory inv, ItemDrop.ItemData item, int x, int y, ref bool result)
    {
        if (!IsLocal(inv) || !ExtraIndex(inv, x, y, out var extra))
            return true;
        if (IsHiddenExtra(inv, extra) || extra < EquipCount && !Fits(item, extra))
        {
            result = false;
            return false;
        }

        var occupant = inv.GetItemAt(x, y);
        if (occupant == null || occupant == item)
            return true;

        _busy = true;
        try
        {
            inv.RemoveItem(occupant);
            var player = Player.m_localPlayer;
            if (!TryAddVisible(inv, occupant) && player != null)
                ItemDrop.DropItem(occupant, occupant.m_stack, player.transform.position + Vector3.up,
                    player.transform.rotation);
        }
        finally
        {
            _busy = false;
        }

        return true;
    }

    private static bool KeepInventory() =>
        ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathKeepInventory);

    private static void TakeGraveLoot(Player player)
    {
        GraveLoot.Clear();
        if (!ModConfig.ExtraSlotsEnabled.Value || player != Player.m_localPlayer || KeepInventory())
            return;
        var inv = player.GetInventory();
        if (inv == null || _visibleHeight < 0)
            return;
        Grow(inv, player);
        _busy = true;
        try
        {
            for (var i = 0; i < ExtraCount; i++)
            {
                var pos = ExtraPos(inv, i);
                var item = inv.GetItemAt(pos.x, pos.y);
                if (item == null)
                    continue;
                inv.RemoveItem(item);
                item.m_equipped = false;
                GraveLoot.Add(item);
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private static void DropGraveLoot(Player player)
    {
        if (GraveLoot.Count == 0)
            return;
        try
        {
            if (player == null || player.m_tombstone == null || KeepInventory())
            {
                if (player != null)
                {
                    foreach (var item in GraveLoot)
                        ItemDrop.DropItem(item, item.m_stack, player.transform.position + Vector3.up,
                            player.transform.rotation);
                }

                return;
            }

            var go = Object.Instantiate(player.m_tombstone, player.GetCenterPoint() + player.transform.right,
                player.transform.rotation);
            var box = go.GetComponent<Container>()?.GetInventory();
            var tomb = go.GetComponent<TombStone>();
            if (box == null || tomb == null)
            {
                foreach (var item in GraveLoot)
                    ItemDrop.DropItem(item, item.m_stack, player.transform.position + Vector3.up,
                        player.transform.rotation);
                return;
            }

            foreach (var item in GraveLoot)
            {
                if (!box.AddItem(item))
                    ItemDrop.DropItem(item, item.m_stack, go.transform.position + Vector3.up,
                        go.transform.rotation);
            }

            var profile = Game.instance?.GetPlayerProfile();
            if (profile != null)
                tomb.Setup(profile.GetName(), profile.GetPlayerID());
        }
        finally
        {
            GraveLoot.Clear();
        }
    }

    private static bool CanStackVisible(Inventory inv, ItemDrop.ItemData? item)
    {
        if (item?.m_shared == null)
            return false;
        var found = inv.FindFreeStackItem(item.m_shared.m_name, item.m_quality, item.m_worldLevel);
        return found != null && found.m_gridPos.y < _visibleHeight;
    }

    private static bool Fits(ItemDrop.ItemData? item, int extra)
    {
        if (item?.m_shared == null || extra < 0 || extra >= EquipCount)
            return false;
        var x = extra % 2;
        var y = extra / 2;
        return Worn[y, x] == item.m_shared.m_itemType;
    }

    private static void SeatAllWorn(Player? player)
    {
        if (player == null || _busy || !IsOwnedPlayer(player))
            return;
        if (Traverse.Create(player).Field("m_isLoading").GetValue<bool>())
            return;
        var inv = player.GetInventory();
        if (inv == null)
            return;
        Grow(inv, player);
        foreach (var item in new List<ItemDrop.ItemData>(inv.GetAllItems()))
        {
            if (item?.m_shared == null || !item.m_equipped)
                continue;
            if (!SlotOf(item.m_shared.m_itemType, out _))
                continue;
            SeatWorn(player, item);
        }
    }

    private static void WearExtras(Player? player)
    {
        if (player == null || _busy || !IsOwnedPlayer(player) || CraftingNow())
            return;
        if (Traverse.Create(player).Field("m_isLoading").GetValue<bool>())
            return;
        var inv = player.GetInventory();
        if (inv == null || _visibleHeight < 0)
            return;
        _busy = true;
        try
        {
            for (var i = 0; i < EquipCount; i++)
            {
                var pos = ExtraPos(inv, i);
                var item = inv.GetItemAt(pos.x, pos.y);
                if (item?.m_shared == null)
                    continue;
                if (!item.m_equipped)
                    player.EquipItem(item, false);
            }

            for (var i = 0; i < QuickCount; i++)
            {
                var pos = ExtraPos(inv, EquipCount + i);
                var item = inv.GetItemAt(pos.x, pos.y);
                if (item?.m_shared == null || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable)
                    continue;
                if (item.m_equipped)
                    player.EquipItem(item, false);
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private static void SeatWorn(Player player, ItemDrop.ItemData item)
    {
        if (_busy || item?.m_shared == null || !SlotOf(item.m_shared.m_itemType, out var extra))
            return;
        if (Traverse.Create(player).Field("m_isLoading").GetValue<bool>())
            return;
        var inv = player.GetInventory();
        if (inv == null)
            return;
        Grow(inv);
        var dest = ExtraPos(inv, extra);
        if (item.m_gridPos.x == dest.x && item.m_gridPos.y == dest.y)
        {
            HoldWorn(player, item);
            return;
        }

        if (!inv.ContainsItem(item))
            return;

        _busy = true;
        try
        {
            var occupant = inv.GetItemAt(dest.x, dest.y);
            if (occupant != null && occupant != item)
            {
                inv.RemoveItem(occupant);
                if (!TryAddVisible(inv, occupant))
                    ItemDrop.DropItem(occupant, occupant.m_stack, player.transform.position + Vector3.up,
                        player.transform.rotation);
            }

            var stack = item.m_stack;
            inv.RemoveItem(item);
            if (!inv.AddItem(item, stack, dest.x, dest.y, false)
                && !TryAddVisible(inv, item))
                ItemDrop.DropItem(item, stack, player.transform.position + Vector3.up,
                    player.transform.rotation);
        }
        finally
        {
            _busy = false;
        }

        var seated = inv.GetItemAt(dest.x, dest.y);
        if (seated != null)
            HoldWorn(player, seated);
    }

    private static void HoldWorn(Player player, ItemDrop.ItemData item)
    {
        item.m_equipped = true;
        if (player.IsItemEquiped(item))
            return;
        _doingEquip = true;
        try
        {
            player.EquipItem(item, false);
        }
        finally
        {
            _doingEquip = false;
        }
    }

    private static void UnseatWorn(Player player, ItemDrop.ItemData item)
    {
        if (Traverse.Create(player).Field("m_isLoading").GetValue<bool>())
            return;
        if (!SlotOf(item.m_shared.m_itemType, out _))
            return;
        var inv = player.GetInventory();
        if (inv == null || !inv.ContainsItem(item))
            return;
        if (!ExtraIndex(inv, item.m_gridPos.x, item.m_gridPos.y, out var extra) || extra >= EquipCount)
            return;

        _busy = true;
        try
        {
            inv.RemoveItem(item);
            item.m_equipped = false;
            if (!TryAddVisible(inv, item))
                ItemDrop.DropItem(item, item.m_stack, player.transform.position + Vector3.up,
                    player.transform.rotation);
        }
        finally
        {
            _busy = false;
        }
    }

    private static void PullLegacy(Player player)
    {
        if (player == null || _busy)
            return;
        var inv = player.GetInventory();
        if (inv == null)
            return;
        Grow(inv);
        var bkg = Traverse.Create(inv).Field("m_bkg").GetValue<Sprite>();
        Import(player, inv, EquipKey, new Inventory("RestlessEquip", bkg, 2, 3), true);
        Import(player, inv, QuickKey, new Inventory("RestlessQuick", bkg, 1, 3), false);
    }

    private static void Import(Player player, Inventory home, string key, Inventory bag, bool worn)
    {
        if (!player.m_customData.TryGetValue(key, out var raw) || string.IsNullOrEmpty(raw))
            return;
        try
        {
            bag.Load(new ZPackage(raw));
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning("inventory.equipment_slots import: " + e.Message);
            return;
        }

        _busy = true;
        try
        {
            foreach (var item in new List<ItemDrop.ItemData>(bag.GetAllItems()))
            {
                if (item == null)
                    continue;
                bag.RemoveItem(item);
                Vector2i dest;
                if (worn && item.m_shared != null && SlotOf(item.m_shared.m_itemType, out var extra))
                    dest = ExtraPos(home, extra);
                else
                {
                    var free = -1;
                    for (var i = EquipCount; i < ExtraCount; i++)
                    {
                        var pos = ExtraPos(home, i);
                        if (home.GetItemAt(pos.x, pos.y) != null)
                            continue;
                        free = i;
                        break;
                    }

                    if (free < 0)
                    {
                        if (!TryAddVisible(home, item))
                            ItemDrop.DropItem(item, item.m_stack, player.transform.position + Vector3.up,
                                player.transform.rotation);
                        continue;
                    }

                    dest = ExtraPos(home, free);
                }

                if (home.GetItemAt(dest.x, dest.y) == null)
                    home.AddItem(item, item.m_stack, dest.x, dest.y, false);
                else if (!TryAddVisible(home, item))
                    ItemDrop.DropItem(item, item.m_stack, player.transform.position + Vector3.up,
                        player.transform.rotation);
            }

            player.m_customData.Remove(key);
        }
        finally
        {
            _busy = false;
        }
    }

    private static void UseHotkeys(Player player)
    {
        if (Console.IsVisible() || Chat.instance != null && Chat.instance.HasFocus())
            return;
        if (player.IsDead() || player.IsTeleporting())
            return;
        var inv = player.GetInventory();
        if (inv == null || _visibleHeight < 0)
            return;
        TryUse(player, inv, 0, ModConfig.QuickSlot1);
        TryUse(player, inv, 1, ModConfig.QuickSlot2);
        TryUse(player, inv, 2, ModConfig.QuickSlot3);
    }

    private static void TryUse(Player player, Inventory inv, int row, ConfigEntry<KeyboardShortcut> key)
    {
        if (!Tapped(key.Value))
            return;
        var pos = ExtraPos(inv, EquipCount + row);
        var item = inv.GetItemAt(pos.x, pos.y);
        if (item == null)
            return;
        player.UseItem(inv, item, false);
    }

    // BepInEx IsDown() fails if any other keyboard key is held (sprint, move, block).
    // Required modifiers from the bind still have to be down.
    private static bool Tapped(KeyboardShortcut bind)
    {
        if (bind.MainKey == KeyCode.None || !Input.GetKeyDown(bind.MainKey))
            return false;
        foreach (var mod in bind.Modifiers)
        {
            if (!Input.GetKey(mod))
                return false;
        }

        return true;
    }

    private static void Relocate(InventoryGrid grid)
    {
        var inv = grid.GetInventory();
        if (inv == null || _visibleHeight < 0 || grid.m_gridRoot == null)
            return;

        var space = grid.m_elementSpace > 8f ? grid.m_elementSpace : 70f;
        grid.m_gridRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _visibleHeight * space);

        var root = EnsureSlotRoot();
        var dump = EnsureHiddenRoot();
        if (root == null)
            return;

        var w = inv.GetWidth();
        var spare = ExtraRows * w;
        var sample = grid.GetElement(0, 0, w);
        var east = grid.GetElement(Mathf.Min(1, w - 1), 0, w);
        var south = grid.GetElement(0, Mathf.Min(1, _visibleHeight - 1), w);
        if (sample == null)
            return;
        var origin = sample.GetComponent<RectTransform>();
        var stepX = east != null
            ? east.GetComponent<RectTransform>().position.x - origin.position.x
            : space;
        var stepY = south != null
            ? origin.position.y - south.GetComponent<RectTransform>().position.y
            : space;
        if (Mathf.Abs(stepX) < 8f)
            stepX = space;
        if (Mathf.Abs(stepY) < 8f)
            stepY = space;

        List<InventoryElement>? elements;
        try
        {
            elements = Traverse.Create(grid).Field("m_elements").GetValue<List<InventoryElement>>();
        }
        catch
        {
            return;
        }

        if (elements == null || elements.Count < _visibleHeight * w + spare)
            return;

        for (var i = 0; i < spare; i++)
        {
            var pos = ExtraPos(inv, i);
            var element = ElementAt(elements, pos, w) ?? grid.GetElement(pos.x, pos.y, w);
            if (element == null)
                continue;
            var go = element.gameObject;
            if (i >= ExtraCount)
            {
                go.SetActive(false);
                if (dump != null && go.transform.parent != dump)
                    go.transform.SetParent(dump, false);
                continue;
            }

            go.SetActive(true);
            if (go.transform.parent != root)
                go.transform.SetParent(root, false);
            var rt = go.GetComponent<RectTransform>();
            if (element.m_selected != null)
                element.m_selected.SetActive(false);

            int col;
            int row;
            if (i < EquipCount)
            {
                col = w + i % 2;
                row = i / 2;
            }
            else
            {
                col = w + 2;
                row = i - EquipCount;
            }

            rt.pivot = origin.pivot;
            rt.sizeDelta = origin.sizeDelta;
            rt.position = new Vector3(origin.position.x + col * stepX,
                origin.position.y - row * stepY, origin.position.z);
            try
            {
                Traverse.Create(element).Field("m_pos").SetValue(pos);
            }
            catch
            {
                // Position stays whatever UpdateGui wrote.
            }
            var item = inv.GetItemAt(pos.x, pos.y);
            var lit = item is { m_equipped: true };
            var fresh = go.transform.Find("RestlessSlot") == null;
            var locked = SlotLock.Held(pos);
            var material = ModConfig.InventoryScreenEnabled.Value;
            var sig = PaintSig(item, lit, locked) * 31 + (material ? 1 : 0);
            GameObject plate;
            if (fresh || ExtraPaint[i] != sig)
            {
                ExtraPaint[i] = sig;
                plate = RestlessUi.DressSlot(go, element.m_icon, lit, item, Hidden, true, locked, inventory: material);
            }
            else
                plate = go.transform.Find("RestlessSlot")!.gameObject;
            Ours.Add(plate);
            if (fresh)
            {
                StealButton(go, plate);
                QuietCell(go, element.m_icon);
                foreach (var tmp in element.GetComponentsInChildren<TMP_Text>(true))
                {
                    RestlessUi.SilenceTmp(tmp);
                    tmp.gameObject.SetActive(false);
                }
            }

            HideFoodMarkers(go, element.m_icon);
            var tag = i < EquipCount
                ? WornName[Mathf.Clamp(i / 2, 0, 2), Mathf.Clamp(i % 2, 0, 1)]
                : FaceKey(i - EquipCount);
            Caption(go.transform, "RestlessKind", !material && item == null && i < EquipCount ? tag : "", RestlessUi.Muted);
            if (i < EquipCount)
                RestlessUi.InventoryEmpty(plate, "empty-" + WornName[i / 2, i % 2].ToLowerInvariant(), material && item == null);
            else
            {
                Caption(go.transform, "RestlessBind", material ? "" : tag, RestlessUi.Text);
                RestlessUi.InventoryBinding(go.transform, material ? tag : "");
                var bind = go.transform.Find("RestlessInventoryBind");
                if (bind != null) Ours.Add(bind.gameObject);
            }
        }

        PurgeStale(root, inv, elements, w);

        ParkStats(grid, origin, stepX, stepY, w);
    }

    private static long PaintSig(ItemDrop.ItemData? item, bool lit, bool locked)
    {
        unchecked
        {
            long h = lit ? 1 : 0;
            h = h * 31 + (locked ? 1 : 0);
            if (item == null)
                return h;
            h = h * 31 + item.m_stack;
            h = h * 31 + item.m_quality;
            h = h * 31 + (item.m_shared != null ? item.m_shared.m_name.GetHashCode() : 0);
            h = h * 31 + (int)(item.GetDurabilityPercentage() * 20f);
            h = h * 31 + (item.m_equipped ? 1 : 0);
            return h;
        }
    }

    private static void StealButton(GameObject go, GameObject plate)
    {
        var button = go.GetComponent<Button>();
        var plateImg = plate.GetComponent<Image>();
        if (button != null)
        {
            button.transition = Selectable.Transition.None;
            var colors = button.colors;
            colors.colorMultiplier = 1f;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            button.colors = colors;
            if (plateImg != null)
                button.targetGraphic = plateImg;
        }
        var root = go.GetComponent<Image>();
        if (root == null || root == plateImg)
            return;
        root.sprite = null;
        root.color = Color.clear;
        root.enabled = false;
        root.raycastTarget = false;
    }

    private static void QuietCell(GameObject go, Image? icon)
    {
        foreach (var image in go.GetComponentsInChildren<Image>(true))
        {
            if (image == icon || image.transform.name.StartsWith("Restless")
                || image.transform.parent != null && image.transform.parent.name.StartsWith("Restless"))
                continue;
            image.sprite = null;
            image.color = Color.clear;
            image.raycastTarget = false;
            image.enabled = false;
            var n = image.gameObject.name.ToLowerInvariant();
            if (n is "bkg" or "background" or "selected" || n.Contains("equip") || n.Contains("queued")
                || n.Contains("durab") || n.Contains("drop"))
                image.gameObject.SetActive(false);
        }

        foreach (var extra in go.GetComponentsInChildren<Shadow>(true))
            extra.enabled = false;
        foreach (var extra in go.GetComponentsInChildren<Outline>(true))
            extra.enabled = false;
    }

    private static void ParkStats(InventoryGrid grid, RectTransform origin, float stepX, float stepY, int width)
    {
        var gui = InventoryGui.instance;
        var inv = grid.GetInventory();
        if (gui == null || inv == null)
            return;
        var row = Mathf.Max(_visibleHeight - 1, 0);
        if (!SlotBox(grid.GetElement(0, row, width), out _, out var rowTop, out _, out var rowBottom))
        {
            var half = origin.rect.height * 0.5f * origin.lossyScale.y;
            rowTop = origin.position.y - row * stepY + half;
            rowBottom = origin.position.y - row * stepY - half;
        }

        var legs = ExtraPos(inv, 4);
        var last = ExtraPos(inv, EquipCount + QuickCount - 1);
        if (!SlotBox(grid.GetElement(legs.x, legs.y, width), out var extraLeft, out _, out _, out _))
            extraLeft = origin.position.x + width * stepX - origin.rect.width * 0.5f * origin.lossyScale.x;
        if (!SlotBox(grid.GetElement(last.x, last.y, width), out _, out _, out var extraRight, out _))
            extraRight = origin.position.x + (width + 2) * stepX
                + origin.rect.width * 0.5f * origin.lossyScale.x;

        var armor = StatHost(gui.m_armor);
        var weight = StatHost(gui.m_weight);
        QuietStat(armor);
        QuietStat(weight);
        FitStat(armor, extraLeft, rowTop, extraRight, rowBottom, false, 1.25f);
        FitStat(weight, extraLeft, rowTop, extraRight, rowBottom, true, 1.7f);
    }

    private static RectTransform? StatHost(Component? src)
    {
        if (src == null)
            return null;
        var t = src.transform;
        if (t.name is "Armor" or "Weight")
            return t as RectTransform;
        if (t.parent != null && t.parent.name is "Armor" or "Weight")
            return t.parent as RectTransform;
        return t.parent as RectTransform ?? t as RectTransform;
    }

    private static void QuietStat(RectTransform? stat)
    {
        if (stat == null)
            return;
        foreach (var image in stat.GetComponentsInChildren<Image>(true))
        {
            if (image.transform.name.StartsWith("Restless"))
                continue;
            var n = image.gameObject.name.ToLowerInvariant();
            if (n is "bkg" or "background" || image.transform == stat)
            {
                image.color = Color.clear;
                image.enabled = false;
            }
        }
    }

    private static RectTransform FaceOf(RectTransform host)
    {
        var plate = host.Find("RestlessStat") as RectTransform;
        return plate != null ? plate : host;
    }

    private static bool SlotBox(InventoryElement? element, out float left, out float top, out float right,
        out float bottom)
    {
        left = top = right = bottom = 0f;
        if (element == null)
            return false;
        var plate = element.transform.Find("RestlessSlot") as RectTransform
            ?? element.GetComponent<RectTransform>();
        if (plate == null)
            return false;
        var box = new Vector3[4];
        plate.GetWorldCorners(box);
        left = box[0].x;
        bottom = box[0].y;
        right = box[2].x;
        top = box[1].y;
        return right > left && top > bottom;
    }

    private static void FitStat(RectTransform? host, float left, float top, float right, float bottom,
        bool pinRight, float wide)
    {
        if (host == null || top <= bottom)
            return;
        var face = FaceOf(host);
        var sx = Mathf.Max(Mathf.Abs(face.lossyScale.x), 0.01f);
        var sy = Mathf.Max(Mathf.Abs(face.lossyScale.y), 0.01f);
        var height = (top - bottom) / sy;
        var width = height * wide;
        face.anchorMin = face.anchorMax = new Vector2(0.5f, 0.5f);
        face.pivot = pinRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        face.sizeDelta = new Vector2(width, height);
        face.position = new Vector3(pinRight ? right : left, top, face.position.z);
        LayStatRow(host, face);
    }

    private static void LayStatRow(RectTransform host, RectTransform plate)
    {
        Image? icon = null;
        foreach (var image in host.GetComponentsInChildren<Image>(true))
        {
            var n = image.gameObject.name.ToLowerInvariant();
            if (n.Contains("armor_icon") || n.Contains("weight_icon"))
                icon = image;
        }

        Text? label = null;
        foreach (var text in host.GetComponentsInChildren<Text>(true))
        {
            if (text.gameObject.name.StartsWith("Restless_"))
                label = text;
        }

        var box = new Vector3[4];
        plate.GetWorldCorners(box);
        var h = box[1].y - box[0].y;
        var pad = h * 0.14f;
        var iconSize = h * 0.52f;
        var midY = (box[0].y + box[1].y) * 0.5f;
        if (icon != null)
        {
            var irt = icon.rectTransform;
            irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            var isx = Mathf.Max(Mathf.Abs(irt.lossyScale.x), 0.01f);
            irt.sizeDelta = new Vector2(iconSize / isx, iconSize / isx);
            irt.position = new Vector3(box[0].x + pad + iconSize * 0.5f, midY, irt.position.z);
        }

        if (label == null)
            return;
        label.alignment = TextAnchor.MiddleLeft;
        var lrt = label.rectTransform;
        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
        lrt.pivot = new Vector2(0f, 0.5f);
        var textLeft = box[0].x + pad + (icon != null ? iconSize + pad * 0.6f : 0f);
        lrt.position = new Vector3(textLeft, midY, lrt.position.z);
        var lsx = Mathf.Max(Mathf.Abs(lrt.lossyScale.x), 0.01f);
        var lsy = Mathf.Max(Mathf.Abs(lrt.lossyScale.y), 0.01f);
        lrt.sizeDelta = new Vector2(Mathf.Max(8f, (box[2].x - pad - textLeft) / lsx),
            Mathf.Max(8f, h * 0.7f / lsy));
    }

    private static RectTransform? EnsureSlotRoot()
    {
        if (_slotRoot != null)
            return _slotRoot;
        var player = InventoryGui.instance?.m_player;
        if (player == null)
            return null;
        var go = new GameObject("RestlessSlotRoot", typeof(RectTransform));
        go.transform.SetParent(player, false);
        _slotRoot = go.GetComponent<RectTransform>();
        _slotRoot.anchorMin = _slotRoot.anchorMax = new Vector2(0f, 1f);
        _slotRoot.pivot = new Vector2(0f, 1f);
        _slotRoot.localScale = Vector3.one;
        _slotRoot.SetAsLastSibling();
        Ours.Add(go);
        return _slotRoot;
    }

    private static RectTransform? EnsureHiddenRoot()
    {
        if (_hiddenRoot != null)
            return _hiddenRoot;
        var player = InventoryGui.instance?.m_player;
        if (player == null)
            return null;
        var go = new GameObject("RestlessHiddenSlots", typeof(RectTransform));
        go.transform.SetParent(player, false);
        go.SetActive(false);
        _hiddenRoot = go.GetComponent<RectTransform>();
        _hiddenRoot.anchoredPosition = new Vector2(-100000f, 0f);
        Ours.Add(go);
        return _hiddenRoot;
    }

    private static InventoryElement? ElementAt(List<InventoryElement> elements, Vector2i pos, int width)
    {
        var i = pos.y * width + pos.x;
        if (i < 0 || i >= elements.Count)
            return null;
        return elements[i];
    }

    private static void PurgeStale(RectTransform root, Inventory inv, List<InventoryElement> live, int width)
    {
        var keep = new HashSet<GameObject>();
        for (var i = 0; i < ExtraCount; i++)
        {
            var element = ElementAt(live, ExtraPos(inv, i), width);
            if (element != null)
                keep.Add(element.gameObject);
        }

        for (var i = root.childCount - 1; i >= 0; i--)
        {
            var child = root.GetChild(i);
            if (child.GetComponent<InventoryElement>() == null || keep.Contains(child.gameObject))
                continue;
            Object.Destroy(child.gameObject);
        }
    }

    private static void HideFoodMarkers(GameObject go, Image? icon)
    {
        foreach (var image in go.GetComponentsInChildren<Image>(true))
        {
            if (image == icon || image.transform.name.StartsWith("Restless")
                || image.transform.parent != null && image.transform.parent.name.StartsWith("Restless"))
                continue;
            var n = image.gameObject.name.ToLowerInvariant();
            if (!n.Contains("food") && !n.Contains("fork") && !n.Contains("eat"))
                continue;
            image.enabled = false;
            image.sprite = null;
            image.color = Color.clear;
            image.gameObject.SetActive(false);
        }
    }

    private static void Caption(Transform cell, string name, string copy, Color color,
        TextAnchor align = TextAnchor.LowerCenter)
    {
        var node = cell.Find(name);
        Text face;
        if (node == null)
        {
            face = RestlessUi.Label(cell, copy, RestlessUi.HudMeta, color, align);
            face.gameObject.name = name;
            RestlessUi.Stretch(face.gameObject, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, 2f));
            Ours.Add(face.gameObject);
        }
        else
            face = node.GetComponent<Text>();

        if (face == null)
            return;
        face.text = copy;
        face.color = color;
        face.gameObject.SetActive(copy.Length > 0);
    }

    private static void PaintHud()
    {
        if (!ModConfig.ExtraSlotsEnabled.Value)
        {
            if (_hud != null)
                _hud.SetActive(false);
            _hudRight = 0f;
            return;
        }

        var player = Player.m_localPlayer;
        var hud = global::Hud.instance?.m_rootObject;
        var inv = player?.GetInventory();
        if (player == null || hud == null || !hud.activeInHierarchy || inv == null || _visibleHeight < 0
            || !Hotbar.TryPitch(out var step, out var lastX, out var midY)
            || !Hotbar.TrySlotLocal(out var cellSize, out var plateSize))
        {
            if (_hud != null)
                _hud.SetActive(false);
            return;
        }

        EnsureHud(hud.transform);
        _hud!.SetActive(true);
        for (var i = 0; i < HudSlots.Length; i++)
        {
            var cell = HudSlots[i];
            var pos = ExtraPos(inv, EquipCount + i);
            var item = inv.GetItemAt(pos.x, pos.y);
            var lit = item != null && (item.m_equipped || player.GetRightItem() == item || player.GetLeftItem() == item);
            var rt = cell.Go.GetComponent<RectTransform>();
            rt.sizeDelta = cellSize;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.position = new Vector3(lastX + (i + 1) * step, midY, rt.position.z);
            if (cell.Icon != null)
            {
                cell.Icon.sprite = item?.GetIcon();
                cell.Icon.enabled = item?.GetIcon() != null;
                cell.Icon.color = Color.white;
            }

            RestlessUi.DressSlot(cell.Go, cell.Icon, lit, item, Hidden, false, SlotLock.Held(pos));
            var dressed = cell.Go.transform.Find("RestlessSlot") as RectTransform;
            if (dressed != null)
            {
                // Match hotbar DressSlot: idle +8, held +14. Sampled plateSize is idle.
                var grow = lit ? 6f : 0f;
                dressed.sizeDelta = plateSize + new Vector2(grow, grow);
            }
            var key = cell.Go.transform.Find("RestlessKey");
            if (key != null)
            {
                key.GetComponent<Image>().color = lit
                    ? Color.Lerp(RestlessUi.ChipTint, RestlessUi.Accent, 0.45f)
                    : RestlessUi.ChipTint;
            }

            if (cell.Key != null)
            {
                cell.Key.text = FaceKey(i);
                cell.Key.color = lit ? RestlessUi.Accent : RestlessUi.Text;
            }
        }

        _hudRight = lastX + 3f * step + Mathf.Abs(step) * 0.5f;
    }

    private static void EnsureHud(Transform parent)
    {
        if (_hud != null)
            return;
        _hud = RestlessUi.Node(parent, "RestlessQuickHud");
        Ours.Add(_hud);
        for (var i = 0; i < HudSlots.Length; i++)
        {
            var go = RestlessUi.Node(_hud.transform, "slot" + i);
            var icon = RestlessUi.Graphic(go.transform, "icon", Color.white, false).GetComponent<Image>();
            icon.preserveAspect = true;
            RestlessUi.Stretch(icon.gameObject, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));
            var chip = RestlessUi.Chip(go.transform, "RestlessKey");
            RestlessUi.Pin(chip, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f),
                new Vector2(22f, 18f));
            var key = RestlessUi.Label(chip.transform, FaceKey(i), RestlessUi.HudMeta, RestlessUi.Text,
                TextAnchor.MiddleCenter);
            RestlessUi.Stretch(key.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            HudSlots[i] = new Cell { Go = go, Icon = icon, Key = key };
            Ours.Add(go);
            Ours.Add(chip);
        }
    }

    private static string FaceKey(int row)
    {
        var key = row switch
        {
            0 => ModConfig.QuickSlot1.Value,
            1 => ModConfig.QuickSlot2.Value,
            _ => ModConfig.QuickSlot3.Value
        };
        var name = key.MainKey.ToString();
        return name.StartsWith("Alpha") ? name.Substring(5) : name;
    }

    private static void TearDownUi()
    {
        foreach (var go in Ours)
        {
            if (go != null)
                Object.Destroy(go);
        }

        Ours.Clear();
        foreach (var behaviour in Hidden)
        {
            if (behaviour != null)
                behaviour.enabled = true;
        }

        Hidden.Clear();
        Array.Clear(ExtraPaint, 0, ExtraPaint.Length);
        _slotRoot = null;
        _hiddenRoot = null;
        _hud = null;
        _hudRight = 0f;
        Array.Clear(HudSlots, 0, HudSlots.Length);
    }
}

