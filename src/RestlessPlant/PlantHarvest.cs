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
    }

    private static void Bulk(Pickable picked, Humanoid character)
    {
        var name = Utils.GetPrefabName(picked.gameObject);
        var range = PlantConfig.HarvestRange.Value;
        var origin = picked.transform.position;
        _busy = true;
        try
        {
            foreach (var other in Object.FindObjectsByType<Pickable>(FindObjectsSortMode.None))
            {
                if (other == null || other == picked || !other.CanBePicked())
                    continue;
                if (Utils.GetPrefabName(other.gameObject) != name || !Ours(other))
                    continue;
                if ((other.transform.position - origin).sqrMagnitude > range * range)
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
