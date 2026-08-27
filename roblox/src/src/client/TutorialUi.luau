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
-- botões de verdade capturam dedo, e todos têm ≥48dp de altura (mínimo do GDD
-- §19.3).
--
-- CONVITE, NÃO IMPOSIÇÃO (R11). Esta tela tem TRÊS estados, e só um deles
-- aparece por vez:
--   "offer"   o convite: duas saídas do MESMO peso visual (mesmo tamanho,
--             mesma cor, mesma borda — nenhum botão "primário", porque
--             onboarding que envergonha quem pula é onboarding obrigatório
--             com outro nome). Uma vez por jogador, no lobby.
--   "reopen"  só o botão TREINAR, no MESMO slot do PULAR (topo-centro) — um
--             slot que já existia e que fica VAZIO fora do Campo de Provas.
--             Zero pixel novo na tela de quem está jogando; e o servidor só
--             manda este estado no lobby.
--   "step"    o roteiro de sempre.
--
-- PROTOCOLO: contraparte de server/Tutorial.luau. Os dois lados têm autoteste
-- sobre os MESMOS literais — foi um contrato de payload em desacordo entre
-- estes dois arquivos que travou o jogo inteiro em 18/08.
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
	--- partida (direita), e longe do polegar que mira. O MESMO botão vira
	--- TREINAR (`tutorial.reopen`) fora do Campo de Provas — os dois estados
	--- nunca coexistem, então é um slot só, não dois.
	skipW = 140,
	skipH = 48, -- mínimo do GDD §19.3
	skipTop = 14,
	enterW = 300,
	enterH = 72,
	--- CONVITE: as duas saídas têm o MESMO tamanho, de propósito
	offerW = 460,
	offerTitleH = 54,
	offerSubH = 26,
	offerBtnW = 210,
	offerBtnH = 72,
	offerGap = 18,
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

--- S→C que NÃO é frase do roteiro. Todo passo que o servidor manda tem de cair
--- aqui ou em TEXT_KEY; o autoteste percorre a lista inteira e reprova se
--- sobrar um passo sem tratamento — que é exatamente a falha (payload que um
--- lado manda e o outro não entende) que travou o jogo em 18/08.
local SPECIAL: { [string]: string } = {
	offer = "offer",
	reopen = "reopen",
	["end"] = "hide",
	lead = "lead",
}

--- Payload S→C → o que fazer com a tela. Pura: é o que o autoteste interroga.
local function modeFor(payload: any): string?
	if typeof(payload) ~= "table" then
		return nil
	end
	local s = (payload :: any).step
	if typeof(s) ~= "string" then
		return nil
	end
	return if TEXT_KEY[s] then "step" else SPECIAL[s]
end

--- OS TRÊS PAYLOADS C→S, num lugar só — e é ESTA função que os botões chamam,
--- então o autoteste interroga o mesmo código que o dedo do jogador aciona.
--- Contraparte: a tabela `INTENT` de server/Tutorial.luau.
local SEND: { [string]: string } = { train = "accept", play = "decline", leave = "skip" }
local function intent(kind: string): { [string]: any }
	return { step = assert(SEND[kind], "intenção desconhecida: " .. kind) }
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
local offerBox: Frame
local offerTitle: TextLabel
local offerSub: TextLabel
local trainBtn: TextButton
local playBtn: TextButton
local pingHint: Frame
local pingStroke: UIStroke

--- estado local (só apresentação)
local step = ""
--- "" · "offer" · "reopen" · "step" — quem manda é o servidor
local mode = ""
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

	local bw, bh = math.floor(L.offerBtnW * k), math.floor(L.offerBtnH * k)
	local gap = math.floor(L.offerGap * k)
	offerBox.Size = UDim2.fromOffset(
		math.max(math.floor(L.offerW * k), bw * 2 + gap),
		math.floor((L.offerTitleH + L.offerSubH + L.offerGap) * k) + bh
	)
	offerTitle.Size = UDim2.new(1, 0, 0, math.floor(L.offerTitleH * k))
	offerSub.Position = UDim2.new(0, 0, 0, math.floor(L.offerTitleH * k))
	offerSub.Size = UDim2.new(1, 0, 0, math.floor(L.offerSubH * k))
	-- MESMO tamanho para os dois: o peso visual é a mensagem
	trainBtn.Size = UDim2.fromOffset(bw, bh)
	playBtn.Size = UDim2.fromOffset(bw, bh)
	trainBtn.Position = UDim2.new(0.5, -math.floor(gap / 2), 1, 0)
	playBtn.Position = UDim2.new(0.5, math.floor(gap / 2), 1, 0)
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
	mode = "step"
	local key = TEXT_KEY[step]
	msgLabel.Text = Strings.t(key)
	--- a dica de elemento só existe quando o elemento na mão NÃO resolve o
	--- passo (o servidor é quem sabe) — dica que mente é pior que dica ausente
	local name = if payload.needSwitch == true then elementName(payload.element) else ""
	subLabel.Text = if name ~= "" then Strings.t("tutorial.switch", name) else ""
	subLabel.Visible = name ~= ""

	msg.Visible = true
	skipBtn.Text = Strings.t("tutorial.skip")
	skipBtn.Visible = step ~= "done"
	pingHint.Visible = step == "combo"
	offerBox.Visible = false
	enterBtn.Visible = step == "done"
	overlay.Visible = step == "done"
end

local function hide()
	step = ""
	mode = ""
	msg.Visible = false
	skipBtn.Visible = false
	pingHint.Visible = false
	overlay.Visible = false
	offerBox.Visible = false
	clearLead()
end

--- O CONVITE. Escondendo tudo o mais: a pergunta é a única coisa na tela.
local function showOffer()
	hide()
	mode = "offer"
	offerTitle.Text = Strings.t("tutorial.offer")
	offerSub.Text = Strings.t("tutorial.offer.sub")
	trainBtn.Text = Strings.t("tutorial.offer.yes")
	playBtn.Text = Strings.t("tutorial.offer.no")
	offerBox.Visible = true
	enterBtn.Visible = false
	overlay.Visible = true
end

--- A PORTA DE VOLTA de quem disse "JOGAR AGORA": o mesmo slot do PULAR, com o
--- texto trocado. O servidor só manda isto no lobby.
local function showReopen()
	hide()
	mode = "reopen"
	skipBtn.Text = Strings.t("tutorial.reopen")
	skipBtn.Visible = true
end

local function onServer(payload: any)
	local kind = modeFor(payload)
	if kind == "lead" then
		if typeof((payload :: any).pos) == "Vector3" then
			showLead((payload :: any).pos)
		end
	elseif kind == "hide" then
		hide()
	elseif kind == "offer" then
		showOffer()
	elseif kind == "reopen" then
		showReopen()
	elseif kind == "step" then
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

	-- ------------------------------------------------- PULAR / TREINAR
	--- Respeita quem já sabe jogar: um toque, sem confirmação, sem "tem
	--- certeza?". O servidor decide o que fazer com isso.
	--- O MESMO botão serve de porta de volta ao Campo de Provas
	--- (`tutorial.reopen`) fora de uma sessão: os dois usos nunca coexistem, e
	--- reaproveitar o slot é o que mantém a tela sem widget novo.
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

	-- ------------------------------------------------------------ O CONVITE
	--- Mora no MESMO overlay do fim do roteiro (nenhum painel novo foi criado):
	--- os dois estados são excludentes e o overlay já existia.
	offerBox = mk("Frame", {
		Name = "Offer",
		Active = false,
		AnchorPoint = Vector2.new(0.5, 0.5),
		Position = UDim2.fromScale(0.5, 0.5),
		BackgroundTransparency = 1,
		Visible = false,
	}, overlay)

	offerTitle = mk("TextLabel", {
		BackgroundTransparency = 1,
		Font = Enum.Font.GothamBold,
		Text = "",
		TextColor3 = TEXT,
		TextScaled = true,
		TextStrokeColor3 = NIGHT_DEEP,
		TextStrokeTransparency = 0.4,
	}, offerBox)
	mk("UITextSizeConstraint", { MaxTextSize = 34 }, offerTitle)

	offerSub = mk("TextLabel", {
		BackgroundTransparency = 1,
		Font = Enum.Font.Gotham,
		Text = "",
		TextColor3 = TEXT_DIM,
		TextScaled = true,
	}, offerBox)
	mk("UITextSizeConstraint", { MaxTextSize = 18 }, offerSub)

	--- AS DUAS SAÍDAS SÃO IGUAIS: mesmo tamanho, mesma cor de fundo, mesma
	--- borda, mesma cor de texto, mesma fonte. Nenhuma é "primária" e nenhuma é
	--- "cancelar" — a diferença entre elas é só o que dizem. Onboarding que
	--- pinta PULAR de cinza e TREINAR de dourado é onboarding obrigatório com
	--- outro nome, e foi disso que o Diretor reclamou.
	local function choice(name: string, anchorX: number): TextButton
		local b = mk("TextButton", {
			Name = name,
			AnchorPoint = Vector2.new(anchorX, 1),
			BackgroundColor3 = NIGHT,
			BackgroundTransparency = 0.15,
			AutoButtonColor = true,
			BorderSizePixel = 0,
			Font = Enum.Font.GothamBold,
			Text = "",
			TextColor3 = TEXT,
			TextScaled = true,
		}, offerBox)
		mk("UICorner", { CornerRadius = UDim.new(0, 12) }, b)
		mk("UIStroke", { Color = GOLD, Thickness = 2, Transparency = 0.25 }, b)
		mk("UITextSizeConstraint", { MaxTextSize = 24 }, b)
		return b
	end
	trainBtn = choice("Train", 1)
	playBtn = choice("Play", 0)
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
		ev:FireServer(intent("leave"))
		hide()
	end
	--- O botão do topo-centro é DOIS botões em um slot só: dentro do Campo de
	--- Provas ele é PULAR; fora dele (estado "reopen") é TREINAR, e aí a
	--- intenção é a OPOSTA. Quem some a tela é o servidor, com o passo 1.
	table.insert(
		conns,
		skipBtn.Activated:Connect(function()
			if mode == "reopen" then
				ev:FireServer(intent("train"))
				skipBtn.Visible = false
			else
				leave()
			end
		end)
	)
	table.insert(conns, enterBtn.Activated:Connect(leave))
	table.insert(
		conns,
		trainBtn.Activated:Connect(function()
			ev:FireServer(intent("train"))
			hide() -- o servidor responde com o passo 1; até lá, tela limpa
		end)
	)
	table.insert(
		conns,
		playBtn.Activated:Connect(function()
			ev:FireServer(intent("play"))
			hide() -- o servidor responde com o botão TREINAR do lobby
		end)
	)
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

-- ============================================================================
-- AUTOTESTE — cole no Command Bar do Studio (cliente):
--   require(game.StarterPlayer.StarterPlayerScripts.Client.TutorialUi).selfTest()
-- É a METADE DE CLIENTE do contrato com server/Tutorial.luau. Puro: não toca em
-- widget, não manda nada para o servidor, não depende de `init`.
-- POR QUE ELE EXISTE: em 18/08 o servidor esperava um payload e o cliente
-- mandava outro nos DOIS botões. Ninguém saía do onboarding, o lobby travava e
-- NENHUMA PARTIDA COMEÇAVA — em nenhum servidor. Revisão não pegou; teste
-- pegou. Toda mensagem nova entre estes dois arquivos passa por aqui.
-- ============================================================================
function TutorialUi.selfTest(): boolean
	-- S→C: todo passo que o servidor manda tem de ter tratamento AQUI
	local fromServer = { "aim", "targets", "burn", "freeze", "combo", "done", "offer", "reopen", "end", "lead" }
	for _, s in ipairs(fromServer) do
		assert(modeFor({ step = s }) ~= nil, "o cliente não sabe o que fazer com o passo '" .. s .. "' que o servidor manda")
	end
	assert(modeFor({ step = "offer" }) == "offer", "o convite tem de abrir a escolha, não o roteiro")
	assert(modeFor({ step = "reopen" }) == "reopen", "a porta de volta é só o botão, não o roteiro")
	assert(modeFor(nil) == nil and modeFor("offer") == nil and modeFor({}) == nil, "payload inválido não mexe na tela")

	-- C→S: os literais que `server/Tutorial.luau` (tabela INTENT) espera
	assert(intent("train").step == "accept", "TREINAR tem de chegar ao servidor como aceitação")
	assert(intent("play").step == "decline", "JOGAR AGORA tem de chegar ao servidor como recusa")
	assert(intent("leave").step == "skip", "PULAR/ENTRAR NO ROYALE continuam sendo a mesma saída")

	-- nenhum texto solto: chave que falta apareceria CRUA na tela do jogador
	local keys = {
		"tutorial.offer",
		"tutorial.offer.sub",
		"tutorial.offer.yes",
		"tutorial.offer.no",
		"tutorial.reopen",
		"tutorial.skip",
		"tutorial.enter",
		"tutorial.lead",
	}
	for _, key in ipairs(keys) do
		assert(Strings.t(key) ~= key, "chave de texto ausente (sairia crua na tela): " .. key)
	end
	for _, key in pairs(TEXT_KEY) do
		assert(Strings.t(key) ~= key, "chave de texto ausente (sairia crua na tela): " .. key)
	end

	print(string.format("[TutorialUi] selfTest OK — %d passos S→C · %d intenções C→S", #fromServer, 3))
	return true
end

return TutorialUi
