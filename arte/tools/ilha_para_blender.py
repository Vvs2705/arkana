"""Leva a ilha do Documento Mestre montada no Unity para o Blender (25/09/2026).

Passo 1 (Unity sem janela; o editor do Unity FECHADO — dois Unity no mesmo projeto se travam):
    set ARKANA_EXPORT=<pasta>
    Unity.exe -batchmode -projectPath mobile-unity -executeMethod Arkana.EditorTools.ExportarIlhaMestre.Exportar -quit
    -> pecas.txt, arvores.txt, chao.png, conferencia-vila.png
Passo 2 (este script):  python arte/tools/ilha_para_blender.py <pasta>
    -> relevo16.png (1025 x 1025, 16 bits, norte em cima) e planta-<regiao>.gltf: cada peca vira um NO glTF
       (nome P:<fonte> ou B:<Cube|Cylinder|Sphere>, matriz, extras com a cor). O importador glTF do Blender faz a
       conversao de eixos, a mesma que ele faz nas pecas do kit — so' sobra um giro de 180 graus em Z no mundo
       (conferido 25/09 pela foto de cima da vila, Unity x Blender).
Passo 3 (Blender, pelo MCP ou pelo console): ver o bloco MONTAGEM no fim deste arquivo.

Conversoes (medidas no codigo-fonte do glTFast: ele NEGA o X ao importar): no glTF, matriz = F * W_unity * F, com
F = diag(-1, 1, 1, 1). No Blender, matriz = Rz(180) * (o que o importador entrega).
Relevo: fonte unica = mobile-unity/Assets/_Arkana/Resources/ilha-mestre-altura.bytes (2049^2, -40..660 m); o
Blender recebe 1 ponto a cada 2 (4,7 m) porque a malha cheia (4,2 M de vertices) nao cabe numa maquina de 8 GB.
"""
import collections
import json
import math
import os
import sys

import numpy as np
from PIL import Image

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ALTURA = os.path.join(RAIZ, "mobile-unity", "Assets", "_Arkana", "Resources", "ilha-mestre-altura.bytes")


def fwf(m):
    """glTF = F * W * F (F = diag(-1,1,1,1)); m em colunas (indice = col*4 + linha)."""
    out = list(m)
    for col in range(4):
        for lin in range(4):
            if (lin == 0) != (col == 0):
                out[col * 4 + lin] = -out[col * 4 + lin]
    return out


def main(pasta):
    a = np.fromfile(ALTURA, dtype="<u2").reshape(2049, 2049)
    Image.fromarray(a[::2, ::2][::-1].astype(np.uint16)).save(os.path.join(pasta, "relevo16.png"))
    grupos = collections.defaultdict(list)
    for linha in open(os.path.join(pasta, "pecas.txt"), encoding="utf-8"):
        tipo, grupo, fonte, mat, cor = linha.rstrip("\n").split("|")
        no = {"name": f"{tipo}:{fonte}", "matrix": fwf([float(v) for v in mat.split(",")]),
              "extras": {"tipo": tipo, "fonte": fonte}}
        if cor:
            no["extras"]["cor"] = [float(c) for c in cor.split(",")]
        grupos[grupo].append(no)
    arv = []
    for linha in open(os.path.join(pasta, "arvores.txt"), encoding="utf-8"):
        fonte, x, y, z, rot, esc = linha.strip().split("|")
        x, y, z, rot, esc = map(float, (x, y, z, rot, esc))
        c, s = math.cos(rot), math.sin(rot)
        W = [c * esc, 0, -s * esc, 0, 0, esc, 0, 0, s * esc, 0, c * esc, 0, x, y, z, 1]   # T*Ry*S do Unity
        arv.append({"name": f"P:{fonte}", "matrix": fwf(W), "extras": {"tipo": "P", "fonte": fonte}})
    for k in range(0, len(arv), 3000):
        grupos[f"ARKANA_21_Vegetacao_{k // 3000 + 1}"] = arv[k:k + 3000]
    for g, nos in grupos.items():
        doc = {"asset": {"version": "2.0", "generator": "arkana-planta"}, "scene": 0,
               "scenes": [{"name": g, "nodes": list(range(len(nos)))}], "nodes": nos}
        with open(os.path.join(pasta, f"planta-{g}.gltf"), "w", encoding="utf-8") as f:
            json.dump(doc, f)
        print(f"{g}: {len(nos)}")


if __name__ == "__main__":
    main(sys.argv[1])

# ------------------------------------------------------------------------------------------------ MONTAGEM (Blender)
# Rodado em 25/09 pelo MCP do Blender, em etapas (o Diretor acompanhou na tela). Cada etapa e' idempotente.
#  1. Terreno: grid 1024 x 1024 subdivisoes, escala (4800, 4400, 1), z = -40; modificador DISPLACE com a imagem
#     relevo16.png (Non-Color, EXTEND), coordenadas UV, direcao Z, mid_level 0, strength 700. Material: chao.png por UV.
#  2. Agua: plano do mar em 0; disco do lago (elipse 450 x 350 * 1,25, centro (0,150)) em 105; fita do rio pelos
#     pontos RIO de arte/tools/ilha_mestre.py (nivel + 0,15, meia-largura + 7).
#  3. Kit: cada GLB de mobile-unity/Assets/_Arkana/Resources vira a colecao "molde_<nome>" dentro de
#     ARKANA_22_KIT_MODULAR (oculta). Arvores usam as versoes -lod1 (leves).
#  4. Plantas: import_scene.gltf(planta-<regiao>.gltf); para cada no: matrix_world = Rz(180) @ matrix_world;
#     "P:" vira instancia de colecao (instance_type COLLECTION) do molde; "B:" vira objeto com a malha primitiva
#     (cubo 1 m, cilindro r 0,5 x 2 m, esfera r 0,5) e a cor do extras (sRGB -> linear) no slot do OBJETO.
#  5. Subterraneo (25/09, v002): python arte/tools/subterraneo.py <pasta> -> subterraneo-R10/R11/R12.gltf (cascas dos
#     vazios, normais para dentro, material com backface culling: de fora se ve o interior) + buracos.png. Importar cada
#     glTF em ARKANA_R10_Caverna_Profunda / R11_Rede_Tuneis / R12_Caverna_Subterranea (filhas de ARKANA_R01_R12_REGIOES).
#     Bocas no terreno: atributo booleano "buraco" por face do grid (indice = j*1024 + i, i = x, j = y a partir do SO; cada
#     face cobre 2 x 2 pixels de buracos.png) + modificador Geometry Nodes ARK_Buracos_Terreno (Delete Geometry por esse
#     atributo). Para inspecionar: ocultar ARKANA_01_TERRENO/20_PEDRAS/21_VEGETACAO.
#  6. Salvar em arte/cenario/documento-mestre/ARKANA_Ilha_Mestre_vNNN.blend (fora do git: ~110 MB, reproduzivel).
