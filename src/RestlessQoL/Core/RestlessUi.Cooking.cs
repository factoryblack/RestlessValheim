using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.Core;

internal static partial class RestlessUi
{
    // The portrait artwork becomes the card surface. Nine-slicing keeps the
    // corner folds intact when the card is wider than the original portrait.
    public static void CookHeroSurface(GameObject target)
    {
        var image = target.GetComponent<Image>();
        var sprite = Kit.Sprite("cook-portrait", new Vector4(330f, 210f, 350f, 350f));
        if (image == null || sprite == null) return;
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.pixelsPerUnitMultiplier = 6f;
        image.color = Color.white;
        image.raycastTarget = true;
        Rim(target, on: false);
        var old = target.transform.Find("paperAccent");
        if (old != null) old.gameObject.SetActive(false);
    }
}
