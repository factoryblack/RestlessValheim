# RestlessCore

Valheim 1.0 quality-of-life core for Restless. One plugin, isolated Harmony modules, host-locked config.

This is **not** the old 60-mod `RestlessQOL` 1.0.7 pack. The master list of what is in the DLL, planned, optional, or skipped is [`catalogue.yaml`](catalogue.yaml).

## Layout

```
catalogue.yaml            Master behaviour list (built / planned / skip)
src/RestlessQoL/          C# BepInEx plugin
thunderstore/plugin/      Thunderstore package for the DLL
thunderstore/pack/        Optional one-click install of BepInEx + Jötunn + this DLL
```

## Plugin v0.1

Enabled by default, each feature can fail without taking the others down:

| Module | What it does |
|---|---|
| Craft from storage | Nearby chests count toward crafting |
| Build from storage | Nearby chests count toward placing pieces |
| Quick stack (`` ` ``) | Dump matching stacks into nearby chests |
| Restock (`Shift+`` ` ``) | Fill existing stacks from nearby chests |
| Station pull | Smelters/kilns take ore from those chests |
| Area repair | Hammer repair hits a radius |
| Workbench range | Configurable station use range |
| Death pins | Pin goes away when the tombstone is emptied |
| Swim-wield | Keep tools equipped in water |
| Friendly fire | Ballistae skip tames |
| Axe combo / crossbow | Small player papercuts |
| Eternal fire | Campfires / hearths stay lit (`Fireplace.m_infiniteFuel`) |
| Dig deeper | Hoe raise/dig cap is 20 m instead of 8 |
| Pet pantry | Hungry tames eat matching food from nearby chests |
| Ground vacuum | Floor piles route into chests that already have that item |
| Floating items | Drops float (nails still sink) |
| Settings panel | F8 or pause-menu Restless. Glass overlay, not Azu Config Manager. |
| Buff list | Food and status effects stacked under the minimap |

UI timers and dedicated armor slots are optional sidecars — see `catalogue.yaml`. PlanBuild, skills, drawers, collectors, portals, Auga, and ValheimPlus are skip.

## Pack v0.1

BepInEx + Jötunn + this DLL. Not a 60-mod stack. Optional MyLittleUI / EquipmentAndQuickSlots are listed in the catalogue if you still want them.

Do **not** also install ValheimPlus, BetterUI, MinimalUI, or AzuCraftyBoxes next to this. They patch the same inventory/build methods.

## Build

Valheim is expected at `C:\Program Files (x86)\Steam\steamapps\common\Valheim`. Copy `Environment.props.example` to `Environment.props` if yours is elsewhere.

```
dotnet build src/RestlessQoL/RestlessQoL.csproj -c Release
```

Output: `dist/RestlessCore.dll`. Drop it into a r2modman profile `BepInEx/plugins/RestlessCore/` along with Jötunn.

```
powershell -File scripts/test-local.ps1          # build + install into Steam Valheim
powershell -File scripts/test-local.ps1 -Launch  # same, then start the game
powershell -File scripts/pack.ps1                # zip artifacts/ for Thunderstore / r2modman import
```

This Steam folder is already BepInEx-patched. Launch from Steam, not a vanilla shortcut.

Until Thunderstore is published, r2modman: BepInEx + Jötunn online, then Import local `artifacts/Restless-RestlessCore-0.1.0.zip`. The pack zip only works after the plugin itself is on Thunderstore.
