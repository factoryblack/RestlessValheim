"""Stage local release archives from the same TOMLs used by Thunderstore CLI."""
from pathlib import Path
import argparse
import importlib.util
import json
import shutil
import sys
sys.dont_write_bytecode = True
import tomllib
import zipfile

root = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('pins', root / 'scripts/sync-versions.py')
pins = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pins)
packages = pins.load_pins()['packages']
parser = argparse.ArgumentParser()
parser.add_argument('--projects', action='store_true')
parser.add_argument('--install-plan', action='store_true')
args = parser.parse_args()
if args.projects:
    print(json.dumps([p['csproj'] for p in packages.values() if 'csproj' in p]))
elif args.install_plan:
    result = []
    for package in packages.values():
        if "csproj" not in package: continue
        folder = root / package["thunderstore_dir"]
        config = tomllib.loads((folder / "thunderstore.toml").read_text(encoding="utf-8"))
        dlls = [e for e in config["build"].get("copy", []) if Path(e["source"]).suffix == ".dll"]
        if len(dlls) != 1: raise SystemExit(f"Expected one plugin DLL for {package['id']}")
        plugin_name = Path(dlls[0]["source"]).stem
        files = []
        for entry in config["build"].get("copy", []):
            source = (folder / entry["source"]).resolve()
            target = Path(entry["target"].strip("/"))
            if target.is_absolute() or ".." in target.parts:
                raise SystemExit(f"Invalid package target: {target}")
            if not source.exists(): raise SystemExit(f"Missing package input: {source}")
            if source.name in {"CHANGELOG.md", "README.md", "icon.png"}: continue
            for file in sorted(source.rglob("*")) if source.is_dir() else [source]:
                if file.is_file():
                    dest = target / (file.relative_to(source) if source.is_dir() else file.name)
                    files.append({"source": str(file), "target": (Path(plugin_name) / dest).as_posix()})
        result.append({"id": package["id"], "version": package["version"], "files": files})
    print(json.dumps(result))
else:
    plans = []
    for package in packages.values():
        folder = root / package['thunderstore_dir']
        config = tomllib.loads((folder / 'thunderstore.toml').read_text(encoding="utf-8"))
        manifest = json.loads((folder / 'manifest.json').read_text(encoding="utf-8"))
        name = f"{config['package']['namespace']}-{manifest['name']}-{manifest['version_number']}"
        artifacts = (root / 'artifacts').resolve()
        if (artifacts / name).resolve().parent != artifacts:
            raise SystemExit(f'Invalid package staging name: {name}')
        copies = [(folder / 'manifest.json', 'manifest.json'), (folder / config['build']['icon'], 'icon.png'), (folder / config['build']['readme'], 'README.md')]
        for entry in config['build'].get('copy', []):
            source = (folder / entry['source']).resolve()
            destination = entry['target'].strip('/')
            if Path(destination).is_absolute() or '..' in Path(destination).parts:
                raise SystemExit(f'Invalid package target: {destination}')
            copies.append((source, destination if source.is_dir() else str(Path(destination) / source.name)))
        for source, _ in copies:
            if not source.exists():
                raise SystemExit(f'Missing package input: {source}')
        plans.append((name, copies))
    for name, copies in plans:
        artifacts = (root / 'artifacts').resolve()
        stage = (artifacts / name).resolve()
        if stage.parent != artifacts:
            raise SystemExit(f'Invalid package staging name: {name}')
        if stage.exists():
            shutil.rmtree(stage)
        stage.mkdir(parents=True)
        for source, target in copies:
            destination = stage / target
            if source.is_dir():
                shutil.copytree(source, destination, dirs_exist_ok=True)
            else:
                destination.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(source, destination)
        archive = stage.parent / (name + '.zip')
        with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as output:
            for path in sorted(stage.rglob('*')):
                if path.is_file():
                    output.write(path, path.relative_to(stage))
        print(f'Wrote {archive}')
