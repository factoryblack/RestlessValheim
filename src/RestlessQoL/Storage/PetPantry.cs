using HarmonyLib;
using RestlessQoL.Core;
using UnityEngine;

namespace RestlessQoL.Storage;

public sealed class PetPantry : FeatureModule
{
    public override string Id => "storage.petpantry";
    public override bool Enabled => true;

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateConsumeItem))]
        private static void UpdateConsumeItem(MonsterAI __instance, Humanoid humanoid, ref bool __result)
        {
            if (__result || !ModConfig.PetPantryEnabled.Value || !ModConfig.StorageEnabled.Value)
                return;
            if (__instance.m_consumeSearchTimer > 0.0001f)
                return;
            if (__instance.m_nview == null || !__instance.m_nview.IsOwner())
                return;
            var tame = __instance.m_tamable;
            if (tame == null || !tame.IsTamed() || !tame.IsHungry())
                return;
            if (__instance.m_consumeItems == null || __instance.m_consumeItems.Count == 0)
                return;

            // Dedicated has no local player. playerId 0 skips access, same as stations.
            var playerId = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0L;
            var origin = __instance.transform.position;
            var range = ModConfig.StorageRange.Value;

            foreach (var food in __instance.m_consumeItems)
            {
                if (food?.m_itemData?.m_shared == null)
                    continue;
                if (NearbyStorage.TryConsumeAround(origin, range, playerId, food.m_itemData.m_shared.m_name, 1) <= 0)
                    continue;

                __instance.m_onConsumedItem?.Invoke(food);
                humanoid.m_consumeItemEffects?.Create(origin, Quaternion.identity, null, 1f, -1, default(ZDOID));
                __instance.m_animator?.SetTrigger("consume");
                __result = true;
                return;
            }
        }
    }
}
