--!strict
-- ============================================================================
-- ARKANA (Roblox) — RAIA C · UI DE TOQUE ("ArkanaControls")
--
-- Só desenha e reporta toque. Toda a REGRA do gesto (mirar-arrastando, tap
-- rápido, cancelar) vive em Controls.luau — aqui é pintura e alvo de dedo.
--
-- ESTA TELA SÓ EXISTE NO ESQUEMA DE TOQUE (`Device.schemes()`). Não é "o HUD do
-- celular que o PC também recebe": no PC a mira é o cursor e o disparo é botão
-- de mouse, e um botão de tela ali só rouba pixel e dispara em duplicidade.
-- O ramo de MouseButton1 abaixo é o que mantém estes botões testáveis no
-- emulador de aparelho do Studio — em máquina de verdade ele não é alcançável,
-- porque teclado+mouse juntos já tiraram esta ScreenGui de cena.
--
-- REQUISITO Nº 2 (reprovado no teste do Diretor): a troca de elemento tem de
-- responder ao toque. Por isso:
--   · o slot do elemento é um TextButton de 58dp × 64dp (mín. do GDD é 48dp);
--   · responde no InputBegan (dedo desceu), não em Activated/gesto;
--   · o slot NÃO muda de tamanho ao ser selecionado — quem cresce é o ícone
--     dentro dele. Botão que se mexe embaixo do dedo é botão que erra o toque.
--
-- ACESSIBILIDADE (GDD §10, inegociável): cada elemento tem COR + FORMA.
-- As formas vêm de Elements.shape e são desenhadas só com Frames (zero asset,
-- zero Toolbox): Wedge→losango · Ball→círculo · Block→quadrado · Ring→anel
-- vazado · Bolt→ziguezague. Legíveis em preto e branco.
--
-- PREFERÊNCIAS DE CONFORTO (R14): esta tela lê a loja do `Accessibility` DIRETO
-- — nada de `A11y.attach`. Duas razões, as duas duras:
--   · CONTRASTE. O `repaint` do attach força TODO `UIStroke.Transparency` a 0.
--     Aqui o stroke não é enfeite, é ESTADO: anel armado do gesto, elemento
--     ativo, correr/agachar ligados. Attachar acenderia os cinco anéis de uma
--     vez e o polegar perderia qual botão está armado. Então o contraste entra
--     pela porta oposta: cada canal de estado tem o PAR aceso/apagado, e o alto
--     contraste AFASTA os dois valores — nunca junta.
--   · LAYOUT. O attach estampa um `UIScale` por filho, e `UIScale` cresce o
--     objeto sem mexer na `Position` dele. Estes botões são posicionados em px
--     absolutos derivados de `dpScale`, então cresceriam por cima uns dos
--     outros e sairiam do arco do polegar. Aqui a escala entra na MESMA conta
--     que já posiciona tudo.
-- E o multiplicador só CRESCE: os 48dp do GDD §19.3 nasceram de reprovação em
-- aparelho real. Quem escolhe UI menor encolhe painel — nunca o que o dedo tem
-- de acertar.
-- ============================================================================

local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Strings = require(Shared:WaitForChild("Strings"))
local Elements = require(Shared:WaitForChild("Elements"))
--- LOJA ÚNICA de preferência. Lida direto; ver o cabeçalho para o porquê de
--- esta tela não passar pelo `A11y.attach`.
local A11y = require(script.Parent:WaitForChild("Accessibility"))

--- GDD §10
local GOLD = Color3.fromHex("#F0C75E")
local NIGHT = Color3.fromHex("#0B1026")
local INK = Color3.fromRGB(255, 255, 255)
--- comandos de movimento: aço frio, para não competir com o dourado das magias
local STEEL = Color3.fromHex("#8FA6C8")

-- layout em "dp" (ver dpScale abaixo). Ancorado no canto inferior direito:
-- zona de polegar do destro em paisagem. O lado esquerdo fica LIVRE para o
-- thumbstick nativo e o miolo/alto da tela livre para girar a câmera.
type Spec = { size: number, x: number, y: number, left: boolean? }

local BASIC: Spec = { size = 112, x = 92, y = 92 }
local TACTIC: Spec = { size = 84, x = 198, y = 124 }
local DODGE: Spec = { size = 74, x = 96, y = 196 }
--- COMANDOS BÁSICOS DE BR (reprovados no 2º teste do Diretor por não existirem).
--- PULAR segue o arco do polegar direito, depois da tática: o lugar "acima do
--- ataque" já é da esquiva, e botão que se mexe embaixo do dedo é botão errado.
local JUMP: Spec = { size = 96, x = 296, y = 92 }
--- CORRER/AGACHAR à esquerda-baixo, ACIMA do thumbstick nativo: 250dp de altura
--- deixam livre a zona baixa onde o polegar esquerdo pousa para andar.
--- CALIBRAÇÃO: se o Diretor esbarrar neles ao andar, é este 250 que sobe.
local SPRINT: Spec = { size = 72, x = 62, y = 250, left = true }
local CROUCH: Spec = { size = 72, x = 148, y = 250, left = true }
local SLOT_W, SLOT_H, SLOT_PAD = 58, 64, 8
local ROW_RIGHT, ROW_BOTTOM = 20, 252
local ICON_IDLE, ICON_ACTIVE = 32, 42

--- TODO alvo de dedo desta tela, em dp. É desta lista que o autoteste tira o
--- piso do GDD §19.3 — botão novo entra AQUI ou o teste não o enxerga.
local TARGETS = { BASIC.size, TACTIC.size, DODGE.size, JUMP.size, SPRINT.size, CROUCH.size, SLOT_W, SLOT_H }
local SMALLEST_DP = math.min(table.unpack(TARGETS))

--- PARES [normal, ALTO CONTRASTE] dos canais que carregam ESTADO. O alto
--- contraste deixa o fundo mais opaco (legível contra a arena atrás) e AUMENTA
--- a distância entre aceso e apagado. Nunca a diminui: juntar os dois é
--- exatamente o que o `repaint` do `A11y.attach` faria aqui.
local ACT_BG = { 0.12, 0 } --- fundo do botão de ação (constante: não é estado)
local RING_OFF = { 0.7, 0.9 } --- anel PARADO; armado é sempre RING_ON
local RING_ON = 0
local SLOT_BG_ON = { 0.1, 0 }
local SLOT_BG_OFF = { 0.45, 0.25 }

--- 1 = normal · 2 = ALTO CONTRASTE. `pcall` porque a loja é de outra tela e um
--- knob de conforto não pode derrubar os controles do jogo.
local function hc(): number
	local ok, on = pcall(A11y.highContrast)
	return if ok and on == true then 2 else 1
end

--- Multiplicador de acessibilidade do alvo de dedo. SÓ CRESCE (ver cabeçalho).
--- Separada de `a11yScale` para o autoteste provar o piso sem mexer na loja.
local function growOnly(v: any): number
	return math.max(1, tonumber(v) or 1)
end

local function a11yScale(): number
	local ok, v = pcall(A11y.uiScale)
	return growOnly(if ok then v else 1)
end

export type Handle = {
	gui: ScreenGui,
	setElement: (string) -> (),
	setPressed: (string, boolean) -> (),
	setCooldown: (string, number, number) -> (),
	--- botões que ficam LIGADOS (correr, agachar). Quem manda no estado é o
	--- Controls; aqui é só pintura.
	setToggle: (string, boolean) -> (),
}

local Touch = {}

--- 1 dp → px. O Roblox não expõe DPI; a referência prática é a altura da
--- viewport (640 px ≈ celular pequeno em paisagem). CALIBRAÇÃO: se os botões
--- saírem grandes/pequenos demais no aparelho do Diretor, é este número.
--- Recebe a ALTURA da viewport (e não a Camera) para o autoteste poder provar
--- o piso de 48dp sem forjar uma câmera.
local function dpScale(viewportY: number): number
	return math.clamp(viewportY / 640, 0.85, 2.0)
end

--- Preenchimento vertical sem clipping (respeita o canto arredondado):
--- frac = 1 → tudo apagado (acabou de usar) · frac = 0 → cheio (pronto).
local function setFill(grad: UIGradient, frac: number, bright: Color3, dim: Color3)
	local p = math.clamp(1 - frac, 0, 1)
	if p <= 0.002 then
		grad.Color = ColorSequence.new(dim)
	elseif p >= 0.998 then
		grad.Color = ColorSequence.new(bright)
	else
		grad.Color = ColorSequence.new({
			ColorSequenceKeypoint.new(0, bright),
			ColorSequenceKeypoint.new(p, bright),
			ColorSequenceKeypoint.new(p + 0.001, dim),
			ColorSequenceKeypoint.new(1, dim),
		})
	end
end

--- Desenha a FORMA do elemento dentro de `box`. Devolve os strokes que
--- precisam de espessura em px (o anel do Vento).
local function paintShape(box: Frame, element: any): { UIStroke }
	local strokes: { UIStroke } = {}
	local function frame(): Frame
		local f = Instance.new("Frame")
		f.BackgroundColor3 = element.color
		f.BorderSizePixel = 0
		f.AnchorPoint = Vector2.new(0.5, 0.5)
		f.Position = UDim2.fromScale(0.5, 0.5)
		f.Size = UDim2.fromScale(1, 1)
		f.Parent = box
		return f
	end

	if element.shape == "Ball" then -- Água: círculo cheio
		local k = Instance.new("UICorner")
		k.CornerRadius = UDim.new(1, 0)
		k.Parent = frame()
	elseif element.shape == "Block" then -- Terra: quadrado
		frame().Size = UDim2.fromScale(0.86, 0.86)
	elseif element.shape == "Wedge" then -- Fogo: losango (quadrado a 45°)
		local f = frame()
		f.Size = UDim2.fromScale(0.74, 0.74)
		f.Rotation = 45
	elseif element.shape == "Ring" then -- Vento: anel vazado
		local f = frame()
		f.BackgroundTransparency = 1
		local k = Instance.new("UICorner")
		k.CornerRadius = UDim.new(1, 0)
		k.Parent = f
		local s = Instance.new("UIStroke")
		s.Color = element.color
		s.Parent = f
		table.insert(strokes, s)
	else -- Bolt (Raio): ziguezague de duas barras opostas
		local a = frame()
		a.Size = UDim2.fromScale(0.26, 0.58)
		a.Position = UDim2.fromScale(0.42, 0.28)
		a.Rotation = 22
		local b = frame()
		b.Size = UDim2.fromScale(0.26, 0.58)
		b.Position = UDim2.fromScale(0.58, 0.72)
		b.Rotation = -22
	end
	return strokes
end

--- Monta a ScreenGui. `onPress(kind, input)` entrega o InputObject cru para o
--- Controls rastrear o arrasto; `onElement(id)` é a troca imediata;
--- `onSimple(kind)` são os botões de uma ação só (pular, correr, agachar).
function Touch.build(
	player: Player,
	onPress: (string, InputObject) -> (),
	onElement: (string) -> (),
	onSimple: (string) -> ()
): Handle
	local camera = workspace.CurrentCamera
	local scale = dpScale(camera.ViewportSize.Y) * a11yScale()
	--- cada widget registra como se redimensiona (rotação de tela / resize)
	local sized: { (number) -> () } = {}

	local gui = Instance.new("ScreenGui")
	gui.Name = "ArkanaControls" -- raia D usa "ArkanaHud" — não colidir
	gui.ResetOnSpawn = false
	gui.IgnoreGuiInset = true
	gui.DisplayOrder = 5
	gui.ZIndexBehavior = Enum.ZIndexBehavior.Sibling
	gui.Parent = player:WaitForChild("PlayerGui")

	-- --------------------------------------------------------- botões de ação
	type Action = {
		btn: TextButton,
		grad: UIGradient,
		stroke: UIStroke,
		glyph: string,
		color: Color3,
		dim: Color3,
		frac: number,
		--- anel ACESO? (armado pelo gesto, ou correr/agachar ligado). Guardado
		--- porque o alto contraste REPINTA o anel e precisa saber o estado — ler
		--- a transparência de volta seria adivinhar.
		lit: boolean,
	}
	local actions: { [string]: Action } = {}

	local function actionButton(kind: string, glyph: string, spec: Spec, color: Color3, simple: boolean?)
		local btn = Instance.new("TextButton")
		btn.Name = kind
		btn.AnchorPoint = Vector2.new(0.5, 0.5)
		btn.BackgroundColor3 = INK -- o UIGradient é quem colore (fill de cooldown)
		btn.BackgroundTransparency = ACT_BG[hc()]
		btn.AutoButtonColor = false
		btn.Text = glyph
		btn.TextColor3 = NIGHT
		btn.Font = Enum.Font.GothamBold -- Chakra Petch (GDD §10) não é fonte da plataforma
		btn.Parent = gui

		local corner = Instance.new("UICorner")
		corner.CornerRadius = UDim.new(1, 0)
		corner.Parent = btn

		local grad = Instance.new("UIGradient")
		grad.Rotation = 270 -- enche de baixo para cima
		grad.Parent = btn

		local stroke = Instance.new("UIStroke")
		stroke.Color = GOLD
		stroke.Transparency = RING_OFF[hc()]
		stroke.Parent = btn

		table.insert(sized, function(s)
			btn.Size = UDim2.fromOffset(spec.size * s, spec.size * s)
			btn.Position = if spec.left
				then UDim2.new(0, spec.x * s, 1, -spec.y * s)
				else UDim2.new(1, -spec.x * s, 1, -spec.y * s)
			btn.TextSize = math.max(12, math.round(spec.size * 0.22 * s))
			stroke.Thickness = math.max(2, 3 * s)
		end)

		-- REQUISITO Nº 1: o gesto de magia começa AQUI e o Controls o segue até
		-- soltar. Comando básico (`simple`) não tem gesto nenhum: resolve no dedo
		-- descendo, que é o que um pulo precisa ser.
		btn.InputBegan:Connect(function(input)
			if
				input.UserInputType == Enum.UserInputType.Touch
				or input.UserInputType == Enum.UserInputType.MouseButton1
			then
				if simple then
					onSimple(kind)
				else
					onPress(kind, input)
				end
			end
		end)

		local dim = color:Lerp(NIGHT, 0.72)
		actions[kind] = {
			btn = btn,
			grad = grad,
			stroke = stroke,
			glyph = glyph,
			color = color,
			dim = dim,
			frac = 0,
			lit = false,
		}
		setFill(grad, 0, color, dim)
	end

	local first = Elements.ById[Elements.Order[1]]
	actionButton("basic", Strings.t("controls.basic"), BASIC, first.color)
	actionButton("tactic", Strings.t("controls.tactic"), TACTIC, first.glow)
	actionButton("dodge", Strings.t("controls.dodge"), DODGE, GOLD)
	-- comandos básicos: sem eles não tem jogo (teste do Diretor)
	actionButton("jump", Strings.t("controls.jump"), JUMP, GOLD, true)
	actionButton("sprint", Strings.t("controls.sprint"), SPRINT, STEEL, true)
	actionButton("crouch", Strings.t("controls.crouch"), CROUCH, STEEL, true)

	-- ------------------------------------------------- carrossel de elementos
	local row = Instance.new("Frame")
	row.Name = "Elementos"
	row.AnchorPoint = Vector2.new(1, 1)
	row.BackgroundTransparency = 1
	row.Parent = gui

	local list = Instance.new("UIListLayout")
	list.FillDirection = Enum.FillDirection.Horizontal
	list.HorizontalAlignment = Enum.HorizontalAlignment.Right
	list.VerticalAlignment = Enum.VerticalAlignment.Center
	list.SortOrder = Enum.SortOrder.LayoutOrder
	list.Parent = row

	local rowW = #Elements.Order * SLOT_W + (#Elements.Order - 1) * SLOT_PAD
	table.insert(sized, function(s)
		row.Size = UDim2.fromOffset(rowW * s, SLOT_H * s)
		row.Position = UDim2.new(1, -ROW_RIGHT * s, 1, -ROW_BOTTOM * s)
		list.Padding = UDim.new(0, SLOT_PAD * s)
	end)

	type Slot = { btn: TextButton, icon: Frame, stroke: UIStroke }
	local slots: { [string]: Slot } = {}

	for i, id in ipairs(Elements.Order) do
		local element = Elements.ById[id]

		local btn = Instance.new("TextButton")
		btn.Name = id
		btn.LayoutOrder = i
		btn.BackgroundColor3 = NIGHT
		btn.BackgroundTransparency = SLOT_BG_OFF[hc()]
		btn.AutoButtonColor = false
		btn.Text = ""
		btn.Parent = row

		local corner = Instance.new("UICorner")
		corner.CornerRadius = UDim.new(0, 10)
		corner.Parent = btn

		local stroke = Instance.new("UIStroke")
		stroke.Color = GOLD
		stroke.Transparency = 1 -- só o ativo mostra a borda dourada
		stroke.Parent = btn

		local icon = Instance.new("Frame")
		icon.Name = "Forma"
		icon.BackgroundTransparency = 1
		icon.AnchorPoint = Vector2.new(0.5, 0.5)
		icon.Position = UDim2.fromScale(0.5, 0.4)
		icon.Parent = btn

		local ringStrokes = paintShape(icon, element)

		local name = Instance.new("TextLabel")
		name.BackgroundTransparency = 1
		name.AnchorPoint = Vector2.new(0.5, 1)
		name.Position = UDim2.new(0.5, 0, 1, 0)
		name.Size = UDim2.new(1, 0, 0.24, 0)
		-- cor + forma + PALAVRA: três canais, não um. A palavra sai do idioma
		-- ATIVO (mesmo critério de MatchUi/TutorialUi): o rótulo que o dedo lê
		-- não pode ficar em português numa sessão em inglês.
		name.Text = if Strings.getLang() == "pt" then element.pt else element.en
		name.TextColor3 = INK
		name.TextScaled = true
		name.Font = Enum.Font.GothamBold
		name.Parent = btn

		table.insert(sized, function(s)
			btn.Size = UDim2.fromOffset(SLOT_W * s, SLOT_H * s)
			stroke.Thickness = math.max(2, 3 * s)
			for _, st in ipairs(ringStrokes) do
				st.Thickness = math.max(2, 4 * s)
			end
		end)

		-- responde no dedo DESCENDO (requisito nº 2), sem esperar gesto nenhum
		btn.InputBegan:Connect(function(input)
			if
				input.UserInputType == Enum.UserInputType.Touch
				or input.UserInputType == Enum.UserInputType.MouseButton1
			then
				onElement(id)
			end
		end)

		slots[id] = { btn = btn, icon = icon, stroke = stroke }
	end

	-- --------------------------------------------------------------- fachada
	local activeId = Elements.Order[1]

	local function sizeIcons()
		for id, slot in pairs(slots) do
			local px = (if id == activeId then ICON_ACTIVE else ICON_IDLE) * scale
			slot.icon.Size = UDim2.fromOffset(px, px)
		end
	end

	local function applyScale()
		--- a preferência de tamanho de UI entra na MESMA conta que posiciona os
		--- botões — por isso ela cresce o alvo de dedo em vez de só inchar o
		--- desenho por cima do vizinho (ver cabeçalho).
		scale = dpScale(camera.ViewportSize.Y) * a11yScale()
		for _, fn in ipairs(sized) do
			fn(scale)
		end
		sizeIcons()
	end

	local function setElement(id: string)
		if not Elements.isValid(id) then
			return
		end
		activeId = id
		local element = Elements.ById[id :: any]
		local c = hc()
		for slotId, slot in pairs(slots) do
			local on = slotId == id
			slot.stroke.Transparency = if on then RING_ON else 1
			slot.btn.BackgroundTransparency = if on then SLOT_BG_ON[c] else SLOT_BG_OFF[c]
		end
		sizeIcons()
		-- o botão de ataque assume a cor do elemento: o dedo vê o que vai sair.
		-- Repinta na hora — "basic" não tem cooldown para repintá-lo depois.
		local function recolor(a: Action, c: Color3)
			a.color = c
			a.dim = c:Lerp(NIGHT, 0.72)
			setFill(a.grad, a.frac, a.color, a.dim)
		end
		recolor(actions.basic, element.color)
		recolor(actions.tactic, element.glow)
	end

	--- O ÚNICO lugar que escreve o anel. Aceso é sempre opaco; apagado sai do par
	--- de contraste. Estado primeiro, contraste depois — nunca o contrário.
	local function paintStroke(a: Action)
		a.stroke.Transparency = if a.lit then RING_ON else RING_OFF[hc()]
	end

	--- correr/agachar ficam ACESOS enquanto valem: apagado = desligado, sem
	--- depender de cor nenhuma (o mesmo critério de acessibilidade do GDD §10).
	local function setToggle(kind: string, on: boolean)
		local a = actions[kind]
		if not a then
			return
		end
		a.frac = if on then 0 else 1
		setFill(a.grad, a.frac, a.color, a.dim)
		a.lit = on
		paintStroke(a)
	end

	--- ANEL ARMADO: aceso = soltar o dedo AGORA dispara · apagado = soltar não
	--- faz nada (voltou para dentro da deadzone e o tiro rápido já expirou).
	--- Quem decide é `Controls.willFire` — a MESMA conta que dispara —, aqui é
	--- só pintura. É o feedback do CANCELAR, que separa "mirei errado" de
	--- "atirei errado" para o polegar, sem depender da seta no mundo (que fica
	--- longe do dedo e pode estar atrás do personagem).
	--- O Controls repinta isto TODO FRAME (o estado muda com o relógio, não só
	--- com o dedo), então escrita repetida é proibida: 60 gravações/s por botão
	--- para repetir o valor que já estava lá é a mesma conta que a seta de mira
	--- já pagou uma vez, no aparelho mais fraco.
	local pressed: { [string]: boolean } = {}
	local function setPressed(kind: string, on: boolean)
		local a = actions[kind]
		if not a or pressed[kind] == on then
			return
		end
		pressed[kind] = on
		a.lit = on
		paintStroke(a)
	end

	local function setCooldown(kind: string, remaining: number, total: number)
		local a = actions[kind]
		if not a then
			return
		end
		local frac = if total > 0 then math.clamp(remaining / total, 0, 1) else 0
		a.frac = frac
		setFill(a.grad, frac, a.color, a.dim)
		a.btn.Text = if remaining > 0.05 then string.format("%.1f", remaining) else a.glyph
	end

	--- A ÚNICA porta da acessibilidade nesta tela: redimensiona o alvo de dedo e
	--- reescolhe o par aceso/apagado de cada canal, SEM tocar em qual botão está
	--- aceso. É o que o `A11y.attach` não sabe fazer (ver cabeçalho).
	local function applyA11y()
		applyScale()
		local c = hc()
		for _, a in pairs(actions) do
			a.btn.BackgroundTransparency = ACT_BG[c]
			paintStroke(a)
		end
		setElement(activeId) -- borda e fundo dos slots saem daqui
	end

	applyA11y()
	setToggle("sprint", false)
	setToggle("crouch", false)
	camera:GetPropertyChangedSignal("ViewportSize"):Connect(applyScale)
	--- `pcall` no registro (a loja é de outra tela) e filtro por chave: mexer no
	--- volume não precisa remontar botão nenhum.
	pcall(A11y.onChange, function(key: string)
		if key == "uiSize" or key == "contrast" then
			applyA11y()
		end
	end)

	return {
		gui = gui,
		setElement = setElement,
		setPressed = setPressed,
		setCooldown = setCooldown,
		setToggle = setToggle,
	}
end

-- ============================================================================
-- AUTOTESTE — cole no Command Bar do Studio (cliente):
--   require(game.Players.LocalPlayer.PlayerScripts.Client.ControlsTouch).selfTest()
-- Cobre as três coisas desta tela que quebram CALADAS: o alvo de dedo abaixo do
-- piso do GDD §19.3, a preferência de tamanho que não chega no botão (era o
-- defeito da R13: quem aumentou a UI não ganhava dedo maior) e o alto contraste
-- apagando o ESTADO em vez de realçá-lo.
-- ============================================================================
function Touch.selfTest()
	-- ------------------------------------------------------ 1) piso de 48dp
	-- Pior caso REAL: viewport minúscula (dpScale trava em 0.85) com a MENOR
	-- escolha do menu (uiSize 0.9).
	assert(dpScale(1) == 0.85 and dpScale(999999) == 2, "as travas de dpScale mudaram")
	assert(growOnly(0.9) == 1, "UI menor encolheu o alvo de dedo — 48dp é piso, não meta")
	assert(growOnly(1.3) == 1.3, "quem aumentou a UI não ganhou alvo maior")
	assert(SMALLEST_DP * dpScale(1) * growOnly(0.9) >= 48, "menor alvo de dedo abaixo dos 48dp do GDD §19.3")

	local player = Players.LocalPlayer
	if not player then
		print("[ControlsTouch] autoteste PARCIAL — sem LocalPlayer: só as contas puras")
		return true
	end
	local noop = function() end
	local h = Touch.build(player, noop, noop, noop)
	local row = h.gui:FindFirstChild("Elementos")
	local idle = row and row:FindFirstChild(Elements.Order[1])
	local live = row and row:FindFirstChild(Elements.Order[2])
	assert(idle and live, "carrossel de elementos sumiu")

	local wasSize, wasContrast = A11y.get("uiSize"), A11y.get("contrast")

	-- --------------------------------------------------------- 2) a FIAÇÃO
	-- A conta certa com o fio solto é exatamente o que estava em produção: o
	-- alvo tem de mudar na TELA MONTADA quando o menu muda.
	A11y.set("uiSize", 0.9)
	local small = idle.Size.X.Offset
	A11y.set("uiSize", 1.3)
	local big = idle.Size.X.Offset
	assert(big > small, "aumentar a UI não aumentou o alvo de dedo: a tela de toque ignora o menu")
	assert(small >= 48, "a menor escolha do menu furou os 48dp nesta viewport")

	-- ------------------------------- 3) contraste NÃO pode apagar o estado
	-- Este é o assert que reprova quem "resolver" a acessibilidade daqui com
	-- `A11y.attach(gui)`: o `repaint` dele força todo UIStroke a 0 e acende os
	-- cinco anéis (e os dois comandos) de uma vez.
	h.setElement(Elements.Order[2])
	h.setPressed("basic", true)
	h.setPressed("tactic", false)
	local armed = (h.gui:FindFirstChild("basic") :: any):FindFirstChildOfClass("UIStroke")
	local parado = (h.gui:FindFirstChild("tactic") :: any):FindFirstChildOfClass("UIStroke")
	for _, on in ipairs({ false, true }) do
		A11y.set("contrast", on)
		local tag = if on then " [alto contraste]" else ""
		assert(
			(live :: any):FindFirstChildOfClass("UIStroke").Transparency
				< (idle :: any):FindFirstChildOfClass("UIStroke").Transparency,
			"elemento ativo e inativo com a MESMA borda" .. tag
		)
		assert(
			(live :: any).BackgroundTransparency < (idle :: any).BackgroundTransparency,
			"elemento ativo e inativo com o MESMO fundo" .. tag
		)
		assert(armed.Transparency < parado.Transparency, "anel ARMADO e anel parado iguais" .. tag)
	end

	-- A ROTA ERRADA, conferida DEPOIS de a preferência ter mexido de verdade —
	-- é aí que `A11y.attach` deixa rastro: um `UIScale` chamado "A11yScale" em
	-- cada filho da ScreenGui (cresce o botão sem mexer na Position em px: ele
	-- sai do arco do polegar) e os atributos `A11yBg`/`A11yStroke` que o
	-- `repaint` grava ao tomar conta dos canais de ESTADO. Conferir antes do
	-- primeiro `A11y.set` daria verde por vacuidade.
	for _, child in ipairs(h.gui:GetChildren()) do
		assert(
			not child:FindFirstChild("A11yScale"),
			"alguém chamou A11y.attach nesta tela: o UIScale desalinha o botão da posição em px"
		)
	end
	for _, d in ipairs(h.gui:GetDescendants()) do
		assert(
			d:GetAttribute("A11yStroke") == nil and d:GetAttribute("A11yBg") == nil,
			"o repaint do A11y passou por aqui: ele é dono do UIStroke, e aqui UIStroke é ESTADO"
		)
	end

	A11y.set("uiSize", wasSize)
	A11y.set("contrast", wasContrast)
	h.gui:Destroy()
	print("[ControlsTouch] autoteste OK — 48dp com folga · o menu chega no alvo de dedo · contraste não apaga o estado")
	return true
end

return Touch
