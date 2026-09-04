# -*- coding: utf-8 -*-
"""pc-unreal/scripts/nomes_das_areas.py — escreve o nome de cada area NO CHAO.

PEDIDO DO DIRETOR, duas vezes: *"a ideia que as pessoas vejam os nomes da
superficie de cada area para escolher onde cair"*. Num battle royale a queda e' a
primeira decisao da partida, e ela e' cega se o jogador nao sabe o que esta'
vendo la' embaixo.

COMO: um `TextRenderActor` por area, DEITADO (pitch -90) e grande, sobre a cota
medida por traco. Texto deitado le' de cima e desaparece do nivel do olho — que
e' exatamente o que se quer: informacao de mapa na queda, cenario limpo no chao.

OS NOMES sao os SEIS oficiais do design (concept art da Ilha Fraturada), nao
invencao minha:
  Vale da Convergencia · Terracos de Nymara · Bosque Suspenso de Aeris
  Picos de Fulgar · Espinha de Basalto · Caldeira de Cineris

O ALAGADO fica SEM nome de proposito: o design nao batizou aquela baixada, e
inventar nome de area num jogo com lore e' decisao do Diretor, nao minha.

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
PREFIXO = "Nome_"

# A ilha REAL, medida por traco: 2.400 m centrada na origem.
MAPA_M = 2400.0
K = MAPA_M / 300.0

# O QUE VAI ESCRITO, e por que e' curto: o nome completo do design ("Bosque
# Suspenso de Aeris") tem 24 letras. Para ler de 800 m de altura cada letra
# precisa de ~30 m, e 24 letras dariam 720 m de largura — o nome de uma area
# invadiria a vizinha. Medido no render de 04/09 com 12 m por letra: some.
#
# A saida e' a METADE DISTINTIVA, que e' como battle royale rotula area de
# verdade — o jogador le' "NYMARA" em meio segundo de queda livre, nao uma
# frase. O nome completo continua no design e na UI; aqui vai o que se le'.
# rotulo, nome completo (para registro), centro em espaco-base de `Island.pois()`
AREAS = [
    ("CONVERGENCIA", "Vale da Convergencia",     (  0,   0)),
    ("NYMARA",       "Terracos de Nymara",       ( 75,  30)),
    ("AERIS",        "Bosque Suspenso de Aeris", (-60, -66)),
    ("FULGAR",       "Picos de Fulgar",          ( 12,  84)),
    ("BASALTO",      "Espinha de Basalto",       ( 63, -73)),
    ("CINERIS",      "Caldeira de Cineris",      ( 10, -98)),
]

# 30 m por letra. "CONVERGENCIA" (12 letras) da' 360 m de largura num mapa de
# 2.400 m: le' na queda e nao encosta na area vizinha.
TAMANHO_CM = 3000.0
ACIMA_CM = 60.0          # tira do chao para nao brigar por profundidade
MAR_CM = 60.0

_linhas = []


def diz(msg):
    _linhas.append(str(msg))
    unreal.log(str(msg))


def chao(mundo, x, y, ignorar):
    h = unreal.SystemLibrary.line_trace_single(
            mundo, unreal.Vector(x, y, 40000.0), unreal.Vector(x, y, -40000.0),
            unreal.TraceTypeQuery.TRACE_TYPE_QUERY1, True, ignorar,
            unreal.DrawDebugTrace.NONE, True)
    return None if h is None else h.to_dict()["impact_point"].z


def chao_seco_perto(mundo, x, y, ignorar):
    """Procura chao seco a partir do centro da area, em espiral.

    POR QUE PRECISA: area de AGUA tem o centro molhado por definicao — o lago e' um
    lago. Escrever o nome so' onde o centro e' seco deixaria "Terracos de Nymara"
    sem nome nenhum, que e' o oposto do pedido. O nome vai para a MARGEM, que e'
    onde o jogador pousa de qualquer forma.
    """
    z = chao(mundo, x, y, ignorar)
    if z is not None and z >= MAR_CM:
        return x, y, z
    import math
    for raio in (8000.0, 16000.0, 24000.0, 32000.0, 40000.0):
        for k in range(12):
            a = 2.0 * math.pi * k / 12.0
            px = x + math.cos(a) * raio
            py = y + math.sin(a) * raio
            pz = chao(mundo, px, py, ignorar)
            if pz is not None and pz >= MAR_CM:
                return px, py, pz
    return None, None, None


def escrever():
    sub = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
    sub.load_level(NIVEL)
    unreal.WorldPartitionBlueprintLibrary.load_actors(
            [d.get_editor_property("guid")
             for d in unreal.WorldPartitionBlueprintLibrary.get_actor_descs()])
    asub = unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
    mundo = unreal.get_editor_subsystem(unreal.UnrealEditorSubsystem).get_editor_world()

    # o traco tem que ignorar o KIT: o nome vai no CHAO, nao no topo de uma pedra
    ignorar = [a for a in asub.get_all_level_actors()
               if a.get_actor_label().startswith(("Kit_", "Partida_", PREFIXO))]
    diz("IGNORANDO NO TRACO: %d atores" % len(ignorar))

    for a in asub.get_all_level_actors():
        if a.get_actor_label().startswith(PREFIXO):
            asub.destroy_actor(a)

    postos = 0
    for rotulo, nome, (bx, bz) in AREAS:
        x = bx * K * 100.0
        y = bz * K * 100.0
        x, y, z = chao_seco_perto(mundo, x, y, ignorar)
        if z is None:
            diz("%-13s sem chao seco em 400 m — pulado" % rotulo)
            continue
        ator = asub.spawn_actor_from_class(
                unreal.TextRenderActor, unreal.Vector(x, y, z + ACIMA_CM),
                # DEITADO: pitch -90 vira a face do texto para o ceu.
                unreal.Rotator(0.0, -90.0, 0.0))
        ator.set_actor_label(PREFIXO + rotulo)
        ator.set_editor_property("is_spatially_loaded", False)
        c = ator.text_render
        c.set_editor_property("text", unreal.Text(rotulo))
        c.set_editor_property("world_size", TAMANHO_CM)
        c.set_editor_property("horizontal_alignment",
                              unreal.HorizTextAligment.EHTA_CENTER)
        c.set_editor_property("vertical_alignment",
                              unreal.VerticalTextAligment.EVRTA_TEXT_CENTER)
        c.set_editor_property("text_render_color",
                              unreal.Color(r=255, g=232, b=150, a=255))
        diz("%-13s (%s) em (%.0f, %.0f) chao %.0f cm" % (rotulo, nome, x, y, z))
        postos += 1

    diz("NOMES ESCRITOS: %d de %d" % (postos, len(AREAS)))
    diz("save_current_level -> %s" % sub.save_current_level())
    diz("save_dirty_packages -> %s"
        % unreal.EditorLoadingAndSavingUtils.save_dirty_packages(True, True))


try:
    escrever()
except Exception:
    diz("EXCECAO:" + chr(10) + traceback.format_exc())
finally:
    io.open(DIARIO, "w", encoding="utf-8").write("\n".join(_linhas) + "\n")
