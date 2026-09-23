using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.Core;

internal static partial class RestlessUi
{
    // Nested crafting regions share the outer sheet instead of stacking paper
    // textures, rims and translucent backgrounds over one another.
    public static void CraftInset(GameObject target)
    {
        var image = target.GetComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = Color.clear;
        image.raycastTarget = false;
        Rim(target, on: false);
        foreach (var name in new[] { "paperAccent", "RestlessPaperRule" })
        {
            var old = target.transform.Find(name);
            if (old != null) old.gameObject.SetActive(false);
        }
    }

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
        // DressSlot initially makes a square. The material frame has a count
        // ledge and must use the entire portrait-shaped requirement cell.
        Stretch(target, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var image = target.GetComponent<Image>();
        image.sprite = Kit.Sprite("craft-material-socket");
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        Rim(target, on: false);
        var rim = target.transform.Find("paperAccent");
        if (rim != null) rim.gameObject.SetActive(false);
    }

    public static void RecipeSelection(GameObject target, bool selected)
    {
        CraftInset(target);
        var image = target.GetComponent<Image>();
        image.raycastTarget = true;
        image.color = selected ? new Color(0.65f, 0.51f, 0.28f, 0.22f) : new Color(0f, 0f, 0f, 0.1f);
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

    public static GameObject StationCorner(Transform sheet)
    {
        var corner = sheet.Find("RestlessStationCorner")?.gameObject;
        if (corner != null) return corner;
        corner = Picture(sheet, "RestlessStationCorner", "craft-station-corner");
        Pin(corner, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(76f, 76f));
        corner.GetComponent<Image>().raycastTarget = false;
        return corner;
    }
}
