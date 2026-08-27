--!strict
-- ============================================================================
-- RAIA D (HUD/UI) — SELO DE ELEMENTO: COR + FORMA.
--
-- REGRA DE ACESSIBILIDADE INEGOCIÁVEL (GDD §10): elemento nunca é só cor.
-- Existe porque DOIS lugares mostram "qual elemento é este" e não podem
-- divergir: a linha do parceiro no HUD e o card de convite da Sintonia — ali
-- o jogador precisa ler os dois elementos do combo em daltonismo pleno.
--
-- Mesma linguagem das formas da raia C (ControlsTouch): Wedge→losango ·
-- Ball→círculo · Block→quadrado · Ring→anel vazado · Bolt→ziguezague.
-- Só primitivas: zero asset, zero Toolbox.
-- ============================================================================

local ReplicatedStorage = game:GetService("ReplicatedStorage")

local Elements = require(ReplicatedStorage:WaitForChild("Shared"):WaitForChild("Elements"))

local Badge = {}

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

--- Caixa vazia de `px`×`px`. O chamador posiciona; `Badge.paint` desenha dentro.
function Badge.new(parent: Instance, px: number): Frame
	return mk("Frame", {
		Name = "Badge",
		BackgroundTransparency = 1,
		Size = UDim2.fromOffset(px, px),
	}, parent)
end

--- Redesenha a caixa com a forma do elemento. `id` inválido = caixa vazia
--- (nenhum elemento conhecido — melhor vazio do que um símbolo mentiroso).
function Badge.paint(box: Frame, id: any)
	for _, child in ipairs(box:GetChildren()) do
		child:Destroy()
	end
	local e = if Elements.isValid(id) then (Elements.ById :: any)[id] :: Elements.Element else nil
	if not e then
		return
	end
	local px = math.max(box.AbsoluteSize.X, box.Size.X.Offset, 8)

	local function shape(scale: number, rotation: number): Frame
		return mk("Frame", {
			BackgroundColor3 = e.color,
			BorderSizePixel = 0,
			AnchorPoint = Vector2.new(0.5, 0.5),
			Position = UDim2.fromScale(0.5, 0.5),
			Size = UDim2.fromScale(scale, scale),
			Rotation = rotation,
		}, box)
	end

	if e.shape == "Ball" then
		mk("UICorner", { CornerRadius = UDim.new(1, 0) }, shape(0.9, 0))
	elseif e.shape == "Block" then
		shape(0.82, 0)
	elseif e.shape == "Wedge" then
		shape(0.72, 45)
	elseif e.shape == "Ring" then
		local ring = shape(0.86, 0)
		ring.BackgroundTransparency = 1
		mk("UICorner", { CornerRadius = UDim.new(1, 0) }, ring)
		mk("UIStroke", { Color = e.color, Thickness = math.max(2, px * 0.16) }, ring)
	else -- Bolt: dois traços em ziguezague
		local top = shape(1, 34)
		top.Size = UDim2.fromScale(0.66, 0.17)
		top.Position = UDim2.fromScale(0.5, 0.32)
		local bottom = shape(1, -34)
		bottom.Size = UDim2.fromScale(0.66, 0.17)
		bottom.Position = UDim2.fromScale(0.5, 0.68)
	end
end

return Badge
