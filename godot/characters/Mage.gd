@tool
extends Node3D
## Mage — protagonista ARKANA (raia PERSONAGEM). Visual PURO: malha procedural
## (SurfaceTool, zero binario) + AnimationPlayer. Sem fisica/input.
##
## Contrato (godot/ARQUITETURA.md) — NAO MUDA:
##   play_anim("idle"|"run"|"cast")  — crossfade 0.15s, sem pulo
##   set_tint(Color)                 — recolore o manto (bots reusam o modelo)
##   sinal cast_fired / get_cast_fire_time() / get_anim_length("cast")
##
## ADICIONADO (nao substitui nada):
##   set_mage(slug)      — veste o corpo base como um dos 20 magos do elenco
##                         (paleta + porte + adereco). Ver mage_identity.gd.
##   get_mage_sheet()    — a ficha aplicada, ja' com defaults.
## (nao se chama set_identity porque Node3D JA' tem set_identity() nativo — o
##  proprio Godot reclama do override.)
## Ordem: set_mage() define a paleta; um set_tint() posterior AINDA vence
## (e' assim que os bots ficam distinguiveis do player mesmo vestindo o mesmo mago).

signal cast_fired

const CAST_FIRE_TIME := 0.22            # s dentro da anim `cast` em que a mao abre

## Crossfade por DESTINO (s). Entrar em `cast` e' quase seco de proposito: o
## dedo aperta Fogo e tem que VER o gesto no mesmo instante; ja' idle e run
## podem se dissolver com calma sem parecer moles.
const BLEND := {"idle": 0.18, "run": 0.12, "cast": 0.05}

const COL_ROBE := Color("1b2440")       # azul-noite arcano (GDD §10, clareado p/ leitura)
const COL_GOLD := Color("f0c75e")       # dourado arcano (GDD §10)
const COL_DARK := Color("07080f")       # vazio do capuz
const COL_SKIN := Color("e0b98d")

const SHADER := preload("res://characters/mage_toon.gdshader")
const IDS := preload("res://characters/mage_identity.gd")
const MODEL_DIR := "res://characters/modelos"
const REQUIRED_ANIMS := ["idle", "run", "cast"]

## ANIMACOES OPCIONAIS. NAO entram em REQUIRED_ANIMS de proposito: um modelo
## externo que nao as tenha continua valendo. Se entrassem como obrigatorias,
## Pyra e Brok — que so' tem idle/run/cast — cairiam no mago procedural, ou
## seja, uma feature nova apagaria os modelos que ja' funcionam.
## Quem chama pergunta antes com has_anim(); play_anim() cai no substituto
## abaixo em vez de reclamar no console.
const OPTIONAL_ANIMS := ["cair", "planar", "pegar", "derrubado"]

## Para onde cada opcional cai quando o modelo nao a tem. Nunca fica sem pose.
const ANIM_FALLBACK := {"cair": "idle", "planar": "idle", "pegar": "cast",
		"derrubado": "idle"}

## MEIA-VOLTA OBRIGATORIA NO MODELO EXTERNO — nao e' gosto, e' conversao de
## convencao. glTF (e a Meshy, que exporta glTF) posiciona o personagem olhando
## para +Z. O Godot anda para -Z: o Pawn calcula o passo com
## `-global_transform.basis.z` e vira o corpo com `atan2(-dir.x, -dir.z)`.
## Sem esta correcao o modelo entra de costas para o mundo e de FRENTE para a
## camera de 3a pessoa: as pernas correm ao contrario do deslocamento, o que o
## olho le como PATINACAO, e andar de re' e' o unico momento em que a passada
## parece certa.
##
## MEDIDO em 25/08/2026 no aparelho do Diretor: Pyra e Brok, os dois modelos
## reais do jogo, apareciam de rosto para a camera. O braco de chama dela — que
## e' o ESQUERDO na ficha — saia' a direita do quadro, prova de que o que se via
## era a frente. A Pyra carregava o defeito desde 21/08 sem ninguem notar.
const MODEL_YAW := PI

## QUAIS ANIMACOES DO MODEL EXTERNO REPETEM EM LACO.
## O importador glTF do Godot traz TODA animacao com LOOP_NONE. Sem esta
## tabela, "run" toca uma vez — uns tres passos —, congela no ultimo quadro e o
## corpo segue deslizando: e' a PATINACAO que o Diretor relatou em 25/08 depois
## de ja' termos consertado a orientacao ("da' apenas os tres primeiros passos e
## depois patina").
## "cast" fica de FORA de proposito: e' disparo unico, e o `cast_fired` depende
## de ela terminar. Em laco, o tiro sairia repetido.
const ANIMS_EM_LACO := ["idle", "run", "cair", "planar", "derrubado"]
const ANIM_ALIASES := {
	"idle": ["idle", "Idle", "IDLE", "Armature|Idle", "mixamo.com",
		"standing_idle", "breathing_idle", "idle_01", "idle_loop",
		# A biblioteca da Meshy numera as variantes (Idle_02, Idle_03...).
		"idle_02", "idle_03"],
	"run": ["run", "Run", "RUN", "Running", "running", "Armature|Run",
		"Armature|Running", "Run Forward", "running_forward", "locomotion_run",
		"sprint", "jog", "fast_run"],
	"cast": ["cast", "Cast", "CAST", "Spellcast", "Spell Cast", "spell_cast",
		"casting", "magic_cast", "Attack", "attack", "attack1",
		"Standing 1H Magic Attack", "magic_attack", "shoot", "fireball",
		"Armature|Cast", "Armature|Attack",
		# Biblioteca da Meshy. "soell" NAO e' erro nosso: e' como o clipe
		# "Mage Spell Cast" vem gravado dentro do .glb (conferido em 25/08 lendo
		# o JSON do arquivo). Se a Meshy corrigir, o alias certo ja' esta' acima.
		"mage_soell_cast", "mage_spell_cast"],
	"cair": ["cair", "fall", "Fall", "falling", "Falling", "freefall",
		"free_fall", "skydive", "Skydiving", "air", "jump_loop"],
	"planar": ["planar", "glide", "Glide", "gliding", "Gliding", "parachute",
		"wingsuit", "hover", "Hovering", "flying", "Flying"],
	"pegar": ["pegar", "pickup", "Pickup", "pick_up", "Picking Up", "grab",
		"Grab", "interact", "Interacting", "loot", "crouch_pickup"],
	# NENHUM modelo do elenco tem este clipe hoje (Pyra tem 3 animacoes, Brok
	# tem 5). Os aliases ja' existem para o dia em que ele vier da biblioteca da
	# Meshy — ate' la' o fallback segura, e quem realmente usa a animacao e' o
	# mago procedural, que e' o que 18 dos 20 personagens rodam.
	"derrubado": ["derrubado", "downed", "Downed", "knocked", "Knocked Down",
		"knockdown", "crawl", "Crawl", "crawling", "Crawling", "wounded",
		"injured", "dying", "getting_up", "lying"],
}

## PRESETS DE MATERIAL — um shader so', quatro respostas de luz diferentes.
## E' o que separa "tudo parece o mesmo plastico" de tecido/metal/pele/magia.
##   spec_*      brilho especular (metal corta em degrau, tecido e' macio)
##   shade_wrap  meia-lambert: quanto a luz "enrola" para o lado escuro
##   rim_*       fresnel que destaca a silhueta contra o fundo
const MAT := {
	"cloth": {"spec_amount": 0.05, "spec_power": 8.0, "spec_tight": 0.0,
		"shade_wrap": 0.30, "rim_strength": 0.28, "rim_power": 3.0, "ao_strength": 1.0},
	"metal": {"spec_amount": 0.80, "spec_power": 42.0, "spec_tight": 1.0,
		"shade_wrap": 0.08, "rim_strength": 0.55, "rim_power": 2.4, "ao_strength": 0.7},
	"skin": {"spec_amount": 0.14, "spec_power": 18.0, "spec_tight": 0.0,
		"shade_wrap": 0.45, "rim_strength": 0.22, "rim_power": 3.5, "ao_strength": 1.0},
	# energia magica nao ocluia e nao reflete: so' emite, com rim forte
	"energy": {"spec_amount": 0.0, "spec_power": 8.0, "spec_tight": 0.0,
		"shade_wrap": 0.70, "rim_strength": 0.90, "rim_power": 2.0, "ao_strength": 0.0},
}

# ponytail: malhas compartilhadas entre instancias (player + 6 bots = 1 build).
# A chave e' explicita e UNICA por forma — opts nao entram nela.
static var _mesh_cache: Dictionary = {}
static var _scene_cache: Dictionary = {}

## Slug do elenco (ex.: "01-pyra"). Vazio = mago generico.
@export var mage_id: String = "":
	set(v):
		mage_id = v
		if is_inside_tree():
			_build()

## Fase 2 do pipeline grafico: se houver um .glb game-ready para o mago,
## ele assume o visual. Se faltar arquivo/rig/animacao, cai no procedural.
@export var prefer_imported_model := true:
	set(v):
		prefer_imported_model = v
		if is_inside_tree():
			_build()

@export var imported_model_path_override := "":
	set(v):
		imported_model_path_override = v
		if is_inside_tree():
			_build()

var _player: AnimationPlayer
var _external_player: AnimationPlayer
var _tint_mats: Array[Material] = []
var _orbits: Array[Node3D] = []          # pivos de companheiro (corvo, mariposa, fada)
var _bulk := 1.0                         # largura da raca; ver _apply_identity
var _pending_tint: Variant = null
var _pending_anim := ""
var _model_source := "procedural"
var _model_report := {}
var _anim_map := {}
var _cast_emit_pending := false


func _ready() -> void:
	_build()
	if _pending_tint != null:
		set_tint(_pending_tint)
	if _pending_anim != "":
		play_anim(_pending_anim)
	else:
		play_anim("idle")


func _process(delta: float) -> void:
	# Companheiro orbitando: 3 linhas e continuo entre trocas de animacao
	# (uma track de AnimationPlayer daria salto no crossfade idle->cast).
	for o in _orbits:
		o.rotate_y(delta * 0.9)
	if _cast_emit_pending and _external_player != null:
		var cast_anim: String = _anim_map.get("cast", "cast")
		if _external_player.current_animation != cast_anim:
			_cast_emit_pending = false
			set_process(not _orbits.is_empty())
		elif _external_player.current_animation_position >= CAST_FIRE_TIME:
			_cast_emit_pending = false
			_cast_fire()
			set_process(not _orbits.is_empty())


# -------------------------------------------------------------------- contrato

## O mago tem esta animacao? Quem for usar uma OPTIONAL_ANIMS pergunta antes,
## para poder mudar o comportamento (ex.: nao prometer plan(e)io que nao existe).
func has_anim(nome: String) -> bool:
	var player := _active_player()
	if player == null:
		return false
	return player.has_animation(_anim_name(nome))


func play_anim(nome: String) -> void:
	var player := _active_player()
	if player == null:
		_pending_anim = nome           # chamado antes de entrar na arvore
		return
	var anim := _anim_name(nome)
	# O que foi PEDIDO, guardado antes de qualquer substituicao. E' por ele que
	# se decide o gatilho do projetil — ver o `_cast_emit_pending` abaixo.
	var pedido := nome
	# Opcional ausente cai no substituto em silencio: o jogo nao pode ficar sem
	# pose, e um warning por frame poluiria o console durante a queda inteira.
	if not player.has_animation(anim) and ANIM_FALLBACK.has(nome):
		nome = ANIM_FALLBACK[nome]
		anim = _anim_name(nome)
	if player.has_animation(anim):
		# DISPARO CONTINUO (queixa do Diretor, 26/08: "ao lancar magia o boneco
		# balanca a mao para cima e fica por isso"): pedir a MESMA animacao de
		# tiro unico enquanto ela toca nao fazia nada — play() com o clipe
		# corrente nao reinicia — e no fim o clipe congelava no ultimo quadro,
		# braco no ar, ate' o proximo TROCAR de nome. Um-so-disparo pedido de
		# novo agora REINICIA; as de laco continuam intocadas.
		if not (nome in ANIMS_EM_LACO) and player.current_animation == anim \
				and player.is_playing():
			player.stop()
		player.play(anim, float(BLEND.get(nome, 0.15)))
		if player == _external_player:
			# PEDIDO, nunca o substituto. "pegar" cai em "cast" nos modelos que
			# nao tem a opcional (Pyra e Brok): se olhassemos o nome final,
			# APANHAR UM ITEM EMITIRIA cast_fired — ou seja, pegar viraria tiro
			# no dia em que o disparo for pendurado nesse sinal.
			_cast_emit_pending = pedido == "cast"
			set_process(_cast_emit_pending or not _orbits.is_empty())
	else:
		push_warning("Mage: animacao desconhecida '%s'" % nome)


## Cadencia da animacao (fim da patinacao dos pes). Quem manda e' o Pawn, que
## sabe a velocidade real; aqui so' repassamos pro AnimationPlayer ativo —
## serve igual pro modelo externo (.glb) e pro procedural.
func set_anim_speed(escala: float) -> void:
	var player := _active_player()
	if player != null:
		player.speed_scale = maxf(escala, 0.01)


func set_tint(cor: Color) -> void:
	if _tint_mats.is_empty():
		_pending_tint = cor            # chamado antes de entrar na arvore
		return
	for m in _tint_mats:
		if m is ShaderMaterial:
			(m as ShaderMaterial).set_shader_parameter("tint", cor)
			(m as ShaderMaterial).set_shader_parameter("tint_mix", 0.85)
		elif m is StandardMaterial3D:
			(m as StandardMaterial3D).albedo_color = cor


func get_cast_fire_time() -> float:
	return CAST_FIRE_TIME


func get_anim_length(nome: String) -> float:
	var player := _active_player()
	var anim := _anim_name(nome)
	if player != null and player.has_animation(anim):
		return player.get_animation(anim).length
	return 0.0


func _cast_fire() -> void:
	cast_fired.emit()


# ------------------------------------------------------------------ identidade

## Veste o corpo base como um dos 20 magos. Reconstroi (barato: malhas cacheadas).
func set_mage(slug: String) -> void:
	mage_id = slug


## Ficha aplicada, com os defaults ja' preenchidos.
func get_mage_sheet() -> Dictionary:
	return IDS.get_id(mage_id)


## Fonte visual atual: "procedural" ou "external:<res://...glb>".
func get_model_source() -> String:
	return _model_source


## O AnimationPlayer do .glb importado, ou null se o mago esta' no procedural.
## Existe para FERRAMENTA e TESTE (o no' fica aninhado em profundidade que varia
## conforme o exportador); o jogo fala com o Mage por play_anim().
func get_animation_player_externo() -> AnimationPlayer:
	return _external_player


func get_model_report() -> Dictionary:
	return _model_report.duplicate(true)


static func model_path_for(slug: String) -> String:
	if slug == "":
		return ""
	var filename := slug
	var dash := slug.find("-")
	if dash >= 0 and dash < slug.length() - 1:
		filename = slug.substr(dash + 1)
	return "%s/%s.glb" % [MODEL_DIR, filename]


func _current_model_path() -> String:
	return imported_model_path_override if imported_model_path_override != "" else model_path_for(mage_id)


# ---------------------------------------------------------------- construcao

func _build() -> void:
	for c in get_children():
		remove_child(c)
		c.queue_free()
	_player = null
	_external_player = null
	_model_source = "procedural"
	_model_report = {}
	_anim_map = {}
	_cast_emit_pending = false
	_tint_mats.clear()
	_orbits.clear()

	var idf := IDS.get_id(mage_id)
	_bulk = float(idf["bulk"])
	if prefer_imported_model and _build_imported_model():
		return

	var navy := _mat("cloth", Color(idf["robe"]))    # manto/tunica/capuz/mangas
	var gold := _mat("metal", Color(idf["trim"]))    # debruns, cinto, ombreiras
	var dark := _mat("cloth", COL_DARK)              # interior do capuz
	var skin := _mat("skin", COL_SKIN)               # maos livres para conjurar
	var acc := Color(idf["accent"])
	var eye := _mat("energy", COL_DARK, acc, 3.0)    # olhos que brilham no vazio
	var gem := _mat("energy", acc, acc, 1.6)         # emblema no peito
	_tint_mats.append(navy)

	var rig := Node3D.new()
	rig.name = "Rig"
	add_child(rig)
	var hips := Node3D.new()
	hips.name = "Hips"
	# float_h: a fada que nunca pousa. Vai no Hips porque Rig:position e' animado.
	hips.position = Vector3(0, 0.92 + float(idf["float_h"]), 0)
	rig.add_child(hips)

	# Manto: sino facetado ate' o chao, bainha em zigue-zague + debrum dourado.
	# Barra mais aberta (0.52) e cintura mais fina que a versao anterior — num
	# jogo de combate a silhueta e' o que se le' primeiro.
	var robe := _mi(hips, "Robe", _lathe("robe", [
		Vector3(-0.90, 0.52, 0), Vector3(-0.88, 0.505, 0), Vector3(-0.60, 0.415, 0),
		Vector3(-0.30, 0.315, 0), Vector3(0.0, 0.245, 0), Vector3(0.12, 0.205, 0),
	], 12, {"scallop": 0.13, "cap_bottom": true, "ao": 0.45}), navy)
	# A LARGURA da raca mora aqui, no manto (a massa que a silhueta le'), e nao
	# numa escala nao-uniforme no Rig: escala nao-uniforme no PAI CISALHA todo
	# filho rotacionado (ombreiras e bracos sairam tortos ate' isto ser movido).
	robe.scale = Vector3(_bulk, 1.0, _bulk)
	_mi(robe, "RobeTrim", _lathe("trim", [
		Vector3(-0.87, 0.522, 0), Vector3(-0.78, 0.478, 0),
	], 12, {}), gold)

	# Tronco em V: cintura cinchada (0.215) e ombro largo (0.31). Custo de um
	# ponto a mais no perfil, e o boneco deixa de ser um barril.
	var torso := _mi(hips, "Torso", _lathe("torso", [
		Vector3(0.0, 0.235, 0), Vector3(0.11, 0.215, 0),
		Vector3(0.30, 0.310, 0), Vector3(0.42, 0.155, 0),
	], 8, {"cap_top": true, "ao": 0.30}), navy, Vector3(0, 0.10, 0))
	_mi(torso, "Belt", _lathe("belt", [
		Vector3(0.06, 0.238, 0), Vector3(0.11, 0.232, 0), Vector3(0.15, 0.222, 0),
	], 8, {}), gold)
	_mi(torso, "Collar", _lathe("collar", [
		Vector3(0.0, 0.215, 0), Vector3(0.07, 0.165, 0),
	], 8, {}), gold, Vector3(0, 0.36, 0))
	_mi(torso, "Emblem", _disc("emblem", 0.05, 4), gem, Vector3(0, 0.26, -0.30))

	# Ombreiras douradas caidas para fora. Inclinacao contida (0.24): com 0.35 elas
	# viravam asas e engoliam os bracos — o ombro tem que ser largo, nao gigante.
	var pad := _lathe("pad", [
		Vector3(0.0, 0.175, 0), Vector3(0.06, 0.138, 0), Vector3(0.125, 0.05, 0),
	], 6, {"cap_top": true, "cap_bottom": true, "ao": 0.25})
	_mi(torso, "PadL", pad, gold, Vector3(-0.315 * _bulk, 0.36, 0), Vector3(0, 0, 0.24))
	_mi(torso, "PadR", pad, gold, Vector3(0.315 * _bulk, 0.36, 0), Vector3(0, 0, -0.24))

	# Capuz: revolucao parcial (frente aberta), bico varrido para tras,
	# forro interno escuro e dois olhos dourados flutuando no vazio
	var head := _mi(torso, "Head", _lathe("hood", [
		Vector3(-0.06, 0.15, 0), Vector3(0.02, 0.20, 0), Vector3(0.10, 0.19, 0.01),
		Vector3(0.20, 0.155, 0.05), Vector3(0.28, 0.09, 0.10), Vector3(0.36, 0.02, 0.16),
	], 8, {"a0": deg_to_rad(325.0), "sweep": deg_to_rad(250.0), "cap_top": true,
		"ao": 0.22}), navy, Vector3(0, 0.46, 0))
	_mi(head, "HoodInner", _lathe("hood_in", [
		Vector3(-0.06, 0.15, 0), Vector3(0.02, 0.20, 0), Vector3(0.10, 0.19, 0.01),
		Vector3(0.20, 0.155, 0.05), Vector3(0.28, 0.09, 0.10), Vector3(0.36, 0.02, 0.16),
	], 8, {"a0": deg_to_rad(325.0), "sweep": deg_to_rad(250.0), "rscale": 0.93,
		"inside": true}), dark)
	_mi(head, "EyeL", _disc("eye", 0.016, 4), eye, Vector3(-0.034, 0.07, -0.135))
	_mi(head, "EyeR", _disc("eye", 0.016, 4), eye, Vector3(0.034, 0.07, -0.135))

	# Bracos: manga sino + punho dourado + mao livre
	var sleeve := _lathe("sleeve", [
		Vector3(-0.44, 0.135, 0), Vector3(-0.40, 0.118, 0),
		Vector3(-0.20, 0.078, 0), Vector3(0.02, 0.09, 0),
	], 6, {"cap_bottom": true, "cap_top": true, "ao": 0.30})
	var cuff := _lathe("cuff", [
		Vector3(-0.43, 0.143, 0), Vector3(-0.36, 0.126, 0),
	], 6, {})
	var hand := _lathe("hand", [
		Vector3(-0.10, 0.02, 0), Vector3(-0.07, 0.05, 0),
		Vector3(-0.03, 0.055, 0), Vector3(0.0, 0.035, 0),
	], 6, {"cap_bottom": true, "cap_top": true, "ao": 0.20})
	var arm_l := _mi(torso, "ArmL", sleeve, navy,
		Vector3(-0.315 * _bulk, 0.38, 0), Vector3(0.04, 0, -0.14))
	_mi(arm_l, "CuffL", cuff, gold)
	_mi(arm_l, "HandL", hand, skin, Vector3(0, -0.46, 0))
	var arm_r := _mi(torso, "ArmR", sleeve, navy,
		Vector3(0.315 * _bulk, 0.38, 0), Vector3(0.04, 0, 0.14))
	_mi(arm_r, "CuffR", cuff, gold)
	_mi(arm_r, "HandR", hand, skin, Vector3(0, -0.46, 0))

	_apply_identity(rig, idf)

	# Animacoes
	_player = AnimationPlayer.new()
	_player.name = "AnimationPlayer"
	add_child(_player)
	var lib := AnimationLibrary.new()
	lib.add_animation("idle", _anim_idle())
	lib.add_animation("run", _anim_run())
	lib.add_animation("cast", _anim_cast())
	lib.add_animation("cair", _anim_cair())
	lib.add_animation("planar", _anim_planar())
	lib.add_animation("pegar", _anim_pegar())
	lib.add_animation("derrubado", _anim_derrubado())
	_player.add_animation_library("", lib)
	_anim_map = {"idle": "idle", "run": "run", "cast": "cast"}
	_model_report = _make_model_report(self, _player, _model_source)
	set_process(not _orbits.is_empty())


func _build_imported_model() -> bool:
	var path := _current_model_path()
	if path == "" or not ResourceLoader.exists(path):
		return false
	var packed: Variant = _scene_cache.get(path)
	if packed == null:
		packed = load(path)
		if packed is PackedScene:
			_scene_cache[path] = packed
	if not (packed is PackedScene):
		push_warning("Mage: modelo '%s' nao e' uma PackedScene importavel" % path)
		return false
	var inst: Node = (packed as PackedScene).instantiate()
	if not (inst is Node3D):
		push_warning("Mage: modelo '%s' nao tem raiz Node3D" % path)
		inst.free()
		return false
	var player := _find_animation_player(inst)
	if player == null:
		push_warning("Mage: modelo '%s' sem AnimationPlayer; usando procedural" % path)
		inst.free()
		return false
	var aliases := _resolve_animation_aliases(player)
	for nome in REQUIRED_ANIMS:
		if not aliases.has(nome):
			push_warning("Mage: modelo '%s' sem animacao '%s'; usando procedural" % [path, nome])
			inst.free()
			return false
	if player.get_animation(aliases["cast"]).length <= CAST_FIRE_TIME:
		push_warning("Mage: modelo '%s' tem cast curto demais; usando procedural" % path)
		inst.free()
		return false
	_aplica_laco(player, aliases)
	_domar_pbr(inst)
	inst.name = "ExternalModel"
	(inst as Node3D).rotation.y = MODEL_YAW   # ver MODEL_YAW: +Z do glTF -> -Z do Godot
	add_child(inst)
	_external_player = player
	_anim_map = aliases
	_model_source = "external:%s" % path
	_collect_external_tint_mats(inst)
	_model_report = _make_model_report(inst, player, _model_source)
	set_process(false)
	return true


func _anim_name(contract_name: String) -> String:
	return _anim_map.get(contract_name, contract_name)


## Liga o laco nas animacoes continuas do modelo externo. Ver ANIMS_EM_LACO.
## A Animation vem do .glb IMPORTADO e e' compartilhada pelo cache de cenas —
## por isso marcamos uma vez, na montagem, e nao a cada play().
## METAL DA MESHY NO MOBILE (26/08). O PBR dos modelos vem com mapa metallic,
## e metal e' quase todo REFLEXO: sem reflection probe (nao ha' nenhuma — e nao
## cabe no orcamento), o renderer mobile devolve breu. No video do Diretor a
## personagem anda como uma SILHUETA PRETA pela ilha inteira. Grampear o fator
## metallic devolve o difuso; o brilho "de metal" que sobrevive e' o specular
## comum, que nao depende de probe. Roughness ganha piso pelo mesmo motivo.
static func _domar_pbr(raiz: Node) -> void:
	var pilha: Array = [raiz]
	while not pilha.is_empty():
		var n: Node = pilha.pop_back()
		for c in n.get_children():
			pilha.append(c)
		if not (n is MeshInstance3D):
			continue
		var mi := n as MeshInstance3D
		if mi.mesh == null:
			continue
		for s in mi.mesh.get_surface_count():
			var m := mi.mesh.surface_get_material(s)
			if m is StandardMaterial3D:
				var sm := m as StandardMaterial3D
				sm.metallic = minf(sm.metallic, 0.2)
				sm.roughness = maxf(sm.roughness, 0.45)


static func _aplica_laco(player: AnimationPlayer, aliases: Dictionary) -> void:
	for contrato in ANIMS_EM_LACO:
		if not aliases.has(contrato):
			continue
		var anim := player.get_animation(aliases[contrato])
		if anim != null:
			anim.loop_mode = Animation.LOOP_LINEAR


static func _resolve_animation_aliases(player: AnimationPlayer) -> Dictionary:
	var out := {}
	var names_by_norm := {}
	for anim_name in player.get_animation_list():
		names_by_norm[_normalized_anim_name(anim_name)] = anim_name
	for contract in REQUIRED_ANIMS:
		for alias in ANIM_ALIASES[contract]:
			var norm := _normalized_anim_name(alias)
			if names_by_norm.has(norm):
				out[contract] = names_by_norm[norm]
				break
	return out


static func _normalized_anim_name(name: String) -> String:
	var out := name
	var pipe := out.rfind("|")
	if pipe >= 0 and pipe < out.length() - 1:
		out = out.substr(pipe + 1)
	out = out.to_lower()
	out = out.replace("-", "_").replace(" ", "_")
	while out.find("__") >= 0:
		out = out.replace("__", "_")
	return out.strip_edges()


func _active_player() -> AnimationPlayer:
	return _external_player if _external_player != null else _player


static func _find_animation_player(root: Node) -> AnimationPlayer:
	if root is AnimationPlayer:
		return root
	for c in root.get_children():
		var found := _find_animation_player(c)
		if found != null:
			return found
	return null


func _collect_external_tint_mats(root: Node) -> void:
	for n in root.find_children("*", "MeshInstance3D", true, false):
		var mi := n as MeshInstance3D
		if mi.material_override != null:
			_tint_mats.append(mi.material_override.duplicate())
			mi.material_override = _tint_mats.back()
			continue
		if mi.mesh == null:
			continue
		for s in mi.mesh.get_surface_count():
			var mat := mi.get_surface_override_material(s)
			if mat == null:
				mat = mi.mesh.surface_get_material(s)
			if mat != null:
				var copy := mat.duplicate()
				mi.set_surface_override_material(s, copy)
				_tint_mats.append(copy)


func _make_model_report(root: Node, player: AnimationPlayer, source: String) -> Dictionary:
	var report := {
		"source": source,
		"mesh_instances": 0,
		"vertices": 0,
		"triangles": 0,
		"surface_count": 0,
		"material_slots": 0,
		"bones": 0,
		"animations": _anim_map.duplicate(),
	}
	_accumulate_mesh_report(root, report)
	_accumulate_skeleton_report(root, report)
	for n in root.find_children("*", "MeshInstance3D", true, false):
		_accumulate_mesh_report(n, report)
	for s in root.find_children("*", "Skeleton3D", true, false):
		_accumulate_skeleton_report(s, report)
	if player != null:
		report["animation_count"] = player.get_animation_list().size()
	return report


func _accumulate_mesh_report(n: Node, report: Dictionary) -> void:
	if not (n is MeshInstance3D):
		return
	var mi := n as MeshInstance3D
	report["mesh_instances"] += 1
	if mi.mesh == null:
		return
	for s in mi.mesh.get_surface_count():
		report["surface_count"] += 1
		var arrays: Array = mi.mesh.surface_get_arrays(s)
		var verts := (arrays[Mesh.ARRAY_VERTEX] as PackedVector3Array).size()
		report["vertices"] += verts
		report["triangles"] += _triangulos_da_superficie(mi.mesh, s, arrays, verts)
		if mi.material_override != null or mi.get_surface_override_material(s) != null \
				or mi.mesh.surface_get_material(s) != null:
			report["material_slots"] += 1


## Triangulos de UMA superficie, respeitando o tipo de primitiva.
##
## POR QUE NAO E' vertices/3: quando a superficie e' INDEXADA, os vertices sao
## reaproveitados e o numero de triangulos sai da lista de INDICES. E costura de
## UV duplica vertice sem criar triangulo nenhum — foi por isso que Pyra
## (22.561 vertices) e Brok (29.212) reprovavam num teto de 20.000 estando os
## dois dentro do orcamento real, que sempre foi em TRIANGULO.
static func _triangulos_da_superficie(malha: Mesh, s: int, arrays: Array, verts: int) -> int:
	var prim: int = malha.surface_get_primitive_type(s)
	var indices := 0
	if arrays.size() > Mesh.ARRAY_INDEX and arrays[Mesh.ARRAY_INDEX] != null:
		indices = (arrays[Mesh.ARRAY_INDEX] as PackedInt32Array).size()
	var n := indices if indices > 0 else verts
	match prim:
		Mesh.PRIMITIVE_TRIANGLES:
			return n / 3
		Mesh.PRIMITIVE_TRIANGLE_STRIP:
			return maxi(n - 2, 0)
		_:
			return 0   # linha ou ponto nao e' superficie: nao conta orcamento


func _accumulate_skeleton_report(n: Node, report: Dictionary) -> void:
	if n is Skeleton3D:
		report["bones"] += (n as Skeleton3D).get_bone_count()


## Porte + adereços do mago. Tudo sobre o MESMO corpo — ver mage_identity.gd.
func _apply_identity(rig: Node3D, idf: Dictionary) -> void:
	# PORTE: a ALTURA escala o Rig, e UNIFORMEMENTE. Escala nao-uniforme aqui
	# cisalharia ombreiras, bracos e capuz (todos rotacionados/animados) — a
	# largura (bulk) e' aplicada no manto e nos offsets dos bracos, em _build().
	var s: float = float(idf["height"]) / IDS.REF_HEIGHT
	rig.scale = Vector3(s, s, s)

	for path in idf["resize"]:
		var n := rig.get_node_or_null(NodePath(path))
		if n is Node3D:
			(n as Node3D).scale *= idf["resize"][path]

	for path in idf["recolor"]:
		var n := rig.get_node_or_null(NodePath(path))
		if n is MeshInstance3D:
			var r: Dictionary = idf["recolor"][path]
			var c := Color(r.get("col", idf["accent"]))
			var g: float = r.get("glow", 0.0)
			(n as MeshInstance3D).material_override = _mat(
				r.get("mat", "cloth"), c, c if g > 0.0 else Color.BLACK, g)

	for p in idf["props"]:
		var host := rig.get_node_or_null(NodePath(p["at"]))
		if host == null:
			push_warning("Mage: prop '%s' sem host '%s'" % [p["n"], p["at"]])
			continue
		var col := Color(p.get("col", idf["accent"]))
		var glow: float = p.get("glow", 0.0)
		var mat := _mat(p.get("mat", "cloth"), col,
			col if glow > 0.0 else Color.BLACK, glow)
		var mesh := _lathe(p["key"], p["prof"], p.get("sides", 6), p.get("opts", {}))
		if p.has("orbit"):
			# companheiro: o pivo gira, a peca fica no raio (o corvo do Corvomante)
			var pivot := Node3D.new()
			pivot.name = String(p["n"]) + "Orbit"
			pivot.position = p.get("pos", Vector3.ZERO)
			host.add_child(pivot)
			_orbits.append(pivot)
			_mi(pivot, p["n"], mesh, mat,
				Vector3(float(p["orbit"]), 0, 0), p.get("rot", Vector3.ZERO))
		else:
			var mi := _mi(host, p["n"], mesh, mat,
				p.get("pos", Vector3.ZERO), p.get("rot", Vector3.ZERO))
			mi.scale = p.get("scale", Vector3.ONE)


func _mat(kind: String, albedo: Color, emission := Color.BLACK,
		energy := 0.0) -> ShaderMaterial:
	var m := ShaderMaterial.new()
	m.shader = SHADER
	m.set_shader_parameter("albedo", albedo)
	m.set_shader_parameter("emission_color", emission)
	m.set_shader_parameter("emission_energy", energy)
	var preset: Dictionary = MAT.get(kind, MAT["cloth"])
	for k in preset:
		m.set_shader_parameter(k, preset[k])
	# rim na cor do proprio material: dourado brilha dourado, magia brilha magia
	m.set_shader_parameter("rim_color", albedo.lightened(0.45))
	return m


func _mi(parent: Node, nm: String, mesh: Mesh, mat: Material,
		pos := Vector3.ZERO, rot := Vector3.ZERO) -> MeshInstance3D:
	var mi := MeshInstance3D.new()
	mi.name = nm
	mi.mesh = mesh
	mi.material_override = mat
	mi.position = pos
	mi.rotation = rot
	parent.add_child(mi)
	return mi


## Revolve um perfil (x=altura, y=raio, z=recuo em Z do anel) em torno de Y.
## Nao-indexado + generate_normals() = flat shading facetado.
## opts: cap_top/cap_bottom (bool), scallop (bainha zigue-zague no anel 0),
##       a0/sweep (revolucao parcial, ex.: capuz aberto na frente),
##       rscale (escala de raio), inside (inverte faces — forro interno),
##       ao (0..1: oclusao ASSADA no vertice, escura no inicio do perfil e limpa
##           no fim; e' o que da' dobra ao manto e sombra sob a manga de graca).
static func _lathe(key: String, profile: Array, sides: int, opts: Dictionary) -> ArrayMesh:
	if _mesh_cache.has(key):
		return _mesh_cache[key]
	var a0: float = opts.get("a0", 0.0)
	var sweep: float = opts.get("sweep", TAU)
	var wrap := is_equal_approx(sweep, TAU)
	var inside: bool = opts.get("inside", false)
	var rscale: float = opts.get("rscale", 1.0)
	var scallop: float = opts.get("scallop", 0.0)
	var ao: float = opts.get("ao", 0.0)
	var cols := sides if wrap else sides + 1
	var y0: float = profile[0].x
	var y1: float = profile[profile.size() - 1].x

	var rings: Array = []
	for i in profile.size():
		var p: Vector3 = profile[i]
		var ring := PackedVector3Array()
		for j in cols:
			var a := a0 + sweep * float(j) / float(sides)
			var r := p.y * rscale
			var y := p.x
			if scallop > 0.0 and i == 0 and j % 2 == 1:
				r *= 1.0 - scallop
				y += scallop * 0.30
			ring.append(Vector3(cos(a) * r, y, sin(a) * r + p.z))
		rings.append(ring)

	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	for i in rings.size() - 1:
		var lo: PackedVector3Array = rings[i]
		var hi: PackedVector3Array = rings[i + 1]
		for j in sides:
			var jn := (j + 1) % cols if wrap else j + 1
			_tri(st, lo[j], lo[jn], hi[jn], inside, ao, y0, y1)
			_tri(st, lo[j], hi[jn], hi[j], inside, ao, y0, y1)
	if opts.get("cap_bottom", false):
		var p0: Vector3 = profile[0]
		var c0 := Vector3(0.0, p0.x, p0.z)
		var r0: PackedVector3Array = rings[0]
		for j in sides:
			var jn := (j + 1) % cols if wrap else j + 1
			_tri(st, c0, r0[jn], r0[j], inside, ao, y0, y1)
	if opts.get("cap_top", false):
		var pt: Vector3 = profile[profile.size() - 1]
		var ct := Vector3(0.0, pt.x, pt.z)
		var rt: PackedVector3Array = rings[rings.size() - 1]
		for j in sides:
			var jn := (j + 1) % cols if wrap else j + 1
			_tri(st, ct, rt[j], rt[jn], inside, ao, y0, y1)
	st.generate_normals()
	var mesh := st.commit()
	_mesh_cache[key] = mesh
	return mesh


## Disco/losango plano voltado para -Z (frente do personagem).
static func _disc(key: String, r: float, sides: int) -> ArrayMesh:
	if _mesh_cache.has(key):
		return _mesh_cache[key]
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	for j in sides:
		var a := TAU * float(j) / float(sides)
		var an := TAU * float(j + 1) / float(sides)
		st.set_color(Color.WHITE)
		st.add_vertex(Vector3.ZERO)
		st.set_color(Color.WHITE)
		st.add_vertex(Vector3(cos(a) * r, sin(a) * r, 0.0))
		st.set_color(Color.WHITE)
		st.add_vertex(Vector3(cos(an) * r, sin(an) * r, 0.0))
	st.generate_normals()
	var mesh := st.commit()
	_mesh_cache[key] = mesh
	return mesh


static func _tri(st: SurfaceTool, a: Vector3, b: Vector3, c: Vector3, flip: bool,
		ao: float, y0: float, y1: float) -> void:
	var vs := [a, c, b] if flip else [a, b, c]
	for v in vs:
		st.set_color(_ao(v.y, ao, y0, y1))
		st.add_vertex(v)


## AO assado: mais escuro no inicio do perfil (barra do manto, axila da manga)
## e limpo no fim. Custo ZERO em runtime — e' so' um COLOR no vertice.
static func _ao(y: float, ao: float, y0: float, y1: float) -> Color:
	if ao <= 0.0 or is_equal_approx(y0, y1):
		return Color.WHITE
	var t := clampf(inverse_lerp(y0, y1, y), 0.0, 1.0)
	var v := 1.0 - ao * (1.0 - t) * (1.0 - t)
	return Color(v, v, v)


# ----------------------------------------------------------------- animacoes

const _P := "Rig/Hips/Torso/"          # prefixo dos membros


func _new_anim(length: float, looped: bool) -> Animation:
	var a := Animation.new()
	a.length = length
	a.loop_mode = Animation.LOOP_LINEAR if looped else Animation.LOOP_NONE
	return a


func _tr(a: Animation, path: String, keys: Array) -> void:
	var t := a.add_track(Animation.TYPE_VALUE)
	a.track_set_path(t, NodePath(path))
	a.track_set_interpolation_type(t, Animation.INTERPOLATION_CUBIC)
	for k in keys:
		a.track_insert_key(t, k[0], k[1])


## Respiro. O peso vem do ATRASO: o manto e o capuz chegam ao pico DEPOIS do
## tronco (1.5s / 1.45s contra 1.2s). Sem esse atraso o boneco e' uma peca so'.
func _anim_idle() -> Animation:
	var a := _new_anim(2.4, true)
	_tr(a, "Rig:position", [[0.0, Vector3.ZERO], [1.2, Vector3(0, 0.02, 0)], [2.4, Vector3.ZERO]])
	# leve troca de apoio de um pe' para o outro — tira a rigidez de manequim
	_tr(a, "Rig:rotation", [
		[0.0, Vector3(0, 0, 0.012)], [1.2, Vector3(0, 0, -0.012)], [2.4, Vector3(0, 0, 0.012)]])
	_tr(a, _P.trim_suffix("/") + ":rotation",
		[[0.0, Vector3(0.02, 0, 0)], [1.2, Vector3(0.055, 0, 0)], [2.4, Vector3(0.02, 0, 0)]])
	_tr(a, "Rig/Hips/Robe:rotation",
		[[0.0, Vector3(0.006, 0, -0.004)], [1.5, Vector3(0.035, 0, 0.02)],
		[2.4, Vector3(0.006, 0, -0.004)]])
	# a escala do manto CARREGA o bulk da raca — a track sobrescreve o node
	_tr(a, "Rig/Hips/Robe:scale",
		[[0.0, _robe_scale(1.0)], [1.5, _robe_scale(1.02)], [2.4, _robe_scale(1.0)]])
	_tr(a, _P + "Head:rotation",
		[[0.0, Vector3.ZERO], [1.45, Vector3(0.04, 0, 0)], [2.4, Vector3.ZERO]])
	_tr(a, _P + "ArmL:rotation",
		[[0.0, Vector3(0.04, 0, -0.14)], [1.35, Vector3(0.09, 0, -0.18)],
		[2.4, Vector3(0.04, 0, -0.14)]])
	_tr(a, _P + "ArmR:rotation",
		[[0.0, Vector3(0.04, 0, 0.14)], [1.35, Vector3(0.09, 0, 0.18)],
		[2.4, Vector3(0.04, 0, 0.14)]])
	_tr(a, _P + "ArmR/HandR:scale", [[0.0, Vector3.ONE]])
	_tr(a, _P + "ArmL/HandL:scale", [[0.0, Vector3.ONE]])
	return a


## Corrida. Peso = o quadril CAI no contato (nao so' sobe), o corpo rola de um
## lado para o outro, e a barra do manto chega 0.08s atrasada em relacao ao passo.
func _anim_run() -> Animation:
	var a := _new_anim(0.6, true)
	_tr(a, "Rig:rotation", [                                    # inclinado + rolagem
		[0.0, Vector3(-0.12, 0, 0.045)], [0.3, Vector3(-0.12, 0, -0.045)],
		[0.6, Vector3(-0.12, 0, 0.045)]])
	_tr(a, "Rig:position", [
		[0.0, Vector3(0, 0.005, 0)], [0.15, Vector3(0, 0.06, 0)], [0.3, Vector3(0, 0.005, 0)],
		[0.45, Vector3(0, 0.06, 0)], [0.6, Vector3(0, 0.005, 0)]])
	_tr(a, _P + "ArmL:rotation", [
		[0.0, Vector3(0.6, 0, -0.12)], [0.3, Vector3(-0.6, 0, -0.12)], [0.6, Vector3(0.6, 0, -0.12)]])
	_tr(a, _P + "ArmR:rotation", [
		[0.0, Vector3(-0.6, 0, 0.12)], [0.3, Vector3(0.6, 0, 0.12)], [0.6, Vector3(-0.6, 0, 0.12)]])
	_tr(a, _P.trim_suffix("/") + ":rotation", [
		[0.0, Vector3(0.08, 0.10, 0)], [0.3, Vector3(0.08, -0.10, 0)], [0.6, Vector3(0.08, 0.10, 0)]])
	_tr(a, _P + "Head:rotation", [
		[0.0, Vector3(0.10, -0.06, 0)], [0.3, Vector3(0.10, 0.06, 0)], [0.6, Vector3(0.10, -0.06, 0)]])
	# barra do manto: mesma frequencia, 0.08s ATRASADA -> arrasta em vez de colar
	_tr(a, "Rig/Hips/Robe:rotation", [
		[0.0, Vector3(-0.185, 0, -0.018)], [0.08, Vector3(-0.20, 0, 0)],
		[0.23, Vector3(-0.13, 0, 0.022)], [0.38, Vector3(-0.20, 0, 0)],
		[0.53, Vector3(-0.13, 0, -0.022)], [0.6, Vector3(-0.185, 0, -0.018)]])
	_tr(a, "Rig/Hips/Robe:scale", [[0.0, _robe_scale(1.0)]])
	_tr(a, _P + "ArmR/HandR:scale", [[0.0, Vector3.ONE]])
	_tr(a, _P + "ArmL/HandL:scale", [[0.0, Vector3.ONE]])
	return a


## Escala do manto ja' com a largura da raca embutida (o anao e' largo tambem
## no meio de uma animacao, nao so' parado).
func _robe_scale(k: float) -> Vector3:
	return Vector3(k * _bulk, 1.0, k * _bulk)


## Conjuracao. Tres tempos de animacao classica:
##   ANTECIPACAO (0.0-0.10)     o corpo recua e o capuz puxa para tras
##   DISPARO     (0.22)         a mao abre — e' aqui que sai o projetil (cast_fired)
##   FOLLOW-THROUGH (0.32-0.55) o manto ULTRAPASSA e volta; o braco assenta depois
## QUEDA LIVRE. Sem paraquedas e sem magia (ordem do Diretor): o corpo cai de
## barriga para baixo, bracos abertos e joelhos dobrados, com a instabilidade de
## quem NAO controla o ar. O manto e' o que mais denuncia a velocidade — ele
## sobe e treme, em vez de acompanhar o corpo.
func _anim_cair() -> Animation:
	var a := _new_anim(1.1, true)
	_tr(a, "Rig:rotation", [                                   # inclinado p/ frente, oscilando
		[0.0, Vector3(-0.85, 0, 0.10)], [0.55, Vector3(-0.92, 0, -0.10)],
		[1.1, Vector3(-0.85, 0, 0.10)]])
	_tr(a, "Rig:position", [
		[0.0, Vector3(0, 0.04, 0)], [0.55, Vector3(0, -0.02, 0)], [1.1, Vector3(0, 0.04, 0)]])
	_tr(a, _P + "ArmL:rotation", [                             # bracos abertos e tremendo
		[0.0, Vector3(-0.5, 0, -1.25)], [0.35, Vector3(-0.35, 0, -1.45)],
		[0.75, Vector3(-0.55, 0, -1.30)], [1.1, Vector3(-0.5, 0, -1.25)]])
	_tr(a, _P + "ArmR:rotation", [
		[0.0, Vector3(-0.5, 0, 1.25)], [0.35, Vector3(-0.55, 0, 1.30)],
		[0.75, Vector3(-0.35, 0, 1.45)], [1.1, Vector3(-0.5, 0, 1.25)]])
	_tr(a, _P + "Head:rotation", [                             # olhando para o chao que vem
		[0.0, Vector3(0.45, -0.05, 0)], [0.55, Vector3(0.5, 0.05, 0)],
		[1.1, Vector3(0.45, -0.05, 0)]])
	# manto SOBE: o ar empurra de baixo. E' o sinal mais legivel de velocidade.
	_tr(a, "Rig/Hips/Robe:rotation", [
		[0.0, Vector3(0.62, 0, -0.06)], [0.28, Vector3(0.70, 0, 0.05)],
		[0.62, Vector3(0.58, 0, -0.05)], [1.1, Vector3(0.62, 0, -0.06)]])
	_tr(a, "Rig/Hips/Robe:scale", [[0.0, _robe_scale(1.0)]])
	_tr(a, _P + "ArmR/HandR:scale", [[0.0, Vector3.ONE]])
	_tr(a, _P + "ArmL/HandL:scale", [[0.0, Vector3.ONE]])
	return a


## PLANEIO. O oposto da queda livre: o corpo ACHA o eixo. Menos amplitude, mais
## alinhamento — bracos firmes e abertos, cabeca no horizonte, manto esticado
## para tras como asa. O jogador tem que LER pelo corpo que ganhou controle,
## sem depender de icone na tela.
func _anim_planar() -> Animation:
	var a := _new_anim(2.0, true)
	_tr(a, "Rig:rotation", [
		[0.0, Vector3(-1.05, 0, 0.035)], [1.0, Vector3(-1.09, 0, -0.035)],
		[2.0, Vector3(-1.05, 0, 0.035)]])
	_tr(a, "Rig:position", [
		[0.0, Vector3(0, 0.02, 0)], [1.0, Vector3(0, 0.05, 0)], [2.0, Vector3(0, 0.02, 0)]])
	_tr(a, _P + "ArmL:rotation", [                             # firmes: controle
		[0.0, Vector3(-0.18, 0, -1.48)], [1.0, Vector3(-0.22, 0, -1.52)],
		[2.0, Vector3(-0.18, 0, -1.48)]])
	_tr(a, _P + "ArmR:rotation", [
		[0.0, Vector3(-0.18, 0, 1.48)], [1.0, Vector3(-0.22, 0, 1.52)],
		[2.0, Vector3(-0.18, 0, 1.48)]])
	_tr(a, _P + "Head:rotation", [[0.0, Vector3(0.22, 0, 0)]])  # horizonte, nao o chao
	_tr(a, "Rig/Hips/Robe:rotation", [                          # esticado, quase sem tremer
		[0.0, Vector3(0.80, 0, -0.02)], [1.0, Vector3(0.84, 0, 0.02)],
		[2.0, Vector3(0.80, 0, -0.02)]])
	_tr(a, "Rig/Hips/Robe:scale", [[0.0, _robe_scale(1.0)]])
	_tr(a, _P + "ArmR/HandR:scale", [[0.0, Vector3.ONE]])
	_tr(a, _P + "ArmL/HandL:scale", [[0.0, Vector3.ONE]])
	return a


## PEGAR / ABRIR. Disparo unico, curto de proposito: o dedo aperta e o gesto
## precisa terminar antes do proximo toque. Agacha, estende o braco direito e
## volta. Serve tanto para item no chao quanto para canalizar o bau.
func _anim_pegar() -> Animation:
	var a := _new_anim(0.65, false)
	_tr(a, "Rig:position", [                                   # agacha e volta
		[0.0, Vector3.ZERO], [0.22, Vector3(0, -0.16, 0)],
		[0.42, Vector3(0, -0.14, 0)], [0.65, Vector3.ZERO]])
	_tr(a, "Rig:rotation", [
		[0.0, Vector3.ZERO], [0.22, Vector3(0.22, 0, 0)],
		[0.42, Vector3(0.20, 0, 0)], [0.65, Vector3.ZERO]])
	_tr(a, _P + "ArmR:rotation", [                             # o braco que pega
		[0.0, Vector3(0.04, 0, 0.14)], [0.24, Vector3(0.95, 0, 0.30)],
		[0.44, Vector3(0.80, 0, 0.22)], [0.65, Vector3(0.04, 0, 0.14)]])
	_tr(a, _P + "ArmL:rotation", [                             # o outro equilibra
		[0.0, Vector3(0.04, 0, -0.14)], [0.24, Vector3(-0.25, 0, -0.30)],
		[0.65, Vector3(0.04, 0, -0.14)]])
	_tr(a, _P + "Head:rotation", [                             # olha para o que pega
		[0.0, Vector3.ZERO], [0.24, Vector3(0.42, 0, 0)], [0.65, Vector3.ZERO]])
	_tr(a, "Rig/Hips/Robe:rotation", [
		[0.0, Vector3(0.006, 0, 0)], [0.26, Vector3(-0.10, 0, 0)],
		[0.65, Vector3(0.006, 0, 0)]])
	_tr(a, "Rig/Hips/Robe:scale", [[0.0, _robe_scale(1.0)]])
	_tr(a, _P + "ArmR/HandR:scale", [[0.0, Vector3.ONE]])
	_tr(a, _P + "ArmL/HandL:scale", [[0.0, Vector3.ONE]])
	return a


## DERRUBADO. Estado SUSTENTADO, entao anda EM LACO — nao e' disparo unico como
## `pegar`. Ate' 26/08 o caido continuava usando a animacao de locomocao mais
## lenta: ficava DE PE', parado, no meio da partida, e o unico sinal de que
## estava caido era a HUD. Num battle royale isso e' informacao errada — o
## aliado precisa LER pelo corpo, de longe e sem icone, quem da' para reerguer.
##
## VOCABULARIO 10+ (mesma lei de gameplay/Derrubado.gd): ninguem sangra e
## ninguem se contorce. O mago esta' CAIDO e APOIADO num braco, respirando
## pesado. A cada volta ele LEVANTA A CABECA procurando quem vem — esse beat e'
## de proposito: e' o corpo dizendo "ainda da' tempo", que e' exatamente a
## informacao que o estado carrega.
##
## Lento de proposito (3,4 s). A leitura tem que ser "parado, vivo, esperando",
## e nao "agitado" — agitacao a esta distancia le' como combate.
func _anim_derrubado() -> Animation:
	var a := _new_anim(3.4, true)
	# no chao, tombado para o lado do braco que apoia. O respiro e' a UNICA
	# coisa que se mexe muito: sem ele o caido le' como cadaver, e cadaver nao
	# se reergue — a silhueta estaria mentindo sobre a regra do jogo.
	# QUADRIL no chao, corpo tombado para o lado do braco que apoia. A primeira
	# versao usava -0.58 e pitch 0.78: fotografada, o mago AFUNDAVA no terreno e
	# lia como amontoado de pano, nao como alguem apoiado. Menos queda e menos
	# inclinacao — quem sustenta o peito e' o tronco, logo abaixo.
	_tr(a, "Rig:position", [
		[0.0, Vector3(0, -0.55, 0)], [1.7, Vector3(0, -0.522, 0)],
		[3.4, Vector3(0, -0.55, 0)]])
	_tr(a, "Rig:rotation", [
		[0.0, Vector3(0.30, 0, 0.40)], [1.7, Vector3(0.27, 0, 0.38)],
		[3.4, Vector3(0.30, 0, 0.40)]])
	# esquerdo APOIA: estendido para tras e para baixo, quase sem se mexer — e'
	# ele que sustenta o peso, e peso sustentado nao balanca.
	_tr(a, _P + "ArmL:rotation", [
		[0.0, Vector3(-0.30, 0, -1.15)], [1.7, Vector3(-0.34, 0, -1.12)],
		[3.4, Vector3(-0.30, 0, -1.15)]])
	# direito LIVRE: pende sobre o colo e, no beat da cabeca, se estende um
	# pouco — o comeco de um gesto de chamar que o corpo nao termina.
	_tr(a, _P + "ArmR:rotation", [
		[0.0, Vector3(0.55, 0, 0.42)], [1.5, Vector3(0.30, 0, 0.62)],
		[2.2, Vector3(0.42, 0, 0.52)], [3.4, Vector3(0.55, 0, 0.42)]])
	# a cabeca: baixa, e SOBE uma vez por volta. E' o beat que diz "ainda da' tempo".
	_tr(a, _P + "Head:rotation", [
		[0.0, Vector3(0.40, 0.12, 0)], [1.4, Vector3(-0.06, -0.16, 0)],
		[2.1, Vector3(0.08, -0.10, 0)], [3.4, Vector3(0.40, 0.12, 0)]])
	# TRONCO: e' aqui que o peito se levanta. Sem esta contra-rotacao o mago
	# fica de bruces e some no chao — foi o defeito da primeira foto. O respiro
	# pesado tambem mora aqui, defasado do quadril (mesma licao do _anim_idle:
	# sem atraso entre as partes o boneco e' uma peca so').
	_tr(a, _P.trim_suffix("/") + ":rotation", [
		[0.0, Vector3(-0.46, 0.08, -0.10)], [1.9, Vector3(-0.52, 0.05, -0.09)],
		[3.4, Vector3(-0.46, 0.08, -0.10)]])
	# manto ESPALHADO no chao: para de acompanhar o corpo e vira pano parado.
	_tr(a, "Rig/Hips/Robe:rotation", [
		[0.0, Vector3(-0.14, 0, 0.05)], [1.9, Vector3(-0.12, 0, 0.04)],
		[3.4, Vector3(-0.14, 0, 0.05)]])
	_tr(a, "Rig/Hips/Robe:scale", [[0.0, _robe_scale(1.0)]])
	_tr(a, _P + "ArmR/HandR:scale", [[0.0, Vector3.ONE]])
	_tr(a, _P + "ArmL/HandL:scale", [[0.0, Vector3.ONE]])
	return a


func _anim_cast() -> Animation:
	var a := _new_anim(0.55, false)
	_tr(a, _P + "ArmR:rotation", [
		[0.0, Vector3(0.04, 0, 0.14)], [0.10, Vector3(-0.62, 0, 0.32)],
		[CAST_FIRE_TIME, Vector3(1.45, 0, 0.04)], [0.32, Vector3(1.52, 0, 0.02)],
		[0.42, Vector3(1.22, 0, 0.07)], [0.55, Vector3(0.04, 0, 0.14)]])
	_tr(a, _P + "ArmR/HandR:scale", [
		[0.0, Vector3.ONE], [0.10, Vector3(0.78, 0.78, 0.78)],
		[0.24, Vector3(1.32, 1.32, 1.32)], [0.42, Vector3(1.12, 1.12, 1.12)],
		[0.55, Vector3.ONE]])
	_tr(a, _P + "ArmL:rotation", [
		[0.0, Vector3(0.04, 0, -0.14)], [0.10, Vector3(0.16, 0, -0.24)],
		[CAST_FIRE_TIME, Vector3(-0.38, 0, -0.20)], [0.40, Vector3(-0.22, 0, -0.18)],
		[0.55, Vector3(0.04, 0, -0.14)]])
	_tr(a, _P.trim_suffix("/") + ":rotation", [
		[0.0, Vector3(0.02, 0, 0)], [0.10, Vector3(0.09, -0.32, 0)],
		[CAST_FIRE_TIME, Vector3(-0.03, 0.30, 0)], [0.36, Vector3(0.0, 0.22, 0)],
		[0.55, Vector3(0.02, 0, 0)]])
	# capuz: PUXA para tras antes (antecipacao) e chega no alvo depois do braco
	_tr(a, _P + "Head:rotation", [
		[0.0, Vector3.ZERO], [0.10, Vector3(-0.10, -0.05, 0)],
		[0.28, Vector3(0.12, 0.07, 0)], [0.55, Vector3.ZERO]])
	_tr(a, "Rig:position", [
		[0.0, Vector3.ZERO], [0.10, Vector3(0, 0.012, 0)],
		[CAST_FIRE_TIME, Vector3(0, -0.03, 0)], [0.55, Vector3.ZERO]])
	_tr(a, "Rig:rotation", [[0.0, Vector3.ZERO]])
	# manto: ULTRAPASSA no 0.34 e so' ai' assenta — o follow-through que da' peso
	_tr(a, "Rig/Hips/Robe:rotation", [
		[0.0, Vector3.ZERO], [0.10, Vector3(0.05, 0, 0)],
		[CAST_FIRE_TIME, Vector3(-0.10, 0, 0)], [0.34, Vector3(-0.14, 0, 0.03)],
		[0.46, Vector3(0.04, 0, -0.015)], [0.55, Vector3.ZERO]])
	# marcador do disparo: a propria anim avisa a raia de gameplay
	var t := a.add_track(Animation.TYPE_METHOD)
	a.track_set_path(t, NodePath("."))
	a.track_insert_key(t, CAST_FIRE_TIME, {"method": "_cast_fire", "args": []})
	return a
