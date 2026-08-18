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
-- ORÇAMENTO DE PARTS (mobile, GDD §19.2 — teto ~4000): ≈2650 no boot —
--   piso 994 (runs horizontais com teto de 4 células, para cada laje ter o
--   próprio tom) + borda 4 + água 205 + árvore 389×3 + rocha 51×2 + grama 176.
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

-- --------------------------------------------------------- máquina de estados
--- Escreve o estado, agenda a expiração e sincroniza o visual. `duration <= 0`
--- significa estado permanente (Burned) ou volta ao Normal.
local function setState(idx: number, state: Grid.State, duration: number)
	local cell = cells[idx]
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

local function ignite(idx: number)
	if isFlammable(idx) then
		setState(idx, "Burning", T.burnDuration)
	end
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
		shade(base, 0.94 + Arena.hash01(x0, cy, SALT_GROUND) * 0.12),
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
	local rock = Grid.COLORS.Rock
	newPart("BorderN", Vector3.new(w, BORDER_H, CELL), Vector3.new(0, y, -half), rock, true)
	newPart("BorderS", Vector3.new(w, BORDER_H, CELL), Vector3.new(0, y, half), rock, true)
	newPart("BorderW", Vector3.new(CELL, BORDER_H, d), Vector3.new(-half, y, 0), rock, true)
	newPart("BorderE", Vector3.new(CELL, BORDER_H, d), Vector3.new(half, y, 0), rock, true)
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
			shade(Grid.COLORS.Dirt, 0.86),
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
	acc = 0

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
	forDisc(cx, cy, r, function(i, cell)
		if cell.state == "Frozen" or cell.state == "Mud" then
			setState(i, restState(i), 0) -- fogo derrete o gelo e seca a lama
		elseif isFlammable(i) then
			ignite(i)
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
		elseif cell.state == "Burned" or (cell.material == "Dirt" and cell.state == "Normal") then
			setState(i, "Mud", T.mudDuration)
		end
	end)
end

--- TERRA: ergue muro destrutível. Não nasce muro sobre água, rocha ou árvore.
local function applyEarth(cx: number, cy: number, r: number, strong: boolean)
	local function raiseWall(i: number)
		local cell = cells[i]
		if cell.state == "Wall" then
			return
		end
		local m = cell.material
		if m == "Water" or m == "Rock" or m == "Tree" then
			return
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
			forNeighbors(i, ignite)
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
			forNeighbors(i, function(nIdx)
				if isFlammable(nIdx) and rng:NextNumber() < T.propagateChance then
					ignite(nIdx)
				end
			end)
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
-- ============================================================================
--- cota em que o personagem PISA nesta célula
local function standY(cx: number, cy: number): number
	local cell = cells[idxOf(cx, cy)]
	return if cell.material == "Water" then waterY(cx, cy) else FLOOR_Y
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

	local parts = if folder then #folder:GetDescendants() else 0
	assert(parts <= 4000, "orçamento de parts do GDD §19.2 estourado: " .. parts)
	print(string.format("[Terrain] autoteste OK — %d parts · lago %d (%d de margem) · 0 armadilhas", parts, water, shore))
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
