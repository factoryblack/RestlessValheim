using HarmonyLib;
using Jotunn.Managers;
using RestlessQoL.Core;
using UnityEngine;

namespace RestlessQoL.HudTweaks;

public sealed class Vitals : FeatureModule
{
    public override string Id => "ui.vitals";
    public override bool Enabled => true;

    private static RestlessUi.HudMeter? _health;
    private static RestlessUi.HudMeter? _adrenaline;
    private static RestlessUi.HudMeter? _stamina;
    private static RestlessUi.HudMeter? _eitr;
    private static GameObject? _root;

    protected override void OnLoaded()
    {
        GUIManager.OnCustomGUIAvailable += TearDown;
    }

    public override void Tick()
    {
        if (ModConfig.VitalsEnabled.Value)
            return;
        TearDown();
        RestoreVanilla();
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(global::Hud), nameof(global::Hud.Update))]
        private static void HudTick(global::Hud __instance)
        {
            if (!ModConfig.VitalsEnabled.Value)
            {
                TearDown();
                RestoreVanilla();
                return;
            }

            HideVanilla(__instance);
            var player = Player.m_localPlayer;
            if (player == null || __instance.m_rootObject == null || !__instance.m_rootObject.activeInHierarchy)
            {
                if (_root != null)
                    _root.SetActive(false);
                return;
            }

            Ensure(__instance);
            Paint(player);
        }
    }

    private static void Ensure(global::Hud hud)
    {
        if (_root != null && _health != null && _adrenaline != null && _stamina != null && _eitr != null
            && _root.transform.Find("health/track") != null
            && _root.transform.Find("adrenaline/track") != null
            && _root.transform.Find("eitr/track") != null)
        {
            _root.SetActive(true);
            return;
        }

        TearDown();
        _root = RestlessUi.Node(hud.m_rootObject.transform, "RestlessVitals");
        _health = RestlessUi.HudMeter.Health(_root.transform);
        _adrenaline = RestlessUi.HudMeter.Adrenaline(_root.transform);
        _stamina = RestlessUi.HudMeter.Stamina(_root.transform);
        _eitr = RestlessUi.HudMeter.Eitr(_root.transform);
    }

    private static void Paint(Player player)
    {
        if (_health == null || _adrenaline == null || _stamina == null || _eitr == null)
            return;
        if (!Span(out var left, out var right, out var midY, out _))
            return;

        var numbers = ModConfig.VitalsNumbers.Value;
        var stack = (numbers ? RestlessUi.HudMeter.TallHeight : RestlessUi.HudMeter.Height) + 4f;

        var hpMax = Mathf.Max(1f, player.GetMaxHealth());
        var hpSpan = RestlessUi.HudMeter.Span(hpMax, 25f);
        _health.Set(player.GetHealth(), hpMax, hpSpan, numbers);
        _health.Park(left, midY);
        _health.Root.SetActive(true);

        var adrMax = player.GetMaxAdrenaline();
        var showAdr = ModConfig.VitalsAdrenaline.Value && adrMax > 0.01f;
        _adrenaline.Root.SetActive(showAdr);
        if (showAdr)
        {
            _adrenaline.Set(player.GetAdrenaline(), adrMax, RestlessUi.HudMeter.Span(adrMax, 20f), numbers);
            _adrenaline.Park(left, midY + stack);
        }

        var stamMax = Mathf.Max(1f, player.GetMaxStamina());
        var stam = player.GetStamina();
        var showStam = ModConfig.VitalsStaminaAlways.Value || stam < stamMax * 0.98f;
        _stamina.Root.SetActive(showStam);
        if (showStam)
        {
            _stamina.Set(stam, stamMax, RestlessUi.HudMeter.Span(stamMax, 50f), numbers);
            _stamina.Park(right, midY);
        }

        var eitrMax = player.GetMaxEitr();
        var showEitr = ModConfig.VitalsEitr.Value && eitrMax > 0.01f;
        _eitr.Root.SetActive(showEitr);
        if (showEitr)
        {
            _eitr.Set(player.GetEitr(), eitrMax, RestlessUi.HudMeter.Span(eitrMax, 40f), numbers);
            _eitr.Park(right, showStam ? midY + stack : midY);
        }
    }

    private static bool Span(out float left, out float right, out float midY, out float height)
    {
        if (Hotbar.TrySpan(out left, out right, out midY, out height)
            || Hotbar.Measure(Object.FindFirstObjectByType<HotkeyBar>(), out left, out right, out midY, out height))
        {
            ExtraSlots.Widen(ref right);
            return true;
        }

        return false;
    }

    private static void HideVanilla(global::Hud hud)
    {
        RestlessUi.Quiet(hud.m_healthBarRoot);
        RestlessUi.Quiet(hud.m_healthPanel);
        RestlessUi.Quiet(hud.m_foodBarRoot);
        RestlessUi.Quiet(hud.m_staminaBar2Root);
        if (ModConfig.VitalsAdrenaline.Value)
            RestlessUi.Quiet(hud.m_adrenalineBarRoot);
        else
            RestlessUi.Loud(hud.m_adrenalineBarRoot);
        if (ModConfig.VitalsEitr.Value)
            RestlessUi.Quiet(hud.m_eitrBarRoot);
        else
            RestlessUi.Loud(hud.m_eitrBarRoot);
    }

    private static void RestoreVanilla()
    {
        var hud = global::Hud.instance;
        if (hud == null)
            return;
        RestlessUi.Loud(hud.m_healthBarRoot);
        RestlessUi.Loud(hud.m_healthPanel);
        RestlessUi.Loud(hud.m_foodBarRoot);
        RestlessUi.Loud(hud.m_staminaBar2Root);
        RestlessUi.Loud(hud.m_adrenalineBarRoot);
        RestlessUi.Loud(hud.m_eitrBarRoot);
    }

    private static void TearDown()
    {
        if (_root != null)
            Object.Destroy(_root);
        _root = null;
        _health = null;
        _adrenaline = null;
        _stamina = null;
        _eitr = null;
    }
}
