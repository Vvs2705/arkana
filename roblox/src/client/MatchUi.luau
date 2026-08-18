--!strict
-- ============================================================================
-- RAIA (R3) — UI DA PARTIDA: contador de vivos, timer da zona, aviso de "fora
-- da zona" e a tela de fim com o SELO DO CAMPEÃO (GDD §18.6).
--
-- O cliente NÃO decide nada (docs/ROBLOX.md §3): tudo desenhado aqui vem de
-- Net.MatchState. O único cálculo local é "estou fora do círculo?", que serve
-- só para PISCAR o aviso — quem aplica o dano da zona é o servidor.
--
-- IDENTIDADE (GDD §10): azul-noite #0B1026 + dourado #F0C75E, painéis chapados,
-- cantos vivos. ZONA DE POLEGAR: a barra fica no ALTO À DIREITA — o painel de
-- vida/escudo/mana (ArkanaHud) mora no alto à esquerda e os botões de toque
-- (ArkanaControls) na metade de baixo.
-- ============================================================================

local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Elements = require(Shared:WaitForChild("Elements"))
local Net = require(Shared:WaitForChild("Net"))
local Strings = require(Shared:WaitForChild("Strings"))

-- ------------------------------------------------------------------ paleta
local NIGHT = Color3.fromHex("#0B1026")
local NIGHT_DEEP = Color3.fromHex("#05070F")
local GOLD = Color3.fromHex("#F0C75E")
local GOLD_DIM = Color3.fromHex("#8A7336")
local TEXT = Color3.fromHex("#E8E6F0")
local TEXT_DIM = Color3.fromHex("#9A97AD")
local DANGER = Color3.fromHex("#E04040")
local ZONE_VIOLET = Color3.fromHex("#8A5CF0")

-- ponytail: rótulos locais porque `Shared/Strings` é da raia D e esta raia não
-- pode editá-lo. Quando a raia D adicionar as chaves `match.*`, isto vira
-- Strings.t() e some. Segue o mesmo idioma que o resto da UI já escolheu.
local L10N = {
	pt = {
		alive = "VIVOS",
		waiting = "AGUARDANDO MAGOS",
		starting = "COMEÇA EM",
		hold = "A ZONA FECHA EM",
		shrink = "ZONA FECHANDO",
		phase = "FASE %d/%d",
		outside = "FORA DA ZONA",
		outsideSub = "corra para dentro do círculo",
		sealTitle = "SELO DO CAMPEÃO",
		champion = "CAMPEÃO",
		noChampion = "NINGUÉM SOBROU",
		kills = "ABATES",
		damage = "DANO",
		combos = "SINTONIAS",
		duration = "DURAÇÃO",
		elements = "ELEMENTOS",
		spectating = "ESPECTADOR",
		none = "—",
	},
	en = {
		alive = "ALIVE",
		waiting = "WAITING FOR MAGES",
		starting = "STARTS IN",
		hold = "ZONE CLOSES IN",
		shrink = "ZONE CLOSING",
		phase = "PHASE %d/%d",
		outside = "OUTSIDE THE ZONE",
		outsideSub = "run into the circle",
		sealTitle = "CHAMPION'S SEAL",
		champion = "CHAMPION",
		noChampion = "NO ONE LEFT",
		kills = "KILLS",
		damage = "DAMAGE",
		combos = "ATTUNEMENTS",
		duration = "TIME",
		elements = "ELEMENTS",
		spectating = "SPECTATING",
		none = "—",
	},
}

local L = {
	barTop = 12,
	barRight = 12,
	barW = 190,
	barH = 62,
	warnTop = 0.30,
	--- pulsos por segundo do aviso de fora da zona
	warnHz = 2.2,
	overlayFade = 4, -- fração por segundo
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

local function clock(t: number): string
	local s = math.max(0, math.floor(t + 0.5))
	return string.format("%d:%02d", math.floor(s / 60), s % 60)
end

-- ============================================================================
local MatchUi = {}

local gui: ScreenGui? = nil
local conns: { RBXScriptConnection } = {}
local tr: { [string]: string } = L10N.pt
local localPlayer: Player? = nil

-- widgets
local bar: Frame
local aliveLabel: TextLabel
local zoneLabel: TextLabel
local phaseLabel: TextLabel
local warn: Frame
local warnText: TextLabel
local warnSub: TextLabel
local overlay: Frame
local sealTitle: TextLabel
local sealName: TextLabel
local sealRows: TextLabel
local sealElements: Frame

--- último pacote do servidor (fonte da verdade)
local st = {
	state = "Lobby",
	phase = 0,
	phases = 1,
	shrinking = false,
	center = Vector3.zero,
	radius = math.huge,
	alive = 0,
	total = 1,
	timeLeft = 0,
	seal = nil :: any,
}
local outside = false
local pulse = 0
local overlayShown = 0
local wasEnded = false

-- ------------------------------------------------------------------ build
local function buildBar(root: ScreenGui)
	bar = mk("Frame", {
		Name = "MatchBar",
		AnchorPoint = Vector2.new(1, 0),
		BackgroundColor3 = NIGHT,
		BackgroundTransparency = 0.4,
		BorderSizePixel = 0,
		Position = UDim2.new(1, -L.barRight, 0, L.barTop),
		Size = UDim2.fromOffset(L.barW, L.barH),
	}, root)
	mk("UIStroke", { Color = GOLD_DIM, Thickness = 1, Transparency = 0.2 }, bar)

	local content = mk("Frame", { BackgroundTransparency = 1, Size = UDim2.fromScale(1, 1) }, bar)
	mk("UIPadding", {
		PaddingLeft = UDim.new(0, 8),
		PaddingRight = UDim.new(0, 8),
		PaddingTop = UDim.new(0, 6),
		PaddingBottom = UDim.new(0, 6),
	}, content)

	aliveLabel = mk("TextLabel", {
		BackgroundTransparency = 1,
		Position = UDim2.fromScale(0, 0),
		Size = UDim2.new(1, 0, 0, 22),
		Font = Enum.Font.GothamBold,
		Text = "",
		TextColor3 = GOLD,
		TextScaled = true,
		TextXAlignment = Enum.TextXAlignment.Right,
	}, content)
	mk("UITextSizeConstraint", { MaxTextSize = 22 }, aliveLabel)

	zoneLabel = mk("TextLabel", {
		BackgroundTransparency = 1,
		Position = UDim2.new(0, 0, 0, 24),
		Size = UDim2.new(1, 0, 0, 14),
		Font = Enum.Font.GothamBold,
		Text = "",
		TextColor3 = TEXT,
		TextScaled = true,
		TextXAlignment = Enum.TextXAlignment.Right,
	}, content)
	mk("UITextSizeConstraint", { MaxTextSize = 14 }, zoneLabel)

	phaseLabel = mk("TextLabel", {
		BackgroundTransparency = 1,
		Position = UDim2.new(0, 0, 0, 38),
		Size = UDim2.new(1, 0, 0, 12),
		Font = Enum.Font.Gotham,
		Text = "",
		TextColor3 = TEXT_DIM,
		TextScaled = true,
		TextXAlignment = Enum.TextXAlignment.Right,
	}, content)
	mk("UITextSizeConstraint", { MaxTextSize = 12 }, phaseLabel)
end

local function buildWarning(root: ScreenGui)
	warn = mk("Frame", {
		Name = "OutOfZone",
		AnchorPoint = Vector2.new(0.5, 0),
		BackgroundTransparency = 1,
		Position = UDim2.new(0.5, 0, L.warnTop, 0),
		Size = UDim2.new(0.7, 0, 0, 54),
		Visible = false,
	}, root)

	warnText = mk("TextLabel", {
		BackgroundTransparency = 1,
		Size = UDim2.new(1, 0, 0, 34),
		Font = Enum.Font.GothamBlack,
		Text = "",
		TextColor3 = DANGER,
		TextScaled = true,
		TextStrokeColor3 = NIGHT_DEEP,
		TextStrokeTransparency = 0.1,
	}, warn)
	mk("UITextSizeConstraint", { MaxTextSize = 34 }, warnText)

	warnSub = mk("TextLabel", {
		BackgroundTransparency = 1,
		Position = UDim2.new(0, 0, 0, 34),
		Size = UDim2.new(1, 0, 0, 18),
		Font = Enum.Font.GothamBold,
		Text = "",
		TextColor3 = TEXT,
		TextScaled = true,
		TextStrokeColor3 = NIGHT_DEEP,
		TextStrokeTransparency = 0.3,
	}, warn)
	mk("UITextSizeConstraint", { MaxTextSize = 16 }, warnSub)
end

--- Tela de fim: o card do GDD §18.6. Chapado, dourado, pronto para print.
local function buildOverlay(root: ScreenGui)
	overlay = mk("Frame", {
		Name = "EndScreen",
		BackgroundColor3 = NIGHT_DEEP,
		BackgroundTransparency = 1,
		BorderSizePixel = 0,
		Size = UDim2.fromScale(1, 1),
		Visible = false,
		ZIndex = 10,
	}, root)

	local card = mk("Frame", {
		Name = "Seal",
		AnchorPoint = Vector2.new(0.5, 0.5),
		BackgroundColor3 = NIGHT,
		BackgroundTransparency = 0.05,
		BorderSizePixel = 0,
		Position = UDim2.fromScale(0.5, 0.5),
		Size = UDim2.new(0.62, 0, 0.56, 0),
		ZIndex = 11,
	}, overlay)
	mk("UISizeConstraint", { MinSize = Vector2.new(280, 210), MaxSize = Vector2.new(520, 360) }, card)
	mk("UIStroke", { Color = GOLD, Thickness = 2, Transparency = 0.1 }, card)
	mk("UIPadding", {
		PaddingLeft = UDim.new(0, 16),
		PaddingRight = UDim.new(0, 16),
		PaddingTop = UDim.new(0, 14),
		PaddingBottom = UDim.new(0, 14),
	}, card)

	sealTitle = mk("TextLabel", {
		BackgroundTransparency = 1,
		Size = UDim2.new(1, 0, 0, 16),
		Font = Enum.Font.GothamBold,
		Text = "",
		TextColor3 = GOLD_DIM,
		TextScaled = true,
		ZIndex = 12,
	}, card)
	mk("UITextSizeConstraint", { MaxTextSize = 16 }, sealTitle)

	sealName = mk("TextLabel", {
		BackgroundTransparency = 1,
		Position = UDim2.new(0, 0, 0, 22),
		Size = UDim2.new(1, 0, 0, 44),
		Font = Enum.Font.GothamBlack,
		Text = "",
		TextColor3 = GOLD,
		TextScaled = true,
		ZIndex = 12,
	}, card)
	mk("UITextSizeConstraint", { MaxTextSize = 40 }, sealName)

	sealRows = mk("TextLabel", {
		BackgroundTransparency = 1,
		Position = UDim2.new(0, 0, 0, 74),
		Size = UDim2.new(1, 0, 0, 56),
		Font = Enum.Font.GothamBold,
		Text = "",
		TextColor3 = TEXT,
		TextScaled = true,
		TextWrapped = true,
		ZIndex = 12,
	}, card)
	mk("UITextSizeConstraint", { MaxTextSize = 18 }, sealRows)

	sealElements = mk("Frame", {
		Name = "Elements",
		AnchorPoint = Vector2.new(0.5, 1),
		BackgroundTransparency = 1,
		Position = UDim2.new(0.5, 0, 1, 0),
		Size = UDim2.new(1, 0, 0, 26),
		ZIndex = 12,
	}, card)
	mk("UIListLayout", {
		FillDirection = Enum.FillDirection.Horizontal,
		HorizontalAlignment = Enum.HorizontalAlignment.Center,
		VerticalAlignment = Enum.VerticalAlignment.Center,
		Padding = UDim.new(0, 6),
	}, sealElements)
end

-- ------------------------------------------------------------------ desenho
--- Chips de elemento: COR + NOME (GDD §10 — nunca só cor).
local function fillElements(list: { any })
	for _, child in ipairs(sealElements:GetChildren()) do
		if child:IsA("Frame") then
			child:Destroy()
		end
	end
	for _, id in ipairs(list) do
		local e = Elements.ById[id]
		if not e then
			continue
		end
		local label = if Strings.getLang() == "pt" then e.pt else e.en
		local chip = mk("Frame", {
			BackgroundColor3 = e.color,
			BackgroundTransparency = 0.15,
			BorderSizePixel = 0,
			Size = UDim2.fromOffset(74, 20),
			ZIndex = 12,
		}, sealElements)
		mk("UIStroke", { Color = NIGHT_DEEP, Thickness = 1 }, chip)
		mk("TextLabel", {
			BackgroundTransparency = 1,
			Size = UDim2.fromScale(1, 1),
			Font = Enum.Font.GothamBold,
			Text = string.upper(label),
			TextColor3 = NIGHT_DEEP,
			TextScaled = true,
			ZIndex = 13,
		}, chip)
	end
end

local function showSeal()
	local seal = st.seal
	overlay.Visible = true
	sealTitle.Text = tr.sealTitle
	if not seal then
		sealName.Text = tr.noChampion
		sealRows.Text = ""
		fillElements({})
		return
	end
	sealName.Text = string.upper(tostring(seal.champion))
	sealRows.Text = string.format(
		"%s %d    %s %d    %s %d\n%s %s    %s",
		tr.kills,
		tonumber(seal.kills) or 0,
		tr.damage,
		tonumber(seal.damage) or 0,
		tr.combos,
		#(seal.combos or {}),
		tr.duration,
		clock(tonumber(seal.duration) or 0),
		string.format("1/%d", tonumber(seal.total) or 1)
	)
	fillElements(seal.elements or {})
end

local function hideSeal()
	overlay.Visible = false
	overlay.BackgroundTransparency = 1
	overlayShown = 0
end

-- ------------------------------------------------------------------ API
function MatchUi.init(player: Player)
	if gui then
		return
	end
	localPlayer = player
	Strings.followPlayerLocale()
	tr = L10N[Strings.getLang()] or L10N.pt

	local playerGui = player:WaitForChild("PlayerGui")
	local root = mk("ScreenGui", {
		Name = "ArkanaMatch", -- raia D usa "ArkanaHud", raia C "ArkanaControls"
		ResetOnSpawn = false, -- sobrevive à morte: a tela de fim não pode sumir
		IgnoreGuiInset = false,
		DisplayOrder = 6, -- acima do HUD e dos botões: o fim de partida cobre tudo
		ZIndexBehavior = Enum.ZIndexBehavior.Sibling,
	}, playerGui)
	gui = root

	buildBar(root)
	buildWarning(root)
	buildOverlay(root)
	warnText.Text = tr.outside
	warnSub.Text = tr.outsideSub

	table.insert(
		conns,
		Net.event(Net.Names.MatchState).OnClientEvent:Connect(function(p: any)
			if typeof(p) ~= "table" then
				return
			end
			st.state = tostring(p.state or st.state)
			st.phase = tonumber(p.phase) or st.phase
			st.phases = tonumber(p.phases) or st.phases
			st.shrinking = p.shrinking == true
			st.alive = tonumber(p.alive) or 0
			st.total = math.max(1, tonumber(p.total) or 1)
			st.timeLeft = tonumber(p.timeLeft) or 0
			if typeof(p.zoneCenter) == "Vector3" then
				st.center = p.zoneCenter
			end
			st.radius = tonumber(p.zoneRadius) or st.radius
			st.seal = p.seal

			-- só na TRANSIÇÃO: o card chega a cada pacote e remontá-lo 2x/s
			-- piscaria os chips de elemento na cara do campeão.
			local ended = st.state == "Ended"
			if ended ~= wasEnded then
				wasEnded = ended
				if ended then
					showSeal()
				else
					hideSeal()
				end
			end
		end)
	)
end

function MatchUi.update(dt: number)
	if not gui then
		return
	end
	st.timeLeft = math.max(0, st.timeLeft - dt)

	-- ------------------------------------------------------------- barra
	aliveLabel.Text = string.format("%s %d/%d", tr.alive, st.alive, st.total)
	if st.state == "Lobby" then
		zoneLabel.Text = string.format("%s %s", tr.starting, clock(st.timeLeft))
		zoneLabel.TextColor3 = TEXT
		phaseLabel.Text = tr.waiting
	elseif st.state == "Ended" then
		zoneLabel.Text = clock(st.timeLeft)
		zoneLabel.TextColor3 = TEXT_DIM
		phaseLabel.Text = ""
	else
		local head = if st.shrinking then tr.shrink else tr.hold
		zoneLabel.Text = string.format("%s %s", head, clock(st.timeLeft))
		zoneLabel.TextColor3 = if st.shrinking then ZONE_VIOLET else TEXT
		phaseLabel.Text = string.format(tr.phase, st.phase, st.phases)
	end

	-- ----------------------------------------------- fora da zona (só aviso)
	outside = false
	if st.state == "Playing" then
		local char = localPlayer and (localPlayer :: Player).Character
		local rootPart = char and char:FindFirstChild("HumanoidRootPart")
		local hum = char and char:FindFirstChildOfClass("Humanoid")
		if rootPart and rootPart:IsA("BasePart") and hum and hum.Health > 0 then
			local pos = rootPart.Position
			local dx, dz = pos.X - st.center.X, pos.Z - st.center.Z
			outside = math.sqrt(dx * dx + dz * dz) > st.radius
		end
	end
	warn.Visible = outside
	if outside then
		pulse = (pulse + dt * L.warnHz) % 1
		local a = 0.15 + 0.35 * (1 - math.cos(pulse * math.pi * 2)) / 2
		warnText.TextTransparency = a
		warnSub.TextTransparency = a * 0.6
	end

	-- ------------------------------------------------------ tela de fim
	if overlay.Visible then
		overlayShown = math.min(1, overlayShown + dt * L.overlayFade)
		overlay.BackgroundTransparency = 1 - overlayShown * 0.55
	end
end

--- Some com tudo: nada vaza entre respawns/trocas de lugar.
function MatchUi.destroy()
	for _, c in ipairs(conns) do
		c:Disconnect()
	end
	table.clear(conns)
	if gui then
		gui:Destroy()
		gui = nil
	end
end

return MatchUi
