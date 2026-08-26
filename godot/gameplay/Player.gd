## Player: CharacterBody3D + camera 3a pessoa sobre o ombro (SpringArm com
## colisao). A HUD alimenta move_input/add_look/request_fire — o player nao
## le Input direto (toque e' territorio da UI).
class_name Player
extends Pawn

## Sensibilidade da mira: rad por px de canvas com o slider no PADRAO (3.0).
## KNOB de calibragem no aparelho — mexer aqui move a faixa INTEIRA do menu;
## o jogador ajusta o gosto dele em Configuracoes > Controles.
const SENS_BASE := 0.008 / 3.0

var mana: float = float(Balance.PLAYER.mana_max)
var move_input := Vector2.ZERO   # joystick: x direita, y baixo
var look_sens := 0.008           # rad por px de canvas — vem de Configuracoes
var sens_mira := 1.0             # multiplicador enquanto arrasta a partir do Fogo
var inverter_y := 1.0            # -1.0 quando "Inverter eixo Y" esta ligado
var cam_yaw: Node3D
var cam_pitch: Node3D
var cam: Camera3D
var _fire_cd := 0.0
var _cast := 0.0
var _want_fire := false
var _want_dodge := false
var _want_tatica := false
var _want_suprema := false
var _aiming := false


func _ready() -> void:
	super()
	add_to_group("player")
	# Mira do jogador e' preferencia DELE (GDD par.12). Le a config no spawn e
	# fica no grupo para receber mudanca do menu na hora (ver menu/Config.gd).
	add_to_group("config_ouvintes")
	aplicar_config(Config.atual())
	_build_camera()
	Bus.element_changed.emit(element)  # HUD sincroniza carrossel/cadencia no spawn


func aplicar_config(cfg: Dictionary) -> void:
	var c: Dictionary = cfg.get("controles", {})
	look_sens = SENS_BASE * clampf(float(c.get("sensibilidade", 3.0)), 0.1, 10.0)
	sens_mira = clampf(float(c.get("sens_mira", 1.0)), 0.1, 3.0)
	inverter_y = -1.0 if bool(c.get("inverter_y", false)) else 1.0


## Troca de elemento (carrossel). So' aceita o que Balance.ELEMENTS conhece.
func set_element(el: String) -> void:
	if el == element or not (el in Balance.ELEMENTS):
		return
	element = el
	Bus.element_changed.emit(el)


func _build_camera() -> void:
	cam_yaw = Node3D.new()
	cam_pitch = Node3D.new()
	var arm := SpringArm3D.new()
	arm.spring_length = 4.15
	arm.position = Vector3(0.78, 0.22, 0.0)  # sobre o ombro, com ar suficiente para ler a arena
	arm.add_excluded_object(get_rid())
	cam = Camera3D.new()
	cam.current = true
	cam.fov = 62.0
	arm.add_child(cam)
	cam_pitch.add_child(arm)
	cam_pitch.rotation.x = -0.30
	cam_yaw.add_child(cam_pitch)
	add_child(cam_yaw)
	cam_yaw.top_level = true  # a camera NAO herda o giro do corpo


func set_aiming(on: bool) -> void:
	_aiming = on


func add_look(rel: Vector2) -> void:
	if hp <= 0.0:
		return
	# "Sensibilidade ao mirar" so' vale enquanto o dedo arrasta a partir do Fogo
	# (_aiming) — e' o momento de ajuste fino, que pede curso diferente do olhar.
	var s := look_sens * (sens_mira if _aiming else 1.0)
	cam_yaw.rotation.y -= rel.x * s
	cam_pitch.rotation.x = clampf(cam_pitch.rotation.x - rel.y * s * inverter_y, -1.1, 0.5)


func request_fire() -> void:
	_want_fire = true  # consumido no _physics_process (raycast precisa do passo de fisica)


func request_dodge() -> void:
	_want_dodge = true  # consumido no _physics_process (precisa da direcao de movimento)


## HABILIDADES (GDD §3). Os dois botoes novos da HUD entram por aqui e SO'
## por aqui: quem sabe o que a habilidade faz e' o kit do mago (KitRunner,
## acoplado por fora em set_mage) — o Player nao conhece nenhum mago.
## ECONOMIA (§4.2): nao ha' desconto de mana em nenhuma das duas. Elas pagam
## cooldown, e quem cobra e' o KitRunner.
func request_tatica() -> void:
	_want_tatica = true


func request_suprema() -> void:
	_want_suprema = true


## Direcao que o kit usa para mirar (leque da Pyra, fio da Tessa): a da
## CAMERA, que e' onde o jogador esta' olhando — nao a do corpo.
func kit_aim_dir() -> Vector3:
	var d := -cam_yaw.global_transform.basis.z
	d.y = 0.0
	return d.normalized() if d.length_squared() > 0.0001 else -global_transform.basis.z


## A identidade do mago MANDA no kit: quem troca de mago troca de habilidade.
## O kit entra por COMPOSICAO (no' filho), do mesmo jeito que o ArmaSlot.
func set_mage(slug: String) -> void:
	super(slug)
	KitRunner.acoplar(self, slug)


func _physics_process(delta: float) -> void:
	cam_yaw.global_position = global_position + Vector3(0, 1.85, 0)
	_fire_cd = maxf(_fire_cd - delta, 0.0)
	_cast = maxf(_cast - delta, 0.0)
	if mana < float(Balance.PLAYER.mana_max):
		mana = minf(mana + float(Balance.PLAYER.mana_regen) * delta, float(Balance.PLAYER.mana_max))
		Bus.mana_changed.emit(mana, float(Balance.PLAYER.mana_max))
	apply_gravity(delta)
	var dir := _move_dir()
	if _want_dodge:
		_want_dodge = false
		try_dodge(dir)
	move_velocity(dir, delta)  # produto unico + dash + knockback (caminho do Pawn)
	move_and_slide()
	_turn(dir, delta)
	if _want_tatica:
		_want_tatica = false
		var k := KitRunner.de(self)
		if k != null and Derrubado.pode_agir(self):
			k.usar_tatica()
	if _want_suprema:
		_want_suprema = false
		var ks := KitRunner.de(self)
		if ks != null and Derrubado.pode_agir(self):
			ks.usar_suprema()
	if _want_fire:
		_want_fire = false
		_try_fire()
	# a locomocao sai da velocidade REAL (com histerese), nao do input: com
	# inercia o corpo ainda desliza um tico depois que o dedo sai do joystick.
	anim("cast" if _cast > 0.0 else locomotion_anim())


## Giro do corpo. Parado gira mais rapido (pivo no lugar e' de graca); em
## velocidade cheia gira no ritmo normal, senao vira num pino.
func _turn(dir: Vector3, delta: float) -> void:
	var frac := clampf(horizontal_speed() / maxf(current_speed(), 0.01), 0.0, 1.0)
	var pivo := lerpf(1.0 + float(Balance.MOVE.turn_pivot_bonus), 1.0, frac)
	if _aiming:
		rotation.y = lerp_angle(rotation.y, cam_yaw.rotation.y,
				minf(float(Balance.MOVE.turn_rate_aim) * pivo * delta, 1.0))
	else:
		face_dir(dir, delta, float(Balance.MOVE.turn_rate_free) * pivo)


func _move_dir() -> Vector3:
	var mag := stick_magnitude(move_input)
	if mag <= 0.0:
		return Vector3.ZERO
	var f := -cam_yaw.global_transform.basis.z
	f.y = 0.0
	f = f.normalized()
	var r := cam_yaw.global_transform.basis.x
	r.y = 0.0
	r = r.normalized()
	var d := (f * -move_input.y + r * move_input.x)
	d.y = 0.0
	return d.normalized() * mag


## Curva de resposta do joystick. Reescala DEPOIS da zona morta (senao o mago
## salta pra 12% da velocidade no instante em que o dedo passa a deadzone) e
## aplica um expoente: com curve > 1 sobra curso fino perto do centro, ou seja,
## da' pra ANDAR devagar em vez de so' correr.
static func stick_magnitude(v: Vector2) -> float:
	var dz := float(Balance.MOVE.stick_deadzone)
	var m := v.limit_length(1.0).length()
	if m <= dz:
		return 0.0
	return pow(clampf((m - dz) / maxf(1.0 - dz, 0.001), 0.0, 1.0),
			float(Balance.MOVE.stick_curve))


func _try_fire() -> void:
	# Derrubado nao conjura; quem CANALIZA um resgate tambem nao. A Vitalis e' a
	# excecao viva disso: quem canaliza por ela e' a Lumen (passiva Maos Livres).
	if not Derrubado.pode_agir(self) or is_in_group("reanimando"):
		return
	# A LEI DAS LUVAS (26/08 — DIRECAO.md §1): sem luva nao ha' ataque basico.
	# So' o ATAQUE: tatica e suprema sao natas e nao passam por aqui.
	if not ArmaSlot.armado_de(self):
		return
	# A ARMA ARCANA manda no ataque (GDD §16.2) — e desde 26/08 o ELEMENTO
	# tambem e' dela (DIRECAO.md §1): o spec sai do elemento QUE VAI SAIR, nao
	# do carrossel; senao a mana cobrada seria a de um elemento e o tiro de outro.
	var slot0 := ArmaSlot.de(self)
	var el_previsto: String = element if slot0 == null else (
			slot0.elemento if slot0.elemento != "" else element)
	var s: Dictionary = ArmaSlot.spec_de(self, el_previsto)
	if _fire_cd > 0.0 or mana < float(s.mana_cost):
		return
	# LIMITADOR DE KIT: "sai 1s sem conjurar" (Veu) vale para o ATAQUE tambem.
	# Sem kit acoplado a funcao devolve true — os 17 magos declarados atacam
	# igual a antes deste sistema existir.
	if not KitRunner.pode_conjurar_de(self):
		return
	# Manopla porta DOIS elementos (GDD §16.2): ela decide qual sai no disparo.
	var slot := ArmaSlot.de(self)
	var el_tiro: String = element if slot == null else slot.elemento_do_disparo(element)
	mana -= float(s.mana_cost)
	Bus.spell_cast.emit(el_tiro)  # som do disparo (Sfx observa; costura R19)
	Bus.mana_changed.emit(mana, float(Balance.PLAYER.mana_max))
	_fire_cd = float(s.fire_rate)
	KitRunner.avisar_ataque(self)  # a Entrelinha da Veu quebra quando ela ataca
	_cast = 0.3
	rotation.y = cam_yaw.rotation.y  # o personagem gira para a mira ao disparar
	var from := global_position + Vector3(0, 1.4, 0)
	var aim_dir := (_aim_point() - from).normalized()
	Projectile.launch(get_parent(), self, from + aim_dir * 0.9, aim_dir, el_tiro)
	# Retrigger POR TIRO: anim() so' repassa quando o NOME muda, entao no fogo
	# continuo o braco subia no 1o tiro e congelava. Chamar o visual direto
	# reinicia o clipe a cada disparo (Mage.play_anim trata o reinicio).
	if visual != null and visual.has_method("play_anim"):
		visual.play_anim("cast")
	anim("cast")


## Botao PEGAR da HUD: equipa o loot mais proximo dentro do alcance.
func request_pegar() -> void:
	if not Derrubado.pode_agir(self):
		return
	var slot := ArmaSlot.de(self)
	if slot != null:
		slot.pegar()


## Ponto que o reticulo (centro da camera) esta' olhando.
func _aim_point() -> Vector3:
	var cam_from := cam.global_position
	var cam_dir := -cam.global_transform.basis.z
	var to := cam_from + cam_dir * float(ArmaSlot.spec_de(self, element).range)
	var q := PhysicsRayQueryParameters3D.create(cam_from, to)
	q.exclude = [get_rid()]
	var hit := get_world_3d().direct_space_state.intersect_ray(q)
	if hit.is_empty():
		return to
	return hit["position"]


func die() -> void:
	move_input = Vector2.ZERO
	anim("idle")
	set_tint(Color(0.25, 0.25, 0.3))
	set_physics_process(false)
