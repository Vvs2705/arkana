--!strict
-- ============================================================================
-- RAIA A (Arena/Terreno) — TERRENO REATIVO: constrói o mundo em blocos e roda
-- a máquina de estados da química (GDD §14 + as 3 regras de BotW, GDD §16.1).
-- Este módulo é a AUTORIDADE do estado do mapa. Só o servidor o executa.
--
-- AS 3 REGRAS (GDD §16.1), que são a arquitetura deste arquivo:
--   1. elementos mudam o ESTADO de materiais  → applyFire/Water/Earth/Lightning
--   2. elementos mudam o estado de outros ELEMENTOS (água apaga fogo) → idem
--   3. materiais NUNCA mudam materiais → `cell.material` é escrito uma só vez,
--      no build; toda a magia acontece em `cell.state`.
--
-- MODELO DE FOGO (corrigido em 18/08, achado da auditoria externa):
--   O modelo antigo re-sorteava `propagateChance` (30%) por vizinho A CADA TICK
--   durante os 8s de queima — até 16 rolagens para a MESMA aresta:
--       1 - (1 - 0,30)^16 ≈ 99,67%
--   Isso não é "propagação de 30%", é "floresta conectada queima inteira, com
--   certeza". É a carbonização que já tinha reprovado no protótipo 2D.
--   Agora são DUAS regras, e as duas são teto:
--     a) UMA ROLAGEM POR ARESTA — quando uma célula COMEÇA a queimar, cada
--        vizinho combustível recebe UMA chance (`spreadChancePerEdge`) e nunca
--        mais. O fogo vira uma frente que avança, não uma sentença.
--     b) ORÇAMENTO DE COMBUSTÍVEL — cada IGNIÇÃO (o incêndio que nasce de uma
--        magia) carrega `fuelBudget` células. Cada célula que a FRENTE acende
--        consome 1; esgotou, a frente para mesmo cercada de floresta. É o teto
--        duro que não depende de sorteio — e o que torna o fogo balanceável.
--   O orçamento cobre a PROPAGAÇÃO, não o raio da magia: as células acesas
--   direto pelo impacto são sementes e não cobram dele (senão uma tática de
--   raio 3 gastaria o orçamento inteiro no próprio impacto e nada propagaria).
--
-- ORÇAMENTO DE PARTS (mobile, GDD §19.2 — teto ~4000): ≈2676 no boot —
--   piso ~1100 (runs horizontais com teto de 4 células, para cada laje ter o
--   próprio tom) + borda 4 + água 232 + árvore 307×3 + rocha ~80×2 + grama 219
--   + 23 dos marcos verticais dos POIs (`buildMark`).
--   Estados em células de piso (Mud, Wall) criam part PREGUIÇOSAMENTE e a
--   destroem ao expirar — o mapa nunca acumula geometria morta.
--   Se precisar cortar: GROUND_RUN_MAX 4 → 6 devolve ~140 parts.
--
-- FRONTEIRA DE RESPONSABILIDADE: o Terrain NUNCA aplica dano. Ele publica o
-- perigo em `hazardAt` e o Combat (raia B) decide quem toma quanto.
-- ============================================================================
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local Workspace = game:GetService("Workspace")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Grid = require(Shared:WaitForChild("Grid"))
local Balance = require(Shared:WaitForChild("Balance"))
--- só pelo hash determinístico (variação visual sem semente global)
local Arena = require(script.Parent:WaitForChild("Arena"))

local Terrain = {}

local T = Balance.terrain
local CELL = Grid.CELL_SIZE
local FLOOR_Y = Grid.FLOOR_Y

-- --------------------------------------------------------------- geometria
--- espessura das lajes de piso: as laterais formam a margem/barranco do lago
local GROUND_THICK = 6
--- o lago é um rebaixo REAL no terreno; congelar ergue a lâmina até o chão
local WATER_THICK = 4
local WATER_DEPTH = 2.5
--- MARGEM EM DEGRAU (achado do playtest 18/08: dava para cair e não sair).
--- A água que faz fronteira com terra é RASA e forma um degrau; o miolo do lago
--- continua fundo — a profundidade é justamente o que o Diretor aprovou.
--- Os dois degraus (terra→raso = 1.1, raso→fundo = 1.4) ficam abaixo do
--- step-up automático do Humanoid (~HipHeight, ≈2 studs no R15): sai andando,
--- sem depender do pulo. NÃO SUBIR estes números sem re-testar a saída.
local SHORE_DEPTH = 1.1
local WATER_Y = FLOOR_Y - WATER_DEPTH - WATER_THICK / 2
local SHORE_Y = FLOOR_Y - SHORE_DEPTH - WATER_THICK / 2
local ICE_Y = FLOOR_Y - WATER_THICK / 2
--- tronco largo o bastante para a floresta bloquear de fato (personagem ≈ 2.6
--- studs de largura desde o avatar 1.3× — o vão de 1.4 entre troncos barra)
local TRUNK_W = 2.6
local TRUNK_H = 7
local STUMP_H = 2
--- copa em 2 blocos (corpo largo + coroa estreita): silhueta escalonada, o look
--- Pixel Gun. O corpo é maior que a célula, então copas vizinhas ainda fecham
--- um teto contínuo. Só o TOPO varia por árvore (o corpo começa sempre em
--- TRUNK_H): baixar a copa raspava a cabeça do avatar 1.3× (~7.4 studs).
local CANOPY_H = 3.2
local CANOPY_VARY = 1.2
local CROWN_H = 1.6
local CROWN_W = Grid.CELL_SIZE - 1.4
--- areia da praia: detalhe de level art, não contrato de gameplay (o material
--- da célula continua sendo Dirt — é nela que a Água vira lamaçal)
local SAND = Color3.fromHex("#C2A567")
--- teto do merge de piso: cada laje ganha o próprio tom e o campo deixa de ser
--- uma chapa de cor única. Menor = mais variação e mais parts.
local GROUND_RUN_MAX = 4
--- sais do hash visual (distintos dos do Arena para não correlacionar camadas)
local SALT_CELL, SALT_GROUND, SALT_TREE, SALT_ROCK = 53, 59, 61, 67
--- tintas finas de chão (lama / carvão) — encostam no piso sem z-fighting
local FILM_THICK = 0.4
local FILM_Y = FLOOR_Y + 0.05 - FILM_THICK / 2
--- o paredão de borda é estrutura, não afloramento: alto demais para pular fora
local BORDER_H = Grid.HEIGHTS.Rock * 2

-- ------------------------------------------------------------------- estado
--- grade achatada: idx = cy * COLS + cx + 1
local cells: { Grid.Cell } = {}
--- troncos das árvores (a `part` da célula é a COPA — é ela que some no fogo)
local trunks: { [number]: BasePart } = {}
--- parts criadas sob demanda para estados em células de piso (Wall, Mud)
local fx: { [number]: BasePart } = {}
--- células com timer rodando. A simulação SÓ itera aqui — nunca as 3600.
local active: { [number]: boolean } = {}
--- células cuja COBERTURA o fogo já consumiu. É um fato PERMANENTE do mapa e
--- por isso vive FORA de `cell.state`: estados transitórios (Mud, Wall) passam
--- por cima de Burned e, ao expirarem, a árvore não pode "ressuscitar".
local burnedOut: { [number]: boolean } = {}

--- INCÊNDIO = uma frente com orçamento próprio (ver "MODELO DE FOGO" no topo).
--- `seeds` são as células acesas pelo impacto da magia; `spread` as que a frente
--- conquistou (as únicas que cobram do `budget`); `alive` é quantas ainda
--- queimam — chega a zero, o incêndio acabou e vira uma linha de telemetria.
type Fire = {
	id: number,
	budget: number,
	seeds: number,
	spread: number,
	alive: number,
	cx: number,
	cy: number,
}
local fires: { [number]: Fire } = {}
--- célula acesa → id do incêndio que a acendeu (só enquanto ela queima)
local fireOf: { [number]: number } = {}
--- células que ainda devem fazer a ÚNICA rolagem por aresta (no próximo tick:
--- é o que faz a frente avançar um anel por tick em vez de tudo no mesmo frame)
local pendingSpread: { [number]: boolean } = {}
local nextFireId = 0

local folder: Folder? = nil
local acc = 0
--- gerador próprio: não mexe no estado global de `math.random` das outras raias
local rng = Random.new()

--- guarda de performance do flood fill elétrico (não é número de balanceamento)
local ELECTRIFY_FLOOD_LIMIT = 200

local function idxOf(cx: number, cy: number): number
	return cy * Grid.COLS + cx + 1
end

-- ------------------------------------------------------------------- visual
local function newPart(name: string, size: Vector3, pos: Vector3, color: Color3, collide: boolean): BasePart
	local p = Instance.new("Part")
	p.Name = name
	p.Anchored = true
	p.CanCollide = collide
	-- SmoothPlastic = cor chapada, sem textura fotorrealista (direção blocky)
	p.Material = Enum.Material.SmoothPlastic
	p.TopSurface = Enum.SurfaceType.Smooth
	p.BottomSurface = Enum.SurfaceType.Smooth
	p.Size = size
	p.Position = pos
	p.Color = color
	p.Parent = folder
	return p
end

local function shade(c: Color3, f: number): Color3
	return Color3.new(math.min(c.R * f, 1), math.min(c.G * f, 1), math.min(c.B * f, 1))
end

--- HIERARQUIA VISUAL (auditoria P2, 18/08): o CENÁRIO cede saturação para os
--- 5 elementais (saturados, GDD §10) dominarem a leitura do combate. Puxa a
--- cor na direção do próprio cinza de luminância — mesma claridade, menos
--- croma (dessaturado ≠ escuro: a iluminação global já foi corrigida à parte).
--- Aplica-se a chão, borda, rocha e tronco — NUNCA a cor de ESTADO (fogo,
--- gelo, raio, lama: isso é gameplay e continua saturado).
local DESAT = 0.24
local function desat(c: Color3): Color3
	local g = (c.R + c.G + c.B) / 3
	return Color3.new(c.R + (g - c.R) * DESAT, c.G + (g - c.G) * DESAT, c.B + (g - c.B) * DESAT)
end

--- Água de MARGEM: faz fronteira (8-vizinhança) com algo que não é água. É o
--- degrau raso pelo qual se sai do lago andando. Puro do layout — não guarda
--- tabela nem depende de ordem de construção.
local function isShoreWater(cx: number, cy: number): boolean
	for dy = -1, 1 do
		for dx = -1, 1 do
			local x, y = cx + dx, cy + dy
			if not Grid.inBounds(x, y) or cells[idxOf(x, y)].material ~= "Water" then
				return true
			end
		end
	end
	return false
end

local function waterY(cx: number, cy: number): number
	return if isShoreWater(cx, cy) then SHORE_Y else WATER_Y
end

--- cor do MATERIAL com variação sutil por célula (mata o efeito "chapa de cor
--- única"). Só vale no estado Normal: estado tem cor própria e é gameplay.
local function baseColor(cell: Grid.Cell): Color3
	local c = Grid.COLORS[cell.material]
	if cell.material == "Water" and isShoreWater(cell.cx, cell.cy) then
		c = shade(c, 1.22) -- o raso lê mais claro: a margem se anuncia sozinha
	elseif cell.material == "Rock" then
		c = desat(c) -- rocha é cenário: cede saturação para a magia (auditoria P2)
	end
	return shade(c, 0.93 + Arena.hash01(cell.cx, cell.cy, SALT_CELL) * 0.14)
end

--- o estado sobrepõe a cor do material (Normal não tem cor de estado)
local function stateColor(cell: Grid.Cell): Color3
	return Grid.STATE_COLORS[cell.state] or baseColor(cell)
end

--- altura do tufo de grama alta: varia SÓ PARA CIMA — Grid.HEIGHTS.TallGrass é
--- o mínimo que ESCONDE o personagem, e sem esconderijo a contra-jogada do
--- GDD §14 ("fogo em grama alta revela quem se escondia") não tem o que revelar.
local function tallGrassH(cx: number, cy: number): number
	return Grid.HEIGHTS.TallGrass + Arena.hash01(cx, cy, SALT_CELL) * 0.9
end

--- corpo da copa desta árvore (a coroa vai por cima) — varia a linha do topo
local function canopyH(cx: number, cy: number): number
	return CANOPY_H + Arena.hash01(cx, cy, SALT_TREE) * CANOPY_VARY
end

--- Reflete `cell.state` na geometria. Único ponto que toca parts fora do build.
local function applyVisual(idx: number)
	local cell = cells[idx]
	local center = Grid.cellToWorld(cell.cx, cell.cy)
	local st = cell.state

	-- GEOMETRIA SOB DEMANDA: muro da Terra (bloco alto) e lamaçal em célula de
	-- piso (tinta fina) dividem a MESMA slot. Trocar de estado troca a part —
	-- reaproveitar a existente faria um lamaçal virar muro invisível.
	local want: string? = if st == "Wall"
		then "Wall"
		elseif st == "Mud" and cell.part == nil then "Mud"
		else nil
	local have = fx[idx]
	if have and have.Name ~= want then
		have:Destroy() -- nada de geometria morta acumulada no mapa
		fx[idx] = nil
		have = nil
	end
	if want == "Wall" then
		if not have then
			fx[idx] = newPart(
				"Wall",
				Vector3.new(CELL, Grid.WALL_HEIGHT, CELL),
				Vector3.new(center.X, FLOOR_Y + Grid.WALL_HEIGHT / 2, center.Z),
				Grid.STATE_COLORS.Wall,
				true
			)
		end
		return
	elseif want == "Mud" then
		if not have then
			fx[idx] = newPart(
				"Mud",
				Vector3.new(CELL, FILM_THICK, CELL),
				Vector3.new(center.X, FILM_Y, center.Z),
				Grid.STATE_COLORS.Mud,
				false
			)
		end
		return
	end

	if cell.material == "Tree" then
		if burnedOut[idx] then
			-- a COBERTURA DESAPARECE (GDD §14): a copa cai, sobra o toco
			if cell.part then
				cell.part:Destroy()
				cell.part = nil
			end
			local trunk = trunks[idx]
			if trunk then
				trunk.Size = Vector3.new(TRUNK_W, STUMP_H, TRUNK_W)
				trunk.Position = Vector3.new(center.X, FLOOR_Y + STUMP_H / 2, center.Z)
				trunk.Color = Grid.STATE_COLORS.Burned
			end
		elseif cell.part then
			local col = stateColor(cell)
			cell.part.Color = col
			for _, ch in ipairs(cell.part:GetChildren()) do
				if ch:IsA("BasePart") then
					ch.Color = col -- a coroa acompanha o corpo (fogo tem que ler)
				end
			end
		end
		return
	end

	if cell.material == "Water" then
		local p = cell.part
		if p then
			p.Color = stateColor(cell)
			-- congelar ERGUE a lâmina até o nível do chão: a ponte é literal.
			-- Fora do gelo, cada célula volta à SUA cota (rasa na margem).
			local y = if st == "Frozen" then ICE_Y else waterY(cell.cx, cell.cy)
			p.Position = Vector3.new(center.X, y, center.Z)
		end
		return
	end

	local p = cell.part
	if p then
		p.Color = stateColor(cell)
		if cell.material == "TallGrass" then
			-- grama alta queimada vira cinza rente ao chão (deixa de esconder)
			local h = if burnedOut[idx] then FILM_THICK else tallGrassH(cell.cx, cell.cy)
			p.Size = Vector3.new(CELL, h, CELL)
			p.Position = Vector3.new(center.X, FLOOR_Y + h / 2, center.Z)
		end
	end
end

-- ------------------------------------------------------------- telemetria
--- Require PREGUIÇOSO e protegido: a auditoria quer medir "células afetadas por
--- ignição" para detectar carbonização, mas telemetria é observação — se o
--- coletor não existir, o terreno segue queimando. `nil` = não resolvido ainda,
--- `false` = ausente (não tenta de novo).
local telemetry: any = nil
local fireEvent: string = "terrain_state_change"

local function emitFireOut(fire: Fire)
	if telemetry == nil then
		local ok, tel, ev = pcall(function()
			return require(script.Parent:FindFirstChild("Telemetry") :: any) :: any,
				require(Shared:FindFirstChild("Events") :: any) :: any
		end)
		telemetry = if ok and tel then tel else false
		if ok and ev and ev.Name and ev.Name.TerrainStateChange then
			fireEvent = ev.Name.TerrainStateChange
		end
	end
	if not telemetry then
		return
	end
	pcall(telemetry.emit, fireEvent, nil, {
		-- contrato que o coletor documentou para terreno: { state, cells }. É por
		-- `state = "Burning"` + `element` que ele conta isto como IGNIÇÃO.
		state = "Burning",
		element = "Fire",
		--- o evento sai quando a frente MORRE (é aí que o tamanho é definitivo)
		reason = "fire_out",
		--- O NÚMERO QUE A AUDITORIA PEDIU: células afetadas por esta ignição.
		cells = fire.seeds + fire.spread,
		seeds = fire.seeds,
		spread = fire.spread,
		fuel_left = fire.budget,
		--- frente parada por falta de combustível (e não por falta de floresta):
		--- se isto vier alto no dashboard, `fuelBudget` está apertado demais
		fuel_exhausted = fire.budget <= 0,
		cx = fire.cx,
		cy = fire.cy,
	})
end

-- --------------------------------------------------------- máquina de estados
--- Célula deixou de queimar (expirou, apagou na água, virou muro): devolve a
--- vaga ao incêndio. Quando a última chama de uma frente morre, o incêndio se
--- fecha e reporta o tamanho. Ponto ÚNICO — todo caminho passa por `setState`.
local function leaveFire(idx: number)
	local id = fireOf[idx]
	fireOf[idx] = nil
	pendingSpread[idx] = nil
	local fire = if id then fires[id] else nil
	if not fire then
		return
	end
	fire.alive -= 1
	if fire.alive <= 0 then
		fires[fire.id] = nil
		emitFireOut(fire)
	end
end

--- Escreve o estado, agenda a expiração e sincroniza o visual. `duration <= 0`
--- significa estado permanente (Burned) ou volta ao Normal.
local function setState(idx: number, state: Grid.State, duration: number)
	local cell = cells[idx]
	if cell.state == "Burning" and state ~= "Burning" then
		leaveFire(idx)
	end
	cell.state = state
	if duration > 0 then
		cell.stateUntil = os.clock() + duration
		active[idx] = true
	else
		cell.stateUntil = 0
		active[idx] = nil
	end
	applyVisual(idx)
end

--- estado de repouso da célula: carvão é permanente, todo o resto volta ao Normal
local function restState(idx: number): Grid.State
	return if burnedOut[idx] then "Burned" else "Normal"
end

--- LIÇÃO DO PROTÓTIPO 2D (docs/HISTORICO.md): o fogo consome COBERTURA
--- (floresta e grama alta) e NUNCA o campo aberto — senão o incêndio carboniza
--- o mapa inteiro. `Grid.FLAMMABLE` e `Balance.terrain.grassBurns` são a lei.
local function isFlammable(idx: number): boolean
	local cell = cells[idx]
	if cell.state ~= "Normal" or burnedOut[idx] then
		return false -- o que já virou carvão não queima de novo
	end
	if cell.material == "Grass" then
		return T.grassBurns
	end
	return Grid.FLAMMABLE[cell.material] == true
end

--- Abre um incêndio: uma ignição = uma magia de Fogo = um orçamento.
local function newFire(cx: number, cy: number): Fire
	nextFireId += 1
	local fire: Fire = { id = nextFireId, budget = T.fuelBudget, seeds = 0, spread = 0, alive = 0, cx = cx, cy = cy }
	fires[nextFireId] = fire
	return fire
end

--- Acende `idx` dentro da frente `fire`. `charge = true` quando a chama veio da
--- PROPAGAÇÃO (rolagem de aresta ou rajada de Vento) — só ela cobra combustível;
--- as sementes do impacto da magia são de graça (ver "MODELO DE FOGO" no topo).
local function ignite(idx: number, fire: Fire, charge: boolean): boolean
	if not isFlammable(idx) then
		return false
	end
	if charge then
		if fire.budget <= 0 then
			return false -- teto duro: a frente para mesmo com floresta em volta
		end
		fire.budget -= 1
		fire.spread += 1
	else
		fire.seeds += 1
	end
	fireOf[idx] = fire.id
	fire.alive += 1
	pendingSpread[idx] = true -- fará a sua ÚNICA rolagem por aresta no próximo tick
	setState(idx, "Burning", T.burnDuration)
	return true
end

-- ------------------------------------------------------- iteração espacial
local function forDisc(cx: number, cy: number, r: number, fn: (idx: number, cell: Grid.Cell) -> ())
	local ri = math.max(0, math.ceil(r))
	local rr = r * r
	for dy = -ri, ri do
		for dx = -ri, ri do
			if dx * dx + dy * dy <= rr then
				local x, y = cx + dx, cy + dy
				if Grid.inBounds(x, y) then
					local i = idxOf(x, y)
					fn(i, cells[i])
				end
			end
		end
	end
end

local function forNeighbors(idx: number, fn: (nIdx: number) -> ())
	local cell = cells[idx]
	for _, d in ipairs(Grid.NEIGHBORS4) do
		local x, y = cell.cx + d[1], cell.cy + d[2]
		if Grid.inBounds(x, y) then
			fn(idxOf(x, y))
		end
	end
end

-- --------------------------------------------------------------- construção
local function emitGroundRun(x0: number, x1: number, cy: number, base: Color3)
	local a = Grid.cellToWorld(x0, cy)
	local b = Grid.cellToWorld(x1, cy)
	newPart(
		"Ground",
		Vector3.new((x1 - x0 + 1) * CELL, GROUND_THICK, CELL),
		Vector3.new((a.X + b.X) / 2, FLOOR_Y - GROUND_THICK / 2, a.Z),
		-- chão dessaturado (auditoria P2): o projétil elemental domina a leitura
		shade(desat(base), 0.94 + Arena.hash01(x0, cy, SALT_GROUND) * 0.12),
		true
	)
end

--- praia: terra encostada na água (8-vizinhança) ganha cor de areia. É pintura
--- — o material continua Dirt, então a regra "Água + terra = lamaçal" fica de pé.
local function isBeach(layout: { { Grid.Material } }, cx: number, cy: number): boolean
	if layout[cy][cx] ~= "Dirt" then
		return false
	end
	for dy = -1, 1 do
		for dx = -1, 1 do
			local x, y = cx + dx, cy + dy
			if Grid.inBounds(x, y) and layout[y][x] == "Water" then
				return true
			end
		end
	end
	return false
end

--- Piso em lajes mescladas por linha (greedy horizontal, com teto de
--- comprimento para que cada laje tenha o próprio tom). O lago fica de fora de
--- propósito: sem laje embaixo, a água é um rebaixo real e não um decalque.
local function buildGround(layout: { { Grid.Material } })
	for cy = 1, Grid.ROWS - 2 do
		local runKey: string? = nil
		local runColor: Color3? = nil
		local runStart = 0
		for cx = 1, Grid.COLS - 1 do
			local key: string? = nil
			local color: Color3? = nil
			-- cx == COLS-1 é o anel de borda: entra como "sem piso" e fecha o run
			if cx < Grid.COLS - 1 then
				local m = layout[cy][cx]
				if m ~= "Water" then
					if isBeach(layout, cx, cy) then
						key, color = "Sand", SAND
					else
						key, color = m, Grid.COLORS[m]
					end
				end
			end
			if key ~= runKey or cx - runStart >= GROUND_RUN_MAX then
				if runKey ~= nil then
					emitGroundRun(runStart, cx - 1, cy, runColor :: Color3)
				end
				runKey = key
				runColor = color
				runStart = cx
			end
		end
	end
end

--- Paredão natural da borda em 4 lajes (economiza ~236 parts do anel de rocha).
local function buildBorder()
	local w, d = Grid.WIDTH_STUDS, Grid.DEPTH_STUDS
	local half = (w - CELL) / 2
	local y = FLOOR_Y + BORDER_H / 2 - GROUND_THICK
	local rock = desat(Grid.COLORS.Rock)
	newPart("BorderN", Vector3.new(w, BORDER_H, CELL), Vector3.new(0, y, -half), rock, true)
	newPart("BorderS", Vector3.new(w, BORDER_H, CELL), Vector3.new(0, y, half), rock, true)
	newPart("BorderW", Vector3.new(CELL, BORDER_H, d), Vector3.new(-half, y, 0), rock, true)
	newPart("BorderE", Vector3.new(CELL, BORDER_H, d), Vector3.new(half, y, 0), rock, true)
end

-- ------------------------------------------------------------------- marcos
--- DOURADO ARCANO (GDD §10) no topo dos 4 marcos: o MESMO acento em todos —
--- a família visual que faz os POIs lerem como "o mesmo jogo" na thumbnail.
--- SmoothPlastic de propósito, NÃO Neon: neon é o vocabulário dos altares de
--- revive (sinal de gameplay); marco é orientação, não objetivo.
local GOLD = Color3.fromHex("#F0C75E")

--- célula de pegada → marco (preenchido no build a partir de Arena.MARKS)
local markAt: { [number]: Arena.Mark } = {}

--- Silhuetas dos marcos verticais (auditoria P2: "reconhecível de longe").
--- Geometria própria em células de ROCHA — as regras (bloqueia andar e tiro,
--- não queima, não aceita muro) já vêm do material; aqui é SÓ forma. Tudo
--- axis-aligned: o look blocky nem quer rotação, e o harness de QA não a
--- simula. Peças acima da cabeça vão SEM colisão (silhueta não pode travar
--- movimento que o layout declara livre). Alturas escalonadas de propósito
--- (47/44/39/24): quatro assinaturas distintas contra o céu.
local function buildMark(m: Arena.Mark)
	local w = (m.w or 1) * CELL
	local c0 = Grid.cellToWorld(m.cx, m.cy)
	-- centro da PEGADA (marcos 2×2 ficam entre células)
	local x = c0.X + (w - CELL) / 2
	local z = c0.Z + (w - CELL) / 2
	local stone = desat(Grid.COLORS.Rock)
	local kind = m.kind

	if kind == "TempleTower" then
		-- o marco-mestre do mapa: pé no píer (a laje de chão sob a célula de
		-- rocha aflora no nível do piso, rente ao gelo quando o lago congela)
		newPart("MarcoTorreBase", Vector3.new(6.4, 3, 6.4), Vector3.new(x, FLOOR_Y + 1.5, z), shade(stone, 0.92), true)
		newPart("MarcoTorre", Vector3.new(3.6, 30, 3.6), Vector3.new(x, FLOOR_Y + 18, z), stone, true)
		newPart("MarcoTorreTopo", Vector3.new(5.4, 6, 5.4), Vector3.new(x, FLOOR_Y + 36, z), shade(stone, 1.08), false)
		newPart("MarcoTorreCoroa", Vector3.new(1.8, 8, 1.8), Vector3.new(x, FLOOR_Y + 43, z), GOLD, false)
	elseif kind == "TempleColumn" then
		-- colunas do templo: em cima do gelo são COBERTURA (altura de rocha)
		newPart("MarcoColuna", Vector3.new(2.4, 13, 2.4), Vector3.new(x, FLOOR_Y + 6.5, z), stone, true)
		newPart("MarcoCapitel", Vector3.new(3.6, 1.8, 3.6), Vector3.new(x, FLOOR_Y + 13.9, z), shade(stone, 1.1), false)
	elseif kind == "MotherTree" then
		local bark = desat(shade(Grid.COLORS.Dirt, 0.8))
		newPart("MarcoArvoreMae", Vector3.new(6, 30, 6), Vector3.new(x, FLOOR_Y + 15, z), bark, true)
		local leaf = Grid.COLORS.Tree
		newPart("MarcoCopa1", Vector3.new(15, 7, 15), Vector3.new(x, FLOOR_Y + 30.5, z), shade(leaf, 1.06), false)
		newPart("MarcoCopa2", Vector3.new(10, 6, 10), Vector3.new(x, FLOOR_Y + 37, z), shade(leaf, 1.14), false)
		newPart("MarcoFlor", Vector3.new(5, 4.5, 5), Vector3.new(x, FLOOR_Y + 42.25, z), GOLD, false)
	elseif kind == "Obelisk" then
		newPart("MarcoObeliscoBase", Vector3.new(5, 2.5, 5), Vector3.new(x, FLOOR_Y + 1.25, z), shade(stone, 0.9), true)
		newPart("MarcoObelisco", Vector3.new(3, 18, 3), Vector3.new(x, FLOOR_Y + 11.5, z), stone, true)
		-- a RACHADURA é deslocamento, não rotação: o tramo de cima sai 0.9 stud
		-- do eixo — lê como quebrado a qualquer distância, custa zero física
		newPart("MarcoObeliscoAlto", Vector3.new(2.4, 14, 2.4), Vector3.new(x + 0.9, FLOOR_Y + 27.5, z), shade(stone, 1.05), false)
		newPart("MarcoObeliscoPonta", Vector3.new(1.6, 4.5, 1.6), Vector3.new(x + 0.9, FLOOR_Y + 36.75, z), GOLD, false)
	elseif kind == "Menhir" then
		newPart("MarcoMenir", Vector3.new(3, 24, 3), Vector3.new(x, FLOOR_Y + 12, z), stone, true)
		newPart("MarcoMenirRuna", Vector3.new(3.3, 1.6, 3.3), Vector3.new(x, FLOOR_Y + 19, z), GOLD, false)
		-- a laje caída aponta para LESTE e morre dentro do canal: 1.9 de altura
		-- fica abaixo do step-up (~2) — sobe andando, vira ponte improvisada.
		-- Colisão LIGADA de propósito: só ADICIONA rota, nunca tira (nenhuma
		-- célula que o layout diz livre fica bloqueada por uma peça de 1.9).
		newPart("MarcoMenirLaje", Vector3.new(11, 1.9, 2.4), Vector3.new(x + 7, FLOOR_Y + 0.95, z), shade(stone, 0.85), true)
	end
end

local function buildCellPart(idx: number, cell: Grid.Cell)
	local c = Grid.cellToWorld(cell.cx, cell.cy)
	local mat = cell.material

	local col = baseColor(cell)

	if mat == "Water" then
		cell.part = newPart(
			"Water",
			Vector3.new(CELL, WATER_THICK, CELL),
			Vector3.new(c.X, waterY(cell.cx, cell.cy), c.Z),
			col,
			true
		)
	elseif mat == "Tree" then
		-- a copa começa sempre no topo do tronco; o que varia é a linha do topo
		local bodyH = canopyH(cell.cx, cell.cy)
		trunks[idx] = newPart(
			"Trunk",
			Vector3.new(TRUNK_W, TRUNK_H, TRUNK_W),
			Vector3.new(c.X, FLOOR_Y + TRUNK_H / 2, c.Z),
			desat(shade(Grid.COLORS.Dirt, 0.86)),
			true
		)
		local canopy = newPart(
			"Canopy",
			Vector3.new(CELL + 0.2, bodyH, CELL + 0.2),
			Vector3.new(c.X, FLOOR_Y + TRUNK_H + bodyH / 2, c.Z),
			col,
			true
		)
		-- coroa FILHA da copa: some junto quando o fogo derruba a cobertura,
		-- sem uma segunda tabela de bookkeeping para manter em sincronia
		local crown = newPart(
			"Crown",
			Vector3.new(CROWN_W, CROWN_H, CROWN_W),
			Vector3.new(c.X, FLOOR_Y + TRUNK_H + bodyH + CROWN_H / 2, c.Z),
			col,
			false
		)
		crown.Parent = canopy
		cell.part = canopy
	elseif mat == "Rock" then
		if markAt[idx] then
			return -- pegada de marco: a silhueta é do buildMark, não o bloco padrão
		end
		local v = Arena.hash01(cell.cx, cell.cy, SALT_ROCK)
		-- varia SÓ PARA CIMA: Grid.HEIGHTS.Rock é a altura que faz da rocha
		-- COBERTURA (esconde o mago inteiro). Encolher afloramento = furar cobertura.
		local h = Grid.HEIGHTS.Rock * (1 + v * 0.35)
		local rock = newPart("Rock", Vector3.new(CELL, h, CELL), Vector3.new(c.X, FLOOR_Y + h / 2, c.Z), col, true)
		-- lasca de topo (decorativa, sem colisão — nasce acima da cabeça):
		-- quebra o cubo perfeito e dá a leitura "facetada" do afloramento
		local capH = h * 0.3
		local cap = newPart(
			"RockCap",
			Vector3.new(CELL - 1.2, capH, CELL - 1.2),
			Vector3.new(c.X + (v - 0.5) * 1.2, FLOOR_Y + h + capH / 2, c.Z + (0.5 - v) * 1.2),
			shade(col, 1.12),
			false
		)
		cap.Parent = rock
		cell.part = rock
	elseif mat == "TallGrass" then
		local h = tallGrassH(cell.cx, cell.cy)
		-- sem colisão: a grama alta atrasa (speedMultAt), não barra
		cell.part = newPart("TallGrass", Vector3.new(CELL, h, CELL), Vector3.new(c.X, FLOOR_Y + h / 2, c.Z), col, false)
	end
end

--- Constrói as partes do mundo a partir do layout (chamado 1x no boot).
function Terrain.build(layout: { { Grid.Material } })
	local old = Workspace:FindFirstChild("Arena")
	if old then
		old:Destroy()
	end
	local f = Instance.new("Folder")
	f.Name = "Arena"
	f.Parent = Workspace
	folder = f

	cells = {}
	trunks = {}
	fx = {}
	active = {}
	burnedOut = {}
	fires = {}
	fireOf = {}
	pendingSpread = {}
	acc = 0

	table.clear(markAt)
	for _, m in ipairs(Arena.MARKS) do
		Arena.forMarkCells(m, function(mcx, mcy)
			markAt[idxOf(mcx, mcy)] = m
		end)
	end

	for cy = 0, Grid.ROWS - 1 do
		for cx = 0, Grid.COLS - 1 do
			cells[idxOf(cx, cy)] = {
				cx = cx,
				cy = cy,
				material = layout[cy][cx],
				state = "Normal" :: Grid.State,
				stateUntil = 0,
				wallHp = 0,
			}
		end
	end

	buildGround(layout)
	buildBorder()
	for cy = 1, Grid.ROWS - 2 do
		for cx = 1, Grid.COLS - 2 do
			local i = idxOf(cx, cy)
			buildCellPart(i, cells[i])
		end
	end
	-- marcos verticais dos POIs (a pegada já é rocha; aqui entra a silhueta)
	for _, m in ipairs(Arena.MARKS) do
		buildMark(m)
	end

	-- sem SpawnLocation o Roblox larga o jogador em (0,0,0) e ele cai do mundo
	local spawn = Instance.new("SpawnLocation")
	spawn.Name = "ArenaSpawn"
	spawn.Anchored = true
	spawn.Neutral = true
	spawn.Duration = 0 -- zero forcefield: o Campo de Provas testa combate real
	spawn.Material = Enum.Material.SmoothPlastic
	spawn.Color = Grid.COLORS.Road
	spawn.Size = Vector3.new(CELL * 3, 1, CELL * 3)
	spawn.Position = Vector3.new(0, FLOOR_Y + 0.5, 0)
	spawn.Parent = f
end

-- ------------------------------------------------------------------ consultas
--- Célula lógica (ou nil fora dos limites).
function Terrain.getCell(cx: number, cy: number): Grid.Cell?
	if not Grid.inBounds(cx, cy) then
		return nil
	end
	return cells[idxOf(cx, cy)]
end

--- Bloqueia movimento? (água só é atravessável quando congelada — GDD §14)
function Terrain.isBlocking(cx: number, cy: number): boolean
	local cell = Terrain.getCell(cx, cy)
	if not cell then
		return true -- fora do mapa é paredão
	end
	if cell.state == "Wall" then
		return true
	end
	local m = cell.material
	if m == "Rock" then
		return true
	elseif m == "Tree" then
		-- queimada não bloqueia: queimar a floresta ABRE caminho (é uma jogada).
		-- Consulta `burnedOut`, não o estado: um lamaçal sobre o carvão é
		-- transitório, a copa derrubada não é.
		return not burnedOut[idxOf(cx, cy)]
	elseif m == "Water" then
		return cell.state ~= "Frozen"
	end
	return false
end

--- Barra um PROJÉTIL na altura do peito? Só obstáculo alto: árvore em pé, rocha
--- e muro. A ÁGUA NÃO barra tiro (embora barre o andar, ver `isBlocking`) —
--- se barrasse, todo disparo estouraria na margem e o lago congelado deixaria
--- de ser rota, matando a pergunta de validação V3 (docs/ROBLOX.md §1).
function Terrain.blocksProjectile(cx: number, cy: number): boolean
	local cell = Terrain.getCell(cx, cy)
	if not cell then
		return true -- o paredão de borda para o projétil
	end
	if cell.state == "Wall" then
		return true
	end
	local m = cell.material
	if m == "Rock" then
		return true
	elseif m == "Tree" then
		return not burnedOut[idxOf(cx, cy)] -- árvore em pé (viva ou em chamas)
	end
	return false
end

--- Multiplicador de velocidade no ponto (lama desacelera, grama alta um pouco).
function Terrain.speedMultAt(pos: Vector3): number
	local cell = Terrain.getCell(Grid.worldToCell(pos))
	if not cell then
		return 1
	end
	if cell.state == "Mud" then
		return T.mudSlowMult
	end
	if cell.material == "TallGrass" and cell.state == "Normal" then
		return T.tallGrassSpeedMult
	end
	return 1
end

--- DPS do perigo no ponto e seu tipo, ou (0, nil). O Terrain NUNCA aplica dano:
--- quem lê isto e decide quem morre é o Combat (raia B).
function Terrain.hazardAt(pos: Vector3): (number, string?)
	local cell = Terrain.getCell(Grid.worldToCell(pos))
	if not cell then
		return 0, nil
	end
	if cell.state == "Burning" then
		-- o dano sobe de burnDmgMin a burnDmgMax conforme a célula queima
		local remaining = math.max(0, cell.stateUntil - os.clock())
		local f = math.clamp(1 - remaining / T.burnDuration, 0, 1)
		return T.burnDmgMin + (T.burnDmgMax - T.burnDmgMin) * f, "Burning"
	elseif cell.state == "Electrified" then
		return T.electrifyDps, "Electrified"
	end
	return 0, nil
end

-- -------------------------------------------------------------- reações (§14)
--- FOGO: acende cobertura, derrete o gelo e seca a lama.
local function applyFire(cx: number, cy: number, r: number)
	-- o incêndio só nasce se houver o que queimar: sem isto, cada bola de fogo
	-- em campo aberto abriria uma frente vazia e uma linha de telemetria falsa
	local fire: Fire? = nil
	forDisc(cx, cy, r, function(i, cell)
		if cell.state == "Frozen" or cell.state == "Mud" then
			setState(i, restState(i), 0) -- fogo derrete o gelo e seca a lama
		elseif isFlammable(i) then
			local f = fire or newFire(cx, cy)
			fire = f
			ignite(i, f, false) -- semente: o raio da magia não cobra combustível
		end
	end)
end

--- ÁGUA: apaga o fogo, congela o lago (vira ponte) e faz lamaçal em terra/carvão.
local function applyWater(cx: number, cy: number, r: number)
	forDisc(cx, cy, r, function(i, cell)
		if cell.state == "Burning" then
			setState(i, restState(i), 0) -- material preservado: apagar não carboniza
		elseif cell.material == "Water" and (cell.state == "Normal" or cell.state == "Electrified") then
			setState(i, "Frozen", T.freezeDuration) -- congelar também corta a condução
		-- `Silt` (lodo da baixada) entra JUNTO com `Dirt`: é o mesmo chão de
		-- terra com outra cor, e a baixada é justamente onde "Água + terra =
		-- lamaçal" (§14) é a jogada da casa. Deixá-lo de fora apagaria a
		-- interação no único POI construído em volta dela.
		elseif
			cell.state == "Burned"
			or ((cell.material == "Dirt" or cell.material == "Silt") and cell.state == "Normal")
		then
			setState(i, "Mud", T.mudDuration)
		end
	end)
end

--- ANTI-GRIEFING G2 (R9) — quem está DE PÉ em cada célula da área.
--- O muro de Terra é um bloco maciço de `Grid.WALL_HEIGHT` studs: nascer NA
--- célula de um personagem o enterra dentro da pedra. O caso que fecha o alpha
--- é o parceiro (duas pessoas estranhas em dupla), mas a regra vale para TODOS
--- — assim ela não precisa saber quem conjurou (o `applyElement` não carrega
--- autor, e inventar esse parâmetro custaria a assinatura em 4 raias) e não vira
--- vantagem competitiva de ninguém: emparedar inimigo por `wallDuration` também
--- nunca foi jogada de design, é um botão de deletar.
--- ponytail: TETO CONHECIDO — a célula do ROOT, não o volume do corpo. Quem
--- estiver rente à divisa pode ver o muro subir encostado nele (o motor empurra,
--- não enterra). Se o playtest reclamar, o upgrade é marcar também a célula do
--- canto mais próximo.
local function standingCells(cx: number, cy: number, r: number): { [number]: boolean }
	local out: { [number]: boolean } = {}
	local params = OverlapParams.new()
	params.FilterType = Enum.RaycastFilterType.Exclude
	-- a arena inteira fora: senão a esfera devolve centenas de parts de piso
	params.FilterDescendantsInstances = if folder then { folder :: Instance } else {}
	local center = Grid.cellToWorld(cx, cy)
	-- +2 células de folga: pega o corpo fora do centro e quem está no ar
	for _, part in ipairs(Workspace:GetPartBoundsInRadius(center, (r + 2) * CELL, params)) do
		local model = part:FindFirstAncestorOfClass("Model")
		local hum = if model then model:FindFirstChildOfClass("Humanoid") else nil
		local root = if hum and hum.Health > 0 then hum.RootPart else nil
		if root then
			local ox, oy = Grid.worldToCell(root.Position)
			if Grid.inBounds(ox, oy) then
				out[idxOf(ox, oy)] = true
			end
		end
	end
	return out
end

--- TERRA: ergue muro destrutível. Não nasce muro sobre água, rocha ou árvore —
--- nem em cima de gente (G2).
local function applyEarth(cx: number, cy: number, r: number, strong: boolean)
	--- resolvido PREGUIÇOSAMENTE: a consulta espacial só sai quando algum muro
	--- de verdade ia subir (basic de Terra impacta ~4x/s e a maioria bate em
	--- água/rocha/árvore ou em muro já erguido)
	local standing: { [number]: boolean }? = nil
	local function raiseWall(i: number)
		local cell = cells[i]
		if cell.state == "Wall" then
			return
		end
		local m = cell.material
		if m == "Water" or m == "Rock" or m == "Tree" then
			return
		end
		local occupied = standing
		if occupied == nil then
			occupied = standingCells(cx, cy, r)
			standing = occupied
		end
		if occupied[i] then
			return -- G2: ninguém é emparedado dentro da própria célula
		end
		cell.wallHp = T.wallHp
		setState(i, "Wall", T.wallDuration) -- a terra sela o chão: sufoca fogo e lama
	end

	if strong then
		forDisc(cx, cy, r, function(i)
			raiseWall(i)
		end)
	elseif Grid.inBounds(cx, cy) then
		raiseWall(idxOf(cx, cy))
	end
end

--- VENTO — triângulo do fogo (GDD §16.4): a chama pequena (≤1 vizinho em
--- chamas) fica sem oxigênio e APAGA; o incêndio estabelecido (≥2) é ALIMENTADO
--- e espalha na hora. É a arma de dois gumes da tabela do GDD §14.
local function applyWind(cx: number, cy: number, r: number)
	local burning: { number } = {}
	forDisc(cx, cy, r, function(i, cell)
		if cell.state == "Burning" then
			table.insert(burning, i)
		end
	end)
	-- fotografa os contadores ANTES de agir: a rajada é simultânea
	local counts: { number } = {}
	for k, i in ipairs(burning) do
		local n = 0
		forNeighbors(i, function(nIdx)
			if cells[nIdx].state == "Burning" then
				n += 1
			end
		end)
		counts[k] = n
	end
	for k, i in ipairs(burning) do
		if counts[k] <= 1 then
			setState(i, restState(i), 0) -- sem oxigênio, a chama pequena morre
		else
			-- a rajada acende sem sorteio (é a arma de dois gumes do GDD §14),
			-- mas COBRA COMBUSTÍVEL da frente: se o Vento não pagasse, ele seria
			-- exatamente o buraco pelo qual a carbonização voltaria.
			local fire = fires[fireOf[i] or 0]
			if fire then
				forNeighbors(i, function(nIdx)
					ignite(nIdx, fire, true)
				end)
			end
		end
	end
end

--- RAIO — condução (GDD §14): eletriza TODA a água conectada por flood fill.
--- Gelo e rocha isolam naturalmente (o fill só atravessa água líquida).
local function applyLightning(cx: number, cy: number, r: number)
	local function isLiquid(cell: Grid.Cell): boolean
		return cell.material == "Water" and cell.state ~= "Frozen"
	end

	-- fonte: a célula de impacto, senão a primeira água líquida do raio
	local source: number? = nil
	if Grid.inBounds(cx, cy) and isLiquid(cells[idxOf(cx, cy)]) then
		source = idxOf(cx, cy)
	else
		forDisc(cx, cy, r, function(i, cell)
			if source == nil and isLiquid(cell) then
				source = i
			end
		end)
	end
	if source == nil then
		return
	end

	local start = source :: number
	local queue: { number } = { start }
	local seen: { [number]: boolean } = { [start] = true }
	local head = 1
	while head <= #queue and head <= ELECTRIFY_FLOOD_LIMIT do
		local i = queue[head]
		head += 1
		setState(i, "Electrified", T.electrifyDuration) -- refresca se já estava
		forNeighbors(i, function(nIdx)
			if not seen[nIdx] and isLiquid(cells[nIdx]) then
				seen[nIdx] = true
				table.insert(queue, nIdx)
			end
		end)
	end
end

--- ENTRADA DA QUÍMICA: uma magia do elemento `element` atinge a área.
--- `strong` = magia tática (efeitos maiores: muro de pedra em área).
function Terrain.applyElement(cx: number, cy: number, element: string, radiusCells: number, strong: boolean)
	if element == "Fire" then
		applyFire(cx, cy, radiusCells)
	elseif element == "Water" then
		applyWater(cx, cy, radiusCells)
	elseif element == "Earth" then
		applyEarth(cx, cy, radiusCells, strong)
	elseif element == "Wind" then
		applyWind(cx, cy, radiusCells)
	elseif element == "Lightning" then
		applyLightning(cx, cy, radiusCells)
	end
end

--- Dano em muro atingido por projétil. Árvore NÃO cai a tiro (decisão herdada
--- do 2D): remover cobertura natural é exclusividade do FOGO — mantém o pilar
--- do GDD §14 legível e o tuning simples.
function Terrain.damageWall(cx: number, cy: number, dmg: number)
	local cell = Terrain.getCell(cx, cy)
	if not cell or cell.state ~= "Wall" then
		return
	end
	cell.wallHp -= dmg
	if cell.wallHp <= 0 then
		local i = idxOf(cx, cy)
		cell.wallHp = 0
		setState(i, restState(i), 0) -- o muro desmorona
	end
end

-- ------------------------------------------------------------------ simulação
--- Um tick: propaga o fogo e expira os estados. Itera SÓ as células ativas.
local function simTick()
	local now = os.clock()
	-- fotografia: células acesas AGORA só entram na propagação no próximo tick
	local snapshot: { number } = {}
	for i in pairs(active) do
		table.insert(snapshot, i)
	end

	for _, i in ipairs(snapshot) do
		local cell = cells[i]
		if cell.state == "Burning" then
			-- UMA ROLAGEM POR ARESTA: a célula sorteia cada vizinho UMA vez, no
			-- tick seguinte à própria ignição, e nunca mais. `pendingSpread` é o
			-- que garante o "nunca mais" — era daí que vinham as 16 rolagens.
			if pendingSpread[i] then
				pendingSpread[i] = nil
				local fire = fires[fireOf[i] or 0]
				if fire then
					forNeighbors(i, function(nIdx)
						if fire.budget > 0 and isFlammable(nIdx) and rng:NextNumber() < T.spreadChancePerEdge then
							ignite(nIdx, fire, true)
						end
					end)
				end
			end
			if now >= cell.stateUntil then
				-- carvão PERMANENTE: a cobertura sumiu e o mapa mudou de verdade
				burnedOut[i] = true
				setState(i, "Burned", 0)
			end
		elseif cell.stateUntil > 0 and now >= cell.stateUntil then
			if cell.state == "Wall" then
				cell.wallHp = 0
			end
			setState(i, restState(i), 0)
		end
	end
end

-- ============================================================================
-- AUTOTESTE (rodar na Command Bar do Studio, DEPOIS do Play — precisa do mapa
-- construído):  require(game.ServerScriptService.Server.Terrain).selfTest()
-- Cobre o que custou um playtest para descobrir: dá para SAIR de todo lugar
-- onde dá para ENTRAR. Reprova de novo se alguém mexer nas cotas do lago.
-- Cobre também a correção do fogo (auditoria 18/08): uma ignição no meio da
-- floresta densa NÃO pode consumir a floresta inteira.
-- ============================================================================
--- cota em que o personagem PISA nesta célula
local function standY(cx: number, cy: number): number
	local cell = cells[idxOf(cx, cy)]
	return if cell.material == "Water" then waterY(cx, cy) else FLOOR_Y
end

local CELL_COUNT = Grid.COLS * Grid.ROWS

--- CORAÇÃO DA MAIOR MANCHA COMBUSTÍVEL CONTÍGUA — o pior lugar do mapa para
--- acender. Não basta procurar um 5×5 denso: uma ilha de grama alta é densa e
--- não prova nada; o que a auditoria teme é a FLORESTA CONECTADA. Então: acha
--- as componentes por flood 4-vizinhos, fica com a maior e, dentro dela, mira
--- a janela 5×5 mais cheia. Varre em vez de usar coordenada mágica — o layout é
--- de outra raia e pode mudar sem avisar este teste.
local function forestHeart(): (number, number, number)
	local comp: { [number]: number } = {}
	local bestId, bestSize, nextId = 0, 0, 0
	for cy = 1, Grid.ROWS - 2 do
		for cx = 1, Grid.COLS - 2 do
			local start = idxOf(cx, cy)
			if comp[start] == nil and Grid.FLAMMABLE[cells[start].material] then
				nextId += 1
				comp[start] = nextId
				local queue = { start }
				local head = 1
				while head <= #queue do
					local i = queue[head]
					head += 1
					forNeighbors(i, function(n)
						if comp[n] == nil and Grid.FLAMMABLE[cells[n].material] then
							comp[n] = nextId
							table.insert(queue, n)
						end
					end)
				end
				if #queue > bestSize then
					bestId, bestSize = nextId, #queue
				end
			end
		end
	end

	local bx, by, best = 0, 0, -1
	for cy = 3, Grid.ROWS - 4 do
		for cx = 3, Grid.COLS - 4 do
			if comp[idxOf(cx, cy)] == bestId then
				local n = 0
				for dy = -2, 2 do
					for dx = -2, 2 do
						if Grid.FLAMMABLE[cells[idxOf(cx + dx, cy + dy)].material] then
							n += 1
						end
					end
				end
				if n > best then
					bx, by, best = cx, cy, n
				end
			end
		end
	end
	return bx, by, bestSize
end

--- Apaga tudo que a simulação acendeu: o autoteste não pode entregar o mapa
--- carbonizado para a partida que vem depois dele.
local function clearFires()
	fires = {}
	fireOf = {}
	pendingSpread = {}
	for i = 1, CELL_COUNT do
		local st = cells[i].state
		if st == "Burning" or st == "Burned" then
			burnedOut[i] = nil
			setState(i, "Normal", 0)
		end
	end
end

--- SIMULAÇÃO OFFLINE DE UM INCÊNDIO (sem Studio e sem esperar os 8s de
--- relógio): acende e roda os ticks na mão. O RELÓGIO NÃO ANDA durante o laço,
--- então NENHUMA célula expira — este é o PIOR CASO do modelo: nada se apaga
--- sozinho, quem segura a frente é só o orçamento. Devolve (acesas, sementes,
--- propagadas) e deixa o mapa como encontrou.
local function simulateIgnition(cx: number, cy: number, radius: number): (number, number, number)
	local before = nextFireId
	applyFire(cx, cy, radius)
	local fire = if nextFireId > before then fires[nextFireId] else nil
	for _ = 1, 64 do -- 64 ticks = 32 s de jogo; a frente para muito antes
		simTick()
	end
	local lit = 0
	for i = 1, CELL_COUNT do
		if cells[i].state == "Burning" then
			lit += 1
		end
	end
	local seeds = if fire then fire.seeds else 0
	local spread = if fire then fire.spread else 0
	clearFires()
	return lit, seeds, spread
end

function Terrain.selfTest()
	assert(#cells > 0, "rode DEPOIS do build (Play), senão não há mapa para testar")
	--- degrau que o Humanoid sobe andando. Fica abaixo da HipHeight (≈2 no R15,
	--- ≈2.6 com o avatar 1.3×) — conservador de propósito.
	local STEP = 2.0
	local function walkable(cx: number, cy: number): boolean
		if cx <= 0 or cy <= 0 or cx >= Grid.COLS - 1 or cy >= Grid.ROWS - 1 then
			return false
		end
		local m = cells[idxOf(cx, cy)].material
		return m ~= "Rock" and m ~= "Tree"
	end

	-- BFS do campo aberto central subindo só degraus ≤ STEP. Descer é sempre
	-- livre, então "alcançável a pé" ⇒ "escapável a pé".
	local seen: { [number]: boolean } = {}
	local queue: { { number } } = { { 30, 30 } }
	seen[idxOf(30, 30)] = true
	local head = 1
	while head <= #queue do
		local c = queue[head]
		head += 1
		for _, d in ipairs(Grid.NEIGHBORS4) do
			local x, y = c[1] + d[1], c[2] + d[2]
			if walkable(x, y) and not seen[idxOf(x, y)] and standY(x, y) - standY(c[1], c[2]) <= STEP then
				seen[idxOf(x, y)] = true
				table.insert(queue, { x, y })
			end
		end
	end

	local water, shore, stuck = 0, 0, 0
	for cy = 1, Grid.ROWS - 2 do
		for cx = 1, Grid.COLS - 2 do
			if not walkable(cx, cy) then
				continue
			end
			if cells[idxOf(cx, cy)].material == "Water" then
				water += 1
				if isShoreWater(cx, cy) then
					shore += 1
				end
			end
			if not seen[idxOf(cx, cy)] then
				stuck += 1
				warn(string.format("[Terrain] célula sem saída em (%d,%d)", cx, cy))
			end
		end
	end
	assert(shore > 0, "lago sem margem rasa: sem o degrau ninguém sai da água")
	assert(stuck == 0, string.format("%d células prenderiam o jogador", stuck))

	-- ---------------------------------------------------------------- FOGO
	-- O ASSERT QUE PAGA A CORREÇÃO DA AUDITORIA (18/08): com o modelo antigo
	-- (30% por vizinho a cada tick, 16 rolagens ≈ 99,7%) uma ignição no meio da
	-- floresta acendia a mancha contígua INTEIRA (~238 células) e este teste
	-- reprovaria. Cada rodada acende no ponto mais denso do mapa e roda o pior
	-- caso (relógio parado: nada se apaga sozinho).
	local flammable = 0
	for i = 1, CELL_COUNT do
		if Grid.FLAMMABLE[cells[i].material] then
			flammable += 1
		end
	end
	local fx0, fy0, patch = forestHeart()
	local radius = Balance.elements.Fire.tactic.terrainRadius
	local RUNS = 12
	--- teto de leitura: uma ignição não pode levar 1/3 da mancha contígua em que
	--- caiu. É folgado de propósito (o teto real é `seeds + fuelBudget`) — quem
	--- reprova aqui é regressão de MODELO, não variação de sorteio. Com o modelo
	--- antigo (99,7% por aresta) daria a mancha INTEIRA e reprovaria na hora.
	local sane = patch // 3
	local worst, sum = 0, 0
	for _ = 1, RUNS do
		local lit, seeds, spread = simulateIgnition(fx0, fy0, radius)
		assert(
			spread <= T.fuelBudget,
			string.format("orçamento de combustível furado: %d células propagadas > %d", spread, T.fuelBudget)
		)
		assert(
			lit <= seeds + T.fuelBudget,
			string.format("incêndio passou do teto: %d acesas > %d sementes + %d de orçamento", lit, seeds, T.fuelBudget)
		)
		assert(
			lit <= sane,
			string.format("carbonização: 1 ignição acendeu %d células da mancha de %d", lit, patch)
		)
		worst = math.max(worst, lit)
		sum += lit
	end

	-- -------------------------------------------- MURO NÃO EMPAREDA (G2, R9)
	-- O muro da Terra nascia sem olhar quem estava de pé na célula: dava para
	-- enterrar o próprio parceiro dentro de pedra por `wallDuration` (20 s) com
	-- uma tática de Terra — griefing puro entre estranhos pareados em dupla.
	-- O teste cobra as DUAS metades da correção: a célula de quem está de pé
	-- fica LIVRE **e** as outras viram muro (a proteção não pode custar poder;
	-- o muro continua sendo jogada tática de verdade).
	local WR = 2 -- o maior disco de muro do jogo: tática de Terra e Cristais Carregados
	local wx, wy = 0, 0
	for cy = WR + 1, Grid.ROWS - WR - 2 do
		for cx = WR + 1, Grid.COLS - WR - 2 do
			local free = true
			forDisc(cx, cy, WR, function(_, cell)
				local m = cell.material
				if m == "Water" or m == "Rock" or m == "Tree" or cell.state ~= "Normal" then
					free = false
				end
			end)
			if free then
				wx, wy = cx, cy
				break
			end
		end
		if wx > 0 then
			break
		end
	end
	assert(wx > 0, "não achei disco de terreno livre para testar o muro")

	local rig = Instance.new("Model")
	rig.Name = "TerrainTesteMuro"
	local rigRoot = Instance.new("Part")
	rigRoot.Name = "HumanoidRootPart"
	rigRoot.Anchored = true
	rigRoot.Position = Grid.cellToWorld(wx, wy) + Vector3.new(0, 3, 0)
	rigRoot.Parent = rig
	local rigHum = Instance.new("Humanoid")
	rigHum.Health = 100 -- vivo: cadáver não segura muro (a regra olha Health > 0)
	rigHum.Parent = rig
	rig.Parent = Workspace

	applyEarth(wx, wy, WR, true) -- o disco inteiro EM CIMA dele
	assert(
		cells[idxOf(wx, wy)].state ~= "Wall",
		string.format("MURO EM CIMA DE QUEM ESTÁ DE PÉ em (%d,%d): dá para prender o parceiro dentro da pedra", wx, wy)
	)
	local raised = 0
	forDisc(wx, wy, WR, function(i)
		if cells[i].state == "Wall" then
			raised += 1
			setState(i, restState(i), 0) -- o autoteste não entrega a arena murada
		end
	end)
	rig:Destroy()
	assert(raised >= 8, "o muro tem de continuar subindo em volta — proteção não pode virar nerf (" .. raised .. ")")

	-- ------------------------------------- LODO AINDA VIRA LAMAÇAL (R9, item 5)
	-- O lodo da baixada deixou de ser `Dirt` para ganhar cor própria. As regras
	-- dele têm de ser as mesmas: se `applyWater` esquecer o material novo, a
	-- baixada continua BONITA e para de fazer lamaçal — o POI inteiro foi
	-- construído em volta dessa interação (§14) e a perda seria silenciosa.
	local silt = 0
	for i, cell in ipairs(cells) do
		if cell.material == "Silt" and cell.state == "Normal" then
			silt = i
			break
		end
	end
	assert(silt > 0, "a baixada não tem lodo: o POI sudeste sumiu do layout")
	applyWater(cells[silt].cx, cells[silt].cy, 0)
	assert(cells[silt].state == "Mud", "Água no lodo parou de fazer lamaçal (§14) — a cor nova levou a regra junto")
	setState(silt, restState(silt), 0) -- o autoteste não entrega a arena molhada

	local parts = if folder then #folder:GetDescendants() else 0
	assert(parts <= 4000, "orçamento de parts do GDD §19.2 estourado: " .. parts)
	print(string.format("[Terrain] autoteste OK — %d parts · lago %d (%d de margem) · 0 armadilhas", parts, water, shore))
	print(
		string.format(
			"[Terrain] fogo no coração da floresta (%d,%d) · %d ignições · pior caso %d células (média %.1f)"
				.. " · mancha contígua %d · combustível do mapa %d · teto de sanidade %d",
			fx0,
			fy0,
			RUNS,
			worst,
			sum / RUNS,
			patch,
			flammable,
			sane
		)
	)
end

--- Tick da simulação em passo fixo (`burnTick`). Chamado pelo Heartbeat do Main
--- — este módulo nunca tem loop nem `wait()` próprio.
function Terrain.step(dt: number)
	acc += dt
	while acc >= T.burnTick do
		acc -= T.burnTick
		simTick()
	end
end

return Terrain
