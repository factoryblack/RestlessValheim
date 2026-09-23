using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.Core;

internal static partial class RestlessUi
{
    public static readonly Color SetActiveTint = Hex(0xB2C982);

    public static void LoadoutPlaque(GameObject target)
    {
        var image = target.GetComponent<Image>();
        image.sprite = Kit.Sprite("loadout-stat-plaque", new Vector4(100f, 28f, 100f, 28f));
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 3f;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    public static void LoadoutCorner(Transform parent)
    {
        var corner = Picture(parent, "loadoutCorner", "loadout-corner");
        Pin(corner, Vector2.one, Vector2.one, Vector2.zero, new Vector2(96f, 96f));
        var crest = Picture(corner.transform, "characterCrest", "loadout-crest");
        Pin(crest, Vector2.one, Vector2.one, new Vector2(-9f, -9f), new Vector2(40f, 40f));
        corner.GetComponent<Image>().raycastTarget = crest.GetComponent<Image>().raycastTarget = false;
    }

    public static GameObject SetWell(Transform parent)
    {
        var panel = Tray(parent, "setBonus", false);
        PaperSurface(panel);
        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.spacing = 6f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return panel;
    }

    // Shared by item inspect and the loadout reader. Width is the inset content
    // width; text measures before the parent layout so long set names can wrap.
    public static void SetIdentity(Transform parent, string name, int? equipped,
        int required, bool active, float width)
    {
        var tint = active ? SetActiveTint : PaperMuted;
        var head = Node(parent, "setIdentity");
        var seal = Picture(head.transform, "seal", "equipment-set-seal");
        Pin(seal, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -4f), new Vector2(36f, 36f));
        seal.GetComponent<Image>().color = active ? Color.white : new Color(0.7f, 0.7f, 0.7f);
        seal.GetComponent<Image>().raycastTarget = false;
        var title = Label(head.transform, name, 23, Text, TextAnchor.UpperLeft);
        Pin(title.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(48f, -3f), new Vector2(Mathf.Max(40f, width - 48f), 30f));
        title.horizontalOverflow = HorizontalWrapMode.Wrap;
        title.verticalOverflow = VerticalWrapMode.Overflow;
        head.AddComponent<LayoutElement>().preferredHeight = Mathf.Max(44f, title.preferredHeight + 6f);
        LoadoutCopy(parent, "SET BONUS", width, 16, tint);
        LoadoutCopy(parent, equipped.HasValue
            ? (active ? "Active" : "Inactive") + " · " + equipped + "/" + required + " equipped"
            : required + " pieces required", width, 18, tint);
        if (required > 0 && equipped.HasValue)
        {
            var marks = Node(parent, "setPieces");
            marks.AddComponent<LayoutElement>().preferredHeight = 10f;
            var rail = Node(marks.transform, "rail");
            Pin(rail, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
                new Vector2(Mathf.Min(width, 160f), 6f));
            // A bounded progress rail, not quality gems. Numeric count stays exact
            // for unusually large addon sets without creating hundreds of objects.
            var count = Mathf.Min(required, 12);
            for (var i = 0; i < count; i++)
            {
                var mark = Graphic(rail.transform, "piece", (i + 1) * required <= equipped.Value * count ? tint : Ink, false);
                Stretch(mark, new Vector2((float)i / count, 0f), new Vector2((float)(i + 1) / count, 1f),
                    new Vector2(1f, 0f), new Vector2(-3f, 0f));
            }
        }
    }

    public static Text LoadoutCopy(Transform parent, string copy, float width, int size, Color colour)
    {
        var text = Label(parent, copy, size, colour, TextAnchor.UpperLeft);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(40f, width));
        text.gameObject.AddComponent<LayoutElement>().preferredHeight = text.preferredHeight + 3f;
        return text;
    }
}
