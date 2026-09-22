using HarmonyLib;
using Jotunn.Managers;
using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

public sealed class MapChrome : FeatureModule
{
    public override string Id => "ui.minimap";
    public override bool Enabled => true;

    private static GameObject? _biomePlate;
    private static Text? _biome;
    private static Text? _clock;
    private static MapStudio? _studio;
    private static Transform? _shipWindHome;
    private static bool _dressed;

    protected override void OnLoaded()
    {
        GUIManager.OnCustomGUIAvailable += () =>
        {
            var map = Minimap.instance;
            if (map != null)
                Undress(map);
            else
            {
                _studio?.Dispose();
                _studio = null;
                _biomePlate = null;
                _biome = null;
                _shipWindHome = null;
                _dressed = false;
            }
        };
    }

    public override void Tick()
    {
        var map = Minimap.instance;
        if (map == null || map.m_mapImageSmall == null)
            return;
        if (!ModConfig.MapChromeEnabled.Value && _dressed)
            Undress(map);
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.Start))]
        private static void Start(Minimap __instance)
        {
            _dressed = false;
            if (ModConfig.MapChromeEnabled.Value)
                Dress(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Hud), "UpdateShipHud")]
        private static void ShipHud(Hud __instance)
        {
            if (!ModConfig.MapChromeEnabled.Value)
            {
                RestoreShipWind(__instance);
                return;
            }

            var map = Minimap.instance;
            var mapRoot = map != null && map.m_smallRoot != null ? map.m_smallRoot : map?.m_mapSmall;
            if (mapRoot == null || !mapRoot.activeInHierarchy)
            {
                RestoreShipWind(__instance);
                return;
            }

            var ship = Player.m_localPlayer?.GetControlledShip();
            if (ship == null || !__instance.IsVisible())
            {
                RestoreShipWind(__instance);
                return;
            }

            DockShipWind(__instance, map);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.Update))]
        private static void Update(Minimap __instance)
        {
            if (!ModConfig.MapChromeEnabled.Value)
                return;
            if (__instance.m_mapImageSmall == null)
                return;
            var root = __instance.m_smallRoot != null ? __instance.m_smallRoot : __instance.m_mapSmall;
            if (root != null && !root.activeInHierarchy)
            {
                if (_biomePlate != null)
                    _biomePlate.SetActive(false);
                _studio?.Sleep();
                return;
            }

            if (!_dressed)
                Dress(__instance);
            QuietBox(__instance);
            Capture(__instance);
            Sync(__instance);
            RestlessUi.HudTuck.Tick();
            if (_biomePlate != null)
                RestlessUi.HudTuck.Fade(_biomePlate);
        }
    }

    private static void Dress(Minimap map)
    {
        if (map.m_mapImageSmall == null)
            return;

        ClearOld(map);
        Unwrap(map);
        QuietBox(map);
        _studio ??= new MapStudio("RestlessMapStudio", Vector3.zero, -80f);
        _studio.EnsureStudio();
        _studio.EnsureView(map.m_mapImageSmall, "RestlessMapView", false, false, Vector4.zero);
        LiftMarkers(map);
        EnsureBiome(map);
        _dressed = true;
    }

    private static void Undress(Minimap map)
    {
        RestoreBox(map);
        RestoreMinimapWind(map);
        RestoreShipWind(Hud.instance);
        ReturnMarkers(map);
        map.m_mapImageSmall.enabled = true;
        _studio?.Sleep();
        if (_biomePlate != null)
        {
            Object.Destroy(_biomePlate);
            _biomePlate = null;
            _biome = null;
            _clock = null;
        }

        if (map.m_biomeNameSmall != null)
            map.m_biomeNameSmall.alpha = 1f;
        _studio?.Dispose();
        _studio = null;
        _dressed = false;
    }

    private static void RestoreBox(Minimap map)
    {
        var root = map.m_smallRoot != null ? map.m_smallRoot.transform : map.m_mapSmall != null ? map.m_mapSmall.transform : map.m_mapImageSmall.transform.parent;
        if (root == null)
            return;
        foreach (var img in root.GetComponentsInChildren<Image>(true))
        {
            if (img.name == "small")
                img.enabled = true;
        }
    }

    private static void ClearOld(Minimap map)
    {
        var parent = map.m_mapImageSmall.transform.parent;
        if (parent == null)
            return;
        foreach (var name in new[] { "RestlessMapCompass", "RestlessMapRing", "RestlessMapBack", "RestlessTear" })
        {
            var leftover = parent.Find(name);
            if (leftover != null)
                Object.Destroy(leftover.gameObject);
        }

        var oldWind = parent.Find("RestlessWind");
        if (oldWind != null)
            Object.Destroy(oldWind.gameObject);
    }

    private static void Unwrap(Minimap map)
    {
        var mapRt = map.m_mapImageSmall.rectTransform;
        var wrap = mapRt.parent;
        if (wrap == null || wrap.name != "RestlessMapTear")
            return;

        var home = wrap.parent;
        var wrapRt = wrap.GetComponent<RectTransform>();
        RestlessUi.CopyRect(mapRt, wrapRt);
        mapRt.SetParent(home, false);
        RestlessUi.CopyRect(mapRt, wrapRt);
        for (var i = wrap.childCount - 1; i >= 0; i--)
            wrap.GetChild(i).SetParent(home, true);
        Object.Destroy(wrap.gameObject);
    }

    private static void QuietBox(Minimap map)
    {
        var root = map.m_smallRoot != null ? map.m_smallRoot.transform : map.m_mapSmall != null ? map.m_mapSmall.transform : map.m_mapImageSmall.transform.parent;
        if (root == null)
            return;

        foreach (var img in root.GetComponentsInChildren<Image>(true))
        {
            if (img.name.StartsWith("Restless"))
                continue;
            if (img.name == "small")
                img.enabled = false;
            else if (img.name == "MapClick")
            {
                img.color = Color.clear;
                img.raycastTarget = true;
            }
        }

        if (map.m_biomeNameSmall != null)
            map.m_biomeNameSmall.alpha = PlateOn() ? 0f : 1f;
    }

    private static void Capture(Minimap map)
    {
        if (!ModConfig.MapTearEnabled.Value || _studio == null)
        {
            map.m_mapImageSmall.enabled = true;
            _studio?.Sleep();
            ReturnMarkers(map);
            return;
        }

        var src = map.m_mapImageSmall;
        var size = Mathf.Clamp(Mathf.RoundToInt(src.rectTransform.rect.width), 128, 512);
        if (!_studio.Capture(src, size, size, true, Motion()))
            return;
        LiftMarkers(map);
    }

    private static int Motion()
    {
        var player = Player.m_localPlayer;
        if (player == null)
            return 0;
        var p = player.transform.position;
        return (Mathf.RoundToInt(p.x * 4) * 397) ^ Mathf.RoundToInt(p.z * 4);
    }

    private static void EnsureBiome(Minimap map)
    {
        if (_biomePlate != null)
            return;
        var parent = Host(map);
        if (parent == null)
            return;

        _biomePlate = RestlessUi.Strip(parent, "RestlessBiome");
        RestlessUi.Pin(_biomePlate, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(168f, 28f));
        _biome = RestlessUi.Label(_biomePlate.transform, "", RestlessUi.HudSize, RestlessUi.Text, TextAnchor.MiddleLeft);
        RestlessUi.Stretch(_biome.gameObject, Vector2.zero, Vector2.one, new Vector2(14f, 2f), new Vector2(-28f, -2f));
        _clock = RestlessUi.Label(_biomePlate.transform, "", RestlessUi.HudSize, RestlessUi.Muted, TextAnchor.MiddleRight);
    }

    private static void Sync(Minimap map)
    {
        var mapRt = map.m_mapImageSmall.rectTransform;
        QuietBox(map);

        if (_biomePlate != null && _biome != null)
        {
            var text = ModConfig.MapBiomePlate.Value && map.m_biomeNameSmall != null ? map.m_biomeNameSmall.text : "";
            var clock = ClockText();
            _biome.text = text;
            if (_clock != null)
                _clock.text = clock;
            var show = PlateOn() && (!string.IsNullOrEmpty(text) || ModConfig.MapWindPlate.Value || clock.Length > 0);
            _biomePlate.SetActive(show);
            if (show)
            {
                var wide = clock.Length > 0 ? 220f : 168f;
                var plate = _biomePlate.GetComponent<RectTransform>();
                plate.sizeDelta = new Vector2(wide, 28f);
                var windPad = ModConfig.MapWindPlate.Value ? -28f : -10f;
                var clockPad = clock.Length > 0 ? 88f : 0f;
                RestlessUi.Stretch(_biome.gameObject, Vector2.zero, Vector2.one, new Vector2(14f, 2f),
                    new Vector2(windPad - clockPad, -2f));
                if (_clock != null)
                    RestlessUi.Stretch(_clock.gameObject, new Vector2(1f, 0f), Vector2.one,
                        new Vector2(-clockPad - 4f, 2f), new Vector2(windPad, -2f));
                RestlessUi.Dock(plate, mapRt, new Vector2(0.5f, 1f), new Vector2(0f, -16f));
            }
        }

        DockMinimapWind(map);
        LiftMarkers(map);
    }

    private static bool PlateOn() =>
        ModConfig.MapBiomePlate.Value || ModConfig.MapWindPlate.Value || ModConfig.MapClock.Value;

    private static string ClockText()
    {
        if (!ModConfig.MapClock.Value || EnvMan.instance == null)
            return "";
        var day = EnvMan.instance.GetDay();
        var frac = Mathf.Repeat(EnvMan.instance.GetDayFraction(), 1f);
        var hours = Mathf.FloorToInt(frac * 24f);
        var mins = Mathf.FloorToInt((frac * 24f - hours) * 60f);
        return "Day " + day + " · " + hours.ToString("00") + ":" + mins.ToString("00");
    }

    private static void DockMinimapWind(Minimap map)
    {
        var wind = map.m_windMarker;
        if (wind == null || _biomePlate == null)
            return;

        if (!ModConfig.MapWindPlate.Value || !_biomePlate.activeSelf)
        {
            RestoreMinimapWind(map);
            return;
        }

        if (wind.parent != _biomePlate.transform)
            wind.SetParent(_biomePlate.transform, false);

        wind.anchorMin = wind.anchorMax = new Vector2(1f, 0.5f);
        wind.pivot = new Vector2(0.5f, 0.5f);
        wind.sizeDelta = new Vector2(16f, 16f);
        wind.anchoredPosition = new Vector2(-16f, 0f);
        wind.localScale = Vector3.one;
        foreach (var img in wind.GetComponentsInChildren<Image>(true))
            img.color = RestlessUi.Text;
    }

    private static void RestoreMinimapWind(Minimap map)
    {
        var wind = map.m_windMarker;
        if (wind == null || _biomePlate == null)
            return;
        if (wind.parent != _biomePlate.transform)
            return;
        var home = Host(map);
        if (home != null)
            wind.SetParent(home, true);
    }

    private static void DockShipWind(Hud hud, Minimap map)
    {
        var wind = hud.m_shipWindIndicatorRoot;
        var mapRt = map.m_mapImageSmall?.rectTransform;
        var root = MapRoot(map);
        if (wind == null || mapRt == null || root == null)
            return;

        _shipWindHome ??= wind.parent;
        if (wind.parent != root)
            wind.SetParent(root, false);

        wind.localScale = Vector3.one;
        RestlessUi.Dock(wind, mapRt, new Vector2(0f, 0.5f), new Vector2(-12f, 0f));
    }

    private static void RestoreShipWind(Hud? hud)
    {
        var wind = hud?.m_shipWindIndicatorRoot;
        if (wind == null || _shipWindHome == null || wind.parent == _shipWindHome)
            return;
        wind.SetParent(_shipWindHome, false);
        wind.localScale = Vector3.one;
    }

    private static void LiftMarkers(Minimap map)
    {
        if (_studio?.Wrap == null || !_studio.Wrap.activeSelf)
        {
            ReturnMarkers(map);
            return;
        }

        var wrap = _studio.Wrap.transform;
        MapStudio.Adopt(map.m_pinRootSmall, wrap);
        MapStudio.Adopt(map.m_pinNameRootSmall, wrap);
        MapStudio.Adopt(map.m_smallMarker, wrap);
        MapStudio.Adopt(map.m_smallShipMarker, wrap);
    }

    private static void ReturnMarkers(Minimap map)
    {
        var host = Host(map);
        if (host == null)
            return;
        MapStudio.Adopt(map.m_pinRootSmall, host);
        MapStudio.Adopt(map.m_pinNameRootSmall, host);
        MapStudio.Adopt(map.m_smallMarker, host);
        MapStudio.Adopt(map.m_smallShipMarker, host);
        if (map.m_windMarker != null)
            MapStudio.Adopt(map.m_windMarker, host);
    }

    private static Transform? Host(Minimap map) =>
        map.m_mapImageSmall != null ? map.m_mapImageSmall.transform.parent : null;

    private static Transform? MapRoot(Minimap map)
    {
        if (map.m_smallRoot != null)
            return map.m_smallRoot.transform;
        if (map.m_mapSmall != null)
            return map.m_mapSmall.transform;
        return Host(map);
    }
}
