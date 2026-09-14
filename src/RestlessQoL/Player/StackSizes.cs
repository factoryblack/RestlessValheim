using System.Collections.Generic;
using HarmonyLib;
using RestlessQoL.Core;
using UnityEngine;

namespace RestlessQoL.PlayerTweaks;

// Shared m_maxStackSize only. Remember vanilla so the F8 slider cannot
// compound. Items that do not stack (max 1) stay unique.
public sealed class StackSizes : FeatureModule
{
    public override string Id => "inventory.stack_size";
    public override bool Enabled => true;

    private static readonly Dictionary<string, int> Vanilla = new();

    protected override void OnLoaded()
    {
        ModConfig.StackSizeMultiplier.SettingChanged += (_, _) => Apply();
        Apply();
    }

    private static void Apply()
    {
        var db = ObjectDB.instance;
        if (db?.m_items == null)
            return;
        var mult = Mathf.Max(1f, ModConfig.StackSizeMultiplier.Value);
        foreach (var prefab in db.m_items)
        {
            if (prefab == null)
                continue;
            var shared = prefab.GetComponent<ItemDrop>()?.m_itemData?.m_shared;
            if (shared == null)
                continue;
            if (!Vanilla.ContainsKey(prefab.name))
                Vanilla[prefab.name] = shared.m_maxStackSize;
            var cap = Vanilla[prefab.name];
            if (cap <= 1)
                continue;
            shared.m_maxStackSize = Mathf.Max(cap, Mathf.RoundToInt(cap * mult));
        }
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        private static void AfterAwake() => Apply();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        private static void AfterCopy() => Apply();
    }
}
