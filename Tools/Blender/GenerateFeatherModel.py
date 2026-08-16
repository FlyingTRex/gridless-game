# Generates a replacement Feather model (2026-08-16) -- the original
# imported Feather.glb turned out to have a genuinely degenerate mesh (2
# of its 4 quad vertices coincide at the origin), leaving only one real
# triangle -- a thin spike, not a recognizable feather shape, confirmed
# via a direct render. Run headless:
#   blender --background --python Tools/Blender/GenerateFeatherModel.py
#
# Output: Tools/Blender/Output/Feather.glb (+ preview)
#
# Player reference: 1 world unit = 1 meter. A real feather (e.g. a
# chicken covert feather) is roughly 8-12cm long, ~2-3cm wide at its
# widest -- matches the original (broken) model's own measured bounds
# (0.02m wide, 0.12m long), so the same target size is kept.

import bpy
import math
import os
import mathutils
import bmesh

OUTPUT_DIR = os.path.join(os.path.dirname(bpy.data.filepath) or
                           r"d:\Ben\Downloads\gridless-game.tar\gridless-game\Tools\Blender",
                           "Output")
os.makedirs(OUTPUT_DIR, exist_ok=True)

VANE_COLOR = (0.88, 0.85, 0.78, 1.0)
QUILL_COLOR = (0.93, 0.90, 0.83, 1.0)


def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in list(bpy.data.meshes):
        if block.users == 0:
            bpy.data.meshes.remove(block)
    for block in list(bpy.data.materials):
        if block.users == 0:
            bpy.data.materials.remove(block)


def new_material(name, color, roughness=0.75):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.roughness = roughness
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = roughness
    return mat


def world_bounds_min_z(obj):
    return min((obj.matrix_world @ mathutils.Vector(c)).z for c in obj.bound_box)


def ground_and_apply(objs):
    min_z = min(world_bounds_min_z(o) for o in objs)
    offset = -min_z
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.location.z += offset
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)


def export_and_render(name, objs):
    glb_path = os.path.join(OUTPUT_DIR, f"{name}.glb")
    preview_path = os.path.join(OUTPUT_DIR, f"{name}_preview.png")

    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.ops.export_scene.gltf(filepath=glb_path, export_format='GLB', use_selection=True)
    print(f"Exported {glb_path}")

    for obj in list(bpy.data.objects):
        if obj.type in ('CAMERA', 'LIGHT'):
            bpy.data.objects.remove(obj, do_unlink=True)

    bbox_max = max(max(abs(c) for c in o.bound_box[6]) for o in objs) + 0.02
    cam_dist = max(bbox_max * 3.5, 0.15)
    bpy.ops.object.camera_add(location=(cam_dist * 0.3, -cam_dist, cam_dist * 0.5),
                               rotation=(math.radians(68), 0, math.radians(15)))
    cam = bpy.context.active_object
    bpy.context.scene.camera = cam

    bpy.ops.object.light_add(type='SUN', location=(cam_dist, -cam_dist, cam_dist * 1.5))
    sun = bpy.context.active_object
    sun.data.energy = 5.0
    sun.rotation_euler = (math.radians(45), 0, math.radians(45))

    bpy.ops.object.light_add(type='SUN', location=(-cam_dist, cam_dist, cam_dist))
    fill = bpy.context.active_object
    fill.data.energy = 2.0
    fill.rotation_euler = (math.radians(-30), 0, math.radians(-135))

    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = 512
    scene.render.resolution_y = 512
    scene.render.filepath = preview_path
    bpy.ops.render.render(write_still=True)
    print(f"Rendered preview {preview_path}")


def build_feather():
    clear_scene()

    vane_mat = new_material("Feather_Vane_mat", VANE_COLOR, roughness=0.75)
    quill_mat = new_material("Feather_Quill_mat", QUILL_COLOR, roughness=0.5)

    # Vane: a flat elongated leaf shape (plane subdivided along its
    # length, pinched to a point at both ends) -- classic feather
    # silhouette, not a single degenerate quad.
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=(0, 0, 0))
    vane = bpy.context.active_object
    vane.name = "Vane"
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.subdivide(number_cuts=8)
    bpy.ops.object.mode_set(mode='OBJECT')

    bm = bmesh.new()
    bm.from_mesh(vane.data)
    bm.verts.ensure_lookup_table()
    for v in bm.verts:
        # v.co.y runs -0.5..0.5 along the feather's length before scaling.
        t = v.co.y + 0.5  # 0 at base, 1 at tip
        if t < 0.15:
            pinch = t / 0.15
        elif t > 0.85:
            pinch = (1.0 - t) / 0.15
        else:
            pinch = 1.0
        pinch = max(0.03, pinch)
        v.co.x *= pinch
        # Slight natural curve along the length.
        v.co.z += math.sin(t * math.pi) * 0.04
    bm.to_mesh(vane.data)
    bm.free()

    vane.scale = (0.022, 0.12, 0.12)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    vane.data.materials.append(vane_mat)

    # Quill: a very thin cylinder down the vane's centerline, slightly
    # raised so it doesn't z-fight with the vane plane.
    bpy.ops.mesh.primitive_cylinder_add(radius=0.0015, depth=0.115, location=(0, 0.058, 0.001))
    quill = bpy.context.active_object
    quill.name = "Quill"
    quill.rotation_euler = (math.radians(90), 0, 0)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    quill.data.materials.append(quill_mat)

    objs = [vane, quill]
    ground_and_apply(objs)
    export_and_render("Feather", objs)


build_feather()
print("Feather model generated.")
