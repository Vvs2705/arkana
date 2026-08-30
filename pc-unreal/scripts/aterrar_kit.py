# -*- coding: utf-8 -*-
"""pc-unreal/scripts/aterrar_kit.py — poe cada peca no chao REAL, medido por traco.

POR QUE ESTE ARQUIVO EXISTE: `semear_kit.py` calcula a cota lendo o `.r16`, e o
filtro dele foi respeitado — zero pecas sobre terreno abaixo de 0,5 m **segundo
o meu mapa**. Mas no render as pecas aparecem sobre a parte submersa da
paisagem. Ou seja: o meu mapa e o terreno que o Unreal construiu **discordam** em
algum ponto, e eu nao consegui explicar onde.

Em vez de continuar discutindo com a imagem, este script pergunta ao motor:
traca um raio de cima para baixo em cada posicao e usa o que o COLISOR do terreno
responder. E' a unica fonte que nao depende da minha aritmetica.

O que ele faz, nesta ordem:
  1. carrega TODOS os atores (o traco precisa dos proxies do terreno em memoria);
  2. traca sob cada peca do kit e grava a cota verdadeira;
  3. **reassenta** a peca nessa cota (mantendo a correcao de pivo);
  4. **APAGA** a peca cujo chao estiver abaixo do nivel do mar — peca no fundo do
     mar nao e' cenario, e' lixo;
  5. escreve `kit-aterrado.json` com o antes/depois, que e' a medida que faltava.

USO (headless, editor FECHADO):
  UnrealEditor-Cmd.exe <uproject> -run=pythonscript -script="<este arquivo>"
"""
import io
import json
import os
import traceback

import unreal

AQUI = os.path.dirname(os.path.abspath(__file__))
PLANTADO = os.path.join(AQUI, "kit-plantado.json")
MEDIDO = os.path.join(AQUI, "kit-medido.json")
RELATORIO = os.path.join(AQUI, "kit-aterrado.json")
DIARIO = os.path.join(AQUI, "ultimo-run.txt")
NIVEL = "/Game/ARKANA/L_IlhaFraturada"
PREFIXO = "Kit_"

# Abaixo disso a peca esta no mar e sai do nivel.
MAR_CM = 60.0
# De onde o raio parte e ate' onde vai. O pico da ilha tem 12.031 cm.
DE_CM = 40000.0
ATE_CM = -40000.0

_linhas = []
_CHAVE = None


def diz(msg):
    _linhas.append(str(msg))
    unreal.log(str(msg))


def carregar_tudo():
    """Traz todos os atores para a memoria — o traco precisa de colisor."""
    descs = unreal.WorldPartitionBlueprintLibrary.get_actor_descs()
    guids = [d.get_editor_property("guid") for d in descs]
    diz("DESCRITORES: %d — carregando todos" % len(guids))
    unreal.WorldPartitionBlueprintLibrary.load_actors(guids)
    return len(guids)


def _mundo():
    for obtem in (lambda: unreal.get_editor_subsystem(
                            unreal.UnrealEditorSubsystem).get_editor_world(),
                  lambda: unreal.EditorLevelLibrary.get_editor_world()):
        try:
            w = obtem()
            if w is not None:
                return w
        except Exception:
            pass
    return None


def chao(mundo, x, y, ignorar):
    """Cota do terreno em (x, y), ou None se o raio nao achar nada."""
    h = unreal.SystemLibrary.line_trace_single(
            mundo,
            unreal.Vector(x, y, DE_CM),
            unreal.Vector(x, y, ATE_CM),
            unreal.TraceTypeQuery.TRACE_TYPE_QUERY1,
            True, ignorar, unreal.DrawDebugTrace.NONE, True)
    if h is None:
        return None
    # `HitResult` no Python do UE 5.8 nao tem atributo `impact_point` nem existe
    # `GameplayStatics.break_hit_result` — medido em 30/08. O que ele tem e'
    # `to_dict()`. A chave e' descoberta uma vez e guardada em `_CHAVE`.
    global _CHAVE
    d = h.to_dict()
    if _CHAVE is None:
        for c in ("impact_point", "ImpactPoint", "location", "Location"):
            if c in d:
                _CHAVE = c
                diz("CHAVE DO PONTO DE IMPACTO: %s (de %s)" % (c, sorted(d.keys())))
                break
        if _CHAVE is None:
            diz("ERRO: HitResult sem ponto de impacto. Chaves: %s" % sorted(d.keys()))
            return None
    return d[_CHAVE].z


def aterrar():
    dados = json.load(io.open(PLANTADO, encoding="utf-8"))
    medidas = json.load(io.open(MEDIDO, encoding="utf-8"))

    sub = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
    sub.load_level(NIVEL)
    carregar_tudo()
    asub = unreal.get_editor_subsystem(unreal.EditorActorSubsystem)

    mundo = _mundo()
    diz("MUNDO: %s" % mundo)
    if mundo is None:
        diz("ERRO: sem mundo para tracar")
        return

    # indexar os atores do kit pelo rotulo
    porta = {}
    for a in asub.get_all_level_actors():
        r = a.get_actor_label()
        if r.startswith(PREFIXO):
            porta[r] = a
    diz("ATORES DO KIT EM MEMORIA: %d" % len(porta))
    if not porta:
        diz("ERRO: nenhum ator do kit carregado")
        return

    # o traco tem que IGNORAR as proprias pecas, senao ele acha a peca de cima
    ignorar = list(porta.values())

    relatorio = []
    reassentadas = 0
    apagadas = 0
    sem_chao = 0
    for slug, info in sorted(dados["pecas"].items()):
        med = medidas["pecas"][slug]
        base_z = med["base_z_cm"]
        for k, inst in enumerate(info["instancias"]):
            rot = "%s%s_%03d" % (PREFIXO, slug, k)
            a = porta.get(rot)
            if a is None:
                continue
            x, y, z_antes = inst["loc"]
            z_chao = chao(mundo, x, y, ignorar)
            if z_chao is None:
                sem_chao += 1
                continue
            if z_chao < MAR_CM:
                asub.destroy_actor(a)
                apagadas += 1
                relatorio.append({"rot": rot, "x": x, "y": y,
                                  "z_antes": z_antes, "chao": round(z_chao, 1),
                                  "acao": "apagada (no mar)"})
                continue
            e = a.get_actor_scale3d().x
            z_novo = z_chao - base_z * e - 0.06 * abs(base_z) * e
            a.set_actor_location(unreal.Vector(x, y, z_novo), False, False)
            reassentadas += 1
            relatorio.append({"rot": rot, "x": x, "y": y,
                              "z_antes": z_antes, "chao": round(z_chao, 1),
                              "z_novo": round(z_novo, 1), "acao": "reassentada"})

    diz("REASSENTADAS: %d | APAGADAS (no mar): %d | SEM CHAO: %d"
        % (reassentadas, apagadas, sem_chao))
    if relatorio:
        erros = [r["chao"] - (r["z_antes"]) for r in relatorio if "z_novo" in r]
        if erros:
            diz("DESLOCAMENTO Z (chao real - z que eu tinha): min %.0f  max %.0f  "
                "medio %.0f cm" % (min(erros), max(erros), sum(erros) / len(erros)))
    io.open(RELATORIO, "w", encoding="utf-8").write(
            json.dumps(relatorio, indent=1, ensure_ascii=False))

    diz("save_current_level -> %s" % sub.save_current_level())
    diz("save_dirty_packages -> %s"
        % unreal.EditorLoadingAndSavingUtils.save_dirty_packages(True, True))


try:
    aterrar()
except Exception:
    diz("EXCECAO:" + chr(10) + traceback.format_exc())
finally:
    io.open(DIARIO, "w", encoding="utf-8").write("\n".join(_linhas) + "\n")
