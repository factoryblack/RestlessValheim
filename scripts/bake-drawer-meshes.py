# Import Meshy FBX and write Unity-Y-up binary meshes (.rcm) for RestlessDrawers.
import shutil
import struct
from pathlib import Path

import bpy
from mathutils import Vector

root = Path(__file__).resolve().parents[1]
src_dir = root / "art" / "drawers" / "mesh"
out_dir = root / "art" / "drawers" / "runtime"
out_dir.mkdir(parents=True, exist_ok=True)


def wipe():
    if bpy.data.objects:
        bpy.ops.object.select_all(action="SELECT")
        bpy.ops.object.delete(use_global=False)
    for coll in (bpy.data.meshes, bpy.data.materials, bpy.data.images, bpy.data.armatures, bpy.data.objects):
        for block in list(coll):
            coll.remove(block)


def unity(v):
    return Vector((v.x, v.z, v.y))


def bake_one(fbx: Path):
    wipe()
    bpy.ops.import_scene.fbx(filepath=str(fbx))
    verts = []
    indices = []
    for obj in list(bpy.data.objects):
        if obj.type != "MESH":
            continue
        mesh = obj.data
        mesh.calc_loop_triangles()
        uv_layer = mesh.uv_layers.active
        mw = obj.matrix_world
        nmat = mw.to_3x3()
        for tri in mesh.loop_triangles:
            tri_idx = []
            for loop_i in tri.loops:
                loop = mesh.loops[loop_i]
                pos = unity(mw @ mesh.vertices[loop.vertex_index].co)
                nrm = unity(nmat @ mesh.vertices[loop.vertex_index].normal).normalized()
                if nrm.length == 0:
                    nrm = Vector((0.0, 1.0, 0.0))
                uv = uv_layer.data[loop_i].uv if uv_layer else Vector((0.0, 0.0))
                verts.append((pos.x, pos.y, pos.z, nrm.x, nrm.y, nrm.z, uv.x, uv.y))
                tri_idx.append(len(verts) - 1)
            indices.extend((tri_idx[0], tri_idx[2], tri_idx[1]))

    if not verts or not indices:
        raise RuntimeError(f"{fbx.stem}: empty mesh")

    min_y = min(v[1] for v in verts)
    if abs(min_y) > 1e-5:
        verts = [(v[0], v[1] - min_y, v[2], v[3], v[4], v[5], v[6], v[7]) for v in verts]

    dest = out_dir / f"{fbx.stem}.rcm"
    with dest.open("wb") as fh:
        fh.write(b"RCM2")
        fh.write(struct.pack("<ii", len(verts), len(indices)))
        for v in verts:
            fh.write(struct.pack("<8f", *v))
        fh.write(struct.pack(f"<{len(indices)}i", *indices))

    png = src_dir / f"{fbx.stem}.png"
    if png.exists():
        shutil.copy2(png, out_dir / f"{fbx.stem}.png")

    xs = [v[0] for v in verts]
    ys = [v[1] for v in verts]
    zs = [v[2] for v in verts]
    print(
        f"{fbx.stem}\t{len(verts)}\t{len(indices)//3}\t"
        f"{max(xs)-min(xs):.3f}x{max(zs)-min(zs):.3f}x{max(ys)-min(ys):.3f}\t{dest.stat().st_size}"
    )


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    files = sorted(src_dir.glob("*.fbx"))
    if not files:
        raise SystemExit(f"no fbx in {src_dir}")
    print("id\tloops\ttris\tsize_xz_y\tbytes")
    for fbx in files:
        bake_one(fbx)
    print(f"baked {len(files)} -> {out_dir}")


if __name__ == "__main__":
    main()
