using System.Collections.Generic;
using HarmonyLib;
using Jotunn.Managers;
using RestlessQoL.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

public sealed class MapScreen : FeatureModule
{
    public override string Id => "ui.map";
    public override bool Enabled => true;

    private const float PlateW = 260f;
    private const float PlateH = 42f;
    private const float HintPad = 28f;
    private const float IconGap = 10f;
    private const float TrayW = 240f;
    private const float TrayH = 44f;
    private const float TrayGap = 8f;
    private const float TrayIconGap = 12f;
    private static readonly Vector4 TearBorder = new(72f, 72f, 72f, 72f);

    private static readonly (string verb, string[] buttons, string fallback)[] BindRows =
    [
        ("Ping", new[] { "MapPing", "JoyMapPing" }, "MMB"),
        ("Pin", new[] { "AddPin" }, "LMB×2"),
        ("Remove", new[] { "RemovePin" }, "RMB"),
        ("Cross", new[] { "CrossOffPin" }, "LMB"),
        ("Zoom", new[] { "MapZoomIn", "MapZoomOut", "JoyMapZoomIn", "JoyMapZoomOut" }, "Wheel")
    ];

    private static GameObject? _root;
    private static GameObject? _biomePlate;
    private static Text? _biome;
    private static GameObject? _namePlate;
    private static RestlessUi.KeyStack? _hints;
    private static MapStudio? _studio;
    private static readonly List<IconChip> Icons = new();
    private static readonly List<ToggleRow> Toggles = new();
    private static readonly List<GameObject> Ours = new();
    // Capture only properties this dresser changes, before their first mutation.
    private static readonly HashSet<Object> Remembered = new();
    private static readonly List<System.Action> Restore = new();
    private static readonly List<RestlessControlFeedback> Feedback = new();
    private static bool _dressed;

    private sealed class ToggleRow
    {
        public GameObject Strip = null!;
        public Toggle? Vanilla;
        public Minimap.PinType Filter = Minimap.PinType.None;
        public Button Switch = null!;
        public Text Label = null!;
        public bool Last;
        public bool Shared;
        public bool IsFilter => !Shared && Filter != Minimap.PinType.None;
    }

    private sealed class IconChip
    {
        public GameObject Chip = null!;
        public RectTransform Icon = null!;
        public Image? Selected;
        public Minimap.PinType Type;
    }

    protected override void OnLoaded()
    {
        GUIManager.OnCustomGUIAvailable += TearDown;
    }

    public override void Tick()
    {
        if (ModConfig.MapScreenEnabled.Value)
            return;
        var map = Minimap.instance;
        if (map != null && _dressed)
            Undress(map);
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.Update))]
        private static void AfterUpdate(Minimap __instance)
        {
            if (!ModConfig.MapScreenEnabled.Value)
            {
                if (_dressed)
                    Undress(__instance);
                return;
            }

            if (__instance.m_largeRoot == null || !__instance.m_largeRoot.activeInHierarchy)
            {
                Sleep();
                return;
            }

            if (!_dressed)
                Dress(__instance);
            Sync(__instance);
        }
    }

    private static void Dress(Minimap map)
    {
        if (map.m_mapImageLarge == null || map.m_largeRoot == null)
            return;

        _root = RestlessUi.Node(map.m_largeRoot.transform, "RestlessMap");
        _biomePlate = RestlessUi.Strip(_root.transform, "biome");
        RestlessUi.Pin(_biomePlate, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f),
            new Vector2(PlateW, PlateH));
        RestlessUi.PaperSurface(_biomePlate, small: true);
        _biome = RestlessUi.Label(_biomePlate.transform, "", RestlessUi.BodySize + 2, RestlessUi.Text,
            TextAnchor.MiddleCenter);
        RestlessUi.Stretch(_biome.gameObject, Vector2.zero, Vector2.one, new Vector2(16f, 2f), new Vector2(-16f, -2f));

        RestlessUi.BoundedLabel(_biome, RestlessUi.BodySize + 2, RestlessUi.HintSize);
        _namePlate = RestlessUi.Strip(_root.transform, "name", null, true);
        RestlessUi.Pin(_namePlate, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(280f, 40f));
        RestlessUi.PaperSurface(_namePlate, small: true);
        Ours.Add(_namePlate); // Reparented beside the live input while naming.
        _namePlate.SetActive(false);

        _hints = RestlessUi.KeyStack.Plate(_root.transform, "hints");
        _hints.Set("", "", HintFaces());
        var hintPlate = _hints.Root.transform.Find("plate");
        if (hintPlate != null) RestlessUi.PaperSurface(hintPlate.gameObject);
        Ours.Add(_hints.Root); // Docking moves it outside _root.

        _studio ??= new MapStudio("RestlessLargeMapStudio", new Vector3(80f, 0f, 0f), -81f);
        _studio.EnsureStudio();
        if (map.m_mapImageLarge != null)
            _studio.EnsureView(map.m_mapImageLarge, "RestlessLargeView", true, true, TearBorder);
        LiftMarkers(map);
        DressToggles(map);
        DressIcons(map);
        _dressed = true;
    }

    private static void Sync(Minimap map)
    {
        if (_root == null)
            return;
        _root.SetActive(true);
        QuietChrome(map);
        Capture(map);
        ParkChrome(map);
        ParkTray(map);
        PaintBiome(map);
        PaintName(map);
        PaintToggles();
        PaintIcons();
    }

    private static void ParkChrome(Minimap map)
    {
        if (_studio?.Wrap == null)
            return;
        var wrap = _studio.Wrap.GetComponent<RectTransform>();
        if (_biomePlate != null)
            RestlessUi.Dock(_biomePlate.GetComponent<RectTransform>(), wrap, new Vector2(0.5f, 1f),
                new Vector2(0f, -28f));
        ParkHints(map);
    }

    private static void ParkHints(Minimap map)
    {
        if (_hints == null || map.m_mapImageLarge == null || map.m_largeRoot == null)
            return;
        var host = map.m_largeRoot.transform;
        if (_hints.Root.transform.parent != host)
            _hints.Root.transform.SetParent(host, false);
        var rt = _hints.Root.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = Vector2.zero;
        var corners = new Vector3[4];
        map.m_mapImageLarge.rectTransform.GetWorldCorners(corners);
        var scale = rt.lossyScale.x == 0f ? 1f : rt.lossyScale.x;
        rt.position = corners[0] + new Vector3(HintPad * scale, HintPad * scale, 0f);
    }

    private static void ParkTray(Minimap map)
    {
        if (map.m_mapImageLarge == null || map.m_largeRoot == null)
            return;
        var host = map.m_largeRoot.transform;
        var corners = new Vector3[4];
        map.m_mapImageLarge.rectTransform.GetWorldCorners(corners);
        var br = corners[3];
        var panel = Deep(host, "IconPanel");
        var scale = map.m_mapImageLarge.rectTransform.lossyScale.x;
        if (scale < 0.01f)
            scale = 1f;
        var iconW = 56f;
        if (panel != null)
        {
            var rt = panel.GetComponent<RectTransform>();
            RememberRect(rt);
            iconW = Mathf.Max(rt.rect.width, 56f);
            if (rt.parent != host)
                rt.SetParent(host, true);
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.position = br + new Vector3(-HintPad * scale, HintPad * scale, 0f);
        }

        var x = br.x - (HintPad + iconW + TrayIconGap) * scale;
        var y = br.y + HintPad * scale;
        var n = Toggles.Count;
        for (var i = 0; i < n; i++)
        {
            var strip = Toggles[i].Strip;
            if (strip == null)
                continue;
            if (strip.transform.parent != host)
                strip.transform.SetParent(host, false);
            RestlessUi.Pin(strip, new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero,
                new Vector2(TrayW, TrayH));
            var fromBottom = (n - 1 - i) * (TrayH + TrayGap);
            strip.GetComponent<RectTransform>().position = new Vector3(x, y + fromBottom * scale, br.z);
        }
    }

    private static void Capture(Minimap map)
    {
        if (_studio == null || map.m_mapImageLarge == null)
            return;
        var src = map.m_mapImageLarge;
        var scale = Mathf.Min(1f, 1600f / Mathf.Max(64f, src.rectTransform.rect.width));
        var w = Mathf.Max(256, Mathf.RoundToInt(src.rectTransform.rect.width * scale));
        var h = Mathf.Max(128, Mathf.RoundToInt(src.rectTransform.rect.height * scale));
        if (!_studio.Capture(src, w, h, false))
            return;
        _studio.HideHit(src);
        if (map.m_namePin == null)
            _studio.StackOver(src);
        LiftMarkers(map);
    }

    private static void PaintBiome(Minimap map)
    {
        if (_biomePlate == null || _biome == null)
            return;
        var text = map.m_biomeNameLarge != null ? map.m_biomeNameLarge.text : "";
        _biome.text = text ?? "";
        _biomePlate.SetActive(!string.IsNullOrWhiteSpace(text));
    }

    private static void PaintName(Minimap map)
    {
        if (_namePlate == null)
            return;
        var field = NameField(map);
        var naming = map.m_namePin != null && field != null && field.gameObject.activeInHierarchy;
        if (!naming || field == null)
        {
            _namePlate.SetActive(false);
            return;
        }

        _namePlate.SetActive(true);

        if (field.transform.parent != null && _namePlate.transform.parent != field.transform.parent)
            _namePlate.transform.SetParent(field.transform.parent, true);
        RestlessUi.CopyRect(_namePlate.GetComponent<RectTransform>(), field.GetComponent<RectTransform>());
        var plate = _namePlate.GetComponent<RectTransform>();
        plate.offsetMin -= new Vector2(18f, 8f);
        plate.offsetMax += new Vector2(18f, 8f);
        var fieldIdx = field.transform.GetSiblingIndex();
        var plateIdx = _namePlate.transform.GetSiblingIndex();
        if (plateIdx != fieldIdx - 1)
            _namePlate.transform.SetSiblingIndex(fieldIdx);
        StyleName(field);
    }

    private static void StyleName(Component field)
    {
        foreach (var img in field.GetComponentsInChildren<Image>(true))
        {
            // Only clear the field backing. Keep selection/caret graphics live.
            if (img.GetComponent<InputField>() != null || img.GetComponent<TMP_InputField>() != null)
            {
                RememberImage(img);
                img.color = Color.clear;
                img.raycastTarget = true;
                continue;
            }

        }

        var input = field.GetComponent<InputField>();
        if (input?.textComponent != null)
        {
            RememberText(input.textComponent);
            input.textComponent.fontSize = RestlessUi.BodySize + 2;
            input.textComponent.font = RestlessUi.Face();
            input.textComponent.color = RestlessUi.Text;
            input.textComponent.supportRichText = false;
        }

        if (input?.placeholder is Text ph)
        {
            RememberText(ph);
            ph.fontSize = RestlessUi.BodySize + 2;
            ph.font = RestlessUi.Face();
            ph.color = RestlessUi.Muted;
        }

        var tmp = field.GetComponent<TMP_InputField>();
        if (tmp?.textComponent != null)
        {
            RememberTmp(tmp.textComponent);
            tmp.textComponent.color = RestlessUi.Text;
        }
        if (tmp?.placeholder is TMP_Text tmpPh)
        {
            RememberTmp(tmpPh);
            tmpPh.color = RestlessUi.PaperMuted;
        }
    }

    private static List<(string verb, string face)> HintFaces()
    {
        var rows = new List<(string, string)>();
        foreach (var row in BindRows)
        {
            var face = RestlessUi.Bind(row.buttons);
            rows.Add((row.verb, string.IsNullOrEmpty(face) ? row.fallback : face));
        }

        return rows;
    }

    private static void DressToggles(Minimap map)
    {
        Toggles.Clear();
        DressShared(map);
        DressFilter(map, "Deaths", Minimap.PinType.Death, "IconDeath");
        DressFilter(map, "Bosses", Minimap.PinType.Boss, "IconBoss");
        DressFilter(map, "Pings", Minimap.PinType.Ping, "IconPing");
        foreach (var name in new[] { "PublicPanel", "SharedPanel", "IconPanel2", "IconPingPanel" })
        {
            var panel = Deep(map.m_largeRoot.transform, name);
            if (panel != null)
                Mute(panel.gameObject);
        }
    }

    private static void DressShared(Minimap map)
    {
        if (_root == null)
            return;
        var panel = Deep(map.m_largeRoot.transform, "SharedPanel");
        var toggle = panel != null ? panel.GetComponentInChildren<Toggle>(true) : null;
        if (toggle != null)
        {
            var enabled = toggle.enabled;
            var interactable = toggle.interactable;
            Restore.Add(() => { if (toggle != null) { toggle.enabled = enabled; toggle.interactable = interactable; } });
            toggle.interactable = false;
            toggle.enabled = false;
            foreach (var graphic in toggle.GetComponentsInChildren<Graphic>(true))
            {
                var raycast = graphic.raycastTarget;
                Restore.Add(() => { if (graphic != null) graphic.raycastTarget = raycast; });
                graphic.raycastTarget = false;
            }
        }

        if (panel != null)
            Mute(panel.gameObject);

        var strip = RestlessUi.Strip(_root.transform, "RestlessSharedPanel", null, true);
        Ours.Add(strip);
        RestlessUi.PaperSurface(strip, small: true);
        RestlessUi.Pin(strip, new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero,
            new Vector2(TrayW, TrayH));

        FaceOn(strip, PinFace(map, Minimap.PinType.Shout) ?? PinFace(map, Minimap.PinType.Bed),
            out var labelPad);
        var text = RestlessUi.Label(strip.transform, "Shared", RestlessUi.BodySize, RestlessUi.Text,
            TextAnchor.MiddleLeft);
        RestlessUi.Stretch(text.gameObject, Vector2.zero, Vector2.one, new Vector2(labelPad, 2f),
            new Vector2(-72f, -2f));
        var on = map.m_showSharedMapData;
        RestlessUi.BoundedLabel(text, RestlessUi.BodySize, RestlessUi.HintSize);
        var sw = RestlessUi.Switch(strip.transform, on);
        RestlessUi.PaperSwitch(sw, on);
        RestlessUi.Pin(sw.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f),
            new Vector2(RestlessUi.ToggleWidth, RestlessUi.ToggleHeight));
        var row = new ToggleRow
        {
            Strip = strip,
            Vanilla = toggle,
            Switch = sw,
            Label = text,
            Shared = true,
            Last = on
        };
        sw.onClick.AddListener(() => FlipShared(row));
        Toggles.Add(row);
    }

    private static void DressFilter(Minimap map, string title, Minimap.PinType type, string iconName)
    {
        if (_root == null)
            return;

        var strip = RestlessUi.Strip(_root.transform, "RestlessFilter" + type, null, true);
        Ours.Add(strip);
        RestlessUi.PaperSurface(strip, small: true);
        RestlessUi.Pin(strip, new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero,
            new Vector2(TrayW, TrayH));

        FaceOn(strip, FaceOf(map, iconName), out var labelPad);

        var on = IconShown(type);
        var text = RestlessUi.Label(strip.transform, title, RestlessUi.BodySize, RestlessUi.Text,
            TextAnchor.MiddleLeft);
        RestlessUi.Stretch(text.gameObject, Vector2.zero, Vector2.one, new Vector2(labelPad, 2f),
            new Vector2(-72f, -2f));
        RestlessUi.BoundedLabel(text, RestlessUi.BodySize, RestlessUi.HintSize);
        var sw = RestlessUi.Switch(strip.transform, on);
        RestlessUi.PaperSwitch(sw, on);
        RestlessUi.Pin(sw.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f),
            new Vector2(RestlessUi.ToggleWidth, RestlessUi.ToggleHeight));
        var row = new ToggleRow
        {
            Strip = strip,
            Filter = type,
            Switch = sw,
            Label = text,
            Last = on
        };
        sw.onClick.AddListener(() =>
        {
            ToggleFilter(type);
            row.Last = IconShown(type);
            RestlessUi.PaperSwitch(row.Switch, row.Last);
        });
        Toggles.Add(row);
    }

    private static void PaintToggles()
    {
        var map = Minimap.instance;
        foreach (var row in Toggles)
        {
            if (row.Switch == null)
                continue;
            var on = row.Shared
                ? map != null && map.m_showSharedMapData
                : IconShown(row.Filter);
            row.Last = on;
            RestlessUi.PaperSwitch(row.Switch, on);
        }
    }

    private static void FlipShared(ToggleRow row)
    {
        var map = Minimap.instance;
        if (map == null)
            return;
        map.m_showSharedMapData = !map.m_showSharedMapData;
        if (row.Vanilla != null)
            row.Vanilla.SetIsOnWithoutNotify(map.m_showSharedMapData);
        row.Last = map.m_showSharedMapData;
        RestlessUi.PaperSwitch(row.Switch, row.Last);
    }

    private static void FaceOn(GameObject strip, Sprite? sprite, out float labelPad)
    {
        labelPad = 16f;
        if (sprite == null)
            return;
        var pic = RestlessUi.Graphic(strip.transform, "icon", Color.white, false);
        pic.GetComponent<Image>().sprite = sprite;
        pic.GetComponent<Image>().preserveAspect = true;
        RestlessUi.Pin(pic, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f),
            new Vector2(22f, 22f));
        labelPad = 44f;
    }

    private static Sprite? FaceOf(Minimap map, string iconName)
    {
        var icon = Deep(map.m_largeRoot.transform, iconName);
        if (icon == null)
            return null;
        foreach (var img in icon.GetComponentsInChildren<Image>(true))
        {
            if (img.name == "Selected" || img.sprite == null)
                continue;
            return img.sprite;
        }

        return null;
    }

    private static Sprite? PinFace(Minimap map, Minimap.PinType type)
    {
        if (map.m_icons == null)
            return null;
        foreach (var data in map.m_icons)
        {
            if (data.m_name == type)
                return data.m_icon;
        }

        return null;
    }

    private static void DressIcons(Minimap map)
    {
        Icons.Clear();
        foreach (var name in new[] { "Icon0", "Icon1", "Icon2", "Icon3", "Icon4" })
        {
            var icon = Deep(map.m_largeRoot.transform, name);
            if (icon == null || icon.parent == null)
                continue;
            var plate = RestlessUi.Strip(icon.parent, "Restless" + name);
            Ours.Add(plate);
            plate.AddComponent<LayoutElement>().ignoreLayout = true;
            RestlessUi.PaperSurface(plate, small: true);
            var control = icon.GetComponent<Selectable>();
            if (control != null && control.GetComponent<RestlessControlFeedback>() == null)
                Feedback.Add(RestlessUi.ControlFeedback(control));
            RestlessUi.CopyRect(plate.GetComponent<RectTransform>(), icon.GetComponent<RectTransform>());
            var rt = plate.GetComponent<RectTransform>();
            rt.offsetMin -= new Vector2(6f, 6f);
            rt.offsetMax += new Vector2(6f, 6f);
            plate.transform.SetSiblingIndex(icon.GetSiblingIndex());
            plate.GetComponent<Image>().raycastTarget = false;
            Image? selected = null;
            foreach (var img in icon.GetComponentsInChildren<Image>(true))
            {
                if (img.name == "Selected")
                    selected = img;
            }

            Icons.Add(new IconChip
            {
                Chip = plate,
                Icon = icon.GetComponent<RectTransform>(),
                Selected = selected,
                Type = TypeOf(name)
            });
        }

        var panel = Deep(map.m_largeRoot.transform, "IconPanel");
        if (panel != null)
            SpaceIcons(panel);
    }

    private static void SpaceIcons(Transform panel)
    {
        var rows = new List<IconChip>();
        foreach (var row in Icons)
        {
            if (row.Icon != null && row.Icon.parent == panel)
                rows.Add(row);
        }

        rows.Sort((a, b) => b.Icon.anchoredPosition.y.CompareTo(a.Icon.anchoredPosition.y));
        if (rows.Count == 0)
            return;
        var x = rows[0].Icon.anchoredPosition.x;
        var y = rows[0].Icon.anchoredPosition.y;
        foreach (var row in rows)
        {
            RememberRect(row.Icon);
            row.Icon.anchoredPosition = new Vector2(x, y);
            RestlessUi.CopyRect(row.Chip.GetComponent<RectTransform>(), row.Icon);
            var chip = row.Chip.GetComponent<RectTransform>();
            chip.offsetMin -= new Vector2(6f, 6f);
            chip.offsetMax += new Vector2(6f, 6f);
            y -= row.Icon.rect.height + IconGap;
        }

        var panelRt = panel.GetComponent<RectTransform>();
        var last = rows[rows.Count - 1].Icon;
        var span = rows[0].Icon.anchoredPosition.y - last.anchoredPosition.y + last.rect.height + 12f;
        RememberRect(panelRt);
        panelRt.sizeDelta = new Vector2(panelRt.sizeDelta.x, span);
        var panelImg = panel.GetComponent<Image>();
        if (panelImg != null)
        {
            RememberImage(panelImg);
            panelImg.enabled = false;
        }
    }

    private static Minimap.PinType TypeOf(string name) =>
        name switch
        {
            "Icon0" => Minimap.PinType.Icon0,
            "Icon1" => Minimap.PinType.Icon1,
            "Icon2" => Minimap.PinType.Icon2,
            "Icon3" => Minimap.PinType.Icon3,
            "Icon4" => Minimap.PinType.Icon4,
            "IconDeath" => Minimap.PinType.Death,
            "IconBoss" => Minimap.PinType.Boss,
            "IconPing" => Minimap.PinType.Ping,
            _ => Minimap.PinType.None
        };

    private static Minimap.PinType PickedIcon()
    {
        foreach (var row in Icons)
        {
            if (row.Selected != null && row.Selected.enabled && row.Selected.gameObject.activeInHierarchy)
                return row.Type;
        }

        var map = Minimap.instance;
        if (map == null)
            return Minimap.PinType.None;
        var data = Traverse.Create(map);
        foreach (var name in new[] { "m_selectedType", "m_selectedIcon" })
        {
            var field = data.Field(name);
            if (field.FieldExists() && field.GetValue() is Minimap.PinType pin)
                return pin;
        }

        return Minimap.PinType.None;
    }

    private static void PaintIcons()
    {
        var picked = PickedIcon();
        foreach (var row in Icons)
        {
            if (row.Chip == null || row.Icon == null)
                continue;
            var lit = row.Type == picked && picked != Minimap.PinType.None;
            if (row.Selected != null)
            {
                // Preserve the enabled/active state written by vanilla selection.
                var selected = row.Selected;
                if (Remembered.Add(selected))
                {
                    var color = selected.color;
                    Restore.Add(() => { if (selected != null) selected.color = color; });
                }
                selected.color = Color.clear;
            }
            RestlessUi.CopyRect(row.Chip.GetComponent<RectTransform>(), row.Icon);
            var plate = row.Chip.GetComponent<RectTransform>();
            const float pad = 5f;
            plate.offsetMin -= new Vector2(pad, pad);
            plate.offsetMax += new Vector2(pad, pad);
            RestlessUi.PaperSurface(row.Chip, small: true,
                accent: lit ? RestlessUi.Accent : (Color?)null);
            row.Chip.GetComponent<Image>().raycastTarget = false;
        }
    }

    private static void QuietChrome(Minimap map)
    {
        if (map.m_biomeNameLarge != null)
        {
            RememberTmp(map.m_biomeNameLarge);
            map.m_biomeNameLarge.alpha = 0f;
        }

        foreach (var img in map.m_largeRoot.GetComponentsInChildren<Image>(true))
        {
            if (img.name.StartsWith("Restless"))
                continue;
            var n = img.name;
            if (n is "large" or "Bkg" or "keyboard_hint" or "gamepad_hint" or "mouse1" or "mouse2")
            {
                RememberImage(img);
                img.enabled = false;
                if (n is "keyboard_hint" or "gamepad_hint")
                    MuteHintCluster(img.transform);
            }

            if (n is "MapClick")
            {
                RememberImage(img);
                img.color = Color.clear;
                img.enabled = true;
                img.raycastTarget = true;
                img.canvasRenderer.cull = false;
                img.canvasRenderer.cullTransparentMesh = false;
            }
        }

        foreach (var name in new[] { "PublicPanel", "SharedPanel", "IconPanel2", "IconPingPanel" })
        {
            var panel = Deep(map.m_largeRoot.transform, name);
            if (panel != null)
                Mute(panel.gameObject);
        }

        foreach (var tmp in map.m_largeRoot.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp.name.StartsWith("Restless"))
                continue;
            if (tmp.name is "large_biome" or "Inventory tip" || tmp.name.StartsWith("Text - "))
            {
                RememberTmp(tmp);
                tmp.alpha = 0f;
                tmp.enabled = false;
                MuteHintCluster(tmp.transform);
            }
        }

        Mute(AsGo(map.m_hints));
        Mute(AsGo(map.m_sharedMapHint));
    }

    private static void MuteHintCluster(Transform node)
    {
        var parent = node.parent;
        if (parent == null || parent.name.StartsWith("Restless"))
            return;
        if (parent.childCount > 12 || parent == Minimap.instance?.m_largeRoot?.transform)
            return;
        Mute(parent.gameObject);
    }

    private static void Mute(GameObject? go)
    {
        if (go == null) return;
        var group = go.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = go.AddComponent<CanvasGroup>();
            Remembered.Add(group);
            Restore.Add(() => { if (group != null) Object.Destroy(group); });
        }
        else if (Remembered.Add(group))
        {
            var alpha = group.alpha;
            var blocks = group.blocksRaycasts;
            var interactable = group.interactable;
            Restore.Add(() => { if (group != null) { group.alpha = alpha; group.blocksRaycasts = blocks; group.interactable = interactable; } });
        }
        RestlessUi.Quiet(go);
    }

    private static void RememberImage(Image image)
    {
        if (!Remembered.Add(image)) return;
        var enabled = image.enabled;
        var color = image.color;
        var raycast = image.raycastTarget;
        var cull = image.canvasRenderer.cull;
        var cullMesh = image.canvasRenderer.cullTransparentMesh;
        Restore.Add(() =>
        {
            if (image == null) return;
            image.enabled = enabled;
            image.color = color;
            image.raycastTarget = raycast;
            image.canvasRenderer.cull = cull;
            image.canvasRenderer.cullTransparentMesh = cullMesh;
        });
    }

    private static void RememberTmp(TMP_Text text)
    {
        if (!Remembered.Add(text)) return;
        var color = text.color;
        var enabled = text.enabled;
        Restore.Add(() => { if (text != null) { text.color = color; text.enabled = enabled; } });
    }

    private static void RememberText(Text text)
    {
        if (!Remembered.Add(text)) return;
        var font = text.font;
        var size = text.fontSize;
        var color = text.color;
        var rich = text.supportRichText;
        Restore.Add(() => { if (text != null) { text.font = font; text.fontSize = size; text.color = color; text.supportRichText = rich; } });
    }

    private static void RememberRect(RectTransform rect)
    {
        if (!Remembered.Add(rect)) return;
        var parent = rect.parent;
        var sibling = rect.GetSiblingIndex();
        var min = rect.anchorMin;
        var max = rect.anchorMax;
        var pivot = rect.pivot;
        var position = rect.anchoredPosition3D;
        var size = rect.sizeDelta;
        var scale = rect.localScale;
        var rotation = rect.localRotation;
        Restore.Add(() =>
        {
            if (rect == null || parent == null) return;
            rect.SetParent(parent, false);
            rect.SetSiblingIndex(sibling);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition3D = position;
            rect.localScale = scale;
            rect.localRotation = rotation;
        });
    }

    private static void RestoreNative()
    {
        foreach (var feedback in Feedback)
            if (feedback != null) Object.Destroy(feedback);
        Feedback.Clear();
        for (var i = Restore.Count - 1; i >= 0; i--) Restore[i]();
        Restore.Clear();
        Remembered.Clear();
    }

    private static void LiftMarkers(Minimap map)
    {
        if (_studio?.Wrap == null || !_studio.Wrap.activeSelf)
        {
            ReturnMarkers(map);
            return;
        }

        var wrap = _studio.Wrap.transform;
        MapStudio.Adopt(map.m_pinRootLarge, wrap);
        MapStudio.Adopt(map.m_pinNameRootLarge, wrap);
        MapStudio.Adopt(map.m_largeMarker, wrap);
        MapStudio.Adopt(map.m_largeShipMarker, wrap);
    }

    private static void ReturnMarkers(Minimap map)
    {
        var host = map.m_mapImageLarge != null ? map.m_mapImageLarge.transform.parent : null;
        if (host == null)
            return;
        MapStudio.Adopt(map.m_pinRootLarge, host);
        MapStudio.Adopt(map.m_pinNameRootLarge, host);
        MapStudio.Adopt(map.m_largeMarker, host);
        MapStudio.Adopt(map.m_largeShipMarker, host);
    }

    private static void Sleep()
    {
        if (_root != null)
            _root.SetActive(false);
        _studio?.Sleep();
        var map = Minimap.instance;
        if (map?.m_mapImageLarge != null)
            _studio?.ShowSource(map.m_mapImageLarge);
    }

    private static bool IconShown(Minimap.PinType type)
    {
        var map = Minimap.instance;
        if (map == null)
            return true;
        var data = Traverse.Create(map);
        var field = data.Field("m_visibleIconTypes");
        if (field.FieldExists() && field.GetValue() is bool[] arr)
        {
            var i = (int)type;
            return i < 0 || i >= arr.Length || arr[i];
        }

        var visible = data.Method("VisibleOnMap", new[] { typeof(Minimap.PinType) });
        if (visible.MethodExists())
            return visible.GetValue<bool>(type);

        return true;
    }

    private static void ToggleFilter(Minimap.PinType type)
    {
        var map = Minimap.instance;
        if (map == null)
            return;
        var data = Traverse.Create(map);
        var toggle = data.Method("ToggleIconFilter", new[] { typeof(Minimap.PinType) });
        if (toggle.MethodExists())
        {
            toggle.GetValue(type);
            return;
        }

        var alt = type switch
        {
            Minimap.PinType.Death => "OnAltPressedIconDeath",
            Minimap.PinType.Boss => "OnAltPressedIconBoss",
            _ => null
        };
        if (alt != null)
            data.Method(alt).GetValue();
    }

    private static Component? NameField(Minimap map)
    {
        var field = map.m_nameInput;
        return field == null ? null : field;
    }

    private static GameObject? AsGo(object? node) =>
        node switch
        {
            GameObject go => go,
            Component c => c.gameObject,
            _ => null
        };

    private static Transform? Deep(Transform root, string name) => RestlessUi.Deep(root, name);

    private static void Undress(Minimap map)
    {
        ReturnMarkers(map);
        if (map.m_mapImageLarge != null)
            _studio?.ShowSource(map.m_mapImageLarge);

        RestoreNative();
        ClearOwned();
    }

    private static void ClearOwned()
    {
        if (_root != null)
            Object.Destroy(_root);
        foreach (var go in Ours)
        {
            if (go != null)
                Object.Destroy(go);
        }

        Ours.Clear();
        Icons.Clear();
        _root = null;
        _biomePlate = null;
        _biome = null;
        _namePlate = null;
        _hints = null;
        Toggles.Clear();
        _studio?.Dispose();
        _studio = null;
        _dressed = false;
    }

    private static void TearDown()
    {
        var map = Minimap.instance;
        if (map != null)
            Undress(map);
        else
        {
            RestoreNative();
            ClearOwned();
        }
    }
}
