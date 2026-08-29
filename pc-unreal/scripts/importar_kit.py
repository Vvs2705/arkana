# -*- coding: utf-8 -*-
"""pc-unreal/scripts/importar_kit.py — traz as pecas do Meshy e MEDE tudo.

Ele faz duas coisas, e a segunda e' a que importa:

1. Importa os `.glb` de `arte/cenario/ilha-fraturada/_originais-3d/` que ainda
   nao estao no projeto. CRUS: 3 M de faces, sem Blender, sem decimacao.

2. **Mede.** Escreve `kit-medido.json` com a caixa real de cada peca e com a
   posicao do pico da ilha. Sem esses numeros nao ha' como plantar nada:

   - O Meshy exporta as pecas com ~2 m de tamanho. Escala 100 (metro -> cm)
     transformaria um arco de 1,91 m num arco de **191 m**. A escala de cada peca
     tem que sair de uma ALTURA-ALVO em metros dividida pela caixa medida, nunca
     de um numero redondo.
   - A ilha e' um Landscape do World Partition: no commandlet os proxies estao
     descarregados, mas os DESCRITORES trazem a caixa de cada pedaco. O pedaco de
     Z maximo mais alto e' onde esta' o pico — e isso e' o que confirma se o eixo
     Z do Godot virou Y do Unreal sem espelhar.

USO (headless, editor FECHADO):
  UnrealEditor-Cmd.exe <uproject> -run=pythonscript -script="<este arquivo>"
"""
import io
import json
import os
import traceback

import unreal

RAIZ = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                    "..", ".."))
ORIGEM = os.path.join(RAIZ, "arte", "cenario", "ilha-fraturada", "_originais-3d")
DESTINO = "/Game/ARKANA"
NIVEL = "/Game/ARKANA/L_IlhaFraturada"
MEDIDAS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "kit-medido.json")
DIARIO = os.path.join(os.path.dirname(os.path.abspath(__file__)), "ultimo-run.txt")

_linhas = []


def diz(msg):
    _linhas.append(str(msg))
    unreal.log(str(msg))


def _slug(nome_arquivo):
    return os.path.splitext(os.path.basename(nome_arquivo))[0]


def importar():
    if not os.path.isdir(ORIGEM):
        diz("ERRO: nao achei %s" % ORIGEM)
        return []
    glbs = sorted(f for f in os.listdir(ORIGEM) if f.lower().endswith(".glb"))
    diz("GLB NA ORIGEM: %d" % len(glbs))

    tarefas = []
    for g in glbs:
        slug = _slug(g)
        # a Interchange poe cada glb numa subpasta com o nome do arquivo
        if unreal.EditorAssetLibrary.does_directory_exist("%s/%s" % (DESTINO, slug)):
            continue
        t = unreal.AssetImportTask()
        t.filename = os.path.join(ORIGEM, g)
        t.destination_path = DESTINO
        t.automated = True
        t.save = True
        t.replace_existing = True
        tarefas.append(t)

    diz("A IMPORTAR: %s" % [_slug(t.filename) for t in tarefas])
    if tarefas:
        unreal.AssetToolsHelpers.get_asset_tools().import_asset_tasks(tarefas)
    return [_slug(g) for g in glbs]


def medir_pecas(slugs):
    """Caixa real de cada malha, em cm, mais Nanite sim/nao e contagem."""
    reg = unreal.AssetRegistryHelpers.get_asset_registry()
    fora = {}
    for slug in slugs:
        pasta = "%s/%s" % (DESTINO, slug)
        if not unreal.EditorAssetLibrary.does_directory_exist(pasta):
            diz("SEM PASTA: %s" % pasta)
            continue
        malhas = []
        for caminho in unreal.EditorAssetLibrary.list_assets(pasta, recursive=True):
            a = unreal.EditorAssetLibrary.load_asset(caminho)
            if isinstance(a, unreal.StaticMesh):
                malhas.append((caminho, a))
        if not malhas:
            diz("SEM MALHA EM: %s" % pasta)
            continue
        # a peca pode vir partida em varias malhas; a maior manda
        caminho, malha = max(malhas, key=lambda m: m[1].get_bounding_box().max.z
                             - m[1].get_bounding_box().min.z)
        cx = malha.get_bounding_box()
        nanite = malha.get_editor_property("nanite_settings")
        fora[slug] = {
            "ativo": caminho.split(".")[0],
            "malhas_na_peca": len(malhas),
            "caixa_cm": [round(cx.max.x - cx.min.x, 2),
                         round(cx.max.y - cx.min.y, 2),
                         round(cx.max.z - cx.min.z, 2)],
            "base_z_cm": round(cx.min.z, 2),
            "nanite": bool(nanite.get_editor_property("enabled")),
            "triangulos": int(malha.get_num_triangles(0)),
        }
        diz("%-38s caixa %s cm  nanite=%s  tris=%d" % (
            slug, fora[slug]["caixa_cm"], fora[slug]["nanite"],
            fora[slug]["triangulos"]))
    return fora


def medir_ilha():
    """Onde esta' o pico, medido pelos descritores do World Partition."""
    unreal.get_editor_subsystem(unreal.LevelEditorSubsystem).load_level(NIVEL)
    descs = unreal.WorldPartitionBlueprintLibrary.get_actor_descs()
    alto = None
    lo = [1e12, 1e12, 1e12]
    hi = [-1e12, -1e12, -1e12]
    for d in descs:
        if "LandscapeStreamingProxy" not in str(d.get_editor_property("native_class")):
            continue
        b = d.get_editor_property("bounds")
        mn, mx = b.min, b.max
        lo = [min(lo[0], mn.x), min(lo[1], mn.y), min(lo[2], mn.z)]
        hi = [max(hi[0], mx.x), max(hi[1], mx.y), max(hi[2], mx.z)]
        if alto is None or mx.z > alto[0]:
            alto = (mx.z, (mn.x + mx.x) * 0.5, (mn.y + mx.y) * 0.5)
    if alto is None:
        diz("ERRO: nenhum proxy de paisagem nos descritores")
        return {}
    fora = {
        "caixa_min_cm": [round(v, 1) for v in lo],
        "caixa_max_cm": [round(v, 1) for v in hi],
        "pico_z_cm": round(alto[0], 1),
        "pico_centro_do_pedaco_cm": [round(alto[1], 1), round(alto[2], 1)],
    }
    diz("ILHA: de %s a %s cm" % (fora["caixa_min_cm"], fora["caixa_max_cm"]))
    diz("PICO: Z=%.1f cm no pedaco centrado em %s cm"
        % (fora["pico_z_cm"], fora["pico_centro_do_pedaco_cm"]))
    return fora


try:
    slugs = importar()
    dados = {"pecas": medir_pecas(slugs), "ilha": medir_ilha()}
    io.open(MEDIDAS, "w", encoding="utf-8").write(
            json.dumps(dados, indent=2, ensure_ascii=False))
    diz("MEDIDAS EM: %s" % MEDIDAS)
except Exception:
    diz("EXCECAO:" + chr(10) + traceback.format_exc())
finally:
    io.open(DIARIO, "w", encoding="utf-8").write("\n".join(_linhas) + "\n")
