using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace RestlessPlant;

[HarmonyPatch]
internal static class PlantHarvest
{
    private static Piece? _remembered;
    private static bool _busy;
    private static ZNetScene? _scene;
    private static float _nextCleanup;
    private static readonly Dictionary<string, Piece> Seeds = new(StringComparer.Ordinal);
    private const string SeedKey = "RestlessReplant";

    internal static void Tick()
    {
        if (_scene != ZNetScene.instance)
        {
            _scene = ZNetScene.instance;
            PickedDrops.Clear();
            Seeds.Clear();
            _remembered = null;
        }
        if (Time.unscaledTime < _nextCleanup) return;
        _nextCleanup = Time.unscaledTime + 2f;
        var gone = new List<ZDOID>();
        foreach (var pair in PickedDrops)
            if (pair.Value == null || pair.Value.m_nview == null || !pair.Value.m_nview.IsValid()) gone.Add(pair.Key);
        foreach (var id in gone) PickedDrops.Remove(id);
    }

    private static Piece? SeedFor(Pickable crop)
    {
        var name = Utils.GetPrefabName(crop.gameObject);
        var stamped = crop.m_nview != null && crop.m_nview.IsValid() ? crop.m_nview.GetZDO().GetString(SeedKey) : "";
        if (!string.IsNullOrEmpty(stamped))
        {
            var prefab = ZNetScene.instance?.GetPrefab(stamped);
            if (prefab != null) return prefab.GetComponent<Piece>();
        }
        if (Seeds.TryGetValue(name, out var seed)) return seed;
        if (ZNetScene.instance == null) return null;
        // Older crops only have the grown prefab. Discover its actual sapling,
        // rather than using the last selected crop and replanting the wrong kind.
        foreach (var prefab in ZNetScene.instance.m_prefabs)
        {
            var plant = prefab != null ? prefab.GetComponent<Plant>() : null;
            var piece = prefab != null ? prefab.GetComponent<Piece>() : null;
            if (plant == null || piece == null || plant.m_grownPrefabs == null) continue;
            foreach (var grown in plant.m_grownPrefabs)
                if (grown != null && Utils.GetPrefabName(grown) == name) { Seeds[name] = piece; return piece; }
        }
        return null;
    }

    public static void Remember(Piece? piece)
    {
        if (piece != null)
            _remembered = piece;
    }

    public static Piece? Hint() => PlantConfig.Replant.Value ? _remembered : null;

    public static bool Grown(Pickable pickable) => Ours(pickable);

    private static bool Ours(Pickable pickable)
    {
        if (pickable == null)
            return false;
        var name = Utils.GetPrefabName(pickable.gameObject);
        if (name.StartsWith("Restless_"))
            return true;
        if (CropNames.Contains(name))
            return true;
        var piece = pickable.GetComponentInParent<Piece>();
        if (piece != null && piece.GetCreator() != 0L)
            return true;
        var view = pickable.m_nview;
        return view != null && view.IsValid() && view.GetZDO().GetLong(PlantedKey, 0L) != 0L;
    }

    // A grown carrot replaces the sapling and does not keep its Piece.
    // These never spawn wild, so a field planted before the stamp still harvests.
    private static readonly HashSet<string> CropNames = new(StringComparer.Ordinal)
    {
        "Pickable_Carrot", "Pickable_SeedCarrot",
        "Pickable_Turnip", "Pickable_SeedTurnip",
        "Pickable_Onion", "Pickable_SeedOnion",
        "Pickable_Barley", "Pickable_Flax"
    };

    private const string PlantedKey = "RestlessPlanted";

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
        private static void AfterPick(Pickable __instance, Humanoid character, bool __result)
        {
            if (!__result || !PlantConfig.On || _busy || character != Player.m_localPlayer)
                return;
            if (!Ours(__instance))
                return;

            var piece = SeedFor(__instance);
            if (piece != null)
                Remember(piece);

            if (PlantConfig.BulkHarvest.Value)
                Bulk(__instance, character);

            // Replant only one-shot crops. Forage and bushes regrow via Pickable.m_respawnTimeMinutes.
            if (PlantConfig.Replant.Value && __instance.m_respawnTimeMinutes <= 0f && piece != null)
                Replant(character as Player, __instance.transform.position, __instance.transform.rotation, piece);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Beehive), nameof(Beehive.Interact))]
        private static void AfterHive(Beehive __instance, Humanoid character, bool __result)
        {
            if (!__result || !PlantConfig.On || !PlantConfig.BulkBeehives.Value || _busy)
                return;
            if (character != Player.m_localPlayer)
                return;
            BulkHives(__instance, character);
        }

        // Grow drops the sapling. Mark the ripe pickable so area harvest can see it.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Plant), nameof(Plant.Grow))]
        private static void StampGrown(Plant __instance, GameObject __result)
        {
            if (__result == null)
                return;
            var view = __result.GetComponent<ZNetView>();
            if (view == null || !view.IsValid() || !view.IsOwner())
                return;
            var piece = __instance.GetComponent<Piece>();
            var creator = piece != null ? piece.GetCreator() : 0L;
            if (creator == 0L)
                return;
            view.GetZDO().Set(PlantedKey, creator);
            view.GetZDO().Set(SeedKey, Utils.GetPrefabName(__instance.gameObject));
        }

        // The nest reuses the beehive script. If its labels or drop were left
        // on honey, it reads as a hive. Feathers and the nest lines are already
        // in the game.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Beehive), nameof(Beehive.Awake))]
        private static void NameNest(Beehive __instance)
        {
            if (!Nest(__instance))
                return;
            if (__instance.m_name == "$piece_beehive" || string.IsNullOrEmpty(__instance.m_name))
                __instance.m_name = "$piece_birdnest";
            if (__instance.m_extractText == "$piece_beehive_extract")
                __instance.m_extractText = "$piece_birdnest_extract";
            if (__instance.m_checkText == "$piece_beehive_check")
                __instance.m_checkText = "$piece_birdnest_check";
            if (__instance.m_areaText == "$piece_beehive_area")
                __instance.m_areaText = "$piece_birdnest_area";
            if (__instance.m_freespaceText == "$piece_beehive_freespace")
                __instance.m_freespaceText = "$piece_birdnest_freespace";
            if (__instance.m_sleepText == "$piece_beehive_sleep")
                __instance.m_sleepText = "$piece_birdnest_sleep";
            if (__instance.m_happyText == "$piece_beehive_happy")
                __instance.m_happyText = "$piece_birdnest_happy";
            var shared = __instance.m_honeyItem != null ? __instance.m_honeyItem.m_itemData?.m_shared?.m_name : null;
            if (shared != "$item_honey" && shared != null)
                return;
            var feather = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab("Feathers") : null;
            var drop = feather != null ? feather.GetComponent<ItemDrop>() : null;
            if (drop != null)
                __instance.m_honeyItem = drop;
        }
    }

    private static readonly Dictionary<ZDOID, ItemDrop> PickedDrops = new();
    private static ZDOID _picking;
    private static ItemDrop? _pickingDrop;

    // A stack of 1 merges into what you already hold, so the drop's own item
    // never sits in the inventory and the "already picked" check misses it.
    // If the ground object has not actually gone yet, auto-pickup adds it
    // again. That keeps going for the player at the nest until the host's
    // copy of the drop is created and the remove sticks.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Pickup))]
    private static bool OneDrop(GameObject go, ref bool __result)
    {
        _picking = ZDOID.None;
        _pickingDrop = null;
        if (!PlantConfig.On) return true;
        var drop = go != null ? go.GetComponent<ItemDrop>() : null;
        if (drop == null || drop.m_nview == null || !drop.m_nview.IsValid())
            return true;
        var id = drop.m_nview.GetZDO().m_uid;
        if (id.IsNone())
            return true;
        if (PickedDrops.ContainsKey(id))
        {
            __result = false;
            return false;
        }

        _picking = id;
        _pickingDrop = drop;
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Pickup))]
    private static void RememberDrop(bool __result)
    {
        if (__result && !_picking.IsNone() && _pickingDrop != null)
            PickedDrops[_picking] = _pickingDrop;
        _picking = ZDOID.None;
        _pickingDrop = null;
    }

    // Owner id 0 makes the extract run on every peer that has the nest.
    // The client then keeps a private pile. Only the owner spawns, and if
    // nobody owns it the host takes it.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Beehive), nameof(Beehive.RPC_Extract))]
    private static bool OneExtract(Beehive __instance)
    {
        var view = __instance.m_nview;
        if (view == null || !view.IsValid())
            return false;
        if (view.IsOwner())
            return true;
        if (ZNet.instance != null && ZNet.instance.IsServer())
        {
            view.ClaimOwnership();
            return true;
        }

        return false;
    }

    // A missing timestamp is year 1. The next tick then fills the nest.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Beehive), nameof(Beehive.GetTimeSinceLastUpdate))]
    private static void Clock(Beehive __instance)
    {
        var view = __instance.m_nview;
        if (view == null || !view.IsValid() || ZNet.instance == null)
            return;
        var zdo = view.GetZDO();
        if (zdo.GetLong(ZDOVars.s_lastTime, 0L) > 1L)
            return;
        zdo.Set(ZDOVars.s_lastTime, ZNet.instance.GetTime().Ticks);
    }

    private static void BulkHives(Beehive picked, Humanoid character)
    {
        var range = PlantConfig.HarvestRange.Value;
        var origin = picked.transform.position;
        _busy = true;
        try
        {
            foreach (var other in Nearby<Beehive>(origin, range))
            {
                if (other == picked || !SameHive(picked, other))
                    continue;
                other.Interact(character, false, false);
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private static void Bulk(Pickable picked, Humanoid character)
    {
        var range = PlantConfig.HarvestRange.Value;
        var origin = picked.transform.position;
        _busy = true;
        try
        {
            foreach (var other in Nearby<Pickable>(origin, range))
            {
                if (other == picked || !other.CanBePicked())
                    continue;
                if (!Ours(other))
                    continue;
                var pos = other.transform.position;
                var rot = other.transform.rotation;
                var piece = SeedFor(other);
                var oneShot = other.m_respawnTimeMinutes <= 0f;
                other.Interact(character, false, false);
                if (PlantConfig.Replant.Value && oneShot && piece != null)
                    Replant(character as Player, pos, rot, piece);
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private static readonly HashSet<int> Seen = new();

    private static IEnumerable<T> Nearby<T>(Vector3 origin, float range) where T : Component
    {
        Seen.Clear();
        var hits = Physics.OverlapSphere(origin, range, ~0, QueryTriggerInteraction.Collide);
        foreach (var hit in hits)
        {
            if (hit == null)
                continue;
            var item = hit.GetComponentInParent<T>();
            if (item == null || !Seen.Add(item.GetInstanceID()))
                continue;
            yield return item;
        }
    }

    private static void Replant(Player? player, Vector3 pos, Quaternion rot, Piece? piece)
    {
        var seed = piece != null ? piece : _remembered;
        if (player == null || seed == null)
            return;
        if (!player.HaveRequirements(seed, Player.RequirementMode.CanBuild))
            return;
        if (!player.m_noPlacementCost && (ZoneSystem.instance == null || !ZoneSystem.instance.GetGlobalKey(seed.FreeBuildKey())))
            player.ConsumeResources(seed.m_resources, 0);
        player.PlacePiece(seed, pos, rot, false, false);
    }

    private static bool Nest(Beehive hive)
    {
        if (hive == null)
            return false;
        var prefab = Utils.GetPrefabName(hive.gameObject) ?? "";
        var label = hive.m_name ?? "";
        return prefab.IndexOf("birdnest", System.StringComparison.OrdinalIgnoreCase) >= 0
            || label.IndexOf("birdnest", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool SameHive(Beehive a, Beehive b)
    {
        if (Nest(a) != Nest(b))
            return false;
        return Utils.GetPrefabName(a.gameObject) == Utils.GetPrefabName(b.gameObject);
    }
}
