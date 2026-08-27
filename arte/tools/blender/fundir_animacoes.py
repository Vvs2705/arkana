# NAO USE ISTO PARA PERSONAGEM. (27/08/2026, ordem do Diretor.)
#
# Fundir clipes de um personagem no esqueleto de outro FOI O DEFEITO: os magos
# ficaram com animacao de robo, e o brok.glb saiu com CLIPE DUPLICADO —
# 'mage_soell_cast' era uma copia de 'cair', 'derrubado' era copia de 'Idle_02'
# e 'nadar_parado' era copia de 'Walking'. Dez clipes distintos vendidos como
# treze: ao lancar magia, o mago tocava a animacao de CAIR.
#
# O caminho certo para personagem e' o SITE da Meshy, um por um: la' o rig tem
# uma etapa de MARCACAO DE ARTICULACOES (queixo, ombros, cotovelos, pulsos,
# virilha, joelhos, tornozelos) que a API nao tem, e as animacoes sao aplicadas
# ao esqueleto DAQUELE corpo, nao retargetadas de outro.
#
# Este arquivo fica para PROPS e cenario, onde nao ha esqueleto para confundir.
#
# Funde clipes de animacao de um GLB da Meshy no GLB de jogo de um personagem.
# Mesmo esqueleto dos dois lados (acoes retargetam por NOME de osso).
# Uso (headless):
#   blender --background --python tools/blender/fundir_animacoes.py -- \
#       <repo.glb> <novo_com_clipes.glb> <saida.glb> [manter]
#
# O 4o argumento "manter" inverte o descarte: clipe que NAO esta na tabela
# RENOMEAR fica, com o nome original. Serve para o caso oposto ao de 26/08 —
# ali o repo tinha os clipes e a Meshy trazia os novos; em 27/08 e o contrario,
# o personagem novo vem do rig da API com so' walking/running, e quem DOA os
# clipes e um personagem que ja funciona (brok.glb: Idle_02, Running,
# mage_soell_cast — os tres obrigatorios de Mage.REQUIRED_ANIMS).
# CONFERIDO em 27/08: o rigger da Meshy entrega o MESMO esqueleto para todos —
# 24 ossos, mesmos nomes, mesma ordem em brok.glb e em 03-veu_rigged.glb —
# entao a acao retargeta por nome de osso sem uma linha de codigo a mais.
#
# O que faz: importa o modelo do REPO (malha+rig+acoes que o jogo ja' usa,
# incluindo o cast — que o export "Todos Adicionados" da Meshy OMITIU),
# importa so' as ACOES do GLB novo, renomeia para os aliases do jogo
# (Mage.gd ANIM_ALIASES), descarta duplicatas, e exporta um unico GLB com
# cada acao como uma animacao glTF.
import bpy
import sys

argv = sys.argv[sys.argv.index("--") + 1:]
REPO, NOVO, SAIDA = argv[:3]
MANTER = len(argv) > 3 and argv[3] == "manter"

# clipes novos -> nome que o jogo conhece (Mage.gd). Match por substring:
# o importador pode prefixar/sufixar nomes de acao.
RENOMEAR = {
    "falling_down": "cair",
    "01a040ba-5134-729a-8d57-dfc5b9807200": "planar",       # Planar horizontal v2 (barriga)
    "01a040b7-aa1c-7190-83ef-63b41459dc76": "mergulho",     # dive de cabeca (pendencia da agua)
    "Swim_Forward": "nadar",
    "Swim_Idle": "nadar_parado",
    "Female_Crouch_Pick_Throw_Forward": "pegar",
    "Groan_Holding_Stomach_in_Sleep": "derrubado",
    "Crouch_Walk_Right_with_Torch": "ande_agachado",
}

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=REPO)
objs_repo = set(bpy.data.objects)
acts_repo = set(bpy.data.actions)

bpy.ops.import_scene.gltf(filepath=NOVO)
novas = [a for a in bpy.data.actions if a not in acts_repo]

mantidas = []
for a in novas:
    alvo = next((v for k, v in RENOMEAR.items() if k in a.name), None)
    if alvo is None:
        if not MANTER:
            bpy.data.actions.remove(a)  # duplicata do que o repo ja' tem
            continue
        alvo = a.name               # doador: o clipe fica com o nome original
    a.name = alvo
    mantidas.append(alvo)

# apaga a segunda copia de malha/rig — as acoes sobrevivem por fake_user/NLA
for o in [o for o in bpy.data.objects if o not in objs_repo]:
    bpy.data.objects.remove(o, do_unlink=True)

arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
ad = arm.animation_data if arm.animation_data else arm.animation_data_create()
ja = {s.action.name for t in ad.nla_tracks for s in t.strips if s.action}
for a in bpy.data.actions:
    a.use_fake_user = True
    if a.name in ja:
        continue
    tr = ad.nla_tracks.new()
    tr.name = a.name
    tr.strips.new(a.name, 0, a)
    tr.mute = True  # trilhas so' para o exportador enxergar as acoes

bpy.ops.export_scene.gltf(
    filepath=SAIDA,
    export_format="GLB",
    export_animation_mode="ACTIONS",
    export_skins=True,
    export_def_bones=False,
    export_yup=True,
)
print("FUNDIDO:", SAIDA, "| clipes novos:", ", ".join(sorted(mantidas)))
