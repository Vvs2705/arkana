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
local A11y = require(script.Parent:WaitForChild("Accessibility"))

-- ------------------------------------------------------------------ paleta
local NIGHT = Color3.fromHex("#0B1026")
local NIGHT_DEEP = Color3.fromHex("#05070F")
local GOLD = Color3.fromHex("#F0C75E")
local GOLD_DIM = Color3.fromHex("#8A7336")
local TEXT = Color3.fromHex("#E8E6F0")
local TEXT_DIM = Color3.fromHex("#9A97AD")
local DANGER = Color3.fromHex("#E04040")
local ZONE_VIOLET = Color3.fromHex("#8A5CF0")

--- MOTIVO (do servidor) → CHAVE DE TEXTO. Nenhuma frase mora no código de UI
--- (regra dura do projeto): o servidor manda o motivo, esta tabela escolhe a
--- chave e o Strings escreve — inclusive em inglês.
local SPECTATE_WHY: { [string]: string } = {
	late = "match.spectating.late",
	out = "match.spectating.out",
}

local L = {
	barTop = 12,
	barRight = 12,
	barW = 190,
	barH = 62,
	--- barra SEM a linha da fase (modo ESSENCIAL, ver `phaseLineVisible`): sobram
	--- os times vivos e o relógio da zona, que são as duas linhas que mudam.
	barHMin = 50,
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
local localPlayer: Player? = nil

-- widgets
local bar: Frame
local aliveLabel: TextLabel
local zoneLabel: TextLabel
local phaseLabel: TextLabel
local warn: Frame
local warnText: TextLabel
local warnSub: TextLabel
local spectate: Frame
local spectTitle: TextLabel
local spectWhy: TextLabel
local spectNext: TextLabel
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
	--- EM DUOS A INFORMAÇÃO ÚTIL É "TIMES VIVOS", não "jogadores vivos": com
	--- 4 vivos o jogador precisa saber se são 4 duplas ou 2. O servidor publica
	--- `teamsAlive` (e, quando tiver, `teamsTotal`) no Net.MatchState.
	--- `nil` = o campo ainda não veio nesta leva → a barra volta a contar
	--- JOGADORES em vez de mentir um número de times.
	teamsAlive = nil :: number?,
	teamsTotal = nil :: number?,
	timeLeft = 0,
	seal = nil :: any,
	--- POR QUE estou assistindo: "late" (entrei com a prova em andamento) ou
	--- "out" (caí nesta prova). `nil` = não sou espectador — inclusive o ESPÍRITO,
	--- que o servidor mantém fora daqui de propósito: ele ainda volta pelo altar.
	spectateKind = nil :: string?,
}
local outside = false
local pulse = 0
local overlayShown = 0
local wasEnded = false

-- ------------------------------------------- DENSIDADE DA TELA (R11, Diretor)
--- "FASE 2/5" é a linha mais PARADA da tela: muda umas quatro vezes numa prova
--- inteira e nunca decide um tiroteio — quem decide é o relógio logo acima, que
--- já diz FECHANDO/ESTÁVEL. Em ESSENCIAL ela deixa de ocupar espaço permanente
--- e vira aviso: aparece quando o número MUDA e some sozinha. Em COMPLETO fica
--- fixa, como sempre foi. Fora de "Playing" ela também fica: no lobby e no fim
--- é ela que carrega o rótulo do estado, e ali não há tiroteio para atrapalhar.
local PHASE_HOLD = 4
local phaseLeft = 0
local hudFull = false

--- Leitura PROTEGIDA: `hudDensity` é chave da raia do Accessibility e pode não
--- existir ainda. Chave desconhecida (ou erro) cai no padrão ESSENCIAL.
local function densityFull(): boolean
	local ok, v = pcall(A11y.get, "hudDensity")
	return ok and v == "full"
end

--- Pura de propósito: é o que o autoteste consegue reprovar sem montar a tela.
local function phaseLineVisible(full: boolean, playing: boolean, hold: number): boolean
	return full or not playing or hold > 0
end

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
	--- canônico = destro (alto à DIREITA). No HUD canhoto ela troca de lado
	--- junto com as barras de vida — o A11y espelha o conjunto.
	A11y.place(bar, Vector2.new(1, 0), UDim2.new(1, -L.barRight, 0, L.barTop))

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

--- PLACA DE ESPECTADOR — a primeira impressão do estranho num alpha público.
--- Quem entra com a prova em andamento nasce no poleiro a 120 studs do chão; sem
--- esta placa ele olha o mapa de cima e conclui que o jogo travou. Por isso é
--- PERMANENTE enquanto durar o estado, e não um toast que some antes de o
--- cliente dele terminar de carregar.
---
--- ALTO E CENTRAL: não disputa espaço com a barra (alto à direita), com a HUD
--- (alto à esquerda) nem com os botões (metade de baixo), e fica ACIMA do aviso
--- de fora da zona (0.30), que de qualquer forma nunca acende para quem está
--- ancorado no centro. ZIndex 12 contra os 10 da tela de fim: é justamente no
--- fim da prova que "próxima partida" precisa continuar legível.
local function buildSpectate(root: ScreenGui)
	spectate = mk("Frame", {
		Name = "Spectating",
		AnchorPoint = Vector2.new(0.5, 0),
		BackgroundColor3 = NIGHT,
		BackgroundTransparency = 0.2,
		BorderSizePixel = 0,
		Position = UDim2.new(0.5, 0, 0.06, 0),
		Size = UDim2.new(0.56, 0, 0, 66),
		Visible = false,
		ZIndex = 12,
	}, root)
	mk("UISizeConstraint", { MinSize = Vector2.new(210, 66), MaxSize = Vector2.new(420, 66) }, spectate)
	mk("UIStroke", { Color = GOLD_DIM, Thickness = 1, Transparency = 0.2 }, spectate)
	mk("UIPadding", {
		PaddingLeft = UDim.new(0, 10),
		PaddingRight = UDim.new(0, 10),
		PaddingTop = UDim.new(0, 4),
	}, spectate)

	spectTitle = mk("TextLabel", {
		BackgroundTransparency = 1,
		Size = UDim2.new(1, 0, 0, 22),
		Font = Enum.Font.GothamBlack,
		Text = "",
		TextColor3 = GOLD,
		TextScaled = true,
		ZIndex = 13,
	}, spectate)
	mk("UITextSizeConstraint", { MaxTextSize = 22 }, spectTitle)

	--- A LINHA QUE MUDA TUDO: "você entra na próxima" × "você caiu nesta prova".
	spectWhy = mk("TextLabel", {
		BackgroundTransparency = 1,
		Position = UDim2.new(0, 0, 0, 23),
		Size = UDim2.new(1, 0, 0, 19),
		Font = Enum.Font.GothamBold,
		Text = "",
		TextColor3 = TEXT,
		TextScaled = true,
		ZIndex = 13,
	}, spectate)
	mk("UITextSizeConstraint", { MaxTextSize = 18 }, spectWhy)

	spectNext = mk("TextLabel", {
		BackgroundTransparency = 1,
		Position = UDim2.new(0, 0, 0, 43),
		Size = UDim2.new(1, 0, 0, 15),
		Font = Enum.Font.Gotham,
		Text = "",
		TextColor3 = TEXT_DIM,
		TextScaled = true,
		ZIndex = 13,
	}, spectate)
	mk("UITextSizeConstraint", { MaxTextSize = 14 }, spectNext)
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
	-- fundo de tela cheia não escala junto com a preferência de tamanho: crescer
	-- 130% só o faria vazar da tela (o card interno já tem UISizeConstraint)
	overlay:SetAttribute("A11yNoScale", true)

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
	sealTitle.Text = Strings.t("match.seal.title")
	if not seal then
		sealName.Text = Strings.t("match.seal.nochampion")
		sealRows.Text = ""
		fillElements({})
		return
	end
	sealName.Text = string.upper(tostring(seal.champion))
	sealRows.Text = string.format(
		"%s %d    %s %d    %s %d\n%s %s    %s",
		Strings.t("match.kills"),
		tonumber(seal.kills) or 0,
		Strings.t("match.damage"),
		tonumber(seal.damage) or 0,
		Strings.t("match.combos"),
		#(seal.combos or {}),
		Strings.t("match.duration"),
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

	local playerGui = player:WaitForChild("PlayerGui")
	local root = mk("ScreenGui", {
		Name = "ArkanaMatch", -- raia D usa "ArkanaHud", raia C "ArkanaControls"
		ResetOnSpawn = false, -- sobrevive à morte: a tela de fim não pode sumir
		IgnoreGuiInset = false,
		--- COLISÃO CORRIGIDA: a SintoniaUi também declarava 6 ("o aceite não pode
		--- ser tapado") e o Roblox não define quem ganha o empate — a tela de fim
		--- de partida e o card de ACEITAR podiam se cobrir em ordem aleatória.
		--- Esta ScreenGui é a que diz "cobre tudo", então ela sobe: acima do HUD
		--- (4), dos botões (5), da Sintonia (6), do tutorial (7) e das legendas
		--- (8); abaixo do menu de acessibilidade (20), que é modal.
		DisplayOrder = 10,
		ZIndexBehavior = Enum.ZIndexBehavior.Sibling,
	}, playerGui)
	gui = root

	buildBar(root)
	buildWarning(root)
	buildSpectate(root)
	buildOverlay(root)
	A11y.attach(root)
	warnText.Text = Strings.t("match.outside")
	warnSub.Text = Strings.t("match.outside.sub")

	--- densidade: o `update` já reavalia por quadro, aqui só o valor inicial e o
	--- gancho de mudança (`pcall` porque a chave/o gancho são de outra raia).
	hudFull = densityFull()
	pcall(A11y.onChange, function(key: string)
		if key == "hudDensity" then
			hudFull = densityFull()
		end
	end)

	table.insert(
		conns,
		Net.event(Net.Names.MatchState).OnClientEvent:Connect(function(p: any)
			if typeof(p) ~= "table" then
				return
			end
			st.state = tostring(p.state or st.state)
			local lastPhase = st.phase
			st.phase = tonumber(p.phase) or st.phase
			--- em ESSENCIAL a linha da fase só existe quando o número MUDA
			if st.phase ~= lastPhase then
				phaseLeft = PHASE_HOLD
			end
			st.phases = tonumber(p.phases) or st.phases
			local wasShrinking = st.shrinking
			st.shrinking = p.shrinking == true
			st.alive = tonumber(p.alive) or 0
			st.total = math.max(1, tonumber(p.total) or 1)
			--- FALLBACK SILENCIOSO: `tonumber(nil)` é `nil`, então enquanto a raia
			--- do Match não publicar estes campos a barra segue contando jogadores.
			st.teamsAlive = tonumber(p.teamsAlive)
			st.teamsTotal = tonumber(p.teamsTotal)
			st.timeLeft = tonumber(p.timeLeft) or 0
			--- Só string entra: pacote sem o campo (ou com lixo) apaga o painel em
			--- vez de deixar a placa velha de pé sobre uma partida nova.
			st.spectateKind = if typeof(p.spectateKind) == "string" then p.spectateKind else nil
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

			-- LEGENDA: "a zona começou a fechar" é um cue que hoje só existe na
			-- cor do relógio. Quem joga sem som (ou sem reparar no canto) lê aqui.
			if st.shrinking and not wasShrinking then
				A11y.caption(Strings.t("match.shrink"), ZONE_VIOLET, 2.2)
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
	--- Em duos o que decide a jogada é QUANTOS TIMES restam. Sem `teamsTotal` a
	--- barra mostra só o contador — melhor um número certo sem denominador do
	--- que "4/8" com o 8 inventado aqui.
	if st.teamsAlive then
		aliveLabel.Text = if st.teamsTotal
			then string.format("%s %d/%d", Strings.t("match.teams"), st.teamsAlive, st.teamsTotal)
			else string.format("%s %d", Strings.t("match.teams"), st.teamsAlive)
	else
		aliveLabel.Text = string.format("%s %d/%d", Strings.t("match.alive"), st.alive, st.total)
	end
	if st.state == "Lobby" then
		zoneLabel.Text = string.format("%s %s", Strings.t("match.starting"), clock(st.timeLeft))
		zoneLabel.TextColor3 = TEXT
		phaseLabel.Text = Strings.t("match.waiting")
	elseif st.state == "Ended" then
		zoneLabel.Text = clock(st.timeLeft)
		zoneLabel.TextColor3 = TEXT_DIM
		phaseLabel.Text = ""
	else
		local head = if st.shrinking then Strings.t("match.shrink") else Strings.t("match.hold")
		zoneLabel.Text = string.format("%s %s", head, clock(st.timeLeft))
		zoneLabel.TextColor3 = if st.shrinking then ZONE_VIOLET else TEXT
		phaseLabel.Text = Strings.t("match.phase", st.phase, st.phases)
	end

	--- LINHA DA FASE sob demanda (ESSENCIAL): a barra encolhe quando ela não está
	--- lá, senão sobraria um buraco de 12 px no alto à direita.
	if phaseLeft > 0 then
		phaseLeft -= dt
	end
	local showPhase = phaseLineVisible(hudFull, st.state == "Playing", phaseLeft)
	phaseLabel.Visible = showPhase
	bar.Size = UDim2.fromOffset(L.barW, if showPhase then L.barH else L.barHMin)

	-- ----------------------------------------------- fora da zona (só aviso)
	local wasOutside = outside
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
		-- LEGENDA só na ENTRADA: fora da zona é estado contínuo e a faixa é para
		-- "aconteceu isto agora" — repetir por frame viraria ruído.
		if not wasOutside then
			A11y.caption(Strings.t("match.outside"), DANGER, 2.0)
		end
		pulse = (pulse + dt * L.warnHz) % 1
		--- "reduzir flashes": com o pulso em 0 o aviso fica ACESO e fixo em vez
		--- de sumir — reduzir piscada nunca pode reduzir informação.
		local a = 0.15 + 0.35 * (1 - math.cos(pulse * math.pi * 2)) / 2 * A11y.flash()
		warnText.TextTransparency = a
		warnSub.TextTransparency = a * 0.6
	end

	-- --------------------------------------------------- placa de espectador
	--- TRÊS ESTADOS, e a placa só existe no terceiro: jogando (o servidor manda
	--- `spectateKind` nil) · ESPÍRITO (também nil — quem ainda volta pelo altar
	--- não é espectador, e é o servidor que decide isso) · POLEIRO.
	local kind = st.spectateKind
	spectate.Visible = kind ~= nil
	if kind then
		spectTitle.Text = Strings.t("match.spectating")
		spectWhy.Text = Strings.t(SPECTATE_WHY[kind] or "match.spectating.out")
		--- TEMPO ATÉ A PRÓXIMA — SEM RELÓGIO INVENTADO. Ninguém sabe em que segundo
		--- esta prova acaba: a última dupla pode vencer no quadro seguinte ou a zona
		--- pode levar mais três fases. O teto de duração que o servidor guarda é
		--- TETO, não previsão — uma contagem regressiva a partir dele erraria por
		--- minutos, e número inventado destrói a confiança em todos os outros
		--- números da tela. Então a linha mostra o que É verdade e vem do servidor:
		--- em que FASE a zona está e quantos TIMES sobraram (2 = está acabando).
		--- Terminada a prova, ela vira o rótulo da próxima — e o relógio REAL dela
		--- é o da barra logo acima, que aí sim conta um tempo conhecido.
		if st.state == "Playing" then
			local line = Strings.t("match.phase", st.phase, st.phases)
			if st.teamsAlive then
				line ..= string.format("   %s %d", Strings.t("match.teams"), st.teamsAlive)
			end
			spectNext.Text = line
		else
			spectNext.Text = Strings.t("match.spectating.next")
		end
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
	phaseLeft = 0
end

-- ============================================================================
-- AUTOTESTE — roda no runner headless e na Command Bar do Studio (cliente):
--   require(game.Players.LocalPlayer.PlayerScripts.Client.MatchUi).selfTest()
-- Cobre o que não dá para ver a olho: a regra de densidade da linha da fase e a
-- promessa de zona de polegar (nada desta tela cai na metade de baixo, que é dos
-- botões da raia C, nem na faixa de legendas do rodapé).
-- ============================================================================
function MatchUi.selfTest()
	-- A PONTE COM O MENU: `hudDensity` é chave de outra raia. A leitura é
	-- protegida (falta da chave não derruba a tela) e o padrão é ESSENCIAL.
	assert(not densityFull(), "padrão de fábrica de `hudDensity` tem de ser ESSENCIAL")

	-- DENSIDADE: em ESSENCIAL a fase é aviso; em COMPLETO é linha fixa.
	assert(phaseLineVisible(true, true, 0), "COMPLETO tem de manter a linha da fase")
	assert(not phaseLineVisible(false, true, 0), "ESSENCIAL não pode manter a fase parada na tela")
	assert(phaseLineVisible(false, true, 0.1), "a fase tem de APARECER quando o número muda")
	assert(phaseLineVisible(false, false, 0), "fora da partida a linha fica: é ela que diz o estado")
	assert(PHASE_HOLD > 0, "aviso com prazo zero nunca chega a ser lido")
	assert(L.barHMin < L.barH, "sem a linha da fase a barra tem de encolher, não deixar buraco")

	-- ZONA DE POLEGAR (régua: 640 px de altura = celular pequeno em paisagem,
	-- a mesma da raia C). Metade de baixo = botões; rodapé = legendas.
	local VIEW_H = 640
	local half = VIEW_H / 2
	assert(L.barTop + L.barH < half, "MatchBar invadiu a metade de baixo")
	--- a placa de espectador é ancorada em ESCALA (0.06) + 66 px de altura
	assert(0.06 * VIEW_H + 66 < half, "placa de espectador invadiu a metade de baixo")
	assert(L.warnTop * VIEW_H + 54 < half, "aviso de fora da zona invadiu a metade de baixo")

	-- relógio: o texto que o jogador mais olha na prova inteira
	assert(clock(0) == "0:00" and clock(65) == "1:05", "relógio da zona escreveu errado")
	assert(clock(-3) == "0:00", "tempo negativo não pode virar relógio de trás para frente")

	print("[MatchUi] autoteste OK — densidade da fase + zona de polegar + relógio")
	return true
end

return MatchUi
