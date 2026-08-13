# Generates the Campfire model (ring of rocks + charred wood pile) from
# scratch in Blender, replacing the pre-Blender placeholder (scaled
# cylinders). Run headless:
#   blender --background --python Tools/Blender/GenerateCampfireModel.py
#
# Outputs:
#   Tools/Blender/Output/campfire_preview.png  — quick render for visual review
#   Tools/Blender/Output/Campfire.glb          — the exported model
#
# Two objects only, named "Rocks" and "Wood" — kept as separate meshes/
# renderers on purpose so the Unity-side Campfire.cs SetLit() can swap
# material on the Wood renderer only, leaving Rocks static (per
# CAMPFIRE_PLANNING.md's "logs swap material when lit, rocks stay static"
# decision). Materials assigned here are simple placeholders (flat colors)
# — Unity import reassigns the project's real materials (RockChunk.mat,
# TreeBark.mat) by material-slot name, not these.
#
# Player reference: CharacterController height 1.8m — a real campfire ring
# is roughly knee-height, so rocks ~0.15-0.2m tall, wood pile ~0.3-0.4m,
# total footprint ~0.9m diameter.

import bpy
import bmesh
import math
import random
import os
import mathutils

random.seed(7)

OUTPUT_DIR = os.path.join(os.path.dirname(bpy.data.filepath) or
                           r"d:\Ben\Downloads\gridless-game.tar\gridless-game\Tools\Blender",
                           "Output")
os.makedirs(OUTPUT_DIR, exist_ok=True)
GLB_PATH = os.path.join(OUTPUT_DIR, "Campfire.glb")
PREVIEW_PATH = os.path.join(OUTPUT_DIR, "campfire_preview.png")

# ---------------------------------------------------------------- cleanup
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for block in list(bpy.data.meshes):
    if block.users == 0:
        bpy.data.meshes.remove(block)

# ------------------------------------------------------------------ rocks
def make_rock(location, radius):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=radius, location=location)
    obj = bpy.context.active_object
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    for v in bm.verts:
        disp = random.uniform(-0.18, 0.18) * radius
        v.co += v.normal * disp
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    obj.scale = (
        random.uniform(0.85, 1.15),
        random.uniform(0.85, 1.15),
        random.uniform(0.55, 0.75),  # squash on Z so rocks read as sitting, not floating spheres
    )
    obj.rotation_euler = (0, 0, random.uniform(0, math.tau))
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    return obj

ring_radius = 0.42
num_rocks = 8
rock_objs = []
for i in range(num_rocks):
    angle = (math.tau / num_rocks) * i + random.uniform(-0.12, 0.12)
    r = ring_radius + random.uniform(-0.03, 0.05)
    x = math.cos(angle) * r
    y = math.sin(angle) * r
    rock_radius = random.uniform(0.11, 0.17)
    rock_objs.append(make_rock((x, y, rock_radius * 0.35), rock_radius))

bpy.ops.object.select_all(action='DESELECT')
for o in rock_objs:
    o.select_set(True)
bpy.context.view_layer.objects.active = rock_objs[0]
bpy.ops.object.join()
rocks = bpy.context.active_object
rocks.name = "Rocks"

rock_mat = bpy.data.materials.new("Rock_mat")
rock_mat.diffuse_color = (0.42, 0.40, 0.37, 1.0)
rocks.data.materials.append(rock_mat)

# -------------------------------------------------------------- wood pile
def make_stick(length, r_base, r_tip):
    bpy.ops.mesh.primitive_cone_add(vertices=7, radius1=r_base, radius2=r_tip, depth=length, location=(0, 0, 0))
    obj = bpy.context.active_object
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.translate(bm, verts=bm.verts, vec=(0, 0, length / 2))
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    return obj

num_sticks = 6
pile_base_radius = 0.09
wood_objs = []
for i in range(num_sticks):
    length = random.uniform(0.42, 0.58)
    r_base = random.uniform(0.035, 0.05)
    r_tip = r_base * random.uniform(0.35, 0.55)
    obj = make_stick(length, r_base, r_tip)

    angle = (math.tau / num_sticks) * i + random.uniform(-0.25, 0.25)
    lean_deg = random.uniform(22, 38)  # from vertical, so tips cross above center in a shallow teepee
    bx = math.cos(angle) * pile_base_radius
    by = math.sin(angle) * pile_base_radius

    obj.location = (bx, by, 0.0)
    obj.rotation_euler = (math.radians(lean_deg), 0.0, angle + math.pi)  # tilt then aim tip toward center
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=False)
    wood_objs.append(obj)

bpy.ops.object.select_all(action='DESELECT')
for o in wood_objs:
    o.select_set(True)
bpy.context.view_layer.objects.active = wood_objs[0]
bpy.ops.object.join()
wood = bpy.context.active_object
wood.name = "Wood"

wood_mat = bpy.data.materials.new("Wood_mat")
wood_mat.diffuse_color = (0.12, 0.07, 0.05, 1.0)
wood.data.materials.append(wood_mat)

# ------------------------------------------------------- ground the whole prop
def world_bounds_min_z(obj):
    return min((obj.matrix_world @ mathutils.Vector(c)).z for c in obj.bound_box)

min_z = min(world_bounds_min_z(rocks), world_bounds_min_z(wood))
offset = -min_z
rocks.location.z += offset
wood.location.z += offset

bpy.ops.object.select_all(action='DESELECT')
rocks.select_set(True)
wood.select_set(True)
bpy.context.view_layer.objects.active = rocks
bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)

# ------------------------------------------------------------------ export
bpy.ops.object.select_all(action='DESELECT')
rocks.select_set(True)
wood.select_set(True)
bpy.ops.export_scene.gltf(
    filepath=GLB_PATH,
    export_format='GLB',
    use_selection=True,
)
print(f"Exported {GLB_PATH}")

# ------------------------------------------------------------- preview render
bpy.ops.object.camera_add(location=(1.1, -1.1, 0.85), rotation=(math.radians(65), 0, math.radians(45)))
cam = bpy.context.active_object
bpy.context.scene.camera = cam

bpy.ops.object.light_add(type='SUN', location=(1.0, -1.0, 2.0))
sun = bpy.context.active_object
sun.data.energy = 3.0
sun.rotation_euler = (math.radians(45), 0, math.radians(45))

scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.filepath = PREVIEW_PATH
bpy.ops.render.render(write_still=True)
print(f"Rendered preview {PREVIEW_PATH}")
