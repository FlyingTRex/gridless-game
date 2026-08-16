# Generates the Steak and Potatoes model (2026-08-15) — a merged
# pan+steak+potato model replacing the Cooked-Meat-reused placeholder,
# per Ben's ask ("can we merge the meat, potato and frying pan into a
# single model?"). Run headless:
#   blender --background --python Tools/Blender/GenerateSteakAndPotatoesModel.py
#
# Output: Tools/Blender/Output/SteakAndPotatoes.glb (+ preview)
#
# Reuses GenerateCookwareModels.py's Frying Pan geometry/material as the
# base, adds a browned steak slab and a potato sitting in the pan. Player
# reference: 1 world unit = 1 meter — same ~0.1m pan radius as the
# standalone Frying Pan pickup.

import bpy
import math
import os
import mathutils

OUTPUT_DIR = os.path.join(os.path.dirname(bpy.data.filepath) or
                           r"d:\Ben\Downloads\gridless-game.tar\gridless-game\Tools\Blender",
                           "Output")
os.makedirs(OUTPUT_DIR, exist_ok=True)

CAST_IRON_COLOR = (0.05, 0.05, 0.055, 1.0)
STEAK_COLOR = (0.32, 0.14, 0.10, 1.0)
STEAK_SEAR_COLOR = (0.16, 0.06, 0.05, 1.0)
POTATO_COLOR = (0.78, 0.58, 0.32, 1.0)


def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in list(bpy.data.meshes):
        if block.users == 0:
            bpy.data.meshes.remove(block)
    for block in list(bpy.data.materials):
        if block.users == 0:
            bpy.data.materials.remove(block)


def new_material(name, color, roughness=0.5, metallic=0.0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.roughness = roughness
    mat.metallic = metallic
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Metallic"].default_value = metallic
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
    bpy.ops.object.camera_add(location=(cam_dist, -cam_dist, cam_dist * 0.9),
                               rotation=(math.radians(55), 0, math.radians(45)))
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

    pan_mat = new_material("Pan_mat", CAST_IRON_COLOR, roughness=0.5, metallic=0.85)
    steak_mat = new_material("Steak_mat", STEAK_COLOR, roughness=0.6)
    sear_mat = new_material("Sear_mat", STEAK_SEAR_COLOR, roughness=0.7)
    potato_mat = new_material("Potato_mat", POTATO_COLOR, roughness=0.75)

    objs = []

    # Pan body (same proportions as the standalone Frying Pan).
    radius, height = 0.1, 0.025
    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=height, location=(0, 0, height / 2), vertices=24)
    body = bpy.context.active_object
    body.name = "PanBody"
    body.data.materials.append(pan_mat)
    objs.append(body)

    bpy.ops.mesh.primitive_torus_add(major_radius=radius, minor_radius=0.006,
                                      major_segments=24, minor_segments=8,
                                      location=(0, 0, height - 0.004))
    rim = bpy.context.active_object
    rim.name = "PanRim"
    rim.data.materials.append(pan_mat)
    objs.append(rim)

    handle_len = 0.16
    bpy.ops.mesh.primitive_cylinder_add(radius=0.012, depth=handle_len,
                                         location=(radius + handle_len / 2, 0, height / 2))
    handle = bpy.context.active_object
    handle.name = "PanHandle"
    handle.rotation_euler = (0, math.radians(90), 0)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    handle.data.materials.append(pan_mat)
    objs.append(handle)

    # Steak: a flattened, rounded slab sitting in the pan, off-center to
    # leave room for the potato beside it.
    steak_z = height + 0.008
    bpy.ops.mesh.primitive_cube_add(size=1, location=(-0.025, 0.01, steak_z))
    steak = bpy.context.active_object
    steak.name = "Steak"
    steak.scale = (0.05, 0.038, 0.014)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(steak.data)
    bmesh.ops.bevel(bm, geom=bm.edges[:], offset=0.006, segments=2)
    bm.to_mesh(steak.data)
    bm.free()
    steak.data.materials.append(steak_mat)
    objs.append(steak)

    # Sear marks: two thin dark bars across the steak's top face.
    for i, off in enumerate((-0.012, 0.012)):
        bpy.ops.mesh.primitive_cube_add(size=1, location=(-0.025 + off, 0.01, steak_z + 0.011))
        sear = bpy.context.active_object
        sear.name = f"SearMark{i}"
        sear.rotation_euler = (0, 0, math.radians(30))
        sear.scale = (0.004, 0.032, 0.002)
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
        sear.data.materials.append(sear_mat)
        objs.append(sear)

    # Potato: a small pinched ellipsoid beside the steak.
    potato_z = height + 0.016
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.02, segments=14, ring_count=10,
                                          location=(0.04, -0.015, potato_z))
    potato = bpy.context.active_object
    potato.name = "Potato"
    potato.scale = (1.0, 0.85, 0.8)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    potato.data.materials.append(potato_mat)
    objs.append(potato)

    ground_and_apply(objs)
    export_and_render("SteakAndPotatoes", objs)


build()
print("Steak and Potatoes model generated.")
