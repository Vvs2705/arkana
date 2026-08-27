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
-- estilhaços e números de dano; sombra e colisão desligadas em tudo. Todo
-- efeito novo passa pelo MODO NÉVOA (`lite()`): em aparelho fraco caem
-- partícula, luz extra e metade dos estilhaços — nunca a leitura do combate.
-- ============================================================================

local ReplicatedStorage = game:GetService("ReplicatedStorage")
local RunService = game:GetService("RunService")
local TweenService = game:GetService("TweenService")
local UserInputService = game:GetService("UserInputService")
local Debris = game:GetService("Debris")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Elements = require(Shared:WaitForChild("Elements"))
local Net = require(Shared:WaitForChild("Net"))

-- ------------------------------------------------------------- modo névoa
--- O knob de qualidade mora no Atmosphere (um só para o Diretor mexer). Require
--- PREGUIÇOSO E PROTEGIDO — mesmo padrão do Main com os módulos opcionais: se o
--- Atmosphere não estiver no lugar, o VFX continua de pé com o palpite abaixo.
local atmos: any = nil
local FALLBACK_LITE = UserInputService.TouchEnabled and not UserInputService.KeyboardEnabled

local function atmosphere(): any
	if atmos == nil then
		local mod = script.Parent and script.Parent:FindFirstChild("Atmosphere")
		local ok, m = pcall(function()
			return require(mod :: ModuleScript)
		end)
		atmos = (ok and m) or false
	end
	return atmos or nil
end

--- true = cortar partícula, luz extra e estilhaço (GDD §19.2). Consultado a
--- cada efeito, não uma vez só: o vigia de fps pode rebaixar no meio da partida.
local function lite(): boolean
	local a = atmosphere()
	if a and a.isLite then
		local ok, v = pcall(a.isLite)
		if ok then
			return v == true
		end
	end
	return FALLBACK_LITE
end

-- ------------------------------------------------------------------ orçamento
local MAX_PROJECTILES = 40
local MAX_DEBRIS = 72
local MAX_NUMBERS = 24
local PROJ_LIFE = 3.0 -- s: rede perdida/impacto não avisado não vira lixo eterno

local IMPACT = {
	--- estilhaços: o modo leve corta pela metade (o teto duro continua o mesmo)
	weakShards = 8,
	strongShards = 16,
	weakShardsLite = 3,
	strongShardsLite = 6,
	shardLife = 0.6,
	flashLife = 0.16, -- mais curto e mais forte: flash longo vira borrão
	waveLife = 0.34,
	weakWave = 12, -- diâmetro final da onda de choque, em studs
	strongWave = 24,
	--- alcance em studs onde o impacto ainda sacode a câmera
	shakeRange = 70,
	traumaWeak = 0.22,
	traumaStrong = 0.5,
}

--- Flash de conjuração na ponta do cajado. O `origin` do SpellSpawned JÁ é a
--- ponta (o servidor soma o MUZZLE) — não precisamos caçar o braço do avatar.
local CAST = {
	life = 0.18,
	weakDisc = 3.4, -- diâmetro final do disco, em studs
	strongDisc = 5.6,
}

--- Sintonia disparada: o momento mais importante do jogo (GDD §9). Grande,
--- vertical e de cor do combo — impossível confundir com um tiro comum.
local SINTONIA = {
	pillarHeight = 120,
	pillarLife = 0.9,
	domeLife = 0.55,
	ringLife = 0.7,
	shards = 18,
	shardLife = 1.1,
	trauma = 0.85,
	shakeRange = 160, -- ninguém perde a Sintonia por estar longe
}

local SHAKE = {
	decay = 1.8, -- trauma/s
	maxAngleDeg = 2.6,
	maxOffset = 0.35, -- studs
}

local DMG_NUM = {
	rise = 4.5, -- studs
	life = 0.75,
	bigAt = 15, -- dano "grande" = dourado e maior
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
	--- pulsação (só no modo cheio): guardamos o tamanho base para não somar erro
	baseSize: Vector3?,
	light: PointLight?,
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

--- Disco/cilindro deitado ou de pé. O eixo do Cylinder é o X LOCAL, então
--- girá-lo 90° em Z põe a face virada para cima (onda no chão) e 90° em Y põe
--- a face virada para a frente (flash de cano).
local FLAT = CFrame.Angles(0, 0, math.rad(90))
local FACE = CFrame.Angles(0, math.rad(90), 0)

local function disc(color: Color3, thickness: number, diameter: number): BasePart
	local p = Instance.new("Part")
	p.Shape = Enum.PartType.Cylinder
	p.Size = Vector3.new(thickness, diameter, diameter)
	p.Color = color
	decor(p)
	p.Material = Enum.Material.Neon -- neon = passa do Threshold do bloom = magia
	return p
end

--- Luz curta que morre junto com o efeito. No modo leve a luz sai: PointLight
--- é dos itens mais caros do frame em celular.
local function blink(parent: BasePart, color: Color3, brightness: number, range: number, life: number)
	if lite() then
		return
	end
	local l = Instance.new("PointLight")
	l.Color = color
	l.Brightness = brightness
	l.Range = range
	l.Shadows = false
	l.Parent = parent
	TweenService:Create(l, TweenInfo.new(life), { Brightness = 0 }):Play()
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
local function shapeOffset(shape: string, age: number): CFrame
	if shape == "Ring" then
		return FACE -- disco simétrico: girá-lo no próprio eixo não se vê
	elseif shape == "Block" then
		return CFrame.Angles(age * 5, age * 4, 0)
	elseif shape == "Bolt" then
		return CFrame.Angles(0, 0, age * 14)
	end
	return CFrame.identity
end

--- FLASH DE CONJURAÇÃO: disco neon na ponta do cajado, virado para o trajeto.
--- Sem isso o tiro "nasce seco" — este é o feedback de que a magia SAIU de mim.
local function castFlash(origin: Vector3, dir: Vector3, e: any, strong: boolean)
	local d = if strong then CAST.strongDisc else CAST.weakDisc
	local flash = disc(e.glow, 0.3, d * 0.3)
	flash.Transparency = 0.05
	flash.CFrame = CFrame.lookAt(origin, origin + dir) * FACE
	TweenService:Create(flash, TweenInfo.new(CAST.life, Enum.EasingStyle.Quad, Enum.EasingDirection.Out), {
		Size = Vector3.new(0.05, d, d),
		Transparency = 1,
	}):Play()
	-- luz só na TÁTICA: o básico é spammável e a luz do próprio projétil (que
	-- nasce nesta mesma posição) já acende o cano. Orçamento de PointLight é
	-- curto em celular — gastar no tiro comum é o jeito rápido de perder fps.
	if strong then
		blink(flash, e.glow, 5, 16, CAST.life)
	end
	Debris:AddItem(flash, CAST.life + 0.05)
end

--- Partículas acompanhando o projétil, na cor do elemento. Só no modo cheio:
--- 40 projéteis × emissor é o primeiro lugar onde um celular fraco engasga.
local function attachSparks(part: BasePart, e: any)
	local pe = Instance.new("ParticleEmitter")
	pe.Color = ColorSequence.new(e.glow, e.color)
	pe.Size = NumberSequence.new({
		NumberSequenceKeypoint.new(0, 0.85),
		NumberSequenceKeypoint.new(1, 0),
	})
	pe.Transparency = NumberSequence.new({
		NumberSequenceKeypoint.new(0, 0.15),
		NumberSequenceKeypoint.new(1, 1),
	})
	pe.Lifetime = NumberRange.new(0.16, 0.26)
	pe.Rate = 18 -- ~4 vivas por projétil: cauda densa sem custo de fogos
	pe.Speed = NumberRange.new(0, 1.5)
	pe.SpreadAngle = Vector2.new(20, 20)
	pe.Rotation = NumberRange.new(0, 360)
	pe.RotSpeed = NumberRange.new(-140, 140)
	pe.LightEmission = 0.8
	pe.LightInfluence = 0 -- cor chapada: a partícula não escurece na sombra
	pe.Parent = part
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

	local e = element(payload.element)
	local strong = payload.strong == true
	-- o flash sai ANTES do teto: mesmo com a tela cheia de projéteis, o jogador
	-- precisa ver que o próprio disparo aconteceu
	castFlash(origin, dir, e, strong)

	if projCount >= MAX_PROJECTILES then
		return -- teto de partículas: o servidor continua certo, só não desenhamos
	end

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

	-- trilha em blocos: dois pontos e um Trail. Mais longa e afinando no fim —
	-- a cauda é o que faz um projétil lento parecer rápido.
	local a0 = Instance.new("Attachment")
	a0.Position = Vector3.new(0, 0.5, 0)
	a0.Parent = part
	local a1 = Instance.new("Attachment")
	a1.Position = Vector3.new(0, -0.5, 0)
	a1.Parent = part
	local trail = Instance.new("Trail")
	trail.Attachment0 = a0
	trail.Attachment1 = a1
	trail.Color = ColorSequence.new(e.glow, e.color)
	trail.Transparency = NumberSequence.new(0.15, 1)
	trail.WidthScale = NumberSequence.new({
		NumberSequenceKeypoint.new(0, 1),
		NumberSequenceKeypoint.new(1, 0.15),
	})
	trail.Lifetime = 0.38
	trail.LightEmission = 0.75
	trail.LightInfluence = 0
	trail.FaceCamera = true
	trail.Parent = part

	local rich = not lite()
	if rich then
		attachSparks(part, e)
	end

	projectiles[id] = {
		part = part,
		shape = e.shape,
		pos = origin,
		dir = dir,
		speed = math.clamp(tonumber(payload.speed) or 60, 1, 400),
		age = 0,
		baseSize = if rich then part.Size else nil,
		light = light,
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

--- ACESSIBILIDADE (costura do coordenador, 18/08): as preferências de conforto
--- existiam mas não chegavam a quem DESENHA — então "reduzir tremor" não
--- reduzia tremor nenhum. Require preguiçoso e protegido, o mesmo padrão que
--- este arquivo já usa para o Atmosphere: sem o módulo, tudo vale 1 e o
--- comportamento é o de antes.
local a11y: any = nil
local function pref(name: string): number
	if a11y == nil then
		local mod = script.Parent and script.Parent:FindFirstChild("Accessibility")
		local ok, m = pcall(function()
			return require(mod :: ModuleScript)
		end)
		a11y = (ok and m) or false
	end
	if a11y and a11y[name] then
		local ok, v = pcall(a11y[name])
		if ok and typeof(v) == "number" then
			return math.clamp(v, 0, 1)
		end
	end
	return 1
end

local function addTrauma(pos: Vector3, amount: number, range: number)
	local plr = localPlayer
	local char = plr and plr.Character
	local root = char and char:FindFirstChild("HumanoidRootPart") :: BasePart?
	if not root then
		return
	end
	local d = (root.Position - pos).Magnitude
	if d > range then
		return
	end
	-- impacto pequeno ≠ tática, e longe sacode menos que no colo. O fator de
	-- conforto entra AQUI, na fonte: quem pediu tremor zero não recebe nenhum.
	trauma = math.min(1, trauma + amount * (1 - d / range) * pref("shake"))
end

--- Estilhaços com física de verdade (Anchored = false): custam pouco e são o
--- que dá peso ao impacto. Devolve quantos saíram (para devolver ao orçamento).
local function shards(pos: Vector3, color: Color3, want: number, life: number, up: number): number
	local n = math.min(want, MAX_DEBRIS - debrisAlive)
	if n <= 0 then
		return 0
	end
	debrisAlive += n
	for _ = 1, n do
		local s = 0.35 + math.random() * 0.45
		local shard = Instance.new("Part")
		shard.Size = Vector3.new(s, s, s)
		shard.Color = color
		decor(shard)
		shard.Anchored = false -- só estes caem: a gravidade faz a graça
		shard.CFrame = CFrame.new(pos + Vector3.new(math.random() - 0.5, math.random() * 0.6, math.random() - 0.5))
			* CFrame.Angles(math.random() * 6, math.random() * 6, 0)
		shard.AssemblyLinearVelocity =
			Vector3.new((math.random() - 0.5) * 40, up + math.random() * 18, (math.random() - 0.5) * 40)
		TweenService:Create(shard, TweenInfo.new(life), { Transparency = 1 }):Play()
		Debris:AddItem(shard, life)
	end
	task.delay(life + 0.1, function()
		debrisAlive -= n
	end)
	return n
end

--- Onda de choque: disco neon deitado que abre no chão e some. UMA part —
--- o anel em blocos custa 8 e lê igual a esta distância de jogo.
local function shockwave(pos: Vector3, color: Color3, diameter: number, life: number)
	local wave = disc(color, 0.5, diameter * 0.15)
	wave.Transparency = 0.1
	wave.CFrame = CFrame.new(pos) * FLAT
	TweenService:Create(wave, TweenInfo.new(life, Enum.EasingStyle.Quint, Enum.EasingDirection.Out), {
		Size = Vector3.new(0.05, diameter, diameter),
		Transparency = 1,
	}):Play()
	Debris:AddItem(wave, life + 0.05)
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
	local isLite = lite()

	-- flash: bola neon que abre e some. Curto e FORTE — o olho lê o pico, não
	-- a duração; flash demorado só vira borrão na tela pequena.
	local flash = Instance.new("Part")
	flash.Shape = Enum.PartType.Ball
	flash.Size = Vector3.new(1, 1, 1)
	flash.Color = e.glow
	decor(flash)
	flash.Material = Enum.Material.Neon
	flash.CFrame = CFrame.new(pos)
	local flashLight = Instance.new("PointLight")
	flashLight.Color = e.glow
	flashLight.Brightness = strong and 9 or 5
	flashLight.Range = strong and 32 or 18
	flashLight.Shadows = false
	flashLight.Parent = flash
	local size = strong and 13 or 7
	TweenService:Create(flash, TweenInfo.new(IMPACT.flashLife, Enum.EasingStyle.Quad, Enum.EasingDirection.Out), {
		Size = Vector3.new(size, size, size),
		Transparency = 1,
	}):Play()
	TweenService:Create(flashLight, TweenInfo.new(IMPACT.flashLife), { Brightness = 0 }):Play()
	Debris:AddItem(flash, IMPACT.flashLife + 0.05)

	-- onda de choque no chão: é ela que diz "ONDE" o impacto pegou
	shockwave(pos, e.glow, if strong then IMPACT.strongWave else IMPACT.weakWave, IMPACT.waveLife)

	-- explosão em BLOCOS: cubinhos que voam e somem (nada de partícula cara)
	local want = if isLite
		then (if strong then IMPACT.strongShardsLite else IMPACT.weakShardsLite)
		else (if strong then IMPACT.strongShards else IMPACT.weakShards)
	-- intensidade de VFX escolhida pelo jogador (acessibilidade). Piso de 1
	-- estilhaço: o impacto continua LEGÍVEL mesmo no mínimo — reduzir conforto
	-- não pode virar desvantagem competitiva.
	want = math.max(1, math.floor(want * pref("vfx") + 0.5))
	shards(pos, e.color, want, IMPACT.shardLife, 12)

	addTrauma(pos, if strong then IMPACT.traumaStrong else IMPACT.traumaWeak, IMPACT.shakeRange)
end

-- ------------------------------------------------------------------ sintonia
--- A FUSÃO (GDD §9): pilar de luz + domo + onda gigante na cor do combo, mais
--- um soco de cor na tela inteira. Nada aqui se parece com um tiro comum — é o
--- ponto alto da partida e tem que ser visto do outro lado do mapa.
local function sintonia(payload: any)
	if typeof(payload) ~= "table" or payload.phase ~= "fired" then
		return
	end
	local pos = payload.pos
	if typeof(pos) ~= "Vector3" then
		return
	end
	local combo = Elements.Combos[tostring(payload.comboKey)]
	local col = (combo and combo.color) or GOLD
	local radius = math.clamp(tonumber(payload.radius) or 16, 4, 60)
	local isLite = lite()

	-- 1) PILAR vertical: a leitura à distância. Some subindo.
	local pillar = disc(col, SINTONIA.pillarHeight, radius * 0.5)
	pillar.Transparency = 0.25
	pillar.CFrame = CFrame.new(pos + Vector3.new(0, SINTONIA.pillarHeight * 0.5, 0)) * FLAT
	TweenService:Create(pillar, TweenInfo.new(SINTONIA.pillarLife, Enum.EasingStyle.Quad, Enum.EasingDirection.Out), {
		Size = Vector3.new(SINTONIA.pillarHeight, radius * 1.6, radius * 1.6),
		Transparency = 1,
	}):Play()
	blink(pillar, col, 12, math.min(60, radius * 3), SINTONIA.pillarLife)
	Debris:AddItem(pillar, SINTONIA.pillarLife + 0.05)

	-- 2) DOMO: a esfera que marca o raio real do combo (é onde doeu). Fora do
	-- modo leve: uma esfera translúcida de 40+ studs tapa a tela inteira e
	-- fill-rate é justamente onde o celular fraco morre. Lá o pilar + a onda no
	-- chão já contam a mesma história.
	if not isLite then
		local dome = Instance.new("Part")
		dome.Shape = Enum.PartType.Ball
		dome.Size = Vector3.new(2, 2, 2)
		dome.Color = col
		decor(dome)
		dome.Material = Enum.Material.Neon
		dome.Transparency = 0.45
		dome.CFrame = CFrame.new(pos)
		TweenService:Create(dome, TweenInfo.new(SINTONIA.domeLife, Enum.EasingStyle.Quint, Enum.EasingDirection.Out), {
			Size = Vector3.new(radius * 2, radius * 2, radius * 2),
			Transparency = 1,
		}):Play()
		Debris:AddItem(dome, SINTONIA.domeLife + 0.05)
	end

	-- 3) ONDA no chão, no diâmetro do efeito
	shockwave(pos, col, radius * 2.4, SINTONIA.ringLife)

	if not isLite then
		shards(pos, col, SINTONIA.shards, SINTONIA.shardLife, 26)
	end

	addTrauma(pos, SINTONIA.trauma, SINTONIA.shakeRange)

	-- 4) a tela inteira toma a cor do combo por um instante (Atmosphere é dono
	-- do color grading; se ele não estiver carregado, o resto continua valendo).
	-- A FORÇA vem da preferência "FLASHES": este é o MAIOR flash do jogo (tela
	-- inteira) e era o único lugar que ignorava o menu de acessibilidade — ia
	-- com 1 fixo. Em 0 o `Atmosphere.pulse` não acende nada; o pilar, o domo e a
	-- legenda continuam contando que a Sintonia saiu.
	local a = atmosphere()
	if a and a.pulse then
		pcall(a.pulse, col, pref("flash"), 0.75)
	end
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
	-- a SintoniaUi também escuta este remote (ela desenha o banner, nós o mundo)
	table.insert(conns, Net.event(Net.Names.SintoniaState).OnClientEvent:Connect(sintonia))

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
			-- pulsação: a magia "respira" enquanto voa. baseSize só existe no
			-- modo cheio — no leve o projétil é sólido e não custa nada por frame.
			local base = p.baseSize
			if base then
				local k = math.sin(p.age * 16)
				p.part.Size = base * (1 + 0.1 * k)
				local l = p.light
				if l then
					l.Brightness = 2 + 1.1 * k
				end
			end
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
