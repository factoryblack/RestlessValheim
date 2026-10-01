"""Keep package bios, guide links and the pack roster consistent.

Editorial package pages are hand-written. Versions remain in versions.yaml;
bios/guide links live in thunderstore/public.json. No runtime files are changed.
"""
from pathlib import Path
import importlib.util
import json
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("restless_versions", ROOT / "scripts/sync-versions.py")
versions = importlib.util.module_from_spec(spec)
spec.loader.exec_module(versions)


def render():
    pins = versions.load_pins()
    public = json.loads((ROOT / "thunderstore/public.json").read_text(encoding="utf-8"))
    expected = {p["thunderstore_dir"].split("/")[-1] for p in pins["packages"].values()}
    if set(public) != expected:
        raise ValueError("public.json must describe exactly the packages in versions.yaml")
    planned = {}
    for folder, info in public.items():
        if len(info["description"]) > 250 or "\n" in info["description"]:
            raise ValueError(folder + ": bio must fit 250 characters on one line")
        for filename in ("manifest.json", "thunderstore.toml"):
            path = Path("thunderstore") / folder / filename
            text = (ROOT / path).read_text(encoding="utf-8")
            if filename.endswith("json"):
                data = json.loads(text)
                data["description"] = info["description"]
                data["website_url"] = info["website_url"]
                text = json.dumps(data, indent=2) + "\n"
            else:
                for key, val in (("description", info["description"]), ("websiteUrl", info["website_url"])):
                    text, count = re.subn(r"^" + key + r" = .*?$", lambda _: key + " = " + json.dumps(val, ensure_ascii=False), text, count=1, flags=re.M)
                    if count != 1:
                        raise ValueError(str(path) + ": missing " + key)
            planned[path] = text
    path = Path("thunderstore/pack/README.md")
    text = (ROOT / path).read_text(encoding="utf-8")
    rows = ["| Included mod | What it adds |", "| --- | --- |"]
    for key in pins["packages"]["pack"]["members"]:
        pkg = pins["packages"][key]
        info = public[pkg["thunderstore_dir"].split("/")[-1]]
        url = "https://thunderstore.io/c/valheim/p/Restless/" + pkg["thunderstore"] + "/"
        rows.append("| [" + info["name"] + "](" + url + ") | " + info["description"] + " |")
    block = "<!-- pack-members:start -->\n" + "\n".join(rows) + "\n<!-- pack-members:end -->"
    text, count = re.subn(r"<!-- pack-members:start -->[\s\S]*?<!-- pack-members:end -->", lambda _: block, text, count=1)
    if count != 1:
        raise ValueError("Pack page is missing its generated roster markers")
    planned[path] = text
    return planned


def main():
    if sys.argv[1:] not in ([], ["--check"]):
        raise SystemExit("Usage: sync-public-docs.py [--check]")
    planned = render()
    stale = [str(p) for p, s in planned.items() if (ROOT / p).read_text(encoding="utf-8").replace("\r\n", "\n") != s]
    if sys.argv[1:] == ["--check"]:
        if stale:
            raise SystemExit("Public metadata is stale; run python scripts/sync-public-docs.py:\n" + "\n".join(stale))
        print("Public package metadata and pack roster match")
    else:
        for path, text in planned.items():
            (ROOT / path).write_text(text, encoding="utf-8", newline="\n")
        print("Updated public metadata in " + str(len(planned)) + " files")


if __name__ == "__main__":
    main()
