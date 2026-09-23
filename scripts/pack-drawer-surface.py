# Fold Meshy metal + roughness into the chest shader's surface map.
# Red is metal. Alpha is smoothness (roughness flipped). Empty texels stay matte.
import io
import zipfile
from pathlib import Path

from PIL import Image

root = Path(__file__).resolve().parents[1]
zips = Path.home() / "Downloads"
mesh_dir = root / "art" / "drawers" / "mesh"
runtime_dir = root / "art" / "drawers" / "runtime"
ids = ("wooden", "personal", "reinforced", "blackmetal")
size = 1024
# Wood smoothness from the reinforced map sits near here. Unused UV space is 0,0
# in both pictures, which would otherwise become a mirror.
empty_smooth = 75


def load_gray(zip_file: zipfile.ZipFile, suffix: str) -> Image.Image:
    name = next(n for n in zip_file.namelist() if n.endswith(suffix))
    image = Image.open(io.BytesIO(zip_file.read(name))).convert("L")
    return image.resize((size, size), Image.Resampling.BOX)


def pack(metal: Image.Image, rough: Image.Image) -> Image.Image:
    mr = metal.tobytes()
    rr = rough.tobytes()
    out = bytearray(size * size * 4)
    for i, (m, r) in enumerate(zip(mr, rr)):
        o = i * 4
        if m < 8 and r < 8:
            out[o + 3] = empty_smooth
            continue
        out[o] = m
        out[o + 3] = 255 - r
    return Image.frombytes("RGBA", (size, size), bytes(out))


def main() -> None:
    mesh_dir.mkdir(parents=True, exist_ok=True)
    runtime_dir.mkdir(parents=True, exist_ok=True)
    for drawer_id in ids:
        zip_path = zips / f"{drawer_id}.zip"
        if not zip_path.exists():
            raise SystemExit(f"missing {zip_path}")
        with zipfile.ZipFile(zip_path) as zip_file:
            image = pack(load_gray(zip_file, "_metallic.png"), load_gray(zip_file, "_roughness.png"))
        for folder in (mesh_dir, runtime_dir):
            dest = folder / f"{drawer_id}.metal.png"
            image.save(dest, format="PNG", optimize=True)
            print(f"{drawer_id}\t{dest.relative_to(root)}\t{dest.stat().st_size}")


if __name__ == "__main__":
    main()
