--!strict
-- ============================================================================
-- RAIA B (Combate) — magias, projéteis com tempo de viagem (GDD §4.1: NADA de
-- hitscan), mana, cooldowns, dano, esquiva com i-frames e Escudo Evolutivo
-- (GDD §5). AUTORIDADE: valida toda intenção do cliente antes de agir.
--
-- REGRA DE SERVIDOR AUTORITATIVO (docs/ROBLOX.md §3) — como ela vive aqui:
--   · do cliente só entram KIND ("basic"/"tactic") e DIREÇÃO (já sanitizada
--     pelo Main via Net.sanitizeDirection). Dano, mana, cooldown, alcance e
--     colisão são decididos AQUI e nunca chegam pela rede.
--   · todo timestamp é `os.clock()` do SERVIDOR — nenhum relógio de cliente.
--   · o cliente só APRESENTA o que Net.SpellSpawned/SpellImpact/StatsUpdate/
--     DamageNumber anunciam.
--
-- Todo número de balanceamento sai de Shared/Balance. As constantes do bloco
-- "sensação/varredura" abaixo são game feel (mesma separação que o protótipo
-- 2D faz em src/entities/Player.ts e Projectile.ts) — não são balanceamento.
-- ============================================================================

local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Balance = require(Shared:WaitForChild("Balance"))
local Elements = require(Shared:WaitForChild("Elements"))
local Grid = require(Shared:WaitForChild("Grid"))
local Net = require(Shared:WaitForChild("Net"))

-- --- constantes de sensação/varredura (NÃO são balanceamento) ---------------
--- alcance máximo de qualquer projétil. 2D: 720 px ÷ 8 px/stud = 90 studs.
local MAX_RANGE = 720 / Balance.PX_PER_STUD
--- folga de corpo no teste centro-a-centro (2D: 14 px ≈ 1.75 studs, arredondado
--- para 2 porque o torso do avatar Roblox é mais largo que o boneco 2D)
local HIT_PAD = 2
--- sub-passo da varredura: menor que meia célula evita túnel em hitch de rede
local SUBSTEP = Grid.CELL_SIZE * 0.5
--- ponta do cajado: de onde o projétil nasce (2D: 20 px)
local MUZZLE = 20 / Balance.PX_PER_STUD
--- cadência de aplicação dos perigos do terreno (2D: 250 ms)
local HAZARD_TICK = 0.25
--- clamp de delta: um hitch do servidor nunca simula mais que isto de uma vez
local MAX_STEP = 0.1
--- cadência do StatsUpdate para o dono (a mana regenera todo frame; 10 Hz é
--- imperceptível na HUD e 10x mais barato que replicar a cada Heartbeat)
local STATS_INTERVAL = 0.1

-- ---------------------------------------------------------------------- tipos
type State = {
	player: Player,
	element: Elements.Id,
	mana: number,
	--- instantes (os.clock) em que cada ação volta a ser permitida
	nextBasic: number,
	nextTactic: number,
	nextDodge: number,
	iframesUntil: number,
	dashUntil: number,
	dashDir: Vector3,
	--- Escudo de Magia Evolutivo (GDD §5)
	shield: number,
	shieldLv: number,
	evo: number,
	character: Model?,
	humanoid: Humanoid?,
	root: BasePart?,
	hazardAcc: number,
	walkSpeedSet: number,
}

type Projectile = {
	id: number,
	owner: Player?,
	element: Elements.Id,
	strong: boolean,
	pos: Vector3,
	dir: Vector3,
	speed: number,
	dmg: number,
	radius: number,
	terrainRadius: number,
	traveled: number,
}

local Combat = {}

local terrain: any = nil
local states: { [Player]: State } = {}
local projectiles: { Projectile } = {}
local nextProjectileId = 0
local statsAcc = 0

-- --- ADIÇÃO ADITIVA DA RAIA R3 (Match/Bots) — nada aqui muda o cálculo de dano
--- Alvos NÃO-jogadores (bots do Match). `findHit` passa a enxergar quem se
--- inscreve aqui; sem inscrição, o combate se comporta exatamente como antes.
local extraTargets: { [Humanoid]: boolean } = {}
--- Observadores de dano: chamados DEPOIS de aplicar, com o dano efetivo. O
--- Match usa para crédito de abate e para os números do Selo do Campeão.
local damageListeners: { (Humanoid, number, Player?) -> () } = {}

--- remotes resolvidos uma vez no init (Net.event faz FindFirstChild por chamada)
local evSpawned: RemoteEvent
local evImpact: RemoteEvent
local evStats: RemoteEvent
local evDamage: RemoteEvent

-- ------------------------------------------------------------------ Sintonia
--- O módulo é stub em R1 (no-op) e vira o pilar em R2. Require preguiçoso e
--- protegido: se ele quebrar ou sumir, o combate continua de pé.
local sintonia: any = nil
local function notifySintonia(player: Player, elementId: string, pos: Vector3)
	if sintonia == nil then
		local mod = script.Parent and script.Parent:FindFirstChild("Sintonia")
		local ok, m = pcall(function()
			return require(mod :: ModuleScript)
		end)
		sintonia = (ok and m) or false
	end
	if sintonia and sintonia.registerCast then
		pcall(sintonia.registerCast, player, elementId, pos)
	end
end

-- -------------------------------------------------------------------- terreno
--- Perigos do terreno: o Terrain NUNCA aplica dano sozinho — quem aplica é o
--- Combat. A raia A expõe `hazardAt`; enquanto não expõe, isto devolve 0.
local function hazardDps(pos: Vector3): number
	local fn = terrain and terrain.hazardAt
	if not fn then
		return 0
	end
	local hz = fn(pos)
	if typeof(hz) == "number" then
		return hz
	end
	if typeof(hz) == "table" then
		return tonumber((hz :: any).dps) or 0
	end
	return 0
end

--- Bloqueio de PROJÉTIL (≠ bloqueio de MOVIMENTO). O projétil voa na altura do
--- peito, então só é barrado por quem é mais alto que ele: árvore, rocha, muro.
--- ponytail: sem o teste de altura, a água (Grid.BLOCKING.Water = true, porque
--- barra o andar) explodiria todo projétil na margem — e "congelar o lago para
--- virar rota" / atirar por cima da água, que é a pergunta de validação V3,
--- morreria. Se a raia A expuser `blocksProjectile`, ela ganha prioridade.
local function blocksProjectile(cx: number, cy: number, y: number): boolean
	if terrain.blocksProjectile then
		return terrain.blocksProjectile(cx, cy) == true
	end
	if not terrain.isBlocking(cx, cy) then
		return false
	end
	local cell = terrain.getCell and terrain.getCell(cx, cy)
	if not cell then
		return true
	end
	local h = if cell.state == "Wall" then Grid.WALL_HEIGHT else (Grid.HEIGHTS[cell.material] or 1)
	return (Grid.FLOOR_Y + h) > y
end

-- --------------------------------------------------------------------- estado
local function isAlive(st: State): boolean
	local hum = st.humanoid
	return hum ~= nil and hum.Parent ~= nil and hum.Health > 0
end

local function shieldMax(st: State): number
	return Balance.shield.caps[st.shieldLv]
end

local function sendStats(st: State)
	local hum = st.humanoid
	local now = os.clock()
	local tacticTotal = Balance.elements[st.element].tactic.cooldown
	if st.shieldLv >= #Balance.shield.caps then
		tacticTotal *= Balance.shield.lv4TacticCooldownMult
	end
	evStats:FireClient(st.player, {
		hp = if hum then hum.Health else 0,
		hpMax = Balance.player.hp,
		shield = st.shield,
		shieldMax = shieldMax(st),
		shieldLv = st.shieldLv,
		evo = st.evo,
		evoNext = if st.shieldLv < #Balance.shield.caps then Balance.shield.thresholds[st.shieldLv + 1] else -1,
		mana = st.mana,
		manaMax = Balance.player.manaMax,
		element = st.element,
		cooldowns = {
			tactic = math.max(0, st.nextTactic - now),
			tacticTotal = tacticTotal,
			dodge = math.max(0, st.nextDodge - now),
			dodgeTotal = Balance.player.dodge.cooldown,
		},
	})
end

-- ----------------------------------------------------------------- ciclo de vida
--- Recebe o módulo Terrain (raia A) para aplicar química no impacto.
function Combat.init(terrainModule: any)
	terrain = terrainModule
	evSpawned = Net.event(Net.Names.SpellSpawned)
	evImpact = Net.event(Net.Names.SpellImpact)
	evStats = Net.event(Net.Names.StatsUpdate)
	evDamage = Net.event(Net.Names.DamageNumber)
end

function Combat.onPlayerAdded(player: Player)
	local now = os.clock()
	states[player] = {
		player = player,
		element = "Fire",
		mana = Balance.player.manaMax,
		nextBasic = now,
		nextTactic = now,
		nextDodge = now,
		iframesUntil = 0,
		dashUntil = 0,
		dashDir = Vector3.zero,
		shield = Balance.shield.caps[1],
		shieldLv = 1,
		evo = 0,
		character = nil,
		humanoid = nil,
		root = nil,
		hazardAcc = 0,
		walkSpeedSet = -1,
	}
end

function Combat.onPlayerRemoving(player: Player)
	states[player] = nil
	for i = #projectiles, 1, -1 do
		if projectiles[i].owner == player then
			table.remove(projectiles, i)
		end
	end
end

function Combat.onCharacterAdded(player: Player, character: Model, humanoid: Humanoid)
	local st = states[player]
	if not st then
		Combat.onPlayerAdded(player)
		st = states[player]
	end
	st.character = character
	st.humanoid = humanoid
	st.root = character:FindFirstChild("HumanoidRootPart") :: BasePart?
	--- o NÍVEL do escudo é conquistado e não se perde ao renascer (GDD §5);
	--- a carga volta cheia, como a mana.
	st.shield = shieldMax(st)
	st.mana = Balance.player.manaMax
	st.dashUntil = 0
	st.iframesUntil = 0
	st.hazardAcc = 0
	st.walkSpeedSet = -1
	sendStats(st)
end

-- ------------------------------------------------------------------ conjuração
local function spawnProjectile(st: State, strong: boolean, dir: Vector3)
	local spec = if strong then Balance.elements[st.element].tactic else Balance.elements[st.element].basic
	local root = st.root :: BasePart
	local origin = root.Position + dir * MUZZLE

	nextProjectileId += 1
	table.insert(projectiles, {
		id = nextProjectileId,
		owner = st.player,
		element = st.element,
		strong = strong,
		pos = origin,
		dir = dir,
		speed = spec.speed,
		dmg = spec.dmg,
		radius = spec.radius,
		terrainRadius = spec.terrainRadius,
		traveled = 0,
	})

	evSpawned:FireAllClients({
		id = nextProjectileId,
		element = st.element,
		origin = origin,
		dir = dir,
		speed = spec.speed,
		strong = strong,
	})
end

--- Intenção validada pelo Main: conjurar básico (M1) ou tático (M2).
--- Aqui a intenção é tratada como HOSTIL: vivo? cadência? mana? Só então age.
function Combat.handleCast(player: Player, kind: string, dir: Vector3)
	local st = states[player]
	if not st or not isAlive(st) or not st.root or not st.root.Parent then
		return
	end
	local now = os.clock()

	if kind == "basic" then
		-- GDD §4 regra 2: ATAQUE consome MANA (e respeita a cadência)
		if now < st.nextBasic then
			return
		end
		local spec = Balance.elements[st.element].basic
		if st.mana < spec.manaCost then
			return
		end
		st.mana -= spec.manaCost
		st.nextBasic = now + spec.fireRate
		spawnProjectile(st, false, dir)
	elseif kind == "tactic" then
		-- GDD §4 regra 2: TÁTICA usa COOLDOWN, nunca mana
		if now < st.nextTactic then
			return
		end
		local cd = Balance.elements[st.element].tactic.cooldown
		if st.shieldLv >= #Balance.shield.caps then
			cd *= Balance.shield.lv4TacticCooldownMult -- perk do nível 4 (GDD §5)
		end
		st.nextTactic = now + cd
		spawnProjectile(st, true, dir)
	end
	sendStats(st)
end

--- Troca de elemento (Q / carrossel de toque). Valida o id.
function Combat.handleSwitch(player: Player, elementId: any)
	local st = states[player]
	if not st or not Elements.isValid(elementId) then
		return
	end
	st.element = elementId :: Elements.Id
	sendStats(st)
end

--- Esquiva com i-frames curtos (GDD §15 item 5).
function Combat.handleDodge(player: Player, dir: Vector3?)
	local st = states[player]
	if not st or not isAlive(st) then
		return
	end
	local root = st.root
	if not root or not root.Parent then
		return
	end
	local now = os.clock()
	if now < st.nextDodge or now < st.dashUntil then
		return
	end
	--- sem direção válida do cliente, esquiva para onde o personagem olha
	local d = dir
	if not d then
		local look = root.CFrame.LookVector
		local flat = Vector3.new(look.X, 0, look.Z)
		d = if flat.Magnitude > 1e-3 then flat.Unit else Vector3.new(0, 0, -1)
	end
	local dodge = Balance.player.dodge
	st.dashDir = d :: Vector3
	st.dashUntil = now + dodge.duration
	st.iframesUntil = now + dodge.iframes
	st.nextDodge = now + dodge.cooldown
	sendStats(st)
end

-- ------------------------------------------------------------------------ dano
--- Dano roteado (usado por Combat, Sintonia e Match/zona). O ESCUDO absorve
--- antes da vida (GDD §5) e i-frames de esquiva ignoram tudo.
--- `strong` só marca o número de dano como forte na HUD — não altera valor.
function Combat.applyDamage(targetHumanoid: Humanoid, amount: number, fromPlayer: Player?, strong: boolean?)
	if amount <= 0 or targetHumanoid.Health <= 0 then
		return
	end
	local character = targetHumanoid.Parent
	local target = if character then Players:GetPlayerFromCharacter(character) else nil
	local tState = if target then states[target] else nil

	if tState and os.clock() < tState.iframesUntil then
		return -- esquiva perfeita
	end

	local rest = amount
	if tState and tState.shield > 0 then
		local absorbed = math.min(tState.shield, rest)
		tState.shield -= absorbed
		rest -= absorbed
	end
	local dealtToHp = math.min(rest, targetHumanoid.Health)
	if dealtToHp > 0 then
		targetHumanoid.Health -= dealtToHp
	end
	local dealt = (amount - rest) + dealtToHp
	if dealt <= 0 then
		return
	end

	for _, fn in ipairs(damageListeners) do
		fn(targetHumanoid, dealt, fromPlayer)
	end

	local rootPart = targetHumanoid.RootPart
	if rootPart then
		evDamage:FireAllClients({ pos = rootPart.Position, amount = dealt, crit = strong == true })
	end

	if tState then
		sendStats(tState)
	end

	-- evolução do Escudo por dano CAUSADO (GDD §5). Dano em aliado sendo
	-- revivido não conta (anti-farm) — o estado "derrubado" chega no R3.
	if fromPlayer and fromPlayer ~= target then
		local st = states[fromPlayer]
		if st then
			st.evo += dealt
			local caps = Balance.shield.caps
			while st.shieldLv < #caps and st.evo >= Balance.shield.thresholds[st.shieldLv + 1] do
				st.shieldLv += 1
				st.shield = caps[st.shieldLv] -- ENCHE no cap novo
			end
			sendStats(st)
		end
	end
end

--- Elemento ativo do jogador (a Sintonia precisa saber o que ele conjurou).
function Combat.getElement(player: Player): string?
	local st = states[player]
	return if st then st.element else nil
end

--- SINTONIA (GDD §9) — a outra metade do limitador "consome a magia dos DOIS
--- conjuradores": a Sintonia cobra o cooldown compartilhado e chama isto para
--- deixar os dois SECOS. É o que faz "combo errado = dupla vulnerável" ser
--- verdade mecânica, e não texto de design. Costura do coordenador na
--- integração da R2/R3 — a Sintonia já chamava esta função de forma opcional.
function Combat.drainMana(player: Player)
	local st = states[player]
	if not st then
		return
	end
	st.mana = 0
	sendStats(st)
end

--- Registra/remove um Humanoid não-jogador (bot) como alvo válido das magias.
function Combat.registerTarget(humanoid: Humanoid)
	extraTargets[humanoid] = true
end

function Combat.unregisterTarget(humanoid: Humanoid)
	extraTargets[humanoid] = nil
end

--- Assina o fluxo de dano (leitura pura, pós-aplicação).
function Combat.onDamage(fn: (Humanoid, number, Player?) -> ())
	table.insert(damageListeners, fn)
end

-- -------------------------------------------------------------------- impacto
local function impact(p: Projectile, hitHumanoid: Humanoid?)
	if hitHumanoid then
		Combat.applyDamage(hitHumanoid, p.dmg, p.owner, p.strong)
	end

	local cx, cy = Grid.worldToCell(p.pos)
	if Grid.inBounds(cx, cy) then
		-- reação elemental do terreno no ponto de impacto (GDD §14)
		terrain.applyElement(cx, cy, p.element, p.terrainRadius, p.strong)
		if not hitHumanoid and blocksProjectile(cx, cy, p.pos.Y) then
			-- bateu em muro/árvore/rocha: descasca a célula (GDD §14)
			terrain.damageWall(cx, cy, Balance.terrain.projectileCellDmg)
		end
	end

	evImpact:FireAllClients({ id = p.id, element = p.element, pos = p.pos, strong = p.strong })

	-- prepara o R2: toda conjuração que ATINGE algo alimenta a janela de Sintonia
	if p.owner then
		notifySintonia(p.owner, p.element, p.pos)
	end
end

--- Alvo mais próximo dentro do passo (esfera curta centro-a-centro).
local function findHit(p: Projectile): Humanoid?
	local reach = p.radius + HIT_PAD
	local reach2 = reach * reach
	for player, st in pairs(states) do
		if player == p.owner or not isAlive(st) then
			continue
		end
		local root = st.root
		if not root or not root.Parent then
			continue
		end
		if (root.Position - p.pos).Magnitude ^ 2 < reach2 then
			return st.humanoid
		end
	end
	-- alvos não-jogadores registrados pela raia R3 (bots): mesma esfera curta
	for hum in pairs(extraTargets) do
		local root = hum.RootPart
		if hum.Health <= 0 or not root or not root.Parent then
			continue
		end
		if (root.Position - p.pos).Magnitude ^ 2 < reach2 then
			return hum
		end
	end
	return nil
end

-- ------------------------------------------------------------------------ tick
--- Tick: move projéteis, aplica dano de terreno, regenera mana, expira i-frames.
function Combat.step(dt: number)
	dt = math.min(dt, MAX_STEP)
	local now = os.clock()

	-- ------------------------------------------------------------- jogadores
	for _, st in pairs(states) do
		local hum = st.humanoid
		local root = st.root
		if not isAlive(st) or not hum or not root or not root.Parent then
			continue
		end

		-- mana regenera devagar (GDD §4 regra global 2)
		st.mana = math.min(Balance.player.manaMax, st.mana + Balance.player.manaRegen * dt)

		-- esquiva: o servidor imprime a velocidade do dash a cada passo (o
		-- atrito comeria um único impulso ao longo dos 0,2s).
		-- ponytail: TETO CONHECIDO — o personagem tem network ownership do
		-- cliente dono, então a velocidade escrita aqui é uma ORDEM que ele
		-- executa, não uma garantia física. Os i-frames e o cooldown (que é o
		-- que importa para o anti-cheat) continuam 100% no servidor. Se o
		-- playtest mostrar dash "borrachudo", o upgrade é root:SetNetworkOwner(nil)
		-- durante o dash, ou trocar por um LinearVelocity de duração fixa.
		if now < st.dashUntil then
			local v = st.dashDir * (Balance.player.dodge.dist / Balance.player.dodge.duration)
			root.AssemblyLinearVelocity = Vector3.new(v.X, root.AssemblyLinearVelocity.Y, v.Z)
		end

		-- lama/grama alta mudam a velocidade de andar (GDD §14)
		local want = Balance.player.speed * (terrain.speedMultAt(root.Position) or 1)
		if math.abs(want - st.walkSpeedSet) > 0.01 then
			hum.WalkSpeed = want
			st.walkSpeedSet = want
		end

		-- perigos do terreno em ticks fixos — o Terrain nunca aplica dano sozinho
		st.hazardAcc += dt
		while st.hazardAcc >= HAZARD_TICK do
			st.hazardAcc -= HAZARD_TICK
			local dps = hazardDps(root.Position)
			if dps > 0 then
				Combat.applyDamage(hum, dps * HAZARD_TICK, nil)
			end
			if hum.Health <= 0 then
				st.hazardAcc = 0
				break
			end
		end
	end

	-- ------------------------------------------------------------- projéteis
	for i = #projectiles, 1, -1 do
		local p = projectiles[i]
		local remaining = p.speed * dt
		local done = false

		while remaining > 0 do
			local stepLen = math.min(SUBSTEP, remaining)
			remaining -= stepLen
			p.pos += p.dir * stepLen
			p.traveled += stepLen

			-- 1) terreno bloqueante (árvore viva, rocha, muro)
			local cx, cy = Grid.worldToCell(p.pos)
			if not Grid.inBounds(cx, cy) then
				-- saiu da arena: dissipa. O SpellImpact ainda vai, senão o
				-- cliente ficaria com um projétil visual órfão para sempre.
				impact(p, nil)
				done = true
				break
			end
			if blocksProjectile(cx, cy, p.pos.Y) then
				impact(p, nil)
				done = true
				break
			end

			-- 2) personagens (o dono nunca se acerta)
			local hit = findHit(p)
			if hit then
				impact(p, hit)
				done = true
				break
			end

			-- 3) alcance esgotado: dissipa no ar, ainda reagindo com o terreno
			if p.traveled >= MAX_RANGE then
				impact(p, nil)
				done = true
				break
			end
		end

		if done then
			table.remove(projectiles, i)
		end
	end

	-- ------------------------------------------------------------ HUD (10 Hz)
	statsAcc += dt
	if statsAcc >= STATS_INTERVAL then
		statsAcc = 0
		for _, st in pairs(states) do
			if st.player.Parent then
				sendStats(st)
			end
		end
	end
end

-- ============================================================================
-- AUTOTESTE — cole no Command Bar do Studio (servidor):
--   require(game.ServerScriptService.Server.Combat).selfTest()
-- Cobre a lógica que não dá para ver a olho: absorção pelo escudo e a escada
-- de evolução do GDD §5. Falha = erro no output, com a linha exata.
-- ============================================================================
function Combat.selfTest()
	local caps, thr = Balance.shield.caps, Balance.shield.thresholds

	-- escada de evolução: mesma regra do applyDamage, sobre um estado fake
	local lv, evo, shield = 1, 0, caps[1]
	local function deal(d: number)
		evo += d
		while lv < #caps and evo >= thr[lv + 1] do
			lv += 1
			shield = caps[lv]
		end
	end
	deal(149)
	assert(lv == 1, "149 de dano não pode evoluir (limiar é 150)")
	deal(1)
	assert(lv == 2 and shield == caps[2], "150 acumulado → nível 2 com escudo cheio")
	deal(thr[4] - thr[2]) -- salto grande: precisa subir DOIS níveis de uma vez
	assert(lv == 4 and shield == caps[4], "salto de dano deve pular para o nível 4")
	deal(10000)
	assert(lv == #caps, "nível não pode passar do teto")

	-- absorção: escudo primeiro, resto na vida (mesma conta do applyDamage)
	local sh, hp = 50, 100
	local rest = 70
	local absorbed = math.min(sh, rest)
	sh -= absorbed
	rest -= absorbed
	hp -= math.min(rest, hp)
	assert(sh == 0 and hp == 80, "70 de dano em 50 de escudo → escudo 0, vida 80")

	print("[Combat] autoteste OK")
	return true
end

return Combat
