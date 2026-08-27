--!strict
-- ============================================================================
-- ARKANA (Roblox) — PRESENÇA DO PERSONAGEM (escala + silhueta de mago)
--
-- REPROVAÇÃO DO DIRETOR (18/08): "bonecos MAIORES e mais NÍTIDOS na tela".
-- A câmera (client/CameraRig) resolve a distância; aqui resolvemos o CORPO:
--   1. avatar ~1.3x maior (escala nativa do Humanoid, R15);
--   2. silhueta de MAGO reconhecível a distância de gameplay — chapéu cônico,
--      manto e cajado, tudo em BLOCOS (docs/ROBLOX.md §2, ref. Pixel Gun).
--
-- REGRA DURA (docs/ROBLOX.md §2): zero Toolbox, zero asset externo, zero
-- MeshPart de terceiros. Geometria nossa, em Parts, por código.
-- Paleta GDD §10: azul-noite #0B1026 + dourado #F0C75E.
--
-- FÍSICA: toda peça é DECORAÇÃO — CanCollide/CanQuery/CanTouch = false e
-- Massless = true. Não muda a massa do personagem, não entra em raycast e não
-- aparece na busca de alvos (Combat/Bots procuram `Humanoid` +
-- `HumanoidRootPart`, que continuam intactos). As peças moram numa Folder
-- dentro do personagem, então `character:FindFirstChild("Head")` e afins
-- (busca rasa) nem enxergam elas.
--
-- Fiação: `Avatar.setup(player, character, humanoid)` no `onCharacter` do
-- Main.server. Não bloqueia o chamador (a montagem acontece no frame seguinte).
-- ============================================================================

local RunService = game:GetService("RunService")

local Avatar = {}

--- Multiplicador do corpo. Faixa útil 1.25–1.4: acima disso o mago começa a
--- raspar nas aberturas de 1 célula (4 studs) da arena. CALIBRAÇÃO.
local SCALE = 1.3
--- exposto: outras raias podem querer casar hitbox/alcance com o corpo novo
Avatar.SCALE = SCALE

local NIGHT = Color3.fromHex("#0B1026") -- azul-noite, base do manto e do chapéu
local GOLD = Color3.fromHex("#F0C75E") -- dourado, acento (fita, gola, cristal)
local BIN = "ArkanaCosmetics"

--- Escala nativa do R15: NumberValues dentro do Humanoid; o motor reconstrói o
--- rig sozinho. O R6 não tem esses valores — ver `resize`.
local SCALE_VALUES = { "BodyHeightScale", "BodyWidthScale", "BodyDepthScale", "HeadScale" }

--- Chapéu em tiers empilhados: {largura, altura} em unidades de tronco.
--- O último tier é a ponta dourada.
local HAT_TIERS = { { 1.30, 0.48 }, { 0.96, 0.48 }, { 0.62, 0.46 }, { 0.30, 0.42 } }

-- --------------------------------------------------------------------- escala
--- Devolve true se a escala pegou (R15). No R6 os NumberValues não existem:
--- redimensionar os Parts na mão quebraria os Motor6D do rig, então o fallback
--- seguro é NÃO escalar — o personagem fica no tamanho padrão e ganha só a
--- fantasia. Toda medida da fantasia sai do tronco, então ela acompanha os dois.
local function resize(humanoid: Humanoid): boolean
	-- sem AutomaticScaling o motor ignora os NumberValues (default é ligado;
	-- garantimos por via das dúvidas, e num Humanoid antigo o pcall só falha)
	pcall(function()
		(humanoid :: any).AutomaticScalingEnabled = true
	end)

	local applied = false
	for _, name in ipairs(SCALE_VALUES) do
		local nv = humanoid:FindFirstChild(name)
		if nv and nv:IsA("NumberValue") then
			nv.Value = SCALE
			applied = true
		end
	end
	return applied
end

-- ------------------------------------------------------------------ fantasia
local function cosmetic(name: string, size: Vector3, color: Color3, parent: Instance): BasePart
	local p = Instance.new("Part")
	p.Name = name
	p.Size = size
	p.Color = color
	p.Material = Enum.Material.SmoothPlastic -- cor chapada (direção blocky)
	p.TopSurface = Enum.SurfaceType.Smooth
	p.BottomSurface = Enum.SurfaceType.Smooth
	p.CanCollide = false
	p.CanQuery = false
	p.CanTouch = false
	p.CastShadow = false -- orçamento de 60 fps no celular (GDD §19.2)
	p.Massless = true
	p.Parent = parent
	return p
end

--- Solda rígida: posiciona a peça e prende no osso. WeldConstraint congela o
--- offset do instante em que nasce — por isso a fantasia só é montada DEPOIS
--- que a escala do R15 já foi aplicada (ver `Avatar.setup`).
local function weld(bone: BasePart, part: BasePart, offset: CFrame)
	part.CFrame = bone.CFrame * offset
	local w = Instance.new("WeldConstraint")
	w.Part0 = bone
	w.Part1 = part
	w.Parent = part
end

--- primeiro nome que existir: cobre R15 e R6 no mesmo caminho
local function bone(character: Model, names: { string }): BasePart?
	for _, n in ipairs(names) do
		local p = character:FindFirstChild(n)
		if p and p:IsA("BasePart") then
			return p
		end
	end
	return nil
end

local function dress(character: Model)
	local old = character:FindFirstChild(BIN)
	if old then
		old:Destroy() -- idempotente: re-vestir nunca duplica peça
	end

	local head = bone(character, { "Head" })
	local torso = bone(character, { "UpperTorso", "Torso" })
	local hand = bone(character, { "RightHand", "Right Arm" })
	if not head or not torso then
		return -- rig fora do esperado: sem fantasia é melhor que fantasia torta
	end

	local bin = Instance.new("Folder")
	bin.Name = BIN
	bin.Parent = character

	--- unidade de medida = meia largura do tronco (≈1 stud no avatar padrão, já
	--- multiplicada pela escala no R15). Amarrar tudo nela faz a fantasia
	--- acompanhar qualquer SCALE e qualquer rig sem número mágico.
	local u = torso.Size.X / 2
	local top = head.Size.Y / 2

	-- chapéu: aba larga + fita dourada + cone em tiers (silhueta de mago)
	weld(head, cosmetic("Aba", Vector3.new(u * 2.2, u * 0.18, u * 2.2), NIGHT, bin), CFrame.new(0, top + u * 0.09, 0))
	weld(head, cosmetic("Fita", Vector3.new(u * 1.5, u * 0.2, u * 1.5), GOLD, bin), CFrame.new(0, top + u * 0.28, 0))
	local y = top + u * 0.38
	for i, tier in ipairs(HAT_TIERS) do
		local w, h = u * tier[1], u * tier[2]
		local color = if i == #HAT_TIERS then GOLD else NIGHT
		weld(head, cosmetic("Chapeu" .. i, Vector3.new(w, h, w), color, bin), CFrame.new(0, y + h / 2, 0))
		y += h
	end

	-- manto: laje nas costas (o +Z local é as costas — o "frente" do CFrame é
	-- o -Z) + gola dourada, que é o que dá leitura do ombro a distância
	weld(
		torso,
		cosmetic("Manto", Vector3.new(torso.Size.X * 1.06, torso.Size.Y * 1.5, u * 0.28), NIGHT, bin),
		CFrame.new(0, -torso.Size.Y * 0.28, torso.Size.Z / 2 + u * 0.14)
	)
	weld(
		torso,
		cosmetic("Gola", Vector3.new(torso.Size.X * 1.12, u * 0.24, torso.Size.Z * 1.2), GOLD, bin),
		CFrame.new(0, torso.Size.Y / 2 - u * 0.12, 0)
	)

	-- cajado: haste + cristal. O eixo Y local da mão/braço corre ao longo do
	-- membro, então uma haste em Y fica "empunhada" e segue a animação sozinha.
	-- CALIBRAÇÃO: offset lateral e altura são chute educado — conferir no
	-- playtest se a haste atravessa o braço em alguma pose.
	if hand then
		local len = u * 4.4
		local lift = len * 0.32
		local side = u * 0.5
		weld(hand, cosmetic("Cajado", Vector3.new(u * 0.2, len, u * 0.2), NIGHT, bin), CFrame.new(side, lift, 0))
		local orb = cosmetic("Cristal", Vector3.new(u * 0.44, u * 0.44, u * 0.44), GOLD, bin)
		orb.Material = Enum.Material.Neon -- o que brilha é magia (GDD §10)
		weld(hand, orb, CFrame.new(side, lift + len / 2 + u * 0.22, 0))
	end
end

-- ------------------------------------------------------------------- fachada
--- `player` entra só por simetria com o resto da fiação do servidor (o módulo
--- não precisa dele): bots podem passar nil e ganhar a mesma silhueta.
function Avatar.setup(_player: Player?, character: Model, humanoid: Humanoid)
	resize(humanoid)
	-- A rescala do R15 é feita pelo motor. Montar a fantasia um frame depois
	-- garante que `torso.Size`/`head.Size` já vieram escalados — soldar antes
	-- deixaria chapéu e cajado no tamanho e no lugar errados.
	task.spawn(function()
		RunService.Heartbeat:Wait()
		if character.Parent and humanoid.Parent then
			dress(character)
		end
	end)
end

return Avatar
