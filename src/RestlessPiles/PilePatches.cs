using HarmonyLib;
using RestlessQoL.Storage;
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
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Destroy), typeof(HitData), typeof(bool))]
    private static void BeforeDestroy(WearNTear __instance, ref bool blockDrop)
    {
        var box = __instance != null ? __instance.GetComponent<PileBox>() : null;
        if (box == null)
            return;
        var refundBuild = !blockDrop;
        box.Spill(refundBuild);
        blockDrop = true;
    }

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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(RestlessQoL.Storage.GroundVacuum), nameof(RestlessQoL.Storage.GroundVacuum.Tick))]
    private static void BeforeVacuum()
    {
        if (PileConfig.On)
            PileBag.VacuumNearby(Player.m_localPlayer);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NearbyStorage), nameof(NearbyStorage.Count))]
    private static void AfterCount(string sharedName, ref int __result)
    {
        if (PileConfig.On)
            __result += PileBag.CountNearby(sharedName);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NearbyStorage), nameof(NearbyStorage.TryConsume))]
    private static void AfterConsume(string sharedName, int amount, ref int __result)
    {
        if (!PileConfig.On || __result >= amount)
            return;
        __result += PileBag.ConsumeNearby(sharedName, amount - __result);
    }
}
