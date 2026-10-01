"""Check player-facing links and package documentation before release."""
from pathlib import Path
import json
import re
from urllib.parse import unquote

root = Path(__file__).resolve().parents[1]
files = [root / 'README.md', *sorted((root / 'docs/player').rglob('*.md')), *sorted((root / 'docs/maintainers').rglob('*.md')), *sorted((root / 'thunderstore').rglob('README.md'))]
errors = []
prefix = 'https://github.com/factoryblack/RestlessValheim/blob/main/'
for path in files:
    text = path.read_text(encoding='utf-8')
    for link in re.findall(r'!?\[[^\]]*\]\(([^\s)]+)\)', text):
        if link.startswith(prefix):
            target = root / unquote(link[len(prefix):].split('#')[0])
        elif '://' in link or link.startswith('#') or link.startswith('mailto:'):
            continue
        else:
            target = path.parent / unquote(link.split('#')[0])
        if not target.exists():
            errors.append(f'{path.relative_to(root)}: missing {link}')
    if 'maintainers' not in path.parts and 'restlessvalheim.vercel.app' in text:
        errors.append(f'{path.relative_to(root)}: accidental website link')
for folder in json.loads((root / 'thunderstore/public.json').read_text()):
    directory = root / 'thunderstore' / folder
    manifest = json.loads((directory / 'manifest.json').read_text())
    headings = re.findall(r'^## (.+)$', (directory / 'CHANGELOG.md').read_text(), re.M)
    if manifest['version_number'] not in headings:
        errors.append(f'{folder}: missing current version changelog')
    if '(screenshot coming)' not in (directory / 'README.md').read_text():
        errors.append(f'{folder}: missing screenshot placeholder')
if errors:
    raise SystemExit('\n'.join(errors))
print(f'Public documentation checks passed ({len(files)} pages, 8 packages).')
