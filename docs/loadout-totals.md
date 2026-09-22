# Loadout totals

The old panel had no Image for PaperSurface to skin. Its armour source list read
GetArmor from every equipped item, including ammo; it treated the neutral damage
multiplier (1) as +100%, added attack multipliers, and misread the armour percentage
field. Its source text was also omitted from change detection.

This revision uses the existing paper panel, Averia/Metric helpers and cloned
native scrollbar art (10 UI units). Totals and a persistent, independently
scrollable source reader share one fitted panel. Hover, click or select a row to
inspect it. No new assets or gameplay configuration are introduced. The existing
F8 loadout toggle still controls it.

## Calculation contract

- Armour total: `Player.GetBodyArmor`. Sources: the actual chest, legs, helmet and
  shoulder slots, then active SE_Stats in order: `(armour + addArmor) * (1 +
  armorMultiplier)`. Weapons/ammunition/utility defaults are never armour sources.
- Block: equipped blocker with the player's Blocking skill; explicitly normal
  block power, not a parry prediction.
- Health/stamina/eitr: live current/max getters; current food values including
  decay. The remainder is labelled Base / other maximum, not invented base stats.
- Capacity: live max; active carry bonuses with a labelled base/other remainder.
- Equipment movement: native getter, actual equipment slots. Fixed effect speed
  bonuses are separate from equipment and conditional movement.
- Attack bonuses: active SE_Stats per damage type. Combine `(general multiplier)
  * (1 + type bonus)` in effect order; general multiplier only applies to its
  declared skill or All. Neutral 1 is zero bonus. Type bonuses remain visible even
  if the held weapon doesn't deal that type. Skill-specific bonuses for other
  weapons appear separately. These are percentages, **not final damage or DPS**.
- Recovery: live SEMan recovery getters. Positive SE_Stats recovery bonuses add;
  reductions multiply in effect order. Equipment eitr contributions follow the
  same slots and ordering as Player.GetEquipmentEitrRegenModifier. Food healing is
  explicitly per tick, with the recovery factor, not HP/second.
- Modified skills: native effective level, trained level, active modifiers and
  any remaining adjustment (including caps). Skill bonuses aren't damage bonuses.
- Resistance: native effective result. Candidate item/effect sources are listed;
  vanilla priority rules are explained instead of presenting them as a sum.
- Sets: count actually worn pieces; check the active effect list. Inactive bonuses
  are described but not counted. Active set effects count once, not per piece.
- All active effects, including hidden effects, have a description row.

Where a native total differs from known contributions, the difference is retained
as Other / game adjustment. Native totals take precedence. A third-party Harmony
patch's author cannot be inferred just from its resulting number.

Attack percentage attribution covers SE_Stats fields (including derived classes'
fields), not arbitrary custom ModifyAttack overrides or target-/hit-time procs.
We deliberately do not call ModifyAttack on a fabricated hit or remove/reapply
player effects to measure contributions. Such callbacks can have side effects.
World difficulty, attack multipliers, skill damage ranges, ammo damage and target
resistance belong to a contextual damage preview, not a passive percentage sum.
Custom effect descriptions remain visible; this is not a claim of universal mod
compatibility. Future attack providers should supply pure, typed contributions.

Mechanics were checked against the game method reference at
[davrum/assembly_valheim, 8d312f8](https://github.com/davrum/assembly_valheim/tree/8d312f8c303ad23a1339aa1760eab120a06a520d):
Player.GetBodyArmor, ApplyArmorDamageMods, UpdateModifiers, UpdateStats,
SE_Stats.ModifyAttack/ModifyArmorMods/Modify*Regen and SEMan.GetStatusEffects.
Compilation against the repository's current Steam assemblies remains the API gate.

## Update cost

Sample at most four times/second, only while open. Slot reflection metadata is
cached. No per-frame status reflection, inventory/hierarchy scans or global canvas
rebuild. Existing row values are updated in place; only changes in row structure
rebuild the list. Source text updates independently of the displayed total, and
scroll position survives value updates. The reader measures wrapped text.

## Validation

`dotnet run --project tests/LoadoutMath.Tests -c Release` checks 14 arithmetic
regressions using the same pure helpers as the collector. The PR workflow runs
these before compiling all five plugins. It does not validate Unity layout.

In-game acceptance checks:

1. Arrows equipped, then another ammo stack: armour total/sources stay unchanged.
   Equip/remove chest, legs, helmet and cape, including through ExtraSlots.
2. Bear set partial/full/partial: set count and active state change; the full set's
   slash/chop/recovery bonuses appear once, then disappear when inactive.
3. Two attack buffs of +10%: +21%, not +20%; neutral effects add no +100%.
   Switch between a qualifying and non-qualifying skill for a skill-only buff.
4. Stack recovery increases with a reduction; check ordered deltas against total.
   Change foods and wait for decay; current pools and food contributions update.
5. Change a source while retaining the same total: reader updates immediately on
   the next sample. Long effect descriptions fit and scroll, without another panel
   under/behind the paper. Mouse wheel moves a modest distance without inertia.
6. Small display/UI scale and gamepad selection: panel stays in bounds, selected
   rows scroll into view. Close, Tab, other character panels and F8 toggle clean up.
7. Profile while closed and open: no continuous hierarchy rebuilding or allocations
   from this sheet while closed. In-game visual/behaviour checks are still required.
