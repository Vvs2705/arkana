--!strict
-- ============================================================================
-- RAIA R2 (cliente) — UI DA SINTONIA: Ping "COMBO?", aceite em 1 toque,
-- canalização e o nome do combo executado. GDD §9 + §18.7.
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

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Elements = require(Shared:WaitForChild("Elements"))
local Grid = require(Shared:WaitForChild("Grid"))
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

-- ------------------------------------------------------------------ layout (dp)
local L = {
	ping = 64, -- 64dp > 48dp do GDD: é o alvo de dedo mais crítico da tela
	pingX = 16,
	cardW = 244,
	cardH = 104,
	acceptH = 56,
	cardGap = 12,
	bannerTop = 166, -- logo abaixo do anúncio do HUD (raia D usa até ~158)
	bannerH = 46,
	pad = 8,
}

--- 1 dp → px, mesma régua da raia C (viewport de 640 px ≈ celular pequeno em
--- paisagem). CALIBRAÇÃO: é este número se os botões saírem fora de escala.
local function dpScale(): number
	local cam = Workspace.CurrentCamera
	return math.clamp((if cam then cam.ViewportSize.Y else 640) / 640, 0.85, 2.0)
end

-- --------------------------------------------------- textos locais (i18n)
--- ponytail: estas chaves pertencem a Shared/Strings, mas aquele arquivo é de
--- outra raia nesta missão. Quando o coordenador abrir Strings.luau, mover
--- este bloco para lá e trocar por Strings.t() — a assinatura já é a mesma.
local UI = {
	pt = {
		ask = "COMBO?",
		accept = "ACEITAR",
		proposed = "%s quer SINTONIA",
		with = "Elemento: %s",
		marked = "ALVO MARCADO",
		channeling = "CANALIZANDO SINTONIA",
		broken = "SINTONIA INTERROMPIDA",
	},
	en = {
		ask = "COMBO?",
		accept = "ACCEPT",
		proposed = "%s wants ATTUNEMENT",
		with = "Element: %s",
		marked = "TARGET MARKED",
		channeling = "CHANNELING ATTUNEMENT",
		broken = "ATTUNEMENT BROKEN",
	},
}

local function t(key: string, ...): string
	local dict = (UI :: any)[Strings.getLang()] or UI.pt
	local s = dict[key] or key
	if select("#", ...) == 0 then
		return s
	end
	local ok, formatted = pcall(string.format, s, ...)
	return ok and formatted or s
end

local function elementOf(id: any): Elements.Element?
	return if Elements.isValid(id) then (Elements.ById :: any)[id] else nil
end

local function elementName(id: any): string
	local e = elementOf(id)
	if not e then
		return "?"
	end
	return if Strings.getLang() == "pt" then e.pt else e.en
end

local function comboName(key: any): string
	local combo = if typeof(key) == "string" then Elements.Combos[key] else nil
	if not combo then
		return "SINTONIA"
	end
	return string.upper(if Strings.getLang() == "pt" then combo.pt else combo.en)
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
local fxFolder: Folder? = nil
local conns: { RBXScriptConnection } = {}

-- widgets
local pingBtn: TextButton
local card: Frame
local cardTitle: TextLabel
local cardSub: TextLabel
local acceptBtn: TextButton
local banner: Frame
local bannerLabel: TextLabel
local bannerFill: Frame

--- proposta recebida/enviada (o servidor manda; aqui só conta o tempo)
type Proposal = { byUserId: number, byName: string, element: any, pos: Vector3, left: number, withUserId: number? }
type Channel = { comboKey: string, pos: Vector3, radius: number, left: number, total: number }

local proposal: Proposal? = nil
local channel: Channel? = nil
local flashLeft = 0
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
local function showMark(pos: Vector3, element: any)
	clearMark()
	local folder = fxFolder
	if not folder then
		return
	end
	local e = elementOf(element)
	local part = fxPart(folder, Vector3.new(3, 3, 3), pos + Vector3.new(0, 6, 0), if e then e.glow else GOLD, 0.25)
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

-- ------------------------------------------------------------------ montagem
local function layout()
	local k = dpScale()
	local ping = math.floor(L.ping * k)
	local cardW, cardH = math.floor(L.cardW * k), math.floor(L.cardH * k)

	pingBtn.Size = UDim2.fromOffset(ping, ping)
	pingBtn.Position = UDim2.new(0, math.floor(L.pingX * k), 0.5, 0)

	card.Size = UDim2.fromOffset(cardW, cardH)
	--- o card encosta EM CIMA do botão de ping: os dois ficam no mesmo alcance
	--- de polegar, e o "ACEITAR" nunca nasce onde o dedo já está apoiado
	card.Position = UDim2.new(0, math.floor(L.pingX * k), 0.5, -math.floor((L.ping / 2 + L.cardGap) * k))
	acceptBtn.Size = UDim2.new(1, 0, 0, math.floor(L.acceptH * k))

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
		Text = t("ask"),
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
		Size = UDim2.new(1, 0, 0, 16),
		Font = Enum.Font.GothamBold,
		Text = "",
		TextColor3 = TEXT,
		TextScaled = true,
		TextXAlignment = Enum.TextXAlignment.Left,
	}, content)
	mk("UITextSizeConstraint", { MaxTextSize = 15 }, cardTitle)

	cardSub = mk("TextLabel", {
		BackgroundTransparency = 1,
		Position = UDim2.new(0, 0, 0, 18),
		Size = UDim2.new(1, 0, 0, 13),
		Font = Enum.Font.Gotham,
		Text = "",
		TextColor3 = TEXT_DIM,
		TextScaled = true,
		TextXAlignment = Enum.TextXAlignment.Left,
	}, content)
	mk("UITextSizeConstraint", { MaxTextSize = 13 }, cardSub)

	--- ALVO DE DEDO PRINCIPAL: 56dp de altura, largura cheia do card. Não muda
	--- de tamanho ao ser tocado (regra da raia C: botão que se mexe erra o toque).
	acceptBtn = mk("TextButton", {
		Name = "Accept",
		AnchorPoint = Vector2.new(0, 1),
		Position = UDim2.new(0, 0, 1, 0),
		BackgroundColor3 = GOLD,
		BorderSizePixel = 0,
		AutoButtonColor = false,
		Font = Enum.Font.GothamBold,
		Text = t("accept"),
		TextColor3 = NIGHT_DEEP,
		TextScaled = true,
	}, content)
	mk("UITextSizeConstraint", { MaxTextSize = 20 }, acceptBtn)

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

-- --------------------------------------------------------------- servidor → UI
local function refreshCard()
	local p = proposal
	local me = localPlayer
	if not p or not me then
		card.Visible = false
		return
	end
	-- proposta minha, ou já aceita: nada a tocar — só o marco no mundo
	if p.byUserId == me.UserId or p.withUserId ~= nil then
		card.Visible = false
		return
	end
	cardTitle.Text = t("proposed", p.byName)
	cardSub.Text = t("with", elementName(p.element))
	card.Visible = true
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
		proposal = {
			byUserId = payload.byUserId,
			byName = tostring(payload.byName or "?"),
			element = payload.element,
			pos = payload.pos,
			left = tonumber(payload.ttl) or 6,
			withUserId = payload.withUserId,
		}
		showMark(payload.pos, payload.element)
		refreshCard()
		if payload.withUserId ~= nil then
			bannerLabel.Text = t("marked")
			bannerFill.Size = UDim2.fromScale(1, 1)
			banner.Visible = true
			flashLeft = 1.6
		end
	elseif phase == "channeling" then
		if typeof(payload.pos) ~= "Vector3" or typeof(payload.comboKey) ~= "string" then
			return
		end
		local total = tonumber(payload.duration) or 1
		channel = {
			comboKey = payload.comboKey,
			pos = payload.pos,
			radius = tonumber(payload.radius) or 12,
			left = total,
			total = math.max(total, 0.05),
		}
		bannerLabel.Text = t("channeling")
		bannerLabel.TextColor3 = comboColor(payload.comboKey)
		banner.Visible = true
		flashLeft = 0
		showBeacon(payload.pos, payload.comboKey)
	elseif phase == "fired" then
		channel = nil
		clearBeacon()
		--- o pacto só se encerra para QUEM conjurou: o combo de outra dupla não
		--- pode apagar o alvo que eu marquei com o meu aliado
		local me = localPlayer
		if me and (payload.byUserId == me.UserId or payload.withUserId == me.UserId) then
			clearMark()
			proposal = nil
			refreshCard()
		end
		bannerLabel.Text = comboName(payload.comboKey)
		bannerLabel.TextColor3 = comboColor(payload.comboKey)
		bannerFill.Size = UDim2.fromScale(1, 1)
		banner.Visible = true
		flashLeft = 2.4
	elseif phase == "failed" then
		channel = nil
		clearBeacon()
		bannerLabel.Text = t("broken")
		bannerLabel.TextColor3 = DANGER
		bannerFill.Size = UDim2.fromScale(0, 1)
		banner.Visible = true
		flashLeft = 2.0
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

	build(root)
	layout()

	table.insert(conns, pingBtn.Activated:Connect(sendPropose))
	table.insert(conns, acceptBtn.Activated:Connect(sendAccept))
	table.insert(conns, Net.event(Net.Names.SintoniaState).OnClientEvent:Connect(onState))

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

	-- proposta expira sozinha (o servidor tem o mesmo prazo — aqui é só a tela)
	local p = proposal
	if p then
		p.left -= dt
		if p.left <= 0 then
			proposal = nil
			clearMark()
			refreshCard()
		end
	end

	-- marco do alvo: flutua e gira devagar (leitura à distância)
	local mark = markPart
	if mark then
		mark.CFrame = CFrame.new(mark.Position) * CFrame.Angles(0, os.clock() * 1.5, math.rad(45))
	end

	-- canalização: barra enche e o quadrado do chão cresce até o raio do combo
	local ch = channel
	if ch then
		ch.left -= dt
		local k = math.clamp(1 - ch.left / ch.total, 0, 1)
		bannerFill.Size = UDim2.fromScale(k, 1)
		local ring = beaconRing
		if ring then
			local side = math.max(2, ch.radius * 2 * k)
			ring.Size = Vector3.new(side, 0.4, side)
			ring.Transparency = 0.6 - 0.3 * k
		end
		local pillar = beaconPillar
		if pillar then
			pillar.Transparency = 0.75 - 0.35 * math.abs(math.sin(os.clock() * 9)) -- pulso
		end
		--- rede de borracha: se o "fired"/"failed" do servidor não chegar, a
		--- tela não pode ficar travada em canalização eterna
		if ch.left < -2 then
			channel = nil
			clearBeacon()
			banner.Visible = false
		end
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
	proposal = nil
	channel = nil
end

return SintoniaUi
