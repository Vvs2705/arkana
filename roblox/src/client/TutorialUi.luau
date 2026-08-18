--!strict
-- ============================================================================
-- ONBOARDING (cliente) — A TELA DO CAMPO DE PROVAS
-- Contraparte de Server/Tutorial.luau. O cliente NÃO decide nada: o servidor
-- diz em que passo o jogador está (Net.Tutorial) e aqui só se apresenta.
--
-- O QUE ESTA TELA É: uma frase por vez, no mesmo lugar, e mais nada. O roteiro
-- da auditoria é explícito — ao ACERTAR, nenhuma tela nova. A única coisa que
-- aparece por engano é o INDICADOR DE ANTECIPAÇÃO (lead), e ele aparece no
-- MUNDO, onde o tiro deveria ter ido, não numa caixa de texto.
--
-- REGRA DE ZONA (mobile-first, docs/ROBLOX.md §4): o alto-esquerdo é do HUD
-- (ArkanaHud), o alto-direito é da partida (ArkanaMatch), o baixo-direito é
-- dos botões (ArkanaControls), o baixo-esquerdo é do thumbstick nativo e a
-- faixa central-esquerda é da Sintonia (ArkanaSintonia). Sobra a COLUNA
-- CENTRAL — é onde esta tela mora, e nada dela fica preso em canto.
--
-- REGRA DE TOQUE: o miolo da tela é por onde se MIRA (arrastar). Por isso todo
-- painel aqui é `Active = false` — Frame inativo não engole o arrasto. Só os
-- dois botões de verdade (PULAR e ENTRAR NO ROYALE) capturam dedo, e os dois
-- têm ≥48dp de altura (mínimo do GDD §19.3).
-- ============================================================================

local ReplicatedStorage = game:GetService("ReplicatedStorage")
local Workspace = game:GetService("Workspace")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Elements = require(Shared:WaitForChild("Elements"))
local Net = require(Shared:WaitForChild("Net"))
local Strings = require(Shared:WaitForChild("Strings"))

-- ------------------------------------------------------------------ paleta
local NIGHT = Color3.fromHex("#0B1026")
local NIGHT_DEEP = Color3.fromHex("#05070F")
local GOLD = Color3.fromHex("#F0C75E")
local TEXT = Color3.fromHex("#E8E6F0")
local TEXT_DIM = Color3.fromHex("#9A97AD")

-- ------------------------------------------------------------------ layout (dp)
local L = {
	--- abaixo do anúncio do HUD (~132) e da faixa da Sintonia (166..212):
	--- CALIBRAÇÃO — é este número se a frase encostar em alguma das duas.
	msgTop = 228,
	msgW = 420,
	msgH = 56,
	subH = 24,
	--- PULAR no topo-centro: fora do painel do HUD (esquerda) e da barra da
	--- partida (direita), e longe do polegar que mira
	skipW = 120,
	skipH = 48, -- mínimo do GDD §19.3
	skipTop = 14,
	enterW = 300,
	enterH = 72,
	--- espelho do botão de ping do SintoniaUi (L.ping / L.pingX): o halo aponta
	--- para ELE. CALIBRAÇÃO: se a Sintonia mover o botão, mova aqui junto.
	pingX = 16,
	ping = 64,
	pad = 10,
}

--- quanto tempo o lead fica no mundo (spec: "brevemente")
local LEAD_LIFE = 1.4

--- 1 dp → px, a mesma régua das raias C e R2 (viewport de 640 px)
local function dpScale(): number
	local cam = Workspace.CurrentCamera
	return math.clamp((if cam then cam.ViewportSize.Y else 640) / 640, 0.85, 2.0)
end

--- Passo → chave de texto. `targets` REPETE a frase do passo anterior de
--- propósito: acertar alvo não abre tela nova (roteiro da auditoria).
local TEXT_KEY: { [string]: string } = {
	aim = "tutorial.aim",
	targets = "tutorial.aim",
	burn = "tutorial.burn",
	freeze = "tutorial.freeze",
	combo = "tutorial.combo",
	done = "tutorial.done",
}

-- ------------------------------------------------------------------ helpers
local function mk(class: string, props: { [string]: any }, parent: Instance?): any
	local inst = Instance.new(class)
	for k, v in pairs(props) do
		(inst :: any)[k] = v
	end
	if parent then
		inst.Parent = parent
	end
	return inst
end

-- ============================================================================
local TutorialUi = {}

local gui: ScreenGui? = nil
local fxFolder: Folder? = nil
local conns: { RBXScriptConnection } = {}

local msg: Frame
local msgLabel: TextLabel
local subLabel: TextLabel
local skipBtn: TextButton
local overlay: Frame
local enterBtn: TextButton
local pingHint: Frame
local pingStroke: UIStroke

--- estado local (só apresentação)
local step = ""
local pulse = 0
local lead: { part: BasePart, life: number }? = nil

-- ------------------------------------------------------------------- mundo
local function ensureFx(): Folder
	if fxFolder and fxFolder.Parent then
		return fxFolder :: Folder
	end
	local f = Instance.new("Folder")
	f.Name = "ArkanaTutorialFx" -- só neste cliente: não replica, não colide
	f.Parent = Workspace
	fxFolder = f
	return f
end

local function clearLead()
	if lead then
		(lead :: any).part:Destroy()
		lead = nil
	end
end

--- INDICADOR DE ANTECIPAÇÃO: um anel no chão, onde o tiro deveria ter ido, com
--- o texto por cima. É a lição inteira do passo 2 em um objeto: alvo que anda
--- se acerta MIRANDO À FRENTE.
local function showLead(pos: Vector3)
	clearLead()
	local p = Instance.new("Part")
	p.Name = "Lead"
	p.Anchored = true
	p.CanCollide = false
	p.CanQuery = false
	p.CanTouch = false
	p.CastShadow = false
	p.Material = Enum.Material.Neon
	p.Shape = Enum.PartType.Cylinder
	p.Size = Vector3.new(0.4, 7, 7)
	p.CFrame = CFrame.new(pos) * CFrame.Angles(0, 0, math.pi / 2)
	p.Color = GOLD
	p.Transparency = 0.25
	p.Parent = ensureFx()

	local bb = mk("BillboardGui", {
		Size = UDim2.fromOffset(180, 26),
		StudsOffset = Vector3.new(0, 4, 0),
		AlwaysOnTop = true,
		MaxDistance = 250,
	}, p)
	mk("TextLabel", {
		BackgroundTransparency = 1,
		Size = UDim2.fromScale(1, 1),
		Font = Enum.Font.GothamBold,
		Text = Strings.t("tutorial.lead"),
		TextColor3 = GOLD,
		TextScaled = true,
		TextStrokeColor3 = NIGHT_DEEP,
		TextStrokeTransparency = 0.2,
	}, bb)

	lead = { part = p, life = LEAD_LIFE }
end

-- -------------------------------------------------------------------- tela
local function layout()
	local k = dpScale()
	msg.Position = UDim2.new(0.5, 0, 0, math.floor(L.msgTop * k))
	msg.Size = UDim2.fromOffset(math.floor(L.msgW * k), math.floor((L.msgH + L.subH) * k))
	skipBtn.Position = UDim2.new(0.5, 0, 0, math.floor(L.skipTop * k))
	skipBtn.Size = UDim2.fromOffset(math.floor(L.skipW * k), math.floor(L.skipH * k))
	enterBtn.Size = UDim2.fromOffset(math.floor(L.enterW * k), math.floor(L.enterH * k))
	pingHint.Position = UDim2.new(0, math.floor(L.pingX * k), 0.5, 0)
	pingHint.Size = UDim2.fromOffset(math.floor(L.ping * k), math.floor(L.ping * k))
end

local function elementName(id: any): string
	local e = if Elements.isValid(id) then (Elements.ById :: any)[id] else nil
	if not e then
		return ""
	end
	return string.upper(if Strings.getLang() == "pt" then e.pt else e.en)
end

local function show(payload: any)
	step = tostring(payload.step)
	local key = TEXT_KEY[step]
	if not key then
		return
	end
	msgLabel.Text = Strings.t(key)
	--- a dica de elemento só existe quando o elemento na mão NÃO resolve o
	--- passo (o servidor é quem sabe) — dica que mente é pior que dica ausente
	local name = if payload.needSwitch == true then elementName(payload.element) else ""
	subLabel.Text = if name ~= "" then Strings.t("tutorial.switch", name) else ""
	subLabel.Visible = name ~= ""

	msg.Visible = true
	skipBtn.Visible = step ~= "done"
	pingHint.Visible = step == "combo"
	overlay.Visible = step == "done"
end

local function hide()
	step = ""
	msg.Visible = false
	skipBtn.Visible = false
	pingHint.Visible = false
	overlay.Visible = false
	clearLead()
end

local function onServer(payload: any)
	if typeof(payload) ~= "table" then
		return
	end
	local kind = tostring((payload :: any).step)
	if kind == "lead" then
		if typeof((payload :: any).pos) == "Vector3" then
			showLead((payload :: any).pos)
		end
	elseif kind == "end" then
		hide()
	else
		show(payload)
	end
end

local function build(root: ScreenGui)
	-- ------------------------------------------------------------- a frase
	--- Active = false em TODO painel: o miolo da tela é por onde se mira, e um
	--- Frame ativo aqui roubaria o arrasto do jogador.
	msg = mk("Frame", {
		Name = "Message",
		Active = false,
		AnchorPoint = Vector2.new(0.5, 0),
		BackgroundColor3 = NIGHT,
		BackgroundTransparency = 0.25,
		BorderSizePixel = 0,
		Visible = false,
	}, root)
	mk("UICorner", { CornerRadius = UDim.new(0, 10) }, msg)
	mk("UIStroke", { Color = GOLD, Thickness = 2, Transparency = 0.35 }, msg)

	-- a frase ocupa a parte de cima do painel; a dica de elemento, a de baixo
	msgLabel = mk("TextLabel", {
		BackgroundTransparency = 1,
		Position = UDim2.new(0, L.pad, 0, 0),
		Size = UDim2.new(1, -L.pad * 2, L.msgH / (L.msgH + L.subH), 0),
		Font = Enum.Font.GothamBold,
		Text = "",
		TextColor3 = TEXT,
		TextScaled = true,
		TextStrokeColor3 = NIGHT_DEEP,
		TextStrokeTransparency = 0.4,
	}, msg)
	mk("UITextSizeConstraint", { MaxTextSize = 34 }, msgLabel)

	subLabel = mk("TextLabel", {
		BackgroundTransparency = 1,
		AnchorPoint = Vector2.new(0.5, 1),
		Position = UDim2.new(0.5, 0, 1, -4),
		Size = UDim2.new(1, -L.pad * 2, L.subH / (L.msgH + L.subH), -4),
		Font = Enum.Font.Gotham,
		Text = "",
		TextColor3 = TEXT_DIM,
		TextScaled = true,
		Visible = false,
	}, msg)
	mk("UITextSizeConstraint", { MaxTextSize = 18 }, subLabel)

	-- -------------------------------------------------------------- PULAR
	--- Respeita quem já sabe jogar: um toque, sem confirmação, sem "tem
	--- certeza?". O servidor decide o que fazer com isso.
	skipBtn = mk("TextButton", {
		Name = "Skip",
		AnchorPoint = Vector2.new(0.5, 0),
		BackgroundColor3 = NIGHT,
		BackgroundTransparency = 0.35,
		AutoButtonColor = false,
		BorderSizePixel = 0,
		Font = Enum.Font.GothamBold,
		Text = Strings.t("tutorial.skip"),
		TextColor3 = TEXT_DIM,
		TextScaled = true,
		Visible = false,
	}, root)
	mk("UICorner", { CornerRadius = UDim.new(0, 8) }, skipBtn)
	mk("UITextSizeConstraint", { MaxTextSize = 18 }, skipBtn)

	-- ------------------------------------------------- halo do botão COMBO?
	--- "Pingue COMBO." é o ÚNICO tutorial verbal do roteiro — e uma frase que
	--- manda tocar um botão sem dizer qual é meia frase. O halo pisca em volta
	--- do botão da Sintonia. Inativo: quem recebe o toque é o botão de baixo.
	pingHint = mk("Frame", {
		Name = "PingHint",
		Active = false,
		AnchorPoint = Vector2.new(0, 0.5),
		BackgroundTransparency = 1,
		Visible = false,
	}, root)
	mk("UICorner", { CornerRadius = UDim.new(1, 0) }, pingHint)
	pingStroke = mk("UIStroke", { Color = GOLD, Thickness = 3 }, pingHint)

	-- ---------------------------------------------------- ENTRAR NO ROYALE
	overlay = mk("Frame", {
		Name = "Done",
		BackgroundColor3 = NIGHT_DEEP,
		BackgroundTransparency = 0.45,
		BorderSizePixel = 0,
		Size = UDim2.fromScale(1, 1),
		Visible = false,
	}, root)
	enterBtn = mk("TextButton", {
		Name = "Enter",
		AnchorPoint = Vector2.new(0.5, 0.5),
		Position = UDim2.fromScale(0.5, 0.62),
		BackgroundColor3 = GOLD,
		AutoButtonColor = true,
		BorderSizePixel = 0,
		Font = Enum.Font.GothamBold,
		Text = Strings.t("tutorial.enter"),
		TextColor3 = NIGHT,
		TextScaled = true,
	}, overlay)
	mk("UICorner", { CornerRadius = UDim.new(0, 12) }, enterBtn)
	mk("UITextSizeConstraint", { MaxTextSize = 28 }, enterBtn)
end

-- ------------------------------------------------------------------- API
function TutorialUi.init(player: Player)
	if gui then
		return
	end
	Strings.followPlayerLocale()

	local playerGui = player:WaitForChild("PlayerGui")
	local root = mk("ScreenGui", {
		Name = "ArkanaTutorial", -- HUD=ArkanaHud · C=ArkanaControls · R2=ArkanaSintonia
		ResetOnSpawn = false,
		IgnoreGuiInset = false,
		--- acima do HUD (4), dos botões (5) e da Sintonia/partida (6); abaixo da
		--- acessibilidade (8), que precisa cobrir tudo
		DisplayOrder = 7,
		ZIndexBehavior = Enum.ZIndexBehavior.Sibling,
	}, playerGui)
	gui = root

	build(root)
	layout()

	local ev = Net.event(Net.Names.Tutorial)
	--- PULAR e ENTRAR NO ROYALE mandam a MESMA intenção: sair do Campo de
	--- Provas. É o servidor que sabe se isso foi conclusão ou abandono.
	local function leave()
		ev:FireServer({ step = "skip" })
		hide()
	end
	table.insert(conns, skipBtn.Activated:Connect(leave))
	table.insert(conns, enterBtn.Activated:Connect(leave))
	table.insert(conns, ev.OnClientEvent:Connect(onServer))

	local cam = Workspace.CurrentCamera
	if cam then
		table.insert(conns, cam:GetPropertyChangedSignal("ViewportSize"):Connect(layout))
	end
end

function TutorialUi.update(dt: number)
	if not gui then
		return
	end
	if pingHint.Visible then
		pulse = (pulse + dt * 2.4) % (math.pi * 2)
		pingStroke.Transparency = 0.15 + 0.45 * (0.5 + 0.5 * math.sin(pulse))
	end
	local l = lead
	if l then
		l.life -= dt
		if l.life <= 0 then
			clearLead()
		else
			--- some crescendo: o lead é um aviso, não um marcador permanente
			local f = 1 - l.life / LEAD_LIFE
			l.part.Transparency = 0.25 + 0.75 * f
			l.part.Size = Vector3.new(0.4, 7 + 5 * f, 7 + 5 * f)
		end
	end
end

return TutorialUi
