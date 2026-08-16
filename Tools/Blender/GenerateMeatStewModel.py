# Generates the Meat Stew model (2026-08-15) — a copy of the Cooking Pot
# geometry filled with broth and visible meat/potato/carrot chunks, same
# "merged accessory + food" convention as Steak and Potatoes (pan) and
# Herbal Tea (kettle). Run headless:
#   blender --background --python Tools/Blender/GenerateMeatStewModel.py
#
# Output: Tools/Blender/Output/MeatStew.glb (+ preview)
#
# Cooking Pot geometry duplicated from GenerateCookwareModels.py (that
# script builds the standalone Cooking Pot pickup and isn't touched here),
# same reasoning as GenerateHerbalTeaModel.py's own duplication note —
# Blender has no clean "import one prefab's mesh data only" path, so each
# merged-model generator stays self-contained. All new pieces (broth,
# chunks) sit on top of the pot, inside the rim — no leaning-against-the-
# side placement this time, so the Blender-Y/Unity-Z occlusion gotcha
# GenerateHerbalTeaModel.py hit doesn't apply here.

import bpy
import math
import os
import mathutils

OUTPUT_DIR = os.path.join(os.path.dirname(bpy.data.filepath) or
                           r"d:\Ben\Downloads\gridless-game.tar\gridless-game\Tools\Blender",
                           "Output")
os.makedirs(OUTPUT_DIR, exist_ok=True)

CAST_IRON_COLOR = (0.05, 0.05, 0.055, 1.0)
BROTH_COLOR = (0.32, 0.18, 0.08, 1.0)
MEAT_COLOR = (0.35, 0.16, 0.12, 1.0)
POTATO_COLOR = (0.80, 0.66, 0.42, 1.0)
CARROT_COLOR = (0.85, 0.45, 0.12, 1.0)


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
                               rotation=(math.radians(58), 0, math.radians(45)))
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


def add_loop_handle(name, mat, radius_major, radius_minor, location, rotation):
    bpy.ops.mesh.primitive_torus_add(major_radius=radius_major, minor_radius=radius_minor,
                                      major_segments=16, minor_segments=8, location=location)
    handle = bpy.context.active_object
    handle.name = name
    handle.rotation_euler = rotation
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    handle.data.materials.append(mat)
    return handle


def build_pot_geometry(pot_mat):
    radius, height = 0.11, 0.16
    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=height, location=(0, 0, height / 2), vertices=24)
    body = bpy.context.active_object
    body.name = "PotBody"
    body.data.materials.append(pot_mat)
    objs = [body]

    bpy.ops.mesh.primitive_torus_add(major_radius=radius, minor_radius=0.007,
                                      major_segments=24, minor_segments=8,
                                      location=(0, 0, height - 0.01))
    rim = bpy.context.active_object
    rim.name = "PotRim"
    rim.data.materials.append(pot_mat)
    objs.append(rim)

    handle_z = height * 0.7
    objs.append(add_loop_handle("PotHandleL", pot_mat, 0.025, 0.006,
                                 (radius + 0.012, 0, handle_z), (0, math.radians(90), 0)))
    objs.append(add_loop_handle("PotHandleR", pot_mat, 0.025, 0.006,
                                 (-radius - 0.012, 0, handle_z), (0, math.radians(90), 0)))

    return objs, radius, height


def add_chunk(name, mat, location, size, rotation_z=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    chunk = bpy.context.active_object
    chunk.name = name
    chunk.rotation_euler = (0, 0, math.radians(rotation_z))
    chunk.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(chunk.data)
    bmesh.ops.bevel(bm, geom=bm.edges[:], offset=min(size) * 0.25, segments=2)
    bm.to_mesh(chunk.data)
    bm.free()
    chunk.data.materials.append(mat)
    return chunk


def build():
    clear_scene()

    pot_mat = new_material("StewPot_mat", CAST_IRON_COLOR, roughness=0.5, metallic=0.85)
    broth_mat = new_material("StewBroth_mat", BROTH_COLOR, roughness=0.25)
    meat_mat = new_material("StewMeat_mat", MEAT_COLOR, roughness=0.6)
    potato_mat = new_material("StewPotato_mat", POTATO_COLOR, roughness=0.7)
    carrot_mat = new_material("StewCarrot_mat", CARROT_COLOR, roughness=0.6)

    pot_objs, radius, height = build_pot_geometry(pot_mat)

    # Broth: a flat disc filling the pot just below the rim.
    broth_z = height - 0.018
    bpy.ops.mesh.primitive_cylinder_add(radius=radius * 0.92, depth=0.006, location=(0, 0, broth_z), vertices=24)
    broth = bpy.context.active_object
    broth.name = "StewBroth"
    broth.data.materials.append(broth_mat)
    objs = pot_objs + [broth]

    # Chunks poking up out of the broth, clustered near center (visible
    # from any horizontal camera angle since they sit on TOP of the pot).
    chunk_z = broth_z + 0.014
    objs.append(add_chunk("StewMeatChunk", meat_mat, (-0.025, 0.015, chunk_z), (0.022, 0.02, 0.016), 20))
    objs.append(add_chunk("StewPotatoChunk", potato_mat, (0.02, -0.02, chunk_z), (0.018, 0.018, 0.016), -15))
    objs.append(add_chunk("StewCarrotChunk1", carrot_mat, (0.01, 0.025, chunk_z), (0.014, 0.014, 0.014), 40))
    objs.append(add_chunk("StewCarrotChunk2", carrot_mat, (-0.015, -0.022, chunk_z), (0.013, 0.013, 0.013), -50))

    ground_and_apply(objs)
    export_and_render("MeatStew", objs)


build()
print("Meat Stew model generated.")
