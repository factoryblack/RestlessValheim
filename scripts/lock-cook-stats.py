# Write the locked v0.1 food numbers into cook.yaml. Safe to re-run.
from pathlib import Path

root = Path(__file__).resolve().parents[1]
path = root / "cook.yaml"
# health, stamina, eitr, regen, minutes
STATS = {
    "hunters_skillet": (32, 24, 0, 2, 25),
    "herb_roasted_venison": (40, 18, 0, 3, 25),
    "honeyed_mushrooms": (18, 42, 0, 2, 25),
    "hunters_hearth_board": (55, 30, 0, 3, 50),
    "honeyed_foragers_table": (30, 55, 0, 3, 50),
    "bear_mushroom_stew": (48, 18, 0, 3, 25),
    "forest_root_medley": (18, 50, 0, 2, 25),
    "foresters_game_supper": (60, 35, 0, 4, 50),
    "foragers_breakfast_board": (35, 60, 0, 3, 50),
    "sausage_turnip_bake": (60, 22, 0, 4, 30),
    "bogroot_mash": (22, 60, 0, 2, 30),
    "cryptkeepers_supper": (70, 40, 0, 4, 50),
    "bog_harvesters_table": (40, 70, 0, 3, 50),
    "serpent_steak": (75, 28, 0, 4, 30),
    "fishermans_chowder": (28, 70, 0, 3, 30),
    "pickled_catch": (25, 65, 0, 3, 30),
    "longship_leviathan_spread": (80, 45, 0, 5, 50),
    "deckhands_provision": (45, 80, 0, 4, 50),
    "wolf_roast": (72, 24, 0, 4, 30),
    "frostberry_preserve": (24, 72, 0, 2, 30),
    "wolf_kings_carving_board": (85, 50, 0, 5, 50),
    "peak_runners_supper": (50, 85, 0, 4, 50),
    "lox_rib_roast": (82, 27, 0, 5, 30),
    "barley_root_bake": (27, 82, 0, 3, 30),
    "jarls_lox_table": (95, 55, 0, 6, 50),
    "golden_harvest_board": (55, 95, 0, 5, 50),
    "sap_glazed_garden_medley": (32, 85, 15, 4, 30),
    "magecap_tart": (30, 18, 90, 4, 30),
    "queens_hunting_table": (100, 55, 30, 6, 50),
    "mistwalkers_garden_table": (55, 100, 35, 5, 50),
    "bonemaw_fiddlehead_roast": (110, 36, 0, 7, 30),
    "emberlords_carving_table": (115, 65, 35, 7, 50),
    "cinder_runners_spread": (65, 115, 40, 6, 50),
}

text = path.read_text(encoding="utf-8")
text = text.replace("updated: 2026-09-17", "updated: 2026-09-18")
text = text.replace("last_audit: 2026-09-17", "last_audit: 2026-09-18")
if "food_minutes" not in text.split("items:", 1)[0]:
    text = text.replace(
        "# kind: meal | feast | sideboard\n",
        "# kind: meal | feast | sideboard\n"
        "# Custom meals/feasts lock food, food_stamina, food_eitr, food_regen, food_minutes.\n"
        "# Vanilla feast = balanced. Custom A = health. Custom B = stamina. Mistlands+ custom feasts carry eitr.\n"
        "# Sideboards are not edible.\n",
    )

lines = text.splitlines(keepends=True)
out = []
cur = None
i = 0
while i < len(lines):
    line = lines[i]
    if line.startswith("  - id:"):
        cur = line.split(":", 1)[1].strip()
    if cur in STATS and line.startswith("    clone_from:"):
        out.append(line)
        i += 1
        while i < len(lines) and lines[i].lstrip().startswith("food"):
            i += 1
        h, s, e, r, m = STATS[cur]
        out.append(f"    food: {h}\n")
        out.append(f"    food_stamina: {s}\n")
        if e:
            out.append(f"    food_eitr: {e}\n")
        out.append(f"    food_regen: {r}\n")
        out.append(f"    food_minutes: {m}\n")
        cur = None
        continue
    out.append(line)
    i += 1

path.write_text("".join(out), encoding="utf-8")
body = path.read_text(encoding="utf-8")
for key in STATS:
    if f"  - id: {key}" not in body:
        raise SystemExit(f"missing row {key}")
    idx = body.index(f"  - id: {key}")
    chunk = body[idx:idx + 500]
    if "food:" not in chunk:
        raise SystemExit(f"no food block on {key}")
print(f"locked {len(STATS)} stat sheets")
