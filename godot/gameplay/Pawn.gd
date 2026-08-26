## Base de player e bots: hp, visual (Mage.tscn com FALLBACK — fiacao
## defensiva obrigatoria) e a VELOCIDADE como produto unico.
class_name Pawn
extends CharacterBody3D

const MAGE_SCENE := "res://characters/Mage.tscn"
const KNOCK_DECAY := 9.0  # m/s^2 — KNOB local; pedir entrada em Balance se sobreviver ao playtest

var hp: float = float(Balance.PLAYER.hp)
var element := "fire"    # padrao do carrossel (Balance.ELEMENTS)
var speed_factor := 1.0  # bots reduzem aqui, nunca no Balance
## AGUA (26/08 — DIRECAO.md §3). `agua_mult` entra no PRODUTO UNICO da
## velocidade como os demais fatores — nunca escreve m/s direto:
##   nadando          0.55  "natural, nada de muito rapido"
##   saindo molhado   0.8   "fisica de roupas molhadas", MOLHADO_S segundos
## O nado comeca quando a lamina cobre o PEITO (1.2 m) — pisar numa poca nao
## e' nadar. Quem responde onde ha' agua e' a ilha (agua_y, grupo "ilha");
## sem ilha na arvore, tudo isto vale 1.0 e nada quebra.
const NADO_MULT := 0.55
const MOLHADO_MULT := 0.8
const MOLHADO_S := 2.5
const PEITO := 1.2
var agua_mult := 1.0
var nadando := false
var _molhado_s := 0.0
var _sup_nado := 0.0
var _ilha: Node3D
var terrain_mult := 1.0  # G2 (gelo/agua) escreve aqui
var status_mult := 1.0   # lentidao/buff escrevem aqui
var iframes_left := 0.0  # esquiva: Combat.deal barra dano enquanto > 0
## ESCUDO DE MAGIA EVOLUTIVO (GDD §5). Quem escreve nestes tres e' o Combat —
## ponto unico. A HUD observa por Bus.shield_changed/shield_broken.
var shield: float = float(Balance.ESCUDO.niveis[0])
var shield_level := 1
var dmg_dealt := 0.0     # dano causado em MAGO INIMIGO (anti-farm no Combat)
## ESTADOS (docs/DANO.md §3.4). Campos RASOS, nao dicionario: zero alocacao por
## tique no mobile, e a exclusividade por categoria sai de graca — acender()
## zera o molhado, molhar() apaga a queimadura. A reacao SUBSTITUI, nunca soma.
var burn_dps := 0.0
var burn_left := 0.0
var wet_left := 0.0
var slow_left := 0.0
var stun_left := 0.0
var knockback := Vector3.ZERO
var visual: Node3D
var _dodge_left := 0.0
var _dodge_cd := 0.0
var _dodge_dir := Vector3.ZERO
var _cur_anim := ""
var _move_vel := Vector3.ZERO   # velocidade horizontal INTENCIONAL (sem knockback)
var _run_anim := false          # estado da histerese idle<->run
var _prev_yaw := 0.0            # giro do frame anterior (banking)
var _anim_speed := 1.0          # speed_scale aplicado (evita reescrever igual)
var _anim_ref := -1.0           # m/s em que a anim "run" roda natural (cache)
var _dot_acc := 0.0             # relogio do tique de DoT (Balance.DOT.tick)
var _dot_bucket := 0.0          # dano de DoT acumulado desde o ultimo tique
var _gravity: float = float(ProjectSettings.get_setting("physics/3d/default_gravity", 9.8))


## Velocidade e' PRODUTO UNICO (regra provada) — ninguem escreve m/s direto.
func current_speed() -> float:
	return float(Balance.PLAYER.speed) * terrain_mult * status_mult * speed_factor * agua_mult


## Caminho UNICO da velocidade horizontal: produto unico + dash + knockback.
## Player e Bot passam por aqui — ninguem escreve velocity.x/z por fora.
func move_velocity(dir: Vector3, delta: float) -> void:
	status_step(delta)  # DoT (terreno + queimadura) e relogio dos estados
	_agua_step(delta)
	_dodge_cd = maxf(_dodge_cd - delta, 0.0)
	iframes_left = maxf(iframes_left - delta, 0.0)
	knockback = knockback.move_toward(Vector3.ZERO, KNOCK_DECAY * delta)
	if stun_left > 0.0:
		dir = Vector3.ZERO  # atordoado: o corpo nao obedece (teto STATUS.stun_cap)
	if _dodge_left > 0.0:
		var dur := float(Balance.DODGE.duration)
		# amostra no MEIO do passo: como a rampa e' linear, a soma dos passos da'
		# exatamente a distancia configurada (sem isso o dash fica ~5% curto).
		var p := clampf((dur - _dodge_left + delta * 0.5) / dur, 0.0, 1.0)
		_dodge_left -= delta
		var v := dash_speed_at(p)
		velocity.x = _dodge_dir.x * v
		velocity.z = _dodge_dir.z * v
		# momentum: ao acabar o dash o corpo JA' esta' correndo pra la',
		# nao precisa reacelerar do zero (e' o que da' a sensacao de agilidade).
		_move_vel = _dodge_dir * current_speed() * float(Balance.DODGE.exit_momentum)
		_update_lean(delta)
		return
	## Aceleracao/inercia: a velocidade PERSEGUE o alvo em vez de saltar pra ele.
	_move_vel = accel_step(_move_vel, dir * current_speed(), delta, is_on_floor())
	velocity.x = _move_vel.x + knockback.x
	velocity.z = _move_vel.z + knockback.z
	_update_lean(delta)


## RELOGIO UNICO DOS ESTADOS + DoT. Roda dentro de move_velocity, o unico tique
## que player e bot ja' compartilham: nenhum estado custa um _process por
## entidade (regra mobile — 6 bots + player).
##
## Duas leis de DoT (docs/DANO.md §3.5):
##   1. TETO de dps somado — nenhum alvo perde mais que Balance.DOT.teto_dps por
##      segundo, nao importa quantas fontes o atinjam. Sem teto o jogador cai de
##      queimadura + terreno + nevoa somados, tres fontes que ele nao consegue ler.
##   2. DoT IGNORA O ESCUDO e vai direto na vida — o escudo protege contra
##      MAGIA, nao contra estar em chamas. E' o que da' dentes ao terreno (§14).
## O balde existe por causa do bug do §2.3: o terreno chamava Combat.deal a CADA
## frame de fisica com ~0.1 de dano. Agora acumula exato e descarrega a cada
## DOT.tick — um numero legivel, um som, um tween.
func status_step(delta: float) -> void:
	stun_left = maxf(stun_left - delta, 0.0)
	wet_left = maxf(wet_left - delta, 0.0)
	if slow_left > 0.0:
		slow_left = maxf(slow_left - delta, 0.0)
		if slow_left <= 0.0:
			status_mult = 1.0
	var dps := burn_dps
	if burn_left > 0.0:
		burn_left = maxf(burn_left - delta, 0.0)
		if burn_left <= 0.0:
			burn_dps = 0.0
	else:
		dps = 0.0
	# dps_at devolve 0.0 se o terreno nao montou (fiacao defensiva).
	dps += TerrainSystem.dps_at(global_position)
	_dot_bucket += minf(dps, float(Balance.DOT.teto_dps)) * delta
	_dot_acc += delta
	if _dot_acc < float(Balance.DOT.tick):
		return
	_dot_acc = 0.0
	if _dot_bucket > 0.0:
		# Dano SO por Combat (ponto unico), nunca hp direto. ignora_escudo=true.
		Combat.deal(self, _dot_bucket, "fire" if burn_dps > 0.0 else "terrain", null, true)
		_dot_bucket = 0.0


## ESTADO TERMICO EXCLUSIVO: acender apaga o molhado, molhar apaga a brasa.
## A queimadura NAO acumula em pilha — refresca (docs/DANO.md §3.3).
func acender(dps: float, dur: float) -> void:
	burn_dps = dps
	burn_left = dur
	wet_left = 0.0


func molhar(dur: float, slow: float) -> void:
	wet_left = dur
	burn_left = 0.0
	burn_dps = 0.0
	lentificar(slow, dur)


func secar() -> void:
	wet_left = 0.0
	if slow_left > 0.0:
		slow_left = 0.0
		status_mult = 1.0


## ESTADO DE MOVIMENTO EXCLUSIVO: a lentidao nova SUBSTITUI a antiga.
func lentificar(fator: float, dur: float) -> void:
	status_mult = fator
	slow_left = dur


## TETO DO KERNEL (Balance.STATUS.stun_cap). NUNCA acima de 0.8s: acima disso o
## jogador perde o controle e o jogo fica injusto.
func atordoar(s: float) -> void:
	stun_left = minf(maxf(stun_left, s), float(Balance.STATUS.stun_cap))


## Estado dominante para o icone da HUD ("" = nenhum).
func estado() -> String:
	if stun_left > 0.0:
		return "stun"
	if burn_left > 0.0:
		return "burn"
	if wet_left > 0.0:
		return "wet"
	return ""


## Capacidade do escudo no nivel atual (GDD §5). A HUD segmenta a barra por aqui.
func shield_max() -> float:
	return float(Balance.ESCUDO.niveis[clampi(shield_level - 1, 0, 3)])


## Um passo de aceleracao horizontal. Pura de proposito: e' o coracao do feel e
## o selftest prova ela direto, sem precisar de mundo fisico.
static func accel_step(cur: Vector3, target: Vector3, delta: float, grounded: bool) -> Vector3:
	var rate: float = Balance.MOVE.accel
	if target.length_squared() < 0.0001:
		rate = float(Balance.MOVE.brake)          # soltou o joystick: freia
	elif cur.dot(target) < 0.0:
		rate = float(Balance.MOVE.turn_accel)     # inverteu o sentido: strafe seco
	if not grounded:
		rate *= float(Balance.MOVE.air_control)
	return cur.move_toward(target, rate * delta)


## Velocidade do dash em p (0=inicio, 1=fim). Rampa linear de burst*media ate
## (2-burst)*media: a MEDIA continua sendo distance/duration, entao a distancia
## percorrida nao muda — so' o arranque, que agora e' no primeiro instante.
static func dash_speed_at(p: float) -> float:
	var media := float(Balance.DODGE.distance) / float(Balance.DODGE.duration)
	var burst := float(Balance.DODGE.burst)
	return media * lerpf(burst, 2.0 - burst, clampf(p, 0.0, 1.0))


## Modulo da velocidade horizontal REAL (o que a animacao e o banking leem).
func horizontal_speed() -> float:
	return Vector2(velocity.x, velocity.z).length()


## Banking: inclina o VISUAL pra dentro da curva, proporcional a quanto o corpo
## girou neste frame e a quanto ele esta' rapido. Parado nao inclina.
func _update_lean(delta: float) -> void:
	if visual == null or delta <= 0.0:
		return
	var yaw_rate := wrapf(rotation.y - _prev_yaw, -PI, PI) / delta
	_prev_yaw = rotation.y
	var frac := clampf(_move_vel.length() / maxf(current_speed(), 0.01), 0.0, 1.0)
	var alvo := clampf(yaw_rate * float(Balance.MOVE.bank_gain),
			-float(Balance.MOVE.bank_max), float(Balance.MOVE.bank_max)) * frac
	visual.rotation.z = lerpf(visual.rotation.z, alvo,
			minf(float(Balance.MOVE.bank_rate) * delta, 1.0))


## Grupo da PERCEPCAO (26/08): os bots varrem "magos" para notar quem esta'
## perto. Pawn de teste tambem entra — e' de proposito, os testes de percepcao
## usam exatamente isso.
func _enter_tree() -> void:
	add_to_group("magos")


func dodge_ready() -> bool:
	return _dodge_cd <= 0.0 and hp > 0.0 and Derrubado.pode_agir(self)


## Fracao 0..1 do cooldown de esquiva (1 = acabou de usar). A UI so' LE.
func dodge_cd_frac() -> float:
	return _dodge_cd / float(Balance.DODGE.cooldown)


## Dash na direcao do movimento (parado: para onde olha) com i-frames curtos.
## Todos os numeros vem de Balance.DODGE.
func try_dodge(dir: Vector3) -> bool:
	if not dodge_ready():
		return false
	dir.y = 0.0
	if dir.length_squared() < 0.0001:
		dir = -global_transform.basis.z
		dir.y = 0.0
	_dodge_dir = dir.normalized()
	_dodge_left = float(Balance.DODGE.duration)
	_dodge_cd = float(Balance.DODGE.cooldown)
	iframes_left = float(Balance.DODGE.iframes)
	if is_in_group("player"):
		Bus.dodge_performed.emit()  # audio/UI observam; esquiva de bot nao vira feedback global
	return true


## Empurrao transitorio (feedback de acerto) — decai sozinho em move_velocity.
func apply_knockback(v: Vector3) -> void:
	knockback += v


func _ready() -> void:
	var col := CollisionShape3D.new()
	var capsule := CapsuleShape3D.new()
	capsule.height = 1.8
	capsule.radius = 0.35
	col.shape = capsule
	col.position.y = 0.9
	add_child(col)
	visual = _build_visual()
	add_child(visual)
	_prev_yaw = rotation.y  # sem isso o 1o frame acusa um giro gigante (banking)


func _build_visual() -> Node3D:
	# A raia PERSONAGEM pode terminar depois — esta cena BOOTA de qualquer jeito.
	if ResourceLoader.exists(MAGE_SCENE):
		var packed: Variant = load(MAGE_SCENE)
		if packed is PackedScene:
			var v: Node = (packed as PackedScene).instantiate()
			if v is Node3D:
				return v
			v.free()
	return _fallback_mage()


func _fallback_mage() -> Node3D:
	var root := Node3D.new()
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(0.45, 0.35, 0.75)
	var body := MeshInstance3D.new()
	var cap := CapsuleMesh.new()
	cap.height = 1.2
	cap.radius = 0.3
	body.mesh = cap
	body.position.y = 1.2
	body.material_override = mat
	var manto := MeshInstance3D.new()
	var cyl := CylinderMesh.new()
	cyl.top_radius = 0.25
	cyl.bottom_radius = 0.55
	cyl.height = 1.0
	manto.mesh = cyl
	manto.position.y = 0.5
	manto.material_override = mat
	root.add_child(body)
	root.add_child(manto)
	root.set_meta("fallback_mat", mat)
	return root


func anim(anim_name: String) -> void:
	if anim_name != _cur_anim:
		_cur_anim = anim_name
		if visual and visual.has_method("play_anim"):
			visual.play_anim(anim_name)
	_sync_anim_speed()


## Nome da animacao de locomocao pela velocidade REAL, com HISTERESE: entra em
## "run" acima de run_anim_enter e so' sai abaixo de run_anim_exit. Com um
## limiar so', a animacao pisca quando a velocidade fica na fronteira.
##
## DERRUBADO vem ANTES da histerese, e por aqui de proposito. Ate' 26/08 o caido
## rastejava devagar, caia no lado "idle" do limiar e ficava DE PE' e parado no
## meio da partida — de longe, indistinguivel de um mago escolhendo o proximo
## passo. O aliado so' descobria quem dava para reerguer pela HUD.
##
## Por que a decisao mora AQUI e nao em Derrubado.gd: Player e Bot reescrevem a
## animacao todo frame (`anim(... locomotion_anim())`), entao qualquer pedido
## feito de fora seria apagado no frame seguinte — foi a licao que ArmaSlot.gesto
## documentou para o gesto de pegar. So' que ali o gesto e' TRANSITORIO e volta
## por relogio; derrubado e' SUSTENTADO e nao tem quando voltar. Estado
## sustentado se resolve na fonte da decisao, nao driblando ela.
##
## A costura com Derrubado ja' existia neste arquivo (ver `dodge_ready`), entao
## isto nao abre acoplamento novo.
func locomotion_anim() -> String:
	if Derrubado.esta(self):
		return "derrubado"
	var v := horizontal_speed()
	if _run_anim:
		if v < float(Balance.MOVE.run_anim_exit):
			_run_anim = false
	elif v > float(Balance.MOVE.run_anim_enter):
		_run_anim = true
	return "run" if _run_anim else "idle"


## FIM DA PATINACAO: a anim de corrida roda na cadencia da velocidade real.
## So' vale pra "run" — em "cast" o speed_scale precisa ser 1.0, senao o
## instante do disparo (CAST_FIRE_TIME do Mage) sai do lugar.
func _sync_anim_speed() -> void:
	var alvo := 1.0
	if _cur_anim == "run":
		if _anim_ref < 0.0:
			_anim_ref = _run_reference_speed()
		if _anim_ref > 0.0:
			alvo = clampf(horizontal_speed() / _anim_ref,
					float(Balance.ANIM.scale_min), float(Balance.ANIM.scale_max))
	if absf(alvo - _anim_speed) < 0.02:
		return
	_anim_speed = alvo
	if visual and visual.has_method("set_anim_speed"):
		visual.set_anim_speed(alvo)


## m/s em que a anim "run" roda natural = passada por ciclo / duracao do ciclo.
## Serve pro .glb externo E pro procedural (cada um tem seu ciclo).
func _run_reference_speed() -> float:
	if visual == null or not visual.has_method("get_anim_length"):
		return 0.0
	var ciclo := float(visual.get_anim_length("run"))
	if ciclo <= 0.0:
		return 0.0
	return float(Balance.ANIM.run_stride_m) / ciclo


## Identidade de mago: o Mage ja' resolve modelo externo (.glb) com fallback
## procedural sozinho — aqui so' repassamos o slug.
## Vem ANTES de set_tint — quem chama os dois na ordem errada perde a paleta.
func set_mage(slug: String) -> void:
	if visual and visual.has_method("set_mage"):
		visual.set_mage(slug)


func set_tint(c: Color) -> void:
	if visual == null:
		return
	if visual.has_method("set_tint"):
		visual.set_tint(c)
	elif visual.has_meta("fallback_mat"):
		(visual.get_meta("fallback_mat") as StandardMaterial3D).albedo_color = c


## O relogio da agua: decide nadar/molhado e o fator do produto unico.
func _agua_step(delta: float) -> void:
	var sup := _superficie_agua()
	var fundo := sup > -1e8 and (sup - global_position.y) >= PEITO
	if fundo:
		_sup_nado = sup
		nadando = true
	elif nadando:
		nadando = false
		_molhado_s = MOLHADO_S  # roupa encharcada: a saida e' lenta uns segundos
	if not nadando:
		_molhado_s = maxf(_molhado_s - delta, 0.0)
	agua_mult = NADO_MULT if nadando else (MOLHADO_MULT if _molhado_s > 0.0 else 1.0)


func _superficie_agua() -> float:
	if _ilha == null or not is_instance_valid(_ilha):
		var tree := get_tree()
		if tree == null:
			return -1e9
		_ilha = tree.get_first_node_in_group("ilha") as Node3D
	if _ilha == null or not _ilha.has_method("agua_y"):
		return -1e9
	return float(_ilha.call("agua_y", global_position.x, global_position.z))


func apply_gravity(delta: float) -> void:
	## NADANDO o corpo FLUTUA com a lamina no peito: a gravidade nao puxa para
	## o fundo do lago (2 m abaixo) nem o mago anda no leito como se nada
	## houvesse — que era exatamente a queixa do Diretor.
	if nadando:
		velocity.y = 0.0
		global_position.y = lerpf(global_position.y, _sup_nado - PEITO,
				minf(6.0 * delta, 1.0))
		return
	if not is_on_floor():
		velocity.y -= _gravity * delta


## Vira o corpo para a direcao dir (XZ). Forward do node e' -Z.
func face_dir(dir: Vector3, delta: float, turn_speed := 10.0) -> void:
	if dir.length_squared() < 0.0001:
		return
	rotation.y = lerp_angle(rotation.y, atan2(-dir.x, -dir.z), turn_speed * delta)


func die() -> void:
	pass  # subclasses dao o feedback
