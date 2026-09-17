# Unflip RCM1 V (Meshy FBX is already Unity-oriented) and bleed albedo islands
# into black UV gutters so seams do not sample empty padding.
import struct
from pathlib import Path

root = Path(__file__).resolve().parents[1]
runtime = root / "art" / "cook" / "runtime"
mesh = root / "art" / "cook" / "mesh"


def unflip_rcm(path: Path) -> str:
    data = bytearray(path.read_bytes())
    if len(data) < 12 or data[:3] != b"RCM":
        raise SystemExit(f"bad rcm {path.name}")
    if data[3] == ord("2"):
        return "skip"
    if data[3] != ord("1"):
        raise SystemExit(f"unknown rcm {path.name}")
    verts = int.from_bytes(data[4:8], "little", signed=True)
    pos = 12
    for _ in range(verts):
        vpos = pos + 28
        old = struct.unpack_from("<f", data, vpos)[0]
        struct.pack_into("<f", data, vpos, 1.0 - old)
        pos += 32
    data[3] = ord("2")
    path.write_bytes(data)
    return "flipped"


def dilate_png(path: Path, rounds: int = 12) -> str:
    import numpy as np
    from PIL import Image

    img = Image.open(path).convert("RGBA")
    arr = np.array(img, dtype=np.uint16)
    empty = (arr[:, :, 3] < 8) | ((arr[:, :, 0] < 8) & (arr[:, :, 1] < 8) & (arr[:, :, 2] < 8))
    if not empty.any():
        return "clean"

    for _ in range(rounds):
        filled = ~empty
        acc = np.zeros(arr.shape, dtype=np.uint32)
        cnt = np.zeros(arr.shape[:2], dtype=np.uint16)
        for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1), (-1, -1), (1, -1), (-1, 1), (1, 1)):
            src = np.roll(np.roll(arr, dy, 0), dx, 1)
            mask = np.roll(np.roll(filled, dy, 0), dx, 1)
            if dy < 0:
                mask[-dy:, :] = False
            elif dy > 0:
                mask[:dy, :] = False
            if dx < 0:
                mask[:, -dx:] = False
            elif dx > 0:
                mask[:, :dx] = False
            acc[mask] += src[mask]
            cnt[mask] += 1
        paint = empty & (cnt > 0)
        if not paint.any():
            break
        color = acc[paint] // cnt[paint][:, None]
        arr[paint] = color
        arr[paint, 3] = 255
        empty[paint] = False

    Image.fromarray(arr.astype(np.uint8), "RGBA").save(path)
    return "dilated"


def main():
    rcm = sorted(runtime.glob("*.rcm"))
    if len(rcm) != 35:
        raise SystemExit(f"expected 35 rcm, found {len(rcm)}")
    flipped = 0
    for path in rcm:
        if unflip_rcm(path) == "flipped":
            flipped += 1

    stems = sorted(p.stem for p in runtime.glob("*.png"))
    dilated = 0
    for stem in stems:
        src = mesh / f"{stem}.png"
        if not src.exists():
            src = runtime / f"{stem}.png"
        if dilate_png(src) == "dilated":
            dilated += 1
        dest = runtime / f"{stem}.png"
        if src != dest:
            dest.write_bytes(src.read_bytes())

    print(f"rcm unflipped {flipped}/{len(rcm)}; albedo dilated {dilated}/{len(stems)}")


if __name__ == "__main__":
    main()
