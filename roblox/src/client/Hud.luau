--!strict
-- ============================================================================
-- RAIA D (HUD/UI) — HUD INFORMATIVO: vida, Escudo Evolutivo (nível + progresso
-- para o próximo), mana, Selo de Arkana e área de anúncios.
--
-- IDENTIDADE (GDD §10): azul-noite #0B1026 + dourado #F0C75E, painéis chapados
-- e cantos vivos (estética blocky — nada de canto arredondado).
-- ACESSIBILIDADE (GDD §10, inegociável): cor + FORMA. O nível do escudo aparece
-- em NÚMERO e em PIPS (4 quadrados) além da cor — daltônico lê igual.
-- ZONA DE POLEGAR: informação no ALTO da tela. A metade de baixo e a direita
-- pertencem aos botões da raia C (ScreenGui "ArkanaControls").
-- O cliente NÃO decide nada: só apresenta Net.StatsUpdate / Net.Announce.
-- ============================================================================

local ReplicatedStorage = game:GetService("ReplicatedStorage")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Balance = require(Shared:WaitForChild("Balance"))
local Net = require(Shared:WaitForChild("Net"))
local Strings = require(Shared:WaitForChild("Strings"))

-- ------------------------------------------------------------------ paleta
local NIGHT = Color3.fromHex("#0B1026")
local NIGHT_DEEP = Color3.fromHex("#05070F")
local GOLD = Color3.fromHex("#F0C75E")
local GOLD_DIM = Color3.fromHex("#8A7336")
local TEXT = Color3.fromHex("#E8E6F0")
local TEXT_DIM = Color3.fromHex("#9A97AD")
local HP_GREEN = Color3.fromHex("#35C94A")
local DANGER = Color3.fromHex("#E04040")
local MANA_BLUE = Color3.fromHex("#2AA7FF")

--- cores do Escudo Evolutivo por nível (GDD §5: branco → azul → roxo → dourado)
local SHIELD_COLORS = {
	Color3.fromHex("#FFFFFF"),
	Color3.fromHex("#3AA0FF"),
	Color3.fromHex("#B060FF"),
	GOLD,
}

--- gemas do Selo na ordem canônica dos elementos (GDD §10: 5 gemas nas pontas)
local SEAL_GEMS = {
	Color3.fromHex("#FF5A2A"), -- Fogo
	Color3.fromHex("#2AA7FF"), -- Água
	Color3.fromHex("#A8763E"), -- Terra
	Color3.fromHex("#8FE8C9"), -- Vento
	Color3.fromHex("#F5D90A"), -- Raio
}

-- ------------------------------------------------------------------ layout
local L = {
	panelPos = UDim2.new(0, 12, 0, 12),
	panelSize = UDim2.new(0.32, 0, 0, 110),
	panelMin = Vector2.new(232, 110),
	panelMax = Vector2.new(380, 110),
	pad = 8,
	labelH = 12,
	hpBarY = 14,
	hpBarH = 14,
	shieldLabelY = 34,
	shieldBarY = 48,
	shieldBarH = 12,
	evoBarY = 62,
	evoBarH = 4,
	manaLabelY = 70,
	manaBarY = 84,
	manaBarH = 10,
	sealSize = 72,
	sealTop = 10,
	--- abaixo do painel e do selo: nunca cobre a leitura de vida/escudo/mana
	announceTop = 132,
	--- suavização das barras (fração por segundo) — evita "pulo" a cada pacote
	barLerp = 12,
	announceHold = 2.6,
	announceFade = 0.5,
	sealSpinDeg = 6, -- graus/s
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

local function label(parent: Instance, y: number, xScale: number, wScale: number, align: Enum.TextXAlignment, color: Color3): TextLabel
	local t = mk("TextLabel", {
		BackgroundTransparency = 1,
		Position = UDim2.new(xScale, 0, 0, y),
		Size = UDim2.new(wScale, 0, 0, L.labelH),
		Font = Enum.Font.GothamBold,
		TextColor3 = color,
		TextScaled = true,
		TextXAlignment = align,
		Text = "",
	}, parent)
	mk("UITextSizeConstraint", { MaxTextSize = 13 }, t)
	return t
end

--- barra chapada: fundo escuro + preenchimento + borda dourada fina. Devolve o
--- Frame de preenchimento (só a largura muda por frame).
local function bar(parent: Instance, y: number, h: number, color: Color3, stroke: boolean): Frame
	local bg = mk("Frame", {
		BackgroundColor3 = NIGHT_DEEP,
		BackgroundTransparency = 0.15,
		BorderSizePixel = 0,
		Position = UDim2.new(0, 0, 0, y),
		Size = UDim2.new(1, 0, 0, h),
	}, parent)
	if stroke then
		mk("UIStroke", { Color = GOLD_DIM, Thickness = 1, Transparency = 0.35 }, bg)
	end
	return mk("Frame", {
		BackgroundColor3 = color,
		BorderSizePixel = 0,
		Size = UDim2.fromScale(1, 1),
	}, bg)
end

local function clamp01(v: number): number
	return math.clamp(v, 0, 1)
end

-- ============================================================================
local Hud = {}

local gui: ScreenGui? = nil
local conns: { RBXScriptConnection } = {}

-- widgets
local hpFill: Frame, shieldFill: Frame, evoFill: Frame, manaFill: Frame
local hpLabel: TextLabel, hpValue: TextLabel
local shieldLabel: TextLabel
local manaLabel: TextLabel, manaValue: TextLabel
local pips: { Frame } = {}
local seal: Frame
local announce: TextLabel

--- último pacote do servidor (fonte da verdade) e o que está desenhado (suave)
local stats = {
	hp = Balance.player.hp,
	hpMax = Balance.player.hp,
	shield = 0,
	shieldMax = Balance.shield.caps[1],
	shieldLv = 1,
	evo = 0,
	mana = Balance.player.manaMax,
	manaMax = Balance.player.manaMax,
}
local shown = { hp = 1, shield = 0, evo = 0, mana = 1 }
local announceLeft = 0

--- progresso 0..1 rumo ao próximo nível do escudo (GDD §5, thresholds do Balance)
local function evoProgress(): number
	local th = Balance.shield.thresholds
	local lv = math.clamp(stats.shieldLv, 1, #th)
	if lv >= #th then
		return 1
	end
	local from, to = th[lv], th[lv + 1]
	if to <= from then
		return 1
	end
	return clamp01((stats.evo - from) / (to - from))
end

local function shieldColor(): Color3
	return SHIELD_COLORS[math.clamp(stats.shieldLv, 1, #SHIELD_COLORS)]
end

local function refreshShieldLevel()
	local color = shieldColor()
	shieldFill.BackgroundColor3 = color
	-- FORMA além da cor: pips acesos = nível (legível sem enxergar a cor)
	for i, pip in ipairs(pips) do
		local on = i <= stats.shieldLv
		pip.BackgroundColor3 = on and color or NIGHT_DEEP
		pip.BackgroundTransparency = on and 0 or 0.3
	end
	-- nível máximo não tem "próximo": a barrinha de evolução some
	local evoBg = evoFill.Parent :: any
	evoBg.Visible = stats.shieldLv < #Balance.shield.thresholds
end

local function showAnnounce(text: string)
	announce.Text = text
	announce.TextTransparency = 0
	announce.TextStrokeTransparency = 0.2
	announceLeft = L.announceHold
end

-- ------------------------------------------------------------------ build
local function buildPanel(root: ScreenGui)
	local panel = mk("Frame", {
		Name = "Stats",
		BackgroundColor3 = NIGHT,
		BackgroundTransparency = 0.4,
		BorderSizePixel = 0,
		Position = L.panelPos,
		Size = L.panelSize,
	}, root)
	mk("UIStroke", { Color = GOLD_DIM, Thickness = 1, Transparency = 0.2 }, panel)
	mk("UISizeConstraint", { MinSize = L.panelMin, MaxSize = L.panelMax }, panel)

	local content = mk("Frame", { BackgroundTransparency = 1, Size = UDim2.fromScale(1, 1) }, panel)
	mk("UIPadding", {
		PaddingLeft = UDim.new(0, L.pad),
		PaddingRight = UDim.new(0, L.pad),
		PaddingTop = UDim.new(0, L.pad),
		PaddingBottom = UDim.new(0, L.pad),
	}, content)

	hpLabel = label(content, 0, 0, 0.5, Enum.TextXAlignment.Left, TEXT_DIM)
	hpValue = label(content, 0, 0.5, 0.5, Enum.TextXAlignment.Right, TEXT)
	hpFill = bar(content, L.hpBarY, L.hpBarH, HP_GREEN, true)

	-- rótulo + nível + números num texto só: a direita desta linha é dos pips
	shieldLabel = label(content, L.shieldLabelY, 0, 0.72, Enum.TextXAlignment.Left, TEXT_DIM)
	shieldFill = bar(content, L.shieldBarY, L.shieldBarH, SHIELD_COLORS[1], true)
	evoFill = bar(content, L.evoBarY, L.evoBarH, GOLD, false)

	-- pips do nível (a FORMA da regra de acessibilidade) — sobre a barra do escudo
	local pipRow = mk("Frame", {
		BackgroundTransparency = 1,
		AnchorPoint = Vector2.new(1, 0),
		Position = UDim2.new(1, 0, 0, L.shieldLabelY + 1),
		Size = UDim2.new(0, 46, 0, 10),
	}, content)
	mk("UIListLayout", {
		FillDirection = Enum.FillDirection.Horizontal,
		HorizontalAlignment = Enum.HorizontalAlignment.Right,
		Padding = UDim.new(0, 2),
	}, pipRow)
	for i = 1, #Balance.shield.caps do
		pips[i] = mk("Frame", {
			BackgroundColor3 = NIGHT_DEEP,
			BorderSizePixel = 0,
			LayoutOrder = i,
			Size = UDim2.new(0, 10, 1, 0),
		}, pipRow)
		mk("UIStroke", { Color = GOLD_DIM, Thickness = 1 }, pips[i])
	end

	manaLabel = label(content, L.manaLabelY, 0, 0.5, Enum.TextXAlignment.Left, TEXT_DIM)
	manaValue = label(content, L.manaLabelY, 0.5, 0.5, Enum.TextXAlignment.Right, TEXT)
	manaFill = bar(content, L.manaBarY, L.manaBarH, MANA_BLUE, true)
end

--- Selo de Arkana: pentágono rúnico com 5 gemas elementais (GDD §10). Hoje é
--- decorativo; no R2 as gemas acendem quando um combo está disponível.
local function buildSeal(root: ScreenGui)
	seal = mk("Frame", {
		Name = "Seal",
		BackgroundTransparency = 1,
		AnchorPoint = Vector2.new(0.5, 0),
		Position = UDim2.new(0.5, 0, 0, L.sealTop),
		Size = UDim2.fromOffset(L.sealSize, L.sealSize),
	}, root)

	local r = L.sealSize * 0.38
	local edge = 2 * r * math.sin(math.rad(36))
	for i = 1, 5 do
		local a = math.rad(-90 + (i - 1) * 72)
		local b = math.rad(-90 + i * 72)
		local x, y = math.cos(a) * r, math.sin(a) * r
		local bx, by = math.cos(b) * r, math.sin(b) * r

		-- aresta do pentágono
		mk("Frame", {
			BackgroundColor3 = GOLD_DIM,
			BackgroundTransparency = 0.3,
			BorderSizePixel = 0,
			AnchorPoint = Vector2.new(0.5, 0.5),
			Position = UDim2.new(0.5, (x + bx) / 2, 0.5, (y + by) / 2),
			Size = UDim2.fromOffset(math.floor(edge + 0.5), 2),
			Rotation = math.deg(math.atan2(by - y, bx - x)),
		}, seal)

		-- gema (quadrado girado 45° = losango: cor + FORMA)
		mk("Frame", {
			Name = "Gem" .. i,
			BackgroundColor3 = SEAL_GEMS[i],
			BackgroundTransparency = 0.45, -- apaga: R2 acende no combo disponível
			BorderSizePixel = 0,
			AnchorPoint = Vector2.new(0.5, 0.5),
			Position = UDim2.new(0.5, x, 0.5, y),
			Size = UDim2.fromOffset(10, 10),
			Rotation = 45,
		}, seal)
	end

	mk("TextLabel", {
		BackgroundTransparency = 1,
		AnchorPoint = Vector2.new(0.5, 0.5),
		Position = UDim2.fromScale(0.5, 0.5),
		Size = UDim2.fromOffset(26, 26),
		Font = Enum.Font.Fantasy,
		Text = "A",
		TextColor3 = GOLD,
		TextScaled = true,
		TextTransparency = 0.15,
	}, seal)
end

-- ------------------------------------------------------------------ API
function Hud.init(_player: Player)
	if gui then
		return
	end
	Strings.followPlayerLocale()

	local playerGui = _player:WaitForChild("PlayerGui")
	local root = mk("ScreenGui", {
		Name = "ArkanaHud", -- a raia C usa "ArkanaControls" — não colidir
		ResetOnSpawn = false, -- sobrevive ao respawn: nada de HUD vazando/recriando
		IgnoreGuiInset = false, -- respeita a barra do Roblox: o HUD começa abaixo dela
		DisplayOrder = 4, -- abaixo do "ArkanaControls" (raia C usa 5)
		ZIndexBehavior = Enum.ZIndexBehavior.Sibling,
	}, playerGui)
	gui = root

	buildPanel(root)
	buildSeal(root)
	announce = mk("TextLabel", {
		Name = "Announce",
		BackgroundTransparency = 1,
		AnchorPoint = Vector2.new(0.5, 0),
		Position = UDim2.new(0.5, 0, 0, L.announceTop),
		Size = UDim2.new(0.6, 0, 0, 26),
		Font = Enum.Font.GothamBold,
		Text = "",
		TextColor3 = GOLD,
		TextScaled = true,
		TextTransparency = 1,
		TextStrokeColor3 = NIGHT_DEEP,
		TextStrokeTransparency = 1,
	}, root)
	mk("UITextSizeConstraint", { MaxTextSize = 22 }, announce)

	hpLabel.Text = Strings.t("hud.hp")
	manaLabel.Text = Strings.t("hud.mana")
	refreshShieldLevel()

	-- ------------------------------------------------ o servidor manda, o HUD mostra
	table.insert(
		conns,
		Net.event(Net.Names.StatsUpdate).OnClientEvent:Connect(function(p: any)
			if typeof(p) ~= "table" then
				return
			end
			local lastLv = stats.shieldLv
			stats.hp = tonumber(p.hp) or stats.hp
			stats.hpMax = tonumber(p.hpMax) or stats.hpMax
			stats.shield = tonumber(p.shield) or stats.shield
			stats.shieldLv = math.clamp(tonumber(p.shieldLv) or stats.shieldLv, 1, #Balance.shield.caps)
			stats.shieldMax = tonumber(p.shieldMax) or Balance.shield.caps[stats.shieldLv]
			stats.evo = tonumber(p.evo) or stats.evo
			stats.mana = tonumber(p.mana) or stats.mana
			stats.manaMax = tonumber(p.manaMax) or stats.manaMax
			-- p.cooldowns é da raia C (desenha nos próprios botões) — o HUD ignora
			if stats.shieldLv ~= lastLv then
				refreshShieldLevel()
				if stats.shieldLv > lastLv then
					showAnnounce(Strings.t("hud.evolved", stats.shieldLv))
				end
			end
		end)
	)

	table.insert(
		conns,
		-- aceita as três formas que o servidor pode mandar: "chave",
		-- { key = "chave" } com args na tabela, ou a chave seguida dos valores.
		Net.event(Net.Names.Announce).OnClientEvent:Connect(function(p: any, ...)
			local isTable = typeof(p) == "table"
			local key = isTable and p.key or p
			if typeof(key) ~= "string" then
				return
			end
			local args = (isTable and p.args) or { ... }
			showAnnounce(Strings.t(key, table.unpack(args)))
		end)
	)
end

function Hud.update(dt: number)
	if not gui then
		return
	end
	local k = math.min(1, dt * L.barLerp)

	local hpTarget = stats.hpMax > 0 and clamp01(stats.hp / stats.hpMax) or 0
	local shTarget = stats.shieldMax > 0 and clamp01(stats.shield / stats.shieldMax) or 0
	local mnTarget = stats.manaMax > 0 and clamp01(stats.mana / stats.manaMax) or 0
	local evTarget = evoProgress()

	shown.hp += (hpTarget - shown.hp) * k
	shown.shield += (shTarget - shown.shield) * k
	shown.mana += (mnTarget - shown.mana) * k
	shown.evo += (evTarget - shown.evo) * k

	hpFill.Size = UDim2.fromScale(shown.hp, 1)
	hpFill.BackgroundColor3 = DANGER:Lerp(HP_GREEN, shown.hp)
	shieldFill.Size = UDim2.fromScale(shown.shield, 1)
	manaFill.Size = UDim2.fromScale(shown.mana, 1)
	evoFill.Size = UDim2.fromScale(shown.evo, 1)

	hpValue.Text = string.format("%d/%d", math.max(0, math.ceil(stats.hp)), math.ceil(stats.hpMax))
	shieldLabel.Text = string.format(
		"%s %s  %d/%d",
		Strings.t("hud.shield"),
		Strings.t("hud.shield.level", stats.shieldLv),
		math.max(0, math.ceil(stats.shield)),
		math.ceil(stats.shieldMax)
	)
	manaValue.Text = string.format("%d", math.max(0, math.floor(stats.mana)))

	seal.Rotation = (seal.Rotation + dt * L.sealSpinDeg) % 360

	if announceLeft > 0 then
		announceLeft -= dt
		local a = clamp01(1 - announceLeft / L.announceFade)
		announce.TextTransparency = a
		announce.TextStrokeTransparency = 0.2 + a * 0.8
	end
end

--- Some com tudo (parts, conexões): nada vaza entre respawns/trocas de lugar.
function Hud.destroy()
	for _, c in ipairs(conns) do
		c:Disconnect()
	end
	table.clear(conns)
	table.clear(pips)
	if gui then
		gui:Destroy()
		gui = nil
	end
end

return Hud
