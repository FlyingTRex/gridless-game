# Generates a Chicken Meat model — third Chicken loot table drop
# (2026-08-16, alongside the existing Feather/Egg). Run headless:
#   blender --background --python Tools/Blender/GenerateChickenMeatModel.py
#
# Output: Tools/Blender/Output/ChickenMeat.glb (+ preview)
#
# Player reference: 1 world unit = 1 meter. A raw chicken drumstick is
# roughly 10-12cm long overall, ~4cm meat-mass diameter -- a classic
# drumstick silhouette (bone protruding from a rounded meat mass) reads as
# distinctly "chicken" rather than reusing Raw Meat's own beef-steak-slab
# look.

import bpy
import math
import os
import mathutils
import bmesh

OUTPUT_DIR = os.path.join(os.path.dirname(bpy.data.filepath) or
                           r"d:\Ben\Downloads\gridless-game.tar\gridless-game\Tools\Blender",
                           "Output")
os.makedirs(OUTPUT_DIR, exist_ok=True)

MEAT_COLOR = (0.72, 0.30, 0.28, 1.0)
BONE_COLOR = (0.92, 0.88, 0.78, 1.0)


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


def build_chicken_meat():
    clear_scene()

    meat_mat = new_material("ChickenMeat_mat", MEAT_COLOR, roughness=0.6)
    bone_mat = new_material("ChickenBone_mat", BONE_COLOR, roughness=0.4)

    # Meat mass: a sphere stretched into a rounded teardrop, laid on its
    # side (drumstick's bulb end).
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.02, segments=16, ring_count=12, location=(0.025, 0, 0))
    meat = bpy.context.active_object
    meat.name = "ChickenMeat"

    bm = bmesh.new()
    bm.from_mesh(meat.data)
    bm.verts.ensure_lookup_table()
    # Pinch the end facing away from the bone (positive X) narrower, same
    # "scale vertices past a threshold inward" taper Egg's own build
    # already uses, just along X instead of Z.
    for v in bm.verts:
        if v.co.x > 0:
            t = v.co.x / 0.02
            pinch = 1.0 - 0.35 * t
            v.co.y *= pinch
            v.co.z *= pinch
    bm.to_mesh(meat.data)
    bm.free()

    meat.scale = (1.4, 1.0, 1.0)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    meat.data.materials.append(meat_mat)

    # Bone: a thin cylinder protruding from the narrow end, with a small
    # knuckle sphere at its tip (classic drumstick silhouette).
    bpy.ops.mesh.primitive_cylinder_add(radius=0.006, depth=0.045, location=(0.06, 0, 0))
    bone = bpy.context.active_object
    bone.name = "Bone"
    bone.rotation_euler = (0, math.radians(90), 0)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    bone.data.materials.append(bone_mat)

    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.009, segments=10, ring_count=8, location=(0.082, 0, 0))
    knuckle = bpy.context.active_object
    knuckle.name = "Knuckle"
    knuckle.data.materials.append(bone_mat)

    objs = [meat, bone, knuckle]
    ground_and_apply(objs)
    export_and_render("ChickenMeat", objs)


build_chicken_meat()
print("Chicken Meat model generated.")
