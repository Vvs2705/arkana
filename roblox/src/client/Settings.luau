--!strict
-- ============================================================================
-- RAIA D (UI) — MENU DE ACESSIBILIDADE E CONFORTO.
--
-- É só a TELA: quem guarda, aplica e persiste é o `Accessibility.luau`. Aqui
-- não existe regra nenhuma — cada linha lê `A11y.get` e escreve `A11y.set`.
--
-- MOBILE-FIRST (docs/ROBLOX.md §4): o botão que abre é pequeno e mora no ALTO
-- (fora da zona de polegar, que é dos botões de combate da raia C); o menu em
-- si é MODAL de tela cheia — enquanto ele está aberto a zona de polegar não
-- vale mais nada, então os alvos podem ser grandes e ficar onde couber.
-- Alvos de 48dp+ (mínimo do GDD §10).
--
-- IDENTIDADE (GDD §10): azul-noite + dourado, painéis chapados, cantos vivos.
-- ACESSIBILIDADE: a opção escolhida não é só COR — leva um marcador "•" e
-- borda grossa, legível em preto e branco.
-- ============================================================================

local ReplicatedStorage = game:GetService("ReplicatedStorage")

local Strings = require(ReplicatedStorage:WaitForChild("Shared"):WaitForChild("Strings"))
local A11y = require(script.Parent:WaitForChild("Accessibility"))

-- ------------------------------------------------------------------ paleta
local NIGHT = Color3.fromHex("#0B1026")
local NIGHT_DEEP = Color3.fromHex("#05070F")
local GOLD = Color3.fromHex("#F0C75E")
local GOLD_DIM = Color3.fromHex("#8A7336")
local TEXT = Color3.fromHex("#E8E6F0")
local TEXT_DIM = Color3.fromHex("#9A97AD")

-- PENDÊNCIA: migrar para Strings. `Shared/Strings.luau` é da raia de ONBOARDING
-- nesta leva e esta raia não pode editá-lo. Quando as chaves `a11y.*` existirem,
-- este bloco vira Strings.t() e some — nada mais muda.
local L10N = {
	pt = {
		title = "ACESSIBILIDADE E CONFORTO",
		close = "FECHAR",
		uiSize = "TAMANHO DA UI",
		leftHanded = "HUD CANHOTO",
		contrast = "ALTO CONTRASTE",
		shake = "TREMOR DE TELA",
		flash = "FLASHES",
		vfx = "INTENSIDADE DE VFX",
		blur = "DESFOQUE (BLUR/DOF)",
		captions = "LEGENDAS DOS AVISOS",
		readyCue = "AVISO DE SINTONIA",
		off = "NÃO",
		on = "SIM",
		half = "MÉDIO",
		full = "CHEIO",
		sizes = { "P", "M", "G", "GG" },
		hint = "O tamanho do texto e a vibração seguem as opções do seu Roblox.",
	},
	en = {
		title = "ACCESSIBILITY & COMFORT",
		close = "CLOSE",
		uiSize = "UI SIZE",
		leftHanded = "LEFT-HANDED HUD",
		contrast = "HIGH CONTRAST",
		shake = "SCREEN SHAKE",
		flash = "FLASHES",
		vfx = "VFX INTENSITY",
		blur = "BLUR / DOF",
		captions = "CUE CAPTIONS",
		readyCue = "ATTUNEMENT CUE",
		off = "OFF",
		on = "ON",
		half = "HALF",
		full = "FULL",
		sizes = { "S", "M", "L", "XL" },
		hint = "Text size and vibration follow your Roblox settings.",
	},
}

local L = {
	openSize = 34,
	openTop = 12,
	--- à DIREITA do Selo de Arkana (72px centrado no topo): o Selo vai até +36,
	--- a barra da partida começa a 202px da borda direita. CALIBRAÇÃO: é este
	--- número se o botão encostar na barra em tela estreita.
	openX = 56,
	rowH = 56,
	chipW = 62,
	chipH = 48,
	pad = 16,
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
local Settings = {}

local gui: ScreenGui? = nil
local conns: { RBXScriptConnection } = {}
local refreshers: { () -> () } = {}
local backdrop: Frame
local tr: any = L10N.pt

type Option = { label: string, value: any }

--- Uma linha = rótulo + N chips. Serve para liga/desliga (2 chips) e para
--- fração (3 chips) sem inventar dois widgets: é a mesma pergunta com número
--- diferente de respostas.
local function choiceRow(parent: Instance, order: number, key: string, name: string, options: { Option })
	local row = mk("Frame", {
		Name = key,
		BackgroundTransparency = 1,
		LayoutOrder = order,
		Size = UDim2.new(1, 0, 0, L.rowH),
	}, parent)

	mk("UITextSizeConstraint", { MaxTextSize = 15 }, mk("TextLabel", {
		BackgroundTransparency = 1,
		AnchorPoint = Vector2.new(0, 0.5),
		Position = UDim2.new(0, 0, 0.5, 0),
		Size = UDim2.new(0.4, 0, 0, 20),
		Font = Enum.Font.GothamBold,
		Text = name,
		TextColor3 = TEXT,
		TextScaled = true,
		TextXAlignment = Enum.TextXAlignment.Left,
	}, row))

	local box = mk("Frame", {
		AnchorPoint = Vector2.new(1, 0.5),
		BackgroundTransparency = 1,
		Position = UDim2.new(1, 0, 0.5, 0),
		Size = UDim2.new(0.58, 0, 0, L.chipH),
	}, row)
	mk("UIListLayout", {
		FillDirection = Enum.FillDirection.Horizontal,
		HorizontalAlignment = Enum.HorizontalAlignment.Right,
		VerticalAlignment = Enum.VerticalAlignment.Center,
		Padding = UDim.new(0, 6),
		SortOrder = Enum.SortOrder.LayoutOrder,
	}, box)

	local chips: { TextButton } = {}
	local strokes: { UIStroke } = {}
	for i, opt in ipairs(options) do
		local chip = mk("TextButton", {
			BackgroundColor3 = NIGHT,
			BorderSizePixel = 0,
			AutoButtonColor = false,
			LayoutOrder = i,
			Size = UDim2.fromOffset(L.chipW, L.chipH),
			Font = Enum.Font.GothamBold,
			Text = opt.label,
			TextColor3 = TEXT_DIM,
			TextScaled = true,
		}, box)
		mk("UITextSizeConstraint", { MaxTextSize = 14 }, chip)
		strokes[i] = mk("UIStroke", { Color = GOLD_DIM, Thickness = 1, Transparency = 0.3 }, chip)
		chips[i] = chip
		table.insert(conns, chip.Activated:Connect(function()
			A11y.set(key, opt.value)
		end))
	end

	local function refresh()
		local cur = A11y.get(key)
		for i, opt in ipairs(options) do
			local on = cur == opt.value
			-- COR + MARCADOR: quem não distingue dourado de azul-noite ainda lê o
			-- "•" e a borda grossa (mesma regra do GDD §10 aplicada à opção)
			chips[i].BackgroundColor3 = if on then GOLD else NIGHT
			chips[i].TextColor3 = if on then NIGHT_DEEP else TEXT_DIM
			chips[i].Text = if on then "• " .. opt.label else opt.label
			strokes[i].Color = if on then GOLD else GOLD_DIM
			strokes[i].Thickness = if on then 2 else 1
		end
	end
	refresh()
	table.insert(refreshers, refresh)
end

local function toggle(parent: Instance, order: number, key: string, name: string)
	choiceRow(parent, order, key, name, {
		{ label = tr.off, value = false },
		{ label = tr.on, value = true },
	})
end

local function amount(parent: Instance, order: number, key: string, name: string)
	choiceRow(parent, order, key, name, {
		{ label = tr.off, value = 0 },
		{ label = tr.half, value = 0.5 },
		{ label = tr.full, value = 1 },
	})
end

-- ------------------------------------------------------------------ build
local function buildOpen(root: ScreenGui)
	local btn = mk("TextButton", {
		Name = "OpenSettings",
		BackgroundColor3 = NIGHT,
		BackgroundTransparency = 0.2,
		BorderSizePixel = 0,
		AutoButtonColor = false,
		Size = UDim2.fromOffset(L.openSize, L.openSize),
		Text = "",
	}, root)
	mk("UIStroke", { Color = GOLD_DIM, Thickness = 1, Transparency = 0.2 }, btn)
	-- três traços em Frame: zero glifo, zero asset — e legível em qualquer fonte
	for i = 1, 3 do
		mk("Frame", {
			BackgroundColor3 = GOLD,
			BorderSizePixel = 0,
			AnchorPoint = Vector2.new(0.5, 0.5),
			Position = UDim2.fromScale(0.5, 0.28 + (i - 1) * 0.22),
			Size = UDim2.fromScale(0.52, 0.09),
		}, btn)
	end
	--- canônico = destro (à direita do Selo). O espelho canhoto é do A11y.
	A11y.place(btn, Vector2.new(0.5, 0), UDim2.new(0.5, L.openX, 0, L.openTop))
	table.insert(conns, btn.Activated:Connect(function()
		backdrop.Visible = not backdrop.Visible
	end))
end

local function buildPanel(root: ScreenGui)
	backdrop = mk("Frame", {
		Name = "Backdrop",
		BackgroundColor3 = NIGHT_DEEP,
		BackgroundTransparency = 0.3,
		BorderSizePixel = 0,
		Size = UDim2.fromScale(1, 1),
		Visible = false,
	}, root)
	-- fundo de tela cheia não escala: crescer 130% só o faria vazar da tela
	backdrop:SetAttribute("A11yNoScale", true)

	local card = mk("Frame", {
		Name = "Card",
		AnchorPoint = Vector2.new(0.5, 0.5),
		BackgroundColor3 = NIGHT,
		BackgroundTransparency = 0.02,
		BorderSizePixel = 0,
		Position = UDim2.fromScale(0.5, 0.5),
		Size = UDim2.fromScale(0.88, 0.86),
	}, backdrop)
	mk("UISizeConstraint", { MinSize = Vector2.new(300, 240), MaxSize = Vector2.new(560, 460) }, card)
	mk("UIStroke", { Color = GOLD, Thickness = 2, Transparency = 0.1 }, card)
	mk("UIPadding", {
		PaddingLeft = UDim.new(0, L.pad),
		PaddingRight = UDim.new(0, L.pad),
		PaddingTop = UDim.new(0, 12),
		PaddingBottom = UDim.new(0, 12),
	}, card)

	mk("UITextSizeConstraint", { MaxTextSize = 17 }, mk("TextLabel", {
		BackgroundTransparency = 1,
		Size = UDim2.new(1, -56, 0, 20),
		Font = Enum.Font.GothamBold,
		Text = tr.title,
		TextColor3 = GOLD,
		TextScaled = true,
		TextXAlignment = Enum.TextXAlignment.Left,
	}, card))

	local close = mk("TextButton", {
		Name = "Close",
		AnchorPoint = Vector2.new(1, 0),
		BackgroundColor3 = NIGHT_DEEP,
		BorderSizePixel = 0,
		AutoButtonColor = false,
		Position = UDim2.new(1, 0, 0, -2),
		Size = UDim2.fromOffset(48, 48),
		Font = Enum.Font.GothamBold,
		Text = "×",
		TextColor3 = TEXT,
		TextScaled = true,
	}, card)
	mk("UIStroke", { Color = GOLD_DIM, Thickness = 1, Transparency = 0.3 }, close)
	mk("UITextSizeConstraint", { MaxTextSize = 26 }, close)
	table.insert(conns, close.Activated:Connect(function()
		backdrop.Visible = false
	end))

	mk("UITextSizeConstraint", { MaxTextSize = 11 }, mk("TextLabel", {
		BackgroundTransparency = 1,
		AnchorPoint = Vector2.new(0, 1),
		Position = UDim2.new(0, 0, 1, 0),
		Size = UDim2.new(1, 0, 0, 14),
		Font = Enum.Font.Gotham,
		Text = tr.hint,
		TextColor3 = TEXT_DIM,
		TextScaled = true,
		TextXAlignment = Enum.TextXAlignment.Left,
	}, card))

	local list = mk("ScrollingFrame", {
		Name = "Options",
		BackgroundTransparency = 1,
		BorderSizePixel = 0,
		Position = UDim2.new(0, 0, 0, 30),
		Size = UDim2.new(1, 0, 1, -50),
		--- recurso nativo em vez de recalcular CanvasSize a cada linha
		AutomaticCanvasSize = Enum.AutomaticSize.Y,
		CanvasSize = UDim2.new(),
		ScrollBarThickness = 4,
		ScrollBarImageColor3 = GOLD_DIM,
	}, card)
	mk("UIListLayout", {
		FillDirection = Enum.FillDirection.Vertical,
		Padding = UDim.new(0, 2),
		SortOrder = Enum.SortOrder.LayoutOrder,
	}, list)

	local sizes: { Option } = {}
	for i, value in ipairs({ 0.9, 1, 1.15, 1.3 }) do
		sizes[i] = { label = tr.sizes[i], value = value }
	end

	choiceRow(list, 1, "uiSize", tr.uiSize, sizes)
	toggle(list, 2, "leftHanded", tr.leftHanded)
	toggle(list, 3, "contrast", tr.contrast)
	amount(list, 4, "shake", tr.shake)
	amount(list, 5, "flash", tr.flash)
	amount(list, 6, "vfx", tr.vfx)
	toggle(list, 7, "blur", tr.blur)
	toggle(list, 8, "captions", tr.captions)
	toggle(list, 9, "readyCue", tr.readyCue)
end

-- ------------------------------------------------------------------ API
function Settings.init(player: Player)
	if gui then
		return
	end
	Strings.followPlayerLocale()
	tr = L10N[Strings.getLang()] or L10N.pt

	local playerGui = player:WaitForChild("PlayerGui")
	local root = mk("ScreenGui", {
		Name = "ArkanaSettings",
		ResetOnSpawn = false,
		IgnoreGuiInset = false,
		DisplayOrder = 20, -- modal: cobre HUD (4), controles (5), partida (6)
		ZIndexBehavior = Enum.ZIndexBehavior.Sibling,
	}, playerGui)
	gui = root

	buildPanel(root)
	buildOpen(root)
	A11y.attach(root)

	--- os chips repintam quando o VALOR muda, não quando o botão é tocado: assim
	--- qualquer outro caminho que mexa na preferência aparece aqui na hora.
	A11y.onChange(function()
		for _, fn in ipairs(refreshers) do
			fn()
		end
	end)
end

--- Sem laço próprio: o menu é todo por evento. Existe para o coordenador poder
--- fiar `Settings.update` junto com o resto sem tratar este módulo à parte.
function Settings.update(_dt: number) end

function Settings.destroy()
	for _, c in ipairs(conns) do
		c:Disconnect()
	end
	table.clear(conns)
	table.clear(refreshers)
	if gui then
		gui:Destroy()
		gui = nil
	end
end

return Settings
