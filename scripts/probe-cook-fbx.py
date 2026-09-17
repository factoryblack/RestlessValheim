import bpy
from mathutils import Vector
from pathlib import Path

root = Path(r"C:\Users\jules\source\repos\RestlessQoL\art\cook\mesh")
print("id\tverts\tfaces\tw\td\th")
for src in sorted(root.glob("*.fbx")):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(src))
    verts = 0
    faces = 0
    mins = Vector((1e9, 1e9, 1e9))
    maxs = Vector((-1e9, -1e9, -1e9))
    for obj in bpy.data.objects:
        if obj.type != "MESH":
            continue
        mesh = obj.data
        verts += len(mesh.vertices)
        faces += len(mesh.polygons)
        for v in mesh.vertices:
            w = obj.matrix_world @ v.co
            mins.x = min(mins.x, w.x)
            mins.y = min(mins.y, w.y)
            mins.z = min(mins.z, w.z)
            maxs.x = max(maxs.x, w.x)
            maxs.y = max(maxs.y, w.y)
            maxs.z = max(maxs.z, w.z)
    size = maxs - mins
    print(f"{src.stem}\t{verts}\t{faces}\t{size.x:.3f}\t{size.y:.3f}\t{size.z:.3f}")
