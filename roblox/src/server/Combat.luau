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
local Events = require(Shared:WaitForChild("Events"))
local Grid = require(Shared:WaitForChild("Grid"))
local Net = require(Shared:WaitForChild("Net"))

-- --- constantes de sensação/varredura (NÃO são balanceamento) ---------------
--- alcance máximo de qualquer projétil. 2D: 720 px ÷ 8 px/stud = 90 studs.
local MAX_RANGE = 720 / Balance.PX_PER_STUD
--- Escala do avatar (raia de arte). Require protegido, mesmo padrão do
--- notifySintonia: o Avatar é cosmético e o Combate não pode cair com ele.
local AVATAR_SCALE = 1.3
do
	local mod = script.Parent and script.Parent:FindFirstChild("Avatar")
	local ok, m = pcall(require, mod :: ModuleScript)
	local scale = if ok and typeof(m) == "table" then tonumber((m :: any).SCALE) else nil
	if scale and scale > 0 then
		AVATAR_SCALE = scale
	end
end
--- folga de corpo no teste centro-a-centro (2D: 14 px ≈ 1.75 studs, arredondado
--- para 2 porque o torso do avatar Roblox é mais largo que o boneco 2D). O
--- corpo cresceu com Avatar.SCALE, e a folga cresce junto: sem isto, tiro que
--- acerta a cabeça na tela erra na mecânica e o TTK medido no playtest mente.
local HIT_PAD = 2 * AVATAR_SCALE
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

-- --- POSTURA (comandos básicos de BR: correr / agachar) ---------------------
-- KNOBS DE CALIBRAÇÃO (não são balanceamento de dano — ajustar no playtest):
--- correr: rápido o bastante para reposicionar, lento o bastante para não
--- transformar o mapa inteiro em corrida (padrão do gênero: ~1.3x)
local SPRINT_MULT = 1.35
--- agachar: metade da velocidade é o preço de ficar baixo atrás da cobertura
local CROUCH_MULT = 0.5
--- quanto o quadril desce ao agachar, em FRAÇÃO do quadril de pé — assim o
--- agachamento acompanha Avatar.SCALE e qualquer rig sem número mágico (o
--- corpo abaixa DE VERDADE e replica: o inimigo vê quem está agachado).
--- ponytail: TETO CONHECIDO — no R6 o HipHeight base é 0, então lá isto vira
--- no-op visual; o R15 (padrão do Roblox hoje) abaixa normalmente. Agachamento
--- visível em R6 pede animação de rig, não outro número aqui.
local CROUCH_HIP_MULT = 0.5

--- Multiplicador de velocidade da postura. Agachar VENCE correr: quem segura
--- o agachar não corre por acidente.
local function stanceMult(sprint: boolean, crouch: boolean): number
	if crouch then
		return CROUCH_MULT
	end
	return if sprint then SPRINT_MULT else 1
end

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
	--- postura (intenção do cliente, aplicada e validada AQUI)
	sprint: boolean,
	crouch: boolean,
	--- HipHeight de pé do rig (R6 e R15 têm bases diferentes)
	hipBase: number,
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
--- valor = elemento do bot (string) quando informado, senão `true`. Os dois são
--- verdadeiros, então quem só pergunta "é alvo?" não precisa saber disso.
local extraTargets: { [Humanoid]: string | boolean } = {}
--- Observadores de dano: chamados DEPOIS de aplicar, com o dano efetivo. O
--- Match usa para crédito de abate e para os números do Selo do Campeão.
local damageListeners: { (Humanoid, number, Player?) -> () } = {}

--- remotes resolvidos uma vez no init (Net.event faz FindFirstChild por chamada)
local evSpawned: RemoteEvent
local evImpact: RemoteEvent
local evStats: RemoteEvent
local evDamage: RemoteEvent
--- POSTURA (correr/agachar). O nome NÃO está em Net.Names porque Shared/Net é
--- do COORDENADOR — Net.event aceita nome livre e cria o RemoteEvent no
--- servidor / espera no cliente. PENDÊNCIA: oficializar como
--- Net.Names.Stance = "Stance" e rotear pelo Main, como as outras intenções.
local evStance: RemoteEvent

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

-- ----------------------------------------------------------------- telemetria
--- Mesmo require preguiçoso e protegido do notifySintonia acima, pela mesma
--- razão: TELEMETRIA NUNCA DECIDE JOGO. Se o coletor quebrar ou sumir, o
--- combate continua de pé e só o dashboard fica pobre.
--- Convenção: `subject` é quem AGE (Player, ou Humanoid do bot); quando não há
--- ator (dano da zona/terreno), o sujeito é quem SOFRE. O `payload` leva só o
--- que é específico do evento — o contexto padrão (match_id, is_bot, team_id,
--- zona, vivos) é o coletor que preenche (Shared/Events).
local telemetry: any = nil
local function emit(name: string, subject: any, payload: any?)
	if telemetry == nil then
		local mod = script.Parent and script.Parent:FindFirstChild("Telemetry")
		local ok, m = pcall(function()
			return require(mod :: ModuleScript)
		end)
		telemetry = (ok and m) or false
	end
	if telemetry and telemetry.emit then
		pcall(telemetry.emit, name, subject, payload)
	end
end

-- ------------------------------------------------------- trava de ELIMINADO
--- P0 da auditoria externa: eliminado continuava conjurando do poleiro de
--- espectador (Match.perch), sujando TTK, funil de Sintonia e dano por
--- elemento com tiros que não pertencem a nenhuma partida.
--- Quem sabe quem foi eliminado é o Match; a trava mora aqui porque é aqui que
--- toda intenção do cliente entra (`handleCast`/`handleDodge`/`handleSwitch`).
local cannotAct: { [Player]: boolean } = {}

--- Chamado pelo Match: `false` na eliminação, `true` no início da partida.
function Combat.setCanAct(player: Player, canAct: boolean)
	cannotAct[player] = if canAct then nil else true
	if not canAct then
		--- eliminado não leva status para o poleiro de espectador (nem para a
		--- próxima partida): a limpeza mora aqui porque este é o ponto único por
		--- onde a eliminação passa. `clearStatus` é definido mais abaixo — em Lua
		--- o campo é resolvido na CHAMADA, não aqui.
		local st = states[player]
		Combat.clearStatus(if st then st.humanoid else nil)
	end
end

function Combat.canAct(player: Player): boolean
	return not cannotAct[player]
end

-- ==========================================================================
-- KERNEL DE STATUS (P1 da auditoria externa)
-- O problema que ele resolve: os combos de Sintonia PROMETIAM "atordoa" e não
-- atordoavam. Qualquer efeito escrito direto em `Humanoid.WalkSpeed` durava um
-- frame — o `Combat.step` reescreve a velocidade todo tick e apagava tudo.
-- Daqui em diante existe UMA tabela de status e UM produto de velocidade
-- (postura × terreno × status). Nada de efeito por fora.
--
-- TETO DURO DE ATORDOAMENTO — 0,6 a 0,8 s. Justificativa da auditoria: com TTK
-- de ~3 s, CC longo elimina counterplay (o alvo morre sem poder responder). O
-- teto é CLAMPADO aqui dentro: nenhum chamador consegue passar disso, nem por
-- engano nem por bug futuro. Mexer neste número é decisão de design, não de
-- implementação.
-- ==========================================================================
--- ponytail: TETO CONHECIDO — o clamp é POR APLICAÇÃO. Dois atordoamentos que
--- se sobrepõem no tempo não somam (fica quem termina depois, no máximo 0,8 s a
--- partir dali), mas dois combos de DUPLAS DIFERENTES em sequência (0,79 s + 0,8
--- s) encadeariam ~1,6 s. Com `sharedCooldown` de 24 s por dupla isso é raro; se
--- o playtest mostrar chain-stun, o upgrade é uma janela de imunidade
--- (`stunImmuneUntil` por Humanoid, ~2 s) — 3 linhas, aqui mesmo.
local STUN_MAX = 0.8
--- Reveal: informação também precisa de contra-jogada. 1,5–2,0 s dá para
--- reagir a um flanco; mais que isso vira caçada pelo mapa inteiro.
local REVEAL_MAX = 2.0
--- Lentidão de status: teto de duração e PISO de velocidade — lentidão perto de
--- zero é atordoamento disfarçado e escaparia do teto acima por outra porta.
local SLOW_MAX = 3.0
local SLOW_FLOOR = 0.35

local STATUS_MAX: { [string]: number } = { Stun = STUN_MAX, Reveal = REVEAL_MAX, Slow = SLOW_MAX }
--- Cor do realce por tipo: efeito que o inimigo não enxerga é efeito que não
--- existe (a mesma auditoria). Stun = âmbar, Reveal = ciano, Slow = terra.
local STATUS_COLOR: { [string]: Color3 } = {
	Stun = Color3.fromHex("#FFE066"),
	Reveal = Color3.fromHex("#8AE9FF"),
	Slow = Color3.fromHex("#B98A4A"),
}

type Status = { expires: number, mag: number, fx: Instance? }

--- CHAVES FRACAS (mesmo motivo do Teams): corpo destruído no meio da partida
--- some sozinho da tabela em vez de segurar a Instance até o fim do servidor —
--- é a metade "não vaza entre partidas" do requisito.
local statuses: { [Humanoid]: { [string]: Status } } = setmetatable({}, { __mode = "k" }) :: any

--- Realce replicado pelo servidor. É a fonte VISÍVEL do efeito.
--- ponytail: TETO CONHECIDO — Highlight replica para TODOS, então o `Reveal`
--- também aparece para o time de quem foi revelado. Mostrar só ao time inimigo
--- exige o CLIENTE criar o Highlight (raia de UI); quando ela existir, este é o
--- ponto único a trocar por um evento S→C. `pcall` porque Highlight é cosmético
--- e o kernel de status não pode cair por causa de enfeite.
local function statusFx(humanoid: Humanoid, kind: string): Instance?
	local char = humanoid.Parent
	if not char then
		return nil
	end
	local ok, fx = pcall(function()
		local hl = Instance.new("Highlight")
		hl.Name = "Status_" .. kind
		hl.FillColor = STATUS_COLOR[kind] or Color3.new(1, 1, 1)
		hl.OutlineColor = STATUS_COLOR[kind] or Color3.new(1, 1, 1)
		hl.FillTransparency = 0.6
		--- só o Reveal atravessa parede — é literalmente o efeito dele. Stun e
		--- Slow marcam quem já está à vista, e não podem virar wallhack.
		hl.DepthMode = if kind == "Reveal"
			then Enum.HighlightDepthMode.AlwaysOnTop
			else Enum.HighlightDepthMode.Occluded
		hl.Adornee = char
		hl.Parent = char
		return hl
	end)
	return if ok then fx else nil
end

--- Aplica (ou renova) um status. Devolve a duração EFETIVA, já clampada — é
--- assim que o chamador descobre que pediu mais do que o teto permite.
--- `by` é quem causou (opcional, só para a telemetria ter de quem falar).
function Combat.applyStatus(humanoid: Humanoid, kind: string, duration: number, magnitude: number?, by: Player?): number
	local cap = STATUS_MAX[kind]
	--- tudo tratado como hostil: tipo inválido, NaN e negativo não entram
	if not humanoid or not cap or typeof(duration) ~= "number" or duration ~= duration then
		return 0
	end
	local d = math.clamp(duration, 0, cap)
	if d <= 0 or humanoid.Health <= 0 then
		return 0
	end
	local mag = tonumber(magnitude) or 1
	if kind == "Slow" then
		mag = math.clamp(mag, SLOW_FLOOR, 1)
	end

	local entry = statuses[humanoid]
	if not entry then
		entry = {}
		statuses[humanoid] = entry
	end
	local cur = entry[kind]
	local now = os.clock()
	local expires = now + d
	if cur and cur.expires > now then
		--- ainda ATIVO: reaplicar ESTENDE (nunca encurta) e fica com o efeito
		--- mais forte. Empilhar duração seria como burlar o teto por repetição.
		cur.expires = math.max(cur.expires, expires)
		cur.mag = math.min(cur.mag, mag)
	else
		--- vencido (a varredura do `step` ainda não passou): entra efeito NOVO —
		--- senão o alvo herdaria a magnitude do efeito anterior para sempre.
		if cur and cur.fx then
			cur.fx:Destroy()
		end
		entry[kind] = { expires = expires, mag = mag, fx = statusFx(humanoid, kind) }
	end

	-- TELEMETRIA: `Shared/Events` é do coordenador e não tem nome para status,
	-- então isto entra no fluxo `damage` com `amount = 0` — o agregador SOMA
	-- `amount`, logo nenhum número de dano se mexe; só a contagem de eventos
	-- cresce. `status` é o campo que separa. PEDIDO NO RELATÓRIO:
	-- `Events.Name.StatusApplied = "status_applied"`.
	emit(Events.Name.Damage, by or humanoid, {
		amount = 0,
		absorbed = 0,
		status = kind,
		duration = d,
		magnitude = mag,
		--- quanto foi PEDIDO vs. quanto valeu: é assim que o clamp fica auditável
		--- no dashboard em vez de virar fé no código.
		requested = duration,
		clamped = duration > d,
		target = humanoid,
		environmental = by == nil,
	})
	return d
end

--- Tempo restante (0 = sem efeito) e a magnitude. Devolver 0 em vez de nil
--- poupa um `if` em todo chamador: "está atordoado?" é `> 0`.
function Combat.getStatus(humanoid: Humanoid, kind: string): (number, number)
	local entry = statuses[humanoid]
	local s = if entry then entry[kind] else nil
	if not s then
		return 0, 1
	end
	local left = s.expires - os.clock()
	return (if left > 0 then left else 0), s.mag
end

--- Multiplicador de velocidade VINDO DO STATUS. Entra no MESMO produto que a
--- postura e o terreno (`Combat.step` e `Bots.move`) — é o contrato que impede
--- o bug original de voltar: ninguém escreve WalkSpeed por fora daquele produto.
function Combat.statusSpeedMult(humanoid: Humanoid): number
	if Combat.getStatus(humanoid, "Stun") > 0 then
		return 0
	end
	local left, mag = Combat.getStatus(humanoid, "Slow")
	return if left > 0 then mag else 1
end

--- Limpa TUDO de um corpo (respawn, eliminação, saída, fim de partida).
function Combat.clearStatus(humanoid: Humanoid?)
	local entry = if humanoid then statuses[humanoid :: Humanoid] else nil
	if not entry then
		return
	end
	for _, s in pairs(entry) do
		if s.fx then
			s.fx:Destroy()
		end
	end
	statuses[humanoid :: Humanoid] = nil
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
	if not st.player.Parent then
		return -- saiu do jogo: não existe cliente para receber
	end
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

	-- intenção de POSTURA: payload tratado como hostil (só dois booleanos saem
	-- dele). O Main roteia as outras intenções; esta se auto-roteia enquanto o
	-- nome não entra em Net.Names.
	evStance = Net.event("Stance")
	evStance.OnServerEvent:Connect(function(player, payload)
		Combat.handleStance(player, payload)
	end)
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
		sprint = false,
		crouch = false,
		hipBase = 0,
	}
end

function Combat.onPlayerRemoving(player: Player)
	local old = states[player]
	Combat.clearStatus(if old then old.humanoid else nil)
	states[player] = nil
	cannotAct[player] = nil
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
	--- CORPO NOVO NASCE LIMPO: status não sobrevive à morte nem à partida
	--- anterior. Limpa o corpo velho (que ainda pode estar caindo por aí) e o
	--- novo (o engine reaproveita Humanoid em alguns fluxos de respawn).
	Combat.clearStatus(st.humanoid)
	Combat.clearStatus(humanoid)
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
	--- corpo novo nasce de pé: a postura antiga não sobrevive à morte
	st.sprint = false
	st.crouch = false
	st.hipBase = humanoid.HipHeight
	--- PULO (comando básico de BR, reprovado no teste de 18/08 por não existir):
	--- o Roblox só obedece JumpPower quando UseJumpPower está ligado. O Main já
	--- liga; repetimos aqui de graça porque quem cria bot/personagem sem passar
	--- pelo Main também precisa pular. Com Gravity 196.2, JumpPower 50 = 6.4
	--- studs de altura — folga larga sobre o rebaixo de 2,5 studs do lago.
	humanoid.UseJumpPower = true
	humanoid.JumpPower = Balance.player.jumpPower
	--- Shift esquerdo é CORRER. O shift-lock nativo (opção do jogador) roubaria
	--- a tecla, então ele sai de cena — a mira do PC é o cursor no chão, não o
	--- centro da tela, e nada aqui depende do mouse travado.
	player.DevEnableMouseLock = false
	sendStats(st)
end

--- Intenção de POSTURA vinda do cliente: correr (sprint) e agachar (crouch).
--- AUTORIDADE: o cliente só pede; quem escreve WalkSpeed é o `step` daqui —
--- por isso postura NÃO pode ser só um efeito de cliente (seria apagada no
--- frame seguinte). Payload hostil até prova em contrário: dois booleanos.
function Combat.handleStance(player: Player, payload: any)
	local st = states[player]
	if not st or typeof(payload) ~= "table" then
		return
	end
	st.sprint = (payload :: any).sprint == true
	local wants = (payload :: any).crouch == true
	local hum = st.humanoid
	if wants ~= st.crouch and hum and hum.Parent then
		if wants then
			--- mede o quadril DE PÉ no instante de agachar, não no nascimento do
			--- personagem: a escala do R15 (Avatar.SCALE) chega um frame depois e
			--- congelar o valor errado deixaria o jogador afundado ao levantar.
			st.hipBase = hum.HipHeight
			hum.HipHeight = st.hipBase * CROUCH_HIP_MULT
		else
			hum.HipHeight = st.hipBase
		end
	end
	st.crouch = wants
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
	if not st or cannotAct[player] or not isAlive(st) or not st.root or not st.root.Parent then
		return -- eliminado não conjura (nem do poleiro de espectador)
	end
	if Combat.getStatus(st.humanoid :: Humanoid, "Stun") > 0 then
		return -- ATORDOADO NÃO CONJURA (kernel de status) — teto de 0,8 s
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
		emit(Events.Name.SpellCast, player, {
			element = st.element,
			kind = "basic",
			pos = (st.root :: BasePart).Position,
			mana_left = st.mana,
		})
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
		emit(Events.Name.TacticCast, player, {
			element = st.element,
			kind = "tactic",
			pos = (st.root :: BasePart).Position,
			cooldown = cd,
		})
	end
	sendStats(st)
end

--- Troca de elemento (Q / carrossel de toque). Valida o id.
function Combat.handleSwitch(player: Player, elementId: any)
	local st = states[player]
	if not st or cannotAct[player] or not Elements.isValid(elementId) then
		return
	end
	st.element = elementId :: Elements.Id
	sendStats(st)
end

--- Esquiva com i-frames curtos (GDD §15 item 5).
function Combat.handleDodge(player: Player, dir: Vector3?)
	local st = states[player]
	if not st or cannotAct[player] or not isAlive(st) then
		return
	end
	if Combat.getStatus(st.humanoid :: Humanoid, "Stun") > 0 then
		return -- atordoado não esquiva (se esquivasse, o Stun não seria Stun)
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
	emit(Events.Name.DodgeStart, player, {
		pos = root.Position,
		dir = st.dashDir,
		iframes = dodge.iframes,
		cooldown = dodge.cooldown,
	})
	sendStats(st)
end

-- ------------------------------------------------------------------------ dano
--- Dano roteado (usado por Combat, Sintonia e Match/zona). O ESCUDO absorve
--- antes da vida (GDD §5) e i-frames de esquiva ignoram tudo.
--- `strong` só marca o número de dano como forte na HUD — não altera valor.
--- `fromNpc` é o AUTOR SEM PLAYER (bot): existe só para a telemetria parar de
--- carimbar tiro de bot como "ambiente" e misturá-lo com zona/terreno (achado
--- do Telemetry.coverage). Não entra no cálculo de dano nem no crédito de
--- escudo — bot não evolui escudo — e os `damageListeners` continuam recebendo
--- `Player?` (o Match credita abate por jogador; mudar isso é raia dele).
function Combat.applyDamage(
	targetHumanoid: Humanoid,
	amount: number,
	fromPlayer: Player?,
	strong: boolean?,
	fromNpc: Humanoid?
)
	if amount <= 0 or targetHumanoid.Health <= 0 then
		return
	end
	local character = targetHumanoid.Parent
	local target = if character then Players:GetPlayerFromCharacter(character) else nil
	local tState = if target then states[target] else nil
	--- elemento do autor não-jogador: o `registerTarget` do bot já guarda isso,
	--- então o dado sai de onde já existe em vez de virar mais um parâmetro.
	local npcElement = if fromNpc then extraTargets[fromNpc] else nil
	local author: any = fromPlayer or fromNpc
	local authorElement = if typeof(npcElement) == "string" then npcElement else nil

	if tState and os.clock() < tState.iframesUntil then
		-- ESQUIVA PERFEITA. Emitir aqui é o que torna a esquiva MENSURÁVEL: sem
		-- este evento, "quantos tiros o i-frame anula" fica invisível, e foi
		-- exatamente esse número que motivou o corte de 0,28 s → 0,18 s
		-- (auditoria externa 18/08). `nullified` marca dano que NÃO aconteceu —
		-- o agregador não pode somá-lo ao dano real.
		emit(Events.Name.Damage, author or targetHumanoid, {
			amount = 0,
			absorbed = 0,
			blocked = amount,
			nullified = true,
			target = targetHumanoid,
			strong = strong == true,
			element = authorElement,
			environmental = author == nil,
		})
		return
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

	-- sem ator (zona/terreno) o sujeito é quem SOFRE — senão o evento não teria
	-- de quem falar. `absorbed` separa escudo de vida: é o número que diz se o
	-- Escudo Evolutivo (GDD §5) está segurando demais ou de menos.
	emit(Events.Name.Damage, author or targetHumanoid, {
		amount = dealt,
		absorbed = amount - rest,
		target = targetHumanoid,
		strong = strong == true,
		element = authorElement,
		environmental = author == nil,
	})
	if targetHumanoid.Health <= 0 then
		-- KNOCK = o golpe que derrubou. A ELIMINAÇÃO é do Match (é ele que sabe
		-- se ainda dá para voltar) — os dois eventos existem de propósito.
		emit(Events.Name.Knock, targetHumanoid, { by = author, amount = dealt })
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
--- RETORNO CONTESTÁVEL (GDD §16.6): "traz de volta o jogador, NÃO o poder".
--- Sem isto o revive devolve o escudo evoluído junto com o mago, e a morte vira
--- irrelevante — que é exatamente o oposto do que a feature existe para fazer.
--- Chamado pelo Match ao concluir o ritual no altar. Costura do coordenador.
function Combat.resetProgress(player: Player)
	local st = states[player]
	if not st then
		return
	end
	st.evo = 0
	st.shieldLv = 1
	st.shield = Balance.shield.caps[1]
	st.mana = Balance.player.manaMax
	sendStats(st)
end

--- Nível do Escudo Evolutivo (1..4). Existe para a telemetria preencher o
--- `shield_tier` do contexto padrão — sem ele o dashboard não consegue dizer
--- se o snowball do escudo dourado (risco apontado na auditoria) é real.
function Combat.getShieldLevel(player: Player): number
	local st = states[player]
	return if st then st.shieldLv else 0
end

function Combat.drainMana(player: Player)
	local st = states[player]
	if not st then
		return
	end
	st.mana = 0
	sendStats(st)
end

--- Registra/remove um Humanoid não-jogador (bot) como alvo válido das magias.
--- `element` (opcional) é o elemento DELE: guardado aqui, vira o `element` do
--- evento de dano quando o bot é o autor (`applyDamage(..., fromNpc)`).
function Combat.registerTarget(humanoid: Humanoid, element: string?)
	extraTargets[humanoid] = element or true
end

function Combat.unregisterTarget(humanoid: Humanoid)
	extraTargets[humanoid] = nil
	Combat.clearStatus(humanoid) -- bot morto não leva status para o próximo round
end

--- Assina o fluxo de dano (leitura pura, pós-aplicação).
function Combat.onDamage(fn: (Humanoid, number, Player?) -> ())
	table.insert(damageListeners, fn)
end

-- -------------------------------------------------------------------- impacto
local function impact(p: Projectile, hitHumanoid: Humanoid?)
	if hitHumanoid then
		Combat.applyDamage(hitHumanoid, p.dmg, p.owner, p.strong)
		-- ACERTO ≠ dano aplicado (i-frames podem ter comido tudo): é o par
		-- SpellCast/SpellHit que dá a taxa de acerto por elemento.
		emit(Events.Name.SpellHit, p.owner or hitHumanoid, {
			element = p.element,
			strong = p.strong,
			target = hitHumanoid,
			pos = p.pos,
			traveled = p.traveled,
		})
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
		local stunned = Combat.getStatus(hum, "Stun") > 0
		if now < st.dashUntil and not stunned then
			local v = st.dashDir * (Balance.player.dodge.dist / Balance.player.dodge.duration)
			root.AssemblyLinearVelocity = Vector3.new(v.X, root.AssemblyLinearVelocity.Y, v.Z)
		elseif stunned then
			--- WalkSpeed 0 impede ANDAR, não mata o embalo que já existe: sem
			--- zerar a horizontal, o atordoado desliza e o efeito não lê.
			local vel = root.AssemblyLinearVelocity
			root.AssemblyLinearVelocity = Vector3.new(0, vel.Y, 0)
		end

		-- lama/grama alta mudam a velocidade de andar (GDD §14); correr e
		-- agachar multiplicam por cima — este é o único lugar que escreve a
		-- WalkSpeed do jogador, então é aqui que a postura vale de fato.
		-- O STATUS entra NESTE produto (nunca por fora): foi escrever WalkSpeed
		-- por fora que fez o atordoamento dos combos não existir até hoje.
		local want = Balance.player.speed
			* (terrain.speedMultAt(root.Position) or 1)
			* stanceMult(st.sprint, st.crouch)
			* Combat.statusSpeedMult(hum)
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

	-- --------------------------------------------------------------- status
	--- `getStatus` já expira sozinho pela conta do relógio; esta varredura existe
	--- para APAGAR O REALCE e soltar a tabela. Roda sobre um mapa quase sempre
	--- vazio (só quem está sob efeito aparece), inclusive bots.
	for hum, entry in pairs(statuses) do
		local empty = true
		for kind, s in pairs(entry) do
			if now >= s.expires or hum.Health <= 0 then
				if s.fx then
					s.fx:Destroy()
				end
				entry[kind] = nil
			else
				empty = false
			end
		end
		if empty then
			statuses[hum] = nil
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

	-- POSTURA: agachar vence correr e nenhuma postura passa perto de zero
	assert(stanceMult(false, false) == 1, "de pé = velocidade base")
	assert(stanceMult(true, false) > 1, "correr tem de ser mais rápido")
	assert(stanceMult(false, true) < 1, "agachado tem de ser mais lento")
	assert(stanceMult(true, true) == stanceMult(false, true), "agachar vence correr")
	assert(Balance.player.speed * stanceMult(false, true) > 1, "agachado ainda anda")

	-- TRAVA DE ELIMINADO (P0): o sujeito falso não tem personagem, então a prova
	-- roda na única intenção que não depende de corpo — a troca de elemento.
	-- `sendStats` sai cedo por falta de `Parent`, então nada vai para a rede.
	local ghost = ({} :: any) :: Player
	Combat.onPlayerAdded(ghost)
	Combat.setCanAct(ghost, false)
	assert(not Combat.canAct(ghost), "eliminado não pode agir")
	Combat.handleSwitch(ghost, "Water")
	assert(Combat.getElement(ghost) == "Fire", "ELIMINADO NÃO AGE: troca de elemento tem de ser ignorada")
	Combat.handleCast(ghost, "basic", Vector3.new(0, 0, -1))
	Combat.handleDodge(ghost, nil)
	assert(states[ghost].mana == Balance.player.manaMax, "eliminado não gasta mana conjurando")
	assert(states[ghost].dashUntil == 0, "eliminado não esquiva")
	Combat.setCanAct(ghost, true)
	Combat.handleSwitch(ghost, "Water")
	assert(Combat.getElement(ghost) == "Water", "reabilitado volta a agir")
	Combat.onPlayerRemoving(ghost)
	assert(Combat.canAct(ghost), "sair do jogo limpa a trava (o id pode voltar)")

	-- KERNEL DE STATUS (P1) — o TETO do atordoamento é a regra que a auditoria
	-- comprou: com TTK de ~3 s, CC longo apaga o counterplay. Pedir 5 s tem de
	-- virar o teto, não 5 s.
	assert(STUN_MAX >= 0.6 and STUN_MAX <= 0.8, "teto de atordoamento fora da janela 0,6–0,8 s da auditoria")
	assert(REVEAL_MAX >= 1.5 and REVEAL_MAX <= 2.0, "Reveal fora da janela 1,5–2,0 s")
	local dummy = Instance.new("Humanoid")
	local got = Combat.applyStatus(dummy, "Stun", 5)
	assert(got == STUN_MAX, "CLAMP: pedir 5 s de atordoamento tem de devolver o teto, veio " .. got)
	local left = Combat.getStatus(dummy, "Stun")
	assert(left > 0 and left <= STUN_MAX, "atordoamento guardado acima do teto: " .. left)
	assert(Combat.statusSpeedMult(dummy) == 0, "atordoado NÃO anda")
	assert(Combat.applyStatus(dummy, "Reveal", 99) == REVEAL_MAX, "Reveal também é clampado")
	assert(Combat.applyStatus(dummy, "Stun", -3) == 0, "duração negativa não vira status")
	assert(Combat.applyStatus(dummy, "Vertigem", 1) == 0, "tipo de status inexistente é ignorado")
	-- lentidão tem PISO: senão viraria atordoamento sem passar pelo teto
	local other = Instance.new("Humanoid")
	Combat.applyStatus(other, "Slow", 1, 0)
	assert(Combat.statusSpeedMult(other) >= SLOW_FLOOR, "lentidão não pode chegar a zero (seria Stun disfarçado)")
	Combat.clearStatus(other)
	assert(Combat.statusSpeedMult(other) == 1, "clear devolve a velocidade cheia")

	-- STATUS LIMPA NO RESPAWN: o corpo novo não herda o que grudou no velho
	local ghost2 = ({} :: any) :: Player
	Combat.onPlayerAdded(ghost2)
	local body = Instance.new("Model")
	local reborn = Instance.new("Humanoid")
	reborn.Parent = body
	Combat.applyStatus(reborn, "Stun", 5)
	assert(Combat.getStatus(reborn, "Stun") > 0, "pré-condição: o corpo estava atordoado")
	Combat.onCharacterAdded(ghost2, body, reborn)
	assert(Combat.getStatus(reborn, "Stun") == 0, "RESPAWN tem de limpar o status (senão vaza entre partidas)")
	assert(Combat.statusSpeedMult(reborn) == 1, "corpo novo nasce com velocidade cheia")
	-- e a ELIMINAÇÃO também limpa (o espectador não fica atordoado no poleiro)
	Combat.applyStatus(reborn, "Slow", 2, 0.5)
	Combat.setCanAct(ghost2, false)
	assert(Combat.statusSpeedMult(reborn) == 1, "eliminar tem de limpar o status")
	Combat.onPlayerRemoving(ghost2)
	body:Destroy()
	dummy:Destroy()
	other:Destroy()

	print("[Combat] autoteste OK")
	return true
end

return Combat
