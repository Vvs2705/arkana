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
# A AGUA E' PINTADA PELO TERRENO, nao e' um plano.
# POR QUE: a primeira versao punha um plano de 6 km em Z=0. Medido em 28/08 —
# ator em Z=0, componente relativo em Z=0, malha Plane, UM unico ator — e mesmo
# assim ele desenhava POR CIMA de terreno que esta' a 10 e a 25 m de cota. Gastei
# horas atras desse defeito de profundidade e nao o expliquei.
# A saida nao foi contornar: foi TIRAR A CAUSA. O terreno da ilha ja' desce a
# -25 m em volta da costa, entao a agua nao precisa de geometria nenhuma — ela e'
# a faixa do material abaixo do nivel do mar. Sem plano, sem profundidade para
# errar, e de graca: nenhum triangulo novo.
AGUA = (0.015, 0.075, 0.13)

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

# LUZ. Os dois KNOBS da cena, e sao os UNICOS — ver exposicao_travada().
# SOL_LUX: 10 (o padrao do Mixer) e' crepusculo. Dia claro fica na casa de 10^5.
# EV100: quanto mais alto, mais ESCURA a imagem. Sobe se estourar, desce se
# apagar. Estes dois se ajustam OLHANDO, e o valor certo e' o que faz o chao
# iluminado bater com a Cor de base.
# 75.000 lux + EV100 15 dava imagem certa mas o Lumen reclamava: "cached
# lighting is going to be clipped". A pre-exposicao do GI dele e' calibrada para
# uma faixa modesta, e cravar sol de dia real a joga para fora. Voltando ao par
# do proprio motor (10 lux) e travando a exposicao no EV equivalente, a imagem
# fica igual e o aviso some. LICAO: exposicao TRAVADA e' o que resolve; o valor
# absoluto do sol e' so' uma escala, e vale mais ficar na faixa que o Lumen espera.
SOL_LUX = 10.0
EV100 = 3.0

# TEXTURA DE DETALHE — o remedio contra o chao de plastico.
# Ver arte/cenario/texturas/00-LEIA.md. Projecao PLANAR em XY do mundo: o terreno
# e' quase deitado, entao esticamento so' aparece em penhasco, e la' quem manda e'
# a silhueta da rocha. LADRILHO_CM e' o KNOB: menor = gramatura mais fina.
TEXTURAS = "/Game/ARKANA/Texturas"
LADRILHO_CM = 600.0
DETALHE_FORCA = 0.30

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


def importar_texturas():
    """Traz os PNG de gramatura de `arte/` para dentro do projeto.

    A arte NAO e' copiada para o pc-unreal a mao: ela vive em `arte/` e o projeto
    IMPORTA de la'. No dia em que a textura for regerada, muda num lugar so'.

    `srgb = False` nao e' detalhe: estas imagens sao MASCARA, nao cor. Importadas
    como sRGB, a curva de gama distorce a modulacao e a gramatura sai com
    contraste errado.
    """
    origem = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                          "..", "..", "arte", "cenario", "texturas"))
    tarefas = []
    for nome in ("detalhe-chao", "detalhe-rocha"):
        if unreal.EditorAssetLibrary.does_asset_exist("%s/%s" % (TEXTURAS, nome)):
            continue
        caminho = os.path.join(origem, nome + ".png")
        if not os.path.exists(caminho):
            diz("AVISO: nao achei %s" % caminho)
            continue
        t = unreal.AssetImportTask()
        t.filename = caminho
        t.destination_path = TEXTURAS
        t.automated = True
        t.save = True
        t.replace_existing = True
        tarefas.append(t)
    if tarefas:
        unreal.AssetToolsHelpers.get_asset_tools().import_asset_tasks(tarefas)
    for nome in ("detalhe-chao", "detalhe-rocha"):
        a = unreal.EditorAssetLibrary.load_asset("%s/%s" % (TEXTURAS, nome))
        if a is not None:
            a.set_editor_property("srgb", False)
            unreal.EditorAssetLibrary.save_loaded_asset(a)
    diz("TEXTURAS: %s" % [unreal.EditorAssetLibrary.does_asset_exist("%s/%s" % (TEXTURAS, n))
                          for n in ("detalhe-chao", "detalhe-rocha")])


def _textura_detalhe(mat, pos):
    """O ramo de gramatura: mundo XY -> ladrilho -> amostra -> em torno de 1,0.

    Devolve None se a textura nao estiver importada — o material continua valido,
    so' que sem gramatura. Um material que se recusa a compilar por falta de PNG
    seria pior do que um chao liso.
    """
    tex = unreal.EditorAssetLibrary.load_asset("%s/detalhe-chao" % TEXTURAS)
    if tex is None:
        diz("AVISO: %s/detalhe-chao nao existe — chao sem gramatura" % TEXTURAS)
        return None

    xy = _no(mat, unreal.MaterialExpressionComponentMask, -700, 480)
    xy.set_editor_property("r", True)
    xy.set_editor_property("g", True)
    xy.set_editor_property("b", False)
    xy.set_editor_property("a", False)
    ME.connect_material_expressions(pos, "", xy, "")

    esc = _const(mat, 1.0 / LADRILHO_CM, -700, 580)
    uv = _no(mat, unreal.MaterialExpressionMultiply, -540, 480)
    ME.connect_material_expressions(xy, "", uv, "A")
    ME.connect_material_expressions(esc, "", uv, "B")

    amostra = _no(mat, unreal.MaterialExpressionTextureSample, -380, 480)
    amostra.set_editor_property("texture", tex)
    ME.connect_material_expressions(uv, "", amostra, "UVs")

    # 0..1 -> (1-forca)..(1+forca)
    centrado = _no(mat, unreal.MaterialExpressionSubtract, -160, 480)
    ME.connect_material_expressions(amostra, "R", centrado, "A")
    ME.connect_material_expressions(_const(mat, 0.5, -160, 580), "", centrado, "B")
    ganho = _no(mat, unreal.MaterialExpressionMultiply, 0, 480)
    ME.connect_material_expressions(centrado, "", ganho, "A")
    ME.connect_material_expressions(_const(mat, 2.0 * DETALHE_FORCA, 0, 580), "", ganho, "B")
    perto = _no(mat, unreal.MaterialExpressionAdd, 160, 480)
    ME.connect_material_expressions(ganho, "", perto, "A")
    ME.connect_material_expressions(_const(mat, 1.0, 160, 580), "", perto, "B")
    diz("GRAMATURA: ladrilho de %.0f cm, forca %.2f" % (LADRILHO_CM, DETALHE_FORCA))
    return perto


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

    manchado = _no(mat, unreal.MaterialExpressionMultiply, 200, -40)
    ME.connect_material_expressions(cor, "", manchado, "A")
    ME.connect_material_expressions(ruido, "", manchado, "B")

    # --- gramatura de perto -------------------------------------------------
    # Sem isto o chao e' massinha a dois metros do olho, por mais certo que
    # estejam relevo e cor. Detalhe em 0..1 vira (1-forca)..(1+forca) e
    # MULTIPLICA a cor — modulacao, nao pintura: a leitura de praia/campo/rocha
    # continua sendo a do bloco de cima.
    det = _textura_detalhe(mat, pos)
    # --- agua: tudo abaixo do nivel do mar ---------------------------------
    fora_dagua = _no(mat, unreal.MaterialExpressionSmoothStep, -420, 200)
    fora_dagua.set_editor_property("const_min", -1.5)
    fora_dagua.set_editor_property("const_max", 0.5)
    ME.connect_material_expressions(metros, "", fora_dagua, "Value")

    final = manchado
    if det is not None:
        final = _no(mat, unreal.MaterialExpressionMultiply, 380, -40)
        ME.connect_material_expressions(manchado, "", final, "A")
        ME.connect_material_expressions(det, "", final, "B")
    # a agua entra POR CIMA de tudo, inclusive da gramatura: agua nao tem grao.
    c_agua = _const3(mat, AGUA, 380, 200)
    com_agua = _no(mat, unreal.MaterialExpressionLinearInterpolate, 520, 60)
    ME.connect_material_expressions(c_agua, "", com_agua, "A")
    ME.connect_material_expressions(final, "", com_agua, "B")
    ME.connect_material_expressions(fora_dagua, "", com_agua, "Alpha")
    ME.connect_material_property(com_agua, "", unreal.MaterialProperty.MP_BASE_COLOR)

    # Chao de battle royale nao brilha: aspereza alta e zero metal. E' a mesma
    # `Pbr.domar` do mobile, so' que aqui e' uma constante e nao um conserto.
    # A AGUA e' a excecao: agua parada espelha, entao a aspereza dela e' baixa —
    # e e' esse contraste de brilho que faz o olho ler "molhado" sem plano nenhum.
    aspero = _no(mat, unreal.MaterialExpressionLinearInterpolate, 520, 200)
    ME.connect_material_expressions(_const(mat, 0.06, 380, 260), "", aspero, "A")
    ME.connect_material_expressions(_const(mat, 0.92, 380, 320), "", aspero, "B")
    ME.connect_material_expressions(fora_dagua, "", aspero, "Alpha")
    ME.connect_material_property(aspero, "", unreal.MaterialProperty.MP_ROUGHNESS)
    metal = _const(mat, 0.0, 200, 140)
    ME.connect_material_property(metal, "", unreal.MaterialProperty.MP_METALLIC)

    ME.recompile_material(mat)
    unreal.EditorAssetLibrary.save_loaded_asset(mat)
    diz("MATERIAL CRIADO: %s/%s" % (PASTA, NOME))
    return mat


def mar():
    """OBSOLETA. Ficou como registro do que NAO fazer.

    Era um plano de 6 km em Z=0. Medido em 28/08: ator em Z=0, componente
    relativo em Z=0, malha `Plane`, um unico ator no nivel — e mesmo assim ele
    desenhava por cima de terreno a 10 e a 25 m de cota. O defeito de
    profundidade nao foi explicado, e a solucao foi tirar a causa: a agua virou
    faixa do MATERIAL do terreno abaixo do nivel do mar. Ver `AGUA` no topo.
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


def _nunca_espacial(ator):
    """Tira o ator do streaming do World Partition.

    DUAS RAZOES, e as duas sao de peso:

    1. CORRETUDE. O mar e o volume de exposicao valem no MAPA INTEIRO. Ator
       global que faz streaming e' ator que some quando o jogador anda para
       longe — o mar sumiria do horizonte.

    2. O DEFEITO QUE ISTO CONSERTA (medido em 28/08). `get_all_level_actors()`
       num commandlet **so' devolve os atores NAO-espaciais**, porque nenhuma
       regiao esta' carregada. A guarda "ja' existe um Mar?" nunca via o mar
       anterior, e cada rodada do script empilhava mais um plano de mar em cima
       do outro. Nao-espacial, a guarda passa a enxergar e a rodada vira
       idempotente — que e' o que um script de montagem tem que ser.
    """
    ator.set_editor_property("is_spatially_loaded", False)


def exposicao_travada(atores_sub):
    """Trava a exposicao da cena e poe o sol num valor de dia claro.

    POR QUE ISTO EXISTE, e a medicao que provou: em 28/08 a ilha aparecia
    BRANCA na visao iluminada. O primeiro palpite seria material errado. Ligando
    a **Cor de base** (o buffer que ignora luz), a ilha estava certa — verde no
    miolo, areia na costa, rocha cinza no pico. Ou seja: o material nunca esteve
    errado, quem mentia era a EXPOSICAO AUTOMATICA.

    A causa: a cena e' dominada por mar escuro, entao o auto-exposure abre para
    compensar e estoura toda a terra. Isso nao e' bug do Unreal — e' o que
    auto-exposure faz. Jogo publicado nao deixa a exposicao livre num mapa
    aberto justamente por isso: a mesma encosta mudaria de cor conforme o
    jogador olhasse para o mar ou para o morro.

    A trava: min = max de brilho automatico e' o jeito documentado de fixar
    exposicao sem mexer em camera. `EV100` fica como o KNOB unico da cena.
    """
    for a in atores_sub.get_all_level_actors():
        if isinstance(a, unreal.DirectionalLight):
            # 10 lux e' o padrao do Mixer e e' luz de CREPUSCULO. Dia claro com
            # SkyAtmosphere pede duas ordens de grandeza a mais.
            a.light_component.set_editor_property("intensity", SOL_LUX)
            a.set_actor_rotation(unreal.Rotator(0.0, -42.0, 30.0), False)
            diz("SOL: %.0f lux, inclinacao -42 graus" % SOL_LUX)

    # ACHAR OU CRIAR, e depois SEMPRE aplicar. A primeira versao daqui pulava o
    # bloco quando o volume ja' existia — e com isso mudar EV100 no topo do
    # arquivo nao mudava nada na cena. Knob que nao muda nada e' knob que mente,
    # e e' pior do que knob nenhum: leva horas a procurar defeito no lugar errado.
    ppv = None
    for a in atores_sub.get_all_level_actors():
        if a.get_actor_label() == "Exposicao":
            ppv = a
            break
    if ppv is None:
        ppv = atores_sub.spawn_actor_from_class(unreal.PostProcessVolume,
                                               unreal.Vector(0, 0, 0))
        ppv.set_actor_label("Exposicao")
        diz("EXPOSICAO: volume criado")
    else:
        diz("EXPOSICAO: volume existente, reajustando")
    ppv.set_editor_property("unbound", True)   # vale no mapa inteiro
    s = ppv.get_editor_property("settings")
    # Travar e' pôr MIN = MAX: o automatico continua ligado mas nao tem para onde
    # correr. E' o jeito de fixar exposicao sem depender das contas de camera
    # (ISO/obturador/diafragma) do modo Manual, que sao mais um lugar para errar.
    s.set_editor_property("override_auto_exposure_min_brightness", True)
    s.set_editor_property("auto_exposure_min_brightness", EV100)
    s.set_editor_property("override_auto_exposure_max_brightness", True)
    s.set_editor_property("auto_exposure_max_brightness", EV100)
    ppv.set_editor_property("settings", s)
    _nunca_espacial(ppv)
    diz("EXPOSICAO: travada em EV100=%.1f" % EV100)


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

    # O MAR DEIXOU DE SER ATOR — ver o comentario de AGUA no topo. Se sobrou um
    # plano de mar de versao anterior, ele sai daqui: um plano gigante em Z=0
    # desenhando por cima do terreno e' pior do que mar nenhum.
    for a in atores_sub.get_all_level_actors():
        if a.get_actor_label() == "Mar":
            atores_sub.destroy_actor(a)
            diz("MAR (plano) APAGADO — a agua agora e' do material")

    exposicao_travada(atores_sub)

    diz("save_current_level -> %s" % sub.save_current_level())
    diz("save_dirty_packages -> %s" % unreal.EditorLoadingAndSavingUtils.save_dirty_packages(True, True))
    diz("ATORES DEPOIS DO SAVE: %s" % [a.get_actor_label() for a in atores_sub.get_all_level_actors()])
    diz("ILHA POSICIONADA: %d paisagem(ns)" % achou)
    return True


try:
    importar_texturas()
    m = construir()
    ok = posicionar_a_ilha(m)
    diz("FIM ok=%s" % ok)
except Exception:
    diz("EXCECAO:" + chr(10) + traceback.format_exc())
    raise
finally:
    fechar_diario()
