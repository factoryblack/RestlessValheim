using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace RestlessPlant;

internal static class PlantHarvest
{
    private static Piece? _remembered;
    private static bool _busy;

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
        var piece = pickable.GetComponentInParent<Piece>();
        return piece != null && piece.GetCreator() != 0L;
    }

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

            var piece = __instance.GetComponentInParent<Piece>();
            if (piece != null)
                Remember(piece);

            if (PlantConfig.BulkHarvest.Value)
                Bulk(__instance, character);

            // Replant only one-shot crops. Forage and bushes regrow via Pickable.m_respawnTimeMinutes.
            if (PlantConfig.Replant.Value && __instance.m_respawnTimeMinutes <= 0f && piece?.GetComponent<Plant>() != null)
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
                if (other == picked)
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
                var piece = other.GetComponentInParent<Piece>();
                var oneShot = other.m_respawnTimeMinutes <= 0f;
                other.Interact(character, false, false);
                if (PlantConfig.Replant.Value && oneShot && piece?.GetComponent<Plant>() != null)
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
        player.PlacePiece(seed, pos, rot, false, false);
    }
}
