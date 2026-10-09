using UnityEngine;

namespace RestlessCook;

// Unity serializes and remaps these references when the registered prefab is
// cloned. Do not recapture a preview's disabled colliders as the vanilla state.
internal sealed class FeastColliderState : MonoBehaviour
{
    public Collider[]? Colliders;
    public bool[] Enabled = System.Array.Empty<bool>();

    internal static void Apply(Collider[] colliders, bool[] enabled, Collider? plate, bool placed)
    {
        for (var i = 0; i < colliders.Length; i++)
            if (colliders[i] != null)
                colliders[i].enabled = placed && i < enabled.Length && enabled[i];
        if (plate != null)
            plate.enabled = !placed;
    }
}
