# -*- coding: utf-8 -*-
"""pc-unreal/scripts/plantar_kit.py — planta o kit na ilha. So' obedece.

Este arquivo NAO decide nada. Quem decide onde cada peca vai e' o
`semear_kit.py`, que roda em Python puro lendo o heightmap. Aqui e' so'
execucao: ler o JSON, criar as instancias, dar colisao, salvar.

UM ATOR POR PECA, e nao um componente instanciado. A primeira versao daqui
tentou `HierarchicalInstancedStaticMesh`, que seria o equivalente do MultiMesh
do mobile — mas o `Actor` exposto ao Python do UE 5.8 nao tem
`add_component_by_class` nem `add_instance_component`, entao nao ha' como pendurar
componente em ator novo por script (medido em 28/08).

O caminho que FUNCIONA e' `StaticMeshActor`, um por peca. E ele nao e' consolo:
sendo atores espaciais, o World Partition os divide por regiao e carrega so' o
que esta' perto — num mapa de 2.400 m isso e' MELHOR do que um componente unico
que fica sempre inteiro na memoria. O preco sao ~1.300 pacotes externos, que e'
o tamanho normal de um nivel de verdade.

A COLISAO, e as tres decisoes que ela exige:

- **Malha do Meshy nao vem com colisao.** Sem tratar, o Unreal cai em colisao
  COMPLEXA (por poligono) — em peca de 3 M de faces isso e' inaceitavel.
- **Passagem tem que ser atravessavel.** Um arco com caixa em volta deixa de ser
  arco e vira parede. Por isso arco e ponte levam decomposicao convexa: varios
  cascos que aproximam o vao.
- **Grama nao bloqueia.** Junco com colisao seria o jogador travando em mato.

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
DIARIO = os.path.join(AQUI, "ultimo-run.txt")
NIVEL = "/Game/ARKANA/L_IlhaFraturada"
PREFIXO = "Kit_"

# Peca que o jogador ATRAVESSA: casco convexo, nunca caixa.
ATRAVESSAVEL = ("19-arco-calcario-nymara", "20-ponte-raiz-aeris")
# Peca que NAO bloqueia nada.
SEM_COLISAO = ("31-juncos-nymara",)

_linhas = []


def diz(msg):
    _linhas.append(str(msg))
    unreal.log(str(msg))


def _usar_simples_como_complexa(malha):
    """Trava a malha em colisao SIMPLES.

    A bandeira NAO fica na StaticMesh — fica no `body_setup` dela. Sem isto o
    Unreal cai em colisao COMPLEXA (por poligono), e por poligono numa peca de
    3 M de faces e' o tipo de custo que derruba servidor.
    """
    corpo = malha.get_editor_property("body_setup")
    if corpo is None:
        diz("  AVISO: sem body_setup — colisao fica no padrao")
        return
    corpo.set_editor_property("collision_trace_flag",
                              unreal.CollisionTraceFlag.CTF_USE_SIMPLE_AS_COMPLEX)


def preparar_colisao(malha, slug):
    """Da' colisao SIMPLES a' malha, do jeito que aquela peca precisa."""
    if slug in SEM_COLISAO:
        diz("  colisao: NENHUMA (%s nao bloqueia) — desligada no componente" % slug)
        return
    # No UE5 as funcoes de malha migraram de `EditorStaticMeshLibrary` para o
    # `StaticMeshEditorSubsystem`. Tentar o novo primeiro e cair no antigo evita
    # que uma subida de versao do motor derrube a plantacao inteira.
    lib = unreal.get_editor_subsystem(unreal.StaticMeshEditorSubsystem)
    if lib is None or not hasattr(lib, "add_simple_collisions"):
        lib = unreal.EditorStaticMeshLibrary
    try:
        lib.remove_collisions(malha)
    except Exception:
        pass
    if slug in ATRAVESSAVEL:
        # 12 cascos aproximam o vao do arco. Menos que isso fecha a passagem.
        lib.set_convex_decomposition_collisions(malha, 12, 24, 500000)
        diz("  colisao: 12 cascos convexos (peca atravessavel)")
    else:
        lib.add_simple_collisions(malha, unreal.ScriptingCollisionShapeType.NDOP18)
        diz("  colisao: NDOP18")
    _usar_simples_como_complexa(malha)
    unreal.EditorAssetLibrary.save_loaded_asset(malha)


def _componente_instanciado(ator):
    """Cria o HISM no ator, tentando os caminhos que a API oferece.

    A criacao de componente por Python mudou de forma entre versoes do motor, e
    um `AttributeError` aqui derrubaria a plantacao inteira sem dizer por que.
    Entao: tenta, e DIZ qual caminho funcionou — informacao que vale ouro no dia
    em que o projeto subir de versao.
    """
    classe = unreal.HierarchicalInstancedStaticMeshComponent
    try:
        c = ator.add_component_by_class(classe, False, unreal.Transform(), False)
        if c is not None:
            diz("  componente via add_component_by_class")
            return c
    except Exception as e:
        diz("  add_component_by_class falhou: %s" % e)
    try:
        c = unreal.new_object(classe, ator)
        ator.add_instance_component(c)
        c.attach_to_component(ator.root_component, "",
                              unreal.AttachmentRule.KEEP_RELATIVE,
                              unreal.AttachmentRule.KEEP_RELATIVE,
                              unreal.AttachmentRule.KEEP_RELATIVE, False)
        c.register_component()
        diz("  componente via new_object")
        return c
    except Exception as e:
        diz("  new_object falhou: %s" % e)
    return None


def limpar_antigos(asub):
    """Apaga o kit da rodada anterior, para a rodada ser idempotente.

    Pelos DESCRITORES do World Partition, e nao por `get_all_level_actors()`:
    os atores do kit sao ESPACIAIS, e num commandlet nenhuma regiao esta'
    carregada — a lista de atores viria vazia e a limpeza nao limparia nada.
    Foi essa exata armadilha que empilhou tres planos de mar em 28/08.
    """
    guids = []
    for d in unreal.WorldPartitionBlueprintLibrary.get_actor_descs():
        if str(d.get_editor_property("label")).startswith(PREFIXO):
            guids.append(d.get_editor_property("guid"))
    if not guids:
        return
    unreal.WorldPartitionBlueprintLibrary.load_actors(guids)
    n = 0
    for a in asub.get_all_level_actors():
        if a.get_actor_label().startswith(PREFIXO):
            asub.destroy_actor(a)
            n += 1
    diz("KIT ANTERIOR APAGADO: %d de %d descritores" % (n, len(guids)))


def plantar():
    if not os.path.exists(PLANTADO):
        diz("ERRO: falta %s — rode semear_kit.py" % PLANTADO)
        return False
    dados = json.load(io.open(PLANTADO, encoding="utf-8"))

    sub = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
    sub.load_level(NIVEL)
    asub = unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
    limpar_antigos(asub)

    total = 0
    for slug, info in sorted(dados["pecas"].items()):
        malha = unreal.EditorAssetLibrary.load_asset(info["ativo"])
        if malha is None:
            diz("SEM ATIVO: %s (%s)" % (slug, info["ativo"]))
            continue
        diz("%s: %d instancias" % (slug, len(info["instancias"])))
        preparar_colisao(malha, slug)

        for k, inst in enumerate(info["instancias"]):
            L = inst["loc"]
            e = inst["escala"]
            ator = asub.spawn_actor_from_class(
                    unreal.StaticMeshActor,
                    unreal.Vector(L[0], L[1], L[2]),
                    unreal.Rotator(0.0, 0.0, inst["yaw"]))
            ator.set_actor_label("%s%s_%03d" % (PREFIXO, slug, k))
            ator.set_actor_scale3d(unreal.Vector(e, e, e))
            comp = ator.static_mesh_component
            comp.set_editor_property("mobility", unreal.ComponentMobility.STATIC)
            comp.set_static_mesh(malha)
            if slug in SEM_COLISAO:
                comp.set_collision_enabled(unreal.CollisionEnabled.NO_COLLISION)
        total += len(info["instancias"])

    diz("TOTAL PLANTADO: %d instancias em %d tipos" % (total, len(dados["pecas"])))
    diz("save_current_level -> %s" % sub.save_current_level())
    diz("save_dirty_packages -> %s"
        % unreal.EditorLoadingAndSavingUtils.save_dirty_packages(True, True))
    return True


try:
    plantar()
except Exception:
    diz("EXCECAO:" + chr(10) + traceback.format_exc())
finally:
    io.open(DIARIO, "w", encoding="utf-8").write("\n".join(_linhas) + "\n")
