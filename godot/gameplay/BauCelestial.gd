## BAU CELESTIAL — o evento de mundo que abre a UNICA porta da MANOPLA
## (GDD §16.2: "SÓ em Baús Celestiais (drop do céu)"). Sem este arquivo a arma
## lendaria e' inalcancavel em partida — o registro dela existe e ninguem chega.
##
## ⚠️ NAO CONFUNDIR com a suprema da Vitalis. No GDD §2 (tabela de paralelo com
## o Apex) a Lifeline aparece como "fada curandeira + baú celestial", mas o
## elenco foi REFORMULADO em 20/08 e a suprema dela virou "Jardim da Aurora"
## (personagens/07-vitalis.md §KIT, e GDD §3 item 7). Ou seja: HOJE o Baú
## Celestial nao e' habilidade de ninguem — e' EVENTO DE MUNDO, e mora aqui.
##
## O CONTRATO DE DESIGN (por que o bau existe): ele nao e' um presente, e' um
## IMA. O GDD trata o brilho do bau como denuncia — quem vai atras se expoe. O
## desenho abaixo e' inteiro construido para isso:
##   1. a queda e' TELEGRAFADA: feixe de luz do chao ao ceu, no lugar exato,
##      QUEDA_S segundos ANTES de pousar. Todo mundo ve, todo mundo decide.
##   2. o bau pousa em campo ABERTO (18-48m do centro, fora dos POIs): quem
##      chega la' nao tem cobertura.
##   3. abrir CANALIZA: quem abre fica parado, de costas para o mapa, por
##      CANALIZAR_S. E' a janela de contra-ataque.
## Nada disso e' aleatorio na hora: TUDO sai do seed da partida.
##
## ORCAMENTO MOBILE (mesma lei do Loot.gd): ZERO _process em repouso. O relogio
## e' um Timer (engine-side), a queda e' um Tween (engine-side) e o _process so'
## LIGA enquanto tem alguem canalizando dentro do raio — e desliga na saida.
## Em 99% da partida este no' custa exatamente nada por frame.
class_name BauCelestial
extends Area3D

enum { ESPERANDO, CAINDO, POUSADO, ABERTO }

## QUANDO CAI — TEMPO FIXO, nao sorteado. Decisao de design registrada:
## a POSICAO e' que vem do seed; o RELOGIO e' publico e igual toda partida.
## Motivo: um evento de mapa so' vira ponto de conflito se o jogador puder
## SE PREPARAR para ele (rotacionar, buscar posicao, emboscar quem for). Se a
## hora fosse sorteada, o bau viraria loteria — quem estivesse por perto no
## momento levava. Com hora fixa, quem leva e' quem PLANEJOU. (E' a mesma
## razao pela qual o circulo do Apex tem cronometro visivel e nao surpresa.)
## Numeros: em 180s de partida, anuncio aos 45s + 6s de queda = pousa aos 51s,
## sobrando ~129s de briga pela manopla. KNOB: subir ANUNCIO_S encurta o
## reinado da manopla; descer demais entrega lendaria antes de qualquer briga.
const ANUNCIO_S := 45.0
const QUEDA_S := 6.0

const ALTURA_QUEDA := 90.0   # m — de onde desce (alto o bastante p/ ver de longe)
const CANALIZAR_S := 3.0     # s parado abrindo — KNOB: a janela de contra-ataque
const RAIO_ABRIR := 2.6      # m — precisa CHEGAR no bau, nao so' olhar
const RAIO_MIN := 18.0       # m do centro — campo aberto, nunca em cima do spawn
const RAIO_MAX := 48.0       # m do centro — dentro da ilha (o loot corta em 70)
const ATRAIR_BOT_M := 55.0   # m — quem escuta o chamado do bau (ver _chamar_bots)

var fase := ESPERANDO
var indice := 0              # qual par fixo de elementos esta' dentro (deterministico)

var _timer: Timer
var _pivo: Node3D            # so' o bau: e' isto que desce do ceu
var _feixe: Node3D           # o telegrafo: feixe + marcador no chao
var _dentro: Array[Node] = []
var _progresso := 0.0
var _gesto_acc := 0.0        # relogio do gesto repetido (ver _gestos)
## O player chegou a canalizar nesta rodada? So' com isso o cancelamento vai
## para a HUD — bot saindo de cima do bau nao pode acender "CANCELOU" na tela
## do jogador (o sinal bau_canalizando e' do PLAYER, ver core/Bus.gd).
var _player_canalizou := false
var _quem_canalizava: Node = null


## Agenda a queda da partida. DETERMINISTICO: mesmo seed = mesmo instante,
## mesmo ponto de pouso e MESMO PAR de elementos na manopla.
## `island` so' precisa responder height(x, z); sem ela tudo pousa em y = 1.0.
static func agendar(parent: Node3D, island: Node, p_seed := Loot.SEED_LOOT) -> BauCelestial:
	var rng := RandomNumberGenerator.new()
	rng.seed = p_seed
	var b := BauCelestial.new()
	b.name = "BauCelestial"
	## O indice sai PRIMEIRO do fluxo do rng, antes do ponto: assim o par de
	## elementos da manopla nao muda se a ilha mudar o relevo (o sorteio do
	## ponto consome quantidade variavel de numeros ao rejeitar agua).
	b.indice = rng.randi() % Arma.PARES_MANOPLA.size()
	var p := _ponto(island, rng)
	parent.add_child(b)
	b.global_position = p
	return b


## Ponto de pouso: anel de campo aberto, longe do centro e longe da agua. Quem
## diz se o chao serve e' a ILHA (`pode_pousar`). Antes era a janela 1.4-8.5
## copiada do Loot — e o teto proibia o bau no plato das ruinas (9,0 m), que e'
## exatamente o tipo de lugar onde um bau lendario devia poder cair.
static func _ponto(island: Node, rng: RandomNumberGenerator) -> Vector3:
	for _tentativa in 16:
		var ang := rng.randf() * TAU
		var raio := rng.randf_range(RAIO_MIN, RAIO_MAX)
		var p := Vector2(cos(ang), sin(ang)) * raio
		var h := 1.0
		if island != null and is_instance_valid(island) and island.has_method("height"):
			h = float(island.height(p.x, p.y))
			if island.has_method("pode_pousar") and not island.pode_pousar(p.x, p.y):
				continue
		return Vector3(p.x, h, p.y)
	return Vector3(0.0, 1.0, 0.0)  # ilha impossivel: cai no centro e o jogo segue


## Os 2 elementos FIXOS da manopla deste bau (GDD §16.2: par fixo, sem troca).
func elementos() -> PackedStringArray:
	return PackedStringArray(Arma.PARES_MANOPLA[indice % Arma.PARES_MANOPLA.size()])


func _ready() -> void:
	var col := CollisionShape3D.new()
	var sp := SphereShape3D.new()
	sp.radius = RAIO_ABRIR
	col.shape = sp
	add_child(col)
	monitoring = false          # so' liga quando o bau POUSA
	set_process(false)          # so' liga quando tem alguem canalizando
	_montar()
	body_entered.connect(_entrou)
	body_exited.connect(_saiu)
	## UM relogio para as duas fases (anuncio -> pouso). Timer e' no' de engine:
	## conta em C++, morre junto com a Arena no restart e PARA quando a arena
	## e' desabilitada no fim da partida. Nada disso um contador manual faria.
	_timer = Timer.new()
	_timer.one_shot = true
	_timer.timeout.connect(_no_tempo)
	add_child(_timer)
	_timer.start(ANUNCIO_S)


func _no_tempo() -> void:
	match fase:
		ESPERANDO:
			_cair()
		CAINDO:
			_pousar()


# ---------------------------------------------------------------- as 3 fases

## 1. A QUEDA — o anuncio. O feixe acende no ponto EXATO de pouso e o bau
## comeca a descer. A HUD observa por Bus.bau_anunciado e poe o marcador.
func _cair() -> void:
	fase = CAINDO
	_feixe.visible = true
	_pivo.visible = true
	_pivo.position.y = ALTURA_QUEDA
	## EASE_OUT: desce rapido e ASSENTA devagar. Queda acelerada (EASE_IN)
	## esconderia o bau ate' o ultimo segundo — o oposto do que o evento quer.
	var tw := create_tween()
	tw.tween_property(_pivo, "position:y", 0.55, QUEDA_S) \
			.set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	var tg := create_tween().set_loops()  # giro lento: le' como objeto magico
	tg.tween_property(_pivo, "rotation:y", TAU, 9.0).from(0.0)
	Bus.bau_anunciado.emit(global_position, QUEDA_S)
	_timer.start(QUEDA_S)


## 2. O POUSO — a partir daqui da' pra abrir. O feixe FICA aceso: o bau no chao
## continua sendo o farol que denuncia quem esta' la' (GDD §16.2).
func _pousar() -> void:
	fase = POUSADO
	monitoring = true
	Projectile.burst(get_parent(), global_position + Vector3(0, 0.4, 0),
			Arma.cor("manopla"), 26)
	Bus.bau_pousou.emit(global_position)
	_chamar_bots()


## 3. ABRIR — canalizacao. So' anda com alguem DENTRO do raio E DE PE'; sair,
## cair ou morrer zera.
func _process(delta: float) -> void:
	if fase != POUSADO:
		return
	var quem := _ativos()
	if quem.is_empty():
		_cancelar()  # derrubado/morto em cima do bau NAO abre bau
		return
	_progresso += delta
	_gestos(delta, quem)
	var o_player := _o_player(quem)
	if o_player != null:
		_player_canalizou = true
		_quem_canalizava = o_player
		Bus.bau_canalizando.emit(o_player, minf(_progresso / CANALIZAR_S, 1.0))
	if _progresso >= CANALIZAR_S:
		_abrir()


## O GESTO DE ABRIR, repetido enquanto a canalizacao dura. CANALIZAR_S sao 3
## segundos: com um unico disparo de 0,65s o mago passaria 2,35s parado em
## idle, e parado nao le' como "estou abrindo o bau" — le' como travamento.
## Repetido, o corpo insiste no bau, que e' exatamente a leitura que o evento
## quer (quem esta' abrindo esta' OCUPADO, e por isso e' alvo).
func _gestos(delta: float, quem: Array[Node]) -> void:
	_gesto_acc -= delta
	if _gesto_acc > 0.0:
		return
	_gesto_acc = ArmaSlot.GESTO_S
	for n in quem:
		ArmaSlot.gesto(n)


## Quem, entre os que estao em cima do bau, PODE canalizar agora. Derrubado e
## morto nao abrem bau: abrir e' um ato, e quem esta' no chao nao age (a mesma
## regra que Player._try_fire aplica ao ataque). E' o que transforma "levou
## dano e caiu" em cancelamento de verdade, em vez de um bau que abre sozinho
## por cima de um corpo.
func _ativos() -> Array[Node]:
	var out: Array[Node] = []
	for n in _dentro:
		if not is_instance_valid(n):
			continue
		var hp: Variant = n.get("hp")
		if hp != null and float(hp) <= 0.0:
			continue
		if not Derrubado.pode_agir(n):
			continue
		out.append(n)
	return out


## CANCELAR E' ESTADO DE 1a CLASSE (regra do projeto): a canalizacao volta do
## ZERO e a HUD e' avisada na BORDA, uma vez — nunca por frame (este metodo e'
## chamado a cada frame enquanto um caido segue em cima do bau).
func _cancelar() -> void:
	_gesto_acc = 0.0
	if _progresso <= 0.0 and not _player_canalizou:
		return
	_progresso = 0.0
	if _player_canalizou:
		_player_canalizou = false
		# Guardado no inicio da canalizacao: no cancelamento o pawn pode ja' ter
		# saido da lista (foi derrubado, morreu ou andou), e o sinal continua
		# precisando dizer de quem era.
		Bus.bau_canalizando.emit(_quem_canalizava, 0.0)
		_quem_canalizava = null


## Quem abre e' quem esta' MAIS PERTO no instante em que a barra enche — nao
## quem chegou primeiro. E' de proposito: com 2 inimigos em cima do bau, a
## canalizacao vira briga de corpo a corpo e da' pra ROUBAR no ultimo segundo.
## Custo: 3 linhas. Ganho: o momento mais tenso da partida.
func _abrir() -> void:
	fase = ABERTO
	set_process(false)
	monitoring = false
	var quem := _mais_perto()
	var els := elementos()  # captura ANTES: pegar() troca o par do loot pela arma velha
	var loot := Loot.bau_celestial(get_parent(), global_position, indice)
	var slot := ArmaSlot.de(quem)
	if slot != null:
		loot.pegar(slot)  # abriu = equipou; a arma velha fica no chao (padrao BR)
	Projectile.burst(get_parent(), global_position + Vector3(0, 1.0, 0),
			Arma.cor("manopla"), 40)
	Bus.bau_aberto.emit(quem != null and quem.is_in_group("player"), els)
	queue_free()


# ---------------------------------------------------------------- presenca

func _entrou(body: Node3D) -> void:
	if fase != POUSADO or ArmaSlot.de(body) == null:
		return  # chao, muro, projetil: o Area3D ve' tudo, so' pawn abre bau
	if not _dentro.has(body):
		_dentro.append(body)
	set_process(true)  # o _process so' existe enquanto tem gente em cima


func _saiu(body: Node3D) -> void:
	_dentro.erase(body)
	if not _dentro.is_empty():
		return
	## CANCELOU: sair do raio zera a canalizacao. Nao e' punicao gratuita —
	## e' o que transforma "chegar no bau" em "SEGURAR o bau", que e' a briga.
	_cancelar()
	set_process(false)


## O player da lista, ou null. Devolve o NO' e nao um booleano porque o sinal
## de canalizacao passou a carregar o dono (ver core/Bus.gd).
func _o_player(lista: Array[Node]) -> Node:
	for n in lista:
		if is_instance_valid(n) and n.is_in_group("player"):
			return n
	return null


func _mais_perto() -> Node:
	var melhor: Node = null
	var d2 := INF
	for n in _ativos():  # caido/morto nao leva a manopla nem estando em cima
		var dd: float = global_position.distance_squared_to((n as Node3D).global_position)
		if dd < d2:
			d2 = dd
			melhor = n
	return melhor


## BOTS: o conflito e' o ponto do evento — bau sem disputa e' so' um presente.
## Chamada DEFENSIVA por has_method: hoje Bot.gd nao tem `ir_para` (raia de IA,
## fora desta) e nada acontece; no dia em que tiver, os bots proximos passam a
## rotacionar para o bau sem que UMA linha daqui mude. Pedido registrado no
## relatorio ao coordenador.
func _chamar_bots() -> void:
	var pai := get_parent()
	if pai == null:
		return
	for n in pai.get_children():
		if n is Bot and n.has_method("ir_para") \
				and global_position.distance_to((n as Node3D).global_position) < ATRAIR_BOT_M:
			n.call("ir_para", global_position)


# ---------------------------------------------------------------- visual

## MODELO PROCEDURAL (zero binario — regra do projeto): caixa + tampa + cintas
## + fechadura, so' primitivas da engine. Ouro emissivo = a cor da raridade
## LENDARIA (Arma.RARIDADES), a mesma da manopla que esta' dentro.
const MODELO := "res://world/modelos/bau.glb"


func _montar() -> void:
	var ouro := Arma.cor("manopla")
	_pivo = Node3D.new()
	_pivo.visible = false
	add_child(_pivo)
	# O MODELO DE JOGO (26/08): o "Gemforged Treasure" do Meshy, decimado a 8k
	# e exportado ja' nas MEDIDAS DO CODIGO (1,15 m de largura — a colisao e o
	# raio de canalizacao nao mudam). Primitivas viram fallback defensivo.
	if ResourceLoader.exists(MODELO):
		var ps: Variant = load(MODELO)
		if ps is PackedScene:
			var m: Node3D = (ps as PackedScene).instantiate()
			m.name = "ModeloBau"
			_pivo.add_child(m)
			Pbr.domar(m)
			_telegrafo(ouro)
			return
	var madeira := StandardMaterial3D.new()
	madeira.albedo_color = Color("3a2b1e")
	# corpo
	var corpo := MeshInstance3D.new()
	var bx := BoxMesh.new()
	bx.size = Vector3(1.15, 0.70, 0.85)
	corpo.mesh = bx
	corpo.material_override = madeira
	_pivo.add_child(corpo)
	# tampa abaulada (prisma deitado: le' como tampo de bau, nao como caixote)
	var tampa := MeshInstance3D.new()
	var pr := PrismMesh.new()
	pr.size = Vector3(1.18, 0.34, 0.88)
	tampa.mesh = pr
	tampa.position.y = 0.52
	tampa.material_override = madeira
	_pivo.add_child(tampa)
	# 2 cintas + fechadura em ouro emissivo: a leitura de LENDARIA a distancia
	for i in 2:
		var cinta := MeshInstance3D.new()
		var cb := BoxMesh.new()
		cb.size = Vector3(0.10, 0.78, 0.90)
		cinta.mesh = cb
		cinta.position = Vector3(-0.34 + 0.68 * float(i), 0.10, 0.0)
		cinta.material_override = ArmaSlot.mat_brilho(ouro, 2.4)
		_pivo.add_child(cinta)
	var trava := MeshInstance3D.new()
	var tb := BoxMesh.new()
	tb.size = Vector3(0.24, 0.24, 0.10)
	trava.mesh = tb
	trava.position = Vector3(0.0, 0.16, 0.46)
	trava.material_override = ArmaSlot.mat_brilho(ouro, 3.4)
	_pivo.add_child(trava)
	_telegrafo(ouro)


## O TELEGRAFO: feixe do chao ao ceu + marcador TRIANGULAR (a forma da
## raridade lendaria, GDD §10 — quem nao distingue cor le' a forma). E' o
## "brilha no ceu, todo mundo ve onde voce esta" do GDD, aplicado ao evento.
func _telegrafo(cor: Color) -> void:
	_feixe = Node3D.new()
	_feixe.visible = false
	add_child(_feixe)
	var col := MeshInstance3D.new()
	var cyl := CylinderMesh.new()
	cyl.top_radius = 0.9
	cyl.bottom_radius = 1.7
	cyl.height = ALTURA_QUEDA
	cyl.radial_segments = 8      # mobile: 8 lados bastam num cilindro translucido
	col.mesh = cyl
	col.position.y = ALTURA_QUEDA * 0.5
	var mat := StandardMaterial3D.new()
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.cull_mode = BaseMaterial3D.CULL_DISABLED
	mat.albedo_color = Color(cor, 0.16)
	mat.emission_enabled = true
	mat.emission = cor
	mat.emission_energy_multiplier = 1.5
	col.material_override = mat
	col.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	_feixe.add_child(col)
	var marca := MeshInstance3D.new()
	var pr := PrismMesh.new()
	pr.size = Vector3(5.2, 0.04, 5.2)
	marca.mesh = pr
	marca.position.y = 0.06
	marca.material_override = ArmaSlot.mat_brilho(cor, 1.3)
	marca.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	_feixe.add_child(marca)
	## Pulso do marcador em Tween (engine-side, custo GDScript zero por frame).
	var tw := create_tween().set_loops()
	tw.tween_property(marca, "scale", Vector3(1.12, 1.0, 1.12), 0.8) \
			.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	tw.tween_property(marca, "scale", Vector3.ONE, 0.8) \
			.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
