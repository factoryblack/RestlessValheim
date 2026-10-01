# Generates the player recipe reference from cook.yaml; never overwrites package copy.
# Source of truth is the yaml. Full-size plates live in docs/cook/wiki/.
from pathlib import Path
import json
import re

root = Path(__file__).resolve().parents[1]
VANILLA_ICON = json.loads((root / "scripts" / "cook-vanilla-icons.json").read_text(encoding="utf-8"))
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
    if row["source"] == "custom" and row["operation"] == "add":
        return f'![{row["name"]}]({ICON}/{stem(row)}.png)'
    url = VANILLA_ICON.get(row["id"])
    if url:
        return f'![{row["name"]}]({url})'
    return ""


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


# Player references are generated independently of the editorial package page.
import sys

BASE = root / "docs/player/recipes"
links = {i["id"]: i["tier"] + ".md#" + i["id"].replace("_", "-") for i in items}

def safe(text):
    return str(text).replace("|", "\\|")

def feed_links(row):
    return ", ".join("[" + safe(by_id[f]["name"]) + "](" + links[f] + ")" for f in row["feeds"] if f in by_id) or "No further recipe listed"

def food_details(row):
    if row.get("kind") == "sideboard":
        return "Not edible; used as a recipe ingredient."
    if "food" not in row and "food_stamina" not in row:
        return "Vanilla values; not overridden by this graph."
    return "Health: {} · Stamina: {} · Eitr: {} · Regeneration: {} per tick · Duration: {} minutes".format(row.get("food", 0), row.get("food_stamina", 0), row.get("food_eitr", 0), row.get("food_regen", 0), row.get("food_minutes", 0))

planned = {}
index = ["# Cook recipe reference", "", "Ingredients, station requirements, food values and recipe links, grouped by biome.", "", "[Learn the kitchen first](../cook.md) · [All player guides](../README.md)", "", "Generated from `cook.yaml`. This reference follows current main; older releases can use different recipes. Native recipe discovery and station upgrades still apply.", "", "## Browse by biome", "", "| Biome | Entries |", "| --- | --- |"]
for tier in TIERS:
    rows = [i for i in items if i["tier"] == tier]
    index.append("| [" + TIER[tier] + "](" + tier + ".md) | " + str(len(rows)) + " |")
index += ["", "## Reading the guide", "", "**New dish** means a custom addition; **Changed vanilla recipe** keeps the original item but changes its ingredients; **Vanilla reference** connects the food graph without adding a second item. An empty ingredient list on a reference is not a free crafting recipe.", "", "Each entry gives its output quantity and station level when specified by the graph. Sideboards are not edible. Food regeneration is per tick, not per second.", "", "[Changed vanilla recipes](vanilla-changes.md) · [Dish artwork gallery](gallery.md)", "", "There are " + str(len(items)) + " food graph entries, including references. This is not a count of newly added items.", ""]
planned[BASE / "README.md"] = "\n".join(index)
for tier in TIERS:
    rows = [i for i in items if i["tier"] == tier]
    page = ["# " + TIER[tier] + " recipes", "", "[Recipe index](README.md) · [Kitchen guide](../cook.md)", "", "Generated from `cook.yaml`; current-source reference.", "", "## Dishes", ""]
    for row in rows:
        page.append("- [" + safe(row["name"]) + "](#" + row["id"].replace("_", "-") + ")")
    for row in rows:
        page += ["", '<a id="' + row["id"].replace("_", "-") + '"></a>', "", "## " + row["name"], "", icon_md(row), "", KIND.get(row["kind"], row["kind"]) + " · " + {"add":"New dish", "rewrite":"Changed vanilla recipe", "reference":"Vanilla reference"}.get(row["operation"], row["operation"]), "", "- Ingredients: " + (recipe(row) if row["uses"] else "Use the native recipe; this entry does not supply an ingredient override."), "- Output quantity: " + (str(row["output_amount"]) if "output_amount" in row else "Native/default output"), "- Station: " + (station(row) or "Native station; not overridden here"), "- " + food_details(row), "- Used in: " + feed_links(row)]
    page += ["", "[Recipe index](README.md) · [Kitchen guide](../cook.md)", ""]
    planned[BASE / (tier + ".md")] = "\n".join(page)
rewrite = ["# Changed vanilla recipes", "", "These items keep their vanilla identity. Ingredients are changed in place, rather than adding duplicate items.", "", "[Recipe index](README.md) · [Kitchen guide](../cook.md)", "", "| Dish | Biome | Ingredients and output |", "| --- | --- | --- |"]
for row in items:
    if row["operation"] == "rewrite":
        rewrite.append(md_row(["[" + safe(row["name"]) + "](" + links[row["id"]] + ")", TIER.get(row["tier"], row["tier"]), safe(recipe(row))]))
rewrite += ["", "The preparation table and serving tray have their separate furniture/tool recipes described in the kitchen guide.", ""]
planned[BASE / "vanilla-changes.md"] = "\n".join(rewrite)
gallery = ["# Dish artwork", "", "Full-size reference renders of the added meals, feasts and sideboards. These images are artwork references; use gameplay screenshots to judge their in-game scale and appearance.", "", "[Recipe index](README.md) · [Kitchen guide](../cook.md)", ""]
for tier in TIERS:
    gallery += ["## " + TIER[tier], ""]
    for row in items:
        if row["tier"] == tier and row["source"] == "custom" and row["operation"] == "add":
            gallery += ["### [" + row["name"] + "](" + links[row["id"]] + ")", "", plate(row), ""]
planned[BASE / "gallery.md"] = "\n".join(gallery)
if sys.argv[1:] not in ([], ["--check"]):
    raise SystemExit("Usage: sync-cook-readme.py [--check]")
if sys.argv[1:] == ["--check"]:
    stale = [str(p.relative_to(root)) for p, s in planned.items() if not p.exists() or p.read_text(encoding="utf-8").replace("\r\n", "\n") != s]
    if stale:
        raise SystemExit("Recipe guides are stale; run python scripts/sync-cook-readme.py:\n" + "\n".join(stale))
    print("Cook recipe guides match (" + str(len(items)) + " entries)")
else:
    for path, text in planned.items():
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8", newline="\n")
    print("Updated " + str(len(planned)) + " recipe guides; package README preserved")
