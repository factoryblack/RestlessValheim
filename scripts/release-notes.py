"""Extract a package's actual version notes, excluding Unreleased and siblings."""
from pathlib import Path
import argparse
import re

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument("package", choices=["plugin", "cook", "plant", "piles", "drawers", "storage", "works", "pack"])
parser.add_argument("version")
parser.add_argument("--output", required=True)
args = parser.parse_args()
text = (root / "thunderstore" / args.package / "CHANGELOG.md").read_text(encoding="utf-8")
match = re.search(r"^## " + re.escape(args.version) + r"\s*\n([\s\S]*?)(?=^## |\Z)", text, re.M)
if not match:
    raise SystemExit("No changelog section for " + args.package + " " + args.version)
Path(args.output).parent.mkdir(parents=True, exist_ok=True)
Path(args.output).write_text("## " + args.version + "\n\n" + match.group(1).strip() + "\n\n[Player guides](https://github.com/factoryblack/RestlessValheim/blob/main/docs/player/README.md)\n", encoding="utf-8")
