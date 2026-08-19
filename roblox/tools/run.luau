--!nocheck
-- ============================================================================
-- ARKANA — RUNNER DE TESTES FORA DO STUDIO (raia 3.6: Engenheiro de QA)
-- Roda com: roblox/tools/run.sh   (que monta o bundle e chama o luau.exe)
--
-- FASE 1 — BOOT: repete a ordem do src/server/Main.server.luau.
-- FASE 2 — AUTOTESTES: chama TODO selfTest() que existir nos módulos.
-- FASE 3 — PARTIDA: 2 jogadores falsos + bots, forceStart, minutos de
--          simulação em passo fixo, e as perguntas que interessam:
--          nada estoura · a zona fecha · a partida ACABA · o relatório sai.
--
-- Este arquivo NÃO conserta nada em src/. Ele só reporta.
-- ============================================================================

local H, SOURCES = ...
H.buildTree()

local DT = 1 / 20 -- passo fixo: 20 Hz é o suficiente para lógica de partida
local MAX_SECONDS = tonumber(SOURCES.__maxSeconds) or 600

local results = {}
local function record(kind, name, ok, detail)
	table.insert(results, { kind = kind, name = name, ok = ok, detail = detail })
	print(string.format("  [%s] %-28s %s", if ok then "PASSOU" else "FALHOU", name, detail or ""))
end

local function trace(err)
	return tostring(err) .. "\n" .. debug.traceback("", 2)
end

-- ============================================================ FASE 1 — BOOT
print("== FASE 1 — BOOT DO SERVIDOR (ordem do Main.server.luau) ==")

local Shared, Server = H.Shared, H.Server
local req = H.require

local boot = {}
local function step1(name, fn)
	local ok, err = xpcall(fn, trace)
	record("boot", name, ok, if ok then nil else err)
	return ok
end

step1("require Shared/*", function()
	boot.Net = req(Shared:WaitForChild("Net"))
	boot.Balance = req(Shared:WaitForChild("Balance"))
	boot.Grid = req(Shared:WaitForChild("Grid"))
	boot.Elements = req(Shared:WaitForChild("Elements"))
	boot.Events = req(Shared:WaitForChild("Events"))
	boot.Strings = req(Shared:WaitForChild("Strings"))
end)

step1("Net.initServer", function()
	boot.Net.initServer()
end)

step1("require Server/*", function()
	boot.Arena = req(Server:WaitForChild("Arena"))
	boot.Terrain = req(Server:WaitForChild("Terrain"))
	boot.Combat = req(Server:WaitForChild("Combat"))
	boot.Sintonia = req(Server:WaitForChild("Sintonia"))
	boot.Match = req(Server:WaitForChild("Match"))
end)

local layout
step1("Arena.generate", function()
	layout = boot.Arena.generate()
	assert(layout, "layout vazio")
end)

step1("Terrain.build", function()
	boot.Terrain.build(layout)
end)
step1("Combat.init", function()
	boot.Combat.init(boot.Terrain)
end)
step1("Sintonia.init", function()
	boot.Sintonia.init(boot.Terrain, boot.Combat)
end)
step1("Match.init", function()
	boot.Match.init(boot.Terrain, boot.Combat)
end)
step1("Tutorial.init(require)", function()
	boot.Tutorial = req(Server:WaitForChild("Tutorial"))
end)
step1("Telemetry.init", function()
	boot.Telemetry = req(Server:WaitForChild("Telemetry"))
	boot.Telemetry.init(boot.Terrain, boot.Combat, boot.Match)
end)

-- ====================================================== FASE 2 — AUTOTESTES
print("")
print("== FASE 2 — AUTOTESTES (selfTest de todo módulo que tiver um) ==")

local FOLDERS = { { "shared", Shared }, { "server", Server }, { "client", H.Client } }
local loaded, noSelfTest = {}, {}

for _, entry in ipairs(FOLDERS) do
	local area, folder = entry[1], entry[2]
	local names = {}
	for _, child in ipairs(folder:GetChildren()) do
		if child:IsA("ModuleScript") then
			table.insert(names, child.Name)
		end
	end
	table.sort(names)
	for _, name in ipairs(names) do
		local label = area .. "/" .. name
		local ok, mod = xpcall(req, trace, folder:FindFirstChild(name))
		if not ok then
			record("load", label, false, "não carregou: " .. tostring(mod))
		else
			loaded[label] = mod
			if type(mod) == "table" and type(mod.selfTest) == "function" then
				local ran, err = xpcall(mod.selfTest, trace)
				record("selfTest", label .. ".selfTest", ran, if ran then nil else err)
			else
				table.insert(noSelfTest, label)
			end
		end
	end
end
print("  (sem selfTest: " .. table.concat(noSelfTest, ", ") .. ")")

-- ================================================ FASE 3 — PARTIDA COMPLETA
print("")
print("== FASE 3 — PARTIDA DE INTEGRAÇÃO (2 humanos + bots, passo fixo) ==")

local Combat, Match, Terrain, Sintonia = boot.Combat, boot.Match, boot.Terrain, boot.Sintonia
local Balance = boot.Balance

--- Mesmos handlers C→S do Main.server.luau. Sem isto o teste nunca passaria
--- pela validação de entrada (Net.sanitize*), que é onde mora o anti-cheat.
local Net = boot.Net
local function serverEvent(name)
	return Net.event(name).OnServerEvent
end
serverEvent(Net.Names.CastSpell):Connect(function(player, payload)
	if typeof(payload) ~= "table" then
		return
	end
	local kind = payload.kind
	if kind ~= "basic" and kind ~= "tactic" then
		return
	end
	local dir = Net.sanitizeDirection(payload.dir)
	if not dir then
		return
	end
	--- `input` (R10) espelha o Main: modo de entrada do disparo, PURA telemetria
	--- (drag = o gesto único do §4 · tap · key). Sanitizado aqui também porque
	--- este espelho é o que o portão executa — validação que só existe no
	--- arquivo que o teste NÃO roda é validação que ninguém verifica.
	Combat.handleCast(player, kind, dir, boot.Events.sanitizeInput(payload.input))
end)
--- Espelha o handler que a R10 criou no Main. O cliente reporta o aparelho
--- desde a R5 e NINGUÉM escutava — `device_tier` saía "unknown" em todo evento.
--- Sem este espelho o portão jamais exercitaria o caminho de volta.
serverEvent(Net.Names.DeviceInfo):Connect(function(player, payload)
	if typeof(payload) ~= "table" then
		return
	end
	local tier = payload.tier
	if tier ~= "low" and tier ~= "mid" and tier ~= "high" then
		return
	end
	if boot.Telemetry and boot.Telemetry.setDevice then
		pcall(boot.Telemetry.setDevice, player, tier, payload.touch == true)
	end
end)
serverEvent(Net.Names.SwitchElement):Connect(function(player, payload)
	if typeof(payload) == "table" then
		Combat.handleSwitch(player, payload.id)
	end
end)
serverEvent(Net.Names.Dodge):Connect(function(player, payload)
	Combat.handleDodge(player, if typeof(payload) == "table" then Net.sanitizeDirection(payload.dir) else nil)
end)
serverEvent(Net.Names.ProposeSintonia):Connect(function(player, payload)
	if typeof(payload) == "table" then
		Sintonia.propose(player, payload.pos)
	end
end)
serverEvent(Net.Names.AcceptSintonia):Connect(function(player, payload)
	if typeof(payload) == "table" then
		Sintonia.accept(player, payload.fromUserId)
	end
end)

--- Mesma fiação por jogador do Main.server.luau (o Match/Combat dependem dela).
local function wirePlayer(player)
	--- SESSÃO (R10): mesma ordem do Main — `session_start` ANTES da fiação, e o
	--- `session_end` sai na saída do jogador. É o único recorte que atravessa
	--- partida (é a pergunta V5), então tem de estar no portão: instrumentação
	--- sem emissor é exatamente o defeito que esta fase encontrou.
	if boot.Telemetry and boot.Telemetry.emit then
		pcall(boot.Telemetry.emit, boot.Events.Name.SessionStart, player)
	end
	Combat.onPlayerAdded(player)
	player.CharacterAdded:Connect(function(char)
		local humanoid = char:WaitForChild("Humanoid")
		humanoid.WalkSpeed = Balance.player.speed
		humanoid.UseJumpPower = true
		humanoid.JumpPower = Balance.player.jumpPower
		humanoid.MaxHealth = Balance.player.hp
		humanoid.Health = Balance.player.hp
		Combat.onCharacterAdded(player, char, humanoid)
	end)
end

local players = {}
local okPlayers, errPlayers = xpcall(function()
	for i = 1, 2 do
		local p = H.addPlayer("Mago" .. i, 1000 + i)
		wirePlayer(p)
		p:LoadCharacter()
		table.insert(players, p)
	end
end, trace)
record("partida", "2 jogadores entram", okPlayers, if okPlayers then nil else errPlayers)

--- O CAMINHO QUE A R10 DESCOBRIU QUEBRADO. O cliente reporta o aparelho em
--- `Net.Names.DeviceInfo` desde a R5 e NINGUEM escutava: `device_tier` saia
--- "unknown" em todo evento, e a pendencia foi registrada por duas fases como
--- "falta o cliente reportar" — diagnostico errado, o receptor e' que nao
--- existia. Disparar o remote de verdade (com o Player certo, como o cliente
--- faz) e' o unico jeito de o portao provar que a ponta a ponta fecha: handler
--- espelhado + `Telemetry.setDevice` + contexto LENDO o carimbo. Sem isto o
--- espelho acima seria mais um teste decorativo.
local okDevice, devErr = xpcall(function()
	for i, p in ipairs(players) do
		serverEvent(boot.Net.Names.DeviceInfo):Fire(p, {
			tier = if i == 1 then "mid" else "high",
			touch = i == 1, -- um celular e um PC: o recorte de TOQUE precisa dos dois
		})
	end
	--- e um disparo carimbado com o gesto unico (§4), pelo remote real: prova que
	--- `input` sobrevive a sanitizacao do handler ate o evento `spell_cast`.
	serverEvent(boot.Net.Names.CastSpell):Fire(players[1], {
		kind = "basic",
		dir = H.Vector3.new(1, 0, 0),
		input = "drag",
	})
	local text = boot.Telemetry.report()
	assert(
		not string.find(text, "ninguem chamou `Telemetry.setDevice`", 1, true),
		"o relato de aparelho nao chegou: `device_tier` continua 'unknown' (era o bug da R5 a R9)"
	)
	--- NAO basta o bloco existir: ele tem de CONTAR. Um assert que so' procura o
	--- cabecalho passa com o agregador quebrado — foi assim que dois testes
	--- decorativos escaparam nesta mesma fase.
	local touchCasts = tonumber(string.match(text, "em aparelho de TOQUE %.*%s*(%d+) disparos")) or 0
	assert(touchCasts > 0, "o `input` nao chegou ao `spell_cast`: recorte de TOQUE ficou em 0 disparos")
end, trace)
record("partida", "aparelho relatado + gesto carimbado", okDevice, if okDevice then nil else devErr)

local okStart, startedOrErr = xpcall(function()
	return Match.forceStart(8)
end, trace)
record("partida", "Match.forceStart(8 bots)", okStart and startedOrErr == true, if okStart then nil else startedOrErr)

-- ------------------------------------------- diagnóstico: janela de telemetria
--- A telemetria reabre a janela SOZINHA no primeiro evento quando `running` é
--- falso (Telemetry.emit → "partida implicita"). Isto vigia se ela reabre
--- DEPOIS do fim — o que apagaria o dado da partida que acabou de terminar.
local implicitAfterEnd = {}
local matchFinished = false
local lastEmit = "?"
do
	local realStart, realEnd = boot.Telemetry.matchStart, boot.Telemetry.matchEnd
	boot.Telemetry.matchStart = function(id, ...)
		if matchFinished then
			table.insert(implicitAfterEnd, tostring(id) .. " durante emit(" .. lastEmit .. ")")
		end
		return realStart(id, ...)
	end
	-- marca o instante EXATO do fechamento: a reabertura acontece dentro do
	-- mesmo Match.step que encerrou, antes de qualquer broadcast
	boot.Telemetry.matchEnd = function(...)
		local r = table.pack(realEnd(...))
		matchFinished = true
		return table.unpack(r, 1, r.n)
	end
end

--- Contador de eventos: mede o que ACONTECEU na partida sem depender do
--- formato do relatório (que é texto para humano, não contrato de teste).
local evCount = {}
do
	local realEmit = boot.Telemetry.emit
	boot.Telemetry.emit = function(name, ...)
		if type(name) == "string" then
			evCount[name] = (evCount[name] or 0) + 1
			lastEmit = name
		end
		return realEmit(name, ...)
	end
end

-- ------------------------------------------------------ jogadores que AGEM
--- Sem intenção nenhuma o teste só mede zona e bots. Este piloto automático
--- gasta os caminhos C→S de verdade: trocar elemento, conjurar, esquivar e
--- propor/aceitar Sintonia (o funil que responde a pergunta V1 do projeto).
local Elements = boot.Elements
local Bots = req(Server:FindFirstChild("Bots"))
local evCast, evSwitch, evDodge = Net.event(Net.Names.CastSpell), Net.event(Net.Names.SwitchElement), Net.event(Net.Names.Dodge)
local evPropose, evAccept = Net.event(Net.Names.ProposeSintonia), Net.event(Net.Names.AcceptSintonia)

local function rootOf(player)
	local char = player.Character
	return if char then char:FindFirstChild("HumanoidRootPart") else nil
end

local function nearestFoe(from)
	local best, bestD = nil, math.huge
	for _, bot in ipairs(Bots.list()) do
		if bot.humanoid.Health > 0 then
			local d = (bot.root.Position - from).Magnitude
			if d < bestD then
				best, bestD = bot.root.Position, d
			end
		end
	end
	return best
end

local actAcc, comboAcc, elementTurn = 0, 0, 0
local function driveHumans(dt)
	actAcc += dt
	comboAcc += dt
	if actAcc >= 0.4 then
		actAcc = 0
		-- a DUPLA fica junta e mira o MESMO inimigo: é a única configuração em
		-- que a Sintonia pode acontecer (janela de 1,5s + epicentro comum).
		-- Sem isto o funil sai zerado por posicionamento, não por bug.
		local lead = rootOf(players[1])
		local mateChar = players[2] and players[2].Character
		if lead and mateChar and mateChar:FindFirstChild("HumanoidRootPart") then
			local mate = mateChar:FindFirstChild("HumanoidRootPart")
			if (mate.Position - lead.Position).Magnitude > 12 then
				mateChar:PivotTo(H.CFrame.new(lead.Position + H.Vector3.new(6, 0, 0)))
			end
		end
		local foe = if lead then nearestFoe(lead.Position) else nil
		-- A DUPLA CAÇA. Parada no ponto de nascimento ela nunca chega a 90 studs
		-- de um bot (medido: 124 studs de distância mínima em 191s), e aí o
		-- denominador do funil da Sintonia não tem como existir. O piloto anda
		-- com `Humanoid:MoveTo`, que é o mesmo comando que os bots usam.
		local leadHum = players[1].Character and players[1].Character:FindFirstChildOfClass("Humanoid")
		if lead and foe and leadHum and leadHum.Health > 0 and (foe - lead.Position).Magnitude > 70 then
			leadHum:MoveTo(foe)
		end
		for i, player in ipairs(players) do
			local root = rootOf(player)
			local hum = player.Character and player.Character:FindFirstChildOfClass("Humanoid")
			if root and hum and hum.Health > 0 then
				local dir = if foe then (foe - root.Position) else H.Vector3.new(1, 0, 0)
				evCast.OnServerEvent:Fire(player, { kind = if i == 1 then "basic" else "tactic", dir = dir })
				if math.fmod(elementTurn, 5) == i then
					evDodge.OnServerEvent:Fire(player, { dir = dir })
				end
			end
		end
	end
	if comboAcc >= 6 then
		comboAcc = 0
		elementTurn += 1
		-- elementos DIFERENTES nos dois: é o que abre combo de Sintonia
		local order = Elements.Order
		for i, player in ipairs(players) do
			evSwitch.OnServerEvent:Fire(player, { id = order[(elementTurn + i) % #order + 1] })
		end
		local a, b = players[1], players[2]
		local ra = rootOf(a)
		if ra and b then
			evPropose.OnServerEvent:Fire(a, { pos = ra.Position })
			-- aceite 0,3s DEPOIS: no mesmo frame a latência do funil sai -0,00s
			H.task.delay(0.3, function()
				evAccept.OnServerEvent:Fire(b, { fromUserId = a.UserId })
			end)
		end
	end
end

--- DIAGNÓSTICO DO DENOMINADOR DO FUNIL (pedido do coordenador). Repete, com
--- API pública, as MESMAS condições do `scanOpportunities` do Sintonia, na
--- mesma cadência (0,5s): aliados · vivos com root · elementos DIFERENTES ·
--- a ≤120 studs um do outro (PING_HEAR) · inimigo vivo a ≤90 studs do ponto
--- médio (OPP_RANGE). Serve para separar "bug do jogo" de "piloto que nunca
--- criou a situação".
local Teams = req(Server:FindFirstChild("Teams"))
local oppSamples, oppHit = 0, { allies = 0, alive = 0, diffEl = 0, near = 0, enemy = 0, all = 0 }
local oppMinPair, oppMinEnemy = math.huge, math.huge
local elementPairs = {}
local oppAcc2 = 0

local function diagnoseOpportunity(dt)
	oppAcc2 += dt
	if oppAcc2 < 0.5 then
		return
	end
	oppAcc2 = 0
	local a, b = players[1], players[2]
	oppSamples += 1
	if not Teams.areAllies(a, b) then
		return
	end
	oppHit.allies += 1
	local ra, rb = rootOf(a), rootOf(b)
	local ha = a.Character and a.Character:FindFirstChildOfClass("Humanoid")
	local hb = b.Character and b.Character:FindFirstChildOfClass("Humanoid")
	if not (ra and rb and ha and hb and ha.Health > 0 and hb.Health > 0) then
		return
	end
	oppHit.alive += 1
	local elA, elB = Combat.getElement(a), Combat.getElement(b)
	if elA and elB then
		elementPairs[tostring(elA) .. "/" .. tostring(elB)] = true
	end
	if not (elA and elB and elA ~= elB) then
		return
	end
	oppHit.diffEl += 1
	local d = (ra.Position - rb.Position).Magnitude
	oppMinPair = math.min(oppMinPair, d)
	if d > 120 then
		return
	end
	oppHit.near += 1
	local mid = (ra.Position + rb.Position) / 2
	local best = math.huge
	for _, bot in ipairs(Bots.list()) do
		if bot.humanoid.Health > 0 then
			best = math.min(best, (bot.root.Position - mid).Magnitude)
		end
	end
	oppMinEnemy = math.min(oppMinEnemy, best)
	if best > 90 then
		return
	end
	oppHit.enemy += 1
	oppHit.all += 1
end

-- ------------------------------------------------------------- laço da partida
local heartbeat = H.RunService.Heartbeat
local frames, simulated = 0, 0
local radiusMin, radiusMax = math.huge, 0
local phaseSeen = {}
local ended, endedAt, endState = false, 0, nil
local stepErrors = {}

local function guard(label, fn, ...)
	local ok, err = xpcall(fn, trace, ...)
	if not ok and #stepErrors < 12 then
		table.insert(stepErrors, label .. ": " .. tostring(err))
	end
	return ok
end

while simulated < MAX_SECONDS do
	frames += 1
	simulated += DT
	H.tick(DT)
	H.physics(DT)
	guard("intenções C→S", driveHumans, DT)
	guard("diagnóstico oportunidade", diagnoseOpportunity, DT)
	guard("Terrain.step", Terrain.step, DT)
	guard("Combat.step", Combat.step, DT)
	guard("Sintonia.step", Sintonia.step, DT)
	guard("Match.step", Match.step, DT)
	guard("Heartbeat", function()
		heartbeat:Fire(DT)
	end)

	local st = H.lastFire.MatchState
	if st then
		if st.zoneRadius then
			radiusMin = math.min(radiusMin, st.zoneRadius)
			radiusMax = math.max(radiusMax, st.zoneRadius)
		end
		if st.phase then
			phaseSeen[st.phase] = true
		end
		if st.state == "Ended" and not ended then
			ended = true
			matchFinished = true
			endedAt = simulated
			endState = st
			-- mais 2s de partida DEPOIS do fim: é onde a telemetria costuma
			-- reabrir janela sozinha e apagar o que acabou de coletar
			for _ = 1, 40 do
				H.tick(DT)
				H.physics(DT)
				guard("Terrain.step(pós-fim)", Terrain.step, DT)
				guard("Combat.step(pós-fim)", Combat.step, DT)
				guard("Sintonia.step(pós-fim)", Sintonia.step, DT)
				guard("Match.step(pós-fim)", Match.step, DT)
				guard("Heartbeat(pós-fim)", function()
					heartbeat:Fire(DT)
				end)
			end
			break
		end
	end
end

local phases = 0
for _ in pairs(phaseSeen) do
	phases += 1
end

record("partida", "nenhum erro em step()", #stepErrors == 0, if #stepErrors == 0 then
	string.format("%d frames · %.0fs simulados", frames, simulated)
else
	table.concat(stepErrors, " | "))

record("partida", "zona fecha nas fases", radiusMax > radiusMin and phases >= 2,
	string.format("raio %.0f -> %.0f · %d fases vistas", radiusMax, radiusMin, phases))

record("partida", "partida TERMINA (matchOver)", ended,
	if ended then string.format("acabou em %.0fs simulados", endedAt)
	else string.format("NÃO acabou em %.0fs — state=%s alive=%s teamsAlive=%s",
		simulated,
		tostring(H.lastFire.MatchState and H.lastFire.MatchState.state),
		tostring(H.lastFire.MatchState and H.lastFire.MatchState.alive),
		tostring(H.lastFire.MatchState and H.lastFire.MatchState.teamsAlive)))

--- O fim é por DUPLA (decisão do coordenador 18/08), não por sobrevivente:
--- a última dupla viva ganha SEM precisar perder o parceiro.
record("partida", "fim é por DUPLA (teamsAlive<=1)", ended and (endState.teamsAlive or 99) <= 1,
	if ended then string.format("teamsAlive=%s · alive=%s", tostring(endState.teamsAlive), tostring(endState.alive))
	else "partida não terminou")

if endState and endState.seal then
	print(string.format("  selo: campeão=%s kills=%s dano=%s duração=%ss",
		tostring(endState.seal.champion), tostring(endState.seal.kills),
		tostring(endState.seal.damage), tostring(endState.seal.duration)))
end

local N = boot.Events.Name
local function n(name)
	return evCount[name] or 0
end
print(string.format("  diagnóstico do denominador: %d amostras (0,5s) · aliados %d · vivos %d · elementos != %d · dist<=120 %d · inimigo<=90 %d · TODAS %d",
	oppSamples, oppHit.allies, oppHit.alive, oppHit.diffEl, oppHit.near, oppHit.enemy, oppHit.all))
do
	local pairsSeen = {}
	for k in pairs(elementPairs) do
		table.insert(pairsSeen, k)
	end
	table.sort(pairsSeen)
	print(string.format("  menor distância da dupla %.0f · menor distância meio->bot %.0f · pares de elemento vistos: %s",
		oppMinPair, oppMinEnemy, table.concat(pairsSeen, " ")))
end

record("partida", "denominador do funil (oportunidade)", n(N.SintoniaOpportunity) > 0,
	string.format("%d evento(s) · situação existiu em %d/%d amostras",
		n(N.SintoniaOpportunity), oppHit.all, oppSamples))

record("partida", "Sintonia: pacto vira canalização", n(N.SintoniaChannelStart) > 0,
	string.format("ping %d · aceite %d · canalização %d · fusão %d · oportunidade %d",
		n(N.SintoniaPing), n(N.SintoniaAccept), n(N.SintoniaChannelStart), n(N.SintoniaFired), n(N.SintoniaOpportunity)))
record("partida", "combate acontece (dano/knock)", n(N.Damage) > 0 and n(N.Knock) > 0,
	string.format("cast %d · dano %d · knock %d · eliminação %d · esquiva %d · terreno %d",
		n(N.SpellCast), n(N.Damage), n(N.Knock), n(N.Elimination), n(N.DodgeStart), n(N.TerrainStateChange)))

record("partida", "telemetria NÃO reabre após o fim", #implicitAfterEnd == 0,
	if #implicitAfterEnd == 0 then "janela do fim preservada"
	else string.format("%d reabertura(s) — o relatório pós-partida sai VAZIO (ids: %s)",
		#implicitAfterEnd, table.concat(implicitAfterEnd, ", ")))

local okReport, report = xpcall(boot.Telemetry.report, trace)
record("partida", "Telemetry.report()", okReport and type(report) == "string",
	if okReport then string.format("%d linhas", select(2, tostring(report):gsub("\n", "\n")) + 1) else report)
if okReport then
	print("")
	print("---------------- RELATÓRIO DA TELEMETRIA ----------------")
	print(report)
	print("--------------------------------------------------------")
end

-- ============================================ FASE 4 — CENÁRIOS DE ROBUSTEZ
--- Os seis travamentos que a R8 achou e corrigiu, cada um num SERVIDOR NOVO
--- (harness próprio, seed própria — mesmo truque do sweep) — mais o S7, a
--- MARATONA da R13, que é UM servidor só de propósito: seis provas encadeadas
--- com gente entrando e saindo, medindo o que cresce de uma prova para a
--- seguinte. Rodam no caminho PADRÃO de propósito: correção sem teste tem prazo
--- de validade, e teste que só roda quando alguém lembra não é portão.
--- `run.sh scenarios` roda só estes.
print("")
print("== FASE 4 — CENÁRIOS DE ROBUSTEZ (S1–S7, um servidor novo por cenário) ==")
do
	local ok, scenarios = xpcall(function()
		return assert(loadstring(SOURCES["tools/scenarios.luau"], "@roblox/tools/scenarios.luau"))(SOURCES)
	end, trace)
	if ok then
		scenarios(function(name, passed, detail)
			record("cenário", name, passed, detail)
		end)
	else
		record("cenário", "carregar scenarios.luau", false, tostring(scenarios))
	end
end

-- ================================================================= RESUMO
print("")
print("== RESUMO ==")
local pass, fail = 0, 0
local failed = {}
for _, r in ipairs(results) do
	if r.ok then
		pass += 1
	else
		fail += 1
		table.insert(failed, r.name)
	end
end

if #H.errors > 0 then
	print(string.format("  %d erro(s) engolido(s) por handler de evento/task:", #H.errors))
	local shown = {}
	for _, e in ipairs(H.errors) do
		local key = e.where .. "|" .. tostring(e.err):sub(1, 160)
		if not shown[key] then
			shown[key] = true
			print("   * " .. e.where .. " -> " .. tostring(e.err):gsub("\n", "\n     "))
		end
	end
end

print(string.format("  verificações: %d · PASSOU %d · FALHOU %d", #results, pass, fail))
if fail > 0 then
	print("  falhas: " .. table.concat(failed, ", "))
	error(string.format("[ARKANA/QA] %d verificação(ões) falharam", fail), 0)
end
print("  tudo verde")
