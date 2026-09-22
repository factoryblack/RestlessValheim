using System;
using System.Collections.Generic;
using BepInEx.Bootstrap;
using Jotunn;
using RestlessQoL.Api;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.Core;

public sealed partial class SettingsUi
{
    private static readonly List<SettingsPage> EcosystemPages = new();
    private static readonly SettingsPage CorePage = new("restless.core", "RestlessCore",
        "Less friction. A clearer view of your world.",
        "Nearby storage, everyday conveniences and the shared Restless interface. Gameplay rules belong to the host; your controls and visual preferences stay yours.");

    private static void RefreshEcosystem()
    {
        EcosystemPages.Clear();
        EcosystemPages.Add(new SettingsPage("restless.cook", "RestlessCook",
            "Cook the haul. Make meals. Set a feast.",
            "Meals and feast boards from Meadows to Ashlands, built around the ingredients you gather. RestlessCore provides the shared interface.",
            packageUrl: Package("RestlessCook")));
        EcosystemPages.Add(new SettingsPage("restless.plant", "RestlessPlant",
            "Grow a patch worth coming home to.",
            "Plant berry bushes and forage with the cultivator. Grow a square, pick a patch and see when fruit comes back.",
            packageUrl: Package("RestlessPlant")));
        EcosystemPages.Add(new SettingsPage("restless.piles", "RestlessPiles",
            "Your resources, stored out in the world.",
            "Wood stacks and stone piles become storage for their resource. Open a pile, take a bag's worth or deposit what you carry.",
            packageUrl: Package("RestlessPiles")));
        EcosystemPages.Add(new SettingsPage("restless.drawers", "RestlessDrawers",
            "Workshop cabinets. Same chests, quieter fronts.",
            "Wood, personal, reinforced and black metal drawers clone their matching chests. One container each; the fronts show what is inside. They snap together as furniture.",
            packageUrl: Package("RestlessDrawers")));
        // A module can supply its own details and settings action without a Core edit.
        foreach (var page in SettingsPageApi.Snapshot())
        {
            if (page.PluginGuid == CorePage.PluginGuid) continue;
            var index = EcosystemPages.FindIndex(p => p.PluginGuid == page.PluginGuid);
            if (index >= 0) EcosystemPages[index] = page;
            else EcosystemPages.Add(page);
        }
    }

    private static string Package(string name) => "https://thunderstore.io/c/valheim/p/Restless/" + name + "/";
    private static bool Loaded(SettingsPage page) => Chainloader.PluginInfos.ContainsKey(page.PluginGuid);

    private static string Status(SettingsPage page)
    {
        var pin = VersionPins.ForGuid(page.PluginGuid);
        if (!Chainloader.PluginInfos.TryGetValue(page.PluginGuid, out var plugin) || plugin?.Metadata == null)
            return pin == null ? "Not installed / not loaded" : "Not installed · this drop lists " + pin;

        var line = "Loaded · " + plugin.Metadata.Version;
        if (pin != null && !SameVersion(plugin.Metadata.Version.ToString(), pin))
            line += " · this drop lists " + pin;
        if (page.PluginGuid == CorePage.PluginGuid)
            line += " · " + JotunnStatus();
        return line;
    }

    private static string JotunnStatus()
    {
        if (!Chainloader.PluginInfos.TryGetValue(Main.ModGuid, out var info) || info?.Metadata == null)
            return "Jötunn missing · expects " + VersionPins.Jotunn;
        var loaded = info.Metadata.Version.ToString();
        return SameVersion(loaded, VersionPins.Jotunn)
            ? "Jötunn " + loaded
            : "Jötunn " + loaded + " · expects " + VersionPins.Jotunn;
    }

    private static bool SameVersion(string loaded, string pin)
    {
        if (System.Version.TryParse(loaded, out var a) && System.Version.TryParse(pin, out var b))
            return a.Major == b.Major && a.Minor == b.Minor && Math.Max(a.Build, 0) == Math.Max(b.Build, 0);
        return string.Equals(loaded, pin, StringComparison.Ordinal);
    }

    private static Sprite? PageIcon(SettingsPage page) => page.Icon ?? Kit.Sprite(page.PluginGuid switch
    {
        "restless.core" => "ecosystem-core",
        "restless.cook" => "ecosystem-cook",
        "restless.plant" => "ecosystem-plant",
        "restless.piles" => "ecosystem-piles",
        "restless.drawers" => "ecosystem-drawers",
        _ => "nav-knot"
    });

    private static Color PageAccent(SettingsPage page) => page.PluginGuid switch
    {
        "restless.cook" => new Color(0.77f, 0.39f, 0.28f),
        "restless.plant" => new Color(0.55f, 0.69f, 0.40f),
        "restless.piles" => new Color(0.71f, 0.68f, 0.61f),
        "restless.drawers" => new Color(0.62f, 0.48f, 0.32f),
        _ => RestlessUi.Accent
    };

    private static void SelectPage(string id)
    {
        var tabs = CurrentTabs();
        var index = tabs.FindIndex(t => t.Id == id);
        if (index < 0) return;
        _tab = index;
        Highlight();
        Rebuild();
    }

    private static void PaintEcosystem(string id)
    {
        var page = EcosystemPages.Find(p => p.PluginGuid == id);
        if (page != null)
        {
            ModuleHero(page);
            EcosystemCopy(page.Details);
            Head(Loaded(page) ? "Part of your world" : "Expand your world");
            EcosystemCopy(Loaded(page)
                ? "This plugin is loaded locally. Multiplayer still follows the server's mod requirements and gameplay configuration."
                : "This is an optional expansion. Install it through your mod manager, then restart the game. This menu does not install or enable mods.");
            if (Loaded(page) && page.OpenSettings != null)
                EcosystemAction("Open " + page.Name + " settings", () =>
                {
                    _afterClose = () =>
                    {
                        try { page.OpenSettings(); }
                        catch (Exception error) { Plugin.Log.LogWarning("settings page " + page.PluginGuid + ": " + error); ShowHint("Could not open this module's settings."); }
                    };
                    Close();
                });
            else if (Loaded(page))
                EcosystemCopy("Use this mod's BepInEx configuration for its gameplay settings.");
            if (page.PackageUrl != null)
                EcosystemAction("View " + page.Name + " on Thunderstore ↗", () => Application.OpenURL(page.PackageUrl));
            return;
        }

        Head("One world. Your Restless collection.");
        EcosystemCopy("Core keeps the essentials together. Explore the expansions that suit the way you play.");
        var pages = new List<SettingsPage> { CorePage };
        pages.AddRange(EcosystemPages);
        for (var i = 0; i < pages.Count; i += 2)
        {
            var row = RestlessUi.Node(_body!.transform, "modules");
            row.AddComponent<LayoutElement>().preferredHeight = 188f;
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            ModuleCard(row.transform, pages[i]);
            if (i + 1 < pages.Count) ModuleCard(row.transform, pages[i + 1]);
        }
        Head("The collection");
        EcosystemCopy("Prefer a curated starting point? The Restless Valheim modpack brings the collection together. A modpack is a mod-manager profile, not a plugin we can detect here.");
        EcosystemAction("View the Restless Valheim modpack ↗", () => Application.OpenURL(Package("Restless_Valheim")), Kit.Sprite("ecosystem-pack"));
    }

    private static void ModuleCard(Transform parent, SettingsPage page)
    {
        var card = RestlessUi.Graphic(parent, page.PluginGuid, Color.white, true);
        card.AddComponent<LayoutElement>().flexibleWidth = 1f;
        RestlessUi.PaperControl(card, PageAccent(page));
        var icon = RestlessUi.Graphic(card.transform, "icon", Color.white, false).GetComponent<Image>();
        icon.sprite = PageIcon(page);
        icon.preserveAspect = true;
        RestlessUi.Pin(icon.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -14f), new Vector2(64f, 64f));
        var title = RestlessUi.Label(card.transform, page.Name, 23, RestlessUi.Text, TextAnchor.MiddleLeft);
        RestlessUi.Stretch(title.gameObject, new Vector2(0f, 1f), Vector2.one, new Vector2(90f, -49f), new Vector2(-12f, -12f));
        RestlessUi.BoundedLabel(title, 23, 17);
        var status = RestlessUi.Label(card.transform, Status(page), 14,
            Loaded(page) ? PageAccent(page) : RestlessUi.PaperMuted, TextAnchor.MiddleLeft);
        RestlessUi.Stretch(status.gameObject, new Vector2(0f, 1f), Vector2.one, new Vector2(90f, -80f), new Vector2(-12f, -49f));
        RestlessUi.BoundedLabel(status, 14, 12);
        var copy = RestlessUi.Label(card.transform, page.Summary, 19, RestlessUi.PaperMuted, TextAnchor.UpperLeft);
        RestlessUi.Stretch(copy.gameObject, Vector2.zero, Vector2.one, new Vector2(16f, 35f), new Vector2(-16f, -91f));
        RestlessUi.BoundedLabel(copy, 19, 16);
        var action = RestlessUi.Label(card.transform, page == CorePage ? "Open Core settings ›" : "Explore module ›",
            16, PageAccent(page), TextAnchor.MiddleLeft);
        RestlessUi.Stretch(action.gameObject, Vector2.zero, new Vector2(1f, 0f), new Vector2(16f, 8f), new Vector2(-16f, 31f));
        var button = card.AddComponent<Button>();
        button.targetGraphic = card.GetComponent<Image>();
        RestlessUi.PaperSelectable(button);
        button.onClick.AddListener(() => SelectPage(page == CorePage ? "storage" : page.PluginGuid));
        ScrollRelay.Bind(card, _scroll);
    }

    private static void ModuleHero(SettingsPage page)
    {
        var hero = RestlessUi.Node(_body!.transform, "module-heading");
        hero.AddComponent<LayoutElement>().preferredHeight = 150f;
        var icon = RestlessUi.Graphic(hero.transform, "icon", Color.white, false).GetComponent<Image>();
        icon.sprite = PageIcon(page);
        icon.preserveAspect = true;
        RestlessUi.Pin(icon.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(128f, 128f));
        var title = RestlessUi.Label(hero.transform, page.Name, 32, RestlessUi.Text, TextAnchor.MiddleLeft);
        RestlessUi.Stretch(title.gameObject, Vector2.zero, Vector2.one, new Vector2(156f, 72f), new Vector2(-12f, -20f));
        RestlessUi.BoundedLabel(title, 32, 24);
        var status = RestlessUi.Label(hero.transform, Status(page), 18, PageAccent(page), TextAnchor.MiddleLeft);
        RestlessUi.Stretch(status.gameObject, Vector2.zero, Vector2.one, new Vector2(156f, 20f), new Vector2(-12f, -86f));
        RestlessUi.BoundedLabel(status, 18, 15);
    }

    private static void EcosystemCopy(string copy)
    {
        var text = RestlessUi.Label(_body!.transform, copy, 20, RestlessUi.PaperMuted, TextAnchor.UpperLeft);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        // Measure against the actual sheet width, before the parent layout pass.
        var width = RestlessUi.PanelWidth - 238f - RestlessUi.TrayGutter - 14f;
        text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        text.gameObject.AddComponent<LayoutElement>().preferredHeight = text.preferredHeight + 12f;
    }

    private static void EcosystemAction(string title, Action action, Sprite? icon = null)
    {
        var plate = Row();
        var label = RestlessUi.Label(plate.transform, title, 20, RestlessUi.Text, TextAnchor.MiddleCenter);
        RestlessUi.Stretch(label.gameObject, Vector2.zero, Vector2.one, new Vector2(12f, 4f), new Vector2(-12f, -4f));
        RestlessUi.BoundedLabel(label, 20, 16);
        if (icon != null)
        {
            var mark = RestlessUi.Graphic(plate.transform, "collection-icon", Color.white, false).GetComponent<Image>();
            mark.sprite = icon;
            mark.preserveAspect = true;
            RestlessUi.Pin(mark.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(34f, 34f));
            RestlessUi.Stretch(label.gameObject, Vector2.zero, Vector2.one, new Vector2(52f, 4f), new Vector2(-12f, -4f));
        }
        var button = plate.AddComponent<Button>();
        button.targetGraphic = plate.GetComponent<Image>();
        RestlessUi.PaperSelectable(button);
        button.onClick.AddListener(() => action());
    }
}
