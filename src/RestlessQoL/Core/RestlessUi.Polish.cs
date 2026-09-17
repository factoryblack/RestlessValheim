using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RestlessQoL.Core;

internal static partial class RestlessUi
{
    private static Sprite? _qualityLozenge;
    public static Sprite? QualityLozenge()
    {
        if (_qualityLozenge != null) return _qualityLozenge;
        var source = Kit.Sprite("quality-lozenge");
        if (source == null) return null;
        _qualityLozenge = Sprite.Create(source.texture, new Rect(11f, 3f, 42f, 58f),
            new Vector2(0.5f, 0.5f), 100f);
        return _qualityLozenge;
    }

    public static void PortraitFrame(GameObject target)
    {
        var image = target.GetComponent<Image>();
        if (image == null) return;
        image.sprite = Kit.Sprite("portrait-frame");
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = false;
        Rim(target, on: false);
    }

    public static void CategoryStrip(GameObject target)
    {
        var image = target.GetComponent<Image>();
        image.sprite = Kit.Sprite("category-strip", new Vector4(64f, 12f, 64f, 12f));
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 3f;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    public static RestlessControlFeedback ControlFeedback(Selectable control)
    {
        var feedback = control.GetComponent<RestlessControlFeedback>();
        if (feedback == null) feedback = control.gameObject.AddComponent<RestlessControlFeedback>();
        feedback.Configure(control);
        return feedback;
    }
}

// Adds only visual feedback. The original Selectable continues to receive every
// pointer, submit, navigation and click event on the same GameObject.
internal sealed class RestlessControlFeedback : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Selectable? _control;
    private GameObject? _focus;
    private bool _hover;
    private bool _pressed;
    public Image? Icon;
    public bool Selected;

    public void Configure(Selectable control)
    {
        _control = control;
        if (_focus != null) return;
        _focus = RestlessUi.Picture(transform, "RestlessFocus", "focus-corners");
        var image = _focus.GetComponent<Image>();
        image.sprite = Kit.Sprite("focus-corners", new Vector4(16f, 16f, 16f, 16f));
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 2f;
        image.color = new Color(1f, 1f, 1f, 0.65f);
        image.raycastTarget = false;
        var layout = _focus.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;
        RestlessUi.Stretch(_focus, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
        _focus.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_control == null) return;
        var enabled = _control.IsActive() && _control.IsInteractable();
        var focused = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        if (_focus != null)
        {
            _focus.SetActive(enabled && (_hover || focused));
            _focus.transform.SetAsLastSibling();
        }
        if (Icon != null)
            Icon.color = new Color(1f, 1f, 1f, !enabled ? 0.32f : _pressed ? 0.8f : Selected || _hover || focused ? 1f : 0.72f);
    }

    public void OnPointerEnter(PointerEventData e) => _hover = true;
    public void OnPointerExit(PointerEventData e)
    {
        if (!e.fullyExited) return;
        _hover = false;
        _pressed = false;
    }
    public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) _pressed = true; }
    public void OnPointerUp(PointerEventData e) => _pressed = false;
    private void OnDisable() { _hover = _pressed = false; if (_focus != null) _focus.SetActive(false); }
    private void OnDestroy() { if (_focus != null) Destroy(_focus); }
}
