# -*- coding: utf-8 -*-
"""pc-unreal/scripts/pontos_de_partida.py — poe PlayerStart em cada POI da ilha.

Sem PlayerStart o nivel abre mas nao JOGA: o motor nao tem onde nascer o pawn.
E' a diferenca entre "cenario bonito" e "mapa jogavel", e por isso este script
existe separado — ele e' o ultimo passo do mapa, nao decoracao.

UM POR POI, e nao um so' no centro: num battle royale o jogador cai em lugares
diferentes, e ter um ponto por POI ja' permite testar a travessia entre eles
sem voar de camera. Os POIs vem do mesmo `Island.pois()` que veste a ilha.

A COTA VEM DE TRACO, nao de conta. A licao de 30/08: o meu heightmap e o terreno
que o Unreal construiu discordam (a ilha saiu com 1.200 m, nao 2.400). Quem
responde onde esta' o chao e' o colisor.

USO (headless, editor FECHADO):
  UnrealEditor-Cmd.exe <uproject> -run=pythonscript -script="<este arquivo>"
"""
import io
import os
import traceback

import unreal

AQUI = os.path.dirname(os.path.abspath(__file__))
DIARIO = os.path.join(AQUI, "ultimo-run.txt")
NIVEL = "/Game/ARKANA/L_IlhaFraturada"
PREFIXO = "Partida_"

# A ilha REAL, medida por traco em 30/08: 2.400 m centrada na ORIGEM.
MAPA_M = 2400.0
CENTRO = (0.0, 0.0)
K = MAPA_M / 300.0

# Os mesmos POIs de `Island.pois()`, em espaco-base.
POIS = {
    "alagado":  (-70,  60),
    "floresta": (-60, -66),
    "lago":     ( 75,  30),
    "ruinas":   ( 63, -73),
    "pico":     ( 12,  84),
    "dunas":    ( 10, -98),
    "centro":   (  0,   0),
}

# Altura do olho: o pawn nasce um pouco acima do chao para nao encravar.
ACIMA_CM = 120.0
MAR_CM = 100.0

_linhas = []


def diz(msg):
    _linhas.append(str(msg))
    unreal.log(str(msg))


def chao(mundo, x, y):
    h = unreal.SystemLibrary.line_trace_single(
            mundo, unreal.Vector(x, y, 40000.0), unreal.Vector(x, y, -40000.0),
            unreal.TraceTypeQuery.TRACE_TYPE_QUERY1, True, [],
            unreal.DrawDebugTrace.NONE, True)
    return None if h is None else h.to_dict()["impact_point"].z


def por():
    sub = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
    sub.load_level(NIVEL)
    guids = [d.get_editor_property("guid")
             for d in unreal.WorldPartitionBlueprintLibrary.get_actor_descs()]
    unreal.WorldPartitionBlueprintLibrary.load_actors(guids)
    asub = unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
    mundo = unreal.get_editor_subsystem(unreal.UnrealEditorSubsystem).get_editor_world()

    for a in asub.get_all_level_actors():
        if a.get_actor_label().startswith(PREFIXO):
            asub.destroy_actor(a)

    postos = 0
    for nome, (bx, bz) in sorted(POIS.items()):
        x = (CENTRO[0] + bx * K) * 100.0
        y = (CENTRO[1] + bz * K) * 100.0
        z = chao(mundo, x, y)
        if z is None:
            diz("%-9s SEM CHAO — pulado" % nome)
            continue
        if z < MAR_CM:
            diz("%-9s no mar (%.0f cm) — pulado" % (nome, z))
            continue
        p = asub.spawn_actor_from_class(
                unreal.PlayerStart, unreal.Vector(x, y, z + ACIMA_CM))
        p.set_actor_label(PREFIXO + nome)
        p.set_editor_property("is_spatially_loaded", False)
        diz("%-9s em (%.0f, %.0f) chao %.0f cm" % (nome, x, y, z))
        postos += 1

    diz("PONTOS DE PARTIDA: %d" % postos)
    diz("save_current_level -> %s" % sub.save_current_level())
    diz("save_dirty_packages -> %s"
        % unreal.EditorLoadingAndSavingUtils.save_dirty_packages(True, True))


try:
    por()
except Exception:
    diz("EXCECAO:" + chr(10) + traceback.format_exc())
finally:
    io.open(DIARIO, "w", encoding="utf-8").write("\n".join(_linhas) + "\n")
