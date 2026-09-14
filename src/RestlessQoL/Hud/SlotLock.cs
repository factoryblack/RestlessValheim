using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using RestlessQoL.Core;
using UnityEngine;

namespace RestlessQoL.HudTweaks;

// Vanilla inventory has no middle-click lock (MMB is build favorites / map).
// Toggle a player-bag cell; Stack / ` / restock leave it alone. No bag sort.
public sealed class SlotLock : FeatureModule
{
    public override string Id => "inventory.slot_lock";
    public override bool Enabled => true;
    public override bool TickInMenus => true;

    private const string Key = "restless.inv.lock";
    private static readonly HashSet<long> Locks = new();
    private static Player? _bound;
    private static bool _stacking;

    public override void Tick()
    {
        Bind(Player.m_localPlayer);
        if (!ModConfig.SlotLockEnabled.Value)
            return;
        if (!InventoryGui.IsVisible() || Console.IsVisible())
            return;
        if (Chat.instance != null && Chat.instance.HasFocus())
            return;
        if (!Input.GetMouseButtonDown(2))
            return;
        var grid = InventoryGui.instance?.m_playerGrid;
        if (grid == null)
            return;
        var el = grid.GetHoveredElement();
        if (el == null)
            return;
        Flip(el.Position);
    }

    public static bool Held(Vector2i pos) =>
        ModConfig.SlotLockEnabled.Value && Locks.Contains(Pack(pos));

    public static bool Held(ItemDrop.ItemData? item) =>
        item != null && Held(item.m_gridPos);

    private static void Flip(Vector2i pos)
    {
        var key = Pack(pos);
        if (!Locks.Remove(key))
            Locks.Add(key);
        Save(Player.m_localPlayer);
    }

    private static long Pack(Vector2i pos) => ((long)pos.y << 32) | (uint)pos.x;

    private static void Bind(Player? player)
    {
        if (player == _bound)
            return;
        _bound = player;
        Locks.Clear();
        if (player?.m_customData == null || !player.m_customData.TryGetValue(Key, out var raw)
            || string.IsNullOrEmpty(raw))
            return;
        foreach (var part in raw.Split(','))
        {
            var bits = part.Split(':');
            if (bits.Length != 2 || !int.TryParse(bits[0], out var x) || !int.TryParse(bits[1], out var y))
                continue;
            Locks.Add(Pack(new Vector2i(x, y)));
        }
    }

    private static void Save(Player? player)
    {
        if (player?.m_customData == null)
            return;
        if (Locks.Count == 0)
        {
            player.m_customData.Remove(Key);
            return;
        }

        var sb = new StringBuilder();
        foreach (var key in Locks)
        {
            if (sb.Length > 0)
                sb.Append(',');
            sb.Append((int)key).Append(':').Append((int)(key >> 32));
        }

        player.m_customData[Key] = sb.ToString();
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.StackAll))]
        private static void BeforeStackAll() => _stacking = true;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.StackAll))]
        private static void AfterStackAll() => _stacking = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData))]
        private static bool BlockLocked(ItemDrop.ItemData item, ref bool __result)
        {
            if (!_stacking || !Held(item))
                return true;
            __result = false;
            return false;
        }
    }
}
