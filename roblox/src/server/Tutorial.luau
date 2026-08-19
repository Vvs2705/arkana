--!strict
-- ============================================================================
-- ONBOARDING — CAMPO DE PROVAS (autoridade: servidor)
-- Origem: auditoria externa de 18/08/2026. O onboarding é o que separa
-- "protótipo que o autor sabe jogar" de "jogo que um desconhecido entende".
-- O roteiro abaixo é a spec da auditoria, quase literal — 3 minutos:
--
--   0–20s   uma única mensagem: "Arraste, mire, solte."
--   20–50s  três alvos móveis. ERROU → indicador de antecipação (lead).
--           ACERTOU → NENHUMA tela nova.
--   50–90s  "Queime a passagem." e "Congele o lago." — magia é VERBO SOBRE O
--           MUNDO, não só dano.
--   90–150s parceiro entra + o único tutorial verbal importante: "Pingue COMBO."
--   150–180 "Você já sabe o necessário." + ENTRAR NO ROYALE.
--
-- NADA É SIMULADO. O fogo é o fogo do Terrain, o alvo é alvo válido do Combat
-- (registerTarget) e leva projétil de verdade, o congelamento é o lago de
-- verdade virando ponte. O Campo de Provas é o próprio jogo em escala pequena
-- — se ele mentisse, ensinaria a jogar um jogo que não existe.
--
-- A LOJA NÃO APARECE. Nem pop-up, nem missão, nem nada entre o jogador e a
-- primeira partida (regra explícita da auditoria).
--
-- TELEMETRIA: cada passo emite `Events.Name.TutorialStep` com `completed` e
-- `reason` — é assim que se mede ABANDONO POR PASSO. Uma linha por passo
-- alcançado: contar linhas = alcance, contar `completed = false` = abandono.
-- Require preguiçoso e protegido (padrão do projeto): telemetria caiu, o jogo
-- continua.
--
-- CONVITE, NÃO IMPOSIÇÃO (R11, pedido do Diretor: "tutorial não é obrigatório
-- precisa ser opcional"). Ninguém mais cai dentro do Campo de Provas por ter
-- entrado no servidor. Quem chega recebe UMA pergunta com duas saídas do mesmo
-- peso — `tutorial.offer.yes` (TREINAR) e `tutorial.offer.no` (JOGAR AGORA) —
-- e a sessão só abre em `Tutorial.start`, que só é chamado pela aceitação.
-- Recusar não fecha a porta: fica o botão `tutorial.reopen` no lobby.
--
-- PROTOCOLO (contraparte: client/TutorialUi.luau — os DOIS lados têm autoteste;
-- foi um contrato de payload em desacordo entre estes dois arquivos que travou
-- o jogo inteiro em 18/08):
--   C→S  { step = "accept" }   entrar no Campo de Provas (convite ou reopen)
--        { step = "decline" }  jogar agora (fica com o botão no lobby)
--        { step = "skip" }     sair (PULAR e ENTRAR NO ROYALE são o mesmo)
--   S→C  { step = "offer" }    o convite de duas saídas
--        { step = "reopen" }   só o botão TREINAR (quem já recusou, no lobby)
--        { step = "end" }      tira tudo da tela
--        { step = <passo> }    o roteiro (ver ORDER)
--
-- FIAÇÃO (o COORDENADOR faz nos Main):
--   Tutorial.offer(player)  -- ao entrar (pergunta; NÃO começa nada)
--   Tutorial.step(dt)       -- no Heartbeat
-- O remote é roteado aqui dentro (`wire`), num lugar só.
-- `skip` no último passo conta como CONCLUÍDO; em qualquer outro, como
-- abandono voluntário. O jogador pode sair a qualquer momento — prender quem
-- já sabe jogar é o oposto de onboarding.
-- ============================================================================

local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local Workspace = game:GetService("Workspace")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Balance = require(Shared:WaitForChild("Balance"))
local Elements = require(Shared:WaitForChild("Elements"))
local Events = require(Shared:WaitForChild("Events"))
local Grid = require(Shared:WaitForChild("Grid"))
local Net = require(Shared:WaitForChild("Net"))
local Strings = require(Shared:WaitForChild("Strings"))

local ServerModules = script.Parent :: Instance
local Terrain = require(ServerModules:WaitForChild("Terrain") :: ModuleScript)
local Combat = require(ServerModules:WaitForChild("Combat") :: ModuleScript)

-- ------------------------------------------------------------------ roteiro
--- Ordem dos passos. O nome é o que vai para a telemetria e para o cliente.
local ORDER = { "aim", "targets", "burn", "freeze", "combo", "done" }

--- Teto de tempo por passo (s), derivado da spec (0–20, 20–50, 50–90, 90–150).
--- Estourar NÃO trava ninguém: registra abandono no passo e segue. O último
--- passo não tem teto — quem chegou lá decide quando entrar no Royale.
local LIMIT: { [string]: number } = {
	aim = 20,
	targets = 30,
	burn = 20,
	freeze = 20,
	combo = 60,
	done = math.huge,
}

-- --- ritmo e geometria do Campo (NÃO é balanceamento) -----------------------
local TUNE = {
	targets = 3,
	--- anel dos alvos: perto o bastante para acertar, longe o bastante para o
	--- tempo de viagem do projétil VALER (é o que o lead ensina)
	targetRing = 30,
	targetSwing = 10, -- amplitude do vaivém (studs)
	targetRate = 1.15, -- rad/s do vaivém
	--- 1 de vida: alvo é ALVO, não inimigo. Bot de verdade tem 100 hp e atira de
	--- volta — no primeiro minuto isso é frustração, não ensino.
	targetHp = 1,
	--- janela para o projétil chegar antes de chamar "erro" (o mais lento do
	--- Balance é Terra a 52 studs/s; 30 studs ≈ 0,58 s)
	missGrace = 0.9,
	--- só um lead por vez na tela: piscar a cada tiro vira ruído
	missCooldown = 1.6,
	partnerSide = 9,
	partnerSpeed = 14,
	--- o parceiro "responde" ao ping (não é instantâneo: parece resposta)
	partnerAnswer = 0.7,
	beaconHeight = 60,
	--- TETO ABSOLUTO DA SESSÃO (s). O roteiro inteiro soma 150 s de tetos por
	--- passo, mas o ÚLTIMO passo ("done") não tem teto de propósito — quem chegou
	--- lá decide quando entrar no Royale. Só que "decide" pressupõe alguém no
	--- teclado: um jogador AFK (ou um cliente que nunca mandou a intenção de
	--- sair) ficava em `sessions` PARA SEMPRE, `Tutorial.active` respondia true
	--- para sempre e o lobby do Match — que espera o onboarding terminar — não
	--- começava partida NENHUMA. Bloqueio de servidor inteiro por causa de um
	--- jogador parado. Este teto é a rede de segurança que fecha a sessão de
	--- qualquer jeito; 210 s = os 180 s do roteiro com folga.
	sessionCap = 210,
} :: { [string]: number }

--- noite do GDD §10 (o manto do boneco é a cor do elemento puxada para ela)
local NIGHT = Color3.fromHex("#0B1026")
local function robeOf(color: Color3): Color3
	return color:Lerp(NIGHT, 0.5)
end

-- -------------------------------------------------------------------- tipos
type Dummy = {
	model: Model,
	humanoid: Humanoid,
	root: BasePart,
	home: Vector3,
	axis: Vector3,
	phase: number,
	vel: Vector3,
}

type Cell = { cx: number, cy: number }

type Session = {
	player: Player,
	index: number,
	--- tempo dentro do passo atual
	t: number,
	--- os.clock() da abertura da sessão — base do teto absoluto (TUNE.sessionCap)
	startedAt: number,
	center: Vector3,
	--- cobertura inflamável escolhida (a "passagem") e o lago escolhido
	burn: { Cell },
	lake: { Cell },
	targets: { Dummy },
	partner: Dummy?,
	partnerElement: Elements.Id,
	beacon: BasePart?,
	--- o jogador já conjurou alguma vez? (é o passo 1 inteiro)
	fired: boolean,
	--- instante em que um disparo vira "erro" (0 = nenhum disparo pendente)
	missAt: number,
	missReadyAt: number,
	--- combo: quando o parceiro responde e quando a fusão sai (0 = não agendado)
	answerAt: number,
	fireAt: number,
	comboPos: Vector3,
	--- último `needSwitch` enviado: só reenvia o passo quando a resposta muda
	needSwitch: boolean,
}

local Tutorial = {}

local sessions: { [Player]: Session } = {}
--- quem já PASSOU pelo Campo de Provas nesta sessão de servidor (concluiu ou
--- saiu por vontade própria): não recebe convite, não vê o botão, não repete
local seen: { [Player]: boolean } = {}
--- ANTI-SPAM DO CONVITE. `seen` só cobre quem ENTROU no Campo de Provas —
--- quem recusou nunca abriu sessão e seria perguntado de novo a cada respawn
--- (e `startMatch` recarrega o personagem de todo mundo, toda partida). Esta
--- marca é a que diz "a pergunta já foi feita": nil = nunca perguntado ·
--- "pending" = entrou no meio da prova, pergunta no lobby · "asked" = já
--- perguntado, nunca mais.
local offered: { [Player]: string } = {}
--- passo em que a LARGADA interrompeu o treino. Quem foi cortado retoma daqui
--- no lobby seguinte, em vez de recomeçar do zero (ver `closeMarks`).
local resume: { [Player]: number } = {}
--- último estado de partida visto pelo `step`: a varredura de tela roda na
--- TRANSIÇÃO Lobby↔Playing, não a cada quadro
local wasPlaying = false
local folder: Folder? = nil
local evTutorial: RemoteEvent? = nil
local evSintonia: RemoteEvent? = nil
local evAnnounce: RemoteEvent? = nil
local wired = false

-- ----------------------------------------------------------------- telemetria
--- Mesmo require preguiçoso e protegido do Combat/Sintonia: se o coletor cair,
--- o onboarding continua de pé e só o dashboard fica pobre.
local telemetry: any = nil
local function emit(name: string, subject: any, payload: any?)
	if telemetry == nil then
		local mod = script.Parent and script.Parent:FindFirstChild("Telemetry")
		local ok, m = pcall(function()
			return require(mod :: ModuleScript)
		end)
		telemetry = (ok and m) or false
	end
	if telemetry and telemetry.emit then
		pcall(telemetry.emit, name, subject, payload)
	end
end

-- -------------------------------------------------------------------- helpers
local function events(): RemoteEvent
	if not evTutorial then
		evTutorial = Net.event(Net.Names.Tutorial)
		evSintonia = Net.event(Net.Names.SintoniaState)
		evAnnounce = Net.event(Net.Names.Announce)
	end
	return evTutorial :: RemoteEvent
end

local function bin(): Folder
	if folder and folder.Parent then
		return folder :: Folder
	end
	local f = Instance.new("Folder")
	f.Name = "Tutorial" -- fora de "Arena": o Terrain.build não leva junto
	f.Parent = Workspace
	folder = f
	return f
end

local function sendTo(player: Player, payload: { [string]: any })
	events():FireClient(player, payload)
end

local function send(s: Session, payload: { [string]: any })
	sendTo(s.player, payload)
end

-- --------------------------------------------------- quem pode ser convidado
--- As DUAS perguntas do convite, puras (só leem as marcas). Puras de propósito:
--- é o que o autoteste consegue interrogar sem Studio e sem jogador de verdade.
--- `offerable` = nunca foi perguntado. `reopenable` = já foi perguntado, disse
--- que não (ou foi cortado pela largada) e ainda não passou pelo Campo.
local function offerable(player: Player?): boolean
	return player ~= nil and offered[player] == nil and not seen[player] and sessions[player] == nil
end

local function reopenable(player: Player?): boolean
	return player ~= nil and offered[player] == "asked" and not seen[player] and sessions[player] == nil
end

--- Marcas de fim de sessão. CORTADO PELA LARGADA NÃO É "JÁ TREINOU": o jogador
--- não escolheu sair, então ele não pode perder o direito ao Campo de Provas —
--- volta o botão no lobby seguinte e o roteiro RETOMA do passo onde parou (sem
--- isso, num servidor com lobby de 25 s o treino recomeçaria do passo 1 para
--- sempre e a oferta de "3 minutos" seria mentira).
local function closeMarks(player: Player, index: number, reason: string)
	if reason == "match" then
		resume[player] = index
		offered[player] = "asked" -- devolve o botão TREINAR no lobby seguinte
	else
		seen[player] = true
		resume[player] = nil
	end
end

local function rootOf(player: Player): BasePart?
	local char = player.Character
	if not char or not char.Parent then
		return nil
	end
	return char:FindFirstChild("HumanoidRootPart") :: BasePart?
end

local function elementOf(player: Player): Elements.Id
	local id = Combat.getElement(player)
	return if Elements.isValid(id) then id :: Elements.Id else "Fire"
end

local function flat(v: Vector3): Vector3
	return Vector3.new(v.X, Grid.FLOOR_Y, v.Z)
end

-- --------------------------------------------------------------- corpo blocky
--- Corpo mínimo que o Combat aceita como alvo: `findHit` só pede
--- `Humanoid.RootPart` + vida > 0, e o engine só resolve RootPart num Model com
--- part chamada HumanoidRootPart. Três parts bastam (o rig R6 completo do
--- Bots.luau existe para ANDAR; estes são ancorados e movidos por PivotTo).
---
--- `chest` é a posição do ROOT, não do chão: o projétil do Combat voa na altura
--- do peito de QUEM ATIRA (`spawnProjectile`), então o alvo tem de estar nessa
--- cota. Alvo alinhado ao chão sobraria fora da esfera de acerto e o passo 2
--- ensinaria que "acertar não acerta".
local function buildDummy(name: string, color: Color3, chest: Vector3, hp: number): Dummy
	local model = Instance.new("Model")
	model.Name = "TutorialDummy"

	local function part(pname: string, size: Vector3, c: Color3, y: number): BasePart
		local p = Instance.new("Part")
		p.Name = pname
		p.Size = size
		p.Color = c
		p.Material = Enum.Material.SmoothPlastic -- cor chapada (direção blocky)
		p.TopSurface = Enum.SurfaceType.Smooth
		p.BottomSurface = Enum.SurfaceType.Smooth
		p.Anchored = true
		p.CanCollide = false
		p.CastShadow = false
		p.CFrame = CFrame.new(chest + Vector3.new(0, y, 0))
		p.Parent = model
		return p
	end

	local robe = robeOf(color)
	local root = part("HumanoidRootPart", Vector3.new(2, 2, 1), color, 0)
	root.Transparency = 1
	part("Torso", Vector3.new(2.2, 2.6, 1.2), robe, 0)
	-- a cabeça carrega a COR do elemento: leitura à distância sem depender de UI
	part("Head", Vector3.new(1.6, 1.6, 1.6), color, 2)

	local hum = Instance.new("Humanoid")
	hum.RigType = Enum.HumanoidRigType.R6
	hum.MaxHealth = hp
	hum.Health = hp
	hum.DisplayName = name
	hum.HealthDisplayType = Enum.HumanoidHealthDisplayType.AlwaysOff
	hum.Parent = model

	model.PrimaryPart = root
	model.Parent = bin()
	Combat.registerTarget(hum) -- alvo VÁLIDO: leva projétil de verdade

	return {
		model = model,
		humanoid = hum,
		root = root,
		home = chest,
		axis = Vector3.new(1, 0, 0),
		phase = 0,
		vel = Vector3.zero,
	}
end

local function killDummy(d: Dummy)
	Combat.unregisterTarget(d.humanoid)
	d.model:Destroy()
end

local function clearDummies(s: Session)
	for _, d in ipairs(s.targets) do
		killDummy(d)
	end
	table.clear(s.targets)
	if s.partner then
		killDummy(s.partner :: Dummy)
		s.partner = nil
	end
end

-- ------------------------------------------------------------------- beacon
--- Pilar de luz sobre o objetivo. É o que evita o pecado clássico do
--- onboarding: mandar "queime a passagem" sem dizer ONDE fica a passagem.
local function setBeacon(s: Session, pos: Vector3?, color: Color3?)
	if s.beacon then
		(s.beacon :: BasePart):Destroy()
		s.beacon = nil
	end
	if not pos then
		return
	end
	local p = Instance.new("Part")
	p.Name = "TutorialBeacon"
	p.Anchored = true
	p.CanCollide = false
	p.CanQuery = false
	p.CanTouch = false
	p.CastShadow = false
	p.Material = Enum.Material.Neon -- o que brilha é magia (GDD §10)
	p.Shape = Enum.PartType.Cylinder
	p.Size = Vector3.new(TUNE.beaconHeight, 3, 3)
	-- cilindro do Roblox nasce deitado no X: girar 90° o põe de pé
	p.CFrame = CFrame.new(pos + Vector3.new(0, TUNE.beaconHeight / 2, 0)) * CFrame.Angles(0, 0, math.pi / 2)
	p.Color = color or Color3.fromHex("#F0C75E")
	p.Transparency = 0.55
	p.Parent = bin()
	s.beacon = p
end

-- ------------------------------------------------------- leitura do terreno
--- O Campo de Provas NÃO é um mapa à parte: ele é achado dentro da arena de
--- verdade. Três varreduras de 3600 células (uma vez por jogador, no início):
--- centro do lago → cobertura inflamável mais perto dele → célula de água mais
--- perto dessa cobertura. Achar em vez de fixar coordenadas é o que faz o
--- tutorial sobreviver a qualquer mudança do Arena.luau.
local function scanGround(): (Vector3?, { Cell }, { Cell })
	local sx, sy, n = 0, 0, 0
	for cy = 0, Grid.ROWS - 1 do
		for cx = 0, Grid.COLS - 1 do
			local cell = Terrain.getCell(cx, cy)
			if cell and cell.material == "Water" then
				sx += cx
				sy += cy
				n += 1
			end
		end
	end
	if n == 0 then
		return nil, {}, {}
	end
	local lx, ly = sx / n, sy / n

	local best, bx, by = math.huge, -1, -1
	for cy = 0, Grid.ROWS - 1 do
		for cx = 0, Grid.COLS - 1 do
			local cell = Terrain.getCell(cx, cy)
			--- só cobertura EM PÉ: célula já queimando/carbonizada faria o passo
			--- nascer concluído (e o jogador nunca veria o que o fogo faz)
			if cell and Grid.FLAMMABLE[cell.material] and cell.state == "Normal" then
				local d = (cx - lx) ^ 2 + (cy - ly) ^ 2
				if d < best then
					best, bx, by = d, cx, cy
				end
			end
		end
	end
	if bx < 0 then
		return nil, {}, {}
	end

	--- a "passagem": a cobertura em volta da célula escolhida. Basta UMA delas
	--- pegar fogo para o passo estar entendido — o incêndio faz o resto.
	--- a âncora vem PRIMEIRO: é ela que o pilar de luz marca
	local burn: { Cell } = { { cx = bx, cy = by } }
	for dy = -3, 3 do
		for dx = -3, 3 do
			local cell = if dx ~= 0 or dy ~= 0 then Terrain.getCell(bx + dx, by + dy) else nil
			if cell and Grid.FLAMMABLE[cell.material] and cell.state == "Normal" then
				table.insert(burn, { cx = bx + dx, cy = by + dy })
			end
		end
	end

	local lake: { Cell } = {}
	local wbest, wx, wy = math.huge, -1, -1
	for cy = 0, Grid.ROWS - 1 do
		for cx = 0, Grid.COLS - 1 do
			local cell = Terrain.getCell(cx, cy)
			--- lago ainda líquido: congelado já não ensina nada
			if cell and cell.material == "Water" and cell.state ~= "Frozen" then
				local d = (cx - bx) ^ 2 + (cy - by) ^ 2
				if d < wbest then
					wbest, wx, wy = d, cx, cy
				end
			end
		end
	end
	table.insert(lake, { cx = wx, cy = wy })
	for dy = -3, 3 do
		for dx = -3, 3 do
			local cell = if dx ~= 0 or dy ~= 0 then Terrain.getCell(wx + dx, wy + dy) else nil
			if cell and cell.material == "Water" and cell.state ~= "Frozen" then
				table.insert(lake, { cx = wx + dx, cy = wy + dy })
			end
		end
	end

	--- centro do Campo: chão livre entre a cobertura e a água (é onde os alvos
	--- móveis cabem e de onde os dois objetivos ficam à vista)
	local center: Vector3? = nil
	for f = 0.5, 0.05, -0.05 do
		local cx = math.floor(bx + (wx - bx) * f + 0.5)
		local cy = math.floor(by + (wy - by) * f + 0.5)
		if Grid.inBounds(cx, cy) and not Terrain.isBlocking(cx, cy) then
			center = Grid.cellToWorld(cx, cy)
			break
		end
	end
	return center, burn, lake
end

-- ---------------------------------------------------------------- passos
local function stepName(s: Session): string
	return ORDER[s.index] or "done"
end

--- Cota do peito do jogador (o avatar é escalado — Avatar.SCALE): é dela que
--- sai a altura de tudo que precisa ser acertado a tiro.
local function chestY(player: Player): number
	local root = rootOf(player)
	return if root then root.Position.Y else Grid.FLOOR_Y + 3.5
end

--- Ponto do anel que NÃO está dentro de árvore/rocha/água: alvo dentro de
--- cobertura alta é alvo que o projétil nunca alcança (o tiro estoura na
--- árvore) — o passo viraria impossível sem dizer por quê.
local function openSpot(from: Vector3, dir: Vector3, radius: number): Vector3
	for r = radius, Grid.CELL_SIZE, -Grid.CELL_SIZE do
		local p = from + dir * r
		local cx, cy = Grid.worldToCell(p)
		if Grid.inBounds(cx, cy) and not Terrain.isBlocking(cx, cy) then
			return Grid.cellToWorld(cx, cy)
		end
	end
	return from
end

local function spawnTargets(s: Session)
	local rng = Random.new()
	local y = chestY(s.player)
	for i = 1, TUNE.targets do
		local a = (i - 1) / TUNE.targets * math.pi * 2 + rng:NextNumber()
		local dir = Vector3.new(math.cos(a), 0, math.sin(a))
		local home = openSpot(s.center, dir, TUNE.targetRing) + Vector3.new(0, y, 0)
		local element = Elements.Order[(i - 1) % #Elements.Order + 1]
		local d = buildDummy("", Elements.ById[element].color, home, TUNE.targetHp)
		-- vaivém PERPENDICULAR à linha de tiro: é o movimento que obriga a mirar
		-- à frente, e é dele que sai o indicador de antecipação
		d.axis = Vector3.new(-dir.Z, 0, dir.X)
		d.phase = rng:NextNumber() * math.pi * 2
		table.insert(s.targets, d)
	end
end

--- Elemento que o passo pede (nil = qualquer um serve).
local function wants(step: string): Elements.Id?
	if step == "burn" then
		return "Fire"
	elseif step == "freeze" then
		return "Water"
	end
	return nil
end

local function sendStep(s: Session)
	local step = stepName(s)
	local want = wants(step)
	s.needSwitch = want ~= nil and elementOf(s.player) ~= want
	send(s, {
		step = step,
		index = s.index,
		total = #ORDER,
		--- o cliente só mostra "Troque para X" quando o elemento na mão não
		--- resolve o passo: dica que mente é pior que dica que falta
		needSwitch = s.needSwitch,
		element = want,
		--- o combo que ESTA dupla faz (o cliente mostra o nome no lugar certo)
		partnerElement = if step == "combo" then s.partnerElement else nil,
	})
end

local function startStep(s: Session)
	local step = stepName(s)
	s.t = 0
	s.missAt = 0

	if step == "targets" then
		spawnTargets(s)
	elseif step == "burn" then
		local c = s.burn[1]
		setBeacon(s, if c then Grid.cellToWorld(c.cx, c.cy) else nil, Elements.ById.Fire.color)
	elseif step == "freeze" then
		local c = s.lake[1]
		setBeacon(s, if c then Grid.cellToWorld(c.cx, c.cy) else nil, Elements.ById.Water.color)
	elseif step == "combo" then
		setBeacon(s, nil)
		local root = rootOf(s.player)
		local at = if root then root.Position else s.center
		-- parceiro com elemento DIFERENTE: mesmo elemento não funde (GDD §9), e
		-- um parceiro que não pode combinar tornaria o passo impossível
		local mine = elementOf(s.player)
		for _, id in ipairs(Elements.Order) do
			if id ~= mine then
				s.partnerElement = id
				break
			end
		end
		s.partner = buildDummy(
			Strings.t("hud.partner"),
			Elements.ById[s.partnerElement].color,
			flat(at) + Vector3.new(TUNE.partnerSide, chestY(s.player), 0),
			Balance.player.hp
		)
	else
		setBeacon(s, nil)
	end

	sendStep(s)
end

--- UMA linha de telemetria por passo ALCANÇADO. Contar linhas = alcance do
--- passo; contar `completed = false` = abandono no passo. `reason`: "done",
--- "timeout", "skip", "enter" ou "left".
local function noteStep(s: Session, completed: boolean, reason: string)
	emit(Events.Name.TutorialStep, s.player, {
		step = stepName(s),
		index = s.index,
		completed = completed,
		reason = reason,
		elapsed = s.t,
	})
end

--- Fecha o passo atual e abre o próximo.
local function advance(s: Session, completed: boolean, reason: string)
	noteStep(s, completed, reason)
	if s.index >= #ORDER then
		return
	end
	s.index += 1
	clearDummies(s) -- alvos/parceiro não vazam para o passo seguinte
	startStep(s)
end

local function finish(s: Session, completed: boolean, reason: string)
	noteStep(s, completed, reason)
	clearDummies(s)
	setBeacon(s, nil)
	send(s, { step = "end" })
	sessions[s.player] = nil
	closeMarks(s.player, s.index, reason)
end

-- -------------------------------------------------------- passo 2: o lead
--- Indicador de antecipação: onde o tiro DEVERIA ter ido. Calculado com a
--- velocidade real do projétil do elemento na mão (Balance) e a velocidade real
--- do alvo — mentir aqui ensinaria a mira errada.
local function sendLead(s: Session)
	local root = rootOf(s.player)
	if not root then
		return
	end
	local from = root.Position
	local best: Dummy? = nil
	local bestD = math.huge
	for _, d in ipairs(s.targets) do
		local dist = (d.root.Position - from).Magnitude
		if dist < bestD then
			best, bestD = d, dist
		end
	end
	if not best then
		return
	end
	local target = best :: Dummy
	local speed = Balance.elements[elementOf(s.player)].basic.speed
	local flight = bestD / math.max(speed, 1)
	send(s, { step = "lead", pos = target.root.Position + target.vel * flight })
end

-- ------------------------------------------------- passo 5: a Sintonia real
--- O ping é o de verdade (o Main roteia ProposeSintonia → Sintonia.propose).
--- Aqui só se OBSERVA que ele saiu, para o parceiro responder.
---
--- ponytail: TETO CONHECIDO — a canalização do Sintonia.luau só existe entre
--- dois Players (Cast.player/Channel.a|b), então um parceiro-bot não entra
--- nela. O passo reproduz o par chamando os MESMOS sistemas que a fusão de
--- verdade chama: Net.SintoniaState (canalização + nome do combo, desenhados
--- pelo SintoniaUi que já existe) e Terrain.applyElement (a reação de mundo).
--- Nada de VFX exclusivo de tutorial. Quando a Sintonia aceitar Humanoid como
--- par, trocar este bloco por Sintonia.registerCast e apagar o resto.
local function comboOf(s: Session): Elements.Combo?
	return Elements.getCombo(elementOf(s.player), s.partnerElement)
end

local function beginCombo(s: Session)
	local combo = comboOf(s)
	if not combo or not evSintonia then
		return
	end
	s.fireAt = os.clock() + Balance.sintonia.channel
	;(evSintonia :: RemoteEvent):FireClient(s.player, {
		phase = "channeling",
		comboKey = combo.key,
		pos = s.comboPos,
		radius = combo.radius,
		duration = Balance.sintonia.channel,
		byUserId = s.player.UserId,
	})
end

local function fireCombo(s: Session)
	local combo = comboOf(s)
	s.fireAt = 0
	if not combo or not evSintonia then
		return
	end
	-- 1) MUNDO: as duas magias caem no mesmo ponto (a química é do Terrain)
	local cx, cy = Grid.worldToCell(s.comboPos)
	if Grid.inBounds(cx, cy) then
		local r = combo.radius * 0.5 / Grid.CELL_SIZE
		Terrain.applyElement(cx, cy, elementOf(s.player), r, true)
		Terrain.applyElement(cx, cy, s.partnerElement, r, true)
	end
	-- 2) TELA: o mesmo pacote que o SintoniaUi já sabe desenhar
	;(evSintonia :: RemoteEvent):FireClient(s.player, {
		phase = "fired",
		comboKey = combo.key,
		pos = s.comboPos,
		radius = combo.radius,
		duration = combo.duration,
		byUserId = s.player.UserId,
	})
	if evAnnounce then
		local isPt = string.sub(s.player.LocaleId or "", 1, 2) == "pt"
		;(evAnnounce :: RemoteEvent):FireClient(s.player, {
			key = "announce.sintonia.fired",
			args = { if isPt then combo.pt else combo.en },
		})
	end
end

--- O jogador pode trocar de elemento no meio do passo. Se ele pegar JUSTAMENTE
--- o elemento do parceiro, não existe fusão (GDD §9: mesmo elemento não funde)
--- e o passo ficaria sem saída. Então quem muda é o PARCEIRO, na hora e com a
--- cor junto — a dupla nunca fica impossibilitada de combinar.
local function retunePartner(s: Session)
	local mine = elementOf(s.player)
	if s.partnerElement ~= mine then
		return
	end
	for _, id in ipairs(Elements.Order) do
		if id ~= mine then
			s.partnerElement = id
			break
		end
	end
	local partner = s.partner
	if not partner then
		return
	end
	local color = Elements.ById[s.partnerElement].color
	for _, part in ipairs((partner :: Dummy).model:GetChildren()) do
		if part:IsA("BasePart") then
			part.Color = if part.Name == "Torso" then robeOf(color) else color
		end
	end
end

--- Ping observado (C→S). O parceiro só responde no passo do combo.
local function onPing(player: Player, payload: any)
	local s = sessions[player]
	if not s or stepName(s) ~= "combo" or s.answerAt > 0 or s.fireAt > 0 then
		return
	end
	local root = rootOf(player)
	if not root then
		return
	end
	local pos = if typeof(payload) == "table" then (payload :: any).pos else nil
	local mark = flat(root.Position)
	if typeof(pos) == "Vector3" and pos.Magnitude == pos.Magnitude and pos.Magnitude ~= math.huge then
		local d = flat(pos) - mark
		mark += if d.Magnitude > 60 then d.Unit * 60 else d
	end
	s.comboPos = mark + Vector3.new(0, 3, 0)
	retunePartner(s)
	s.answerAt = os.clock() + TUNE.partnerAnswer
end

--- Disparo observado (C→S): fecha o passo 1 (o gesto É a lição) e abre a
--- janela de "errou?" do passo 2.
local function onCast(player: Player)
	local s = sessions[player]
	if not s then
		return
	end
	s.fired = true
	local now = os.clock()
	if stepName(s) ~= "targets" or #s.targets == 0 or s.missAt > 0 or now < s.missReadyAt then
		return
	end
	s.missAt = now + TUNE.missGrace
end

--- O QUE O CLIENTE QUER (C→S). Tabela única em vez de uma cadeia de `if`: é a
--- lista fechada do que o servidor aceita, e é ela que o autoteste interroga
--- dos dois lados (o `TutorialUi.selfTest` afirma os MESMOS literais).
--- "enter"/"done" e `skip = true` são as formas antigas que o Main roteava —
--- ficam valendo porque cliente velho continua no ar durante um deploy.
--- NOTE que "offer"/"reopen" (S→C) NÃO estão aqui: um cliente que devolvesse o
--- próprio convite não pode abrir sessão nenhuma.
local INTENT: { [string]: string } = {
	accept = "in",
	decline = "no",
	skip = "out",
	enter = "out",
	done = "out",
}

--- Payload é HOSTIL até prova em contrário — daí os testes de tipo.
local function intentOf(payload: any): string?
	if typeof(payload) ~= "table" then
		return nil
	end
	local p = payload :: any
	if p.skip == true then
		return "out"
	end
	return if typeof(p.step) == "string" then INTENT[p.step] else nil
end

--- Partida em andamento? Require preguiçoso e protegido (o mesmo padrão que o
--- Match usa para perguntar do tutorial). Quem entra NO MEIO da partida já nasce
--- eliminado — não conjura, não derruba alvo, não conclui passo nenhum — e a
--- sessão presa ainda seguraria o lobby DEPOIS que a partida acabasse.
local matchMod: any = nil
local matchChecked = false
local function matchRunning(): boolean
	if not matchChecked then
		matchChecked = true
		local mod = ServerModules:FindFirstChild("Match")
		local ok, m = pcall(require, mod :: ModuleScript)
		matchMod = ok and m or nil
	end
	if not matchMod or not matchMod.isPlaying then
		return false
	end
	local ok, yes = pcall(matchMod.isPlaying)
	return ok and yes == true
end

--- RECUSAR NÃO É PORTA DE MÃO ÚNICA. Quem disse "JOGAR AGORA" fica com o botão
--- `tutorial.reopen`, e ele mora no MESMO slot do PULAR (topo-centro), que só
--- existe dentro do Campo de Provas — nenhum pixel novo na tela de quem joga.
local function decline(player: Player)
	if seen[player] or sessions[player] then
		return
	end
	offered[player] = "asked"
	--- durante a prova a tela é do jogo: a recusa é registrada, mas o botão só
	--- aparece no lobby (quem manda é o `refresh`). Cobre a recusa atrasada de
	--- um cliente lento — ou hostil — depois da largada.
	if not matchRunning() then
		sendTo(player, { step = "reopen" })
	end
end

local function wire()
	if wired then
		return
	end
	wired = true
	-- OBSERVAÇÃO PURA: estes dois remotes já são roteados pelo Main (Combat e
	-- Sintonia decidem). Ouvir de novo aqui não muda nada do jogo — só conta
	-- para o roteiro. Nenhum dado do cliente vira estado sem validação.
	Net.event(Net.Names.CastSpell).OnServerEvent:Connect(onCast)
	Net.event(Net.Names.ProposeSintonia).OnServerEvent:Connect(onPing)

	--- ROTEAMENTO DO CAMPO DE PROVAS — UM DONO SÓ (R11). Antes o Main tinha um
	--- handler paralelo para este mesmo remote, e foi justamente o desacordo
	--- entre os dois contratos (Main esperava `payload.skip`, o TutorialUi manda
	--- `{ step = "skip" }`) que travou o servidor inteiro em 18/08: ninguém saía
	--- do onboarding e o lobby do Match não começava partida nenhuma. Agora o
	--- protocolo inteiro (entrar, recusar, sair) mora AQUI, ao lado da tabela
	--- que o autoteste interroga.
	Net.event(Net.Names.Tutorial).OnServerEvent:Connect(function(player, payload)
		local intent = intentOf(payload)
		if intent == "out" then
			Tutorial.skip(player)
		elseif intent == "in" then
			Tutorial.start(player)
		elseif intent == "no" then
			decline(player)
		end
	end)
end

-- --------------------------------------------------------- checagem por passo
--- Cada passo termina por FATO DO MUNDO, nunca por "o jogador clicou em ok".
local function completed(s: Session, step: string): boolean
	if step == "aim" then
		--- o primeiro disparo JÁ é o passo: o gesto é a lição
		return s.fired
	elseif step == "targets" then
		return #s.targets == 0
	elseif step == "burn" then
		for _, c in ipairs(s.burn) do
			local cell = Terrain.getCell(c.cx, c.cy)
			if cell and (cell.state == "Burning" or cell.state == "Burned") then
				return true
			end
		end
	elseif step == "freeze" then
		for _, c in ipairs(s.lake) do
			local cell = Terrain.getCell(c.cx, c.cy)
			if cell and cell.state == "Frozen" then
				return true
			end
		end
	end
	return false
end

--- Move alvos e parceiro. Corpos ancorados: quem manda no lugar é o servidor,
--- frame a frame — nada de física de cliente decidindo onde o alvo está.
local function moveDummies(s: Session, dt: number)
	local root = rootOf(s.player)
	local look = if root then root.Position else s.center

	for i = #s.targets, 1, -1 do
		local d = s.targets[i]
		if d.humanoid.Health <= 0 then
			-- ACERTOU: nenhuma tela nova (spec). O alvo some, e isso basta.
			killDummy(d)
			table.remove(s.targets, i)
			s.missAt = 0
			continue
		end
		d.phase += dt * TUNE.targetRate
		--- `home` já é a cota do peito: o pivô do modelo É o root
		local eye = d.home + d.axis * (math.sin(d.phase) * TUNE.targetSwing)
		d.vel = d.axis * (math.cos(d.phase) * TUNE.targetSwing * TUNE.targetRate)
		local at = Vector3.new(look.X, eye.Y, look.Z)
		d.model:PivotTo(if (at - eye).Magnitude > 1 then CFrame.lookAt(eye, at) else CFrame.new(eye))
	end

	local partner = s.partner
	if partner and root then
		--- o parceiro anda até o lado do jogador (ou até o ponto pingado, depois
		--- que responde): presença, não escolta perfeita
		local goal = if s.answerAt > 0 or s.fireAt > 0 then s.comboPos else root.Position
		local here = partner.root.Position
		--- só no plano: a cota do peito foi fixada no spawn e não muda
		local to = Vector3.new(goal.X - here.X, 0, goal.Z - here.Z)
		if to.Magnitude > TUNE.partnerSide then
			local eye = here + to.Unit * math.min(TUNE.partnerSpeed * dt, to.Magnitude)
			local at = Vector3.new(root.Position.X, eye.Y, root.Position.Z)
			partner.model:PivotTo(if (at - eye).Magnitude > 1 then CFrame.lookAt(eye, at) else CFrame.new(eye))
		end
	end
end

-- ----------------------------------------------------------------------- API
--- O CONVITE. Substitui o antigo `Tutorial.start` como porta de entrada do
--- Main: pergunta, e só. A sessão só abre se o jogador ACEITAR (intent "in").
--- Devolve `false` para quem já foi perguntado, já treinou, ou está no meio de
--- uma prova — nesse último caso a pergunta fica PENDENTE e sai no lobby
--- seguinte (servidor público: desconhecido entra a qualquer hora, inclusive no
--- minuto 4 de uma partida, e não pode ficar sem nunca ver a oferta).
function Tutorial.offer(player: Player): boolean
	if not offerable(player) then
		return false
	end
	wire()
	events()
	if matchRunning() then
		offered[player] = "pending"
		return false
	end
	offered[player] = "asked"
	sendTo(player, { step = "offer" })
	return true
end

--- Começa o Campo de Provas. Devolve `false` para quem já passou por ele nesta
--- sessão (ou para quem ainda não tem corpo) — o tutorial NÃO repete.
function Tutorial.start(player: Player): boolean
	if sessions[player] or seen[player] then
		return false
	end
	--- NÃO no meio de uma partida: o Main chama isto em TODO corpo novo, e
	--- `startMatch` recarrega personagem. Sem esta guarda, quem entra no meio da
	--- prova (ou o próprio começo da partida) abria um Campo de Provas com alvos
	--- dentro da arena viva e passos impossíveis de concluir.
	if matchRunning() then
		return false
	end
	local root = rootOf(player)
	if not root then
		return false
	end
	wire()
	events()

	local center, burn, lake = scanGround()
	if not center then
		return false -- terreno sem lago/cobertura: não há Campo de Provas a montar
	end

	--- RETOMADA: quem foi cortado pela largada volta do passo onde parou (ver
	--- `closeMarks`). Quem nunca entrou começa do 1.
	local s: Session = {
		player = player,
		index = math.clamp(resume[player] or 1, 1, #ORDER),
		t = 0,
		startedAt = os.clock(),
		center = center :: Vector3,
		burn = burn,
		lake = lake,
		targets = {},
		fired = false,
		partner = nil,
		partnerElement = "Water",
		beacon = nil,
		missAt = 0,
		missReadyAt = 0,
		answerAt = 0,
		fireAt = 0,
		comboPos = center :: Vector3,
		needSwitch = false,
	}
	sessions[player] = s
	resume[player] = nil
	offered[player] = "asked" -- aceitou: a pergunta está respondida para sempre
	-- cai DIRETO no Campo de Provas (spec: 0–20s, sem menu, sem loja, sem nada)
	local char = player.Character
	if char then
		char:PivotTo(CFrame.new((center :: Vector3) + Vector3.new(0, 4, 0)))
	end
	startStep(s)
	return true
end

--- PULAR (a qualquer momento) e ENTRAR NO ROYALE são o MESMO caminho: sair do
--- Campo de Provas. No último passo é conclusão; antes dele, abandono — e a
--- diferença aparece na telemetria, que é o ponto.
function Tutorial.skip(player: Player)
	local s = sessions[player]
	if not s then
		return
	end
	local last = s.index >= #ORDER
	finish(s, last, if last then "enter" else "skip")
end

function Tutorial.active(player: Player): boolean
	return sessions[player] ~= nil
end

--- Reconciliação de TELA na virada Lobby↔Playing — uma varredura por transição,
--- não uma por quadro. Durante a prova a coluna central é do jogo (nada de
--- convite por cima de partida); no lobby seguinte o convite pendente sai e o
--- botão TREINAR de quem recusou volta sozinho.
local function refresh(playing: boolean)
	for _, player in ipairs(Players:GetPlayers()) do
		if sessions[player] then
			continue
		end
		if playing then
			sendTo(player, { step = "end" })
		elseif offered[player] == "pending" and not seen[player] then
			offered[player] = "asked"
			sendTo(player, { step = "offer" })
		elseif reopenable(player) then
			sendTo(player, { step = "reopen" })
		end
	end
end

function Tutorial.step(dt: number)
	-- A PARTIDA COMEÇOU POR CIMA DO ONBOARDING (alpha público). `Tutorial.start`
	-- já se recusa a abrir sessão durante a prova, mas isso não cobre o caminho
	-- inverso — e desde a R11 o lobby NÃO ESPERA MAIS por quem treina, então
	-- este é o caminho NORMAL, não a exceção. Uma sessão que sobrevive à largada
	-- fica com alvos ANCORADOS e VÁLIDOS (`Combat.registerTarget`) dentro da
	-- arena viva, um pilar de luz no meio do mapa e passos que apontam para
	-- células de um terreno que o `startMatch` já reconstruiu. Fechar aqui é
	-- fechar no único lugar que é dono das sessões — vale para toda porta de
	-- entrada de partida (`startMatch` e `forceStart`).
	-- O jogador cortado NÃO fica no prejuízo: o `announce.match.start` que o
	-- Match acabou de mandar explica o que houve (o Match roda ANTES deste
	-- módulo no Heartbeat), o passo alcançado fica guardado e o botão TREINAR
	-- volta no lobby seguinte — ver `closeMarks`.
	local playing = matchRunning()
	if playing and next(sessions) ~= nil then
		for _, s in pairs(sessions) do
			finish(s, s.index >= #ORDER, "match")
		end
	end
	if playing ~= wasPlaying then
		wasPlaying = playing
		refresh(playing)
	end
	if playing or next(sessions) == nil then
		return
	end
	local now = os.clock()
	for player, s in pairs(sessions) do
		if not player.Parent or not rootOf(player) then
			-- saiu (ou morreu de vez): o passo em aberto conta como abandono
			finish(s, false, "left")
			continue
		end
		-- TETO ABSOLUTO: nenhuma sessão fica aberta para sempre. Sem isto um
		-- jogador AFK no último passo (que não tem teto próprio) segurava o lobby
		-- do servidor inteiro. Quem chegou ao fim do roteiro conta como CONCLUÍDO
		-- mesmo estourando o relógio — ele viu tudo, só não apertou o botão.
		if now - s.startedAt >= TUNE.sessionCap then
			finish(s, s.index >= #ORDER, "timeout")
			continue
		end
		s.t += dt
		local step = stepName(s)
		moveDummies(s, dt)

		-- erro do passo 2: o projétil já teria chegado e nenhum alvo caiu
		if s.missAt > 0 and now >= s.missAt then
			s.missAt = 0
			s.missReadyAt = now + TUNE.missCooldown
			if step == "targets" then
				sendLead(s)
			end
		end

		-- a dica de troca de elemento acompanha a mão do jogador
		local want = wants(step)
		if want and s.needSwitch ~= (elementOf(player) ~= want) then
			sendStep(s)
		end

		if s.answerAt > 0 and now >= s.answerAt then
			s.answerAt = 0
			beginCombo(s)
		elseif s.fireAt > 0 and now >= s.fireAt then
			fireCombo(s)
			advance(s, true, "done")
			continue
		end

		if completed(s, step) then
			advance(s, true, "done")
		elseif s.answerAt == 0 and s.fireAt == 0 and s.t >= (LIMIT[step] or math.huge) then
			advance(s, false, "timeout")
		end
	end
end

--- Limpeza de saída (o coordenador chama junto dos outros onPlayerRemoving).
function Tutorial.onPlayerRemoving(player: Player)
	local s = sessions[player]
	if s then
		clearDummies(s)
		setBeacon(s, nil)
		sessions[player] = nil
	end
	-- as três marcas são chaveadas por Player: deixar qualquer uma para trás
	-- segura o Instance do jogador que já saiu (o servidor roda por horas)
	seen[player] = nil
	offered[player] = nil
	resume[player] = nil
end

--- Sanidade sem Studio (mesmo padrão de Terrain/Combat/Match.selfTest).
function Tutorial.selfTest(): boolean
	local ok = true
	local center, burn, lake = scanGround()
	if not center then
		warn("[Tutorial] selfTest: arena sem lago ou sem cobertura inflamável")
		ok = false
	end
	if #burn == 0 or #lake == 0 then
		warn(string.format("[Tutorial] selfTest: passagem=%d células · lago=%d células", #burn, #lake))
		ok = false
	end
	for _, name in ipairs(ORDER) do
		if LIMIT[name] == nil then
			warn("[Tutorial] selfTest: passo sem teto de tempo — " .. name)
			ok = false
		end
	end
	-- o passo do combo exige que exista fusão entre elementos diferentes
	if not Elements.getCombo("Fire", "Water") then
		warn("[Tutorial] selfTest: matriz de Sintonia vazia")
		ok = false
	end

	-- ------------------------------------------------------- SAÍDA DO CAMPO
	-- O BUG QUE TRAVAVA O SERVIDOR INTEIRO (18/08): o TutorialUi manda
	-- `{ step = "skip" }` nos dois botões e o roteamento do Main só entendia
	-- `{ skip = true }` — ninguém saía do onboarding, `Tutorial.active` ficava
	-- true e o lobby do Match nunca começava partida. O primeiro assert
	-- reprovaria antes da correção.
	assert(intentOf({ step = "skip" }) == "out", "o payload REAL do TutorialUi tem de tirar o jogador do Campo de Provas")
	assert(intentOf({ skip = true }) == "out", "o payload documentado no Main continua valendo")
	assert(intentOf({ step = "enter" }) == "out" and intentOf({ step = "done" }) == "out", "cliente velho continua saindo")
	assert(intentOf({ step = "combo" }) == nil, "passo qualquer não é pedido de saída")
	assert(intentOf(nil) == nil and intentOf("skip") == nil, "payload hostil não mexe no tutorial de ninguém")

	-- ============================ CONVITE, NÃO IMPOSIÇÃO (R11) ============================
	-- Chave sintética: as três marcas do convite são tabelas comuns indexadas por
	-- jogador, então dá para interrogar a máquina de estados inteira sem Studio,
	-- sem jogador e sem tocar em RemoteEvent.
	local ghost = { Name = "selfTest" } :: any
	offered[ghost], seen[ghost], resume[ghost] = nil, nil, nil

	-- (a) NINGUÉM ENTRA NO CAMPO DE PROVAS SEM TER ACEITADO
	assert(intentOf({ step = "accept" }) == "in", "TREINAR é a única porta de entrada do Campo de Provas")
	assert(intentOf({ step = "offer" }) == nil, "o convite (S→C) devolvido por um cliente NÃO é aceitação")
	assert(intentOf({ step = "reopen" }) == nil, "o botão (S→C) devolvido por um cliente NÃO é aceitação")
	assert(intentOf({ step = "decline" }) == "no", "JOGAR AGORA tem de chegar como recusa, não como entrada")
	assert(offerable(ghost), "quem nunca foi perguntado recebe o convite")
	offered[ghost] = "asked" -- é tudo que `Tutorial.offer` faz: PERGUNTAR
	assert(not Tutorial.active(ghost), "o convite não abre sessão — quem abre é o `start`, e só pelo intent 'in'")

	-- (b) QUEM RECUSOU NÃO É PERGUNTADO DE NOVO (nem a cada respawn: o
	-- `startMatch` recarrega o personagem de todo mundo, toda partida)
	assert(not offerable(ghost), "a pergunta é UMA por jogador por sessão de servidor")
	assert(reopenable(ghost), "recusar não é porta de mão única: fica o botão `tutorial.reopen`")

	-- (c) CORTADO PELA LARGADA ≠ "JÁ TREINOU". O lobby não espera mais por quem
	-- treina, então a interrupção é o caminho NORMAL — se ela consumisse o
	-- direito ao Campo de Provas, o Diretor teria trocado um tutorial
	-- obrigatório por um tutorial impossível.
	closeMarks(ghost, 3, "match")
	assert(resume[ghost] == 3, "o passo alcançado tem de sobreviver à largada (senão recomeça do 1 para sempre)")
	assert(not seen[ghost] and reopenable(ghost), "quem foi cortado pela largada continua podendo treinar")
	closeMarks(ghost, 3, "skip")
	assert(seen[ghost] and resume[ghost] == nil, "quem saiu por vontade própria não é perguntado nem retomado")
	assert(not reopenable(ghost) and not offerable(ghost), "quem já passou pelo Campo de Provas não vê mais nada")
	offered[ghost], seen[ghost], resume[ghost] = nil, nil, nil -- não deixa lixo

	-- ---------------------------------------------------- TETO ABSOLUTO (AFK)
	-- O último passo não tem teto próprio DE PROPÓSITO (quem chegou lá decide
	-- quando entrar no Royale) — então a sessão só fecha sozinha por este número.
	-- Sem ele, um jogador parado segura o lobby de todo mundo para sempre.
	local scripted = 0
	for _, name in ipairs(ORDER) do
		local lim = LIMIT[name]
		if lim and lim < math.huge then
			scripted += lim
		end
	end
	assert(
		TUNE.sessionCap ~= nil and TUNE.sessionCap < math.huge,
		"sem teto absoluto de sessão um jogador AFK trava o lobby do servidor para sempre"
	)
	assert(
		TUNE.sessionCap > scripted,
		string.format(
			"teto absoluto (%ds) menor que o roteiro (%ds): cortaria quem ainda está jogando",
			TUNE.sessionCap,
			scripted
		)
	)

	-- ------------------------------------- SESSÃO NÃO ATRAVESSA A LARGADA
	-- `Tutorial.step` fecha toda sessão aberta quando a prova começa: alvo
	-- ancorado e VÁLIDO dentro da arena viva, pilar de luz no meio do mapa e
	-- passos apontando para células de um terreno que o `startMatch` já
	-- reconstruiu. O que dá para conferir sem Studio é justamente o que
	-- quebraria o laço mestre: esta consulta roda a cada quadro com sessão
	-- aberta e tem de devolver booleano SEMPRE — com ou sem Match na árvore.
	assert(typeof(matchRunning()) == "boolean", "a consulta de partida em andamento tem de devolver booleano sempre")
	assert(not Tutorial.active(nil :: any), "sem sessão ninguém está no Campo de Provas (nem chave inválida)")
	print(string.format("[Tutorial] selfTest %s — %d passos", if ok then "OK" else "FALHOU", #ORDER))
	return ok
end

Players.PlayerRemoving:Connect(function(player)
	Tutorial.onPlayerRemoving(player)
end)

return Tutorial
