using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.Core;

internal static partial class RestlessUi
{
    // Compact controls may shrink modestly. Descriptions use measured wrapping instead.
    public static void BoundedLabel(Text label, int preferred, int minimum)
    {
        label.fontSize = preferred;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = Mathf.Min(minimum, preferred);
        label.resizeTextMaxSize = preferred;
        // Wrapping constrains best-fit to the available width as well as height.
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
    }
}
