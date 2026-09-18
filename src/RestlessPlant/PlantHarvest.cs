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
        if (CropBook.IsOurs(name))
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

            if (PlantConfig.BulkHarvest.Value)
                Bulk(__instance, character);

            if (PlantConfig.Replant.Value && __instance.m_respawnTimeMinutes <= 0f)
                Replant(character as Player, __instance.transform.position, __instance.transform.rotation);
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
                other.Interact(character, false, false);
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private static void Replant(Player? player, Vector3 pos, Quaternion rot)
    {
        if (player == null || _remembered == null)
            return;
        if (!player.HaveRequirements(_remembered, Player.RequirementMode.CanBuild))
            return;
        player.PlacePiece(_remembered, pos, rot, false, false);
    }
}
