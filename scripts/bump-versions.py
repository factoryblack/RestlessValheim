# Choose the next package version from the last published tag.
# The kind is the part that cannot be guessed from the diff.
#
#   python scripts/bump-versions.py core=new cook=new plant=new piles=new storage=new
#   python scripts/bump-versions.py --dry-run drawers=fix
#
# new    second digit, last digit returns to 0
# fix    last digit only
# break  first digit, only after 1.0
# done   1.0.0, only while that package is still 0.x
#
# An untagged version in versions.yaml is the open drop. The bump is applied
# to the last tag, so running this again reclassifies that drop instead of
# stacking another number. The pack's last digit moves only when the published
# pack is part of a new drop. Changelog sentences stay written by hand.
from pathlib import Path
import re
import subprocess
import sys

root = Path(__file__).resolve().parents[1]
pins_path = root / "versions.yaml"

# Tag prefix, Thunderstore name, short name used in the publish notes.
PACKAGES = {
    "core": ("v", "RestlessCore", "Core"),
    "cook": ("cook-v", "RestlessCook", "Cook"),
    "plant": ("plant-v", "RestlessPlant", "Plant"),
    "piles": ("piles-v", "RestlessPiles", "Piles"),
    "drawers": ("drawers-v", "RestlessDrawers", "Drawers"),
    "storage": ("storage-v", "RestlessStorage", "Storage"),
}
TAG_ORDER = ["core", "cook", "plant", "piles", "drawers", "storage"]
KINDS = {"new", "fix", "break", "done"}
HEADING = re.compile(r"^## (\d+\.\d+\.\d+)[ \t]*$", re.M)


def unquote(val: str) -> str:
    val = val.strip()
    if len(val) >= 2 and val[0] == val[-1] and val[0] in "\"'":
        return val[1:-1]
    return val


def load_pins():
    data = {"packages": {}}
    pkg = None
    for line in pins_path.read_text(encoding="utf-8").splitlines():
        raw = line.split("#", 1)[0].rstrip()
        if not raw.strip():
            continue
        indent = len(raw) - len(raw.lstrip())
        key, _, val = raw.strip().partition(":")
        key, val = key.strip(), unquote(val.strip())
        if indent == 0 and key == "packages":
            pkg = None
            continue
        if indent == 0:
            data[key] = val
            pkg = None
            continue
        if indent == 2 and not val:
            pkg = key
            data["packages"][pkg] = {"id": pkg}
            continue
        if indent == 4 and pkg:
            data["packages"][pkg][key] = val
    return data


def parse(version: str):
    parts = version.split(".")
    if len(parts) != 3 or not all(part.isdigit() for part in parts):
        raise SystemExit(f"not a three-part version: {version}")
    return tuple(int(part) for part in parts)


def fmt(version) -> str:
    return ".".join(str(part) for part in version)


def git_tags(prefix: str):
    out = subprocess.check_output(["git", "tag", "-l", prefix + "*"], cwd=root, text=True)
    found = []
    for line in out.splitlines():
        if not line.startswith(prefix):
            continue
        rest = line[len(prefix):]
        parts = rest.split(".")
        if len(parts) == 3 and all(part.isdigit() for part in parts):
            found.append(tuple(int(part) for part in parts))
    return found


def latest_tag(prefix: str):
    found = git_tags(prefix)
    return max(found) if found else None


def tag_exists(prefix: str, version: str) -> bool:
    name = prefix + version
    result = subprocess.run(
        ["git", "rev-parse", "-q", "--verify", "refs/tags/" + name],
        cwd=root,
        capture_output=True,
    )
    return result.returncode == 0


def apply_kind(base, kind: str):
    major, minor, patch = base
    if kind == "fix":
        return (major, minor, patch + 1)
    if kind == "new":
        return (major, minor + 1, 0)
    if kind == "break":
        if major == 0:
            raise SystemExit("a break before 1.0 is `new`. Use `done` when that package is finished.")
        return (major + 1, 0, 0)
    if kind == "done":
        if major != 0:
            raise SystemExit("`done` is only the step from 0.x to 1.0.0.")
        return (1, 0, 0)
    raise SystemExit(f"unknown kind {kind}")


def read_text(path: Path):
    data = path.read_bytes()
    newline = "\r\n" if b"\r\n" in data else "\n"
    return data.decode("utf-8").replace("\r\n", "\n"), newline


def write_text(path: Path, text: str, newline: str):
    path.write_bytes(text.replace("\n", newline).encode("utf-8"))


def set_yaml_version(text: str, pkg: str, version: str) -> str:
    pattern = rf"(^  {re.escape(pkg)}:\n(?:    .*\n)*?    version: \")[^\"]+(\")"
    updated, count = re.subn(pattern, rf"\g<1>{version}\2", text, count=1, flags=re.M)
    if count != 1:
        raise SystemExit(f"versions.yaml has no version for {pkg}")
    return updated


def open_end(text: str) -> int:
    matches = list(HEADING.finditer(text))
    if len(matches) < 2:
        return len(text)
    return matches[1].start()


def retitle(text: str, prefix: str, new: str) -> str:
    match = HEADING.search(text)
    if match is None:
        return f"# Changelog\n\n## {new}\n\n{text}"
    current = match.group(1)
    if current == new:
        return text
    if tag_exists(prefix, current):
        return text[:match.start()] + f"## {new}\n\n" + text[match.start():]
    return text[:match.start()] + f"## {new}" + text[match.end():]


def replace_open(text: str, old: str, new: str) -> str:
    if old == new:
        return text
    end = open_end(text)
    return text[:end].replace(old, new) + text[end:]


def parse_args(argv):
    dry = False
    kinds = {}
    for arg in argv:
        if arg == "--dry-run":
            dry = True
            continue
        if "=" not in arg:
            raise SystemExit("usage: bump-versions.py [--dry-run] core=new cook=fix")
        name, kind = arg.split("=", 1)
        if name == "pack":
            raise SystemExit("the pack's last digit moves by itself")
        if name not in PACKAGES:
            raise SystemExit(f"unknown package {name}")
        if kind not in KINDS:
            raise SystemExit(f"unknown kind {kind} (new, fix, break, done)")
        kinds[name] = kind
    if not kinds:
        raise SystemExit("usage: bump-versions.py [--dry-run] core=new cook=fix")
    return dry, kinds


def main(argv):
    dry, kinds = parse_args(argv)
    pins = load_pins()
    pkgs = pins["packages"]
    planned = {}
    for name, kind in kinds.items():
        prefix = PACKAGES[name][0]
        published = latest_tag(prefix)
        if published is None:
            raise SystemExit(f"{name} has no published tag to bump from")
        planned[name] = fmt(apply_kind(published, kind))

    old_core = pkgs["core"]["version"]
    new_core = planned.get("core", old_core)
    pack_published = latest_tag("pack-v")
    pack_now = pkgs["pack"]["version"]
    members_changed = any(planned[name] != pkgs[name]["version"] for name in planned)
    if members_changed and pack_published is not None and parse(pack_now) == pack_published:
        planned_pack = fmt(apply_kind(pack_published, "fix"))
    else:
        planned_pack = pack_now

    changed = {name: (pkgs[name]["version"], planned[name]) for name in planned if pkgs[name]["version"] != planned[name]}
    pack_changed = planned_pack != pack_now
    if not changed and not pack_changed:
        print("versions already match the last published tags", flush=True)
        for name in TAG_ORDER:
            if name in planned:
                print(f"  {name} {planned[name]} ({kinds[name]})", flush=True)
        return

    print("from the last published tag:", flush=True)
    for name in TAG_ORDER:
        if name not in planned:
            continue
        prefix = PACKAGES[name][0]
        published = fmt(latest_tag(prefix))
        print(f"  {name} {published} -> {planned[name]} ({kinds[name]})", flush=True)
    if pack_changed:
        print(f"  pack {pack_now} -> {planned_pack}", flush=True)
    else:
        print(f"  pack {pack_now} stays", flush=True)
    if dry:
        return

    yaml_text, yaml_nl = read_text(pins_path)
    for name, version in list(planned.items()) + [("pack", planned_pack)]:
        yaml_text = set_yaml_version(yaml_text, name, version)
    write_text(pins_path, yaml_text, yaml_nl)

    for name, (old, new) in changed.items():
        folder = pkgs[name]["thunderstore_dir"]
        path = root / folder / "CHANGELOG.md"
        text, newline = read_text(path)
        text = retitle(text, PACKAGES[name][0], new)
        if old_core != new_core:
            text = replace_open(text, f"Needs Core {old_core}", f"Needs Core {new_core}")
        write_text(path, text, newline)
        readme = root / folder / "README.md"
        if readme.exists() and old_core != new_core:
            body, readme_nl = read_text(readme)
            body = body.replace(f"Needs RestlessCore {old_core}", f"Needs RestlessCore {new_core}")
            body = body.replace(f"Needs Core {old_core}", f"Needs Core {new_core}")
            write_text(readme, body, readme_nl)

    if old_core != new_core:
        for name in PACKAGES:
            if name in changed:
                continue
            path = root / pkgs[name]["thunderstore_dir"] / "CHANGELOG.md"
            if not path.exists():
                continue
            text, newline = read_text(path)
            updated = replace_open(text, f"Needs Core {old_core}", f"Needs Core {new_core}")
            if updated != text:
                write_text(path, updated, newline)

    pack_log = root / pkgs["pack"]["thunderstore_dir"] / "CHANGELOG.md"
    text, newline = read_text(pack_log)
    if pack_changed:
        text = retitle(text, "pack-v", planned_pack)
    for name, (old, new) in changed.items():
        full = PACKAGES[name][1]
        text = replace_open(text, f"{full} {old}", f"{full} {new}")
    write_text(pack_log, text, newline)

    catalogue = root / "catalogue.yaml"
    text, newline = read_text(catalogue)
    for name, (old, new) in changed.items():
        full = PACKAGES[name][1]
        text = text.replace(f"{full} {old}", f"{full} {new}")
    if old_core != new_core:
        text = text.replace(f"Needs Core {old_core}", f"Needs Core {new_core}")
    write_text(catalogue, text, newline)

    publish = root / "thunderstore" / "PUBLISH.md"
    if publish.exists():
        text, newline = read_text(publish)
        for name, (old, new) in changed.items():
            full, short = PACKAGES[name][1], PACKAGES[name][2]
            text = re.sub(
                rf"(\| {re.escape(full)} \| [^|\n]+ \| \*\*){re.escape(old)}(\*\*)",
                rf"\g<1>{new}\2",
                text,
            )
            text = text.replace(f"{full} {old}", f"{full} {new}")
            text = text.replace(f"{short} {old}", f"{short} {new}")
        text = text.replace(
            "After a Jötunn or package bump: `python scripts/sync-versions.py`.",
            "A package bump is `python scripts/bump-versions.py core=new cook=fix`. Changelog sentences stay written by hand.",
        )
        versions = {name: pkgs[name]["version"] for name in TAG_ORDER}
        versions.update(planned)
        versions["pack"] = planned_pack
        lines = []
        for name in TAG_ORDER + ["pack"]:
            prefix = "pack-v" if name == "pack" else PACKAGES[name][0]
            version = versions[name]
            if tag_exists(prefix, version):
                continue
            tag = prefix + version
            lines.append(f"git tag {tag}")
            lines.append(f"git push origin {tag}")
        block = "\n".join(lines)
        text, count = re.subn(r"```\n(?:git tag .*?\n)+```", "```\n" + block + "\n```", text, count=1)
        if count != 1:
            raise SystemExit("thunderstore/PUBLISH.md is missing its tag block")
        write_text(publish, text, newline)

    subprocess.check_call([sys.executable, str(root / "scripts" / "sync-versions.py")], cwd=root)
    print("stamped. Changelog sentences were left as written.")


if __name__ == "__main__":
    main(sys.argv[1:])
