using System.Collections.Generic;
using RestlessQoL.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

public sealed partial class InventoryScreen
{
    private static readonly List<Transform> SmallDialogs = new();
    private static readonly List<TMP_Text> DialogCopy = new();

    private static void FindSmallDialogs(InventoryGui gui)
    {
        SmallDialogs.Clear();
        DialogCopy.Clear();
        foreach (var name in new[] { "SplitDialog", "VariantDialog" })
        {
            var root = RestlessUi.Deep(gui.transform, name);
            if (root == null) continue;
            SmallDialogs.Add(root);
            DialogCopy.AddRange(root.GetComponentsInChildren<TMP_Text>(true));
        }
    }

    private static int MixSmallDialogs(int hash)
    {
        unchecked
        {
            foreach (var root in SmallDialogs)
                hash = hash * 31 + (root != null && root.gameObject.activeInHierarchy ? 1 : 0);
            foreach (var text in DialogCopy) hash = MixTmp(hash, text);
            return hash;
        }
    }

    private static void DressSmallDialogs()
    {
        foreach (var root in SmallDialogs)
        {
            if (root == null || !root.gameObject.activeInHierarchy) continue;
            // Retain dimmers, masks, slider geometry, variant previews and input
            // caret/selection. Replace only identifiable panel backing paint.
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (RestlessUi.Owned(image.transform) || image.GetComponent<Mask>() != null
                    || image.GetComponentInParent<TMP_InputField>() != null
                    || image.GetComponentInParent<InputField>() != null
                    || image.GetComponentInParent<Selectable>() != null) continue;
                var name = image.name.ToLowerInvariant();
                if (name is not ("bkg" or "background" or "panel-back")) continue;
                var tag = "RestlessDialogPaper" + image.GetInstanceID();
                if (image.transform.parent.Find(tag) != null) continue;
                var paper = RestlessUi.Graphic(image.transform.parent, tag, Color.white, false);
                Ours.Add(paper);
                paper.AddComponent<LayoutElement>().ignoreLayout = true;
                RestlessUi.CopyRect(paper.GetComponent<RectTransform>(), image.rectTransform);
                paper.transform.SetSiblingIndex(image.transform.GetSiblingIndex());
                RestlessUi.InventorySurface(paper);
                Ghost(image); // Retain the panel's click interception.
            }
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (RestlessUi.Owned(button.transform)) continue;
                // Icon-only variant options keep native selected/disabled paint.
                var text = button.GetComponentInChildren<TMP_Text>(true);
                if (text == null) continue;
                var plate = button.transform.Find("RestlessDialogAction")?.gameObject;
                if (plate == null)
                {
                    plate = RestlessUi.Graphic(button.transform, "RestlessDialogAction", Color.white, false);
                    Ours.Add(plate);
                    plate.AddComponent<LayoutElement>().ignoreLayout = true;
                    RestlessUi.Stretch(plate, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    plate.transform.SetAsFirstSibling();
                    RestlessUi.PaperSurface(plate, true);
                    if (button.targetGraphic is Image native) Ghost(native);
                    BindCraftControl(button, plate.GetComponent<Image>(), null, false);
                }
            }
        }
        foreach (var text in DialogCopy)
        {
            if (text == null || !text.gameObject.activeInHierarchy
                || text.GetComponentInParent<TMP_InputField>() != null
                || text.GetComponentInParent<InputField>() != null) continue;
            Face(text, "dialog" + text.GetInstanceID(), RestlessUi.Text, RestlessUi.BodySize, true);
        }
    }
}
