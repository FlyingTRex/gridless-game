# Generates the single-cell "proof of concept" Garden Plot model
# (COOKING_AND_GARDENING_PLANNING.md, single-plant POC pass 2026-08-14) —
# a small raised wooden-frame bed just big enough for one plant, distinct
# from (and much smaller than) the eventual 4x4 GardenPlot design. Run
# headless:
#   blender --background --python Tools/Blender/GenerateGardenPlotModel.py
#
# Output: Tools/Blender/Output/GardenPlot.glb (+ preview png)
#
# Player reference: CharacterController height 1.8m, 1 world unit = 1
# meter. A real raised garden bed is roughly knee-height and small enough
# to reach into from any side — ~0.8m square footprint, ~0.3m tall walls,
# matching a single-plant scale (this is NOT the eventual 4x4/5m version).

import bpy
import bmesh
import math
import os
import mathutils

OUTPUT_DIR = os.path.join(os.path.dirname(bpy.data.filepath) or
                           r"d:\Ben\Downloads\gridless-game.tar\gridless-game\Tools\Blender",
                           "Output")
os.makedirs(OUTPUT_DIR, exist_ok=True)

WOOD_COLOR = (0.36, 0.24, 0.14, 1.0)
SOIL_COLOR = (0.22, 0.15, 0.10, 1.0)

BED_SIZE = 0.8      # outer footprint, square
WALL_HEIGHT = 0.3
WALL_THICKNESS = 0.06


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


def add_wall(name, size_x, size_y, size_z, location):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    wall = bpy.context.active_object
    wall.name = name
    wall.scale = (size_x, size_y, size_z)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return wall


def build_garden_plot():
    clear_scene()

    half = BED_SIZE / 2
    inner = half - WALL_THICKNESS / 2
    wall_center_z = WALL_HEIGHT / 2

    walls = [
        add_wall("WallNorth", BED_SIZE, WALL_THICKNESS, WALL_HEIGHT, (0, inner, wall_center_z)),
        add_wall("WallSouth", BED_SIZE, WALL_THICKNESS, WALL_HEIGHT, (0, -inner, wall_center_z)),
        add_wall("WallEast", WALL_THICKNESS, BED_SIZE, WALL_HEIGHT, (inner, 0, wall_center_z)),
        add_wall("WallWest", WALL_THICKNESS, BED_SIZE, WALL_HEIGHT, (-inner, 0, wall_center_z)),
    ]
    wood_mat = new_material("GardenPlotWood_mat", WOOD_COLOR, roughness=0.85)
    for w in walls:
        w.data.materials.append(wood_mat)

    soil_size = BED_SIZE - WALL_THICKNESS * 2
    soil_height = WALL_HEIGHT * 0.7
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, soil_height / 2))
    soil = bpy.context.active_object
    soil.name = "Soil"
    soil.scale = (soil_size, soil_size, soil_height)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    soil_mat = new_material("GardenPlotSoil_mat", SOIL_COLOR, roughness=0.95)
    soil.data.materials.append(soil_mat)

    objs = walls + [soil]
    ground_and_apply(objs)
    export_and_render("GardenPlot", objs)


build_garden_plot()
print("Garden Plot model generated.")
