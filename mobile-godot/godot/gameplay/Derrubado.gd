## DERRUBADO E REERGUER — o estado que define o battle royale de esquadrao.
## Ate' aqui vida zero era morte imediata e METADE do elenco nao tinha como
## existir: a passiva da Vitalis (Lumen reergue enquanto ela luta, GDD §3.7), a
## passiva do Ilusionista (ao ser DERRUBADO quebra em cacos de luz, §3.8), a
## kamikaze que derruba o conjurador em vez de mata-lo (§4.5) e o Espirito
## Errante (§18.6) todas comecam nesta palavra.
##
## VOCABULARIO 10+: aqui ninguem sangra e ninguem se mutila. O mago DERRUBA e
## ESVAECE — a luz dele se apaga aos poucos ate' apagar de vez. O Ilusionista
## quebra em CACOS DE LUZ. Nenhuma linha deste arquivo diz outra coisa.
##
## COMO ACOPLA — POR FORA (mesmo padrao do ArmaSlot e do KitRunner): este e' um
## no' FILHO do pawn. Tem estado quem tem este filho:
##     Derrubado.derrubar(pawn)      # forca (kamikaze, Ilusionista)
##     Derrubado.esta(pawn)          # esta' derrubado?
##     Derrubado.pode_agir(pawn)     # false enquanto derrubado
## Pawn.gd / Combat.gd / Player.gd / Bot.gd nao sabem que este arquivo existe —
## as 6 linhas de costura estao listadas no relatorio da raia.
##
## O TRUQUE QUE FAZ TUDO FUNCIONAR SEM REESCREVER O DANO: ao cair, a vida NAO
## fica em zero — ela vira a RESERVA DE ESVAECIMENTO (hp = 100 de novo, agora
## drenando). Com isso o ponto unico de dano (Combat.deal) continua valendo
## palavra por palavra: o esvaecimento e' DoT como qualquer outro, a
## finalizacao e' dano normal, e a morte de verdade sai do MESMO `if hp <= 0`
## que ja' existia — por isso `Bus.entity_died` sai UMA vez so'.
##
## ORCAMENTO MOBILE (lei do Loot/Bau): ZERO _process por entidade derrubada. O
## relogio e' um Timer (conta em C++), a 4 Hz, e existe apenas enquanto alguem
## esta' caido — em 99% da partida este arquivo custa exatamente nada.
class_name Derrubado
extends Node

# ---------------------------------------------------------------- KNOBS
## Locais de proposito: core/Balance.gd e' de outra raia. Pedido de migracao
## registrado no relatorio ao coordenador (bloco Balance.DERRUBADO).

## Quanto tempo o mago fica caido antes de apagar de vez. AUMENTAR = resgate
## vira rotina e a kill deixa de valer; DIMINUIR = ninguem chega a tempo e o
## estado vira so' uma animacao de morte mais longa. 30s e' a faixa do genero.
const ESVAECER_S := 30.0
## Canalizacao do resgate. AUMENTAR = reerguer no meio da briga fica impossivel;
## DIMINUIR = o abate perde valor (mata, ele levanta, mata de novo).
const REERGUER_S := 6.0
const RAIO_M := 2.4          # m — precisa CHEGAR no caido, nao so' olhar
const RASTEJO := 0.35        # fator de velocidade do caido (rastejar)
## Volta com vida PARCIAL e ZERO escudo (GDD §3: "a fada de revive nao gera
## escudo — o Apex removeu por ser forte demais, copiamos a licao").
const VIDA_REERGUIDO := 0.30
const TIQUE := 0.25          # s — mesma cadencia do Balance.DOT.tick
## INTERRUPCAO: o progresso DECAI na mesma velocidade com que subiu (1.0), nao
## zera e nao congela. Zerar transforma resgate contestado em tudo-ou-nada e
## pune escorregao de dedo no mobile; congelar tira qualquer premio de
## pressionar quem resgata. Decaindo simetrico, "atrapalhar o socorro" vale
## exatamente o tempo que voce comprou. KNOB: 0.0 congela, >1.0 pune mais.
const DECAI := 1.0
## SOLO (a realidade de hoje: 1 player + 6 bots, sem esquadrao). Num BR solo
## derrubado sem aliados e' morte com passos a mais — entao o estado SO'
## acontece para quem tem alguem que possa vir. Bot nao tem esquadrao, logo bot
## nao cai: morre como sempre morreu, e a partida de hoje nao muda em nada.
## Ligar este KNOB derruba o player solo tambem (playtest do tempo/feel).
const SOLO_DERRUBA := false

var pawn: Node                 # o caido (Pawn, ou o REFLEXO do Ilusionista)
var causador: Node             # quem derrubou (null = terreno/esvaecimento)
## Canalizacao acumulada em SEGUNDOS, nao em fracao 0..1: multiplo exato de
## TIQUE fecha a conta sem erro de ponto flutuante (0,25 e' exato em binario;
## 1/24 nao e'). Sem isso o resgate encalha em 0.99999 e nunca completa.
var canal := 0.0
var mult_reerguer := 1.0       # Jardim da Aurora escreve aqui (acelerar)
var _mult_left := 0.0
var _speed_factor := 1.0       # guardado para devolver no reerguer
var _reanimador: Node          # quem esta' canalizando AGORA (null = ninguem)
var _timer: Timer


# ------------------------------------------------------------ porta de entrada

## A COSTURA COM O PONTO UNICO DE DANO. Combat.deal chama isto no instante em
## que a vida zera; `true` = NAO morra agora, virou derrubado.
static func interceptar(alvo: Node, autor: Node = null) -> bool:
	if esta(alvo):
		return false      # ja' estava caido: ISTO foi a finalizacao — deixa morrer
	if not pode_cair(alvo):
		return false      # solo/bot/nao-pawn: morre como sempre morreu
	derrubar(alvo, autor)
	return true


## Derruba a FORCA, sem perguntar se ha' esquadrao. E' esta a porta da kamikaze
## (§4.5: "o conjurador cai no estado derrubado") e do reflexo caido do
## Ilusionista (§3.8) — os dois derrubam por REGRA, nao por falta de vida.
static func derrubar(alvo: Node, autor: Node = null) -> Derrubado:
	if alvo == null or not is_instance_valid(alvo) or not ("hp" in alvo):
		return null
	var d := de(alvo)
	if d != null:
		return d
	d = Derrubado.new()
	d.name = "Derrubado"
	d.causador = autor
	alvo.add_child(d)   # _ready faz a entrada no estado
	return d


## REERGUER. `por` = quem resgatou (null = kit/roteiro). `frac` da' o gancho do
## Ilusionista, que precisa levantar com a vida que quiser.
static func reerguer(alvo: Node, por: Node = null, frac := VIDA_REERGUIDO) -> bool:
	var d := de(alvo)
	if d == null:
		return false
	d._sair()
	var maximo := float(Balance.PLAYER.hp)
	alvo.hp = maxf(maximo * frac, 1.0)
	## ZERO ESCUDO (GDD §3). O NIVEL fica — ele e' progressao de dano causado,
	## nao um bem que a queda confisca; o que nao volta e' a capacidade cheia.
	if "shield" in alvo:
		alvo.shield = 0.0
		Bus.shield_changed.emit(alvo, 0.0, Combat.escudo_max(alvo), int(alvo.shield_level))
	if alvo.is_in_group("player"):
		Bus.health_changed.emit(float(alvo.hp), maximo)
	Bus.entity_reerguida.emit(alvo, por)
	return true


# ------------------------------------------------------------------ consultas

static func de(p: Node) -> Derrubado:
	if p == null or not is_instance_valid(p):
		return null
	for c in p.get_children():
		# o filtro do queue_free importa: reerguer e finalizar liberam o no', e
		# ate' o fim do frame ele ainda aparece na lista de filhos.
		if c is Derrubado and not c.is_queued_for_deletion():
			return c
	return null


static func esta(p: Node) -> bool:
	return de(p) != null


## Derrubado nao conjura, nao usa tatica/suprema e nao esquiva (a unica coisa
## que lhe resta e' rastejar). Fiacao defensiva: quem nao esta' caido AGE.
static func pode_agir(p: Node) -> bool:
	return not esta(p)


## Cai quem TEM quem venha busca-lo. E' a regra que impede o "derrubado solo",
## que e' morte com passos a mais.
static func pode_cair(alvo: Node) -> bool:
	if alvo == null or not is_instance_valid(alvo) or not ("hp" in alvo):
		return false
	return SOLO_DERRUBA or tem_esquadrao(alvo)


## Ha' alguem de pe' no mesmo esquadrao? Hoje esquadrao = grupo "player" — o
## MESMO criterio do Combat._aliado, de proposito: quando duplas/trios
## entrarem, sao ESTAS duas funcoes que mudam, e mais nenhuma.
static func tem_esquadrao(alvo: Node) -> bool:
	var pai := alvo.get_parent()
	if pai == null:
		return false
	for n in pai.get_children():
		if n != alvo and _mesmo_esquadrao(n, alvo) and _de_pe(n):
			return true
	return false


static func _mesmo_esquadrao(a: Node, b: Node) -> bool:
	return a.is_in_group("player") and b.is_in_group("player")


static func _de_pe(n: Node) -> bool:
	if n.is_in_group("reanimador"):
		return true            # PROXY sem corpo (a Lumen da Vitalis)
	return ("hp" in n) and float(n.hp) > 0.0 and not esta(n)


## JARDIM DA AURORA (§3.7): "reerguer/reviver ali e' 50% mais rapido" —
## Derrubado.acelerar(caido, 1.5, dur). Vale para qualquer fonte futura.
static func acelerar(alvo: Node, mult: float, dur: float) -> void:
	var d := de(alvo)
	if d == null:
		return
	d.mult_reerguer = maxf(mult, 0.0)
	d._mult_left = maxf(d._mult_left, dur)


## Reserva de esvaecimento = a vida cheia do mago. Nao e' capricho: com este
## valor a barra da HUD e o sinal health_changed continuam significando a mesma
## coisa que significavam de pe', sem tabela nova.
static func vida_esvaecer() -> float:
	return float(Balance.PLAYER.hp)


static func dano_tique() -> float:
	return vida_esvaecer() / ESVAECER_S * TIQUE


## 1.0 = acabou de cair, 0.0 = apagou. A HUD multiplica por ESVAECER_S se
## quiser os segundos. Sai da VIDA, nao de um cronometro paralelo: quem leva
## dano no chao apaga mais cedo, e o numero da HUD ja' conta isso sozinho.
func esvaecimento() -> float:
	if not is_instance_valid(pawn):
		return 0.0
	return clampf(float(pawn.hp) / vida_esvaecer(), 0.0, 1.0)


## 0..1 do resgate (1.0 = levantou). A HUD desenha o anel por aqui.
func progresso() -> float:
	return clampf(canal / REERGUER_S, 0.0, 1.0)


# --------------------------------------------------------------- ciclo de vida

func _ready() -> void:
	pawn = get_parent()
	# Rastejar: a velocidade e' PRODUTO UNICO (regra do projeto) — entra-se
	# nela por um fator, nunca escrevendo m/s. Ponytail: speed_factor e' campo
	# de spawn (so' o Bot escreve, uma vez); o teto conhecido e' outra raia
	# passar a escrever nele por frame — ai' isto vira produto, nao troca.
	if "speed_factor" in pawn:
		_speed_factor = float(pawn.speed_factor)
		pawn.speed_factor = _speed_factor * RASTEJO
	pawn.hp = vida_esvaecer()
	## Derrubado nao tem escudo. Sem esta linha um caido por DoT (que ignora o
	## escudo, Balance.DOT) ficaria no chao com 50 de escudo intacto.
	if "shield" in pawn:
		pawn.shield = 0.0
		Bus.shield_broken.emit(pawn)
		Bus.shield_changed.emit(pawn, 0.0, Combat.escudo_max(pawn), int(pawn.shield_level))
	## Limpeza por SINAL, nao pela costura: se qualquer outro caminho matar
	## este pawn, este no' se desfaz sozinho em vez de deixar o rastejo colado.
	Bus.entity_died.connect(_on_morreu)
	_timer = Timer.new()
	_timer.wait_time = TIQUE
	_timer.timeout.connect(_tique)
	add_child(_timer)
	_timer.start()
	Bus.entity_derrubada.emit(pawn, causador)
	_avisar()


## O UNICO relogio do estado (4 Hz, engine-side): esvaecimento, resgate e
## feedback saem todos daqui.
func _tique() -> void:
	if not is_instance_valid(pawn):
		queue_free()
		return
	if _mult_left > 0.0:
		_mult_left = maxf(_mult_left - TIQUE, 0.0)
		if _mult_left <= 0.0:
			mult_reerguer = 1.0
	_marcar(_achar_reanimador())
	if _reanimador != null:
		canal = minf(canal + TIQUE * mult_reerguer, REERGUER_S)
	else:
		canal = maxf(canal - TIQUE * DECAI, 0.0)
	_avisar()
	if canal >= REERGUER_S:
		Derrubado.reerguer(pawn, _reanimador)
		return
	## O ESVAECIMENTO PASSA PELO PONTO UNICO DE DANO (nunca hp direto), com
	## ignora_escudo=true — e' DoT como o terreno e a queimadura. Quando a
	## reserva acaba, Combat.deal chega no mesmo `if hp <= 0` de sempre, a
	## costura devolve false (ja' estamos caidos) e `entity_died` sai UMA vez.
	var quanto := dano_tique()
	if float(pawn.hp) - quanto < 0.001:
		quanto = float(pawn.hp)  # ultimo tique: fecha a conta EXATA em zero
	Combat.deal(pawn, quanto, "esvaecer", causador, true)


func _on_morreu(quem: Node) -> void:
	if quem == pawn:
		_sair()


## Desfaz o estado (por reerguer ou por morte). Nao mexe em vida: quem chama
## sabe o que quer nela.
func _sair() -> void:
	_marcar(null)
	if is_instance_valid(pawn) and "speed_factor" in pawn:
		pawn.speed_factor = _speed_factor
	if _timer != null:
		_timer.stop()
	queue_free()


# ------------------------------------------------------------------- o resgate

## Quem esta' canalizando: o mais proximo do esquadrao, de pe', dentro do raio.
## Varre os filhos da arena (7 nos) — mais barato e mais testavel que Area3D,
## e roda em headless. Mesmo criterio do KitRunner.alvos_perto.
func _achar_reanimador() -> Node:
	var pai := pawn.get_parent()
	if pai == null or not (pawn is Node3D):
		return null
	var origem: Vector3 = (pawn as Node3D).global_position
	var melhor: Node = null
	var d2 := RAIO_M * RAIO_M
	for n in pai.get_children():
		if n == pawn or not (n is Node3D):
			continue
		if not _mesmo_esquadrao(n, pawn) or not _de_pe(n):
			continue
		var dd: float = (n as Node3D).global_position.distance_squared_to(origem)
		if dd <= d2:
			d2 = dd
			melhor = n
	return melhor


## MAOS LIVRES (§3.7) SEM UMA LINHA DE EXCECAO: quem canaliza entra no grupo
## "reanimando" e a costura do Player la' barra o disparo. A Vitalis nunca
## entra nesse grupo porque nao e' ela que canaliza — e' a LUMEN, um no' do
## grupo "reanimador" que a raia de habilidades faz voar ate' o caido. A
## passiva dela e', literalmente, delegar este marcador.
func _marcar(novo: Node) -> void:
	if novo == _reanimador:
		return
	if is_instance_valid(_reanimador):
		_reanimador.remove_from_group("reanimando")
	_reanimador = novo
	if is_instance_valid(_reanimador):
		_reanimador.add_to_group("reanimando")


## FEEDBACK (a HUD OBSERVA, nunca decide). So' sai quando o PLAYER esta' na
## cena — 6 bots caidos emitindo a 4 Hz seria enxurrada no Bus, mesma regra do
## kit_cooldown.
func _avisar() -> void:
	if pawn.is_in_group("player") or (_reanimador != null and _reanimador.is_in_group("player")):
		Bus.derrubado_progresso.emit(pawn, esvaecimento(), progresso())
