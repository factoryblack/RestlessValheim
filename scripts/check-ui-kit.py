#!/usr/bin/env python3
"""Validate supported material contracts against the embedded PNGs before compilation."""
import re
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "src/RestlessQoL/Assets"
manifest = (ROOT / "src/RestlessQoL/Api/UiKitAssets.cs").read_text()
pattern = r'new\("([\w-]+)",(\d+),(\d+),Image.Type.(\w+),(Vector4.zero|new Vector4\(([^)]+)\))'
entries = list(re.finditer(pattern, manifest))
roles = re.search(r'public enum UiKitAsset\s*\{([^}]+)\}', manifest).group(1).split(',')
errors = []
if len(entries) != len(roles):
    errors.append("Every supported role must have exactly one manifest entry")
seen = set()
for entry in entries:
    name, width, height, drawing, _, caps = entry.groups()
    if name in seen:
        errors.append(f"Duplicate material: {name}")
    seen.add(name)
    path = ASSETS / (name + ".png")
    if not path.is_file():
        errors.append(f"Missing embedded material: {name}")
        continue
    data = path.read_bytes()
    if len(data) < 24 or data[:8] != b'\x89PNG\r\n\x1a\n' or data[12:16] != b'IHDR':
        errors.append(f"Invalid PNG header: {name}")
        continue
    actual = struct.unpack('>II', data[16:24])
    if actual != (int(width), int(height)):
        errors.append(f"{name}: manifest {width}x{height}, artwork {actual[0]}x{actual[1]}")
    border = [float(v.strip().rstrip('f')) for v in caps.split(',')] if caps else [0]*4
    if len(border) != 4 or min(border) < 0 or border[0]+border[2] > actual[0] or border[1]+border[3] > actual[1]:
        errors.append(f"{name}: slice caps exceed artwork bounds")
    if drawing == 'Simple' and any(border):
        errors.append(f"{name}: proportional artwork must not declare slice caps")
if errors:
    raise SystemExit('\n'.join(errors))
print(f"UI kit: {len(entries)} material contracts match embedded artwork")
