using System.Collections.Generic;
using UnityEngine;

namespace RestlessQoL.Core;

internal static class NearbyQuery
{
    public static IEnumerable<T> UniqueInSphere<T>(Vector3 origin, float radius) where T : Component
    {
        if (radius <= 0f)
            yield break;

        var hits = Physics.OverlapSphere(origin, radius);
        var seen = new HashSet<int>();
        foreach (var hit in hits)
        {
            var component = hit.GetComponentInParent<T>();
            if (component == null)
                continue;
            if (!seen.Add(component.GetInstanceID()))
                continue;
            yield return component;
        }
    }
}
