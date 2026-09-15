using System.Collections.Generic;
using System.Reflection;
using Jotunn.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.Core;

// One language for every Restless surface (F8, buffs, map, notices, hotbar, Tab):
//   Strip  — sliced row-idle (StripBorder), RowTint. Settings rows, buffs, biome bar, toasts.
//   Chip   — same slice, tighter ChipBorder, ChipTint. Keys, Q/E, switches.
//   Tray   — solid Nested + Rim. F8 sheet, item inspect. Never 9-slice row-idle tall.
//   TrayHead — F8 title atom. Ink Strip at RowWidth, inset (TrayPad / TrayGutter).
//   TitleTear — solid Ink Strip behind a title, may escape the tray top-right.
//   Rim    — Unity Outline (Accent 0.85, 1.2/−1.2). Trays always. Torn atoms skip
//            it except F8 row hover. Call Rim(); do not copy Outline by hand.
//   Well   — generated dark circle. Do not use on the hotbar or toast icons.
//   Card   — abandoned tall-lip experiment. Do not use.
//   Type   — Averia regular; Bold only at TitleSize. Text / Muted / Accent.
//   Fade   — FadeSeconds. Slide uses the same clock.
//   HudRow — Strip + left-anchored torn fill + icon + title + meta. Notices and buffs.
//   HudMeter — thin Ink track sized to max HP/stamina + masked fill (current).
//              18px on purpose (thinner is the look). Health left of Bar
//              (grows left). Stamina right of Bar (grows right).
//   Wear    — vertical torn slit on the right of a slot (row-idle left+right
//              edges, 9-sliced top/bottom). Cream when healthy, red when not.
//   KeyChip— Chip + Bind(). Mouse binds use cream mouse-left/right/middle.
//   KeyStack — KeyChip column. BottomRight (combat / hammer / Tab) stays
//              bottom-right; baseline = hotbar bottom. Look is world hover.
//   Quiet  — CanvasGroup hide. Loud restores. MapOpen / InventoryOpen are the HUD gates.
//   HudTuck — FadeSeconds slide. Map + buffs + notices ease off while Tab / chest is open.
//   Dock   — TopLeft = every notice. Bar = item hotbar. Health docks left of Bar
//            (grows left). Stamina docks right of Bar (grows right).
//            BottomRight = action hints. No TMP.
//            No hotkey-btn / slider.png / toggle-on / map-compass / 9-sliced
//            panel-back as a card.
//   Mask   — panel-back alpha only (minimap tear). Never UI-Mask the vanilla map RawImage.
internal static partial class RestlessUi
{
    public static readonly Color Text = Hex(0xE2D6C0);
    public static readonly Color Muted = Hex(0x8A7F6E);
    public static readonly Color Accent = Hex(0xFFDD99);
    public static readonly Color ScrollHit = Color.clear;
    public static readonly Color Dim = new(0f, 0f, 0f, 0.88f);
    public static readonly Color RowTint = new(0.28f, 0.26f, 0.24f, 1f);
    public static readonly Color Nested = new(0.20f, 0.18f, 0.16f, 1f);
    public static readonly Color ChipTint = new(0.32f, 0.29f, 0.26f, 1f);
    public static readonly Color Ink = new(0.10f, 0.09f, 0.08f, 1f);
    public static readonly Color SwitchOn = new(0.82f, 0.62f, 0.28f, 1f);
    public static readonly Color FillTint = new(0.82f, 0.62f, 0.28f, 0.45f);
    public static readonly Color Selected = new(0.93f, 0.74f, 0.36f, 1f);
    public static readonly Color HealthTint = Hex(0xC4453A);
    public static readonly Color StaminaTint = Hex(0xD6A83C);

    public static readonly Vector4 StripBorder = new(28f, 14f, 28f, 14f);
    // Taller top cap so a tall plate does not stretch the torn lip. Bottom 14px already holds.
    public static readonly Vector4 TallBorder = new(28f, 14f, 28f, 42f);
    public static readonly Vector4 ChipBorder = new(24f, 12f, 24f, 12f);
    public static readonly Vector4 WearBorder = new(0f, 28f, 0f, 28f);

    public const int TitleSize = 32;
    public const int BodySize = 18;
    public const int HintSize = 16;
    public const int MetaSize = 15;
    public const int HudSize = 13;
    public const int HudMeta = 12;

    public const float PanelWidth = 960f;
    public const float PanelHeight = 720f;
    public const float BarWidth = 960f;
    public const float BarHeight = 64f;
    public const float RowWidth = 860f;
    public const float RowHeight = 52f;
    public const float RowGap = 8f;
    public const float TrayGutter = (PanelWidth - RowWidth) * 0.5f;
    public const float TrayPad = 16f;
    public const float PlateInset = 28f;
    public const float HeadOverhang = 24f;
    public const float HeadLift = 12f;
    public const float ToggleWidth = 56f;
    public const float ToggleHeight = 30f;
    public const float KeyWidth = 132f;
    public const float KeyHeight = 36f;
    public const float FadeSeconds = 0.14f;
    public const float SlideSeconds = FadeSeconds;

    public enum HudDock
    {
        Center,
        Right,
        TopLeft,
        MapEast,
        Bar,
        BottomRight,
        Look
    }

    private static Sprite? _circle;

    internal static Color Hex(int rgb) =>
        new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

    public static float Toward(float current, float target) =>
        Mathf.MoveTowards(current, target, Time.unscaledDeltaTime / FadeSeconds);

    public static Sprite Circle(int px = 64)
    {
        if (_circle != null)
            return _circle;
        var tex = new Texture2D(px, px, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var mid = (px - 1) * 0.5f;
        var r = mid - 1.2f;
        for (var y = 0; y < px; y++)
        {
            for (var x = 0; x < px; x++)
            {
                var d = Vector2.Distance(new Vector2(x, y), new Vector2(mid, mid));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 1f)));
            }
        }

        tex.Apply();
        _circle = Sprite.Create(tex, new Rect(0f, 0f, px, px), new Vector2(0.5f, 0.5f), 100f);
        _circle.name = "restless-well";
        return _circle;
    }

    public static GameObject Well(Transform parent, string name, Color? tint = null)
    {
        var go = Graphic(parent, name, tint ?? Ink, false);
        var image = go.GetComponent<Image>();
        image.sprite = Circle();
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        return go;
    }

    public static RectTransform Place(GameObject go, HudDock dock, Vector2 size, Vector2 extra)
    {
        return dock switch
        {
            HudDock.Right => Pin(go, new Vector2(1f, 0.5f), new Vector2(1f, 0f), new Vector2(-28f, -16f) + extra, size),
            HudDock.Center => Pin(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 72f) + extra, size),
            HudDock.TopLeft => Pin(go, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f) + extra, size),
            HudDock.MapEast => Pin(go, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -8f) + extra, size),
            HudDock.Bar => Pin(go, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 96f) + extra, size),
            HudDock.BottomRight => Pin(go, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 28f) + extra, size),
            HudDock.Look => Pin(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -40f) + extra, size),
            _ => Pin(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), extra, size)
        };
    }

    public static Font Face(bool bold = false)
    {
        if (bold && GUIManager.Instance?.AveriaSerifBold != null)
            return GUIManager.Instance.AveriaSerifBold;
        if (GUIManager.Instance?.AveriaSerif != null)
            return GUIManager.Instance.AveriaSerif;
        if (GUIManager.Instance?.AveriaSerifBold != null)
            return GUIManager.Instance.AveriaSerifBold;
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    public static GameObject Node(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        return go;
    }

    public static GameObject Graphic(Transform parent, string name, Color color, bool raycast = true)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return go;
    }

    public static GameObject Picture(Transform parent, string name, string asset, bool raycast = false)
    {
        var go = Graphic(parent, name, Color.white, raycast);
        var image = go.GetComponent<Image>();
        var sprite = Kit.Sprite(asset);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
        }
        else
        {
            image.color = Ink;
        }

        return go;
    }

    public static RectTransform Stretch(GameObject go, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = offMin;
        rt.offsetMax = offMax;
        return rt;
    }

    public static RectTransform Pin(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static void CopyRect(RectTransform dest, RectTransform src)
    {
        dest.anchorMin = src.anchorMin;
        dest.anchorMax = src.anchorMax;
        dest.pivot = src.pivot;
        dest.anchoredPosition = src.anchoredPosition;
        dest.sizeDelta = src.sizeDelta;
        dest.offsetMin = src.offsetMin;
        dest.offsetMax = src.offsetMax;
        dest.localScale = src.localScale;
        dest.localRotation = src.localRotation;
    }

    public static void Dock(RectTransform plate, RectTransform target, Vector2 pivot, Vector2 extra)
    {
        var corners = new Vector3[4];
        target.GetWorldCorners(corners);
        plate.pivot = new Vector2(0.5f, 0.5f);
        plate.position = new Vector3(
            Mathf.Lerp(corners[0].x, corners[2].x, pivot.x),
            Mathf.Lerp(corners[0].y, corners[2].y, pivot.y),
            corners[0].z);
        plate.anchoredPosition += extra;
    }

    // Large map is open. Buffs / action hints / look hide. Hotbar, vitals, and
    // extra quick slots stay — they dock to Hotbar.TrySpan.
    public static bool MapOpen()
    {
        var map = Minimap.instance;
        return map != null && map.m_largeRoot != null && map.m_largeRoot.activeInHierarchy;
    }

    public static bool InventoryOpen() => InventoryGui.IsVisible();

    // Tab / chest takes the HUD. Same clock as Fade. Call Tick once a frame
    // (re-entrant). Map / buffs / notices ease off while inventory is open.
    public static class HudTuck
    {
        public static float Amount { get; private set; }
        private static readonly Dictionary<int, Vector2> Home = new();
        private static readonly Vector2 MapAway = new(56f, 72f);
        public static readonly Vector2 NoticeAway = new(-(HudRow.NoticeWidth + 48f), 40f);
        private static int _frame = -1;

        public static bool Want => InventoryOpen() && !MapOpen();

        public static float Ease
        {
            get
            {
                var t = Amount;
                return t * t * (3f - 2f * t);
            }
        }

        public static void Tick()
        {
            if (_frame == Time.frameCount)
                return;
            _frame = Time.frameCount;
            Amount = Toward(Amount, Want ? 1f : 0f);
            if (Amount < 0.001f)
                Amount = 0f;
            if (Amount > 0.999f)
                Amount = 1f;
        }

        public static void Map()
        {
            var map = Minimap.instance;
            var root = map != null && map.m_smallRoot != null ? map.m_smallRoot : map?.m_mapSmall;
            if (root == null)
                return;
            var rt = root.GetComponent<RectTransform>();
            if (rt == null)
                return;
            Shift(rt, MapAway);
            Fade(root);
        }

        public static void Fade(GameObject? go)
        {
            if (go == null)
                return;
            var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
            var e = Ease;
            cg.alpha = 1f - e;
            cg.blocksRaycasts = e < 0.45f;
            cg.interactable = e < 0.45f;
        }

        public static void Shift(RectTransform rt, Vector2 away)
        {
            var id = rt.GetInstanceID();
            if (Amount <= 0f)
            {
                if (Home.TryGetValue(id, out var home))
                    rt.anchoredPosition = home;
                else
                    Home[id] = rt.anchoredPosition;
                return;
            }

            if (!Home.ContainsKey(id))
                Home[id] = rt.anchoredPosition;
            rt.anchoredPosition = Home[id] + away * Ease;
        }
    }

    // Cream diamond on a locked slot. Opposite corner from quality.
    public static void DressLock(Transform host, bool locked)
    {
        var mark = host.Find("RestlessLock");
        if (!locked)
        {
            if (mark != null)
                mark.gameObject.SetActive(false);
            return;
        }

        if (mark == null)
            mark = Picture(host, "RestlessLock", "diamond").transform;

        mark.SetAsLastSibling();
        mark.gameObject.SetActive(true);
        Pin(mark.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -8f),
            new Vector2(12f, 12f));
        var img = mark.GetComponent<Image>();
        img.color = Accent;
        img.raycastTarget = false;
    }

    // Averia stack count. Vanilla amount / count TMP stays muted.
    public static void DressAmount(Transform host, int stack)
    {
        var mark = host.Find("RestlessAmount");
        if (stack <= 1)
        {
            if (mark != null)
                mark.gameObject.SetActive(false);
            return;
        }

        Text face;
        if (mark == null)
        {
            face = Label(host, "", HudMeta, Text, TextAnchor.LowerRight);
            face.gameObject.name = "RestlessAmount";
            Stretch(face.gameObject, Vector2.zero, Vector2.one, new Vector2(4f, 2f), new Vector2(-4f, 2f));
            mark = face.transform;
        }
        else
            face = mark.GetComponent<Text>();

        mark.SetAsLastSibling();
        mark.gameObject.SetActive(true);
        if (face == null)
            return;
        face.text = stack.ToString();
        face.color = Text;
        face.alignment = TextAnchor.LowerRight;
    }

    // Quality / upgrade level. Kit btn-small badge + Averia. Last sibling so
    // it sits on the icon, not under it. Hidden at 1 (vanilla).
    public static void DressQuality(Transform host, int quality, bool left = true)
    {
        var mark = host.Find("RestlessQuality");
        if (quality <= 1)
        {
            if (mark != null)
                mark.gameObject.SetActive(false);
            return;
        }

        if (mark == null)
        {
            var go = Slice(host, "RestlessQuality", "btn-small", ChipBorder);
            go.GetComponent<Image>().raycastTarget = false;
            var label = Label(go.transform, "", HudMeta, Accent, TextAnchor.MiddleCenter);
            Stretch(label.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            mark = go.transform;
        }

        mark.SetAsLastSibling();
        mark.gameObject.SetActive(true);
        var anchor = left ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
        Pin(mark.gameObject, anchor, anchor, left ? new Vector2(11f, -9f) : new Vector2(-11f, -9f),
            new Vector2(22f, 22f));
        mark.GetComponent<Image>().color = Color.Lerp(ChipTint, Accent, 0.2f);
        var text = mark.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = quality.ToString();
            text.color = Accent;
        }
    }

    public static void Quiet(GameObject? go)
    {
        if (go == null)
            return;
        var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
    }

    public static void Quiet(Component? part)
    {
        if (part != null)
            Quiet(part.gameObject);
    }

    public static void Loud(GameObject? go)
    {
        if (go == null)
            return;
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null)
            return;
        cg.alpha = 1f;
        cg.blocksRaycasts = true;
        cg.interactable = true;
    }

    public static void Loud(Component? part)
    {
        if (part != null)
            Loud(part.gameObject);
    }

    public static void HangUnder(RectTransform plate, RectTransform target, Vector2 extra)
    {
        var corners = new Vector3[4];
        target.GetWorldCorners(corners);
        plate.pivot = new Vector2(1f, 1f);
        plate.position = corners[3];
        plate.anchoredPosition += extra;
    }

    public static void ParkColumn(RectTransform plate, RectTransform column, RectTransform floor, float pad)
    {
        var mapBox = new Vector3[4];
        column.GetWorldCorners(mapBox);
        var hud = new Vector3[4];
        floor.GetWorldCorners(hud);
        var scale = plate.lossyScale.y < 0.01f ? 1f : plate.lossyScale.y;
        plate.pivot = new Vector2(1f, 0f);
        plate.position = new Vector3(mapBox[3].x, hud[0].y + pad * scale, mapBox[3].z);
    }

    public static Transform? Deep(Transform root, string name)
    {
        if (root.name == name)
            return root;
        for (var i = 0; i < root.childCount; i++)
        {
            var hit = Deep(root.GetChild(i), name);
            if (hit != null)
                return hit;
        }

        return null;
    }

    public static T? Deep<T>(Transform root, string name) where T : Component
    {
        var node = Deep(root, name);
        return node != null ? node.GetComponent<T>() : null;
    }

    public static string Bare(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "";
        if (raw.IndexOf("MISSING", System.StringComparison.OrdinalIgnoreCase) >= 0
            && raw.IndexOf("BUTTONDEF", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return "";
        if (raw.IndexOf("$button", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return "";
        if (raw.IndexOf('$') >= 0 && Localization.instance != null)
            raw = Localization.instance.Localize(raw);
        var sb = new System.Text.StringBuilder(raw.Length);
        var hide = false;
        var glue = false;
        var tag = new System.Text.StringBuilder();
        foreach (var c in raw)
        {
            if (c == '<')
            {
                hide = true;
                tag.Length = 0;
                glue = sb.Length > 0 && (char.IsLetterOrDigit(sb[sb.Length - 1]) || sb[sb.Length - 1] == ':');
                continue;
            }

            if (c == '>')
            {
                hide = false;
                var name = tag.ToString();
                if (name.StartsWith("space", System.StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("/space", System.StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append(' ');
                    glue = false;
                }

                continue;
            }

            if (hide)
            {
                tag.Append(c);
                continue;
            }

            var ch = c == '\u00A0' || c == '\u202F' || c == '\u2009' ? ' ' : c;
            if (glue && (char.IsLetterOrDigit(ch) || ch == '('))
                sb.Append(' ');
            glue = ch == ':';
            if (ch != '\r')
                sb.Append(ch);
        }

        return sb.ToString();
    }

    public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align)
    {
        var go = new GameObject("label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var ui = go.GetComponent<Text>();
        ui.font = Face(size >= TitleSize);
        ui.fontSize = size;
        ui.color = color;
        ui.alignment = align;
        ui.text = text;
        ui.raycastTarget = false;
        ui.supportRichText = false;
        ui.horizontalOverflow = HorizontalWrapMode.Overflow;
        ui.verticalOverflow = VerticalWrapMode.Truncate;
        return ui;
    }

    public static void Paint(GameObject root)
    {
        foreach (var text in root.GetComponentsInChildren<Text>(true))
            text.font = Face(text.fontSize >= TitleSize);
    }

    public static GameObject Slice(Transform parent, string name, string asset, Vector4 border, bool raycast = false)
    {
        var go = Picture(parent, name, asset, raycast);
        var image = go.GetComponent<Image>();
        var sprite = Kit.Sprite(asset, border);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
        }

        return go;
    }

    public static GameObject Strip(Transform parent, string name, Color? tint = null, bool raycast = false)
    {
        var go = Slice(parent, name, "row-idle", StripBorder, raycast);
        go.GetComponent<Image>().color = tint ?? RowTint;
        return go;
    }

    public static GameObject TallStrip(Transform parent, string name, Color? tint = null, bool raycast = false)
    {
        var go = Slice(parent, name, "row-idle", TallBorder, raycast);
        go.GetComponent<Image>().color = tint ?? RowTint;
        return go;
    }

    public static GameObject Chip(Transform parent, string name)
    {
        var go = Slice(parent, name, "row-idle", ChipBorder, true);
        go.GetComponent<Image>().color = ChipTint;
        return go;
    }

    // Chip + Averia on a vanilla Button. Caller ghosts the wood. Title from
    // ButtonCopy when the live label exists, else the fallback.
    public static GameObject ChipButton(Button button, string title, bool lit, Vector2? size = null)
    {
        var pin = ButtonSize(button, size);
        var hold = button.GetComponent<LayoutElement>();
        if (hold == null)
            hold = button.gameObject.AddComponent<LayoutElement>();
        hold.minWidth = hold.preferredWidth = pin.x;
        hold.minHeight = hold.preferredHeight = pin.y;

        var chip = button.transform.Find("RestlessChip");
        if (chip == null)
        {
            var go = Chip(button.transform, "RestlessChip");
            var label = Label(go.transform, title, HudSize, Text, TextAnchor.MiddleCenter);
            label.gameObject.name = "RestlessChipFace";
            Stretch(label.gameObject, Vector2.zero, Vector2.one, new Vector2(8f, 1f), new Vector2(-8f, -1f));
            chip = go.transform;
        }

        chip.SetAsLastSibling();
        Pin(chip.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, pin);
        chip.GetComponent<Image>().raycastTarget = true;
        PaintSlot(chip.gameObject, lit);
        var face = chip.Find("RestlessChipFace")?.GetComponent<Text>();
        if (face == null)
        {
            face = chip.GetComponentInChildren<Text>(true);
            if (face != null)
                face.gameObject.name = "RestlessChipFace";
        }

        if (face != null)
        {
            face.enabled = true;
            face.text = title;
            face.color = lit ? Accent : Text;
        }

        return chip.gameObject;
    }

    public static Vector2 ButtonSize(Button button, Vector2? preferred = null)
    {
        if (preferred is { x: > 8f, y: > 8f })
            return preferred.Value;
        var rt = button.GetComponent<RectTransform>();
        var w = rt != null ? rt.rect.width : 0f;
        var h = rt != null ? rt.rect.height : 0f;
        if (w < 8f)
            w = 160f;
        if (h < 8f)
            h = 36f;
        return new Vector2(w, h);
    }

    public static string ButtonCopy(Button button, string fallback)
    {
        foreach (var tmp in button.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp.transform.name.StartsWith("Restless") || HintNode(tmp.transform))
                continue;
            var n = tmp.gameObject.name.ToLowerInvariant();
            if (n.Contains("selected") || tmp.transform.parent != null
                && tmp.transform.parent.name.ToLowerInvariant().Contains("selected"))
                continue;
            var copy = Bare(tmp.text);
            if (copy.Length == 0 || copy is "Test" or "Button" or "Text" or "Label")
                continue;
            return copy;
        }

        return fallback;
    }

    public static bool HintNode(Transform? node)
    {
        for (var t = node; t != null; t = t.parent)
        {
            var n = t.name.ToLowerInvariant();
            if (n.Contains("gamepad") || n is "help_text" || n.Contains("binding"))
                return true;
        }

        return false;
    }

    public static void SilenceTmp(TMP_Text? tmp)
    {
        if (tmp == null || tmp.transform.name.StartsWith("Restless"))
            return;
        tmp.alpha = 0f;
        tmp.enabled = false;
        tmp.raycastTarget = false;
        tmp.maxVisibleCharacters = 0;
        var color = tmp.color;
        color.a = 0f;
        tmp.color = color;
        var cr = tmp.GetComponent<CanvasRenderer>();
        if (cr != null)
            cr.SetAlpha(0f);
        foreach (var sub in tmp.GetComponentsInChildren<CanvasRenderer>(true))
            sub.SetAlpha(0f);
    }

    // Unity Outline recipe. Torn atoms skip this except F8 row hover.
    public static Outline Rim(GameObject plate, Color? tint = null, bool on = true)
    {
        var outline = plate.GetComponent<Outline>() ?? plate.AddComponent<Outline>();
        var c = tint ?? Accent;
        outline.effectColor = new Color(c.r, c.g, c.b, 0.85f);
        outline.effectDistance = new Vector2(1.2f, -1.2f);
        outline.useGraphicAlpha = true;
        outline.enabled = on;
        return outline;
    }

    // Solid Nested field + Rim. One well that holds atoms. Do not 9-slice
    // row-idle over this plate.
    public static GameObject Tray(Transform parent, string name, bool raycast = true)
    {
        var go = Graphic(parent, name, Nested, raycast);
        var image = go.GetComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = Nested;
        Rim(go);
        return go;
    }

    // F8 title atom. Ink Strip, row width, inset from the tray Rim.
    public static GameObject TrayHead(GameObject tray, string name)
    {
        var go = tray.transform.Find(name)?.gameObject;
        if (go == null)
            go = Strip(tray.transform, name, Ink, true);
        Pin(go, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -TrayPad),
            new Vector2(RowWidth, BarHeight));
        return go;
    }

    // Solid Ink tear behind a title. Escapes the tray at the top-right only.
    // First sibling so Averia on the title stays in front.
    public static GameObject TitleTear(GameObject tray, RectTransform? title)
    {
        var go = tray.transform.Find("titleTear")?.gameObject;
        if (go == null)
            go = Strip(tray.transform, "titleTear", Ink, false);
        var img = go.GetComponent<Image>();
        img.color = Ink;
        img.raycastTarget = false;
        go.transform.SetAsFirstSibling();
        if (title == null || !title.gameObject.activeInHierarchy)
        {
            go.SetActive(false);
            return go;
        }

        go.SetActive(true);
        var box = new Vector3[4];
        title.GetWorldCorners(box);
        var pad = 14f;
        PlaceWorld(go.GetComponent<RectTransform>(), box[0].x - pad, box[0].y - 8f,
            box[2].x + HeadOverhang, box[1].y + HeadLift);
        return go;
    }

    private static void PlaceWorld(RectTransform plate, float minX, float minY, float maxX, float maxY)
    {
        var sx = Mathf.Abs(plate.lossyScale.x) < 0.01f ? 1f : Mathf.Abs(plate.lossyScale.x);
        var sy = Mathf.Abs(plate.lossyScale.y) < 0.01f ? 1f : Mathf.Abs(plate.lossyScale.y);
        plate.anchorMin = plate.anchorMax = new Vector2(0.5f, 0.5f);
        plate.pivot = new Vector2(0.5f, 0.5f);
        plate.position = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, plate.position.z);
        plate.sizeDelta = new Vector2((maxX - minX) / sx, (maxY - minY) / sy);
    }

    // Nested field with real kit chrome on the rim — row-idle lips (same as a
    // HudRow) and wear-slit sides. Abandoned: lips hid inspect titles. Use Tray.
    public static GameObject Card(Transform parent, string name, Color tint, bool raycast = true)
    {
        var go = parent.Find(name)?.gameObject;
        if (go == null)
            go = Graphic(parent, name, Color.clear, raycast);
        var root = go.GetComponent<Image>();
        root.sprite = null;
        root.type = Image.Type.Simple;
        root.color = Color.clear;
        root.raycastTarget = raycast;

        var dead = go.transform.Find("rim");
        if (dead != null)
            Object.Destroy(dead.gameObject);

        foreach (var stale in new[] { "edgeLeft", "edgeRight" })
        {
            var extra = go.transform.Find(stale);
            if (extra != null)
                Object.Destroy(extra.gameObject);
        }

        var fill = go.transform.Find("fill");
        if (fill == null)
            fill = Graphic(go.transform, "fill", tint, false).transform;
        Stretch(fill.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var fillImg = fill.GetComponent<Image>();
        fillImg.sprite = null;
        fillImg.color = tint;
        fillImg.raycastTarget = false;
        fill.SetAsFirstSibling();

        const float lip = 40f;
        Edge(go.transform, "edgeTop", "row-idle", StripBorder, RowTint,
            new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -lip), Vector2.zero);
        Edge(go.transform, "edgeBot", "row-idle", StripBorder, RowTint,
            Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, lip));
        return go;
    }

    private static void Edge(Transform parent, string name, string asset, Vector4 border, Color tint,
        Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
    {
        var edge = parent.Find(name);
        if (edge == null)
            edge = Slice(parent, name, asset, border, false).transform;
        Stretch(edge.gameObject, min, max, offMin, offMax);
        var img = edge.GetComponent<Image>();
        img.color = tint;
        img.raycastTarget = false;
    }

    public static void PaintSlot(GameObject plate, bool lit)
    {
        var image = plate.GetComponent<Image>();
        var sprite = Kit.Sprite("row-idle", StripBorder);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
        }

        image.color = lit ? Color.Lerp(RowTint, Accent, 0.62f) : RowTint;
    }

    // Torn plate + Wear on a vanilla inventory / hotbar cell. Hides GuiBar.
    // Does not add a key chip. Extra Tab / HUD slots call this.
    public static GameObject DressSlot(GameObject cell, Image? icon, bool lit, ItemDrop.ItemData? item,
        List<Behaviour>? hidden = null, bool fillCell = false, bool locked = false)
    {
        var plate = cell.transform.Find("RestlessSlot");
        if (plate == null)
        {
            HideSlotChrome(cell, icon, hidden);
            var old = cell.transform.Find("RestlessWell");
            if (old != null)
                Object.Destroy(old.gameObject);
            plate = Strip(cell.transform, "RestlessSlot").transform;
            plate.SetAsFirstSibling();
        }

        var size = fillCell ? CellSize(cell, icon) : SlotSize(cell, icon) + (lit ? 14f : 8f);
        var plateRt = plate.GetComponent<RectTransform>();
        if (plateRt != null && (Mathf.Abs(plateRt.sizeDelta.x - size) > 0.5f
            || Mathf.Abs(plateRt.sizeDelta.y - size) > 0.5f))
            Pin(plate.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(size, size));
        var plateImg = plate.GetComponent<Image>();
        plateImg.raycastTarget = true;
        PaintSlot(plate.gameObject, lit);

        var wear = Wear.Of(plate);
        if (item?.m_shared != null && item.m_shared.m_useDurability && item.GetMaxDurability() > 0f)
            wear.Set(item.GetDurabilityPercentage());
        else
            wear.Hide();

        HideVanillaSlotText(cell);
        DressAmount(cell.transform, item?.m_stack ?? 0);
        DressQuality(cell.transform, item?.m_quality ?? 0);
        DressLock(cell.transform, locked);
        return plate.gameObject;
    }

    public static void HideSlotChrome(GameObject cell, Image? icon, List<Behaviour>? hidden = null)
    {
        foreach (var bar in cell.GetComponentsInChildren<GuiBar>(true))
        {
            Mute(bar, hidden);
            bar.gameObject.SetActive(false);
        }

        foreach (var image in cell.GetComponentsInChildren<Image>(true))
        {
            if (KeepSlotImage(image, icon))
                continue;
            Mute(image, hidden);
            var n = image.gameObject.name.ToLowerInvariant();
            if (n.Contains("durab") || n.Contains("health") || n.Contains("equip") || n.Contains("queued")
                || n.Contains("selected") || n.Contains("drop") || image.GetComponent<GuiBar>() != null)
                image.gameObject.SetActive(false);
        }

        foreach (var tmp in cell.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp.transform.name.StartsWith("Restless"))
                continue;
            Mute(tmp, hidden);
            if (VanillaSlotText(tmp.gameObject.name))
            {
                SilenceTmp(tmp);
                tmp.gameObject.SetActive(false);
            }
        }

        foreach (var text in cell.GetComponentsInChildren<Text>(true))
        {
            if (text.transform.name.StartsWith("Restless") || text.transform.parent != null &&
                text.transform.parent.name.StartsWith("Restless"))
                continue;
            Mute(text, hidden);
            if (VanillaSlotText(text.gameObject.name))
                text.gameObject.SetActive(false);
        }
    }

    public static void HideVanillaSlotText(GameObject cell)
    {
        foreach (var tmp in cell.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp.transform.name.StartsWith("Restless") || !VanillaSlotText(tmp.gameObject.name))
                continue;
            SilenceTmp(tmp);
            tmp.gameObject.SetActive(false);
        }

        foreach (var text in cell.GetComponentsInChildren<Text>(true))
        {
            if (text.transform.name.StartsWith("Restless") || text.transform.parent != null &&
                text.transform.parent.name.StartsWith("Restless") || !VanillaSlotText(text.gameObject.name))
                continue;
            text.enabled = false;
            text.gameObject.SetActive(false);
        }
    }

    private static bool VanillaSlotText(string name)
    {
        var n = name.ToLowerInvariant();
        return n.Contains("amount") || n.Contains("count") || n.Contains("stack")
               || n.Contains("quality") || n.Contains("level");
    }

    private static float SlotSize(GameObject cell, Image? icon)
    {
        if (icon != null)
            return Mathf.Max(icon.rectTransform.rect.width, 48f);
        var rt = cell.GetComponent<RectTransform>();
        return rt != null ? Mathf.Max(rt.rect.height, 48f) : 56f;
    }

    private static float CellSize(GameObject cell, Image? icon)
    {
        var rt = cell.GetComponent<RectTransform>();
        if (rt != null && rt.rect.width > 8f && rt.rect.height > 8f)
            return Mathf.Min(rt.rect.width, rt.rect.height);
        return SlotSize(cell, icon);
    }

    private static bool KeepSlotImage(Image image, Image? icon)
    {
        if (image == icon)
            return true;
        if (image.transform.name.StartsWith("Restless"))
            return true;
        if (image.transform.parent != null && image.transform.parent.name.StartsWith("Restless"))
            return true;
        if (image.GetComponent<Button>() != null)
        {
            image.color = Color.clear;
            image.raycastTarget = true;
            image.enabled = true;
            return true;
        }

        var n = image.gameObject.name.ToLowerInvariant();
        return n.Contains("food") || n.Contains("noteleport") || n.Contains("no_teleport");
    }

    private static void Mute(Behaviour behaviour, List<Behaviour>? hidden)
    {
        if (behaviour == null)
            return;
        if (behaviour.enabled && hidden != null)
            hidden.Add(behaviour);
        behaviour.enabled = false;
    }

    // Resolve a vanilla ZInput button to Averia-facing copy. Tries $KEY_ tokens,
    // then GetBoundKeyString, then a short mouse alias. Never return MISSING BUTTONDEF.
    public static string Bind(params string[] names)
    {
        foreach (var name in names)
        {
            if (TryBind(name, out var face))
                return face;
        }

        foreach (var name in names)
        {
            var mapped = Alias(name);
            if (Usable(mapped) && !string.Equals(mapped, name, System.StringComparison.OrdinalIgnoreCase))
                return mapped;
        }

        return "";
    }

    public static Sprite? ItemIcon(string prefab, string? token = null)
    {
        var db = ObjectDB.instance;
        if (db == null)
            return null;
        foreach (var name in new[] { prefab, prefab.ToLowerInvariant(), "item_" + prefab.ToLowerInvariant() })
        {
            var icon = IconOf(db.GetItemPrefab(name)?.GetComponent<ItemDrop>());
            if (icon != null)
                return icon;
        }

        if (db.m_items == null)
            return null;
        foreach (var go in db.m_items)
        {
            if (go == null)
                continue;
            var drop = go.GetComponent<ItemDrop>();
            var shared = drop?.m_itemData?.m_shared;
            if (shared == null)
                continue;
            if (go.name.IndexOf("offering", System.StringComparison.OrdinalIgnoreCase) >= 0)
                continue;
            var hit = (!string.IsNullOrEmpty(token) && shared.m_name == token)
                || (!string.IsNullOrEmpty(shared.m_name)
                    && shared.m_name.IndexOf(prefab, System.StringComparison.OrdinalIgnoreCase) >= 0)
                || go.name.IndexOf(prefab, System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (!hit)
                continue;
            var icon = IconOf(drop);
            if (icon != null)
                return icon;
        }

        return null;
    }

    private static Sprite? _bowl;
    private static bool _bowlLogged;

    public static Sprite? BowlIcon()
    {
        if (_bowl != null)
            return _bowl;
        var found = VanillaFork() ?? FindBowl();
        if (found != null)
        {
            _bowl = found;
            if (!_bowlLogged)
            {
                _bowlLogged = true;
                Plugin.Log.LogInfo("eat: " + found.name);
            }

            return _bowl;
        }

        if (!_bowlLogged && ObjectDB.instance?.m_items != null && ObjectDB.instance.m_items.Count > 0)
        {
            _bowlLogged = true;
            Plugin.Log.LogInfo("eat: kit eat-fork");
        }

        return Kit.Sprite("eat-fork");
    }

    private static bool _forkScan;

    private static Sprite? VanillaFork()
    {
        var hud = global::Hud.instance;
        if (hud != null)
        {
            foreach (var name in new[] { "m_foodIcon", "m_eatIcon", "m_foodSprite" })
            {
                var sprite = SpriteOf(typeof(global::Hud).GetField(name)?.GetValue(hud));
                if (sprite != null)
                    return sprite;
            }

            if (hud.m_foodIcons != null)
            {
                foreach (var img in hud.m_foodIcons)
                {
                    if (img?.sprite != null && ForkName(img.sprite.name, img.gameObject.name))
                        return img.sprite;
                }
            }

            if (hud.m_foodBarRoot != null)
            {
                foreach (var img in hud.m_foodBarRoot.GetComponentsInChildren<Image>(true))
                {
                    if (img.sprite != null && ForkName(img.sprite.name, img.gameObject.name))
                        return img.sprite;
                }
            }
        }

        if (_forkScan)
            return null;
        _forkScan = true;
        foreach (var sprite in Resources.FindObjectsOfTypeAll<Sprite>())
        {
            if (sprite != null && ForkName(sprite.name))
                return sprite;
        }

        return null;
    }

    private static bool ForkName(params string[] names)
    {
        foreach (var name in names)
        {
            if (string.IsNullOrEmpty(name))
                continue;
            if (name.IndexOf("fork", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return name.IndexOf("pitch", System.StringComparison.OrdinalIgnoreCase) < 0;
        }

        return false;
    }

    private static Sprite? SpriteOf(object? value) =>
        value as Sprite
        ?? (value as Image)?.sprite
        ?? (value as GameObject)?.GetComponent<Image>()?.sprite
        ?? (value as Component)?.GetComponent<Image>()?.sprite;

    private static Sprite? FindBowl()
    {
        foreach (var name in new[] { "Bowl", "bowl", "item_bowl", "WoodenBowl", "WoodBowl" })
        {
            var icon = NamedItemIcon(name);
            if (icon != null)
                return icon;
        }

        return ItemIcon("Bowl", "$item_bowl")
            ?? ItemIcon("Bowl", "$item_woodbowl")
            ?? ScanBowlItems()
            ?? ScanBowlRecipes()
            ?? ScanBowlInventory()
            ?? FoodBowl();
    }

    private static Sprite? NamedItemIcon(string name)
    {
        var go = ObjectDB.instance?.GetItemPrefab(name);
        if (go == null && ZNetScene.instance != null)
            go = ZNetScene.instance.GetPrefab(name);
        if (go == null)
            go = PrefabManager.Instance?.GetPrefab(name);
        return Offering(go) ? null : IconOf(go?.GetComponent<ItemDrop>());
    }

    private static Sprite? ScanBowlItems()
    {
        var items = ObjectDB.instance?.m_items;
        if (items == null)
            return null;
        foreach (var go in items)
        {
            if (Offering(go))
                continue;
            var drop = go.GetComponent<ItemDrop>();
            var shared = drop?.m_itemData?.m_shared;
            if (shared == null || !LooksLikeBowl(go.name, shared.m_name))
                continue;
            var icon = IconOf(drop);
            if (icon != null)
                return icon;
        }

        return null;
    }

    private static Sprite? ScanBowlRecipes()
    {
        var recipes = ObjectDB.instance?.m_recipes;
        if (recipes == null)
            return null;
        foreach (var recipe in recipes)
        {
            var drop = recipe?.m_item;
            if (drop == null || Offering(drop.gameObject))
                continue;
            var shared = drop.m_itemData?.m_shared;
            if (shared == null || !LooksLikeBowl(drop.gameObject.name, shared.m_name))
                continue;
            var icon = IconOf(drop);
            if (icon != null)
                return icon;
        }

        return null;
    }

    private static Sprite? ScanBowlInventory()
    {
        var pack = Player.m_localPlayer?.GetInventory()?.GetAllItems();
        if (pack == null)
            return null;
        foreach (var item in pack)
        {
            if (item?.m_shared == null || !LooksLikeBowl("", item.m_shared.m_name))
                continue;
            var shared = item.m_shared;
            try
            {
                var icon = item.GetIcon();
                if (icon != null)
                    return icon;
            }
            catch
            {
                // older ItemData has no GetIcon
            }

            if (shared.m_icons != null && shared.m_icons.Length > 0)
                return shared.m_icons[0];
        }

        return null;
    }

    private static bool Offering(GameObject? go) =>
        go != null && go.name.IndexOf("offer", System.StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool LooksLikeBowl(string prefab, string? token)
    {
        if (HasBowl(prefab) && prefab.IndexOf("offer", System.StringComparison.OrdinalIgnoreCase) < 0)
            return true;
        if (string.IsNullOrEmpty(token))
            return false;
        if (HasBowl(token) && token.IndexOf("offer", System.StringComparison.OrdinalIgnoreCase) < 0)
            return true;
        if (Localization.instance == null)
            return false;
        return Localization.instance.Localize(token)
            .Equals("Bowl", System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasBowl(string? text) =>
        !string.IsNullOrEmpty(text)
        && text.IndexOf("bowl", System.StringComparison.OrdinalIgnoreCase) >= 0;

    private static Sprite? FoodBowl()
    {
        var root = global::Hud.instance?.m_foodBarRoot;
        if (root == null)
            return null;
        foreach (var img in root.GetComponentsInChildren<Image>(true))
        {
            if (img.sprite == null)
                continue;
            if (img.gameObject.name.IndexOf("bowl", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return img.sprite;
        }

        return null;
    }

    public static Sprite? IconOf(ItemDrop? drop) => IconOf(drop?.m_itemData);

    public static Sprite? IconOf(ItemDrop.ItemData? item)
    {
        if (item?.m_shared == null)
            return null;
        try
        {
            var icon = item.GetIcon();
            if (icon != null)
                return icon;
        }
        catch
        {
            // older ItemData has no GetIcon
        }

        var icons = item.m_shared.m_icons;
        if (icons == null || icons.Length == 0)
            return null;
        return icons[Mathf.Clamp(item.m_variant, 0, icons.Length - 1)];
    }

    public static bool Bound(params string[] names)
    {
        var key = Bind(names);
        if (string.IsNullOrEmpty(key))
            return false;
        foreach (var name in names)
        {
            if (string.Equals(key, name, System.StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    public static GameObject KeyChip(Transform parent, string name)
    {
        var chip = Chip(parent, name);
        var verb = Label(chip.transform, "", HudMeta, Text, TextAnchor.MiddleLeft);
        verb.gameObject.name = "verb";
        Stretch(verb.gameObject, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-58f, 0f));
        verb.horizontalOverflow = HorizontalWrapMode.Overflow;
        var face = Label(chip.transform, "", HudMeta, Text, TextAnchor.MiddleRight);
        face.gameObject.name = "face";
        Pin(face.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f),
            new Vector2(48f, 20f));
        var icon = Graphic(chip.transform, "glyph", Text, false);
        Pin(icon, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(16f, 20f));
        var image = icon.GetComponent<Image>();
        image.preserveAspect = true;
        image.enabled = false;
        chip.GetComponent<Image>().raycastTarget = false;
        return chip;
    }

    public static void SetKeyChip(GameObject chip, string verb, params string[] buttons)
    {
        var key = Bind(buttons);
        if (string.IsNullOrEmpty(key) && verb == "Rotate")
            key = "Q / E";
        PaintChip(chip, verb, key);
    }

    public static void PaintChip(GameObject chip, string verb, string face)
    {
        if (chip.transform.Find("verb") == null)
            return;
        if (face == "1-8")
            face = "1–8";
        var mouse = MouseSprite(face);
        var verbUi = chip.transform.Find("verb")?.GetComponent<Text>();
        var faceUi = chip.transform.Find("face")?.GetComponent<Text>();
        var icon = chip.transform.Find("glyph")?.GetComponent<Image>();
        if (verbUi != null)
        {
            verbUi.supportRichText = false;
            verbUi.alignment = TextAnchor.MiddleLeft;
            verbUi.text = verb ?? "";
        }

        if (icon != null)
        {
            icon.enabled = mouse != null;
            icon.sprite = mouse;
            icon.color = Text;
        }

        if (faceUi != null)
        {
            faceUi.supportRichText = false;
            var extra = face is "LMB×2" ? "×2" : "";
            faceUi.enabled = mouse == null || extra.Length > 0;
            faceUi.text = mouse == null ? face : extra;
        }
    }

    public static string KeyFace(string raw)
    {
        raw = raw.Trim();
        if (raw.StartsWith("$KEY_", System.StringComparison.Ordinal))
            return Bind(raw.Substring(5));
        if (raw.Equals("Use", System.StringComparison.OrdinalIgnoreCase)
            || raw.Equals("UseItem", System.StringComparison.OrdinalIgnoreCase))
            return Bind("Use", "UseItem", "JoyUse");
        var mapped = Alias(raw);
        return Usable(mapped) ? mapped : "";
    }

    private static Sprite? MouseSprite(string key)
    {
        if (key is "LMB" or "LMB×2")
            return Kit.Sprite("mouse-left");
        if (key is "RMB")
            return Kit.Sprite("mouse-right");
        if (key is "MMB")
            return Kit.Sprite("mouse-middle");
        return null;
    }

    // KeyChip column, or one torn Look plate. Features pass rows; they do not Pin.
    public sealed class KeyStack
    {
        public const float ChipW = 156f;
        public const float ChipH = 24f;
        public const float Gap = 4f;
        public const float TitleH = 26f;
        public const float PlateW = 236f;
        public const float StoryW = 360f;
        public const float Pad = 14f;

        public readonly GameObject Root;
        private readonly List<GameObject> _chips = new();
        private readonly Text? _title;
        private readonly Text? _body;
        private readonly Text? _meta;
        private readonly bool _down;
        private readonly bool _plated;
        private readonly float _rowW;
        private readonly Transform _rows;

        private KeyStack(GameObject root, Text? title, Text? body, Text? meta, bool down, bool plated, float rowW,
            Transform rows)
        {
            Root = root;
            _title = title;
            _body = body;
            _meta = meta;
            _down = down;
            _plated = plated;
            _rowW = rowW;
            _rows = rows;
        }

        public static KeyStack BottomRight(Transform parent)
        {
            var root = Node(parent, "RestlessHints");
            Place(root, HudDock.BottomRight, new Vector2(HudRow.BuffWidth, ChipH), Vector2.zero);
            return new KeyStack(root, null, null, null, false, false, HudRow.BuffWidth, root.transform);
        }

        public static KeyStack Look(Transform parent)
        {
            var stack = Plate(parent, "RestlessLook");
            Place(stack.Root, HudDock.Look, new Vector2(PlateW, 48f), Vector2.zero);
            return stack;
        }

        public static KeyStack Plate(Transform parent, string name)
        {
            var root = Node(parent, name);
            var plate = Strip(root.transform, "plate");
            Stretch(plate, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var title = Label(plate.transform, "", BodySize, Text, TextAnchor.MiddleCenter);
            Pin(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -Pad),
                new Vector2(PlateW - Pad * 2f, TitleH));
            var body = Label(plate.transform, "", HudSize, Text, TextAnchor.UpperLeft);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            body.gameObject.SetActive(false);
            var meta = Label(plate.transform, "", HudMeta, Muted, TextAnchor.MiddleCenter);
            Pin(meta.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -(Pad + TitleH)), new Vector2(PlateW - Pad * 2f, 18f));
            meta.gameObject.SetActive(false);
            return new KeyStack(root, title, body, meta, true, true, PlateW, plate.transform);
        }

        public void Hide() => Root.SetActive(false);

        public void Set(IReadOnlyList<(string verb, string[] buttons)> rows)
        {
            var faces = new List<(string verb, string face)>();
            foreach (var row in rows)
            {
                var key = Bind(row.buttons);
                if (string.IsNullOrEmpty(key) && row.verb == "Rotate")
                    key = "Q / E";
                if (string.IsNullOrEmpty(key))
                    continue;
                faces.Add((row.verb, key));
            }

            Paint("", "", "", faces, PlateW);
        }

        public void Set(string title, string extra, IReadOnlyList<(string verb, string face)> rows) =>
            Paint(title, "", extra, rows, PlateW);

        public void SetStory(string title, string body, string extra,
            IReadOnlyList<(string verb, string face)> rows) =>
            Paint(title, body, extra, rows, StoryW);

        private void Paint(string title, string body, string extra, IReadOnlyList<(string verb, string face)> rows,
            float wide)
        {
            if (rows.Count == 0 && string.IsNullOrEmpty(title) && string.IsNullOrEmpty(body))
            {
                Hide();
                return;
            }

            Root.SetActive(true);
            var story = !string.IsNullOrEmpty(body);
            if (!_plated)
                wide = _rowW;
            var head = _plated ? Pad : 0f;
            if (_title != null)
            {
                _title.text = title ?? "";
                _title.alignment = story ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
                _title.gameObject.SetActive(!string.IsNullOrEmpty(title));
                if (!string.IsNullOrEmpty(title))
                {
                    Pin(_title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -head), new Vector2(wide - Pad * 2f, TitleH));
                    head += TitleH;
                }
            }

            if (_body != null)
            {
                _body.text = body ?? "";
                _body.gameObject.SetActive(story);
                if (story)
                {
                    Pin(_body.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -head), new Vector2(wide - Pad * 2f, 8f));
                    var tall = Mathf.Clamp(_body.preferredHeight, HudSize, 88f);
                    Pin(_body.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -head), new Vector2(wide - Pad * 2f, tall));
                    head += tall + 4f;
                }
            }

            if (_meta != null)
            {
                _meta.text = extra ?? "";
                _meta.alignment = story ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
                _meta.gameObject.SetActive(!string.IsNullOrEmpty(extra));
                if (!string.IsNullOrEmpty(extra))
                {
                    Pin(_meta.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -head), new Vector2(wide - Pad * 2f, 18f));
                    head += 18f;
                }
            }

            var n = rows.Count;
            var chipW = _plated ? wide - Pad * 2f : wide;
            var chips = n * ChipH + Mathf.Max(0, n - 1) * Gap;
            Root.GetComponent<RectTransform>().sizeDelta =
                new Vector2(wide, head + chips + (_plated ? Pad : 0f));

            while (_chips.Count < n)
                _chips.Add(LookRow());
            if (n > 0 && _chips[0].transform.Find("verb") == null)
            {
                foreach (var old in _chips)
                    Object.Destroy(old);
                _chips.Clear();
                while (_chips.Count < n)
                    _chips.Add(LookRow());
            }

            var origin = _down ? new Vector2(0.5f, 1f) : new Vector2(1f, 0f);
            for (var i = 0; i < _chips.Count; i++)
            {
                if (i >= n)
                {
                    _chips[i].SetActive(false);
                    continue;
                }

                PaintChip(_chips[i], rows[i].verb, rows[i].face);
                var y = _down ? -(head + i * (ChipH + Gap)) : i * (ChipH + Gap);
                Pin(_chips[i], origin, origin, new Vector2(0f, y),
                    new Vector2(chipW, ChipH));
                _chips[i].SetActive(true);
            }
        }

        private GameObject LookRow()
        {
            var chip = KeyChip(_rows, "hint" + _chips.Count);
            if (_plated)
                chip.GetComponent<Image>().enabled = false;
            return chip;
        }
    }

    private static bool TryBind(string name, out string face)
    {
        face = "";
        if (string.IsNullOrEmpty(name))
            return false;

        if (Localization.instance != null)
        {
            var token = Localization.instance.Localize("$KEY_" + name);
            if (Usable(token) && token != "$KEY_" + name)
            {
                face = Alias(token);
                return Usable(face);
            }
        }

        if (ZInput.instance == null)
            return false;

        string raw;
        try
        {
            raw = ZInput.instance.GetBoundKeyString(name, false);
        }
        catch
        {
            return false;
        }

        if (!Usable(raw))
            return false;
        face = FaceKey(raw);
        return Usable(face);
    }

    private static bool Usable(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.IndexOf("MISSING", System.StringComparison.OrdinalIgnoreCase) < 0
        && value.IndexOf("BUTTONDEF", System.StringComparison.OrdinalIgnoreCase) < 0
        && value.IndexOf("<sprite", System.StringComparison.OrdinalIgnoreCase) < 0
        && value.IndexOf("sprite=", System.StringComparison.OrdinalIgnoreCase) < 0
        && value.IndexOf("$button", System.StringComparison.OrdinalIgnoreCase) < 0
        && value.IndexOf('<') < 0
        && !value.StartsWith("$KEY_", System.StringComparison.Ordinal);

    private static string FaceKey(string raw)
    {
        var loc = Localization.instance != null ? Localization.instance.Localize(raw) : raw;
        if (!Usable(loc) || loc.IndexOf('$') >= 0)
            return Alias(raw);
        return Alias(loc);
    }

    private static string Alias(string raw)
    {
        var t = raw.Trim().ToLowerInvariant();
        if (t.Contains("mouse-3") || t.Contains("mouse 3") || t.Contains("mouse2") || t is "mmb"
            || t is "remove" or "secondattack" or "secondaryattack" or "joyremove")
            return "MMB";
        if (t.Contains("mouse-2") || t.Contains("mouse 2") || t is "mouse1" or "rmb"
            || t is "block" or "buildmenu" or "joyblock")
            return "RMB";
        if (t.Contains("mouse-1") || t.Contains("mouse 1") || t.Contains("mouse0") || t is "lmb"
            || t is "attack" or "joyattack" or "joyplace")
            return "LMB";
        if (t.Contains("lshift") || t.Contains("leftshift") || t is "altplace" or "run")
            return "Shift";
        return raw;
    }

    public static void Wipe(Transform parent)
    {
        for (var i = parent.childCount - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }

    public static Button Switch(Transform parent, bool on)
    {
        var go = Chip(parent, "switch");
        Pin(go, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(ToggleWidth, ToggleHeight));
        var knob = Picture(go.transform, "knob", "diamond");
        PlaceKnob(knob, on);
        go.GetComponent<Image>().color = on ? SwitchOn : ChipTint;
        var button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        button.transition = Selectable.Transition.None;
        return button;
    }

    public static void SetSwitch(Button button, bool on)
    {
        button.GetComponent<Image>().color = on ? SwitchOn : ChipTint;
        var knob = button.transform.Find("knob");
        if (knob != null)
            PlaceKnob(knob.gameObject, on);
    }

    private static void PlaceKnob(GameObject knob, bool on)
    {
        Pin(knob, new Vector2(on ? 1f : 0f, 0.5f), new Vector2(on ? 1f : 0f, 0.5f),
            new Vector2(on ? -4f : 4f, 0f), new Vector2(16f, 16f));
    }

    // One HUD strip: torn row + optional duration fill + icon + title + meta.
    // Notices and buffs both go through this. Do not rebuild the layout in a feature.
    public sealed class HudRow
    {
        public const float NoticeWidth = 480f;
        public const float NoticeHeight = 57f;
        public const float BuffWidth = 196f;
        public const float BuffHeight = 36f;

        public readonly GameObject Root;
        public readonly RectTransform Rect;
        public readonly CanvasGroup Fade;
        public readonly Image Fill;
        public readonly Image Icon;
        public readonly GameObject? Pocket;
        public readonly Text Title;
        public readonly Text Meta;

        private readonly RectTransform _fillRt;
        private readonly float _titleLeft;
        private readonly float _titleBare;
        private readonly float _metaWidth;
        private readonly float _fillPadX;
        private readonly float _fillPadY;
        private readonly float _fillWide;

        private HudRow(GameObject root, RectTransform rect, CanvasGroup fade, Image fill, RectTransform fillRt,
            Image icon, GameObject? pocket, Text title, Text meta, float titleLeft, float titleBare,
            float metaWidth, float fillPadX, float fillPadY, float fillWide)
        {
            Root = root;
            Rect = rect;
            Fade = fade;
            Fill = fill;
            _fillRt = fillRt;
            Icon = icon;
            Pocket = pocket;
            Title = title;
            Meta = meta;
            _titleLeft = titleLeft;
            _titleBare = titleBare;
            _metaWidth = metaWidth;
            _fillPadX = fillPadX;
            _fillPadY = fillPadY;
            _fillWide = fillWide;
        }

        public static HudRow Notice(Transform parent)
        {
            var row = Build(parent, "toast", NoticeWidth, NoticeHeight, new Vector2(0f, 1f), true, 30f, 28f, 38f,
                Vector2.zero, Vector2.zero, 8f, 6f, NoticeWidth, 52f, 16f, 48f);
            row.Title.fontSize = BodySize;
            row.Meta.fontSize = HintSize;
            return row;
        }

        public static HudRow Buff(Transform parent) =>
            Build(parent, "buff", BuffWidth, BuffHeight, new Vector2(1f, 1f), false, 26f, 16f, 0f,
                new Vector2(14f, 4f), Vector2.zero, 2f, 2f, BuffWidth - 14f, 34f, 14f, 46f);

        public void SetFill(float amount)
        {
            amount = Mathf.Clamp01(amount);
            var parent = _fillRt.parent as RectTransform;
            var wide = parent != null && parent.rect.width > 8f ? parent.rect.width : _fillWide;
            var inner = Mathf.Max(0f, wide - _fillPadX * 2f);
            var w = inner * amount;
            _fillRt.anchorMin = new Vector2(0f, 0f);
            _fillRt.anchorMax = new Vector2(0f, 1f);
            _fillRt.pivot = new Vector2(0f, 0.5f);
            _fillRt.sizeDelta = new Vector2(w, -_fillPadY * 2f);
            _fillRt.anchoredPosition = new Vector2(_fillPadX, 0f);
            Fill.enabled = amount > 0.02f;
        }

        public void SetIcon(Sprite? sprite)
        {
            var has = sprite != null;
            Icon.enabled = has;
            Icon.sprite = sprite;
            if (Pocket != null)
                Pocket.SetActive(has);
        }

        public void SetCopy(string title, string? meta)
        {
            Title.text = title ?? "";
            var show = !string.IsNullOrEmpty(meta);
            Meta.text = meta ?? "";
            Meta.gameObject.SetActive(show);
            var icon = Icon.enabled || (Pocket != null && Pocket.activeSelf);
            Stretch(Title.gameObject, Vector2.zero, Vector2.one,
                new Vector2(icon ? _titleLeft : _titleBare, 4f),
                new Vector2(show ? -(_metaWidth + 12f) : -12f, -4f));
        }

        private static HudRow Build(Transform parent, string name, float width, float height, Vector2 corner,
            bool pocket, float iconSize, float iconX, float pocketSize, Vector2 stripMin, Vector2 stripMax,
            float fillPadX, float fillPadY, float fillWide, float titleLeft, float titleBare, float metaWidth)
        {
            var root = Node(parent, name);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = corner;
            rect.pivot = corner;
            rect.sizeDelta = new Vector2(width, height);

            var fade = root.AddComponent<CanvasGroup>();
            fade.blocksRaycasts = false;

            var strip = Strip(root.transform, "strip");
            Stretch(strip, Vector2.zero, Vector2.one, stripMin, stripMax);

            var fillGo = Strip(strip.transform, "fill", FillTint);
            var fill = fillGo.GetComponent<Image>();
            fill.raycastTarget = false;
            fill.enabled = false;
            var fillRt = fillGo.GetComponent<RectTransform>();

            GameObject? pocketGo = null;
            if (pocket)
            {
                pocketGo = Strip(root.transform, "pocket", Ink);
                Pin(pocketGo, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(iconX, 0f), new Vector2(pocketSize, pocketSize));
                pocketGo.SetActive(false);
            }

            var iconGo = Graphic(root.transform, "icon", Color.white, false);
            Pin(iconGo, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(iconX, 0f), new Vector2(iconSize, iconSize));
            var icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.enabled = false;

            var title = Label(root.transform, "", HudSize, RestlessUi.Text, TextAnchor.MiddleLeft);
            Stretch(title.gameObject, Vector2.zero, Vector2.one, new Vector2(titleBare, 4f), new Vector2(-12f, -4f));

            var meta = Label(root.transform, "", HudMeta, RestlessUi.Accent, TextAnchor.MiddleRight);
            Pin(meta.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-10f, 0f), new Vector2(metaWidth, 22f));
            meta.gameObject.SetActive(false);

            return new HudRow(root, rect, fade, fill, fillRt, icon, pocketGo, title, meta, titleLeft, titleBare,
                metaWidth, fillPadX, fillPadY, fillWide);
        }
    }

    // Thin Ink track beside the hotbar. Width follows max HP/stamina. Height
    // stays 18px — a 9-slice plate here reads as a fat card, not a bar.
    public sealed class HudMeter
    {
        public const float MinWidth = 88f;
        public const float BaseWidth = 112f;
        public const float CapWidth = 260f;
        public const float Height = 18f;
        public const float TallHeight = 24f;
        public const float Gap = 10f;
        private const float PerPoint = 1.1f;

        public readonly GameObject Root;
        public readonly RectTransform Rect;

        private readonly Image _fill;
        private readonly RectTransform _fillRt;
        private readonly Text _readout;
        private readonly bool _fromRight;
        private bool _numbers;
        private float _span = BaseWidth;

        private HudMeter(GameObject root, RectTransform rect, Image fill, RectTransform fillRt, Text readout,
            bool fromRight)
        {
            Root = root;
            Rect = rect;
            _fill = fill;
            _fillRt = fillRt;
            _readout = readout;
            _fromRight = fromRight;
        }

        public static HudMeter Health(Transform parent) => Build(parent, "health", true, HealthTint);

        public static HudMeter Stamina(Transform parent) => Build(parent, "stamina", false, StaminaTint);

        public static float Span(float max, float baseMax) =>
            Mathf.Clamp(BaseWidth + Mathf.Max(0f, max - baseMax) * PerPoint, MinWidth, CapWidth);

        public void Set(float current, float max, float span, bool numbers = false)
        {
            _span = Mathf.Clamp(span, MinWidth, CapWidth);
            var amount = Mathf.Clamp01(current / Mathf.Max(1f, max));
            _numbers = numbers;
            _readout.enabled = numbers;
            _readout.text = numbers ? Mathf.CeilToInt(Mathf.Max(0f, current)).ToString() : "";
            _readout.color = amount > 0.45f ? Ink : Text;
            var w = _span * amount;
            if (_fromRight)
            {
                _fillRt.anchorMin = new Vector2(1f, 0f);
                _fillRt.anchorMax = new Vector2(1f, 1f);
                _fillRt.pivot = new Vector2(1f, 0.5f);
            }
            else
            {
                _fillRt.anchorMin = new Vector2(0f, 0f);
                _fillRt.anchorMax = new Vector2(0f, 1f);
                _fillRt.pivot = new Vector2(0f, 0.5f);
            }

            _fillRt.sizeDelta = new Vector2(w, 0f);
            _fillRt.anchoredPosition = Vector2.zero;
            _fill.enabled = amount > 0.01f;
            Rect.sizeDelta = new Vector2(_span, numbers ? TallHeight : Height);
        }

        public void Park(float worldInnerX, float worldMidY)
        {
            Rect.anchorMin = Rect.anchorMax = new Vector2(0.5f, 0f);
            Rect.pivot = _fromRight ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
            Rect.sizeDelta = new Vector2(_span, _numbers ? TallHeight : Height);
            Rect.position = new Vector3(worldInnerX, worldMidY, Rect.position.z);
            Rect.anchoredPosition += new Vector2(_fromRight ? -Gap : Gap, 0f);
        }

        private static HudMeter Build(Transform parent, string name, bool fromRight, Color tint)
        {
            var root = Node(parent, name);
            var rect = root.GetComponent<RectTransform>();
            rect.pivot = fromRight ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(MinWidth, Height);

            var track = Strip(root.transform, "track", Ink);
            Stretch(track, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var clip = Slice(root.transform, "clip", "row-idle", StripBorder);
            Stretch(clip, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var clipImg = clip.GetComponent<Image>();
            clipImg.color = Color.white;
            clipImg.raycastTarget = false;
            var mask = clip.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var fillGo = Graphic(clip.transform, "fill", tint, false);
            var fill = fillGo.GetComponent<Image>();
            fill.type = Image.Type.Simple;
            var fillRt = fillGo.GetComponent<RectTransform>();

            var readout = Label(root.transform, "", HudMeta, Text, TextAnchor.MiddleCenter);
            readout.gameObject.name = "readout";
            Stretch(readout.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return new HudMeter(root, rect, fill, fillRt, readout, fromRight);
        }
    }

    // Durability is a vertical torn slit on the right of the slot: row-idle's
    // ragged left and right stacked into a thin ribbon, 9-sliced top/bottom.
    // Grows up from the bottom. Cream when healthy, red when not.
    public sealed class Wear
    {
        public const float Thickness = 11f;
        public const float InsetX = 6f;
        public const float InsetY = 8f;

        private readonly Image _fill;
        private readonly RectTransform _fillRt;

        private Wear(Image fill, RectTransform fillRt)
        {
            _fill = fill;
            _fillRt = fillRt;
        }

        public static Wear Of(Transform plate)
        {
            var fill = plate.Find("vein")?.GetComponent<Image>();
            if (fill != null)
                return new Wear(fill, fill.rectTransform);

            var mask = plate.GetComponent<Mask>();
            if (mask != null)
                Object.Destroy(mask);

            foreach (var name in new[] { "wear", "nub", "slit", "tear", "spine", "RestlessWear", "RestlessWearOld" })
            {
                var stale = plate.Find(name);
                if (stale != null)
                {
                    stale.gameObject.name = name + "Gone";
                    Object.Destroy(stale.gameObject);
                }
            }

            return On(plate);
        }

        public static Wear On(Transform plate)
        {
            var fillGo = Slice(plate, "vein", "wear-slit", WearBorder);
            var fill = fillGo.GetComponent<Image>();
            fill.raycastTarget = false;
            return new Wear(fill, fillGo.GetComponent<RectTransform>());
        }

        public void Set(float amount)
        {
            amount = Mathf.Clamp01(amount);
            var plate = _fillRt.parent as RectTransform;
            var tall = plate != null && plate.rect.height > 8f ? plate.rect.height : 56f;
            var inner = Mathf.Max(8f, tall - InsetY * 2f);
            _fillRt.anchorMin = new Vector2(1f, 0f);
            _fillRt.anchorMax = new Vector2(1f, 0f);
            _fillRt.pivot = new Vector2(1f, 0f);
            _fillRt.sizeDelta = new Vector2(Thickness, inner * amount);
            _fillRt.anchoredPosition = new Vector2(-InsetX, InsetY);
            var tint = Color.Lerp(HealthTint, Accent, amount);
            tint.a = 1f;
            _fill.color = tint;
            _fill.enabled = amount > 0.02f;
        }

        public void Hide() => _fill.enabled = false;
    }
}
