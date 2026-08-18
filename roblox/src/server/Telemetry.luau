--!strict
-- ============================================================================
-- ARKANA (Roblox) — TELEMETRIA (raia 6.1: Analista de Dados / Engenheiro de
-- Telemetria). Coletor central: recebe eventos de Combat/Sintonia/Terrain/Match,
-- enriquece com o CONTEXTO PADRÃO de Shared/Events e agrega para leitura do
-- Diretor no fim da partida.
--
-- POR QUE ISTO EXISTE (auditoria externa de 18/08/2026):
--   · "o maior buraco do projeto não é documental: não existe comportamento de
--      jogador externo para analisar";
--   · "sem `is_bot`, todos os primeiros dashboards serão enganadores".
-- Logo: TODO número deste relatório sai em DUAS colunas — `human_only` e
-- `all_entities`. Nenhuma conclusão de balanceamento pode ser tirada da coluna
-- que mistura bot com gente.
--
-- INVARIANTE (não negociável): telemetria NUNCA altera o jogo.
--   · nada aqui escreve estado, dano, timing ou física;
--   · o observador de dano roda dentro de pcall — o Combat itera os listeners
--     SEM proteção, e um erro nosso abortaria o applyDamage no meio;
--   · sorteio usa `Random.new()` PRÓPRIO: chamar `math.random()` global
--     avançaria o mesmo fluxo que o Terrain usa para propagar fogo — telemetria
--     mudando propagação de incêndio é exatamente o que não pode acontecer.
--
-- MEMÓRIA: não existe log cru de eventos. Tudo é agregado O(1) + amostras com
-- teto (reservatório) — partida de 3 h ocupa o mesmo que partida de 3 min.
--
-- CONTRATO DE EMISSÃO (o que as outras raias mandam hoje — o resto é descoberto
-- aqui). Sujeito = de QUEM o evento fala; o payload traz só o específico:
--   Damage        atacante (ou a VÍTIMA quando `environmental`)
--                 { amount, absorbed, target=<Humanoid>, environmental }
--   Knock         vítima   { by=<matador>, amount }   ← o golpe que DERRUBOU
--   Elimination   vítima   { by, place, alive, zone_phase } ← saiu da partida
--   DodgeStart    jogador  { pos, dir, iframes, cooldown }
--   Sintonia*     jogador  { combo_key, with_user_id, element, with_element,
--                            by_pact (channel_start), latency (accept) }
--   TerrainStateChange  nil (mundo)  { state, reason, cells, seeds, spread,
--                                      fuel_left, fuel_exhausted, cx, cy }
--   MatchStart/MatchEnd { match_id, ... }
--
-- KNOCK ≠ ELIMINATION, e este relatório NUNCA soma os dois: queda é o golpe,
-- eliminação é a saída da partida (com colocação). Somar faria a curva de vivos
-- e a taxa de abandono mentirem.
--
-- Apelidos aceitos de propósito (amount|dmg, target|victim, by|from,
-- nullified|iframe): campo ausente = valor neutro, NUNCA erro. Um coletor que
-- estoura com payload torto vira ruído no meio do playtest.
--
-- COMO O DIRETOR LÊ (Command Bar do Studio, contexto SERVIDOR):
--   require(game.ServerScriptService.Server.Telemetry).report()  -- string
--   require(game.ServerScriptService.Server.Playtest).report()   -- imprime
-- ============================================================================

local Players = game:GetService("Players")
local RunService = game:GetService("RunService")
local ReplicatedStorage = game:GetService("ReplicatedStorage")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Events = require(Shared:WaitForChild("Events"))
local Balance = require(Shared:WaitForChild("Balance"))
local Grid = require(Shared:WaitForChild("Grid"))

local Telemetry = {}

-- --- tetos e cadências (custo/memória — não são balanceamento) --------------
--- amostras de TTK por coorte. Acima disto entra reservatório (amostragem
--- uniforme, sem viés de fim de partida) em vez de simplesmente descartar.
local TTK_CAP = 512
--- pontos da curva `alive_count × tempo`
local CURVE_CAP = 240
--- cadência da amostragem passiva (curva de vivos + varredura de queimadas)
local SAMPLE_INTERVAL = 5.0
--- validade do cache de "quantos estão vivos" — sem ele, cada evento varreria
--- a lista de jogadores; com ele, o custo por evento é uma leitura de campo.
local ALIVE_TTL = 0.5
--- teto do mapa de "primeiro dano sofrido" (chaves são strings, não Instances:
--- telemetria não segura referência de Humanoid destruído)
local FIRSTHIT_CAP = 512

--- Traço ao vivo: imprime cada evento já enriquecido. O Diretor liga com
--- `Playtest.trace(true)` quando quer VER a instrumentação disparando.
Telemetry.VERBOSE = false

-- ------------------------------------------------------------------- estado
type Agg = {
	events: { [string]: number },
	dmgByElement: { [string]: number },
	killsByElement: { [string]: number },
	dmgTotal: number,
	absorbed: number,
	kills: number,
	--- QUEDA e ELIMINAÇÃO são contadas separadas de propósito (ver cabeçalho)
	knocks: number,
	eliminations: number,
	dodges: number,
	nullified: number,
	--- canalizações que nasceram de um PACTO de ping (coordenação intencional)
	--- contra as que foram coincidência de tiro
	pactChannels: number,
	acceptLatSum: number,
	acceptLatN: number,
	terrainByActor: { [string]: number },
	terrainTotal: number,
	ttk: { number },
	ttkSeen: number,
}

local function newAgg(): Agg
	return {
		events = {},
		dmgByElement = {},
		killsByElement = {},
		dmgTotal = 0,
		absorbed = 0,
		kills = 0,
		knocks = 0,
		eliminations = 0,
		dodges = 0,
		nullified = 0,
		pactChannels = 0,
		acceptLatSum = 0,
		acceptLatN = 0,
		terrainByActor = {},
		terrainTotal = 0,
		ttk = {},
		ttkSeen = 0,
	}
end

local human: Agg = newAgg()
local all: Agg = newAgg()
--- Coortes pré-montadas: humano entra nas DUAS, bot só na total. Sem closure e
--- sem alocação por evento.
local HUMAN_COHORTS: { Agg } = { all, human }
local BOT_COHORTS: { Agg } = { all }

local terrain: any = nil
local combat: any = nil
local matchMod: any = nil
local teams: any = nil
local bots: any = nil
--- algum evento chegou com plataforma do cliente? (checklist de pendencias)
local sawDeviceTier = false

local matchId: string? = nil
local matchStartedAt = 0
local matchEndedAt = 0
local running = false
--- true depois de um matchEnd: impede que evento residual reabra a janela e
--- apague o relatório da partida que acabou (ver Telemetry.emit).
local ended = false

--- instante do primeiro dano da partida (tempo até o primeiro combate)
local firstDamageAt: number? = nil
--- primeiro dano SOFRIDO por sujeito, chaveado por string (base do TTK)
local firstHitAt: { [string]: number } = {}
local firstHitCount = 0
--- eliminados que a telemetria viu (o Match não expõe estado; isto é o que
--- permite contar vivos sem depender dele)
local eliminatedKeys: { [string]: boolean } = {}

--- curva de vivos: { t, humanos, total }
local curve: { { number } } = {}
local curveTruncated = false

--- células que JÁ queimaram nesta partida (varredura da grade — não depende de
--- ninguém emitir nada) e quantas eram inflamáveis no começo
local burnedCells: { [number]: boolean } = {}
local burnedCount = 0
local flammableAtStart = 0

--- INCÊNDIOS FECHADOS (Terrain emite uma vez por FRENTE, com o tamanho dela).
--- Fora das coortes de propósito: fogo é evento do mundo, não tem is_bot.
local fireFronts = 0
local fireCells = 0
local fireCellsMax = 0
local fireExhausted = 0

--- de onde veio o dado de dano/abate (o relatório precisa dizer isto na cara:
--- número sem procedência é pior que número faltando)
local damageSource = "nenhuma"
local externalDamage = false
local externalKnock = false

local sampleAcc = 0
local internalFailures = 0
local heartbeatConn: RBXScriptConnection? = nil
--- RNG PRÓPRIO — ver INVARIANTE no topo
local rng = Random.new()
local aliveCache = { at = -1, humans = 0, total = 0 }

-- ------------------------------------------------------------------ helpers
--- Percentil por interpolação linear. `nearest-rank` seria mais curto e mentiria
--- com n pequeno (com 3 amostras p10 e p50 dariam a MESMA coisa) — e é
--- justamente com poucas amostras que o primeiro playtest vai operar.
local function percentile(sorted: { number }, p: number): number
	local n = #sorted
	if n == 0 then
		return 0
	end
	if n == 1 then
		return sorted[1]
	end
	local rank = (n - 1) * p + 1
	local lo = math.floor(rank)
	local hi = math.ceil(rank)
	if lo == hi then
		return sorted[lo]
	end
	return sorted[lo] + (sorted[hi] - sorted[lo]) * (rank - lo)
end

--- Amostragem em reservatório: com o teto cheio, cada amostra nova ainda tem
--- chance justa de entrar. Descartar direto enviesaria o TTK para o INÍCIO da
--- partida — e o fim de partida é onde o TTK muda (zona apertando).
local function pushSample(agg: Agg, value: number)
	agg.ttkSeen += 1
	local list = agg.ttk
	if #list < TTK_CAP then
		table.insert(list, value)
		return
	end
	local j = rng:NextInteger(1, agg.ttkSeen)
	if j <= TTK_CAP then
		list[j] = value
	end
end

local function cohortsFor(isBot: boolean): { Agg }
	return if isBot then BOT_COHORTS else HUMAN_COHORTS
end

--- Player por trás de qualquer coisa que a raia A possa passar como sujeito.
--- Um nível só de indireção — nada de recursão para não travar com `{player=self}`.
local function asPlayer(v: any): Player?
	local t = typeof(v)
	if t == "Instance" then
		local inst = v :: Instance
		if inst:IsA("Player") then
			return inst :: Player
		end
		if inst:IsA("Humanoid") then
			local char = inst.Parent
			return if char then Players:GetPlayerFromCharacter(char) else nil
		end
		if inst:IsA("Model") then
			return Players:GetPlayerFromCharacter(inst)
		end
		return nil
	elseif t == "number" then
		local ok, p = pcall(function()
			return Players:GetPlayerByUserId(v :: number)
		end)
		return if ok then p else nil
	elseif t == "table" then
		local inner = (v :: any).player
		if typeof(inner) == "Instance" and (inner :: Instance):IsA("Player") then
			return inner :: Player
		end
	end
	return nil
end

--- Identidade + `is_bot` do sujeito. REGRA: é humano quem resolve para um
--- Player; TODO o resto é bot. Sujeito ausente = "world" (evento do mundo,
--- conta nas duas colunas — não existe "mundo humano" nem "mundo bot").
--- Devolve: chave estável, is_bot, Player (se humano) e o MEMBRO de time —
--- `Teams.teamOf` aceita Player OU Humanoid de bot, e a própria Instance é o id.
local function resolve(subject: any): (string, boolean, Player?, Instance?)
	local player = asPlayer(subject)
	if player then
		return Events.userHash(player.UserId), false, player, player
	end
	if subject == nil then
		return "world", false, nil, nil
	end
	local t = typeof(subject)
	if t == "table" then
		local name = (subject :: any).name
		if typeof(name) == "string" then
			return "bot:" .. name, true, nil, nil
		end
	elseif t == "Instance" then
		local inst = subject :: Instance
		local model = if inst:IsA("Humanoid") then inst.Parent else inst
		return "bot:" .. (if model then model.Name else inst.Name), true, nil, inst
	elseif t == "string" then
		return "bot:" .. (subject :: string), true, nil, nil
	end
	return "bot:?", true, nil, nil
end

local function elementOf(player: Player?, override: any): string
	if typeof(override) == "string" then
		return override :: string
	end
	if player and combat and combat.getElement then
		local ok, el = pcall(combat.getElement, player)
		if ok and typeof(el) == "string" then
			return el :: string
		end
	end
	return "?"
end

--- `member` é Player ou o Humanoid do bot (Teams.Member). Sem times montados
--- (lobby) `teamOf` devolve nil e o campo vai neutro — não é erro.
local function teamOf(member: Instance?): number
	if not member or not teams or not teams.teamOf then
		return 0
	end
	local ok, id = pcall(teams.teamOf, member)
	return if ok and typeof(id) == "number" then id :: number else 0
end

local function rootPos(player: Player?): Vector3?
	if not player then
		return nil
	end
	local char = player.Character
	local root = if char then char:FindFirstChild("HumanoidRootPart") else nil
	return if root and root:IsA("BasePart") then root.Position else nil
end

--- Vivos AGORA sem depender do Match (que não expõe estado): humanos com vida
--- que a telemetria ainda não viu morrer + `Bots.aliveCount()`.
local function aliveNow(): (number, number)
	local now = os.clock()
	if now - aliveCache.at < ALIVE_TTL then
		return aliveCache.humans, aliveCache.total
	end
	local humans = 0
	for _, player in ipairs(Players:GetPlayers()) do
		local char = player.Character
		local hum = if char then char:FindFirstChildOfClass("Humanoid") else nil
		if hum and hum.Health > 0 and not eliminatedKeys[Events.userHash(player.UserId)] then
			humans += 1
		end
	end
	local botsAlive = 0
	if bots and bots.aliveCount then
		local ok, n = pcall(bots.aliveCount)
		botsAlive = if ok and typeof(n) == "number" then n :: number else 0
	end
	aliveCache.at = now
	aliveCache.humans = humans
	aliveCache.total = humans + botsAlive
	return humans, humans + botsAlive
end

--- Fase da zona ESTIMADA a partir do relógio da partida e de Balance.match.zone.
--- O Match não expõe `zone.phase` (ver PENDÊNCIAS no relatório); enquanto não
--- expuser, isto é uma aproximação honesta — e quem emitir `zone_phase` no
--- payload sobrescreve.
local function zonePhase(): number
	if not running then
		return 0
	end
	local Z = Balance.match.zone
	local per = Z.holdTime + Z.shrinkTime
	if per <= 0 then
		return 0
	end
	local elapsed = os.clock() - matchStartedAt
	return math.clamp(math.floor(elapsed / per) + 1, 1, Z.phases)
end

-- ------------------------------------------------------- contexto padrão
--- Preenche os 11 campos de `Events.CONTEXT_FIELDS`. Quem emite manda só o
--- específico; tudo abaixo é descoberto aqui.
local function buildContext(name: string, subject: any, payload: any): { [string]: any }
	local key, isBot, player, member = resolve(subject)
	local p: any = if typeof(payload) == "table" then payload else {}
	local pos = if typeof(p.pos) == "Vector3" then p.pos else rootPos(player)
	if typeof(p.device_tier) == "string" then
		sawDeviceTier = true
	end
	local _, aliveTotal = aliveNow()
	return {
		event = name,
		match_id = matchId or "sem_partida",
		user_id_hash = key,
		is_bot = isBot,
		team_id = if typeof(p.team_id) == "number" then p.team_id else teamOf(member),
		device_tier = if typeof(p.device_tier) == "string" then p.device_tier else "unknown",
		timestamp = os.clock(),
		element = elementOf(player, p.element),
		position_bucket = if pos then Events.positionBucket(pos) else "?",
		shield_tier = if typeof(p.shield_tier) == "number" then p.shield_tier else 0,
		alive_count = if typeof(p.alive_count) == "number" then p.alive_count else aliveTotal,
		zone_phase = if typeof(p.zone_phase) == "number" then p.zone_phase else zonePhase(),
	}
end

-- ------------------------------------------------------------- agregação
local N = Events.Name

--- Fecha o relógio do TTK da vítima. A amostra é consumida UMA vez: se o
--- `knock` chegar, a `elimination` do mesmo corpo não conta de novo.
local function takeTtk(victimKey: string, now: number, cohorts: { Agg })
	local started = firstHitAt[victimKey]
	if not started then
		return
	end
	for _, g in ipairs(cohorts) do
		pushSample(g, now - started)
	end
	firstHitAt[victimKey] = nil
	firstHitCount = math.max(0, firstHitCount - 1)
end

--- Núcleo do coletor. Recebe já resolvido (por isso o autoteste consegue provar
--- a separação human_only/all_entities sem fabricar Player nenhum).
local function record(name: string, isBot: boolean, ctx: { [string]: any }, payload: any)
	local p: any = if typeof(payload) == "table" then payload else {}
	local cohorts = cohortsFor(isBot)
	local now = ctx.timestamp or os.clock()

	for _, g in ipairs(cohorts) do
		g.events[name] = (g.events[name] or 0) + 1
	end

	if name == N.Damage then
		local amount = tonumber(p.amount or p.dmg) or 0
		local absorbed = tonumber(p.absorbed) or 0
		local nullified = p.nullified == true or p.iframe == true
		--- Dano SEM ator (zona, terreno, tiro de bot) chega com o sujeito sendo a
		--- VÍTIMA — logo `ctx.element` ali é o elemento de quem APANHOU. Jogar
		--- isso na dominância inverteria a leitura; vai para o balde `ambiente`.
		local element = if p.environmental == true then "ambiente" else (ctx.element or "?")
		for _, g in ipairs(cohorts) do
			if nullified then
				g.nullified += 1
			else
				g.dmgTotal += amount
				g.absorbed += absorbed
				g.dmgByElement[element] = (g.dmgByElement[element] or 0) + amount
			end
		end
		if not nullified and amount > 0 then
			if firstDamageAt == nil then
				firstDamageAt = now
			end
			--- relógio do TTK: começa no PRIMEIRO dano sofrido pela vítima
			local victim = p.target or p.victim
			local victimKey = if victim ~= nil then (resolve(victim)) else nil
			if victimKey and firstHitAt[victimKey] == nil then
				if firstHitCount >= FIRSTHIT_CAP then
					table.clear(firstHitAt)
					firstHitCount = 0
				end
				firstHitAt[victimKey] = now
				firstHitCount += 1
			end
		end
	elseif name == N.Knock then
		--- QUEDA — o golpe que derrubou. É AQUI que o TTK fecha: a queixa
		--- "morri instantaneamente" é sobre o tempo até cair, não sobre a
		--- colocação final. Eliminação é outro evento e outra contagem.
		for _, g in ipairs(cohorts) do
			g.knocks += 1
		end
		takeTtk(ctx.user_id_hash, now, cohorts)
	elseif name == N.Elimination then
		--- DUAS coortes diferentes no mesmo evento, de propósito:
		---   · a ELIMINAÇÃO é da vítima (quem saiu da partida);
		---   · o ABATE é do matador (dominância de elemento é dele).
		for _, g in ipairs(cohorts) do
			g.eliminations += 1
		end
		local killer = asPlayer(p.by or p.from)
		local killerIsBot = (p.by ~= nil or p.from ~= nil) and killer == nil
		local killElement = elementOf(killer, p.by_element)
		for _, g in ipairs(cohortsFor(killerIsBot)) do
			g.kills += 1
			g.killsByElement[killElement] = (g.killsByElement[killElement] or 0) + 1
		end
		--- rede: se ninguém emitiu `knock`, o TTK ainda fecha aqui (a amostra é
		--- consumida uma vez só — quem chegar primeiro leva).
		takeTtk(ctx.user_id_hash, now, cohorts)
		eliminatedKeys[ctx.user_id_hash] = true
		aliveCache.at = -1
	elseif name == N.DodgeStart then
		for _, g in ipairs(cohorts) do
			g.dodges += 1
		end
	elseif name == N.SintoniaChannelStart then
		if p.by_pact == true then
			for _, g in ipairs(cohorts) do
				g.pactChannels += 1
			end
		end
	elseif name == N.SintoniaAccept then
		local latency = tonumber(p.latency)
		if latency then
			for _, g in ipairs(cohorts) do
				g.acceptLatSum += latency
				g.acceptLatN += 1
			end
		end
	elseif name == N.TerrainStateChange or name == N.TerrainTacticalOutcome then
		local actor = ctx.user_id_hash
		for _, g in ipairs(cohorts) do
			g.terrainTotal += 1
			g.terrainByActor[actor] = (g.terrainByActor[actor] or 0) + 1
		end
		--- frente de incêndio FECHADA: o Terrain reporta o tamanho dela de uma
		--- vez. É a medida direta de carbonização (GDD §16.1 / lição do 2D).
		local cells = tonumber(p.cells)
		if cells then
			fireFronts += 1
			fireCells += cells
			fireCellsMax = math.max(fireCellsMax, cells)
			if p.fuel_exhausted == true then
				fireExhausted += 1
			end
		end
	end

	if Telemetry.VERBOSE then
		print(string.format(
			"[tel] %-24s bot=%-5s el=%-9s time=%d zona=%d vivos=%d bucket=%s",
			name,
			tostring(ctx.is_bot),
			tostring(ctx.element),
			ctx.team_id or 0,
			ctx.zone_phase or 0,
			ctx.alive_count or 0,
			tostring(ctx.position_bucket)
		))
	end
end

-- ------------------------------------------------------- varredura do mundo
--- Quantas células já pegaram fogo. NÃO depende de emissor nenhum — lê o mundo
--- direto e responde sozinha a pergunta V3 ("a floresta carboniza?").
--- `Burned` é permanente e `Burning` vira `Burned`, então a varredura periódica
--- só perde o que a água apagou entre duas amostras.
--- ponytail: TETO CONHECIDO — 3600 células a cada 5 s. Se o mapa crescer,
--- o upgrade é o Terrain expor um contador de ignições, não varrer mais rápido.
local function scanTerrain(first: boolean)
	if not terrain or not terrain.getCell then
		return
	end
	local flammable = 0
	for cy = 0, Grid.ROWS - 1 do
		for cx = 0, Grid.COLS - 1 do
			local cell = terrain.getCell(cx, cy)
			if cell then
				if first then
					local mat = cell.material
					local burns = if mat == "Grass" then Balance.terrain.grassBurns else Grid.FLAMMABLE[mat] == true
					if burns then
						flammable += 1
					end
				end
				if cell.state == "Burning" or cell.state == "Burned" then
					local idx = cy * Grid.COLS + cx
					if not burnedCells[idx] then
						burnedCells[idx] = true
						burnedCount += 1
					end
				end
			end
		end
	end
	if first then
		flammableAtStart = flammable
	end
end

local function sample()
	if not running then
		return
	end
	local humans, total = aliveNow()
	if #curve < CURVE_CAP then
		table.insert(curve, { os.clock() - matchStartedAt, humans, total })
	else
		curveTruncated = true
	end
	scanTerrain(false)
end

-- ------------------------------------------------------------------- API
--- Ponto único de entrada. `payload` traz só os campos específicos do evento;
--- o contexto padrão é preenchido aqui.
function Telemetry.emit(name: string, subject: any, payload: any?)
	if typeof(name) ~= "string" then
		return
	end
	--- partida implícita: se ninguém chamou matchStart, a janela abre no
	--- primeiro evento. Dado que chega e some é pior que dado sem rótulo.
	if not running then
		if name == N.MatchEnd then
			return
		end
		-- NÃO REABRIR APÓS O FIM (achado do harness headless, 18/08): depois do
		-- `matchEnd` ainda chegam eventos residuais — fogo do terreno queimando,
		-- dano de zona no frame seguinte. Abrir uma "partida implícita" com eles
		-- ZERAVA os agregados da partida que acabou, e `Playtest.report()` — o
		-- fluxo documentado para o Diretor — devolvia relatório vazio logo
		-- depois de jogar. O relatório da partida encerrada é preservado; o
		-- resíduo é descartado.
		if ended then
			return
		end
		local id = if typeof(payload) == "table" then (payload :: any).match_id else nil
		Telemetry.matchStart(if typeof(id) == "string" then id else "implicita")
	end
	if name == N.Damage then
		externalDamage = true
		damageSource = "eventos `damage` da raia A"
	elseif name == N.Knock then
		externalKnock = true
	end
	local ctx = buildContext(name, subject, payload)
	record(name, ctx.is_bot, ctx, payload)
	if name == N.MatchEnd then
		Telemetry.matchEnd()
	end
end

--- Observador de dano do Combat (REDE DE SEGURANÇA). O Combat já emite
--- `damage`/`knock`; isto só entra em cena se aquele caminho morrer (o emit lá
--- é pcall — falha em silêncio). Sem uma rede, um playtest inteiro pode voltar
--- sem TTK nenhum, que é justamente o que a auditoria não aceita mais.
--- Desliga sozinho no instante em que o evento explícito aparece.
local function onDamageHook(target: Humanoid, dealt: number, from: Player?)
	if not running or externalDamage then
		return
	end
	if damageSource == "nenhuma" then
		damageSource = "hook Combat.onDamage (rede — o `damage` explícito não veio)"
	end
	--- mesma forma do evento real: sem ator, o sujeito é quem SOFRE e o dano vai
	--- para o balde `ambiente` (senão viraria dominância do elemento da vítima).
	local payload = { target = target, amount = dealt, environmental = from == nil }
	local ctx = buildContext(N.Damage, from or target, payload)
	record(N.Damage, ctx.is_bot, ctx, payload)
	if target.Health <= 0 and not externalKnock then
		local vctx = buildContext(N.Knock, target, { by = from })
		record(N.Knock, vctx.is_bot, vctx, { by = from })
	end
end

--- Chamado uma vez no boot com os módulos que ela observa.
function Telemetry.init(terrainModule: any, combatModule: any, matchModule: any)
	terrain = terrainModule
	combat = combatModule
	matchMod = matchModule

	--- Requires protegidos (mesmo padrão do Combat com Sintonia/Avatar): estes
	--- módulos são de OUTRAS raias e podem estar em obra. Faltando qualquer um,
	--- o campo correspondente vira neutro e o relatório diz que faltou.
	local folder = script.Parent
	if folder then
		local okTeams, t = pcall(function()
			return require(folder:FindFirstChild("Teams") :: ModuleScript)
		end)
		teams = if okTeams then t else nil
		local okBots, b = pcall(function()
			return require(folder:FindFirstChild("Bots") :: ModuleScript)
		end)
		bots = if okBots then b else nil
	end

	if combat and combat.onDamage then
		--- INVARIANTE: o Combat itera os observadores SEM pcall. Um erro aqui
		--- abortaria o applyDamage entre o dano e o número na tela. Esta é a
		--- fronteira — com o caminho oficial vivo, custa uma leitura de boolean.
		pcall(combat.onDamage, function(target: Humanoid, dealt: number, from: Player?)
			if externalDamage then
				return -- o caminho oficial está vivo: a rede nem aloca
			end
			--- ADIADO DE PROPÓSITO: o Combat chama os observadores ANTES do
			--- próprio `emit(damage)`. Sem adiar um passo, o PRIMEIRO dano da
			--- partida seria contado duas vezes (uma pela rede, outra pelo
			--- evento). Adiando, a rede só age quando o evento não veio.
			task.defer(function()
				if not pcall(onDamageHook, target, dealt, from) then
					internalFailures += 1
				end
			end)
		end)
	end

	--- O loop mestre do Main não chama Telemetry.step (e o Main não é desta
	--- raia). A amostragem passiva se conecta sozinha — é a única forma de ter
	--- curva de vivos e varredura de queimadas sem tocar em arquivo alheio.
	if heartbeatConn then
		heartbeatConn:Disconnect()
	end
	heartbeatConn = RunService.Heartbeat:Connect(function(dt)
		sampleAcc += dt
		if sampleAcc < SAMPLE_INTERVAL then
			return
		end
		sampleAcc = 0
		if not pcall(sample) then
			internalFailures += 1
		end
	end)
end

--- Abre a janela de coleta (zera TUDO: partida nova, dado novo).
function Telemetry.matchStart(id: string?)
	human = newAgg()
	all = newAgg()
	HUMAN_COHORTS = { all, human }
	BOT_COHORTS = { all }
	table.clear(firstHitAt)
	table.clear(eliminatedKeys)
	table.clear(burnedCells)
	table.clear(curve)
	firstHitCount = 0
	burnedCount = 0
	fireFronts, fireCells, fireCellsMax, fireExhausted = 0, 0, 0, 0
	flammableAtStart = 0
	curveTruncated = false
	firstDamageAt = nil
	externalDamage = false
	externalKnock = false
	damageSource = "nenhuma"
	internalFailures = 0
	sampleAcc = 0
	aliveCache.at = -1
	matchId = if typeof(id) == "string" and id ~= "" then id else string.format("m_%x", math.floor(os.clock() * 1000))
	matchStartedAt = os.clock()
	matchEndedAt = 0
	running = true
	ended = false
	pcall(scanTerrain, true)
	print(string.format("[Telemetry] coleta ABERTA — partida %s (variante Sintonia %s)", matchId, tostring(Balance.experiment.sintoniaVariant)))
end

--- Fecha a janela e imprime o relatório (é o momento em que o Diretor lê).
function Telemetry.matchEnd()
	if not running then
		return
	end
	pcall(scanTerrain, false)
	matchEndedAt = os.clock()
	running = false
	ended = true
	print(Telemetry.report())
end

-- -------------------------------------------------------------- relatório
local function pct(num: number, den: number): string
	if den <= 0 then
		return "-"
	end
	return string.format("%.0f%%", num / den * 100)
end

local function row(out: { string }, label: string, h: string, a: string)
	table.insert(out, string.format("%-38s %14s %14s", label, h, a))
end

local function countRow(out: { string }, label: string, get: (Agg) -> number)
	row(out, label, string.format("%d", get(human)), string.format("%d", get(all)))
end

local function convRow(out: { string }, label: string, num: (Agg) -> number, den: (Agg) -> number)
	row(out, label, pct(num(human), den(human)), pct(num(all), den(all)))
end

local function ev(name: string): (Agg) -> number
	return function(g: Agg): number
		return g.events[name] or 0
	end
end

local function ttkSorted(g: Agg): { number }
	local copy = table.clone(g.ttk)
	table.sort(copy)
	return copy
end

local function secs(v: number): string
	return string.format("%.2fs", v)
end

--- Relatório LEGÍVEL na saída do servidor, em duas colunas.
function Telemetry.report(): string
	local out: { string } = {}
	local dur = (if matchEndedAt > 0 then matchEndedAt else os.clock()) - matchStartedAt
	if matchStartedAt == 0 then
		dur = 0
	end
	--- taxa por minuto só faz sentido com partida de verdade: com 0,3 s de
	--- duração ela divide por quase zero e imprime número de mentira.
	local function perMin(v: number): string
		return if dur >= 5 then string.format("%.1f", v / (dur / 60)) else "-"
	end

	local line = string.rep("=", 68)
	table.insert(out, "")
	table.insert(out, line)
	table.insert(out, "ARKANA - RELATORIO DE TELEMETRIA")
	table.insert(out, line)
	table.insert(out, string.format("partida .......... %s%s", matchId or "nenhuma coletada ainda", if running then "  (EM ANDAMENTO)" else ""))
	table.insert(out, string.format("duracao .......... %dm%02ds", math.floor(dur / 60), math.floor(dur % 60)))
	table.insert(out, string.format("variante Sintonia  %s  (channel %.2fs / cd %.1fs / x%.2f)", tostring(Balance.experiment.sintoniaVariant), Balance.sintonia.channel, Balance.sintonia.sharedCooldown, Balance.sintonia.dmgMult))
	table.insert(out, string.format("fonte do dano .... %s", damageSource))
	table.insert(out, "colunas .......... human_only = evento cujo SUJEITO e humano")
	table.insert(out, "                   all_entities = humanos + bots (NAO use para balancear)")
	table.insert(out, "")
	row(out, "metrica", "human_only", "all_entities")

	-- ---------------------------------------------------------------- funil
	table.insert(out, "-- FUNIL DA SINTONIA (pergunta V1: duplas coordenam?) ----------------")
	table.insert(out, "   (oportunidade e' contada na BORDA: uma por situacao aberta,")
	table.insert(out, "    nao uma por frame — nao normalize por tempo)")
	countRow(out, "oportunidades", ev(N.SintoniaOpportunity))
	countRow(out, "pings", ev(N.SintoniaPing))
	convRow(out, "  conversao oportunidade -> ping", ev(N.SintoniaPing), ev(N.SintoniaOpportunity))
	countRow(out, "aceites", ev(N.SintoniaAccept))
	convRow(out, "  conversao ping -> aceite", ev(N.SintoniaAccept), ev(N.SintoniaPing))
	row(out, "  latencia media do aceite", if human.acceptLatN > 0 then secs(human.acceptLatSum / human.acceptLatN) else "-", if all.acceptLatN > 0 then secs(all.acceptLatSum / all.acceptLatN) else "-")
	countRow(out, "canalizacoes", ev(N.SintoniaChannelStart))
	convRow(out, "  conversao aceite -> canalizacao", ev(N.SintoniaChannelStart), ev(N.SintoniaAccept))
	-- A PERGUNTA V1 EM UMA LINHA: canalizacao que nasceu de PACTO (ping aceito)
	-- e coordenacao INTENCIONAL; o resto foi coincidencia de tiro.
	countRow(out, "  ... por PACTO (ping aceito)", function(g)
		return g.pactChannels
	end)
	countRow(out, "  ... por coincidencia de tiro", function(g)
		return math.max(0, (g.events[N.SintoniaChannelStart] or 0) - g.pactChannels)
	end)
	convRow(out, "  INTENCIONALIDADE (pacto / canaliz.)", function(g)
		return g.pactChannels
	end, ev(N.SintoniaChannelStart))
	countRow(out, "interrompidas", ev(N.SintoniaInterrupted))
	convRow(out, "  taxa de interrupcao", ev(N.SintoniaInterrupted), ev(N.SintoniaChannelStart))
	countRow(out, "fundidas", ev(N.SintoniaFired))
	convRow(out, "  conversao canalizacao -> fusao", ev(N.SintoniaFired), ev(N.SintoniaChannelStart))
	convRow(out, "  CONVERSAO TOTAL (oport. -> fusao)", ev(N.SintoniaFired), ev(N.SintoniaOpportunity))

	-- ------------------------------------------------------------------ TTK
	table.insert(out, "-- TEMPO ATE A QUEDA (TTK: 1o dano sofrido -> knock) ------------------")
	local hs, as = ttkSorted(human), ttkSorted(all)
	row(out, "amostras (vistas / guardadas)", string.format("%d / %d", human.ttkSeen, #hs), string.format("%d / %d", all.ttkSeen, #as))
	row(out, "p10  (a queixa 'morri instantaneo')", secs(percentile(hs, 0.10)), secs(percentile(as, 0.10)))
	row(out, "p50  (mediana)", secs(percentile(hs, 0.50)), secs(percentile(as, 0.50)))
	row(out, "p90", secs(percentile(hs, 0.90)), secs(percentile(as, 0.90)))
	table.insert(out, "   (media global NAO entra aqui de proposito: ela esconde o p10)")
	countRow(out, "quedas (knock)", function(g)
		return g.knocks
	end)
	countRow(out, "eliminacoes (saiu da partida)", function(g)
		return g.eliminations
	end)
	countRow(out, "quedas SEM eliminacao", function(g)
		return math.max(0, g.knocks - g.eliminations)
	end)
	table.insert(out, "   (knock e eliminacao NUNCA sao somados: um e' o golpe, o outro e' a saida)")

	-- ------------------------------------------------------- dano / elemento
	table.insert(out, "-- DANO POR ELEMENTO (dominancia) ------------------------------------")
	local order = { "Fire", "Water", "Earth", "Wind", "Lightning", "ambiente", "?" }
	for _, el in ipairs(order) do
		local dh, da = human.dmgByElement[el] or 0, all.dmgByElement[el] or 0
		if dh > 0 or da > 0 then
			row(out, el, string.format("%d (%s)", dh, pct(dh, human.dmgTotal)), string.format("%d (%s)", da, pct(da, all.dmgTotal)))
		end
	end
	countRow(out, "dano total", function(g)
		return math.floor(g.dmgTotal)
	end)
	convRow(out, "  absorvido pelo Escudo (GDD 5)", function(g)
		return g.absorbed
	end, function(g)
		return g.dmgTotal
	end)
	table.insert(out, "   ('ambiente' = zona, terreno e tiro de bot: chegam sem autor)")
	table.insert(out, "-- ABATES POR ELEMENTO -----------------------------------------------")
	for _, el in ipairs(order) do
		local kh, ka = human.killsByElement[el] or 0, all.killsByElement[el] or 0
		if kh > 0 or ka > 0 then
			row(out, el, string.format("%d (%s)", kh, pct(kh, human.kills)), string.format("%d (%s)", ka, pct(ka, all.kills)))
		end
	end
	countRow(out, "abates totais", function(g)
		return g.kills
	end)

	-- -------------------------------------------------------------- esquiva
	table.insert(out, "-- ESQUIVA (i-frames 0.18s / cd 2.6s) --------------------------------")
	countRow(out, "esquivas", function(g)
		return g.dodges
	end)
	row(out, "esquivas por minuto", perMin(human.dodges), perMin(all.dodges))
	countRow(out, "projeteis anulados por i-frame", function(g)
		return g.nullified
	end)
	convRow(out, "  % dos eventos de dano anulados", function(g)
		return g.nullified
	end, ev(N.Damage))

	-- -------------------------------------------------------------- terreno
	table.insert(out, "-- TERRENO (pergunta V3: o mapa vira carvao?) ------------------------")
	countRow(out, "interacoes de terreno", function(g)
		return g.terrainTotal
	end)
	local function actors(g: Agg): number
		local n = 0
		for _ in pairs(g.terrainByActor) do
			n += 1
		end
		return n
	end
	row(out, "interacoes por AUTOR/partida", string.format("%.1f", human.terrainTotal / math.max(actors(human), 1)), string.format("%.1f", all.terrainTotal / math.max(actors(all), 1)))
	table.insert(out, string.format("frentes de incendio fechadas ... %d", fireFronts))
	table.insert(out, string.format("celulas por incendio ........... media %.1f / maior %d", if fireFronts > 0 then fireCells / fireFronts else 0, fireCellsMax))
	table.insert(out, string.format("frentes travadas por combustivel %d (%s) — se alto, `fuelBudget` esta apertado", fireExhausted, pct(fireExhausted, fireFronts)))
	table.insert(out, string.format("celulas que pegaram fogo ....... %d de %d inflamaveis (%s da cobertura)", burnedCount, flammableAtStart, pct(burnedCount, flammableAtStart)))
	table.insert(out, "   (a linha acima e' varredura DIRETA da grade: nao depende de emissor")
	table.insert(out, "    nenhum e serve de conferencia contra os eventos de incendio)")

	-- ---------------------------------------------------------------- ritmo
	table.insert(out, "-- RITMO DA PARTIDA --------------------------------------------------")
	table.insert(out, string.format("tempo ate o primeiro combate ... %s", if firstDamageAt then secs(firstDamageAt - matchStartedAt) else "nunca houve combate"))
	table.insert(out, string.format("curva alive_count x tempo (%d amostras%s):", #curve, if curveTruncated then ", TRUNCADA" else ""))
	local stride = math.max(1, math.ceil(#curve / 16))
	for i = 1, #curve, stride do
		local c = curve[i]
		table.insert(out, string.format("   t=%5.0fs   humanos %2d   total %2d", c[1], c[2], c[3]))
	end
	if #curve == 0 then
		table.insert(out, "   (sem amostras — a partida nao rodou tempo suficiente)")
	end

	-- ----------------------------------------------------------- pendências
	table.insert(out, "-- PENDENCIAS DE INSTRUMENTACAO (o que ainda nao da para medir) ------")
	local missing = 0
	local function needs(cond: boolean, text: string)
		if cond then
			missing += 1
			table.insert(out, "   [ ] " .. text)
		end
	end
	needs((all.events[N.SintoniaOpportunity] or 0) == 0, "`sintonia_opportunity` (denominador do funil) nao chegou nesta partida")
	needs((all.events[N.SintoniaPing] or 0) == 0, "`sintonia_ping` / `sintonia_accept` / `channel_start` / `interrupted` / `fired`")
	needs((all.events[N.DodgeStart] or 0) == 0, "`dodge_start` no Combat.handleDodge")
	needs(all.nullified == 0, "i-frame que ANULA projetil nao e' emitido: Combat.applyDamage retorna antes do emit — pedir `damage` com `nullified=true` (e' a validacao do corte 0.28s -> 0.18s)")
	needs((all.events[N.TerrainStateChange] or 0) == 0, "`terrain_state_change` (frente de incendio) nao chegou")
	needs((all.dmgByElement.ambiente or 0) > 0, "dano de BOT chega como `environmental` (Bots passa fromPlayer=nil): nao da para separar bot de zona/terreno nem atribuir elemento")
	needs(all.terrainByActor.world ~= nil, "`terrain_state_change` vem com sujeito `nil` (mundo): interacao de terreno POR JOGADOR nao tem como ser atribuida")
	needs(not externalDamage, "`damage` explicito nao chegou — os numeros sairam do hook Combat.onDamage")
	needs(teams == nil or teams.teamOf == nil, "Teams indisponivel — `team_id` sai 0 em todo evento")
	needs(combat == nil or combat.getShieldLevel == nil, "Combat nao expoe nivel de escudo — `shield_tier` sai 0 (pedir Combat.getShieldLevel)")
	needs(matchMod == nil or matchMod.getState == nil, "Match nao expoe fase da zona — `zone_phase` e ESTIMADO por relogio (pedir Match.getState)")
	needs(not sawDeviceTier, "`device_tier` exige o cliente reportar plataforma — sai 'unknown'")
	if missing == 0 then
		table.insert(out, "   (nenhuma — instrumentacao completa)")
	end
	if internalFailures > 0 then
		table.insert(out, string.format("   !! %d falhas internas engolidas por pcall (o jogo nao sentiu; o dado sim)", internalFailures))
	end
	table.insert(out, line)
	table.insert(out, "")
	return table.concat(out, "\n")
end

-- ============================================================================
-- AUTOTESTE — cole no Command Bar do Studio (contexto SERVIDOR):
--   require(game.ServerScriptService.Server.Telemetry).selfTest()
-- Prova o que nao da para ver a olho e o que, errado, faria o primeiro
-- playtest mentir: os percentis (p10 e' a metrica da auditoria) e a separacao
-- human_only / all_entities.
-- ============================================================================
function Telemetry.selfTest()
	-- 1) PERCENTIL — amostra conhecida 1..10, interpolacao linear
	local s = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
	local function near(a: number, b: number): boolean
		return math.abs(a - b) < 1e-9
	end
	assert(near(percentile(s, 0.50), 5.5), "p50 de 1..10 tem de ser 5.5")
	assert(near(percentile(s, 0.10), 1.9), "p10 de 1..10 tem de ser 1.9")
	assert(near(percentile(s, 0.90), 9.1), "p90 de 1..10 tem de ser 9.1")
	assert(percentile({}, 0.5) == 0, "amostra vazia nao pode estourar")
	assert(percentile({ 7 }, 0.1) == 7, "amostra unica: todo percentil e' ela")
	-- p10 < p50: o motivo de a auditoria proibir media global
	local skew = { 0.4, 0.5, 0.6, 9, 9, 9, 9, 9, 9, 9 }
	assert(percentile(skew, 0.10) < percentile(skew, 0.50), "p10 precisa enxergar a cauda curta")

	-- 2) RESERVATORIO — com o teto cheio, ainda amostra (e nao vaza)
	local g = newAgg()
	for i = 1, TTK_CAP * 3 do
		pushSample(g, i)
	end
	assert(#g.ttk == TTK_CAP, "reservatorio tem de respeitar o teto")
	assert(g.ttkSeen == TTK_CAP * 3, "o teto nao pode apagar a CONTAGEM de amostras vistas")

	-- 3) SEPARACAO human_only / all_entities — o coracao do relatorio
	local saveH, saveA, saveHC, saveBC = human, all, HUMAN_COHORTS, BOT_COHORTS
	local saveMatch, saveRunning = matchId, running
	local saveFirst, saveCount, saveHits = firstDamageAt, firstHitCount, table.clone(firstHitAt)
	human, all = newAgg(), newAgg()
	HUMAN_COHORTS, BOT_COHORTS = { all, human }, { all }
	local function fake(name: string, isBot: boolean, element: string, payload: any)
		local ctx = { event = name, is_bot = isBot, element = element, user_id_hash = if isBot then "bot:t" else "u_t", timestamp = 100, team_id = 0, zone_phase = 1, alive_count = 2, position_bucket = "0,0" }
		record(name, isBot, ctx, payload)
	end
	fake(N.Damage, false, "Fire", { amount = 30, target = "vitima" })
	fake(N.Damage, true, "Water", { amount = 70, target = "vitima" })
	assert(human.dmgTotal == 30, "human_only nao pode enxergar dano de bot")
	assert(all.dmgTotal == 100, "all_entities tem de somar os dois")
	assert(human.dmgByElement.Water == nil, "elemento de bot vazou para human_only")
	-- dano sem autor: o elemento do CONTEXTO e' o da vitima, nao pode virar dominancia
	fake(N.Damage, false, "Earth", { amount = 5, environmental = true })
	assert(human.dmgByElement.Earth == nil, "dano de ambiente nao pode ser creditado a elemento")
	assert(human.dmgByElement.ambiente == 5, "dano sem autor tem de cair no balde `ambiente`")
	fake(N.SintoniaPing, false, "Fire", {})
	fake(N.SintoniaPing, true, "Fire", {})
	assert((human.events[N.SintoniaPing] or 0) == 1 and (all.events[N.SintoniaPing] or 0) == 2, "funil tem de separar bot de gente")

	-- 4) TTK ponta a ponta: 1o dano -> KNOCK, na coorte da VITIMA. E a
	--    eliminacao que vem depois NAO pode contar a mesma morte de novo.
	table.clear(firstHitAt)
	local victimKey = "u_v"
	firstHitAt[victimKey] = 100
	local function vctx(name: string, t: number)
		return { event = name, is_bot = false, element = "Water", user_id_hash = victimKey, timestamp = t, team_id = 0, zone_phase = 1, alive_count = 1, position_bucket = "0,0" }
	end
	record(N.Knock, false, vctx(N.Knock, 103.5), { amount = 12 })
	assert(#human.ttk == 1 and near(human.ttk[1], 3.5), "TTK = intervalo entre o primeiro dano e a QUEDA")
	assert(human.knocks == 1 and human.eliminations == 0, "knock nao pode contar como eliminacao")
	record(N.Elimination, false, vctx(N.Elimination, 104), { by_element = "Lightning", place = 3 })
	assert(#human.ttk == 1, "a eliminacao nao pode amostrar o TTK que o knock ja fechou")
	assert(human.eliminations == 1 and human.knocks == 1, "queda e eliminacao sao contagens SEPARADAS")
	assert((human.killsByElement.Lightning or 0) == 1, "o abate e' creditado ao elemento do MATADOR, nao da vitima")
	assert(human.killsByElement.Water == nil, "elemento da vitima nao pode virar abate")
	assert(eliminatedKeys[victimKey] == true, "a vitima precisa sair da contagem de vivos")
	eliminatedKeys[victimKey] = nil

	-- 4b) by_pact: coordenacao INTENCIONAL separada de coincidencia de tiro
	fake(N.SintoniaChannelStart, false, "Fire", { by_pact = true })
	fake(N.SintoniaChannelStart, false, "Fire", { by_pact = false })
	assert(human.pactChannels == 1, "so a canalizacao com pacto conta como intencional")
	assert((human.events[N.SintoniaChannelStart] or 0) == 2, "as duas canalizacoes contam no funil")

	-- 5) CONTRATO: os 11 campos de Events.CONTEXT_FIELDS existem no contexto
	local ctx = buildContext(N.SpellCast, nil, nil)
	for _, field in ipairs(Events.CONTEXT_FIELDS) do
		assert(ctx[field] ~= nil, "contexto padrao sem o campo `" .. field .. "`")
	end
	assert(ctx.is_bot == false and ctx.user_id_hash == "world", "evento sem sujeito e' do mundo")
	local _, isBot = resolve({ name = "Bot 3", humanoid = {} })
	assert(isBot, "quem nao resolve para Player e' bot — a regra que a auditoria exige")

	-- 6) o relatorio precisa SAIR (formatar string nao pode estourar com zeros)
	local text = Telemetry.report()
	assert(#text > 0 and string.find(text, "human_only", 1, true), "relatorio vazio ou sem as duas colunas")

	human, all, HUMAN_COHORTS, BOT_COHORTS = saveH, saveA, saveHC, saveBC
	matchId, running = saveMatch, saveRunning
	firstDamageAt, firstHitCount = saveFirst, saveCount
	table.clear(firstHitAt)
	for k, v in pairs(saveHits) do
		firstHitAt[k] = v
	end
	print("[Telemetry] autoteste OK — percentis, reservatorio, knock/eliminacao e separacao human/bot")
	return true
end

return Telemetry
