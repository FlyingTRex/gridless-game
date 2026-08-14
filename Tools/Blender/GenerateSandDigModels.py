# Generates the three simple props the Sand dig-site mechanic needs
# (BUGS_AND_ENHANCEMENTS.md's digging/water-scarcity section, "Dig sites,
# not free-form digging"). Run headless:
#   blender --background --python Tools/Blender/GenerateSandDigModels.py
#
# Outputs (one glb + one preview png per model):
#   Tools/Blender/Output/SandPatch.glb   — the dig site's standing visual
#   Tools/Blender/Output/SandPickup.glb  — the small clump that scatters
#                                           on break, becomes the Sand item
#   Tools/Blender/Output/DigHole.glb     — shown after breaking, hidden
#                                           again on respawn (ResourceNode
#                                           .holeVisualPrefab)
#
# Player reference: CharacterController height 1.8m, 1 world unit = 1
# meter. All three are flat ground-level props, matching WaterSource's
# "Water Puddle" disc convention rather than a standing object:
#   SandPatch  ~1.0m diameter, ~0.08m tall, gently domed, sand-tan
#   SandPickup ~0.16m diameter clump, sand-tan (same material)
#   DigHole    ~1.0m diameter, ~0.03m tall, darker dirt-brown

import bpy
import bmesh
import math
import os
import mathutils

OUTPUT_DIR = os.path.join(os.path.dirname(bpy.data.filepath) or
                           r"d:\Ben\Downloads\gridless-game.tar\gridless-game\Tools\Blender",
                           "Output")
os.makedirs(OUTPUT_DIR, exist_ok=True)

# Darker than "real" sand looks in isolation — a fully diffuse (non-
# metallic) material under IconBaker's bright ambient + two directional
# lights blows out to near-white well before a realistic sand albedo
# (~0.76), unlike the Ingot family (fully metallic, mostly specular-lit,
# so the same raw albedo value reads fine there). Found live comparing
# the baked SandIcon against Iron's — darkened empirically, not derived.
SAND_COLOR = (0.55, 0.47, 0.32, 1.0)
DIRT_COLOR = (0.30, 0.22, 0.15, 1.0)


def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in list(bpy.data.meshes):
        if block.users == 0:
            bpy.data.meshes.remove(block)
    for block in list(bpy.data.materials):
        if block.users == 0:
            bpy.data.materials.remove(block)


def new_material(name, color, roughness=0.9):
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

    bbox_max = max(max(abs(c) for c in o.bound_box[6]) for o in objs) + 0.05
    cam_dist = max(bbox_max * 3.5, 0.25)
    bpy.ops.object.camera_add(location=(cam_dist, -cam_dist, cam_dist * 0.8),
                               rotation=(math.radians(60), 0, math.radians(45)))
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


def build_sand_patch():
    clear_scene()
    radius, height = 0.5, 0.08

    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=height, vertices=20,
                                         location=(0, 0, height / 2))
    patch = bpy.context.active_object
    patch.name = "SandPatch"

    # Gentle dome: push the top-cap center vertices up slightly.
    bpy.ops.object.mode_set(mode='EDIT')
    bm = bmesh.from_edit_mesh(patch.data)
    for v in bm.verts:
        if v.co.z > 0 and v.co.x ** 2 + v.co.y ** 2 < (radius * 0.5) ** 2:
            v.co.z += height * 0.4
    bmesh.update_edit_mesh(patch.data)
    bpy.ops.object.mode_set(mode='OBJECT')

    bpy.ops.object.transform_apply(location=False, rotation=False, scale=False)
    mat = new_material("SandPatch_mat", SAND_COLOR, roughness=0.92)
    patch.data.materials.append(mat)

    objs = [patch]
    ground_and_apply(objs)
    export_and_render("SandPatch", objs)


def build_sand_pickup():
    clear_scene()
    radius = 0.08

    bpy.ops.mesh.primitive_ico_sphere_add(radius=radius, subdivisions=2, location=(0, 0, radius * 0.7))
    clump = bpy.context.active_object
    clump.name = "SandPickup"
    clump.scale = (1.1, 1.0, 0.65)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    mat = new_material("SandPickup_mat", SAND_COLOR, roughness=0.92)
    clump.data.materials.append(mat)

    objs = [clump]
    ground_and_apply(objs)
    export_and_render("SandPickup", objs)


def build_dig_hole():
    clear_scene()
    radius, height = 0.5, 0.03

    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=height, vertices=20,
                                         location=(0, 0, height / 2))
    hole = bpy.context.active_object
    hole.name = "DigHole"
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=False)

    mat = new_material("DigHole_mat", DIRT_COLOR, roughness=0.95)
    hole.data.materials.append(mat)

    objs = [hole]
    ground_and_apply(objs)
    export_and_render("DigHole", objs)


build_sand_patch()
build_sand_pickup()
build_dig_hole()
print("All three Sand dig-site models generated.")
