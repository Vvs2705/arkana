--!strict
-- ============================================================================
-- RAIA DO GRIMÓRIO (UI) — A PÁGINA ILUMINADA (GDD §18.3, versão Lite)
--
-- Só a TELA: quem decide o que foi descoberto é o servidor (Grimoire.luau).
-- Este módulo recebe { pages, found, latest? } pelo remote "Grimoire" e mostra.
-- Nenhum toque daqui muda estado de jogo — abrir o livro é olhar, não agir.
--
-- DUAS PEÇAS:
--   1. TOAST — aviso discreto quando uma página acende. Fica no rodapé-centro
--      (acima da faixa de legendas do A11y), em ScreenGui de DisplayOrder 3:
--      ABAIXO do HUD (4), dos botões (5) e de tudo — nunca tapa combate.
--   2. LIVRO — modal com as 12 entradas. Descoberta = título + descrição em
--      dourado; trancada = "? ? ?" (a graça é caçar — o enigma não entrega).
--
-- MOBILE-FIRST (GDD §19.3): alvos ≥48dp. O botão que abre mora no ALTO, à
-- ESQUERDA do Selo — espelho do botão de opções (que fica à direita). No modo
-- canhoto o A11y espelha os dois juntos, então nunca colidem.
--
-- IDENTIDADE (GDD §10): azul-noite + dourado, mesma paleta do Settings.
-- Dono: RAIA DO GRIMÓRIO.
-- ============================================================================

local ReplicatedStorage = game:GetService("ReplicatedStorage")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Strings = require(Shared:WaitForChild("Strings"))
local Net = require(Shared:WaitForChild("Net"))
local A11y = require(script.Parent:WaitForChild("Accessibility"))

local Ui = {}

-- ------------------------------------------------------------------ paleta
local NIGHT = Color3.fromHex("#0B1026")
local NIGHT_DEEP = Color3.fromHex("#05070F")
local GOLD = Color3.fromHex("#F0C75E")
local GOLD_DIM = Color3.fromHex("#8A7336")
local TEXT = Color3.fromHex("#E8E6F0")
local TEXT_DIM = Color3.fromHex("#9A97AD")

local L = {
	--- 48dp: mínimo do GDD §19.3 — mesmo tamanho do botão de opções
	openSize = 48,
	openTop = 12,
	--- à ESQUERDA do Selo (72px centrado): espelho exato do `openX` do
	--- Settings (+62). CALIBRAÇÃO: se encostar no painel do HUD em tela
	--- estreita, é este número.
	openX = 62,
	rowH = 64, -- ≥48dp; sobra respiro para título + descrição
	pad = 16,
	toastW = 320,
	toastH = 52,
	--- acima da faixa de legendas do A11y (rodapé, -14..-44) e do polegar
	toastBottom = 76,
	toastHold = 3.2,
	toastFade = 0.6,
}

-- ------------------------------------------------------------------ estado
local root: ScreenGui? = nil
local toastGui: ScreenGui? = nil
local backdrop: Frame
local listFrame: ScrollingFrame
local countLabel: TextLabel
local toastBox: Frame
local toastOver: TextLabel
local toastTitle: TextLabel
local toastLeft = 0
local pages: { string } = {}
local found: { [string]: boolean } = {}
local conns: { RBXScriptConnection } = {}

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

--- REGRA DA CAÇA (pura — é o que o autoteste prova): página trancada NUNCA
--- mostra o título nem a descrição reais. Só o enigma. Entregar a resposta
--- aqui mataria o colecionismo que o GDD §18.3 promete.
function Ui.rowText(id: string, lit: boolean): (string, string)
	if lit then
		return Strings.t("grimoire.page." .. id .. ".t"), Strings.t("grimoire.page." .. id .. ".d")
	end
	return Strings.t("grimoire.locked"), Strings.t("grimoire.locked.hint")
end

-- ------------------------------------------------------------------- livro
local function buildRow(parent: Instance, order: number, id: string, lit: boolean)
	local row = mk("Frame", {
		Name = id,
		BackgroundColor3 = NIGHT,
		BackgroundTransparency = if lit then 0.15 else 0.5,
		BorderSizePixel = 0,
		LayoutOrder = order,
		Size = UDim2.new(1, 0, 0, L.rowH),
	}, parent)
	mk("UIStroke", {
		Color = if lit then GOLD else GOLD_DIM,
		Thickness = if lit then 2 else 1,
		Transparency = if lit then 0.1 else 0.6,
	}, row)

	-- selo da página: losango dourado quando acesa, "?" apagado quando não —
	-- forma + cor, nunca só cor (GDD §10)
	local slot = mk("Frame", {
		BackgroundTransparency = 1,
		Position = UDim2.fromOffset(6, math.floor((L.rowH - 36) / 2)),
		Size = UDim2.fromOffset(36, 36),
	}, row)
	if lit then
		mk("Frame", {
			BackgroundColor3 = GOLD,
			BorderSizePixel = 0,
			AnchorPoint = Vector2.new(0.5, 0.5),
			Position = UDim2.fromScale(0.5, 0.5),
			Size = UDim2.fromScale(0.55, 0.55),
			Rotation = 45,
		}, slot)
	else
		mk("UITextSizeConstraint", { MaxTextSize = 20 }, mk("TextLabel", {
			BackgroundTransparency = 1,
			Size = UDim2.fromScale(1, 1),
			Font = Enum.Font.GothamBold,
			Text = "?",
			TextColor3 = TEXT_DIM,
			TextScaled = true,
			TextTransparency = 0.35,
		}, slot))
	end

	local title, desc = Ui.rowText(id, lit)
	mk("UITextSizeConstraint", { MaxTextSize = 16 }, mk("TextLabel", {
		BackgroundTransparency = 1,
		Position = UDim2.new(0, 50, 0, 8),
		Size = UDim2.new(1, -58, 0, 20),
		Font = Enum.Font.GothamBold,
		Text = title,
		TextColor3 = if lit then GOLD else TEXT_DIM,
		TextScaled = true,
		TextXAlignment = Enum.TextXAlignment.Left,
	}, row))
	mk("UITextSizeConstraint", { MaxTextSize = 13 }, mk("TextLabel", {
		BackgroundTransparency = 1,
		Position = UDim2.new(0, 50, 0, 30),
		Size = UDim2.new(1, -58, 0, 26),
		Font = Enum.Font.Gotham,
		Text = desc,
		TextColor3 = if lit then TEXT else TEXT_DIM,
		TextScaled = true,
		TextTransparency = if lit then 0 else 0.25,
		TextWrapped = true,
		TextXAlignment = Enum.TextXAlignment.Left,
		TextYAlignment = Enum.TextYAlignment.Top,
	}, row))
end

local function rebuild()
	if not root then
		return
	end
	for _, child in ipairs(listFrame:GetChildren()) do
		if child:IsA("Frame") then
			child:Destroy()
		end
	end
	local lit = 0
	for order, id in ipairs(pages) do
		local isLit = found[id] == true
		if isLit then
			lit += 1
		end
		buildRow(listFrame, order, id, isLit)
	end
	countLabel.Text = if #pages > 0 then Strings.t("grimoire.count", lit, #pages) else Strings.t("grimoire.empty")
end

-- ------------------------------------------------------------------- toast
local function showToast(id: string)
	toastOver.Text = Strings.t("grimoire.new")
	toastTitle.Text = Strings.t("grimoire.page." .. id .. ".t")
	toastBox.Visible = true
	toastBox.BackgroundTransparency = 0.2
	toastOver.TextTransparency = 0
	toastTitle.TextTransparency = 0
	toastLeft = L.toastHold
end

-- ------------------------------------------------------------------- build
local function buildToast(pg: Instance)
	local g = mk("ScreenGui", {
		Name = "ArkanaGrimoireToast",
		ResetOnSpawn = false,
		IgnoreGuiInset = false,
		DisplayOrder = 3, -- ABAIXO de tudo: o aviso nunca tapa o combate
		ZIndexBehavior = Enum.ZIndexBehavior.Sibling,
	}, pg)
	toastBox = mk("Frame", {
		Name = "Toast",
		AnchorPoint = Vector2.new(0.5, 1),
		BackgroundColor3 = NIGHT,
		BackgroundTransparency = 0.2,
		BorderSizePixel = 0,
		Position = UDim2.new(0.5, 0, 1, -L.toastBottom),
		Size = UDim2.fromOffset(L.toastW, L.toastH),
		Visible = false,
	}, g)
	mk("UIStroke", { Color = GOLD, Thickness = 1, Transparency = 0.25 }, toastBox)
	toastOver = mk("TextLabel", {
		BackgroundTransparency = 1,
		Position = UDim2.new(0, 8, 0, 5),
		Size = UDim2.new(1, -16, 0, 14),
		Font = Enum.Font.Gotham,
		Text = "",
		TextColor3 = TEXT_DIM,
		TextSize = 11,
	}, toastBox)
	toastTitle = mk("TextLabel", {
		BackgroundTransparency = 1,
		Position = UDim2.new(0, 8, 0, 20),
		Size = UDim2.new(1, -16, 0, 26),
		Font = Enum.Font.GothamBold,
		Text = "",
		TextColor3 = GOLD,
		TextScaled = true,
	}, toastBox)
	mk("UITextSizeConstraint", { MaxTextSize = 18 }, toastTitle)
	toastGui = g
	A11y.attach(g)
end

local function buildOpen(rootGui: ScreenGui)
	local btn = mk("TextButton", {
		Name = "OpenGrimoire",
		BackgroundColor3 = NIGHT,
		BackgroundTransparency = 0.2,
		BorderSizePixel = 0,
		AutoButtonColor = false,
		Size = UDim2.fromOffset(L.openSize, L.openSize),
		Text = "",
	}, rootGui)
	mk("UIStroke", { Color = GOLD_DIM, Thickness = 1, Transparency = 0.2 }, btn)
	-- ícone-livro em Frames: lombada + duas páginas — zero glifo, zero asset
	mk("Frame", {
		BackgroundColor3 = GOLD,
		BorderSizePixel = 0,
		AnchorPoint = Vector2.new(0.5, 0.5),
		Position = UDim2.fromScale(0.5, 0.5),
		Size = UDim2.fromScale(0.06, 0.55),
	}, btn)
	for _, side in ipairs({ -1, 1 }) do
		mk("Frame", {
			BackgroundColor3 = GOLD,
			BackgroundTransparency = 0.35,
			BorderSizePixel = 0,
			AnchorPoint = Vector2.new(0.5, 0.5),
			Position = UDim2.fromScale(0.5 + side * 0.17, 0.5),
			Size = UDim2.fromScale(0.24, 0.5),
			Rotation = side * 8,
		}, btn)
	end
	--- canônico = destro (à ESQUERDA do Selo; opções fica à direita). O espelho
	--- canhoto é do A11y — os dois botões trocam de lado juntos.
	A11y.place(btn, Vector2.new(0.5, 0), UDim2.new(0.5, -L.openX, 0, L.openTop))
	table.insert(conns, btn.Activated:Connect(function()
		backdrop.Visible = not backdrop.Visible
	end))
end

local function buildBook(rootGui: ScreenGui)
	backdrop = mk("Frame", {
		Name = "Backdrop",
		BackgroundColor3 = NIGHT_DEEP,
		BackgroundTransparency = 0.06,
		BorderSizePixel = 0,
		Size = UDim2.fromScale(1, 1),
		Visible = false,
	}, rootGui)

	local panel = mk("Frame", {
		Name = "Panel",
		AnchorPoint = Vector2.new(0.5, 0.5),
		BackgroundTransparency = 1,
		Position = UDim2.fromScale(0.5, 0.5),
		Size = UDim2.new(1, -2 * L.pad, 1, -2 * L.pad),
	}, backdrop)
	mk("UISizeConstraint", { MaxSize = Vector2.new(560, math.huge) }, panel)

	mk("UITextSizeConstraint", { MaxTextSize = 18 }, mk("TextLabel", {
		BackgroundTransparency = 1,
		Size = UDim2.new(1, -140, 0, 24),
		Font = Enum.Font.GothamBold,
		Text = Strings.t("grimoire.title"),
		TextColor3 = GOLD,
		TextScaled = true,
		TextXAlignment = Enum.TextXAlignment.Left,
	}, panel))

	countLabel = mk("TextLabel", {
		AnchorPoint = Vector2.new(0, 1),
		BackgroundTransparency = 1,
		Position = UDim2.new(0, 0, 1, 0),
		Size = UDim2.new(1, 0, 0, 16),
		Font = Enum.Font.Gotham,
		Text = Strings.t("grimoire.empty"),
		TextColor3 = TEXT_DIM,
		TextSize = 13,
		TextXAlignment = Enum.TextXAlignment.Left,
	}, panel)

	local close = mk("TextButton", {
		AnchorPoint = Vector2.new(1, 0),
		BackgroundColor3 = NIGHT,
		BackgroundTransparency = 0.2,
		BorderSizePixel = 0,
		AutoButtonColor = false,
		Position = UDim2.new(1, 0, 0, -2),
		Size = UDim2.fromOffset(120, 48), -- ≥48dp
		Font = Enum.Font.GothamBold,
		Text = Strings.t("grimoire.close"),
		TextColor3 = TEXT,
		TextSize = 14,
	}, panel)
	mk("UIStroke", { Color = GOLD_DIM, Thickness = 1 }, close)
	table.insert(conns, close.Activated:Connect(function()
		backdrop.Visible = false
	end))

	listFrame = mk("ScrollingFrame", {
		BackgroundTransparency = 1,
		BorderSizePixel = 0,
		Position = UDim2.new(0, 0, 0, 54),
		Size = UDim2.new(1, 0, 1, -78),
		CanvasSize = UDim2.new(),
		AutomaticCanvasSize = Enum.AutomaticCanvasSize.Y,
		ScrollBarThickness = 4,
		ScrollBarImageColor3 = GOLD_DIM,
	}, panel)
	mk("UIListLayout", {
		Padding = UDim.new(0, 8),
		SortOrder = Enum.SortOrder.LayoutOrder,
	}, listFrame)
end

-- --------------------------------------------------------------------- API
function Ui.init(player: Player)
	Strings.followPlayerLocale()
	local pg = player:WaitForChild("PlayerGui")

	local g = mk("ScreenGui", {
		Name = "ArkanaGrimoire",
		ResetOnSpawn = false,
		IgnoreGuiInset = false,
		DisplayOrder = 19, -- modal: acima do jogo; ABAIXO do menu de opções (20)
		ZIndexBehavior = Enum.ZIndexBehavior.Sibling,
	}, pg)
	root = g
	buildBook(g)
	buildOpen(g)
	buildToast(pg)
	A11y.attach(g)

	--- A fiação do remote fica FORA do init: `Net.event` no cliente espera o
	--- servidor criar o remote (até 30s). Bloquear aqui atrasaria o init de
	--- todos os módulos que vêm depois no Main — o boot do cliente é serial.
	task.spawn(function()
		local ok, ev = pcall(Net.event, "Grimoire")
		if not ok or not ev then
			return -- servidor sem Grimório: o livro fica vazio, o jogo segue
		end
		table.insert(conns, ev.OnClientEvent:Connect(function(data: any)
			if typeof(data) ~= "table" then
				return
			end
			if typeof(data.pages) == "table" then
				pages = data.pages
			end
			if typeof(data.found) == "table" then
				table.clear(found)
				for _, id in ipairs(data.found) do
					if typeof(id) == "string" then
						found[id] = true
					end
				end
			end
			rebuild()
			if typeof(data.latest) == "string" then
				showToast(data.latest)
			end
		end))
		--- pede o estado: o push do PlayerAdded pode ter saído antes de o
		--- cliente carregar. O servidor responde com a lista completa.
		ev:FireServer()
	end)
end

function Ui.update(dt: number)
	if toastLeft <= 0 then
		return
	end
	toastLeft -= dt
	if toastLeft <= 0 then
		toastBox.Visible = false
	elseif toastLeft < L.toastFade then
		local k = 1 - toastLeft / L.toastFade
		toastBox.BackgroundTransparency = 0.2 + 0.8 * k
		toastOver.TextTransparency = k
		toastTitle.TextTransparency = k
	end
end

function Ui.destroy()
	for _, c in ipairs(conns) do
		c:Disconnect()
	end
	table.clear(conns)
	if root then
		root:Destroy()
		root = nil
	end
	if toastGui then
		toastGui:Destroy()
		toastGui = nil
	end
	toastLeft = 0
end

-- ============================================================================
-- AUTOTESTE — roda no runner headless e na Command Bar do Studio (cliente):
--   require(game.Players.LocalPlayer.PlayerScripts.Client.GrimoireUi).selfTest()
-- Cobre a única regra desta tela que não dá para ver a olho no Studio: a
-- página trancada NÃO pode vazar o título nem a descrição — nos dois idiomas.
-- ============================================================================
function Ui.selfTest()
	local before = Strings.getLang()
	for _, lang in ipairs({ "pt", "en" }) do
		Strings.setLang(lang :: any)
		local litT, litD = Ui.rowText("frozen_bridge", true)
		local lockT, lockD = Ui.rowText("frozen_bridge", false)
		assert(litT ~= "grimoire.page.frozen_bridge.t", "página acesa sem título [" .. lang .. "]")
		assert(lockT == Strings.t("grimoire.locked") and lockT ~= litT, "página trancada vazou o título [" .. lang .. "]")
		assert(lockD == Strings.t("grimoire.locked.hint") and lockD ~= litD, "página trancada vazou a descrição [" .. lang .. "]")
	end
	Strings.setLang(before)
	assert(L.openSize >= 48 and L.rowH >= 48, "alvo de toque abaixo dos 48dp do GDD §19.3")
	print("[GrimoireUi] autoteste OK — o enigma não vaza")
	return true
end

return Ui
