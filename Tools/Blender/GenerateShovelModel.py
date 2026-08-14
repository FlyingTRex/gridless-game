# Generates the Rudimentary Shovel model (first tier of a future
# Crude->Masterwork Shovel ladder, DEXTERITY_CONSTITUTION_PLANNING.md's
# neighbor doc BUGS_AND_ENHANCEMENTS.md's digging/water-scarcity section).
# Run headless:
#   blender --background --python Tools/Blender/GenerateShovelModel.py
#
# Output: Tools/Blender/Output/RudimentaryShovel.glb / _preview.png
#
# Player reference: CharacterController height 1.8m, 1 world unit = 1
# meter (TestScene.unity, confirmed project-wide convention). A real
# long-handled shovel is roughly 0.9-1.1m overall — sized at the low end
# of that (a "rudimentary" tool, not a full adult-height spade):
#   Handle: 0.017m radius, 0.72m long, wood-brown
#   Blade:  0.16m wide, 0.22m long, tapering to a point, metal-gray
#   Grip:   a small sphere cap at the very top of the handle
# Pivot at the base (blade tip) by construction, via ground_and_apply.

import bpy
import bmesh
import math
import os
import mathutils

OUTPUT_DIR = os.path.join(os.path.dirname(bpy.data.filepath) or
                           r"d:\Ben\Downloads\gridless-game.tar\gridless-game\Tools\Blender",
                           "Output")
os.makedirs(OUTPUT_DIR, exist_ok=True)


def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in list(bpy.data.meshes):
        if block.users == 0:
            bpy.data.meshes.remove(block)
    for block in list(bpy.data.materials):
        if block.users == 0:
            bpy.data.materials.remove(block)


def new_material(name, color, roughness=0.6, metallic=0.0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.roughness = roughness
    mat.metallic = metallic
    # diffuse_color alone doesn't drive the actual EEVEE render output —
    # only the node graph does. Wire it into the Principled BSDF's Base
    # Color/Roughness/Metallic explicitly (CLAUDE.md's Blender gotcha).
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


def build_blade():
    # A simple flat trowel-shaped blade: wide at the top, tapering to a
    # point at the bottom, thin extrusion for a bit of real thickness.
    width, height, thickness = 0.16, 0.22, 0.012

    bm = bmesh.new()
    verts = [
        bm.verts.new((-width / 2, 0, height)),
        bm.verts.new((width / 2, 0, height)),
        bm.verts.new((width * 0.35, 0, height * 0.25)),
        bm.verts.new((0, 0, 0)),
        bm.verts.new((-width * 0.35, 0, height * 0.25)),
    ]
    face = bm.faces.new(verts)
    geom = bmesh.ops.extrude_face_region(bm, geom=[face])
    verts_extruded = [v for v in geom['geom'] if isinstance(v, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, vec=(0, thickness, 0), verts=verts_extruded)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)

    mesh = bpy.data.meshes.new("BladeMesh")
    bm.to_mesh(mesh)
    bm.free()

    blade = bpy.data.objects.new("Blade", mesh)
    bpy.context.collection.objects.link(blade)
    blade.location = (0, -thickness / 2, 0)
    bpy.context.view_layer.objects.active = blade
    blade.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)

    blade_mat = new_material("ShovelBlade_mat", (0.55, 0.56, 0.58, 1.0), roughness=0.35, metallic=0.85)
    blade.data.materials.append(blade_mat)
    return blade, height


def build_shovel():
    clear_scene()

    blade, blade_height = build_blade()

    handle_radius, handle_length = 0.017, 0.72
    bpy.ops.mesh.primitive_cylinder_add(radius=handle_radius, depth=handle_length, vertices=12,
                                         location=(0, 0, blade_height + handle_length / 2))
    handle = bpy.context.active_object
    handle.name = "Handle"
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=False)
    handle_mat = new_material("ShovelHandle_mat", (0.42, 0.28, 0.15, 1.0), roughness=0.75)
    handle.data.materials.append(handle_mat)

    grip_radius = handle_radius * 1.6
    bpy.ops.mesh.primitive_uv_sphere_add(radius=grip_radius, location=(0, 0, blade_height + handle_length))
    grip = bpy.context.active_object
    grip.name = "Grip"
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=False)
    grip.data.materials.append(handle_mat)

    objs = [blade, handle, grip]
    ground_and_apply(objs)
    export_and_render("RudimentaryShovel", objs)


build_shovel()
print("Shovel model generated.")
