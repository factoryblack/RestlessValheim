"""Pull the Storekeeper's Table zip into art/storage/mesh.

Albedo becomes table.png at 1024. Metal (red) and flipped roughness (alpha)
become table.metal.png, same layout the chest shader already reads.
"""
import io
import sys
import zipfile
from pathlib import Path

from PIL import Image

root = Path(__file__).resolve().parents[1]
mesh_dir = root / "art" / "storage" / "mesh"
size = 1024
empty_smooth = 75


def named(zip_file: zipfile.ZipFile, suffix: str, blocked: tuple[str, ...]) -> str:
    names = [
        n for n in zip_file.namelist()
        if n.endswith(suffix) and not n.endswith("/")
        and not any(word in Path(n).name for word in blocked)
    ]
    if len(names) != 1:
        raise SystemExit(f"{suffix} excluding {blocked}: expected 1 file, found {names}")
    return names[0]


def load_gray(zip_file: zipfile.ZipFile, suffix: str, blocked: tuple[str, ...]) -> Image.Image:
    image = Image.open(io.BytesIO(zip_file.read(named(zip_file, suffix, blocked)))).convert("L")
    return image.resize((size, size), Image.Resampling.BOX)


def pack(metal: Image.Image, rough: Image.Image) -> Image.Image:
    out = bytearray(size * size * 4)
    for i, (m, r) in enumerate(zip(metal.tobytes(), rough.tobytes())):
        o = i * 4
        if m < 8 and r < 8:
            out[o + 3] = empty_smooth
            continue
        out[o] = m
        out[o + 3] = 255 - r
    return Image.frombytes("RGBA", (size, size), bytes(out))


def main() -> None:
    zip_path = Path(sys.argv[1]) if len(sys.argv) > 1 else Path.home() / "Downloads" / "modelx.zip"
    if not zip_path.exists():
        raise SystemExit(f"missing {zip_path}")
    mesh_dir.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(zip_path) as zip_file:
        fbx_name = named(zip_file, ".fbx", ())
        albedo_name = named(zip_file, ".png", ("metallic", "roughness", "normal", "emissive"))
        (mesh_dir / "table.fbx").write_bytes(zip_file.read(fbx_name))
        albedo = Image.open(io.BytesIO(zip_file.read(albedo_name))).convert("RGBA")
        albedo = albedo.resize((size, size), Image.Resampling.LANCZOS)
        albedo.save(mesh_dir / "table.png", format="PNG", optimize=True)
        surface = pack(
            load_gray(zip_file, "_metallic.png", ("roughness",)),
            load_gray(zip_file, "_roughness.png", ("metallic",)),
        )
        surface.save(mesh_dir / "table.metal.png", format="PNG", optimize=True)
    fbx = mesh_dir / "table.fbx"
    print(f"fbx\t{fbx.stat().st_size}")
    print(f"albedo\t{(mesh_dir / 'table.png').stat().st_size}")
    print(f"metal\t{(mesh_dir / 'table.metal.png').stat().st_size}")


if __name__ == "__main__":
    main()
