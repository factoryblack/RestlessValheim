using HarmonyLib;
using UnityEngine;

namespace RestlessPiles;

[HarmonyPatch]
internal static class PilePatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    private static void AfterScene(ZNetScene __instance)
    {
        if (__instance?.m_prefabs == null)
            return;
        foreach (var prefab in __instance.m_prefabs)
            Pile.TryAttach(prefab);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Piece), "Awake")]
    private static void AfterPiece(Piece __instance) => Pile.TryAttach(__instance.gameObject);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Destroy))]
    private static void BeforeDestroy(WearNTear __instance) => Pile.Spill(__instance);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Remove))]
    private static void BeforeRemove(WearNTear __instance) => Pile.Spill(__instance);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HoverText), nameof(HoverText.GetHoverText))]
    private static void AfterHoverLabel(HoverText __instance, ref string __result)
    {
        var box = __instance.GetComponentInParent<PileBox>();
        if (box != null && PileConfig.On)
            __result = box.GetHoverText();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(RestlessQoL.Storage.QuickStack), nameof(RestlessQoL.Storage.QuickStack.Run))]
    private static void BeforeQuickStack(Player player)
    {
        if (PileConfig.On)
            PileBag.DumpNearby(player);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RestlessQoL.Storage.GroundVacuum), nameof(RestlessQoL.Storage.GroundVacuum.Tick))]
    private static void AfterVacuum()
    {
        if (PileConfig.On)
            PileBag.VacuumNearby(Player.m_localPlayer);
    }
}
