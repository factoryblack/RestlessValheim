using HarmonyLib;
using UnityEngine;

namespace RestlessPlant;

internal static class PlantGrow
{
    public static bool Free => PlantConfig.On && PlantConfig.GrowAnywhere.Value;

    private static bool _till;
    private static bool _ground;
    private static bool _need;

    public static void Relax(GameObject? ghost)
    {
        if (!Free || ghost == null)
            return;
        foreach (var piece in ghost.GetComponentsInChildren<Piece>(true))
        {
            piece.m_cultivatedGroundOnly = false;
            piece.m_groundOnly = false;
            piece.m_noInWater = false;
        }

        foreach (var plant in ghost.GetComponentsInChildren<Plant>(true))
            plant.m_needCultivatedGround = false;
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Plant), nameof(Plant.GetStatus))]
        private static bool AnyStatus(ref Plant.Status __result)
        {
            if (!Free)
                return true;
            __result = Plant.Status.Healthy;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
        private static void BeforeGhost(Player __instance)
        {
            if (!PlantGrid.Cultivating(__instance))
                return;
            Relax(__instance.m_placementGhost);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
        private static void Soften(Piece piece)
        {
            if (!Free || piece == null)
                return;
            _till = piece.m_cultivatedGroundOnly;
            _ground = piece.m_groundOnly;
            piece.m_cultivatedGroundOnly = false;
            piece.m_groundOnly = false;
            var plant = piece.GetComponent<Plant>();
            if (plant == null)
                return;
            _need = plant.m_needCultivatedGround;
            plant.m_needCultivatedGround = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
        private static void Harden(Piece piece)
        {
            if (!Free || piece == null)
                return;
            piece.m_cultivatedGroundOnly = _till;
            piece.m_groundOnly = _ground;
            var plant = piece.GetComponent<Plant>();
            if (plant == null)
                return;
            plant.m_needCultivatedGround = _need;
        }

        // The placed copy used to keep a cleared biome from the piece table.
        // Only the planter runs the health check, so only they saw
        // "Can't grow in this environment" while everyone else saw a live crop.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Plant), nameof(Plant.UpdateHealth))]
        private static void KeepAlive(Plant __instance)
        {
            if (Free)
            {
                __instance.m_status = Plant.Status.Healthy;
                return;
            }

            if (__instance.m_status != Plant.Status.WrongBiome)
                return;
            if (__instance.m_biome == 0)
            {
                __instance.m_status = Plant.Status.Healthy;
                return;
            }

            var map = Heightmap.FindHeightmap(__instance.transform.position);
            if (map == null || map.GetBiome(__instance.transform.position) == Heightmap.Biome.None)
                __instance.m_status = Plant.Status.Healthy;
        }
    }
}
