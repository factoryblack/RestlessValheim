using System;
using System.Collections.Generic;
using HarmonyLib;
using RestlessQoL.Core;
using RestlessQoL.Storage;

namespace RestlessQoL.Production;

public sealed class StationPull : FeatureModule
{
    public override string Id => "production.pull";
    public override bool Enabled => true;

    private static bool On =>
        ModConfig.StorageEnabled.Value && ModConfig.StationPullEnabled.Value;

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnAddOre))]
        private static void BeginOre(ref ItemDrop.ItemData item, ref IDisposable __state)
        {
            if (On)
                __state = NearbyStorage.BeginDirectTake();
            NearbyStorage.EnsureDropPrefab(item);
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnAddOre))]
        private static void EndOre(IDisposable __state) => __state?.Dispose();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.OnUseItem))]
        private static void BeginCook(ref ItemDrop.ItemData item, ref IDisposable __state)
        {
            if (On)
                __state = NearbyStorage.BeginDirectTake();
            NearbyStorage.EnsureDropPrefab(item);
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.OnUseItem))]
        private static void EndCook(IDisposable __state) => __state?.Dispose();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.AddItem))]
        private static void BeginFerment(ref ItemDrop.ItemData item, ref IDisposable __state)
        {
            if (On)
                __state = NearbyStorage.BeginDirectTake();
            NearbyStorage.EnsureDropPrefab(item);
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.AddItem))]
        private static void EndFerment(IDisposable __state) => __state?.Dispose();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Smelter), nameof(Smelter.FindCookableItem))]
        private static void SmelterFind(Smelter __instance, Inventory inventory, ref ItemDrop.ItemData __result)
        {
            TryFind(inventory, From(__instance.m_conversion, c => c.m_from), ref __result);
            NearbyStorage.EnsureDropPrefab(__result);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.FindCookableItem))]
        private static void CookFind(CookingStation __instance, Inventory inventory, ref ItemDrop.ItemData __result)
        {
            TryFind(inventory, From(__instance.m_conversion, c => c.m_from), ref __result);
            NearbyStorage.EnsureDropPrefab(__result);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.FindCookableItem))]
        private static void FermentFind(Fermenter __instance, Inventory inventory, ref ItemDrop.ItemData __result)
        {
            TryFind(inventory, From(__instance.m_conversion, c => c.m_from), ref __result);
            NearbyStorage.EnsureDropPrefab(__result);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveOneItem))]
        private static void RemoveOneItem(Inventory __instance, ItemDrop.ItemData item, ref bool __result)
        {
            if (__result || !On || !NearbyStorage.DirectTake || !NearbyStorage.IsLocalPlayerInventory(__instance) || item?.m_shared == null)
                return;
            if (NearbyStorage.TryConsume(item.m_shared.m_name, 1, honorLeaveOne: false) > 0)
                __result = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), typeof(ItemDrop.ItemData), typeof(int))]
        private static void RemoveItemData(Inventory __instance, ItemDrop.ItemData item, int amount, ref bool __result)
        {
            if (__result || !On || !NearbyStorage.DirectTake || !NearbyStorage.IsLocalPlayerInventory(__instance) || item?.m_shared == null || amount <= 0)
                return;
            if (NearbyStorage.TryConsume(item.m_shared.m_name, amount, honorLeaveOne: false) == amount)
                __result = true;
        }

        private static void TryFind(Inventory inventory, IEnumerable<ItemDrop> sources, ref ItemDrop.ItemData result)
        {
            if (result != null || !On || !NearbyStorage.IsLocalPlayerInventory(inventory))
                return;
            foreach (var from in sources)
            {
                if (NearbyStorage.TryCloneIfPresent(from, out var item))
                {
                    result = item;
                    return;
                }
            }
        }

        private static IEnumerable<ItemDrop> From<T>(IEnumerable<T> conversions, Func<T, ItemDrop> from) =>
            conversions == null ? Array.Empty<ItemDrop>() : Enumerate(conversions, from);

        private static IEnumerable<ItemDrop> Enumerate<T>(IEnumerable<T> conversions, Func<T, ItemDrop> from)
        {
            foreach (var conversion in conversions)
                yield return from(conversion);
        }
    }
}
