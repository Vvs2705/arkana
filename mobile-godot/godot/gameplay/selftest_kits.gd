## Selftest headless da raia HABILIDADES (os kits dos magos). Rodar:
##   Godot --headless --path godot --script res://gameplay/selftest_kits.gd
##
## PROVA AS DUAS LEIS DO GDD, uma por uma:
##   §4.2 ECONOMIA — tatica e suprema NAO tiram mana (a mana e' do ataque), e
##                   cooldown conta, bloqueia reuso e libera na hora certa;
##   §4.3 TELEGRAFIA — a suprema tem fase de aviso ANTES do efeito, e o piso
##                   de 1s e' grampeado no motor (dado ruim nao burla a lei).
## E PROVA CADA ⚖️ LIMITADOR DECLARADO dos 3 kits implementados: agua apaga a
## muralha da Pyra e molha o braco (+1s), vento a empurra 3m, o braco esfria
## depois da suprema; a Veu sai do Atravessar SEM CONJURAR e qualquer dano
## quebra a Entrelinha; o fio da Tessa cai com 1 golpe na ancora, conduz raio
## pelo terreno e o Tear-Mae nao engole curtissimo alcance.
##
## NOTA (mesma da selftest da raia GAMEPLAY): em modo --script os autoloads so'
## registram DEPOIS que este arquivo compila — por isso aqui NADA e'
## referenciado lexicalmente (Bus, Balance, Kits, KitRunner...): tudo chega via
## load()/get_node() no 1o frame. O tempo tambem nao e' esperado: os tiques de
## fisica sao chamados A MAO, o que deixa o teste deterministico.
extends SceneTree

const IMPLEMENTADOS := ["01-pyra", "03-veu", "10-tessa"]

var fails := 0
var _bus: Node
var _kits: GDScript
var _kr: GDScript
var _player: GDScript
var _bot: GDScript
var _combat: GDScript
var _proj: GDScript
var _cds: Array = []
var _teles: Array = []
var _states: Array = []
var _bounds: Array = []
var _thits: Array = []


func _init() -> void:
	process_frame.connect(_run, CONNECT_ONE_SHOT)


func _run() -> void:
	_bus = root.get_node("Bus")
	_kits = load("res://core/Kits.gd")
	_kr = load("res://gameplay/KitRunner.gd")
	_player = load("res://gameplay/Player.gd")
	_bot = load("res://gameplay/Bot.gd")
	_combat = load("res://gameplay/Combat.gd")
	_proj = load("res://gameplay/Projectile.gd")
	_bus.kit_cooldown.connect(func(t: String, r: float, tot: float) -> void:
		_cds.append([t, r, tot]))
	_bus.kit_telegraph.connect(func(s: String, t: String, d: float, _p := Vector3.ZERO) -> void:
		_teles.append([s, t, d]))
	_bus.kit_state.connect(func(n: String, on: bool) -> void: _states.append([n, on]))
	_bus.kit_bound.connect(func(s: String, impl: bool) -> void: _bounds.append([s, impl]))
	_bus.terrain_hit.connect(func(e: String, p: Vector3, s: bool) -> void:
		_thits.append([e, p, s]))
	_test_registro()
	_test_economia()
	_test_carga_no_ar()
	_test_telegrafia()
	_test_pyra()
	_test_veu()
	_test_tessa()
	_test_utilitarios()
	if fails == 0:
		print("SELFTEST OK")
	else:
		printerr("SELFTEST FALHOU: %d" % fails)
	quit(0 if fails == 0 else 1)


func _check(cond: bool, name: String) -> void:
	if cond:
		print("  ok    - " + name)
	else:
		fails += 1
		printerr("  FALHA - " + name)


# ------------------------------------------------------------------ apoio

## Monta uma arena com UM mago e devolve [arena, pawn, kit].
func _montar(slug: String) -> Array:
	var arena := Node3D.new()
	root.add_child(arena)
	var p: Node = _player.new()
	arena.add_child(p)
	p.set_physics_process(false)  # quem anda o relogio aqui e' o teste
	p.set_mage(slug)
	return [arena, p, _kr.de(p)]


func _inimigo(arena: Node3D, pos: Vector3) -> Node:
	var b: Node = _bot.new()
	arena.add_child(b)
	b.set_physics_process(false)
	b.global_position = pos
	return b


## Anda o relogio do KIT a mao (deterministico, sem esperar frame de fisica).
func _andar(k: Object, total: float, dt := 0.1) -> void:
	var n := int(ceil(total / dt))
	for i in n:
		k._physics_process(dt)


func _filhos_do_grupo(arena: Node3D, grupo: String) -> Array:
	var out: Array = []
	for c in arena.get_children():
		if c.is_in_group(grupo) and not c.is_queued_for_deletion():
			out.append(c)
	return out


## O nucleo visual de uma brasa da Pyra (headless nao renderiza: o teste
## consulta o ESTADO do no' — mesh, material, particulas — nunca o renderer).
func _nucleo_de(muro: Node) -> MeshInstance3D:
	for c in muro.get_children():
		if c is MeshInstance3D:
			return c
	return null


func _tem_estado(nome: String, ligado: bool) -> bool:
	for e in _states:
		if e[0] == nome and bool(e[1]) == ligado:
			return true
	return false


# ------------------------------------------------------------------ testes

func _test_registro() -> void:
	print("[Kits: o registro dos 20]")
	var elenco: GDScript = load("res://menu/Elenco.gd")
	_check(_kits.slugs().size() == 20, "20 magos declarados em Kits")
	var faltando: Array = []
	for m in elenco.MAGOS:
		if not _kits.MAGOS.has(str(m.slug)):
			faltando.append(str(m.slug))
	_check(faltando.is_empty(), "todo mago do Elenco tem entrada em Kits")
	var impl: Array = []
	for s in _kits.slugs():
		if _kits.implementado(str(s)):
			impl.append(str(s))
	impl.sort()
	_check(impl == IMPLEMENTADOS, "3 implementados; os outros 17 declarados e inertes")
	_check(_kr.IMPL.keys().size() == 3, "o registro de scripts bate com os implementados")
	for s in IMPLEMENTADOS:
		_check(_kr.IMPL.has(s), "kit registrado: " + s)
	# CONTRATO: todo mago responde a todos os campos, mesmo sem implementacao.
	var d: Dictionary = _kits.de("20-pip")
	var completo := true
	for campo in _kits.PADRAO:
		completo = completo and d.has(campo)
	_check(completo, "mago nao implementado ainda responde a ficha inteira")
	_check(not bool(d.implementado), "mago nao implementado marcado como tal")
	_check(_kits.de("nao-existe").has("tatica_cd"), "slug desconhecido cai no padrao (defensivo)")
	## §4.2 NO FORMATO: se um dia alguem escrever custo de mana num kit, o
	## registro passa a mentir sobre a lei. O teste varre os 20 atras disso.
	var suja: Array = []
	for s in _kits.slugs():
		for bloco in ["passiva", "tatica", "suprema"]:
			for campo in (_kits.de(str(s))[bloco] as Dictionary):
				var c := str(campo).to_lower()
				if c.contains("mana") or c.contains("custo"):
					suja.append("%s.%s.%s" % [s, bloco, campo])
	_check(suja.is_empty(), "§4.2: nenhum kit tem custo de MANA (so' cooldown)")
	## §4.3 NO FORMATO: telegrafia declarada dentro da faixa do GDD.
	for s in IMPLEMENTADOS:
		var t := float(_kits.de(s).telegrafia)
		_check(t >= _kits.TELEGRAFIA_MIN and t <= _kits.TELEGRAFIA_MAX,
				"§4.3: %s avisa entre 1s e 4s (%.1fs)" % [s, t])


func _test_economia() -> void:
	print("[§4.2: tatica e suprema pagam COOLDOWN, nunca mana]")
	var m := _montar("01-pyra")
	var arena: Node3D = m[0]
	var p: Node = m[1]
	var k: Object = m[2]
	_check(_bounds.size() > 0 and _bounds[-1][0] == "01-pyra" and bool(_bounds[-1][1]),
			"Bus.kit_bound anuncia o mago com kit para a HUD")
	var mana0: float = float(p.mana)
	_cds.clear()
	_check(k.pronto_tatica(), "tatica pronta no spawn")
	# A CARGA NO NASCIMENTO (26/08): tatica CHEIA, suprema VAZIA — e' a decisao
	# "todos caem sem nenhum poder alem das habilidades; a suprema comeca em 0%".
	# Aqui, ANTES de qualquer caminhada de tempo: 10s de teste ja' enchem 20%.
	_check(is_zero_approx(k.suprema_carga), "suprema COMECA em 0% — nada de cair carregado")
	_check(is_equal_approx(k.frac_suprema(), 1.0), "frac = 1.0 (falta tudo) a 0%")
	_check(k.usar_tatica(), "tatica dispara")
	_check(is_equal_approx(float(p.mana), mana0), "TATICA NAO consome mana")
	_check(not _cds.is_empty() and _cds[0][0] == "tatica" and _cds[0][1] > 0.0,
			"Bus.kit_cooldown avisa a HUD no uso")
	_check(not k.pronto_tatica(), "tatica entra em cooldown")
	_check(not k.usar_tatica(), "cooldown BLOQUEIA o reuso")
	_check(is_equal_approx(k.frac_tatica(), 1.0), "frac_tatica = 1.0 logo apos o uso")
	_andar(k, float(k.dados.tatica_cd) * 0.5)
	_check(k.frac_tatica() < 0.6 and k.frac_tatica() > 0.4, "o cooldown CONTA (metade na metade)")
	_check(not k.pronto_tatica(), "ainda bloqueada na metade do cooldown")
	_andar(k, float(k.dados.tatica_cd) * 0.5 + 0.2)
	_check(k.pronto_tatica(), "tatica volta quando o cooldown zera")
	_check(_cds.any(func(e: Array) -> bool: return e[0] == "tatica" and float(e[1]) <= 0.0),
			"Bus.kit_cooldown avisa a HUD quando fica pronta")
	# ---- A CARGA DA SUPREMA (26/08 — DIRECAO.md §4). As checagens de
	# nascimento estao la' em cima, antes das caminhadas de tempo da tatica —
	# aqui a barra ja' andou, entao zera-se para medir o canal do TEMPO limpo.
	k.suprema_carga = 0.0
	_check(not k.pronto_suprema(), "a 0%% a suprema NAO dispara")
	_check(not k.usar_suprema(), "usar a 0%% e' recusado")
	var carga_s: float = float(k.dados.suprema_carga)
	_andar(k, carga_s * 0.5)
	_check(k.suprema_carga > 0.45 and k.suprema_carga < 0.55,
			"o TEMPO enche: ~50%% na metade de %.0fs" % carga_s)
	# DANO CAUSADO acelera (modelo Apex): o pawn e' a FONTE, o alvo e' outro.
	var antes: float = k.suprema_carga
	var outro := _inimigo(arena, Vector3(5, 0, 0))
	_bus.damage_applied.emit(outro, 100.0, "fire", p, false)
	_check(k.suprema_carga > antes + 0.10,
			"100 de dano causado adianta a carga (CARGA_POR_DANO)")
	# dano RECEBIDO nao carrega — senao apanhar viraria bateria
	antes = k.suprema_carga
	_bus.damage_applied.emit(p, 50.0, "fire", outro, false)
	_check(is_equal_approx(k.suprema_carga, antes), "dano RECEBIDO nao enche a carga")
	k.suprema_carga = 1.0
	mana0 = float(p.mana)
	_cds.clear()
	_check(k.usar_suprema(), "a 100%% a suprema dispara")
	_check(is_equal_approx(float(p.mana), mana0), "SUPREMA NAO consome mana")
	_check(is_zero_approx(k.suprema_carga), "usar GASTA a carga inteira: volta a 0%%")
	_check(not k.usar_suprema(), "vazia de novo, bloqueada de novo")
	_check(not _cds.is_empty() and _cds[0][0] == "suprema"
			and is_equal_approx(float(_cds[0][2]), float(k.dados.suprema_carga)),
			"Bus.kit_cooldown leva os segundos de carga para a HUD")
	arena.queue_free()


## A CARGA NO AR (video do Diretor, 26/08): a barra da suprema andava assim que
## a tela abria — a contagem so' pode comecar DEPOIS do pouso. O gancho e' o da
## propria queda, `Queda.no_ar(pawn)`: ele procura um filho CHAMADO "Queda" e
## le' `fase`, entao um stub com so' esse campo reproduz o mago no ar sem
## levantar castelo nem ilha. A fiacao defensiva (pawn SEM no' "Queda" carrega
## normal) ja' e' provada por _test_economia inteiro — todo pawn de la' nasce
## sem queda e a barra enche.
func _test_carga_no_ar() -> void:
	print("[26/08: a carga da suprema NAO anda no ar — so' depois do pouso]")
	var m := _montar("01-pyra")
	var arena: Node3D = m[0]
	var p: Node = m[1]
	var k: Object = m[2]
	var stub := GDScript.new()
	stub.source_code = "extends Node\nvar fase := \"caindo\""
	stub.reload()
	var q: Node = stub.new()
	q.name = "Queda"
	p.add_child(q)
	_andar(k, 5.0)
	_check(is_zero_approx(k.suprema_carga), "no ar o TEMPO nao enche a carga")
	var outro := _inimigo(arena, Vector3(5, 0, 0))
	_bus.damage_applied.emit(outro, 100.0, "fire", p, false)
	_check(is_zero_approx(k.suprema_carga),
			"no ar nem DANO CAUSADO enche (o ponto unico barra os dois canais)")
	q.set("fase", "pousou")
	_andar(k, 1.0)
	_check(k.suprema_carga > 0.0, "pousou: a carga volta a andar na hora")
	arena.queue_free()


func _test_telegrafia() -> void:
	print("[§4.3: toda suprema AVISA antes de acontecer]")
	var m := _montar("01-pyra")
	var arena: Node3D = m[0]
	var p: Node = m[1]
	var k: Object = m[2]
	_teles.clear()
	var mana0: float = float(p.mana)
	k.suprema_carga = 1.0  # o teste e' sobre o AVISO; a carga ja' foi provada
	_check(k.usar_suprema(), "suprema aceita")
	_check(k.telegrafia > 0.0, "existe fase de telegrafia")
	_check(not k.estado_ativo("braco_livre"), "o EFEITO ainda nao aconteceu no toque")
	_check(_teles.size() == 1 and _teles[0][1] == "suprema"
			and is_equal_approx(float(_teles[0][2]), float(k.dados.telegrafia)),
			"Bus.kit_telegraph leva som+visual do aviso (%.1fs)" % float(k.dados.telegrafia))
	_andar(k, float(k.dados.telegrafia) - 0.2, 0.1)
	_check(not k.estado_ativo("braco_livre"), "durante o aviso NADA acontece")
	var projeteis_no_aviso := 0
	for c in arena.get_children():
		if c.get_script() == _proj:
			projeteis_no_aviso += 1
	_check(projeteis_no_aviso == 0, "o lanca-chamas nao cuspiu nada durante o aviso")
	_andar(k, 0.4, 0.1)
	_check(k.estado_ativo("braco_livre"), "o efeito comeca DEPOIS do aviso")
	_andar(k, 1.0, 0.1)
	var projeteis := 0
	for c in arena.get_children():
		if c.get_script() == _proj:
			projeteis += 1
	_check(projeteis >= int(k.dados.suprema.leque_tiros), "o leque continuo dispara em leque")
	_check(is_equal_approx(float(p.mana), mana0), "o leque da suprema tambem NAO custa mana")
	## ANTI-VACUIDADE: um kit com telegrafia 0 (dado ruim) nao consegue burlar
	## a lei — o motor grampeia no piso do GDD.
	var m2 := _montar("01-pyra")
	var k2: Object = m2[2]
	k2.dados["telegrafia"] = 0.0
	k2.suprema_carga = 1.0
	k2.usar_suprema()
	_check(k2.telegrafia >= float(_kits.TELEGRAFIA_MIN),
			"telegrafia 0 e' grampeada no piso de 1s (a lei nao depende do dado)")
	arena.queue_free()
	(m2[0] as Node).queue_free()


func _test_pyra() -> void:
	print("[Pyra: Muralha de Brasas, Braco Livre e os limitadores]")
	var m := _montar("01-pyra")
	var arena: Node3D = m[0]
	var p: Node = m[1]
	var k: Object = m[2]
	p.global_position = Vector3.ZERO
	var t: Dictionary = k.dados.tatica
	k.usar_tatica()
	var muros := _filhos_do_grupo(arena, "brasas_pyra")
	_check(muros.size() == 1, "a tatica ergue a muralha")
	var muro: Node = muros[0]
	_check(is_equal_approx(muro.a.distance_to(muro.b), float(t.comprimento)),
			"muralha de %.0fm (GDD)" % float(t.comprimento))
	## O VISUAL NOVO (video do Diretor, 26/08: a muralha lia como RETANGULO
	## CHAPADO laranja). Receita Spellbreak: forma de chama + emissivo em
	## gradiente + particulas + flicker em passos. Prova por ESTADO consultavel.
	var nucleo := _nucleo_de(muro)
	_check(nucleo != null and nucleo.mesh is PrismMesh,
			"nucleo em CUNHA (silhueta de chama) — nao mais caixa chapada")
	var vmat: StandardMaterial3D = nucleo.material_override if nucleo != null else null
	_check(vmat != null and vmat.emission_enabled, "nucleo EMISSIVO (fogo brilha sozinho)")
	_check(vmat != null and vmat.emission.is_equal_approx(_proj.tint("fire")),
			"emissivo na cor canonica do FOGO (paleta §10)")
	_check(vmat != null and vmat.albedo_texture is GradientTexture2D,
			"gradiente vertical: base avermelhada, topo amarelo — nao cor chapada")
	var chamas: GPUParticles3D = null
	var luz_dinamica := false
	for c in muro.get_children():
		if c is GPUParticles3D:
			chamas = c
		if c is Light3D:
			luz_dinamica = true
	_check(chamas != null and chamas.emitting, "UMA GPUParticles3D de chamas, emitindo")
	_check(chamas != null and chamas.amount <= 48, "mobile: no maximo 48 particulas por muralha")
	_check(not luz_dinamica, "ZERO luz dinamica na muralha (mobile ilumina com COR)")
	# flicker em PASSOS (12-15 fps do Spellbreak): meio passo do relogio nao
	# muda nada; o passo cheio TROCA a energia num degrau — nunca lerp continuo.
	muro._flicker_acc = 0.0
	var e0: float = vmat.emission_energy_multiplier if vmat != null else 0.0
	muro._flicker(0.04)
	_check(vmat != null and is_equal_approx(vmat.emission_energy_multiplier, e0),
			"flicker: MEIO passo do relogio nao muda a energia (sem lerp)")
	muro._flicker(0.05)
	_check(vmat != null and not is_equal_approx(vmat.emission_energy_multiplier, e0),
			"flicker: o passo cheio TROCA a energia em degrau")
	# dano moderado em quem atravessa — e NUNCA na dona (fogo dela nao a fere)
	var vitima := _inimigo(arena, muro.global_position)
	# Desde a R22 o dano bate no ESCUDO antes da vida: a conta e' vida+escudo.
	var ehp0: float = float(vitima.hp) + float(vitima.shield)
	var ehp_pyra: float = float(p.hp) + float(p.shield)
	p.dmg_dealt = 0.0
	p.global_position = muro.global_position  # a Pyra DENTRO da propria muralha
	muro._physics_process(0.2)
	_check(float(vitima.hp) + float(vitima.shield) < ehp0, "quem atravessa a muralha leva dano")
	_check(is_equal_approx(float(p.hp) + float(p.shield), ehp_pyra),
			"a muralha NAO fere a propria Pyra")
	_check(float(p.dmg_dealt) > 0.0,
			"o dano da tatica CREDITA a evolucao do escudo (fonte = a Pyra)")
	# passiva: dentro das proprias chamas ela ganha velocidade
	k._physics_process(0.05)
	_check(is_equal_approx(float(p.status_mult), float(k.dados.passiva.buff_vel)),
			"passiva: +10%% de velocidade atravessando chamas")
	# ⚖️ agua APAGA a muralha e MOLHA o braco: +1s de recarga
	var cd_antes: float = float(k.tatica_cd)
	_bus.terrain_hit.emit("water", muro.global_position, false)
	_check(muro.is_queued_for_deletion(), "⚖️ agua APAGA a muralha (§14)")
	_check(is_equal_approx(float(k.tatica_cd), cd_antes + float(t.molhado_cd_extra)),
			"⚖️ braco molhado: +1s na recarga da tatica")
	_check(_tem_estado("braco_molhado", true), "a HUD fica sabendo do braco molhado")
	# ⚖️ vento EMPURRA a muralha 3m
	k.tatica_cd = 0.0
	k.usar_tatica()
	var muro2: Node = _filhos_do_grupo(arena, "brasas_pyra")[0]
	# perf mobile: TODAS as muralhas dividem o MESMO material de nucleo
	var nucleo2 := _nucleo_de(muro2)
	_check(nucleo2 != null and nucleo2.material_override == vmat,
			"UM material de nucleo COMPARTILHADO entre muralhas (perf mobile)")
	var pos_antes: Vector3 = muro2.global_position
	_bus.terrain_hit.emit("wind", pos_antes + Vector3(0, 0, 2.0), false)
	_check(is_equal_approx(pos_antes.distance_to(muro2.global_position), float(t.vento_empurra)),
			"⚖️ vento empurra a muralha 3m (§14)")
	# ⚖️ ao acabar a suprema o braco ESFRIA: 4s sem tatica e -15% de velocidade
	var s: Dictionary = k.dados.suprema
	k.suprema_carga = 1.0  # carga CHEIA: o teste quer usar a suprema ja'
	k.usar_suprema()
	_andar(k, float(k.dados.telegrafia) + float(s.duracao) + 0.2)
	_check(not k.estado_ativo("braco_livre"), "a suprema tem duracao FIXA (6s)")
	# TOLERANCIA DE UM TIQUE (0.1) e nao de 0.01: a caminhada para' um tique
	# DEPOIS de o estado expirar, entao o esfriamento ja' contou 0.1s. Ate'
	# 26/08 a folga de 0.01 passava VACUAMENTE — a tatica_cd antiga de 14s
	# ainda estava correndo (sobrava 6.4s) e mascarava o esfria de 4s. Com a
	# tatica em 9s (DIRECAO.md) a sobra e' 1.4s, o maxf do esfria REALMENTE
	# levanta para 4s, e este check passou a provar o que sempre disse provar.
	_check(k.tatica_cd >= float(s.esfria_dur) - 0.15, "⚖️ braco frio: %.0fs sem tatica"
			% float(s.esfria_dur))
	_check(not k.pronto_tatica(), "⚖️ tatica realmente bloqueada com o braco frio")
	# O CHIP DESLIGA (video do Diretor, 26/08: BRACO FRIO preso na tela do
	# segundo 2 ao 55). Pelo relogio de estados ele expira e avisa a HUD.
	_check(k.estado_ativo("braco_frio"), "braco_frio esta' no RELOGIO de estados")
	_andar(k, float(s.esfria_dur) + 0.2)
	_check(not k.estado_ativo("braco_frio"), "braco_frio EXPIRA sozinho")
	_check(_tem_estado("braco_frio", false),
			"e a HUD recebe o DESLIGAMENTO — o chip nao fica preso na tela")
	k._physics_process(0.05)
	_check(is_equal_approx(float(p.status_mult), float(s.esfria_vel)),
			"⚖️ braco frio: -15%% de velocidade")
	# a imunidade da passiva DEVOLVE o dano de fogo do chao no mesmo tique
	p.hp = 50.0
	k.devolver_dano(6.0)
	_check(is_equal_approx(float(p.hp), 56.0), "passiva: o fogo do chao nao fica nela")
	arena.queue_free()


func _test_veu() -> void:
	print("[Veu: Entrelinha, Atravessar, Mare — e o preco de cada um]")
	var m := _montar("03-veu")
	var arena: Node3D = m[0]
	var p: Node = m[1]
	var k: Object = m[2]
	var t: Dictionary = k.dados.tatica
	_states.clear()
	# passiva: 4s quieta => desfocada
	k._physics_process(0.1)
	_check(not bool(k.mem.get("desfocada", false)), "recem-atacada nao desfoca")
	_andar(k, float(k.dados.passiva.quietude) + 0.2)
	_check(bool(k.mem.get("desfocada", false)), "Entrelinha: desfoca com 4s de quietude")
	_check(_tem_estado("desfocada", true), "a HUD fica sabendo da Entrelinha")
	# ⚖️ QUALQUER dano quebra a passiva
	_combat.deal(p, 5.0, "fire")
	_check(not bool(k.mem.get("desfocada", false)), "⚖️ qualquer dano QUEBRA a Entrelinha")
	_check(k.desde_dano <= 0.001, "o relogio de dano zerou")
	# tatica: 0,8s de ativacao ANTES do plano espectral
	var mana0: float = float(p.mana)
	var mascara0: int = int(p.collision_mask)
	_check(k.usar_tatica(), "Atravessar dispara")
	_check(is_equal_approx(float(p.mana), mana0), "Atravessar NAO consome mana")
	_check(k.estado_ativo("ativacao") and not k.estado_ativo("espectral"),
			"a tatica tambem avisa (0,8s de ativacao)")
	_andar(k, float(t.ativacao) + 0.05)
	_check(k.estado_ativo("espectral"), "entra no plano espectral depois da ativacao")
	_check(int(p.collision_mask) == 0, "intangivel: atravessa parede fina")
	_check(float(p.iframes_left) > 0.0, "invulneravel no plano espectral")
	_check(_filhos_do_grupo(arena, "eco_veu").size() == 1,
			"⚖️ o ECO fica visivel na entrada (o inimigo ve por onde ela sumiu)")
	_check(k.mem.has("mascara"), "a mascara original fica guardada para voltar")
	_andar(k, float(t.duracao) + 0.05)
	_check(int(p.collision_mask) == mascara0, "volta a colidir ao sair do plano")
	# ⚖️ sai 1s SEM CONJURAR — e isso vale ate para o ATAQUE
	_check(not k.pode_conjurar(), "⚖️ sai do Atravessar sem conjurar")
	var mana_antes: float = float(p.mana)
	p._try_fire()
	_check(is_equal_approx(float(p.mana), mana_antes), "⚖️ o ATAQUE tambem fica barrado")
	_check(not k.pronto_tatica(), "⚖️ e a tatica tambem, no silencio")
	_andar(k, float(t.silencio_saida) + 0.05)
	_check(k.pode_conjurar(), "o silencio tem prazo: 1s e volta")
	# suprema: a Mare silencia TODO MUNDO por 5s e toca o sino
	_states.clear()
	var s: Dictionary = k.dados.suprema
	k.suprema_carga = 1.0  # carga CHEIA: o teste quer usar a suprema ja'
	k.usar_suprema()
	_andar(k, float(k.dados.telegrafia) + 0.05)
	_check(k.estado_ativo("mare"), "a Mare comeca depois do aviso")
	_check(int(p.collision_mask) == 0, "na Mare o grupo fica intangivel")
	_check(not k.pode_conjurar(), "⚖️ na Mare NINGUEM conjura")
	_check(_tem_estado("sino_espectral", true), "⚖️ o SINO toca no mundo real (counter sonoro)")
	_andar(k, float(s.duracao) + 0.05)
	_check(_tem_estado("sino_espectral", false), "o sino para junto com a Mare")
	_check(int(p.collision_mask) == mascara0, "a Mare devolve a colisao no fim")
	arena.queue_free()


func _test_tessa() -> void:
	print("[Tessa: Compasso, Fio do Tear, Tear-Mae — e o preco de cada um]")
	var m := _montar("10-tessa")
	var arena: Node3D = m[0]
	var p: Node = m[1]
	var k: Object = m[2]
	p.global_position = Vector3.ZERO
	var t: Dictionary = k.dados.tatica
	var pas: Dictionary = k.dados.passiva
	## Compasso Runico: o escudo e' o do KERNEL (Pawn.shield, R22) — o kit so'
	## REGENERA. Aqui provamos a regeneracao, nao a absorcao (essa e' do Combat).
	p.shield = 10.0
	_andar(k, 1.0, 0.1)
	_check(is_equal_approx(float(p.shield), 10.0 + float(pas.escudo_regen)),
			"Compasso Runico: o marca-passo regenera %0.0f de escudo por segundo"
			% float(pas.escudo_regen))
	p.shield = float(p.shield_max())
	_andar(k, 2.0, 0.1)
	_check(is_equal_approx(float(p.shield), float(p.shield_max())),
			"a regeneracao respeita o teto do nivel de escudo")
	# ...e QUEBRAR o escudo e' que da' o arranco (+15% por 2s).
	_states.clear()
	p.shield = 0.0
	k._physics_process(0.05)
	_check(_tem_estado("escudo_quebrado", true), "a HUD sabe que o escudo quebrou")
	_check(is_equal_approx(float(p.status_mult), float(pas.quebra_buff_vel)),
			"+15%% de velocidade quando o escudo quebra")
	# tatica: o fio entre 2 ancoras, no maximo 6
	for i in int(t.max_fios) + 2:
		k.tatica_cd = 0.0
		k.silencio = 0.0
		k.usar_tatica()
	var fios := _filhos_do_grupo(arena, "fios_tessa")
	_check(fios.size() == int(t.max_fios), "⚖️ no maximo %d fios (o 7o apaga o 1o)"
			% int(t.max_fios))
	var fio: Node = fios[0]
	_check(is_equal_approx(fio.a.distance_to(fio.b), float(t.comprimento)),
			"o fio liga 2 pontos a %.0fm" % float(t.comprimento))
	# tocar o fio: dano + lentidao + raio publicado no terreno
	var vitima := _inimigo(arena, (fio.a + fio.b) * 0.5)
	var ehp: float = float(vitima.hp) + float(vitima.shield)
	p.dmg_dealt = 0.0
	_thits.clear()
	_states.clear()
	fio._physics_process(0.2)
	_check(float(vitima.hp) + float(vitima.shield) < ehp, "tocar o fio doi")
	_check(float(p.dmg_dealt) > 0.0, "o dano do fio CREDITA a evolucao do escudo dela")
	_check(_thits.any(func(e: Array) -> bool: return e[0] == "lightning"),
			"⚖️ agua conduz: o toque publica RAIO no terreno (§14), que nao tem time")
	_check(is_equal_approx(float(vitima.status_mult), float(t.lentidao)),
			"o fio deixa lento (pela porta do Pawn, sem duplicar status)")
	_check(_tem_estado("fio_zumbido", true), "⚖️ o fio ZUMBE a 5m (se denuncia)")
	# ⚖️ 1 golpe em qualquer ancora derruba o fio
	_bus.terrain_hit.emit("fire", fio.a, false)
	_check(fio.is_queued_for_deletion(), "⚖️ UM golpe na ancora derruba o fio")
	# suprema: o Tear-Mae absorve projetil inimigo, menos no curtissimo alcance
	k.suprema_carga = 1.0  # carga CHEIA: o teste quer usar a suprema ja'
	k.silencio = 0.0
	k.usar_suprema()
	_andar(k, float(k.dados.telegrafia) + 0.05)
	var tear: Variant = k.mem.get("tear")
	_check(tear != null and is_instance_valid(tear), "o Tear-Mae abre depois do aviso")
	var s: Dictionary = k.dados.suprema
	var atirador := _inimigo(arena, Vector3(20, 0, 0))
	var longe: Node = _proj.launch(arena, atirador, Vector3(4.0, 1.0, 0.0), Vector3.LEFT, "fire")
	var colado: Node = _proj.launch(arena, atirador, Vector3(1.0, 1.0, 0.0), Vector3.LEFT, "fire")
	longe.set_physics_process(false)
	colado.set_physics_process(false)
	p.shield = 0.0
	tear._physics_process(0.2)
	_check(longe.is_queued_for_deletion(), "o Tear engole projetil inimigo no alcance")
	_check(not colado.is_queued_for_deletion(),
			"⚖️ zona morta de %.0fm: nao engole curtissimo alcance" % float(s.zona_morta))
	_check(is_equal_approx(float(p.shield), float(s.escudo_por_projetil)),
			"o absorvido vira ESCUDO tecido (no escudo do kernel, nao num paralelo)")
	k.suprema_carga = 1.0  # carga CHEIA: o teste quer usar a suprema ja'
	k.usar_suprema()
	_andar(k, float(k.dados.telegrafia) + 0.05)
	_check(k.mem.get("tear") == tear, "⚖️ maximo 1 Tear-Mae por vez")
	arena.queue_free()


func _test_utilitarios() -> void:
	print("[motor: utilitarios puros e mago sem kit]")
	_check(is_equal_approx(_kr.dist_segmento(Vector3(0, 0, 2), Vector3(-5, 0, 0),
			Vector3(5, 0, 0)), 2.0), "dist_segmento: perpendicular no meio")
	_check(is_equal_approx(_kr.dist_segmento(Vector3(9, 0, 0), Vector3(-5, 0, 0),
			Vector3(5, 0, 0)), 4.0), "dist_segmento: fora da ponta mede da ponta")
	# um mago DECLARADO mas sem kit joga normal: sem tatica, sem suprema e
	# conjurando ataque como antes deste sistema existir.
	var m := _montar("09-vex")
	var k: Object = m[2]
	_check(not k.pronto_tatica() and not k.usar_tatica(), "mago sem kit nao tem tatica")
	_check(not k.usar_suprema(), "mago sem kit nao tem suprema")
	_check(k.pode_conjurar(), "mago sem kit ATACA normalmente")
	var avulso := Node.new()
	_check(_kr.pode_conjurar_de(avulso), "quem nao tem KitRunner conjura (defensivo)")
	avulso.free()
	(m[0] as Node).queue_free()
