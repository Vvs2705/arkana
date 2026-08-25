## A TEMPESTADE ARCANA — o circulo que fecha. Sem este arquivo o jogo nao e'
## um battle royale: e' um deathmatch com cronometro (a partida acabava so'
## por Balance.MATCH.duration_s ou por acabarem os bots).
##
## ⚠️ O GDD NAO ESPECIFICA ESTA MECANICA. Ele a PRESSUPOE em tres lugares —
## §"Degrau 3" ("castelo voador, queda de vassoura, ZONA FECHANDO, escudo
## evolutivo"), §3 Vidente ("le' Torres Arcanas: revela a PROXIMA ZONA SEGURA")
## e §16.5 resgate ("o espirito dura ate' a ZONA ENTRAR NA FASE FINAL") — mas
## em nenhum deles diz quantas fases, quanto encolhe ou quanto doi. TUDO o que
## esta' abaixo (tabela FASES, RAIO_INICIAL, DESLOCAMENTO, TICK_S, o dano
## crescente e a decisao de "ultimo em pe") foi PROJETADO AQUI e aguarda o
## carimbo do Diretor. Se o GDD ganhar um §sobre a zona, este arquivo obedece.
##
## O CONTRATO DE DESIGN (por que a zona existe): ela nao e' um cronometro
## disfarcado, e' um COMPRESSOR. Cada fase tem duas metades:
##   1. ESPERA — a zona esta' PARADA e o proximo circulo ja' esta' DESENHADO no
##      chao (anel fantasma que atravessa morro, `no_depth_test`). O jogador ve'
##      para onde correr ANTES de precisar correr. E' a lei do §4.3 aplicada ao
##      mapa: "se mata rapido, avisa antes".
##   2. FECHA — a parede anda. Quem ficou fora sangra, e o dano cresce a cada
##      fase: na fase 1 e' um empurrao (1.5 dps), na fase 5 e' uma sentenca
##      (18 dps mata 100 de vida em 5.6s).
## Nada e' sorteado na hora: TODOS os centros saem do seed, calculados de uma
## vez no _ready (ver `plano`).
##
## ORCAMENTO MOBILE (mesma lei do BauCelestial/Loot): ZERO _process. O relogio
## das fases e' um Timer, o encolhimento e' um Tween sobre a propriedade `raio`
## (o setter move a parede — engine-side, nenhuma linha de GDScript por frame)
## e o dano roda num Timer de 1s que varre ~7 pawns. Custo por frame: zero.
class_name Zona
extends Node3D

## Seed proprio (nao o do Loot): mexer no loot nao pode mudar os circulos.
const SEED_ZONA := 2707

## Raio de partida = MEIA-ILHA. Garante que ninguem nasce fora da zona,
## qualquer que seja o spawn.
##
## OS RAIOS DESTE ARQUIVO ESCALAM COM O MAPA. Eles foram calibrados contra uma
## ilha de ILHA_REF metros de lado; quando a ilha muda de tamanho, `plano()` le'
## o lado real e multiplica tudo. Sem isso, em 26/08 a ilha passou de 180 para
## 300 m e o raio inicial de 90 (que era meia-ilha) virou menos de um terco:
## 6 dos 14 pontos de nascimento ficaram FORA do primeiro circulo e tomavam
## dano no segundo zero. O numero cravado nao acusou nada — o comentario ao
## lado dele ja' dizia "meia-ilha" e mesmo assim envelheceu calado.
##
## KNOB: baixar aperta a queda desde o primeiro segundo; subir faz a fase 1 nao
## significar nada.
const RAIO_INICIAL := 90.0

## A ilha contra a qual RAIO_INICIAL e a tabela FASES foram escritos.
## NAO e' o tamanho atual do mapa — e' a regua de calibragem. Mudar este numero
## reinterpreta todos os raios abaixo; para mudar o MAPA, mexa em Island.SIZE.
const ILHA_REF := 180.0

## AS 5 FASES. `espera` = zona parada com o proximo circulo ja' visivel;
## `fecha` = parede andando; `raio` = onde ela para; `dps` = dano por segundo
## de quem ficou fora DURANTE E DEPOIS de fechar.
## Soma dos tempos = 165s dentro dos 180s de Balance.MATCH.duration_s (so'
## LEITURA daqui — Balance e' outra raia). Os 15s que sobram sao de proposito:
## o ultimo circulo (4m) fecha aos 165s e o duelo final acontece em cima dele.
##
## KNOBs da tabela:
##   espera AUMENTAR = mais tempo de loot e rotacao calma, partida mais morna;
##          DIMINUIR = corrida constante, sem janela para procurar arma.
##   fecha  AUMENTAR = da' pra atravessar a parede sofrendo pouco (a zona vira
##          sugestao); DIMINUIR = quem estava longe morre sem chance de correr.
##   raio   descer mais rapido = confronto mais cedo e partidas mais curtas;
##          descer devagar = o mapa nunca aperta e a zona nao decide nada.
##   dps    AUMENTAR = a zona vira a maior causa de morte do jogo (roubo de
##          kill dos magos); DIMINUIR = compensa ficar fora tomando dano de
##          graca, que e' exatamente o que a zona existe para impedir.
const FASES := [
	{"espera": 25.0, "fecha": 20.0, "raio": 62.0, "dps": 1.5},
	{"espera": 20.0, "fecha": 18.0, "raio": 42.0, "dps": 3.0},
	{"espera": 18.0, "fecha": 16.0, "raio": 24.0, "dps": 6.0},
	{"espera": 15.0, "fecha": 14.0, "raio": 12.0, "dps": 11.0},
	{"espera": 10.0, "fecha":  9.0, "raio":  4.0, "dps": 18.0},
]

## Quanto o centro novo pode fugir do centro velho, como fracao da folga
## (raio_velho - raio_novo). 1.0 = o circulo novo pode encostar na borda do
## velho (rotacoes brutais, quem esta' no lado errado nao chega); 0.0 = tudo
## concentrico e o mapa vira funil previsivel. 0.75 mantem a rotacao viva
## deixando margem de fuga.
const DESLOCAMENTO := 0.75

## s por aplicacao de dano. NAO BAIXAR PARA VALOR DE FRAME: e' exatamente o bug
## de docs/DANO.md §2.3 (dano fracionario por frame vira int 0, ~60 sinais por
## segundo, tween morto e recriado). Com 1s o menor tique do jogo e' 1.5 de
## dano — sempre >= 1, nunca zero. AUMENTAR: dano ambiental mais grosseiro,
## menos precisao na hora de morrer. DIMINUIR (ate' 0.25): mais preciso, mais
## sinal no Bus.
const TICK_S := 1.0

const ALTURA_PAREDE := 55.0     # m — alta o bastante para nao dar pra "ver por cima"
const COR := Color("b06cff")    # violeta arcano (paleta GDD §10: cor, nunca escuridao)
## m do centro em que um bot decide rotacionar. Fracao do raio: ate' 85% do
## caminho para a borda ele briga; passou disso, corre.
const BOT_MARGEM := 0.85

var fase := 0                   # 0 = antes da 1a fase; 1..FASES.size()
var fechando := false           # a parede esta' andando AGORA
## Centro e raio do circulo ATUAL. Os dois sao TWEENADOS durante o fechamento
## e os dois tem setter: e' o setter que move/escala a parede, entao o
## encolhimento inteiro nao precisa de UMA linha de _process.
var centro := Vector3.ZERO:
	set(v):
		centro = v
		_por_parede()
var raio := RAIO_INICIAL:
	set(v):
		raio = v
		_por_parede()

var _plano: Array = []          # [{centro, raio, dps}] — sorteado UMA vez, no seed
var _timer: Timer               # relogio das fases (espera -> fecha -> espera...)
var _tick: Timer                # relogio do dano (TICK_S)
var _parede: MeshInstance3D     # a cupula: raio 1.0, escalada pelo setter
var _anel: MeshInstance3D       # o telegrafo: anel do PROXIMO circulo, no chao
var _player_fora := false       # borda: so' emite zona_estado quando MUDA


## Cria a zona da partida. DETERMINISTICO: mesmo seed = mesma sequencia de
## centros e raios. `island` so' precisa responder height(x, z) — serve para
## nao plantar o circulo final no meio do lago; sem ela tudo funciona igual.
## Quantas vezes o mapa atual e' maior que a regua de calibragem (ILHA_REF).
## Le' o lado da ilha que foi ENTREGUE, e nao um preload de Island.gd: em 26/08
## a ilha ficou com erro de sintaxe salvo em disco por um tempo, e um preload
## teria derrubado a Zona junto. Sem ilha, devolve 1.0 e nada muda — e' o que
## mantem o selftest da zona rodando sem mundo.
static func escala_do_mapa(island: Node) -> float:
	if island == null or not ("SIZE" in island):
		return 1.0
	var lado := float(island.SIZE)
	if lado <= 0.0:
		return 1.0
	return lado / ILHA_REF


static func criar(parent: Node3D, island: Node, p_seed := SEED_ZONA) -> Zona:
	var z := Zona.new()
	z.name = "Zona"
	z._plano = plano(island, p_seed)
	parent.add_child(z)
	return z


## O SORTEIO INTEIRO, DE UMA VEZ (e' o que torna o determinismo testavel sem
## rodar partida: `Zona.plano(null, seed)` devolve a sequencia completa).
## Lei do circulo: o novo esta' SEMPRE contido no anterior — dist(c1,c0) + r1
## <= r0. Sem isso existiria ponto seguro AGORA que fica fora depois de andar,
## e o jogador nao teria como planejar rotacao.
## Raio com que a tempestade abre. Deriva do plano, que ja' nasce escalado.
func _raio_de_abertura() -> float:
	if _plano.is_empty():
		return RAIO_INICIAL
	# O primeiro circulo e' o DESTINO da fase 1; a abertura e' maior que ele na
	# mesma proporcao em que RAIO_INICIAL e' maior que FASES[0].raio.
	return float(_plano[0].raio) * (RAIO_INICIAL / float(FASES[0].raio))


static func plano(island: Node, p_seed := SEED_ZONA) -> Array:
	var rng := RandomNumberGenerator.new()
	rng.seed = p_seed
	var esc := escala_do_mapa(island)
	var c := Vector3.ZERO
	var r := RAIO_INICIAL * esc
	var out: Array = []
	for f in FASES:
		var novo_r := float(f.raio) * esc
		var folga: float = maxf(r - novo_r, 0.0) * DESLOCAMENTO
		c = _sortear_centro(island, rng, c, folga)
		r = novo_r
		out.append({"centro": c, "raio": r, "dps": float(f.dps)})
	return out


## Centro novo dentro da folga. sqrt(randf()) para distribuir por AREA e nao
## por raio (senao os centros se amontoam no meio). Rejeita agua pelo mesmo
## corte de altura do BauCelestial — o circulo final nao pode ser um lago onde
## ninguem pisa. ponytail: testa so' o ponto central; um circulo de 62m ainda
## pode pegar o lago na borda, e deve mesmo (agua e' terreno, nao buraco).
static func _sortear_centro(island: Node, rng: RandomNumberGenerator, c: Vector3,
		folga: float) -> Vector3:
	var ultimo := c
	for _tentativa in 12:
		var ang := rng.randf() * TAU
		var d := sqrt(rng.randf()) * folga
		var p := Vector3(c.x + cos(ang) * d, 0.0, c.z + sin(ang) * d)
		ultimo = p
		if island == null or not is_instance_valid(island) or not island.has_method("height"):
			return p
		var h := float(island.height(p.x, p.z))
		if h >= 1.4 and h <= 8.5:
			return p
	return ultimo  # ilha impossivel: aceita o ultimo palpite e o jogo segue


func _ready() -> void:
	if _plano.is_empty():
		_plano = plano(null)
	_montar_visual()
	# O raio de abertura sai da MESMA conta que gerou o plano (ja' escalado pelo
	# mapa). Ler a constante crua aqui era o que deixava a parede nascer com o
	# tamanho da ilha antiga mesmo com o plano correto.
	raio = _raio_de_abertura()     # dispara o setter: parede na escala certa
	_timer = Timer.new()
	_timer.one_shot = true
	_timer.timeout.connect(_no_tempo)
	add_child(_timer)
	## O dano tem relogio PROPRIO, separado do das fases: nenhum pawn ganha
	## _process por causa da zona (6 bots + player = 7 varreduras por segundo).
	_tick = Timer.new()
	_tick.wait_time = TICK_S
	_tick.timeout.connect(_tique)
	add_child(_tick)
	_tick.start()
	_avisar()


# ------------------------------------------------------------- as duas metades

## ESPERA — a zona esta' parada e o PROXIMO circulo ja' esta' desenhado.
## A HUD observa zona_avisou e poe o circulo na bussola/minimapa.
func _avisar() -> void:
	if fase >= _plano.size():
		return  # ultimo circulo fechado: a tempestade nao avanca mais
	var alvo: Dictionary = _plano[fase]
	var espera := float(FASES[fase].espera)
	_desenhar_anel(alvo.centro, float(alvo.raio))
	Bus.zona_avisou.emit(fase + 1, alvo.centro, float(alvo.raio), espera)
	_timer.start(espera)


## FECHA — a parede anda. Centro e raio caminham JUNTOS no mesmo Tween, na
## mesma duracao: e' isso que mantem a lei do circulo contido valida em todo
## instante do trajeto, e nao so' no comeco e no fim.
func _fechar() -> void:
	var alvo: Dictionary = _plano[fase]
	fase += 1
	fechando = true
	var dur := float(FASES[fase - 1].fecha)
	var tw := create_tween().set_parallel()
	tw.tween_property(self, "raio", float(alvo.raio), dur)
	tw.tween_property(self, "centro", alvo.centro, dur)
	Bus.zona_fechando.emit(fase, alvo.centro, float(alvo.raio), dur)
	_timer.start(dur)


func _no_tempo() -> void:
	if fechando:
		_chegou()
	else:
		_fechar()


## A parede chegou. Crava os valores exatos (o Tween pode parar em 61.9998) e
## chama a proxima espera.
func _chegou() -> void:
	fechando = false
	var alvo: Dictionary = _plano[fase - 1]
	centro = alvo.centro
	raio = float(alvo.raio)
	_anel.visible = false
	_avisar()


func _por_parede() -> void:
	if not is_instance_valid(_parede):
		return
	_parede.scale = Vector3(raio, 1.0, raio)
	_parede.position = Vector3(centro.x, ALTURA_PAREDE * 0.5, centro.z)


# ------------------------------------------------------------------- o dano

## O TIQUE. Roda 1x por segundo, varre os pawns da arena e cobra de quem esta'
## fora. Dano SO' por Combat.deal (ponto unico do projeto, ARQUITETURA.md) e
## sempre >= 1.5 por aplicacao — nunca o dano-zero de 60Hz do §2.3.
## `ignora_escudo = true`: a tempestade nao e' MAGIA de ninguem, o escudo de
## magia nao a segura (mesma regra da queimadura e do terreno).
func _tique() -> void:
	var dps := dps_atual()
	var pai := get_parent()
	if pai == null:
		return
	var player_fora := false
	for n in pai.get_children():
		if not (n is Pawn) or not is_instance_valid(n):
			continue
		var p := n as Pawn
		if p.hp <= 0.0:
			continue
		var d := _dist(p.global_position)
		if d <= raio:
			_puxar_bot(p, d)
			continue
		if p.is_in_group("player"):
			player_fora = true
		var dano := dps * TICK_S
		var efetivo: float = Combat.deal(p, dano, "zona", null, true)
		if efetivo > 0.0 and p.is_in_group("player"):
			Bus.zona_dano.emit(efetivo, dps)
		_puxar_bot(p, d)
	## BORDA, nao por frame: a HUD acende/apaga a vinheta uma vez por travessia.
	if player_fora != _player_fora:
		_player_fora = player_fora
		Bus.zona_estado.emit(not player_fora)


## DPS da fase corrente. Antes da 1a fase fechar, a zona nao doi (a partida
## abre com queda e loot, nao com sangramento).
func dps_atual() -> float:
	if fase <= 0:
		return 0.0
	return float(FASES[mini(fase, FASES.size()) - 1].dps)


func dentro(pos: Vector3) -> bool:
	return _dist(pos) <= raio


## Distancia HORIZONTAL: a zona e' um cilindro, nao uma esfera — subir no
## plato das ruinas nao pode tirar ninguem do circulo.
func _dist(pos: Vector3) -> float:
	return Vector2(pos.x - centro.x, pos.z - centro.z).length()


## BOTS: sem isto eles vagam ate' morrer na tempestade e a partida acaba
## sozinha em ~2 minutos (bots_alive chega a 0 = vitoria de graca).
## Chamada DEFENSIVA por has_method, igual a' do BauCelestial: hoje Bot.gd nao
## tem `ir_para` (raia de IA, fora desta) e nada acontece; no dia em que tiver,
## todo bot perto da borda passa a rotacionar sem que UMA linha daqui mude.
## Pedido registrado no relatorio ao coordenador.
func _puxar_bot(p: Pawn, d: float) -> void:
	if fase <= 0 or not (p is Bot) or not p.has_method("ir_para"):
		return
	if d > raio * BOT_MARGEM:
		p.call("ir_para", centro)


# ------------------------------------------------------------------ visual

## PROCEDURAL, zero binario (regra do projeto). A cupula e' UM cilindro sem
## tampas, de raio 1.0, escalado pelo setter de `raio` — encolher a zona nao
## reconstroi malha nenhuma.
func _montar_visual() -> void:
	_parede = MeshInstance3D.new()
	var cyl := CylinderMesh.new()
	cyl.top_radius = 1.0
	cyl.bottom_radius = 1.0
	cyl.height = ALTURA_PAREDE
	cyl.radial_segments = 48     # mobile: 48 lados ja' leem como circulo a 90m
	cyl.rings = 1
	cyl.cap_top = false
	cyl.cap_bottom = false
	_parede.mesh = cyl
	var mat := StandardMaterial3D.new()
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	mat.cull_mode = BaseMaterial3D.CULL_DISABLED   # visivel de dentro E de fora
	mat.albedo_color = Color(COR, 0.20)
	mat.emission_enabled = true
	mat.emission = COR
	mat.emission_energy_multiplier = 1.8
	_parede.material_override = mat
	_parede.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(_parede)
	_por_parede()
	## Pulso em Tween (engine-side): a parede "respira" sem custar frame.
	var tw := create_tween().set_loops()
	tw.tween_property(mat, "emission_energy_multiplier", 3.0, 1.1) \
			.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	tw.tween_property(mat, "emission_energy_multiplier", 1.4, 1.1) \
			.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)

	## O TELEGRAFO NO MUNDO: anel do PROXIMO circulo, deitado no chao, com
	## no_depth_test — atravessa morro e arvore. E' a resposta 3D da pergunta
	## "para onde eu corro?", que a bussola da HUD repete em 2D.
	_anel = MeshInstance3D.new()
	_anel.visible = false
	var amat := StandardMaterial3D.new()
	amat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	amat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	amat.albedo_color = Color(COR, 0.55)
	amat.emission_enabled = true
	amat.emission = COR
	amat.emission_energy_multiplier = 2.2
	amat.no_depth_test = true
	## render_priority mora no MATERIAL (transparente), nao no MeshInstance3D:
	## e' o que garante que o anel desenhe DEPOIS da parede quando os dois se
	## sobrepoem no fim da partida.
	amat.render_priority = 1
	_anel.material_override = amat
	_anel.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(_anel)


## O anel e' RECONSTRUIDO por fase (5 vezes na partida inteira) em vez de
## escalado: escalar um toro afina a espessura junto, e o anel de 4m da fase 5
## viraria um fio de 4cm invisivel — justo o momento em que ele mais importa.
func _desenhar_anel(c: Vector3, r: float) -> void:
	var t := TorusMesh.new()
	t.inner_radius = maxf(r - 0.6, 0.05)
	t.outer_radius = r + 0.6
	t.rings = 48
	t.ring_segments = 4          # secao quadrada: e' uma marca no chao, nao um cano
	_anel.mesh = t
	_anel.position = Vector3(c.x, 1.2, c.z)
	_anel.visible = true
