# UI audit fixes and the F8 ecosystem

Source baseline: `a156afa192f778cf994f791f325d336d8a464aa4` (22 September 2026).
This is an implementation/review note, not a second feature catalogue.

## Audit repairs

- Inventory records native graphic colour, enabled/raycast state, TMP visibility
  limits and renderer alpha before suppression. Objects explicitly deactivated by
  the dresser regain their previous active state. Quiet CanvasGroups are restored
  or removed if the dresser created them. Slot snapshots are taken once per cell,
  before the shared slot helper changes native chrome; owned objects are excluded.
- Build requirement labels/counts refresh their semantic colour independently of
  changes to their text. The existing idle-update path remains in place.
- Craft detail and inspect share contribution badge/section rendering. Craft
  preview passes a cloned result item to existing tooltip providers, including
  the next quality for upgrades. The selected recipe is resolved only on a detail
  rebuild. API invalidation participates in both the inventory stamp and detail
  cache. Existing same-recipe scroll offsets remain intact.
- SplitDialog and VariantDialog have a dedicated, conservative material pass.
  Known panel backings and text-button faces use the existing paper kit. Native
  inputs, carets, slider tracks, icon-only variant selection and hit rects stay
  intact. The broad inventory dresser still skips them. Dialog text and visibility
  join the existing change stamp; there is no new per-frame hierarchy scan.

## F8 navigation and visual direction

F8 opens to a collection overview on first use and remembers the selected page
within the session. The left rail separates Core settings from expansion pages;
Q/E and mouse navigation remain available. The former HUD list is split into HUD
and Screens & slots. All previous ModConfig bindings and host locks are retained.
The scrollable rail accommodates additional registered modules.

The overview has a two-column set of paper cards: Core, Cook, Plant and Piles.
Cards use the exact committed Thunderstore icons (linked embedded resources),
cream headings, brief summaries, explicit plugin-load status and quiet accents.
Individual module pages show the larger icon, description and package link. The
pack has a separate link and graphic: a modpack is not a detectable loaded plugin.

No new bitmap artwork is introduced. Existing package graphics establish unique
module identities without another set of ornate frames. Missing modules are
labelled `Not installed / not loaded`; Core cannot distinguish absence from a
plugin load failure. Only a user's package-link click opens a browser. There are
no update requests, downloads, installs, telemetry or mod enable/disable controls.
Loaded status reflects BepInEx locally, not server compatibility or feature toggles.

## Module page API

`RestlessQoL.Api.SettingsPageApi.Register(SettingsPage)` returns an IDisposable.
Register on Unity's main thread during startup; dispose during shutdown. Pages
are snapshotted when F8 opens. Use the actual BepInEx plugin GUID (namespaced).
Duplicate registrations fail; built-in Cook/Plant/Piles descriptors can be
replaced by registering their GUID. Core's settings pages stay Core-owned.
The API accepts display strings, an optional borrowed Sprite, an HTTPS package
URL and an optional OpenSettings action. Core never destroys the borrowed sprite.

```csharp
_registration = SettingsPageApi.Register(new SettingsPage(
    PluginGuid, "My expansion", "A short player-facing summary.",
    "What this expansion adds to your world.", icon: mySprite,
    packageUrl: "https://thunderstore.io/c/valheim/p/MyTeam/MyExpansion/",
    openSettings: () => MySettings.Open()));
```

An installed module's explicit settings action runs after F8 finishes closing
and releases its input block. Exceptions are logged without crashing Core.
Modules retain configuration ownership and host synchronisation. Built-in pages
without a registered action point players to the mod's existing BepInEx config;
this pass does not pretend to provide their settings controls.

## Validation

Local checks: C# syntax parsing; project XML parsing; all old F8 ModConfig
references retained; embedded icon paths verified against the pinned repo tree;
catalogue canvas regenerated. The local environment has no .NET SDK or Valheim
assemblies. GitHub's existing PR workflow supplies the actual compilation gate.
No Unity render or in-game verification is claimed. Binary icon previews could
not be retrieved through the connector; artwork is reused unmodified by path.

Before merging, playtest:

1. Toggle inventory skin off/on with bag, chest, crafting, split and variants.
   Native counts, controls, dimmers, gamepad hints and inputs must return; repeat
   open/close and world reload to catch duplicate plates or stale static state.
2. Keep a build piece selected while requirement colour changes without the count
   changing. Check shortage recovery, station requirements and selection changes.
3. Register a tooltip badge/section provider; inspect then craft/upgrade its item.
   Confirm result quality, Invalidate refresh, disposal and exception isolation.
   Long contributions must scroll without losing the item identity. Confirm
   m_selectedRecipe remains the expected recipe/item pair on the target game build.
4. Open F8 with Core alone, then the full collection. Test Q/E, scroll, key capture,
   text entry, the ledger toggle, Escape and reopen. Check smaller UI scales,
   1080p/1440p, controller selection and long labels. Every old control must remain
   reachable and host-locked settings must remain locked for a non-admin client.
5. Verify icon clarity and the card summaries at game size. Select an absent
   expansion, follow its explicit link, register a module page with a settings
   action, and confirm F8 releases input before that settings UI opens.

Performance-specific refactoring remains with the parallel review. New module
catalogue/icon work occurs on menu open or page selection, not per frame.
