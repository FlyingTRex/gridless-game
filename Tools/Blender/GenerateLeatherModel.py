# Generates the Leather model (2026-08-15) — the first real source of
# Leather in the game (BUGS_AND_ENHANCEMENTS.md's long-open "where Leather
# comes from" question, answered by hunting: Deer drops it). A folded hide
# swatch, distinct from Cloth (which is a plain flat weave) — leather has
# real thickness and a slightly draped/uneven fold. Run headless:
#   blender --background --python Tools/Blender/GenerateLeatherModel.py
#
# Output: Tools/Blender/Output/Leather.glb (+ preview)
#
# Player reference: 1 world unit = 1 meter. A carryable folded hide swatch
# is roughly 20-25cm across, a few cm thick when folded.

import bpy
import math
import os
import mathutils
import bmesh

OUTPUT_DIR = os.path.join(os.path.dirname(bpy.data.filepath) or
                           r"d:\Ben\Downloads\gridless-game.tar\gridless-game\Tools\Blender",
                           "Output")
os.makedirs(OUTPUT_DIR, exist_ok=True)

LEATHER_COLOR = (0.45, 0.28, 0.16, 1.0)


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
    cam_dist = max(bbox_max * 3.5, 0.3)
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


def build():
    clear_scene()
    mat = new_material("Leather_mat", LEATHER_COLOR, roughness=0.8)

    width, depth = 0.22, 0.18
    bpy.ops.mesh.primitive_plane_add(size=1, location=(0, 0, 0))
    hide = bpy.context.active_object
    hide.name = "Leather"
    hide.scale = (width, depth, 1)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    # Subdivide so the fold/drape deformation below has geometry to bend.
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.subdivide(number_cuts=6)
    bpy.ops.object.mode_set(mode='OBJECT')

    # Give it real thickness (a flat plane reads as paper-thin, not hide)
    # and a slight uneven drape/fold via a Solidify + a bit of vertex noise.
    solid = hide.modifiers.new("Thickness", 'SOLIDIFY')
    solid.thickness = 0.015
    bpy.ops.object.modifier_apply(modifier="Thickness")

    bm = bmesh.new()
    bm.from_mesh(hide.data)
    bm.verts.ensure_lookup_table()
    for v in bm.verts:
        # A gentle fold: bend upward toward the edges (draped-over-itself
        # look) plus small per-vertex jitter so it doesn't read as a
        # perfectly flat mat.
        foldheight = (abs(v.co.x) / width) ** 2 * 0.02
        jitter = (hash((round(v.co.x, 4), round(v.co.y, 4))) % 100) / 100 * 0.004
        v.co.z += foldheight + jitter
    bm.to_mesh(hide.data)
    bm.free()

    hide.data.materials.append(mat)

    objs = [hide]
    ground_and_apply(objs)
    export_and_render("Leather", objs)


build()
print("Leather model generated.")
