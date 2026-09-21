using HarmonyLib;
using UnityEngine;

namespace RestlessPlant;

internal static class PlantGrow
{
    public static bool Free => PlantConfig.On && PlantConfig.GrowAnywhere.Value;

    private static bool _till;
    private static bool _ground;
    private static bool _need;
    private static Heightmap.Biome _biome;

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
        {
            plant.m_needCultivatedGround = false;
            plant.m_biome = 0;
        }
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
            _biome = plant.m_biome;
            plant.m_needCultivatedGround = false;
            plant.m_biome = 0;
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
            plant.m_biome = _biome;
        }
    }
}
