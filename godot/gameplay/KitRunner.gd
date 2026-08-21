## MOTOR DAS HABILIDADES (GDD §3 e §4) — o coracao que faltava. Ate aqui os
## 20 magos jogavam identico: mesmo tiro, mesma esquiva. O kit (passiva /
## tatica / suprema) vive neste no'.
##
## COMO ACOPLA — POR FORA. KitRunner e' um no' FILHO do pawn (composicao, o
## mesmo padrao do ArmaSlot da R21): Pawn.gd, Bot.gd, Combat.gd e Main.gd NAO
## foram tocados e nao sabem que ele existe. Tem kit quem tem este filho:
##     KitRunner.acoplar(pawn, "01-pyra")
##
## AS DUAS LEIS QUE ESTE ARQUIVO FAZ CUMPRIR (e o selftest prova):
##  1. ECONOMIA (§4.2): tatica e suprema pagam COOLDOWN. Nenhuma linha deste
##     arquivo — nem dos kits em habilidades/ — le ou escreve pawn.mana.
##  2. TELEGRAFIA (§4.3): usar_suprema() NAO executa o efeito, ela AGENDA.
##     Entre o toque e o efeito ha' a fase de aviso (som+visual), grampeada
##     na faixa Kits.TELEGRAFIA_MIN..MAX. "Se mata rapido, avisa antes."
##
## MOBILE: um _physics_process por PAWN (7 no total), nunca um por efeito de
## kit — os efeitos (muralha, fio, tear) tem tique proprio de 0,1s. Zero
## binario: todo VFX e' mesh procedural com material emissivo.
class_name KitRunner
extends Node

## O REGISTRO. Slug -> script do kit. O CONTRATO de um mago implementado sao
## 5 estaticos (os dois ultimos costumam ser "pass" — sao ganchos):
##   static func tick(k: KitRunner, delta: float) -> void    (passiva + estados)
##   static func tatica(k: KitRunner) -> void
##   static func suprema(k: KitRunner) -> void               (pos-telegrafia)
##   static func dano_recebido(k: KitRunner, quanto: float, el: String) -> void
##   static func estado_acabou(k: KitRunner, nome: String) -> void
## Sao 5 fixos de proposito: chamada direta e' mais barata e mais legivel que
## reflexao (GDScript nao responde has_method para estatico de script).
## Mago fora daqui e' DECLARADO em Kits e roda sem habilidade — de proposito.
const IMPL := {
	"01-pyra": preload("res://gameplay/habilidades/pyra.gd"),
	"03-veu": preload("res://gameplay/habilidades/veu.gd"),
	"10-tessa": preload("res://gameplay/habilidades/tessa.gd"),
}

var pawn: Pawn                      # o dono (Player ou Bot)
var slug := ""
var dados: Dictionary = Kits.PADRAO.duplicate(true)
# Untyped de proposito: as habilidades sao ESTATICAS do script do mago e a
# chamada precisa ser dinamica (o analisador estatico nao conhece os metodos
# declarados dentro de um GDScript carregado).
var impl = null                     # null = mago sem kit implementado
var tatica_cd := 0.0
var suprema_cd := 0.0
var telegrafia := 0.0               # > 0 = suprema avisada, ainda nao aconteceu
var silencio := 0.0                 # > 0 = "sem conjurar" (limitador da Veu)
var estados := {}                   # nome -> s restantes (braco_livre, espectral...)
var mem := {}                       # rascunho do kit (ancoras, escudo, acumuladores)
var _dash_frac := 0.0
var dash_iniciou := false           # true no frame em que a esquiva comecou
## Dois relogios GENERICOS que quase toda passiva pede (a Entrelinha da Veu
## pede os dois): ha' quanto tempo este pawn nao conjura e nao apanha.
## Comecam ZERADOS: quem acabou de cair na ilha nao esta' "quieto ha' um
## seculo" — a Entrelinha da Veu tem que ser conquistada tambem no spawn.
var desde_ataque := 0.0
var desde_dano := 0.0
var _intangivel := false


# ------------------------------------------------------------ acoplamento

## Poe (ou troca) o kit de um pawn. Idempotente: chamar de novo com outro slug
## reconfigura o mesmo no' em vez de empilhar kits.
static func acoplar(p: Node, mago: String) -> KitRunner:
	var k := de(p)
	if k == null:
		k = KitRunner.new()
		k.name = "KitRunner"
		p.add_child(k)
	k.configurar(mago)
	return k


static func de(p: Node) -> KitRunner:
	if p == null or not is_instance_valid(p):
		return null
	for c in p.get_children():
		if c is KitRunner:
			return c
	return null


## Fiacao defensiva para quem NAO tem kit: sem KitRunner, conjura sempre.
## E' o que mantem os 17 magos declarados jogaveis (so' sem habilidade).
static func pode_conjurar_de(p: Node) -> bool:
	var k := de(p)
	return true if k == null else k.pode_conjurar()


## Lentidao/buff em QUALQUER pawn, tenha kit ou nao (o fio da Tessa lentifica
## bot, que nasce sem KitRunner). Porta do Pawn, com o relogio dele.
static func afetar_velocidade(alvo: Node, mult: float, dur: float) -> void:
	if alvo != null and is_instance_valid(alvo) and alvo.has_method("lentificar"):
		alvo.lentificar(mult, dur)


## ESCUDO (GDD §5) — desde a R22 o escudo evolutivo e' do kernel (Pawn.shield,
## escrito pelo Combat). O Compasso Runico da Tessa REGENERA e o Tear-Mae TECE,
## e nem um nem outro e' dano: nao ha' porta no Combat para credito de escudo.
## Este e' o UNICO ponto do sistema de kits que escreve shield — PEDIDO AO
## COORDENADOR no relatorio: uma Combat.tecer_escudo(alvo, quanto, fonte)
## apagaria estas 4 linhas.
static func regenerar_escudo(alvo: Node, quanto: float) -> float:
	if alvo == null or not is_instance_valid(alvo) or not alvo.has_method("shield_max"):
		return 0.0
	alvo.shield = minf(float(alvo.shield) + quanto, float(alvo.shield_max()))
	Bus.shield_changed.emit(alvo, float(alvo.shield), float(alvo.shield_max()),
			int(alvo.shield_level))
	return float(alvo.shield)


func configurar(mago: String) -> void:
	slug = mago
	dados = Kits.de(mago)
	impl = IMPL.get(mago, null)
	tatica_cd = 0.0
	suprema_cd = 0.0
	telegrafia = 0.0
	estados.clear()
	mem.clear()
	if pawn != null and pawn.is_in_group("player"):
		# A HUD OBSERVA: qual mago entrou e se ele tem botao de tatica/suprema.
		Bus.kit_bound.emit(slug, impl != null)


func _ready() -> void:
	pawn = get_parent() as Pawn
	# A passiva da Veu quebra com QUALQUER dano: escutamos o sinal NOVO
	# (damage_applied, float e com fonte), nao o legado de inteiro — dano de 0,4
	# quebra a Entrelinha do mesmo jeito, e o legado so' emite quando fecha 1.
	Bus.damage_applied.connect(_on_damage)
	if slug != "" and pawn != null and pawn.is_in_group("player"):
		Bus.kit_bound.emit(slug, impl != null)


func _on_damage(alvo: Node, amount: float, element: String, _fonte: Node,
		_no_escudo: bool) -> void:
	if alvo != pawn:
		return
	desde_dano = 0.0
	if impl != null:
		impl.dano_recebido(self, amount, element)


## O Player avisa aqui quando um ATAQUE sai (Player._try_fire). E' o que
## quebra a Entrelinha da Veu — e serve para qualquer passiva futura de
## "ficou quieto por N segundos".
func notificar_ataque() -> void:
	desde_ataque = 0.0


static func avisar_ataque(p: Node) -> void:
	var k := de(p)
	if k != null:
		k.notificar_ataque()


# ------------------------------------------------------------------ tique

func _physics_process(delta: float) -> void:
	if pawn == null or not is_instance_valid(pawn) or float(pawn.hp) <= 0.0:
		return
	_tick_tempos(delta)
	_tick_dash()
	_tick_intangivel(delta)
	if impl != null:
		impl.tick(self, delta)


func _tick_tempos(delta: float) -> void:
	var tatica_antes := tatica_cd
	var suprema_antes := suprema_cd
	tatica_cd = maxf(tatica_cd - delta, 0.0)
	suprema_cd = maxf(suprema_cd - delta, 0.0)
	silencio = maxf(silencio - delta, 0.0)
	desde_ataque += delta
	desde_dano += delta
	if tatica_antes > 0.0 and tatica_cd <= 0.0:
		avisar_cd("tatica", 0.0)
	if suprema_antes > 0.0 and suprema_cd <= 0.0:
		avisar_cd("suprema", 0.0)
	for nome in estados.keys():
		var t := float(estados[nome]) - delta
		if t <= 0.0:
			estados.erase(nome)
			avisar_estado(nome, false)
			if impl != null:
				impl.estado_acabou(self, nome)
		else:
			estados[nome] = t
	## A TELEGRAFIA (§4.3): o efeito da suprema so' acontece DEPOIS do aviso.
	if telegrafia > 0.0:
		telegrafia = maxf(telegrafia - delta, 0.0)
		if telegrafia <= 0.0 and impl != null:
			impl.suprema(self)


## INTANGIBILIDADE (o Atravessar e a Mare da Veu). Mora AQUI, e nao no kit da
## Veu, porque a Mare leva ALIADOS junto — e aliado nao tem o kit dela. Sem
## mascara de colisao nao ha' chao: a altura fica travada na da entrada.
## Ponytail: e' o caminho que existe sem editar Pawn.gd (outra raia). Teto
## conhecido: se acabar exatamente dentro de rocha, o corpo e' empurrado no
## passo de fisica seguinte.
func intangivel(on: bool) -> void:
	if not is_instance_valid(pawn):
		return
	if on:
		mem["mascara"] = int(pawn.collision_mask)
		mem["altura"] = pawn.global_position.y
		pawn.collision_mask = 0
		_intangivel = true
	else:
		pawn.collision_mask = int(mem.get("mascara", 1))
		_intangivel = false


func _tick_intangivel(delta: float) -> void:
	if not _intangivel:
		return
	pawn.iframes_left = maxf(float(pawn.iframes_left), delta * 3.0)  # invulneravel
	pawn.velocity.y = 0.0
	pawn.global_position.y = float(mem.get("altura", pawn.global_position.y))


## Detecta a esquiva SEM tocar em Pawn.gd: dodge_cd_frac() salta para 1.0 no
## frame do dash (API publica que a HUD ja' le). Pyra usa para deixar fogo.
func _tick_dash() -> void:
	var f := 0.0
	if pawn.has_method("dodge_cd_frac"):
		f = float(pawn.dodge_cd_frac())
	dash_iniciou = f > _dash_frac + 0.001
	_dash_frac = f


# -------------------------------------------------------------- as 3 portas

## Conjurar (ATAQUE incluso) esta' liberado? O limitador da Veu ("1s sem
## conjurar" ao sair do Atravessar, e a Mare inteira) mora aqui.
func pode_conjurar() -> bool:
	return silencio <= 0.0 and is_instance_valid(pawn) and float(pawn.hp) > 0.0


func pronto_tatica() -> bool:
	return impl != null and tatica_cd <= 0.0 and pode_conjurar()


func pronto_suprema() -> bool:
	return impl != null and suprema_cd <= 0.0 and telegrafia <= 0.0 and pode_conjurar()


## TATICA — paga COOLDOWN, nunca mana (GDD §4.2).
func usar_tatica() -> bool:
	if not pronto_tatica():
		return false
	tatica_cd = float(dados.tatica_cd)
	avisar_cd("tatica", tatica_cd)
	impl.tatica(self)
	return true


## SUPREMA — paga COOLDOWN e, antes do efeito, AVISA (GDD §4.3). Esta funcao
## nunca executa o efeito: quem executa e' _tick_tempos quando o aviso acaba.
func usar_suprema() -> bool:
	if not pronto_suprema():
		return false
	suprema_cd = float(dados.suprema_cd)
	telegrafia = clampf(float(dados.telegrafia), Kits.TELEGRAFIA_MIN, Kits.TELEGRAFIA_MAX)
	avisar_cd("suprema", suprema_cd)
	# GDD §4.3: "se mata rapido, avisa antes" — vale para TODOS, inclusive bot.
	# Restringir ao player tornava a suprema inimiga muda, matando a contrajogada.
	Bus.kit_telegraph.emit(slug, "suprema", telegrafia, pawn.global_position)
	return true


# ------------------------------------------------------- servicos dos kits

## Onde os efeitos NASCEM: a Arena. Efeito filho da arena morre no restart
## junto com ela (estado que atravessa partida ja' vazou 3x no projeto).
func arena() -> Node:
	if pawn != null and is_instance_valid(pawn) and pawn.get_parent() != null:
		return pawn.get_parent()
	return self


## Direcao de mira. Player entrega a da CAMERA (kit_aim_dir); quem nao tem
## entrega o proprio corpo — bot e teste headless funcionam igual.
func mira() -> Vector3:
	if pawn.has_method("kit_aim_dir"):
		var d: Vector3 = pawn.kit_aim_dir()
		if d.length_squared() > 0.0001:
			return d.normalized()
	var f := -pawn.global_transform.basis.z
	f.y = 0.0
	return f.normalized() if f.length_squared() > 0.0001 else Vector3.FORWARD


func pos() -> Vector3:
	return pawn.global_position


## Velocidade: o kit NAO escreve status_mult. Quem e' dono do produto unico e'
## o Pawn, e ele ja' tem a porta com relogio proprio (lentificar substitui, nao
## soma — docs/DANO.md §3.4). Buff e' lentidao com fator > 1.
func buff_velocidade(mult: float, dur: float) -> void:
	if is_instance_valid(pawn):
		pawn.lentificar(mult, dur)


func silenciar(s: float) -> void:
	silencio = maxf(silencio, s)
	avisar_estado("silencio", true)


## Soma tempo ao cooldown da tatica JA' em curso (o braco molhado da Pyra).
func acrescentar_cd_tatica(s: float) -> void:
	tatica_cd += s
	avisar_cd("tatica", tatica_cd)


func ligar_estado(nome: String, dur: float) -> void:
	estados[nome] = dur
	avisar_estado(nome, true)


func estado_ativo(nome: String) -> bool:
	return estados.has(nome)


## IMUNIDADE E ESCUDO SEM TOCAR NO PONTO UNICO DE DANO: Combat.deal ja' tirou
## o hp (ele e' de outra raia e nao tem gancho de prevencao). Aqui o kit
## DEVOLVE, no mesmo tique de fisica, exatamente o que a regra dele nega —
## KitRunner e' filho do pawn, entao processa depois do pai, no mesmo frame.
## PEDIDO AO COORDENADOR no relatorio: um "dano_mult"/"imune_a" no Pawn
## resolveria isto na ORIGEM e este metodo sumiria.
func devolver_dano(quanto: float) -> void:
	if quanto <= 0.0 or not is_instance_valid(pawn):
		return
	var maximo := float(Balance.PLAYER.hp)
	pawn.hp = minf(float(pawn.hp) + quanto, maximo)
	if pawn.is_in_group("player"):
		Bus.health_changed.emit(float(pawn.hp), maximo)


# --------------------------------------------------------- leitura da HUD

## 0..1 do cooldown (1 = acabou de usar, 0 = pronto) — mesma convencao do
## dodge_cd_frac que a HUD ja' desenha.
func frac_tatica() -> float:
	return tatica_cd / maxf(float(dados.tatica_cd), 0.001)


func frac_suprema() -> float:
	return suprema_cd / maxf(float(dados.suprema_cd), 0.001)


## Sinais SO' do player: 6 bots emitindo cooldown viraria enxurrada no Bus.
## Emitimos na BORDA (usou / ficou pronto), nunca por frame — a HUD interpola.
func avisar_cd(tipo: String, restante: float) -> void:
	if pawn != null and pawn.is_in_group("player"):
		var total := float(dados.tatica_cd if tipo == "tatica" else dados.suprema_cd)
		Bus.kit_cooldown.emit(tipo, restante, total)


func avisar_estado(nome: String, ligado: bool) -> void:
	if pawn != null and pawn.is_in_group("player"):
		Bus.kit_state.emit(nome, ligado)


# ------------------------------------------------------ utilitarios comuns

## Pawns vivos perto de um ponto. Varre os filhos da arena (7 nos): mais
## barato e mais testavel que Area3D com sinal de fisica, e roda em headless.
static func alvos_perto(origem: Node, ponto: Vector3, raio: float, excluir: Node = null) -> Array:
	var out: Array = []
	if origem == null or not is_instance_valid(origem):
		return out
	var r2 := raio * raio
	for c in origem.get_children():
		if c == excluir or not (c is Node3D) or not ("hp" in c):
			continue
		if float(c.hp) <= 0.0:
			continue
		if (c as Node3D).global_position.distance_squared_to(ponto) <= r2:
			out.append(c)
	return out


## Distancia de um ponto ao SEGMENTO a-b (o fio da Tessa e a muralha da Pyra
## sao segmentos, nao esferas). Pura: o selftest prova ela direto.
static func dist_segmento(p: Vector3, a: Vector3, b: Vector3) -> float:
	var ab := b - a
	var len2 := ab.length_squared()
	if len2 < 0.0001:
		return p.distance_to(a)
	var t := clampf((p - a).dot(ab) / len2, 0.0, 1.0)
	return p.distance_to(a + ab * t)


## Material emissivo procedural — zero binario em todo VFX de kit.
static func mat_brilho(cor: Color, forca := 2.0) -> StandardMaterial3D:
	var m := StandardMaterial3D.new()
	m.albedo_color = cor
	m.emission_enabled = true
	m.emission = cor
	m.emission_energy_multiplier = forca
	m.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	m.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	return m
