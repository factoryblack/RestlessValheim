"""Print pending publication commands from pins and local tags; never publish."""
from pathlib import Path
import importlib.util
import subprocess
import sys

sys.dont_write_bytecode = True
root = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('versions', root / 'scripts/sync-versions.py')
versions = importlib.util.module_from_spec(spec)
spec.loader.exec_module(versions)
packages = versions.load_pins()['packages']
prefixes = {'core': 'v', 'cook': 'cook-v', 'plant': 'plant-v', 'piles': 'piles-v',
            'drawers': 'drawers-v', 'storage': 'storage-v', 'works': 'works-v', 'pack': 'pack-v'}
for name in ['core', 'cook', 'works', 'plant', 'piles', 'drawers', 'storage', 'pack']:
    tag = prefixes[name] + packages[name]['version']
    existing = subprocess.check_output(['git', 'tag', '--list', tag], cwd=root, text=True).strip()
    if existing:
        continue
    print(f'git tag {tag}\ngit push origin {tag}')
