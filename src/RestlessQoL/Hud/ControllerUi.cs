using UnityEngine;

namespace RestlessQoL.HudTweaks;

internal static class ControllerUi
{
    internal static bool Focused(InventoryElement? element)
    {
        var gui = InventoryGui.instance;
        if (element == null || gui == null || !ZInput.IsExclusiveGamepadActive()) return false;
        return At(gui.m_playerGrid, element) || At(gui.ContainerGrid, element);
    }

    private static bool At(InventoryGrid? grid, InventoryElement element)
    {
        if (grid == null || !grid.gameObject.activeInHierarchy || grid.m_uiGroup == null || !grid.m_uiGroup.IsActive
            || grid.SelectionGridPosition != element.Position) return false;
        var inv = grid.GetInventory(); var pos = element.Position;
        return inv != null && grid.GetElement(pos.x, pos.y, inv.GetWidth()) == element;
    }
}
