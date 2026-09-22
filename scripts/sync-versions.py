# Stamp loader and package pins from versions.yaml.
# Source of truth is that file. Do not edit stamped Jötunn / BepInEx / package
# versions by hand. Feature rows stay in catalogue.yaml.
from pathlib import Path
import json
import re
import sys

root = Path(__file__).resolve().parents[1]
pins_path = root / "versions.yaml"


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
            if key == "members":
                data["packages"][pkg][key] = [
                    x.strip() for x in val.strip("[]").split(",") if x.strip()
                ]
            elif key == "needs_core":
                data["packages"][pkg][key] = val.lower() == "true"
            else:
                data["packages"][pkg][key] = val
    if "bepinex" not in data or "jotunn" not in data:
        raise SystemExit("versions.yaml needs bepinex and jotunn")
    if "core" not in data["packages"]:
        raise SystemExit("versions.yaml needs packages.core")
    return data


def sub(text, pattern, repl, count=0):
    updated, n = re.subn(pattern, repl, text, count=count)
    return updated, n


def write_if(path: Path, text: str, planned: dict):
    path = path if path.is_absolute() else root / path
    planned[str(path.relative_to(root)).replace("\\", "/")] = text


def stamp_directory_props(pins, planned):
    path = root / "Directory.Build.props"
    text = path.read_text(encoding="utf-8")
    if "<JotunnLibVersion>" in text:
        text, _ = sub(text, r"<JotunnLibVersion>.*</JotunnLibVersion>",
                      f"<JotunnLibVersion>{pins['jotunn']}</JotunnLibVersion>")
    else:
        text = text.replace(
            "    <ExecutePrebuild>true</ExecutePrebuild>",
            f"    <JotunnLibVersion>{pins['jotunn']}</JotunnLibVersion>\n"
            "    <ExecutePrebuild>true</ExecutePrebuild>",
            1,
        )
    write_if(path, text, planned)


def stamp_csproj(pkg, planned):
    rel = pkg.get("csproj")
    if not rel:
        return
    path = root / rel
    text = path.read_text(encoding="utf-8")
    text, _ = sub(text, r"<Version>.*</Version>", f"<Version>{pkg['version']}</Version>", count=1)
    text, _ = sub(
        text,
        r'<PackageReference Include="JotunnLib" Version="[^"]+" />',
        '<PackageReference Include="JotunnLib" Version="$(JotunnLibVersion)" />',
    )
    write_if(path, text, planned)


def stamp_plugin(pkg, planned):
    rel = pkg.get("plugin")
    if not rel:
        return
    path = root / rel
    text = path.read_text(encoding="utf-8")
    if pkg["id"] == "core":
        text, n = sub(
            text,
            r'public const string PluginVersion = .*?;',
            "public const string PluginVersion = VersionPins.Core;",
            count=1,
        )
        if n and "using RestlessQoL.Core;" not in text:
            text = text.replace("using RestlessQoL.Core;\n", "using RestlessQoL.Core;\n", 1)
        if "using RestlessQoL.Core;" not in text:
            text = text.replace("namespace RestlessQoL;\n", "using RestlessQoL.Core;\n\nnamespace RestlessQoL;\n", 1)
    else:
        text, _ = sub(
            text,
            r'public const string PluginVersion = ".*";',
            f'public const string PluginVersion = "{pkg["version"]}";',
            count=1,
        )
    write_if(path, text, planned)


def stamp_toml(path: Path, version, deps, planned):
    text = path.read_text(encoding="utf-8")
    text, _ = sub(text, r'versionNumber = ".*"', f'versionNumber = "{version}"', count=1)
    for key, ver in deps.items():
        line = f'{key} = "{ver}"'
        if re.search(rf'{re.escape(key)} = "', text):
            text, _ = sub(text, rf'{re.escape(key)} = ".*"', line, count=1)
        else:
            block = re.search(r"(\[package\.dependencies\]\n(?:[A-Za-z0-9_-]+ = \".*\"\n)*)", text)
            if block:
                text = text.replace(block.group(1), block.group(1) + line + "\n", 1)
            else:
                text = text.replace("[package.dependencies]\n", "[package.dependencies]\n" + line + "\n", 1)
    write_if(path, text, planned)


def stamp_manifest(path: Path, name, version, deps, planned):
    data = json.loads(path.read_text(encoding="utf-8"))
    data["name"] = name
    data["version_number"] = version
    listed = data.get("dependencies") or []
    for key, ver in deps.items():
        prefix = key + "-"
        entry = f"{key}-{ver}"
        if any(d.startswith(prefix) for d in listed):
            listed = [entry if d.startswith(prefix) else d for d in listed]
        else:
            listed.append(entry)
    data["dependencies"] = listed
    write_if(path, json.dumps(data, indent=2) + "\n", planned)


def package_deps(pins, pkg):
    deps = {
        "denikson-BepInExPack_Valheim": pins["bepinex"],
        "ValheimModding-Jotunn": pins["jotunn"],
    }
    if pkg.get("needs_core"):
        deps["Restless-RestlessCore"] = pins["packages"]["core"]["version"]
    if pkg["id"] == "pack":
        for member in pkg.get("members") or []:
            other = pins["packages"][member]
            deps[f"Restless-{other['thunderstore']}"] = other["version"]
    return deps


def stamp_thunderstore(pins, planned):
    for pkg in pins["packages"].values():
        folder = pkg.get("thunderstore_dir")
        if not folder:
            continue
        deps = package_deps(pins, pkg)
        stamp_toml(root / folder / "thunderstore.toml", pkg["version"], deps, planned)
        stamp_manifest(root / folder / "manifest.json", pkg["thunderstore"], pkg["version"], deps, planned)


def stamp_version_pins(pins, planned):
    pkgs = pins["packages"]
    cases = []
    consts = []
    for key, pkg in pkgs.items():
        if "guid" not in pkg:
            continue
        name = key[:1].upper() + key[1:]
        consts.append(f'    public const string {name} = "{pkg["version"]}";')
        cases.append(f'        "{pkg["guid"]}" => {name},')
    body = "\n".join(consts)
    switch = "\n".join(cases)
    text = f"""namespace RestlessQoL.Core;

// Generated by scripts/sync-versions.py from versions.yaml. Do not edit.
internal static class VersionPins
{{
    public const string BepInEx = "{pins['bepinex']}";
    public const string Jotunn = "{pins['jotunn']}";
{body}

    public static string? ForGuid(string guid) => guid switch
    {{
{switch}
        _ => null
    }};
}}
"""
    write_if(root / "src/RestlessQoL/Core/VersionPins.cs", text, planned)


def stamp_catalogue(pins, planned):
    path = root / "catalogue.yaml"
    text = path.read_text(encoding="utf-8")
    text, _ = sub(text, r'(id: loader\.bepinex[\s\S]*?note: "Required to load anything\. Pin )[^ ]+',
                  rf'\g<1>{pins["bepinex"]}', count=1)
    text, _ = sub(text, r'(id: loader\.jotunn[\s\S]*?note: "Config sync / admin lock\. Pin )[^ ]+',
                  rf'\g<1>{pins["jotunn"]}', count=1)
    write_if(path, text, planned)


def stamp_workflow(pins, planned):
    path = root / ".github/workflows/build.yml"
    text = path.read_text(encoding="utf-8")
    text, _ = sub(text, r"BEPINEX_PACK: .*", f"BEPINEX_PACK: {pins['bepinex']}", count=1)
    text, _ = sub(text, r"JOTUNN: .*", f"JOTUNN: {pins['jotunn']}", count=1)
    write_if(path, text, planned)


def stamp_readmes(pins, planned):
    replacements = {
        root / "thunderstore/plugin/README.md": (
            r"Needs \*\*BepInExPack [^*]+\*\* and \*\*Jötunn [^*]+\*\*.",
            f"Needs **BepInExPack {pins['bepinex']}** and **Jötunn {pins['jotunn']}**.",
        ),
        root / "thunderstore/pack/README.md": (
            r"Plus \*\*BepInExPack [^*]+\*\* and \*\*Jötunn [^*]+\*\*.",
            f"Plus **BepInExPack {pins['bepinex']}** and **Jötunn {pins['jotunn']}**.",
        ),
    }
    for path, (pattern, repl) in replacements.items():
        text = path.read_text(encoding="utf-8")
        text, n = sub(text, pattern, repl, count=1)
        if n:
            write_if(path, text, planned)


def plan(pins):
    planned = {}
    stamp_directory_props(pins, planned)
    stamp_version_pins(pins, planned)
    stamp_catalogue(pins, planned)
    stamp_workflow(pins, planned)
    stamp_readmes(pins, planned)
    stamp_thunderstore(pins, planned)
    for pkg in pins["packages"].values():
        stamp_csproj(pkg, planned)
        stamp_plugin(pkg, planned)
    return planned


def apply(planned):
    for rel, text in planned.items():
        path = root / rel
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8", newline="\n")


def check(planned):
    drift = []
    for rel, text in planned.items():
        path = root / rel
        current = path.read_text(encoding="utf-8") if path.exists() else ""
        if current.replace("\r\n", "\n") != text.replace("\r\n", "\n"):
            drift.append(rel)
    if drift:
        print("version pins are stale. Run python scripts/sync-versions.py")
        for rel in drift:
            print("  " + rel)
        raise SystemExit(1)
    print(f"version pins match ({len(planned)} files)")


def main(argv):
    pins = load_pins()
    if argv == ["--print", "jotunn"]:
        print(pins["jotunn"], end="")
        return
    if argv == ["--print", "bepinex"]:
        print(pins["bepinex"], end="")
        return
    planned = plan(pins)
    if argv == ["--check"]:
        check(planned)
        return
    if argv:
        raise SystemExit("usage: sync-versions.py [--check] [--print jotunn|bepinex]")
    apply(planned)
    print(f"stamped {len(planned)} files from versions.yaml")
    print(f"  jotunn {pins['jotunn']}  bepinex {pins['bepinex']}  core {pins['packages']['core']['version']}")


if __name__ == "__main__":
    main(sys.argv[1:])
