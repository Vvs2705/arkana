--!nocheck
-- ============================================================================
-- ARKANA — CENÁRIOS DE ROBUSTEZ (raia A: Engenheiro de QA)
-- Roda junto com o portão padrão (`roblox/tools/run.sh`) e também sozinho
-- (`roblox/tools/run.sh scenarios`) quando se quer só estes seis.
--
-- POR QUE ESTE ARQUIVO EXISTE: a fase R8 achou e corrigiu seis travamentos de
-- robustez lendo código e simulando à mão. Correção sem teste é correção com
-- prazo de validade — a fase seguinte reintroduz qualquer uma delas em
-- silêncio. Cada cenário aqui é UMA pergunta de sobrevivência do servidor, e
-- cada um foi validado REINTRODUZINDO o defeito e vendo o teste ficar vermelho
-- (relatado no fecho da R9). Cenário que passa sem nunca ter falhado não é
-- cobertura, é decoração.
--
--   S1  o lobby SEGURA enquanto alguém está no Campo de Provas
--   S2  o arrepio do espírito tem freio (uma marca só, e perto do espírito)
--   S3  bot preso em geometria não trava a partida nem sobrevive para sempre
--   S4  servidor esvaziou: a partida acaba, ninguém nasce plateia de bots
--   S5  existe TETO DE TEMPO e ele acompanha a variante de ritmo
--   S6  `forceStart` de `Ended` não deixa estado vazar entre partidas
--
-- Cada cenário monta um SERVIDOR NOVO (harness próprio, seed própria) — mesmo
-- truque do sweep.luau. Um cenário não pode herdar mundo do outro: é
-- exatamente esse tipo de herança que o S6 existe para proibir.
-- ============================================================================

local SOURCES = ...
local BASE_BALANCE = SOURCES["shared/Balance.luau"]

-- ------------------------------------------------------------------- boot
--- Servidor limpo, na ordem do Main.server.luau (a mesma que o run.luau
--- repete). `zoneVariant` troca a variante de ritmo no FONTE do Balance antes
--- do load — nenhum arquivo de src/ é tocado, é o mesmo caminho do sweep.
local function boot(tag: string, opts: any?): any
	local o = opts or {}
	SOURCES.__seed = o.seed or 771109
	if o.zoneVariant then
		local patched, hits = BASE_BALANCE:gsub('(zoneVariant%s*=%s*")%w+(")', "%1" .. o.zoneVariant .. "%2", 1)
		assert(hits == 1, 'não achei zoneVariant = "..." no Balance.luau')
		SOURCES["shared/Balance.luau"] = patched
	else
		SOURCES["shared/Balance.luau"] = BASE_BALANCE
	end

	local H = assert(loadstring(SOURCES["tools/harness.luau"], "@harness-" .. tag))(SOURCES)
	-- seis servidores × ~130 linhas de relatório afogariam o portão, que é o
	-- produto daqui. Erro engolido continua visível: vai para H.errors.
	H.GLOBALS.print = function() end
	H.buildTree()

	local req = H.require
	local Shared, Server = H.Shared, H.Server
	local Net = req(Shared:WaitForChild("Net"))
	local Balance = req(Shared:WaitForChild("Balance"))
	Net.initServer()
	local Arena = req(Server:WaitForChild("Arena"))
	local Terrain = req(Server:WaitForChild("Terrain"))
	local Combat = req(Server:WaitForChild("Combat"))
	local Sintonia = req(Server:WaitForChild("Sintonia"))
	local Match = req(Server:WaitForChild("Match"))
	Terrain.build(Arena.generate())
	Combat.init(Terrain)
	Sintonia.init(Terrain, Combat)
	Match.init(Terrain, Combat)
	local Tutorial = req(Server:WaitForChild("Tutorial"))
	local Bots = req(Server:WaitForChild("Bots"))

	--- Contador de eventos pela MESMA porta que o run.luau usa (Telemetry.emit).
	--- Sem `Telemetry.init`: aqui ninguém quer relatório, só o fato "aconteceu".
	local events = {}
	local Telemetry = req(Server:WaitForChild("Telemetry"))
	Telemetry.emit = function(name)
		if type(name) == "string" then
			events[name] = (events[name] or 0) + 1
		end
	end

	local ctx: any = { H = H, Net = Net, Balance = Balance, Terrain = Terrain, Combat = Combat }
	ctx.Sintonia, ctx.Match, ctx.Bots, ctx.Tutorial = Sintonia, Match, Bots, Tutorial
	ctx.events, ctx.players, ctx.simulated = events, {}, 0

	--- Mesma fiação por jogador do Main.server.luau (sem a parte de cliente).
	function ctx.addPlayer(name: string, userId: number)
		local p = H.addPlayer(name, userId)
		Combat.onPlayerAdded(p)
		p.CharacterAdded:Connect(function(char)
			local hum = char:WaitForChild("Humanoid")
			hum.WalkSpeed = Balance.player.speed
			hum.MaxHealth = Balance.player.hp
			hum.Health = Balance.player.hp
			Combat.onCharacterAdded(p, char, hum)
		end)
		p:LoadCharacter()
		table.insert(ctx.players, p)
		return p
	end

	--- Saída pela porta do Main (`Players.PlayerRemoving`), na mesma ordem.
	function ctx.removePlayer(p)
		H.removePlayer(p)
		Combat.onPlayerRemoving(p)
		Sintonia.onPlayerRemoving(p)
		pcall(Tutorial.onPlayerRemoving, p)
		Match.onPlayerRemoving(p)
	end

	--- Um quadro do loop mestre. `hook(i)` roda DEPOIS dos step() — é onde os
	--- cenários prendem um bot na geometria ou seguram alguém no centro.
	function ctx.step(dt: number, frames: number?, hook: any?)
		for i = 1, (frames or 1) do
			H.tick(dt)
			H.physics(dt)
			Terrain.step(dt)
			Combat.step(dt)
			Sintonia.step(dt)
			Match.step(dt)
			Tutorial.step(dt)
			ctx.simulated += dt
			if hook then
				hook(i)
			end
		end
	end

	--- Anda até `pred()` virar verdade (ou estourar o teto). Devolve o instante
	--- simulado, ou nil — "não aconteceu em N segundos" é o achado dos cenários
	--- de travamento, então isto nunca vira laço infinito.
	function ctx.until_(dt: number, maxSeconds: number, pred: any, hook: any?)
		local t = 0
		while t < maxSeconds do
			ctx.step(dt, 1, hook)
			t += dt
			if pred() then
				return t
			end
		end
		return nil
	end

	return ctx
end

--- "0s" para um fim que veio no primeiro quadro esconde justamente o que se
--- quer ler: abaixo de 10 s o número sai com casa decimal.
local function fmt(n: number): string
	return string.format(if n < 10 then "%.1fs" else "%.0fs", n)
end

-- ============================================================ S1 — TUTORIAL
--- O Campo de Provas é a primeira coisa que alguém de fora vê, e `startMatch`
--- reconstrói terreno e recarrega personagens. Começar a partida por cima do
--- tutorial destrói a sessão de quem está aprendendo — e era o que acontecia
--- aos 25 s. O lobby tem de SEGURAR enquanto `Tutorial.active`, e SOLTAR
--- assim que a sessão fecha (senão a cura vira a doença: servidor parado).
local function s1()
	local c = boot("s1")
	local p = c.addPlayer("Novato", 5001)
	c.step(0.1, 5) -- o corpo assenta antes de montar o Campo de Provas
	assert(c.Tutorial.start(p), "pré-condição: o Campo de Provas monta no lobby")
	assert(c.Tutorial.active(p), "pré-condição: a sessão do onboarding está aberta")

	-- 70 s = quase TRÊS lobbies (25 s cada) com um jogador só: com bots ligados
	-- `ready` é verdadeiro desde o primeiro quadro, então sem a guarda a partida
	-- já teria começado por cima do tutorial.
	local started = c.until_(0.1, 70, function()
		return c.Match.isPlaying()
	end)
	assert(
		started == nil,
		string.format("a partida começou em %s POR CIMA do tutorial (sessão ainda aberta)", fmt(started or 0))
	)
	assert(c.Tutorial.active(p), "pré-condição: a sessão continuava aberta durante a espera")

	-- e o outro lado da moeda: fechada a sessão, o lobby volta a andar
	c.Tutorial.skip(p)
	assert(not c.Tutorial.active(p), "pré-condição: `skip` fecha a sessão")
	local after = c.until_(0.1, 60, function()
		return c.Match.isPlaying()
	end)
	assert(after ~= nil, "a sessão fechou e a partida NÃO começou em 60 s — o lobby ficou preso")

	return string.format("segurou 70s com o tutorial aberto · partida saiu %s depois do skip", fmt(after))
end

-- ============================================================== S2 — ARREPIO
--- O arrepio (GDD §18.6) revela UM inimigo perto da própria dupla. Sem freio, o
--- morto vira radar do mapa inteiro: pinga sem parar, de qualquer canto. Dois
--- freios têm de existir e são observáveis de fora — UMA marca por espírito, e
--- a posição pedida tem de estar ao alcance do PRÓPRIO espírito.
--- NÃO COBERTO AQUI: o terceiro freio (`TUNE.pingCooldown`, a janela mínima
--- contra pedido a 1 kHz de um canto vazio). Ele só age no caso SEM alvo, que
--- por definição não produz efeito nenhum de fora — aceito e recusado são
--- indistinguíveis pela API pública. Quem cobre é `Match.selfTest`, que afirma
--- a existência da janela. Testar isso aqui seria decoração.
local function s2()
	local c = boot("s2")
	c.addPlayer("Mago1", 5101) -- o parceiro: sem dupla montada não há espírito
	local b = c.addPlayer("Mago2", 5102)
	c.step(0.1, 5)
	assert(c.Match.forceStart(6), "pré-condição: a partida começa")
	c.step(0.1, 20)

	-- `b` cai: a essência nasce no lugar do corpo (o retorno ainda está aberto)
	local charB = b.Character
	local humB = charB and charB:FindFirstChildOfClass("Humanoid")
	assert(humB, "pré-condição: o segundo mago tem corpo")
	local deathAt = charB:FindFirstChild("HumanoidRootPart").Position
	humB.Health = 0
	c.step(0.1, 4)

	-- um inimigo ENCOSTADO na essência: com alvo por perto, o pedido legítimo
	-- tem tudo para dar certo — o que sobra de diferença é o freio.
	local bot = c.Bots.list()[1]
	assert(bot and bot.humanoid.Health > 0, "pré-condição: existe bot vivo para ser revelado")
	bot.root.Parent:PivotTo(c.H.CFrame.new(deathAt + c.H.Vector3.new(14, 0, 0)))

	--- UMA MARCA sai para o espírito E para o parceiro (dois FireClient): o que
	--- interessa aqui é se a marca SAIU, não quantos clientes a receberam.
	local function marked(pos): boolean
		local before = c.H.fireCount.SpiritPing or 0
		c.Match.spiritPing(b, pos)
		return (c.H.fireCount.SpiritPing or 0) > before
	end

	local near = deathAt + c.H.Vector3.new(14, 0, 0)

	-- (1) LONGE DO ESPÍRITO: um segundo bot do outro lado do mapa serviria de
	-- alvo, mas o pedido nem chega à varredura — o alcance é medido a partir da
	-- essência, não do mapa.
	local far = deathAt + c.H.Vector3.new(900, 0, 0)
	local bot2 = c.Bots.list()[2]
	if bot2 then
		bot2.root.Parent:PivotTo(c.H.CFrame.new(far + c.H.Vector3.new(6, 0, 0)))
	end
	assert(not marked(far), "pedido LONGE do espírito virou marca — o morto virou radar do mapa inteiro")

	-- (2) o pedido legítimo funciona (sem isto o teste acima provaria só que o
	-- arrepio está quebrado, e um arrepio quebrado passa em qualquer freio)
	assert(marked(near), "o arrepio legítimo, com inimigo ao alcance, NÃO saiu")

	-- (3) UMA marca por espírito. A janela mínima anti-DoS é passada de
	-- propósito (relógio adiantado): o que se mede aqui é o GASTO do arrepio,
	-- não o cooldown.
	c.step(1.0, 2)
	assert(bot.humanoid.Health > 0, "pré-condição: o alvo continua vivo e ao alcance para o segundo pedido")
	assert(not marked(near), "o espírito marcou DUAS vezes — arrepio que não gasta é wallhack de graça")

	return "longe do espírito: recusado · legítimo: 1 marca · repetido: recusado"
end

-- ================================================================== S3 — BOT
--- Bot preso na geometria (rocha, quina, muro de Terra) não pode virar
--- sobrevivente eterno: a partida não acabaria nunca e o servidor ficaria
--- refém de um Humanoid encostado numa pedra. Duas coisas o resolvem e as duas
--- estão sob teste: o ANTITRAVAMENTO do próprio bot (ele percebe que pediu para
--- andar e não saiu do lugar, e contorna) e a zona, que cobra de quem fica.
local function s3()
	local c = boot("s3", { seed = 90210 })
	c.addPlayer("Mago1", 5201)
	c.addPlayer("Mago2", 5202)
	c.step(0.1, 5)
	assert(c.Match.forceStart(6), "pré-condição: a partida começa")
	c.step(0.1, 10)

	local bot = c.Bots.list()[1]
	assert(bot, "pré-condição: existe bot na partida")
	-- 260 studs do centro: fora da zona desde a primeira fase (startRadius=170).
	-- A cola é reaplicada a CADA quadro, depois do H.physics — é isto que
	-- reproduz "pediu para andar e não saiu do lugar".
	local stuckAt = c.H.CFrame.new(c.H.Vector3.new(260, 6, 0))
	bot.root.Parent:PivotTo(stuckAt)
	local sawUnstick = false
	local glue = function()
		if bot.humanoid.Health > 0 then
			bot.root.Parent:PivotTo(stuckAt)
			if bot.state == "UNSTICK" then
				sawUnstick = true
			end
		end
	end

	local diedAt = c.until_(0.1, 200, function()
		return bot.humanoid.Health <= 0
	end, glue)
	assert(sawUnstick, "o bot ficou 200 s pedindo para andar sem sair do lugar e NINGUÉM percebeu (sem antitravamento)")
	assert(diedAt ~= nil, "o bot colado na geometria sobreviveu 200 s FORA da zona — sobrevivente eterno")

	-- teto DERIVADO do cronograma (o dobro dele), não um número escolhido a olho:
	-- o teto duro do Match é ~1,5x o cronograma, então este limite fica sempre
	-- acima dele — e continua assim se alguém trocar a variante de ritmo.
	local Z = c.Balance.match.zone
	local cap = Z.phases * (Z.holdTime + Z.shrinkTime) * 2
	local endedAt = c.until_(0.1, cap, function()
		return not c.Match.isPlaying()
	end, glue)
	assert(endedAt ~= nil, string.format("a partida não terminou em %s com um bot preso na geometria", fmt(cap)))

	return string.format("antitravamento reagiu · bot preso morreu em %s · partida acabou em %s", fmt(diedAt), fmt(diedAt + endedAt))
end

-- ========================================================= S4 — SERVIDOR VAZIO
--- O mais importante dos seis. Sem humano dentro não se simula battle royale
--- para ninguém: medido antes da correção, 20 s depois do último humano sair a
--- partida seguia `Playing` com 8 bots para plateia nenhuma — e quem entrasse
--- ali nascia ESPECTADOR de uma partida de bots, sem nada na tela explicando.
local function s4()
	local c = boot("s4")
	local a = c.addPlayer("Mago1", 5301)
	local b = c.addPlayer("Mago2", 5302)
	c.step(0.1, 5)
	assert(c.Match.forceStart(8), "pré-condição: a partida começa")
	c.step(0.1, 100) -- 10 s de partida de verdade
	assert(c.Match.isPlaying(), "pré-condição: a partida está rodando com os dois humanos")
	assert(c.Bots.aliveCount() > 0, "pré-condição: os bots estão vivos (a partida não acabou sozinha)")

	c.removePlayer(a)
	assert(c.Match.isPlaying(), "com UM mago dentro a prova continua — bot é companhia, não motivo de fim")
	c.removePlayer(b)

	-- 20 s: a mesma janela em que o defeito foi medido. Passa MUITO de um
	-- quadro: se o fim depende de sorte de tick, isto pega.
	local endedAt = c.until_(0.1, 20, function()
		return not c.Match.isPlaying()
	end)
	assert(endedAt ~= nil, "20 s sem nenhum humano no servidor e a partida seguia `Playing` para plateia nenhuma")

	-- e quem chega DEPOIS não herda a partida dos bots como espectador
	local late = c.addPlayer("Atrasado", 5303)
	c.step(0.1, 20)
	assert(not c.Match.isPlaying(), "a partida de bots recomeçou sozinha por cima de quem acabou de entrar")
	local st = c.H.lastFire.MatchState
	assert(st, "pré-condição: o servidor manda estado para quem entrou")
	assert(not st.spectating, "quem entrou depois do esvaziamento nasceu ESPECTADOR de uma partida de bots")

	return string.format("acabou %s depois do último humano sair · quem entra depois não nasce plateia", fmt(endedAt))
end

-- ============================================================ S5 — TETO DE TEMPO
--- A fase final da zona não fecha até zero: ela PARA em `endRadius` e segura.
--- Duplas paradas dentro desse círculo (AFK, cliente travado, griefer parado de
--- propósito) deixam a máquina de estados em `Playing` para sempre — medido em
--- 900 s antes da correção. O teto tem de existir E ser DERIVADO do cronograma
--- (`matchTimeLimit`), não um número fixo: mudando a variante de ritmo, o teto
--- muda junto. Quatro AFK colados no centro da zona, zero bots, ninguém
--- atacando ninguém: a ÚNICA saída possível é o teto.
local function s5()
	local measured, detail = {}, {}
	for _, variant in ipairs({ "blitz", "media", "longa" }) do
		local c = boot("s5-" .. variant, { zoneVariant = variant })
		local Z = c.Balance.match.zone
		local schedule = Z.phases * (Z.holdTime + Z.shrinkTime)
		for i = 1, 4 do -- 4 magos = DUAS duplas: ninguém vence por dupla única
			c.addPlayer("Afk" .. i, 5400 + i)
		end
		c.step(0.1, 5)
		assert(c.Match.forceStart(0), "pré-condição: a partida começa sem bots")

		--- colados no centro da zona a cada quadro: fora do dano da zona, longe
		--- de qualquer inimigo, sem nenhuma intenção C→S. Partida imortal.
		local afk = function()
			local st = c.H.lastFire.MatchState
			local at = st and st.zoneCenter
			if not at then
				return
			end
			for _, p in ipairs(c.players) do
				if p.Character then
					p.Character:PivotTo(c.H.CFrame.new(at + c.H.Vector3.new(0, 4, 0)))
				end
			end
		end

		local endedAt = c.until_(0.2, schedule * 3, function()
			return not c.Match.isPlaying()
		end, afk)
		assert(
			endedAt ~= nil,
			string.format("variante %s: %s de AFK no círculo final e ainda `Playing` — sem teto de tempo", variant, fmt(schedule * 3))
		)
		assert(
			endedAt > schedule,
			string.format("variante %s: o teto (%s) cortou a partida DENTRO do cronograma da zona (%s)", variant, fmt(endedAt), fmt(schedule))
		)
		measured[variant] = endedAt
		table.insert(detail, string.format("%s %s/%s", variant, fmt(endedAt), fmt(schedule)))
	end

	-- DERIVADO, não fixo: teto constante daria os três iguais. Este é o teste
	-- que separa "existe um número grande no código" de "o teto acompanha o
	-- cronograma que o Balance define".
	assert(
		measured.blitz < measured.media and measured.media < measured.longa,
		string.format(
			"o teto NÃO acompanha a variante de ritmo (blitz %s · media %s · longa %s) — é número fixo, não derivado do cronograma",
			fmt(measured.blitz), fmt(measured.media), fmt(measured.longa)
		)
	)

	return "teto por variante (fim/cronograma): " .. table.concat(detail, " · ")
end

-- ========================================================== S6 — forceStart
--- O segundo mais importante. Estado que atravessa partidas já vazou TRÊS vezes
--- neste projeto (escudo dourado, cooldown da Sintonia, e as duas de novo pela
--- porta do `forceStart`, que pula o `toLobby` onde morava a limpeza). Aqui a
--- partida é encerrada e recomeçada PELA PORTA QUE PULA O LOBBY, com as duas
--- coisas sujas de propósito: escudo no nível 4 e a dupla travada no cooldown
--- compartilhado de 24 s.
local function s6()
	local c = boot("s6")
	local a = c.addPlayer("Mago1", 5501)
	local b = c.addPlayer("Mago2", 5502)
	c.step(0.1, 5)
	assert(c.Match.forceStart(4), "pré-condição: a partida começa")
	c.step(0.1, 20)

	-- (1) SUJEIRA A: escudo dourado. Dano de verdade em INIMIGO (o único que
	-- evolui: dano em aliado não conta), com o alvo recuperado a cada golpe para
	-- a evolução chegar ao topo sem a partida acabar.
	local bot = c.Bots.list()[1]
	assert(bot, "pré-condição: existe bot inimigo para bater")
	for _ = 1, 400 do
		if c.Combat.getShieldLevel(a) >= 4 then
			break
		end
		c.Combat.applyDamage(bot.humanoid, 20, a, false)
		bot.humanoid.Health = bot.humanoid.MaxHealth
	end
	assert(c.Combat.getShieldLevel(a) == 4, "pré-condição: dá para chegar ao escudo nível 4 batendo em inimigo")

	-- (2) SUJEIRA B: cooldown compartilhado da Sintonia, cobrado NO INÍCIO da
	-- canalização (GDD §9) e válido por 24 s — mais que a tela de fim inteira.
	local pos = c.H.Vector3.new(0, 0, 0)
	local function pact()
		c.Sintonia.registerCast(a, "Fire", pos)
		c.Sintonia.registerCast(b, "Water", pos)
	end
	local channels = function()
		return c.events.sintonia_channel_start or 0
	end
	pact()
	assert(channels() == 1, "pré-condição: a dupla consegue canalizar uma Sintonia")
	c.step(0.1, 20) -- a fusão sai e solta os dois; o cooldown FICA
	pact()
	assert(channels() == 1, "pré-condição: o cooldown compartilhado está de pé (segunda fusão recusada)")

	-- A PORTA: encerra e recomeça DIRETO da tela de fim, sem passar pelo lobby.
	assert(c.Match.forceEnd(), "pré-condição: a partida em andamento encerra")
	assert(not c.Match.isPlaying(), "pré-condição: o estado é `Ended`")
	assert(c.Match.forceStart(4), "`forceStart` a partir de `Ended` foi recusado — a porta não existe mais")
	c.step(0.1, 20)

	assert(
		c.Combat.getShieldLevel(a) == 1,
		string.format(
			"o Escudo Evolutivo VAZOU para a partida nova (nível %d): quem terminou dourado começa dourado",
			c.Combat.getShieldLevel(a)
		)
	)
	pact()
	assert(
		channels() == 2,
		"o cooldown da Sintonia VAZOU para a partida nova: quem fundiu no fim entra travado 24 s na seguinte"
	)

	return "escudo volta ao nível 1 · cooldown da Sintonia zerado na porta que pula o lobby"
end

-- ================================================================== runner
local SCENARIOS = {
	{ "S1 tutorial segura o lobby", s1 },
	{ "S2 arrepio tem freio", s2 },
	{ "S3 bot preso não trava", s3 },
	{ "S4 servidor vazio encerra", s4 },
	{ "S5 teto de tempo derivado", s5 },
	{ "S6 forceStart não vaza estado", s6 },
}

--- `record(nome, ok, detalhe)` — o mesmo formato do run.luau, para os cenários
--- entrarem na CONTAGEM do portão padrão. Teste que só roda quando alguém
--- lembra não é portão.
return function(record): number
	local failed = 0
	for _, entry in ipairs(SCENARIOS) do
		local ok, result = xpcall(entry[2], function(err)
			return tostring(err)
		end)
		if not ok then
			failed += 1
		end
		record(entry[1], ok, tostring(result))
	end
	return failed
end
