--!strict
-- ============================================================================
-- RAIA R2 (cliente) — UI DA SINTONIA: Ping "COMBO?", CONVITE EM 1 TOQUE,
-- canalização e o nome do combo executado. GDD §9 + §18.7.
--
-- O CONVITE É O PRODUTO (auditoria de 18/08): quem recebe um ping vê um card
-- com quem chamou, os DOIS elementos (cor + FORMA), o NOME do combo que sai
-- disso — "EXPLOSÃO DE PLASMA", não "combo" — e dois alvos de dedo grandes:
-- [ ACEITAR ] e [ × ]. Aceitou, os dois passam a ver o MESMO marco no mundo e
-- a MESMA barra. Recusar é local e silencioso: não pune, não avisa o outro,
-- não abre tutorial. Coordenar sem microfone tem de ser um gesto trivial.
--
-- O CLIENTE NÃO DECIDE NADA. Ele manda duas intenções (ProposeSintonia,
-- AcceptSintonia) e desenha o que Net.SintoniaState anuncia. Distância,
-- janela, cooldown, elementos e "está vivo?" são do servidor — se esta tela
-- mentir, o servidor simplesmente ignora.
--
-- MOBILE-FIRST (docs/ROBLOX.md §4, lição reprovada no teste do Diretor):
--   · alvo de toque de 64dp (mínimo do GDD é 48dp), botão que NÃO se mexe
--     embaixo do dedo;
--   · zona livre: o alto é da raia D ("ArkanaHud"), o canto inferior direito
--     é da raia C ("ArkanaControls") e o inferior esquerdo é do thumbstick
--     nativo. Sobra a FAIXA CENTRAL ESQUERDA — é onde esta UI mora.
--
-- ACESSIBILIDADE (GDD §10): a canalização não é só cor. É barra que enche +
-- texto + um marco BLOCKY no mundo, visível a todos (é o telegrama que dá a
-- chance de interromper, exigida pelo GDD §9).
-- ============================================================================

local ReplicatedStorage = game:GetService("ReplicatedStorage")
local Workspace = game:GetService("Workspace")
local Debris = game:GetService("Debris")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Elements = require(Shared:WaitForChild("Elements"))
local Grid = require(Shared:WaitForChild("Grid"))
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
local DANGER = Color3.fromHex("#E04040")

--- cor do TIME (a mesma do parceiro no HUD) — o pacto é da DUPLA, não do
--- elemento. Manter idêntica à `ALLY` de Hud.luau.
local ALLY = Color3.fromHex("#FF5CE1")

-- ------------------------------------------------------------------ layout (dp)
local L = {
	ping = 64, -- 64dp > 48dp do GDD: é o alvo de dedo mais crítico da tela
	pingX = 16,
	cardW = 262,
	cardH = 152,
	--- os dois alvos de dedo do convite. 56dp e 52dp > 48dp do GDD.
	acceptH = 56,
	declineW = 52,
	badge = 30,
	cardGap = 12,
	bannerTop = 166, -- logo abaixo do anúncio do HUD (raia D usa até ~158)
	bannerH = 46,
	pad = 8,
}

--- CEGUEIRA DOS COMBOS (GDD §9): Tempestade de Areia e Cortina de Vapor
--- prometem "cegar". O servidor entrega o que ele pode garantir (dano, Reveal,
--- Lentidão); a metade visual é do cliente, porque cegueira é o que ESTA tela
--- mostra e mais nada. É COSMÉTICO — nada de jogo depende dele, então não há
--- nada a burlar aqui: a área e o dano continuam sendo do servidor.
--- CALIBRAÇÃO: `blindLife` é o knob se o véu incomodar no playtest.
local BLIND: { [string]: boolean } = {
	[Elements.comboKey("Earth", "Wind")] = true, -- Tempestade de Areia
	[Elements.comboKey("Fire", "Water")] = true, -- Cortina de Vapor
}
local BLIND_LIFE = 1.5
--- opacidade de PICO do véu. Multiplicada pela preferência de flashes: quem
--- pediu menos flash NÃO pode ser cegado de verdade (menu de acessibilidade), e
--- em 0 o véu some — a legenda com o nome do combo continua dizendo o que houve.
local BLIND_PEAK = 0.62

--- Distância até onde a canalização de OUTRA dupla ainda me interessa na faixa
--- do topo. Mais longe que isto o telegrama continua no MUNDO (pilar de luz),
--- que é onde ele importa — a faixa é para o que me atinge.
--- CALIBRAÇÃO: é este número se a tela piscar com combo do outro lado do mapa.
local BANNER_RANGE = 140

--- 1 dp → px, mesma régua da raia C (viewport de 640 px ≈ celular pequeno em
--- paisagem). CALIBRAÇÃO: é este número se os botões saírem fora de escala.
local function dpScale(): number
	local cam = Workspace.CurrentCamera
	return math.clamp((if cam then cam.ViewportSize.Y else 640) / 640, 0.85, 2.0)
end

-- ------------------------------------------------------------------ i18n
--- Todo texto vem de Shared/Strings (chaves `sintonia.*`). Nada solto aqui.
local function elementOf(id: any): Elements.Element?
	return if Elements.isValid(id) then (Elements.ById :: any)[id] else nil
end

local function comboName(key: any): string
	local combo = if typeof(key) == "string" then Elements.Combos[key] else nil
	if not combo then
		return Strings.t("hud.seal")
	end
	return string.upper(if Strings.getLang() == "pt" then combo.pt else combo.en)
end

--- O combo que ESTES dois elementos fazem. A matriz é do GDD §9 e vive em
--- Shared/Elements — aqui só se pergunta a ela.
local function comboOf(a: any, b: any): Elements.Combo?
	if not (Elements.isValid(a) and Elements.isValid(b)) then
		return nil
	end
	return Elements.getCombo(a, b)
end

local function comboColor(key: any): Color3
	local combo = if typeof(key) == "string" then Elements.Combos[key] else nil
	return if combo then combo.color else GOLD
end

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

--- Bloco de FX local (só existe neste cliente — não replica, não colide e
--- nunca entra em raycast/consulta espacial do servidor).
local function fxPart(parent: Instance, size: Vector3, pos: Vector3, color: Color3, transparency: number): BasePart
	local p = Instance.new("Part")
	p.Anchored = true
	p.CanCollide = false
	p.CanQuery = false
	p.CanTouch = false
	p.CastShadow = false
	p.Massless = true
	p.Material = Enum.Material.Neon -- o que brilha é magia (GDD §10)
	p.Size = size
	p.Position = pos
	p.Color = color
	p.Transparency = transparency
	p.Parent = parent
	return p
end

-- ============================================================================
local SintoniaUi = {}

local localPlayer: Player? = nil
local gui: ScreenGui? = nil
local blindGui: ScreenGui? = nil
local fxFolder: Folder? = nil
local conns: { RBXScriptConnection } = {}

-- widgets
local pingBtn: TextButton
local card: Frame
local cardTitle: TextLabel
local comboLabel: TextLabel
local badgeA: Frame
local badgeB: Frame
local acceptBtn: TextButton
local declineBtn: TextButton
local banner: Frame
local bannerLabel: TextLabel
local bannerFill: Frame
local veil: Frame

--- meu elemento ativo (Net.StatsUpdate) — sem ele não dá para dizer QUAL combo
--- sai da dupla, que é o que transforma "aceitar?" em decisão informada
local myElement: any = nil
--- recusa é LOCAL: o servidor não tem remote de recusa e a proposta expira
--- sozinha. Recusar não pune, não avisa o outro, não abre tutorial.
local declinedFrom: number? = nil

--- proposta recebida/enviada (o servidor manda; aqui só conta o tempo)
type Proposal = {
	byUserId: number,
	byName: string,
	element: any,
	pos: Vector3,
	left: number,
	total: number,
	withUserId: number?,
}
--- `mine` = eu conjuro ou estou perto o bastante para a faixa do topo valer
type Channel = { comboKey: string, pos: Vector3, radius: number, left: number, total: number, mine: boolean }
--- A JANELA DE 1,5 s (GDD §9): depois do pacto, os dois têm este tempo para
--- conjurar JUNTOS. O servidor é dono do relógio e publica `windowLeft` na fase
--- `window` do Net.SintoniaState; aqui é só a barra que mostra o prazo REAL em
--- vez de um palpite do cliente.
type Window = { left: number, total: number, mine: boolean }

local proposal: Proposal? = nil
local channel: Channel? = nil
local window: Window? = nil
local flashLeft = 0
local veilLeft = 0
local veilPeak = 0
--- espelho COSMÉTICO do anti-spam de ping. A autoridade é o servidor: se este
--- número mentir, o ping simplesmente não sai.
local pingLockLeft = 0

-- FX de mundo
local markPart: BasePart? = nil
local beaconRing: BasePart? = nil
local beaconPillar: BasePart? = nil

-- --------------------------------------------------------------------- FX
local function clearBeacon()
	if beaconRing then
		beaconRing:Destroy()
		beaconRing = nil
	end
	if beaconPillar then
		beaconPillar:Destroy()
		beaconPillar = nil
	end
end

local function clearMark()
	if markPart then
		markPart:Destroy()
		markPart = nil
	end
end

--- Marco do alvo pingado: losango flutuante na cor do elemento proposto.
--- Com o pacto fechado ele vira a COR DO TIME e cresce — e os DOIS jogadores
--- veem exatamente o mesmo marco, que é o ponto inteiro do GDD §18.7.
local function showMark(pos: Vector3, element: any, pact: boolean)
	clearMark()
	local folder = fxFolder
	if not folder then
		return
	end
	local e = elementOf(element)
	local color = if pact then ALLY else (if e then e.glow else GOLD)
	local side = if pact then 4 else 3
	local part = fxPart(folder, Vector3.new(side, side, side), pos + Vector3.new(0, 6, 0), color, 0.25)
	part.CFrame = CFrame.new(part.Position) * CFrame.Angles(0, math.rad(45), math.rad(45))
	markPart = part
end

--- Telegrama da canalização (GDD §9): quadrado que ENCHE até o raio do combo
--- + pilar de luz. Blocky de propósito — é a linguagem visual do projeto.
local function showBeacon(pos: Vector3, key: string)
	clearBeacon()
	local folder = fxFolder
	if not folder then
		return
	end
	local color = comboColor(key)
	beaconRing = fxPart(folder, Vector3.new(2, 0.4, 2), Vector3.new(pos.X, Grid.FLOOR_Y + 0.3, pos.Z), color, 0.5)
	beaconPillar = fxPart(folder, Vector3.new(2, 60, 2), Vector3.new(pos.X, Grid.FLOOR_Y + 30, pos.Z), color, 0.6)
end

-- ------------------------------------------------------------------ intenções
local function aimPoint(): Vector3?
	local player = localPlayer
	local char = if player then player.Character else nil
	local root = if char then char:FindFirstChild("HumanoidRootPart") :: BasePart? else nil
	if not root then
		return nil
	end
	--- alvo = 40 studs à frente da CÂMERA, achatado no chão. O servidor prende
	--- ao alcance de uma magia — aqui é só a intenção "ali na minha frente".
	local cam = Workspace.CurrentCamera
	local look = if cam then cam.CFrame.LookVector else root.CFrame.LookVector
	local dir = Vector3.new(look.X, 0, look.Z)
	dir = if dir.Magnitude > 1e-3 then dir.Unit else Vector3.new(0, 0, -1)
	return Vector3.new(root.Position.X, Grid.FLOOR_Y, root.Position.Z) + dir * 40
end

local function sendPropose()
	if pingLockLeft > 0 then
		return
	end
	local pos = aimPoint()
	if not pos then
		return
	end
	pingLockLeft = 2.0
	Net.event(Net.Names.ProposeSintonia):FireServer({ pos = pos })
end

local function sendAccept()
	local p = proposal
	if not p or p.withUserId ~= nil then
		return
	end
	Net.event(Net.Names.AcceptSintonia):FireServer({ fromUserId = p.byUserId })
end

--- Recusar é só sumir com o card. Sem punição, sem aviso ao proponente, sem
--- tutorial: errar o convite não pode custar nada (auditoria de 18/08).
local function sendDecline()
	local p = proposal
	if p then
		declinedFrom = p.byUserId
	end
	card.Visible = false
end

-- ------------------------------------------------------------------ montagem
local function layout()
	local k = dpScale()
	local ping = math.floor(L.ping * k)
	local cardW, cardH = math.floor(L.cardW * k), math.floor(L.cardH * k)

	--- POSIÇÃO pelo A11y: o canônico é a faixa central ESQUERDA (destro, com o
	--- polegar direito nos botões da raia C). No HUD canhoto o par ping+card
	--- espelha para a direita — e chamar `place` de novo a cada relayout mantém
	--- o espelho válido depois de trocar o tamanho da viewport.
	pingBtn.Size = UDim2.fromOffset(ping, ping)
	A11y.place(pingBtn, Vector2.new(0, 0.5), UDim2.new(0, math.floor(L.pingX * k), 0.5, 0))

	card.Size = UDim2.fromOffset(cardW, cardH)
	--- o card encosta EM CIMA do botão de ping: os dois ficam no mesmo alcance
	--- de polegar, e o "ACEITAR" nunca nasce onde o dedo já está apoiado
	A11y.place(
		card,
		Vector2.new(0, 1),
		UDim2.new(0, math.floor(L.pingX * k), 0.5, -math.floor((L.ping / 2 + L.cardGap) * k))
	)
	--- ACEITAR ocupa o resto da largura; o × fica no canto, longe o bastante
	--- para o polegar não confundir os dois
	local decline = math.floor(L.declineW * k)
	declineBtn.Size = UDim2.new(0, decline, 1, 0)
	acceptBtn.Size = UDim2.new(1, -(decline + math.floor(6 * k)), 1, 0)
	local badge = math.floor(L.badge * k)
	badgeA.Size = UDim2.fromOffset(badge, badge)
	badgeB.Size = UDim2.fromOffset(badge, badge)

	banner.Position = UDim2.new(0.5, 0, 0, math.floor(L.bannerTop * k))
	banner.Size = UDim2.new(0.46, 0, 0, math.floor(L.bannerH * k))
end

local function build(root: ScreenGui)
	-- ---------------------------------------------------------- ping "COMBO?"
	pingBtn = mk("TextButton", {
		Name = "Ping",
		AnchorPoint = Vector2.new(0, 0.5),
		BackgroundColor3 = NIGHT,
		BackgroundTransparency = 0.15,
		BorderSizePixel = 0,
		AutoButtonColor = false,
		Font = Enum.Font.GothamBold,
		Text = Strings.t("sintonia.ask"),
		TextColor3 = GOLD,
		TextScaled = true,
	}, root)
	mk("UIStroke", { Color = GOLD, Thickness = 2, Transparency = 0.15 }, pingBtn)
	mk("UITextSizeConstraint", { MaxTextSize = 18 }, pingBtn)

	-- ------------------------------------------------- card da proposta recebida
	card = mk("Frame", {
		Name = "Proposal",
		AnchorPoint = Vector2.new(0, 1),
		BackgroundColor3 = NIGHT,
		BackgroundTransparency = 0.1,
		BorderSizePixel = 0,
		Visible = false,
	}, root)
	mk("UIStroke", { Color = GOLD, Thickness = 2, Transparency = 0.1 }, card)

	local content = mk("Frame", { BackgroundTransparency = 1, Size = UDim2.fromScale(1, 1) }, card)
	mk("UIPadding", {
		PaddingLeft = UDim.new(0, L.pad),
		PaddingRight = UDim.new(0, L.pad),
		PaddingTop = UDim.new(0, L.pad),
		PaddingBottom = UDim.new(0, L.pad),
	}, content)

	cardTitle = mk("TextLabel", {
		BackgroundTransparency = 1,
		Size = UDim2.new(1, 0, 0.12, 0),
		Font = Enum.Font.GothamBold,
		Text = "",
		TextColor3 = TEXT,
		TextScaled = true,
		TextXAlignment = Enum.TextXAlignment.Left,
	}, content)
	mk("UITextSizeConstraint", { MaxTextSize = 15 }, cardTitle)

	--- toda proposta vem com alvo (o servidor prende a posição ao alcance de uma
	--- magia). Dizer isso no card resolve o caso em que o marco está atrás do
	--- jogador: ele aceita sabendo que existe um ponto, e só depois se vira.
	mk(
		"UITextSizeConstraint",
		{ MaxTextSize = 11 },
		mk("TextLabel", {
			BackgroundTransparency = 1,
			Size = UDim2.new(1, 0, 0.12, 0),
			Font = Enum.Font.Gotham,
			Text = Strings.t("sintonia.marked"),
			TextColor3 = GOLD_DIM,
			TextScaled = true,
			TextXAlignment = Enum.TextXAlignment.Right,
		}, content)
	)

	--- OS DOIS ELEMENTOS em cor + FORMA (GDD §10). É o que o jogador confere
	--- antes de apertar: sem eles, "ACEITAR" é aposta no escuro.
	local row = mk("Frame", {
		Name = "Pair",
		BackgroundTransparency = 1,
		Position = UDim2.new(0, 0, 0.16, 0),
		Size = UDim2.new(1, 0, 0.24, 0),
	}, content)
	mk("UIListLayout", {
		FillDirection = Enum.FillDirection.Horizontal,
		VerticalAlignment = Enum.VerticalAlignment.Center,
		Padding = UDim.new(0, 6),
		SortOrder = Enum.SortOrder.LayoutOrder,
	}, row)

	badgeA = Badge.new(row, L.badge)
	badgeA.LayoutOrder = 1
	mk("TextLabel", {
		BackgroundTransparency = 1,
		LayoutOrder = 2,
		Size = UDim2.new(0, 14, 0.7, 0),
		Font = Enum.Font.GothamBold,
		Text = "+",
		TextColor3 = TEXT_DIM,
		TextScaled = true,
	}, row)
	badgeB = Badge.new(row, L.badge)
	badgeB.LayoutOrder = 3

	--- O NOME DO COMBO. É a promessa do toque — "EXPLOSÃO DE PLASMA" ensina a
	--- matriz do GDD §9 sem uma linha de tutorial.
	comboLabel = mk("TextLabel", {
		BackgroundTransparency = 1,
		Position = UDim2.new(0, 0, 0.43, 0),
		Size = UDim2.new(1, 0, 0.15, 0),
		Font = Enum.Font.GothamBlack,
		Text = "",
		TextColor3 = GOLD,
		TextScaled = true,
		TextXAlignment = Enum.TextXAlignment.Left,
	}, content)
	mk("UITextSizeConstraint", { MaxTextSize = 19 }, comboLabel)

	--- ALVOS DE DEDO (56dp e 52dp — o mínimo do GDD é 48dp): ACEITAR toma quase
	--- toda a largura, × fica no canto. Nenhum dos dois muda de tamanho ao ser
	--- tocado (regra da raia C: botão que se mexe embaixo do dedo erra o toque).
	local buttons = mk("Frame", {
		Name = "Actions",
		AnchorPoint = Vector2.new(0, 1),
		BackgroundTransparency = 1,
		Position = UDim2.new(0, 0, 1, 0),
		Size = UDim2.new(1, 0, 0.41, 0),
	}, content)

	acceptBtn = mk("TextButton", {
		Name = "Accept",
		BackgroundColor3 = GOLD,
		BorderSizePixel = 0,
		AutoButtonColor = false,
		Font = Enum.Font.GothamBold,
		Text = Strings.t("sintonia.accept"),
		TextColor3 = NIGHT_DEEP,
		TextScaled = true,
	}, buttons)
	mk("UITextSizeConstraint", { MaxTextSize = 20 }, acceptBtn)

	declineBtn = mk("TextButton", {
		Name = "Decline",
		AnchorPoint = Vector2.new(1, 0),
		Position = UDim2.new(1, 0, 0, 0),
		BackgroundColor3 = NIGHT_DEEP,
		BackgroundTransparency = 0.2,
		BorderSizePixel = 0,
		AutoButtonColor = false,
		Font = Enum.Font.GothamBold,
		Text = Strings.t("sintonia.decline"),
		TextColor3 = TEXT_DIM,
		TextScaled = true,
	}, buttons)
	mk("UIStroke", { Color = GOLD_DIM, Thickness = 1, Transparency = 0.3 }, declineBtn)
	mk("UITextSizeConstraint", { MaxTextSize = 26 }, declineBtn)

	-- ------------------------------------------------- faixa de canalização
	banner = mk("Frame", {
		Name = "Channel",
		AnchorPoint = Vector2.new(0.5, 0),
		BackgroundColor3 = NIGHT,
		BackgroundTransparency = 0.25,
		BorderSizePixel = 0,
		Visible = false,
	}, root)
	mk("UIStroke", { Color = GOLD_DIM, Thickness = 1, Transparency = 0.2 }, banner)

	bannerLabel = mk("TextLabel", {
		BackgroundTransparency = 1,
		Position = UDim2.new(0, 6, 0, 4),
		Size = UDim2.new(1, -12, 0, 20),
		Font = Enum.Font.GothamBold,
		Text = "",
		TextColor3 = GOLD,
		TextScaled = true,
	}, banner)
	mk("UITextSizeConstraint", { MaxTextSize = 20 }, bannerLabel)

	--- a barra é a FORMA da acessibilidade: quem não distingue a cor ainda lê
	--- "falta isto para explodir" (GDD §10)
	local barBg = mk("Frame", {
		BackgroundColor3 = NIGHT_DEEP,
		BorderSizePixel = 0,
		AnchorPoint = Vector2.new(0.5, 1),
		Position = UDim2.new(0.5, 0, 1, -6),
		Size = UDim2.new(1, -12, 0, 8),
	}, banner)
	bannerFill = mk("Frame", {
		BackgroundColor3 = GOLD,
		BorderSizePixel = 0,
		Size = UDim2.fromScale(0, 1),
	}, barBg)
end

--- Véu de cegueira em ScreenGui PRÓPRIA, com DisplayOrder BAIXO de propósito:
--- ele cega o MUNDO, não o HUD. Tapar a própria vida, a mana e os botões da
--- raia C transformaria um efeito tático em "o jogo travou" — e em celular o
--- jogador não tem como conferir de outro jeito.
--- `Active = false`: o véu não engole toque nenhum.
local function buildVeil(root: ScreenGui)
	veil = mk("Frame", {
		Name = "Blind",
		Active = false,
		BackgroundColor3 = Color3.new(1, 1, 1),
		BackgroundTransparency = 1,
		BorderSizePixel = 0,
		Size = UDim2.fromScale(1, 1),
		Visible = false,
	}, root)
	veil:SetAttribute("A11yNoScale", true)
end

--- Estou dentro da área de um combo que CEGA? A área e o raio são os do GDD §9
--- (Shared/Elements) — o cliente não inventa alcance, só pergunta à mesma
--- tabela que o servidor usou para calcular o dano.
local function blindMe(payload: any, key: string)
	local me = localPlayer
	if not me or not BLIND[key] then
		return
	end
	-- os dois conjuradores ficam de fora, igual ao dano (Sintonia.luau)
	if payload.byUserId == me.UserId or payload.withUserId == me.UserId then
		return
	end
	local combo = Elements.Combos[key]
	local char = me.Character
	local root = if char then char:FindFirstChild("HumanoidRootPart") else nil
	if not combo or not root or not root:IsA("BasePart") or typeof(payload.pos) ~= "Vector3" then
		return
	end
	if (root.Position - payload.pos).Magnitude > combo.radius then
		return
	end
	veilPeak = BLIND_PEAK * A11y.flash()
	if veilPeak <= 0.01 then
		return -- flashes reduzidos a zero: fica só a legenda, ninguém cega de fato
	end
	veil.BackgroundColor3 = combo.color
	veil.BackgroundTransparency = 1 - veilPeak
	veil.Visible = true
	veilLeft = BLIND_LIFE
end

-- --------------------------------------------------------------- servidor → UI
--- O CONVITE. Card claro: quem chamou, os DOIS elementos (cor + forma), o nome
--- do combo que sai disso e dois alvos de dedo. Nada mais.
local function refreshCard()
	local p = proposal
	local me = localPlayer
	if not p or not me then
		card.Visible = false
		return
	end
	-- proposta minha, já aceita, ou recusada: nada a tocar — só o marco no mundo
	if p.byUserId == me.UserId or p.withUserId ~= nil or declinedFrom == p.byUserId then
		card.Visible = false
		return
	end
	cardTitle.Text = Strings.t("sintonia.proposed", p.byName)
	Badge.paint(badgeA, p.element)
	Badge.paint(badgeB, myElement)

	local combo = comboOf(p.element, myElement)
	if combo then
		comboLabel.Text = comboName(combo.key)
		comboLabel.TextColor3 = combo.color
	elseif Elements.isValid(myElement) then
		--- mesmo elemento não funde (GDD §9). O convite continua aceitável — a
		--- dupla ainda pode trocar de elemento antes de conjurar. O card ensina,
		--- não bloqueia.
		comboLabel.Text = Strings.t("sintonia.same")
		comboLabel.TextColor3 = DANGER
	else
		comboLabel.Text = Strings.t("sintonia.together")
		comboLabel.TextColor3 = TEXT_DIM
	end
	card.Visible = true
end

--- A faixa do topo é para o que me atinge: combo meu, ou perto de mim. O
--- telegrama do MUNDO (pilar de luz) continua global — é ele que dá ao inimigo
--- a chance de interromper, exigida pelo GDD §9.
local function concernsMe(payload: any): boolean
	local me = localPlayer
	if not me then
		return false
	end
	if payload.byUserId == me.UserId or payload.withUserId == me.UserId then
		return true
	end
	local char = me.Character
	local root = if char then char:FindFirstChild("HumanoidRootPart") else nil
	if not root or not root:IsA("BasePart") or typeof(payload.pos) ~= "Vector3" then
		return false
	end
	return (root.Position - payload.pos).Magnitude <= BANNER_RANGE
end

local function onState(payload: any)
	if typeof(payload) ~= "table" then
		return
	end
	local phase = payload.phase

	if phase == "proposed" then
		if typeof(payload.pos) ~= "Vector3" or typeof(payload.byUserId) ~= "number" then
			return
		end
		local ttl = tonumber(payload.ttl) or 6
		local pact = payload.withUserId ~= nil
		proposal = {
			byUserId = payload.byUserId,
			byName = tostring(payload.byName or "?"),
			element = payload.element,
			pos = payload.pos,
			left = ttl,
			total = math.max(ttl, 0.05),
			withUserId = payload.withUserId,
		}
		if not pact then
			declinedFrom = nil -- pingou de novo: o convite volta, sem rancor
		end
		showMark(payload.pos, payload.element, pact)
		refreshCard()

		local me = localPlayer
		if pact then
			--- PACTO FECHADO: os DOIS veem o mesmo marco e a mesma barra até a
			--- hora de conjurar junto.
			bannerLabel.Text = Strings.t("sintonia.together")
			bannerLabel.TextColor3 = ALLY
			bannerFill.BackgroundColor3 = ALLY
			banner.Visible = true
			flashLeft = 0
			if me and (payload.byUserId == me.UserId or payload.withUserId == me.UserId) then
				A11y.caption(Strings.t("sintonia.together"), ALLY, 2.4)
			end
		elseif me and payload.byUserId == me.UserId then
			-- meu ping saiu: confirmação curta, sem roubar a tela
			bannerLabel.Text = Strings.t("sintonia.marked")
			bannerLabel.TextColor3 = GOLD
			bannerFill.BackgroundColor3 = GOLD
			bannerFill.Size = UDim2.fromScale(1, 1)
			banner.Visible = true
			flashLeft = 1.6
		end
	elseif phase == "window" then
		--- A JANELA REAL (1,5 s do GDD §9), com o relógio do SERVIDOR. Fallback
		--- silencioso: se a fase nunca chegar, `window` fica nulo e a faixa
		--- continua no comportamento antigo (barra do pacto) sem um erro sequer.
		local left = tonumber(payload.windowLeft)
		if not left or left <= 0 then
			return
		end
		--- o servidor pode reemitir a fase com o prazo já correndo: o TOTAL é o
		--- maior valor visto, senão a barra reiniciaria cheia no meio da janela.
		local w = window
		local total = if w and left <= w.total then w.total else math.max(left, 0.05)
		--- `mine` é SEMPRE verdadeiro aqui: o servidor manda esta fase só para a
		--- audiência da dupla (conjurador + aliados). Refazer o teste de
		--- distância só criaria o caso em que a janela chega e a barra não sobe.
		window = { left = left, total = total, mine = true }
		bannerLabel.Text = Strings.t("sintonia.together")
		bannerLabel.TextColor3 = ALLY
		bannerFill.BackgroundColor3 = ALLY
		banner.Visible = true
		flashLeft = 0
		-- legenda só na ABERTURA da janela: reemissão não repete o aviso
		if not w then
			A11y.caption(Strings.t("sintonia.together"), ALLY, math.min(left + 0.4, 2.4))
		end
	elseif phase == "channeling" then
		if typeof(payload.pos) ~= "Vector3" or typeof(payload.comboKey) ~= "string" then
			return
		end
		local total = tonumber(payload.duration) or 1
		local mine = concernsMe(payload)
		channel = {
			comboKey = payload.comboKey,
			pos = payload.pos,
			radius = tonumber(payload.radius) or 12,
			left = total,
			total = math.max(total, 0.05),
			mine = mine,
		}
		window = nil -- a janela cumpriu o papel: agora o relógio é o da canalização
		showBeacon(payload.pos, payload.comboKey)
		if mine then
			bannerLabel.Text = Strings.t("sintonia.channeling")
			bannerLabel.TextColor3 = comboColor(payload.comboKey)
			bannerFill.BackgroundColor3 = comboColor(payload.comboKey)
			banner.Visible = true
			flashLeft = 0
			A11y.caption(Strings.t("sintonia.channeling"), comboColor(payload.comboKey), total + 0.4)
		end
	elseif phase == "fired" then
		channel = nil
		window = nil
		clearBeacon()
		if typeof(payload.comboKey) == "string" then
			blindMe(payload, payload.comboKey)
		end
		--- o pacto só se encerra para QUEM conjurou: o combo de outra dupla não
		--- pode apagar o alvo que eu marquei com o meu aliado
		local me = localPlayer
		if me and (payload.byUserId == me.UserId or payload.withUserId == me.UserId) then
			clearMark()
			proposal = nil
			refreshCard()
		end
		if concernsMe(payload) then
			bannerLabel.Text = comboName(payload.comboKey)
			bannerLabel.TextColor3 = comboColor(payload.comboKey)
			bannerFill.BackgroundColor3 = comboColor(payload.comboKey)
			bannerFill.Size = UDim2.fromScale(1, 1)
			banner.Visible = true
			flashLeft = 2.4
			A11y.caption(comboName(payload.comboKey), comboColor(payload.comboKey), 2.4)
		end
	elseif phase == "failed" then
		channel = nil
		window = nil
		clearBeacon()
		if concernsMe(payload) then
			bannerLabel.Text = Strings.t("sintonia.broken")
			bannerLabel.TextColor3 = DANGER
			bannerFill.BackgroundColor3 = DANGER
			bannerFill.Size = UDim2.fromScale(0, 1)
			banner.Visible = true
			flashLeft = 2.0
			A11y.caption(Strings.t("sintonia.broken"), DANGER, 2.0)
		end
	end
end

-- ------------------------------------------------------------------- API
function SintoniaUi.init(player: Player)
	if gui then
		return
	end
	localPlayer = player
	Strings.followPlayerLocale()

	local playerGui = player:WaitForChild("PlayerGui")
	local root = mk("ScreenGui", {
		Name = "ArkanaSintonia", -- raia C usa "ArkanaControls", raia D "ArkanaHud"
		ResetOnSpawn = false,
		IgnoreGuiInset = false,
		DisplayOrder = 6, -- acima do HUD e dos botões: o aceite não pode ser tapado
		ZIndexBehavior = Enum.ZIndexBehavior.Sibling,
	}, playerGui)
	gui = root

	local folder = Instance.new("Folder")
	folder.Name = "ArkanaSintoniaFx" -- criado no cliente: existe só aqui
	folder.Parent = Workspace
	fxFolder = folder

	local blind = mk("ScreenGui", {
		Name = "ArkanaBlind",
		ResetOnSpawn = false,
		IgnoreGuiInset = true, -- cegueira cobre a tela toda, inclusive a barra
		DisplayOrder = 1, -- ABAIXO do HUD (4), dos botões (5) e desta UI (6)
		ZIndexBehavior = Enum.ZIndexBehavior.Sibling,
	}, playerGui)
	blindGui = blind

	build(root)
	buildVeil(blind)
	A11y.attach(root)
	layout()

	table.insert(conns, pingBtn.Activated:Connect(sendPropose))
	table.insert(conns, acceptBtn.Activated:Connect(sendAccept))
	table.insert(conns, declineBtn.Activated:Connect(sendDecline))
	table.insert(conns, Net.event(Net.Names.SintoniaState).OnClientEvent:Connect(onState))

	--- meu elemento ativo é do servidor (Net.StatsUpdate). Trocar de elemento
	--- com o card aberto REESCREVE o nome do combo na hora — é assim que o
	--- jogador descobre a matriz do GDD §9 sem tutorial.
	table.insert(
		conns,
		Net.event(Net.Names.StatsUpdate).OnClientEvent:Connect(function(p: any)
			if typeof(p) == "table" and Elements.isValid(p.element) and p.element ~= myElement then
				myElement = p.element
				if card.Visible then
					refreshCard()
				end
			end
		end)
	)

	--- ARREPIO DO ESPÍRITO (GDD §18.6) — costura do coordenador, 18/08.
	--- O servidor já mandava este remote para o caído e para a dupla, e NENHUM
	--- módulo do cliente escutava: o morto apertava o botão, o servidor fazia
	--- tudo certo e a tela não mostrava nada. Era o único recurso de quem caiu.
	--- Desenho: coluna curta na cor do time, no lugar denunciado, que apaga
	--- sozinha em `hold` segundos (o servidor manda a duração).
	table.insert(
		conns,
		Net.event(Net.Names.SpiritPing).OnClientEvent:Connect(function(p: any)
			if typeof(p) ~= "table" or typeof(p.pos) ~= "Vector3" or not fxFolder then
				return
			end
			local hold = math.clamp(tonumber(p.hold) or 2, 0.5, 8)
			local pos = p.pos :: Vector3
			local mark = fxPart(fxFolder, Vector3.new(1.6, 26, 1.6), pos + Vector3.new(0, 13, 0), ALLY, 0.35)
			local base = fxPart(fxFolder, Vector3.new(7, 0.4, 7), pos + Vector3.new(0, 0.3, 0), ALLY, 0.5)
			Debris:AddItem(mark, hold)
			Debris:AddItem(base, hold)
		end)
	)

	local cam = Workspace.CurrentCamera
	if cam then
		table.insert(conns, cam:GetPropertyChangedSignal("ViewportSize"):Connect(layout))
	end
end

function SintoniaUi.update(dt: number)
	if not gui then
		return
	end

	-- ping: o botão apaga durante o anti-spam (feedback imediato ao toque)
	if pingLockLeft > 0 then
		pingLockLeft -= dt
		pingBtn.BackgroundTransparency = 0.6
		pingBtn.TextTransparency = 0.5
	else
		pingBtn.BackgroundTransparency = 0.15
		pingBtn.TextTransparency = 0
	end

	-- proposta/pacto expiram sozinhos (o servidor tem o mesmo prazo — aqui é a tela)
	local p = proposal
	if p then
		p.left -= dt
		if p.left <= 0 then
			proposal = nil
			p = nil
			clearMark()
			refreshCard()
		end
	end

	-- marco do alvo: flutua e gira devagar (leitura à distância)
	local mark = markPart
	if mark then
		mark.CFrame = CFrame.new(mark.Position) * CFrame.Angles(0, os.clock() * 1.5, math.rad(45))
	end

	-- canalização: o quadrado do chão cresce até o raio do combo. TODOS veem,
	-- inimigo inclusive — é o telegrama que permite interromper (GDD §9).
	local ch = channel
	if ch then
		ch.left -= dt
		local k = math.clamp(1 - ch.left / ch.total, 0, 1)
		local ring = beaconRing
		if ring then
			local side = math.max(2, ch.radius * 2 * k)
			ring.Size = Vector3.new(side, 0.4, side)
			ring.Transparency = 0.6 - 0.3 * k
		end
		local pillar = beaconPillar
		if pillar then
			--- "reduzir flashes": o pilar continua LÁ (é o telegrama que dá ao
			--- inimigo a chance de interromper, GDD §9) — só para de pulsar.
			pillar.Transparency = 0.75 - 0.35 * math.abs(math.sin(os.clock() * 9)) * A11y.flash()
		end
		--- rede de borracha: se o "fired"/"failed" do servidor não chegar, a
		--- tela não pode ficar travada em canalização eterna
		if ch.left < -2 then
			channel = nil
			ch = nil
			clearBeacon()
			banner.Visible = false
		end
	end

	-- véu de cegueira: abre no pico e clareia. Quadrático porque o fim precisa
	-- devolver a visão rápido — cegueira longa vira punição, não telegrama.
	if veilLeft > 0 then
		veilLeft -= dt
		if veilLeft <= 0 then
			veil.Visible = false
			veil.BackgroundTransparency = 1
		else
			local k = veilLeft / BLIND_LIFE
			veil.BackgroundTransparency = 1 - veilPeak * k * k
		end
	end

	-- janela de 1,5 s: o servidor manda o prazo, aqui só corre o relógio entre
	-- pacotes. Expirou sem canalizar = a dupla perdeu a janela e a barra some.
	local w = window
	if w then
		w.left -= dt
		if w.left <= 0 then
			window = nil
			w = nil
		end
	end

	-- faixa do topo, por prioridade: canalização que me atinge > janela de
	-- conjuração > pacto aberto > último anúncio apagando.
	if ch and ch.mine then
		bannerFill.Size = UDim2.fromScale(math.clamp(1 - ch.left / ch.total, 0, 1), 1)
	elseif w and w.mine then
		bannerFill.Size = UDim2.fromScale(math.clamp(w.left / w.total, 0, 1), 1)
	elseif p and p.withUserId ~= nil then
		bannerFill.Size = UDim2.fromScale(math.clamp(p.left / p.total, 0, 1), 1)
	elseif flashLeft > 0 then
		flashLeft -= dt
		if flashLeft <= 0 then
			banner.Visible = false
		end
	elseif banner.Visible then
		banner.Visible = false
	end
end

--- Some com tudo (GUI, parts, conexões): nada vaza entre respawns.
function SintoniaUi.destroy()
	for _, c in ipairs(conns) do
		c:Disconnect()
	end
	table.clear(conns)
	clearBeacon()
	clearMark()
	if fxFolder then
		fxFolder:Destroy()
		fxFolder = nil
	end
	if gui then
		gui:Destroy()
		gui = nil
	end
	if blindGui then
		blindGui:Destroy()
		blindGui = nil
	end
	proposal = nil
	channel = nil
	window = nil
	myElement = nil
	declinedFrom = nil
	veilLeft, veilPeak = 0, 0
end

return SintoniaUi
