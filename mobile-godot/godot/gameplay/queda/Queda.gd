## A QUEDA — a abertura de partida de todo battle royale. O castelo cruza o
## mapa, o jogador salta, cai, plana e pousa. Quatro fases, uma so' maquina.
##
## ORDEM DO DIRETOR (26/08): "para os jogadores cairem no mapa como todo
## battle royale, SEM QUALQUER PODER alem das habilidades natas". Durante a
## queda nao existe magia — so' o corpo. Como o Player consome disparo, esquiva,
## tatica e suprema DENTRO do `_physics_process` dele (os quatro, sem excecao),
## desligar o `_physics_process` do Player E' o bloqueio: nao sobra caminho por
## onde uma magia saia no ar. As intencoes que ficaram engatilhadas sao LIMPAS
## no pouso — senao o primeiro toque no chao dispararia o botao que o jogador
## apertou a 200m de altura. Provado no selftest desta pasta.
##
## POR QUE A QUEDA NAO USA FISICA: a 55 m/s um CharacterBody3D anda ~0,9m por
## passo de fisica — colisao nesse passo e' tunelamento e custo de varredura
## por frame, no aparelho, com 7 pawns em campo. Aqui a posicao e' escrita
## direto e a verdade do chao e' `Island.height(x, z)`. A ilha e' PROCEDURAL e
## nasce em runtime: quem supoe y = 0 pousa dentro do morro ou 6m no ar.
## ponytail: sem colisao no ar — nao da' pra pousar em cima de arvore ou
## telhado, o pouso e' sempre no terreno. Quando o mapa ganhar teto que valha
## pouso, o lugar de mexer e' `_solo()`.
##
## ORCAMENTO MOBILE: UM no' com `_physics_process`, e so' enquanto se cai. No
## instante do pouso ele se desliga (`set_physics_process(false)`) e a partida
## inteira depois disso custa zero. Nenhuma particula, nenhum corpo extra.
class_name Queda
extends Node

## Velocidade TERMINAL da queda livre, m/s. 55 e' o que faz os 260m entre o
## castelo e o planeio passarem em ~5s: rapido o bastante pra assustar, lento o
## bastante pra dar pra corrigir a mira do pouso. SUBIR = queda seca demais, o
## jogador nao consegue escolher; DESCER = abertura arrastada.
const VEL_QUEDA := 55.0

## m/s^2 ate' chegar na terminal. Sem rampa o salto e' um teletransporte pra 55
## m/s no primeiro frame e o corpo ja' nasce rapido. Com 40 (~4g, bem acima da
## gravidade real DE PROPOSITO — e' jogo, nao simulador) a terminal chega em
## ~1,4s: da' pra SENTIR o vazio do primeiro instante.
const ACEL_QUEDA := 40.0

## Ganho horizontal enquanto se mergulha, m/s. E' o "inclinar o corpo" da fase
## 2: com ~5s de queda livre valem ~110m de deslocamento — mais da metade da
## ilha, entao saltar cedo e mergulhar chega tao longe quanto saltar em cima.
const VEL_QUEDA_HORIZ := 22.0

## Altura acima do SOLO (nao do mar) em que o corpo acha o eixo e vira planeio.
## 60m da' ~5s de planeio: tempo de escolher o telhado, e nao so' o morro.
## SUBIR = plana cedo, viaja mais longe e mais lento; DESCER = queda quase ate'
## o chao, com susto e sem escolha.
const ALTURA_PLANEIO := 60.0

## Descida no planeio, m/s. VEL_QUEDA/VEL_PLANEIO ~ 4,6: a diferenca precisa
## ser GRANDE o bastante para o jogador sentir a transicao sem olhar a HUD.
const VEL_PLANEIO := 12.0

## Deslocamento horizontal planando, m/s. Menor que o do mergulho de proposito:
## mergulhar tem que ser a forma de ir LONGE, planar a de ajustar o pouso.
const VEL_PLANEIO_HORIZ := 16.0

## m/s^2 de freio ao entrar no planeio. Com 90 os 55 m/s viram 12 em ~0,5s
## (~16m de queda): e' o "puxao" do corpo achando o eixo. Baixar demais gasta a
## janela de 60m inteira freando; subir demais para no ar como um elevador.
const FREIO_PLANEIO := 90.0

## Folga acima do terreno que ja' conta como chao, em metros. Serve para o
## ultimo passo de fisica nao enterrar o mago no morro antes de a maquina ver
## que chegou.
const ALTURA_POUSO := 0.15

## Hz do altimetro no Bus. 10 Hz e' fluido para o olho e custa 1/6 dos sinais
## de um por frame — a HUD interpola se quiser mais. NUNCA por frame: e' o
## mesmo erro de docs/DANO.md §2.3, com 60 sinais/s de puro ruido.
const HZ_ALTIMETRO := 10.0

## Onde o mago viaja em relacao ao castelo: pendurado sob o portao. ABAIXO da
## rocha porque a camera de 3a pessoa fica atras e acima — de dentro do patio
## o jogador olharia para a parede em vez do mapa que ele precisa ler.
const PORTAO := Vector3(0.0, -6.0, 0.0)

## A ABERTURA E' O CASTELO (ordem do Diretor, 26/08): "ele deve aparecer no
## ceu, voando, como uma ilha flutuante — e nao a imagem de como vemos ele por
## dentro; a camera fica mais distante". Enquanto se viaja, o pivô da camera
## larga o ombro do mago (que esta' pendurado SOB a rocha — era por isso que a
## abertura era um breu de pedra) e vai ao coracao do castelo, com o braco
## recuado ate' aqui: a ilha voadora inteira contra o ceu, o mapa la' embaixo,
## e o arrastar de olhar vira ORBITA em volta dela. No salto tudo volta ao
## ombro — o corte e' o mesmo dos BRs. 90m enquadra os ~52m do castelo com
## folga; DESCER aproxima (a rocha volta a encher a tela), SUBIR miniaturiza.
const CAM_CASTELO_DIST := 90.0
const CAM_CASTELO_ALVO := Vector3(0.0, 6.0, 0.0)

## Seed proprio dos bots: mexer no loot ou na zona nao pode mudar onde eles
## pousam (mesma regra dos outros sistemas deterministicos do projeto).
const SEED_BOTS := 5171

## Fracao da rota em que um bot pode saltar. Nem no primeiro instante (todos
## cairiam na ponta de entrada, fora do mapa) nem no ultimo (viraria fila).
const SALTO_BOT_MIN := 0.18
const SALTO_BOT_MAX := 0.86

var fase := "no_castelo"     # "no_castelo" | "caindo" | "planando" | "pousou"
var castelo: Castelo

## Player ou BOT? So' o jogador fala com o Bus (altimetro, fase) e so' ele
## salta por toque. O bot cai pela MESMA lei — passo de fisica desligado, ou
## seja, sem magia no ar — e salta sozinho numa hora sorteada.
var e_player := true
var _t_salto := -1.0         # bot: instante da rota em que ele pula
var _player: Node3D
var _island: Node
var _t := 0.0                # relogio da rota do castelo
var _meia_ilha := 0.0        # SIZE/2 do mapa desta partida (cache: ver _mover)
var _vy := 0.0               # velocidade de DESCIDA, positiva (m/s)
var _alt_acc := 0.0          # relogio do altimetro
var _arm_len := -1.0         # braco original da camera, devolvido no salto
var _arm_mask := 0


## A UNICA porta de entrada. Poe o player no castelo e devolve o no' que manda
## na queda (null se nao havia player). Tudo nasce sob a Arena: restart = queda
## nova, sem estado atravessando partida.
static func iniciar(arena: Node3D, island: Node, player: Node3D) -> Queda:
	if arena == null or player == null or not is_instance_valid(player):
		return null
	var q := Queda.new()
	q.name = "Queda"          # o nome importa: `Queda.no_ar(pawn)` procura por ele
	q._player = player
	q._island = island
	q.castelo = Castelo.criar(arena, island)
	player.add_child(q)
	return q


## OS BOTS CAEM JUNTO. Eles compartilham o castelo do jogador (um castelo so'
## no ceu) e obedecem a MESMA lei: o passo de fisica fica desligado ate' pousar,
## entao nenhum deles conjura no ar.
##
## DETERMINISTICO por seed proprio: a mesma partida sorteia sempre os mesmos
## instantes de salto. Sem isso, dois testes do mesmo cenario dariam pousos
## diferentes e o portao viraria moeda.
static func iniciar_bots(bots: Array, island: Node, castelo_do_player: Castelo,
		p_seed := SEED_BOTS) -> Array:
	var fora: Array = []
	var rng := RandomNumberGenerator.new()
	rng.seed = p_seed
	for b in bots:
		if b == null or not is_instance_valid(b) or not (b is Node3D):
			continue
		var q := Queda.new()
		q.name = "Queda"
		q.e_player = false
		q._player = b as Node3D
		q._island = island
		q.castelo = castelo_do_player
		## Espalhados pela rota: se todos saltassem juntos, os seis pousariam
		## no mesmo ponto e a partida abriria com um amontoado.
		q._t_salto = rng.randf_range(SALTO_BOT_MIN, SALTO_BOT_MAX)
		(b as Node3D).add_child(q)
		fora.append(q)
	return fora


## O jogador esta' no ar AGORA? E' o gancho para quem quiser barrar magia na
## origem (um `if Queda.no_ar(self): return` no topo de um `_try_fire`). Hoje o
## bloqueio ja' e' garantido por outro caminho — o `_physics_process` desligado
## — e esta funcao existe para a costura ficar EXPLICITA no dia em que alguem
## mover o disparo para fora do passo de fisica.
static func no_ar(pawn: Node) -> bool:
	if pawn == null or not is_instance_valid(pawn):
		return false
	var q := pawn.get_node_or_null("Queda")
	return q != null and str(q.fase) != "pousou"


func _ready() -> void:
	## O corpo passa a ser conduzido por aqui: sem isto o Player aplicaria
	## gravidade e `move_and_slide` por baixo, brigando com a posicao escrita
	## nesta maquina — e, de quebra, e' o que barra magia no ar (ver topo).
	_player.set_physics_process(false)
	if _player is CharacterBody3D:
		(_player as CharacterBody3D).velocity = Vector3.ZERO
	## GRUDA JA', no frame zero. Sem isto o mago passa o primeiro frame no
	## SPAWN (no chao) e um salto imediato terminaria em pouso instantaneo —
	## foi o que apareceu ao rodar a queda contra a ilha de verdade.
	_grudar()
	## O CASTELO VIAJA SEM BONECOS (decisao no 14, 26/08 — DIRECAO.md §2):
	## como a nave do Apex e o onibus do Fortnite, o que cruza o ceu e' um
	## OBJETO, nao uma multidao pendurada. O mago SURGE ao acionar o salto —
	## "mesmo que ele surja do nada isso nao e' um problema", palavras dele.
	if _player is Node3D:
		(_player as Node3D).visible = false
	_meia_ilha = Castelo.lado(_island) * 0.5
	_anim("cair")   # no portao a pose de queda ja' le' como "prestes a saltar"
	_cam_castelo(true)
	Bus.queda_fase.emit(fase)


func _physics_process(delta: float) -> void:
	if not is_instance_valid(_player):
		set_physics_process(false)
		return
	match fase:
		"no_castelo":
			_viajar(delta)
		"caindo":
			_cair(delta)
		"planando":
			_planar(delta)
	if e_player:
		_camera()          # bot nao tem camera para arrastar
		_altimetro(delta)  # nem altimetro: a HUD e' do jogador


# ------------------------------------------------------------ 1. no castelo

## O jogador viaja colado no castelo. Ele pode OLHAR (a camera nao passa pelo
## passo de fisica do Player) e escolher a hora; um toque salta.
func _viajar(delta: float) -> void:
	_t += delta
	_grudar()
	if not e_player and _t_salto >= 0.0 and _t >= _t_salto * _duracao_rota():
		saltar()
		return
	if _tocou() or _t >= _duracao_rota():
		## Quem nao salta e' EMPURRADO no fim da rota: ninguem fica preso num
		## castelo que ja' saiu do mapa (e que se apaga sozinho la').
		saltar()


## Pendurado no portao. O castelo se move por Tween (engine-side), entao esta
## e' a unica linha que o mago precisa por frame enquanto viaja.
func _grudar() -> void:
	if is_instance_valid(castelo):
		_player.global_position = castelo.global_position + PORTAO


## UM TOQUE SALTA. Le as intencoes que a HUD ja' engatilha hoje (Fogo/Esquiva)
## em vez de exigir botao novo: a UI e' outra raia e a queda nao pode depender
## de ela mudar para funcionar. Botao dedicado, quando existir, chama `saltar()`.
func _tocou() -> bool:
	if not e_player:
		return false          # bot nao tem dedo; ele salta pelo relogio da rota
	## O botao de SALTO entrou na HUD em 27/08 e e', por nome e por gesto, o
	## caminho natural de saltar do castelo — os outros dois seguem valendo.
	var tocou := bool(_player.get("_want_fire")) or bool(_player.get("_want_dodge")) 			or bool(_player.get("_want_jump"))
	if tocou:
		_limpar_intencoes()
	return tocou


func _duracao_rota() -> float:
	if is_instance_valid(castelo) and not castelo.plano.is_empty():
		return float(castelo.plano.duracao)
	return Castelo.DURACAO


## Pula. Publica so' na BORDA (chamar duas vezes nao emite duas). ponytail: o
## corpo NAO herda a velocidade do castelo — seria um knob a mais pra calibrar
## e ninguem sente 10 m/s dentro de uma queda de 55.
func saltar() -> bool:
	if fase != "no_castelo":
		return false
	# O SURGIMENTO (decisao no 14): o corpo aparece na janela do castelo no
	# instante do salto — antes disso o castelo viaja vazio.
	if _player is Node3D:
		(_player as Node3D).visible = true
	_cam_castelo(false)
	_fase("caindo")
	_anim("cair")
	return true


# --------------------------------------------------------- 2. queda livre

func _cair(delta: float) -> void:
	_vy = move_toward(_vy, VEL_QUEDA, ACEL_QUEDA * delta)
	_mover(delta, VEL_QUEDA_HORIZ)
	var alt := altura()
	if alt <= ALTURA_POUSO:
		_pousar()
	elif alt <= ALTURA_PLANEIO:
		## Abaixo desta altura o corpo acha o eixo. O freio mora no _planar:
		## trocar de fase nao teleporta velocidade, ela CAI ate' a de planeio.
		_fase("planando")
		_anim("planar")


# -------------------------------------------------------------- 3. planeio

func _planar(delta: float) -> void:
	_vy = move_toward(_vy, VEL_PLANEIO, FREIO_PLANEIO * delta)
	_mover(delta, VEL_PLANEIO_HORIZ)
	if altura() <= ALTURA_POUSO:
		_pousar()


# ----------------------------------------------------------------- 4. pouso

## Devolve o controle ao jogador do jeito que o Player espera encontrar: em pe'
## no terreno, parado, sem intencao velha engatilhada e com o passo de fisica
## de volta no dono.
func _pousar() -> void:
	var p := _player.global_position
	_player.global_position = Vector3(p.x, _solo(p.x, p.z), p.z)
	_vy = 0.0
	if _player is CharacterBody3D:
		(_player as CharacterBody3D).velocity = Vector3.ZERO
	_player.set("_move_vel", Vector3.ZERO)   # inercia do Pawn: chega no chao zerada
	_limpar_intencoes()
	_player.set_physics_process(true)
	_anim("idle")
	_fase("pousou")
	if e_player:
		Bus.queda_altura.emit(0.0, 0.0)      # a HUD apaga o altimetro
	set_physics_process(false)               # acabou: esta maquina nao custa mais nada


# ------------------------------------------------------------------ comuns

## Passo de movimento do ar: desce por `_vy` e deriva na direcao do joystick.
## A direcao sai do PROPRIO Player (`_move_dir`), que ja' resolve zona morta,
## curva do analogico e eixo da camera — reimplementar aqui seria um segundo
## lugar para o controle divergir.
func _mover(delta: float, vel_h: float) -> void:
	var d := _direcao()
	var antes := _player.global_position
	_player.global_position = _prender(
			antes + Vector3(d.x * vel_h, -_vy, d.z * vel_h) * delta, antes)
	if d.length_squared() > 0.0001 and _player.has_method("face_dir"):
		_player.call("face_dir", d, delta)


## FORA DO QUADRADO DO TERRENO NAO HA' MALHA NEM COLISAO: quem pousa la' pousa
## no vazio e cai para sempre assim que a fisica volta. A regra nao e' um
## clamp seco — a ROTA DO CASTELO COMECA FORA DO MAPA, e teleportar para dentro
## quem acabou de saltar seria um solavanco de dezenas de metros. Aqui quem
## esta' fora so' fica proibido de se AFASTAR mais; voltar e' sempre livre.
func _prender(p: Vector3, antes: Vector3) -> Vector3:
	var lx := maxf(_meia_ilha, absf(antes.x))
	var lz := maxf(_meia_ilha, absf(antes.z))
	p.x = clampf(p.x, -lx, lx)
	p.z = clampf(p.z, -lz, lz)
	return p


func _direcao() -> Vector3:
	if not _player.has_method("_move_dir"):
		return Vector3.ZERO
	var d: Vector3 = _player.call("_move_dir")
	d.y = 0.0
	return d


## Altura acima do CHAO, que e' o que o altimetro e o pouso querem saber — a
## ilha tem morro de 9m e lago de -2,4m, entao altura absoluta mentiria.
func altura() -> float:
	var p := _player.global_position
	return p.y - _solo(p.x, p.z)


## A verdade do chao. `height()` e' o contrato de leitura da ilha; sem ilha
## (selftest, cena solta) o mundo e' plano no zero.
func _solo(x: float, z: float) -> float:
	if _island != null and is_instance_valid(_island) and _island.has_method("height"):
		return float(_island.call("height", x, z))
	return 0.0


func _fase(nova: String) -> void:
	fase = nova
	if e_player:
		Bus.queda_fase.emit(nova)   # a HUD e' do JOGADOR: bot nao mexe nela


## A camera do Player e' reposicionada dentro do `_physics_process` dele, que
## esta' desligado — sem esta linha o mago cai e a camera fica no castelo.
## No trajeto ("no_castelo") o pivô mora no CASTELO, nao no ombro (ver
## CAM_CASTELO_DIST): e' o plano geral da ilha voadora que o Diretor pediu.
func _camera() -> void:
	var cam: Variant = _player.get("cam_yaw")
	if not (cam is Node3D):
		return
	if fase == "no_castelo" and is_instance_valid(castelo):
		(cam as Node3D).global_position = castelo.global_position + CAM_CASTELO_ALVO
	else:
		(cam as Node3D).global_position = _player.global_position + Vector3(0, 1.85, 0)


## Liga/desliga o plano geral do castelo: braco da camera recuado e sem
## colisao (a PROPRIA rocha encolheria o SpringArm e devolveria o breu).
## Devolve exatamente o que encontrou — o ombro do mago nao e' desta maquina.
func _cam_castelo(ligar: bool) -> void:
	if not e_player:
		return
	var pitch: Variant = _player.get("cam_pitch")
	if not (pitch is Node3D) or (pitch as Node3D).get_child_count() == 0:
		return
	var arm := (pitch as Node3D).get_child(0) as SpringArm3D
	if arm == null:
		return
	if ligar:
		_arm_len = arm.spring_length
		_arm_mask = arm.collision_mask
		arm.spring_length = CAM_CASTELO_DIST
		arm.collision_mask = 0
		_camera()   # ja' no frame zero — sem esperar o primeiro passo de fisica
	elif _arm_len >= 0.0:
		arm.spring_length = _arm_len
		arm.collision_mask = _arm_mask


## Altimetro a HZ_ALTIMETRO. `velocidade` sai POSITIVA = descendo.
func _altimetro(delta: float) -> void:
	_alt_acc += delta
	if _alt_acc < 1.0 / HZ_ALTIMETRO:
		return
	_alt_acc = 0.0
	Bus.queda_altura.emit(maxf(altura(), 0.0), _vy)


## Nenhuma magia sai do ar (ordem do Diretor): o que ficou engatilhado durante
## a queda MORRE aqui, nao no chao. `set` em propriedade inexistente e' no-op —
## um pawn de teste sem estes campos passa por aqui sem ruido.
func _limpar_intencoes() -> void:
	_player.set("_want_fire", false)
	_player.set("_want_dodge", false)
	_player.set("_want_jump", false)
	_player.set("_want_tatica", false)
	_player.set("_want_suprema", false)


func _anim(nome: String) -> void:
	if _player.has_method("anim"):
		_player.call("anim", nome)
