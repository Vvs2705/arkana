--!strict
-- ============================================================================
-- ARKANA (Roblox) — GRIMÓRIO DE DESCOBERTAS, VERSÃO LITE (GDD §18.3)
--
-- 12 páginas. Cada página é uma INTERAÇÃO que o jogador descobriu sozinho —
-- progressão por EXPERIMENTAÇÃO, não por poder. Nenhuma página dá dano, mana,
-- escudo ou qualquer vantagem: é o Juramento anti-P2W (GDD §19.1) por
-- construção, não por promessa. Descobrir é o prêmio.
--
-- AUTORIDADE: o SERVIDOR decide o que foi descoberto. O cliente (GrimoireUi)
-- só recebe a lista e mostra. Nenhum remote aceita "eu descobri X".
--
-- POR QUE LITE: a auditoria pediu 12 primeiro e expansão só se os jogadores
-- perseguirem as páginas. Quem responde isso é a telemetria — por isso toda
-- descoberta emite `Events.Name.Discovery`: dá para medir se a página acendeu
-- SOZINHA (o jogador achou) ou logo depois de um tutorial mandar.
--
-- COMO ELE ENXERGA O JOGO (sem tocar em arquivo de outra raia):
--   1. `Combat.onDamage`  — gancho público que já existe (abate e "no vermelho")
--   2. um observador em cima de `Telemetry.emit` — é o barramento onde
--      Sintonia/Match já publicam fusão, interrupção, revive e fim de partida.
--      TETO CONHECIDO: é um wrap em função de outro módulo, não uma assinatura.
--      O gancho que falta e resolveria isto de vez: `Telemetry.onEvent(fn)`.
--   3. varredura do TERRENO por consulta de estado (`Terrain.getCell`) —
--      o Terrain não emite mudança COM AUTOR (pendência já registrada pela
--      telemetria), então a autoria sai de quem está perto com o elemento na mão.
--
-- Dono: RAIA DO GRIMÓRIO.
-- ============================================================================

local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Events = require(Shared:WaitForChild("Events"))
local Grid = require(Shared:WaitForChild("Grid"))
local Net = require(Shared:WaitForChild("Net"))

local Grimoire = {}

--- Remote próprio. NÃO está em `Net.Names` porque `shared/Net.luau` é de outra
--- raia — `Net.event` cria sob demanda no servidor, então o contrato funciona
--- sem editar arquivo alheio. PEDIDO AO COORDENADOR: promover para
--- `Net.Names.Grimoire = "Grimoire"` quando a raia do Net abrir.
--- C→S: sem payload ("me manda meu grimório"). S→C: { pages, found, latest? }.
Grimoire.REMOTE = "Grimoire"

--- ORDEM CANÔNICA das 12 páginas. É esta lista que vai para o cliente — assim
--- a UI nunca duplica o catálogo e não tem como divergir do servidor.
Grimoire.ORDER = {
	-- terreno (GDD §14) — as seis que ensinam o mapa como recurso
	"frozen_bridge",
	"conduction",
	"douse",
	"earth_cover",
	"quagmire",
	"smoke_out",
	-- dupla e desfecho
	"sintonia_done",
	"sintonia_broken",
	"revive",
	"terrain_master",
	"red_zone",
	"shield_slayer",
}

--- Páginas nascidas do TERRENO: são as que contam para `terrain_master`.
local TERRAIN_PAGES: { [string]: boolean } = {
	frozen_bridge = true,
	conduction = true,
	douse = true,
	earth_cover = true,
	quagmire = true,
	smoke_out = true,
}

local VALID: { [string]: boolean } = {}
for _, id in ipairs(Grimoire.ORDER) do
	VALID[id] = true
end

local TUNE = {
	--- varredura do terreno: 2 Hz é mais que suficiente para um estado que dura
	--- segundos, e mantém o custo em ~289 células por jogador por passada.
	scanInterval = 0.5,
	scanCells = 8, -- 8 células = 32 studs de raio ao redor do mago
	--- alguém "revelado" pela queimada da grama alta
	revealRadius = 24,
	--- "no vermelho": fração de vida abaixo da qual a mordida da zona conta
	redFraction = 0.25,
	redSurvive = 10, -- e sobreviver este tanto de segundos DEPOIS
	--- janela para creditar a interrupção de uma Sintonia a quem bateu
	interruptCredit = 4,
	shieldTier = 3,
	terrainForMaster = 3,
	dataKeyPrefix = "g_",
	dataStore = "ArkanaGrimoire_v1",
}

-- ------------------------------------------------------------------- estado
type Track = {
	found: { [string]: boolean },
	--- interações de terreno DESTA partida (conta mesmo se a página já existia)
	terrainThisMatch: { [string]: boolean },
	--- instante em que a zona/terreno deixou o jogador no vermelho (0 = não está)
	redAt: number,
	dirty: boolean,
}

local tracks: { [Player]: Track } = {}
--- último a bater em cada jogador (crédito da Sintonia interrompida)
local lastHit: { [Player]: { by: Player, at: number } } = {}
--- estado por célula visto na última varredura (índice do Grid → estado)
local seenState: { [number]: string } = {}

local terrain: any = nil
local combat: any = nil
local remote: RemoteEvent? = nil
local scanAcc = 0
local damageHooked = false
local teleHooked = false
local remoteHooked = false

local N = Events.Name

-- ------------------------------------------------------------------ vizinhos
--- Módulos de OUTRAS raias entram sempre por require protegido: faltando um,
--- o Grimório perde uma fonte de sinal e o jogo continua de pé.
local function neighbour(name: string): any
	local folder = script.Parent
	if not folder then
		return nil
	end
	local ok, m = pcall(function()
		return require(folder:FindFirstChild(name) :: ModuleScript)
	end)
	return (ok and m) or nil
end

local telemetry: any = nil
local function tele(): any
	if telemetry == nil then
		telemetry = neighbour("Telemetry") or false
	end
	return telemetry
end

local function emit(name: string, subject: any, payload: any?)
	local t = tele()
	if t and t.emit then
		pcall(t.emit, name, subject, payload)
	end
end

local bots: any = nil
local function botList(): { any }
	if bots == nil then
		bots = neighbour("Bots") or false
	end
	if not bots or not bots.list then
		return {}
	end
	local ok, list = pcall(bots.list)
	return if ok and type(list) == "table" then list else {}
end

-- ---------------------------------------------------------------- utilidades
local function isPlayer(v: any): boolean
	return typeof(v) == "Instance" and v:IsA("Player")
end

local function livePlayer(player: Player): (Humanoid?, BasePart?)
	local char = player.Character
	if not char then
		return nil, nil
	end
	local hum = char:FindFirstChildOfClass("Humanoid")
	local root = char:FindFirstChild("HumanoidRootPart")
	if hum and hum.Health > 0 and root and root:IsA("BasePart") then
		return hum, root
	end
	return nil, nil
end

local function playerOf(humanoid: Humanoid): Player?
	local char = humanoid.Parent
	if not char then
		return nil
	end
	return Players:GetPlayerFromCharacter(char)
end

local function byUserId(id: any): Player?
	if typeof(id) ~= "number" then
		return nil
	end
	local ok, p = pcall(function()
		return Players:GetPlayerByUserId(id)
	end)
	return if ok then p else nil
end

local function countKeys(t: { [string]: boolean }): number
	local n = 0
	for _ in pairs(t) do
		n += 1
	end
	return n
end

local function foundList(tr: Track): { string }
	local out = {}
	for _, id in ipairs(Grimoire.ORDER) do
		if tr.found[id] then
			table.insert(out, id)
		end
	end
	return out
end

-- ------------------------------------------------------------------ rede S→C
local function push(player: Player, latest: string?)
	local tr = tracks[player]
	if not tr or not remote then
		return
	end
	(remote :: RemoteEvent):FireClient(player, {
		pages = Grimoire.ORDER,
		found = foundList(tr),
		latest = latest,
	})
end

-- --------------------------------------------------------------- persistência
--- DataStore SEMPRE por pcall: no Studio ele nem existe e a falha tem de ser
--- SILENCIOSA (padrão do projeto — nada quebra por causa de dado). Sem
--- persistência o Grimório vale por sessão, que já basta para o playtest.
local store: any = nil
local function ensureStore(): any
	if store == nil then
		local ok, s = pcall(function()
			return game:GetService("DataStoreService"):GetDataStore(TUNE.dataStore)
		end)
		store = (ok and s) or false
	end
	return store
end

local function keyOf(player: Player): string
	return TUNE.dataKeyPrefix .. tostring(player.UserId)
end

-- ------------------------------------------------------------------ descoberta
local function unlock(player: Player, id: string)
	local tr = tracks[player]
	if not tr or not VALID[id] then
		return
	end
	if TERRAIN_PAGES[id] then
		tr.terrainThisMatch[id] = true
	end
	if tr.found[id] then
		return
	end
	tr.found[id] = true
	tr.dirty = true
	push(player, id)
	--- A pergunta que a auditoria quer responder: a página acendeu porque o
	--- jogador experimentou, ou porque alguém mandou? O evento é o dado bruto.
	emit(N.Discovery, player, {
		id = id,
		found_total = countKeys(tr.found),
		of_total = #Grimoire.ORDER,
	})
end

-- ============================================================================
-- REGRA PURA DA QUÍMICA (é o que o autoteste prova sem jogo nenhum).
-- Espelha `Terrain.applyElement`: dada a transição de estado de UMA célula e o
-- elemento na mão do mago mais próximo, qual página isto acende?
-- Se a química do Terrain mudar, ESTA tabela muda junto — e o autoteste é o
-- lugar onde a divergência aparece.
-- ============================================================================
function Grimoire.pageFor(element: string?, prev: string, cur: string, material: string): string?
	if prev == cur then
		return nil
	end
	if element == "Earth" and cur == "Wall" then
		return "earth_cover"
	elseif element == "Water" then
		if cur == "Mud" then
			return "quagmire"
		end
		--- apagar preserva o material; queimar até o fim vira "Burned". A
		--- diferença entre os dois é exatamente a contra-jogada do GDD §14.
		if prev == "Burning" and cur ~= "Burned" then
			return "douse"
		end
	elseif element == "Lightning" and cur == "Electrified" then
		return "conduction"
	elseif element == "Fire" and cur == "Burning" and material == "TallGrass" then
		--- "revelou alguém" é conferido no chamador (precisa de mundo)
		return "smoke_out"
	end
	return nil
end

-- ------------------------------------------------------------ varredura do chão
local function someoneNear(pos: Vector3, exclude: Player): boolean
	for _, other in ipairs(Players:GetPlayers()) do
		if other ~= exclude then
			local _, root = livePlayer(other)
			if root and (root.Position - pos).Magnitude <= TUNE.revealRadius then
				return true
			end
		end
	end
	for _, bot in ipairs(botList()) do
		local hum, root = bot.humanoid, bot.root
		if hum and root and hum.Health > 0 and (root.Position - pos).Magnitude <= TUNE.revealRadius then
			return true
		end
	end
	return false
end

--- ponytail: TETO CONHECIDO — a autoria é por PROXIMIDADE + elemento na mão,
--- não por autor do projétil (o Terrain não emite mudança com autor). Dois
--- magos do mesmo elemento colados podem trocar o crédito de uma página. É
--- barato, não dá poder a ninguém e some no dia em que existir
--- `Terrain.onStateChange(cx, cy, prev, cur, by)`.
local function scanAround(player: Player, pos: Vector3)
	if not terrain or not terrain.getCell then
		return
	end
	local element = if combat and combat.getElement then combat.getElement(player) else nil
	local cx0, cy0 = Grid.worldToCell(pos)

	local here = terrain.getCell(cx0, cy0)
	if here and here.material == "Water" and here.state == "Frozen" then
		unlock(player, "frozen_bridge") -- congelou (ou pegou carona) e ATRAVESSOU
	end

	local r = TUNE.scanCells
	for cy = cy0 - r, cy0 + r do
		for cx = cx0 - r, cx0 + r do
			local cell = terrain.getCell(cx, cy)
			if cell then
				local i = cy * Grid.COLS + cx
				local prev = seenState[i]
				seenState[i] = cell.state
				if prev and prev ~= cell.state then
					local page = Grimoire.pageFor(element, prev, cell.state, cell.material)
					if page == "smoke_out" and not someoneNear(Grid.cellToWorld(cx, cy), player) then
						page = nil -- queimou, mas não revelou ninguém: não é a página
					end
					if page then
						unlock(player, page)
					end
				end
			end
		end
	end
end

-- --------------------------------------------------------- gancho: Combat.onDamage
--- INVARIANTE (mesma nota da telemetria): o Combat itera os observadores SEM
--- pcall. Um erro aqui abortaria o `applyDamage` no meio. Por isso todo o corpo
--- roda dentro de pcall no ponto de assinatura.
local function onDamage(target: Humanoid, _dealt: number, from: Player?)
	local victim = playerOf(target)
	if from and victim then
		lastHit[victim] = { by = from, at = os.clock() }
	end

	if target.Health <= 0 then
		if from and combat and combat.getShieldLevel and combat.getShieldLevel(from) >= TUNE.shieldTier then
			unlock(from, "shield_slayer")
		end
		if victim and tracks[victim] then
			tracks[victim].redAt = 0 -- morreu: não sobreviveu ao vermelho
		end
		return
	end

	--- `from == nil` é dano de AMBIENTE (zona e perigo de terreno chegam assim).
	--- ponytail: TETO CONHECIDO — sem `Match.getState()` não dá para separar a
	--- mordida da zona da lava do terreno; as duas contam como "no vermelho".
	if not from and victim then
		local tr = tracks[victim]
		local maxHp = if target.MaxHealth > 0 then target.MaxHealth else 100
		if tr and tr.redAt == 0 and target.Health / maxHp <= TUNE.redFraction then
			tr.redAt = os.clock()
		end
	end
end

-- ------------------------------------------------ gancho: barramento de eventos
--- Quem cortou a Sintonia não vem no evento (o `reason` é textual). Crédito vai
--- para quem bateu num dos dois canalizadores há pouco.
--- ponytail: aliado que interrompe o PRÓPRIO combo não ganha página (é o teste
--- `~=` abaixo); um terceiro aliado ganharia. Sem fogo amigo no jogo, isto não
--- acontece — se um dia acontecer, o conserto é consultar `Teams.areAllies`.
local function creditInterrupt(a: any, payload: any)
	local b = byUserId(if typeof(payload) == "table" then payload.with_user_id else nil)
	local now, best, bestAt = os.clock(), nil :: Player?, 0
	for _, victim in ipairs({ a, b }) do
		if isPlayer(victim) then
			local hit = lastHit[victim :: Player]
			if hit and now - hit.at <= TUNE.interruptCredit and hit.at > bestAt then
				best, bestAt = hit.by, hit.at
			end
		end
	end
	if best and best ~= a and best ~= b then
		unlock(best, "sintonia_broken")
	end
end

local function observe(name: string, subject: any, payload: any)
	if name == N.SintoniaFired then
		if isPlayer(subject) then
			unlock(subject :: Player, "sintonia_done")
		end
		local other = byUserId(if typeof(payload) == "table" then payload.with_user_id else nil)
		if other then
			unlock(other, "sintonia_done") -- os DOIS conjuraram: os dois descobriram
		end
	elseif name == N.SintoniaInterrupted then
		creditInterrupt(subject, payload)
	elseif name == N.ReviveComplete then
		local by = if typeof(payload) == "table" then payload.by else nil
		if isPlayer(by) then
			unlock(by :: Player, "revive")
		end
	elseif name == N.MatchStart then
		for _, tr in pairs(tracks) do
			table.clear(tr.terrainThisMatch)
		end
		table.clear(seenState)
	elseif name == N.MatchEnd then
		--- o sujeito do `match_end` é o CAMPEÃO (Player ou bot)
		if isPlayer(subject) then
			local tr = tracks[subject :: Player]
			if tr and countKeys(tr.terrainThisMatch) >= TUNE.terrainForMaster then
				unlock(subject :: Player, "terrain_master")
			end
		end
		for _, tr in pairs(tracks) do
			table.clear(tr.terrainThisMatch)
		end
	end
end

--- ponytail: WRAP, não assinatura — `Telemetry` não expõe `onEvent(fn)`. É o
--- único jeito de ver `sintonia_fired`/`revive_complete`/`match_end` sem editar
--- arquivo de outra raia. Observa ANTES do original de propósito: no `match_end`
--- a janela de coleta fecha, e a `Discovery` do campeão tem de entrar antes.
local function hookTelemetry()
	if teleHooked then
		return
	end
	local t = tele()
	if not t or type(t.emit) ~= "function" then
		return
	end
	teleHooked = true
	local original = t.emit
	t.emit = function(name: string, subject: any, payload: any?)
		if typeof(name) == "string" then
			pcall(observe, name, subject, payload)
		end
		return original(name, subject, payload)
	end
end

-- --------------------------------------------------------------------- API
--- Chamado uma vez no boot do servidor. Os módulos podem vir por parâmetro
--- (como Sintonia/Match recebem) ou serem descobertos por require protegido.
function Grimoire.init(terrainModule: any?, combatModule: any?)
	terrain = terrainModule or neighbour("Terrain")
	combat = combatModule or neighbour("Combat")

	if combat and combat.onDamage and not damageHooked then
		damageHooked = true
		pcall(combat.onDamage, function(target: Humanoid, dealt: number, from: Player?)
			pcall(onDamage, target, dealt, from)
		end)
	end

	hookTelemetry()

	local ok, ev = pcall(Net.event, Grimoire.REMOTE)
	if ok and ev then
		remote = ev
		if not remoteHooked then
			remoteHooked = true
			--- ÚNICA intenção que o cliente pode mandar: "me manda meu grimório".
			--- Sem payload, sem "eu descobri X" — a autoridade é toda daqui.
			ev.OnServerEvent:Connect(function(player: Player)
				pcall(push, player, nil)
			end)
		end
	end

	table.clear(seenState)
	scanAcc = 0
end

function Grimoire.onPlayerAdded(player: Player)
	tracks[player] = { found = {}, terrainThisMatch = {}, redAt = 0, dirty = false }
	push(player, nil)

	task.spawn(function()
		local s = ensureStore()
		local tr = tracks[player]
		if not s or not tr then
			return
		end
		local ok, saved = pcall(function()
			return s:GetAsync(keyOf(player))
		end)
		if ok and type(saved) == "table" then
			for _, id in ipairs(saved) do
				if VALID[id] then
					tr.found[id] = true
				end
			end
			push(player, nil)
		end
	end)
end

function Grimoire.onPlayerRemoving(player: Player)
	local tr = tracks[player]
	tracks[player] = nil
	lastHit[player] = nil
	if not tr or not tr.dirty then
		return
	end
	local s = ensureStore()
	if not s then
		return -- Studio / DataStore fora do ar: em memória já bastou
	end
	local list = foundList(tr)
	pcall(function()
		s:SetAsync(keyOf(player), list)
	end)
end

--- Um passo: varre o chão ao redor de cada mago vivo e fecha a conta do
--- "sobreviveu no vermelho". Tudo aqui é LEITURA de estado — o Grimório nunca
--- decide nada de jogo.
function Grimoire.step(dt: number)
	scanAcc += dt
	if scanAcc < TUNE.scanInterval then
		return
	end
	scanAcc = 0
	local now = os.clock()
	for _, player in ipairs(Players:GetPlayers()) do
		local tr = tracks[player]
		if tr then
			local hum, root = livePlayer(player)
			if hum and root then
				scanAround(player, root.Position)
				if tr.redAt > 0 and now - tr.redAt >= TUNE.redSurvive then
					tr.redAt = 0
					unlock(player, "red_zone")
				end
			else
				tr.redAt = 0
			end
		end
	end
end

--- Quantas páginas o jogador tem (para HUD/relatório — leitura pura).
function Grimoire.progress(player: Player): (number, number)
	local tr = tracks[player]
	return (if tr then countKeys(tr.found) else 0), #Grimoire.ORDER
end

-- ============================================================================
-- AUTOTESTE — roda no runner headless e na Command Bar do Studio:
--   require(game.ServerScriptService.Server.Grimoire).selfTest()
-- Cobre o que não dá para ver a olho: o catálogo (12 páginas, sem repetição,
-- todas com texto nos DOIS idiomas) e a regra pura da química, que é o único
-- pedaço de lógica de verdade deste módulo.
-- ============================================================================
function Grimoire.selfTest()
	-- 1) catálogo: 12 páginas, ids únicos
	assert(#Grimoire.ORDER == 12, "o Grimório Lite tem 12 páginas, achei " .. #Grimoire.ORDER)
	local seen: { [string]: boolean } = {}
	for _, id in ipairs(Grimoire.ORDER) do
		assert(not seen[id], "id repetido no catálogo: " .. id)
		seen[id] = true
	end
	local terrainCount = 0
	for id in pairs(TERRAIN_PAGES) do
		assert(seen[id], "página de terreno fora do catálogo: " .. id)
		terrainCount += 1
	end
	assert(
		terrainCount >= TUNE.terrainForMaster,
		"`terrain_master` pede " .. TUNE.terrainForMaster .. " interações e só existem " .. terrainCount
	)

	-- 2) TEXTO NOS DOIS IDIOMAS. Chave ausente volta como a própria chave — é
	--    assim que uma página muda vira falha em vez de virar "grimoire.page..."
	--    na tela do jogador.
	local Strings = require(Shared:WaitForChild("Strings"))
	local before = Strings.getLang()
	for _, lang in ipairs({ "pt", "en" }) do
		Strings.setLang(lang)
		for _, id in ipairs(Grimoire.ORDER) do
			for _, suffix in ipairs({ ".t", ".d" }) do
				local key = "grimoire.page." .. id .. suffix
				assert(Strings.t(key) ~= key, "falta texto [" .. lang .. "] para " .. key)
			end
		end
		for _, key in ipairs({ "grimoire.title", "grimoire.close", "grimoire.new", "grimoire.locked", "grimoire.locked.hint" }) do
			assert(Strings.t(key) ~= key, "falta texto [" .. lang .. "] para " .. key)
		end
		assert(Strings.t("grimoire.count", 3, 12) ~= "grimoire.count", "grimoire.count não formatou [" .. lang .. "]")
	end
	Strings.setLang(before)

	-- 3) a regra pura da química (espelho de Terrain.applyElement)
	assert(Grimoire.pageFor("Earth", "Normal", "Wall", "Grass") == "earth_cover", "Terra ergue muro")
	assert(Grimoire.pageFor("Water", "Normal", "Mud", "Dirt") == "quagmire", "Água em terra vira lamaçal")
	assert(Grimoire.pageFor("Water", "Burning", "Normal", "TallGrass") == "douse", "Água apaga fogo")
	assert(Grimoire.pageFor("Water", "Burning", "Burned", "TallGrass") == nil, "queimar até o carvão NÃO é apagar")
	assert(Grimoire.pageFor("Lightning", "Normal", "Electrified", "Water") == "conduction", "Raio conduz na água")
	assert(Grimoire.pageFor("Fire", "Normal", "Burning", "TallGrass") == "smoke_out", "Fogo em grama alta revela")
	assert(Grimoire.pageFor("Fire", "Normal", "Burning", "Tree") == nil, "árvore queimando não é a página da grama alta")
	assert(Grimoire.pageFor(nil, "Normal", "Wall", "Grass") == nil, "sem elemento na mão não há autoria")
	assert(Grimoire.pageFor("Earth", "Wall", "Wall", "Grass") == nil, "estado que não mudou não descobre nada")
	assert(Grimoire.pageFor("Wind", "Burning", "Normal", "TallGrass") == nil, "Vento apagando não é a página da Água")

	-- 4) nenhuma página pode dar poder: o catálogo é só id + texto (Juramento
	--    anti-P2W, GDD §19.1). Se alguém pendurar um número aqui, isto reprova.
	for _, id in ipairs(Grimoire.ORDER) do
		assert(typeof(id) == "string", "página com forma inesperada — o catálogo é só de ids")
	end

	print(string.format("[Grimoire] autoteste OK — %d páginas (%d de terreno)", #Grimoire.ORDER, terrainCount))
	return true
end

return Grimoire
