using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RestlessQoL.Core;

internal static partial class RestlessUi
{
    // Opt-in skin of DressSlot: data, native cells and their input targets stay intact.
    private static void InventorySlot(GameObject plate, GameObject cell, bool equipped,
        ItemDrop.ItemData? item, bool on)
    {
        var state = plate.GetComponent<RestlessInventorySlot>();
        var quality = cell.transform.Find("RestlessQuality");
        if (quality != null) quality.GetComponent<Image>().enabled = !on;
        if (!on)
        {
            if (state != null) state.enabled = false;
            if (state != null) plate.GetComponent<Image>().preserveAspect = false;
            var oldAmount = cell.transform.Find("RestlessAmount");
            if (state != null && oldAmount != null)
            {
                Stretch(oldAmount.gameObject, Vector2.zero, Vector2.one, new Vector2(4f, 2f), new Vector2(-4f, 2f));
                var face = oldAmount.GetComponent<Text>();
                face.resizeTextForBestFit = false;
                face.fontSize = HudMeta;
                face.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            var empty = plate.transform.Find("RestlessEmptyEquipment");
            if (empty != null) empty.gameObject.SetActive(false);
            return;
        }

        var image = plate.GetComponent<Image>();
        var sprite = Kit.Sprite("inventory-slot");
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
        }
        if (state == null) state = plate.AddComponent<RestlessInventorySlot>();
        state.enabled = true;
        state.Cell = cell;
        state.Equipped = equipped;

        // Separate corners for quality and lock; bottom strip belongs to counts/binds.
        if (quality != null)
            Pin(quality.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(5f, -5f), new Vector2(18f, 18f));
        var amount = cell.transform.Find("RestlessAmount");
        if (amount != null)
        {
            Stretch(amount.gameObject, Vector2.zero, new Vector2(1f, 0f),
                new Vector2(22f, 3f), new Vector2(-7f, 22f));
            BoundedLabel(amount.GetComponent<Text>(), HintSize, HudMeta);
        }
        var vein = plate.transform.Find("vein") as RectTransform;
        if (vein != null)
        {
            var tall = plate.GetComponent<RectTransform>().rect.height;
            var remaining = item != null ? Mathf.Clamp01(item.GetDurabilityPercentage()) : 0f;
            vein.sizeDelta = new Vector2(4f, Mathf.Max(0f, tall - 44f) * remaining);
            vein.anchoredPosition = new Vector2(-7f, 22f);
        }
    }

    public static void InventoryEmpty(GameObject plate, string asset, bool empty)
    {
        var mark = plate.transform.Find("RestlessEmptyEquipment");
        if (mark == null && !empty) return;
        if (mark == null)
            mark = Picture(plate.transform, "RestlessEmptyEquipment", asset).transform;
        mark.gameObject.SetActive(empty);
        if (!empty) return;
        var image = mark.GetComponent<Image>();
        image.sprite = Kit.Sprite(asset);
        image.color = new Color(Text.r, Text.g, Text.b, 0.20f);
        Stretch(mark.gameObject, Vector2.zero, Vector2.one, new Vector2(15f, 15f), new Vector2(-15f, -15f));
    }

    public static void InventoryBinding(Transform cell, string binding)
    {
        var mark = cell.Find("RestlessInventoryBind");
        if (mark == null && string.IsNullOrEmpty(binding)) return;
        if (mark == null)
        {
            var label = Label(cell, "", HudMeta, Muted, TextAnchor.LowerLeft);
            label.name = "RestlessInventoryBind";
            mark = label.transform;
        }
        mark.gameObject.SetActive(!string.IsNullOrEmpty(binding));
        var face = mark.GetComponent<Text>();
        face.text = binding;
        BoundedLabel(face, HudMeta, 10);
        Stretch(mark.gameObject, Vector2.zero, new Vector2(1f, 0f),
            new Vector2(6f, 3f), new Vector2(-24f, 22f));
    }

    public static void InventorySurface(GameObject target)
    {
        PaperSurface(target);
        var image = target.GetComponent<Image>();
        if (image != null) image.color = new Color(1f, 1f, 1f, 0.82f);
    }
}

// Only visual feedback. Events continue to the native Button/InventoryGrid.
internal sealed class RestlessInventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject Cell = null!;
    public bool Equipped;
    private bool _hover;
    private GameObject? _focus;
    private GameObject? _equipped;
    private void Awake()
    {
        _focus = RestlessUi.Node(transform, "RestlessInventoryFocus");
        RestlessUi.Stretch(_focus, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
        Edge(Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 1f));
        Edge(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -1f), Vector2.zero);
        Edge(Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(1f, 0f));
        Edge(new Vector2(1f, 0f), Vector2.one, new Vector2(-1f, 0f), Vector2.zero);
        _equipped = RestlessUi.Graphic(transform, "RestlessInventoryEquipped", RestlessUi.Accent, false);
        RestlessUi.Stretch(_equipped, Vector2.zero, new Vector2(1f, 0f), new Vector2(10f, 2f), new Vector2(-10f, 4f));
    }
    private void Edge(Vector2 min, Vector2 max, Vector2 a, Vector2 b)
    {
        var edge = RestlessUi.Graphic(_focus!.transform, "RestlessFocusEdge", RestlessUi.Text, false);
        RestlessUi.Stretch(edge, min, max, a, b);
    }
    public void OnPointerEnter(PointerEventData e) => _hover = true;
    public void OnPointerExit(PointerEventData e) { if (e.fullyExited) _hover = false; }
    private void LateUpdate()
    {
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        var focus = selected != null && Cell != null
            && (selected == Cell || selected.transform.IsChildOf(Cell.transform));
        var native = Cell != null ? Cell.GetComponent<InventoryElement>() : null;
        focus |= native != null && native.m_selected != null && native.m_selected.activeSelf;
        if (_focus != null) _focus.SetActive(_hover || focus);
        if (_equipped != null) _equipped.SetActive(Equipped);
    }
    private void OnDisable()
    {
        _hover = false;
        if (_focus != null) _focus.SetActive(false);
        if (_equipped != null) _equipped.SetActive(false);
    }
}
