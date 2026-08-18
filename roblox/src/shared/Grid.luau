--!strict
-- ============================================================================
-- ARKANA (Roblox) — GRID DO TERRENO REATIVO (contrato compartilhado)
-- Porte da grade lógica do protótipo 2D (src/terrain/) para 3D em blocos.
-- Fonte: GDD §14 (a tabela de ações/resultados/contra-jogadas é LEI).
--
-- Arquitetura herdada das 3 regras do chemistry engine de BotW (GDD §16.1):
--   1. elementos mudam o ESTADO de materiais;
--   2. elementos mudam o estado de outros ELEMENTOS (água apaga fogo);
--   3. materiais não mudam materiais.
-- O MATERIAL de uma célula é imutável; o ESTADO é o que a magia altera.
-- Dono: COORDENADOR.
-- ============================================================================

local Grid = {}

--- lado da célula em studs (personagem Roblox ≈ 2 studs de largura)
Grid.CELL_SIZE = 4
Grid.COLS = 60
Grid.ROWS = 60
--- altura do piso: o mundo fica em y=0 e os blocos crescem para cima
Grid.FLOOR_Y = 0

Grid.WIDTH_STUDS = Grid.COLS * Grid.CELL_SIZE
Grid.DEPTH_STUDS = Grid.ROWS * Grid.CELL_SIZE

-- Materiais (imutáveis por célula) — espelham o protótipo 2D
export type Material = "Grass" | "TallGrass" | "Tree" | "Water" | "Rock" | "Dirt" | "Road"

-- Estados (mutáveis — é aqui que a magia age)
export type State = "Normal" | "Burning" | "Burned" | "Frozen" | "Mud" | "Electrified" | "Wall"

export type Cell = {
	cx: number,
	cy: number,
	material: Material,
	state: State,
	--- instante (os.clock) em que o estado atual expira; 0 = não expira
	stateUntil: number,
	--- vida do muro erguido por Terra (GDD §14); 0 quando não há muro
	wallHp: number,
	--- parte visual que representa a célula (criada pelo servidor, replica sozinha)
	part: BasePart?,
}

--- materiais que o fogo consome (LIÇÃO DO 2D: campo aberto NÃO queima —
--- senão o incêndio carboniza o mapa inteiro; ver docs/HISTORICO.md)
Grid.FLAMMABLE = {
	Tree = true,
	TallGrass = true,
	Grass = false,
	Water = false,
	Rock = false,
	Dirt = false,
	Road = false,
} :: { [Material]: boolean }

--- materiais que bloqueiam movimento no estado normal
Grid.BLOCKING = {
	Tree = true,
	Rock = true,
	Water = true, -- água só é atravessável quando Frozen (vira ponte — GDD §14)
	Grass = false,
	TallGrass = false,
	Dirt = false,
	Road = false,
} :: { [Material]: boolean }

--- paleta blocky por material (GDD §10 + direção Pixel Gun: cores chapadas)
Grid.COLORS = {
	Grass = Color3.fromHex("#3E7A3A"),
	TallGrass = Color3.fromHex("#2F6330"),
	Tree = Color3.fromHex("#255024"),
	Water = Color3.fromHex("#2A6BAA"),
	Rock = Color3.fromHex("#6E6E78"),
	Dirt = Color3.fromHex("#7A5A38"),
	Road = Color3.fromHex("#8A7A62"),
} :: { [Material]: Color3 }

--- cor por estado (sobrepõe a do material quando o estado não é Normal)
Grid.STATE_COLORS = {
	Burning = Color3.fromHex("#FF5A2A"),
	Burned = Color3.fromHex("#2A2725"),
	Frozen = Color3.fromHex("#BFE8FF"),
	Mud = Color3.fromHex("#4A3A24"),
	Electrified = Color3.fromHex("#F5D90A"),
	Wall = Color3.fromHex("#6E6E78"),
} :: { [State]: Color3 }

--- altura do bloco por material (o relevo que dá volume ao mundo blocky)
Grid.HEIGHTS = {
	Grass = 1,
	--- 3 studs: alto o bastante para ESCONDER um personagem (~5 studs) — sem
	--- isso a contra-jogada do GDD §14 ("Fogo em grama alta REVELA quem estava
	--- escondido") não tem o que revelar. Achado da raia A na R1.
	TallGrass = 3,
	Tree = 12,
	Water = 1,
	Rock = 8,
	Dirt = 1,
	Road = 1,
} :: { [Material]: number }

Grid.WALL_HEIGHT = 10

-- ---------------------------------------------------------------- conversões
--- centro da célula no mundo. A arena é centrada na origem.
function Grid.cellToWorld(cx: number, cy: number): Vector3
	local x = (cx - Grid.COLS / 2 + 0.5) * Grid.CELL_SIZE
	local z = (cy - Grid.ROWS / 2 + 0.5) * Grid.CELL_SIZE
	return Vector3.new(x, Grid.FLOOR_Y, z)
end

function Grid.worldToCell(pos: Vector3): (number, number)
	local cx = math.floor(pos.X / Grid.CELL_SIZE + Grid.COLS / 2)
	local cy = math.floor(pos.Z / Grid.CELL_SIZE + Grid.ROWS / 2)
	return cx, cy
end

function Grid.inBounds(cx: number, cy: number): boolean
	return cx >= 0 and cy >= 0 and cx < Grid.COLS and cy < Grid.ROWS
end

--- vizinhança-4 (a propagação de fogo do protótipo usa esta vizinhança)
Grid.NEIGHBORS4 = { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } }

return Grid
