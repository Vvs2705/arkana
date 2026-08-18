--!strict
-- ============================================================================
-- RAIA (R3) — BOTS DE PREENCHIMENTO (GDD §19.6: "bots nas primeiras partidas",
-- retenção de dia 1). Corpo montado em BLOCOS por código (rig R6 de 7 parts —
-- ZERO Toolbox, zero modelo de terceiros, docs/ROBLOX.md §2) e a MESMA FSM já
-- validada no protótipo 2D (src/entities/Bot.ts):
--     WANDER → CHASE → ATTACK, com FLEE_FIRE em prioridade máxima
-- e um estado a mais que só existe no BR: TO_ZONE (correr para dentro da zona).
--
-- AUTORIDADE: roda 100% no servidor. O bot NUNCA escreve em Humanoid.Health —
-- todo dano (tiro dele, fogo que ele pisa) passa por Combat.applyDamage.
-- Todo número de balanceamento vem de Balance.bots; o bloco TUNE abaixo é
-- comportamento (portado do TUNE do Bot.ts 2D), não balanceamento.
-- ============================================================================

local Debris = game:GetService("Debris")
local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local Workspace = game:GetService("Workspace")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Balance = require(Shared:WaitForChild("Balance"))
local Elements = require(Shared:WaitForChild("Elements"))
local Grid = require(Shared:WaitForChild("Grid"))
local Net = require(Shared:WaitForChild("Net"))

--- Times: o bot preenche partida (GDD §19.6) e, em duo ímpar, vira PARCEIRO de
--- um humano. Sem esta consulta a lista de alvos era global e ele metralhava o
--- próprio parceiro. Require direto (o Teams não depende de ninguém).
local Teams = require((script.Parent :: Instance):WaitForChild("Teams") :: ModuleScript)

local B = Balance.bots

-- --- comportamento (porte do TUNE de src/entities/Bot.ts) -------------------
local TUNE = {
	--- fração da velocidade em WANDER ("anda devagar")
	wanderSpeedMult = 0.4,
	wanderDirMin = 1.0,
	wanderDirMax = 2.0,
	--- CHASE → WANDER após 3s sem ver ninguém
	loseSight = 3.0,
	--- erro de mira de ±6° (bot não é aimbot — GDD §19.6: novato ganha confiança)
	aimErrorDeg = 6,
	--- "para/anda pouco" no ATTACK: strafe perpendicular suave
	attackStrafeMult = 0.25,
	fleeTime = 1.0,
	fireScan = 0.2,
	hazardTick = 0.25,
	--- o Humanoid:MoveTo tem timeout nativo de 8s: reemitir a ordem mantém o passo
	moveRefresh = 0.15,
	--- distância do ponto de destino do MoveTo (longe = passo liso, perto = trava)
	moveLead = 10,
	muzzle = 2.5,
	--- sub-passo da varredura do tiro (mesma regra do Combat: < meia célula)
	substep = Grid.CELL_SIZE * 0.5,
	maxRange = 90,
	--- folga de corpo no teste centro-a-centro
	hitPad = 2,
	corpseLife = 4,
	--- margem para o bot decidir "estou fora da zona" antes de tomar dano
	zoneMargin = 0.9,
	--- altura do olho/cano (o tiro nasce na altura do peito, como o do jogador)
	muzzleY = 1.5,
} :: { [string]: number }

-- ------------------------------------------------------------------- tipos
export type Bot = {
	id: number,
	name: string,
	model: Model,
	humanoid: Humanoid,
	root: BasePart,
	element: Elements.Id,
	state: string,
	wanderDir: Vector3,
	nextWanderAt: number,
	lastSeenAt: number,
	nextShotAt: number,
	strafePhase: number,
	fleeDir: Vector3,
	fleeUntil: number,
	scanAcc: number,
	hazardAcc: number,
	moveAcc: number,
	--- números do Selo do Campeão (GDD §18.6) quando quem vence é um bot
	kills: number,
	damage: number,
}

type Shot = {
	id: number,
	bot: Bot,
	element: Elements.Id,
	pos: Vector3,
	dir: Vector3,
	traveled: number,
}

--- alvo válido publicado pelo Match (jogadores vivos + bots vivos)
export type Target = { humanoid: Humanoid, root: BasePart }

local Bots = {}

local terrain: any = nil
local combat: any = nil
local getTargets: (() -> { Target })? = nil
--- lista de alvos FOTOGRAFADA uma vez por step. Sem isto, cada bot e cada
--- sub-passo de cada tiro remontaria a lista inteira — 11 bots a 60fps custam
--- caro por nada, e a fotografia é idêntica dentro do mesmo frame.
local frameTargets: { Target } = {}

local bots: { Bot } = {}
local shots: { Shot } = {}
local folder: Folder? = nil
local nextId = 0
--- ids NEGATIVOS: o Vfx do cliente indexa projéteis por id e o Combat usa os
--- positivos — assim os dois nunca colidem na mesma tabela.
local nextShotId = 0
local rng = Random.new()

local zoneCenter = Vector3.zero
local zoneRadius = math.huge

local evSpawned: RemoteEvent
local evImpact: RemoteEvent

-- ------------------------------------------------------------------ helpers
local function flatDist(a: Vector3, b: Vector3): number
	local dx, dz = a.X - b.X, a.Z - b.Z
	return math.sqrt(dx * dx + dz * dz)
end

local function flatDir(from: Vector3, to: Vector3): Vector3
	local d = Vector3.new(to.X - from.X, 0, to.Z - from.Z)
	return if d.Magnitude > 1e-3 then d.Unit else Vector3.new(0, 0, -1)
end

local function randomDir(): Vector3
	local a = rng:NextNumber() * math.pi * 2
	return Vector3.new(math.cos(a), 0, math.sin(a))
end

-- --------------------------------------------------------------------- times
--- Membro de time de um Humanoid. ATENÇÃO à assimetria do roster (Match): para
--- HUMANO quem entra no time é o PLAYER — o Humanoid dele não está na tabela —,
--- para bot é o próprio Humanoid. Perguntar com a chave errada devolve "sem
--- time", e sem time todo mundo vira inimigo (que é exatamente o bug de hoje).
local function memberOf(hum: Humanoid): Teams.Member
	local char = hum.Parent
	local player = if char then Players:GetPlayerFromCharacter(char :: Instance) else nil
	return player or hum
end

--- É inimigo? No LOBBY (nenhum time montado) `areAllies` é falso para todos e o
--- bot se comporta como antes — lado seguro do erro: nunca deixa de reagir.
local function isFoe(bot: Bot, hum: Humanoid): boolean
	return not Teams.areAllies(memberOf(bot.humanoid), memberOf(hum))
end

-- ------------------------------------------------------------- corpo blocky
--- Rig R6 montado à mão: 7 parts + 6 Motor6D. Nomes são contrato do engine
--- (Humanoid exige HumanoidRootPart/Torso/Head) — não renomear.
local function buildBody(name: string, color: Color3): (Model, Humanoid, BasePart)
	local model = Instance.new("Model")
	model.Name = name

	local function part(pname: string, size: Vector3, c: Color3, collide: boolean): BasePart
		local p = Instance.new("Part")
		p.Name = pname
		p.Size = size
		p.Color = c
		p.Material = Enum.Material.SmoothPlastic -- cor chapada (direção blocky)
		p.TopSurface = Enum.SurfaceType.Smooth
		p.BottomSurface = Enum.SurfaceType.Smooth
		p.CanCollide = collide
		p.Parent = model
		return p
	end

	local robe = color:Lerp(Color3.fromHex("#0B1026"), 0.55)
	local root = part("HumanoidRootPart", Vector3.new(2, 2, 1), color, false)
	root.Transparency = 1
	local torso = part("Torso", Vector3.new(2, 2, 1), robe, true)
	local head = part("Head", Vector3.new(1.4, 1.4, 1.4), Color3.fromHex("#E8E6F0"), true)
	local rArm = part("Right Arm", Vector3.new(1, 2, 1), color, true)
	local lArm = part("Left Arm", Vector3.new(1, 2, 1), color, true)
	local rLeg = part("Right Leg", Vector3.new(1, 2, 1), robe, true)
	local lLeg = part("Left Leg", Vector3.new(1, 2, 1), robe, true)

	local function joint(jname: string, p0: BasePart, p1: BasePart, c0: CFrame, c1: CFrame)
		local m = Instance.new("Motor6D")
		m.Name = jname
		m.Part0 = p0
		m.Part1 = p1
		m.C0 = c0
		m.C1 = c1
		m.Parent = p0
	end

	local flip = CFrame.Angles(-math.pi / 2, 0, math.pi)
	joint("RootJoint", root, torso, flip, flip)
	joint("Neck", torso, head, CFrame.new(0, 1, 0) * flip, CFrame.new(0, -head.Size.Y / 2, 0) * flip)
	local right = CFrame.Angles(0, math.pi / 2, 0)
	local left = CFrame.Angles(0, -math.pi / 2, 0)
	joint("Right Shoulder", torso, rArm, CFrame.new(1, 0.5, 0) * right, CFrame.new(-0.5, 0.5, 0) * right)
	joint("Left Shoulder", torso, lArm, CFrame.new(-1, 0.5, 0) * left, CFrame.new(0.5, 0.5, 0) * left)
	joint("Right Hip", torso, rLeg, CFrame.new(1, -1, 0) * right, CFrame.new(0.5, 1, 0) * right)
	joint("Left Hip", torso, lLeg, CFrame.new(-1, -1, 0) * left, CFrame.new(-0.5, 1, 0) * left)

	local hum = Instance.new("Humanoid")
	hum.RigType = Enum.HumanoidRigType.R6
	hum.MaxHealth = B.hp
	hum.Health = B.hp
	hum.WalkSpeed = B.speed
	hum.DisplayName = name
	hum.Parent = model

	model.PrimaryPart = root
	return model, hum, root
end

-- ------------------------------------------------------------------ percepção
--- Célula que ESCONDE (grama alta em pé): o raio de visão despenca — é a mesma
--- regra do 2D e o que faz "queimar a grama revela quem se esconde" valer.
local function sightRadiusFor(pos: Vector3): number
	local cell = terrain.getCell(Grid.worldToCell(pos))
	if cell and cell.material == "TallGrass" and cell.state == "Normal" then
		return B.sightConcealed
	end
	return B.sightRadius
end

local function nearestTarget(bot: Bot): Target?
	local list = frameTargets
	local pos = bot.root.Position
	local best: Target? = nil
	local bestD = math.huge
	for _, t in ipairs(list) do
		if t.humanoid == bot.humanoid or t.humanoid.Health <= 0 or not t.root.Parent then
			continue
		end
		if not isFoe(bot, t.humanoid) then
			continue -- PARCEIRO DE DUO não é alvo (a lista do Match é global)
		end
		local d = flatDist(pos, t.root.Position)
		if d < bestD and d <= sightRadiusFor(t.root.Position) then
			best, bestD = t, d
		end
	end
	return best
end

--- Varre as células ao redor: havendo perigo (fogo/eletricidade), define a
--- direção de fuga oposta ao centro de massa do perigo. Porte 1:1 do 2D.
local function scanFire(bot: Bot): boolean
	local pos = bot.root.Position
	local cx, cy = Grid.worldToCell(pos)
	local r = B.fleeRadiusCells
	local sum = Vector3.zero
	local found = 0
	for dy = -r, r do
		for dx = -r, r do
			local x, y = cx + dx, cy + dy
			if not Grid.inBounds(x, y) then
				continue
			end
			local w = Grid.cellToWorld(x, y)
			if (terrain.hazardAt(w)) > 0 then
				sum += Vector3.new(w.X - pos.X, 0, w.Z - pos.Z)
				found += 1
			end
		end
	end
	if found == 0 then
		return false
	end
	-- fogo debaixo dos pés (soma ~zero): foge para qualquer lado
	bot.fleeDir = if sum.Magnitude < 1 then randomDir() else -sum.Unit
	return true
end

-- ------------------------------------------------------------------- tiro
local function shoot(bot: Bot, target: Target)
	local from = bot.root.Position + Vector3.new(0, TUNE.muzzleY, 0)
	local base = flatDir(from, target.root.Position)
	local err = math.rad(rng:NextNumber(-TUNE.aimErrorDeg, TUNE.aimErrorDeg))
	local dir = (CFrame.Angles(0, err, 0) * base).Unit

	nextShotId -= 1
	table.insert(shots, {
		id = nextShotId,
		bot = bot,
		element = bot.element,
		pos = from + dir * TUNE.muzzle,
		dir = dir,
		traveled = 0,
	})
	evSpawned:FireAllClients({
		id = nextShotId,
		element = bot.element,
		origin = from + dir * TUNE.muzzle,
		dir = dir,
		speed = B.projSpeed,
		strong = false,
	})
end

local function shotImpact(shot: Shot, hit: Humanoid?)
	if hit then
		-- o dano SEMPRE passa pelo Combat (escudo, i-frames, número na tela).
		-- O 5º argumento é o AUTOR sem Player: sem ele, tiro de bot chegava na
		-- telemetria como "ambiente" e se misturava com zona e terreno (achado do
		-- Telemetry.coverage). O elemento sai do `registerTarget` do próprio bot.
		local before = hit.Health
		combat.applyDamage(hit, B.dmg, nil, false, shot.bot.humanoid)
		-- crédito para o Selo caso o campeão seja um bot. Mede só a perda de
		-- VIDA: o que o escudo do alvo absorveu não entra (contagem conservadora).
		local dealt = before - hit.Health
		shot.bot.damage += dealt
		if before > 0 and hit.Health <= 0 then
			shot.bot.kills += 1
		end
	end

	local cx, cy = Grid.worldToCell(shot.pos)
	if Grid.inBounds(cx, cy) then
		local terrainRadius = Balance.elements[shot.element].basic.terrainRadius
		terrain.applyElement(cx, cy, shot.element, terrainRadius, false)
	end
	evImpact:FireAllClients({ id = shot.id, element = shot.element, pos = shot.pos, strong = false })
end

local function shotHit(shot: Shot): Humanoid?
	local reach = B.projRadius + TUNE.hitPad
	for _, t in ipairs(frameTargets) do
		if t.humanoid == shot.bot.humanoid or t.humanoid.Health <= 0 or not t.root.Parent then
			continue
		end
		--- o tiro ATRAVESSA o aliado: mirar longe do parceiro não basta se ele
		--- cruzar a linha de fogo um segundo depois (é o mesmo bug, com atraso).
		if not isFoe(shot.bot, t.humanoid) then
			continue
		end
		if (t.root.Position - shot.pos).Magnitude < reach then
			return t.humanoid
		end
	end
	return nil
end

local function stepShots(dt: number)
	for i = #shots, 1, -1 do
		local shot = shots[i]
		local remaining = B.projSpeed * dt
		local done = false
		while remaining > 0 do
			local stepLen = math.min(TUNE.substep, remaining)
			remaining -= stepLen
			shot.pos += shot.dir * stepLen
			shot.traveled += stepLen

			local cx, cy = Grid.worldToCell(shot.pos)
			if not Grid.inBounds(cx, cy) or terrain.blocksProjectile(cx, cy) then
				shotImpact(shot, nil)
				done = true
				break
			end
			local hit = shotHit(shot)
			if hit then
				shotImpact(shot, hit)
				done = true
				break
			end
			if shot.traveled >= TUNE.maxRange then
				shotImpact(shot, nil)
				done = true
				break
			end
		end
		if done then
			table.remove(shots, i)
		end
	end
end

-- --------------------------------------------------------------------- FSM
local function move(bot: Bot, dir: Vector3, speedMult: number)
	--- MESMO produto do jogador (Combat.step): base × terreno × status. O bot é o
	--- inimigo que o playtest tem de sobra — se a lentidão dos combos não valesse
	--- aqui, ela seria invisível na maior parte das partidas.
	bot.humanoid.WalkSpeed = B.speed
		* speedMult
		* (terrain.speedMultAt(bot.root.Position) or 1)
		* combat.statusSpeedMult(bot.humanoid)
	if bot.moveAcc < TUNE.moveRefresh then
		return
	end
	bot.moveAcc = 0
	bot.humanoid:MoveTo(bot.root.Position + dir * TUNE.moveLead)
end

local function stepBot(bot: Bot, dt: number, now: number)
	local hum, root = bot.humanoid, bot.root
	local pos = root.Position
	bot.moveAcc += dt

	-- perigo do terreno na célula dos pés (o Terrain nunca aplica dano sozinho)
	bot.hazardAcc += dt
	while bot.hazardAcc >= TUNE.hazardTick do
		bot.hazardAcc -= TUNE.hazardTick
		local dps = terrain.hazardAt(pos)
		if dps > 0 then
			combat.applyDamage(hum, dps * TUNE.hazardTick, nil, false)
		end
		if hum.Health <= 0 then
			return
		end
	end

	-- PRIORIDADE 0: ATORDOADO não anda e não atira (kernel de status do Combat).
	-- Dura no máximo 0,8 s pelo teto da auditoria, então nem a zona perde muito.
	if combat.getStatus(hum, "Stun") > 0 then
		hum.WalkSpeed = 0
		return
	end

	-- PRIORIDADE 1: estar fora da zona mata mais rápido que qualquer inimigo
	if flatDist(pos, zoneCenter) > zoneRadius * TUNE.zoneMargin then
		bot.state = "TO_ZONE"
		move(bot, flatDir(pos, zoneCenter), 1)
		return
	end

	-- PRIORIDADE 2: fogo por perto (DoD do 2D: "reagem ao fogo")
	bot.scanAcc += dt
	if bot.state ~= "FLEE_FIRE" and bot.scanAcc >= TUNE.fireScan then
		bot.scanAcc = 0
		if scanFire(bot) then
			bot.state = "FLEE_FIRE"
			bot.fleeUntil = now + TUNE.fleeTime
		end
	end
	if bot.state == "FLEE_FIRE" then
		move(bot, bot.fleeDir, 1)
		if now >= bot.fleeUntil then
			-- reavalia: se ainda houver fogo, o scan re-dispara a fuga
			bot.state = "WANDER"
			bot.nextWanderAt = 0
			bot.scanAcc = TUNE.fireScan
		end
		return
	end

	local target = nearestTarget(bot)
	if target then
		bot.lastSeenAt = now
	end

	if bot.state == "WANDER" then
		if target then
			bot.state = "CHASE"
		else
			if now >= bot.nextWanderAt then
				bot.nextWanderAt = now + rng:NextNumber(TUNE.wanderDirMin, TUNE.wanderDirMax)
				-- 15% de pausa: bot parado o tempo todo em movimento parece robô
				bot.wanderDir = if rng:NextNumber() < 0.15 then Vector3.zero else randomDir()
			end
			if bot.wanderDir.Magnitude > 0 then
				move(bot, bot.wanderDir, TUNE.wanderSpeedMult)
			end
			return
		end
	end

	if not target then
		if now - bot.lastSeenAt > TUNE.loseSight then
			bot.state = "WANDER"
			bot.nextWanderAt = 0
		end
		return
	end

	local dist = flatDist(pos, target.root.Position)
	if dist > B.attackRange then
		bot.state = "CHASE"
		move(bot, flatDir(pos, target.root.Position), 1)
		return
	end

	-- ATTACK: strafe perpendicular suave + tiro na cadência do Balance
	bot.state = "ATTACK"
	local toTarget = flatDir(pos, target.root.Position)
	local strafe = Vector3.new(-toTarget.Z, 0, toTarget.X) * math.sin(now * 2.5 + bot.strafePhase)
	if strafe.Magnitude > 1e-3 then
		move(bot, strafe.Unit, TUNE.attackStrafeMult)
	end
	if now >= bot.nextShotAt then
		bot.nextShotAt = now + B.fireRate
		shoot(bot, target)
	end
end

-- --------------------------------------------------------------------- API
--- CONTRATO COM A RAIA C (controles): os bots vivem em `workspace.Bots`, cada um
--- um Model com `Humanoid` + `HumanoidRootPart`. É por esses três nomes que a
--- assistência de mira do celular acha os alvos — não mudar sem avisar a raia C.
local function ensureFolder(): Folder
	if folder and folder.Parent then
		return folder
	end
	local existing = Workspace:FindFirstChild("Bots")
	if existing and existing:IsA("Folder") then
		folder = existing
		return existing
	end
	local f = Instance.new("Folder")
	f.Name = "Bots" -- fora do folder "Arena": o Terrain.build não os leva junto
	f.Parent = Workspace
	folder = f
	return f
end

function Bots.init(terrainModule: any, combatModule: any, targetsFn: () -> { Target })
	terrain = terrainModule
	combat = combatModule
	getTargets = targetsFn
	evSpawned = Net.event(Net.Names.SpellSpawned)
	evImpact = Net.event(Net.Names.SpellImpact)
	ensureFolder() -- existe desde o boot: o scan do cliente nunca acha nil
end

--- Cria `count` bots nos pontos dados (o Match escolhe pontos andáveis).
--- Elementos em rodízio: as 5 identidades aparecem em toda partida (V4).
function Bots.spawn(count: number, points: { Vector3 }): { Bot }
	local created: { Bot } = {}
	if count <= 0 or #points == 0 then
		return created
	end
	local bin = ensureFolder()
	local now = os.clock()
	for i = 1, count do
		nextId += 1
		local element = Elements.Order[(nextId - 1) % #Elements.Order + 1]
		local name = string.format("Autômato %d", nextId)
		local model, hum, root = buildBody(name, Elements.ById[element].color)
		model:PivotTo(CFrame.new(points[(i - 1) % #points + 1] + Vector3.new(0, 4, 0)))
		model.Parent = bin
		-- física no servidor: bot é autoridade nossa, não de nenhum cliente
		pcall(function()
			root:SetNetworkOwner(nil)
		end)

		local bot: Bot = {
			id = nextId,
			name = name,
			model = model,
			humanoid = hum,
			root = root,
			element = element,
			state = "WANDER",
			wanderDir = randomDir(),
			nextWanderAt = 0,
			lastSeenAt = 0,
			nextShotAt = now + rng:NextNumber(0, B.fireRate),
			strafePhase = rng:NextNumber() * math.pi * 2,
			fleeDir = Vector3.zero,
			fleeUntil = 0,
			scanAcc = 0,
			hazardAcc = 0,
			moveAcc = TUNE.moveRefresh,
			kills = 0,
			damage = 0,
		}
		table.insert(bots, bot)
		table.insert(created, bot)

		-- o bot vira ALVO VÁLIDO das magias dos jogadores (registro do Combat). O
		-- elemento vai junto: é dali que sai a atribuição do dano DELE.
		combat.registerTarget(hum, element)
		hum.Died:Connect(function()
			combat.unregisterTarget(hum)
			Debris:AddItem(model, TUNE.corpseLife)
		end)
	end
	return created
end

function Bots.list(): { Bot }
	return bots
end

function Bots.aliveCount(): number
	local n = 0
	for _, bot in ipairs(bots) do
		if bot.humanoid.Health > 0 then
			n += 1
		end
	end
	return n
end

--- Zona arcana atual (o bot corre para dentro dela — prioridade máxima).
function Bots.setZone(center: Vector3, radius: number)
	zoneCenter = center
	zoneRadius = radius
end

--- Apaga todos os bots e projéteis (fim de partida / volta ao lobby).
function Bots.clear()
	for _, bot in ipairs(bots) do
		combat.unregisterTarget(bot.humanoid)
		bot.model:Destroy()
	end
	table.clear(bots)
	table.clear(shots)
end

function Bots.step(dt: number)
	if #bots == 0 and #shots == 0 then
		return
	end
	frameTargets = if getTargets then (getTargets :: any)() else {}
	local now = os.clock()
	for _, bot in ipairs(bots) do
		if bot.humanoid.Health > 0 and bot.root.Parent then
			stepBot(bot, dt, now)
		end
	end
	stepShots(dt)
end

-- ============================================================================
-- AUTOTESTE — cole no Command Bar do Studio (servidor):
--   require(game.ServerScriptService.Server.Bots).selfTest()
-- Cobre a única regra daqui que não dá para ver a olho: quem é ALVO. O resto da
-- FSM se vê andando na arena.
-- ⚠ RODE NO LOBBY: monta e limpa o Teams DE VERDADE.
-- ============================================================================
function Bots.selfTest()
	--- os membros são Humanoids de mentira: para o Teams, bot e jogador são
	--- membros iguais, e é essa simetria que o filtro depende.
	local hums: { Humanoid } = {}
	for _ = 1, 4 do
		table.insert(hums, Instance.new("Humanoid"))
	end
	Teams.build(hums :: any, 2) -- duos: {1,2} · {3,4}

	local bot = { humanoid = hums[1] } :: any
	assert(not isFoe(bot, hums[2]), "PARCEIRO DE DUO não pode ser alvo (o bug de hoje)")
	assert(isFoe(bot, hums[3]), "quem é de outro time continua sendo alvo")
	assert(isFoe(bot, hums[4]), "o outro duo inteiro é inimigo")

	Teams.clear()
	assert(isFoe(bot, hums[2]), "sem times (lobby) o bot reage a todo mundo — lado seguro do erro")
	for _, h in ipairs(hums) do
		h:Destroy()
	end

	print("[Bots] autoteste OK — alvo filtrado por time")
	return true
end

return Bots
