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

	while simulated < cap do
		simulated += DT
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
print(string.format("   %d partidas · seeds distintas · variantes de zona em ciclo (blitz/media/longa)", N))
print("")

local records = {}
local failures = {}
for i = 1, N do
	local variant = VARIANTS[(i - 1) % #VARIANTS + 1]
	local seed = 100003 + i * 7919
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

w("")
w("-- V4: DOMINANCIA DE ELEMENTO (regua: indice dano/uso <= 1,25x) ------")
w("   %-10s %6s %8s %7s %8s %8s", "elemento", "casts", "dano", "abates", "idx(h)", "idx(a)")
local ELS = { "Fire", "Water", "Earth", "Wind", "Lightning" }
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
