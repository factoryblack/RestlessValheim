# Set-piece tooltip clarity

Set effect rows used to enter the same flat parser as the item's own stats.
That placed set recovery/resistance beside armour and floated set Slash/Chop
bonuses into the ordinary damage badges. A small `Set effect (3 parts)` row did
not communicate that everything following it was conditional on the full set.

The tooltip now extracts the complete native set header and effect body before
sorting item rows. The shared inspect/crafting renderer places it in an inset
Tray with `SET BONUS`, the effect name, equipped/required count and explicit
Active/Inactive state. A quiet green edge marks an active bonus; inactive uses
muted ink. Benefits, drawbacks, damage badges and flavour remain together inside
the well. It says the bonus requires equipped pieces and applies once per set.
Existing art and measured text/chip helpers are reused; quality diamonds remain
item quality. No extra set diamonds or padlocks.

Counts come from Humanoid.GetSetCount (cached method lookup), and activation from
the player's actual SEMan. Hovering an unequipped piece or crafting preview never
counts it as worn. No stat calculations, effect activation or gameplay changes.
Inspect includes activation/count in its existing 250 ms signature check;
crafting samples the cached preview's set state at the same interval and retains
scroll on refresh. No per-frame gear searches or preview clones are added.

Extraction matches the entire localised header and body after existing rich-text
normalisation. It removes one contiguous span, not all occurrences of those stats:
an identical per-piece bonus remains in the item section. Following native/mod
content is retained. If a mod changes the set block so it no longer matches the
native effect body, the original text is preserved rather than guessed or dropped.
Such custom tooltip replacements retain their original presentation as a fallback.

Validation: the PR workflow runs 9 pure extraction regressions and compiles all
plugins. In-game visual verification remains necessary:

- Bear chest alone: Armour/Weight/Durability above the well; Berserk recovery,
  weaknesses and Slash/Chop inside it; inactive and 1/3 equipped.
- Full Bear set: 3/3 Active on each piece, clearly one shared set bonus.
- Hover another bag/chest copy while full set is worn: still 3/3, never 4/3.
- Unequip a piece with the tooltip open; status refreshes without changing items.
- Craft/upgrade preview: shows currently equipped count, not a hypothetical count.
- Ordinary gear and gear with individual equip effects retain their own stats.
- Long descriptions, narrow crafting panels and other languages: content wraps
  within the well and remains inside the existing scrolling region.
