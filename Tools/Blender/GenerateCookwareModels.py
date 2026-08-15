# Generates the 4 Campfire cooking accessories (2026-08-15,
# CAMPFIRE_PLANNING.md section 4's "still the one open gap" — the
# grillSlot/cookingPotSlot/kettleSlot/fryingPanSlot data/gating structure
# has been live since v0.3.30-dev, just visually a blank placeholder).
# Run headless:
#   blender --background --python Tools/Blender/GenerateCookwareModels.py
#
# Output: Tools/Blender/Output/{Grill,CookingPot,Kettle,FryingPan}.glb (+ previews)
#
# Player reference: 1 world unit = 1 meter (CharacterController height 1.8).
# A real charcoal-grill grate is roughly 30-35cm across; a small cast-iron
# camp pot roughly 20-22cm diameter / 16-18cm tall; a camp kettle roughly
# 15-18cm diameter with a spout+handle bringing its overall height close to
# the pot's; a frying pan's bowl roughly 20cm across with a handle bringing
# total length to ~35-38cm.

import bpy
import math
import os
import mathutils

OUTPUT_DIR = os.path.join(os.path.dirname(bpy.data.filepath) or
                           r"d:\Ben\Downloads\gridless-game.tar\gridless-game\Tools\Blender",
                           "Output")
os.makedirs(OUTPUT_DIR, exist_ok=True)

CAST_IRON_COLOR = (0.05, 0.05, 0.055, 1.0)
TIN_COLOR = (0.55, 0.56, 0.58, 1.0)


def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in list(bpy.data.meshes):
        if block.users == 0:
            bpy.data.meshes.remove(block)
    for block in list(bpy.data.materials):
        if block.users == 0:
            bpy.data.materials.remove(block)


def new_metal_material(name, color, roughness=0.5, metallic=0.85):
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


def add_loop_handle(name, mat, radius_major, radius_minor, location, rotation):
    bpy.ops.mesh.primitive_torus_add(major_radius=radius_major, minor_radius=radius_minor,
                                      major_segments=16, minor_segments=8, location=location)
    handle = bpy.context.active_object
    handle.name = name
    handle.rotation_euler = rotation
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    handle.data.materials.append(mat)
    return handle


def build_grill():
    clear_scene()
    mat = new_metal_material("Grill_mat", CAST_IRON_COLOR, roughness=0.55)

    width, depth, bar_h = 0.32, 0.24, 0.012
    objs = []

    # Frame: 4 thin bars around the perimeter.
    frame_thickness = 0.012
    for (sx, sy, loc) in [
        (width, frame_thickness, (0, depth / 2 - frame_thickness / 2, 0)),
        (width, frame_thickness, (0, -depth / 2 + frame_thickness / 2, 0)),
        (frame_thickness, depth, (width / 2 - frame_thickness / 2, 0, 0)),
        (frame_thickness, depth, (-width / 2 + frame_thickness / 2, 0, 0)),
    ]:
        bpy.ops.mesh.primitive_cube_add(size=1, location=(loc[0], loc[1], bar_h / 2))
        bar = bpy.context.active_object
        bar.scale = (sx, sy, bar_h)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        bar.data.materials.append(mat)
        objs.append(bar)

    # Grate rods running across the frame.
    rod_radius = 0.006
    rod_count = 7
    for i in range(rod_count):
        x = -width / 2 + (i + 0.5) * (width / rod_count)
        bpy.ops.mesh.primitive_cylinder_add(radius=rod_radius, depth=depth, location=(x, 0, bar_h / 2))
        rod = bpy.context.active_object
        rod.rotation_euler = (math.radians(90), 0, 0)
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
        rod.data.materials.append(mat)
        objs.append(rod)

    # Short legs so it visibly sits above the fire, not flush on the ground.
    leg_h = 0.05
    for (lx, ly) in [(width / 2 - 0.02, depth / 2 - 0.02), (-width / 2 + 0.02, depth / 2 - 0.02),
                      (width / 2 - 0.02, -depth / 2 + 0.02), (-width / 2 + 0.02, -depth / 2 + 0.02)]:
        bpy.ops.mesh.primitive_cylinder_add(radius=0.008, depth=leg_h, location=(lx, ly, -leg_h / 2))
        leg = bpy.context.active_object
        leg.data.materials.append(mat)
        objs.append(leg)

    ground_and_apply(objs)
    export_and_render("Grill", objs)


def build_cooking_pot():
    clear_scene()
    mat = new_metal_material("CookingPot_mat", CAST_IRON_COLOR, roughness=0.45)

    radius, height = 0.11, 0.16
    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=height, location=(0, 0, height / 2), vertices=24)
    body = bpy.context.active_object
    body.name = "PotBody"
    body.data.materials.append(mat)
    objs = [body]

    # Rim lip near the top.
    bpy.ops.mesh.primitive_torus_add(major_radius=radius, minor_radius=0.007,
                                      major_segments=24, minor_segments=8,
                                      location=(0, 0, height - 0.01))
    rim = bpy.context.active_object
    rim.name = "PotRim"
    rim.data.materials.append(mat)
    objs.append(rim)

    # Two loop handles on opposite sides, near the top.
    handle_z = height * 0.7
    objs.append(add_loop_handle("PotHandleL", mat, 0.025, 0.006,
                                 (radius + 0.012, 0, handle_z), (0, math.radians(90), 0)))
    objs.append(add_loop_handle("PotHandleR", mat, 0.025, 0.006,
                                 (-radius - 0.012, 0, handle_z), (0, math.radians(90), 0)))

    ground_and_apply(objs)
    export_and_render("CookingPot", objs)


def build_kettle():
    clear_scene()
    mat = new_metal_material("Kettle_mat", TIN_COLOR, roughness=0.3, metallic=0.9)

    radius, height = 0.085, 0.12
    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=height, location=(0, 0, height / 2), vertices=24)
    body = bpy.context.active_object
    body.name = "KettleBody"
    # Taper the top inward slightly for a kettle silhouette.
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(body.data)
    bm.verts.ensure_lookup_table()
    for v in bm.verts:
        if v.co.z > height * 0.3:
            v.co.x *= 0.8
            v.co.y *= 0.8
    bm.to_mesh(body.data)
    bm.free()
    body.data.materials.append(mat)
    objs = [body]

    # Spout: a thin cone jutting out and slightly up from one side.
    bpy.ops.mesh.primitive_cone_add(radius1=0.018, radius2=0.008, depth=0.09,
                                     location=(radius + 0.03, 0, height * 0.55))
    spout = bpy.context.active_object
    spout.name = "KettleSpout"
    spout.rotation_euler = (0, math.radians(70), 0)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    spout.data.materials.append(mat)
    objs.append(spout)

    # Arched handle over the top.
    objs.append(add_loop_handle("KettleHandle", mat, radius * 0.75, 0.007,
                                 (0, 0, height * 0.85), (math.radians(90), 0, 0)))

    # Small lid knob on top.
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.012, location=(0, 0, height + 0.005))
    knob = bpy.context.active_object
    knob.name = "KettleKnob"
    knob.data.materials.append(mat)
    objs.append(knob)

    ground_and_apply(objs)
    export_and_render("Kettle", objs)


def build_frying_pan():
    clear_scene()
    mat = new_metal_material("FryingPan_mat", CAST_IRON_COLOR, roughness=0.5)

    radius, height = 0.1, 0.025
    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=height, location=(0, 0, height / 2), vertices=24)
    body = bpy.context.active_object
    body.name = "PanBody"
    body.data.materials.append(mat)
    objs = [body]

    # Slightly raised rim so it reads as a shallow bowl, not a flat disc.
    bpy.ops.mesh.primitive_torus_add(major_radius=radius, minor_radius=0.006,
                                      major_segments=24, minor_segments=8,
                                      location=(0, 0, height - 0.004))
    rim = bpy.context.active_object
    rim.name = "PanRim"
    rim.data.materials.append(mat)
    objs.append(rim)

    # Long straight handle extending from the edge.
    handle_len = 0.16
    bpy.ops.mesh.primitive_cylinder_add(radius=0.012, depth=handle_len,
                                         location=(radius + handle_len / 2, 0, height / 2))
    handle = bpy.context.active_object
    handle.name = "PanHandle"
    handle.rotation_euler = (0, math.radians(90), 0)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    handle.data.materials.append(mat)
    objs.append(handle)

    ground_and_apply(objs)
    export_and_render("FryingPan", objs)


build_grill()
build_cooking_pot()
build_kettle()
build_frying_pan()
print("Cookware models generated.")
