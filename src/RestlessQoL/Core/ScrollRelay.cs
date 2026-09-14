using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RestlessQoL.Core;

internal sealed class ScrollRelay : MonoBehaviour, IScrollHandler
{
    public ScrollRect? Target;

    public void OnScroll(PointerEventData eventData)
    {
        if (Target != null)
            Target.OnScroll(eventData);
    }

    public static void Bind(GameObject go, ScrollRect? scroll)
    {
        if (go == null || scroll == null)
            return;
        var relay = go.GetComponent<ScrollRelay>() ?? go.AddComponent<ScrollRelay>();
        relay.Target = scroll;
    }
}
