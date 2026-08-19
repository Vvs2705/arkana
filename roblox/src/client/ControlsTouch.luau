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
-- ============================================================================

local ReplicatedStorage = game:GetService("ReplicatedStorage")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Strings = require(Shared:WaitForChild("Strings"))
local Elements = require(Shared:WaitForChild("Elements"))

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
local function dpScale(camera: Camera): number
	return math.clamp(camera.ViewportSize.Y / 640, 0.85, 2.0)
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
	local scale = dpScale(camera)
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
	type Action =
		{ btn: TextButton, grad: UIGradient, stroke: UIStroke, glyph: string, color: Color3, dim: Color3, frac: number }
	local actions: { [string]: Action } = {}

	local function actionButton(kind: string, glyph: string, spec: Spec, color: Color3, simple: boolean?)
		local btn = Instance.new("TextButton")
		btn.Name = kind
		btn.AnchorPoint = Vector2.new(0.5, 0.5)
		btn.BackgroundColor3 = INK -- o UIGradient é quem colore (fill de cooldown)
		btn.BackgroundTransparency = 0.12
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
		stroke.Transparency = 0.7
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
		actions[kind] =
			{ btn = btn, grad = grad, stroke = stroke, glyph = glyph, color = color, dim = dim, frac = 0 }
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
		btn.BackgroundTransparency = 0.45
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
		name.Text = element.pt -- cor + forma + PALAVRA: três canais, não um
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
		scale = dpScale(camera)
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
		for slotId, slot in pairs(slots) do
			local on = slotId == id
			slot.stroke.Transparency = if on then 0 else 1
			slot.btn.BackgroundTransparency = if on then 0.1 else 0.45
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

	--- correr/agachar ficam ACESOS enquanto valem: apagado = desligado, sem
	--- depender de cor nenhuma (o mesmo critério de acessibilidade do GDD §10).
	local function setToggle(kind: string, on: boolean)
		local a = actions[kind]
		if not a then
			return
		end
		a.frac = if on then 0 else 1
		setFill(a.grad, a.frac, a.color, a.dim)
		a.stroke.Transparency = if on then 0 else 0.7
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
		a.stroke.Transparency = if on then 0 else 0.7
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

	applyScale()
	setElement(activeId)
	setToggle("sprint", false)
	setToggle("crouch", false)
	camera:GetPropertyChangedSignal("ViewportSize"):Connect(applyScale)

	return {
		gui = gui,
		setElement = setElement,
		setPressed = setPressed,
		setCooldown = setCooldown,
		setToggle = setToggle,
	}
end

return Touch
