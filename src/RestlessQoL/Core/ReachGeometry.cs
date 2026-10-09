using UnityEngine;

namespace RestlessQoL.Core;

internal static class ReachGeometry
{
    public static bool Sphere(Vector3 point, Vector3 origin, float radius) =>
        radius > 0f && (point - origin).sqrMagnitude <= radius * radius;

    // Native HaveBuildStationInRange ignores height and excludes the exact boundary.
    public static bool Station(Vector3 point, Vector3 origin, float radius)
    {
        var offset = point - origin;
        return radius > 0f && offset.x * offset.x + offset.z * offset.z < radius * radius;
    }
}
