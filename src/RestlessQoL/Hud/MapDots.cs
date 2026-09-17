using System.Collections.Generic;
using HarmonyLib;
using RestlessQoL.Core;
using UnityEngine;

namespace RestlessQoL.HudTweaks;

// Always share your map position and draw every other player as a cream diamond.
// Vanilla only lists peers with Visible on, and only sends a position when that
// flag is set — so we keep public on while this is enabled.
public sealed class MapDots : FeatureModule
{
    public override string Id => "map.player_dots";
    public override bool Enabled => true;

    protected override void OnLoaded()
    {
        ModConfig.MapPlayerDots.SettingChanged += (_, _) =>
        {
            if (ModConfig.MapPlayerDots.Value)
                KeepPublic();
            else
                ZNet.instance?.SetPublicReferencePosition(false);
        };
    }

    private static bool On => ModConfig.MapPlayerDots.Value;

    private static Sprite? Diamond() => Kit.Sprite("map-player") ?? Kit.Sprite("diamond");

    private static void KeepPublic()
    {
        if (!On)
            return;
        var net = ZNet.instance;
        if (net == null)
            return;
        if (!net.IsReferencePositionPublic())
            net.SetPublicReferencePosition(true);
        var toggle = Minimap.instance?.m_publicPosition;
        if (toggle != null && !toggle.isOn)
            toggle.SetIsOnWithoutNotify(true);
    }

    private static void PaintPins(Minimap map)
    {
        var sprite = Diamond();
        foreach (var pin in map.m_playerPins)
        {
            if (sprite != null)
                pin.m_icon = sprite;
            if (pin.m_iconElement == null)
                continue;
            if (sprite != null)
                pin.m_iconElement.sprite = sprite;
            pin.m_iconElement.color = Color.white;
            pin.m_iconElement.preserveAspect = true;
        }
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Update))]
        private static void AfterNet() => KeepPublic();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.GetOtherPublicPlayers))]
        private static bool OtherPlayers(ZNet __instance, List<ZNet.PlayerInfo> playerList)
        {
            if (!On)
                return true;
            foreach (var player in __instance.GetPlayerList())
            {
                if (player.m_characterID.IsNone())
                    continue;
                if (player.m_characterID == __instance.m_characterID)
                    continue;
                if (!player.m_publicPosition)
                    continue;
                playerList.Add(player);
            }

            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.GetSprite))]
        private static void PlayerSprite(Minimap.PinType type, ref Sprite __result)
        {
            if (!On || type != Minimap.PinType.Player)
                return;
            var sprite = Diamond();
            if (sprite != null)
                __result = sprite;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdatePlayerPins))]
        private static void AfterPins(Minimap __instance)
        {
            if (On)
                PaintPins(__instance);
        }
    }
}
