import math
import os
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "godot" / "characters" / "modelos" / "pyra.glb"


def clear():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def mat(name, color, metallic=0.0, roughness=0.65, emission=None, strength=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness
        if emission:
            bsdf.inputs["Emission Color"].default_value = emission
            bsdf.inputs["Emission Strength"].default_value = strength
    return m


def cube(name, loc, scale, material):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material)
    bevel(obj, 0.025, 2)
    return obj


def cyl(name, loc, radius, depth, material, vertices=12, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(material)
    smooth(obj)
    return obj


def cone(name, loc, r1, r2, depth, material, vertices=12, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=r1, radius2=r2, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(material)
    smooth(obj)
    return obj


def sphere(name, loc, scale, material, segments=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=8, radius=1.0, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(material)
    smooth(obj)
    return obj


def smooth(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.shade_smooth()
    obj.select_set(False)


def bevel(obj, amount=0.02, segments=1):
    mod = obj.modifiers.new("soft_game_edges", "BEVEL")
    mod.width = amount
    mod.segments = segments
    mod.affect = "EDGES"
    obj.modifiers.new("weighted_normals", "WEIGHTED_NORMAL")


def apply_modifiers(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    for mod in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=mod.name)


def make_armature():
    bpy.ops.object.armature_add(location=(0, 0, 0))
    arm = bpy.context.object
    arm.name = "Pyra_Armature"
    data = arm.data
    data.name = "Pyra_Skeleton"
    bpy.ops.object.mode_set(mode="EDIT")
    root = data.edit_bones[0]
    root.name = "Root"
    root.head = (0, 0, 0)
    root.tail = (0, 0, 0.9)
    spine = data.edit_bones.new("Spine")
    spine.head = root.tail
    spine.tail = (0, 0, 1.55)
    spine.parent = root
    head = data.edit_bones.new("Head")
    head.head = spine.tail
    head.tail = (0, 0, 1.85)
    head.parent = spine
    arm_l = data.edit_bones.new("FlameArm_L")
    arm_l.head = (-0.22, 0, 1.35)
    arm_l.tail = (-0.78, 0, 0.96)
    arm_l.parent = spine
    arm_r = data.edit_bones.new("Arm_R")
    arm_r.head = (0.22, 0, 1.35)
    arm_r.tail = (0.68, 0, 0.96)
    arm_r.parent = spine
    bpy.ops.object.mode_set(mode="OBJECT")
    return arm


def key(obj, frame, loc=None, rot=None, scale=None):
    bpy.context.scene.frame_set(frame)
    if loc is not None:
        obj.location = loc
        obj.keyframe_insert(data_path="location", frame=frame)
    if rot is not None:
        obj.rotation_euler = rot
        obj.keyframe_insert(data_path="rotation_euler", frame=frame)
    if scale is not None:
        obj.scale = scale
        obj.keyframe_insert(data_path="scale", frame=frame)


def action_for(obj, name, frames):
    if obj.animation_data is None:
        obj.animation_data_create()
    action = bpy.data.actions.new(name)
    obj.animation_data.action = action
    for frame, loc, rot, scale in frames:
        key(obj, frame, loc, rot, scale)
    action.name = name
    action.use_fake_user = True
    track = obj.animation_data.nla_tracks.new()
    track.name = name
    track.strips.new(name, frames[0][0], action)
    track.mute = True


def main():
    clear()
    bpy.context.scene.render.fps = 30
    robe = mat("night_blue_cloth", (0.025, 0.03, 0.22, 1), roughness=0.86)
    bronze = mat("bronze_gauntlet", (0.58, 0.28, 0.085, 1), metallic=0.78, roughness=0.36)
    flame = mat("living_flame", (1.0, 0.23, 0.035, 1), roughness=0.28, emission=(1.0, 0.22, 0.035, 1), strength=4.0)
    hot = mat("white_hot_core", (1.0, 0.72, 0.18, 1), roughness=0.25, emission=(1.0, 0.62, 0.13, 1), strength=5.5)
    skin = mat("scarred_skin", (0.62, 0.36, 0.23, 1), roughness=0.74)
    dark = mat("charred_dark", (0.03, 0.025, 0.02, 1), roughness=0.9)
    ash = mat("burned_cloth_edge", (0.055, 0.043, 0.038, 1), roughness=0.95)

    armature = make_armature()

    root = bpy.data.objects.new("Pyra_Root", None)
    bpy.context.collection.objects.link(root)
    armature.parent = root

    pieces = [
        cone("short_burned_robe", (0, 0, 0.63), 0.50, 0.23, 1.08, robe, 20),
        cone("inner_shadow_under_robe", (0, 0.015, 0.50), 0.42, 0.15, 0.84, dark, 14),
        cube("front_armor_breastplate", (0, -0.045, 1.25), (0.43, 0.10, 0.50), bronze),
        cube("night_tunic_back", (0, 0.055, 1.22), (0.50, 0.12, 0.58), robe),
        sphere("head", (0, -0.015, 1.69), (0.155, 0.125, 0.185), skin, 24),
        sphere("burned_hair_mass", (0.035, 0.045, 1.82), (0.16, 0.10, 0.095), dark, 16),
        cyl("right_upper_arm", (0.35, 0, 1.24), 0.055, 0.38, skin, 12, (0, math.radians(62), 0)),
        cyl("right_forearm", (0.55, 0, 1.03), 0.050, 0.34, skin, 12, (0, math.radians(38), 0)),
        cyl("left_gauntlet_upper", (-0.37, 0, 1.24), 0.105, 0.42, bronze, 16, (0, math.radians(-62), 0)),
        cyl("left_gauntlet_forearm", (-0.62, 0, 1.02), 0.135, 0.43, bronze, 16, (0, math.radians(-43), 0)),
        cone("left_living_flame_outer", (-0.86, -0.005, 0.88), 0.20, 0.025, 0.66, flame, 18, (0, math.radians(-27), 0)),
        cone("left_living_flame_core", (-0.88, -0.035, 0.88), 0.10, 0.008, 0.58, hot, 14, (0, math.radians(-27), 0)),
        cube("left_shoulder_plate", (-0.30, -0.005, 1.44), (0.34, 0.26, 0.12), bronze),
        cube("right_shoulder_plate", (0.27, -0.005, 1.43), (0.22, 0.20, 0.09), bronze),
        cyl("left_boot", (-0.17, 0, 0.13), 0.075, 0.30, dark, 10),
        cyl("right_boot", (0.17, 0, 0.13), 0.075, 0.30, dark, 10),
    ]
    # Burned uneven robe hem and bronze vent bands sell the Pyra silhouette from camera distance.
    for i, x in enumerate([-0.34, -0.18, 0.02, 0.21, 0.37]):
        strip = cube(f"burnt_robe_cut_{i}", (x, -0.24, 0.18 + 0.035 * (i % 2)), (0.08, 0.035, 0.28), ash)
        strip.rotation_euler[2] = math.radians((-1) ** i * 7)
        pieces.append(strip)
    for i, loc in enumerate([(-0.51, -0.002, 1.10), (-0.66, -0.003, 0.98), (-0.76, -0.004, 0.88)]):
        band = cyl(f"gauntlet_vent_band_{i}", loc, 0.145 - i * 0.014, 0.045, dark, 16, (0, math.radians(-43), 0))
        pieces.append(band)
    for obj in pieces:
        obj.parent = root

    # Scar marks as thin bronze-orange strips on neck/face.
    scar1 = cube("burn_scar_neck", (-0.065, -0.132, 1.54), (0.018, 0.008, 0.19), flame)
    scar1.rotation_euler[2] = math.radians(-18)
    scar1.parent = root
    scar2 = cube("burn_scar_jaw", (-0.075, -0.13, 1.66), (0.014, 0.008, 0.13), flame)
    scar2.rotation_euler[2] = math.radians(48)
    scar2.parent = root

    meshes = pieces + [scar1, scar2]
    for obj in meshes:
        apply_modifiers(obj)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = pieces[0]
    bpy.ops.object.join()
    body = bpy.context.object
    body.name = "Pyra_Body"
    body.parent = root

    action_for(root, "Idle", [
        (1, Vector((0, 0, 0)), (0, 0, 0), (1, 1, 1)),
        (30, Vector((0, 0, 0.025)), (0, 0, math.radians(1.5)), (1.01, 1.01, 1.01)),
        (60, Vector((0, 0, 0)), (0, 0, 0), (1, 1, 1)),
    ])
    action_for(root, "Armature|Running", [
        (1, Vector((0, 0, 0.00)), (math.radians(-4), 0, math.radians(2)), (1, 1, 1)),
        (10, Vector((0, 0, 0.06)), (math.radians(-5), 0, math.radians(-2)), (1, 1, 1)),
        (20, Vector((0, 0, 0.00)), (math.radians(-4), 0, math.radians(2)), (1, 1, 1)),
    ])
    action_for(root, "Spell Cast", [
        (1, Vector((0, 0, 0)), (0, 0, 0), (1, 1, 1)),
        (8, Vector((0, 0, 0.02)), (0, 0, math.radians(-8)), (1, 1, 1)),
        (14, Vector((0, 0, -0.025)), (0, 0, math.radians(12)), (1.04, 1.04, 1.04)),
        (24, Vector((0, 0, 0)), (0, 0, 0), (1, 1, 1)),
    ])

    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for obj in [armature, body]:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root

    OUT.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.gltf(
        filepath=str(OUT),
        export_format="GLB",
        use_selection=True,
        export_animations=True,
        export_nla_strips=True,
        export_materials="EXPORT",
        export_yup=True,
    )
    print(f"EXPORT {OUT}")


if __name__ == "__main__":
    main()
