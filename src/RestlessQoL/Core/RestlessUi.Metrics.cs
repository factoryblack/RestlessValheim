using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.Core;

internal static partial class RestlessUi
{
    // Display-only metric, reusable for character records and future summary panels.
    public static void Metric(Transform parent, string title, string value, float from = 0f, float to = 1f)
    {
        var cell = Node(parent, "RestlessMetric");
        Stretch(cell, new Vector2(from, 0f), new Vector2(to, 1f), Vector2.zero, Vector2.zero);
        var label = Label(cell.transform, title, BodySize, PaperMuted, TextAnchor.MiddleLeft);
        Stretch(label.gameObject, Vector2.zero, new Vector2(0.64f, 1f), new Vector2(20f, 6f), new Vector2(-8f, -6f));
        BoundedLabel(label, BodySize, HintSize);
        var number = Label(cell.transform, value, BodySize + 2, Accent, TextAnchor.MiddleRight);
        Stretch(number.gameObject, new Vector2(0.64f, 0f), Vector2.one, new Vector2(4f, 6f), new Vector2(-20f, -6f));
        BoundedLabel(number, BodySize + 2, HintSize);
    }
}
