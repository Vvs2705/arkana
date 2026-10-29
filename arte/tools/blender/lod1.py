# tools/blender/lod1.py — o "-lod1" de uma peca do cenario: a MESMA malha decimada, sem mexer em escala, giro ou origem.
#
# Por que nao o otimizar.py (29/10/2026): ele aplica escala e "pe' no chao" e, na arvore do Tripo (no' com giro de -90 em
# X do glTF), o LOD1 saia DEITADO (0,48 m de altura contra 1,0). Aqui o objeto fica como veio: solda, decima, exporta —
# o LOD0 e o LOD1 caem no mesmo lugar dentro do LODGroup do Terrain.
#
# Uso (headless):
#   blender --background --factory-startup --python tools/blender/lod1.py -- <entrada.glb> <saida.glb> <tris_alvo> [suave_graus]
import bmesh
import bpy
import math
import sys

argv = sys.argv[sys.argv.index("--") + 1 :]
entrada, saida, alvo = argv[0], argv[1], int(argv[2])
suave = float(argv[3]) if len(argv) > 3 else 80.0  # folhagem ~80; pedra ~40

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=entrada)
for obj in [o for o in bpy.context.scene.objects if o.type == "MESH"]:
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    # solda as costuras do glTF antes do COLLAPSE (sem isso cada ilha decima sozinha e a malha estilhaca — licao de 12/09)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)
    bm.to_mesh(obj.data)
    bm.free()
    bpy.ops.mesh.customdata_custom_splitnormals_clear()
    tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    mod = obj.modifiers.new("Decimar", "DECIMATE")
    mod.decimate_type = "COLLAPSE"
    mod.ratio = min(1.0, alvo / max(tris, 1))
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(suave))
    print(
        f"[lod1] {obj.name}: {tris} -> {sum(len(p.vertices) - 2 for p in obj.data.polygons)} tris"
    )
    obj.select_set(False)

bpy.ops.export_scene.gltf(filepath=saida, export_format="GLB", export_apply=True)
print(f"[lod1] salvo: {saida}")
