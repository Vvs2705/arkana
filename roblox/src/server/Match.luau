--!strict
-- ============================================================================
-- RAIA (R3) — LOOP DE BATTLE ROYALE: lobby, início, zona arcana fechando,
-- eliminações, bots de preenchimento (GDD §19.6) e Selo do Campeão (GDD §18.6).
-- Escopo do GDD §19.2: 20 jogadores, partidas curtas (4–6 min).
--
-- AUTORIDADE (docs/ROBLOX.md §3): a partida INTEIRA é decidida aqui. O cliente
-- só recebe Net.MatchState/Net.Announce e apresenta. A ÚNICA intenção C→S que
-- entra aqui é o "arrepio" do espírito (Match.spiritPing) — e ela não muda a
-- partida: marca uma posição. Roteada no próprio `init`, pelo mesmo motivo que
-- o Combat roteia a postura: o remote não está em Net.Names (Shared é do
-- COORDENADOR) e Net.event aceita nome livre. Oficializar depois.
--
-- FRONTEIRA DE DANO: a zona NUNCA escreve em Humanoid.Health. Ela calcula o DPS
-- da fase e manda Combat.applyDamage decidir (escudo, i-frames, número na tela)
-- — a mesma fronteira que o Terrain respeita com `hazardAt`.
--
-- RETORNO CONTESTÁVEL (GDD §16.6 + §18.6): morrer cedo não encerra a sessão.
-- Quem cai vira ESPÍRITO (não conjura, acompanha a dupla, dá UM arrepio); o
-- parceiro recolhe a ESSÊNCIA e canaliza num ALTAR fixo e visível. O ritual é
-- denunciado (cor + anúncio), só vale ANTES da fase final da zona e UMA vez por
-- dupla, e devolve o jogador com KIT BASE. Traz o jogador de volta sem tornar a
-- morte irrelevante — e cria disputa por altar em vez de menu de espectador.
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
	--- TETO DURO DE DURAÇÃO (alpha público). A fase final da zona NÃO fecha até
	--- zero: ela para em `Z.endRadius` e segura com `dur = math.huge`. Duas duplas
	--- paradas dentro desse círculo — AFK, cliente travado, griefer parado de
	--- propósito — deixam a máquina de estados em `Playing` PARA SEMPRE. Medido
	--- no cenário de robustez: 900 s simulados, 4 AFK colados no centro, ainda
	--- `Playing` com teamsAlive=2, e nenhuma partida nova nunca mais naquele
	--- servidor. Estes dois números NÃO são balanceamento: multiplicam o
	--- cronograma que o Balance já define (fator + folga), então mudam junto com
	--- a variante de ritmo e no jogo normal nunca são alcançados (partida cheia
	--- ~330 s contra teto de ~490 s).
	overtimeFactor = 1.35,
	overtimeSlack = 45,
	--- ARREPIO — janela mínima entre dois pedidos do MESMO espírito. O gasto do
	--- arrepio só acontece quando ele ACHA alguém: sem esta janela um cliente
	--- hostil pede sem parar de um lugar vazio e cada pedido varre a lista
	--- inteira de vivos de graça (é remote C→S, logo é fronteira de confiança).
	pingCooldown = 0.5,
} :: { [string]: number }

-- --- RITUAL DE RESSURREIÇÃO (GDD §16.6 + §18.6) -----------------------------
--- Regra do projeto: "números viram dados; o código lê dados". Estes ainda não
--- têm campo em Balance (`Balance.match.revive` é do COORDENADOR), então a
--- leitura JÁ está pronta e o padrão daqui só vale enquanto o campo não existe.
--- Rebalancear o ritual = criar `Balance.match.revive`, sem tocar neste arquivo.
local RV: any = (M :: any).revive or {}
local REVIVE = {
	--- canalização no altar. O GDD §16.6 diz 7 s; a auditoria pediu ~6 s —
	--- longo o bastante para ser contestável, curto o bastante para tentar.
	channel = RV.channel or 6.0,
	--- raio do altar: sair daqui ZERA a canalização (não pausa)
	radius = RV.radius or 12,
	--- distância para o aliado recolher a essência do chão
	pickup = RV.pickup or 10,
	--- quantos altares nascem no mapa. Pontos FIXOS (GDD §16.6): o inimigo sabe
	--- onde o revive vai acontecer — é isso que transforma o ritual em disputa.
	altars = RV.altars or 4,
	--- fração do raio inicial da zona onde o anel de altares fica
	ringFrac = RV.ringFrac or 0.45,
	--- altura em que o espírito flutua sobre a essência. Projétil voa na
	--- HORIZONTAL na altura do peito: lá em cima o espírito não vira escudo.
	hoverY = RV.hoverY or 12,
	--- alcance do arrepio, medido a partir do próprio espírito (não do mapa)
	pingRange = RV.pingRange or 70,
	--- segundos que o inimigo revelado fica marcado para a dupla (GDD §18.6)
	pingHold = RV.pingHold or 2.0,
}

local ZONE_COLOR = Color3.fromHex("#8A5CF0")
local ALTAR_COLOR = Color3.fromHex("#C9A227")
--- cor do altar EM RITUAL: a denúncia visual que o inimigo lê de longe
local ALTAR_HOT = Color3.fromHex("#FF3B6B")
local ESSENCE_COLOR = Color3.fromHex("#7FE8FF")

-- ------------------------------------------------------------------- estado
type PlayerStat = {
	kills: number,
	damage: number,
	--- elementos que CAUSARAM dano (é isso que o Selo conta como "usado")
	elements: { [string]: boolean },
	combos: { string },
}

--- Estado de um caído que ainda PODE voltar. Existe do knock até o retorno (ou
--- até a fase final fechar o ritual). Um por jogador — uma dupla tem no máximo
--- um retorno, e é `revivesUsed` que garante isso.
type Spirit = {
	--- time no instante da queda (o limite é POR DUPLA, não por jogador)
	team: number?,
	--- a essência: objeto do mundo, é ela que o aliado carrega
	orb: BasePart?,
	--- quem está levando (nil = caída no chão, esperando ser recolhida)
	carrier: Player?,
	--- segundos acumulados de canalização no altar
	channel: number,
	--- arrepios restantes (GDD §18.6: UM)
	ping: number,
	--- `os.clock()` mínimo do próximo pedido de arrepio (anti-spam do remote)
	nextPingAt: number,
}

type Altar = { pos: Vector3, pad: BasePart, beam: BasePart, hot: boolean }

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

--- Tabela NOVA com chaves fracas (mesmo padrão do Teams: nasce com o metatable
--- em vez de depender de `table.clear` conviver com ele).
local function weakKeyed(): any
	return setmetatable({}, { __mode = "k" })
end

local matchId = ""
--- Quem está FORA da contagem desta prova — e POR QUÊ. O valor é o motivo, não
--- um `true`: "late" = entrou com a prova em andamento (não perdeu nada, joga a
--- próxima) · "out" = caiu nesta prova. As duas situações são opostas para quem
--- lê a tela, e um booleano não tem como dizer qual é. Toda leitura antiga
--- continua valendo sem mudar uma linha (string não-vazia é verdadeira em Lua),
--- e o motivo NÃO é estado novo por jogador: mora na mesma tabela que já é
--- limpa nas duas portas de partida nova (`startMatch` e `toLobby`, ambas por
--- `clearEliminated`) — que é exatamente onde os três vazamentos anteriores do
--- projeto passaram.
local eliminated: { [Player]: string } = {}
local stats: { [Player]: PlayerStat } = {}
--- último jogador a acertar cada humanoid (crédito de abate).
--- CHAVES FRACAS (mesmo motivo do `deathWired`): quem leva um tiro e depois SAI
--- do servidor — ou é um bot que o `Bots.clear` destrói sem morrer — deixava o
--- Humanoid preso aqui até o próximo reset, e segurar um Humanoid segura o Model
--- inteiro junto. Num servidor de alpha que roda por horas, isso é lixo vivo
--- entre a eliminação e o fim da partida por gente que só passou pelo servidor.
local lastHit: { [Humanoid]: { player: Player, at: number } } = weakKeyed()
--- CHAVES FRACAS (mesmo motivo do Teams): a marca de "já liguei o Died deste
--- humanoid" só some quando ele morre. Corpo que a partida trocou sem morrer —
--- todo jogador a cada `startMatch`, todo bot que o `Bots.clear` destrói —
--- ficava aqui para sempre, e segurar um Humanoid segura o Model inteiro junto:
--- ~20 personagens vazando por partida num servidor que nunca reinicia.
local deathWired: { [Humanoid]: boolean } = weakKeyed()
local champion: any = nil
local seal: any = nil

--- caídos que ainda podem voltar, e as duplas que JÁ gastaram o retorno
local spirits: { [Player]: Spirit } = {}
local revivesUsed: { [number]: boolean } = {}
local altars: { Altar } = {}
--- nº de bots fixado pelo harness de playtest (Match.forceStart); nil = normal
local forcedBots: number? = nil

local stateAcc = 0
local zoneAcc = 0
local rng = Random.new()

local evState: RemoteEvent
local evAnnounce: RemoteEvent
local evSpirit: RemoteEvent

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
local function markEliminated(player: Player, reason: string)
	eliminated[player] = reason
	combat.setCanAct(player, false)
end

--- O QUE A TELA DESTE JOGADOR MOSTRA: `nil` = está jogando · "late" = entrou no
--- meio da prova · "out" = caiu nesta prova.
---
--- ESPÍRITO NÃO É ESPECTADOR — é o terceiro estado, e é por isso que esta função
--- existe em vez de o `broadcast` ler `eliminated` direto. Quem caiu mas ainda
--- pode voltar pelo altar está fora da CONTAGEM, não fora da PARTIDA: dizer
--- "você caiu nesta prova" para ele seria mentir sobre a única coisa que ainda
--- depende do parceiro atravessar o mapa. O espírito tem a própria apresentação
--- (essência, arrepio, painel da dupla) e nunca cai no painel de poleiro.
---
--- PURA de propósito: é assim que o autoteste cobre os três estados sem partida.
local function spectateKind(reason: string?, isSpirit: boolean): string?
	if isSpirit then
		return nil
	end
	return reason
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

--- Os vivos como MEMBROS DE TIME. Não dá para reaproveitar `aliveEntities`: a
--- chave de time de um jogador é o Player, e a de um bot é o Humanoid.
local function aliveMembers(): { Teams.Member }
	local list: { Teams.Member } = {}
	for _, player in ipairs(alivePlayers()) do
		table.insert(list, player)
	end
	for _, bot in ipairs(Bots.list()) do
		if bot.humanoid.Health > 0 and bot.root.Parent then
			table.insert(list, bot.humanoid)
		end
	end
	return list
end

--- Quantos TIMES distintos há nesta lista. Em duos é esta a informação útil
--- ("3 times restantes" > "5 magos vivos") — a UI mostra isto, não o outro.
--- Recebe a lista de fora só para o autoteste conseguir chamá-la sem partida.
local function countTeams(members: { Teams.Member }): number
	local seen: { [number]: boolean } = {}
	local count = 0
	local loose = 0
	for _, member in ipairs(members) do
		local id = Teams.teamOf(member)
		if id == nil then
			--- fora de partida (lobby) ninguém tem time: cada um conta por si,
			--- senão a UI anunciaria "0 times" com gente viva na tela
			loose += 1
		elseif not seen[id] then
			seen[id] = true
			count += 1
		end
	end
	return count + loose
end

-- --------------------------------------------------------------------- zona
local function radiusForPhase(i: number): number
	local t = math.clamp(i / Z.phases, 0, 1)
	return Z.startRadius + (Z.endRadius - Z.startRadius) * t
end

--- Teto DURO de duração da partida (ver `TUNE.overtimeFactor`). DERIVADO do
--- cronograma que o Balance já define: muda junto com a variante de ritmo e não
--- cria número novo para alguém manter em dois lugares.
local function matchTimeLimit(): number
	return Z.phases * (Z.holdTime + Z.shrinkTime) * TUNE.overtimeFactor + TUNE.overtimeSlack
end

--- A partida tem de acabar AGORA, sem esperar a zona resolver? As duas causas
--- são de servidor público, e nenhuma delas o BR resolve sozinho:
---   · servidor VAZIO — não se simula battle royale para ninguém, e quem entrar
---     depois nasceria espectador de uma partida de bots;
---   · TETO DE TEMPO — a fase final da zona segura para sempre; sem teto, gente
---     parada dentro do círculo final trava `Playing` e o servidor inteiro junto.
--- PURA de propósito: é assim que o autoteste cobre as duas regras sem partida.
local function mustEndNow(humans: number, elapsed: number): boolean
	return humans <= 0 or elapsed >= matchTimeLimit()
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

-- ============================================================================
-- RITUAL DE RESSURREIÇÃO — Espírito → Essência → Altar → Retorno
-- (GDD §16.6 "Círculo de Invocação" + §18.6 "Espírito Errante")
--
-- POR QUE ISTO EXISTE: sem retorno, morrer aos 40 s de uma partida de 5 min
-- termina a sessão do jogador — e um jogo que quer ENSINAR coordenação de dupla
-- não pode tirar metade da dupla do ar no primeiro erro. Free Fire, Apex e
-- Fortnite resolvem com retorno CONTESTÁVEL: quem volta, volta porque o
-- parceiro atravessou o mapa e sobreviveu a um ritual denunciado.
--
-- OS QUATRO FREIOS que impedem "morte não importa": custo de tempo (6 s parado),
-- custo de posição (altar é ponto FIXO — dá para campear), custo de progresso
-- (volta com kit base) e teto duro (uma vez por dupla, e nunca na fase final).
-- ============================================================================

--- Peça de mundo do ritual (altar e essência). Sem colisão e sem query: nada
--- aqui empurra jogador nem explode projétil — o ritual é ALVO, não obstáculo.
local function neonPart(name: string, size: Vector3, cf: CFrame, transparency: number): BasePart
	local p = Instance.new("Part")
	p.Name = name
	p.Anchored = true
	p.CanCollide = false
	p.CanQuery = false
	p.CanTouch = false
	p.CastShadow = false
	p.Material = Enum.Material.Neon
	p.Color = ALTAR_COLOR
	p.Transparency = transparency
	p.Size = size
	p.CFrame = cf
	p.Parent = Workspace
	return p
end

--- Ponto ANDÁVEL mais próximo, caminhando em direção ao centro da arena (mesmo
--- truque do `spawnPoints`). Altar dentro de uma rocha seria altar inalcançável
--- — e o mapa é sorteado a cada partida, então isto não é caso raro.
local function walkableNear(pos: Vector3): Vector3
	local toCenter = Vector3.new(-pos.X, 0, -pos.Z)
	local step = if toCenter.Magnitude > 1e-3
		then toCenter.Unit * Grid.CELL_SIZE
		else Vector3.new(Grid.CELL_SIZE, 0, 0)
	local p = pos
	for _ = 0, 10 do
		local cx, cy = Grid.worldToCell(p)
		if Grid.inBounds(cx, cy) and not terrain.isBlocking(cx, cy) then
			return Grid.cellToWorld(cx, cy)
		end
		p += step
	end
	return pos
end

local function clearAltars()
	for _, a in ipairs(altars) do
		a.pad:Destroy()
		a.beam:Destroy()
	end
	table.clear(altars)
end

--- Anel FIXO de altares. Fixo é o ponto do design: o inimigo aprende os quatro
--- lugares do mapa onde alguém pode voltar, e passa a vigiá-los.
local function buildAltars()
	clearAltars()
	local r = Z.startRadius * REVIVE.ringFrac
	for i = 1, REVIVE.altars do
		local a = (i - 0.5) / REVIVE.altars * math.pi * 2
		local at = walkableNear(Vector3.new(math.cos(a) * r, Grid.FLOOR_Y, math.sin(a) * r))
		local d = REVIVE.radius * 2
		-- círculo no chão: a ÁREA em que o ritual acontece, do tamanho exato do
		-- raio de canalização (o jogador lê a regra sem tutorial).
		local pad = neonPart(
			"CirculoDeInvocacao",
			Vector3.new(0.4, d, d),
			CFrame.new(at.X, Grid.FLOOR_Y + 0.3, at.Z) * CFrame.Angles(0, 0, math.pi / 2),
			0.45
		)
		pad.Shape = Enum.PartType.Cylinder
		-- pilar: é o que se vê do outro lado do mapa ("visível" do GDD §16.6)
		local beam = neonPart(
			"FarolDoAltar",
			Vector3.new(2, 90, 2),
			CFrame.new(at.X, Grid.FLOOR_Y + 45, at.Z),
			0.62
		)
		table.insert(altars, { pos = at, pad = pad, beam = beam, hot = false })
	end
end

--- Acende/apaga o altar. Só escreve na TRANSIÇÃO: pulsar a transparência todo
--- frame replicaria 4 propriedades × 60 Hz para 20 clientes por nada.
local function setAltarHot(a: Altar, hot: boolean)
	if a.hot == hot then
		return
	end
	a.hot = hot
	local color = if hot then ALTAR_HOT else ALTAR_COLOR
	a.pad.Color = color
	a.beam.Color = color
	a.beam.Transparency = if hot then 0.15 else 0.62
	a.pad.Transparency = if hot then 0.15 else 0.45
end

--- O retorno ainda é possível para esta dupla? PURA de propósito (o autoteste
--- chama sem partida): a fase da zona e o teto de uma vez por dupla são os dois
--- limites que impedem o revive de tornar a morte irrelevante.
--- O corte é por FASE (`zone.phase`), NUNCA por segundo absoluto — assim ele
--- significa a mesma coisa nas três variantes de ritmo do Balance.
local function reviveAllowed(team: number?): boolean
	return zone.phase < Z.phases and team ~= nil and not revivesUsed[team]
end

--- Corpo de ESPÍRITO: translúcido, sem colisão e ANCORADO. Ancorar não é
--- economia — é o que impede o morto de virar batedor voando pelo mapa. Ele só
--- acompanha a própria essência, e a altura o mantém fora do alcance dos
--- projéteis (que voam na horizontal): espírito não serve de escudo humano.
local function ghostify(player: Player)
	local char = player.Character
	if not char then
		return
	end
	for _, d in ipairs(char:GetDescendants()) do
		if d:IsA("BasePart") then
			d.CanCollide = false
			d.CanQuery = false
			d.Anchored = true
			if d.Transparency < 0.65 then
				d.Transparency = 0.65
			end
		end
	end
end

local function clearSpirit(player: Player)
	local sp = spirits[player]
	if not sp then
		return
	end
	if sp.orb then
		sp.orb:Destroy()
	end
	spirits[player] = nil
end

local function clearAllSpirits()
	for player in pairs(spirits) do
		clearSpirit(player)
	end
end

--- Queda de quem ainda pode voltar: nasce a essência no lugar do corpo. Se a
--- dupla já gastou o retorno (ou a zona entrou na fase final), NÃO nasce nada e
--- o jogador segue o caminho antigo — espectador no poleiro.
local function makeSpirit(player: Player, at: Vector3)
	local team = Teams.teamOf(player)
	if not reviveAllowed(team) then
		return
	end
	local orb = neonPart("EssenciaArcana", Vector3.new(2.5, 2.5, 2.5), CFrame.new(at + Vector3.new(0, 3, 0)), 0.15)
	orb.Shape = Enum.PartType.Ball
	orb.Color = ESSENCE_COLOR
	spirits[player] = { team = team, orb = orb, carrier = nil, channel = 0, ping = 1, nextPingAt = 0 }
	announceTo(player, "announce.revive.spirit")
end

--- Altar em que este jogador está pisando (nil = fora de qualquer área).
local function altarNear(player: Player): Altar?
	local _, root = livePlayer(player)
	if not root then
		return nil
	end
	for _, a in ipairs(altars) do
		if flatDist(root.Position, a.pos) <= REVIVE.radius then
			return a
		end
	end
	return nil
end

--- Ritual concluído: o jogador VOLTA, o poder não (GDD §16.6).
local function completeRevive(player: Player, sp: Spirit, at: Vector3)
	local carrier = sp.carrier
	local team = sp.team
	if team then
		revivesUsed[team] = true -- teto duro: uma vez por dupla, por partida
	end
	clearSpirit(player)
	unmarkEliminated(player) -- volta a poder conjurar (a trava é do Combat)

	-- KIT BASE: sem escudo evoluído, sem progresso. Quem sabe de escudo/evolução
	-- é o Combat — o Match só PEDE. Chamado ANTES do corpo novo de propósito: o
	-- `onCharacterAdded` do Combat enche o escudo no cap do nível ATUAL, que
	-- depois deste reset já é o nível 1.
	-- (o TETO CONHECIDO que estava anotado aqui caiu: `Combat.resetProgress` já
	-- existe e zera evolução, nível e carga do escudo — o freio "volta o jogador,
	-- não o poder" é mecânico agora, não mais promessa de documento. A guarda
	-- fica porque a chamada é opcional por contrato entre as raias.)
	if combat.resetProgress then
		combat.resetProgress(player)
	end

	task.spawn(function()
		player:LoadCharacter()
		local char = player.Character
		if char then
			char:PivotTo(CFrame.new(at + Vector3.new(0, 4, 0)))
		end
	end)

	announceAll("announce.revive.done", player.DisplayName)
	emit(Events.Name.ReviveComplete, player, {
		by = carrier,
		team = team,
		zone_phase = zone.phase,
		channel = REVIVE.channel,
	})
end

--- Um passo do ritual. É o ÚNICO lugar que mexe em `spirits` durante a partida:
--- recolhimento, transporte, canalização e retorno moram juntos porque são o
--- mesmo estado — separar em módulos daria três lugares para dessincronizar.
local function stepRevive(dt: number)
	local open = zone.phase < Z.phases
	local hot: { [Altar]: boolean } = {}

	for player, sp in pairs(spirits) do
		local orb = sp.orb
		if not player.Parent or not (orb and orb.Parent) then
			clearSpirit(player)
			continue
		end

		-- carregador precisa continuar VIVO e em jogo; se cair, a essência fica
		-- onde ele estava (é o momento em que o revive vira contra-ataque).
		local carrier = sp.carrier
		if carrier and not (carrier.Parent and not eliminated[carrier] and (livePlayer(carrier))) then
			sp.carrier = nil
			sp.channel = 0
		end

		if sp.carrier then
			local _, root = livePlayer(sp.carrier :: Player)
			if root then
				orb.CFrame = root.CFrame * CFrame.new(0, 5, 0)
			end
		else
			-- no chão: qualquer ALIADO VIVO que chegue perto recolhe. Sem botão:
			-- num jogo de polegar, "chegar perto" já é a intenção.
			for _, mate in ipairs(Teams.matesOf(player)) do
				if typeof(mate) == "Instance" and mate:IsA("Player") and not eliminated[mate] then
					local _, root = livePlayer(mate)
					if root and flatDist(root.Position, orb.Position) <= REVIVE.pickup then
						sp.carrier = mate
						announceTo(mate, "announce.revive.carrying")
						break
					end
				end
			end
		end

		-- o espírito flutua sobre a própria essência: antes de recolhida, sobre
		-- o corpo; depois, junto do parceiro. Uma posição só para as duas coisas.
		local char = player.Character
		local _, ghostRoot = livePlayer(player)
		if char and ghostRoot then
			char:PivotTo(CFrame.new(orb.Position + Vector3.new(0, REVIVE.hoverY, 0)))
		end

		-- TETO POR DUPLA aplicado TAMBÉM aqui, não só no nascimento da essência:
		-- `makeSpirit` barra a segunda queda, mas uma essência que já estava no
		-- chão quando o retorno foi gasto continuava canalizável. Em duos isso
		-- nunca acontece (só há um parceiro para carregar); em qualquer time
		-- maior que 2 é a brecha do segundo revive. `reviveAllowed` é a mesma
		-- regra pura que o teste cobre — um lugar só para a lei.
		local altar = if reviveAllowed(sp.team) and sp.carrier then altarNear(sp.carrier :: Player) else nil
		if altar then
			if sp.channel <= 0 then
				-- ÁREA DENUNCIADA: o altar acende e TODO MUNDO ouve. O ritual não
				-- é escapatória silenciosa — é um convite para brigar aqui.
				announceAll("announce.revive.channel")
				emit(Events.Name.ReviveStart, sp.carrier, {
					spirit = player,
					team = sp.team,
					zone_phase = zone.phase,
					channel = REVIVE.channel,
				})
			end
			hot[altar] = true
			sp.channel += dt
			if sp.channel >= REVIVE.channel then
				completeRevive(player, sp, altar.pos)
			end
		elseif sp.channel > 0 then
			sp.channel = 0 -- saiu da área: recomeça do zero, não pausa
		end
	end

	for _, a in ipairs(altars) do
		setAltarHot(a, hot[a] == true)
	end

	-- FASE FINAL: o ritual fecha para todo mundo e os espíritos pendentes viram
	-- espectadores. É o mesmo corte por FASE do `reviveAllowed` — aplicado
	-- continuamente, ele também é o prazo de validade da essência.
	if not open and next(spirits) ~= nil then
		for player in pairs(spirits) do
			clearSpirit(player)
			perch(player)
		end
	end
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

--- FIM DE PARTIDA EM DUOS (decisão do coordenador, 18/08): a partida acaba
--- quando resta UMA DUPLA, não um jogador. Com a contagem antiga, a última
--- dupla viva ficava esperando a zona matar um dos dois — o vencedor tinha de
--- perder o parceiro para ganhar, o que é o oposto de um jogo sobre cooperar.
--- Cai para a contagem de jogadores quando não há times montados (solo/lobby).
local function matchOver(): boolean
	local members = aliveMembers()
	local teams = countTeams(members)
	if teams > 0 then
		return teams <= 1
	end
	return #members <= 1
end

--- Sujeito do Selo por trás de um Humanoid: o Player quando existe, senão o bot.
--- Devolve nil para corpo que não é competidor (alvo de tutorial, por exemplo).
local function subjectOf(hum: Humanoid): any
	local char = hum.Parent
	local player = if char then Players:GetPlayerFromCharacter(char :: Instance) else nil
	if player then
		return player
	end
	for _, bot in ipairs(Bots.list()) do
		if bot.humanoid == hum then
			return bot
		end
	end
	return nil
end

--- Ordena candidatos a campeão: abate pesa mais que dano, dano desempata.
--- Player e bot têm os mesmos dois números — só moram em lugares diferentes.
local function championScore(subject: any): number
	if typeof(subject) == "Instance" then
		local s = statOf(subject :: Player)
		return s.kills * 1000 + s.damage
	end
	return (subject.kills or 0) * 1000 + (subject.damage or 0)
end

--- Campeão entre os sobreviventes. Recebe a lista pronta (e não a partida) só
--- para o autoteste conseguir chamá-la sem Studio — mesmo truque do `countTeams`.
local function pickChampion(subjects: { any }): any
	local best, who = -1, nil
	for _, subject in ipairs(subjects) do
		local score = championScore(subject)
		if score > best then
			best, who = score, subject
		end
	end
	return who
end

local function finish()
	if state ~= "Playing" then
		return
	end
	state = "Ended"
	stateT = 0

	-- CAMPEÃO NO FIM POR DUPLA (costura quebrada, 18/08): desde que `matchOver`
	-- passou a contar TIMES, o desfecho NORMAL do jogo deixa DOIS vivos. A
	-- checagem antiga (`#survivors == 1`) só nomeava campeão no caso raro do
	-- sobrevivente solitário — na vitória mais comum o Selo do Campeão (GDD
	-- §18.6) simplesmente não existia e a tela de fim mostrava "sem campeão".
	-- Agora o Selo sai do MELHOR sobrevivente (abates, depois dano).
	local subjects: { any } = {}
	for _, e in ipairs(aliveEntities()) do
		local subject = subjectOf(e.humanoid)
		if subject then
			table.insert(subjects, subject)
		end
	end
	champion = pickChampion(subjects)
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
	-- QUEM JÁ SAIU NÃO SAI DE NOVO. O corpo de espectador/espírito continua
	-- sujeito ao perigo de célula (o hazard do terreno é por célula e ignora
	-- altura), e sem este guard cada morte repetida contaria colocação, abate e
	-- `place` — envenenando exatamente o dado que a auditoria quer ler.
	if victim and eliminated[victim] then
		return
	end
	if victim then
		markEliminated(victim, "out") -- e a partir daqui ele não conjura mais
		announceTo(victim, "announce.death")
		-- ESPÍRITO (GDD §18.6): se a dupla ainda tem retorno e a zona não chegou
		-- na fase final, a essência fica no chão e a sessão dele continua.
		local root = hum.RootPart
		makeSpirit(victim, if root then root.Position else zone.center)
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

	if stateT >= TUNE.startGrace and matchOver() then
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
--- O QUE NUNCA ATRAVESSA UMA PARTIDA: o progresso do Escudo Evolutivo (GDD §5) e
--- o cooldown compartilhado da Sintonia. Os dois vazamentos foram fechados na
--- revisão de 18/08 — mas a correção morava só no `toLobby`, e existem DUAS
--- portas para uma partida nova: o ciclo normal (Ended → Lobby → startMatch) e o
--- `Match.forceStart` chamado direto da tela de fim (Playtest/harness), que pula
--- o lobby inteiro. Pela segunda porta os dois vazamentos voltavam: quem
--- terminou no nível 4 começava a próxima já dourado, e quem fundiu no fim
--- entrava travado por 24 s. Uma função, chamada nas duas portas.
local function resetCrossMatchState()
	-- Vem ANTES de qualquer `LoadCharacter`: o `onCharacterAdded` do Combat enche
	-- o escudo no cap do nível ATUAL, que depois deste reset já é o nível 1.
	if combat and combat.resetProgress then
		for _, player in ipairs(Players:GetPlayers()) do
			combat.resetProgress(player)
		end
	end
	-- Require preguiçoso e protegido (padrão do projeto): se a raia R2 ainda não
	-- expôs `resetMatch`, isto vira no-op. FindFirstChild (não WaitForChild):
	-- roda dentro do Heartbeat e módulo ausente não pode travar o loop mestre.
	local okS, S = pcall(require, ServerModules:FindFirstChild("Sintonia") :: ModuleScript)
	if okS and S and (S :: any).resetMatch then
		pcall((S :: any).resetMatch)
	end
end

local function startMatch()
	state = "Playing"
	stateT = 0
	matchStartedAt = os.clock()
	-- o acumulador do dano da zona pode ter sobrado da partida anterior (o laço
	-- para no meio quando alguém vence): zerado aqui, senão a partida NOVA
	-- começaria descontando ticks de zona atrasados no primeiro quadro.
	zoneAcc = 0
	table.clear(stats)
	lastHit = weakKeyed()
	clearEliminated() -- todo mundo volta a poder agir
	clearAllSpirits()
	table.clear(revivesUsed) -- o teto de retorno é POR PARTIDA
	champion = nil
	seal = nil
	Bots.clear()
	-- idempotente: no ciclo normal o `toLobby` já rodou isto. Existe aqui para a
	-- porta do `forceStart` direto da tela de fim — ver `resetCrossMatchState`.
	resetCrossMatchState()

	-- terreno novo a cada partida: a floresta queimada da rodada anterior não
	-- pode nascer carbonizada (mataria a pergunta de validação V3).
	terrain.build(Arena.generate())

	zone.center = Vector3.new(0, Grid.FLOOR_Y, 0)
	zone.radius = Z.startRadius
	beginPhase(1)
	syncZonePart()
	Bots.setZone(zone.center, zone.radius)
	-- altares DEPOIS do terreno: são objetos do Workspace, não células, e o
	-- `walkableNear` precisa da grade já construída para desviar de rocha/água.
	buildAltars()

	local humans = Players:GetPlayers()
	local botCount = 0
	if forcedBots then
		botCount = math.clamp(forcedBots, 0, TUNE.maxBots) -- harness de playtest
		forcedBots = nil
	elseif M.fillWithBots then
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
	clearAllSpirits()
	clearAltars()
	Bots.clear()
	Bots.setZone(Vector3.zero, math.huge)
	if zonePart then
		zonePart:Destroy()
		zonePart = nil
	end
	-- PROGRESSO E COOLDOWN NÃO ATRAVESSAM PARTIDA (achado da revisão de combate,
	-- 18/08): o Escudo Evolutivo é conquistado DENTRO de uma prova e o cooldown
	-- compartilhado da Sintonia é de 24 s. Vem ANTES do `LoadCharacter` de baixo
	-- — ver `resetCrossMatchState`, que é o mesmo reset que o `startMatch` chama
	-- para a porta do `forceStart`.
	resetCrossMatchState()

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
	lastHit = weakKeyed()
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
		--- TIMES vivos, não magos vivos: em duos "3 times restantes" é a
		--- informação que muda a decisão do jogador. `alive` continua indo para
		--- a UI não perder o número fino.
		teamsAlive = countTeams(aliveMembers()),
		total = math.max(totalAtStart, 1),
		timeLeft = timeLeft,
		seal = seal,
	}

	-- Progresso do ritual por jogador ENVOLVIDO (o que canaliza e o que volta).
	-- Uma passada só: sem isto o carregador encara 6 s de tela parada sem saber
	-- se está funcionando, e o ritual vira bug aos olhos de quem testa.
	local channelOf: { [Player]: number } = {}
	for dead, sp in pairs(spirits) do
		if sp.channel > 0 then
			local k = math.clamp(sp.channel / REVIVE.channel, 0, 1)
			channelOf[dead] = k
			if sp.carrier then
				channelOf[sp.carrier] = k
			end
		end
	end

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
					--- caído tem 0 de vida no painel, mesmo com corpo de espírito
					--- respirando no servidor: senão a HUD mostraria "100 HP" para
					--- um parceiro que está morto na tela.
					hp = if hum and not eliminated[m] then hum.Health else 0,
					hpMax = if hum then hum.MaxHealth else 0,
					alive = hum ~= nil and not eliminated[m],
					--- caiu MAS ainda dá para trazer de volta: é este bit que
					--- transforma "perdi o parceiro" em "vai buscar a essência"
					spirit = spirits[m] ~= nil,
				}
				break
			end
		end
		payload.mate = mate
		local isSpirit = spirits[player] ~= nil
		payload.spirit = isSpirit
		--- TRÊS ESTADOS, NUNCA DOIS: jogando (`spectating` falso, `spirit` falso) ·
		--- ESPÍRITO (`spirit` verdadeiro — acompanha a dupla e ainda volta pelo
		--- altar; nunca é poleiro) · ESPECTADOR de poleiro (`spectating`).
		---
		--- `spectating` continua sendo o bit que LIGA o painel; `spectateKind` diz
		--- QUAL das duas frases entra nele. Um bit sozinho junta duas situações
		--- opostas — "entrei agora, não perdi nada, jogo a próxima" e "caí de vez,
		--- perdi ESTA prova" — e num servidor público a primeira é a experiência
		--- da MAIORIA de quem entra. (A pendência da string caiu:
		--- `announce.match.spectating` existe e sai no `onCharacter`.)
		local kind = spectateKind(eliminated[player], isSpirit)
		payload.spectating = kind ~= nil
		payload.spectateKind = kind
		payload.reviveChannel = channelOf[player] or 0
		evState:FireClient(player, payload)
	end
end

-- --------------------------------------------------------------------- API
function Match.init(terrainModule: any, combatModule: any)
	terrain = terrainModule
	combat = combatModule
	evState = Net.event(Net.Names.MatchState)
	evAnnounce = Net.event(Net.Names.Announce)
	--- ARREPIO do espírito (GDD §18.6). C→S: { pos: Vector3 } · S→C:
	--- { pos: Vector3, hold: number }. Roteado AQUI e não no Main — mesmo padrão
	--- da postura no Combat: quem sabe quem é espírito é este módulo.
	--- (A pendência que morava neste comentário caiu na R6: o coordenador
	--- oficializou o nome em Net.Names. A chamada é que continuava com a string
	--- crua, o que deixava o contrato com dois donos — corrigido na R9.)
	evSpirit = Net.event(Net.Names.SpiritPing)
	evSpirit.OnServerEvent:Connect(function(player, payload)
		Match.spiritPing(player, if typeof(payload) == "table" then (payload :: any).pos else nil)
	end)

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
			if spirits[player] then
				ghostify(player) -- ESPÍRITO: acompanha a dupla, não compete
			else
				perch(player) -- eliminado de vez: espectador, não competidor
				-- O aviso escrito sai AQUI, e não no PlayerAdded: no PlayerAdded o
				-- cliente dele ainda pode não ter ligado o OnClientEvent do Announce
				-- e o toast sairia para ninguém. Aqui é também o instante exato em
				-- que a câmera sobe 120 studs — é quando a explicação é necessária.
				-- Só para quem CHEGOU AGORA: quem caiu já ouviu `announce.death`, e
				-- repetir "você entra na próxima" por cima seria trocar o luto dele
				-- por um aviso que não é o dele.
				if eliminated[player] == "late" then
					announceTo(player, "announce.match.spectating")
				end
			end
		end
	end

	local function onPlayer(player: Player)
		if state == "Playing" then
			-- ENTROU NO MEIO DA PROVA. Não se injeta gente num battle royale já
			-- começado — mas o motivo "late" (e não "out") é o que separa, na tela
			-- dele, "não perdi nada" de "perdi esta prova". O painel permanente
			-- sai do `broadcast`; o toast, do `onCharacter` logo acima.
			markEliminated(player, "late")
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

--- ARREPIO (GDD §18.6): o espírito revela UM inimigo perto da própria dupla.
--- Intenção do cliente tratada como HOSTIL: só espírito pinga, só uma vez, e a
--- posição pedida tem de estar ao alcance do PRÓPRIO espírito — sem esse teste
--- o morto viraria radar do mapa inteiro, que é o oposto de contra-jogável.
--- Público para o COORDENADOR poder mover o roteamento para o Main depois.
function Match.spiritPing(player: Player, rawPos: any)
	local sp = spirits[player]
	if state ~= "Playing" or not sp or sp.ping <= 0 or typeof(rawPos) ~= "Vector3" then
		return
	end
	local orb = sp.orb
	if not (orb and orb.Parent) or flatDist(rawPos, orb.Position) > REVIVE.pingRange then
		return
	end
	-- JANELA MÍNIMA (alpha público): o arrepio só é GASTO quando acha alguém, e
	-- pedido sem alvo saía de graça. Um cliente hostil pedindo a 1 kHz de um
	-- canto vazio fazia o servidor remontar a lista inteira de vivos a cada
	-- pedido — DoS barato por dentro de um remote legítimo. Cobra a janela ANTES
	-- da varredura, e cobra mesmo quando não acha nada (é o caso do abuso).
	local now = os.clock()
	if now < sp.nextPingAt then
		return
	end
	sp.nextPingAt = now + TUNE.pingCooldown

	local best: Bots.Target? = nil
	local bestD = REVIVE.pingRange
	for _, e in ipairs(aliveEntities()) do
		local char = e.humanoid.Parent
		local owner = if char then Players:GetPlayerFromCharacter(char) else nil
		local member: any = owner or e.humanoid
		if Teams.teamOf(member) ~= sp.team then
			local d = flatDist(e.root.Position, rawPos)
			if d < bestD then
				best, bestD = e, d
			end
		end
	end
	if not best then
		return
	end

	sp.ping -= 1
	local marked = (best :: Bots.Target).root.Position
	local mark = { pos = marked, hold = REVIVE.pingHold }
	evSpirit:FireClient(player, mark)
	for _, mate in ipairs(Teams.matesOf(player)) do
		if typeof(mate) == "Instance" and mate:IsA("Player") then
			evSpirit:FireClient(mate, mark)
		end
	end
	-- CONTRA-JOGADA (GDD §18.6): o revelado SENTE o arrepio. Marcação sem aviso
	-- seria wallhack de graça para quem morreu.
	local revealed = (best :: Bots.Target).humanoid.Parent
	local victim = if revealed then Players:GetPlayerFromCharacter(revealed) else nil
	if victim then
		announceTo(victim, "announce.spirit.chill")
	end
end

-- --------------------------------------------------- controles do harness
--- Começa a partida AGORA, sem lobby (Playtest.luau). `bots` fixa quantos bots
--- entram; `nil` mantém o preenchimento normal do Balance. Sem isto o harness
--- só conseguia EMPURRAR o lobby mexendo em Balance.match, e não conseguia
--- encerrar nada — era a pendência declarada no cabeçalho do Playtest.
function Match.forceStart(bots: number?): boolean
	if state == "Playing" then
		return false
	end
	forcedBots = if typeof(bots) == "number" then math.max(0, math.floor(bots)) else nil
	startMatch()
	return true
end

--- Encerra a partida em andamento: fecha o relatório de telemetria e cai na
--- tela de fim, exatamente como uma vitória (sem reiniciar o Studio).
function Match.forceEnd(): boolean
	if state ~= "Playing" then
		return false
	end
	finish()
	return true
end

function Match.onPlayerRemoving(player: Player)
	unmarkEliminated(player)
	clearSpirit(player)
	stats[player] = nil
	if state == "Playing" and stateT >= TUNE.startGrace and matchOver() then
		finish()
	end
end

--- A partida está em andamento? O Tutorial pergunta antes de montar o Campo de
--- Provas — quem entra no meio da prova nasce eliminado e não concluiria passo
--- nenhum. Leitura pura: ninguém muda estado por aqui.
function Match.isPlaying(): boolean
	return state == "Playing"
end

function Match.step(dt: number)
	stateT += dt

	if state == "Lobby" then
		local n = #Players:GetPlayers()
		-- com bots ligados, UM mago já forma partida (GDD §19.6: o novato joga
		-- na hora); sem bots, respeita minPlayersToStart.
		local ready = n >= M.minPlayersToStart or (M.fillWithBots and n >= 1)
		-- O LOBBY NÃO ESPERA MAIS PELO ONBOARDING (R11 — "tutorial não é
		-- obrigatório precisa ser opcional"). Até aqui, um jogador no Campo de
		-- Provas SEGURAVA a partida de todo mundo por até 240 s: quem não pediu
		-- tutorial nenhum ficava preso no lobby por causa da escolha de um
		-- desconhecido — ou seja, o onboarding era obrigatório para os OUTROS.
		-- Agora o lobby só olha para quem quer jogar. Quem está treinando é
		-- fechado pelo dono das sessões (`Tutorial.step`, que consulta
		-- `Match.isPlaying`), guarda o passo alcançado e recebe o botão TREINAR
		-- de volta no lobby seguinte — nenhuma partida espera por ninguém.
		if ready then
			lobbyLeft -= dt
			if lobbyLeft <= 0 then
				startMatch()
			end
		else
			lobbyLeft = M.lobbyWait
		end
	elseif state == "Playing" then
		-- DUAS SAÍDAS QUE O BR NÃO RESOLVE SOZINHO (ver `mustEndNow`). Medidas no
		-- cenário de robustez ANTES da correção: (1) 20 s depois do último humano
		-- sair, ainda `Playing` com 8 bots andando para plateia nenhuma — e quem
		-- entrasse ali nascia espectador de uma partida de bots; (2) 900 s com
		-- quatro AFK dentro do círculo final, `Playing` para sempre. Encerram pela
		-- MESMA porta de sempre (`finish`), que é o que mantém uma saída só.
		local humans = #Players:GetPlayers()
		if mustEndNow(humans, stateT) then
			print(
				string.format(
					"[Match] encerrando por segurança — %s",
					if humans <= 0 then "servidor vazio" else string.format("teto de %ds", math.floor(matchTimeLimit()))
				)
			)
			finish()
		end
		if state == "Playing" then
			stepZone(dt)
			zoneAcc += dt
			-- `state == "Playing"` no laço: `applyZoneDamage` mata, `onDeath` pode
			-- chamar `finish`, e sem esta condição a iteração seguinte continuava
			-- queimando gente numa partida que já acabou.
			while zoneAcc >= TUNE.zoneTick and state == "Playing" do
				zoneAcc -= TUNE.zoneTick
				applyZoneDamage()
				-- rede de segurança a 2 Hz (o caso instantâneo já é tratado no
				-- Died): cobre quem saiu do servidor ou sumiu sem morrer.
				if stateT >= TUNE.startGrace and matchOver() then
					finish()
				end
			end
		end
		if state == "Playing" then
			stepRevive(dt)
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

	-- RITMO DA PARTIDA: `holdTime`/`shrinkTime` já saem RESOLVIDOS pela variante
	-- (Balance.experiment.zoneVariant = blitz 4:35 / media 5:30 / longa 7:15).
	-- O Match não sabe que existe experimento — só confere que o cronograma
	-- resultante cabe na janela jogável.
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

	-- CONTAGEM DE TIMES (o número que a UI mostra em duos)
	assert(countTeams(roster) == 3, "5 membros em duos = 3 times vivos")
	assert(countTeams({ roster[1], roster[2] }) == 1, "uma dupla inteira conta como UM time")
	assert(countTeams({ roster[1], roster[3] }) == 2, "um de cada dupla = 2 times")
	assert(countTeams({ roster[1] }) == 1, "sobrevivente sozinho ainda é um time")
	assert(countTeams({}) == 0, "ninguém vivo = 0 times")

	-- RITUAL DE RESSURREIÇÃO: os dois freios que impedem a morte de virar
	-- detalhe. O corte é por FASE — se um dia virar segundos, este teste
	-- continua passando e a regra muda de significado quando o ritmo mudar.
	local savedPhase, savedUsed = zone.phase, revivesUsed[7]
	zone.phase = 1
	assert(reviveAllowed(7), "antes da fase final a dupla pode voltar")
	assert(not reviveAllowed(nil), "sem time não há retorno (lobby/espectador)")
	revivesUsed[7] = true
	assert(not reviveAllowed(7), "cada dupla volta no MÁXIMO uma vez por partida")
	revivesUsed[7] = nil
	zone.phase = Z.phases
	assert(not reviveAllowed(7), "na fase final da zona o ritual fecha para todos")
	zone.phase = math.max(1, Z.phases - 1)
	assert(reviveAllowed(7), "a fase anterior à final ainda permite o retorno")
	zone.phase, revivesUsed[7] = savedPhase, savedUsed
	assert(REVIVE.channel > 0 and REVIVE.radius > 0, "o ritual precisa de tempo e de área")

	-- SELO DO CAMPEÃO NO FIM POR DUPLA. O ASSERT QUE PAGA A CORREÇÃO DE 18/08:
	-- desde que a partida passou a acabar quando resta UM TIME, o desfecho normal
	-- deixa DOIS vivos — e a checagem antiga (`#survivors == 1`) devolvia campeão
	-- nil, ou seja, a vitória mais comum do jogo terminava sem Selo na tela.
	local solo = { name = "Solo", kills = 1, damage = 500 }
	local ace = { name = "Ace", kills = 3, damage = 100 }
	assert(pickChampion({ solo, ace }) ~= nil, "vitória de DUPLA (2 vivos) tem de ter campeão")
	assert(pickChampion({ solo, ace }) == ace, "campeão é quem tem mais abates; dano só desempata")
	assert(pickChampion({ solo }) == solo, "sobrevivente solitário continua sendo campeão")
	assert(pickChampion({}) == nil, "sem sobrevivente não há campeão (o servidor não inventa um)")

	-- O LOBBY NÃO ESPERA PELO ONBOARDING (R11). O tutorial virou OPCIONAL, e uma
	-- espera pelo Campo de Provas de um desconhecido é o tutorial voltando a ser
	-- obrigatório — para quem nem escolheu fazê-lo. A regra antiga tinha um teto
	-- de 240 s; o conserto não é aumentar nem diminuir o teto, é não ter espera.
	-- Este assert é a trava contra a volta do botão: reintroduzir o `holding`
	-- exige o número, e o número não existe mais.
	assert(
		(TUNE :: any).tutorialHoldMax == nil,
		"o lobby voltou a esperar pelo onboarding — quem não pediu tutorial não pode ser segurado por quem pediu"
	)

	-- ========================= ROBUSTEZ DE SERVIDOR PÚBLICO =========================
	-- Os dois caminhos que deixavam a máquina de estados presa em `Playing`. Os
	-- dois foram MEDIDOS no cenário de robustez antes de existir esta regra:
	--   · servidor esvaziou: 20 s depois do último humano sair, ainda `Playing`
	--     com 8 bots andando — e quem entrasse virava espectador de bots;
	--   · quatro AFK colados no centro: 900 s simulados, `teamsAlive=2`, sem fim.
	--     A fase final da zona para em `endRadius` e SEGURA (`dur = math.huge`),
	--     então "a zona resolve" é falso quando ninguém sai do círculo.
	assert(mustEndNow(0, 1), "servidor vazio no meio da prova encerra na hora — não se simula BR para ninguém")
	assert(not mustEndNow(1, 1), "com UM mago dentro a prova continua (bots são companhia, não motivo de fim)")
	assert(matchTimeLimit() < math.huge, "sem teto de tempo a partida pode ficar em Playing para sempre")
	assert(
		not mustEndNow(2, total),
		string.format("o teto (%ds) não pode cortar partida ainda dentro do cronograma da zona (%ds)", matchTimeLimit(), total)
	)
	assert(mustEndNow(2, matchTimeLimit()), "passado o teto a prova termina mesmo com todo mundo parado")

	-- ======================= TRÊS ESTADOS NA TELA (alpha público) =======================
	-- jogando · ESPÍRITO · ESPECTADOR de poleiro. O bit `spectating` sozinho
	-- juntava duas situações OPOSTAS: quem entrou agora (não perdeu nada, joga a
	-- próxima) e quem caiu de vez (perdeu ESTA prova). Num servidor público a
	-- primeira é a experiência da MAIORIA de quem entra — e sem a distinção ela
	-- chega no cliente como "você está fora", que é o contrário da verdade.
	assert(spectateKind(nil, false) == nil, "quem está jogando não é espectador")
	assert(spectateKind("late", false) == "late", "quem entrou no meio da prova assiste ESTA e joga a próxima")
	assert(spectateKind("out", false) == "out", "quem caiu de vez perdeu ESTA prova")
	assert(
		spectateKind("late", false) ~= spectateKind("out", false),
		"os dois espectadores não podem chegar IGUAIS no cliente: um não perdeu nada, o outro perdeu a prova"
	)
	-- ESPÍRITO NÃO É ESPECTADOR: está fora da contagem, não fora da partida —
	-- ainda volta pelo altar enquanto o parceiro atravessa o mapa. Mandá-lo para
	-- o painel de poleiro é dizer "você caiu nesta prova" para quem ainda joga.
	assert(spectateKind("out", true) == nil, "espírito acompanha a dupla — não é espectador de poleiro")
	assert(spectateKind("late", true) == nil, "espírito nunca vira painel de poleiro, seja qual for o motivo")

	-- ARREPIO: fronteira de confiança C→S. O gasto só acontece quando ACHA
	-- alguém, então sem janela mínima um pedido vazio varria a lista de vivos de
	-- graça, quantas vezes o cliente quisesse.
	assert(TUNE.pingCooldown > 0, "o arrepio precisa de janela mínima entre pedidos, senão vira DoS barato")

	Teams.clear()
	assert(Teams.teamOf(roster[1]) == nil, "clear apaga os times")
	for _, d in ipairs(roster) do
		local dummy = d :: Humanoid
		dummy:Destroy()
	end

	print(
		string.format(
			"[Match] autoteste OK — partida cheia ~%ds (%d fases de %ds parada + %ds fechando) · ritual %.1fs em %d altares",
			total,
			Z.phases,
			Z.holdTime,
			Z.shrinkTime,
			REVIVE.channel,
			REVIVE.altars
		)
	)
	return true
end

return Match
