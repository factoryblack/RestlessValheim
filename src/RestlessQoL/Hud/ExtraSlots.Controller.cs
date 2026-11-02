using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

public sealed partial class ExtraSlots
{
    private static bool UpdateControllerGrid(InventoryGrid grid)
    {
        var gui = InventoryGui.instance;
        var inv = grid.GetInventory();
        if (!ModConfig.ExtraSlotsEnabled.Value || _visibleHeight < 1 || gui == null
            || grid != gui.m_playerGrid || inv == null || inv.GetWidth() < 5
            || inv != Player.m_localPlayer?.GetInventory()) return true;
        // Match native input ownership. Other grids, mouse, console and touch
        // retain the original handler. No global button state is consumed.
        if (grid.m_uiGroup == null || !grid.m_uiGroup.IsActive || global::Console.IsVisible()
            || ZInput.IsTouchActive() || !ZInput.IsExclusiveGamepadActive()) return true;

        var width = inv.GetWidth();
        var before = grid.SelectionGridPosition;
        var pos = ControllerNavigation.Move(before, width, _visibleHeight, 0, 0);
        if (Down("Left")) pos = ControllerNavigation.Move(pos, width, _visibleHeight, -1, 0);
        if (Down("Right")) pos = ControllerNavigation.Move(pos, width, _visibleHeight, 1, 0);
        var visual = ControllerNavigation.Visual(pos, width, _visibleHeight);
        if (Down("Up"))
        {
            if (visual.y == 0 && grid.jumpToNextContainer && grid.OnMoveToUpperInventoryGrid != null)
            {
                grid.OnMoveToUpperInventoryGrid(new Vector2i(Mathf.Min(visual.x, width - 1), 0));
                return false;
            }
            pos = ControllerNavigation.Move(pos, width, _visibleHeight, 0, -1);
            grid.jumpToNextContainer = false;
        }
        if (Down("Down"))
        {
            var bottom = visual.x < width ? _visibleHeight - 1 : 2;
            if (visual.y == bottom && grid.jumpToNextContainer && grid.OnMoveToLowerInventoryGrid != null)
            {
                grid.OnMoveToLowerInventoryGrid(new Vector2i(Mathf.Min(visual.x, width - 1), _visibleHeight - 1));
                return false;
            }
            pos = ControllerNavigation.Move(pos, width, _visibleHeight, 0, 1);
            grid.jumpToNextContainer = false;
        }
        grid.SetGamepadSelection(pos);
        if (pos != before)
        {
            var element = grid.GetGamepadSelectedElement();
            if (element != null) element.GetComponent<Selectable>()?.Select();
            if (pos.y < _visibleHeight && element != null && grid.m_ensureVisible != null)
                grid.m_ensureVisible.CenterOnItem(element);
        }
        if (!Held("Up") && !Held("Down")) grid.jumpToNextContainer = true;

        // Same native callbacks/modifiers, after resolving the visual target.
        // Simultaneous move + action therefore operates on the visible cell.
        if (ZInput.GetButtonDown("JoyButtonA"))
            grid.m_onSelected?.Invoke(grid, grid.GetGamepadSelectedItem(), pos,
                ControllerNavigation.Modifier(ZInput.GetButton("JoyLTrigger"), ZInput.GetButton("JoyRTrigger")));
        if (ZInput.GetButtonDown("JoyButtonX"))
        {
            if (ZInput.GetButton("JoyLTrigger"))
                grid.m_onSelected?.Invoke(grid, grid.GetGamepadSelectedItem(), pos, InventoryGrid.Modifier.Move);
            else grid.m_onRightClick?.Invoke(grid, grid.GetGamepadSelectedItem(), pos);
        }
        return false;
    }

    private static bool Down(string direction) => ZInput.GetButtonDown("JoyDPad" + direction) || ZInput.GetButtonDown("JoyLStick" + direction);
    private static bool Held(string direction) => ZInput.GetButton("JoyDPad" + direction) || ZInput.GetButton("JoyLStick" + direction);
}
