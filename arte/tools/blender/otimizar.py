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
#       <entrada.glb> <saida.glb> <tris_alvo> <maior_dimensao_m> [tex_max_px] [entradas_extras] [giro_x_graus]
#
# 12/09/2026: a DECIMACAO COLLAPSE abria buracos (a rocha saia estilhacada no jogo). A topologia agora vem
# do REMESH do site da Meshy (10K/3K, fechada); aqui passa-se tris_alvo = 0 (nao decima) e tex_max_px
# (ex.: 1024) — a textura cai para JPEG desse tamanho e so' a cor base fica: o KitCenario do Unity so' le'
# baseColor, e normal/metal 2K embutidos iam para o APK a toa.
import bpy
import sys

argv = sys.argv[sys.argv.index("--") + 1:]
entrada, saida, tris_alvo, tamanho = argv[0], argv[1], int(argv[2]), float(argv[3])
tex_max = int(argv[4]) if len(argv) > 4 else 0
# 6o argumento (12/09): entradas do BSDF que ficam ALEM da cor base, separadas por virgula — o bau e as luvas mantem
# a emissao ("Emission Color,Emission Strength"): gema e brasa acesas sao a leitura do loot, e o pos agora tem bloom
manter = {"Base Color"} | (set(argv[5].split(",")) if len(argv) > 5 and argv[5] else set())
# 7o argumento (12/09): giro em X (graus) antes de medir — a luva do cajado veio DEITADA da Meshy (dedos em +Z) e
# as outras em pe'; -90 poe os dedos para cima
giro_x = float(argv[6]) if len(argv) > 6 else 0.0

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

if tris_alvo > 0:
    # SOLDA antes de decimar (onda 5B, 12/09): o glb do remesh chega em ~900 ILHAS soltas (o glTF duplica o vertice em
    # toda costura de UV/normal e o importador nao junta, nem com merge_vertices=True). O COLLAPSE decima cada ilha
    # sozinha: era ISSO que estilhacava a rocha, nao a razao. Solda por distancia, limpa a normal importada, decima e
    # refaz a normal por angulo. So' quando decima: com tris_alvo = 0 a normal do remesh fica como veio.
    import bmesh, math
    bm = bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)
    bm.to_mesh(obj.data); bm.free()
    bpy.ops.mesh.customdata_custom_splitnormals_clear()
    tris_atual = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    razao = min(1.0, tris_alvo / max(tris_atual, 1))
    mod = obj.modifiers.new("Decimar", "DECIMATE")
    mod.decimate_type = "COLLAPSE"
    mod.ratio = razao
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(40))   # KNOB: aresta de pedra viva x face lisa

if tex_max > 0:
    # so' a cor base (e o que `manter` pedir) fica ligada no BSDF; o resto sai do export
    for mat in bpy.data.materials:
        if not mat.use_nodes:
            continue
        for no in mat.node_tree.nodes:
            if no.type != "BSDF_PRINCIPLED":
                continue
            for entrada_bsdf in no.inputs:
                if entrada_bsdf.name not in manter:
                    for link in list(entrada_bsdf.links):
                        mat.node_tree.links.remove(link)
    for img in bpy.data.images:
        w, h = img.size
        if max(w, h) > tex_max:
            k = tex_max / max(w, h)
            img.scale(max(1, int(w * k)), max(1, int(h * k)))

if giro_x:
    # gira a MALHA (o importador do glTF deixa o objeto em modo quaternio: rotation_euler seria ignorado calado)
    import math
    from mathutils import Matrix
    obj.data.transform(Matrix.Rotation(math.radians(giro_x), 4, "X"))
    obj.data.update()

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

if tex_max > 0:
    bpy.ops.export_scene.gltf(filepath=saida, export_format="GLB", export_image_format="JPEG")
else:
    bpy.ops.export_scene.gltf(filepath=saida, export_format="GLB")
print(f"[otimizar] salvo: {saida}")
