# Restless UI kit

This is the maintained reference for the shared kit. The older asset audits and
playtest notes are historical records, not statements about current branch status.

## Supported expansion API

`RestlessQoL.Api.UiKitApi.Version` is 1. Reference `RestlessCore.dll` and Unity UI.
Use the API on Unity's main thread after GUI creation. Gameplay data, network
requests and recipe planning belong to the expansion, not these controls.

| Component | Supported entry point | Behaviour |
| --- | --- | --- |
| Paper material | `Surface` | Panel or compact chip; optional accent |
| Primary action | `ActionSurface` | Shared forged action material |
| Button | `Button` | Shared padding, readable text and interaction states |
| Search / number input | `Field` | Single-line text; configurable limit; optional integer validation |
| Tabs | `Tab` | Category strip and selected marker |
| Quality | `Quality` | Diamonds and bounded numeric overflow |
| Vertical reader | `Reader`, `ResizeReader`, `SizeReader` | Native scrollbar, content gutter, Restless wheel motion |
| Progress | `Progress` | Pooled completed/running layers; explicit hide/reset |
| Theme / placement | `Text`, `Muted`, `Accent`, `Place` | Shared colours and top-left canvas coordinates |
| Asset contract | `Asset(UiKitAsset)` | Immutable drawing rules, dimensions, content insets and minimum size |

Cooking and Workshop use the public button, field, reader and progress builders.
Their recipe, order and machine cards retain domain-specific layouts. A full
recipe-card schema, rarity presentation and skill-tree widgets are not promised
by version 1: extract those when their data and interaction contracts are clear.
Internal `RestlessUi` and `Kit` methods remain implementation details. Existing
friend assemblies still use specialised internal components; new expansions do
not need friend access for the supported components above.

```csharp
using RestlessQoL.Api;

var search = UiKitApi.Field(root, "Search…", new Rect(24, 24, 280, 40));
var reader = UiKitApi.Reader(root, "items", new Rect(24, 76, 280, 400), 64);
search.onValueChanged.AddListener(query => RefreshItems(query));
// Update content only when the data changes, then measure its actual height.
UiKitApi.SizeReader(reader, measuredHeight);
```

The external consumer in `tests/UiKit.Consumer` deliberately has no friend access.
CI compiles it against the real game/Unity references to protect the public boundary.

## Ownership and updates

The caller owns returned GameObjects and listeners. Destroy the screen root when
closing permanently; pooled screens can deactivate their root. Shared textures,
sprites and asset descriptors belong to Core: do not destroy or mutate them.
These builders do not poll, perform network work or add per-frame layout updates.
The existing reader advances wheel motion while moving and otherwise uses native
ScrollRect behaviour. Do not rebuild cards or refresh text every frame.

Buttons use the existing normal, hover, selected, pressed and disabled states.
Set `interactable` for availability. Keep a readable explanation beside blocked
or host-controlled actions; colour alone does not explain why an action is blocked.
Progress `Set` updates both layers and visibility; `Hide` clears pooled visibility.

## Asset contracts

`Api/UiKitAssets.cs` owns the supported material roles. Native sizes and slice caps
are artwork pixels. Minimum sizes and content insets are canvas units. Insets use
left, bottom, right, top order. Simple ornaments preserve aspect; do not stretch
corners or portraits to fill arbitrary rectangles. Insets are guidance for the
content layout, not automatic padding added to every Image.

The material helpers consume these same descriptors. Existing screen-specific
spacing is preserved. The caller must leave room for descriptions and translated
labels: measure description height instead of shrinking long prose. Best-fit text
is reserved for compact controls with a bounded minimum font size.

`Kit.Texture` caches one decoded texture per embedded name. Sprite variants share
that texture and are keyed by name plus the full border vector. Missing/decode
failures are remembered and logged once per asset, so bad references do not keep
allocating or flood the log. Warm-up discovers embedded PNG resources automatically,
including `cook-portrait` and ecosystem icons; there is no second asset-name list.

`python3 scripts/check-ui-kit.py` validates every supported material against its
PNG header and rejects invalid slice caps. It runs before CI compilation. New
supported roles require a descriptor and a matching embedded asset. Specialised
artwork outside these roles remains internal until its layout contract is reusable.

## Visual acceptance

Build checks cannot certify in-game appearance. Before release, check:

- Cooking and Workshop search, quantity controls, actions, progress and all tabs.
- Native scrollbar width, drag behaviour, wheel direction and reader resizing.
- A long item name and long description at normal and smaller UI resolutions.
- Keyboard/controller focus, disabled actions and readable blocked explanations.
- Switching pooled cards between active progress, no progress and empty results.
- Tooltip quality, category, paper panel, icon frame and corner proportions.

Keep the diamond inventory lock marker, native scrollbars and restrained Nordic
visual treatment. Add new artwork only when a component needs a distinct semantic
state; avoid turning every label into another framed badge.
