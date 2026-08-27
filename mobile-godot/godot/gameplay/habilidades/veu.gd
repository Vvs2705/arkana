## KIT DA VEU (03) — Errante. GDD §3, ficha em personagens/03-veu.md.
##   Passiva  Entrelinha      — 4s sem atacar/apanhar: desfoca a mais de 20m
##   Tatica   Atravessar      — 0,8s de ativacao + 1,5s no plano espectral
##   Suprema  Mare Espectral  — 5s de plano espectral para ela e os aliados
##
## OS ⚖️ LIMITADORES SAO PARTE DO KIT e estao TODOS aqui:
##   · QUALQUER dano quebra a passiva (o relogio desde_dano zera)
##   · o Atravessar deixa um ECO visivel na entrada
##   · ela sai do Atravessar 1s SEM CONJURAR (silencio — vale ate para o ataque)
##   · na Mare NINGUEM conjura (silencio pelos 5s inteiros)
##   · a Mare toca um SINO espectral no mundo real, na posicao do grupo: e' o
##     counter sonoro, e o audio observa Bus.kit_state("sino_espectral").
##
## O "atravessa paredes finas" e' KitRunner.intangivel(): a intangibilidade
## mora no motor, e nao aqui, porque a Mare leva ALIADOS junto — e aliado nao
## tem o kit da Veu.
extends RefCounted


static func tick(k: KitRunner, _delta: float) -> void:
	_passiva(k)  # a passiva e' TUDO que a Veu precisa por frame


## Entrelinha: quieta ha' 4s (sem atacar E sem apanhar) => desfocada. A
## DISTANCIA (20m) e' regra de quem OLHA: publicamos o estado e a distancia em
## Kits; quem desenha silhueta le daqui.
static func _passiva(k: KitRunner) -> void:
	var p: Dictionary = k.dados.passiva
	var quieta := minf(k.desde_ataque, k.desde_dano) >= float(p.quietude)
	if quieta == bool(k.mem.get("desfocada", false)):
		return
	k.mem["desfocada"] = quieta
	k.avisar_estado("desfocada", quieta)


## Atravessar: a tatica tambem AVISA (0,8s de ativacao) antes de valer.
static func tatica(k: KitRunner) -> void:
	k.ligar_estado("ativacao", float(k.dados.tatica.ativacao))


static func suprema(k: KitRunner) -> void:
	var s: Dictionary = k.dados.suprema
	## "ela e ALIADOS num raio de 6m no cast entram JUNTOS". Aliado hoje = quem
	## esta' no grupo "player" (a partida de 3 min e' 1 player + 6 bots
	## inimigos); quando a dupla existir, nada aqui muda de forma.
	var grupo: Array = [k.pawn]
	for a in KitRunner.alvos_perto(k.arena(), k.pos(), float(s.raio), k.pawn):
		if a.is_in_group("player"):
			grupo.append(a)
	for a in grupo:
		var kk := KitRunner.de(a)
		if kk == null:
			kk = KitRunner.acoplar(a, "")
		kk.ligar_estado("mare", float(s.duracao))
		kk.buff_velocidade(float(s.buff_vel), float(s.duracao))
		kk.silenciar(float(s.silencio))  # ⚖️ na Mare NINGUEM conjura
		kk.intangivel(true)
	Bus.kit_state.emit("sino_espectral", true)  # ⚖️ o sino toca no mundo real


static func estado_acabou(k: KitRunner, nome: String) -> void:
	match nome:
		"ativacao":
			_entrar(k)
		"espectral":
			k.intangivel(false)
			# ⚖️ o preco do Atravessar: 1s sem conjurar (ataque incluso).
			k.silenciar(float(k.dados.tatica.silencio_saida))
		"mare":
			k.intangivel(false)
			Bus.kit_state.emit("sino_espectral", false)


static func _entrar(k: KitRunner) -> void:
	var t: Dictionary = k.dados.tatica
	Eco.criar(k, float(t.eco_dur))  # ⚖️ o eco fica na ENTRADA, visivel
	k.ligar_estado("espectral", float(t.duracao))
	k.buff_velocidade(float(t.buff_vel), float(t.duracao))  # "mais rapida"
	k.intangivel(true)


static func dano_recebido(k: KitRunner, _quanto: float, _el: String) -> void:
	## ⚖️ "qualquer dano quebra a passiva" — o relogio desde_dano ja' zerou no
	## KitRunner; aqui so' derrubamos o estado no mesmo frame, sem esperar tique.
	if bool(k.mem.get("desfocada", false)):
		k.mem["desfocada"] = false
		k.avisar_estado("desfocada", false)


## --------------------------------------------------------------------- ECO
## O rastro que ela deixa na ENTRADA do plano espectral: e' o que da' ao
## inimigo a informacao de por onde ela sumiu. VFX procedural que se apaga.
class Eco extends Node3D:
	var _left := 2.0

	static func criar(k: KitRunner, dur: float) -> Eco:
		var e := Eco.new()
		e._left = dur
		k.arena().add_child(e)
		e.global_position = k.pos()
		return e

	func _ready() -> void:
		add_to_group("eco_veu")
		var m := MeshInstance3D.new()
		var c := CapsuleMesh.new()
		c.height = 1.8
		c.radius = 0.35
		m.mesh = c
		m.material_override = KitRunner.mat_brilho(Color(0.55, 0.75, 1.0, 0.35), 1.6)
		m.position.y = 0.9
		add_child(m)

	func _physics_process(delta: float) -> void:
		_left -= delta
		scale = Vector3.ONE * maxf(_left, 0.01)  # some encolhendo, sem tween
		if _left <= 0.0:
			queue_free()
