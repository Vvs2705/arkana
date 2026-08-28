# -*- coding: utf-8 -*-
"""pc-unreal/scripts/limpar_duplicatas.py — apaga atores repetidos do nivel.

POR QUE EXISTE: `get_all_level_actors()` num commandlet **so' devolve os atores
NAO-espaciais**, porque nenhuma regiao do World Partition esta' carregada. Uma
guarda do tipo "ja' existe um Mar?" escrita em cima dessa lista nunca ve' o que
ja' existe, e cada rodada do script de montagem empilha mais uma copia.

O `material_ilha.py` ja' foi corrigido na raiz (mar e exposicao nascem
NAO-espaciais, entao a guarda passa a enxerga-los). Este arquivo limpa o que a
versao defeituosa deixou para tras — e fica no repo porque a mesma armadilha vai
voltar no dia em que alguem espalhar peca por script.

COMO ELE ENXERGA O QUE A OUTRA LISTA NAO MOSTRA: pelos DESCRITORES do World
Partition (`get_actor_descs`), que existem sem a regiao estar carregada. Achado o
alvo, `load_actors` traz o ator para a memoria e ai' da' para apagar.

USO (headless, com o editor FECHADO):
  UnrealEditor-Cmd.exe <uproject> -run=pythonscript -script="<este arquivo>"
"""
import io
import os
import traceback

import unreal

NIVEL = "/Game/ARKANA/L_IlhaFraturada"
# Os globais do nivel: um de cada, sempre. Se aparecer mais, sobra o primeiro.
UNICOS = ("Mar", "Exposicao")

DIARIO = os.path.join(os.path.dirname(os.path.abspath(__file__)), "ultimo-run.txt")
_linhas = []


def diz(msg):
    _linhas.append(str(msg))
    unreal.log(str(msg))


def limpar():
    sub = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
    sub.load_level(NIVEL)
    asub = unreal.get_editor_subsystem(unreal.EditorActorSubsystem)

    descs = unreal.WorldPartitionBlueprintLibrary.get_actor_descs()
    diz("DESCRITORES NO NIVEL: %d" % len(descs))

    alvos = []
    for d in descs:
        rotulo = str(d.get_editor_property("label"))
        if rotulo in UNICOS:
            alvos.append((rotulo, d))
    diz("ALVOS ACHADOS: %s" % [a[0] for a in alvos])
    if not alvos:
        diz("nada a limpar")
        return

    unreal.WorldPartitionBlueprintLibrary.load_actors(
            [d.get_editor_property("guid") for _, d in alvos])

    vistos = set()
    apagados = 0
    for a in asub.get_all_level_actors():
        r = a.get_actor_label()
        if r not in UNICOS:
            continue
        if r in vistos:
            asub.destroy_actor(a)
            apagados += 1
            continue
        vistos.add(r)
        # o sobrevivente vira NAO-espacial, que e' o que ele sempre devia ser:
        # mar e exposicao valem no mapa inteiro e nao podem sair de streaming.
        a.set_editor_property("is_spatially_loaded", False)
        diz("MANTIDO e tornado nao-espacial: %s" % r)

    diz("APAGADOS: %d" % apagados)
    diz("save_current_level -> %s" % sub.save_current_level())
    diz("save_dirty_packages -> %s"
        % unreal.EditorLoadingAndSavingUtils.save_dirty_packages(True, True))


try:
    limpar()
except Exception:
    diz("EXCECAO:" + chr(10) + traceback.format_exc())
finally:
    io.open(DIARIO, "w", encoding="utf-8").write("\n".join(_linhas) + "\n")
