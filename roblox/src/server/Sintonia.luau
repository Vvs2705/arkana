--!strict
-- ============================================================================
-- RAIA R2 — SINTONIA / CONJURAÇÃO COMBINADA (GDD §9): O PILAR DE INOVAÇÃO.
-- É a razão mecânica de jogar em dupla e a pergunta de validação nº 1 do
-- produto (docs/ROBLOX.md §1, V1). Autoridade 100% servidor.
--
-- COMO FUNCIONA (o fluxo inteiro em 5 linhas):
--   1. todo projétil que impacta chama `registerCast` (Combat, raia B);
--   2. duas conjurações de JOGADORES DIFERENTES, de ELEMENTOS DIFERENTES,
--      dentro de `window` (1,5s) e a menos de `targetRadius` uma da outra →
--   3. começa a CANALIZAÇÃO (`channel` ≈ 1s), anunciada a TODOS (inclusive
--      inimigos: o telegrama é o que permite interromper);
--   4. se um dos dois morrer/sair no meio → "failed" (e o custo NÃO volta);
--   5. senão → "fired": dano em área ×`dmgMult` + reação de terreno coerente
--      com a fantasia do combo.
--
-- OS LIMITADORES DO GDD §9 SÃO LEI — sem eles o jogo quebra:
--   · mesmo elemento NÃO funde (Elements.getCombo já devolve nil);
--   · canalização visível/interrompível ANTES da fusão;
--   · CUSTO COBRADO NO INÍCIO: o `sharedCooldown` (24s) entra nos DOIS quando
--     a canalização começa, não quando o combo sai. Combo errado (ou
--     interrompido) deixa a dupla seca e vulnerável — é isso que impede spam;
--   · todo combo INTERAGE COM O TERRENO (casamento dos dois pilares do jogo).
--
-- FRONTEIRA DE RESPONSABILIDADE (igual ao resto do servidor): quem aplica dano
-- é o Combat (`applyDamage`) e quem muda o mundo é o Terrain (`applyElement`).
-- Este módulo decide QUANDO, ONDE e QUANTO — e nunca toca em part de arena.
-- ============================================================================

local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local Workspace = game:GetService("Workspace")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Balance = require(Shared:WaitForChild("Balance"))
local Elements = require(Shared:WaitForChild("Elements"))
local Grid = require(Shared:WaitForChild("Grid"))
local Net = require(Shared:WaitForChild("Net"))

--- todos os números do combo saem daqui (GDD: "números viram dados")
local S = Balance.sintonia

-- --- constantes sociais / anti-abuso (NÃO são balanceamento de combate) -----
--- alcance máximo do alvo pingado: o mesmo do projétil (720 px do 2D ÷ 8)
local PING_RANGE = 720 / Balance.PX_PER_STUD
--- quem "ouve" o ping: aliado tem de estar plausivelmente na mesma briga
local PING_HEAR = 120
--- anti-spam de ping (o ping é social, não é magia — cooldown curto)
local PING_COOLDOWN = 2.0
--- proposta sem resposta morre sozinha; aceita, o alvo marcado dura mais
local PROPOSAL_TTL = 6.0
local PACT_TTL = 12.0
--- altura em que o combo acontece (peito): o impacto pode ter sido na copa de
--- uma árvore, e a explosão tem de pegar quem está no chão
local COMBO_Y = Grid.FLOOR_Y + 3
--- componente vertical do empurrão (studs/s) — game feel, não balanceamento
local PUSH_UP = 18

-- ============================================================================
-- RECEITA DE TERRENO POR COMBO — a metade "mundo" de cada uma das 10 fusões.
-- A ORDEM importa e as ausências são deliberadas (a química do Terrain é
-- sequencial: Água CONGELA o lago e o gelo isola, Vento APAGA chama solitária):
--   · Eletrocussão NÃO aplica Água — congelaria a poça e mataria a condução;
--   · Tornado Flamejante NÃO aplica Vento — apagaria a chama recém-nascida;
--   · Cortina de Vapor aplica só Água — vapor é fogo APAGADO, não fogo somado.
-- `strong` = usa a variante tática da reação (Terra ergue muro em ÁREA).
-- ============================================================================
type Recipe = {
	--- elementos aplicados ao terreno, na ordem
	apply: { string },
	strong: boolean?,
	--- fração do raio do combo usada NO TERRENO (o dano continua no raio cheio)
	scale: number?,
	--- empurrão horizontal em studs/s (nil = combo não empurra)
	push: number?,
}

local K = Elements.comboKey

local EFFECTS: { [string]: Recipe } = {
	-- Tornado Flamejante: incendeia e SUGA/empurra (GDD §9)
	[K("Fire", "Wind")] = { apply = { "Fire" }, push = 70 },
	-- Chuva de Magma: queima a área e nega terreno
	[K("Fire", "Earth")] = { apply = { "Fire" } },
	-- Explosão de Plasma: burst; o raio ainda conduz por qualquer poça
	[K("Fire", "Lightning")] = { apply = { "Lightning", "Fire" }, push = 40 },
	-- Cortina de Vapor: a água escalda e APAGA o incêndio embaixo
	[K("Fire", "Water")] = { apply = { "Water" } },
	-- Eletrocussão: só Raio — a água conectada inteira eletriza (flood fill)
	[K("Water", "Lightning")] = { apply = { "Lightning" } },
	-- Lamaçal: a água encharca terra/carvão e vira lama (lentidão severa)
	[K("Water", "Earth")] = { apply = { "Water" } },
	-- Tempestade Torrencial: o counter do GDD §9 — APAGA fogo e empurra
	[K("Water", "Wind")] = { apply = { "Water" }, push = 55 },
	-- Tempestade de Areia: rajada que abafa chama pequena e cega
	[K("Earth", "Wind")] = { apply = { "Wind" }, push = 30 },
	-- Cristais Carregados: cacho de cristais (muros) + carga elétrica. O terreno
	-- usa METADE do raio de propósito: o GDD pede MINAS, não uma redoma — um
	-- disco cheio de muros no raio 16 prenderia o alvo por 20s (wallDuration).
	[K("Earth", "Lightning")] = { apply = { "Earth", "Lightning" }, strong = true, scale = 0.5 },
	-- Nuvem Tempestuosa: descarga que persegue; conduz na água
	[K("Wind", "Lightning")] = { apply = { "Lightning" }, push = 25 },
}

-- ---------------------------------------------------------------------- tipos
type Cast = {
	player: Player,
	element: Elements.Id,
	pos: Vector3,
	t: number,
}

type Channel = {
	a: Player,
	b: Player,
	elA: Elements.Id,
	elB: Elements.Id,
	combo: Elements.Combo,
	pos: Vector3,
	fireAt: number,
}

--- Ping de Sintonia (GDD §18.7). `withUserId` preenchido = pacto selado: os
--- DOIS veem o mesmo alvo marcado.
type Proposal = {
	player: Player,
	element: Elements.Id,
	pos: Vector3,
	expires: number,
	withUserId: number?,
}

local Sintonia = {}

local terrain: any = nil
local combat: any = nil
local evState: RemoteEvent
local evAnnounce: RemoteEvent

--- conjurações dentro da janela (podado todo step; ~120 entradas no pior caso)
local recent: { Cast } = {}
local channels: { Channel } = {}
--- proposta indexada pelo userId de QUEM PROPÔS
local proposals: { [number]: Proposal } = {}
--- cooldown compartilhado e trava de canalização, por userId
local readyAt: { [number]: number } = {}
local busy: { [number]: boolean } = {}
local pingReadyAt: { [number]: number } = {}

-- -------------------------------------------------------------------- helpers
local function rootOf(player: Player): BasePart?
	local char = player.Character
	if not char or not char.Parent then
		return nil
	end
	return char:FindFirstChild("HumanoidRootPart") :: BasePart?
end

local function humanoidOf(player: Player): Humanoid?
	local char = player.Character
	if not char or not char.Parent then
		return nil
	end
	return char:FindFirstChildOfClass("Humanoid")
end

local function isAlive(player: Player): boolean
	if not player.Parent then
		return false -- saiu do jogo
	end
	local hum = humanoidOf(player)
	return hum ~= nil and hum.Health > 0
end

local function onCooldown(player: Player): boolean
	return os.clock() < (readyAt[player.UserId] or 0)
end

local function flat(pos: Vector3): Vector3
	return Vector3.new(pos.X, COMBO_Y, pos.Z)
end

--- Pacto ativo entre os dois? Devolve o alvo marcado (GDD §18.7).
local function markBetween(from: number, to: number): Vector3?
	local p = proposals[from]
	if p and p.withUserId == to and os.clock() <= p.expires then
		return p.pos
	end
	return nil
end

local function pactMark(a: Player, b: Player): Vector3?
	return markBetween(a.UserId, b.UserId) or markBetween(b.UserId, a.UserId)
end

--- "Mesmo alvo/área" (GDD §9). Devolve o EPICENTRO do combo, ou nil.
--- ponytail: o pacto do Ping dá a única vantagem MECÂNICA do ping — as duas
--- conjurações valem se as duas caírem perto do ALVO COMBINADO, ainda que
--- longe uma da outra. Sem isso o ping seria só enfeite; com isso, combinar
--- com um desconhecido pelo botão compensa. Quando houver squads (R3), este é
--- o ponto exato de exigir "mesmo time".
local function epicenter(a: Cast, bPlayer: Player, bPos: Vector3): Vector3?
	if (a.pos - bPos).Magnitude <= S.targetRadius then
		return flat((a.pos + bPos) / 2)
	end
	local mark = pactMark(a.player, bPlayer)
	if mark and (a.pos - mark).Magnitude <= S.targetRadius and (bPos - mark).Magnitude <= S.targetRadius then
		return flat(mark)
	end
	return nil
end

--- Humanoides dentro do raio, menos os participantes. Usa a consulta espacial
--- NATIVA: pega bots (R3) e qualquer coisa com Humanoid de graça, e o filtro
--- exclui a pasta Arena (senão a esfera devolveria centenas de parts de piso).
local function humanoidsIn(pos: Vector3, radius: number, skip: { Player }): { Humanoid }
	local exclude: { Instance } = {}
	local arena = Workspace:FindFirstChild("Arena")
	if arena then
		table.insert(exclude, arena)
	end
	for _, p in ipairs(skip) do
		local char = p.Character
		if char then
			table.insert(exclude, char)
		end
	end

	local params = OverlapParams.new()
	params.FilterType = Enum.RaycastFilterType.Exclude
	params.FilterDescendantsInstances = exclude

	local out: { Humanoid } = {}
	local seen: { [Humanoid]: boolean } = {}
	for _, part in ipairs(Workspace:GetPartBoundsInRadius(pos, radius, params)) do
		local model = part:FindFirstAncestorOfClass("Model")
		local hum = if model then model:FindFirstChildOfClass("Humanoid") else nil
		if hum and not seen[hum] and hum.Health > 0 then
			seen[hum] = true
			table.insert(out, hum)
		end
	end
	return out
end

-- ------------------------------------------------------------------ broadcast
--- Fato decidido pelo servidor. O cliente SÓ APRESENTA (Net.SintoniaState).
local function announceState(payload: { [string]: any }, only: { Player }?)
	if only then
		for _, p in ipairs(only) do
			if p.Parent then
				evState:FireClient(p, payload)
			end
		end
	else
		evState:FireAllClients(payload)
	end
end

--- Nome do combo no idioma de CADA jogador. `Player.LocaleId` é legível no
--- servidor — não dá para mandar um texto só e acertar os dois idiomas.
local function announceCombo(combo: Elements.Combo)
	for _, p in ipairs(Players:GetPlayers()) do
		local isPt = string.sub(p.LocaleId or "", 1, 2) == "pt"
		evAnnounce:FireClient(p, {
			key = "announce.sintonia.fired",
			args = { if isPt then combo.pt else combo.en },
		})
	end
end

--- Placar da partida (raia R3). `Match.noteCombo` foi publicado LÁ pedindo que
--- a R2 chame na fusão — sem isso o Selo do Campeão mostra 0 combos (GDD
--- §18.6). Require preguiçoso e protegido, o mesmo padrão que o Combat usa
--- para falar com este módulo: se o Match sumir, a Sintonia continua de pé.
local match: any = nil
local function noteCombo(player: Player, comboKey: string)
	if match == nil then
		local mod = script.Parent and script.Parent:FindFirstChild("Match")
		local ok, m = pcall(function()
			return require(mod :: ModuleScript)
		end)
		match = (ok and m) or false
	end
	if match and match.noteCombo then
		pcall(match.noteCombo, player, comboKey)
	end
end

-- ----------------------------------------------------------------- canalização
--- Cobra o custo dos DOIS e abre a canalização. Ponto único onde a Sintonia
--- "gasta" — chamado antes de qualquer efeito, de propósito.
local function beginChannel(a: Cast, b: Player, elB: Elements.Id, pos: Vector3)
	local combo = Elements.getCombo(a.element, elB)
	if not combo then
		return -- mesmo elemento (ou par inexistente): não funde
	end
	local now = os.clock()

	busy[a.player.UserId] = true
	busy[b.UserId] = true
	-- CUSTO COBRADO JÁ (GDD §9): interromper NÃO devolve o cooldown. É o que
	-- torna o combo errado punitivo e o spam impossível.
	readyAt[a.player.UserId] = now + S.sharedCooldown
	readyAt[b.UserId] = now + S.sharedCooldown

	-- "Consome a magia dos DOIS": o cooldown compartilhado acima é a parte que
	-- vive nesta raia. A mana pertence ao Combat (raia B) e ainda não tem API
	-- pública — chamada opcional, no mesmo padrão que o Combat usa para falar
	-- com este módulo. Quando `Combat.drainMana` existir, passa a valer sozinha.
	if combat and combat.drainMana then
		pcall(combat.drainMana, a.player)
		pcall(combat.drainMana, b)
	end

	table.insert(channels, {
		a = a.player,
		b = b,
		elA = a.element,
		elB = elB,
		combo = combo,
		pos = pos,
		fireAt = now + S.channel,
	})

	-- TODOS veem a canalização, inimigo inclusive: é o telegrama que dá a
	-- janela de interrupção exigida pelo GDD §9.
	announceState({
		phase = "channeling",
		comboKey = combo.key,
		pos = pos,
		radius = combo.radius,
		duration = S.channel,
		byUserId = a.player.UserId,
		withUserId = b.UserId,
	})
end

local function releaseChannel(ch: Channel)
	busy[ch.a.UserId] = nil
	busy[ch.b.UserId] = nil
end

--- A fusão acontece: dano em área ×dmgMult + a receita de terreno do combo.
local function fireChannel(ch: Channel)
	local combo = ch.combo
	local recipe = EFFECTS[combo.key]

	-- 1) MUNDO — todo combo interage com o terreno (GDD §9)
	local cx, cy = Grid.worldToCell(ch.pos)
	if recipe and terrain and Grid.inBounds(cx, cy) then
		local radiusCells = combo.radius * (recipe.scale or 1) / Grid.CELL_SIZE
		for _, element in ipairs(recipe.apply) do
			terrain.applyElement(cx, cy, element, radiusCells, recipe.strong == true)
		end
	end

	-- 2) DANO — "mais forte que a soma das partes" (GDD §9), literalmente: a
	-- soma dos dois básicos multiplicada por `dmgMult`. Os dois conjuradores
	-- ficam de fora (a magia é DELES). Efeitos persistentes (lava, poça
	-- eletrizada, lama) continuam machucando pelo terreno, via Combat.
	local dmg = (Balance.elements[ch.elA].basic.dmg + Balance.elements[ch.elB].basic.dmg) * S.dmgMult
	local victims = humanoidsIn(ch.pos, combo.radius, { ch.a, ch.b })
	for i, hum in ipairs(victims) do
		-- crédito de evolução do Escudo alternado: o combo é dos DOIS e um só
		-- número de dano por alvo continua legível na tela (GDD §5).
		local credit = if i % 2 == 0 then ch.b else ch.a
		if combat then
			combat.applyDamage(hum, dmg, credit, true)
		end
		local push = recipe and recipe.push
		local root = hum.RootPart
		if push and root and root.Parent then
			local away = root.Position - ch.pos
			local dir = Vector3.new(away.X, 0, away.Z)
			dir = if dir.Magnitude > 1e-3 then dir.Unit else Vector3.new(0, 0, -1)
			-- ponytail: TETO CONHECIDO — o personagem tem network ownership do
			-- cliente dono, então isto é uma ORDEM, não garantia física (mesma
			-- ressalva do dash em Combat.step). Se o playtest mostrar empurrão
			-- borrachudo, o upgrade é root:SetNetworkOwner(nil) por ~0,3s.
			root.AssemblyLinearVelocity = dir * push + Vector3.new(0, PUSH_UP, 0)
		end
	end

	announceState({
		phase = "fired",
		comboKey = combo.key,
		pos = ch.pos,
		radius = combo.radius,
		duration = combo.duration,
		byUserId = ch.a.UserId,
		withUserId = ch.b.UserId,
		hits = #victims,
	})
	announceCombo(combo)
	noteCombo(ch.a, combo.key)
	noteCombo(ch.b, combo.key)
end

-- --------------------------------------------------------------------- API
function Sintonia.init(terrainModule: any, combatModule: any)
	terrain = terrainModule
	combat = combatModule
	evState = Net.event(Net.Names.SintoniaState)
	evAnnounce = Net.event(Net.Names.Announce)
end

--- Chamado pelo Combat a cada conjuração que impacta: alimenta a janela.
--- Trata TUDO como hostil: elemento válido, vivo, sem cooldown, sem canalizar.
function Sintonia.registerCast(player: Player, elementId: string, pos: Vector3)
	if not evState or typeof(pos) ~= "Vector3" or not Elements.isValid(elementId) then
		return
	end
	if not isAlive(player) or busy[player.UserId] or onCooldown(player) then
		return
	end
	local element = elementId :: Elements.Id
	local now = os.clock()

	for i = #recent, 1, -1 do
		local c = recent[i]
		if now - c.t > S.window then
			table.remove(recent, i) -- fora da janela de 1,5s
			continue
		end
		-- mesmo jogador não combina consigo; MESMO ELEMENTO NÃO FUNDE (GDD §9)
		if c.player == player or c.element == element then
			continue
		end
		if busy[c.player.UserId] or onCooldown(c.player) or not isAlive(c.player) then
			continue
		end
		local center = epicenter(c, player, pos)
		if center then
			table.remove(recent, i)
			beginChannel(c, player, element, center)
			return
		end
	end

	table.insert(recent, { player = player, element = element, pos = pos, t = now })
end

-- ------------------------------------------------- Ping de Sintonia (§18.7)
--- Posição pingada pelo cliente NUNCA entra crua: rejeita NaN/infinito e
--- prende o alvo ao alcance de uma magia a partir de quem pingou.
local function sanitizePos(raw: any, from: Vector3): Vector3
	if typeof(raw) ~= "Vector3" then
		return from
	end
	local m = raw.Magnitude
	if m ~= m or m == math.huge then
		return from
	end
	local d = raw - from
	if d.Magnitude > PING_RANGE then
		return from + d.Unit * PING_RANGE
	end
	return raw
end

--- "COMBO?" — propõe Sintonia a quem estiver por perto, com o elemento ativo.
function Sintonia.propose(player: Player, rawPos: any)
	if not evState or not isAlive(player) then
		return
	end
	local uid = player.UserId
	local now = os.clock()
	if now < (pingReadyAt[uid] or 0) then
		return -- anti-spam de ping
	end
	local root = rootOf(player)
	if not root then
		return
	end
	local element = combat and combat.getElement(player)
	if not Elements.isValid(element) then
		return
	end

	pingReadyAt[uid] = now + PING_COOLDOWN
	local pos = flat(sanitizePos(rawPos, root.Position))
	proposals[uid] = {
		player = player,
		element = element :: Elements.Id,
		pos = pos,
		expires = now + PROPOSAL_TTL,
		withUserId = nil,
	}

	local payload = {
		phase = "proposed",
		byUserId = uid,
		byName = player.DisplayName,
		element = element,
		pos = pos,
		ttl = PROPOSAL_TTL,
	}
	--- o proponente vê a própria marca; os vizinhos veem a proposta
	local audience: { Player } = { player }
	for _, other in ipairs(Players:GetPlayers()) do
		local r = if other ~= player then rootOf(other) else nil
		if r and (r.Position - root.Position).Magnitude <= PING_HEAR then
			table.insert(audience, other)
			evAnnounce:FireClient(other, {
				key = "announce.sintonia.proposed",
				args = { player.DisplayName },
			})
		end
	end
	announceState(payload, audience)
end

--- 1 toque do aliado: o alvo passa a ser COMUM aos dois (GDD §18.7).
function Sintonia.accept(player: Player, fromUserId: any)
	if not evState or typeof(fromUserId) ~= "number" then
		return
	end
	local prop = proposals[fromUserId]
	if not prop or prop.player == player or os.clock() > prop.expires then
		return
	end
	if not isAlive(player) or not isAlive(prop.player) then
		return
	end
	local mine, theirs = rootOf(player), rootOf(prop.player)
	if not mine or not theirs or (mine.Position - theirs.Position).Magnitude > PING_HEAR then
		return -- aceitar do outro lado do mapa não é combinar
	end

	prop.withUserId = player.UserId
	prop.expires = os.clock() + PACT_TTL
	announceState({
		phase = "proposed",
		byUserId = fromUserId,
		byName = prop.player.DisplayName,
		withUserId = player.UserId,
		withName = player.DisplayName,
		element = prop.element,
		pos = prop.pos,
		ttl = PACT_TTL,
	}, { player, prop.player })
end

-- -------------------------------------------------------------------- tick
function Sintonia.step(_dt: number)
	local now = os.clock()

	for i = #recent, 1, -1 do
		if now - recent[i].t > S.window then
			table.remove(recent, i)
		end
	end
	for uid, p in pairs(proposals) do
		if now > p.expires then
			proposals[uid] = nil
		end
	end

	for i = #channels, 1, -1 do
		local ch = channels[i]
		if not isAlive(ch.a) or not isAlive(ch.b) then
			-- INTERROMPIDO (GDD §9): morreu no meio da canalização. O custo
			-- já foi cobrado — a dupla fica seca. É o risco do combo.
			table.remove(channels, i)
			releaseChannel(ch)
			announceState({
				phase = "failed",
				comboKey = ch.combo.key,
				pos = ch.pos,
				byUserId = ch.a.UserId,
				withUserId = ch.b.UserId,
			})
		elseif now >= ch.fireAt then
			table.remove(channels, i)
			releaseChannel(ch)
			fireChannel(ch)
		end
	end
end

function Sintonia.onPlayerRemoving(player: Player)
	local uid = player.UserId
	proposals[uid] = nil
	readyAt[uid] = nil
	busy[uid] = nil
	pingReadyAt[uid] = nil
	for i = #recent, 1, -1 do
		if recent[i].player == player then
			table.remove(recent, i)
		end
	end
	-- os canais dele morrem no próximo step (isAlive falha sem Parent), mas
	-- soltar a referência aqui evita segurar o Player por um frame inteiro.
	for i = #channels, 1, -1 do
		local ch = channels[i]
		if ch.a == player or ch.b == player then
			table.remove(channels, i)
			releaseChannel(ch)
			announceState({
				phase = "failed",
				comboKey = ch.combo.key,
				pos = ch.pos,
				byUserId = ch.a.UserId,
				withUserId = ch.b.UserId,
			})
		end
	end
end

-- ============================================================================
-- AUTOTESTE — cole no Command Bar do Studio (servidor):
--   require(game.ServerScriptService.Server.Sintonia).selfTest()
-- Cobre o que não dá para ver a olho: a matriz completa, a regra de "mesmo
-- elemento não funde", a geometria da janela e a conta do dano.
-- ============================================================================
function Sintonia.selfTest()
	-- 1) os 10 pares existem e TODOS têm receita de terreno (combo sem receita
	--    seria uma fusão que não muda o mundo — quebra o pilar do GDD §9)
	local pairs10 = 0
	for _, a in ipairs(Elements.Order) do
		for _, b in ipairs(Elements.Order) do
			local combo = Elements.getCombo(a, b)
			if a == b then
				assert(combo == nil, "mesmo elemento NÃO pode fundir: " .. a)
			else
				assert(combo, "par sem combo na matriz: " .. a .. "+" .. b)
				local r = EFFECTS[(combo :: Elements.Combo).key]
				assert(r and #r.apply > 0, "combo sem reação de terreno: " .. (combo :: Elements.Combo).key)
			end
		end
	end
	for key in pairs(Elements.Combos) do
		assert(EFFECTS[key], "receita faltando para " .. key)
		pairs10 += 1
	end
	assert(pairs10 == 10, "a matriz do GDD §9 tem 10 pares, achei " .. pairs10)

	-- 2) receitas que dependem da ORDEM da química do Terrain
	assert(#EFFECTS[K("Water", "Lightning")].apply == 1, "Eletrocussão não pode aplicar Água (congelaria a poça)")
	assert(EFFECTS[K("Water", "Lightning")].apply[1] == "Lightning", "Eletrocussão conduz pelo Raio")
	assert(EFFECTS[K("Fire", "Wind")].apply[1] == "Fire", "Tornado Flamejante incendeia (Vento apagaria)")
	assert(EFFECTS[K("Water", "Wind")].apply[1] == "Water", "Tempestade Torrencial tem de APAGAR fogo")

	-- 3) janela/área: a mesma conta do `epicenter`, sem estado de jogo
	local o = Vector3.new(0, 0, 0)
	local near = Vector3.new(S.targetRadius - 1, 0, 0)
	local far = Vector3.new(S.targetRadius + 1, 0, 0)
	assert((o - near).Magnitude <= S.targetRadius, "dentro do raio deve fundir")
	assert((o - far).Magnitude > S.targetRadius, "fora do raio NÃO pode fundir")

	-- 4) "mais forte que a soma das partes" (GDD §9)
	local a, b = Balance.elements.Fire.basic.dmg, Balance.elements.Lightning.basic.dmg
	assert((a + b) * S.dmgMult > a + b, "dmgMult tem de ser > 1")
	assert(S.channel > 0 and S.window > 0 and S.sharedCooldown > S.window, "limitadores do GDD §9 inconsistentes")

	print(string.format("[Sintonia] autoteste OK — %d combos, dmgMult %.2f", pairs10, S.dmgMult))
	return true
end

return Sintonia
