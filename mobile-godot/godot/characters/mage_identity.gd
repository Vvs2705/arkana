extends RefCounted
## Registro de IDENTIDADE VISUAL dos 20 magos (raia PERSONAGEM).
##
## Por que DADOS e nao 20 malhas: o orcamento mobile e' 7 personagens na tela.
## Uma malha por mago custaria 20 builds e 20x o cache. Aqui cada mago e' uma
## entrada de dicionario que faz TRES coisas sobre o mesmo corpo base:
##   1. PALETA  — os hex da ficha (manto/debrum/acento)
##   2. PORTE   — altura e largura (o anao 1,40 largo, o golem 2,30, a fada 0,60)
##   3. ADERECO — pecas-assinatura montadas com o MESMO _lathe do corpo
## E' isso que o jogador le' a 20m: cor, silhueta, e o troco que so' aquele mago tem.
##
## COMO CADASTRAR UM MAGO NOVO (5 minutos, zero malha nova):
##   1. Abra personagens/NN-slug.md e copie os 3 hex de "## Paleta" e a altura
##      de "## Aparencia fisica".
##   2. Adicione uma entrada em MAGES com a chave "NN-slug".
##   3. Escolha o(s) adereco(s) em "props" — cada um e' um perfil de revolucao
##      (mesmo formato do corpo: Vector3(altura, raio, recuo_z)).
##   4. Rode: Godot --headless --path godot --script res://characters/selftest.gd
##      (o teste valida paleta, porte e orcamento de vertices de TODO mago do
##      registro — se estourar 3k ele acusa).
##
## CAMPOS
##   robe/trim/accent : hex sem '#'. robe e' o que set_tint() sobrescreve nos bots.
##   height           : metros. O corpo base mede REF_HEIGHT; o Rig escala por
##                      height/REF_HEIGHT.
##   bulk             : largura da raca (anao 1.32, elfo 0.92). Aplicada no MANTO
##                      e nos offsets dos bracos — nunca como escala nao-uniforme
##                      no Rig, que cisalharia ombreira/braco/capuz rotacionados.
##   float_h          : metros acima do chao (fada que nunca pousa).
##   resize           : {caminho_no_Rig: Vector3} — engorda peca ja existente.
##                      CUIDADO: se for nao-uniforme, so' em no' SEM filho
##                      rotacionado (Head, PadL/R, ArmL/R sao seguros; Torso NAO).
##   recolor          : {caminho_no_Rig: {"mat":..,"col":..,"glow":..}}
##   props            : lista de adereços (abaixo).
##
## PROP
##   n     nome do no'            at    caminho sob "Rig" onde pendura
##   key   chave de cache (UNICA no projeto — malha compartilhada entre instancias)
##   prof  perfil de revolucao    sides lados (6 = barato, 10 = redondo)
##   opts  o mesmo dict do Mage._lathe (cap_top/cap_bottom/ao/scallop/a0/sweep/...)
##   mat   "cloth"|"metal"|"skin"|"energy"    col hex (default: accent)
##   glow  energia de emissao     pos/rot/scale transformada local
##   orbit raio em metros — vira companheiro girando (corvo, mariposa, fada)

const REF_HEIGHT := 1.80

const DEFAULT := {
	"robe": "1b2440", "trim": "f0c75e", "accent": "f0c75e",
	"height": REF_HEIGHT, "bulk": 1.0, "float_h": 0.0,
	"resize": {}, "recolor": {}, "props": [],
}

const MAGES := {

# --- 01 PYRA -----------------------------------------------------------------
# Ficha: 1,78m. Braco ESQUERDO de chama viva numa manopla de bronze — "a manopla
# e' 2x o volume do braco direito (leitura instantanea)". Assimetria e' a assinatura.
"01-pyra": {
	"robe": "2b2e63", "trim": "a8763e", "accent": "ff5a2a",
	"height": 1.78,
	"resize": {
		"Hips/Torso/ArmL": Vector3(1.5, 1.0, 1.5),   # o braco inteiro engorda
		"Hips/Torso/PadL": Vector3(1.35, 1.2, 1.35), # ombreira de campanha
	},
	"recolor": {"Hips/Torso/Emblem": {"mat": "energy", "col": "ff5a2a", "glow": 2.4}},
	"props": [
		# placas da manopla: tres aneis de bronze com respiro entre eles
		{"n": "Gauntlet", "at": "Hips/Torso/ArmL", "key": "pyra_gauntlet",
		 "prof": [Vector3(-0.46, 0.150, 0), Vector3(-0.38, 0.168, 0),
		          Vector3(-0.32, 0.150, 0), Vector3(-0.25, 0.162, 0),
		          Vector3(-0.19, 0.142, 0)],
		 "sides": 6, "opts": {"cap_bottom": true, "ao": 0.18},
		 "mat": "metal", "col": "a8763e"},
		# brasa nas juntas: cone emissivo saindo da manopla para a mao
		{"n": "Ember", "at": "Hips/Torso/ArmL/HandL", "key": "pyra_ember",
		 "prof": [Vector3(-0.13, 0.055, 0), Vector3(-0.04, 0.075, 0),
		          Vector3(0.06, 0.030, 0)],
		 "sides": 6, "opts": {"cap_top": true, "cap_bottom": true},
		 "mat": "energy", "col": "ff5a2a", "glow": 3.2, "pos": Vector3(0, -0.02, 0)},
	],
},

# --- 05 CORVOMANTE -----------------------------------------------------------
# Ficha: 1,80m MAGRO, "silhueta vertical com gola alta"; olho direito de obsidiana
# (emissivo fraco); o corvo carrega o olho vivo e "orbita o ombro quando ocioso".
"05-corvomante": {
	"robe": "4a4a50", "trim": "2e8b57", "accent": "2e8b57",
	"height": 1.80, "bulk": 0.92,
	"recolor": {"Hips/Torso/Head/EyeR": {"mat": "metal", "col": "1a1a22", "glow": 0.35}},
	"props": [
		# gola alta que sobe ate' o queixo — e' ela que verticaliza a silhueta
		{"n": "HighCollar", "at": "Hips/Torso", "key": "corvo_collar",
		 "prof": [Vector3(0.30, 0.150, 0), Vector3(0.44, 0.135, 0),
		          Vector3(0.56, 0.175, 0.02)],
		 "sides": 8, "opts": {"ao": 0.30}, "mat": "cloth", "col": "2e8b57"},
		# o corvo: cauda-corpo-bico num perfil so'. O eixo longo (Y) e' girado 90°
		# em X para ficar TANGENTE a orbita — ele aponta para onde esta' voando,
		# que e' o que faz 24 triangulos lerem como bicho e nao como bolha.
		{"n": "Crow", "at": "Hips/Torso", "key": "corvo_bird",
		 "prof": [Vector3(-0.17, 0.012, 0), Vector3(-0.09, 0.040, 0),
		          Vector3(-0.01, 0.062, 0), Vector3(0.08, 0.045, 0),
		          Vector3(0.15, 0.010, 0)],
		 "sides": 6, "opts": {"cap_top": true, "cap_bottom": true},
		 "mat": "energy", "col": "2e8b57", "glow": 2.6,
		 "orbit": 0.62, "pos": Vector3(0, 0.52, 0), "rot": Vector3(1.5708, 0, 0)},
	],
},

# --- 13 BROK -----------------------------------------------------------------
# Ficha: 1,40m "largo como uma porta", "silhueta baixa e LARGA (contraste com os
# elfos)"; barba ruiva trancada; um olho substituido por gema runica azul.
"13-brok": {
	# accent = BRASA (nao o azul): assim o olho natural fica cor de forja e so' o
	# olho DIREITO fica azul-runa — a assimetria da ficha vira leitura de verdade.
	"robe": "a8763e", "trim": "2aa7ff", "accent": "ff5a2a",
	"height": 1.40, "bulk": 1.32,
	"resize": {
		"Hips/Torso/PadL": Vector3(1.22, 1.15, 1.22),  # ombro de forjador
		"Hips/Torso/PadR": Vector3(1.22, 1.15, 1.22),
		"Hips/Torso/Head": Vector3(1.18, 1.05, 1.18), # cabeca grande = leitura de anao
	},
	"recolor": {"Hips/Torso/Head/EyeR": {"mat": "energy", "col": "2aa7ff", "glow": 3.0}},
	"props": [
		# barba: desce do capuz para o PEITO. O topo tem que ficar ABAIXO da aba do
		# capuz (y=-0.06 no espaco do Head) — mais alto que isso ela engole a cabeca.
		{"n": "Beard", "at": "Hips/Torso/Head", "key": "brok_beard",
		 "prof": [Vector3(-0.36, 0.045, 0), Vector3(-0.26, 0.105, 0),
		          Vector3(-0.14, 0.130, 0), Vector3(-0.04, 0.100, 0)],
		 "sides": 8, "opts": {"cap_bottom": true, "cap_top": true, "ao": 0.40},
		 "mat": "cloth", "col": "c2521f", "pos": Vector3(0, 0, -0.06)},
		# anel de bronze da tranca — pega o specular de metal e quebra o vermelho
		{"n": "BeardRing", "at": "Hips/Torso/Head", "key": "brok_ring",
		 "prof": [Vector3(-0.30, 0.078, 0), Vector3(-0.26, 0.082, 0)],
		 "sides": 8, "opts": {}, "mat": "metal", "col": "a8763e",
		 "pos": Vector3(0, 0, -0.06)},
	],
},

}


## Ficha do mago com os defaults preenchidos. Slug desconhecido = mago generico.
static func get_id(slug: String) -> Dictionary:
	var d := DEFAULT.duplicate(true)
	if MAGES.has(slug):
		d.merge(MAGES[slug], true)
	return d


static func slugs() -> Array:
	return MAGES.keys()
