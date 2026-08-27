# tools/blender/otimizar.py — hi-poly do Meshy -> modelo de JOGO.
#
# O que faz, por peca: importa o GLB (~2M tris, texturas embutidas), DECIMA ao
# alvo de triangulos preservando UVs (o normal/albedo do proprio Meshy continua
# valendo — por isso nao ha' re-bake aqui), ESCALA para o tamanho real em
# metros (o Meshy exporta em escala arbitraria) e re-exporta GLB com texturas.
#
# O metallic NAO e' tratado aqui de proposito: o grampo pro mobile ja' vive no
# runtime (core/Pbr.gd, mesma licao do _domar_pbr do Mage) — uma fonte so'.
#
# Uso (headless):
#   blender --background --python tools/blender/otimizar.py -- \
#       <entrada.glb> <saida.glb> <tris_alvo> <maior_dimensao_m>
import bpy
import sys

argv = sys.argv[sys.argv.index("--") + 1:]
entrada, saida, tris_alvo, tamanho = argv[0], argv[1], int(argv[2]), float(argv[3])

# cena limpa (o cubo default do Blender ja' vazou para um export uma vez na
# historia de todo pipeline do mundo; aqui ele morre antes de nascer)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=entrada)

malhas = [o for o in bpy.context.scene.objects if o.type == "MESH"]
assert malhas, "GLB sem malha"

# tudo numa malha so' (o Meshy ja' entrega assim; junta por garantia)
bpy.ops.object.select_all(action="DESELECT")
for o in malhas:
    o.select_set(True)
bpy.context.view_layer.objects.active = malhas[0]
if len(malhas) > 1:
    bpy.ops.object.join()
obj = bpy.context.view_layer.objects.active

tris_atual = sum(len(p.vertices) - 2 for p in obj.data.polygons)
razao = min(1.0, tris_alvo / max(tris_atual, 1))
print(f"[otimizar] {entrada}: {tris_atual} tris -> alvo {tris_alvo} (razao {razao:.4f})")

mod = obj.modifiers.new("Decimar", "DECIMATE")
mod.decimate_type = "COLLAPSE"
mod.ratio = razao
bpy.ops.object.modifier_apply(modifier=mod.name)

# escala para o tamanho REAL: maior dimensao da caixa = <tamanho> metros
dims = obj.dimensions
maior = max(dims.x, dims.y, dims.z)
if maior > 0:
    f = tamanho / maior
    obj.scale = (f, f, f)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

# pe' no chao: a base da caixa em y=0 (o jogo posiciona pela origem)
min_z = min((obj.matrix_world @ v.co).z for v in obj.data.vertices)
obj.location.z -= min_z
bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)

tris_final = sum(len(p.vertices) - 2 for p in obj.data.polygons)
print(f"[otimizar] final: {tris_final} tris, dims {obj.dimensions.x:.2f} x "
      f"{obj.dimensions.y:.2f} x {obj.dimensions.z:.2f} m")

bpy.ops.export_scene.gltf(filepath=saida, export_format="GLB")
print(f"[otimizar] salvo: {saida}")
