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
-- MOVIMENTO E CÂMERA SÃO NATIVOS. O thumbstick de toque e o WASD vêm prontos
-- no PlayerModule do Roblox e a câmera de 3ª pessoa também: "aproveitar os
-- sistemas da plataforma" (docs/ROBLOX.md §2). Aqui só ajustamos o zoom para
-- leitura de combate. Reinventar joystick é proibido.
--
-- AUTORIDADE: este arquivo só ENVIA INTENÇÃO (Net.CastSpell/SwitchElement/
-- Dodge). Nenhum dano, mana ou alcance é decidido aqui — é tudo do servidor.
-- O cooldown desenhado no botão é PREVISÃO local; StatsUpdate corrige.
-- ============================================================================

local ContextActionService = game:GetService("ContextActionService")
local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local UserInputService = game:GetService("UserInputService")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Balance = require(Shared:WaitForChild("Balance"))
local Elements = require(Shared:WaitForChild("Elements"))
local Net = require(Shared:WaitForChild("Net"))
local ControlsTouch = require(script.Parent:WaitForChild("ControlsTouch"))

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

local function doCast(kind: string, dir: Vector3)
	local from = rootPosition()
	if not from or not remCast then
		return
	end
	if kind == "tactic" and not ready("tactic") then
		return
	end
	(remCast :: RemoteEvent):FireServer({ kind = kind, dir = assisted(from, dir) })
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

-- ------------------------------------------------------- máquina do gesto

local function updateDrag(g: Gesture, input: InputObject)
	local delta = Vector2.new(input.Position.X, input.Position.Y) - g.origin
	g.dragPx = delta.Magnitude
	-- de volta ao centro = mira desarmada (soltar aí em diante CANCELA)
	g.dir = if g.dragPx >= Balance.controls.aimDeadzonePx then screenToWorld(delta) else nil
end

local function release(g: Gesture)
	if touchUi then
		(touchUi :: TouchHandle).setPressed(g.kind, false)
	end
	local dir = g.dir
	if not dir and os.clock() - g.startedAt >= Balance.controls.tapMaxDuration then
		return -- segurou e voltou ao centro: CANCELA
	end
	if g.kind == "dodge" then
		-- toque curto na esquiva: rola para onde já se está andando (nil), que é
		-- mais útil do que a direção da câmera
		doDodge(dir)
	else
		doCast(g.kind, dir or facing()) -- toque curto: tiro rápido, direção da câmera
	end
end

--- Chamado pelo botão de toque no instante em que o dedo desce.
local function beginGesture(kind: string, input: InputObject)
	active[input] = {
		kind = kind,
		input = input,
		origin = Vector2.new(input.Position.X, input.Position.Y),
		startedAt = os.clock(),
		dragPx = 0,
		dir = nil,
	}
	if touchUi then
		(touchUi :: TouchHandle).setPressed(kind, true)
	end
end

--- Gesto que está mirando agora (o que desenha a seta no mundo). Também varre
--- toque cancelado pelo sistema (app em segundo plano), que não gera
--- InputEnded — senão a seta ficaria presa na tela.
local function aimingGesture(): Gesture?
	local found: Gesture? = nil
	for input, g in pairs(active) do
		if g.input.UserInputState == Enum.UserInputState.End then
			active[input] = nil
		elseif g.dir and not found then
			found = g
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

local function hideAimMarker()
	for _, p in ipairs(trail) do
		p.Transparency = 1
	end
	for _, p in ipairs(head) do
		p.Transparency = 1
	end
end

local function drawAimMarker(from: Vector3, dir: Vector3)
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

-- ------------------------------------------------------------- PC (teclado)

local function pcAimDirection(from: Vector3): Vector3
	local loc = UserInputService:GetMouseLocation()
	local ray = camera:ScreenPointToRay(loc.X, loc.Y)
	--- intersecta o plano do chão na altura do personagem (Mouse.Hit erraria a
	--- direção sempre que houvesse parede/árvore perto)
	if math.abs(ray.Direction.Y) > 1e-4 then
		local t = (from.Y - ray.Origin.Y) / ray.Direction.Y
		if t > 0 then
			return flat((ray.Origin + ray.Direction * t) - from)
		end
	end
	return facing()
end

local function bindKeyboard()
	-- Espaço = Esquiva. Sink derruba o pulo padrão: esquiva com i-frames é a
	-- mecânica do GDD §15, pular não é. (Toque continua com botão próprio.)
	ContextActionService:BindAction("ArkanaDodge", function(_, state)
		if state == Enum.UserInputState.Begin then
			doDodge(nil)
		end
		return Enum.ContextActionResult.Sink
	end, false, Enum.KeyCode.Space, Enum.KeyCode.LeftShift)

	UserInputService.InputBegan:Connect(function(input, processed)
		if processed then
			return
		end
		if input.KeyCode == Enum.KeyCode.Q then
			cycleElement(1)
			return
		elseif input.KeyCode == Enum.KeyCode.E then
			cycleElement(-1)
			return
		end
		local slot = table.find(NUM_KEYS, input.KeyCode)
		if slot then
			setElement(Elements.Order[slot])
			return
		end
		if input.UserInputType == Enum.UserInputType.MouseButton2 then
			local from = rootPosition()
			if from then
				doCast("tactic", pcAimDirection(from))
			end
		end
	end)
end

-- ------------------------------------------------------------------ fachada

function Controls.init(p: Player)
	player = p

	-- câmera de 3ª pessoa NATIVA, só afastada para caber o combate na tela.
	-- ponytail: o Roblox não expõe API suportada para o ângulo inicial — o
	-- zoom é o knob real de leitura de combate. Ajustar no playtest.
	p.CameraMode = Enum.CameraMode.Classic
	p.CameraMaxZoomDistance = 26
	p.CameraMinZoomDistance = 14

	remCast = Net.event(Net.Names.CastSpell)
	remSwitch = Net.event(Net.Names.SwitchElement)
	remDodge = Net.event(Net.Names.Dodge)

	buildAimMarker()

	if UserInputService.TouchEnabled then
		touchUi = ControlsTouch.build(p, beginGesture, setElement)
	end
	if UserInputService.KeyboardEnabled then
		bindKeyboard()
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
			active[input] = nil
			release(g)
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
				cdTotal[kind] = math.max(cdTotal[kind], left)
			end
		end
	end)

	-- servidor e cliente começam no mesmo elemento
	if remSwitch then
		(remSwitch :: RemoteEvent):FireServer({ id = element })
	end
end

function Controls.update(_dt: number)
	local from = rootPosition()

	-- PC: M1 segurado atira continuamente na direção do mouse (o fireRate real
	-- é do servidor; aqui só evitamos inundar o remote)
	if
		from
		and UserInputService.MouseEnabled
		and UserInputService:IsMouseButtonPressed(Enum.UserInputType.MouseButton1)
		and not UserInputService:GetFocusedTextBox()
	then
		if os.clock() - lastBasic >= (Balance.elements :: any)[element].basic.fireRate then
			doCast("basic", pcAimDirection(from))
		end
	end

	-- seta de mira: gesto de toque em curso, ou o mouse do PC enquanto atira
	local g = aimingGesture()
	if from and g and g.dir then
		drawAimMarker(from, assisted(from, g.dir :: Vector3))
	elseif
		from
		and UserInputService.MouseEnabled
		and UserInputService:IsMouseButtonPressed(Enum.UserInputType.MouseButton1)
	then
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

return Controls
