# Regenerates thunderstore/cook/README.md from cook.yaml.
# Source of truth is the yaml. Full-size plates live in docs/cook/wiki/.
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
text = (root / "cook.yaml").read_text(encoding="utf-8")
items = []
cur = None
use = None
mode = None
section = None
for raw in text.splitlines():
    line = raw.rstrip()
    if line in ("kit:", "items:"):
        if cur and section == "items":
            items.append(cur)
        cur = None
        use = None
        mode = None
        section = line[:-1]
        continue
    if section != "items":
        continue
    if line.startswith("  - id:"):
        if cur:
            items.append(cur)
        cur = {"id": line.split(":", 1)[1].strip(), "feeds": [], "uses": []}
        use = None
        mode = None
        continue
    if cur is None:
        continue
    if line.startswith("    feeds:"):
        mode = "feeds"
        if "[]" in line:
            mode = None
        continue
    if line.startswith("    uses:"):
        mode = "uses"
        continue
    if mode == "feeds" and line.startswith("      - "):
        cur["feeds"].append(line.split("-", 1)[1].strip())
        continue
    if mode == "uses" and line.startswith("      - item:"):
        use = {"item": line.split(":", 1)[1].strip()}
        cur["uses"].append(use)
        continue
    if mode == "uses" and use is not None and line.startswith("        amount:"):
        use["amount"] = int(line.split(":", 1)[1].strip())
        continue
    if line.startswith("    ") and ":" in line and not line.startswith("      "):
        mode = None
        key, val = line.strip().split(":", 1)
        val = val.strip()
        if len(val) >= 2 and val[0] == val[-1] and val[0] in "\"'":
            val = val[1:-1]
        if key in ("station_level", "output_amount", "food", "food_stamina", "food_eitr", "food_regen", "food_minutes"):
            cur[key] = int(val)
        else:
            cur[key] = val

if cur:
    items.append(cur)

by_id = {i["id"]: i for i in items}
by_prefab = {i["prefab"]: i for i in items if i.get("prefab")}

TIER = {
    "meadows": "Meadows",
    "blackforest": "Black Forest",
    "swamp": "Swamp",
    "ocean": "Ocean",
    "mountain": "Mountain",
    "plains": "Plains",
    "mistlands": "Mistlands",
    "ashlands": "Ashlands",
}
TIERS = list(TIER)
KIND = {"meal": "Meal", "feast": "Feast", "sideboard": "Sideboard"}
STATION = {
    "piece_cauldron": "Cauldron",
    "piece_preptable": "Food preparation table",
}

VANILLA = {
    "BarleyFlour": "Barley flour",
    "BlackSoup": "Black soup",
    "BloodPudding": "Blood pudding",
    "Blueberries": "Blueberries",
    "BoarJerky": "Boar jerky",
    "Bread": "Bread",
    "Carrot": "Carrot",
    "CarrotSoup": "Carrot soup",
    "ChickenEgg": "Egg",
    "Cloudberry": "Cloudberry",
    "Bloodbag": "Bloodbag",
    "CookedAsksvinMeat": "Cooked asksvin meat",
    "CookedBjornMeat": "Cooked bear meat",
    "CookedBoneMawSerpentMeat": "Cooked bonemaw meat",
    "CookedBugMeat": "Cooked seeker meat",
    "CookedChickenMeat": "Cooked chicken",
    "CookedDeerMeat": "Cooked deer meat",
    "CookedEgg": "Cooked egg",
    "CookedHareMeat": "Cooked hare meat",
    "CookedLoxMeat": "Cooked lox meat",
    "CookedMeat": "Cooked boar meat",
    "CookedVoltureMeat": "Cooked volture meat",
    "CookedWolfMeat": "Cooked wolf meat",
    "Entrails": "Entrails",
    "Dandelion": "Dandelion",
    "Eyescream": "Eyescream",
    "Fiddleheadfern": "Fiddlehead",
    "FierySvinstew": "Fiery svinstew",
    "FishAndBread": "Fish 'n' bread",
    "FishCooked": "Cooked fish",
    "FishWraps": "Fish wraps",
    "FreezeGland": "Freeze gland",
    "FreshSeaweed": "Fresh seaweed",
    "Honey": "Honey",
    "HoneyGlazedChicken": "Honey glazed chicken",
    "LoxPie": "Lox meat pie",
    "MagicallyStuffedShroom": "Stuffed mushroom",
    "MarinatedGreens": "Marinated greens",
    "MashedMeat": "Mashed meat",
    "MeatPlatter": "Meat platter",
    "MinceMeatSauce": "Minced meat sauce",
    "MisthareSupreme": "Misthare supreme",
    "MushroomSmokePuff": "Smoke puff",
    "Mushroom": "Mushroom",
    "MushroomJotunPuffs": "Jotun puffs",
    "MushroomMagecap": "Magecap",
    "MushroomOmelette": "Mushroom omelette",
    "MushroomYellow": "Yellow mushroom",
    "NeckTailGrilled": "Grilled neck tail",
    "Onion": "Onion",
    "OnionSoup": "Onion soup",
    "PiquantPie": "Piquant pie",
    "PulledBear": "Pulled bear",
    "QueensJam": "Queen's jam",
    "Raspberry": "Raspberry",
    "RoastedCrustPie": "Roasted crust pie",
    "RoyalJelly": "Royal jelly",
    "Salad": "Salad",
    "Sausages": "Sausages",
    "ScorchingMedley": "Scorching medley",
    "SeekerAspic": "Seeker aspic",
    "SerpentMeatCooked": "Cooked serpent meat",
    "SerpentStew": "Serpent stew",
    "ShocklateSmoothie": "Muckshake",
    "SizzlingBerryBroth": "Sizzling berry broth",
    "SparklingShroomshake": "Sparkling shroomshake",
    "SpiceAshlands": "Fiery Spice Powder",
    "SpiceForests": "Woodland Herb Blend",
    "SpiceMistlands": "Herbs of the Hidden Hills",
    "SpiceMountains": "Mountain Peak Pepper Powder",
    "SpiceOceans": "Seafarer's Herbs",
    "SpicePlains": "Grasslands Herbalist Harvest",
    "SpicyMarmalade": "Spicy marmalade",
    "Thistle": "Thistle",
    "Turnip": "Turnip",
    "TurnipStew": "Turnip stew",
    "Vineberry": "Vineberry",
    "WolfJerky": "Wolf jerky",
    "WolfMeatSkewer": "Wolf skewer",
    "YggdrasilPorridge": "Yggdrasil porridge",
    "sap": "Sap",
}

WIKI = "https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki"
ICON = "https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons"


def label(token):
    if token in by_prefab:
        return by_prefab[token]["name"]
    if token in VANILLA:
        return VANILLA[token]
    return re.sub(r"([a-z])([A-Z])", r"\1 \2", token)


def stem(row):
    return row["id"].replace("_", "-")


def recipe(row):
    bits = [f"{u['amount']} {label(u['item'])}" for u in row["uses"]]
    out = row.get("output_amount", 1)
    line = " + ".join(bits)
    if out != 1:
        return f"{line} → {out}"
    return line


def feeds(row):
    names = [by_id[f]["name"] for f in row["feeds"] if f in by_id]
    return ", ".join(names) if names else "—"


def station(row):
    name = STATION.get(row.get("station", ""), row.get("station", ""))
    level = row.get("station_level", 1)
    return f"{name} {level}" if name else ""


def icon_md(row):
    if row["source"] != "custom" or row["operation"] != "add":
        return ""
    return f'![{row["name"]}]({ICON}/{stem(row)}.png)'


def plate(row):
    return f'![{row["name"]}]({WIKI}/{stem(row)}.png)'


def stats(row):
    if row.get("kind") == "sideboard":
        return "not edible"
    if "food" not in row and "food_stamina" not in row:
        return "vanilla"
    line = f'{row.get("food", 0)}/{row.get("food_stamina", 0)}'
    if row.get("food_eitr"):
        line += f'/{row["food_eitr"]}'
    return f'{line} · {row.get("food_regen", 0)} regen · {row.get("food_minutes", 0)}m'


def md_row(cells):
    return "| " + " | ".join(cells) + " |"


lines = []
add = lines.append

add("# RestlessCook")
add("")
add("Valheim 1.0 cooking for Restless. **Cook the haul, make meals, turn meals into feasts.**")
add("")
add("Hard-depends on **RestlessCore**, **BepInExPack 5.4.2350**, and **Jötunn 2.30.1**. Everyone on the server needs this mod (minor version match).")
add("")
add("v0.1 is Meadows through Ashlands. Deep North waits.")
add("")
add("This page is the recipe wiki. Isolated plates are the full renders; the in-game slots use 256px copies of the same art. The graph itself is [`cook.yaml`](https://github.com/factoryblack/RestlessValheim/blob/main/cook.yaml).")
add("")
add("## How it works")
add("")
add("1. Cook the raw meat and fish first.")
add("2. Turn those cuts (and forage) into prepared meals.")
add("3. Assemble meals into feast boards. Two sideboards bundle leftover Mistlands and Ashlands plates so they still reach a vanilla feast.")
add("")
add("Every prepared meal still reaches at least one feast. Existing vanilla feasts stay valid sinks. Protein recipes that already exist are rewritten in place so they ask for the cooked cut.")
add("")
add("Vanilla feasts stay the balanced white fork. Custom feast A is health. Custom feast B is stamina. Mistlands and Ashlands custom feasts also carry eitr. Custom feasts consume finished meals, so they beat the vanilla board rather than matching it. Hidden Hills and Cinder sideboards are assembly pieces, not food.")
add("")
add("The Food preparation table and Serving tray are the vanilla pieces, unlocked in Meadows: 10 wood / 8 resin / 6 leather scraps for the table, 6 wood / 4 leather scraps / 2 resin for the tray at a workbench. Meadows and Black Forest custom boards do not ask for Bog Witch spices. Later custom feasts still do.")
add("")
add("## The matrix")
add("")
add("All 81 graph rows. Custom dishes show a thumb; vanilla rewrites and references keep Iron Gate art.")
add("")
add(md_row(["", "Dish", "Kind", "Biome", "Op", "H/S/E", "Recipe", "Goes into"]))
add(md_row(["---", "---", "---", "---", "---", "---", "---", "---"]))
for row in items:
    add(md_row([
        icon_md(row),
        row["name"],
        KIND.get(row["kind"], row["kind"]),
        TIER.get(row["tier"], row["tier"]),
        row["operation"],
        stats(row),
        recipe(row),
        feeds(row),
    ]))
add("")
add("## Plates")
add("")
add("Full-size isolated renders, grouped by biome. Meals first, then feasts, then sideboards.")
add("")

for tier in TIERS:
    rows = [i for i in items if i["tier"] == tier and i["operation"] == "add"]
    if not rows:
        continue
    add(f"### {TIER[tier]}")
    add("")
    for kind in ("meal", "feast", "sideboard"):
        group = [i for i in rows if i["kind"] == kind]
        if not group:
            continue
        add(f"#### {KIND[kind]}s")
        add("")
        for row in group:
            add(f"**{row['name']}**")
            add("")
            add(plate(row))
            add("")
            add(f"- Recipe: {recipe(row)}")
            add(f"- Station: {station(row)}")
            add(f"- Stats: {stats(row)}")
            add(f"- Goes into: {feeds(row)}")
            add("")
    add("")

rewrites = [i for i in items if i["operation"] == "rewrite"]
add("## Vanilla rewrites")
add("")
add("These keep their vanilla identity. Ingredients change in place; there is no second Meat Platter.")
add("")
add(md_row(["Dish", "Biome", "Now asks for"]))
add(md_row(["---", "---", "---"]))
for row in rewrites:
    add(md_row([row["name"], TIER.get(row["tier"], row["tier"]), recipe(row)]))
add("")
add("## Vanilla spices")
add("")
add("Not new items. They sit on feast boards.")
add("")
add("- Woodland Herb Blend (`SpiceForests`)")
add("- Seafarer's Herbs (`SpiceOceans`)")
add("- Mountain Peak Pepper Powder (`SpiceMountains`)")
add("- Grasslands Herbalist Harvest (`SpicePlains`)")
add("- Herbs of the Hidden Hills (`SpiceMistlands`)")
add("- Fiery Spice Powder (`SpiceAshlands`)")
add("")
add("This package does not replace RestlessCore. Install the **Restless Valheim** modpack, or Core then this.")
add("")

out = root / "thunderstore" / "cook" / "README.md"
out.write_text("\n".join(lines), encoding="utf-8")
print(f"wrote {out} ({len(items)} rows)")
