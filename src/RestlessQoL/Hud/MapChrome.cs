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
    private static GameObject? _windPlate;
    private static MapStudio? _studio;
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
                _windPlate = null;
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
        RestoreWind(map);
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
        RestoreWind(map);
        ReturnMarkers(map);
        map.m_mapImageSmall.enabled = true;
        _studio?.Sleep();
        if (_biomePlate != null)
        {
            Object.Destroy(_biomePlate);
            _biomePlate = null;
            _biome = null;
        }

        if (_windPlate != null)
        {
            Object.Destroy(_windPlate);
            _windPlate = null;
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

    private static void RestoreWind(Minimap map)
    {
        var wind = map.m_windMarker;
        if (wind == null)
            return;
        var parent = wind.parent;
        if (parent == null || (parent.name != "RestlessWind" && parent.name != "RestlessBiome" && parent.name != "RestlessWindPlate"))
            return;
        var home = Host(map);
        if (home != null)
            wind.SetParent(home, true);
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
    }

    private static void EnsureWindPlate(Minimap map)
    {
        if (_windPlate != null)
            return;
        var parent = Host(map);
        if (parent == null)
            return;

        _windPlate = RestlessUi.Strip(parent, "RestlessWindPlate");
        RestlessUi.Pin(_windPlate, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28f, 28f));
    }

    private static void Sync(Minimap map)
    {
        var mapRt = map.m_mapImageSmall.rectTransform;
        QuietBox(map);

        if (_biomePlate != null && _biome != null)
        {
            var text = ModConfig.MapBiomePlate.Value && map.m_biomeNameSmall != null ? map.m_biomeNameSmall.text : "";
            _biome.text = text;
            _biomePlate.SetActive(PlateOn() && !string.IsNullOrEmpty(text));
            if (_biomePlate.activeSelf)
                RestlessUi.Dock(_biomePlate.GetComponent<RectTransform>(), mapRt, new Vector2(0.5f, 1f), new Vector2(0f, -16f));
        }

        DockWind(map, mapRt);
        LiftMarkers(map);
    }

    private static bool PlateOn() =>
        ModConfig.MapBiomePlate.Value;

    // Wind arrow gets its own plate docked to the left of the minimap,
    // independent of the biome bar underneath it.
    private static void DockWind(Minimap map, RectTransform mapRt)
    {
        var wind = map.m_windMarker;
        if (wind == null)
            return;

        if (!ModConfig.MapWindPlate.Value)
        {
            RestoreWind(map);
            if (_windPlate != null)
                _windPlate.SetActive(false);
            return;
        }

        EnsureWindPlate(map);
        if (_windPlate == null)
            return;

        _windPlate.SetActive(true);
        RestlessUi.Dock(_windPlate.GetComponent<RectTransform>(), mapRt, new Vector2(0f, 0.5f), new Vector2(-24f, 0f));

        if (wind.parent != _windPlate.transform)
            wind.SetParent(_windPlate.transform, false);

        var rt = wind;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(16f, 16f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
        foreach (var img in wind.GetComponentsInChildren<Image>(true))
            img.color = RestlessUi.Text;
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
}
