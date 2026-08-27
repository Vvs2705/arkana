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

local L = {
	--- 48dp é o MÍNIMO do GDD §10 que o cabeçalho deste arquivo cita — e este
	--- botão estava em 34, o único alvo de dedo do cliente abaixo da régua.
	--- Em celular ele fica entre o Selo e a barra da partida: errar o toque aqui
	--- é errar a porta de entrada da acessibilidade inteira.
	openSize = 48,
	openTop = 12,
	--- à DIREITA do Selo de Arkana (72px centrado no topo): o Selo vai até +36,
	--- a barra da partida começa a 202px da borda direita. Subiu junto com o
	--- `openSize`: com 48dp e o antigo 56 o botão encostaria no Selo (+36).
	--- CALIBRAÇÃO: é este número se o botão encostar na barra em tela estreita.
	openX = 62,
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
		{ label = Strings.t("a11y.off"), value = false },
		{ label = Strings.t("a11y.on"), value = true },
	})
end

local function amount(parent: Instance, order: number, key: string, name: string)
	choiceRow(parent, order, key, name, {
		{ label = Strings.t("a11y.off"), value = 0 },
		{ label = Strings.t("a11y.half"), value = 0.5 },
		{ label = Strings.t("a11y.full"), value = 1 },
	})
end

--- VOLUME é fração 0..1 igual a `shake`/`flash`/`vfx` — então usa O MESMO
--- widget e a mesma gramática de três respostas. Um slider de arrastar seria um
--- SEGUNDO idioma de controle na mesma tela (e o pior deles no celular: alvo
--- fino, precisa de precisão e não perdoa o dedo).
--- O "cheio" NÃO é 1 fixo: é o padrão da loja, que espelha o mix de fábrica do
--- `Audio`. Assim o chip aceso ao abrir o menu bate com o que o jogo está
--- tocando, em vez de mentir "CHEIO" num volume que ninguém escolheu.
local function volumeRow(key: string): { Option }
	local full = tonumber(A11y.defaults()[key]) or 1
	return {
		{ label = Strings.t("a11y.audio.mute"), value = 0 },
		{ label = Strings.t("a11y.half"), value = full / 2 },
		{ label = Strings.t("a11y.full"), value = full },
	}
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
		Text = Strings.t("a11y.title"),
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
		Text = Strings.t("a11y.hint"),
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
		sizes[i] = { label = Strings.t("a11y.size." .. i), value = value }
	end

	choiceRow(list, 1, "uiSize", Strings.t("a11y.uisize"), sizes)
	toggle(list, 2, "leftHanded", Strings.t("a11y.lefthanded"))
	toggle(list, 3, "contrast", Strings.t("a11y.contrast"))
	amount(list, 4, "shake", Strings.t("a11y.shake"))
	amount(list, 5, "flash", Strings.t("a11y.flash"))
	amount(list, 6, "vfx", Strings.t("a11y.vfx"))
	toggle(list, 7, "blur", Strings.t("a11y.blur"))
	toggle(list, 8, "captions", Strings.t("a11y.captions"))
	toggle(list, 9, "readyCue", Strings.t("a11y.readycue"))

	--- ÚNICO rótulo de seção da tela: som é o bloco que o jogador procura POR
	--- NOME ("cadê o volume?"), e num alpha público é o que decide entre baixar
	--- o volume e fechar o jogo. Fica no fim porque quem chega até aqui rolando
	--- já leu o resto — e o rótulo em dourado marca o corte.
	mk("UITextSizeConstraint", { MaxTextSize = 13 }, mk("TextLabel", {
		BackgroundTransparency = 1,
		LayoutOrder = 10,
		Size = UDim2.new(1, 0, 0, 26),
		Font = Enum.Font.GothamBold,
		Text = Strings.t("a11y.audio"),
		TextColor3 = GOLD,
		TextScaled = true,
		TextXAlignment = Enum.TextXAlignment.Left,
		TextYAlignment = Enum.TextYAlignment.Bottom,
	}, list))

	--- DENSIDADE DE HUD (R11): a queixa do Diretor foi "informação demais". Vira
	--- escolha do jogador e não decisão nossa — quem joga competitivo quer tudo,
	--- quem está aprendendo quer o essencial. Mesma linha de chips das outras.
	choiceRow(list, 10, "hudDensity", Strings.t("a11y.hud"), {
		{ label = Strings.t("a11y.hud.min"), value = "min" },
		{ label = Strings.t("a11y.hud.full"), value = "full" },
	})
	choiceRow(list, 11, "volMaster", Strings.t("a11y.audio.master"), volumeRow("volMaster"))
	choiceRow(list, 12, "volWorld", Strings.t("a11y.audio.world"), volumeRow("volWorld"))
end

-- ------------------------------------------------------------------ API
function Settings.init(player: Player)
	if gui then
		return
	end
	Strings.followPlayerLocale()

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

-- ============================================================================
-- AUTOTESTE — roda no harness (roblox/tools/run.sh) e no Studio:
--   require(game.Players.LocalPlayer.PlayerScripts.Client.Settings).selfTest()
-- Não desenha nada. Cobre as três coisas que fazem uma linha desta tela mentir
-- SEM erro nenhum no console: chip que nunca acende, mudo que sumiu e chave de
-- texto que não existe (vira o próprio nome da chave na cara do jogador).
-- ============================================================================
function Settings.selfTest()
	-- alvo de toque: regra dura do GDD §10, reprovada em teste de aparelho
	assert(L.chipH >= 48 and L.openSize >= 48, "alvo de toque abaixo de 48dp")

	for _, key in ipairs({ "volMaster", "volWorld" }) do
		local def = tonumber(A11y.defaults()[key])
		assert(def, "chave de volume sumiu da loja de preferências: " .. key)
		local hasMute, hasDefault = false, false
		for _, opt in ipairs(volumeRow(key)) do
			local v = tonumber(opt.value) or -1
			assert(v >= 0 and v <= def, key .. ": chip fora do intervalo 0..padrão")
			hasMute = hasMute or v == 0
			hasDefault = hasDefault or v == def
		end
		-- MUDO é o motivo de esta tela existir para o alpha público
		assert(hasMute, key .. " sem chip de MUDO")
		-- sem um chip no valor padrão, o menu abre com a linha APAGADA e o
		-- jogador conclui que a opção está quebrada
		assert(hasDefault, key .. ": nenhum chip bate com o padrão da loja")
	end

	-- `Strings.t` devolve a PRÓPRIA CHAVE quando ela não existe: sem este teste,
	-- uma chave faltando estreia como "a11y.audio.master" escrito no menu.
	for _, key in ipairs({ "a11y.audio", "a11y.audio.master", "a11y.audio.world", "a11y.audio.mute" }) do
		assert(Strings.t(key) ~= key, "chave de texto faltando em Strings: " .. key)
	end

	print("[Settings] autoteste OK — volume: mudo + padrão aceso · alvos 48dp · textos por chave")
	return true
end

return Settings
