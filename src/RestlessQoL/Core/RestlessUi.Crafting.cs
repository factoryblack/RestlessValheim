using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.Core;

internal static partial class RestlessUi
{
    // Material artwork is independent of native controls, item icons and live text.
    public static void CraftTab(GameObject target, bool selected)
    {
        ForgedTab(target, selected);
        var image = target.GetComponent<Image>();
        image.sprite = Kit.Sprite("craft-tab-ribbon", new Vector4(64f, 12f, 64f, 12f));
        image.pixelsPerUnitMultiplier = 3f;
        image.color = selected ? Color.white : new Color(0.8f, 0.8f, 0.8f, 0.65f);
        var marker = target.transform.Find("RestlessTabMarker");
        if (marker != null)
            Pin(marker.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 2f), new Vector2(28f, 12f));
    }

    public static void MaterialSocket(GameObject target)
    {
        var image = target.GetComponent<Image>();
        image.sprite = Kit.Sprite("craft-material-socket");
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        Rim(target, on: false);
    }

    public static void RecipeSelection(GameObject target, bool selected)
    {
        PaperControl(target, selected ? Accent : null);
        var clasp = target.transform.Find("RestlessRecipeClasp")?.gameObject;
        if (clasp == null && selected)
        {
            clasp = Picture(target.transform, "RestlessRecipeClasp", "craft-selection-clasp");
            Pin(clasp, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(2f, 0f), new Vector2(6f, 28f));
            clasp.GetComponent<Image>().raycastTarget = false;
        }
        if (clasp != null) clasp.SetActive(selected);
    }

    public static GameObject StationCorner(Transform icon)
    {
        var corner = icon.Find("RestlessStationCorner")?.gameObject;
        if (corner != null) return corner;
        corner = Picture(icon, "RestlessStationCorner", "craft-station-corner");
        Stretch(corner, Vector2.zero, Vector2.one, new Vector2(-8f, -8f), new Vector2(8f, 8f));
        corner.GetComponent<Image>().raycastTarget = false;
        return corner;
    }
}
