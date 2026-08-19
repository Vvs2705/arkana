--!strict
-- ============================================================================
-- ARKANA (Roblox) — RAIA C · CONTROLES, CÂMERA E MIRA
--
-- REQUISITO Nº 1, do teste do Diretor em aparelho real (18/08):
-- MIRAR E ATIRAR SÃO UM ÚNICO GESTO (padrão Brawl Stars / Free Fire).
--   1. o dedo desce no BOTÃO DA MAGIA e arrasta;
--   2. passando de Balance.controls.aimDeadzonePx aparece a SETA no mundo;
--   3. ao SOLTAR, dispara naquela direção;
--   · soltar em < Balance.controls.tapMaxDuration sem ter arrastado = tiro
--     rápido na direção da câmera;
--   · arrastar e voltar para dentro da deadzone = CANCELA (nada é enviado).
-- Dois gestos separados (mirar, depois atirar) foi REPROVADO — não repetir.
--
-- REQUISITO Nº 2: troca de elemento responde ao toque — ver ControlsTouch.
--
-- REQUISITO Nº 3, do segundo teste do Diretor: SEM COMANDO BÁSICO NÃO TEM
-- JOGO. Pular, correr e agachar existem e são os do gênero:
--   Pular  = ESPAÇO (nativo do Roblox — nunca sinkado aqui) / botão PULAR
--   Correr = SHIFT ESQUERDO segurado / botão CORRER (alterna)
--   Agachar= C (alterna) / botão AGACHAR
--   Esquiva= Q (era do elemento; mobilidade merece a tecla nobre) / ESQ
--   Elemento = 1–5 e RODA DO MOUSE / carrossel
-- O bug era literal: BindAction do Espaço devolvia Sink e ENGOLIA o pulo,
-- então quem caía no lago (rebaixo de 2,5 studs) não subia mais.
--
-- DOIS ESQUEMAS, NÃO UM ADAPTADO (GDD §19.3, redação de 19/08 do Diretor):
-- "a mecânica no computador é DIFERENTE da mecânica no celular". O que se
-- compartilha é a REGRA (dano, mana, cooldown, alcance — tudo do servidor);
-- o GESTO nunca. Na prática, dentro deste arquivo:
--   TOQUE  mira e disparo no MESMO gesto (arrastar do botão, soltar dispara,
--          voltar ao centro cancela). Nunca lê o cursor.
--   PC     mira CONTÍNUA pelo cursor + botão próprio de disparo (M1 básico,
--          M2 tática). Nunca usa a direção da câmera como substituto de mira,
--          nunca desenha botão de tela.
-- Quem responde "qual esquema esta sessão joga" é `Device.schemes()`, e só ele.
--
-- MOVIMENTO E CÂMERA SÃO NATIVOS. O thumbstick de toque e o WASD vêm prontos
-- no PlayerModule do Roblox e a câmera de 3ª pessoa também: "aproveitar os
-- sistemas da plataforma" (docs/ROBLOX.md §2). Aqui só ajustamos o zoom para
-- leitura de combate. Reinventar joystick é proibido.
--
-- AUTORIDADE: este arquivo só ENVIA INTENÇÃO (Net.CastSpell/SwitchElement/
-- Dodge). Nenhum dano, mana ou alcance é decidido aqui — é tudo do servidor.
-- O cooldown desenhado no botão é PREVISÃO local; StatsUpdate corrige.
-- ============================================================================

local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local UserInputService = game:GetService("UserInputService")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Balance = require(Shared:WaitForChild("Balance"))
local Elements = require(Shared:WaitForChild("Elements"))
local Net = require(Shared:WaitForChild("Net"))
--- schema de telemetria: a lista fechada de modos de entrada (drag/tap/key).
--- O autoteste confere os rótulos daqui contra ele — rótulo que o servidor
--- descarta é rótulo que vira buraco calado no relatório.
local Events = require(Shared:WaitForChild("Events"))
local ControlsTouch = require(script.Parent:WaitForChild("ControlsTouch"))
--- `Device.schemes()` é a ÚNICA resposta para "toque ou PC" no cliente. Ver o
--- bloco ESQUEMA DE CONTROLE lá: a pergunta já existiu em três redações
--- diferentes e a divergência entregava os dois esquemas ao mesmo aparelho.
local Device = require(script.Parent:WaitForChild("Device"))

local Controls = {}

--- Um gesto vivo: nasce no InputBegan de um botão (ou tecla) e morre no
--- InputEnded do MESMO InputObject — por isso o rastreio é global, não do
--- botão: o dedo sai de cima dele já no primeiro pixel de arrasto.
type Gesture = {
	kind: string,
	input: InputObject,
	origin: Vector2,
	startedAt: number,
	dragPx: number,
	dir: Vector3?,
}

--- espelho de ControlsTouch.Handle (o require por WaitForChild não resolve
--- tipo entre módulos — declarar aqui evita ruído na análise estática)
type TouchHandle = {
	gui: ScreenGui,
	setElement: (string) -> (),
	setPressed: (string, boolean) -> (),
	setCooldown: (string, number, number) -> (),
	setToggle: (string, boolean) -> (),
}

local camera = workspace.CurrentCamera
local player: Player? = nil
local touchUi: TouchHandle? = nil

local element: string = Elements.Order[1]
--- esquema "Simples" do GDD §19.3 (assistido) é o padrão; o "Avançado" desliga
--- a assistência. Sem tela de opções ainda — a raia de UI chama setAimAssist.
local aimAssist = true

local active: { [any]: Gesture } = {}
local cdUntil: { [string]: number } = { tactic = 0, dodge = 0 }
local cdTotal: { [string]: number } = { tactic = 1, dodge = 1 }
local lastBasic = 0

local remCast: RemoteEvent? = nil
local remSwitch: RemoteEvent? = nil
local remDodge: RemoteEvent? = nil
local remStance: RemoteEvent? = nil

--- postura: INTENÇÃO local, efeito no servidor (ver bloco de comandos básicos)
local sprint = false
local crouch = false

local NUM_KEYS = {
	Enum.KeyCode.One,
	Enum.KeyCode.Two,
	Enum.KeyCode.Three,
	Enum.KeyCode.Four,
	Enum.KeyCode.Five,
}

-- ---------------------------------------------------------------- utilidades

local function flat(v: Vector3): Vector3
	local f = Vector3.new(v.X, 0, v.Z)
	return if f.Magnitude > 1e-3 then f.Unit else Vector3.new(0, 0, -1)
end

--- Direção "para onde eu olho" — em 3ª pessoa quem manda é a câmera.
local function facing(): Vector3
	return flat(camera.CFrame.LookVector)
end

local function rootPosition(): Vector3?
	local char = player and (player :: Player).Character
	local root = char and char:FindFirstChild("HumanoidRootPart")
	return if root then (root :: BasePart).Position else nil
end

local function humanoid(): Humanoid?
	local char = player and (player :: Player).Character
	return if char then char:FindFirstChildOfClass("Humanoid") else nil
end

--- Arrasto de tela → direção no mundo, relativo à câmera. Arrastar PARA CIMA
--- é atirar para longe (convenção do gênero: arrasta-se na direção do alvo,
--- não ao contrário). Só a DIREÇÃO importa; o tamanho do arrasto é ignorado.
local function screenToWorld(delta: Vector2): Vector3
	local cf = camera.CFrame
	local v = flat(cf.RightVector) * delta.X + flat(cf.LookVector) * -delta.Y
	return if v.Magnitude > 1e-3 then v.Unit else facing()
end

--- Alvos plausíveis para a assistência. ponytail: bots ficam em workspace.Bots
--- quando a raia B/R3 criar a pasta; sem ela, só jogadores entram na conta.
local function eachTarget(fn: (Vector3) -> ())
	local function consider(model: Instance)
		local root = model:FindFirstChild("HumanoidRootPart")
		local hum = model:FindFirstChildOfClass("Humanoid")
		if root and hum and (hum :: Humanoid).Health > 0 then
			fn((root :: BasePart).Position)
		end
	end
	for _, other in ipairs(Players:GetPlayers()) do
		if other ~= player and other.Character then
			consider(other.Character :: Model)
		end
	end
	local bots = workspace:FindFirstChild("Bots")
	if bots then
		for _, bot in ipairs(bots:GetChildren()) do
			consider(bot)
		end
	end
end

--- Esquema Simples (GDD §19.3): gruda no inimigo mais alinhado dentro do cone
--- de Balance.controls.aimAssist. É só um AJUSTE DE DIREÇÃO — quem decide se
--- acertou continua sendo o servidor.
local function assisted(from: Vector3, dir: Vector3): Vector3
	if not aimAssist then
		return dir
	end
	local cfg = Balance.controls.aimAssist
	local best: Vector3? = nil
	local bestDot = math.cos(math.rad(cfg.coneDeg))
	eachTarget(function(pos)
		local to = pos - from
		if to.Magnitude > cfg.range then
			return
		end
		local d = flat(to)
		local dot = d:Dot(dir)
		if dot > bestDot then
			bestDot = dot
			best = d
		end
	end)
	return best or dir
end

-- ------------------------------------------------------- cooldowns (previsão)

local function ready(kind: string): boolean
	return os.clock() >= cdUntil[kind]
end

local function startCooldown(kind: string, seconds: number)
	cdTotal[kind] = math.max(seconds, 0.001)
	cdUntil[kind] = os.clock() + seconds
end

-- ------------------------------------------------------------------ intenção

--- `input` é TELEMETRIA (Events.INPUT_KINDS): conta o que a pessoa FEZ para
--- disparar. Não muda nada do tiro — o servidor ignora para efeito de jogo.
local function doCast(kind: string, dir: Vector3, input: string)
	local from = rootPosition()
	if not from or not remCast then
		return
	end
	if kind == "tactic" and not ready("tactic") then
		return
	end
	--- `aim` CARIMBA O ESQUEMA (GDD §19.3: Simples assistido × Avancado manual).
	--- A assistência é aplicada UMA LINHA ACIMA, no cliente, e o servidor recebe a
	--- direção já corrigida — ou seja, sem este campo ele NUNCA soube qual dos
	--- dois estava valendo. Num playtest com metade assistida, o p50 de TTK
	--- humano mistura duas populações e a pergunta V2 ("o TTK está certo com
	--- humanos mirando?") sai sem sentido. Telemetria pura, como o `input`:
	--- mentir aqui não dá vantagem nenhuma, porque a assistência já aconteceu.
	local aim = if aimAssist then "assist" else "manual"
	;(remCast :: RemoteEvent):FireServer({ kind = kind, dir = assisted(from, dir), input = input, aim = aim })
	if kind == "tactic" then
		startCooldown("tactic", (Balance.elements :: any)[element].tactic.cooldown)
	else
		lastBasic = os.clock()
	end
end

local function doDodge(dir: Vector3?)
	if not ready("dodge") or not remDodge then
		return
	end
	local chosen = dir
	if not chosen then
		local hum = humanoid()
		chosen = if hum and hum.MoveDirection.Magnitude > 0.1 then flat(hum.MoveDirection) else facing()
	end
	(remDodge :: RemoteEvent):FireServer({ dir = chosen })
	startCooldown("dodge", Balance.player.dodge.cooldown)
end

local function setElement(id: string)
	if not Elements.isValid(id) or id == element then
		return
	end
	element = id
	if touchUi then
		(touchUi :: TouchHandle).setElement(id)
	end
	if remSwitch then
		(remSwitch :: RemoteEvent):FireServer({ id = id })
	end
end

local function cycleElement(step: number)
	local idx = table.find(Elements.Order, element) or 1
	local count = #Elements.Order
	setElement(Elements.Order[(idx - 1 + step) % count + 1])
end

-- --------------------------------------------- comandos básicos (pular/correr/agachar)
-- DIVISÃO DE TRABALHO, e ela não é negociável:
--   · PULAR é da PLATAFORMA. O Espaço pertence ao Roblox e nenhum BindAction
--     daqui pode sinká-lo. No toque, o botão só encosta em Humanoid.Jump —
--     a física, o duplo-pulo-nunca e o estado de queda continuam do engine.
--   · CORRER e AGACHAR são INTENÇÃO, não efeito: quem escreve Humanoid.WalkSpeed
--     é o servidor (Combat.step, TODO frame, a partir do terreno). Mexer na
--     velocidade aqui seria apagado no frame seguinte — por isso vai no remote.

local function sendStance()
	if remStance then
		(remStance :: RemoteEvent):FireServer({ sprint = sprint, crouch = crouch })
	end
	if touchUi then
		local ui = touchUi :: TouchHandle
		ui.setToggle("sprint", sprint)
		ui.setToggle("crouch", crouch)
	end
end

--- correr: SEGURAR no PC, ALTERNAR no toque (o dedo não pode ficar preso num
--- botão enquanto mira com o outro)
local function setSprint(on: boolean)
	if sprint ~= on then
		sprint = on
		sendStance()
	end
end

local function toggleCrouch()
	crouch = not crouch
	sendStance()
end

--- Pulo do botão de toque. No PC não existe função nenhuma: é o Espaço nativo.
local function doJump()
	local hum = humanoid()
	if hum then
		hum.Jump = true
	end
end

--- Botões de toque de ação simples (sem gesto de mira): um toque, uma ação.
local function onSimpleTouch(kind: string)
	if kind == "jump" then
		doJump()
	elseif kind == "sprint" then
		setSprint(not sprint)
	elseif kind == "crouch" then
		toggleCrouch()
	end
end

-- ------------------------------------------------------- máquina do gesto

--- O QUE ACONTECE SE O DEDO SOLTAR AGORA. Uma conta só, usada por dois:
--- `release` (dispara ou não) e o anel do botão (acende ou não). Eram duas
--- verdades antes — a decisão morava no `release` e o anel só dizia "tem dedo
--- em cima", então o CANCELAR era invisível para o polegar: a pessoa que
--- arrastou, voltou ao centro e soltou não tinha como saber se errou a mira ou
--- errou o tiro. É a distinção que o Diretor cobrou por escrito.
--- Pura, para o autoteste.
function Controls.willFire(dir: Vector3?, held: number): boolean
	return dir ~= nil or held < Balance.controls.tapMaxDuration
end

--- Anel do botão = "solta e sai magia". Apagado = "solta e não sai nada".
local function showArmed(g: Gesture)
	if touchUi then
		(touchUi :: TouchHandle).setPressed(g.kind, Controls.willFire(g.dir, os.clock() - g.startedAt))
	end
end

local function updateDrag(g: Gesture, input: InputObject)
	local delta = Vector2.new(input.Position.X, input.Position.Y) - g.origin
	g.dragPx = delta.Magnitude
	-- de volta ao centro = mira desarmada (soltar aí em diante CANCELA)
	g.dir = if g.dragPx >= Balance.controls.aimDeadzonePx then screenToWorld(delta) else nil
	showArmed(g)
end

--- MODO DE ENTRADA do disparo, decidido no GESTO e não no aparelho: telefone
--- com teclado bluetooth existe, e o dado tem de dizer o que a pessoa FEZ.
--- O InputObject vem do botão que iniciou o gesto (ControlsTouch entrega o
--- objeto cru), então a origem é o próprio dispositivo do toque:
---   dedo + arrastou = drag (o gesto único do docs/ROBLOX.md §4)
---   dedo + toque curto = tap (tiro rápido na direção da câmera)
---   mouse/teclado = key — inclusive clicando no botão de tela num PC com
---   touchscreen: quem clicou não fez o gesto de celular.
function Controls.inputKind(input: any, dragged: boolean): string
	if input and input.UserInputType == Enum.UserInputType.Touch then
		return if dragged then "drag" else "tap"
	end
	return "key"
end

--- SAÍDA ÚNICA do gesto: tira do rastreio E apaga o anel. Eram dois caminhos
--- (o InputEnded normal e a varredura de toque cancelado pelo sistema) e só um
--- apagava o anel — app em segundo plano no meio do arrasto deixava o botão
--- aceso para sempre. Invariante por ESTRUTURA: quem encerra passa por aqui.
local function endGesture(g: Gesture)
	active[g.input] = nil
	if touchUi then
		(touchUi :: TouchHandle).setPressed(g.kind, false)
	end
end

local function release(g: Gesture)
	local held = os.clock() - g.startedAt
	endGesture(g)
	local dir = g.dir
	if not Controls.willFire(dir, held) then
		return -- segurou e voltou ao centro: CANCELA
	end
	if g.kind == "dodge" then
		-- toque curto na esquiva: rola para onde já se está andando (nil), que é
		-- mais útil do que a direção da câmera
		doDodge(dir)
	else
		-- toque curto: tiro rápido, direção da câmera
		doCast(g.kind, dir or facing(), Controls.inputKind(g.input, dir ~= nil))
	end
end

--- Chamado pelo botão de toque no instante em que o dedo desce.
local function beginGesture(kind: string, input: InputObject)
	local g: Gesture = {
		kind = kind,
		input = input,
		origin = Vector2.new(input.Position.X, input.Position.Y),
		startedAt = os.clock(),
		dragPx = 0,
		dir = nil,
	}
	active[input] = g
	showArmed(g) -- dedo acabou de descer: soltar agora é tiro rápido, anel aceso
end

--- Gesto que está mirando agora (o que desenha a seta no mundo). Também varre
--- toque cancelado pelo sistema (app em segundo plano), que não gera
--- InputEnded — senão a seta ficaria presa na tela.
--- E REPINTA O ANEL: `willFire` muda com o RELÓGIO (o tiro rápido expira em
--- tapMaxDuration), então um dedo parado dentro da deadzone passa de "vai
--- disparar" para "vai cancelar" sem gerar evento nenhum de entrada. Sem esta
--- passada o anel mentiria justamente para quem hesitou.
local function aimingGesture(): Gesture?
	local found: Gesture? = nil
	for _, g in pairs(active) do
		if g.input.UserInputState == Enum.UserInputState.End then
			endGesture(g)
		else
			showArmed(g)
			if g.dir and not found then
				found = g
			end
		end
	end
	return found
end

-- ---------------------------------------------------------- seta de mira 3D
-- Blocos, estética Pixel Gun (docs/ROBLOX.md §2): 10 blocos de trilha + duas
-- barras a 45° formando a ponta. Tudo local (não replica) e CanQuery=false
-- para não sujar raycast de ninguém.

local SEGMENTS = 10
local trail: { BasePart } = {}
local head: { BasePart } = {}

local function newMarker(name: string, size: Vector3, parent: Instance): BasePart
	local p = Instance.new("Part")
	p.Name = name
	p.Size = size
	p.Anchored = true
	p.CanCollide = false
	p.CanQuery = false
	p.CanTouch = false
	p.CastShadow = false
	p.Material = Enum.Material.Neon
	p.Transparency = 1
	p.Parent = parent
	return p
end

local function buildAimMarker()
	local folder = Instance.new("Folder")
	folder.Name = "ArkanaAim"
	for i = 1, SEGMENTS do
		trail[i] = newMarker("Trilha" .. i, Vector3.new(0.8, 0.28, 1.8), folder)
	end
	head[1] = newMarker("Ponta1", Vector3.new(0.8, 0.28, 3.4), folder)
	head[2] = newMarker("Ponta2", Vector3.new(0.8, 0.28, 3.4), folder)
	folder.Parent = workspace
end

--- A seta fica ESCONDIDA quase o tempo todo, e `hideAimMarker` roda todo frame.
--- Sem esta trava eram 12 escritas de propriedade por frame (720/s) para repetir
--- o valor que já estava lá — desperdício exatamente no aparelho mais fraco.
local aimShown = false

local function hideAimMarker()
	if not aimShown then
		return
	end
	aimShown = false
	for _, p in ipairs(trail) do
		p.Transparency = 1
	end
	for _, p in ipairs(head) do
		p.Transparency = 1
	end
end

local function drawAimMarker(from: Vector3, dir: Vector3)
	aimShown = true
	local e = (Elements.ById :: any)[element]
	--- alcance visual = alcance da assistência (Balance). CALIBRAÇÃO: é o único
	--- número de "alcance" que existe nos dados; se as magias ganharem alcance
	--- próprio, trocar por ele.
	local range = Balance.controls.aimAssist.range
	--- HumanoidRootPart fica ~2.6 studs acima dos pés (R6/R15). CALIBRAÇÃO.
	local base = from - Vector3.new(0, 2.6, 0)
	local step = range / (SEGMENTS + 1)

	for i, p in ipairs(trail) do
		local at = base + dir * (step * i + 2)
		p.CFrame = CFrame.lookAt(at, at + dir)
		p.Color = e.color
		p.Transparency = 0.35
	end

	local tip = base + dir * (range + 2)
	for i, p in ipairs(head) do
		local side = if i == 1 then 1 else -1
		p.CFrame = CFrame.lookAt(tip, tip + dir) * CFrame.Angles(0, math.rad(side * 45), 0) * CFrame.new(0, 0, 1.7)
		p.Color = e.glow
		p.Transparency = 0.1
	end
end

-- ------------------------------------------------- PC (teclado E MOUSE: um esquema só)

--- Raio da tela → plano do chão na altura do personagem → direção plana. Usar o
--- PLANO (e não `Mouse.Hit`) é o que impede a mira de grudar em parede/árvore.
--- `fallback` responde os três casos sem interseção útil: raio paralelo ao
--- chão, interseção ATRÁS da câmera, e o cursor EM CIMA do próprio personagem.
--- Este último era o furo: caía no `flat`, cujo padrão é a constante de MUNDO
--- (0,0,-1) — escrita para o caminho da câmera, onde ela nunca aparece. No
--- cursor ela aparecia como um tiro para o norte do mapa, ignorando para onde a
--- pessoa olhava. Pura, para o autoteste.
function Controls.groundAim(from: Vector3, origin: Vector3, direction: Vector3, fallback: Vector3): Vector3
	if math.abs(direction.Y) > 1e-4 then
		local t = (from.Y - origin.Y) / direction.Y
		if t > 0 then
			local to = (origin + direction * t) - from
			local f = Vector3.new(to.X, 0, to.Z)
			if f.Magnitude > 1e-3 then
				return f.Unit
			end
		end
	end
	return fallback
end

local function pcAimDirection(from: Vector3): Vector3
	local loc = UserInputService:GetMouseLocation()
	local ray = camera:ScreenPointToRay(loc.X, loc.Y)
	return Controls.groundAim(from, ray.Origin, ray.Direction, facing())
end

--- M1 SEGURADO arma o tiro contínuo do PC — mas só se o clique não foi
--- consumido pela interface. O disparo do PC era o único lido por SONDAGEM
--- (`IsMouseButtonPressed` todo frame), e sondagem não sabe o que a UI comeu:
--- clicar em "Grimório", "Opções" ou no botão do tutorial soltava uma magia
--- junto, com mana e cooldown de verdade — e ainda entrava no relatório como
--- um `key` que ninguém quis. A tática (M2) já era imune porque nasce no
--- InputBegan, que recebe `gameProcessedEvent`; o básico só faltava herdar a
--- mesma porta. Soltar SEMPRE desarma, inclusive soltando em cima da UI.
--- Pura, para o autoteste.
function Controls.armPcFire(down: boolean, processed: boolean): boolean
	return down and not processed
end

local m1Armed = false

--- "O PC está atirando agora?" O engate exige as DUAS coisas: o clique começou
--- no mundo (`m1Armed`) E o botão continua fisicamente apertado. A segunda
--- metade é de graça e cobre o alt-tab segurando o botão — sem ela o disparo
--- contínuo seguiria com a janela em segundo plano.
local function pcFiring(): boolean
	return m1Armed and UserInputService:IsMouseButtonPressed(Enum.UserInputType.MouseButton1)
end

local function bindPc()
	-- NENHUM BindAction aqui. O Espaço é do pulo nativo e sinká-lo foi
	-- exatamente o que reprovou a build anterior.
	UserInputService.InputBegan:Connect(function(input, processed)
		--- ANTES do filtro de `processed`: o M1 precisa VER o clique comido pela
		--- UI para poder recusá-lo (é a única forma de desarmar o disparo
		--- contínuo em cima de um painel).
		if input.UserInputType == Enum.UserInputType.MouseButton1 then
			m1Armed = Controls.armPcFire(true, processed)
			return
		end
		if processed then
			return
		end
		local key = input.KeyCode
		if key == Enum.KeyCode.Q then
			doDodge(nil) -- esquiva: é a habilidade de mobilidade, leva a tecla nobre
			return
		elseif key == Enum.KeyCode.C then
			toggleCrouch()
			return
		elseif key == Enum.KeyCode.LeftShift then
			setSprint(true)
			return
		end
		local slot = table.find(NUM_KEYS, key)
		if slot then
			setElement(Elements.Order[slot])
			return
		end
		if input.UserInputType == Enum.UserInputType.MouseButton2 then
			local from = rootPosition()
			if from then
				doCast("tactic", pcAimDirection(from), "key") -- M2 é PC, sempre
			end
		end
	end)

	UserInputService.InputEnded:Connect(function(input)
		if input.UserInputType == Enum.UserInputType.MouseButton1 then
			m1Armed = Controls.armPcFire(false, false) -- soltar desarma sempre
		end
		if input.KeyCode == Enum.KeyCode.LeftShift then
			setSprint(false) -- correr é SEGURAR, como em todo BR de PC
		end
	end)

	-- roda do mouse cicla o elemento (papel que era do Q).
	-- ponytail: a roda também dá zoom na câmera nativa e isso fica assim — o
	-- CameraRig prende o zoom entre 8 e 16 studs, então o efeito colateral tem 8
	-- studs de tamanho e não vale um ContextActionService só para ele. Se o
	-- playtest reclamar, é aqui que entra o Sink da roda.
	-- `processed` é ignorado de propósito: a câmera nativa marca a roda como
	-- processada e o ciclo morreria calado.
	UserInputService.InputChanged:Connect(function(input)
		if input.UserInputType ~= Enum.UserInputType.MouseWheel then
			return
		end
		if UserInputService:GetFocusedTextBox() then
			return
		end
		cycleElement(if input.Position.Z > 0 then 1 else -1)
	end)
end

-- ------------------------------------------------------------------ fachada

function Controls.init(p: Player)
	player = p

	-- câmera de 3ª pessoa NATIVA. A FAIXA DE ZOOM é do CameraRig e só dele: as
	-- duas linhas que moravam aqui (14/26) eram escritas mortas — o CameraRig
	-- inicializa depois e as substitui por 8/16 — e um segundo lugar guardando o
	-- mesmo knob de calibração é como o comentário da roda do mouse ficou
	-- citando uma faixa que não existe mais.
	p.CameraMode = Enum.CameraMode.Classic

	remCast = Net.event(Net.Names.CastSpell)
	remSwitch = Net.event(Net.Names.SwitchElement)
	remDodge = Net.event(Net.Names.Dodge)
	--- o coordenador já oficializou o nome em Shared/Net — a string solta saiu
	remStance = Net.event(Net.Names.Stance)

	buildAimMarker()

	--- OS DOIS ESQUEMAS, cada um pelo seu critério (GDD §19.3). Podem coexistir
	--- (celular com teclado bluetooth); o que não pode é o de PC receber botão de
	--- tela — era `TouchEnabled` sozinho aqui, e notebook com tela sensível
	--- ganhava o celular inteiro por cima do mouse.
	local touchScheme, pcScheme = Device.schemes()
	if touchScheme then
		touchUi = ControlsTouch.build(p, beginGesture, setElement, onSimpleTouch)
	end
	if pcScheme then
		bindPc()
	end

	-- o arrasto sai do botão no primeiro pixel: seguir o InputObject global é a
	-- ÚNICA forma de acompanhar o dedo até soltar
	UserInputService.InputChanged:Connect(function(input)
		local g = active[input]
		if g then
			updateDrag(g, input)
		end
	end)
	UserInputService.InputEnded:Connect(function(input)
		local g = active[input]
		if g then
			release(g) -- `endGesture` lá dentro é quem tira do rastreio
		end
	end)

	-- cooldown VERDADEIRO vem do servidor quando a raia B publicar; até lá a
	-- previsão local segura o botão. Payload tratado como hostil.
	Net.event(Net.Names.StatsUpdate).OnClientEvent:Connect(function(payload)
		if typeof(payload) ~= "table" then
			return
		end
		local cds = (payload :: any).cooldowns
		if typeof(cds) ~= "table" then
			return
		end
		for kind in pairs(cdUntil) do
			local left = (cds :: any)[kind]
			if typeof(left) == "number" and left >= 0 then
				cdUntil[kind] = os.clock() + left
				--- O servidor JÁ manda o total junto (`tacticTotal`/`dodgeTotal` em
				--- Combat.sendStats) e este código o ignorava: o anel do botão enchia
				--- sobre um total ADIVINHADO (o maior valor já visto), que nunca
				--- encolhe e por isso nunca mais bate. Fica pior com o escudo nível 4,
				--- que MULTIPLICA o cooldown da tática. Palpite só se o campo faltar.
				local total = (cds :: any)[kind .. "Total"]
				cdTotal[kind] = if typeof(total) == "number" and total > 0
					then total
					else math.max(cdTotal[kind], left)
			end
		end
	end)

	-- corpo novo nasce DE PÉ (o servidor zera a postura no respawn). Reenvia a
	-- intenção para os dois lados não discordarem se o Shift ficou segurado.
	p.CharacterAdded:Connect(function()
		crouch = false
		task.defer(sendStance)
	end)

	-- servidor e cliente começam no mesmo elemento e na mesma postura
	if remSwitch then
		(remSwitch :: RemoteEvent):FireServer({ id = element })
	end
	sendStance()
end

function Controls.update(_dt: number)
	local from = rootPosition()

	-- PC: M1 segurado atira continuamente na direção do CURSOR (o fireRate real
	-- é do servidor; aqui só evitamos inundar o remote)
	local firing = from ~= nil and pcFiring() and not UserInputService:GetFocusedTextBox()
	if firing and from then
		if os.clock() - lastBasic >= (Balance.elements :: any)[element].basic.fireRate then
			doCast("basic", pcAimDirection(from), "key") -- M1 segurado: PC
		end
	end

	-- seta de mira: gesto de toque em curso, ou o cursor do PC enquanto atira
	local g = aimingGesture()
	if from and g and g.dir then
		drawAimMarker(from, assisted(from, g.dir :: Vector3))
	elseif from and firing then
		drawAimMarker(from, assisted(from, pcAimDirection(from)))
	else
		hideAimMarker()
	end

	if touchUi then
		local ui = touchUi :: TouchHandle
		local now = os.clock()
		for kind, deadline in pairs(cdUntil) do
			ui.setCooldown(kind, math.max(0, deadline - now), cdTotal[kind])
		end
	end
end

--- Elemento ativo (o HUD lê para destacar o botão correspondente).
function Controls.getElement(): string
	return element
end

--- Esquema de mira (GDD §19.3): true = Simples (assistido) · false = Avançado.
function Controls.setAimAssist(on: boolean)
	aimAssist = on
end

-- ============================================================================
-- AUTOTESTE — cole no Command Bar do Studio (cliente):
--   require(game.Players.LocalPlayer.PlayerScripts.Client.Controls).selfTest()
-- Cobre o que SEPARA OS DOIS ESQUEMAS e vira número: o carimbo do modo de
-- entrada, a decisão disparar-ou-cancelar do gesto de toque, a projeção do
-- cursor do PC e a porta que impede o clique de UI de virar magia. Não cobre
-- pintura nem o dedo em si — cobre as contas.
-- ============================================================================
function Controls.selfTest()
	local finger = { UserInputType = Enum.UserInputType.Touch }
	local mouse = { UserInputType = Enum.UserInputType.MouseButton1 }

	assert(Controls.inputKind(finger, true) == "drag", "dedo que arrastou é o gesto único: drag")
	assert(Controls.inputKind(finger, false) == "tap", "dedo em toque curto é tap")
	assert(Controls.inputKind(mouse, true) == "key", "clique de mouse no botão de tela é PC: key")
	assert(Controls.inputKind(mouse, false) == "key", "clique de mouse é PC mesmo sem arrasto")
	assert(Controls.inputKind(nil, false) == "key", "sem InputObject, o caminho é o do PC")

	--- O rótulo tem de SOBREVIVER ao servidor: `Events.sanitizeInput` derruba o
	--- que não está na lista, e rótulo derrubado vira buraco calado no relatório.
	for _, case in ipairs({ { finger, true }, { finger, false }, { mouse, true } }) do
		local kind = Controls.inputKind(case[1], case[2] :: any)
		assert(Events.sanitizeInput(kind) == kind, "rótulo fora de Events.INPUT_KINDS: " .. kind)
	end

	-- ------------------------------------- TOQUE: disparar ou CANCELAR (§19.3)
	--- A conta que o `release` usa E que acende o anel do botão. O CANCELAR é o
	--- que separa "mirei errado" de "atirei errado"; se ele parar de existir, o
	--- gesto único vira "toda vez que encosto sai magia" — o defeito do APK 2D.
	local tap = Balance.controls.tapMaxDuration
	local aim = Vector3.new(1, 0, 0)
	assert(Controls.willFire(nil, tap * 0.5), "toque curto sem arrasto é o tiro rápido")
	assert(Controls.willFire(aim, tap * 0.5), "arrastou e soltou rápido: dispara na direção mirada")
	assert(Controls.willFire(aim, 5), "segurou mirando e soltou: dispara, sem prazo")
	assert(not Controls.willFire(nil, tap * 2), "voltou ao centro e segurou: CANCELA, nada é enviado")
	assert(not Controls.willFire(nil, tap), "no limiar exato o tiro rápido já expirou")

	-- ------------------------------------------- PC: o cursor no plano do chão
	--- A mira do PC é CONTÍNUA e sai do cursor. Aqui só se verifica o caso
	--- degenerado — cursor em cima do próprio personagem —, que caía na
	--- constante de MUNDO do `flat` e virava tiro para o norte do mapa,
	--- ignorando a câmera. A matemática da projeção NÃO foi tocada.
	local me = Vector3.new(10, 5, -20)
	local fallback = Vector3.new(0, 0, 1)
	local straightDown = Controls.groundAim(me, me + Vector3.new(0, 30, 0), Vector3.new(0, -1, 0), fallback)
	assert(straightDown == fallback, "cursor em cima do personagem tem de cair no fallback da CÂMERA")
	assert(
		Controls.groundAim(me, me + Vector3.new(0, 30, 0), Vector3.new(0, 1, 0), fallback) == fallback,
		"raio subindo nunca cruza o chão: fallback"
	)
	assert(
		Controls.groundAim(me, me + Vector3.new(0, 30, 0), Vector3.new(1, 0, 0), fallback) == fallback,
		"raio paralelo ao chão: fallback"
	)
	local ahead = Controls.groundAim(me, me + Vector3.new(0, 30, 0), Vector3.new(0, -30, 12).Unit, fallback)
	assert(ahead.Y == 0 and math.abs(ahead.Magnitude - 1) < 1e-4, "a direção sai plana e unitária")
	assert(ahead.Z > 0.99, "cursor à frente aponta à frente")

	-- ------------------------------- PC: clique de INTERFACE não vira conjuração
	--- O disparo contínuo do PC era lido por sondagem e não sabia o que a UI
	--- tinha comido: clicar em "Grimório"/"Opções" soltava magia junto.
	assert(Controls.armPcFire(true, false) == true, "clique no mundo arma o disparo contínuo")
	assert(Controls.armPcFire(true, true) == false, "clique consumido pela UI NÃO arma disparo")
	assert(Controls.armPcFire(false, false) == false, "soltar desarma")
	assert(Controls.armPcFire(false, true) == false, "soltar em cima da UI também desarma")

	print("[Controls] autoteste OK — entrada (drag/tap/key) · cancelar do gesto · mira do cursor · clique de UI")
	return true
end

return Controls
