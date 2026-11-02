using System;

namespace RestlessQoL.HudTweaks;

// Inventory storage coordinates stay unchanged. Only controller traversal uses
// the visible bag / two equipment columns / quick-slot column arrangement.
internal static class ControllerNavigation
{
    internal static Vector2i Visual(Vector2i stored, int width, int rows)
    {
        if (stored.y < rows)
            return new Vector2i(Clamp(stored.x, 0, width - 1), Clamp(stored.y, 0, rows - 1));
        var extra = stored.x + (stored.y - rows) * width;
        if (extra < 0 || extra >= 9) return new Vector2i(width - 1, rows - 1);
        return extra < 6 ? new Vector2i(width + extra % 2, extra / 2) : new Vector2i(width + 2, extra - 6);
    }

    internal static Vector2i Stored(Vector2i visual, int width, int rows)
    {
        var x = Clamp(visual.x, 0, width + 2);
        var y = Clamp(visual.y, 0, x < width ? rows - 1 : 2);
        if (x < width) return new Vector2i(x, y);
        var extra = x == width + 2 ? 6 + y : y * 2 + x - width;
        return new Vector2i(extra % width, rows + extra / width);
    }

    internal static Vector2i Move(Vector2i stored, int width, int rows, int dx, int dy)
    {
        var visual = Visual(stored, width, rows);
        return Stored(new Vector2i(visual.x + dx, visual.y + dy), width, rows);
    }

    internal static InventoryGrid.Modifier Modifier(bool split, bool drop) =>
        drop ? InventoryGrid.Modifier.Drop : split ? InventoryGrid.Modifier.Split : InventoryGrid.Modifier.Select;

    internal static bool NearbyStackAllowed(bool gamepad, bool playerGrid, bool container, bool dragging, bool modal, bool enabled) =>
        gamepad && playerGrid && !container && !dragging && !modal && enabled;

    private static int Clamp(int n, int min, int max) => Math.Max(min, Math.Min(max, n));
}
