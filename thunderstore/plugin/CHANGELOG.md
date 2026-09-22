# Changelog

## 0.1.13

- Tab and chests restore native graphics, TMP, hidden objects and CanvasGroups on undress (PR 17)
- Recipe rows and requirement cells remember native count/quality text and CanvasGroup state so turning the inventory skin off restores them (PR 18)
- Hammer requirement colours update even when the count text does not
- Crafting inspect shares tooltip badges and sections, and keeps scroll
- Split and variant dialogs use paper; native input and selection stay
- F8 has an ecosystem overview (Core plus Cook / Plant / Piles / Drawers). Cards show the loaded BepInEx version against `versions.yaml`, and Core also shows Jötunn
- Loadout totals overlay from the armor chip; hover a stat for origins (F8 toggle, not the ledger)
- Day and time sit on the biome bar (F8)
- Fermenter, smelter, kiln, cooking station, and beehive hovers show remaining time (F8)
- Jötunn pin is 2.30.2 (same as the pack). `versions.yaml` is the single pin list

## 0.1.12

- Hammer menu no longer re-dresses every piece and moves the grid on idle frames; hover, category, and live resource counts still update
- Tab inventory paper (bag, chest, craft, skills, collections) only re-dresses when a stack, recipe, or overlay actually changes

## 0.1.11

- Hammer / hoe / serving-tray menu uses the shared paper wells; piece cards keep native counts, stars, arrows and the grey/red availability tint
- Selected piece gets a compact paper card (name, description, costs, station) docked under the grid, sitting with the menu just above the hotbar; native icons and shortage colours stay
- The piece grid scales down once to leave room; hovering another piece grows the card downward without moving the grid
- Two or more costs sit side by side on a taller card so long recipes are not clipped
- The card is as wide as the piece grid, with the cost half taking more of that width so resource names stay on one line
- Build list and the two detail columns scroll on vanilla bars (narrow 10-unit tracks), not a kit scrollbar
- Search box keeps its placeholder, caret and selection

## 0.1.10

- ESC pause labels stay on the paper column (native MenuEntries text stays in the layout instead of being deactivated)

## 0.1.9

- World wind stays on the biome plate; the helm wind ring moves left of the minimap so it is not under the buffs
- Crafting paper stays grey stone: the dresser was retinting the plate black every frame over the right art
- Compendium, trophies and achievements use the shared paper collection shells (status text and progress stay)
- ESC and logout/quit confirmations use the shared paper kit
- M map biome, pin name, filters and switches use the shared paper kit; selected pin art stays live

## 0.1.8

- Hammer hover now shows our own clean health meter under the piece name/icon plate for anything with a WearNTear, instead of relying on the vanilla piece health bar (which stays hidden by default via the existing PieceHealth debug toggle)

## 0.1.7

- Wind arrow moves to its own plate on the left of the minimap instead of sharing the biome bar underneath it

## 0.1.6

- Vitals bleach the torn fill inside the meter mask so health/stamina/eitr read as colour, not charcoal (notice duration fill unchanged)
- Build-from-chests no longer unlocks unknown hammer pieces or feast boards
- Vacuum always syncs the world drop after a partial chest take; restock matches stack quality; storage RPCs reply when off and expire after 3s

## 0.1.5

- Health/adrenaline and stamina/eitr pairs sit vertically centered on the hotbar
- RestlessPiles can skip extra slots when dumping into a pile

## 0.1.4

- Clear Valheim 1.0 cheated item marks on inventory write, load, and world drops (F8, host-locked)
- Craft panel keeps vanilla wood when paper-panel art is missing (was a black slab)
- Minimap other-player pins use a cream diamond with an ink edge
- Craft-from-chests no longer treats a too-low bench as “have the materials” (upgrades stay gated by station level)
- Jötunn 2.30.1 (BepInExPack stays 5.4.2350)

## 0.1.3

- Paper frames on Tab, chests and skills only refit when their bounds move
- Chip buttons restore native layout on undress; HUD extra slots no longer re-dress every frame

## 0.1.2

- Recipe rows show have/need, including nearby chests
- Craft-from-chests no longer counts a LeaveOne-reserved last unit as available
- Adrenaline sits above health on the left of the hotbar; eitr sits above stamina on the right

## 0.1.1

- Listing describes RestlessCore only

## 0.1.0

- Nearby chests for craft, build, station pull, pet pantry, and ground vacuum
- Quick stack (`` ` ``) / restock (`Shift+`` ` ``); middle-click slot lock
- Extra worn slots and Z / X / C; unequip and upgrade park in the bag
- Area repair, wider station range, repair-on-use, stack size, host-locked F8
- Swim-wield, tame-safe fire, axe combo, loaded crossbow, death pins
- Eternal fires, 20 m hoe, floating drops, network uncap
- Restless HUD, inventory, and map chrome are in this build and still landing — vanilla and Restless currently mix
