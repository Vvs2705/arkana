--!strict
-- ============================================================================
-- RAIA (R3) — LOOP DE BATTLE ROYALE: lobby, início, zona arcana fechando,
-- eliminações, bots de preenchimento (GDD §19.6) e Selo do Campeão (GDD §18.6).
-- Escopo do GDD §19.2: 20 jogadores, partidas curtas (4–6 min).
--
-- AUTORIDADE (docs/ROBLOX.md §3): a partida INTEIRA é decidida aqui. O cliente
-- só recebe Net.MatchState/Net.Announce e apresenta. Nenhum remote C→S entra
-- neste módulo — não existe intenção de cliente capaz de mudar a partida.
--
-- FRONTEIRA DE DANO: a zona NUNCA escreve em Humanoid.Health. Ela calcula o DPS
-- da fase e manda Combat.applyDamage decidir (escudo, i-frames, número na tela)
-- — a mesma fronteira que o Terrain respeita com `hazardAt`.
--
-- Ciclo: Lobby → Playing → Ended → Lobby (o servidor nunca "acaba").
-- ============================================================================

local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local ServerScriptService = game:GetService("ServerScriptService")
local Workspace = game:GetService("Workspace")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Balance = require(Shared:WaitForChild("Balance"))
local Elements = require(Shared:WaitForChild("Elements"))
local Events = require(Shared:WaitForChild("Events"))
local Grid = require(Shared:WaitForChild("Grid"))
local Net = require(Shared:WaitForChild("Net"))

local ServerModules = ServerScriptService:WaitForChild("Server")
local Arena = require(ServerModules:WaitForChild("Arena"))
local Bots = require(ServerModules:WaitForChild("Bots"))
local Teams = require(ServerModules:WaitForChild("Teams"))

local M = Balance.match
local Z = M.zone

-- --- ritmo e apresentação (NÃO são balanceamento) ---------------------------
local TUNE = {
	--- cadência do Net.MatchState (a UI interpola o resto — 2 Hz basta)
	stateInterval = 0.5,
	--- cadência do dano da zona (mesmo passo do hazard do terreno)
	zoneTick = 0.5,
	--- tela de fim de partida antes de voltar ao lobby
	endedTime = 10,
	--- carência após o início antes de checar vitória (evita "campeão" no frame 0)
	startGrace = 2.0,
	--- crédito de abate: último a acertar dentro desta janela leva o kill
	killWindow = 10,
	--- altura do poleiro de espectador (fora do alcance de qualquer magia)
	spectateY = 120,
	--- geometria da zona: cilindro translúcido bem mais alto que a arena
	zoneHeight = 260,
	zoneTransparency = 0.86,
	--- raio (em fração do raio da zona) onde os competidores nascem
	spawnRingFrac = 0.62,
	--- ponytail: TETO CONHECIDO — 20 Humanoids de servidor em aparelho fraco
	--- (GDD §19.2) nunca foi medido. O preenchimento vai até `maxPlayers` mas
	--- para neste teto. Sobe para `M.maxPlayers - 1` quando o playtest medir.
	maxBots = 11,
} :: { [string]: number }

local ZONE_COLOR = Color3.fromHex("#8A5CF0")

-- ------------------------------------------------------------------- estado
type PlayerStat = {
	kills: number,
	damage: number,
	--- elementos que CAUSARAM dano (é isso que o Selo conta como "usado")
	elements: { [string]: boolean },
	combos: { string },
}

local Match = {}

local terrain: any = nil
local combat: any = nil

local state: string = "Lobby"
local stateT = 0
local lobbyLeft = M.lobbyWait
local matchStartedAt = 0
local totalAtStart = 0

local zone = {
	phase = 0,
	--- "hold" (parada) ou "shrink" (fechando)
	sub = "hold",
	t = 0,
	dur = 0,
	center = Vector3.zero,
	radius = Z.startRadius,
	fromCenter = Vector3.zero,
	toCenter = Vector3.zero,
	fromRadius = Z.startRadius,
	toRadius = Z.startRadius,
	dps = 0,
}
local zonePart: BasePart? = nil

local matchId = ""
local eliminated: { [Player]: boolean } = {}
local stats: { [Player]: PlayerStat } = {}
--- último jogador a acertar cada humanoid (crédito de abate)
local lastHit: { [Humanoid]: { player: Player, at: number } } = {}
local deathWired: { [Humanoid]: boolean } = {}
local champion: any = nil
local seal: any = nil

local stateAcc = 0
local zoneAcc = 0
local rng = Random.new()

local evState: RemoteEvent
local evAnnounce: RemoteEvent

-- ------------------------------------------------------------------ helpers
local function flatDist(a: Vector3, b: Vector3): number
	local dx, dz = a.X - b.X, a.Z - b.Z
	return math.sqrt(dx * dx + dz * dz)
end

local function announceAll(key: string, ...)
	evAnnounce:FireAllClients({ key = key, args = { ... } })
end

local function announceTo(player: Player, key: string, ...)
	evAnnounce:FireClient(player, { key = key, args = { ... } })
end

-- ----------------------------------------------------------------- telemetria
--- Require preguiçoso e protegido (mesmo padrão do Combat→Sintonia): se o
--- coletor quebrar, a PARTIDA continua. Telemetria observa, nunca decide.
local telemetry: any = nil
local function tele(): any
	if telemetry == nil then
		local mod = ServerModules:FindFirstChild("Telemetry")
		local ok, m = pcall(function()
			return require(mod :: ModuleScript)
		end)
		telemetry = (ok and m) or false
	end
	return telemetry
end

local function emit(name: string, subject: any, payload: any?)
	local t = tele()
	if t and t.emit then
		pcall(t.emit, name, subject, payload)
	end
end

-- ------------------------------------------------- ELIMINADO (trava de ação)
--- P0 da auditoria: eliminado conjurava do poleiro de espectador. Quem sabe
--- quem foi eliminado é ESTE módulo; quem barra a intenção é o Combat. Toda
--- escrita em `eliminated` passa por aqui — é o único jeito de os dois nunca
--- discordarem.
local function markEliminated(player: Player)
	eliminated[player] = true
	combat.setCanAct(player, false)
end

local function unmarkEliminated(player: Player)
	eliminated[player] = nil
	combat.setCanAct(player, true)
end

local function clearEliminated()
	for player in pairs(eliminated) do
		unmarkEliminated(player)
	end
end

local function statOf(player: Player): PlayerStat
	local s = stats[player]
	if not s then
		s = { kills = 0, damage = 0, elements = {}, combos = {} }
		stats[player] = s
	end
	return s
end

--- Personagem vivo do jogador (ou nil): a única fonte de "está em jogo".
local function livePlayer(player: Player): (Humanoid?, BasePart?)
	local char = player.Character
	if not char or not char.Parent then
		return nil, nil
	end
	local hum = char:FindFirstChildOfClass("Humanoid")
	local root = char:FindFirstChild("HumanoidRootPart")
	if hum and root and hum.Health > 0 and root:IsA("BasePart") then
		return hum, root
	end
	return nil, nil
end

--- Todos os competidores vivos (jogadores em jogo + bots). É a lista que o
--- contador de vivos, o dano da zona e a mira dos bots consomem.
local function aliveEntities(): { Bots.Target }
	local list: { Bots.Target } = {}
	for _, player in ipairs(Players:GetPlayers()) do
		if eliminated[player] then
			continue
		end
		local hum, root = livePlayer(player)
		if hum and root then
			table.insert(list, { humanoid = hum, root = root })
		end
	end
	for _, bot in ipairs(Bots.list()) do
		if bot.humanoid.Health > 0 and bot.root.Parent then
			table.insert(list, { humanoid = bot.humanoid, root = bot.root })
		end
	end
	return list
end

local function alivePlayers(): { Player }
	local list: { Player } = {}
	for _, player in ipairs(Players:GetPlayers()) do
		if not eliminated[player] and (livePlayer(player)) then
			table.insert(list, player)
		end
	end
	return list
end

local function aliveCount(): number
	return #alivePlayers() + Bots.aliveCount()
end

-- --------------------------------------------------------------------- zona
local function radiusForPhase(i: number): number
	local t = math.clamp(i / Z.phases, 0, 1)
	return Z.startRadius + (Z.endRadius - Z.startRadius) * t
end

--- Novo centro sorteado DENTRO do círculo anterior, com folga suficiente para
--- o círculo novo caber inteiro no antigo (regra clássica de BR).
local function pickCenter(from: Vector3, fromRadius: number, toRadius: number): Vector3
	local slack = math.max(0, fromRadius - toRadius)
	if slack <= 0 then
		return from
	end
	local a = rng:NextNumber() * math.pi * 2
	local d = math.sqrt(rng:NextNumber()) * slack
	return from + Vector3.new(math.cos(a) * d, 0, math.sin(a) * d)
end

local function ensureZonePart(): BasePart
	if zonePart and zonePart.Parent then
		return zonePart
	end
	local p = Instance.new("Part")
	p.Name = "ArcaneZone"
	p.Anchored = true
	p.CanCollide = false
	p.CanQuery = false
	p.CanTouch = false
	p.CastShadow = false
	p.Shape = Enum.PartType.Cylinder
	p.Material = Enum.Material.SmoothPlastic -- cor chapada (direção blocky)
	p.Color = ZONE_COLOR
	p.Transparency = TUNE.zoneTransparency
	p.Parent = Workspace
	zonePart = p
	return p
end

--- Reflete centro/raio na geometria. O cilindro do Roblox deita no eixo X:
--- girar 90° em Z o deixa em pé, e o Size vira (altura, diâmetro, diâmetro).
local function syncZonePart()
	local p = ensureZonePart()
	local d = math.max(1, zone.radius * 2)
	p.Size = Vector3.new(TUNE.zoneHeight, d, d)
	p.CFrame = CFrame.new(zone.center.X, Grid.FLOOR_Y, zone.center.Z) * CFrame.Angles(0, 0, math.pi / 2)
end

local function beginPhase(i: number)
	zone.phase = i
	zone.sub = "hold"
	zone.t = 0
	zone.dur = Z.holdTime
	zone.fromRadius = zone.radius
	zone.toRadius = radiusForPhase(i)
	zone.fromCenter = zone.center
	zone.toCenter = pickCenter(zone.center, zone.fromRadius, zone.toRadius)
	zone.dps = Z.dpsPerPhase[math.clamp(i, 1, #Z.dpsPerPhase)] or 0
end

local function stepZone(dt: number)
	zone.t += dt
	if zone.sub == "hold" then
		if zone.t >= zone.dur then
			zone.sub = "shrink"
			zone.t = 0
			zone.dur = Z.shrinkTime
			announceAll("announce.zone.closing")
		end
		return
	end

	-- fechando: interpola centro e raio
	local k = if zone.dur > 0 then math.clamp(zone.t / zone.dur, 0, 1) else 1
	zone.radius = zone.fromRadius + (zone.toRadius - zone.fromRadius) * k
	zone.center = zone.fromCenter:Lerp(zone.toCenter, k)
	syncZonePart()
	Bots.setZone(zone.center, zone.radius)

	if k >= 1 then
		if zone.phase < Z.phases then
			beginPhase(zone.phase + 1)
		else
			-- morte súbita: o círculo final fica de pé com o DPS mais alto
			zone.sub = "hold"
			zone.t = 0
			zone.dur = math.huge
		end
	end
end

--- Dano de quem está fora do círculo. NUNCA toca Humanoid.Health: o Combat é
--- que sabe de escudo, i-frames e feedback (GDD §5).
local function applyZoneDamage()
	if zone.dps <= 0 then
		return
	end
	local amount = zone.dps * TUNE.zoneTick
	for _, e in ipairs(aliveEntities()) do
		if flatDist(e.root.Position, zone.center) > zone.radius then
			combat.applyDamage(e.humanoid, amount, nil, false)
		end
	end
end

-- ------------------------------------------------------------- spawn points
--- Anel de pontos ANDÁVEIS ao redor do centro da zona. Uma célula bloqueada
--- (árvore/rocha/água) empurra a busca para fora até achar chão livre.
local function spawnPoints(count: number): { Vector3 }
	local points: { Vector3 } = {}
	local baseR = Z.startRadius * TUNE.spawnRingFrac
	for i = 1, math.max(1, count) do
		local a = (i - 1) / math.max(1, count) * math.pi * 2
		local found: Vector3? = nil
		for attempt = 0, 12 do
			local r = baseR - attempt * Grid.CELL_SIZE * 2
			if r < Grid.CELL_SIZE then
				break
			end
			local p = zone.center + Vector3.new(math.cos(a) * r, 0, math.sin(a) * r)
			local cx, cy = Grid.worldToCell(p)
			if Grid.inBounds(cx, cy) and not terrain.isBlocking(cx, cy) then
				found = Grid.cellToWorld(cx, cy)
				break
			end
		end
		table.insert(points, found or zone.center)
	end
	return points
end

-- ------------------------------------------------------------- eliminações
local function perch(player: Player)
	local char = player.Character
	if not char then
		return
	end
	local root = char:FindFirstChild("HumanoidRootPart")
	char:PivotTo(CFrame.new(zone.center + Vector3.new(0, TUNE.spectateY, 0)))
	if root and root:IsA("BasePart") then
		root.Anchored = true -- espectador flutua: não volta para a partida
	end
end

local function nameOf(hum: Humanoid): string
	local char = hum.Parent
	local player = if char then Players:GetPlayerFromCharacter(char :: Instance) else nil
	if player then
		return player.DisplayName
	end
	return if char then char.Name else "?"
end

local function buildSeal(): any
	if not champion then
		return nil
	end
	local elementsUsed: { string } = {}
	local kills, damage, combos = 0, 0, {}
	local name, isBot = "?", false

	if typeof(champion) == "Instance" and champion:IsA("Player") then
		local player = champion :: Player
		name = player.DisplayName
		local s = statOf(player)
		kills, damage, combos = s.kills, math.floor(s.damage + 0.5), s.combos
		for _, id in ipairs(Elements.Order) do
			if s.elements[id] then
				table.insert(elementsUsed, id)
			end
		end
	else
		local bot = champion :: any
		name, isBot = bot.name, true
		kills, damage = bot.kills, math.floor(bot.damage + 0.5)
		elementsUsed = { bot.element }
	end

	-- GDD §18.6: card compartilhável — elementos usados, combos, dano, abates
	return {
		champion = name,
		isBot = isBot,
		kills = kills,
		damage = damage,
		elements = elementsUsed,
		combos = combos,
		duration = math.floor(os.clock() - matchStartedAt),
		total = totalAtStart,
	}
end

local function finish()
	if state ~= "Playing" then
		return
	end
	state = "Ended"
	stateT = 0

	local survivors = aliveEntities()
	champion = nil
	if #survivors == 1 then
		local hum = survivors[1].humanoid
		local char = hum.Parent
		local player = if char then Players:GetPlayerFromCharacter(char :: Instance) else nil
		if player then
			champion = player
		else
			for _, bot in ipairs(Bots.list()) do
				if bot.humanoid == hum then
					champion = bot
					break
				end
			end
		end
	end
	seal = buildSeal()
	emit(Events.Name.MatchEnd, champion, {
		match_id = matchId,
		duration = os.clock() - matchStartedAt,
		total = totalAtStart,
		seal = seal,
	})
	local t = tele()
	if t and t.matchEnd then
		pcall(t.matchEnd) -- fecha o relatório DEPOIS do último evento
	end
	announceAll("announce.match.end")
	local who = if seal then tostring(seal.champion) else "ninguém"
	print(string.format("[Match] fim de partida — campeão: %s", who))
end

local function onDeath(hum: Humanoid)
	if state ~= "Playing" then
		return
	end
	local victim = hum.Parent and Players:GetPlayerFromCharacter(hum.Parent :: Instance)
	if victim then
		markEliminated(victim) -- e a partir daqui ele não conjura mais
		announceTo(victim, "announce.death")
	end

	local credit = lastHit[hum]
	local killer: Player? = nil
	if credit and os.clock() - credit.at <= TUNE.killWindow and credit.player ~= victim then
		killer = credit.player
		if (killer :: Player).Parent then
			statOf(killer :: Player).kills += 1
			announceTo(killer :: Player, "announce.kill", nameOf(hum))
		end
	end
	lastHit[hum] = nil

	-- ELIMINAÇÃO ≠ KNOCK (que é do Combat): aqui é a saída definitiva da
	-- partida, e `place` é a colocação — o dado que responde "quem sobrevive".
	emit(Events.Name.Elimination, victim or hum, {
		by = killer,
		place = aliveCount() + 1,
		alive = aliveCount(),
		zone_phase = zone.phase,
	})

	if stateT >= TUNE.startGrace and aliveCount() <= 1 then
		finish()
	end
end

--- Liga o Died do humanoid uma única vez (personagens novos e bots).
local function wireDeath(hum: Humanoid)
	if deathWired[hum] then
		return
	end
	deathWired[hum] = true
	hum.Died:Connect(function()
		deathWired[hum] = nil
		onDeath(hum)
	end)
end

-- --------------------------------------------------------- ciclo da partida
local function startMatch()
	state = "Playing"
	stateT = 0
	matchStartedAt = os.clock()
	table.clear(stats)
	table.clear(lastHit)
	clearEliminated() -- todo mundo volta a poder agir
	champion = nil
	seal = nil
	Bots.clear()

	-- terreno novo a cada partida: a floresta queimada da rodada anterior não
	-- pode nascer carbonizada (mataria a pergunta de validação V3).
	terrain.build(Arena.generate())

	zone.center = Vector3.new(0, Grid.FLOOR_Y, 0)
	zone.radius = Z.startRadius
	beginPhase(1)
	syncZonePart()
	Bots.setZone(zone.center, zone.radius)

	local humans = Players:GetPlayers()
	local botCount = 0
	if M.fillWithBots then
		botCount = math.clamp(M.maxPlayers - #humans, 0, TUNE.maxBots)
	end
	totalAtStart = #humans + botCount

	local points = spawnPoints(math.max(1, totalAtStart))
	local slot = 0
	for _, player in ipairs(humans) do
		slot += 1
		local at = points[slot] + Vector3.new(0, 4, 0)
		-- personagem NOVO: vida/mana cheias vêm do bootstrap do Main, sem o
		-- módulo de partida escrever em Humanoid.Health.
		task.spawn(function()
			player:LoadCharacter()
			local char = player.Character
			if char then
				char:PivotTo(CFrame.new(at))
			end
		end)
	end

	local botPoints: { Vector3 } = {}
	for i = slot + 1, #points do
		table.insert(botPoints, points[i])
	end

	-- TIMES (P0): a ordem do roster decide as duplas e HUMANO VEM PRIMEIRO — é
	-- dupla de humanos que a pergunta V1 mede. Bot entra de parceiro só quando
	-- sobra humano ímpar, e o Humanoid dele é membro de time como qualquer um
	-- (senão a Sintonia não teria como saber de que lado ele está).
	local roster: { Teams.Member } = {}
	for _, player in ipairs(humans) do
		table.insert(roster, player)
	end
	if botCount > 0 then
		for _, bot in ipairs(Bots.spawn(botCount, botPoints)) do
			wireDeath(bot.humanoid)
			table.insert(roster, bot.humanoid)
		end
	end
	local teamCount = Teams.build(roster, Teams.DEFAULT_SIZE)

	matchId = string.format("m%x-%04x", os.time(), rng:NextInteger(0, 0xFFFF))
	local t = tele()
	if t and t.matchStart then
		pcall(t.matchStart, matchId)
	end
	emit(Events.Name.MatchStart, nil, {
		match_id = matchId,
		humans = #humans,
		bots = botCount,
		teams = teamCount,
		team_size = Teams.DEFAULT_SIZE,
	})

	announceAll("announce.match.start")
	print(
		string.format(
			"[Match] partida iniciada — %d magos (%d bots) em %d times de %d",
			totalAtStart,
			botCount,
			teamCount,
			Teams.DEFAULT_SIZE
		)
	)
end

local function toLobby()
	state = "Lobby"
	stateT = 0
	lobbyLeft = M.lobbyWait
	champion = nil
	seal = nil
	Bots.clear()
	Bots.setZone(Vector3.zero, math.huge)
	if zonePart then
		zonePart:Destroy()
		zonePart = nil
	end
	for player in pairs(eliminated) do
		-- tira o espectador do poleiro com um corpo NOVO (soltar a âncora o faria
		-- despencar 120 studs). Ele volta a competir na próxima partida.
		if player.Parent then
			task.spawn(function()
				player:LoadCharacter()
			end)
		end
	end
	clearEliminated()
	table.clear(lastHit)
	-- no lobby ninguém tem time: sem aliado, a Sintonia não dispara enquanto a
	-- próxima partida não montar os duos (lado seguro do erro).
	Teams.clear()
	announceAll("announce.match.waiting")
end

-- ---------------------------------------------------------------- broadcast
local function broadcast()
	local timeLeft = if zone.dur == math.huge then 0 else math.max(0, zone.dur - zone.t)
	if state == "Lobby" then
		timeLeft = math.max(0, lobbyLeft)
	elseif state == "Ended" then
		timeLeft = math.max(0, TUNE.endedTime - stateT)
	end
	local payload = {
		state = state,
		phase = zone.phase,
		phases = Z.phases,
		shrinking = zone.sub == "shrink",
		zoneCenter = zone.center,
		zoneRadius = zone.radius,
		zoneNextRadius = zone.toRadius,
		zoneDps = zone.dps,
		alive = aliveCount(),
		total = math.max(totalAtStart, 1),
		timeLeft = timeLeft,
		seal = seal,
	}

	-- PRESENÇA DA DUPLA (costura do coordenador, 18/08). Antes isto era um
	-- FireAllClients cego e o cliente só descobria o parceiro DEPOIS do primeiro
	-- pacto de Sintonia — o que enfraquece justamente a pergunta que o playtest
	-- existe para responder ("dois desconhecidos coordenam?"). Agora cada
	-- jogador recebe o SEU parceiro. É por jogador, então vai FireClient.
	-- Parceiro bot não tem UserId: some do painel de propósito (o cliente
	-- resolve por UserId), mas o duo continua valendo no servidor.
	for _, player in ipairs(Players:GetPlayers()) do
		local mate = nil
		for _, m in ipairs(Teams.matesOf(player)) do
			if typeof(m) == "Instance" and m:IsA("Player") then
				local hum = livePlayer(m)
				mate = {
					userId = m.UserId,
					name = m.DisplayName,
					element = if combat and combat.getElement then combat.getElement(m) else nil,
					hp = if hum then hum.Health else 0,
					hpMax = if hum then hum.MaxHealth else 0,
					alive = hum ~= nil and not eliminated[m],
				}
				break
			end
		end
		payload.mate = mate
		evState:FireClient(player, payload)
	end
end

-- --------------------------------------------------------------------- API
function Match.init(terrainModule: any, combatModule: any)
	terrain = terrainModule
	combat = combatModule
	evState = Net.event(Net.Names.MatchState)
	evAnnounce = Net.event(Net.Names.Announce)

	Bots.init(terrain, combat, aliveEntities)

	-- Observador de dano do Combat: alimenta o crédito de abate e os números do
	-- Selo do Campeão. É leitura pura — não altera o cálculo de dano.
	combat.onDamage(function(target: Humanoid, dealt: number, from: Player?)
		if from then
			local s = statOf(from)
			s.damage += dealt
			local element = combat.getElement(from)
			if element then
				s.elements[element] = true
			end
			lastHit[target] = { player = from, at = os.clock() }
		end
	end)

	local function onCharacter(player: Player, char: Model)
		local hum = char:WaitForChild("Humanoid", 10)
		if hum and hum:IsA("Humanoid") then
			wireDeath(hum)
		end
		if state == "Playing" and eliminated[player] then
			perch(player) -- renasceu eliminado: vira espectador, não competidor
		end
	end

	local function onPlayer(player: Player)
		if state == "Playing" then
			markEliminated(player) -- entrou no meio da prova: assiste esta
		end
		player.CharacterAdded:Connect(function(char)
			onCharacter(player, char)
		end)
		if player.Character then
			onCharacter(player, player.Character)
		end
	end

	Players.PlayerAdded:Connect(onPlayer)
	for _, player in ipairs(Players:GetPlayers()) do
		onPlayer(player)
	end
end

--- Combo de Sintonia executado (GDD §9) — entra no Selo do Campeão. Hook para a
--- raia R2 chamar quando a fusão acontecer; sem ela o Selo mostra 0 combos.
function Match.noteCombo(player: Player, comboName: string)
	if state ~= "Playing" or typeof(comboName) ~= "string" then
		return
	end
	table.insert(statOf(player).combos, comboName)
end

function Match.onPlayerRemoving(player: Player)
	unmarkEliminated(player)
	stats[player] = nil
	if state == "Playing" and stateT >= TUNE.startGrace and aliveCount() <= 1 then
		finish()
	end
end

function Match.step(dt: number)
	stateT += dt

	if state == "Lobby" then
		local n = #Players:GetPlayers()
		-- com bots ligados, UM mago já forma partida (GDD §19.6: o novato joga
		-- na hora); sem bots, respeita minPlayersToStart.
		local ready = n >= M.minPlayersToStart or (M.fillWithBots and n >= 1)
		if ready then
			lobbyLeft -= dt
			if lobbyLeft <= 0 then
				startMatch()
			end
		else
			lobbyLeft = M.lobbyWait
		end
	elseif state == "Playing" then
		stepZone(dt)
		zoneAcc += dt
		while zoneAcc >= TUNE.zoneTick do
			zoneAcc -= TUNE.zoneTick
			applyZoneDamage()
			-- rede de segurança a 2 Hz (o caso instantâneo já é tratado no
			-- Died): cobre quem saiu do servidor ou sumiu sem morrer.
			if stateT >= TUNE.startGrace and aliveCount() <= 1 then
				finish()
			end
		end
		if state == "Playing" then
			Bots.step(dt)
		end
	elseif state == "Ended" then
		if stateT >= TUNE.endedTime then
			toLobby()
		end
	end

	stateAcc += dt
	if stateAcc >= TUNE.stateInterval then
		stateAcc = 0
		broadcast()
	end
end

-- ============================================================================
-- AUTOTESTE — cole no Command Bar do Studio (servidor):
--   require(game.ServerScriptService.Server.Match).selfTest()
-- Cobre a matemática que não dá para ver a olho: a escada de raios da zona e a
-- garantia de que o círculo novo cabe dentro do anterior.
-- ⚠ RODE NO LOBBY: a parte de times monta e limpa o Teams DE VERDADE — no meio
-- de uma partida isto apagaria os duos e desligaria a Sintonia até a próxima.
-- ============================================================================
function Match.selfTest()
	assert(radiusForPhase(0) == Z.startRadius, "fase 0 = raio inicial")
	assert(math.abs(radiusForPhase(Z.phases) - Z.endRadius) < 1e-6, "última fase = raio final")
	for i = 1, Z.phases do
		assert(radiusForPhase(i) < radiusForPhase(i - 1), "a zona só encolhe")
	end
	assert(#Z.dpsPerPhase >= Z.phases, "dpsPerPhase precisa cobrir todas as fases")

	-- o círculo novo tem de caber inteiro no antigo, em 200 sorteios
	for i = 1, 200 do
		local from = Vector3.new(0, 0, 0)
		local r0, r1 = 100, 40
		local c = pickCenter(from, r0, r1)
		assert(flatDist(c, from) + r1 <= r0 + 1e-6, "círculo novo saiu do anterior")
	end

	local total = Z.phases * (Z.holdTime + Z.shrinkTime)
	assert(total >= 120 and total <= 480, "partida fora da janela de 2–8 min: " .. total)

	-- TIMES (P0): o roster de partida é a mistura de jogadores e bots, então o
	-- teste usa Humanoids de mentira — para o Teams os dois são membros iguais.
	local roster: { Teams.Member } = {}
	for _ = 1, 5 do
		table.insert(roster, Instance.new("Humanoid"))
	end
	local teams = Teams.build(roster, Teams.DEFAULT_SIZE)
	assert(teams == 3, "5 membros em duos = 3 times (o ímpar sobra sozinho), achei " .. teams)
	assert(Teams.areAllies(roster[1], roster[2]), "os dois primeiros formam duo")
	assert(not Teams.areAllies(roster[2], roster[3]), "duos vizinhos NÃO são aliados")
	assert(#Teams.matesOf(roster[1]) == 1, "num duo cada um tem exatamente 1 parceiro")
	assert(Teams.teamOf(roster[5]) ~= nil, "o ímpar tem time próprio (não fica sem time)")
	assert(#Teams.matesOf(roster[5]) == 0, "o ímpar não tem com quem fundir — e é isso mesmo")
	Teams.clear()
	assert(Teams.teamOf(roster[1]) == nil, "clear apaga os times")
	for _, d in ipairs(roster) do
		local dummy = d :: Humanoid
		dummy:Destroy()
	end

	print(string.format("[Match] autoteste OK — partida cheia dura ~%ds", total))
	return true
end

return Match
