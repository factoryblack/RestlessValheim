using RestlessQoL.Core;
using RestlessQoL.Storage;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

public sealed partial class InventoryScreen
{
    private static string ControllerCaption(Button button, string fallback)
    {
        var title = RestlessUi.ButtonCopy(button, fallback);
        var input = button.GetComponent<UIGamePad>();
        // Preserve the native handler and its binding. The dresser suppresses
        // its old hint graphics, so expose that binding on the visible chip.
        return ZInput.IsGamepadActive() && input != null && !string.IsNullOrEmpty(input.m_zinputKey)
            ? title + " [" + RestlessUi.Bind(input.m_zinputKey) + "]" : title;
    }

    private static void ControllerStack(InventoryGui gui)
    {
        var grid = gui.m_playerGrid;
        var open = gui.m_inventoryRoot != null && gui.m_inventoryRoot.gameObject.activeInHierarchy;
        var selection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        var typing = selection != null && (selection.GetComponent<InputField>()?.isFocused == true
            || selection.GetComponent<TMP_InputField>()?.isFocused == true);
        var modal = !open || global::Console.IsVisible() || typing || ModalOpen(gui);
        var ready = ControllerNavigation.NearbyStackAllowed(ZInput.IsExclusiveGamepadActive(),
            grid != null && grid.m_uiGroup != null && grid.m_uiGroup.IsActive,
            gui.IsContainerOpen(), gui.m_dragItem != null, modal,
            ModConfig.StorageEnabled.Value && ModConfig.QuickStackEnabled.Value);

        // RStick already means native Stack All with a chest open. This path
        // runs only in the player grid without a chest, using the existing
        // nearby-storage routine and all its lock/ward/item protections.
        if (ready && ZInput.GetButtonDown("JoyRStick") && Player.m_localPlayer != null)
            QuickStack.Run(Player.m_localPlayer);

        if (grid == null || grid.m_gridRoot == null) return;
        var hint = grid.m_gridRoot.Find("RestlessControllerStackHint");
        var show = ready && ModConfig.InventoryScreenEnabled.Value && _dressed;
        if (hint == null && show)
        {
            var go = RestlessUi.KeyChip(grid.m_gridRoot, "RestlessControllerStackHint");
            RestlessUi.Pin(go, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, -4f), new Vector2(210f, 24f));
            foreach (var graphic in go.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            Ours.Add(go);
            hint = go.transform;
        }
        if (hint != null)
        {
            hint.gameObject.SetActive(show);
            if (show) RestlessUi.SetKeyChip(hint.gameObject, "Stack nearby", "JoyRStick");
        }
    }
}
