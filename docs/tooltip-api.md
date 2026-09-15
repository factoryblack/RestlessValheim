# Item inspect and expansion contributions

This branch rebuilds the existing inventory/chest item inspect surface. It does not
implement rarity, set mechanics, a skill tree, or a general screen-registration API.
The assembly remains `RestlessCore`; its existing namespace is `RestlessQoL`.

## Presentation

- 380 canvas-unit tray, reduced for narrower canvases; existing Averia and kit colours.
- 72-unit item slot and measured, wrapped title remain above the scrolling details.
- Actual item quality and optional expansion badges appear before the description.
- Muted labels and gold values have separate measured columns. Damage uses two
  generously sized chips per row. Longer values wrap.
- Unknown vanilla/mod tooltip lines remain visible; description text is not used
  to replace and discard those lines. Damage classification matches whole labels.
- Long details are clipped to the available canvas height. PgUp/PgDn pages the
  details while the item stays hovered. The footer shows the scroll percentage.
- The inspect canvas group cannot intercept inventory pointer events.
- The existing F8 tooltip toggle remains the on/off control.

The reference image now guides actual material assets as well as hierarchy. The
shared RestlessUi material helpers apply a new torn charcoal panel, separate corner
plate, textured chips, tintable edge masks and Nordic ornaments. The tooltip opts
into these helpers; other existing screens retain their original materials.
See `visual-materials.md` and `material-proof.html` for the asset proof and limits.

## Register a contribution

Reference `RestlessCore.dll` and use `RestlessQoL.Api`. Declare your dependency on
Core in your plugin as usual. Register on Unity's main thread during startup, keep
the returned handle, and dispose it during shutdown. IDs must be globally unique
(for example your plugin GUID plus `.tooltip`). Duplicate IDs throw.

```csharp
using System;
using RestlessQoL.Api;
using UnityEngine;

private IDisposable? _tooltipRegistration;

private void RegisterInspect()
{
    _tooltipRegistration = TooltipApi.Register(
        "com.example.myexpansion.tooltip",
        item =>
        {
            // Example only: your expansion owns and synchronises this metadata.
            if (!item.m_customData.TryGetValue("myexpansion.rarity", out var rarity))
                return null;

            var colour = new Color(0.55f, 0.72f, 0.9f);
            return new TooltipContribution(
                badges: new[] { new TooltipBadge(rarity, colour) },
                sections: new[] {
                    new TooltipSection("$myexpansion_set_title",
                        body: "$myexpansion_set_description")
                },
                rimTint: colour);
        },
        order: 100);
}

private void OnDestroy()
{
    _tooltipRegistration?.Dispose();
}
```

`TooltipApi.Version` is currently 1. This is an initial API to validate with a real
expansion before promising long-term binary compatibility.

## Contract

- All API calls and callbacks run on Unity's main thread. No network or expensive
  work in providers. Return null for items you do not augment; do not mutate items.
- Providers run by ascending order, then ordinal ID. Their sections and badges
  preserve their supplied order. First non-null rim tint wins. A rarity must also
  supply a text badge so its meaning does not rely on colour alone.
- The API copies contributed collections. The display localises `$tokens`, strips
  rich-text tags and owns all Unity objects. Unknown tokens remain visible for
  diagnosis. Extensions cannot inject arbitrary Unity UI through this contract.
- Providers are sampled at most every 0.25 seconds during normal hover, immediately
  on item change, and after `TooltipApi.Invalidate()`. Invalidate on your main-thread
  data-change event if you need the next update to refresh immediately.
- A provider exception skips its contribution and logs once per registration.
  The base item and other providers remain available; failed providers are retried.
- Disposing is idempotent and requests a refresh. No extension installed means no
  expansion badges/sections and the standard kit rim.
- This is presentation only. Host authority, saving and synchronisation of gameplay
  values remain the contributing system's responsibility.

## Validation status and handoff

Source inspection completed. This environment has no .NET SDK, game assemblies or
Unity/Valheim runtime, so **compilation and in-game appearance are unverified**.
No screenshot is claimed as an in-game rendering. Keep this as a draft until the
following checks pass on the existing Windows development setup:

1. Run `dotnet build src/RestlessQoL/RestlessQoL.csproj -c Release`, then use the
   existing `scripts/test-local.ps1` workflow to install and launch.
2. Inspect a torch, material stack, upgraded weapon, armour set piece and food in
   both player and chest grids. Compare all information with the vanilla tooltip
   by toggling F8's tooltip setting. Check set/effect lines and durability ranges.
3. Inspect long names, long descriptions and a non-English language. Confirm no
   text overlap, no false damage classification of resistance, and readable chips.
4. Hover the four screen corners at the supported UI scales/resolutions. Check
   the title tear, scroll clipping and panel bounds. Page both ways through a long
   item; its identity must remain visible and all details must be reachable.
5. Move between two items with identical names but different custom data, change
   quality/stack/durability, and lock/unlock a cell. Confirm refresh without leaving
   the hovered item. Close the inventory and toggle the tooltip off/on.
6. Drag, split and transfer items. The inspect card must never capture a click.
7. Register two providers in opposite insertion orders; verify stable order and
   first-tint precedence. Throw from one; verify base/other content survives. Dispose
   twice, re-register the ID, and verify the old content disappears.

Controller-specific overflow navigation is not implemented in this pass. Existing
hover discovery is unchanged; this does not claim support for additional equipment,
crafting, world or skill-tree surfaces.
