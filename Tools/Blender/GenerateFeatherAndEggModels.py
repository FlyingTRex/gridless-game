# Generates Feather and Egg models — Chicken loot table additions
# (2026-08-15, "we get crafting materials"). Run headless:
#   blender --background --python Tools/Blender/GenerateFeatherAndEggModels.py
#
# Output: Tools/Blender/Output/{Feather,Egg}.glb (+ previews)
#
# Player reference: 1 world unit = 1 meter. A real chicken feather is
# roughly 10-12cm long; a chicken egg roughly 5.5cm tall.

import bpy
import math
import os
import mathutils

OUTPUT_DIR = os.path.join(os.path.dirname(bpy.data.filepath) or
                           r"d:\Ben\Downloads\gridless-game.tar\gridless-game\Tools\Blender",
                           "Output")
os.makedirs(OUTPUT_DIR, exist_ok=True)

FEATHER_COLOR = (0.88, 0.85, 0.78, 1.0)
FEATHER_QUILL_COLOR = (0.75, 0.70, 0.60, 1.0)
EGG_SHELL_COLOR = (0.93, 0.87, 0.74, 1.0)


def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in list(bpy.data.meshes):
        if block.users == 0:
            bpy.data.meshes.remove(block)
    for block in list(bpy.data.materials):
        if block.users == 0:
            bpy.data.materials.remove(block)


def new_material(name, color, roughness=0.85):
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
    bpy.ops.object.camera_add(location=(cam_dist, -cam_dist, cam_dist * 0.7),
                               rotation=(math.radians(62), 0, math.radians(45)))
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

    vane_mat = new_material("FeatherVane_mat", FEATHER_COLOR, roughness=0.9)

    length = 0.12
    width = 0.03

    # One tapered blade — wide near the top, pulled to a fine point at
    # the base (real feather silhouette) — simpler and more reliable
    # than a separate vane+quill assembly, which fought alignment bugs
    # for very little visual payoff on an item this small.
    bpy.ops.mesh.primitive_plane_add(size=1, location=(0, 0, 0))
    feather = bpy.context.active_object
    feather.name = "Feather"
    feather.scale = (width, length, 1)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    import bmesh
    bm = bmesh.new()
    bm.from_mesh(feather.data)
    bm.verts.ensure_lookup_table()
    # Pull the two bottom verts (lowest Y = base of the feather) in
    # toward the centerline to form a point; nudge the top two inward
    # slightly too for a leaf-like taper instead of a flat rectangle top.
    verts_by_y = sorted(bm.verts, key=lambda v: v.co.y)
    for v in verts_by_y[:2]:
        v.co.x = 0
    for v in verts_by_y[2:]:
        v.co.x *= 0.6
    bm.to_mesh(feather.data)
    bm.free()

    feather.rotation_euler = (math.radians(90), 0, 0)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    feather.data.materials.append(vane_mat)

    objs = [feather]
    ground_and_apply(objs)
    export_and_render("Feather", objs)


def build_egg():
    clear_scene()

    shell_mat = new_material("EggShell_mat", EGG_SHELL_COLOR, roughness=0.4)

    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.028, segments=16, ring_count=12, location=(0, 0, 0))
    egg = bpy.context.active_object
    egg.name = "Egg"

    # Classic egg silhouette: uniform sphere, then pinch the top narrower
    # than the bottom by scaling vertices above the equator inward.
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(egg.data)
    bm.verts.ensure_lookup_table()
    for v in bm.verts:
        if v.co.z > 0:
            t = v.co.z / 0.028
            pinch = 1.0 - 0.28 * t
            v.co.x *= pinch
            v.co.y *= pinch
    bm.to_mesh(egg.data)
    bm.free()

    egg.scale = (1.0, 1.0, 1.35)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    egg.data.materials.append(shell_mat)

    objs = [egg]
    ground_and_apply(objs)
    export_and_render("Egg", objs)


build_feather()
build_egg()
print("Feather & Egg models generated.")
