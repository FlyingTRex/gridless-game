# Generates a single shared seed-packet model, reused across all 7 Garden
# Plot crops (Carrot/Potato/Ginger/Turnip/Onion/Sweet Potato/Corn) as each
# crop's Seed Packet worldPickupPrefab — one mesh, 7 color-coded material
# variants created afterward in Unity (see COOKING_AND_GARDENING_PLANNING.md
# section 6's seed-pack ideation, 2026-08-15). No per-crop lettering baked
# in (this project's own text-on-imported-model precedent is unreliable),
# so color alone (plus the item's own 2D icon in UI) is what tells packets
# apart. Run headless:
#   blender --background --python Tools/Blender/GenerateSeedPacketModel.py
#
# Output: Tools/Blender/Output/SeedPacket.glb (+ preview png)
#
# Player reference: CharacterController height 1.8m, 1 world unit = 1
# meter. A real paper seed packet is roughly 8cm x 11cm x ~1cm thick —
# small enough to sit in a palm.

import bpy
import math
import os
import mathutils

OUTPUT_DIR = os.path.join(os.path.dirname(bpy.data.filepath) or
                           r"d:\Ben\Downloads\gridless-game.tar\gridless-game\Tools\Blender",
                           "Output")
os.makedirs(OUTPUT_DIR, exist_ok=True)

PAPER_COLOR = (0.85, 0.80, 0.68, 1.0)  # neutral tan — Unity swaps per crop

PACKET_WIDTH = 0.08
PACKET_HEIGHT = 0.09
PACKET_THICKNESS = 0.008
FLAP_HEIGHT = 0.02
FLAP_ANGLE_DEG = 20  # folded-over flap tilts back slightly


def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for block in list(bpy.data.meshes):
        if block.users == 0:
            bpy.data.meshes.remove(block)
    for block in list(bpy.data.materials):
        if block.users == 0:
            bpy.data.materials.remove(block)


def new_material(name, color, roughness=0.95):
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
    cam_dist = max(bbox_max * 4.0, 0.2)
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


def build_seed_packet():
    clear_scene()

    paper_mat = new_material("SeedPacketPaper_mat", PAPER_COLOR)

    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, PACKET_HEIGHT / 2))
    body = bpy.context.active_object
    body.name = "PacketBody"
    body.scale = (PACKET_WIDTH, PACKET_THICKNESS, PACKET_HEIGHT)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    body.data.materials.append(paper_mat)

    # Folded-over top flap — a thin slab tilted back from the packet's top
    # edge, the classic seed-envelope silhouette.
    flap_pivot_z = PACKET_HEIGHT
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, flap_pivot_z + FLAP_HEIGHT / 2))
    flap = bpy.context.active_object
    flap.name = "PacketFlap"
    flap.scale = (PACKET_WIDTH, PACKET_THICKNESS * 1.2, FLAP_HEIGHT)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    flap.data.materials.append(paper_mat)

    # Move the flap's own origin to the fold line (its bottom-center, in
    # world space) without moving its geometry, so rotating it hinges
    # there instead of at its own center — reads as "folded over" rather
    # than floating above the body.
    bpy.context.scene.cursor.location = (0, 0, flap_pivot_z)
    bpy.ops.object.select_all(action='DESELECT')
    flap.select_set(True)
    bpy.context.view_layer.objects.active = flap
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')

    flap.rotation_euler = (math.radians(-FLAP_ANGLE_DEG), 0, 0)

    objs = [body, flap]
    ground_and_apply(objs)
    export_and_render("SeedPacket", objs)


build_seed_packet()
print("Seed Packet model generated.")
