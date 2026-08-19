--!strict
-- ============================================================================
-- ARKANA (Roblox) — PREFERÊNCIAS DE CONFORTO QUE ATRAVESSAM O REJOIN
--
-- POR QUE EXISTE (item aberto nº 6 da R9): as 11 opções de conforto viviam em
-- atributos `Arkana_*` do LocalPlayer — de SESSÃO. Quem baixou o volume ou ligou
-- o alto contraste levava o susto de novo a cada rejoin. Num alpha público esse
-- é o atrito que faz a pessoa não voltar, que é exatamente a pergunta V5.
--
-- DIVISÃO DE TRABALHO (a mesma do Grimório): o SERVIDOR persiste, o cliente não
-- tem DataStore. Aqui não mora nenhuma regra de conforto — o `Accessibility` é a
-- LOJA (padrões, aplicação, mudo↔legendas); este módulo é só o cofre.
--
-- NADA AQUI DECIDE JOGO. São opções de tela e de som: o pior que um cliente
-- hostil consegue é estragar as PRÓPRIAS opções. Por isso o remote não precisa
-- de anti-cheat — precisa de LIMITE: chave livre vinda do cliente viraria
-- armazenamento grátis pago pelo Diretor. Daí a lista fechada abaixo.
--
-- ORÇAMENTO DE DATASTORE: ZERO escritas durante a partida. Mexer no chip só
-- atualiza a cópia em memória; a escrita acontece UMA vez, na saída do jogador
-- (e no fechamento do servidor, para quem ainda estiver dentro). Uma escrita por
-- mexida de slider é o caminho clássico de derrubar um alpha por cota.
--
-- Dono: RAIA C (persistência).
-- ============================================================================

local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Net = require(Shared:WaitForChild("Net"))

local Prefs = {}

local TUNE = {
	dataStore = "ArkanaPrefs_v1",
	dataKeyPrefix = "p_",
	--- payload maior que isto não é jogador, é lixo ou ataque: as preferências
	--- são 11. Não trunca — REJEITA o pacote inteiro (truncar por `pairs`, cuja
	--- ordem é arbitrária, guardaria um subconjunto aleatório).
	maxKeys = 24,
	--- número fora disto é NaN/infinito/absurdo. Um só teto para todas: a faixa
	--- real de cada uma (0..1, e 0.9..1.3 do `uiSize`) é do cliente, que já
	--- valida na entrada da loja. Aqui o teto existe só para o dado guardado ser
	--- pequeno e finito.
	numMax = 4,
}

--- LISTA CANÔNICA — só o TIPO de cada preferência de conforto. É a cópia de
--- servidor da lista que mora no `client/Accessibility.luau` (o servidor não
--- pode requerer módulo de cliente em produção).
---
--- COMO AS DUAS LISTAS SÃO IMPEDIDAS DE DIVERGIR:
---   1. os VALORES padrão NÃO estão aqui. Só o tipo. Quem não guarda número não
---      tem número para divergir — o servidor nunca aplica padrão nenhum, ele
---      devolve o que foi salvo e o cliente completa o resto com o DEFAULTS
---      dele. Uma fonte de verdade para os valores, como a R9 fez com o volume.
---   2. o que sobrou (o conjunto de chaves e o tipo de cada uma) é comparado
---      CHAVE A CHAVE com `Accessibility.defaults()` no `selfTest`, que é
---      portão. Preferência nova, renomeada ou de outro tipo do outro lado
---      reprova o build em vez de virar opção que não salva.
local SPEC: { [string]: string } = {
	uiSize = "number",
	leftHanded = "boolean",
	contrast = "boolean",
	shake = "number",
	flash = "number",
	vfx = "number",
	blur = "boolean",
	captions = "boolean",
	readyCue = "boolean",
	volMaster = "number",
	volWorld = "number",
	hudDensity = "string",
}

type Slot = {
	--- SÓ o que o jogador escolheu de propósito. Preferência que ele nunca tocou
	--- não entra: os padrões de `shake`/`flash`/`contrast` nascem dos sinais
	--- NATIVOS do Roblox (ReducedMotion, PreferredTransparency) e congelar o
	--- valor derivado faria o jogo ignorar para sempre quem ligasse a redução de
	--- movimento DEPOIS. Ausência aqui significa "manda a plataforma".
	prefs: { [string]: any },
	dirty: boolean,
}

local slots: { [Player]: Slot } = {}
local remote: RemoteEvent? = nil
local wired = false

-- --------------------------------------------------------------- persistência
--- DataStore SEMPRE por pcall: no Studio ele nem existe e a falha tem de ser
--- SILENCIOSA (mesmo padrão do `Grimoire`). Sem persistência as preferências
--- valem por sessão — que é exatamente o comportamento de antes desta raia, e
--- ninguém fica sem jogar por causa disso.
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

-- ------------------------------------------------------ validação de entrada
--- Regra pura (é o que o autoteste prova sem jogo nenhum): peneira uma tabela
--- de preferências, venha ela do CLIENTE ou do DataStore. Chave desconhecida e
--- tipo errado somem em silêncio; o resto do pacote continua valendo.
function Prefs.sanitize(raw: any): { [string]: any }
	local out: { [string]: any } = {}
	if typeof(raw) ~= "table" then
		return out
	end
	local n = 0
	for k, v in pairs(raw :: any) do
		n += 1
		if n > TUNE.maxKeys then
			return {} -- pacote absurdo: nada dele entra
		end
		local want = if typeof(k) == "string" then SPEC[k] else nil
		if want ~= nil and typeof(v) == want then
			if want == "number" then
				local num = v :: number
				-- UMA regra só: número FINITO, limitado à janela. NaN e infinito
				-- não têm limite que os salve (e `math.clamp` de NaN é NaN).
				if num == num and num ~= math.huge and num ~= -math.huge then
					out[k] = math.clamp(num, 0, TUNE.numMax)
				end
			else
				out[k] = v
			end
		end
	end
	return out
end

-- ------------------------------------------------------------------ rede S→C
local function push(player: Player)
	local slot = slots[player]
	if not slot or not remote or next(slot.prefs) == nil then
		return -- nada salvo: o cliente fica com os padrões dele, que é o certo
	end
	(remote :: RemoteEvent):FireClient(player, { prefs = slot.prefs })
end

--- C→S: "mudei isto". Função nomeada de propósito — é o que o autoteste chama
--- direto, sem precisar de um RemoteEvent de mentira.
--- NÃO ESCREVE NO DATASTORE. Só marca sujo; a escrita é na saída.
function Prefs.receive(player: Player, payload: any)
	local slot = slots[player]
	if not slot or typeof(payload) ~= "table" then
		return
	end
	for k, v in pairs(Prefs.sanitize((payload :: any).prefs)) do
		if slot.prefs[k] ~= v then
			slot.prefs[k] = v
			slot.dirty = true
		end
	end
end

-- ------------------------------------------------------------------- escrita
--- SÍNCRONA de propósito. Na saída do jogador o servidor pode estar FECHANDO, e
--- aí uma `task.spawn` simplesmente não chega a rodar — o dado se perderia
--- justo na última sessão, que é a que mais importa medir.
local function save(player: Player, slot: Slot)
	if not slot.dirty then
		return -- ninguém mexeu em nada: não gasta cota para reescrever o igual
	end
	slot.dirty = false
	local s = ensureStore()
	if not s then
		return
	end
	pcall(function()
		s:SetAsync(keyOf(player), slot.prefs)
	end)
end

-- ----------------------------------------------------------------------- API
function Prefs.onPlayerAdded(player: Player)
	if slots[player] then
		return -- idempotente: a fiação automática e a do Main podem coexistir
	end
	local slot: Slot = { prefs = {}, dirty = false }
	slots[player] = slot

	--- I/O em thread PRÓPRIA: se o DataStore estiver lento ou fora, o jogador
	--- nasce, joga e usa os padrões. NUNCA bloquear o spawn esperando disco.
	task.spawn(function()
		local s = ensureStore()
		local saved: any = nil
		if s then
			local ok, v = pcall(function()
				return s:GetAsync(keyOf(player))
			end)
			saved = if ok then v else nil
		end
		if slots[player] ~= slot then
			return -- saiu (ou reentrou) enquanto o DataStore pensava
		end
		for k, v in pairs(Prefs.sanitize(saved)) do
			--- o que ele mexeu NESTA sessão vence o que estava salvo: sem isto,
			--- mudar uma opção nos primeiros segundos seria desfeito pela
			--- resposta atrasada do disco.
			if slot.prefs[k] == nil then
				slot.prefs[k] = v
			end
		end
		push(player)
	end)
end

function Prefs.onPlayerRemoving(player: Player)
	local slot = slots[player]
	slots[player] = nil
	if slot then
		save(player, slot)
	end
end

--- Fechamento do servidor: quem ainda está dentro não passa por
--- `PlayerRemoving` de forma confiável. `BindToClose` dá até 30s.
--- ponytail: TETO CONHECIDO — grava em série. Com 10 jogadores sujos são ~10
--- `SetAsync` seguidos, folgados dentro da janela; se um dia o servidor tiver
--- 50 slots, o upgrade é uma `task.spawn` por jogador + espera pelo contador.
function Prefs.flushAll()
	for player, slot in pairs(slots) do
		save(player, slot)
	end
	table.clear(slots)
end

--- O que está guardado para este jogador (leitura; `nil` = jogador desconhecido).
function Prefs.get(player: Player): { [string]: any }?
	local slot = slots[player]
	return if slot then slot.prefs else nil
end

--- Boot. Idempotente e todo protegido: nada aqui pode impedir o servidor de
--- subir — persistência de conforto não é motivo para ninguém ficar sem jogar.
function Prefs.init()
	local ok, ev = pcall(Net.event, Net.Names.Settings)
	if ok and ev then
		remote = ev
	end
	if wired then
		return
	end
	wired = true
	if remote then
		(remote :: RemoteEvent).OnServerEvent:Connect(function(player: Player, payload: any)
			pcall(Prefs.receive, player, payload)
		end)
	end
	--- Fiação própria: assim o coordenador precisa de UMA linha no
	--- `Main.server.luau` (que é de outra raia nesta leva) em vez de quatro.
	pcall(function()
		Players.PlayerAdded:Connect(function(player: Player)
			pcall(Prefs.onPlayerAdded, player)
		end)
		Players.PlayerRemoving:Connect(function(player: Player)
			pcall(Prefs.onPlayerRemoving, player)
		end)
		game:BindToClose(function()
			pcall(Prefs.flushAll)
		end)
	end)
	for _, player in ipairs(Players:GetPlayers()) do
		pcall(Prefs.onPlayerAdded, player) -- quem já estava dentro no boot
	end
end

-- ============================================================================
-- AUTOTESTE — roda no runner headless e na Command Bar do Studio:
--   require(game.ServerScriptService.Server.Prefs).selfTest()
-- ============================================================================

--- A lista do OUTRO lado. No runner ela mora em
--- `Players.LocalPlayer.PlayerScripts.Client`; num servidor de verdade, em
--- `StarterPlayer.StarterPlayerScripts.Client`. Isto só existe para o teste —
--- em produção o servidor nunca requer módulo de cliente.
local function clientDefaults(): { [string]: any }?
	local roots: { Instance } = {}
	local lp = Players.LocalPlayer
	local ps = if lp then lp:FindFirstChild("PlayerScripts") else nil
	if ps then
		table.insert(roots, ps)
	end
	local okSp, sp = pcall(game.GetService, game, "StarterPlayer")
	local sps = if okSp and sp then (sp :: Instance):FindFirstChild("StarterPlayerScripts") else nil
	if sps then
		table.insert(roots, sps)
	end
	for _, root in ipairs(roots) do
		local folder = root:FindFirstChild("Client")
		local mod = if folder then folder:FindFirstChild("Accessibility") else nil
		if mod then
			local ok, m = pcall(require, mod :: ModuleScript)
			if ok and type(m) == "table" and type(m.defaults) == "function" then
				local okD, d = pcall(m.defaults)
				if okD and type(d) == "table" then
					return d
				end
			end
		end
	end
	return nil
end

function Prefs.selfTest()
	-- 1) BOOT SEM DATASTORE. No runner (e no Studio) o serviço não existe: se
	--    isto explodisse, o servidor não subiria por causa de opção de conforto.
	Prefs.init()
	assert(ensureStore() == false, "o runner não tem DataStore — o teste abaixo perde a graça se tiver")

	-- 2) AS DUAS LISTAS. É o portão contra a divergência cliente↔servidor.
	local defaults = clientDefaults()
	assert(defaults, "não achei `Accessibility.defaults()` — sem ela não dá para provar que as listas batem")
	for key, value in pairs(defaults :: { [string]: any }) do
		assert(SPEC[key], "preferência `" .. key .. "` existe no cliente e NÃO salva (falta em SPEC)")
		assert(
			SPEC[key] == typeof(value),
			"tipo divergiu em `" .. key .. "`: cliente " .. typeof(value) .. ", servidor " .. SPEC[key]
		)
	end
	local n = 0
	for key in pairs(SPEC) do
		assert((defaults :: any)[key] ~= nil, "servidor guarda `" .. key .. "` que o cliente não conhece mais")
		n += 1
	end
	assert(n == 12, "as preferências de conforto são 12; SPEC tem " .. n)

	-- 3) PENEIRA (regra pura). Cada linha aqui é uma porta que o cliente não abre.
	assert(next(Prefs.sanitize(nil)) == nil, "payload que não é tabela virou preferência")
	assert(next(Prefs.sanitize("uiSize")) == nil, "string virou preferência")
	assert(Prefs.sanitize({ hackKey = 1 }).hackKey == nil, "CHAVE DESCONHECIDA entrou no cofre")
	assert(Prefs.sanitize({ contrast = "sim" }).contrast == nil, "TIPO ERRADO (string em booleano) entrou")
	assert(Prefs.sanitize({ uiSize = true }).uiSize == nil, "TIPO ERRADO (booleano em número) entrou")
	assert(Prefs.sanitize({ uiSize = 0 / 0 }).uiSize == nil, "NaN entrou")
	assert(Prefs.sanitize({ uiSize = math.huge }).uiSize == nil, "infinito entrou")
	assert(Prefs.sanitize({ uiSize = -math.huge }).uiSize == nil, "infinito negativo entrou")
	assert(Prefs.sanitize({ volMaster = -3 }).volMaster == 0, "número negativo não foi limitado")
	assert(Prefs.sanitize({ volMaster = 1e300 }).volMaster == TUNE.numMax, "número gigante não foi limitado")
	assert(Prefs.sanitize({ volMaster = 3.5 }).volMaster == 3.5, "número válido foi recusado")
	assert(Prefs.sanitize({ [1] = "x", uiSize = 1.15 }).uiSize == 1.15, "chave não-string estragou o pacote bom")
	do
		local flood = {}
		for i = 1, TUNE.maxKeys + 5 do
			flood["k" .. i] = i
		end
		flood.uiSize = 1.3
		assert(next(Prefs.sanitize(flood)) == nil, "pacote gigante do cliente foi aceito")
	end

	-- 4) CICLO DE VIDA SEM DATASTORE: entra, mexe, sai, servidor fecha — e nada
	--    disso pode levantar erro nem guardar lixo.
	local probe = ({ UserId = -1 } :: any) :: Player
	Prefs.onPlayerAdded(probe)
	local got = Prefs.get(probe)
	assert(got and next(got) == nil, "jogador sem nada salvo tem de nascer com o cofre vazio (= padrões)")
	Prefs.receive(probe, { prefs = { uiSize = 1.15, hackKey = 7, contrast = "sim", volMaster = 9 } })
	got = Prefs.get(probe) :: { [string]: any }
	assert(got.uiSize == 1.15, "a escolha do jogador não ficou guardada")
	assert(got.hackKey == nil, "chave desconhecida atravessou o remote")
	assert(got.contrast == nil, "tipo errado atravessou o remote")
	assert(got.volMaster == TUNE.numMax, "número absurdo atravessou o remote sem limite")
	Prefs.receive(probe, "não sou tabela")
	assert(Prefs.get(probe) ~= nil, "payload inválido derrubou o cofre do jogador")
	Prefs.onPlayerRemoving(probe)
	assert(Prefs.get(probe) == nil, "jogador saiu e o cofre continuou na memória")
	Prefs.onPlayerAdded(probe)
	Prefs.receive(probe, { prefs = { blur = false } })
	Prefs.flushAll() -- fechamento do servidor com gente dentro e dado sujo
	assert(Prefs.get(probe) == nil, "flushAll não liberou os slots")

	print("[Prefs] autoteste OK — 12 preferências · listas cliente↔servidor iguais · sem DataStore nada quebra")
	return true
end

return Prefs
