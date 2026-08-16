# Generates the Herbal Tea model (2026-08-15) — a copy of the Kettle
# geometry with the existing Herb model overlaid against it, per Ben's ask
# ("merge a copy of the kettle with the herb model slightly overlaid").
# Run headless:
#   blender --background --python Tools/Blender/GenerateHerbalTeaModel.py
#
# Output: Tools/Blender/Output/HerbalTea.glb (+ preview)
#
# Kettle geometry duplicated from GenerateCookwareModels.py (that script
# builds the standalone Kettle pickup and isn't touched here) rather than
# imported, since Blender has no clean "import one prefab's mesh data only"
# path — this keeps both generators self-contained. Herb.glb (the existing
# Herb pickup model, already in Assets/Models/) is imported directly and
# leaned against the kettle body.

import bpy
import math
import os
import mathutils

OUTPUT_DIR = os.path.join(os.path.dirname(bpy.data.filepath) or
                           r"d:\Ben\Downloads\gridless-game.tar\gridless-game\Tools\Blender",
                           "Output")
os.makedirs(OUTPUT_DIR, exist_ok=True)

HERB_MODEL_PATH = r"d:\Ben\Downloads\gridless-game.tar\gridless-game\Assets\Models\Herb.glb"
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


def world_bounds(objs):
    pts = []
    for o in objs:
        pts.extend(o.matrix_world @ mathutils.Vector(c) for c in o.bound_box)
    xs = [p.x for p in pts]; ys = [p.y for p in pts]; zs = [p.z for p in pts]
    return mathutils.Vector((min(xs), min(ys), min(zs))), mathutils.Vector((max(xs), max(ys), max(zs)))


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


def build_kettle_geometry(mat):
    radius, height = 0.085, 0.12
    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=height, location=(0, 0, height / 2), vertices=24)
    body = bpy.context.active_object
    body.name = "KettleBody"
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

    bpy.ops.mesh.primitive_cone_add(radius1=0.018, radius2=0.008, depth=0.09,
                                     location=(radius + 0.03, 0, height * 0.55))
    spout = bpy.context.active_object
    spout.name = "KettleSpout"
    spout.rotation_euler = (0, math.radians(70), 0)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    spout.data.materials.append(mat)
    objs.append(spout)

    objs.append(add_loop_handle("KettleHandle", mat, radius * 0.75, 0.007,
                                 (0, 0, height * 0.85), (math.radians(90), 0, 0)))

    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.012, location=(0, 0, height + 0.005))
    knob = bpy.context.active_object
    knob.name = "KettleKnob"
    knob.data.materials.append(mat)
    objs.append(knob)

    return objs, radius, height


def build():
    clear_scene()
    mat = new_metal_material("HerbalTeaKettle_mat", TIN_COLOR, roughness=0.3, metallic=0.9)

    kettle_objs, radius, height = build_kettle_geometry(mat)

    # Import the existing Herb model and lean it against the kettle's
    # body, slightly overlapping — Ben's explicit ask, not a separate
    # ingredient sitting apart from it.
    bpy.ops.import_scene.gltf(filepath=HERB_MODEL_PATH)
    herb_objs = [o for o in bpy.context.selected_objects if o.type == 'MESH']
    if not herb_objs:
        # glTF import can parent meshes under an empty; fall back to any
        # newly-added mesh object if the selection didn't carry mesh types.
        herb_objs = [o for o in bpy.data.objects if o.type == 'MESH' and o not in kettle_objs]

    # Herb.glb is a flat leaf lying on the XY plane (~3mm thick) — measure
    # its longest in-plane dimension (length), not Z, or normalizing
    # against near-zero height blows the whole model up to nonsense size.
    herb_min, herb_max = world_bounds(herb_objs)
    herb_size = herb_max - herb_min
    herb_length = max(herb_size.x, herb_size.y, 0.001)

    target_length = 0.10  # believable single-sprig length, ~Feather scale
    scale_factor = target_length / herb_length
    for o in herb_objs:
        o.scale = tuple(s * scale_factor for s in o.scale)
    bpy.ops.object.select_all(action='DESELECT')
    for o in herb_objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = herb_objs[0]
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    # Re-measure after scaling, recenter, then rotate from lying-flat to
    # leaning upright against the kettle's side (mostly around X so its
    # length runs vertically), positioned so its base overlaps the body.
    herb_min, herb_max = world_bounds(herb_objs)
    herb_center_offset = (herb_min + herb_max) * 0.5
    for o in herb_objs:
        o.location -= herb_center_offset
        # A pure X-axis tilt leaves the leaf's flat face edge-on (nearly
        # invisible) from some camera angles, including IconBaker's fixed
        # 3/4 default — adding a Y-axis component breaks that so the leaf
        # presents a visible face from more directions.
        o.rotation_euler = (math.radians(55), math.radians(25), math.radians(15))
    bpy.ops.object.select_all(action='DESELECT')
    for o in herb_objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = herb_objs[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=False)

    # Re-measure once more (post-rotation bounds differ from pre-rotation)
    # and place leaning against the kettle body, base overlapping it.
    # Blender's Y axis becomes Unity's NEGATIVE Z on glTF import — IconBaker's
    # default camera sits at (+X, +Y, -Z) looking toward +Z, so a Blender-
    # space -Y offset (Unity +Z) lands the herb on the FAR side of the
    # kettle from that camera, occluded behind the body. +Y here (Unity -Z)
    # keeps it on the camera's near side instead.
    herb_min, herb_max = world_bounds(herb_objs)
    herb_base_offset = mathutils.Vector((0, 0, -herb_min.z))
    for o in herb_objs:
        o.location += herb_base_offset
        o.location += mathutils.Vector((radius * 0.5, radius * 0.85, height * 0.15))
    bpy.ops.object.select_all(action='DESELECT')
    for o in herb_objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = herb_objs[0]
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)

    all_objs = kettle_objs + herb_objs
    ground_and_apply(all_objs)
    export_and_render("HerbalTea", all_objs)


build()
print("Herbal Tea model generated.")
