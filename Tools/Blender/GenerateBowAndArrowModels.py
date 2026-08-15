# Generates the Bow & Arrow hunting-expansion models (see the "Hunting
# Expansion" design artifact, 2026-08-15): Bow, Stone Arrow, and a
# standalone Stone Arrowhead (used both as its own crafted item and
# visually on the arrow's tip). Run headless:
#   blender --background --python Tools/Blender/GenerateBowAndArrowModels.py
#
# Output: Tools/Blender/Output/{Bow,StoneArrow,StoneArrowhead}.glb (+ previews)
#
# Player reference: CharacterController height 1.8m, 1 world unit = 1
# meter. A real longbow/recurve bow is roughly 1.2-1.4m tall; a hunting
# arrow roughly 0.6-0.7m long; a knapped stone arrowhead roughly 3-5cm.

import bpy
import math
import os
import mathutils

OUTPUT_DIR = os.path.join(os.path.dirname(bpy.data.filepath) or
                           r"d:\Ben\Downloads\gridless-game.tar\gridless-game\Tools\Blender",
                           "Output")
os.makedirs(OUTPUT_DIR, exist_ok=True)

WOOD_COLOR = (0.42, 0.28, 0.16, 1.0)
STRING_COLOR = (0.85, 0.82, 0.72, 1.0)
STONE_COLOR = (0.55, 0.53, 0.50, 1.0)
FLETCHING_COLOR = (0.80, 0.78, 0.70, 1.0)


def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in list(bpy.data.meshes):
        if block.users == 0:
            bpy.data.meshes.remove(block)
    for block in list(bpy.data.materials):
        if block.users == 0:
            bpy.data.materials.remove(block)
    for block in list(bpy.data.curves):
        if block.users == 0:
            bpy.data.curves.remove(block)


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

    bbox_max = max(max(abs(c) for c in o.bound_box[6]) for o in objs) + 0.05
    cam_dist = max(bbox_max * 3.2, 0.3)
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


def add_arrowhead(name, parent_scale=1.0, location=(0, 0, 0), rotation=(0, 0, 0)):
    # A small flat diamond/leaf-shaped flake tapering to a point, built as
    # a scaled+tapered octahedron-ish shape from a subdivided cube for a
    # believable knapped-stone silhouette at low poly cost.
    bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=0.012 * parent_scale, radius2=0,
                                     depth=0.045 * parent_scale, location=location, rotation=rotation)
    head = bpy.context.active_object
    head.name = name
    head.scale.y = 0.35  # flatten into a blade shape
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return head


def build_stone_arrowhead():
    clear_scene()
    stone_mat = new_material("StoneArrowheadStone_mat", STONE_COLOR, roughness=0.75)

    # parent_scale=1.1 -> ~5cm long, a real knapped-arrowhead reference
    # size, not an arbitrary "make it visible" enlargement.
    head = add_arrowhead("StoneArrowhead", parent_scale=1.1,
                          location=(0, 0, 0), rotation=(math.radians(90), 0, 0))
    head.data.materials.append(stone_mat)

    objs = [head]
    ground_and_apply(objs)
    export_and_render("StoneArrowhead", objs)


def build_stone_arrow():
    clear_scene()

    wood_mat = new_material("StoneArrowShaft_mat", WOOD_COLOR, roughness=0.8)
    stone_mat = new_material("StoneArrowTip_mat", STONE_COLOR, roughness=0.75)
    fletch_mat = new_material("StoneArrowFletching_mat", FLETCHING_COLOR, roughness=0.9)

    shaft_length = 0.62
    shaft_radius = 0.005

    bpy.ops.mesh.primitive_cylinder_add(radius=shaft_radius, depth=shaft_length,
                                         location=(0, 0, 0), rotation=(math.radians(90), 0, 0))
    shaft = bpy.context.active_object
    shaft.name = "Shaft"
    shaft.data.materials.append(wood_mat)

    head = add_arrowhead("Tip", parent_scale=1.0,
                          location=(0, -(shaft_length / 2 + 0.02), 0),
                          rotation=(math.radians(90), 0, 0))
    head.data.materials.append(stone_mat)

    fletch_objs = []
    for i, ang in enumerate((0, 120, 240)):
        bpy.ops.mesh.primitive_plane_add(size=1, location=(0, shaft_length / 2 - 0.06, 0))
        fin = bpy.context.active_object
        fin.name = f"Fletch_{i}"
        fin.scale = (0.014, 0.05, 1)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        fin.rotation_euler = (math.radians(90), 0, math.radians(ang))
        fin.data.materials.append(fletch_mat)
        fletch_objs.append(fin)

    objs = [shaft, head] + fletch_objs
    # Already lies flat along Y from construction (cylinder/fletching
    # both built with a 90-degree X rotation from the start) — no
    # additional group rotation needed.
    ground_and_apply(objs)
    export_and_render("StoneArrow", objs)


def build_bow():
    clear_scene()

    wood_mat = new_material("BowWood_mat", WOOD_COLOR, roughness=0.7)
    string_mat = new_material("BowString_mat", STRING_COLOR, roughness=0.6)

    bow_height = 1.3
    limb_curve_depth = 0.11  # how far the limbs bow outward at the middle

    curve_data = bpy.data.curves.new('BowStave', type='CURVE')
    curve_data.dimensions = '3D'
    curve_data.bevel_depth = 0.012
    curve_data.bevel_resolution = 3
    curve_data.resolution_u = 12

    spline = curve_data.splines.new('BEZIER')
    spline.bezier_points.add(4)  # 5 points: bottom tip -> ... -> top tip, one smooth arc

    pts = spline.bezier_points
    half = bow_height / 2
    zs = [-half, -half * 0.5, 0, half * 0.5, half]
    # A single smooth outward bulge — bow-shaped, not an S-curve: the
    # bulge magnitude peaks at the middle and tapers to zero at both
    # tips, all on the same side (consistently +X).
    bulges = [0, limb_curve_depth * 0.85, limb_curve_depth, limb_curve_depth * 0.85, 0]

    for i, p in enumerate(pts):
        p.co = (bulges[i], 0, zs[i])
        p.handle_left_type = 'AUTO'
        p.handle_right_type = 'AUTO'

    stave_obj = bpy.data.objects.new('Stave', curve_data)
    bpy.context.collection.objects.link(stave_obj)
    stave_obj.data.materials.append(wood_mat)

    bpy.context.view_layer.objects.active = stave_obj
    stave_obj.select_set(True)
    bpy.ops.object.convert(target='MESH')
    stave_obj = bpy.context.active_object

    string_length = bow_height * 0.97
    bpy.ops.mesh.primitive_cylinder_add(radius=0.003, depth=string_length,
                                         location=(-0.01, 0, 0))
    string_obj = bpy.context.active_object
    string_obj.name = "String"
    string_obj.data.materials.append(string_mat)

    objs = [stave_obj, string_obj]
    ground_and_apply(objs)
    export_and_render("Bow", objs)


build_stone_arrowhead()
build_stone_arrow()
build_bow()
print("Bow & Arrow models generated.")
