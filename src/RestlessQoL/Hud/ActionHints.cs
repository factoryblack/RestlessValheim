using System.Collections.Generic;
using HarmonyLib;
using Jotunn.Managers;
using RestlessQoL.Core;
using UnityEngine;

namespace RestlessQoL.HudTweaks;

public sealed class ActionHints : FeatureModule
{
    public override string Id => "ui.hints";
    public override bool Enabled => true;

    private static RestlessUi.KeyStack? _stack;
    private static float _mapRightX;

    protected override void OnLoaded()
    {
        GUIManager.OnCustomGUIAvailable += TearDown;
    }

    public override void Tick()
    {
        if (ModConfig.ActionHintsEnabled.Value)
            return;
        _stack?.Hide();
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(KeyHints), "UpdateHints")]
        private static void AfterHints(KeyHints __instance)
        {
            if (!ModConfig.ActionHintsEnabled.Value)
            {
                _stack?.Hide();
                if (!AnyVanilla(__instance))
                    RestoreVanilla(__instance);
                return;
            }

            var combat = Live(__instance.m_combatHints);
            var fishing = Live(__instance.m_fishingHints);
            var build = Live(__instance.m_buildHints);
            var inventory = Live(__instance.m_inventoryHints)
                || Live(__instance.m_inventoryWithContainerHints)
                || RestlessUi.InventoryOpen();
            var other = Live(__instance.m_barberHints) || Live(__instance.m_radialHints);

            HideVanilla(__instance);

            if (other || Hud.IsUserHidden() || RestlessUi.MapOpen())
            {
                _stack?.Hide();
                return;
            }

            var player = Player.m_localPlayer;
            if (inventory)
            {
                Show(InventoryRows());
                return;
            }

            if (player == null)
            {
                _stack?.Hide();
                return;
            }

            if (build)
                Show(BuildRows());
            else if (fishing)
                Show(("Hook", new[] { "Attack", "JoyAttack" }));
            else if (combat)
                Show(CombatRows(player));
            else
                _stack?.Hide();
        }
    }

    private static bool Live(GameObject? go) => go != null && go.activeSelf;

    private static bool AnyVanilla(KeyHints hints) =>
        Live(hints.m_combatHints) || Live(hints.m_fishingHints) || Live(hints.m_buildHints)
        || Live(hints.m_inventoryHints) || Live(hints.m_inventoryWithContainerHints);

    private static void RestoreVanilla(KeyHints hints)
    {
        Show(hints.m_combatHints);
        Show(hints.m_fishingHints);
        Show(hints.m_buildHints);
        Show(hints.m_inventoryHints);
        Show(hints.m_inventoryWithContainerHints);
        Show(hints.m_primaryAttackGP);
        Show(hints.m_primaryAttackKB);
        Show(hints.m_secondaryAttackGP);
        Show(hints.m_secondaryAttackKB);
        Show(hints.m_bowDrawGP);
        Show(hints.m_bowDrawKB);
    }

    private static void HideVanilla(KeyHints hints)
    {
        Quiet(hints.m_combatHints);
        Quiet(hints.m_fishingHints);
        Quiet(hints.m_buildHints);
        Quiet(hints.m_inventoryHints);
        Quiet(hints.m_inventoryWithContainerHints);
        Quiet(hints.m_primaryAttackGP);
        Quiet(hints.m_primaryAttackKB);
        Quiet(hints.m_secondaryAttackGP);
        Quiet(hints.m_secondaryAttackKB);
        Quiet(hints.m_bowDrawGP);
        Quiet(hints.m_bowDrawKB);
    }

    private static void Quiet(GameObject? go)
    {
        if (go != null)
            go.SetActive(false);
    }

    private static void Show(GameObject? go)
    {
        if (go != null)
            go.SetActive(true);
    }

    private static (string verb, string[] buttons)[] InventoryRows() =>
    [
        ("Use", new[] { "Use", "JoyUse" }),
        ("Split", new[] { "JoyLTrigger", "AltPlace" }),
        ("Inventory", new[] { "Inventory", "JoyInventory" })
    ];

    private static (string verb, string[] buttons)[] BuildRows() =>
    [
        ("Place", new[] { "Attack", "JoyPlace", "JoyAttack" }),
        ("Remove", new[] { "Remove", "JoyRemove", "SecondaryAttack", "SecondAttack" }),
        ("Snap", new[] { "AltPlace", "JoyAltPlace" }),
        ("Cycle", new[] { "NextSnap", "PrevSnap" }),
        ("Rotate", new[] { "JoyRotate" }),
        ("Menu", new[] { "BuildMenu" })
    ];

    private static (string verb, string[] buttons)[] CombatRows(Player player)
    {
        var rows = new List<(string, string[])>
        {
            (PrimaryVerb(player), new[] { "Attack", "JoyAttack", "JoyPlace" })
        };
        if (CanBlock(player))
            rows.Add(("Block", new[] { "Block", "JoyBlock" }));
        rows.Add(("Dodge", new[] { "Dodge", "JoyDodge" }));
        return rows.ToArray();
    }

    private static void Show(params (string verb, string[] buttons)[] rows)
    {
        var parent = global::Hud.instance?.m_rootObject != null
            ? global::Hud.instance.m_rootObject.transform
            : null;
        if (parent == null)
            return;

        _stack ??= RestlessUi.KeyStack.BottomRight(parent);

        var kept = new List<(string verb, string[] buttons)>();
        foreach (var row in rows)
        {
            if (row.verb == "Rotate" || RestlessUi.Bound(row.buttons))
                kept.Add(row);
        }

        if (kept.Count == 0)
            _stack.Hide();
        else
        {
            _stack.Set(kept);
            Park();
        }
    }

    // Same bottom-right column. Baseline = hotbar bottom, across the screen.
    private static void Park()
    {
        if (_stack == null)
            return;
        var rt = _stack.Root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(RestlessUi.HudRow.BuffWidth, rt.sizeDelta.y);
        if (rt.parent is not RectTransform parent)
            return;

        var map = Minimap.instance?.m_mapImageSmall != null
            ? Minimap.instance.m_mapImageSmall.rectTransform
            : null;
        if (map != null && RestlessUi.HudTuck.Amount < 0.02f)
        {
            var box = new Vector3[4];
            map.GetWorldCorners(box);
            _mapRightX = box[3].x;
        }

        var x = _mapRightX;
        if (x == 0f)
        {
            var hud = new Vector3[4];
            parent.GetWorldCorners(hud);
            x = hud[2].x;
        }

        var y = parent.TransformPoint(new Vector3(0f, 8f, 0f)).y;
        if (Hotbar.TrySpan(out _, out _, out var midY, out var height)
            || Hotbar.Measure(Object.FindFirstObjectByType<HotkeyBar>(), out _, out _, out midY, out height))
            y = midY - height * 0.5f;

        rt.pivot = new Vector2(1f, 0f);
        rt.position = new Vector3(x, y, rt.position.z);
    }

    private static string PrimaryVerb(Player player)
    {
        var item = player.GetRightItem() ?? player.GetCurrentWeapon();
        if (item?.m_shared == null)
            return "Attack";
        var t = item.m_shared.m_itemType;
        if (t == ItemDrop.ItemData.ItemType.Bow)
            return "Draw";
        if (t is ItemDrop.ItemData.ItemType.Tool or ItemDrop.ItemData.ItemType.Torch)
            return "Use";
        if (item.m_shared.m_skillType == Skills.SkillType.Fishing)
            return "Hook";
        return "Attack";
    }

    private static bool CanBlock(Player player)
    {
        if (player.GetLeftItem() != null)
            return true;
        var item = player.GetRightItem() ?? player.GetCurrentWeapon();
        if (item?.m_shared == null)
            return true;
        var t = item.m_shared.m_itemType;
        if (t is ItemDrop.ItemData.ItemType.Bow or ItemDrop.ItemData.ItemType.Tool
            or ItemDrop.ItemData.ItemType.Torch)
            return false;
        return item.m_shared.m_skillType != Skills.SkillType.Fishing;
    }

    private static void TearDown()
    {
        if (_stack != null)
            Object.Destroy(_stack.Root);
        _stack = null;
    }
}
