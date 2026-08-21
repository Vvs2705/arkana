import math
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
GLB = ROOT / "godot" / "characters" / "modelos" / "pyra.glb"
OUT = ROOT / "godot" / "build" / "pyra_preview.png"


def clear():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def set_origin_view():
    imported = [obj for obj in bpy.context.scene.objects if obj.type in {"MESH", "ARMATURE", "EMPTY"}]
    if not imported:
        return
    xs, ys, zs = [], [], []
    for obj in imported:
        for corner in obj.bound_box:
            p = obj.matrix_world @ Vector(corner)
            xs.append(p.x)
            ys.append(p.y)
            zs.append(p.z)
    center = Vector(((min(xs) + max(xs)) * 0.5, (min(ys) + max(ys)) * 0.5, (min(zs) + max(zs)) * 0.5))
    for obj in imported:
        if obj.parent is None:
            obj.location -= center
            obj.rotation_euler[2] = math.radians(-18)


def add_floor():
    mat = bpy.data.materials.new("preview_floor")
    mat.diffuse_color = (0.09, 0.12, 0.14, 1)
    bpy.ops.mesh.primitive_plane_add(size=5.0, location=(0, 0, -0.02))
    floor = bpy.context.object
    floor.name = "Preview_Floor"
    floor.data.materials.append(mat)


def add_lighting():
    bpy.ops.object.light_add(type="AREA", location=(-2.4, -3.0, 4.2))
    key = bpy.context.object
    key.name = "Key_Warm"
    key.data.energy = 600
    key.data.size = 4.0

    bpy.ops.object.light_add(type="POINT", location=(-1.3, -1.2, 1.4))
    flame = bpy.context.object
    flame.name = "Flame_Bounce"
    flame.data.color = (1.0, 0.34, 0.08)
    flame.data.energy = 130
    flame.data.shadow_soft_size = 2.0

    bpy.ops.object.light_add(type="AREA", location=(2.5, 2.8, 2.6))
    rim = bpy.context.object
    rim.name = "Cool_Rim"
    rim.data.color = (0.42, 0.58, 1.0)
    rim.data.energy = 170
    rim.data.size = 3.0


def add_camera():
    bpy.ops.object.camera_add(location=(0.55, -6.0, 1.15))
    cam = bpy.context.object
    cam.name = "Preview_Camera"
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 3.35
    target = Vector((0, 0, 0.86))
    direction = target - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.camera = cam


def render():
    bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.eevee.taa_render_samples = 64
    bpy.context.scene.render.resolution_x = 1280
    bpy.context.scene.render.resolution_y = 720
    bpy.context.scene.view_settings.view_transform = "AgX"
    bpy.context.scene.view_settings.look = "AgX - Medium High Contrast"
    bpy.context.scene.view_settings.exposure = 0.0
    bpy.context.scene.view_settings.gamma = 1.0
    bpy.context.scene.world = bpy.data.worlds.new("Preview_World")
    bpy.context.scene.world.color = (0.78, 0.66, 0.52)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(OUT)
    bpy.ops.render.render(write_still=True)
    print(f"RENDER {OUT}")


def main():
    clear()
    bpy.ops.import_scene.gltf(filepath=str(GLB))
    set_origin_view()
    add_floor()
    add_lighting()
    add_camera()
    render()


if __name__ == "__main__":
    main()
