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
--   TutorialStep  jogador  { step, index, completed, reason, elapsed }
--   SpellCast/TacticCast  jogador  { input = "drag"|"tap"|"key" }  ← régua §4
--   SessionStart/SessionEnd  jogador  { }   ← V5, ver abaixo
--
-- SESSÃO ≠ PARTIDA (pergunta V5, docs/ROBLOX.md §1). Ninguém mede "volta
-- amanhã" numa sessão; mede-se o que o alpha ENTREGA — quanto tempo ficou,
-- quantas provas jogou e EM QUE PONTO desistiu. Por isso o dado de sessão:
--   · NÃO passa por `record` nem pelos agregados de partida;
--   · NÃO é zerado por `matchStart`/`matchEnd` (é justamente o que a V5 precisa
--     que sobreviva — estado de PARTIDA continua morrendo no fim dela);
--   · NÃO tem coluna `all_entities`: bot não volta amanhã. É humano ou nada.
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
--- amostras de DURAÇÃO DE SESSÃO guardadas.
--- ponytail: TETO CONHECIDO — passado o teto, a duração nova é descartada e o
--- relatório imprime TRUNCADA. 512 sessões num mesmo servidor está muito além
--- de qualquer alpha; se um dia chegar lá, o upgrade é o mesmo reservatório do
--- TTK, não um teto maior.
local SESSION_CAP = 512
--- distintos passos de onboarding que o quadro aceita (o Tutorial tem ~6).
local TUTORIAL_CAP = 16
--- AMOSTRA MÍNIMA para chamar uma razão de resposta. Abaixo de 10, UMA
--- observação a mais move a porcentagem em mais de 10 pontos — isso não é
--- conclusão, é ruído. "n=3, não conclui" vale mais que um número que finge.
local MIN_SAMPLE = 10

--- Traço ao vivo: imprime cada evento já enriquecido. O Diretor liga com
--- `Playtest.trace(true)` quando quer VER a instrumentação disparando.
Telemetry.VERBOSE = false

-- ------------------------------------------------------------------- estado
type Agg = {
	events: { [string]: number },
	dmgByElement: { [string]: number },
	killsByElement: { [string]: number },
	--- CONJURAÇÕES por elemento (basic + tactic). É o DENOMINADOR DE USO: sem ele
	--- "Fogo causou 28% do dano" não diz nada — pode ser dominância ou pode ser
	--- que Fogo foi 28% dos disparos. A régua da auditoria (dano/uso <= 1,25x) só
	--- existe porque este número existe.
	castsByElement: { [string]: number },
	--- dano CAUSADO por nível de escudo de quem causou (0 = sem dado). É a
	--- medida direta do risco de bola de neve do Escudo Evolutivo (GDD §5).
	dmgByTier: { [number]: number },
	--- dano CAUSADO por AUTOR na partida. É a moeda do gatilho pós-playtest nº1
	--- (`shield.thresholds[4]` = p90 do dano por jogador humano): sem a
	--- distribuição por jogador, o gatilho fica inacionável justamente no dia em
	--- que existir dado humano. `sweep.luau` já publica isto — mas só com BOTS, e
	--- o relatório do jogo é o único que o Diretor lê num servidor publicado.
	dmgByActor: { [string]: number },
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
	--- ENGAJAMENTOS (não eventos): quantas oportunidades tiveram AO MENOS um ping
	--- e quantas terminaram em fusão. São razões que NÃO PODEM passar de 100% —
	--- ver o bloco `oppOpen` abaixo para o porquê de não dividir evento por evento.
	oppPinged: number,
	oppFused: number,
	acceptLatSum: number,
	acceptLatN: number,
	terrainByActor: { [string]: number },
	terrainTotal: number,
	ttk: { number },
	ttkSeen: number,
	--- MODO DE ENTRADA do disparo (docs/ROBLOX.md §4: "mira e disparo em UM
	--- gesto", o requisito que reprovou no APK 2D). `inputCasts` é o denominador
	--- de COBERTURA (quantos disparos vieram instrumentados); a régua mesmo é o
	--- recorte de TOQUE: drag (gesto único) contra tap (toque rápido). Em
	--- teclado a pergunta não existe — por isso o recorte é por aparelho.
	inputCasts: number,
	inputTouchByKind: { [string]: number },
}

local function newAgg(): Agg
	return {
		events = {},
		dmgByElement = {},
		killsByElement = {},
		castsByElement = {},
		dmgByTier = {},
		dmgByActor = {},
		dmgTotal = 0,
		absorbed = 0,
		kills = 0,
		knocks = 0,
		eliminations = 0,
		dodges = 0,
		nullified = 0,
		pactChannels = 0,
		oppPinged = 0,
		oppFused = 0,
		acceptLatSum = 0,
		acceptLatN = 0,
		terrainByActor = {},
		terrainTotal = 0,
		ttk = {},
		ttkSeen = 0,
		inputCasts = 0,
		inputTouchByKind = {},
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
--- algum jogador chegou carimbado com plataforma? (checklist de pendencias)
local sawDeviceTier = false

-- ------------------------------------------------- APARELHO (por jogador)
--- Aparelhos CONHECIDOS. Cliente hostil não cria coluna nova no relatório do
--- Diretor: `tier` fora desta lista é DESCARTADO — não vira categoria nova e
--- nem sobrescreve o que já estava certo.
local DEVICE_TIERS = { low = true, mid = true, high = true }
--- Carimbo do jogador, válido para TODOS os eventos seguintes dele. Vive fora
--- da partida (o aparelho não muda quando a partida troca) e sai no fim da
--- SESSÃO.
--- ponytail: TETO CONHECIDO — sem `session_end` fiado, estes dois mapas crescem
--- por jogador DISTINTO visto pelo servidor (dezenas num alpha, e só strings).
--- Upgrade, se um dia importar: limpar em Players.PlayerRemoving.
local deviceTier: { [string]: string } = {}
local deviceTouch: { [string]: boolean } = {}

-- --------------------------------------------------- SESSÃO (pergunta V5)
--- NADA aqui é zerado por matchStart/matchEnd — ver o cabeçalho. É o único
--- estado do módulo que ATRAVESSA partidas de propósito.
type SessionRec = {
	startedAt: number,
	--- provas em que este jogador PRODUZIU evento (ver `record`)
	matches: number,
	lastMatch: string?,
	lastEvent: string,
}
--- sessões ABERTAS (jogador conectado), por hash humano
local sessions: { [string]: SessionRec } = {}
local sessOpened = 0
local sessClosed = 0
local sessDur: { number } = {}
local sessDurTruncated = false
--- FUNIL DE PROVAS: `sessPlayed[k]` = sessões que chegaram a >= k provas. É
--- monótono por construção (quem chega à 2ª passou pela 1ª), então a razão
--- `>=2 / >=1` NÃO PODE passar de 100% — a lição que o funil da Sintonia
--- cobrou caro. Conta sessão ABERTA também: "provas jogadas" só cresce, logo
--- incluir quem ainda está jogando pode SUBESTIMAR a retenção, nunca inflar.
local sessPlayed = { 0, 0, 0 }
--- ONDE desistiu — quatro respostas diferentes para a mesma pergunta, e cada
--- uma muda uma decisão diferente. Ordem fixa para o relatório não embaralhar.
local EXIT_ORDER = { "lobby", "tutorial", "no meio da prova", "depois da prova" }
local sessExit: { [string]: number } = {}
--- fase da zona em que largou NO MEIO da prova (largar na fase 1 e largar na
--- fase 4 são queixas opostas: tédio de abertura x punição de fim)
local quitByPhase: { [number]: number } = {}
--- ONBOARDING no quadro da V5: o Tutorial já emite uma linha por passo
--- ALCANÇADO com `completed` — aqui só se AGREGA (nada é reimplementado).
type StepAgg = { reached: number, dropped: number }
local tutorialSteps: { [string]: StepAgg } = {}
local tutorialOrder: { string } = {}

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

--- ENGAJAMENTOS DE SINTONIA — o conserto do funil que passava de 100%.
---
--- MEDIDO NO HARNESS (18/08): `conversao oportunidade -> ping = 400%`. Não era
--- bug de código, era RÓTULO MENTINDO: o Sintonia emite `sintonia_opportunity`
--- na BORDA DE SUBIDA (uma vez por engajamento, e a janela fica aberta), mas
--- ping/aceite/fusão podem repetir VÁRIAS vezes dentro do mesmo engajamento.
--- Dividir contagem de evento por contagem de evento produzia número impossível
--- — e o único motivo desta telemetria existir é a leitura do playtest.
---
--- CONSERTO: a oportunidade continua sendo "engajamento em que o combo era
--- possível", e o relatório publica `% de oportunidades que viraram >= 1 ping` e
--- `% que viraram fusão` — cada engajamento é marcado NO MÁXIMO UMA VEZ, logo
--- as razões são <= 100% por construção. As contagens brutas saem ao lado, e o
--- excesso vira `pings por oportunidade`, rotulado como RAZÃO (pode passar de
--- 1,0 — e aí quer dizer "a dupla pingou de novo dentro da mesma janela").
---
--- ponytail: TETO CONHECIDO — o evento de ping não diz COM QUEM era o pacto
--- (`sintonia_ping` só carrega `allies_reached`), então um ping credita todo
--- engajamento ABERTO que envolva quem pingou. Isso pode creditar um
--- engajamento a mais numa esquadra grande, nunca a menos, e NUNCA passa de 1
--- por engajamento. Upgrade, se um dia importar: pedir `with_user_id` no ping.
type Engagement = { a: string, b: string, isBot: boolean, pinged: boolean, fused: boolean }
--- chaveado por par de aliados: o Sintonia só reabre a oportunidade do par
--- depois que a anterior fechou, então existe no máximo uma viva por par.
local oppOpen: { [string]: Engagement } = {}

--- maior nível de Escudo Evolutivo visto por sujeito (GDD §5). Responde "com que
--- frequência alguém chega ao nível 4" sem log cru: é um número por jogador.
local shieldPeak: { [string]: number } = {}

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

--- Nível do Escudo Evolutivo de quem é o sujeito. O Combat expõe
--- `getShieldLevel` (pendência atendida pela raia B): sem ler daqui o campo
--- `shield_tier` saía 0 em TODO evento mesmo com a API pronta, e a pergunta do
--- snowball ficava sem denominador.
local function shieldLevelOf(player: Player?): number
	if not player or not combat or not combat.getShieldLevel then
		return 0
	end
	local ok, lv = pcall(combat.getShieldLevel, player)
	return if ok and typeof(lv) == "number" then lv :: number else 0
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
--- Carimba o aparelho de uma CHAVE já resolvida. Separado de `Telemetry.
--- setDevice` porque é aqui que mora a validação — e o autoteste consegue
--- provar a rejeição sem fabricar Player nenhum (`Instance.new("Player")` não
--- existe no Roblox).
local function stampDevice(key: string, tier: any, touch: any): boolean
	if typeof(tier) ~= "string" or not DEVICE_TIERS[tier] then
		return false
	end
	deviceTier[key] = tier
	deviceTouch[key] = touch == true
	sawDeviceTier = true
	return true
end

--- Preenche os 11 campos de `Events.CONTEXT_FIELDS`. Quem emite manda só o
--- específico; tudo abaixo é descoberto aqui.
local function buildContext(name: string, subject: any, payload: any): { [string]: any }
	local key, isBot, player, member = resolve(subject)
	local p: any = if typeof(payload) == "table" then payload else {}
	local pos = if typeof(p.pos) == "Vector3" then p.pos else rootPos(player)
	if typeof(p.device_tier) == "string" then
		sawDeviceTier = true
	end
	--- aparelho vem do CARIMBO do jogador (Telemetry.setDevice), não do payload:
	--- quem emite não sabe a plataforma, e o cliente já reportou uma vez só.
	local device = deviceTier[key]
	local _, aliveTotal = aliveNow()
	return {
		event = name,
		match_id = matchId or "sem_partida",
		user_id_hash = key,
		is_bot = isBot,
		team_id = if typeof(p.team_id) == "number" then p.team_id else teamOf(member),
		device_tier = if typeof(p.device_tier) == "string" then p.device_tier else (device or "unknown"),
		timestamp = os.clock(),
		element = elementOf(player, p.element),
		position_bucket = if pos then Events.positionBucket(pos) else "?",
		shield_tier = if typeof(p.shield_tier) == "number" then p.shield_tier else shieldLevelOf(player),
		alive_count = if typeof(p.alive_count) == "number" then p.alive_count else aliveTotal,
		zone_phase = if typeof(p.zone_phase) == "number" then p.zone_phase else zonePhase(),
	}
end

-- ------------------------------------------------------------- agregação
local N = Events.Name

--- Abre a sessão do jogador. `session_start` repetido (fiação dobrada, rejoin
--- rápido) NÃO reinicia o relógio: perder o começo é perder a única medida de
--- "quanto tempo ficou".
local function openSession(key: string, now: number)
	if sessions[key] then
		return
	end
	sessions[key] = { startedAt = now, matches = 0, lastMatch = nil, lastEvent = "" }
	sessOpened += 1
end

--- Fecha a sessão e classifica ONDE o jogador desistiu. Sair no lobby, sair no
--- meio da 1ª prova e sair depois de 3 provas são TRÊS respostas diferentes
--- para a mesma pergunta — por isso não viram um número só.
local function closeSession(key: string, now: number)
	local s = sessions[key]
	if not s then
		return -- `session_end` sem `session_start`: não dá para medir nada
	end
	sessions[key] = nil
	sessClosed += 1
	if #sessDur < SESSION_CAP then
		table.insert(sessDur, now - s.startedAt)
	else
		sessDurTruncated = true
	end
	local where: string
	if s.matches == 0 then
		--- nunca jogou: o último evento diz se ele estava no Campo de Provas
		--- (onboarding) ou parado no lobby esperando partida.
		where = if s.lastEvent == N.TutorialStep then "tutorial" else "lobby"
	elseif running and not eliminatedKeys[key] then
		--- largou COM a partida rodando e SEM ter sido eliminado: desistência no
		--- meio da prova, o sinal mais duro que existe contra o combate.
		where = "no meio da prova"
		local ph = zonePhase()
		quitByPhase[ph] = (quitByPhase[ph] or 0) + 1
	else
		where = "depois da prova"
	end
	sessExit[where] = (sessExit[where] or 0) + 1
	--- o carimbo de aparelho morre com a sessão (é o que ele descreve)
	deviceTier[key] = nil
	deviceTouch[key] = nil
end

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

--- Chave estável do par de aliados (ordenada: o par A-B é o mesmo que B-A).
local function pairKeyOf(a: string, b: string): string
	return if a < b then a .. "|" .. b else b .. "|" .. a
end

--- Marca o engajamento ABERTO de quem agiu. Cada engajamento conta no máximo
--- UMA vez em cada marca — é isto que impede a razão de passar de 100%.
local function markEngagement(who: string, fused: boolean)
	for _, e in pairs(oppOpen) do
		if e.a == who or e.b == who then
			if fused and not e.fused then
				e.fused = true
				for _, g in ipairs(cohortsFor(e.isBot)) do
					g.oppFused += 1
				end
			elseif not fused and not e.pinged then
				e.pinged = true
				for _, g in ipairs(cohortsFor(e.isBot)) do
					g.oppPinged += 1
				end
			end
		end
	end
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

	--- SESSÃO (V5): quantas PROVAS este jogador jogou. Não existe evento de
	--- "entrou na partida", então participação é ter PRODUZIDO evento com a
	--- partida rodando — é o que separa quem jogou de quem ficou de plateia.
	--- `tutorial_step` fica de fora: quem está no Campo de Provas não está
	--- jogando a prova que corre lá fora, e contar isso inflaria a V5.
	--- ponytail: heurístico. Upgrade, se um dia importar: o Match emitir um
	--- evento de "jogador entrou na partida" e este bloco ler ele.
	local sess = sessions[ctx.user_id_hash]
	if sess then
		sess.lastEvent = name
		if running and name ~= N.TutorialStep and sess.lastMatch ~= matchId then
			sess.lastMatch = matchId
			sess.matches += 1
			if sess.matches <= #sessPlayed then
				sessPlayed[sess.matches] += 1
			end
		end
	end

	--- pico de escudo do sujeito: qualquer evento dele serve de amostra (o nível
	--- só sobe, então o maior visto É o que ele alcançou na partida)
	local tier = tonumber(ctx.shield_tier) or 0
	if tier > (shieldPeak[ctx.user_id_hash] or 0) then
		shieldPeak[ctx.user_id_hash] = tier
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
				g.dmgByTier[tier] = (g.dmgByTier[tier] or 0) + amount
				--- dano SEM autor (`environmental`) chega com a VÍTIMA como sujeito:
				--- creditar aqui inverteria o dono do dano. Vai só o dano com autor.
				if p.environmental ~= true then
					g.dmgByActor[ctx.user_id_hash] = (g.dmgByActor[ctx.user_id_hash] or 0) + amount
				end
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
	elseif name == N.SpellCast or name == N.TacticCast then
		--- USO por elemento. Sem este denominador não existe "ajustado por uso" e
		--- a régua de dominância da auditoria (<= 1,25x) não tem como ser aplicada.
		local el = ctx.element or "?"
		for _, g in ipairs(cohorts) do
			g.castsByElement[el] = (g.castsByElement[el] or 0) + 1
		end
		--- MODO DE ENTRADA (docs/ROBLOX.md §4). Campo OPCIONAL de propósito: se
		--- ainda não vier, nada é contado e o resto do relatório continua
		--- inteiro — a régua diz "0 disparos instrumentados" em vez de inventar
		--- um número. O que a lista de `Events.INPUT_KINDS` não conhece é
		--- descartado (`sanitizeInput` devolve nil), nunca vira balde novo.
		local kind = Events.sanitizeInput(p.input)
		if kind then
			local onTouch = deviceTouch[ctx.user_id_hash] == true
			for _, g in ipairs(cohorts) do
				g.inputCasts += 1
				if onTouch then
					g.inputTouchByKind[kind] = (g.inputTouchByKind[kind] or 0) + 1
				end
			end
		end
	elseif name == N.TutorialStep then
		--- ONBOARDING agregado (o Tutorial já emite UMA linha por passo
		--- ALCANÇADO com `completed`): alcance = linhas do passo, abandono =
		--- linhas com `completed = false`. Nada é reimplementado aqui.
		local step = if typeof(p.step) == "string" then p.step else "?"
		local t = tutorialSteps[step]
		if not t and #tutorialOrder < TUTORIAL_CAP then
			t = { reached = 0, dropped = 0 }
			tutorialSteps[step] = t
			table.insert(tutorialOrder, step)
		end
		if t then
			t.reached += 1
			if p.completed ~= true then
				t.dropped += 1
			end
		end
	elseif name == N.SintoniaOpportunity then
		--- abre (ou reabre) o engajamento do par. Ver o bloco `oppOpen` no topo.
		local other = tonumber(p.with_user_id)
		local otherKey = if other then Events.userHash(other) else "?"
		oppOpen[pairKeyOf(ctx.user_id_hash, otherKey)] =
			{ a = ctx.user_id_hash, b = otherKey, isBot = isBot, pinged = false, fused = false }
	elseif name == N.SintoniaPing then
		--- PING DE TREINO NÃO É RESPOSTA DA V1. O Campo de Provas dispara o ping
		--- de verdade (passo honesto), e ele chega aqui carimbado com
		--- `tutorial = true`. Contá-lo infla o numerador da única pergunta que
		--- este produto existe para responder — "duas pessoas coordenam sem que
		--- alguém mande?" — com gente que estava justamente sendo mandada.
		--- O evento continua contado no total de eventos; o que ele não faz é
		--- abrir engajamento no FUNIL.
		if p.tutorial ~= true then
			markEngagement(ctx.user_id_hash, false)
		end
	elseif name == N.SintoniaFired then
		markEngagement(ctx.user_id_hash, true)
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
	--- SESSÃO SAI ANTES DA JANELA DE PARTIDA — e isto não é atalho, é a única
	--- forma de a V5 existir:
	---   · `session_start` chega no LOBBY: passar pelo bloco abaixo abriria uma
	---     "partida implícita" toda vez que alguém conectasse;
	---   · `session_end` chega DEPOIS do `matchEnd`, e a guarda `ended` (logo
	---     abaixo) descarta resíduo de partida — engoliria exatamente o evento
	---     que responde "em que ponto ele desistiu".
	if name == N.SessionStart or name == N.SessionEnd then
		local key, isBot, player = resolve(subject)
		--- bot não "volta amanhã": sessão é de GENTE ou não é nada.
		if isBot or player == nil then
			return
		end
		local now = os.clock()
		if name == N.SessionStart then
			openSession(key, now)
		else
			closeSession(key, now)
		end
		if Telemetry.VERBOSE then
			print(string.format("[tel] %-24s user=%s sessoes=%d/%d", name, key, sessClosed, sessOpened))
		end
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

--- tier: "low"|"mid"|"high" · touch: boolean. Carimba o jogador para todos os
--- eventos seguintes dele. Chamado pelo handler de Net.Names.DeviceInfo.
---
--- O `tier` vem do CLIENTE por remote — é entrada hostil por definição, e por
--- isso é validada contra a lista conhecida em vez de aceita como veio: um
--- cliente adulterado não pode criar coluna nova no relatório do Diretor nem
--- apagar o carimbo bom com lixo. Nada aqui decide jogo: no pior caso o
--- jogador fica sem carimbo e o relatório o conta como aparelho desconhecido.
function Telemetry.setDevice(player: Player, tier: string, touch: boolean)
	local p = asPlayer(player)
	if not p then
		return
	end
	stampDevice(Events.userHash(p.UserId), tier, touch)
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
	table.clear(oppOpen)
	table.clear(shieldPeak)
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

--- RAZÃO CRUA, não porcentagem: pode passar de 1,0 de forma legítima (o mesmo
--- engajamento aceita vários pings). Rótulo separado de propósito — foi
--- exatamente misturar os dois que produziu "conversão de 400%" no relatório.
local function ratio(num: number, den: number): string
	if den <= 0 then
		return "-"
	end
	return string.format("%.2fx", num / den)
end

local function ratioRow(out: { string }, label: string, num: (Agg) -> number, den: (Agg) -> number)
	row(out, label, ratio(num(human), den(human)), ratio(num(all), den(all)))
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

--- AS DUAS RÉGUAS DO TTK — elas NÃO são a mesma, e o relatório vinha podendo
--- afirmar sucesso contra a mais frouxa sem dizer de quem era o alvo:
---   · GDD §168 ("TTK alvo: ~1,5-2,5s em duelo parelho") é a FONTE DA VERDADE de
---     design, repetida em docs/ROBLOX.md §1 e §10;
---   · a recomendação da auditoria externa (a que `tools/sweep.luau` usa) é p50
---     entre 2,75s e 3,5s.
--- Um p50 de 3,0s PASSA numa e REPROVA na outra. Qual vale é decisão do Diretor
--- — telemetria não escolhe régua, ela só se recusa a esconder que são duas.
local TTK_BAND_GDD = { 1.5, 2.5, "GDD 168 (design, fonte da verdade)" }
local TTK_BAND_AUDIT = { 2.75, 3.5, "auditoria externa (recomendacao, usada no sweep)" }

--- Os 5 elementos jogáveis. `ambiente` e `?` NÃO entram na dominância: são
--- dano sem autor com elemento (zona, terreno, tiro de bot) e inflariam o
--- denominador de quem tem elemento.
local ELEMENTS = { "Fire", "Water", "Earth", "Wind", "Lightning" }

--- ÍNDICE DE DOMINÂNCIA AJUSTADO POR USO (régua da auditoria: <= 1,25x).
--- share do dano / share das conjurações. 1,00x = o elemento causa exatamente
--- o dano que o uso dele explica. Devolve nil quando não há amostra — número
--- ausente é honesto, número inventado não.
local function usageIndex(g: Agg, el: string): number?
	local casts, dmg = 0, 0
	for _, e in ipairs(ELEMENTS) do
		casts += g.castsByElement[e] or 0
		dmg += g.dmgByElement[e] or 0
	end
	local use = g.castsByElement[el] or 0
	if use <= 0 or casts <= 0 or dmg <= 0 then
		return nil
	end
	return ((g.dmgByElement[el] or 0) / dmg) / (use / casts)
end

local function idx(g: Agg, el: string): string
	local v = usageIndex(g, el)
	return if v then string.format("%.2fx", v) else "-"
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

	-- ------------------------------------------- AS CINCO PERGUNTAS (V1..V5)
	--- O Diretor nao deveria ter de INTERPRETAR metrica para saber se o produto
	--- respondeu. Cada pergunta de docs/ROBLOX.md §1 sai aqui com UM numero e
	--- com a decisao que aquele numero muda; o resto do relatorio e' a prova.
	local function verdict(n: number, value: string): string
		return if n >= MIN_SAMPLE then value else string.format("n=%d, NAO CONCLUI", n)
	end
	local function band(v: number, lo: number, hi: number): string
		return if v < lo then "ABAIXO" elseif v > hi then "ACIMA" else "DENTRO"
	end
	local hs, as = ttkSorted(human), ttkSorted(all)
	local oppN = human.events[N.SintoniaOpportunity] or 0
	local castsHuman = 0
	for _, el in ipairs(ELEMENTS) do
		castsHuman += human.castsByElement[el] or 0
	end
	--- pior elemento pela regua V4 — o indice ja e' ajustado por uso
	local worstEl, worstIdx = "-", 0
	for _, el in ipairs(ELEMENTS) do
		local v = usageIndex(human, el)
		if v and v > worstIdx then
			worstEl, worstIdx = el, v
		end
	end
	--- interacao de terreno com AUTOR: o balde `world` diz que o mundo mudou,
	--- nao que alguem TRANSFORMOU o mundo de proposito (que e' a pergunta V3).
	local terrainOwned = 0
	for actor, n in pairs(human.terrainByActor) do
		if actor ~= "world" then
			terrainOwned += n
		end
	end
	local topExit, topExitN = "-", 0
	for _, k in ipairs(EXIT_ORDER) do
		local v = sessExit[k] or 0
		if v > topExitN then
			topExit, topExitN = k, v
		end
	end
	local sessOpen = 0
	for _ in pairs(sessions) do
		sessOpen += 1
	end

	table.insert(out, "-- AS CINCO PERGUNTAS DE VALIDACAO (docs/ROBLOX.md 1) ----------------")
	table.insert(out, "   Uma linha por pergunta: o NUMERO e a DECISAO que ele muda. Tudo aqui")
	table.insert(out, string.format("   sai de human_only. Amostra abaixo de %d sai como NAO CONCLUI: com", MIN_SAMPLE))
	table.insert(out, "   menos que isso UMA observacao move a razao em mais de 10 pontos.")
	table.insert(out, "V1 a Sintonia acontece entre dois jogadores?")
	table.insert(out, "   " .. verdict(oppN, string.format("%s das oportunidades viraram fusao (n=%d engajamentos)", pct(human.oppFused, oppN), oppN)))
	table.insert(out, "   -> baixo = o combo nao esta NASCENDO: mexer na janela do pacto e no")
	table.insert(out, "      cooldown compartilhado ANTES de desenhar combo novo.")
	table.insert(out, "V2 o TTK esta no alvo com humano mirando?")
	table.insert(out, "   " .. verdict(human.ttkSeen, string.format("p50 %s   p10 %s   (n=%d quedas)", secs(percentile(hs, 0.50)), secs(percentile(hs, 0.10)), human.ttkSeen)))
	--- DUAS REGUAS, cada uma com dono. O relatorio nao escolhe — ele mostra as
	--- duas e GRITA quando discordam, porque foi discordando em silencio que
	--- "TTK 5/5 no alvo" virou conclusao repetida desde a R7.
	if human.ttkSeen >= MIN_SAMPLE then
		local p50h = percentile(hs, 0.50)
		local vGdd = band(p50h, TTK_BAND_GDD[1] :: number, TTK_BAND_GDD[2] :: number)
		local vAud = band(p50h, TTK_BAND_AUDIT[1] :: number, TTK_BAND_AUDIT[2] :: number)
		table.insert(out, string.format("   contra %-50s %s", TTK_BAND_GDD[3], vGdd))
		table.insert(out, string.format("   contra %-50s %s", TTK_BAND_AUDIT[3], vAud))
		if vGdd ~= vAud then
			table.insert(out, "   !! AS DUAS REGUAS DISCORDAM neste numero. Qual vale e' decisao do")
			table.insert(out, "      DIRETOR (docs/ROBLOX.md 11.9) — este relatorio nao escolhe por ele.")
		end
	end
	table.insert(out, "   -> fora da faixa = dano/HP em Balance. p10 baixo com p50 bom = a")
	table.insert(out, "      queixa `morri instantaneo` e' real e a mediana estava escondendo.")
	table.insert(out, "V3 queimar a floresta / congelar o lago vira JOGADA?")
	table.insert(out, "   " .. verdict(flammableAtStart, string.format("%s da cobertura inflamavel queimou (%d de %d celulas)", pct(burnedCount, flammableAtStart), burnedCount, flammableAtStart)))
	table.insert(out, string.format("   interacoes de terreno com AUTOR identificado: %d de %d", terrainOwned, human.terrainTotal))
	table.insert(out, string.format("   descobertas do Grimorio: %d   ·   desfechos taticos: %d", human.events[N.Discovery] or 0, human.events[N.TerrainTacticalOutcome] or 0))
	table.insert(out, "   -> ~0% queimado = terreno virou cenario (custo sem retorno). ~100% =")
	table.insert(out, "      o mapa vira carvao: cortar `fuelBudget`. AUTOR em 0 = da' para")
	table.insert(out, "      dizer que queimou, NAO que alguem queimou de proposito.")
	table.insert(out, "   -> `terrain_tactical_outcome` e' o desfecho VERIFICAVEL (abate, fuga,")
	table.insert(out, "      travessia) e NINGUEM o emite ainda: enquanto isso a descoberta do")
	table.insert(out, "      Grimorio e' a proxy honesta — ela diz que o jogador ACHOU a quimica.")
	table.insert(out, "V4 os 5 elementos se equilibram, ou um domina?")
	table.insert(out, "   " .. verdict(castsHuman, string.format("pior indice: %s %.2fx (regua <=1.25x, n=%d conjuracoes)", worstEl, worstIdx, castsHuman)))
	table.insert(out, "   -> acima da regua = nerf DO ELEMENTO. O indice ja desconta uso, entao")
	table.insert(out, "      `esse e' o mais jogado` nao serve de desculpa.")
	table.insert(out, "V5 o combate segura o jogador ate a proxima prova?")
	table.insert(out, "   " .. verdict(sessPlayed[1], string.format("%s de quem jogou 1 prova ficou para a 2a (n=%d sessoes)", pct(sessPlayed[2], sessPlayed[1]), sessPlayed[1])))
	table.insert(out, string.format("   desistencia mais comum: %s (%d de %d sessoes encerradas)", topExit, topExitN, sessClosed))
	table.insert(out, "   -> ninguem mede `volta amanha` numa sessao: ficar para a 2a prova e' o")
	table.insert(out, "      sinal mais honesto que UMA sessao produz. ONDE ele desistiu diz o")
	table.insert(out, "      QUE consertar — bloco de sessao, no fim do relatorio.")
	table.insert(out, "")

	row(out, "metrica", "human_only", "all_entities")

	-- ---------------------------------------------------------------- funil
	table.insert(out, "-- FUNIL DA SINTONIA (pergunta V1: duplas coordenam?) ----------------")
	table.insert(out, "   OPORTUNIDADE = ENGAJAMENTO em que o combo era possivel. E' contada na")
	table.insert(out, "   BORDA DE SUBIDA (uma por janela aberta) enquanto ping/aceite/fusao")
	table.insert(out, "   podem REPETIR dentro da mesma janela — dividir evento por evento dava")
	table.insert(out, "   `conversao de 400%`, numero impossivel. As linhas em MAIUSCULA sao")
	table.insert(out, "   por ENGAJAMENTO (<=100% por construcao); o excesso sai como RAZAO.")
	table.insert(out, "   Nada disto se normaliza por tempo.")
	countRow(out, "oportunidades (engajamentos)", ev(N.SintoniaOpportunity))
	countRow(out, "pings", ev(N.SintoniaPing))
	ratioRow(out, "  pings por oportunidade (razao)", ev(N.SintoniaPing), ev(N.SintoniaOpportunity))
	convRow(out, "  OPORTUNIDADES COM >=1 PING", function(g)
		return g.oppPinged
	end, ev(N.SintoniaOpportunity))
	countRow(out, "aceites", ev(N.SintoniaAccept))
	convRow(out, "  conversao ping -> aceite", ev(N.SintoniaAccept), ev(N.SintoniaPing))
	row(out, "  latencia media do aceite", if human.acceptLatN > 0 then secs(human.acceptLatSum / human.acceptLatN) else "-", if all.acceptLatN > 0 then secs(all.acceptLatSum / all.acceptLatN) else "-")
	countRow(out, "canalizacoes", ev(N.SintoniaChannelStart))
	-- SAIU DO RELATORIO: `aceite -> canalizacao`. Canalizacao NASCE de duas
	-- fontes (pacto de ping E coincidencia de tiro), entao a divisao pelo aceite
	-- podia passar de 100% sem nada estar errado. A leitura honesta da mesma
	-- pergunta e' a INTENCIONALIDADE, logo abaixo: ela e' subconjunto do proprio
	-- denominador e por isso nao tem como estourar.
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
	convRow(out, "  OPORTUNIDADES QUE VIRARAM FUSAO", function(g)
		return g.oppFused
	end, ev(N.SintoniaOpportunity))
	table.insert(out, "   (razao > 1,00x em `pings por oportunidade` = a dupla pingou de novo")
	table.insert(out, "    DENTRO da mesma janela — e' repeticao, nao conversao)")
	table.insert(out, "   (limite conhecido: `sintonia_ping` nao diz COM QUEM era o pacto, entao")
	table.insert(out, "    um ping credita todo engajamento aberto de quem pingou — pode contar")
	table.insert(out, "    um a mais em esquadra grande, nunca mais de 1 por engajamento)")

	-- ------------------------------------------------------------------ TTK
	table.insert(out, "-- TEMPO ATE A QUEDA (TTK: 1o dano sofrido -> knock) ------------------")
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

	-- ------------------------------------------- dominância AJUSTADA POR USO
	--- "Fogo fez 28% do dano" não diz nada sozinho: pode ser dominância ou pode
	--- ser que Fogo foi 28% dos disparos. A régua da auditoria (V4) é sobre a
	--- RAZÃO entre as duas coisas.
	table.insert(out, "-- DOMINANCIA AJUSTADA POR USO (regua V4: indice <= 1.25x) -----------")
	table.insert(out, string.format("   %-10s %6s %9s %10s %8s %8s", "elemento", "casts", "shr uso", "shr dano", "idx(h)", "idx(a)"))
	local castsAll = 0
	for _, el in ipairs(ELEMENTS) do
		castsAll += all.castsByElement[el] or 0
	end
	local dmgAttributed = 0
	for _, el in ipairs(ELEMENTS) do
		dmgAttributed += all.dmgByElement[el] or 0
	end
	for _, el in ipairs(ELEMENTS) do
		local c = all.castsByElement[el] or 0
		table.insert(out, string.format("   %-10s %6d %9s %10s %8s %8s", el, c, pct(c, castsAll), pct(all.dmgByElement[el] or 0, dmgAttributed), idx(human, el), idx(all, el)))
	end
	table.insert(out, "   (`casts` = basic + tactic. `ambiente` e `?` ficam FORA: dano sem autor")
	table.insert(out, "    com elemento nao pode entrar em dominancia de elemento)")

	-- ------------------------------------------------------ escudo evolutivo
	table.insert(out, "-- ESCUDO EVOLUTIVO (GDD 5 — risco de bola de neve) ------------------")
	local maxLv = #Balance.shield.caps
	local tracked, reachedTop, peakSum = 0, 0, 0
	for _, lv in pairs(shieldPeak) do
		tracked += 1
		peakSum += lv
		if lv >= maxLv then
			reachedTop += 1
		end
	end
	table.insert(out, string.format("sujeitos com nivel medido ...... %d (pico medio %.2f)", tracked, if tracked > 0 then peakSum / tracked else 0))
	table.insert(out, string.format("chegaram ao nivel %d ............ %d (%s dos medidos)", maxLv, reachedTop, pct(reachedTop, tracked)))
	local tiers: { string } = {}
	for lv = 1, maxLv do
		table.insert(tiers, string.format("lv%d %s", lv, pct(all.dmgByTier[lv] or 0, all.dmgTotal)))
	end
	table.insert(out, "dano CAUSADO por nivel ......... " .. table.concat(tiers, "  "))
	table.insert(out, string.format("   (nivel 0 = %s do dano: sujeito sem escudo — zona, terreno ou bot)", pct(all.dmgByTier[0] or 0, all.dmgTotal)))
	--- GATILHO POS-PLAYTEST N.1 (contingente carregado desde a R8):
	--- `shield.thresholds[4]` = p90 do dano CAUSADO por jogador HUMANO na
	--- partida. O `sweep.luau` publica esta distribuicao, mas roda com BOTS — e
	--- num servidor publicado o unico relatorio que existe e' este. Sem a
	--- distribuicao aqui, o gatilho ficava inacionavel exatamente no dia em que
	--- finalmente houvesse gente para medir.
	local dmgEach: { number } = {}
	for _, v in pairs(human.dmgByActor) do
		table.insert(dmgEach, v)
	end
	table.sort(dmgEach)
	local thr4 = Balance.shield.thresholds[maxLv] or 0
	table.insert(out, string.format("dano CAUSADO por jogador humano . %d pilotos medidos", #dmgEach))
	--- percentil de 2 pilotos e' interpolacao de anedota: com amostra pequena
	--- sai o veredito, nao o numero.
	table.insert(out, "   " .. verdict(#dmgEach, string.format("p10 %.0f   p50 %.0f   p90 %.0f", percentile(dmgEach, 0.10), percentile(dmgEach, 0.50), percentile(dmgEach, 0.90))))
	local p90 = percentile(dmgEach, 0.90)
	local move = if p90 < 700 then "BAIXE o limiar" elseif p90 > 1400 then "SUBA o limiar" else "MANTENHA o limiar"
	table.insert(out, string.format("   GATILHO 1: `shield.thresholds[%d]` (hoje %.0f) = este p90.", maxLv, thr4))
	table.insert(out, "              <700 baixe · >1400 suba · entre = mantenha" .. (if #dmgEach >= MIN_SAMPLE then "  ->  " .. move else ""))
	table.insert(out, "   (so human_only: o gatilho e' sobre o que GENTE causa. Dano sem autor")
	table.insert(out, "    — zona, terreno, bot — fica fora, senao o p90 mede a zona.)")

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

	-- ------------------------------------------------------ entrada (celular)
	--- O requisito de §4 e' de CONTROLE, nao de balanceamento: "mira e disparo
	--- em UM gesto" foi o que reprovou no teste do APK 2D. A regua e' o recorte
	--- de TOQUE — em teclado a pergunta nem existe.
	table.insert(out, "-- ENTRADA DO DISPARO (requisito 4: mira e disparo em UM gesto) ------")
	local castsH = (human.events[N.SpellCast] or 0) + (human.events[N.TacticCast] or 0)
	local castsA = (all.events[N.SpellCast] or 0) + (all.events[N.TacticCast] or 0)
	row(out, "disparos com `input` / disparos", string.format("%d / %d", human.inputCasts, castsH), string.format("%d / %d", all.inputCasts, castsA))
	local drag = human.inputTouchByKind.drag or 0
	local tap = human.inputTouchByKind.tap or 0
	local kbd = human.inputTouchByKind.key or 0
	local touchN = drag + tap + kbd
	table.insert(out, string.format("em aparelho de TOQUE ........... %d disparos", touchN))
	table.insert(out, string.format("   drag (o gesto unico) ........ %d (%s)", drag, pct(drag, touchN)))
	table.insert(out, string.format("   tap (toque rapido) .......... %d (%s)", tap, pct(tap, touchN)))
	if kbd > 0 then
		table.insert(out, string.format("   key (teclado em aparelho de toque) %d", kbd))
	end
	table.insert(out, "   VEREDITO: " .. verdict(touchN, string.format("%s dos disparos de toque usaram o gesto unico", pct(drag, touchN))))
	table.insert(out, "   (tap alto = o jogador esta CONTORNANDO o gesto que o requisito pede;")
	table.insert(out, "    e' o mesmo sintoma que reprovou o APK 2D. Conserto e' de CONTROLE.)")
	table.insert(out, "   (aparelho vem de Telemetry.setDevice; sem carimbo, o disparo conta na")
	table.insert(out, "    linha de cobertura e fica FORA do recorte de toque — nunca chutado)")

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
	--- GRIMORIO (GDD 18.3): descoberta e' progressao por EXPERIMENTACAO. Enquanto
	--- `terrain_tactical_outcome` nao for emitido por ninguem (ver PENDENCIAS), a
	--- descoberta e' a melhor proxy que existe para "a quimica do terreno esta
	--- sendo achada sozinha" — que e' a pergunta V3 por outro lado.
	countRow(out, "descobertas do Grimorio (GDD 18.3)", ev(N.Discovery))
	countRow(out, "  desfechos TATICOS de terreno", ev(N.TerrainTacticalOutcome))
	table.insert(out, "   (descoberta = o jogador ACHOU a interacao. Enquanto ninguem emitir")
	table.insert(out, "    `terrain_tactical_outcome`, ela e' a unica proxy de `virou jogada`)")

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

	-- --------------------------------------------------------------- sessão
	table.insert(out, "-- SESSAO E RETORNO (pergunta V5: da' vontade de continuar?) ---------")
	table.insert(out, "   NAO zera no fim da partida, de PROPOSITO: e' a janela do SERVIDOR.")
	table.insert(out, "   Sessao atravessa provas — e' exatamente o que a V5 precisa enxergar.")
	table.insert(out, "   SO HUMANO: bot nao volta, entao aqui nao existe all_entities.")
	table.insert(out, string.format("sessoes ........................ %d encerradas  + %d em curso", sessClosed, sessOpen))
	local ds = table.clone(sessDur)
	table.sort(ds)
	table.insert(out, "duracao das encerradas ......... " .. verdict(#ds, string.format("p50 %s   p10 %s%s", secs(percentile(ds, 0.50)), secs(percentile(ds, 0.10)), if sessDurTruncated then "   [TRUNCADA]" else "")))
	table.insert(out, "   (media global fica FORA pelo mesmo motivo do TTK: esconde quem saiu)")
	table.insert(out, string.format("FUNIL DE PROVAS ................ >=1: %d   >=2: %d (%s)   >=3: %d (%s)", sessPlayed[1], sessPlayed[2], pct(sessPlayed[2], sessPlayed[1]), sessPlayed[3], pct(sessPlayed[3], sessPlayed[1])))
	table.insert(out, "   (monotono por construcao — quem chegou a 2a passou pela 1a — entao a")
	table.insert(out, "    razao NAO tem como passar de 100%. Conta sessao ABERTA tambem: quem")
	table.insert(out, "    ainda esta na 1a prova pode SUBESTIMAR a retencao, nunca inflar.)")
	local exits: { string } = {}
	for _, k in ipairs(EXIT_ORDER) do
		table.insert(exits, string.format("%s %d", k, sessExit[k] or 0))
	end
	table.insert(out, "ponto em que desistiu .......... " .. table.concat(exits, " | "))
	local phaseBits: { string } = {}
	for ph = 1, Balance.match.zone.phases do
		table.insert(phaseBits, string.format("f%d %d", ph, quitByPhase[ph] or 0))
	end
	table.insert(out, "   largou no meio, por fase da zona: " .. table.concat(phaseBits, "  "))
	table.insert(out, "   (lobby = espera longa demais · tutorial = onboarding · no meio da")
	table.insert(out, "    prova = o COMBATE nao segurou · depois da prova = falta gancho para")
	table.insert(out, "    a proxima. Sao quatro consertos DIFERENTES, por isso nao viram um")
	table.insert(out, "    numero so. Largar na fase 1 e' tedio de abertura; na ultima e' fim")
	table.insert(out, "    punitivo demais.)")
	table.insert(out, "abandono do onboarding por passo (o Tutorial emite; aqui so agrega):")
	if #tutorialOrder == 0 then
		table.insert(out, "   (nenhum `tutorial_step` chegou)")
	end
	for _, step in ipairs(tutorialOrder) do
		local t = tutorialSteps[step]
		table.insert(out, string.format("   %-20s alcancou %3d   largou %3d (%s)", step, t.reached, t.dropped, pct(t.dropped, t.reached)))
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
	-- SAIU DAQUI: "`device_tier` exige o cliente reportar plataforma". O
	-- DIAGNOSTICO DA R5 ESTAVA ERRADO — o cliente REPORTA desde sempre
	-- (client/Device.luau dispara Net.Names.DeviceInfo); o que nunca existiu foi
	-- o servidor RECEBER. `Telemetry.setDevice` agora recebe, e a pendencia que
	-- sobra e' a fiacao do handler — some sozinha quando alguem chamar.
	needs(not sawDeviceTier, "ninguem chamou `Telemetry.setDevice`: o cliente reporta em Net.Names.DeviceInfo, falta o handler do servidor repassar — `device_tier` sai 'unknown'")
	needs(sessOpened == 0, "`session_start`/`session_end` nao chegaram — a pergunta V5 fica SEM instrumentacao viva (fiar em Players.PlayerAdded/PlayerRemoving)")
	needs(all.inputCasts == 0 and castsA > 0, "`input` nao vem no `spell_cast` — a regua do gesto unico (requisito 4) fica sem amostra")
	needs(#tutorialOrder == 0, "`tutorial_step` nao chegou — abandono do onboarding fica fora do quadro da V5")
	needs((all.events[N.TerrainTacticalOutcome] or 0) == 0, "`terrain_tactical_outcome` esta no schema e e' consumido aqui, mas NINGUEM o emite (seria Terrain/Combat): a V3 fica sem metrica de DESFECHO e sobra a descoberta do Grimorio como proxy")
	needs((all.events[N.Discovery] or 0) == 0, "`discovery` (Grimorio) nao chegou — sem ele a V3 perde ate a proxy")
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

--- NÚMEROS CRUS da partida, para quem AGREGA várias partidas (a varredura de
--- balanceamento em `roblox/tools/sweep.luau`). O relatório acima é texto para
--- humano — parsear texto para somar 30 partidas seria frágil e mentiria no dia
--- em que alguém mudasse uma legenda.
--- Devolve as tabelas VIVAS (sem cópia): quem chamar lê, não escreve. Só
--- números e strings — nenhuma referência a Instance, para o coletor não segurar
--- personagem destruído na memória de quem guardar o snapshot.
function Telemetry.snapshot(): { [string]: any }
	return {
		match_id = matchId,
		ended = ended,
		duration = (if matchEndedAt > 0 then matchEndedAt else os.clock()) - matchStartedAt,
		human = human,
		all = all,
		shield_peak = shieldPeak,
		fire = {
			fronts = fireFronts,
			cells = fireCells,
			max_cells = fireCellsMax,
			exhausted = fireExhausted,
			burned = burnedCount,
			flammable = flammableAtStart,
		},
	}
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
	local saveOpp = table.clone(oppOpen)
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

	-- 4c) FUNIL QUE NAO PODE PASSAR DE 100% — a regressao que este teste guarda e'
	--     a medida real do harness: `conversao oportunidade -> ping = 400%`.
	--     A oportunidade e' UM engajamento; os pings dentro dela sao varios.
	table.clear(oppOpen)
	fake(N.SintoniaOpportunity, false, "Fire", { with_user_id = 4242 })
	for _ = 1, 4 do
		fake(N.SintoniaPing, false, "Fire", {})
	end
	fake(N.SintoniaFired, false, "Fire", {})
	fake(N.SintoniaFired, false, "Fire", {})
	local opps = human.events[N.SintoniaOpportunity] or 0
	assert(opps == 1, "oportunidade e' contada uma vez por engajamento")
	assert((human.events[N.SintoniaPing] or 0) >= 4, "os pings BRUTOS continuam contados")
	assert(human.oppPinged == 1, "4 pings na MESMA janela = 1 oportunidade convertida, nao 4")
	assert(human.oppFused == 1, "duas fusoes na mesma janela nao contam duas vezes")
	assert(human.oppPinged <= opps and human.oppFused <= opps, "razao do funil NAO pode passar de 100%")
	-- janela NOVA do mesmo par tem de poder converter de novo (senao o
	-- denominador congela e o funil passa a subestimar a coordenacao)
	fake(N.SintoniaOpportunity, false, "Fire", { with_user_id = 4242 })
	fake(N.SintoniaPing, false, "Fire", {})
	assert(human.oppPinged == 2, "engajamento novo do mesmo par converte de novo")

	-- 4c-bis) PING DE TREINO FORA DO FUNIL (R11). O Campo de Provas dispara o
	-- ping REAL — é o que torna o passo honesto —, e antes da R11 TODO jogador
	-- era empurrado para lá: cada sessão injetava treino no numerador da única
	-- pergunta que este produto existe para responder ("duas pessoas coordenam
	-- SEM que alguém mande?"). O evento continua contado; o que ele não faz é
	-- converter engajamento.
	local pingsBefore = human.events[N.SintoniaPing] or 0
	fake(N.SintoniaOpportunity, false, "Fire", { with_user_id = 7777 })
	fake(N.SintoniaPing, false, "Fire", { tutorial = true })
	assert(
		human.oppPinged == 2,
		"ping de TREINO converteu engajamento — o numerador da V1 passa a contar quem estava sendo mandado"
	)
	assert(
		(human.events[N.SintoniaPing] or 0) == pingsBefore + 1,
		"o ping de treino sumiu da contagem BRUTA: suprimir o evento tira do Diretor quantos praticam o combo"
	)
	-- o carimbo não pode virar porta dos fundos: ping normal converte igual
	fake(N.SintoniaPing, false, "Fire", {})
	assert(human.oppPinged == 3, "ping normal parou de abrir engajamento — o funil da V1 morreu junto")

	-- 4d) INDICE DE DOMINANCIA (V4): dano proporcional ao uso da' exatamente 1,00x
	local u = newAgg()
	u.castsByElement.Fire, u.castsByElement.Water = 10, 30
	u.dmgByElement.Fire, u.dmgByElement.Water = 100, 300
	assert(math.abs((usageIndex(u, "Fire") or 0) - 1) < 1e-9, "dano proporcional ao uso = 1,00x")
	u.dmgByElement.Fire = 200
	assert((usageIndex(u, "Fire") or 0) > 1.25, "dano acima do uso tem de estourar a regua de 1,25x")
	assert(usageIndex(u, "Earth") == nil, "elemento sem uso nao inventa indice")

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
	assert(string.find(text, "AS CINCO PERGUNTAS", 1, true), "o relatorio tem de responder V1..V5 POR NOME")
	assert(string.find(text, "n=0, NAO CONCLUI", 1, true), "sem amostra o quadro tem de dizer `n=0, nao conclui` em vez de imprimir razao")

	-- 7) APARELHO — `tier` vem do CLIENTE por remote: entrada hostil
	local saveDevTier, saveDevTouch, saveSaw = table.clone(deviceTier), table.clone(deviceTouch), sawDeviceTier
	table.clear(deviceTier)
	table.clear(deviceTouch)
	sawDeviceTier = false
	assert(stampDevice("u_dev", "mid", true), "tier conhecido tem de ser aceito")
	assert(deviceTier.u_dev == "mid" and deviceTouch.u_dev == true, "o carimbo tem de ficar guardado")
	assert(sawDeviceTier, "carimbo valido tem de baixar a pendencia do rodape")
	assert(not stampDevice("u_dev", "ultra", true), "tier fora da lista nao entra")
	assert(not stampDevice("u_dev", 3, true), "tier que nem e' string nao entra")
	assert(deviceTier.u_dev == "mid", "lixo de cliente nao pode SOBRESCREVER carimbo bom")
	assert(deviceTier.ultra == nil, "cliente hostil nao cria coluna nova no relatorio do Diretor")
	stampDevice("u_dev2", "high", "sim")
	assert(deviceTouch.u_dev2 == false, "`touch` so e' verdadeiro vindo boolean true")
	--- o CONTEXTO tem de ler o carimbo: sem esta linha `device_tier` continuaria
	--- 'unknown' com a API pronta — foi exatamente o que aconteceu com o
	--- `shield_tier` antes da R8.
	stampDevice("world", "low", false)
	assert(buildContext(N.SpellCast, nil, nil).device_tier == "low", "o contexto tem de LER o carimbo do jogador")
	deviceTier.world = nil
	local devN = 0
	for _ in pairs(deviceTier) do
		devN += 1
	end
	Telemetry.setDevice(nil :: any, "low", true)
	Telemetry.setDevice({ name = "Bot 1" } :: any, "high", false)
	local devN2 = 0
	for _ in pairs(deviceTier) do
		devN2 += 1
	end
	assert(devN2 == devN, "quem nao resolve para Player nao pode ser carimbado (nem bot, nem lixo)")

	-- 8) MODO DE ENTRADA — a regua do gesto unico (docs/ROBLOX.md 4)
	stampDevice("u_t", "mid", true) -- `fake()` usa a chave "u_t" para humano
	fake(N.SpellCast, false, "Fire", { input = "drag" })
	fake(N.SpellCast, false, "Fire", { input = "tap" })
	fake(N.SpellCast, false, "Fire", { input = "swipe" }) -- inventado pelo cliente
	fake(N.SpellCast, false, "Fire", {}) -- raia B atrasada: campo nem veio
	assert(human.inputCasts == 2, "so `input` da lista conhecida entra na cobertura")
	assert(human.inputTouchByKind.drag == 1 and human.inputTouchByKind.tap == 1, "o recorte de toque separa gesto unico de toque rapido")
	assert(human.inputTouchByKind.swipe == nil, "modo de entrada inventado nao vira balde novo")
	assert((human.castsByElement.Fire or 0) == 4, "disparo SEM `input` continua contando como conjuracao")
	stampDevice("u_t", "high", false) -- mesmo jogador, agora sem toque
	fake(N.SpellCast, false, "Fire", { input = "key" })
	assert(human.inputCasts == 3 and human.inputTouchByKind.key == nil, "teclado nao entra no recorte de TOQUE")

	-- 8b) DANO POR AUTOR — a moeda do gatilho pos-playtest n.1
	local dmgBefore = human.dmgByActor.u_t or 0
	fake(N.Damage, false, "Fire", { amount = 40, target = "vitima" })
	assert(human.dmgByActor.u_t == dmgBefore + 40, "dano com autor tem de ser creditado a QUEM causou")
	fake(N.Damage, false, "Earth", { amount = 500, environmental = true })
	assert(human.dmgByActor.u_t == dmgBefore + 40, "dano de ambiente chega com a VITIMA no sujeito: creditar inverteria o dono do dano")

	-- 8c) GRIMORIO — a proxy da V3 tem de SAIR impressa
	fake(N.Discovery, false, "Fire", { id = "vapor", found_total = 1, of_total = 12 })
	assert((human.events[N.Discovery] or 0) == 1, "descoberta tem de ser contada")

	-- 8d) AS DUAS REGUAS DO TTK — o relatorio nao pode mais afirmar sucesso sem
	--     dizer de QUEM e' o alvo. p50 = 3,0s: ACIMA do GDD (1,5-2,5) e DENTRO
	--     da auditoria (2,75-3,5). O relatorio TEM de gritar a discordancia.
	for _ = 1, 10 do
		pushSample(human, 3.0)
	end
	local textTtk = Telemetry.report()
	assert(string.find(textTtk, "AS DUAS REGUAS DISCORDAM", 1, true), "p50 de 3,0s passa numa regua e reprova na outra: o relatorio tem de dizer isso")
	assert(string.find(textTtk, "GDD 168", 1, true) and string.find(textTtk, "auditoria externa", 1, true), "cada regua tem de sair com a FONTE no nome")
	assert(string.find(textTtk, "descobertas do Grimorio (GDD 18.3)", 1, true), "a descoberta do Grimorio tem de ser IMPRESSA em linha propria (era coletada e nunca aparecia)")
	assert(string.find(textTtk, "GATILHO 1", 1, true), "a distribuicao de dano por jogador tem de sair com o gatilho junto")
	--- e quando as duas CONCORDAM, o alarme nao pode aparecer (senao vira ruido)
	for _ = 1, 20 do
		pushSample(human, 9.0)
	end
	local textAgree = Telemetry.report()
	assert(not string.find(textAgree, "AS DUAS REGUAS DISCORDAM", 1, true), "com p50 acima das DUAS reguas nao ha discordancia para alarmar")

	-- 9) SESSAO (pergunta V5) — o dado que NAO pode morrer com a partida
	local saveSess = table.clone(sessions)
	local saveOpened, saveClosed, saveTrunc = sessOpened, sessClosed, sessDurTruncated
	local saveDur, savePlayed = table.clone(sessDur), table.clone(sessPlayed)
	local saveExit, savePhase = table.clone(sessExit), table.clone(quitByPhase)
	local saveSteps, saveOrder = table.clone(tutorialSteps), table.clone(tutorialOrder)
	--- O teste abaixo chama `Telemetry.matchStart` DE VERDADE — é a única forma
	--- de provar que o fim de partida não leva a sessão junto. Por isso TUDO que
	--- `matchStart` zera é salvo aqui e devolvido no fim: rodar o autoteste com
	--- partida viva não pode custar o dado do Diretor.
	local saveStarted, saveEnded2, saveEndedAt = matchStartedAt, ended, matchEndedAt
	local saveElim, saveBurn, saveCurve = table.clone(eliminatedKeys), table.clone(burnedCells), table.clone(curve)
	local savePeak, saveCurveTr = table.clone(shieldPeak), curveTruncated
	local saveBurnN, saveFlam = burnedCount, flammableAtStart
	local saveF1, saveF2, saveF3, saveF4 = fireFronts, fireCells, fireCellsMax, fireExhausted
	local saveExtD, saveExtK, saveSrc = externalDamage, externalKnock, damageSource
	local saveFails, saveAcc, saveAlive = internalFailures, sampleAcc, aliveCache.at
	table.clear(sessions)
	table.clear(sessDur)
	table.clear(sessExit)
	table.clear(quitByPhase)
	table.clear(tutorialSteps)
	table.clear(tutorialOrder)
	sessOpened, sessClosed, sessDurTruncated = 0, 0, false
	sessPlayed[1], sessPlayed[2], sessPlayed[3] = 0, 0, 0

	local function sfake(key: string, name: string, payload: any)
		record(name, false, { event = name, is_bot = false, element = "Fire", user_id_hash = key, timestamp = 100, team_id = 0, zone_phase = 1, alive_count = 3, position_bucket = "0,0" }, payload)
	end
	local K = "u_sess"
	openSession(K, 100)
	openSession(K, 500)
	assert(sessOpened == 1 and sessions[K].startedAt == 100, "`session_start` repetido nao pode reiniciar o relogio da sessao")
	Telemetry.emit(N.SessionStart, { name = "Bot 9" })
	assert(sessOpened == 1, "bot nao abre sessao — bot nao volta amanha")

	--- PROVAS: uma por partida em que ele PRODUZIU evento. O tutorial nao conta:
	--- quem esta no Campo de Provas nao esta jogando a prova que corre la fora.
	matchId, running = "m_selftest_1", true
	sfake(K, N.TutorialStep, { step = "andar", completed = false, reason = "left" })
	assert(sessions[K].matches == 0, "tutorial nao e' prova")
	assert(tutorialSteps.andar.reached == 1 and tutorialSteps.andar.dropped == 1, "abandono por passo sai do proprio `tutorial_step`")
	sfake(K, N.Damage, { amount = 1, target = "x" })
	assert(sessions[K].matches == 1 and sessPlayed[1] == 1, "evento com a partida rodando = jogou a prova")
	sfake(K, N.Damage, { amount = 1, target = "x" })
	assert(sessions[K].matches == 1, "a MESMA prova nao pode contar duas vezes")
	matchId = "m_selftest_2"
	sfake(K, N.Damage, { amount = 1, target = "x" })
	assert(sessions[K].matches == 2 and sessPlayed[2] == 1, "prova nova conta de novo")
	assert(sessPlayed[2] <= sessPlayed[1] and sessPlayed[3] <= sessPlayed[2], "funil de provas e' monotono: NAO pode passar de 100%")

	--- ONDE desistiu: quatro respostas, quatro consertos diferentes
	closeSession(K, 400)
	assert(sessClosed == 1 and math.abs(sessDur[1] - 300) < 1e-9, "duracao da sessao = fim - inicio")
	assert(sessExit["no meio da prova"] == 1, "largou com a prova rodando e sem ser eliminado = desistiu no meio")
	local quits = 0
	for _, v in pairs(quitByPhase) do
		quits += v
	end
	assert(quits == 1, "largar no meio tem de registrar a FASE da zona")
	assert(deviceTier[K] == nil, "o carimbo de aparelho morre com a sessao que ele descreve")
	openSession("u_lobby", 10)
	closeSession("u_lobby", 20)
	assert(sessExit.lobby == 1, "sair sem jogar prova nenhuma e' abandono de LOBBY")
	openSession("u_tut", 10)
	sfake("u_tut", N.TutorialStep, { step = "atirar", completed = false, reason = "left" })
	closeSession("u_tut", 20)
	assert(sessExit.tutorial == 1, "quem estava no Campo de Provas largou no ONBOARDING, nao no lobby")
	openSession("u_done", 10)
	sfake("u_done", N.Damage, { amount = 1, target = "x" })
	eliminatedKeys["u_done"] = true
	closeSession("u_done", 30)
	assert(sessExit["depois da prova"] == 1, "quem foi eliminado nao 'desistiu no meio'")
	eliminatedKeys["u_done"] = nil
	closeSession("u_fantasma", 99)
	assert(sessClosed == 4, "`session_end` sem `session_start` nao inventa sessao")

	--- SESSAO NAO PASSA PELA JANELA DE PARTIDA: com o coletor parado, um evento
	--- de sessao que caisse no caminho comum abriria PARTIDA IMPLICITA toda vez
	--- que alguem conectasse no lobby.
	matchId, running, ended = nil, false, false
	Telemetry.emit(N.SessionStart, nil)
	assert(not running and matchId == nil, "evento de sessao nao pode abrir PARTIDA IMPLICITA")

	--- ...e o fim da partida NAO pode levar a sessao junto (o inverso do
	--- vazamento classico do projeto: aqui o erro seria ZERAR o que precisa
	--- sobreviver). `matchStart` de verdade, com tudo salvo la em cima.
	--- sessao AINDA ABERTA na hora da virada: e' o caso que mais dói perder —
	--- zerar o mapa vivo faz o jogador conectado nunca fechar sessao, e a V5
	--- perde justamente quem ficou.
	openSession("u_vivo", 42)
	matchId, running = "m_selftest_2", true
	sfake("u_vivo", N.Damage, { amount = 1, target = "x" })
	Telemetry.matchStart("selftest_v5")
	assert(sessOpened == 5 and sessClosed == 4 and #sessDur == 4, "matchStart NAO pode zerar dado de SESSAO")
	assert(sessions.u_vivo ~= nil, "matchStart NAO pode apagar a sessao de quem esta CONECTADO")
	assert(sessions.u_vivo.startedAt == 42 and sessions.u_vivo.matches == 1, "a sessao viva atravessa a virada de partida INTEIRA (relogio e provas)")
	assert(sessPlayed[1] == 3 and sessPlayed[2] == 1 and #tutorialOrder == 2, "funil de provas e onboarding atravessam a partida")
	assert(human.dmgTotal == 0, "o que e' de PARTIDA continua morrendo no matchStart")
	--- estado que `matchEnd` deixa (coletor fechado): `session_end` que chega
	--- depois ainda TEM de contar — e' o dado mais valioso da V5, e a guarda
	--- `ended` do `emit` engoliria se a sessao passasse por la.
	running, ended = false, true
	openSession("u_pos_fim", 1)
	closeSession("u_pos_fim", 9)
	assert(sessClosed == 5, "sessao fechada DEPOIS do fim da partida ainda tem de ser contada")
	assert(sessExit.lobby == 2, "quem entrou e saiu sem jogar prova nenhuma e' abandono de LOBBY")
	assert((sessExit["no meio da prova"] or 0) == 1, "sem partida rodando ninguem pode ter largado NO MEIO")

	local text2 = Telemetry.report()
	assert(string.find(text2, "SESSAO E RETORNO", 1, true) and string.find(text2, "FUNIL DE PROVAS", 1, true), "o bloco da V5 tem de sair no relatorio, cabecalho e funil")
	assert(string.find(text2, "no meio da prova", 1, true), "o ponto de desistencia tem de sair nomeado")

	table.clear(sessions)
	for k, v in pairs(saveSess) do
		sessions[k] = v
	end
	table.clear(sessDur)
	for _, v in ipairs(saveDur) do
		table.insert(sessDur, v)
	end
	table.clear(sessExit)
	for k, v in pairs(saveExit) do
		sessExit[k] = v
	end
	table.clear(quitByPhase)
	for k, v in pairs(savePhase) do
		quitByPhase[k] = v
	end
	table.clear(tutorialSteps)
	for k, v in pairs(saveSteps) do
		tutorialSteps[k] = v
	end
	table.clear(tutorialOrder)
	for _, v in ipairs(saveOrder) do
		table.insert(tutorialOrder, v)
	end
	sessPlayed[1], sessPlayed[2], sessPlayed[3] = savePlayed[1], savePlayed[2], savePlayed[3]
	sessOpened, sessClosed, sessDurTruncated = saveOpened, saveClosed, saveTrunc
	matchStartedAt, ended, matchEndedAt = saveStarted, saveEnded2, saveEndedAt
	--- devolve TUDO que o `matchStart` do teste zerou
	table.clear(eliminatedKeys)
	for k, v in pairs(saveElim) do
		eliminatedKeys[k] = v
	end
	table.clear(burnedCells)
	for k, v in pairs(saveBurn) do
		burnedCells[k] = v
	end
	table.clear(curve)
	for _, v in ipairs(saveCurve) do
		table.insert(curve, v)
	end
	table.clear(shieldPeak)
	for k, v in pairs(savePeak) do
		shieldPeak[k] = v
	end
	curveTruncated, burnedCount, flammableAtStart = saveCurveTr, saveBurnN, saveFlam
	fireFronts, fireCells, fireCellsMax, fireExhausted = saveF1, saveF2, saveF3, saveF4
	externalDamage, externalKnock, damageSource = saveExtD, saveExtK, saveSrc
	internalFailures, sampleAcc, aliveCache.at = saveFails, saveAcc, saveAlive
	table.clear(deviceTier)
	for k, v in pairs(saveDevTier) do
		deviceTier[k] = v
	end
	table.clear(deviceTouch)
	for k, v in pairs(saveDevTouch) do
		deviceTouch[k] = v
	end
	sawDeviceTier = saveSaw

	human, all, HUMAN_COHORTS, BOT_COHORTS = saveH, saveA, saveHC, saveBC
	matchId, running = saveMatch, saveRunning
	firstDamageAt, firstHitCount = saveFirst, saveCount
	table.clear(firstHitAt)
	for k, v in pairs(saveHits) do
		firstHitAt[k] = v
	end
	table.clear(oppOpen)
	for k, v in pairs(saveOpp) do
		oppOpen[k] = v
	end
	print("[Telemetry] autoteste OK — percentis, reservatorio, knock/eliminacao, funil <=100% e separacao human/bot")
	return true
end

return Telemetry
