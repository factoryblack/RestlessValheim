"""Stage local release archives from the same TOMLs used by Thunderstore CLI."""
from pathlib import Path
import argparse
import importlib.util
import json
import shutil
import tomllib
import zipfile

root = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('pins', root / 'scripts/sync-versions.py')
pins = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pins)
packages = pins.load_pins()['packages']
parser = argparse.ArgumentParser()
parser.add_argument('--projects', action='store_true')
args = parser.parse_args()
if args.projects:
    print(json.dumps([p['csproj'] for p in packages.values() if 'csproj' in p]))
else:
    plans = []
    for package in packages.values():
        folder = root / package['thunderstore_dir']
        config = tomllib.loads((folder / 'thunderstore.toml').read_text())
        manifest = json.loads((folder / 'manifest.json').read_text())
        name = f"{config['package']['namespace']}-{manifest['name']}-{manifest['version_number']}"
        copies = [(folder / 'manifest.json', 'manifest.json'), (folder / config['build']['icon'], 'icon.png'), (folder / config['build']['readme'], 'README.md')]
        for entry in config['build'].get('copy', []):
            source = (folder / entry['source']).resolve()
            destination = entry['target'].strip('/')
            copies.append((source, destination if source.is_dir() else str(Path(destination) / source.name)))
        for source, _ in copies:
            if not source.exists():
                raise SystemExit(f'Missing package input: {source}')
        plans.append((name, copies))
    for name, copies in plans:
        stage = root / 'artifacts' / name
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
