using UnityEngine;

namespace RestlessQoL.Storage;

// Network ownership is authority to write, not permission to spend a player's stores.
internal static class StorageAccess
{
    internal static bool Allows(Vector3 position, long actor)
    {
        var covered = false;
        foreach (var area in PrivateArea.m_allAreas)
        {
            if (area == null || !area.IsEnabled() || !area.IsInside(position, 0f)) continue;
            covered = true;
            // Match native CheckAccess's default: permission in any covering ward.
            if (actor != 0 && ((area.m_piece != null && area.m_piece.GetCreator() == actor) || area.IsPermitted(actor))) return true;
        }
        return !covered;
    }

    internal static bool UsesBag(Player player, long actor, Vector3 origin, float range) =>
        player != null && actor != 0 && player.GetPlayerID() == actor
        && (player.transform.position - origin).sqrMagnitude <= range * range;

    internal static float Reach(float requested, float configured) => Mathf.Max(0f, Mathf.Min(requested, configured));
}
