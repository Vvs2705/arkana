## PONTO UNICO de dano (regra provada do projeto — ARQUITETURA.md).
## Ninguem toca hp nem escudo de ninguem fora daqui; UI/telemetria OBSERVAM
## pelo Bus.
##
## A ORDEM DO DANO (GDD §5 + docs/DANO.md §3.2):
##   1. o ESCUDO absorve primeiro, com o fator `esc` do elemento;
##   2. o excedente do MESMO tiro TRANSBORDA para a vida, com o fator `vida`;
##   3. DoT (queimadura, terreno, nevoa) pula o passo 1 — o escudo protege
##      contra MAGIA, nao contra estar em chamas (KNOB Balance.DOT.ignora_escudo).
## Sem transbordo o ultimo tiro contra um escudo de 3 pontos desperdicaria 10
## de dano e o TTK ganharia um degrau aleatorio.
class_name Combat


## Troco de dano fracionario por alvo (ver comentario em deal()). Alvo morto
## sai do dicionario em entity_died — sem isso vaza um float por bot por partida.


## `source` = quem causou (null p/ terreno/ambiente). `ignora_escudo` = e' DoT.
## Devolve o dano EFETIVO aplicado (escudo + vida); 0.0 = nao passou. Devolver
## o numero em vez de bool mantem todo `if Combat.deal(...)` funcionando (0.0 e'
## falso) e da' ao chamador o valor certo para o numero flutuante — sem isso o
## acerto em escudo mostraria o dano cru, nao o que o escudo comeu.
static func deal(target: Node, amount: float, element := "fire", source: Node = null,
		ignora_escudo := false) -> float:
	if not is_instance_valid(target) or not ("hp" in target):
		return 0.0
	if not (amount > 0.0):  # barra NaN, zero e negativo de uma vez so'
		return 0.0
	if target.hp <= 0.0:
		return 0.0  # ja' morto: nada de dano nem sinal duplicado
	if ("iframes_left" in target) and float(target.iframes_left) > 0.0:
		return 0.0  # esquiva com i-frames (Balance.DODGE.iframes) — dano nao passa

	var no_escudo := 0.0
	var restante := amount
	var pula: bool = ignora_escudo and bool(Balance.DOT.ignora_escudo)
	if not pula and _tem_escudo(target):
		var esc := fator(element, "esc")
		var pedido := amount * esc                        # quanto ESTE elemento morde
		no_escudo = minf(pedido, float(target.shield))
		target.shield = float(target.shield) - no_escudo
		# TRANSBORDO: o excedente volta a dano CRU e segue para a vida.
		restante = (pedido - no_escudo) / maxf(esc, 0.001) if bool(Balance.ESCUDO.transbordo) else 0.0
		if float(target.shield) <= 0.0:
			Bus.shield_broken.emit(target)
		Bus.shield_changed.emit(target, float(target.shield), escudo_max(target),
				int(target.shield_level))
	var na_vida := 0.0
	if restante > 0.0:
		# Sem inflar overkill: o dano EFETIVO e' o que a barra perdeu de verdade
		# — senao um tiro de 200 numa vida de 10 creditaria 200 para a evolucao.
		na_vida = minf(restante * fator(element, "vida"), float(target.hp))
		target.hp = float(target.hp) - na_vida
	var efetivo := no_escudo + na_vida
	if efetivo <= 0.0:
		return 0.0

	_creditar(source, target, efetivo)
	Bus.damage_applied.emit(target, efetivo, element, source, no_escudo > 0.0)
	if na_vida > 0.0 and target.is_in_group("player"):
		Bus.health_changed.emit(float(target.hp), float(Balance.PLAYER.hp))
	# DERRUBADO (gameplay/Derrubado.gd): quem tem esquadrao CAI em vez de morrer.
	# false = morre mesmo (solo, bot, ou finalizacao de quem ja' estava caido).
	if target.hp <= 0.0 and not Derrubado.interceptar(target, source):
		Bus.entity_died.emit(target)
		if target.has_method("die"):
			target.die()
	return efetivo


## Fator adimensional do elemento (esc/vida/empurrao/estrutura). Quem nao e'
## elemento ("terrain", DoT, kit) e' NEUTRO — 1.0 em tudo.
static func fator(element: String, chave: String) -> float:
	if not (element in Balance.ELEMENTS):
		return 1.0
	return float(Arma.base(element).get(chave, 1.0))


static func escudo_max(who: Node) -> float:
	var niveis: Array = Balance.ESCUDO.niveis
	return float(niveis[clampi(int(who.shield_level) - 1, 0, niveis.size() - 1)])


static func _tem_escudo(target: Node) -> bool:
	return ("shield" in target) and ("shield_level" in target) and float(target.shield) > 0.0


## ANTI-FARM COMPLETO (GDD §5 + docs/DANO.md §3.9). So' conta dano em MAGO
## INIMIGO. Fica de fora: dano proprio, dano em aliado e dano em alvo que NAO
## e' mago — muro, torreta, totem, casulo, bigorna, frasco, corvo. A checagem
## do ultimo caso e' ESTRUTURAL (so' pawn tem `shield_level`), entao nao existe
## lista de excecoes para manter atualizada quando o elenco crescer.
static func _creditar(source: Node, target: Node, dano: float) -> void:
	if source == null or not is_instance_valid(source) or source == target:
		return
	if not ("dmg_dealt" in source) or not ("shield_level" in target):
		return
	if _aliado(source, target):
		return
	source.dmg_dealt = float(source.dmg_dealt) + dano
	_evoluir(source)


## Time: hoje o unico esquadrao e' o do player (grupo "player") e os bots sao
## inimigos entre si e do player. Quando duplas/trios entrarem, e' AQUI que a
## checagem de esquadrao substitui o grupo — em UM lugar so'.
static func _aliado(a: Node, b: Node) -> bool:
	return a.is_in_group("player") and b.is_in_group("player")


## Escudo EVOLUTIVO (GDD §5): o dano causado sobe o nivel, o nivel sobe a
## capacidade. Subir de nivel entrega SO' a capacidade NOVA — o escudo nao
## regenera sozinho, entao quem estava com 3 de 50 fica com 3+25 de 75, nunca
## com 75 cheio (senao "subir de nivel" viraria cura gratis no meio da luta).
static func _evoluir(who: Node) -> void:
	var passos: Array = Balance.ESCUDO.evoluir
	var niveis: Array = Balance.ESCUDO.niveis
	var lv := int(who.shield_level)
	var novo := lv
	while novo < passos.size() and float(who.dmg_dealt) >= float(passos[novo]):
		novo += 1
	if novo == lv:
		return
	who.shield = float(who.shield) + float(niveis[novo - 1]) - float(niveis[lv - 1])
	who.shield_level = novo
	Bus.shield_changed.emit(who, float(who.shield), float(niveis[novo - 1]), novo)
