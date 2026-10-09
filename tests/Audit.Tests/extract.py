"""Compile production methods against controllable game boundaries, without copies."""
from pathlib import Path
r = Path(__file__).resolve().parents[2]
def method(path, signature):
    text = (r/path).read_text(encoding='utf-8-sig')
    start = text.index(signature)
    brace = text.index('{', start)
    depth = 1; end = brace + 1
    while depth:
        depth += (text[end] == '{') - (text[end] == '}'); end += 1
    return text[start:end].replace('private static', 'internal static', 1)
groups = {
    'PileHarness': ('src/RestlessPiles/PileBag.cs', ['internal static void List(', 'internal static void Take(', 'public static int CountNearby(', 'public static int ConsumeNearby(', 'internal static int Drain(', 'private static int PullFromPile(', 'private static int TakeOwned(', 'private static ItemDrop.ItemData? RoomInBag(']),
    'PlantHarness': ('src/RestlessPlant/PlantHarvest.cs', ['private static Piece? SeedFor(', 'private static void AfterPick(', 'private static void Replant(', 'internal static void Tick()']),
    'WorksHarness': ('src/RestlessWorks/WorksRun.cs', ['private static void Tick(', 'private static void Describe(']),
    'CookHarness': ('src/RestlessCook/KitchenRun.cs', ['internal static void Tick(']),
}
body = 'using System; using System.Collections.Generic; using UnityEngine; using RestlessQoL.Storage;\n'
for name, (path, signatures) in groups.items():
    body += f'partial class {name} {{\n' + '\n'.join(method(path,s) for s in signatures) + '\n}\n'
dest=Path(__file__).parent/'obj/AuditMethods.cs'; dest.parent.mkdir(exist_ok=True); dest.write_text(body)
