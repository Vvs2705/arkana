--!nocheck
-- ============================================================================
-- ARKANA — CENÁRIOS DE ROBUSTEZ (raia A: Engenheiro de QA)
-- Roda junto com o portão padrão (`roblox/tools/run.sh`) e também sozinho
-- (`roblox/tools/run.sh scenarios`) quando se quer só estes oito.
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
--   S7  MARATONA: seis provas encadeadas com gente entrando e saindo, e o que
--       cresce medido por CONTAGEM (o S6 vê a virada entre duas partidas; o S7
--       vê o servidor de alpha, que roda por horas)
--   S8  os TRÊS módulos que o S7 não carrega — `Telemetry` de verdade,
--       `Grimoire` e `Prefs` — com doze pessoas distintas entrando e saindo:
--       o S7 mede o que a PROVA deixa para trás, o S8 mede o que a PESSOA
--       deixa para trás
--
-- Cada cenário monta um SERVIDOR NOVO (harness próprio, seed própria) — mesmo
-- truque do sweep.luau. Um cenário não pode herdar mundo do outro: é
-- exatamente esse tipo de herança que o S6 existe para proibir. O S7 e o S8 são
-- as exceções deliberadas: cada um é UM servidor só, de propósito, porque a
-- pergunta dos dois é justamente o que sobra de uma prova (S7) ou de um
-- jogador (S8) para o seguinte.
-- ============================================================================

local SOURCES = ...
local BASE_BALANCE = SOURCES["shared/Balance.luau"]

-- ------------------------------------------------------------------- boot
--- Servidor limpo, na ordem do Main.server.luau (a mesma que o run.luau
--- repete). `zoneVariant` troca a variante de ritmo no FONTE do Balance antes
--- do load — nenhum arquivo de src/ é tocado, é o mesmo caminho do sweep.
---
--- `opts.full` (R14) sobe os TRÊS módulos que faltavam — `Telemetry` de
--- verdade (com `init`, em vez do `emit` trocado por contador), `Grimoire` e
--- `Prefs` — e espelha a fiação por jogador do Main para eles. É a ÚNICA
--- diferença, e ela está toda atrás deste booleano: sem `full` cada linha
--- abaixo executa exatamente o que executava antes desta fase, na mesma ordem,
--- com o mesmo conjunto de módulos carregados. Foi por isso que a opção entrou
--- AQUI em vez de virar um `boot` paralelo: S1–S6 são regressões que custaram
--- caro e um segundo boot copiado começa igual e termina divergindo em
--- silêncio — o dia em que alguém consertar um e esquecer o outro, o cenário
--- de regressão passa a medir um servidor que não existe mais.
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
	--- Sem `full`, NÃO há `Telemetry.init` e o original nunca roda: aqui ninguém
	--- quer relatório, só o fato "aconteceu". Com `full` o contador DELEGA ao
	--- coletor de verdade — é o estado interno dele que o S8 mede.
	local events, lastPayload = {}, {}
	local Telemetry = req(Server:WaitForChild("Telemetry"))
	local realEmit = Telemetry.emit
	Telemetry.emit = function(name, subject, payload)
		if type(name) == "string" then
			events[name] = (events[name] or 0) + 1
			--- guarda o ÚLTIMO payload: um evento que acontece não prova que ele
			--- acontece CARIMBADO, e carimbo errado é falha calada (o dado sai,
			--- só sai mentindo). Ver o S1 e a exclusão do treino no funil da V1.
			lastPayload[name] = payload
		end
		if o.full then
			return realEmit(name, subject, payload)
		end
		return nil
	end

	--- OS TRÊS QUE FALTAVAM. Ordem do Main.server.luau: `Grimoire.init` depois
	--- do `Telemetry.init`, porque ele ENVOLVE `Telemetry.emit` para observar
	--- `sintonia_fired`/`match_end` — envolvendo o contador acima, que por sua
	--- vez chama o original. `Prefs.init` se fia sozinho em
	--- Players.PlayerAdded/PlayerRemoving, que é o que `H.addPlayer`/
	--- `H.removePlayer` disparam.
	local Grimoire, Prefs, Events = nil, nil, nil
	if o.full then
		Events = req(Shared:WaitForChild("Events"))
		Telemetry.init(Terrain, Combat, Match)
		Grimoire = req(Server:WaitForChild("Grimoire"))
		Grimoire.init(Terrain, Combat)
		Prefs = req(Server:WaitForChild("Prefs"))
		Prefs.init()
	end

	local ctx: any = { H = H, Net = Net, Balance = Balance, Terrain = Terrain, Combat = Combat }
	ctx.Sintonia, ctx.Match, ctx.Bots, ctx.Tutorial = Sintonia, Match, Bots, Tutorial
	ctx.events, ctx.players, ctx.simulated = events, {}, 0
	ctx.lastPayload = lastPayload
	ctx.Telemetry, ctx.Grimoire, ctx.Prefs = Telemetry, Grimoire, Prefs

	--- Mesma fiação por jogador do Main.server.luau (sem a parte de cliente).
	function ctx.addPlayer(name: string, userId: number)
		local p = H.addPlayer(name, userId)
		if o.full then
			--- ANTES da fiação, como no Main: é o `session_start` da pergunta V5.
			Telemetry.emit(Events.Name.SessionStart, p)
		end
		Combat.onPlayerAdded(p)
		if o.full then
			Grimoire.onPlayerAdded(p)
		end
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
		if o.full then
			--- PRIMEIRA coisa, como no Main: o `session_end` é enriquecido com o
			--- contexto de quem ainda ESTÁ na partida. Emitido depois da
			--- desmontagem, "onde ele desistiu" sairia vazio.
			Telemetry.emit(Events.Name.SessionEnd, p)
		end
		H.removePlayer(p) -- dispara PlayerRemoving: é aqui que o `Prefs` limpa
		Combat.onPlayerRemoving(p)
		Sintonia.onPlayerRemoving(p)
		pcall(Tutorial.onPlayerRemoving, p)
		if o.full then
			Grimoire.onPlayerRemoving(p)
		end
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
			if o.full then
				Grimoire.step(dt)
			end
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
--- REGRA REESCRITA NA R11 — o Diretor determinou que o tutorial é OPCIONAL.
--- O S1 antigo cobrava o oposto: que o lobby SEGURASSE enquanto alguém treinava
--- (a correção da R8 para "a partida começava por cima do onboarding aos 25 s").
--- Aquela regra tornava o tutorial obrigatório PARA QUEM NEM O PEDIU: um
--- desconhecido aprendendo segurava todo mundo por até 240 s. O conserto não foi
--- mexer no teto — foi tirar a espera e transformar o Campo de Provas em CONVITE.
---
--- O cenário continua existindo porque o defeito original continua possível: o
--- perigo nunca foi "a partida começa", foi "a sessão do onboarding fica ÓRFÃ"
--- (`Tutorial.active` verdadeiro para sempre, que este projeto já viu). Então o
--- S1 troca de pergunta: a partida AGORA TEM de começar, e quem estava treinando
--- tem de sair limpo.
local function s1()
	local c = boot("s1")
	local p = c.addPlayer("Novato", 5001)
	c.step(0.1, 5) -- o corpo assenta antes de qualquer convite

	-- 1) NINGUÉM É EMPURRADO. `offer` pergunta e só; sem aceite não há sessão.
	assert(c.Tutorial.offer(p), "pré-condição: o convite sai para quem acabou de entrar")
	assert(
		not c.Tutorial.active(p),
		"o Campo de Provas ABRIU SOZINHO: convite que começa sem resposta é o tutorial obrigatório de volta"
	)

	-- 2) o jogador ACEITA — pelo payload REAL do cliente, não por chamada direta:
	-- foi um contrato de payload divergente entre estas duas pontas que uma vez
	-- impediu QUALQUER partida de começar, em qualquer servidor.
	c.Net.event(c.Net.Names.Tutorial).OnServerEvent:Fire(p, { step = "accept" })
	assert(c.Tutorial.active(p), "aceitar o convite TEM de abrir a sessão (contrato C→S do TutorialUi)")

	-- 2b) O PING DISPARADO DE DENTRO DO TREINO SAI CARIMBADO. O Campo de Provas
	-- usa o ping REAL (é o que torna o passo honesto) — e sem o carimbo ele entra
	-- no funil da pergunta V1, que existe para medir se duas pessoas coordenam
	-- SEM que alguém mande. Antes da R11 todo jogador era empurrado ao tutorial,
	-- então cada sessão injetava treino no numerador da pergunta central do
	-- produto. Quem EXCLUI é o Telemetry; aqui se prova que o carimbo chega.
	local root = p.Character and p.Character:FindFirstChild("HumanoidRootPart")
	if root then
		c.Sintonia.propose(p, root.Position)
		local ping = c.lastPayload["sintonia_ping"]
		assert(ping ~= nil, "pré-condição: o ping do Campo de Provas chega à telemetria")
		assert(
			ping.tutorial == true,
			"ping disparado DENTRO do treino não veio carimbado: ele entra no funil da V1 como coordenação real"
		)
	end

	-- 3) O LOBBY NÃO ESPERA. Com bots ligados `ready` é verdadeiro desde o
	-- primeiro quadro, então a partida tem de sair no tempo normal do lobby —
	-- mesmo com a sessão do onboarding aberta. É o inverso exato do S1 antigo.
	local started = c.until_(0.1, 70, function()
		return c.Match.isPlaying()
	end)
	assert(
		started ~= nil,
		"o lobby ficou preso 70 s por causa de quem estava treinando — tutorial opcional não pode parar o servidor"
	)

	-- 4) E O TREINANDO NÃO FICA ÓRFÃO. Este é o defeito que o S1 sempre existiu
	-- para pegar: sessão aberta para sempre trava o jogador num Campo de Provas
	-- que não existe mais (o `startMatch` reconstrói o terreno por baixo dele).
	c.step(0.1, 20)
	assert(
		not c.Tutorial.active(p),
		"a prova começou e a sessão do onboarding continuou ABERTA: o jogador ficou preso num Campo de Provas que o startMatch já desmontou"
	)
end

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

-- ============================================================ S7 — MARATONA
--- O RISCO QUE NENHUM DOS SEIS ACIMA MEDE. S1–S6 olham UMA partida (o S6, a
--- virada entre duas). Um alpha não é uma partida: é um servidor público
--- rodando por HORAS, com dezenas de pessoas entrando e saindo. As tabelas
--- deste projeto são indexadas por jogador e por partida, e estado que
--- atravessa partida já vazou TRÊS vezes aqui — as três achadas POR ACASO.
--- Vazamento assim não dá erro: dá servidor que fica lento aos 40 min e cai no
--- meio do playtest, com o Diretor sem entender por quê.
---
--- SEIS provas encadeadas, com gente entrando e saindo ENTRE elas e NO MEIO
--- delas, pelas DUAS portas de partida nova: o ciclo normal
--- (`Ended → Lobby → startMatch`) e o `forceStart` direto de `Ended`, que pula
--- o `toLobby` — foi por essa porta que dois dos três vazamentos voltaram.
---
--- TRÊS PERGUNTAS, e as três são CONTAGEM, não estimativa de memória:
---   1. saiu do servidor, sumiu da tabela — provado DUAS vezes: no quadro exato
---      da saída (a entrada dele existia e some) e como rede permanente
---      (tabela por jogador nunca tem mais entradas do que gente conectada);
---   2. nada por partida cresce — o anel de altares é o que o Balance pede, as
---      peças do mundo batem uma a uma com as tabelas (tabela limpa com peça
---      órfã é o mesmo vazamento com outra cara) e o Workspace INTEIRO do
---      sexto lobby tem o mesmo tamanho do primeiro;
---   3. a sexta prova começa, anda e termina sozinha como a primeira — uma
---      largada e um fim contados por prova, e `H.errors` vazio no fim de tudo.
---
--- O QUE ESTA MEDIÇÃO NÃO ENXERGA — dito na cara, senão o cenário vira
--- decoração:
---   · TABELAS DE CHAVE FRACA (`lastHit` e `deathWired` no Match, `teamOf` no
---     Teams, `statuses`/`extraTargets` no Combat). O harness guarda TODA
---     Instance que já criou em `H.parts`/`H.humanoids`, então nada nelas é
---     coletado aqui — contá-las seria medir o coletor, não o código. É por
---     isso que ficam fora do `Match.stateCounts` também.
---     R14: continua sem medição, mas deixou de ser só uma frase — o bloco
---     `TETO DO HARNESS` do S8 PROVA que a Instance fica fixada, e fica
---     vermelho no dia em que isso mudar. Enquanto ficar verde, contar chave
---     fraca aqui dá um número errado PARA CIMA, sempre.
---   · MEMÓRIA DE VERDADE. Nada aqui pesa bytes: chave que some pode ter
---     deixado a tabela com a capacidade inteira alocada, e ninguém aqui vê.
---   · O QUE ESTE SERVIDOR NÃO CARREGA. O `boot` sobe Terrain/Combat/Sintonia/
---     Match/Tutorial/Bots; `Telemetry.emit` é substituído por um contador e
---     `Grimoire`/`Prefs` nem entram. Estado que cresça LÁ (as tabelas por
---     `subjectKey` do Telemetry, por exemplo) passa por baixo do S7 inteiro.
---     R14: FECHADO pelo S8, que sobe os três com `boot(..., { full = true })`.
---     O S7 continua rodando sem eles de propósito — ele é a regressão de
---     partida, e trocar o servidor dele seria trocar o que ele mede.
---   · CONEXÕES. `Connect` sem `Disconnect` é vazamento clássico e não tinha
---     contador nenhum neste harness.
---     R14: existe `H.aliveConnections` e o S8 o usa em DELTA sobre os sinais
---     longevos. O que continua não existindo é o contrato "quantas conexões
---     são normais" — por isso NÃO há número absoluto em lugar nenhum.
local function s7()
	local c = boot("s7", { seed = 424242 })
	--- NÚCLEO que nunca sai. Sem humano dentro o `mustEndNow` encerra por
	--- servidor vazio — e a maratona estaria medindo o S4 de novo, não o que
	--- cresce. Toda prova aqui acaba porque ACABOU, nunca porque esvaziou.
	c.addPlayer("Fixo1", 5701)
	c.addPlayer("Fixo2", 5702)
	c.step(0.1, 5)

	local nextId = 5710
	local function join(): any
		nextId += 1
		return c.addPlayer("Passante" .. nextId, nextId)
	end
	local function stateNow(): string
		local p = c.H.lastFire.MatchState
		return (p and p.state) or "?"
	end

	local Z = c.Balance.match.zone
	local schedule = Z.phases * (Z.holdTime + Z.shrinkTime)
	--- lido do Balance, não cravado: o anel de altares é dado, e o teste não
	--- pode ficar vermelho porque alguém rebalanceou o ritual.
	local ALTARS = ((c.Balance.match :: any).revive or {}).altars or 4

	--- AS DUAS RÉGUAS, lidas no mesmo instante:
	--- (1) `Match.stateCounts()` — contagem de CHAVES das tabelas do módulo;
	--- (2) varredura do Workspace pelas peças que a partida cria (altar, farol,
	---     essência, cilindro da zona). Só a segunda vê peça órfã de tabela já
	---     limpa; só a primeira vê chave órfã de peça já destruída.
	local PARTS = {
		CirculoDeInvocacao = "altarParts",
		FarolDoAltar = "altarParts",
		EssenciaArcana = "essenciaParts",
		ArcaneZone = "zonaParts",
	}
	local function census(): any
		local m = c.Match.stateCounts()
		m.altarParts, m.essenciaParts, m.zonaParts = 0, 0, 0
		for _, inst in ipairs(c.H.Workspace:GetChildren()) do
			local key = PARTS[inst.Name]
			if key then
				m[key] += 1
			end
		end
		m.bots = #c.Bots.list()
		m.online = #c.H.Players:GetPlayers()
		--- E O MUNDO INTEIRO, sem lista de nomes: é a rede que pega o que a
		--- varredura por nome não previu — corpo de quem saiu, modelo de bot,
		--- projétil, efeito de terreno. Foi ela que achou o defeito de fidelidade
		--- do harness (um Model por jogador que passava, acumulando no Workspace).
		m.mundo = #c.H.Workspace:GetChildren()
		return m
	end

	--- Invariantes que valem em QUALQUER instante do ciclo — é o que separa
	--- "rodei seis provas e nada explodiu" de medição.
	local function check(where: string)
		local m = census()
		for _, key in ipairs({ "eliminated", "stats", "spirits" }) do
			assert(
				m[key] <= m.online,
				string.format(
					"%s: `%s` tem %d entradas para %d jogadores conectados — sobrou estado por jogador de quem JÁ SAIU do servidor",
					where, key, m[key], m.online
				)
			)
		end
		--- POR PARTIDA NÃO CRESCE: o anel de altares é do tamanho que o Balance
		--- pede, na primeira prova e na sexta. A identidade com as peças do
		--- mundo sozinha NÃO pega isto — quando a prova nova empilha altares em
		--- cima da anterior, tabela e mundo crescem JUNTOS e continuam batendo.
		assert(
			m.altars <= ALTARS,
			string.format(
				"%s: %d altares para os %d que o Balance pede — a prova nova empilhou o anel em cima do da anterior",
				where, m.altars, ALTARS
			)
		)
		assert(
			m.altarParts == m.altars * 2,
			string.format(
				"%s: %d peças de altar no mundo para %d esperadas — altar órfão no Workspace a cada prova",
				where, m.altarParts, m.altars * 2
			)
		)
		assert(
			m.essenciaParts == m.spirits,
			string.format(
				"%s: %d essências no mundo para %d espíritos na tabela — essência órfã sobrevive ao espírito",
				where, m.essenciaParts, m.spirits
			)
		)
		assert(
			m.zonaParts <= 1,
			string.format("%s: %d cilindros de zona no mundo — a prova nova não reaproveitou o da anterior", where, m.zonaParts)
		)
		return m
	end

	local MATCHES = 6
	local firstLobby, lastLobby, firstEnd, lastEnd = nil, nil, 0, 0

	for m = 1, MATCHES do
		--- provas 1 e 6 rodam INTEIRAS, até resolver por dupla: é o par que
		--- responde "a enésima prova ainda é a primeira?". As do meio são
		--- cortadas — elas estão aqui pelo VAI-E-VEM e pelas duas portas, não
		--- pela duração.
		local full = m == 1 or m == MATCHES
		local a, b = join(), join()
		c.step(0.1, 5)
		assert(c.Match.forceStart(4), string.format("prova %d: o servidor não começa mais (`forceStart` recusado)", m))

		--- anda até `secs`, mas para assim que a prova acaba (o que vem depois
		--- do fim é o ciclo, não a prova).
		local function play(secs: number)
			c.until_(0.1, secs, function()
				return not c.Match.isPlaying()
			end)
		end
		play(10)

		--- TRÊS SAÍDAS, UMA POR TABELA. Cada uma é medida com o MESMO padrão:
		--- prova-se que a entrada EXISTE (senão o teste de limpeza passa por
		--- vacuidade — foi assim que quatro testes desta série passaram), sai o
		--- jogador, e o censo é lido no MESMO quadro. Sem quadro no meio não há
		--- outra coisa que possa ter mexido na contagem: a diferença é dele.
		local function leaves(who: any, key: string, label: string)
			local before = c.Match.stateCounts()[key]
			c.removePlayer(who)
			local after = c.Match.stateCounts()[key]
			assert(
				after == before - 1,
				string.format(
					"prova %d: %s saiu do servidor e `%s` foi de %d para %d — a entrada dele ficou na tabela (num alpha de horas, isso é uma por jogador que passa)",
					m, label, key, before, after
				)
			)
		end

		-- (1) `stats` — `a` deixa RASTRO batendo em inimigo. Ele só sai lá na
		-- tela de fim: a entrada tem de atravessar a prova inteira e sumir na
		-- saída, não antes.
		local bot = c.Bots.list()[1]
		assert(bot and bot.humanoid.Health > 0, string.format("prova %d: pré-condição — existe bot vivo para o passante bater", m))
		c.Combat.applyDamage(bot.humanoid, 20, a, false)
		assert(
			c.Match.stateCounts().stats > 0,
			string.format("prova %d: pré-condição — bater em inimigo cria a entrada em `stats`", m)
		)

		-- (2) `spirits` — `b` CAI (essência de verdade no chão) e SÓ ENTÃO dá
		-- alt-F4. O caso mais comum do alpha: quem morre cedo fecha o jogo.
		local humB = b.Character and b.Character:FindFirstChildOfClass("Humanoid")
		assert(humB, string.format("prova %d: pré-condição — o passante tem corpo", m))
		humB.Health = 0
		c.step(0.1, 3)
		assert(
			c.Match.stateCounts().spirits > 0,
			string.format("prova %d: pré-condição — quem cai cedo vira espírito com essência no chão", m)
		)
		leaves(b, "spirits", "o espírito")
		c.step(0.1, 3)

		-- (3) `eliminated` — alguém ENTRA no meio da prova (nasce "late", fora da
		-- contagem) e desiste ainda eliminado, sem nunca ter jogado.
		play(5)
		local late = join()
		c.step(0.1, 5)
		assert(
			c.Match.stateCounts().eliminated > 0,
			string.format("prova %d: pré-condição — quem entra no meio da prova nasce fora da contagem", m)
		)
		play(5)
		leaves(late, "eliminated", "o atrasado")
		check(string.format("prova %d em andamento", m))

		if full then
			--- SOZINHA, sem `forceEnd`. Teto DERIVADO do cronograma (mesmo truque
			--- do S3): fica acima do teto duro do Match, então "não terminou aqui"
			--- só pode querer dizer que nem a rede de segurança funciona mais.
			--- Se a prova acabou por resolução ou pelo teto é do S5; aqui a
			--- pergunta é se a enésima ainda TERMINA como a primeira.
			local endedAt = c.until_(0.1, schedule * 1.5, function()
				return not c.Match.isPlaying()
			end)
			assert(endedAt ~= nil, string.format("prova %d não terminou sozinha em %s", m, fmt(schedule * 1.5)))
			if m == 1 then
				firstEnd = endedAt
			else
				lastEnd = endedAt
			end
		elseif c.Match.isPlaying() then
			assert(c.Match.forceEnd(), string.format("prova %d: a prova em andamento não encerra", m))
		end
		--- UMA largada e UM fim por prova, sempre — nem evento a menos (a
		--- enésima parou de acontecer) nem a mais (a máquina de estados começou
		--- a disparar duas vezes). Vale para as seis, cortadas ou não.
		assert(
			c.events.match_start == m and c.events.match_end == m,
			string.format(
				"prova %d: %s largadas e %s fins contados — a máquina de estados deixou de dar uma volta por prova",
				m, tostring(c.events.match_start), tostring(c.events.match_end)
			)
		)

		-- SAI ENTRE PROVAS (na tela de fim), com o rastro em `stats` a tiracolo:
		-- a outra metade do vai-e-vem, e a que o `toLobby` não veria.
		leaves(a, "stats", "quem bateu em alguém")
		c.step(0.1, 5)
		check(string.format("prova %d na tela de fim", m))

		--- A PORTA QUE PULA O LOBBY: as provas PARES recomeçam direto de
		--- `Ended` — a volta do laço chama `forceStart` sem passar pelo
		--- `toLobby`, que é onde mora metade da limpeza.
		if m % 2 == 1 or m == MATCHES then
			local back = c.until_(0.1, 25, function()
				return stateNow() == "Lobby"
			end)
			assert(back ~= nil, string.format("prova %d: a tela de fim não devolveu o lobby em 25 s", m))
			c.step(0.1, 10) -- os `LoadCharacter` do `toLobby` drenam aqui
			local lob = check(string.format("lobby depois da prova %d", m))
			--- NO LOBBY tudo que é POR PARTIDA está zerado — é o teste mais duro
			--- que existe para "não cresce": não "cresceu pouco", é zero.
			for _, key in ipairs({ "eliminated", "spirits", "altars", "bots", "altarParts", "essenciaParts", "zonaParts" }) do
				assert(
					lob[key] == 0,
					string.format("lobby depois da prova %d: `%s` ficou em %d — estado da prova anterior sobreviveu ao lobby", m, key, lob[key])
				)
			end
			firstLobby = firstLobby or lob
			lastLobby = lob
		end
	end

	--- O ENÉSIMO LOBBY É O PRIMEIRO? Comparação chave a chave. `online` fica de
	--- fora: quantos jogadores estão no servidor é o ROTEIRO do cenário, não o
	--- que cresce sozinho.
	for key, was in pairs(firstLobby) do
		if key ~= "online" then
			assert(
				lastLobby[key] == was,
				string.format(
					"o lobby da prova %d não é o lobby da prova 1: `%s` era %d e virou %d — estrutura por partida CRESCENDO",
					MATCHES, key, was, lastLobby[key]
				)
			)
		end
	end
	assert(#c.H.errors == 0, string.format("%d erro(s) acumulados nas %d provas: %s", #c.H.errors, MATCHES, tostring(c.H.errors[1] and c.H.errors[1].err)))

	return string.format(
		"%d provas encadeadas · prova 1 terminou sozinha em %s, prova %d em %s · lobby da %da idêntico ao da 1a",
		MATCHES, fmt(firstEnd), MATCHES, fmt(lastEnd), MATCHES
	)
end

-- ==================================================== S8 — OS TRÊS QUE FALTAVAM
--- O QUE A MARATONA NÃO ENXERGAVA. O S7 é UM servidor de seis provas, mas um
--- servidor que NÃO carrega três módulos: o `Telemetry` entra com o `emit`
--- trocado por um contador (o original nunca roda), e `Grimoire` e `Prefs` nem
--- são requeridos. São justamente os três com MAIS tabela por jogador — então
--- estado que crescesse lá passava por baixo da maratona inteira.
---
--- Este cenário sobe os três (`boot(..., { full = true })`) e faz a única coisa
--- que um alpha de horas faz e uma prova não: DOZE pessoas distintas entram e
--- saem, metade no meio da prova e metade na tela de fim. Ele NÃO repete o que
--- o S7 já cobre (altares, espíritos, peças do mundo, a máquina de estados) —
--- três provas curtas bastam para o vai-e-vem, e a prova longa é do S7.
---
--- TRÊS RÉGUAS, todas por CONTAGEM e todas por PORTA PÚBLICA — nenhum arquivo
--- de src/ é tocado, nem para ler estado privado:
---   1. `Telemetry` — a linha "sessoes ... em curso" do `report()` É a contagem
---      de chaves de `sessions`, e `sessions` tem o MESMO ciclo de vida de
---      `deviceTier` e `deviceTouch` (as três nascem no `session_start` e o
---      `closeSession` mata as três juntas). Uma leitura cobre as três tabelas.
---   2. `Prefs` — `Prefs.get(p)` é `nil` para quem o servidor não conhece.
---      Cobrado para CADA um dos que já saíram, não só para o último.
---   3. `Grimoire` — não expõe leitor de tabela, mas expõe a porta C→S "me
---      manda meu grimório", e ela só responde enquanto `tracks[p]` existir. O
---      remote é o detector, e é o mesmo caminho que o cliente usa.
--- Mais o CONTADOR DE CONEXÕES (`H.aliveConnections`), em DELTA: ver o bloco
--- onde ele é montado.
---
--- O QUE ESTE CENÁRIO CONTINUA SEM ENXERGAR:
---   · `sessHumans` (o denominador "humanos distintos" do relatório) CRESCE por
---     jogador distinto, de propósito — é o que impede o acumulado de servidor
---     de fingir conclusão. Aqui ele é medido e LIMITADO por cima, não zerado.
---   · TABELAS DE CHAVE FRACA — ver o bloco `TETO DO HARNESS` logo abaixo, que
---     PROVA por que elas não são mensuráveis aqui em vez de só afirmar.
---   · MEMÓRIA DE VERDADE (bytes). Como no S7: aqui se conta chave.
local function s8()
	local c = boot("s8", { seed = 606061, full = true })

	--- ------------------------------------------------------- TETO DO HARNESS
	--- Por que `lastHit`/`deathWired` (Match), `teamOf` (Teams) e
	--- `statuses`/`extraTargets` (Combat) NÃO são medíveis aqui: são tabelas de
	--- chave fraca, e este harness guarda TODA Instance que já criou em
	--- `H.parts`/`H.humanoids`. Contar chaves nelas mediria o coletor de lixo,
	--- não o código — e daria SEMPRE o número errado, para cima.
	---
	--- Isto era uma frase no cabeçalho do S7. Frase apodrece; esta é a mesma
	--- afirmação como ASSERÇÃO VIVA — se um dia o harness parar de fixar
	--- Instance, este teste fica vermelho e obriga a reler a declaração em vez
	--- de deixar um cenário decorativo medindo o coletor em silêncio.
	do
		local weak = setmetatable({}, { __mode = "k" })
		local ghost = c.H.Instance.new("Humanoid")
		weak[ghost] = true
		ghost:Destroy()
		ghost = nil
		collectgarbage("collect")
		local left = 0
		for _ in pairs(weak) do
			left += 1
		end
		assert(
			left == 1,
			"o harness PAROU de fixar Instance: contagem de tabela de chave fraca virou medição possível — releia esta declaração e a do S7 antes de confiar em qualquer número novo"
		)
	end

	--- NÚCLEO que nunca sai (mesma razão do S7): sem humano dentro o
	--- `mustEndNow` encerra por servidor vazio e a medição vira o S4.
	c.addPlayer("Fixo1", 5801)
	c.addPlayer("Fixo2", 5802)
	c.step(0.1, 5)

	local GRIM = c.Grimoire.REMOTE
	local grimGate = c.Net.event(GRIM).OnServerEvent
	--- "me manda meu grimório" — a única intenção que o cliente pode mandar. O
	--- servidor só responde se `tracks[p]` existir, então resposta para quem já
	--- saiu É a entrada órfã.
	local function grimoireAnswers(p: any): boolean
		local before = c.H.fireCount[GRIM] or 0
		grimGate:Fire(p)
		return (c.H.fireCount[GRIM] or 0) > before
	end

	--- (sessões em curso, humanos distintos) — as duas do relatório. Se as
	--- linhas mudarem de forma isto fica VERMELHO, não passa calado: medição que
	--- deixa de achar o que media não pode virar "zero".
	local function sessionCounts(): (number, number)
		local text = c.Telemetry.report()
		local open = tonumber(string.match(text, "encerradas%s*%+%s*(%d+) em curso"))
		local humans = tonumber(string.match(text, "(%d+) humano%(s%) distinto%(s%)"))
		assert(open and humans, "as linhas de SESSAO sumiram do relatório — a porta de medição do S8 mudou de forma")
		return open, humans
	end

	--- CONEXÕES (item 3 da R14), em DELTA e só sobre sinais LONGEVOS: o
	--- Heartbeat do loop mestre, as duas portas de entrada/saída de jogador e o
	--- `OnServerEvent` de cada remote. Sinal que morre junto com o jogador
	--- (`player.CharacterAdded`) fica de fora de propósito — ali `Connect` sem
	--- `Disconnect` é o padrão certo do engine.
	--- POR QUE DELTA: ninguém neste projeto tem o contrato "quantas conexões são
	--- normais", e inventar um número absoluto seria cravar o estado de hoje
	--- como regra. O delta não precisa desse contrato: doze pessoas entrando e
	--- saindo têm de deixar a conta EXATAMENTE como acharam.
	local WATCHED: { any } = {
		c.H.RunService.Heartbeat,
		c.H.Players.PlayerAdded,
		c.H.Players.PlayerRemoving,
		grimGate,
	}
	for _, name in pairs(c.Net.Names) do
		table.insert(WATCHED, c.Net.event(name).OnServerEvent)
	end
	local function liveConns(): number
		local n = 0
		for _, sig in ipairs(WATCHED) do
			n += c.H.aliveConnections(sig)
		end
		return n
	end

	-- ------------------------------------------------------- PRÉ-CONDIÇÕES
	--- Sem estas quatro, TODA a medição abaixo passa por vacuidade: "ninguém
	--- sobrou na tabela" é trivialmente verdade quando ninguém entrou nela.
	local open0 = sessionCounts()
	assert(open0 == 2, string.format("pré-condição: os dois fixos abriram sessão na telemetria (li %d em curso)", open0))
	assert(grimoireAnswers(c.players[1]), "pré-condição: o grimório de quem ESTÁ conectado responde ao pedido C→S")
	assert(c.Prefs.get(c.players[1]) ~= nil, "pré-condição: quem ESTÁ conectado tem espaço de preferências")
	local conn0 = liveConns()
	assert(conn0 > 0, "pré-condição: existe conexão viva para contar (senão o delta é vácuo)")

	local gone: { any } = {}
	local nextId, joined = 5810, 0
	local function join(): any
		nextId += 1
		joined += 1
		return c.addPlayer("Passante" .. nextId, nextId)
	end
	local function leave(p: any)
		c.removePlayer(p)
		table.insert(gone, p)
	end
	local function stateNow(): string
		local p = c.H.lastFire.MatchState
		return (p and p.state) or "?"
	end

	--- A REDE PERMANENTE: nenhum dos que JÁ SAÍRAM pode ter sobrado em nenhuma
	--- das três tabelas — e a conta é sobre TODOS eles, não sobre o último. É a
	--- diferença entre "a saída limpa" e "o servidor de horas limpa".
	local function check(where: string): (number, number)
		local open, humans = sessionCounts()
		local online = #c.H.Players:GetPlayers()
		assert(
			open <= online,
			string.format(
				"%s: %d sessões ABERTAS na telemetria para %d jogadores conectados — `sessions` (e com ela `deviceTier`/`deviceTouch`) guarda quem já saiu",
				where, open, online
			)
		)
		local prefsLeft, grimLeft = 0, 0
		for _, p in ipairs(gone) do
			if c.Prefs.get(p) ~= nil then
				prefsLeft += 1
			end
			if grimoireAnswers(p) then
				grimLeft += 1
			end
		end
		assert(
			prefsLeft == 0,
			string.format("%s: %d de %d que já saíram ainda têm espaço em `Prefs.slots` — um por jogador que passa pelo servidor", where, prefsLeft, #gone)
		)
		assert(
			grimLeft == 0,
			string.format("%s: %d de %d que já saíram ainda têm grimório no servidor — `Grimoire.tracks` segura o Player de quem fechou o jogo", where, grimLeft, #gone)
		)
		local now = liveConns()
		assert(
			now == conn0,
			string.format("%s: %d conexões vivas nos sinais longevos contra %d do começo — `Connect` por jogador sem `Disconnect`", where, now, conn0)
		)
		return open, humans
	end

	local CYCLES = 3
	local humansEnd = 0
	for m = 1, CYCLES do
		local a, b, cc, d = join(), join(), join(), join()
		c.step(0.1, 5)
		assert(c.Match.forceStart(4), string.format("ciclo %d: o servidor não começa mais (`forceStart` recusado)", m))
		c.step(0.1, 60)

		--- RASTRO DE VERDADE. Sem produzir evento o passante nem entra no
		--- denominador de "humanos distintos" — e a linha que mede o crescimento
		--- por jogador distinto sairia vazia sem ninguém notar.
		local bot = c.Bots.list()[1]
		assert(bot and bot.humanoid.Health > 0, string.format("ciclo %d: pré-condição — existe bot vivo para os passantes baterem", m))
		for _, p in ipairs({ a, b, cc, d }) do
			c.Combat.applyDamage(bot.humanoid, 5, p, false)
			bot.humanoid.Health = bot.humanoid.MaxHealth
		end

		-- METADE sai NO MEIO da prova (o alt-F4 de quem morreu cedo)
		leave(a)
		leave(b)
		c.step(0.1, 10)
		check(string.format("ciclo %d no meio da prova", m))

		if c.Match.isPlaying() then
			assert(c.Match.forceEnd(), string.format("ciclo %d: a prova em andamento não encerra", m))
		end
		c.step(0.1, 10)
		-- e a outra metade na TELA DE FIM (a saída que o `toLobby` não vê)
		leave(cc)
		leave(d)
		check(string.format("ciclo %d na tela de fim", m))

		local back = c.until_(0.1, 25, function()
			return stateNow() == "Lobby"
		end)
		assert(back ~= nil, string.format("ciclo %d: a tela de fim não devolveu o lobby em 25 s", m))
		c.step(0.1, 10)
		local _, humans = check(string.format("lobby depois do ciclo %d", m))
		humansEnd = humans
	end

	--- NO FIM DE TUDO, com os 12 passantes fora: as sessões em curso são
	--- EXATAMENTE os dois fixos. Não "poucas" — dois.
	local openEnd = sessionCounts()
	assert(
		openEnd == 2,
		string.format("depois de %d passantes: %d sessões abertas para os 2 fixos que sobraram — o alpha de horas acumula uma por pessoa", joined, openEnd)
	)
	--- E o que CRESCE de propósito continua limitado por cima: um humano
	--- distinto é uma entrada, nunca mais que isso.
	assert(humansEnd > 0, "pré-condição: os passantes produziram evento (senão a linha de humanos distintos é vácuo)")
	assert(
		humansEnd <= 2 + joined,
		string.format("humanos distintos = %d para %d pessoas que passaram pelo servidor — a tabela do denominador conta gente que não existe", humansEnd, 2 + joined)
	)
	assert(#c.H.errors == 0, string.format("%d erro(s) acumulados: %s", #c.H.errors, tostring(c.H.errors[1] and c.H.errors[1].err)))

	return string.format(
		"%d passantes em %d provas · sessões abertas voltam a %d · Prefs/Grimório zerados para os %d que saíram · conexões vivas %d, sem delta · humanos distintos %d (cresce por gente, de propósito)",
		joined, CYCLES, openEnd, #gone, conn0, humansEnd
	)
end

-- ================================================================== runner
local SCENARIOS = {
	{ "S1 tutorial opcional nao trava o lobby", s1 },
	{ "S2 arrepio tem freio", s2 },
	{ "S3 bot preso não trava", s3 },
	{ "S4 servidor vazio encerra", s4 },
	{ "S5 teto de tempo derivado", s5 },
	{ "S6 forceStart não vaza estado", s6 },
	{ "S7 maratona não vaza estado", s7 },
	{ "S8 Telemetry/Grimoire/Prefs", s8 },
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
