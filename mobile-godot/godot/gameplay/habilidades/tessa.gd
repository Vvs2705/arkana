## KIT DA TESSA (10) — Dominadora. GDD §3, ficha em personagens/10-tessa.md.
##   Passiva  Compasso Runico — o marca-passo regenera escudo; escudo QUEBRADO
##                              da' +15% de velocidade por 2s
##   Tatica   Fio do Tear     — fio de raio entre 2 pontos (ate 6 fios)
##   Suprema  Tear-Mae        — absorve projetil inimigo e TECE escudo
##
## OS ⚖️ LIMITADORES SAO PARTE DO KIT e estao TODOS aqui:
##   · o fio QUEBRA com 1 golpe em qualquer ancora (qualquer impacto a 1,5m)
##   · o fio BRILHA e ZUMBE a menos de 5m (counter em cor+forma+som, §10)
##   · agua no chao CONDUZ o raio do fio para TODOS, inclusive o time dela: o
##     toque emite Bus.terrain_hit("lightning"), e quem eletrifica o lago e'
##     o §14 — que nao pergunta de que time e' quem esta' na agua
##   · o Tear-Mae NAO absorve curtissimo alcance (zona morta de 2m) e e' um so'
##
## O ESCUDO e' o do KERNEL (R22): Pawn.shield / shield_max(), absorvido pelo
## Combat. O kit nao emula escudo nenhum — ele so' REGENERA (passiva) e TECE
## (suprema) pela porta unica KitRunner.regenerar_escudo.
extends RefCounted


static func tick(k: KitRunner, delta: float) -> void:
	var p: Dictionary = k.dados.passiva
	## "quando o escudo QUEBRA, +15% de velocidade por 2s". Lido por BORDA no
	## proprio tique (o kit ja' roda todo frame): um sinal a menos para ouvir.
	## VEM ANTES DA REGENERACAO de proposito — regenerar primeiro devolveria
	## uma casquinha de escudo no mesmo frame e a quebra nunca seria vista.
	var vivo := float(k.pawn.shield) > 0.0
	if bool(k.mem.get("tinha_escudo", true)) and not vivo:
		k.buff_velocidade(float(p.quebra_buff_vel), float(p.quebra_buff_dur))
		k.avisar_estado("escudo_quebrado", true)
	k.mem["tinha_escudo"] = vivo
	## Compasso Runico: o marca-passo regenera o escudo "lentamente" — de 1 em
	## 1 ponto, nao por frame. Regenerar por frame emitiria shield_changed 60
	## vezes por segundo na HUD (a mesma lição do dano fracionario, DANO.md
	## §2.3); assim sao no maximo 4 avisos por segundo, com o mesmo total.
	var acc := float(k.mem.get("regen_acc", 0.0)) + float(p.escudo_regen) * delta
	if acc + 0.0001 >= 1.0:
		var inteiro := floorf(acc + 0.0001)
		KitRunner.regenerar_escudo(k.pawn, inteiro)
		acc -= inteiro
	k.mem["regen_acc"] = acc


## Fio do Tear: as DUAS ancoras nascem juntas — a de baixo dos pes dela e a de
## 6m na mira. Ponytail: o GDD descreve "entre 2 pontos" e este e' o jeito de
## dar os 2 pontos com UM toque, que e' o que o polegar tem no mobile.
static func tatica(k: KitRunner) -> void:
	var t: Dictionary = k.dados.tatica
	var fios: Array = k.mem.get("fios", [])
	fios = fios.filter(func(f: Node) -> bool: return is_instance_valid(f))
	while fios.size() >= int(t.max_fios):  # o 7o fio apaga o mais velho
		var velho: Node = fios.pop_front()
		if is_instance_valid(velho):
			velho.queue_free()
	var a := k.pos()
	fios.append(Fio.criar(k, a, a + k.mira() * float(t.comprimento)))
	k.mem["fios"] = fios


## Tear-Mae — so' roda depois da telegrafia (o tear girando, §4.3).
static func suprema(k: KitRunner) -> void:
	var velho: Variant = k.mem.get("tear")
	if velho is Node and is_instance_valid(velho):
		return  # ⚖️ max. 1 por vez
	k.mem["tear"] = Tear.criar(k)


static func estado_acabou(_k: KitRunner, _nome: String) -> void:
	pass  # o tear e os fios tem prazo proprio


static func dano_recebido(_k: KitRunner, _quanto: float, _el: String) -> void:
	pass  # o escudo e' do kernel: quem absorve e' o Combat, nao o kit


## --------------------------------------------------------------------- FIO
## Segmento de raio entre 2 ancoras. Tique proprio de 0,1s: nada de Area3D com
## sinal de fisica (o teste headless nunca veria) nem _process por metro.
class Fio extends Node3D:
	const TICK := 0.1

	var a := Vector3.ZERO
	var b := Vector3.ZERO
	var dona: Node
	var t: Dictionary = {}
	var duracao := 20.0
	var _acc := 0.0
	var _quem := {}         # id -> s de rearme
	var _zumbindo := false

	static func criar(k: KitRunner, p_a: Vector3, p_b: Vector3) -> Fio:
		var f := Fio.new()
		f.a = p_a
		f.b = p_b
		f.dona = k.pawn
		f.t = k.dados.tatica
		f.duracao = float(f.t.duracao)
		k.arena().add_child(f)
		f.global_position = (p_a + p_b) * 0.5
		return f

	func _ready() -> void:
		add_to_group("fios_tessa")
		_visual()
		## ⚖️ "os fios quebram com 1 golpe em qualquer ancora": TODO impacto de
		## projetil emite terrain_hit (contrato R19) — e' o golpe chegando.
		Bus.terrain_hit.connect(_on_terrain_hit)

	func _physics_process(delta: float) -> void:
		duracao -= delta
		if duracao <= 0.0:
			queue_free()
			return
		for id in _quem.keys():
			var r := float(_quem[id]) - delta
			if r <= 0.0:
				_quem.erase(id)
			else:
				_quem[id] = r
		_acc += delta
		if _acc < TICK:
			return
		_acc = 0.0
		var alcance := a.distance_to(b) * 0.5 + float(t.zumbido_dist)
		var perto := false
		for alvo in KitRunner.alvos_perto(get_parent(), global_position, alcance):
			var d := KitRunner.dist_segmento(alvo.global_position, a, b)
			if d <= float(t.zumbido_dist) and alvo != dona:
				perto = true  # ⚖️ o fio se DENUNCIA a 5m (brilho + zumbido)
			if d > float(t.raio_toque) or alvo == dona:
				continue
			if _quem.has(alvo.get_instance_id()):
				continue
			_tocar(alvo)
		if perto != _zumbindo:
			_zumbindo = perto
			Bus.kit_state.emit("fio_zumbido", perto)

	func _tocar(alvo: Node3D) -> void:
		# `dona` como FONTE: dano de habilidade tambem credita a evolucao do
		# escudo de quem a usou (GDD §5) — senao jogar de tatica sai punido.
		if Combat.deal(alvo, float(t.dano), "lightning", dona) <= 0.0:
			return
		_quem[alvo.get_instance_id()] = float(t.rearme)
		KitRunner.afetar_velocidade(alvo, float(t.lentidao), float(t.lentidao_dur))
		if alvo.is_in_group("player"):
			Bus.kit_state.emit("revelado", true)
		## ⚖️ AGUA CONDUZ (§14): o kit NAO decide o mundo — ele publica o raio
		## no ponto do toque. Se ali tem agua, o terreno eletrifica o lago
		## inteiro e cobra de TODOS, inclusive do time dela.
		Bus.terrain_hit.emit("lightning", alvo.global_position, false)

	func _on_terrain_hit(_element: String, p: Vector3, _strong: bool) -> void:
		var r := float(t.get("ancora_raio", 1.5))
		if p.distance_to(a) <= r or p.distance_to(b) <= r:
			queue_free()  # ⚖️ UM golpe em qualquer ancora derruba o fio

	func _visual() -> void:
		var comp := a.distance_to(b)
		var m := MeshInstance3D.new()
		var caixa := BoxMesh.new()
		caixa.size = Vector3(comp, 0.08, 0.08)
		m.mesh = caixa
		m.material_override = KitRunner.mat_brilho(Color(0.96, 0.85, 0.04, 0.9), 4.0)
		m.position.y = 1.0
		add_child(m)
		var d := b - a
		if d.length_squared() > 0.0001:
			rotation.y = atan2(-d.z, d.x)  # o +X local corre de a para b
		for lado in [-1.0, 1.0]:
			var anc := MeshInstance3D.new()   # as ANCORAS, que sao o ponto fraco
			var esf := SphereMesh.new()
			esf.radius = 0.18
			esf.height = 0.36
			anc.mesh = esf
			anc.material_override = KitRunner.mat_brilho(Color(1.0, 1.0, 0.6, 0.9), 3.0)
			anc.position = Vector3(comp * 0.5 * lado, 1.0, 0.0)
			add_child(anc)


## ---------------------------------------------------------------- TEAR-MAE
## Tear runico giratorio: engole projetil MAGICO inimigo e tece o absorvido em
## escudo para os aliados por perto.
class Tear extends Node3D:
	const TICK := 0.1

	var dona: Node
	var k: KitRunner
	var s: Dictionary = {}
	var duracao := 8.0
	var _acc := 0.0
	var _giro: Node3D

	static func criar(kit: KitRunner) -> Tear:
		var n := Tear.new()
		n.k = kit
		n.dona = kit.pawn
		n.s = kit.dados.suprema
		n.duracao = float(n.s.duracao)
		kit.arena().add_child(n)
		n.global_position = kit.pos()
		return n

	func _ready() -> void:
		add_to_group("tear_tessa")
		_giro = Node3D.new()
		add_child(_giro)
		for i in 3:
			var m := MeshInstance3D.new()
			var t := TorusMesh.new()
			t.inner_radius = 0.9 + float(i) * 0.35
			t.outer_radius = 1.0 + float(i) * 0.35
			m.mesh = t
			m.material_override = KitRunner.mat_brilho(Color(0.9, 0.8, 0.2, 0.5), 2.5)
			m.rotation = Vector3(float(i) * 0.6, 0.0, float(i) * 0.4)
			m.position.y = 1.4
			_giro.add_child(m)

	func _physics_process(delta: float) -> void:
		duracao -= delta
		if duracao <= 0.0:
			queue_free()
			return
		_giro.rotation.y += delta * 2.2   # o tear GIRA: e' leitura visual
		_acc += delta
		if _acc < TICK:
			return
		_acc = 0.0
		if not is_instance_valid(dona) or not is_instance_valid(k):
			return
		var centro := (dona as Node3D).global_position
		for c in get_parent().get_children():
			if not (c is Projectile):
				continue
			var p := c as Projectile
			if p.shooter == dona:
				continue
			var d := p.global_position.distance_to(centro)
			## ⚖️ ZONA MORTA: nada de absorver corpo a corpo/curtissimo alcance.
			if d > float(s.raio) or d < float(s.zona_morta):
				continue
			p.queue_free()
			_tecer(float(s.escudo_por_projetil), centro)

	## Tece o absorvido em escudo para ela e para os aliados por perto.
	func _tecer(quanto: float, centro: Vector3) -> void:
		KitRunner.regenerar_escudo(dona, quanto)
		for a in KitRunner.alvos_perto(get_parent(), centro, float(s.raio_aliado), dona):
			if a.is_in_group("player"):
				KitRunner.regenerar_escudo(a, quanto)
