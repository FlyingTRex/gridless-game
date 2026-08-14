# Generates the four models SKILL_BOOKS_PLANNING.md's build order (Phase 1,
# item 7) needs: a Book ("Skill Book" cover text), a Scroll ("Magic" tag
# text), a sheet of Paper (plain, no label), and a bottle of Ink. Run
# headless:
#   blender --background --python Tools/Blender/GenerateSkillBookModels.py
#
# Outputs (one glb + one preview png per model):
#   Tools/Blender/Output/Book.glb / Book_preview.png
#   Tools/Blender/Output/Scroll.glb / Scroll_preview.png
#   Tools/Blender/Output/Paper.glb / Paper_preview.png
#   Tools/Blender/Output/Ink.glb / Ink_preview.png
#
# Player reference: CharacterController height 1.8m, 1 world unit = 1
# meter (TestScene.unity, confirmed project-wide convention). Sized as
# small hand-held/pocket items, not weapon/tool scale:
#   Book  ~0.22 x 0.16 x 0.035m (a real hardcover novel is close to this)
#   Scroll ~0.05m diameter roll, ~0.26m long (roughly forearm-length)
#   Paper ~0.21 x 0.297m (A4-ish), a few mm thick to stay non-degenerate
#   Ink   ~0.05m diameter bottle, ~0.08m tall including cap
#
# Text (cover/tag labels) uses Blender's bundled default font — no
# external font file dependency.

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
    for block in list(bpy.data.curves):
        if block.users == 0:
            bpy.data.curves.remove(block)
    for block in list(bpy.data.materials):
        if block.users == 0:
            bpy.data.materials.remove(block)


def new_material(name, color, roughness=0.6):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.roughness = roughness
    # diffuse_color alone doesn't drive the actual EEVEE render output —
    # only the node graph does. Wire it into the Principled BSDF's Base
    # Color explicitly so the preview render (and the exported glTF
    # material) actually show the intended color, not flat gray.
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


def add_label(text_body, size, location, rotation, extrude=0.0015, align='CENTER'):
    bpy.ops.object.text_add(location=location, rotation=rotation)
    obj = bpy.context.active_object
    obj.data.body = text_body
    obj.data.align_x = align
    obj.data.align_y = 'CENTER'
    obj.data.extrude = extrude
    obj.data.size = size
    bpy.ops.object.convert(target='MESH')
    return obj


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

    # A second fill light from the opposite side — the single sun left
    # near-white materials (Paper) reading as flat mid-gray against this
    # preview's dark background, easy to misjudge as "wrong color" rather
    # than "under-lit." Doesn't affect the exported glTF, preview only.
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


# ------------------------------------------------------------------- Book
def build_book():
    clear_scene()
    length, width, thickness = 0.22, 0.16, 0.035

    # primitive_cube_add(size=1) already produces a full 1-unit cube
    # (vertices at +/-0.5) -- scale is a direct multiplier on that, so it
    # must be the target dimension itself, not half of it (confirmed live:
    # the first version of this script used length/2 here and every
    # measured dimension in Unity came out at exactly half the intended
    # size).
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, thickness / 2))
    cover = bpy.context.active_object
    cover.name = "Book"
    cover.scale = (length, width, thickness)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    cover_mat = new_material("BookCover_mat", (0.32, 0.08, 0.07, 1.0), roughness=0.75)
    cover.data.materials.append(cover_mat)

    label = add_label("Skill Book", size=0.028,
                       location=(0, 0, thickness + 0.0005),
                       rotation=(0, 0, 0))
    label.name = "CoverText"
    label_mat = new_material("BookText_mat", (0.85, 0.72, 0.35, 1.0), roughness=0.4)
    label.data.materials.append(label_mat)

    objs = [cover, label]
    ground_and_apply(objs)
    export_and_render("Book", objs)


# ----------------------------------------------------------------- Scroll
def build_scroll():
    clear_scene()
    radius, roll_length = 0.025, 0.26

    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=roll_length,
                                         location=(0, 0, radius))
    roll = bpy.context.active_object
    roll.name = "Scroll"
    roll.rotation_euler = (0, math.radians(90), 0)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)

    paper_mat = new_material("ScrollPaper_mat", (0.82, 0.75, 0.58, 1.0), roughness=0.85)
    roll.data.materials.append(paper_mat)

    # Ribbon tie, a thin torus-ish band around the middle of the roll.
    bpy.ops.mesh.primitive_torus_add(location=(0, 0, radius),
                                      major_radius=radius * 1.05, minor_radius=0.004,
                                      major_segments=24, minor_segments=8)
    ribbon = bpy.context.active_object
    ribbon.name = "Ribbon"
    ribbon.rotation_euler = (0, math.radians(90), 0)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    ribbon_mat = new_material("Ribbon_mat", (0.55, 0.1, 0.1, 1.0), roughness=0.5)
    ribbon.data.materials.append(ribbon_mat)

    # Small hanging tag with the "Magic" label, flat against the roll.
    bpy.ops.mesh.primitive_plane_add(size=1, location=(0, radius + 0.003, radius))
    tag = bpy.context.active_object
    tag.name = "Tag"
    tag.scale = (0.05, 0.03, 1)
    tag.rotation_euler = (math.radians(90), 0, 0)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    tag_mat = new_material("Tag_mat", (0.9, 0.86, 0.7, 1.0), roughness=0.7)
    tag.data.materials.append(tag_mat)

    label = add_label("Magic", size=0.016,
                       location=(0, radius + 0.0035, radius),
                       rotation=(math.radians(90), 0, 0))
    label.name = "TagText"
    label_mat = new_material("ScrollText_mat", (0.15, 0.1, 0.05, 1.0), roughness=0.4)
    label.data.materials.append(label_mat)

    objs = [roll, ribbon, tag, label]
    ground_and_apply(objs)
    export_and_render("Scroll", objs)


# ------------------------------------------------------------------ Paper
def build_paper():
    clear_scene()
    length, width, thickness = 0.297, 0.21, 0.003

    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, thickness / 2))
    sheet = bpy.context.active_object
    sheet.name = "Paper"
    sheet.scale = (length, width, thickness)  # see build_book's comment on this exact bug
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    paper_mat = new_material("Paper_mat", (0.94, 0.93, 0.88, 1.0), roughness=0.9)
    sheet.data.materials.append(paper_mat)

    objs = [sheet]
    ground_and_apply(objs)
    export_and_render("Paper", objs)


# -------------------------------------------------------------------- Ink
def build_ink():
    clear_scene()
    bottle_radius, bottle_height = 0.025, 0.06
    cap_radius, cap_height = 0.014, 0.018

    # A real glass-transparency + visible-liquid-inside look needs EEVEE
    # transmission/refraction settings well beyond a placeholder prop's
    # worth of effort — simpler and more legible: the bottle body itself
    # is ink-dark (reads unambiguously as "a bottle of ink" by color and
    # silhouette alone), same "simple placeholder, Unity reassigns the
    # real material later" spirit as every other Blender-made prop here.
    bpy.ops.mesh.primitive_cylinder_add(radius=bottle_radius, depth=bottle_height,
                                         vertices=16, location=(0, 0, bottle_height / 2))
    bottle = bpy.context.active_object
    bottle.name = "Ink"
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=False)
    ink_mat = new_material("Ink_mat", (0.03, 0.02, 0.09, 1.0), roughness=0.2)
    bottle.data.materials.append(ink_mat)

    bpy.ops.mesh.primitive_cylinder_add(radius=cap_radius, depth=cap_height,
                                         vertices=16, location=(0, 0, bottle_height + cap_height / 2))
    cap = bpy.context.active_object
    cap.name = "Cap"
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=False)
    cap_mat = new_material("InkCap_mat", (0.35, 0.32, 0.28, 1.0), roughness=0.5)
    cap.data.materials.append(cap_mat)

    objs = [bottle, cap]
    ground_and_apply(objs)
    export_and_render("Ink", objs)


build_book()
build_scroll()
build_paper()
build_ink()
print("All four models generated.")
