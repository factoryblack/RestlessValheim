"""Exercise publication planning and installation preflight in disposable repositories."""
from pathlib import Path
import importlib.util
import json
import shutil
import subprocess
import sys
import tempfile
import tomllib
import unittest

ROOT = Path(__file__).resolve().parents[2]


class ReleaseTools(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        (self.root / 'scripts').mkdir()
        for name in ['sync-versions.py', 'release-plan.py', 'stage-packages.py', 'install-local.ps1']:
            shutil.copy2(ROOT / 'scripts' / name, self.root / 'scripts' / name)
        shutil.copy2(ROOT / 'versions.yaml', self.root / 'versions.yaml')
        spec = importlib.util.spec_from_file_location('fixture_pins', self.root / 'scripts/sync-versions.py')
        pins = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(pins)
        self.packages = pins.load_pins()['packages']
        for package in self.packages.values():
            folder = self.root / package['thunderstore_dir']
            folder.mkdir(parents=True)
            for name in ['thunderstore.toml', 'manifest.json']:
                shutil.copy2(ROOT / package['thunderstore_dir'] / name, folder / name)
            config = tomllib.loads((folder / 'thunderstore.toml').read_text(encoding='utf-8'))
            for source in [config['build']['icon'], config['build']['readme'],
                           *[e['source'] for e in config['build'].get('copy', [])]]:
                path = (folder / source).resolve()
                if path.suffix:
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_text('fixture', encoding='utf-8')
                else:
                    path.mkdir(parents=True, exist_ok=True)
                    (path / 'fixture.png').write_text('fixture', encoding='utf-8')
        self.git('init', '-q')
        self.git('-c', 'user.name=Release Test', '-c', 'user.email=test@example.invalid',
                 'commit', '-q', '--allow-empty', '-m', 'fixture')

    def git(self, *args):
        return subprocess.check_output(['git', *args], cwd=self.root, text=True).strip()

    def run_script(self, name, *args, success=True):
        result = subprocess.run([sys.executable, str(self.root / 'scripts' / name), *args],
                                cwd=self.root, capture_output=True, text=True)
        self.assertEqual(result.returncode == 0, success, result.stderr)
        return result.stdout if success else result.stderr

    def test_release_plan_orders_pending_versions_and_never_changes_tags(self):
        self.git('tag', 'v0.0.1')
        self.git('tag', 'drawers-v' + self.packages['drawers']['version'])
        before = self.git('tag', '--list')
        output = self.run_script('release-plan.py')
        prefixes = {'core': 'v', 'cook': 'cook-v', 'works': 'works-v', 'plant': 'plant-v',
                    'piles': 'piles-v', 'storage': 'storage-v', 'pack': 'pack-v'}
        tags = [prefixes[p] + self.packages[p]['version']
                for p in ['core', 'cook', 'works', 'plant', 'piles', 'storage', 'pack']]
        self.assertEqual(output.splitlines(),
                         [line for tag in tags for line in [f'git tag {tag}', f'git push origin {tag}']])
        self.assertEqual(self.git('tag', '--list'), before)

    def test_install_plan_covers_all_plugins_assets_and_existing_workshop_folder(self):
        plan = json.loads(self.run_script('stage-packages.py', '--install-plan'))
        self.assertEqual({p['id'] for p in plan}, set(self.packages) - {'pack'})
        targets = [f['target'] for p in plan for f in p['files']]
        self.assertEqual(len(targets), len(set(targets)))
        self.assertEqual(sum(t.endswith('.dll') for t in targets), 7)
        self.assertIn('RestlessWorks/RestlessWorks.dll', targets)
        self.assertIn('RestlessCook/mesh/fixture.png', targets)
        self.assertFalse(any(t.endswith('.md') for t in targets))
        for target in targets:
            self.assertFalse(Path(target).is_absolute())
            self.assertNotIn('..', Path(target).parts)
        self.assertFalse((self.root / 'artifacts').exists())

    def test_missing_package_input_fails_preflight(self):
        (self.root / 'dist/RestlessPlant.dll').unlink()
        error = self.run_script('stage-packages.py', '--install-plan', success=False)
        self.assertIn('Missing package input', error)
        self.assertFalse((self.root / 'artifacts').exists())

    def test_stale_dll_fails_before_archiving_or_installing(self):
        shell = shutil.which('pwsh') or shutil.which('powershell')
        if not shell:
            self.skipTest('PowerShell unavailable')
        dll = self.root / 'dist/RestlessCore.dll'
        dll.unlink()
        generator = self.root / 'generate-fixture.ps1'
        generator.write_text("param([string]$Output)\nAdd-Type -TypeDefinition 'using System.Reflection; [assembly: AssemblyVersion(\"0.0.1.0\")] public class OldPlugin {}' -OutputAssembly $Output\n", encoding='utf-8')
        subprocess.run([shell, '-NoProfile', '-File', str(generator), str(dll)], check=True, capture_output=True)
        game = self.root / 'game'
        legacy = game / 'BepInEx/plugins/RestlessQoL/legacy.txt'
        legacy.parent.mkdir(parents=True)
        legacy.write_text('preserve me')
        result = subprocess.run([shell, '-NoProfile', '-File', str(self.root / 'scripts/install-local.ps1'),
                                 '-ValheimInstall', str(game), '-Python', sys.executable], capture_output=True, text=True)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('Stale build', result.stderr)
        self.assertEqual(legacy.read_text(), 'preserve me')
        self.assertFalse((game / 'BepInEx/plugins/RestlessCore').exists())
        self.assertFalse((self.root / 'artifacts').exists())

    def test_unsafe_stage_name_cannot_delete_outside_artifacts(self):
        path = self.root / 'thunderstore/plugin/manifest.json'
        manifest = json.loads(path.read_text())
        manifest['name'] = '../escape'
        path.write_text(json.dumps(manifest))
        error = self.run_script('stage-packages.py', success=False)
        self.assertIn('Invalid package staging name', error)
        self.assertFalse((self.root / 'artifacts').exists())


if __name__ == '__main__':
    unittest.main()
