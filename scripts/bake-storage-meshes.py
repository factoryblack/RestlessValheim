"""Import the Storekeeper FBX and write a Unity-Y-up mesh plus a hammer icon."""
import shutil
import struct
from pathlib import Path

import bpy
from mathutils import Vector

root = Path(__file__).resolve().parents[1]
src_dir = root / "art" / "storage" / "mesh"
out_dir = root / "art" / "storage" / "runtime"
icon_game = root / "src" / "RestlessStorage" / "Assets" / "table.png"
icon_store = root / "thunderstore" / "storage" / "icon.png"


def wipe():
    if bpy.data.objects:
        bpy.ops.object.select_all(action="SELECT")
        bpy.ops.object.delete(use_global=False)
    for coll in (bpy.data.meshes, bpy.data.materials, bpy.data.images, bpy.data.armatures, bpy.data.objects):
        for block in list(coll):
            coll.remove(block)


def unity(v):
    return Vector((v.x, v.z, v.y))


def bake_mesh(fbx: Path):
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

    out_dir.mkdir(parents=True, exist_ok=True)
    dest = out_dir / "table.rcm"
    with dest.open("wb") as fh:
        fh.write(b"RCM2")
        fh.write(struct.pack("<ii", len(verts), len(indices)))
        for v in verts:
            fh.write(struct.pack("<8f", *v))
        fh.write(struct.pack(f"<{len(indices)}i", *indices))

    xs = [v[0] for v in verts]
    ys = [v[1] for v in verts]
    zs = [v[2] for v in verts]
    print(
        f"table\t{len(verts)}\t{len(indices) // 3}\t"
        f"{max(xs) - min(xs):.3f}x{max(zs) - min(zs):.3f}x{max(ys) - min(ys):.3f}\t{dest.stat().st_size}"
    )


def render_icon():
    scene = bpy.context.scene
    for engine in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE"):
        try:
            scene.render.engine = engine
            break
        except TypeError:
            continue
    scene.render.resolution_x = 256
    scene.render.resolution_y = 256
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"

    albedo_path = src_dir / "table.png"
    image = bpy.data.images.load(str(albedo_path)) if albedo_path.exists() else None
    mat = bpy.data.materials.new("table")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if image is not None and bsdf is not None:
        tex = nodes.new("ShaderNodeTexImage")
        tex.image = image
        links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    for obj in bpy.data.objects:
        if obj.type != "MESH":
            continue
        obj.data.materials.clear()
        obj.data.materials.append(mat)

    mins = Vector((1e9, 1e9, 1e9))
    maxs = Vector((-1e9, -1e9, -1e9))
    for obj in bpy.data.objects:
        if obj.type != "MESH":
            continue
        for corner in obj.bound_box:
            world = obj.matrix_world @ Vector(corner)
            mins = Vector((min(mins.x, world.x), min(mins.y, world.y), min(mins.z, world.z)))
            maxs = Vector((max(maxs.x, world.x), max(maxs.y, world.y), max(maxs.z, world.z)))
    center = (mins + maxs) * 0.5
    span = max((maxs - mins).x, (maxs - mins).y, (maxs - mins).z)
    dist = max(span, 0.2) * 2.2

    cam_data = bpy.data.cameras.new("icon")
    cam_data.lens = 50
    cam = bpy.data.objects.new("icon", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam
    direction = Vector((1.0, -1.15, 0.72)).normalized()
    cam.location = center + direction * dist
    cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()

    sun_data = bpy.data.lights.new("key", "SUN")
    sun_data.energy = 4.5
    sun = bpy.data.objects.new("key", sun_data)
    scene.collection.objects.link(sun)
    sun.rotation_euler = (0.85, 0.15, 0.6)

    icon_game.parent.mkdir(parents=True, exist_ok=True)
    scene.render.filepath = str(icon_game)
    bpy.ops.render.render(write_still=True)
    icon_store.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(icon_game, icon_store)
    print(f"icon\t{icon_game.stat().st_size}")


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    fbx = src_dir / "table.fbx"
    if not fbx.exists():
        raise SystemExit(f"missing {fbx}")
    wipe()
    print("id\tloops\ttris\tsize_xz_y\tbytes")
    bake_mesh(fbx)
    for name in ("table.png", "table.metal.png"):
        src = src_dir / name
        if src.exists():
            shutil.copy2(src, out_dir / name)
    render_icon()
    print(f"baked -> {out_dir}")


if __name__ == "__main__":
    main()
