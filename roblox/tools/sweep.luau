--!nocheck
-- ============================================================================
-- ARKANA — VARREDURA DE BALANCEAMENTO (raia 6.1: Analista de Dados)
-- Roda com: roblox/tools/run.sh sweep N     (padrao N=12)
--
-- O QUE FAZ: N partidas completas (2 pilotos + 8 bots), cada uma num harness
-- NOVO com SEED PROPRIA, ciclando as tres variantes de zona (blitz/media/longa)
-- via patch no FONTE do Balance antes do load — nenhum arquivo de src/ e'
-- tocado. Agrega via Telemetry.snapshot() (numeros crus, nao o texto) e imprime
-- um relatorio unico com a incerteza declarada no cabecalho.
--
-- HONESTIDADE (o entregavel): bot nao joga como gente. Esta varredura acha
-- ASSIMETRIA GROSSEIRA e REGRESSAO entre builds; ela NAO decreta balanceamento.
-- ============================================================================

local S = ...
local N = math.max(1, math.floor(tonumber(S.__sweepN) or 12))
--- deslocamento da base de sementes (3o argumento do run.sh): duas varreduras
--- do MESMO build com bases diferentes sao amostras INDEPENDENTES — e' assim
--- que se separa sinal de ruido antes de recomendar um nerf.
local SEED_OFF = math.floor(tonumber(S.__sweepSeed) or 0)

local DT = 1 / 20
--- teto por partida: agenda inteira da zona + folga. Partida que nao acabou
--- ate' ai vira dado ("nao terminou"), nunca loop infinito.
local CAP_SLACK = 180
local VARIANTS = { "blitz", "media", "longa" }
local BOTS_PER_MATCH = 8

local baseBalanceSrc = S["shared/Balance.luau"]

-- percentil por interpolacao linear (mesma matematica do Telemetry.report —
-- copiada porque a funcao e' local de la; 15 linhas nao justificam export)
local function percentile(sorted, p)
	local n = #sorted
	if n == 0 then
		return 0
	end
	if n == 1 then
		return sorted[1]
	end
	local rank = (n - 1) * p + 1
	local lo, hi = math.floor(rank), math.ceil(rank)
	if lo == hi then
		return sorted[lo]
	end
	return sorted[lo] + (sorted[hi] - sorted[lo]) * (rank - lo)
end

local function addMap(dst, src)
	for k, v in pairs(src or {}) do
		dst[k] = (dst[k] or 0) + v
	end
end

-- ------------------------------------------------------ uma partida completa
--- Boot identico ao do run.luau (que espelha o Main.server.luau), pilotos
--- identicos aos de la — mesma "habilidade" de bot nas duas ferramentas, para
--- os numeros serem comparaveis entre o teste e a varredura.
local function runMatch(seed, variant)
	S.__seed = seed
	local patched, hits = baseBalanceSrc:gsub('(zoneVariant%s*=%s*")%w+(")', "%1" .. variant .. "%2", 1)
	assert(hits == 1, "nao achei zoneVariant = \"...\" no Balance.luau para trocar a variante")
	S["shared/Balance.luau"] = patched

	local H = assert(loadstring(S["tools/harness.luau"], "@harness-seed" .. seed))(S)
	--- os modulos imprimem relatorio/avisos por partida — 30 partidas x ~130
	--- linhas afogariam o agregado, que e' o produto daqui. Erros continuam
	--- visiveis: vao para H.errors e saem contados na linha da partida.
	H.GLOBALS.print = function() end
	H.buildTree()
	local req = H.require
	local Shared, Server = H.Shared, H.Server

	local Net = req(Shared:WaitForChild("Net"))
	local Balance = req(Shared:WaitForChild("Balance"))
	local Elements = req(Shared:WaitForChild("Elements"))
	local Events = req(Shared:WaitForChild("Events"))
	Net.initServer()
	local Arena = req(Server:WaitForChild("Arena"))
	local Terrain = req(Server:WaitForChild("Terrain"))
	local Combat = req(Server:WaitForChild("Combat"))
	local Sintonia = req(Server:WaitForChild("Sintonia"))
	local Match = req(Server:WaitForChild("Match"))
	local layout = Arena.generate()
	Terrain.build(layout)
	Combat.init(Terrain)
	Sintonia.init(Terrain, Combat)
	Match.init(Terrain, Combat)
	local Telemetry = req(Server:WaitForChild("Telemetry"))
	Telemetry.init(Terrain, Combat, Match)
	local Bots = req(Server:FindFirstChild("Bots"))

	-- handlers C->S do Main.server.luau (validacao de entrada incluida)
	local function serverEvent(name)
		return Net.event(name).OnServerEvent
	end
	serverEvent(Net.Names.CastSpell):Connect(function(player, payload)
		if H.typeof(payload) ~= "table" then
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
		Combat.handleCast(player, kind, dir)
	end)
	serverEvent(Net.Names.SwitchElement):Connect(function(player, payload)
		if H.typeof(payload) == "table" then
			Combat.handleSwitch(player, payload.id)
		end
	end)
	serverEvent(Net.Names.Dodge):Connect(function(player, payload)
		Combat.handleDodge(player, if H.typeof(payload) == "table" then Net.sanitizeDirection(payload.dir) else nil)
	end)
	serverEvent(Net.Names.ProposeSintonia):Connect(function(player, payload)
		if H.typeof(payload) == "table" then
			Sintonia.propose(player, payload.pos)
		end
	end)
	serverEvent(Net.Names.AcceptSintonia):Connect(function(player, payload)
		if H.typeof(payload) == "table" then
			Sintonia.accept(player, payload.fromUserId)
		end
	end)

	local players = {}
	local function wirePlayer(player)
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
	for i = 1, 2 do
		local p = H.addPlayer("Mago" .. i, 1000 + i)
		wirePlayer(p)
		p:LoadCharacter()
		table.insert(players, p)
	end

	-- ------------------------------------------------------- SONDA DE PROVENIENCIA
	--- POR QUE EXISTE: a coluna "dano" da dominancia mistura tudo que tem autor
	--- com Player — projetil, fusao de Sintonia — e o balde `ambiente` mistura
	--- terreno com zona e com tiro de bot. Assim nao da' para responder "o Fogo
	--- domina porque a MAGIA e' forte ou porque o INCENDIO cobra aluguel?".
	---
	--- A sonda nao toca src/: envelopa `Combat.applyDamage` (todas as chamadas
	--- passam pelo campo da tabela) e classifica pelo CHUNK de quem chamou
	--- (debug.info) + a assinatura do autor. Sem numero de linha: linha muda,
	--- arquivo nao.
	local prov = {
		dmg = {}, -- [fonte][elemento] = dano
		knocks = {}, -- [fonte] = quedas
		killMatrix = {}, -- [elemento creditado][fonte do golpe] = eliminacoes
		dealt = {}, -- [userHash] = dano CAUSADO por jogador (a moeda do escudo)
		hits = {}, -- [elemento][basic|tactic] = acertos (elemento VERDADEIRO)
		--- "o bot foge do fogo, mas foge BEM?" — exposicao (tempo de bot em cada
		--- estado da FSM) contra rendimento (em que estado o alvo estava quando
		--- o tiro acertou). Se um elemento acerta muito mais em FLEE_FIRE do que
		--- a exposicao a FLEE_FIRE explicaria, a vantagem dele e' do PILOTO
		--- AUTOMATICO, nao da magia.
		stateTime = {}, -- [estado] = segundos-bot
		hitState = {}, -- [elemento][estado do alvo] = acertos
		casts = {}, -- [elemento][basic|tactic] = conjuracoes (Telemetry soma os dois)
		travel = {}, -- [elemento][d|n] = distancia percorrida ate' acertar
	}
	local curSource = "?"
	local lastHazard = "?"

	--- kind vem do proprio Terrain ("Burning"/"Electrified"): o tick de perigo
	--- chama hazardAt e aplica dano na linha seguinte, entao o ultimo kind com
	--- dps > 0 e' o do dano que esta' entrando.
	local realHazardAt = Terrain.hazardAt
	Terrain.hazardAt = function(pos)
		local dps, kind = realHazardAt(pos)
		if dps and dps > 0 then
			lastHazard = if kind == "Burning" then "terreno-fogo" else "terreno-raio"
		end
		return dps, kind
	end

	local realApply = Combat.applyDamage
	Combat.applyDamage = function(target, amount, fromPlayer, strong, fromNpc)
		local chunk = debug.info(2, "s") or "?"
		if chunk:find("Match") then
			curSource = "zona"
		elseif chunk:find("Sintonia") then
			curSource = "sintonia"
		elseif fromPlayer == nil and fromNpc == nil then
			curSource = lastHazard -- Combat:tick de perigo ou Bots:tick de perigo
		elseif chunk:find("Bots") then
			curSource = "tiro-bot"
		else
			curSource = "projetil"
		end
		realApply(target, amount, fromPlayer, strong, fromNpc)
		curSource = "?"
	end

	local function bump(tbl, a, b, n)
		local row = tbl[a]
		if not row then
			row = {}
			tbl[a] = row
		end
		row[b] = (row[b] or 0) + n
	end
	local function isPlayer(x)
		return x ~= nil and H.typeof(x) == "Instance" and x:IsA("Player")
	end
	--- MESMA resolucao do `elementOf` do Telemetry: override do payload, senao o
	--- elemento ATUAL do autor. Reproduzida aqui de proposito — e' justamente
	--- esta regra que a sonda esta' auditando.
	local function elementOf(subject, payload)
		if payload.environmental == true then
			return "ambiente"
		elseif typeof(payload.element) == "string" then
			return payload.element
		elseif isPlayer(subject) then
			return Combat.getElement(subject) or "?"
		end
		return "?"
	end
	local humOf = function(x)
		if isPlayer(x) then
			local char = x.Character
			return if char then char:FindFirstChildOfClass("Humanoid") else nil
		end
		return x
	end
	local knockSource = {}

	-- quem venceu? o MatchEnd sai com o campeao como SUJEITO — e' o unico lugar
	-- de onde da' para ler isso sem pedir API nova ao Match (arquivo de outra raia)
	local championKey = nil
	local realEmit = Telemetry.emit
	Telemetry.emit = function(name, subject, payload)
		if name == Events.Name.MatchEnd and championKey == nil then
			if subject ~= nil and H.typeof(subject) == "Instance" and subject:IsA("Player") then
				championKey = Events.userHash(subject.UserId)
			else
				championKey = "nao-humano"
			end
		elseif name == Events.Name.Damage and H.typeof(payload) == "table" and payload.nullified ~= true then
			local amount = tonumber(payload.amount) or 0
			if amount > 0 then
				bump(prov.dmg, curSource, elementOf(subject, payload), amount)
				--- moeda do Escudo Evolutivo (GDD §5): dano CAUSADO por quem tem
				--- Player. Mesma regra do Combat menos o filtro de aliado — os
				--- pilotos so' atiram em bot, entao a diferenca e' zero aqui.
				if payload.environmental ~= true and isPlayer(subject) then
					local k = Events.userHash(subject.UserId)
					prov.dealt[k] = (prov.dealt[k] or 0) + amount
				end
			end
		elseif name == Events.Name.SpellHit and H.typeof(payload) == "table" then
			--- o SpellHit carrega o elemento VERDADEIRO do projetil (p.element),
			--- nao o elemento atual do dono: e' a testemunha limpa contra a
			--- atribuicao do evento Damage.
			if typeof(payload.element) == "string" then
				bump(prov.hits, payload.element, if payload.strong == true then "tactic" else "basic", 1)
				local st = "humano/?"
				for _, bot in ipairs(Bots.list()) do
					if bot.humanoid == payload.target then
						st = bot.state
						break
					end
				end
				bump(prov.hitState, payload.element, st, 1)
				bump(prov.travel, payload.element, "d", tonumber(payload.traveled) or 0)
				bump(prov.travel, payload.element, "n", 1)
			end
		elseif (name == Events.Name.SpellCast or name == Events.Name.TacticCast)
			and H.typeof(payload) == "table" and typeof(payload.element) == "string" then
			bump(prov.casts, payload.element, if name == Events.Name.TacticCast then "tactic" else "basic", 1)
		elseif name == Events.Name.Knock then
			prov.knocks[curSource] = (prov.knocks[curSource] or 0) + 1
			local hum = humOf(subject)
			if hum ~= nil then
				knockSource[hum] = curSource
			end
		elseif name == Events.Name.Elimination and H.typeof(payload) == "table" then
			--- o Died do Humanoid dispara DENTRO da escrita de Health, ou seja
			--- ANTES do emit do Knock — entao para o alvo que morre agora o mapa
			--- ainda esta' vazio e quem vale e' a fonte da pilha atual.
			local hum = humOf(subject)
			local src = (hum and knockSource[hum]) or (if curSource ~= "?" then curSource else "sem-queda")
			local killer = payload.by
			local el = if isPlayer(killer) then (Combat.getElement(killer) or "?") else "?"
			bump(prov.killMatrix, el, src, 1)
		end
		return realEmit(name, subject, payload)
	end

	assert(Match.forceStart(BOTS_PER_MATCH) == true, "forceStart recusou")

	-- piloto automatico: MESMO comportamento do run.luau (cacar, atirar no
	-- mesmo alvo, esquivar, alternar elementos, propor/aceitar Sintonia)
	local evCast = Net.event(Net.Names.CastSpell)
	local evSwitch = Net.event(Net.Names.SwitchElement)
	local evDodge = Net.event(Net.Names.Dodge)
	local evPropose = Net.event(Net.Names.ProposeSintonia)
	local evAccept = Net.event(Net.Names.AcceptSintonia)

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
			local lead = rootOf(players[1])
			local mateChar = players[2] and players[2].Character
			if lead and mateChar and mateChar:FindFirstChild("HumanoidRootPart") then
				local mate = mateChar:FindFirstChild("HumanoidRootPart")
				if (mate.Position - lead.Position).Magnitude > 12 then
					mateChar:PivotTo(H.CFrame.new(lead.Position + H.Vector3.new(6, 0, 0)))
				end
			end
			local foe = if lead then nearestFoe(lead.Position) else nil
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
			local order = Elements.Order
			for i, player in ipairs(players) do
				evSwitch.OnServerEvent:Fire(player, { id = order[(elementTurn + i) % #order + 1] })
			end
			local a, b = players[1], players[2]
			local ra = rootOf(a)
			if ra and b then
				evPropose.OnServerEvent:Fire(a, { pos = ra.Position })
				H.task.delay(0.3, function()
					evAccept.OnServerEvent:Fire(b, { fromUserId = a.UserId })
				end)
			end
		end
	end

	local Z = Balance.match.zone
	local cap = Balance.match.lobbyWait + Z.phases * (Z.holdTime + Z.shrinkTime) + CAP_SLACK
	local heartbeat = H.RunService.Heartbeat
	local simulated, ended = 0, false
	local stepErrors = {}
	local function guard(label, fn, ...)
		local ok, err = pcall(fn, ...)
		if not ok and #stepErrors < 3 then
			table.insert(stepErrors, label .. ": " .. tostring(err))
		end
	end

	local sampleAcc = 0
	while simulated < cap do
		simulated += DT
		--- amostragem da FSM a 2 Hz: e' o DENOMINADOR de exposicao do bloco de
		--- estados. Mais fino nao muda a conta e custa 20x mais laco.
		sampleAcc += DT
		if sampleAcc >= 0.5 then
			for _, bot in ipairs(Bots.list()) do
				if bot.humanoid.Health > 0 then
					prov.stateTime[bot.state] = (prov.stateTime[bot.state] or 0) + sampleAcc
				end
			end
			sampleAcc = 0
		end
		H.tick(DT)
		H.physics(DT)
		guard("pilotos", driveHumans, DT)
		guard("Terrain.step", Terrain.step, DT)
		guard("Combat.step", Combat.step, DT)
		guard("Sintonia.step", Sintonia.step, DT)
		guard("Match.step", Match.step, DT)
		guard("Heartbeat", function()
			heartbeat:Fire(DT)
		end)
		local st = H.lastFire.MatchState
		if st and st.state == "Ended" then
			ended = true
			break
		end
	end

	local snap = Telemetry.snapshot()
	return {
		variant = variant,
		seed = seed,
		ended = ended,
		sim = simulated,
		duration = snap.duration,
		snap = snap,
		championKey = championKey,
		prov = prov,
		stepErrors = stepErrors,
		harnessErrors = #H.errors,
		balance = Balance, -- so' leitura: dados para o TTK teorico e a agenda da zona
	}
end

-- ------------------------------------------------------------ TTK "de tabela"
--- Duelo parelho TEORICO, direto do Balance: espelho 1v1, todo tiro de basico
--- acerta, sem esquiva, sem tatica, sem viagem de projetil. E' o LIMITE INFERIOR
--- do TTK real — serve de regua fixa contra o alvo (p50 2,75-3,5s), imune ao
--- ruido do piloto. EHP = hp + escudo nivel 1.
local function mirrorTtk(Balance, el)
	local spec = Balance.elements[el].basic
	local ehp = Balance.player.hp + Balance.shield.caps[1]
	local mana, t, dmg, nextShot = Balance.player.manaMax, 0, 0, 0
	local step = 1 / 60
	while dmg < ehp and t < 120 do
		if t >= nextShot and mana >= spec.manaCost then
			mana -= spec.manaCost
			dmg += spec.dmg
			nextShot = t + spec.fireRate
		end
		mana = math.min(Balance.player.manaMax, mana + Balance.player.manaRegen * step)
		t += step
	end
	return t
end

-- =========================================================== laco da varredura
print("== ARKANA - VARREDURA DE BALANCEAMENTO ==")
print(string.format("   %d partidas · seeds distintas (base %d) · zona em ciclo (blitz/media/longa)", N, SEED_OFF))
print("")

local records = {}
local failures = {}
for i = 1, N do
	local variant = VARIANTS[(i - 1) % #VARIANTS + 1]
	local seed = 100003 + (i + SEED_OFF * N) * 7919
	local ok, rec = pcall(runMatch, seed, variant)
	if ok then
		table.insert(records, rec)
		print(string.format("  partida %2d/%d  %-6s seed=%-7d %s %5.0fs  ttk(n)=%d  erros=%d",
			i, N, variant, seed, if rec.ended then "TERMINOU " else "NAO ACABOU", rec.duration,
			rec.snap.all.ttkSeen, #rec.stepErrors + rec.harnessErrors))
	else
		table.insert(failures, { i = i, variant = variant, err = tostring(rec) })
		print(string.format("  partida %2d/%d  %-6s FALHOU: %s", i, N, variant, tostring(rec):sub(1, 160)))
	end
end
S["shared/Balance.luau"] = baseBalanceSrc

assert(#records > 0, "nenhuma partida rodou — nada a agregar")
local Balance = records[1].balance

-- ------------------------------------------------------------------ agregacao
local ttkH, ttkA = {}, {}
local castsH, dmgH, castsA, dmgA, killsA = {}, {}, {}, {}, {}
local evAll = {}
local pactChannels = 0
local oppPinged, oppFused = 0, 0
local fire = { fronts = 0, cells = 0, max = 0, exhausted = 0, burned = 0, flammable = 0 }
local durations = {} -- por variante, so' partidas que TERMINARAM
local notEnded = {}
local provDmg, provKnocks, provKill, provHits = {}, {}, {}, {}
local provStateTime, provHitState, provCasts, provTravel = {}, {}, {}, {}
local dealtList = {} -- dano causado por JOGADOR por PARTIDA (amostra do escudo)
local dealtRate = {} -- o mesmo, normalizado por minuto de partida
local humansMeasured, humanLv3, humanLv4 = 0, 0, 0
local champHuman, champPeakSum, champLv4 = 0, 0, 0
local matchesHumanLv4, winsHumanLv4 = 0, 0
local stepErrorSeen, stepErrorSamples = {}, {}

for _, rec in ipairs(records) do
	local snap = rec.snap
	for _, v in ipairs(snap.human.ttk) do
		table.insert(ttkH, v)
	end
	for _, v in ipairs(snap.all.ttk) do
		table.insert(ttkA, v)
	end
	addMap(castsH, snap.human.castsByElement)
	addMap(dmgH, snap.human.dmgByElement)
	addMap(castsA, snap.all.castsByElement)
	addMap(dmgA, snap.all.dmgByElement)
	addMap(killsA, snap.all.killsByElement)
	addMap(evAll, snap.all.events)
	pactChannels += snap.all.pactChannels
	oppPinged += snap.all.oppPinged
	oppFused += snap.all.oppFused

	local pv = rec.prov
	for src, row in pairs(pv.dmg) do
		provDmg[src] = provDmg[src] or {}
		addMap(provDmg[src], row)
	end
	addMap(provKnocks, pv.knocks)
	for el, row in pairs(pv.killMatrix) do
		provKill[el] = provKill[el] or {}
		addMap(provKill[el], row)
	end
	for el, row in pairs(pv.hits) do
		provHits[el] = provHits[el] or {}
		addMap(provHits[el], row)
	end
	addMap(provStateTime, pv.stateTime)
	for el, row in pairs(pv.casts) do
		provCasts[el] = provCasts[el] or {}
		addMap(provCasts[el], row)
	end
	for el, row in pairs(pv.travel) do
		provTravel[el] = provTravel[el] or {}
		addMap(provTravel[el], row)
	end
	for el, row in pairs(pv.hitState) do
		provHitState[el] = provHitState[el] or {}
		addMap(provHitState[el], row)
	end
	--- 2 amostras por partida (os 2 pilotos). Minuto de partida usa a duracao
	--- MEDIDA, nao a agenda: partida de bot acaba por abate, nao por zona.
	local mins = math.max(rec.duration, 1) / 60
	for k, v in pairs(pv.dealt) do
		if k:sub(1, 1) == "u" then
			table.insert(dealtList, v)
			table.insert(dealtRate, v / mins)
		end
	end

	fire.fronts += snap.fire.fronts
	fire.cells += snap.fire.cells
	fire.max = math.max(fire.max, snap.fire.max_cells)
	fire.exhausted += snap.fire.exhausted
	fire.burned += snap.fire.burned
	fire.flammable += snap.fire.flammable

	durations[rec.variant] = durations[rec.variant] or {}
	if rec.ended then
		table.insert(durations[rec.variant], rec.duration)
	else
		notEnded[rec.variant] = (notEnded[rec.variant] or 0) + 1
	end

	local anyLv4 = false
	for key, peak in pairs(snap.shield_peak) do
		if key:sub(1, 1) == "u" then -- userHash e' "u%08x"; bots sao "bot:*"
			humansMeasured += 1
			if peak >= 3 then
				humanLv3 += 1
			end
			if peak >= 4 then
				humanLv4 += 1
				anyLv4 = true
			end
		end
	end
	if anyLv4 then
		matchesHumanLv4 += 1
	end
	local ck = rec.championKey
	if ck and ck ~= "nao-humano" then
		champHuman += 1
		local peak = snap.shield_peak[ck] or 0
		champPeakSum += peak
		if peak >= 4 then
			champLv4 += 1
		end
		if anyLv4 and (snap.shield_peak[ck] or 0) >= 4 then
			winsHumanLv4 += 1
		end
	end

	for _, e in ipairs(rec.stepErrors) do
		if #stepErrorSamples < 5 and not stepErrorSeen[e] then
			stepErrorSeen[e] = true
			table.insert(stepErrorSamples, e)
		end
	end
end

table.sort(ttkH)
table.sort(ttkA)

-- checagem viva do conserto da Parte 1: razao de funil por engajamento NUNCA
-- pode estourar o denominador — se estourar, a varredura FALHA em vez de mentir
assert(oppPinged <= (evAll.sintonia_opportunity or 0), "funil impossivel: mais engajamentos com ping do que engajamentos")
assert(oppFused <= (evAll.sintonia_opportunity or 0), "funil impossivel: mais engajamentos com fusao do que engajamentos")

-- -------------------------------------------------------------------- relatorio
local line = string.rep("=", 70)
local out = {}
local function w(fmt, ...)
	table.insert(out, if select("#", ...) > 0 then string.format(fmt, ...) else fmt)
end

local endedCount = 0
for _, r in ipairs(records) do
	if r.ended then
		endedCount += 1
	end
end

w("")
w(line)
w("ARKANA - RELATORIO DA VARREDURA DE BALANCEAMENTO")
w(line)
w("LEIA ANTES: isto e' SIMULACAO — 2 pilotos rasos + %d bots por partida,", BOTS_PER_MATCH)
w("sem fisica real (harness). Bot nao mira, nao recua e nao aprende como")
w("gente. Serve para achar ASSIMETRIA GROSSEIRA e REGRESSAO entre builds.")
w("NAO decreta balanceamento: numero fino aqui NAO substitui playtest.")
w("")
w("partidas ......... %d rodadas · %d terminaram · %d falharam no boot", #records + #failures, endedCount, #failures)

w("")
w("-- V2: TTK (1o dano sofrido -> queda) --------------------------------")
w("   alvo da auditoria: p50 entre 2,75s e 3,5s em duelo parelho")
w("   human_only    %4d amostras   p10 %6.2fs   p50 %6.2fs   p90 %6.2fs", #ttkH, percentile(ttkH, 0.1), percentile(ttkH, 0.5), percentile(ttkH, 0.9))
w("   all_entities  %4d amostras   p10 %6.2fs   p50 %6.2fs   p90 %6.2fs", #ttkA, percentile(ttkA, 0.1), percentile(ttkA, 0.5), percentile(ttkA, 0.9))
w("   (amostras vem de partidas com bots — incluem fogo cruzado e zona;")
w("    NAO e' duelo 1v1 limpo. A regua limpa e' a tabela teorica abaixo.)")
w("   TTK TEORICO de duelo espelho (Balance puro: so' basico, tudo acerta,")
w("   sem esquiva/tatica/viagem — limite INFERIOR do real; EHP=%d):", Balance.player.hp + Balance.shield.caps[1])
for _, el in ipairs({ "Fire", "Water", "Earth", "Wind", "Lightning" }) do
	local t = mirrorTtk(Balance, el)
	local flag = if t >= 2.75 and t <= 3.5
		then "DENTRO do alvo"
		elseif t < 2.75 then "ABAIXO do alvo (mata rapido demais)"
		else "acima do alvo (esperado p/ limite inferior)"
	w("     %-10s %6.2fs   %s", el, t, flag)
end

local ELS = { "Fire", "Water", "Earth", "Wind", "Lightning" }

w("")
w("-- REGUA DE MESA: DANO POR MANA e DPS SUSTENTADO (so' Balance) -------")
w("   POR QUE ESTA TABELA EXISTE: o indice dano/uso da V4 divide por CASTS.")
w("   Num jogo com economia de mana isso nao e' adimensional — quem custa")
w("   caro por disparo sobe e quem custa barato desce, mesmo com os dois")
w("   rendendo o mesmo por ponto de mana. Aqui esta' a leitura sem esse vies.")
w("   %-10s %6s %6s %11s %8s %13s %8s", "elemento", "dano", "mana", "dano/mana", "vs media", "dps sustent.", "vs media")
local dpm, dps = {}, {}
local dpmSum, dpsSum = 0, 0
for _, el in ipairs(ELS) do
	local b = Balance.elements[el].basic
	dpm[el] = b.dmg / b.manaCost
	--- ritmo sustentado = o menor entre a cadencia da arma e o que a mana paga
	dps[el] = b.dmg * math.min(1 / b.fireRate, Balance.player.manaRegen / b.manaCost)
	dpmSum += dpm[el]
	dpsSum += dps[el]
end
local dpmAvg, dpsAvg = dpmSum / #ELS, dpsSum / #ELS
for _, el in ipairs(ELS) do
	local b = Balance.elements[el].basic
	w("   %-10s %6d %6d %11.3f %7.2fx %12.1f %8.2fx", el, b.dmg, b.manaCost, dpm[el], dpm[el] / dpmAvg,
		dps[el], dps[el] / dpsAvg)
end
w("   maior/menor: dano/mana %.2fx · dps sustentado %.2fx (regua: <= 1,25x)",
	(function()
		local lo, hi = math.huge, 0
		for _, el in ipairs(ELS) do
			lo, hi = math.min(lo, dpm[el]), math.max(hi, dpm[el])
		end
		return hi / lo
	end)(),
	(function()
		local lo, hi = math.huge, 0
		for _, el in ipairs(ELS) do
			lo, hi = math.min(lo, dps[el]), math.max(hi, dps[el])
		end
		return hi / lo
	end)())

w("")
w("-- V4: DOMINANCIA DE ELEMENTO (regua: indice dano/uso <= 1,25x) ------")
w("   %-10s %6s %8s %7s %8s %8s", "elemento", "casts", "dano", "abates", "idx(h)", "idx(a)")
local function usageIndex(casts, dmg, el)
	local cSum, dSum = 0, 0
	for _, e in ipairs(ELS) do
		cSum += casts[e] or 0
		dSum += dmg[e] or 0
	end
	local use = casts[el] or 0
	if use <= 0 or cSum <= 0 or dSum <= 0 then
		return nil
	end
	return ((dmg[el] or 0) / dSum) / (use / cSum)
end
local worstEl, worstIdx = nil, 0
for _, el in ipairs(ELS) do
	local ih = usageIndex(castsH, dmgH, el)
	local ia = usageIndex(castsA, dmgA, el)
	if ia and ia > worstIdx then
		worstEl, worstIdx = el, ia
	end
	w("   %-10s %6d %8d %7d %8s %8s", el, castsA[el] or 0, math.floor(dmgA[el] or 0), killsA[el] or 0,
		if ih then string.format("%.2fx", ih) else "-", if ia then string.format("%.2fx", ia) else "-")
end
if worstEl then
	w("   pior indice: %s a %.2fx — %s", worstEl, worstIdx, if worstIdx > 1.25 then "ESTOURA a regua de 1,25x" else "dentro da regua")
end
w("   (uso e' quase uniforme POR CONSTRUCAO do piloto, que alterna elemento")
w("    a cada 6s: o indice mede rendimento POR DISPARO contra bots, nao a")
w("    meta de escolha de jogadores. Dano de bot nao tem elemento — fora.)")

w("")
w("-- SONDA: DE ONDE VEM O DANO (fonte x elemento) ----------------------")
w("   A dominancia acima le' o evento `damage`, que carimba o elemento ATUAL")
w("   do autor. Esta tabela separa a MESMA soma por QUEM chamou applyDamage.")
local SOURCES = { "projetil", "sintonia", "tiro-bot", "terreno-fogo", "terreno-raio", "zona" }
local function rowTotal(row)
	local t = 0
	for _, v in pairs(row or {}) do
		t += v
	end
	return t
end
local grand = 0
for _, src in ipairs(SOURCES) do
	grand += rowTotal(provDmg[src])
end
w("   %-13s %9s %7s   %s", "fonte", "dano", "share", "elemento carimbado (top)")
for _, src in ipairs(SOURCES) do
	local row = provDmg[src] or {}
	local tot = rowTotal(row)
	local names = {}
	for el, v in pairs(row) do
		table.insert(names, { el = el, v = v })
	end
	table.sort(names, function(a, b)
		return a.v > b.v
	end)
	local parts = {}
	for i = 1, math.min(3, #names) do
		table.insert(parts, string.format("%s %.0f%%", names[i].el, names[i].v / math.max(tot, 1) * 100))
	end
	w("   %-13s %9d %6.1f%%   %s", src, math.floor(tot), tot / math.max(grand, 1) * 100,
		if #parts > 0 then table.concat(parts, " · ") else "-")
end
w("")
w("   dano ATRIBUIVEL a elemento (o que entra na regua de 1,25x):")
w("   %-10s %10s %10s %10s   %s", "elemento", "projetil", "sintonia", "total", "share sintonia")
local attribTotal, sintTotal = 0, 0
for _, el in ipairs(ELS) do
	local pj = (provDmg["projetil"] or {})[el] or 0
	local sn = (provDmg["sintonia"] or {})[el] or 0
	attribTotal += pj + sn
	sintTotal += sn
	w("   %-10s %10d %10d %10d   %9.0f%%", el, math.floor(pj), math.floor(sn), math.floor(pj + sn),
		sn / math.max(pj + sn, 1) * 100)
end
w("   TOTAL      %10d %10d %10d   %9.0f%%", math.floor(attribTotal - sintTotal), math.floor(sintTotal),
	math.floor(attribTotal), sintTotal / math.max(attribTotal, 1) * 100)
w("   (a Sintonia NAO gera `spell_cast`: cada ponto de dano dela entra no")
w("    NUMERADOR da dominancia sem entrar no DENOMINADOR de uso.)")
w("")
w("   INDICE CORRIGIDO — reconstruido do `spell_hit`, que carrega o elemento")
w("   VERDADEIRO do projetil (o `damage` carrega o elemento atual do dono):")
w("   %-10s %7s %7s %8s %10s %8s", "elemento", "casts", "acertos", "acerto%", "dano proj.", "idx")
local trueDmg, trueCast = {}, {}
local tdSum, tcSum = 0, 0
for _, el in ipairs(ELS) do
	local hb = (provHits[el] or {}).basic or 0
	local ht = (provHits[el] or {}).tactic or 0
	local d = hb * Balance.elements[el].basic.dmg + ht * Balance.elements[el].tactic.dmg
	trueDmg[el], trueCast[el] = d, castsA[el] or 0
	tdSum += d
	tcSum += trueCast[el]
end
for _, el in ipairs(ELS) do
	local hits = ((provHits[el] or {}).basic or 0) + ((provHits[el] or {}).tactic or 0)
	local idx = if trueCast[el] > 0 and tdSum > 0 and tcSum > 0
		then (trueDmg[el] / tdSum) / (trueCast[el] / tcSum)
		else nil
	w("   %-10s %7d %7d %7.0f%% %10d %8s", el, trueCast[el], hits, hits / math.max(trueCast[el], 1) * 100,
		trueDmg[el], if idx then string.format("%.2fx", idx) else "-")
end
w("   (dano nominal do Balance x acertos: ignora escudo/overkill, entao e' o")
w("    RENDIMENTO BRUTO por disparo — a leitura limpa de dominancia de MAGIA.)")
w("")
w("   INDICE A ACERTO IGUAL — a MESMA conta com a taxa de acerto zerada como")
w("   variavel: so' o que o Balance controla (dano nominal x mistura de")
w("   basico/tatico que a mana e o cooldown deixam o jogador conjurar).")
w("   %-10s %8s %8s %11s %9s %9s", "elemento", "basicos", "taticos", "dano/cast", "idx=", "alcance")
local nomSum, nomCast = 0, 0
local nominal = {}
for _, el in ipairs(ELS) do
	local cb = (provCasts[el] or {}).basic or 0
	local ct = (provCasts[el] or {}).tactic or 0
	nominal[el] = cb * Balance.elements[el].basic.dmg + ct * Balance.elements[el].tactic.dmg
	nomSum += nominal[el]
	nomCast += cb + ct
end
for _, el in ipairs(ELS) do
	local cb = (provCasts[el] or {}).basic or 0
	local ct = (provCasts[el] or {}).tactic or 0
	local n = cb + ct
	local perCast = nominal[el] / math.max(n, 1)
	local idxEq = if n > 0 and nomCast > 0 then perCast / (nomSum / nomCast) else nil
	local tr = provTravel[el] or {}
	w("   %-10s %8d %8d %11.2f %8s %8.0f st", el, cb, ct, perCast,
		if idxEq then string.format("%.2fx", idxEq) else "-", (tr.d or 0) / math.max(tr.n or 0, 1))
end
w("   ESTE e' o unico indice que um campo do Balance move. Se ele estiver")
w("   dentro de 1,25x, dominancia observada acima da regua vem de MIRA /")
w("   GEOMETRIA / PILOTO — e nerfar dano trata o sintoma no lugar errado.")
w("")
w("-- SONDA: O BOT FOGE DO FOGO — MAS FOGE BEM? -------------------------")
w("   exposicao: em que estado da FSM os bots passam o tempo (amostra 2 Hz)")
local BSTATES = { "WANDER", "CHASE", "ATTACK", "FLEE_FIRE", "TO_ZONE" }
local stTotal = 0
for _, v in pairs(provStateTime) do
	stTotal += v
end
local expo = {}
for _, st in ipairs(BSTATES) do
	local v = provStateTime[st] or 0
	expo[st] = v / math.max(stTotal, 1)
	w("   %-10s %8.0f s-bot  (%.1f%% do tempo)", st, v, expo[st] * 100)
end
w("")
w("   RENDIMENTO: %% dos acertos de cada elemento que cairam em alvo FUGINDO")
w("   do fogo. Exposicao a FLEE_FIRE = %.1f%%: sobre isso e' vantagem de BOT.", (expo.FLEE_FIRE or 0) * 100)
w("   %-10s %8s %10s %9s %12s", "elemento", "acertos", "em FLEE", "share", "vs exposicao")
for _, el in ipairs(ELS) do
	local row = provHitState[el] or {}
	local tot = 0
	for _, v in pairs(row) do
		tot += v
	end
	local fl = row.FLEE_FIRE or 0
	local share = fl / math.max(tot, 1)
	w("   %-10s %8d %10d %8.0f%% %11.2fx", el, tot, fl, share * 100,
		share / math.max(expo.FLEE_FIRE or 0, 1e-6))
end
w("   (>1x = o elemento colhe alvos que ELE mesmo poe em panico. Humano nao")
w("    corre 1,0 s em linha reta e volta para WANDER: este multiplicador e'")
w("    a parte da dominancia que NAO sobrevive ao playtest humano.)")

w("")
w("-- SONDA: QUEM DA' O GOLPE QUE DERRUBA -------------------------------")
local knockTotal = 0
for _, v in pairs(provKnocks) do
	knockTotal += v
end
for _, src in ipairs(SOURCES) do
	local v = provKnocks[src] or 0
	if v > 0 then
		w("   %-13s %5d quedas (%.0f%%)", src, v, v / math.max(knockTotal, 1) * 100)
	end
end
w("")
w("   eliminacoes creditadas a UM ELEMENTO x fonte REAL do golpe final:")
w("   %-10s %7s %9s %9s %9s %9s %9s", "elemento", "total", "projetil", "sintonia", "terr-fogo", "tiro-bot", "zona/outro")
for _, el in ipairs(ELS) do
	local row = provKill[el] or {}
	local tot = rowTotal(row)
	if tot > 0 then
		local outro = tot - (row["projetil"] or 0) - (row["sintonia"] or 0) - (row["terreno-fogo"] or 0)
			- (row["tiro-bot"] or 0)
		w("   %-10s %7d %9d %9d %9d %9d %9d", el, tot, row["projetil"] or 0, row["sintonia"] or 0,
			row["terreno-fogo"] or 0, row["tiro-bot"] or 0, outro)
	end
end
w("   (credito de abate = ultimo Player que causou dano em <=10s. Fogo de")
w("    terreno NAO credita ninguem: ele MATA e o abate fica com quem atirou")
w("    por ultimo — e com o elemento que esse jogador segura NA HORA.)")

w("")
w("-- ESCUDO EVOLUTIVO (frequencia do nivel 4 e efeito no resultado) ----")
w("   humanos medidos (2 por partida) .... %d", humansMeasured)
w("   chegaram ao nivel 3+ ............... %d (%.0f%%)", humanLv3, humanLv3 / math.max(humansMeasured, 1) * 100)
w("   chegaram ao nivel 4 ................ %d (%.0f%%)", humanLv4, humanLv4 / math.max(humansMeasured, 1) * 100)
w("   partidas com humano nivel 4 ........ %d de %d", matchesHumanLv4, #records)
w("   campeao foi humano ................. %d de %d partidas", champHuman, #records)
if champHuman > 0 then
	w("   pico de escudo medio do campeao .... %.2f (campeoes com nivel 4: %d)", champPeakSum / champHuman, champLv4)
end
if matchesHumanLv4 > 0 then
	w("   quando um humano chegou ao nivel 4, foi campeao em %d/%d dessas partidas", winsHumanLv4, matchesHumanLv4)
else
	w("   (nivel 4 nunca aconteceu na varredura: o snowball nao e' observavel")
	w("    neste regime — 900 de dano causado esta' longe do ritmo dos pilotos)")
end
w("   (bots nao tem escudo evolutivo: a leitura vale so' para os 2 pilotos)")
w("")
w("   SONDA: DANO CAUSADO por piloto por partida (a moeda dos limiares).")
table.sort(dealtList)
table.sort(dealtRate)
w("   limiares do Balance: nv2=%d · nv3=%d · nv4=%d",
	Balance.shield.thresholds[2], Balance.shield.thresholds[3], Balance.shield.thresholds[4])
w("   n=%d amostras   p10 %5.0f   p50 %5.0f   p90 %5.0f   max %5.0f",
	#dealtList, percentile(dealtList, 0.1), percentile(dealtList, 0.5), percentile(dealtList, 0.9),
	if #dealtList > 0 then dealtList[#dealtList] else 0)
w("   por minuto de partida        p50 %5.0f/min   p90 %5.0f/min   max %5.0f/min",
	percentile(dealtRate, 0.5), percentile(dealtRate, 0.9),
	if #dealtRate > 0 then dealtRate[#dealtRate] else 0)
local function reach(th)
	local n = 0
	for _, v in ipairs(dealtList) do
		if v >= th then
			n += 1
		end
	end
	return n
end
for lv = 2, #Balance.shield.thresholds do
	local th = Balance.shield.thresholds[lv]
	w("   alcancaram nv%d (>=%4d de dano) ... %3d de %3d (%.0f%%)", lv, th, reach(th), #dealtList,
		reach(th) / math.max(#dealtList, 1) * 100)
end
w("   (ESTE e' o teste do limiar: nao 'quantos chegaram ao nv4', e sim quanto")
w("    dano um piloto CONSEGUE causar. Piloto atira a cada 0,4s sem lead nem")
w("    recuo: e' PISO de habilidade humana, nunca teto.)")

w("")
w("-- FOGO (orcamento de PROPAGACAO: fuelBudget = %d celulas) -----------", Balance.terrain.fuelBudget)
w("   (teto real por frente = sementes do impacto + fuelBudget: o orcamento")
w("    limita so' o ESPALHAMENTO — Terrain emite cells = seeds + spread)")
w("   frentes de incendio fechadas ....... %d (%.1f por partida)", fire.fronts, fire.fronts / #records)
w("   celulas por ignicao ................ media %.1f / maior %d", if fire.fronts > 0 then fire.cells / fire.fronts else 0, fire.max)
w("   frentes travadas por combustivel ... %d (%.0f%%) — se alto, fuelBudget aperta", fire.exhausted, fire.exhausted / math.max(fire.fronts, 1) * 100)
w("   celulas queimadas por partida ...... media %.1f (%.1f%% da cobertura inflamavel)", fire.burned / #records, fire.burned / math.max(fire.flammable, 1) * 100)

w("")
w("-- RITMO: duracao real por variante de zona --------------------------")
local SCHEDULE = { blitz = 275, media = 330, longa = 435 }
for _, v in ipairs(VARIANTS) do
	local list = durations[v] or {}
	table.sort(list)
	if #list > 0 then
		w("   %-6s n=%-2d  min %4.0fs  p50 %4.0fs  max %4.0fs  (agenda da zona: %ds)%s",
			v, #list, list[1], percentile(list, 0.5), list[#list], SCHEDULE[v],
			if (notEnded[v] or 0) > 0 then string.format("  +%d NAO terminaram", notEnded[v]) else "")
	else
		w("   %-6s nenhuma partida terminou (%d rodadas)", v, notEnded[v] or 0)
	end
end
w("   (partida de bots acaba por ABATE, nao por zona: a duracao aqui mede")
w("    o ritmo do combate simulado, nao a agenda que jogadores vao sentir)")

w("")
w("-- V1: FUNIL DA SINTONIA (agregado; razoes por ENGAJAMENTO, <=100%) --")
local opp = evAll.sintonia_opportunity or 0
w("   oportunidades %d · com >=1 ping %d (%.0f%%) · com fusao %d (%.0f%%)",
	opp, oppPinged, oppPinged / math.max(opp, 1) * 100,
	oppFused, oppFused / math.max(opp, 1) * 100)
w("   pings %d · aceites %d · canalizacoes %d (%d por pacto) · fusoes %d",
	evAll.sintonia_ping or 0, evAll.sintonia_accept or 0, evAll.sintonia_channel_start or 0,
	pactChannels, evAll.sintonia_fired or 0)

if #failures > 0 or #stepErrorSamples > 0 then
	w("")
	w("-- ERROS VISTOS (reportar a raia dona, nao consertar daqui) ----------")
	for _, f in ipairs(failures) do
		w("   boot partida %d (%s): %s", f.i, f.variant, f.err:sub(1, 200))
	end
	for _, e in ipairs(stepErrorSamples) do
		w("   step: %s", tostring(e):sub(1, 200))
	end
end
w(line)
print(table.concat(out, "\n"))
