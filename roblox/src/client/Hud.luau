--!strict
-- ============================================================================
-- RAIA D (HUD/UI) — HUD INFORMATIVO: vida, Escudo Evolutivo (nível + progresso
-- para o próximo), mana, Selo de Arkana, PRESENÇA DO PARCEIRO e anúncios.
--
-- A DUPLA É METADE DO JOGO (auditoria de 18/08): o jogador precisa saber sem
-- procurar quem é o parceiro, onde ele está, qual elemento ele tem e se está
-- vivo. Por isso a faixa do parceiro (alto à direita, sob a barra da partida) e
-- a SETA que aponta para ele — a mesma seta vira marcador sobre a cabeça dele
-- quando está em quadro. Cor do TIME (magenta), nunca a do elemento: elemento
-- se lê no selo ao lado.
-- O SELO DE ARKANA É O MEDIDOR DE SINTONIA (GDD §10): as duas gemas do par
-- acendem quando a fusão está disponível AGORA. É o que ensina o combo sem
-- uma linha de texto.
--
-- IDENTIDADE (GDD §10): azul-noite #0B1026 + dourado #F0C75E, painéis chapados
-- e cantos vivos (estética blocky — nada de canto arredondado).
-- ACESSIBILIDADE (GDD §10, inegociável): cor + FORMA. O nível do escudo aparece
-- em NÚMERO e em PIPS (4 quadrados) além da cor — daltônico lê igual.
-- ZONA DE POLEGAR: informação no ALTO da tela. A metade de baixo e a direita
-- pertencem aos botões da raia C (ScreenGui "ArkanaControls").
-- O cliente NÃO decide nada: só apresenta Net.StatsUpdate / Net.Announce.
-- ============================================================================

local GuiService = game:GetService("GuiService")
local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local Workspace = game:GetService("Workspace")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Balance = require(Shared:WaitForChild("Balance"))
local Elements = require(Shared:WaitForChild("Elements"))
local Net = require(Shared:WaitForChild("Net"))
local Strings = require(Shared:WaitForChild("Strings"))
local A11y = require(script.Parent:WaitForChild("Accessibility"))
local Badge = require(script.Parent:WaitForChild("HudBadge"))

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
--- COR DO TIME (não do elemento). Magenta é a única família de cor que não
--- colide com nada em cena: nenhum dos 5 elementos, nem o dourado da marca, nem
--- o violeta da zona, nem o verde da vida, nem o vermelho do perigo. Some com
--- ela e o parceiro vira mais um borrão na tela.
local ALLY = Color3.fromHex("#FF5CE1")

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
	--- faixa do parceiro: alto à DIREITA, logo abaixo da barra da partida
	--- (ArkanaMatch ocupa 12..74). O centro fica livre para o anúncio e o
	--- alto-esquerdo para as próprias barras.
	partnerTop = 82,
	partnerRight = 12,
	partnerW = 200,
	partnerH = 40,
	--- seta do parceiro: some nas bordas quando ele sai de quadro
	arrow = 30,
	arrowMargin = 34,
}

--- Alcance em que a dupla ainda "combina" — espelho COSMÉTICO do alcance de
--- ping do servidor (Sintonia.PING_HEAR = 120 studs). Só decide se as gemas
--- acendem; quem valida a fusão é o servidor. CALIBRAÇÃO: é este número se as
--- gemas acenderem com o parceiro longe demais para ajudar.
local FUSE_HINT_RANGE = 120

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
local gems: { Frame } = {}
local announce: TextLabel
local statsPanel: Frame
local readyPill: Frame
local partnerPanel: Frame
local partnerName: TextLabel
local partnerBadge: Frame
local partnerFill: Frame
local arrow: Frame

local localPlayer: Player? = nil

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

--- ELEMENTO ATIVO — o meu vem de Net.StatsUpdate. Sem ele o Selo não acende.
local myElement: any = nil
--- PARCEIRO publicado pelo servidor. Contrato PEDIDO à raia de TIMES (hoje o
--- servidor sabe o time em Server/Teams.luau, mas não conta a ninguém):
---   Net.MatchState.mate = { userId: number, name: string?, element: Id? }
--- Ausente = sem dupla; a faixa e a seta somem em vez de mentir.
local mateUserId: number? = nil
local mateName = ""
local mateElement: any = nil
--- VIDA E ESTADO DO PARCEIRO PELO SERVIDOR. O corpo replicado NÃO serve de
--- fonte: quando o parceiro cai ele vira ESPÍRITO e o corpo do espírito continua
--- replicando vida > 0, então ler só o corpo mostrava "parceiro vivo, barra
--- cheia" no exato momento em que ele precisava de socorro. O servidor zera
--- `hp` e manda `alive = false` nesse caso (Match.broadcast). `nil` = o pacote
--- ainda não veio nesta leva → cai no corpo replicado, como era antes.
local mateAliveSrv: boolean? = nil
local mateHp: number? = nil
local mateHpMax: number? = nil
--- Enquanto esse campo não existe: quem fechou PACTO comigo. O servidor só
--- manda `withUserId` para os DOIS envolvidos (Sintonia.accept), então isso é
--- fato dele, não palpite meu — e é exatamente a dupla que a missão mede.
local pactUserId: number? = nil
--- Elemento que cada vizinho MOSTROU ao propor Sintonia — hoje é a única fonte
--- de elemento alheio no contrato (Net.SintoniaState.element). Vale enquanto
--- `mate.element` não vier no pacote da partida.
local elementSeen: { [number]: any } = {}
local mateAlive = false
local paintedElement: any = nil
local fuseReady = false
local gemPulse = 0

--- Quem é meu parceiro: o time publicado na partida e, na falta dele, o pacto
--- de Sintonia em curso. ponytail: a segunda leitura são 2 linhas e some no dia
--- em que `mate` entrar em Net.MatchState — até lá é ela que faz a dupla
--- existir na tela.
local function matePlayer(): Player?
	local uid = mateUserId or pactUserId
	return if uid then Players:GetPlayerByUserId(uid) else nil
end

--- Corpo replicado do jogador: posição e vida chegam de graça pela plataforma,
--- não precisam de remote.
local function bodyOf(p: Player?): (any, any)
	local char = if p then (p :: Player).Character else nil
	if not char then
		return nil, nil
	end
	return char:FindFirstChild("HumanoidRootPart"), char:FindFirstChildOfClass("Humanoid")
end

-- ------------------------------------------- status publicado (débito 1, 18/08)
--- O servidor PUBLICA o estado do status como ATRIBUTO no personagem
--- (`Status_Stun` / `Status_Reveal` / `Status_Slow`, kernel do Combat) e QUEM
--- DESENHA é este cliente — é o que permite filtrar por time, coisa que o
--- Highlight replicado do servidor nunca conseguiu. Cores do kernel:
--- Stun = âmbar, Reveal = ciano, Slow = terra.
local STATUS_KINDS = { "Stun", "Reveal", "Slow" }
local STATUS_COLOR: { [string]: Color3 } = {
	Stun = Color3.fromHex("#FFE066"),
	Reveal = Color3.fromHex("#8AE9FF"),
	Slow = Color3.fromHex("#B98A4A"),
}
--- cadência da varredura: ≤ 31 corpos × 3 atributos é barato a 10 Hz, e 100 ms
--- de atraso não se percebe num efeito que dura 0,6–3,0 s
local STATUS_SCAN = 0.1
local statusAcc = 0
--- CHAVES FRACAS (mesmo padrão do servidor): corpo destruído leva o Highlight
--- junto (é filho dele) e a entrada some sozinha da tabela.
local statusFx: { [Instance]: { [string]: Highlight } } = setmetatable({}, { __mode = "k" }) :: any

--- REVELAÇÃO É PARA O INIMIGO VER: o único realce que este cliente ESCONDE é o
--- de ALIADO — eu já sei onde meu time está. O alvo continua vendo o próprio
--- realce (é o aviso de "estou marcado") e inimigo/neutro veem — que é o efeito.
--- Pura de propósito: o selfTest cobre a regra.
--- ALIADO = mesmo `Status_Team` publicado pelo servidor junto do status. Era
--- `Net.MatchState.mate` sozinho, e bot NÃO TEM UserId: parceiro bot revelado
--- acendia para o dono (débito 1, quitado em 18/08). O `mateChar` fica como
--- rede de segurança para o que roda SEM times montados (tutorial, lobby).
local TEAM_ATTR = "Status_Team"

local function revealVisible(char: Instance?, mateChar: Instance?, myChar: Instance?, myTeam: any): boolean
	if char == nil or char == mateChar then
		return false
	end
	if char == myChar then
		return true -- "estou marcado" é aviso, não vazamento
	end
	return myTeam == nil or char:GetAttribute(TEAM_ATTR) ~= myTeam
end

--- corpos que podem carregar status: personagens dos jogadores + bots
local function statusModels(): { Instance }
	local out: { Instance } = {}
	for _, p in ipairs(Players:GetPlayers()) do
		if p.Character then
			table.insert(out, p.Character :: Instance)
		end
	end
	local bots = Workspace:FindFirstChild("Bots")
	if bots then
		for _, m in ipairs(bots:GetChildren()) do
			table.insert(out, m)
		end
	end
	return out
end

local function scanStatuses()
	local mp = matePlayer()
	local mateChar: Instance? = if mp then mp.Character else nil
	--- meu corpo e meu lado, os dois publicados pelo servidor (Combat.step)
	local myChar: Instance? = if localPlayer then (localPlayer :: Player).Character else nil
	local myTeam: any = if myChar then myChar:GetAttribute(TEAM_ATTR) else nil
	for _, char in ipairs(statusModels()) do
		local entry = statusFx[char]
		for _, kind in ipairs(STATUS_KINDS) do
			local active = char:GetAttribute("Status_" .. kind) ~= nil
			local want = active and (kind ~= "Reveal" or revealVisible(char, mateChar, myChar, myTeam))
			local fx = if entry then entry[kind] else nil
			if want and not fx then
				if not entry then
					entry = {}
					statusFx[char] = entry
				end
				local hl = Instance.new("Highlight")
				hl.Name = "LocalStatus_" .. kind
				hl.FillColor = STATUS_COLOR[kind]
				hl.OutlineColor = STATUS_COLOR[kind]
				hl.FillTransparency = 0.6
				--- só o Reveal atravessa parede — é literalmente o efeito dele.
				--- Stun e Slow marcam quem já está à vista: não viram wallhack.
				hl.DepthMode = if kind == "Reveal"
					then Enum.HighlightDepthMode.AlwaysOnTop
					else Enum.HighlightDepthMode.Occluded
				hl.Adornee = char
				hl.Parent = char
				entry[kind] = hl
			elseif fx and not want then
				fx:Destroy()
				if entry then
					entry[kind] = nil
				end
			end
		end
	end
end

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
	statsPanel = panel
	--- canônico = destro (alto à ESQUERDA). O espelho canhoto é do A11y: quem
	--- joga com a esquerda no ataque quer a leitura no lado que o polegar não tapa.
	A11y.place(panel, Vector2.new(0, 0), L.panelPos)
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

		-- gema (quadrado girado 45° = losango: cor + FORMA). Apagada por padrão;
		-- `refreshGems` acende o PAR quando a fusão está disponível.
		gems[i] = mk("Frame", {
			Name = "Gem" .. i,
			BackgroundColor3 = SEAL_GEMS[i],
			BackgroundTransparency = 0.45,
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

--- INDICADOR DE COMBO DISPONÍVEL (auditoria de 18/08). As gemas do Selo já
--- acendem, mas gema acesa é sutil demais para quem está vendo o jogo pela
--- primeira vez em tela de celular. A pílula DIZ a coisa, com palavra + losango
--- (cor + FORMA, GDD §10) — e some sozinha quando a fusão deixa de existir.
--- Estática de propósito: pisca-pisca aqui seria o oposto de acessível.
--- Hoje a Sintonia É a "ultimate" do jogo; se um dia existir uma suprema
--- separada, ela ganha a mesma pílula com outra cor.
local function buildReadyPill(root: ScreenGui)
	readyPill = mk("Frame", {
		Name = "ComboReady",
		AnchorPoint = Vector2.new(0.5, 0),
		BackgroundColor3 = NIGHT,
		BackgroundTransparency = 0.25,
		BorderSizePixel = 0,
		Position = UDim2.new(0.5, 0, 0, L.sealTop + L.sealSize + 4),
		--- LARGURA PELO CONTEÚDO (recurso nativo): a pílula fica só do tamanho da
		--- frase, em qualquer idioma. Largura fixa encostaria na faixa do
		--- parceiro (alto à direita) em viewport estreita.
		AutomaticSize = Enum.AutomaticSize.X,
		Size = UDim2.fromOffset(0, 20),
		Visible = false,
	}, root)
	mk("UIStroke", { Color = GOLD, Thickness = 1, Transparency = 0.15 }, readyPill)
	mk("UIListLayout", {
		FillDirection = Enum.FillDirection.Horizontal,
		HorizontalAlignment = Enum.HorizontalAlignment.Center,
		VerticalAlignment = Enum.VerticalAlignment.Center,
		Padding = UDim.new(0, 6),
		SortOrder = Enum.SortOrder.LayoutOrder,
	}, readyPill)
	mk("UIPadding", {
		PaddingLeft = UDim.new(0, 8),
		PaddingRight = UDim.new(0, 8),
	}, readyPill)

	mk("Frame", {
		BackgroundColor3 = GOLD,
		BorderSizePixel = 0,
		LayoutOrder = 1,
		Rotation = 45,
		Size = UDim2.fromOffset(9, 9),
	}, readyPill)
	mk("TextLabel", {
		BackgroundTransparency = 1,
		LayoutOrder = 2,
		AutomaticSize = Enum.AutomaticSize.X,
		Size = UDim2.fromOffset(0, 14),
		Font = Enum.Font.GothamBold,
		Text = string.upper(Strings.t("announce.sintonia.ready")),
		TextColor3 = GOLD,
		TextSize = 12, -- fixo: `TextScaled` brigaria com o AutomaticSize
	}, readyPill)
end

--- Faixa do parceiro: nome + elemento (cor + FORMA) + vida. Escondida enquanto
--- não houver dupla — campo vazio no HUD é ruído, não informação.
local function buildPartner(root: ScreenGui)
	partnerPanel = mk("Frame", {
		Name = "Partner",
		AnchorPoint = Vector2.new(1, 0),
		BackgroundColor3 = NIGHT,
		BackgroundTransparency = 0.4,
		BorderSizePixel = 0,
		Position = UDim2.new(1, -L.partnerRight, 0, L.partnerTop),
		Size = UDim2.fromOffset(L.partnerW, L.partnerH),
		Visible = false,
	}, root)
	mk("UIStroke", { Color = ALLY, Thickness = 1, Transparency = 0.3 }, partnerPanel)
	--- espelha junto com o painel de vida: a dupla e as barras trocam de lado
	--- em bloco, senão o HUD canhoto vira uma tela remendada
	A11y.place(partnerPanel, Vector2.new(1, 0), UDim2.new(1, -L.partnerRight, 0, L.partnerTop))

	local content = mk("Frame", { BackgroundTransparency = 1, Size = UDim2.fromScale(1, 1) }, partnerPanel)
	mk("UIPadding", {
		PaddingLeft = UDim.new(0, 6),
		PaddingRight = UDim.new(0, 6),
		PaddingTop = UDim.new(0, 5),
		PaddingBottom = UDim.new(0, 5),
	}, content)

	partnerBadge = Badge.new(content, 18)

	partnerName = mk("TextLabel", {
		BackgroundTransparency = 1,
		Position = UDim2.new(0, 22, 0, 2),
		Size = UDim2.new(1, -22, 0, 14),
		Font = Enum.Font.GothamBold,
		Text = "",
		TextColor3 = ALLY,
		TextScaled = true,
		TextXAlignment = Enum.TextXAlignment.Right,
	}, content)
	mk("UITextSizeConstraint", { MinTextSize = 9, MaxTextSize = 14 }, partnerName)

	partnerFill = bar(content, 22, 6, ALLY, false)
end

--- Seta do parceiro. UMA peça faz os dois papéis: marcador discreto sobre a
--- cabeça dele quando está em quadro, e indicador preso à borda apontando para
--- ele quando não está. Chevron de dois traços — cor do TIME, zero asset.
--- O nome dele já vem de graça do Roblox (nametag nativo) — não repetimos aqui.
local function buildArrow(root: ScreenGui)
	arrow = mk("Frame", {
		Name = "PartnerArrow",
		AnchorPoint = Vector2.new(0.5, 0.5),
		BackgroundTransparency = 1,
		Size = UDim2.fromOffset(L.arrow, L.arrow),
		Visible = false,
	}, root)
	--- em Rotation = 0 o chevron aponta para a DIREITA; o pivô é a ponta,
	--- porque Rotation gira em torno do AnchorPoint
	for _, deg in ipairs({ 34, -34 }) do
		mk("Frame", {
			BackgroundColor3 = ALLY,
			BorderSizePixel = 0,
			AnchorPoint = Vector2.new(1, 0.5),
			Position = UDim2.fromScale(0.96, 0.5),
			Size = UDim2.fromScale(0.62, 0.17),
			Rotation = deg,
		}, arrow)
	end
end

--- O Selo É o medidor de Sintonia (GDD §10): acende as DUAS gemas do par
--- quando existe fusão possível AGORA. É convite, não promessa — quem valida a
--- fusão continua sendo o servidor.
local function refreshGems(dt: number)
	gemPulse = if fuseReady then (gemPulse + dt * 2.2) % 1 else 0
	--- "reduzir flashes": a AMPLITUDE do pulso encolhe, a leitura não. Com o
	--- pulso em 0 as gemas do par ficam acesas e FIXAS — quem não suporta
	--- piscada continua vendo que o combo está disponível.
	local beat = (1 - math.cos(gemPulse * math.pi * 2)) / 2 * A11y.flash()
	readyPill.Visible = fuseReady and A11y.readyCue()
	for i, gem in ipairs(gems) do
		local id: any = Elements.Order[i]
		local lit = fuseReady and (id == myElement or id == mateElement)
		gem.BackgroundTransparency = if lit then 0.05 * (1 - beat) elseif id == myElement then 0.2 else 0.72
		local px = if lit then 11 + beat * 4 else 10
		gem.Size = UDim2.fromOffset(px, px)
	end
end

--- Onde a seta encosta na borda, a partir do vetor centro→parceiro. Pura de
--- propósito: é a única conta desta tela que não dá para conferir a olho — o
--- autoteste no fim do arquivo cobre ela.
local function edgePoint(dx: number, dy: number, halfW: number, halfH: number): (number, number)
	if math.abs(dx) < 1e-3 and math.abs(dy) < 1e-3 then
		dy = 1 -- parceiro exatamente no centro: aponta para baixo em vez de NaN
	end
	local sx = if math.abs(dx) > 1e-3 then halfW / math.abs(dx) else math.huge
	local sy = if math.abs(dy) > 1e-3 then halfH / math.abs(dy) else math.huge
	local s = math.min(sx, sy) -- a borda mais próxima é quem trava
	return dx * s, dy * s
end

--- Presença da dupla: quem é, onde está, qual elemento, se está vivo.
--- Posição e vida do parceiro chegam pelo CORPO replicado (de graça na
--- plataforma); só a identidade e o elemento dependem do servidor.
local function updatePartner()
	local mp = matePlayer()
	local cam = Workspace.CurrentCamera
	if not mp then
		partnerPanel.Visible = false
		arrow.Visible = false
		mateAlive, fuseReady = false, false
		return
	end
	partnerPanel.Visible = true

	local element = mateElement or elementSeen[mp.UserId]
	mateElement = element
	if element ~= paintedElement then
		paintedElement = element
		Badge.paint(partnerBadge, element)
	end

	local mateRoot, hum = bodyOf(mp)
	--- O SERVIDOR MANDA, o HUD mostra — inclusive aqui. Ver o comentário de
	--- `mateAliveSrv`: o corpo do espírito mente, o pacote da partida não.
	mateAlive = if mateAliveSrv ~= nil then mateAliveSrv else (hum ~= nil and hum.Health > 0)
	local hpFrac = 0
	if mateAlive then
		if mateHp and mateHpMax and mateHpMax > 0 then
			hpFrac = clamp01(mateHp / mateHpMax)
		elseif hum then
			hpFrac = clamp01(hum.Health / math.max(hum.MaxHealth, 1))
		end
	end
	partnerName.Text = if mateAlive
		then (if mateName ~= "" then mateName else mp.DisplayName)
		else Strings.t("hud.partner.down")
	partnerName.TextColor3 = if mateAlive then ALLY else DANGER
	partnerFill.BackgroundColor3 = if mateAlive then ALLY else DANGER
	partnerFill.Size = UDim2.fromScale(hpFrac, 1)

	local myRoot = bodyOf(localPlayer)
	if not (mateAlive and mateRoot and myRoot and cam and gui) then
		arrow.Visible = false
		fuseReady = false
		return
	end

	--- fusão possível = elementos diferentes (a matriz do GDD §9 decide) e o
	--- parceiro perto o bastante para atacar o mesmo alvo
	fuseReady = Elements.isValid(myElement)
		and Elements.isValid(element)
		and Elements.getCombo(myElement, element) ~= nil
		and (mateRoot.Position - myRoot.Position).Magnitude <= FUSE_HINT_RANGE

	-- ---------------------------------------------------------------- seta
	local view = (gui :: ScreenGui).AbsoluteSize
	local sp, onScreen = cam:WorldToViewportPoint(mateRoot.Position + Vector3.new(0, 4.5, 0))
	--- WorldToViewportPoint conta a partir do topo REAL da tela; este ScreenGui
	--- respeita a barra do Roblox (IgnoreGuiInset = false), então desconta.
	local inset = GuiService:GetGuiInset()
	local x, y = sp.X - inset.X, sp.Y - inset.Y
	--- atrás da câmera o ponto projeta espelhado: reflete pelo centro para a
	--- seta apontar para trás, e não para o lado errado
	if sp.Z <= 0 then
		x, y = view.X - x, view.Y - y
	end

	local m = L.arrowMargin
	local cx, cy = view.X / 2, view.Y / 2
	if sp.Z <= 0 or not onScreen or x < m or x > view.X - m or y < m or y > view.Y - m then
		-- fora de quadro: encosta no retângulo seguro e aponta para ele
		local ox, oy = edgePoint(x - cx, y - cy, cx - m, cy - m)
		arrow.Position = UDim2.fromOffset(cx + ox, cy + oy)
		arrow.Rotation = math.deg(math.atan2(oy, ox))
	else
		arrow.Position = UDim2.fromOffset(x, y)
		arrow.Rotation = 90 -- em quadro: aponta para BAIXO, na cabeça dele
	end
	arrow.Visible = true
end

-- ------------------------------------------------------------------ API
function Hud.init(player: Player)
	if gui then
		return
	end
	localPlayer = player
	Strings.followPlayerLocale()

	local playerGui = player:WaitForChild("PlayerGui")
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
	buildReadyPill(root)
	buildPartner(root)
	buildArrow(root)
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

	--- escala da UI, alto contraste e o espelho canhoto passam a valer para tudo
	--- que está (e que ainda vai entrar) nesta ScreenGui
	A11y.attach(root)

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
			-- elemento ativo: é ele que acende a minha gema no Selo
			if Elements.isValid(p.element) then
				myElement = p.element
			end
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
			--- `p.args` vem da rede: se não for tabela, `table.unpack` ERRA dentro do
			--- handler e o anúncio some sem deixar rastro. Vale o texto sem os args.
			local raw = isTable and p.args
			local args = if typeof(raw) == "table" then raw else { ... }
			showAnnounce(Strings.t(key, table.unpack(args)))
		end)
	)

	--- QUEM É MEU PARCEIRO — o servidor manda, o HUD mostra. Pacote ausente =
	--- sem dupla (cai no pacto de Sintonia em curso, ver `matePlayer`).
	table.insert(
		conns,
		Net.event(Net.Names.MatchState).OnClientEvent:Connect(function(p: any)
			if typeof(p) ~= "table" then
				return
			end
			local m = p.mate
			if typeof(m) == "table" and tonumber(m.userId) then
				mateUserId = tonumber(m.userId)
				mateName = tostring(m.name or "")
				mateElement = if Elements.isValid(m.element) then m.element else nil
				--- estes três o servidor já mandava e o HUD jogava fora
				mateAliveSrv = if typeof(m.alive) == "boolean" then m.alive else nil
				mateHp = tonumber(m.hp)
				mateHpMax = tonumber(m.hpMax)
			else
				mateUserId, mateName, mateElement = nil, "", nil
				mateAliveSrv, mateHp, mateHpMax = nil, nil, nil
			end
		end)
	)

	--- Quem propõe Sintonia REVELA o elemento (Net.SintoniaState). É a única
	--- fonte de elemento alheio no contrato de hoje — sem ela o Selo nunca
	--- acenderia o par. ponytail: some quando `mate.element` vier na partida.
	table.insert(
		conns,
		Net.event(Net.Names.SintoniaState).OnClientEvent:Connect(function(p: any)
			if typeof(p) ~= "table" or p.phase ~= "proposed" or typeof(p.byUserId) ~= "number" then
				return
			end
			if Elements.isValid(p.element) then
				elementSeen[p.byUserId] = p.element
			end
			--- pacto fechado: o servidor só conta `withUserId` para os DOIS
			--- envolvidos, então se um deles sou eu, o outro é meu parceiro.
			local me = localPlayer
			if me and typeof(p.withUserId) == "number" then
				if p.withUserId == me.UserId then
					pactUserId = p.byUserId
				elseif p.byUserId == me.UserId then
					pactUserId = p.withUserId
				end
			end
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
	updatePartner()
	refreshGems(dt)

	--- status publicado pelo servidor → realce decidido AQUI (débito 1)
	statusAcc += dt
	if statusAcc >= STATUS_SCAN then
		statusAcc = 0
		scanStatuses()
	end

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
	--- realces locais de status moram nos personagens, não nesta ScreenGui
	for _, entry in pairs(statusFx) do
		for _, fx in pairs(entry) do
			fx:Destroy()
		end
	end
	statusFx = setmetatable({}, { __mode = "k" }) :: any
	statusAcc = 0
	table.clear(pips)
	table.clear(gems)
	table.clear(elementSeen)
	mateUserId, mateName, mateElement, paintedElement, pactUserId = nil, "", nil, nil, nil
	mateAliveSrv, mateHp, mateHpMax = nil, nil, nil
	mateAlive, fuseReady = false, false
	localPlayer = nil
	if gui then
		gui:Destroy()
		gui = nil
	end
end

-- ============================================================================
-- AUTOTESTE — cole no Command Bar do Studio (cliente):
--   require(game.Players.LocalPlayer.PlayerScripts.Client.Hud).selfTest()
-- Cobre a única conta desta tela que não dá para ver a olho: a seta do parceiro
-- fora de quadro. Errar aqui deixa a seta parada num canto para sempre — e a
-- presença da dupla é o item nº 1 desta raia.
-- ============================================================================
function Hud.selfTest()
	local hw, hh = 300, 200 -- meia tela útil (já descontada a margem)

	-- parceiro à direita: encosta na borda direita, na altura do meio
	local ox, oy = edgePoint(1000, 0, hw, hh)
	assert(math.abs(ox - hw) < 1e-6 and math.abs(oy) < 1e-6, "seta à direita saiu do lugar")

	-- parceiro acima: encosta no topo
	ox, oy = edgePoint(0, -1000, hw, hh)
	assert(math.abs(oy + hh) < 1e-6 and math.abs(ox) < 1e-6, "seta acima saiu do lugar")

	-- diagonal: trava na borda MAIS PRÓXIMA e nunca vaza do retângulo
	ox, oy = edgePoint(500, 500, hw, hh)
	assert(math.abs(ox) <= hw + 1e-6 and math.abs(oy) <= hh + 1e-6, "seta vazou do retângulo")
	assert(math.abs(oy - hh) < 1e-6, "diagonal tem de travar na borda de cima/baixo")

	-- parceiro exatamente no centro (câmera em cima dele): sem NaN, aponta baixo
	ox, oy = edgePoint(0, 0, hw, hh)
	assert(ox == ox and oy == oy, "caso degenerado virou NaN")
	assert(oy > 0, "caso degenerado tem de apontar para baixo")

	-- FILTRO DO STATUS PUBLICADO (débito 1): o único Reveal que este cliente
	-- esconde é o de ALIADO — o meu e o do inimigo aparecem.
	local me = Instance.new("Model")
	local mate = Instance.new("Model")
	local foe = Instance.new("Model")
	assert(revealVisible(me, mate, me, nil), "o MEU Reveal aparece (aviso de 'estou marcado')")
	assert(revealVisible(foe, mate, me, nil), "Reveal de INIMIGO aparece — é o efeito")
	assert(revealVisible(foe, nil, me, nil), "sem parceiro, todo Reveal aparece")
	assert(not revealVisible(mate, mate, me, nil), "Reveal do PARCEIRO não aparece para mim (já sei onde ele está)")
	assert(not revealVisible(nil, mate, me, nil), "sem corpo não há realce")

	-- POR TIME (o conserto do débito): o parceiro BOT não tem UserId e NUNCA
	-- chega em `Net.MatchState.mate` — sem o lado publicado ele acendia para o
	-- dono. Aqui ele entra como corpo desconhecido, só com `Status_Team`.
	local botMate = Instance.new("Model")
	me:SetAttribute(TEAM_ATTR, 1)
	botMate:SetAttribute(TEAM_ATTR, 1)
	foe:SetAttribute(TEAM_ATTR, 2)
	assert(not revealVisible(botMate, nil, me, 1), "parceiro BOT (mesmo time) NÃO pode acender para o dono")
	assert(revealVisible(foe, nil, me, 1), "inimigo de outro time continua acendendo")
	assert(revealVisible(me, nil, me, 1), "meu próprio Reveal aparece mesmo no meu time")
	local neutral = Instance.new("Model") -- corpo sem time publicado (lobby/tutorial)
	assert(revealVisible(neutral, nil, me, 1), "corpo SEM time publicado não é aliado por omissão")
	assert(revealVisible(botMate, nil, me, nil), "sem MEU time o filtro cai no parceiro conhecido, como antes")

	me:Destroy()
	mate:Destroy()
	foe:Destroy()
	botMate:Destroy()
	neutral:Destroy()

	print("[Hud] autoteste OK — seta do parceiro + filtro de Reveal por time")
	return true
end

return Hud
