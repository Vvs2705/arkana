# -*- coding: utf-8 -*-
"""pc-unreal/scripts/material_ilha.py — o material da Ilha Fraturada, em codigo.

POR QUE EM CODIGO E NAO NO EDITOR: um auto-material de paisagem e' ~20 nos, e
arrastar no' e' o tipo de trabalho que ninguem consegue repetir nem revisar. Aqui
o grafo inteiro cabe numa tela, entra no git e roda igual toda vez.

A REGRA DO MATERIAL, que e' a mesma que o toon.gdshader do mobile ja' provou:
a ROCHA NAO E' PINTADA, ela e' DEDUZIDA DA INCLINACAO. Onde a encosta e' ingreme
o chao vira pedra sozinho; onde e' plano e baixo, vira areia de praia; no meio,
grama. Nenhuma camada precisa ser pintada a mao em 3,4 km de mapa — e por isso a
ilha inteira fica vestida no instante em que o heightmap muda.

Lição herdada do mobile (`Island.gd`, defeito medido em 27/08): o limiar de rocha
tem que cair DENTRO da faixa real de inclinacao do terreno. La', `ny < 0,74` num
terreno cuja encosta mais ingreme media 0,829 fez a rocha NUNCA aparecer. Aqui os
limiares sao 0,86/0,94 de normal-Z e ficam como KNOB explicito no material.

USO (headless):
  UnrealEditor-Cmd.exe <uproject> -run=pythonscript -script="<este arquivo>"
"""
import io
import os
import traceback

import unreal

# DIARIO EM ARQUIVO, e nao `unreal.log`. Medido em 28/08: rodando por
# `-run=pythonscript`, nem `print()` nem `unreal.log()` aparecem na saida do
# commandlet — so' os ERROS aparecem. Um script que "roda com sucesso" e nao faz
# nada e' o pior modo de falhar que existe, entao ele escreve o que fez ao lado
# de si mesmo e o resultado se le' com `cat`.
DIARIO = os.path.join(os.path.dirname(os.path.abspath(__file__)), "ultimo-run.txt")
_linhas = []


def diz(msg):
    _linhas.append(str(msg))
    unreal.log(str(msg))


def fechar_diario():
    io.open(DIARIO, "w", encoding="utf-8").write("\n".join(_linhas) + "\n")


PASTA = "/Game/ARKANA/Materiais"
NOME = "M_IlhaFraturada"
NIVEL = "/Game/ARKANA/L_IlhaFraturada"

# As cores. Nao sao decoracao: sao as tres leituras que o jogador precisa fazer
# de 200 m de altura na queda — praia, campo, penhasco (GDD §10).
AREIA = (0.76, 0.68, 0.50)
GRAMA = (0.33, 0.44, 0.24)
ROCHA = (0.40, 0.38, 0.36)

# KNOBS da inclinacao, em normal-Z (1,0 = chao plano, 0,0 = parede).
# Subir ROCHA_ATE espalha pedra por encosta mais mansa; descer confina a pedra
# ao penhasco. Ver a licao do `ny < 0,74` no cabecalho.
ROCHA_DE = 0.86
ROCHA_ATE = 0.94

# KNOBS da praia, em METROS de cota do mundo. O mar esta' em Z=0 porque o
# heightmap sai do Godot ja' SIMETRICO em torno do zero (ver o exportador).
# Se essa simetria se perder, estes dois numeros passam a mentir.
PRAIA_ATE = 4.0
TERRA_DE = 16.0

# GEOMETRIA DO MUNDO — os numeros que vieram do exportador do Godot.
# Se o heightmap for regerado com outras cotas, estes tres mudam JUNTO.
LADO_CM = 240000.0          # 2.400 m de lado
Z_MAR_CM = 0.0              # ver posicionar_a_ilha, item 2

ME = unreal.MaterialEditingLibrary


def _no(mat, classe, x, y):
    return ME.create_material_expression(mat, classe, x, y)


def _const3(mat, cor, x, y):
    n = _no(mat, unreal.MaterialExpressionConstant3Vector, x, y)
    n.set_editor_property("constant", unreal.LinearColor(cor[0], cor[1], cor[2], 1.0))
    return n


def _const(mat, v, x, y):
    n = _no(mat, unreal.MaterialExpressionConstant, x, y)
    n.set_editor_property("r", v)
    return n


def construir():
    ferramentas = unreal.AssetToolsHelpers.get_asset_tools()
    if unreal.EditorAssetLibrary.does_asset_exist("%s/%s" % (PASTA, NOME)):
        unreal.EditorAssetLibrary.delete_asset("%s/%s" % (PASTA, NOME))
    mat = ferramentas.create_asset(NOME, PASTA, unreal.Material,
                                   unreal.MaterialFactoryNew())

    # --- inclinacao -> quanto de ROCHA -------------------------------------
    normal = _no(mat, unreal.MaterialExpressionVertexNormalWS, -900, -300)
    nz = _no(mat, unreal.MaterialExpressionComponentMask, -700, -300)
    nz.set_editor_property("r", False)
    nz.set_editor_property("g", False)
    nz.set_editor_property("b", True)
    nz.set_editor_property("a", False)
    ME.connect_material_expressions(normal, "", nz, "")

    plano = _no(mat, unreal.MaterialExpressionSmoothStep, -520, -300)
    plano.set_editor_property("const_min", ROCHA_DE)
    plano.set_editor_property("const_max", ROCHA_ATE)
    ME.connect_material_expressions(nz, "", plano, "Value")

    # `plano` vale 1 no chao deitado. A rocha e' o CONTRARIO dele.
    rocha_a = _no(mat, unreal.MaterialExpressionOneMinus, -380, -300)
    ME.connect_material_expressions(plano, "", rocha_a, "")

    # --- cota -> quanto de PRAIA -------------------------------------------
    pos = _no(mat, unreal.MaterialExpressionWorldPosition, -900, 0)
    pz = _no(mat, unreal.MaterialExpressionComponentMask, -700, 0)
    pz.set_editor_property("r", False)
    pz.set_editor_property("g", False)
    pz.set_editor_property("b", True)
    pz.set_editor_property("a", False)
    ME.connect_material_expressions(pos, "", pz, "")

    # o mundo esta' em centimetros; os knobs estao em metros.
    cem = _const(mat, 100.0, -700, 120)
    metros = _no(mat, unreal.MaterialExpressionDivide, -560, 0)
    ME.connect_material_expressions(pz, "", metros, "A")
    ME.connect_material_expressions(cem, "", metros, "B")

    terra_a = _no(mat, unreal.MaterialExpressionSmoothStep, -420, 0)
    terra_a.set_editor_property("const_min", PRAIA_ATE)
    terra_a.set_editor_property("const_max", TERRA_DE)
    ME.connect_material_expressions(metros, "", terra_a, "Value")

    # --- mancha grande: sem ela o campo le' como feltro de uma cor so' ------
    ruido = _no(mat, unreal.MaterialExpressionNoise, -900, 300)
    ruido.set_editor_property("scale", 0.0009)
    ruido.set_editor_property("levels", 3)
    ruido.set_editor_property("output_min", 0.80)
    ruido.set_editor_property("output_max", 1.20)
    ME.connect_material_expressions(pos, "", ruido, "Position")

    # --- mistura ------------------------------------------------------------
    c_areia = _const3(mat, AREIA, -300, -120)
    c_grama = _const3(mat, GRAMA, -300, -40)
    c_rocha = _const3(mat, ROCHA, -300, 40)

    solo = _no(mat, unreal.MaterialExpressionLinearInterpolate, -120, -80)
    ME.connect_material_expressions(c_areia, "", solo, "A")
    ME.connect_material_expressions(c_grama, "", solo, "B")
    ME.connect_material_expressions(terra_a, "", solo, "Alpha")

    cor = _no(mat, unreal.MaterialExpressionLinearInterpolate, 40, -40)
    ME.connect_material_expressions(solo, "", cor, "A")
    ME.connect_material_expressions(c_rocha, "", cor, "B")
    ME.connect_material_expressions(rocha_a, "", cor, "Alpha")

    final = _no(mat, unreal.MaterialExpressionMultiply, 200, -40)
    ME.connect_material_expressions(cor, "", final, "A")
    ME.connect_material_expressions(ruido, "", final, "B")
    ME.connect_material_property(final, "", unreal.MaterialProperty.MP_BASE_COLOR)

    # Chao de battle royale nao brilha: aspereza alta e zero metal. E' a mesma
    # `Pbr.domar` do mobile, so' que aqui e' uma constante e nao um conserto.
    aspero = _const(mat, 0.92, 200, 80)
    ME.connect_material_property(aspero, "", unreal.MaterialProperty.MP_ROUGHNESS)
    metal = _const(mat, 0.0, 200, 140)
    ME.connect_material_property(metal, "", unreal.MaterialProperty.MP_METALLIC)

    ME.recompile_material(mat)
    unreal.EditorAssetLibrary.save_loaded_asset(mat)
    diz("MATERIAL CRIADO: %s/%s" % (PASTA, NOME))
    return mat


def mar():
    """Um plano azul em Z=0. Nao e' agua de verdade e nao pretende ser.

    O que ele resolve e' a SILHUETA: sem mar, a ilha nao tem contorno e a costa
    vira uma bacia de areia que nao termina. Agua com onda, refracao e espuma e'
    o plugin Water e entra depois — este plano custa uma malha e um material.
    """
    nome = "M_MarChapado"
    if not unreal.EditorAssetLibrary.does_asset_exist("%s/%s" % (PASTA, nome)):
        m = unreal.AssetToolsHelpers.get_asset_tools().create_asset(
                nome, PASTA, unreal.Material, unreal.MaterialFactoryNew())
        cor = _const3(m, (0.02, 0.10, 0.17), -300, 0)
        ME.connect_material_property(cor, "", unreal.MaterialProperty.MP_BASE_COLOR)
        r = _const(m, 0.05, -300, 100)   # agua parada espelha; aspereza baixa
        ME.connect_material_property(r, "", unreal.MaterialProperty.MP_ROUGHNESS)
        s = _const(m, 1.0, -300, 160)
        ME.connect_material_property(s, "", unreal.MaterialProperty.MP_SPECULAR)
        ME.recompile_material(m)
        unreal.EditorAssetLibrary.save_loaded_asset(m)
    return unreal.EditorAssetLibrary.load_asset("%s/%s" % (PASTA, nome))


def posicionar_a_ilha(mat):
    """Poe a ilha no lugar certo do mundo e veste a paisagem.

    DUAS CORRECOES, e as duas sao de numero, nao de gosto:

    1. CENTRO. O Landscape nasce com o CANTO na origem, entao metade do mapa fica
       em coordenada positiva e a origem cai no mar. Todo o design (`Island.pois()`,
       a Zona, a rota do castelo) fala de um mapa CENTRADO em zero. Deslocar
       -1200 m em X e Y faz o zero do Unreal ser o zero do design.

    2. NIVEL DO MAR — resolvido na ORIGEM, nao aqui. O exportador do Godot grava
       o heightmap **simetrico em torno do zero**, entao o Z=0 do Unreal ja' E' o
       nivel do mar e nao ha' deslocamento a fazer (Z_MAR_CM = 0).
       Isso nao e' preciosismo: **transform de Landscape nao persiste** mexido por
       script no World Partition — medido em 28/08, o Z voltava a zero no
       recarregamento porque os proxies do terreno estao descarregados no
       commandlet. Corrigir no dado, e nao no ator, tira o problema da jogada.
       A primeira versao normalizava de -6,20 m a +30,08 m e o mar caia 47,8 m
       abaixo do zero: o plano do mar afogava a ilha inteira.
    """
    sub = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
    sub.load_level(NIVEL)
    atores_sub = unreal.get_editor_subsystem(unreal.EditorActorSubsystem)

    achou = 0
    for a in atores_sub.get_all_level_actors():
        if isinstance(a, unreal.Landscape):
            a.set_editor_property("landscape_material", mat)
            a.set_actor_location(unreal.Vector(-LADO_CM * 0.5, -LADO_CM * 0.5, Z_MAR_CM),
                                 False, False)
            achou += 1
    if achou == 0:
        diz("ERRO: NENHUMA Landscape no nivel")
        return False

    # o mar, uma vez so'
    rotulos = [a.get_actor_label() for a in atores_sub.get_all_level_actors()]
    diz("ATORES ANTES DO MAR (%d): %s" % (len(rotulos), rotulos))
    ja_tem = "Mar" in rotulos
    if not ja_tem:
        plano = unreal.EditorAssetLibrary.load_asset("/Engine/BasicShapes/Plane.Plane")
        if plano is None:
            diz("ERRO MAR: /Engine/BasicShapes/Plane nao carregou")
        else:
            # spawn pela CLASSE, nao pelo objeto: spawn_actor_from_object devolveu
            # None aqui e o erro so' aparece uma chamada depois, como AttributeError
            # em NoneType. Pela classe o ator vem garantido e a malha entra depois.
            ator = atores_sub.spawn_actor_from_class(
                    unreal.StaticMeshActor, unreal.Vector(0, 0, 0))
            ator.set_actor_label("Mar")
            comp = ator.static_mesh_component
            comp.set_editor_property("mobility", unreal.ComponentMobility.STATIC)
            comp.set_static_mesh(plano)
            comp.set_material(0, mar())
            # o Plane do motor tem 100 cm; o mar precisa passar da costa em todas as
            # direcoes, senao aparece a borda do plano no horizonte.
            k = (LADO_CM * 2.5) / 100.0
            ator.set_actor_scale3d(unreal.Vector(k, k, 1.0))
            diz("MAR CRIADO: %s escala=%.1f" % (ator.get_actor_label(), k))

    diz("save_current_level -> %s" % sub.save_current_level())
    diz("save_dirty_packages -> %s" % unreal.EditorLoadingAndSavingUtils.save_dirty_packages(True, True))
    diz("ATORES DEPOIS DO SAVE: %s" % [a.get_actor_label() for a in atores_sub.get_all_level_actors()])
    diz("ILHA POSICIONADA: %d paisagem(ns)" % achou)
    return True


try:
    m = construir()
    ok = posicionar_a_ilha(m)
    diz("FIM ok=%s" % ok)
except Exception:
    diz("EXCECAO:" + chr(10) + traceback.format_exc())
    raise
finally:
    fechar_diario()
