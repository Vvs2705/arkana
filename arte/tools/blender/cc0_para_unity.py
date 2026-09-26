"""Pecas CC0 do Kenney -> Resources do Unity, no TAMANHO do jogo (Blender sem janela, 26/09/2026).
Os GLB do Kenney apontam para uma textura externa (Textures/colormap.png) e vem em escala de "ladrilho": este script
importa, junta, escala para a medida real (a MAIOR dimensao vira `metros`), assenta a base no chao e centraliza em X/Y,
e exporta GLB com a textura EMBUTIDA em mobile-unity/Assets/_Arkana/Resources/cc0-<nome>.glb.
Uso: blender -b --factory-startup --python arte/tools/blender/cc0_para_unity.py
Primeiro:  python arte/tools/cc0.py  (baixa os pacotes em arte/cenario/cc0/)."""
import os
import bpy
from mathutils import Matrix, Vector

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
FONTE = os.path.join(RAIZ, "arte", "cenario", "cc0", "survival-kit", "Models", "GLB format")
DESTINO = os.path.join(RAIZ, "mobile-unity", "Assets", "_Arkana", "Resources")
# nome do Kenney: metros da MAIOR dimensao no jogo
PECAS = {
    "campfire-pit": 1.8, "campfire-stand": 2.2, "structure-canvas": 5.5, "tent-canvas": 4.0, "bedroll": 2.0,
    "workbench": 2.2, "signpost": 2.4, "tool-axe": 0.9, "tool-pickaxe": 1.0, "resource-wood": 1.6,
    "resource-planks": 2.0, "box-large-open": 1.3, "bucket": 0.5, "chest": 1.1,
}


def converter(nome, metros):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=os.path.join(FONTE, nome + ".glb"))
    objs = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    for o in objs:
        o.data.transform(o.matrix_world)
        o.matrix_world = Matrix.Identity(4)
        o.parent = None
    for o in bpy.context.scene.objects:
        o.select_set(o in objs)
    bpy.context.view_layer.objects.active = objs[0]
    if len(objs) > 1:
        bpy.ops.object.join()
    obj = bpy.context.active_object
    for o in list(bpy.context.scene.objects):
        if o != obj:
            bpy.data.objects.remove(o, do_unlink=True)
    vs = [v.co for v in obj.data.vertices]
    mn = Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs)))
    mx = Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)))
    esc = metros / max(mx - mn)
    centro = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z))
    obj.data.transform(Matrix.Scale(esc, 4) @ Matrix.Translation(-centro))
    obj.name = "cc0_" + nome
    for img in bpy.data.images:   # a textura externa entra no GLB
        if img.filepath:
            img.pack()
    saida = os.path.join(DESTINO, f"cc0-{nome}.glb")
    bpy.ops.export_scene.gltf(filepath=saida, export_format="GLB", use_selection=False, export_apply=True)
    d = obj.dimensions
    print(f"OK {nome}: {d.x:.2f} x {d.y:.2f} x {d.z:.2f} m -> {saida}")


for n, m in PECAS.items():
    converter(n, m)
