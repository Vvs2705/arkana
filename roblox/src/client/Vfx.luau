--!strict
-- ============================================================================
-- RAIA D (VFX) — apresentação do que o SERVIDOR anuncia. Nada aqui decide
-- jogo: o projétil daqui é só a casca visual; o real (autoritativo, com
-- colisão e dano) vive no servidor.
--
-- ESTÉTICA BLOCKY (docs/ROBLOX.md §2, ref. Pixel Gun): primitivas do Roblox,
-- SmoothPlastic, cores chapadas. ZERO asset externo, ZERO Toolbox.
-- ACESSIBILIDADE (GDD §10): a FORMA do projétil vem de Elements.shape — o
-- elemento se lê pela silhueta, não só pela cor.
-- ORÇAMENTO (GDD §19.2, alvo 60 fps em celular): tetos duros de projéteis,
-- estilhaços e números de dano; sombra e colisão desligadas em tudo.
-- ============================================================================

local ReplicatedStorage = game:GetService("ReplicatedStorage")
local RunService = game:GetService("RunService")
local TweenService = game:GetService("TweenService")
local Debris = game:GetService("Debris")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Elements = require(Shared:WaitForChild("Elements"))
local Net = require(Shared:WaitForChild("Net"))

-- ------------------------------------------------------------------ orçamento
local MAX_PROJECTILES = 40
local MAX_DEBRIS = 72
local MAX_NUMBERS = 24
local PROJ_LIFE = 3.0 -- s: rede perdida/impacto não avisado não vira lixo eterno

local IMPACT = {
	weakShards = 5,
	strongShards = 10,
	shardLife = 0.6,
	flashLife = 0.2,
	--- alcance em studs onde o impacto ainda sacode a câmera
	shakeRange = 70,
	traumaWeak = 0.22,
	traumaStrong = 0.5,
}

local SHAKE = {
	decay = 1.8, -- trauma/s
	maxAngleDeg = 2.6,
	maxOffset = 0.35, -- studs
}

local DMG_NUM = {
	rise = 4.5, -- studs
	life = 0.75,
	bigAt = 15, -- dano "grande" = dourado e maior (espelha o HUD do protótipo 2D)
	maxDistance = 140,
}

local GOLD = Color3.fromHex("#F0C75E")
local NIGHT_DEEP = Color3.fromHex("#05070F")

-- ------------------------------------------------------------------ estado
local Vfx = {}

type Projectile = {
	part: BasePart,
	shape: string,
	pos: Vector3,
	dir: Vector3,
	speed: number,
	age: number,
}

local folder: Folder? = nil
local conns: { RBXScriptConnection } = {}
local projectiles: { [string]: Projectile } = {}
local projCount = 0
local debrisAlive = 0
local numbersAlive = 0
local trauma = 0
local localPlayer: Player? = nil
local SHAKE_BINDING = "ArkanaVfxShake"

local function bin(): Folder
	local f = folder
	if f and f.Parent then
		return f
	end
	f = Instance.new("Folder")
	f.Name = "ArkanaVfx" -- criado no cliente: existe só aqui, não replica
	f.Parent = workspace
	folder = f
	return f
end

local function element(id: any)
	return Elements.ById[id] or Elements.ById.Fire
end

--- todo part de VFX é decoração: sem colisão, sem query, sem sombra.
local function decor(p: BasePart)
	p.Material = Enum.Material.SmoothPlastic
	p.Anchored = true
	p.CanCollide = false
	p.CanQuery = false
	p.CanTouch = false
	p.CastShadow = false
	p.Massless = true
	p.TopSurface = Enum.SurfaceType.Smooth
	p.BottomSurface = Enum.SurfaceType.Smooth
	p.Parent = bin()
end

-- ------------------------------------------------------------------ projétil
--- A silhueta do elemento (Elements.shape) montada em primitivas.
local function buildBody(shape: string, color: Color3): BasePart
	local part: BasePart
	if shape == "Wedge" then
		part = Instance.new("WedgePart")
		part.Size = Vector3.new(1.3, 1.5, 2.4) -- cunha apontada: Fogo
	elseif shape == "Ball" then
		local p = Instance.new("Part")
		p.Shape = Enum.PartType.Ball
		p.Size = Vector3.new(1.8, 1.8, 1.8) -- gota grossa: Água
		part = p
	elseif shape == "Ring" then
		local p = Instance.new("Part")
		p.Shape = Enum.PartType.Cylinder
		p.Size = Vector3.new(0.35, 2.4, 2.4) -- disco fino de pé: Vento
		part = p
	elseif shape == "Bolt" then
		local p = Instance.new("Part")
		p.Size = Vector3.new(0.4, 0.4, 3.4) -- lasca comprida: Raio
		part = p
	else -- "Block" (Terra) e qualquer forma nova que a raia do coordenador criar
		local p = Instance.new("Part")
		p.Size = Vector3.new(1.5, 1.5, 1.5)
		part = p
	end
	part.Color = color
	decor(part)
	return part
end

--- rotação extra da forma: o disco de Vento fica de pé (o eixo do Cylinder é o
--- X local, então gira 90° em Y para encarar o trajeto), cubo e lasca giram.
local RING_FACE = CFrame.Angles(0, math.rad(90), 0)
local function shapeOffset(shape: string, age: number): CFrame
	if shape == "Ring" then
		return RING_FACE -- disco simétrico: girá-lo no próprio eixo não se vê
	elseif shape == "Block" then
		return CFrame.Angles(age * 5, age * 4, 0)
	elseif shape == "Bolt" then
		return CFrame.Angles(0, 0, age * 14)
	end
	return CFrame.identity
end

local function spawnProjectile(payload: any)
	if typeof(payload) ~= "table" then
		return
	end
	local id = tostring(payload.id or "")
	if id == "" or projectiles[id] then
		return
	end
	local origin = payload.origin
	local dir = Net.sanitizeDirection(payload.dir)
	if typeof(origin) ~= "Vector3" or not dir then
		return
	end
	if projCount >= MAX_PROJECTILES then
		return -- teto de partículas: o servidor continua certo, só não desenhamos
	end

	local e = element(payload.element)
	local part = buildBody(e.shape, e.color)
	part.Name = "Proj_" .. id
	part.CFrame = CFrame.lookAt(origin, origin + dir)

	-- o que brilha é magia (GDD §10): luz na cor `glow`, sem sombra
	local light = Instance.new("PointLight")
	light.Color = e.glow
	light.Brightness = 2
	light.Range = 10
	light.Shadows = false
	light.Parent = part

	-- trilha em blocos: dois pontos e um Trail curto
	local a0 = Instance.new("Attachment")
	a0.Position = Vector3.new(0, 0.35, 0)
	a0.Parent = part
	local a1 = Instance.new("Attachment")
	a1.Position = Vector3.new(0, -0.35, 0)
	a1.Parent = part
	local trail = Instance.new("Trail")
	trail.Attachment0 = a0
	trail.Attachment1 = a1
	trail.Color = ColorSequence.new(e.glow, e.color)
	trail.Transparency = NumberSequence.new(0.2, 1)
	trail.Lifetime = 0.22
	trail.LightEmission = 0.6
	trail.FaceCamera = true
	trail.Parent = part

	projectiles[id] = {
		part = part,
		shape = e.shape,
		pos = origin,
		dir = dir,
		speed = math.clamp(tonumber(payload.speed) or 60, 1, 400),
		age = 0,
	}
	projCount += 1
end

local function killProjectile(id: string)
	local p = projectiles[id]
	if not p then
		return
	end
	projectiles[id] = nil
	projCount -= 1
	p.part:Destroy()
end

-- ------------------------------------------------------------------ impacto
local function jitter(s: number, scale: number): number
	return (math.random() * 2 - 1) * s * scale
end

local function addTrauma(pos: Vector3, strong: boolean)
	local plr = localPlayer
	local char = plr and plr.Character
	local root = char and char:FindFirstChild("HumanoidRootPart") :: BasePart?
	if not root then
		return
	end
	local d = (root.Position - pos).Magnitude
	if d > IMPACT.shakeRange then
		return
	end
	-- impacto pequeno ≠ tática, e longe sacode menos que no colo
	local falloff = 1 - d / IMPACT.shakeRange
	local add = (strong and IMPACT.traumaStrong or IMPACT.traumaWeak) * falloff
	trauma = math.min(1, trauma + add)
end

local function impact(payload: any)
	if typeof(payload) ~= "table" then
		return
	end
	local pos = payload.pos
	if typeof(pos) ~= "Vector3" then
		return
	end
	if payload.id then
		killProjectile(tostring(payload.id))
	end

	local e = element(payload.element)
	local strong = payload.strong == true

	-- flash: bola neon que abre e some
	local flash = Instance.new("Part")
	flash.Shape = Enum.PartType.Ball
	flash.Size = Vector3.new(1, 1, 1)
	flash.Color = e.glow
	decor(flash)
	flash.Material = Enum.Material.Neon
	flash.CFrame = CFrame.new(pos)
	local flashLight = Instance.new("PointLight")
	flashLight.Color = e.glow
	flashLight.Brightness = strong and 6 or 3
	flashLight.Range = strong and 26 or 14
	flashLight.Shadows = false
	flashLight.Parent = flash
	local size = strong and 11 or 6
	TweenService:Create(flash, TweenInfo.new(IMPACT.flashLife, Enum.EasingStyle.Quad, Enum.EasingDirection.Out), {
		Size = Vector3.new(size, size, size),
		Transparency = 1,
	}):Play()
	TweenService:Create(flashLight, TweenInfo.new(IMPACT.flashLife), { Brightness = 0 }):Play()
	Debris:AddItem(flash, IMPACT.flashLife + 0.05)

	-- explosão em BLOCOS: cubinhos que voam e somem (nada de partícula cara)
	local want = strong and IMPACT.strongShards or IMPACT.weakShards
	local n = math.min(want, MAX_DEBRIS - debrisAlive)
	if n > 0 then
		debrisAlive += n
		for _ = 1, n do
			local s = 0.35 + math.random() * 0.45
			local shard = Instance.new("Part")
			shard.Size = Vector3.new(s, s, s)
			shard.Color = e.color
			decor(shard)
			shard.Anchored = false -- só estes caem: a gravidade faz a graça
			shard.CFrame = CFrame.new(pos + Vector3.new(math.random() - 0.5, math.random() * 0.6, math.random() - 0.5))
				* CFrame.Angles(math.random() * 6, math.random() * 6, 0)
			shard.AssemblyLinearVelocity = Vector3.new(
				(math.random() - 0.5) * 40,
				12 + math.random() * 18,
				(math.random() - 0.5) * 40
			)
			TweenService:Create(shard, TweenInfo.new(IMPACT.shardLife), { Transparency = 1 }):Play()
			Debris:AddItem(shard, IMPACT.shardLife)
		end
		task.delay(IMPACT.shardLife + 0.1, function()
			debrisAlive -= n
		end)
	end

	addTrauma(pos, strong)
end

-- ------------------------------------------------------------------ números
local function damageNumber(payload: any)
	if typeof(payload) ~= "table" then
		return
	end
	local pos = payload.pos
	local amount = tonumber(payload.amount) or 0
	if typeof(pos) ~= "Vector3" or amount <= 0 or numbersAlive >= MAX_NUMBERS then
		return
	end
	numbersAlive += 1

	local anchor = Instance.new("Part")
	anchor.Size = Vector3.new(0.2, 0.2, 0.2)
	anchor.Transparency = 1
	decor(anchor)
	anchor.CFrame = CFrame.new(pos + Vector3.new((math.random() - 0.5) * 1.5, 0, (math.random() - 0.5) * 1.5))

	local big = payload.crit == true or amount >= DMG_NUM.bigAt
	local gui = Instance.new("BillboardGui")
	gui.Size = UDim2.fromScale(big and 5 or 4, big and 2.4 or 2)
	gui.AlwaysOnTop = true
	gui.MaxDistance = DMG_NUM.maxDistance
	gui.Parent = anchor

	local text = Instance.new("TextLabel")
	text.BackgroundTransparency = 1
	text.Size = UDim2.fromScale(1, 1)
	text.Font = Enum.Font.GothamBold
	text.Text = tostring(math.max(1, math.round(amount)))
	text.TextColor3 = big and GOLD or Color3.new(1, 1, 1)
	text.TextScaled = true
	text.TextStrokeColor3 = NIGHT_DEEP
	text.TextStrokeTransparency = 0.1
	text.Parent = gui

	local info = TweenInfo.new(DMG_NUM.life, Enum.EasingStyle.Quad, Enum.EasingDirection.Out)
	TweenService:Create(anchor, info, { CFrame = anchor.CFrame + Vector3.new(0, DMG_NUM.rise, 0) }):Play()
	TweenService:Create(text, info, { TextTransparency = 1, TextStrokeTransparency = 1 }):Play()
	Debris:AddItem(anchor, DMG_NUM.life)
	task.delay(DMG_NUM.life, function()
		numbersAlive -= 1
	end)
end

-- ------------------------------------------------------------------ API
function Vfx.init(player: Player)
	if localPlayer then
		return
	end
	localPlayer = player
	bin()

	table.insert(conns, Net.event(Net.Names.SpellSpawned).OnClientEvent:Connect(spawnProjectile))
	table.insert(conns, Net.event(Net.Names.SpellImpact).OnClientEvent:Connect(impact))
	table.insert(conns, Net.event(Net.Names.DamageNumber).OnClientEvent:Connect(damageNumber))

	-- Screen shake DEPOIS da câmera padrão do Roblox — em RenderStepped o script
	-- de câmera sobrescreveria o offset e o shake sumiria.
	RunService:BindToRenderStep(SHAKE_BINDING, Enum.RenderPriority.Camera.Value + 10, function(dt: number)
		if trauma <= 0 then
			return
		end
		trauma = math.max(0, trauma - dt * SHAKE.decay)
		local cam = workspace.CurrentCamera
		if not cam then
			return
		end
		local s = trauma * trauma -- decai quadrático: o fim do tranco é suave
		cam.CFrame = cam.CFrame
			* CFrame.new(jitter(s, SHAKE.maxOffset), jitter(s, SHAKE.maxOffset), 0)
			* CFrame.Angles(
				math.rad(jitter(s, SHAKE.maxAngleDeg)),
				math.rad(jitter(s, SHAKE.maxAngleDeg)),
				math.rad(jitter(s, SHAKE.maxAngleDeg))
			)
	end)
end

function Vfx.update(dt: number)
	for id, p in pairs(projectiles) do
		p.age += dt
		if p.age >= PROJ_LIFE or not p.part.Parent then
			killProjectile(id)
		else
			p.pos += p.dir * (p.speed * dt)
			p.part.CFrame = CFrame.lookAt(p.pos, p.pos + p.dir) * shapeOffset(p.shape, p.age)
		end
	end
end

--- Some com tudo: parts, conexões e o binding de câmera. Nada vaza entre
--- respawns nem entre partidas.
function Vfx.destroy()
	for _, c in ipairs(conns) do
		c:Disconnect()
	end
	table.clear(conns)
	for id in pairs(projectiles) do
		killProjectile(id)
	end
	projCount = 0
	debrisAlive = 0
	numbersAlive = 0
	trauma = 0
	pcall(RunService.UnbindFromRenderStep, RunService, SHAKE_BINDING)
	if folder then
		folder:Destroy()
		folder = nil
	end
	localPlayer = nil
end

return Vfx
