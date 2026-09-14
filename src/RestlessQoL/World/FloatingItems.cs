using System;
using System.Collections.Generic;
using HarmonyLib;
using RestlessQoL.Core;
using UnityEngine;

namespace RestlessQoL.World;

public sealed class FloatingItems : FeatureModule
{
    public override string Id => "world.floating";
    public override bool Enabled => true;

    private static HashSet<string>? _sink;
    private static HashSet<string>? _extra;
    private static Floating? _template;

    protected override void OnLoaded()
    {
        ModConfig.FloatingSinkList.SettingChanged += Invalidate;
        ModConfig.FloatingExtraList.SettingChanged += Invalidate;
    }

    private static void Invalidate(object sender, EventArgs e)
    {
        _sink = null;
        _extra = null;
    }

    private static HashSet<string> Sink =>
        _sink ??= Parse(ModConfig.FloatingSinkList.Value);

    private static HashSet<string> Extra =>
        _extra ??= Parse(ModConfig.FloatingExtraList.Value);

    private static HashSet<string> Parse(string raw)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(raw))
            return set;
        foreach (var part in raw.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var name = part.Trim();
            if (name.Length > 0)
                set.Add(name);
        }

        return set;
    }

    private static string PrefabName(ItemDrop drop)
    {
        var name = Utils.GetPrefabName(drop.gameObject);
        return string.IsNullOrEmpty(name) ? drop.gameObject.name : name;
    }

    private static bool ForceSink(string prefab) => Sink.Contains(prefab);

    private static bool ForceFloat(string prefab) =>
        ModConfig.FloatingEverything.Value || Extra.Contains(prefab);

    private static Floating? Template()
    {
        if (_template != null)
            return _template;
        var wood = ObjectDB.instance?.GetItemPrefab("Wood");
        _template = wood != null ? wood.GetComponent<Floating>() : null;
        return _template;
    }

    private static void Apply(ItemDrop drop)
    {
        if (drop == null)
            return;
        var prefab = PrefabName(drop);
        if (ForceSink(prefab))
        {
            if (drop.m_floating == null)
                return;
            UnityEngine.Object.Destroy(drop.m_floating);
            drop.m_floating = null;
            return;
        }

        if (ForceFloat(prefab))
        {
            if (drop.m_floating != null)
                return;
            var added = drop.gameObject.AddComponent<Floating>();
            var src = Template();
            if (src != null)
            {
                added.m_waterLevelOffset = src.m_waterLevelOffset;
                added.m_forceDistance = src.m_forceDistance;
                added.m_force = src.m_force;
                added.m_balanceForceFraction = src.m_balanceForceFraction;
                added.m_damping = src.m_damping;
            }

            drop.m_floating = added;
        }
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Awake))]
        private static void Awake(ItemDrop __instance)
        {
            if (!ModConfig.FloatingItemsEnabled.Value)
                return;
            Apply(__instance);
        }
    }
}
