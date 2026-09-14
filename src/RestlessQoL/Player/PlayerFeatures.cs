using System;
using HarmonyLib;
using RestlessQoL.Core;
using UnityEngine;

namespace RestlessQoL.PlayerTweaks;

public sealed class DeathPins : FeatureModule
{
    public override string Id => "player.deathpins";
    public override bool Enabled => true;

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TombStone), nameof(TombStone.OnTakeAllSuccess))]
        private static void OnTakeAllSuccess(TombStone __instance)
        {
            RemovePin(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TombStone), nameof(TombStone.EasyFitInInventory))]
        private static void EasyFitInInventory(TombStone __instance, bool __result)
        {
            if (__result)
                RemovePin(__instance);
        }

        private static void RemovePin(TombStone tomb)
        {
            if (!ModConfig.DeathPinsEnabled.Value || Minimap.instance == null || tomb == null)
                return;
            Minimap.instance.RemovePin(tomb.transform.position, 1f);
        }
    }
}

public sealed class SwimWield : FeatureModule
{
    public override string Id => "player.swimwield";
    public override bool Enabled => true;

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.HideHandItems))]
        private static bool HideHandItems(Humanoid __instance)
        {
            if (!ModConfig.SwimWieldEnabled.Value)
                return true;
            return __instance is not Player player || !player.IsSwimming();
        }
    }
}

public sealed class CrossbowState : FeatureModule
{
    public override string Id => "player.crossbow";
    public override bool Enabled => true;

    [HarmonyPatch]
    private static class Patches
    {
        [ThreadStatic]
        private static bool _unequipping;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
        private static void UnequipPrefix() => _unequipping = true;

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
        private static void UnequipFinalizer() => _unequipping = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), nameof(Player.ResetLoadedWeapon))]
        private static bool ResetLoadedWeapon()
        {
            return !_unequipping || !ModConfig.CrossbowStateEnabled.Value;
        }
    }
}

public sealed class FriendlyFire : FeatureModule
{
    public override string Id => "player.friendlyfire";
    public override bool Enabled => true;

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Turret), nameof(Turret.Awake))]
        private static void Awake(Turret __instance)
        {
            if (!ModConfig.FriendlyFireEnabled.Value)
                return;
            __instance.m_targetTamed = false;
            __instance.m_targetTamedConfig = false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
        private static bool BeforeDamage(Character __instance, HitData hit)
        {
            if (!ModConfig.FriendlyFireEnabled.Value || hit == null)
                return true;
            var attacker = hit.GetAttacker();
            if (attacker == null || attacker == __instance)
                return true;
            return !Allied(attacker, __instance);
        }
    }

    private static bool Allied(Character a, Character b)
    {
        if (a.IsTamed() && b.IsTamed())
            return true;
        if (a.IsTamed() && b.IsPlayer())
            return true;
        return a.IsPlayer() && b.IsTamed();
    }
}

public sealed class AxeCombo : FeatureModule
{
    public override string Id => "player.axecombo";
    public override bool Enabled => true;

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Attack), nameof(Attack.Start),
            typeof(Humanoid), typeof(Rigidbody), typeof(ZSyncAnimation),
            typeof(CharacterAnimEvent), typeof(VisEquipment), typeof(ItemDrop.ItemData),
            typeof(Attack), typeof(float), typeof(float))]
        private static void Start(Attack __instance, ItemDrop.ItemData weapon)
        {
            if (!ModConfig.AxeComboEnabled.Value || weapon?.m_shared == null)
                return;
            if (weapon.m_shared.m_skillType != Skills.SkillType.Axes)
                return;
            if (__instance.m_resetChainIfHit == DestructibleType.Tree)
                __instance.m_resetChainIfHit = DestructibleType.None;
        }
    }
}

// Vanilla RepairOneItem does one worn piece per click, and only if this
// station can repair that recipe. Run the same loop on use, then keep the
// hammer button off — UpdateRepair would put it back every frame.
public sealed class AutoRepair : FeatureModule
{
    public override string Id => "player.auto_repair";
    public override bool Enabled => true;

    public static bool HidesButton => ModConfig.AutoRepairEnabled.Value;

    public static void HideChrome(InventoryGui gui)
    {
        if (gui.m_repairPanel != null)
            gui.m_repairPanel.gameObject.SetActive(false);
        if (gui.m_repairPanelSelection != null)
            gui.m_repairPanelSelection.gameObject.SetActive(false);
        if (gui.m_repairButton != null)
            gui.m_repairButton.gameObject.SetActive(false);
        if (gui.m_repairButtonGlow != null)
            gui.m_repairButtonGlow.enabled = false;
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
        private static void AfterShow(InventoryGui __instance) => RepairAll(__instance);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Interact))]
        private static void AfterUse(Humanoid user, bool __result)
        {
            if (!__result || user != Player.m_localPlayer)
                return;
            RepairAll(InventoryGui.instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRepair))]
        private static bool SkipButton(InventoryGui __instance)
        {
            if (!ModConfig.AutoRepairEnabled.Value)
                return true;
            HideChrome(__instance);
            return false;
        }
    }

    private static void RepairAll(InventoryGui? gui)
    {
        if (!ModConfig.AutoRepairEnabled.Value || gui == null)
            return;
        var player = Player.m_localPlayer;
        if (player == null)
            return;
        var station = player.GetCurrentCraftingStation();
        if (station == null || !station.m_canRepair || !station.CheckUsable(player, false))
            return;

        var n = 0;
        while (gui.HaveRepairableItems() && n++ < 64)
            gui.RepairOneItem();
    }
}
