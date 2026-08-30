# -*- coding: utf-8 -*-
"""pc-unreal/scripts/sondar_terreno.py — mede o terreno REAL, por traco, e grava.

POR QUE ISTO SUBSTITUI A LEITURA DO `.r16`:

O `.r16` e' o que eu MANDEI para o Unreal. Nao e' o que o Unreal CONSTRUIU. Medido
em 30/08: pedi 2.400 m com 2017 vertices e ele construiu **1.200 m** (16
componentes de 63 quads = 1009 vertices), reamostrando a altura. Semear em cima
do arquivo de entrada colocou metade das pecas no mar e o "pico" num morro de
18,8 m.

A licao, e ela vale para tudo daqui para a frente:

> **O arquivo que entra nao e' o mundo que sai.** Quem responde onde esta' o chao
> e' o COLISOR, e a unica forma honesta de perguntar e' tracar.

Este script traca uma grade sobre a ilha inteira e grava `terreno-medido.json`:
altura real em cada no', mais o envelope da ilha. O `semear_kit.py` passa a ler
DAQUI.

USO (headless, editor FECHADO):
  UnrealEditor-Cmd.exe <uproject> -run=pythonscript -script="<este arquivo>"
"""
import io
import json
import os
import traceback

import unreal

AQUI = os.path.dirname(os.path.abspath(__file__))
SAIDA = os.path.join(AQUI, "terreno-medido.json")
DIARIO = os.path.join(AQUI, "ultimo-run.txt")
NIVEL = "/Game/ARKANA/L_IlhaFraturada"

# A grade. 241 x 241 = 58.081 tracos. Passo de 5 m num mapa de 1.200 m: fino o
# bastante para plantar peca de 3 a 26 m, grosso o bastante para rodar em
# minutos. Mais fino que isso e' precisao que a peca nao enxerga.
LADO = 241
DE_CM = 40000.0
ATE_CM = -40000.0

_linhas = []


def diz(msg):
    _linhas.append(str(msg))
    unreal.log(str(msg))


def envelope():
    """Onde a paisagem comeca e acaba, pelos limites dos proxies."""
    lo = [1e12, 1e12]
    hi = [-1e12, -1e12]
    for d in unreal.WorldPartitionBlueprintLibrary.get_actor_descs():
        if "LandscapeStreamingProxy" not in str(d.get_editor_property("native_class")):
            continue
        b = d.get_editor_property("bounds")
        lo = [min(lo[0], b.min.x), min(lo[1], b.min.y)]
        hi = [max(hi[0], b.max.x), max(hi[1], b.max.y)]
    return lo, hi


def sondar():
    sub = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
    sub.load_level(NIVEL)
    guids = [d.get_editor_property("guid")
             for d in unreal.WorldPartitionBlueprintLibrary.get_actor_descs()]
    unreal.WorldPartitionBlueprintLibrary.load_actors(guids)
    mundo = unreal.get_editor_subsystem(unreal.UnrealEditorSubsystem).get_editor_world()

    # o traco tem que ignorar o KIT, senao mede o topo de uma pedra como chao
    asub = unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
    ignorar = [a for a in asub.get_all_level_actors()
               if a.get_actor_label().startswith(("Kit_", "Partida_"))]
    diz("IGNORANDO NO TRACO: %d atores do kit" % len(ignorar))

    lo, hi = envelope()
    diz("ENVELOPE DOS PROXIES: %s ate %s cm" % ([round(v) for v in lo],
                                               [round(v) for v in hi]))

    passo_x = (hi[0] - lo[0]) / (LADO - 1)
    passo_y = (hi[1] - lo[1]) / (LADO - 1)
    alturas = []
    seco = 0
    for j in range(LADO):
        y = lo[1] + j * passo_y
        linha = []
        for i in range(LADO):
            x = lo[0] + i * passo_x
            h = unreal.SystemLibrary.line_trace_single(
                    mundo, unreal.Vector(x, y, DE_CM), unreal.Vector(x, y, ATE_CM),
                    unreal.TraceTypeQuery.TRACE_TYPE_QUERY1, True, ignorar,
                    unreal.DrawDebugTrace.NONE, True)
            if h is None:
                linha.append(None)          # fora da paisagem
            else:
                z = round(h.to_dict()["impact_point"].z, 1)
                linha.append(z)
                if z > 0:
                    seco += 1
        alturas.append(linha)
        if j % 40 == 0:
            diz("  linha %d/%d" % (j, LADO))

    vivos = [z for lin in alturas for z in lin if z is not None]
    dados = {
        "lado": LADO,
        "canto_cm": [lo[0], lo[1]],
        "passo_cm": [passo_x, passo_y],
        "alturas_cm": alturas,
    }
    io.open(SAIDA, "w", encoding="utf-8").write(json.dumps(dados))
    diz("TRACOS COM CHAO: %d de %d | ACIMA DO MAR: %d (%.1f%%)"
        % (len(vivos), LADO * LADO, seco, 100.0 * seco / (LADO * LADO)))
    if vivos:
        diz("COTA REAL: min %.0f cm  max %.0f cm" % (min(vivos), max(vivos)))
    diz("SAIDA: %s" % SAIDA)


try:
    sondar()
except Exception:
    diz("EXCECAO:" + chr(10) + traceback.format_exc())
finally:
    io.open(DIARIO, "w", encoding="utf-8").write("\n".join(_linhas) + "\n")
