## KIT DA PYRA (01) — Vanguarda. GDD §3, ficha em personagens/01-pyra.md.
##   Passiva  Coracao de Fornalha — fogo NO CHAO nao a fere e reacende o braco
##   Tatica   Muralha de Brasas   — linha de fogo de 8m por 5s
##   Suprema  Braco Livre         — 6s de leque continuo + dash que deixa fogo
##
## OS ⚖️ LIMITADORES SAO PARTE DO KIT (ordem do Diretor) e estao TODOS aqui:
##   · agua/gelo APAGAM a muralha e molham o braco: +1s na recarga da tatica
##   · vento EMPURRA a muralha 3m (§14)
##   · a suprema e' TELEGRAFADA (o KitRunner segura o efeito 1,4s: rugido+brilho)
##   · ao acabar a suprema o braco ESFRIA: 4s sem tatica e −15% de velocidade
##
## Contrato do registro (KitRunner.IMPL): 5 estaticos, nada de estado global.
extends RefCounted


static func tick(k: KitRunner, delta: float) -> void:
	_passiva(k, delta)
	if k.estado_ativo("braco_livre"):
		_braco_livre(k, delta)


## Coracao de Fornalha. A IMUNIDADE e' devolucao no mesmo tique: o ponto unico
## de dano (Combat) e' de outra raia e nao tem gancho de prevencao, entao o kit
## repoe exatamente o que o terreno em chamas tirou — ver devolver_dano().
## E' SO' fogo: o chao ELETRIFICADO (§14) continua doendo nela.
static func _passiva(k: KitRunner, delta: float) -> void:
	var dps := TerrainSystem.dps_at(k.pos())
	var no_fogo := is_equal_approx(dps, float(Balance.TERRAIN.burn_dps))
	if no_fogo:
		## Devolve EXATAMENTE a parte do DoT que veio do chao em chamas. O
		## Pawn soma queimadura + terreno e corta no teto (Balance.DOT.teto_dps)
		## antes de cobrar; devolver o dps cru daria cura de graca a quem
		## estivesse queimando E no fogo ao mesmo tempo. A regra e' a fatia.
		var total := dps + (float(k.pawn.burn_dps) if float(k.pawn.burn_left) > 0.0 else 0.0)
		var cobrado := minf(total, float(Balance.DOT.teto_dps))
		k.devolver_dano(cobrado * (dps / maxf(total, 0.001)) * delta)
	if no_fogo or Brasas.tem_fogo(k.pawn, k.pos()):
		# "+10% de velocidade por 2s ao ATRAVESSAR chamas" — reacende o braco.
		var p: Dictionary = k.dados.passiva
		k.buff_velocidade(float(p.buff_vel), float(p.buff_dur))


## Muralha de Brasas: risca uma linha PERPENDICULAR a mira, 3m a frente.
static func tatica(k: KitRunner) -> void:
	var t: Dictionary = k.dados.tatica
	var d := k.mira()
	var centro := k.pos() + d * 3.0
	var perp := d.cross(Vector3.UP).normalized()
	var meio := float(t.comprimento) * 0.5
	Brasas.criar(k, centro - perp * meio, centro + perp * meio, t, true)


## Braco Livre: a suprema NAO acontece aqui na hora do toque — o KitRunner so'
## chama isto quando a telegrafia (rugido + brilho) acaba. §4.3.
static func suprema(k: KitRunner) -> void:
	var s: Dictionary = k.dados.suprema
	k.mem["leque_acc"] = 0.0
	k.mem["dash_acc"] = 0.0
	k.mem["dash_left"] = 0.0
	k.ligar_estado("braco_livre", float(s.duracao))


## Enquanto a manopla esta' destravada: leque continuo + fogo no rastro do dash.
## NENHUMA linha aqui toca em mana: a suprema ja' pagou em cooldown (§4.2).
static func _braco_livre(k: KitRunner, delta: float) -> void:
	var s: Dictionary = k.dados.suprema
	k.mem["leque_acc"] = float(k.mem.leque_acc) + delta
	if float(k.mem.leque_acc) >= float(s.cadencia):
		k.mem["leque_acc"] = 0.0
		_leque(k, s)
	# O dash e' detectado pela API publica do Pawn (dodge_cd_frac), sem editar
	# Pawn.gd: enquanto ele dura, a Pyra vai deixando poca de fogo.
	if k.dash_iniciou:
		k.mem["dash_left"] = float(Balance.DODGE.duration)
		k.mem["dash_acc"] = 1.0  # forca a primeira poca no mesmo frame
	if float(k.mem.dash_left) > 0.0:
		k.mem["dash_left"] = float(k.mem.dash_left) - delta
		k.mem["dash_acc"] = float(k.mem.dash_acc) + delta * 16.0
		if float(k.mem.dash_acc) >= 1.0:
			k.mem["dash_acc"] = 0.0
			var p := k.pos()
			Brasas.criar(k, p, p, {
				"duracao": s.fogo_dash_dur, "dano": s.fogo_dash_dano,
				"espessura": s.fogo_dash_raio, "rearme": 0.6,
				"aceso_dur": k.dados.tatica.aceso_dur,
			}, false, true)


static func _leque(k: KitRunner, s: Dictionary) -> void:
	var d := k.mira()
	var from := k.pos() + Vector3(0, 1.4, 0) + d * 0.9
	var n := int(s.leque_tiros)
	for i in n:
		var t := 0.0 if n <= 1 else (float(i) / float(n - 1)) * 2.0 - 1.0
		var ang := deg_to_rad(float(s.leque_graus)) * t
		Projectile.launch(k.arena(), k.pawn, from, d.rotated(Vector3.UP, ang), "fire")


## O PRECO DA SUPREMA: o braco esfria. 4s sem tatica e −15% de velocidade.
static func estado_acabou(k: KitRunner, nome: String) -> void:
	if nome != "braco_livre":
		return
	var s: Dictionary = k.dados.suprema
	k.tatica_cd = maxf(k.tatica_cd, float(s.esfria_dur))
	k.avisar_cd("tatica", k.tatica_cd)
	k.buff_velocidade(float(s.esfria_vel), float(s.esfria_dur))
	# LIGAR_ESTADO, nunca avisar_estado direto: o aviso cru liga o chip da HUD
	# e NINGUEM desliga — no video do Diretor (26/08) "BRACO FRIO" ficou preso
	# na tela do segundo 2 ao 55. Pelo relogio de estados ele expira sozinho e
	# a HUD recebe o desligamento pela mesma borda de sempre.
	k.ligar_estado("braco_frio", float(s.esfria_dur))


static func dano_recebido(_k: KitRunner, _quanto: float, _el: String) -> void:
	pass  # a Pyra nao tem gatilho de dano recebido


## ------------------------------------------------------------------ BRASAS
## Fogo NO CHAO: a muralha (segmento de 8m) e a poca do dash (a == b) sao o
## mesmo objeto — muda so' a geometria. Tique proprio de 0,1s (mobile: nada de
## _process por particula) e VFX 100% procedural.
class Brasas extends Node3D:
	const TICK := 0.1

	## ---- knobs do VISUAL (receita do Spellbreak — SPELLBREAK.md §2.2/2.3:
	## a FORMA do elemento e' quem le; fogo e' nucleo emissivo + chama viva,
	## animado em PASSOS de 12–15 fps, nunca interpolacao lisa) ----
	const NUCLEO_ALTURA := 0.9   # m — SUBIR: vira parede que esconde o duelo; DESCER: some no mato
	const CHAMAS_POR_M := 6      # particulas por m de linha — SUBIR: fogo denso E caro (mobile!)
	const CHAMAS_MIN := 12       # piso: a poca do dash nao pode virar meia duzia de fagulhas
	const CHAMAS_MAX := 48       # TETO mobile — SUBIR so' com medicao no aparelho do Diretor
	const CHAMA_SOBE := 1.0      # m que cada chama sobe — SUBIR: fogo alto tampa a mira rasante
	const CHAMA_VIDA := 0.7      # s de vida da chama — SUBIR: fogo "lento"; DESCER: vira fagulha
	const CHAMA_QUAD := Vector2(0.30, 0.42)  # quad vertical: lambida de fogo, nao fagulha redonda
	const FLICKER_PASSO := 0.08  # s entre degraus (~12,5 fps) — DESCER: vira lerp e perde o look
	const FLICKER_NIVEIS := [2.2, 3.4, 2.8]  # energias do nucleo — a chama "respira" em degraus

	var a := Vector3.ZERO
	var b := Vector3.ZERO
	var espessura := 1.0
	var duracao := 5.0
	var dano := 18.0
	var rearme := 1.0
	var aceso_dur := 2.0
	var dono: Node
	var k: KitRunner
	var eh_muralha := false
	var _acc := 0.0
	var _quem := {}   # id do alvo -> s restantes de rearme
	var _flicker_acc := 0.0  # relogio do flicker (degraus, nunca por frame)
	var _flicker_idx := 0

	static func criar(kit: KitRunner, p_a: Vector3, p_b: Vector3, cfg: Dictionary,
			muralha: bool, acende_terreno := false) -> Brasas:
		var n := Brasas.new()
		n.a = p_a
		n.b = p_b
		n.k = kit
		n.dono = kit.pawn
		n.eh_muralha = muralha
		n.duracao = float(cfg.get("duracao", 5.0))
		n.dano = float(cfg.get("dano", 18.0))
		n.espessura = float(cfg.get("espessura", 1.0))
		n.rearme = float(cfg.get("rearme", 1.0))
		n.aceso_dur = float(cfg.get("aceso_dur", 2.0))
		kit.arena().add_child(n)
		n.global_position = (p_a + p_b) * 0.5
		## A COSTURA COM O TERRENO (§14): a POCA do dash acende o mundo pelo
		## contrato do Bus (o terreno reage; este script nunca muda o mundo).
		## A MURALHA nao acende: ela e' fogo de 5s numa linha, e passar o
		## orcamento de combustivel do §14 nela queimaria a ilha a cada 14s.
		if acende_terreno:
			Bus.terrain_hit.emit("fire", n.global_position, false)
		return n

	## Alguma brasa da Pyra cobre este ponto? (a passiva dela le daqui)
	static func tem_fogo(origem: Node, p: Vector3) -> bool:
		if origem == null or not is_instance_valid(origem) or origem.get_tree() == null:
			return false
		for n in origem.get_tree().get_nodes_in_group("brasas_pyra"):
			if n is Brasas and KitRunner.dist_segmento(p, n.a, n.b) <= n.espessura:
				return true
		return false

	func _ready() -> void:
		add_to_group("brasas_pyra")
		_visual()
		## ⚖️ LIMITADORES ELEMENTAIS (§14): agua apaga e molha o braco; vento
		## empurra 3m. Chegam pelo MESMO sinal que o terreno escuta.
		if eh_muralha:
			Bus.terrain_hit.connect(_on_terrain_hit)

	func _physics_process(delta: float) -> void:
		_flicker(delta)
		duracao -= delta
		if duracao <= 0.0:
			queue_free()
			return
		for id in _quem.keys():
			var t := float(_quem[id]) - delta
			if t <= 0.0:
				_quem.erase(id)
			else:
				_quem[id] = t
		_acc += delta
		if _acc < TICK:
			return
		_acc = 0.0
		var alcance := a.distance_to(b) * 0.5 + espessura + 1.0
		for alvo in KitRunner.alvos_perto(get_parent(), global_position, alcance, dono):
			if _quem.has(alvo.get_instance_id()):
				continue
			if KitRunner.dist_segmento(alvo.global_position, a, b) > espessura:
				continue
			# `dono` como FONTE: sem ele o dano de habilidade nao credita a
			# evolucao do escudo (GDD §5) e quem joga de tatica sai punido.
			if Combat.deal(alvo, dano, "fire", dono) > 0.0:
				_quem[alvo.get_instance_id()] = rearme
				_acender(alvo)

	## "quem atravessa sai ACESO (rastro visivel 2s)" — brilho procedural preso
	## no alvo, com prazo proprio. Serve de alvo para os outros: e' informacao.
	func _acender(alvo: Node3D) -> void:
		var m := MeshInstance3D.new()
		var esfera := SphereMesh.new()
		esfera.radius = 0.45
		esfera.height = 0.9
		m.mesh = esfera
		m.material_override = KitRunner.mat_brilho(Color(1.0, 0.45, 0.1, 0.45), 2.5)
		m.position.y = 1.1
		alvo.add_child(m)
		alvo.get_tree().create_timer(aceso_dur).timeout.connect(m.queue_free)

	func _on_terrain_hit(element: String, p: Vector3, _strong: bool) -> void:
		var t: Dictionary = k.dados.tatica if is_instance_valid(k) else {}
		var d := KitRunner.dist_segmento(p, a, b)
		if element == "water" and d <= float(t.get("apaga_raio", 3.0)):
			# ⚖️ apaga a muralha E molha o braco: +1s de recarga na tatica.
			if is_instance_valid(k):
				k.acrescentar_cd_tatica(float(t.get("molhado_cd_extra", 1.0)))
				k.avisar_estado("braco_molhado", true)
			queue_free()
		elif element == "wind" and d <= float(t.get("vento_raio", 4.0)):
			# ⚖️ o vento EMPURRA a muralha 3m para longe do impacto (§14).
			var empurra := float(t.get("vento_empurra", 3.0))
			var dir := (global_position - p)
			dir.y = 0.0
			dir = dir.normalized() if dir.length_squared() > 0.0001 else Vector3.FORWARD
			a += dir * empurra
			b += dir * empurra
			global_position = (a + b) * 0.5

	## UM material de nucleo para TODAS as brasas (perf mobile): estatico,
	## criado uma vez. O flicker mexe NELE — todas as brasas piscam juntas, e
	## tudo bem: e' fogo da mesma maga, e um set por passo do relogio e' mais
	## barato que um material (e um set) por brasa.
	static var _mat_nucleo: StandardMaterial3D

	static func _material_nucleo() -> StandardMaterial3D:
		if _mat_nucleo == null:
			_mat_nucleo = StandardMaterial3D.new()
			_mat_nucleo.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
			## O gradiente e' metade da leitura de fogo (GDD §10: cor + FORMA;
			## chapado numa cor so' foi o que virou "retangulo laranja" no
			## video do Diretor, 26/08): base laranja-avermelhada, topo amarelo.
			var g := Gradient.new()
			g.set_color(0, Color(0.95, 0.25, 0.05))  # base laranja-avermelhada
			g.set_color(1, Color(1.0, 0.85, 0.25))   # topo amarelo
			var tex := GradientTexture2D.new()
			tex.gradient = g
			tex.fill_from = Vector2(0, 1)  # v=1 e' a BASE do mesh...
			tex.fill_to = Vector2(0, 0)    # ...e v=0 o topo
			_mat_nucleo.albedo_texture = tex
			_mat_nucleo.emission_enabled = true
			_mat_nucleo.emission = Projectile.tint("fire")  # cor canonica (§10)
			_mat_nucleo.emission_energy_multiplier = float(FLICKER_NIVEIS[0])
		return _mat_nucleo

	## Flicker em PASSOS (SPELLBREAK.md §2.2: VFX animado a 12–15 fps para o
	## look de anime — nunca lerp continuo): a energia do emissivo SALTA entre
	## niveis a cada FLICKER_PASSO. Chamado pelo _physics_process — e direto
	## pelo selftest, com deltas a mao (headless nao renderiza; prova-se ESTADO).
	func _flicker(delta: float) -> void:
		if _mat_nucleo == null:
			return
		_flicker_acc += delta
		if _flicker_acc < FLICKER_PASSO:
			return
		_flicker_acc = fmod(_flicker_acc, FLICKER_PASSO)
		_flicker_idx = (_flicker_idx + 1) % FLICKER_NIVEIS.size()
		_mat_nucleo.emission_energy_multiplier = float(FLICKER_NIVEIS[_flicker_idx])

	## VFX procedural (zero binario): a licao do Spellbreak (SPELLBREAK.md
	## §2.3) — a Flamewall le a distancia como linha laranja VIVA, nunca um
	## retangulo estatico. Aqui: nucleo baixo em CUNHA (topo pontudo = silhueta
	## de chama; billboard NAO — e' uma linha no mundo), gradiente emissivo e
	## chamas subindo em particula. Baixa de proposito — "bloqueia visao
	## rasante", nao a visao inteira. ZERO luz dinamica: a OmniLight que morava
	## aqui custava caro no mobile e o projeto ilumina com COR.
	func _visual() -> void:
		var comp := maxf(a.distance_to(b), espessura * 2.0)
		var m := MeshInstance3D.new()
		var cunha := PrismMesh.new()
		cunha.size = Vector3(espessura * 2.0, NUCLEO_ALTURA, comp)
		m.mesh = cunha
		m.material_override = _material_nucleo()
		m.position.y = NUCLEO_ALTURA * 0.5
		add_child(m)
		# A cunha e' comprida no Z: giramos o no' para que o +Z local aponte de
		# a para b. (global_position so' e' escrito depois do add_child, em
		# criar() — por isso aqui NADA depende da posicao, so' da direcao.)
		var d := b - a
		if d.length_squared() > 0.0001:
			rotation.y = atan2(d.x, d.z)
		add_child(_chamas(comp))

	## UMA GPUParticles3D por brasa (mobile): chamas de ~1 m subindo ao longo
	## da linha, encolhendo ate' sumir — mesmo estilo de quad billboard do
	## burst do Projectile. local_coords: empurrada pelo vento, a chama vai junto.
	func _chamas(comp: float) -> GPUParticles3D:
		var p := GPUParticles3D.new()
		p.amount = clampi(int(comp * CHAMAS_POR_M), CHAMAS_MIN, CHAMAS_MAX)
		p.lifetime = CHAMA_VIDA
		p.local_coords = true
		var pm := ParticleProcessMaterial.new()
		pm.emission_shape = ParticleProcessMaterial.EMISSION_SHAPE_BOX
		pm.emission_box_extents = Vector3(espessura, 0.05, comp * 0.5)
		pm.direction = Vector3.UP
		pm.spread = 8.0
		pm.gravity = Vector3.ZERO
		# velocidade derivada dos knobs: sobe CHAMA_SOBE durante CHAMA_VIDA
		pm.initial_velocity_min = CHAMA_SOBE / CHAMA_VIDA * 0.8
		pm.initial_velocity_max = CHAMA_SOBE / CHAMA_VIDA * 1.2
		pm.color = Projectile.tint("fire")
		var curva := Curve.new()  # nasce cheia, morre um fiapo — leitura de chama
		curva.add_point(Vector2(0.0, 1.0))
		curva.add_point(Vector2(1.0, 0.05))
		var ct := CurveTexture.new()
		ct.curve = curva
		pm.scale_curve = ct
		p.process_material = pm
		var quad := QuadMesh.new()
		quad.size = CHAMA_QUAD
		var mat := StandardMaterial3D.new()
		mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
		mat.vertex_color_use_as_albedo = true
		mat.billboard_mode = BaseMaterial3D.BILLBOARD_ENABLED
		quad.material = mat
		p.draw_pass_1 = quad
		p.emitting = true
		return p
