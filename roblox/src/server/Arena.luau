--!strict
-- ============================================================================
-- RAIA A (Arena/Terreno) — LAYOUT da arena: decide QUAL material vai em cada
-- célula (lago, floresta, grama alta, rochas, campo aberto, estrada — GDD §14).
-- Materiais são IMUTÁVEIS; quem muda estado é o Terrain. Level design vive aqui.
--
-- DETERMINISMO POR CONSTRUÇÃO: nenhum `math.random` neste arquivo. Toda a
-- variação vem de um hash puro por célula (xorshift32) — mesma arena em toda
-- máquina, toda partida, sem semente global. Porte de src/terrain/layout.ts.
--
-- NÚMEROS DE LEVEL DESIGN (medidos antes de codar, grade 60×60):
--   · LAGO NE ..... 205 células, TODAS contíguas → congelar cria uma rota real
--   · FLORESTA SW . 282 árvores, maior mancha contígua = 238 (84%) → o fogo
--                   percola de verdade. A densidade do núcleo (0.90) fica acima
--                   do limiar de percolação de sítio em vizinhança-4 (~0.593);
--                   abaixo dele o incêndio morre em bolsões e o pilar do GDD §14
--                   ("queimar a floresta é uma jogada") não acontece.
--   · GRAMA ALTA .. 176 células em 5 manchas · ROCHAS 51 + paredão de borda
--   · CAMPO ABERTO  bloco central 10×10 garantidamente livre (combate/spawn)
-- ============================================================================
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local Grid = require(ReplicatedStorage:WaitForChild("Shared"):WaitForChild("Grid"))

local Arena = {}

export type Layout = { { Grid.Material } } -- [cy][cx]

-- ------------------------------------------------------------------- ruído
-- Sais distintos por camada — evita correlação visual entre as features.
local SALT_FOREST = 11
local SALT_PATCH = 23
local SALT_ROCK = 37
local SALT_ROCK_SIZE = 41

--- Hash inteiro determinístico por célula. Só produtos < 2^53 (exatos em
--- double) e operações de `bit32` — nada de multiplicação de 32×32 bits, que
--- perderia os bits baixos e arruinaria a distribuição.
local function hash32(x: number, y: number, salt: number): number
	local h = (x + 1) * 73856093 % 4294967296
	h = bit32.bxor(h, (y + 1) * 19349663 % 4294967296)
	h = bit32.bxor(h, (salt + 1) * 83492791 % 4294967296)
	-- xorshift32 clássico: avalanche boa o bastante para decoração de terreno
	h = bit32.bxor(h, bit32.lshift(h, 13))
	h = bit32.bxor(h, bit32.rshift(h, 17))
	h = bit32.bxor(h, bit32.lshift(h, 5))
	return h
end

--- hash normalizado em [0, 1)
local function hash01(x: number, y: number, salt: number): number
	return hash32(x, y, salt) / 4294967296
end

--- Exposto para o Terrain variar COR e TAMANHO por célula sem inventar um
--- segundo gerador: uma fonte de aleatoriedade só = mesmo mapa em toda máquina.
Arena.hash01 = hash01

-- --------------------------------------------------------- features da arena
-- LAGO (nordeste) — elipse + praia de terra em volta (a terra é onde a Água
-- vira lamaçal, GDD §14).
local LAKE_CX, LAKE_CY = 42, 15
local LAKE_RX, LAKE_RY = 9.5, 7.0
local SHORE_PAD_X, SHORE_PAD_Y = 2.4, 2.2

-- FLORESTA (sudoeste) — caixa com densidade que cai do núcleo para a borda.
local FOREST_X0, FOREST_X1 = 4, 25
local FOREST_Y0, FOREST_Y1 = 34, 54
local FOREST_CORE_DENSITY = 0.90
local FOREST_FALLOFF = 0.30

-- CLAREIRAS de terra: aceiros que barram o incêndio e dão respiro tático.
local CLEARINGS = {
	{ cx = 14, cy = 44, r = 2.6 }, -- dentro da floresta
	{ cx = 40, cy = 40, r = 3.0 }, -- a leste do campo central
}

-- MANCHAS de grama alta (emboscada — e o fogo REVELA quem se esconde, GDD §14).
local TALL_GRASS_PATCHES = {
	{ cx = 9, cy = 17 },
	{ cx = 24, cy = 19 },
	{ cx = 47, cy = 42 },
	{ cx = 34, cy = 49 },
	{ cx = 15, cy = 29 },
}

-- AGLOMERADOS de rocha: cobertura indestrutível espalhada pelo campo aberto.
local ROCK_CLUSTER_OFFSETS = { { 0, 0 }, { 1, 0 }, { 0, 1 }, { -1, 0 }, { 1, 1 } }
--- 1 semente a cada ~131 células elegíveis (densidade escolhida no dimensionamento)
local ROCK_RARITY = 131

-- CAMPO ABERTO central: bloco garantidamente sem bloqueio (spawn e combate).
local OPEN_MIN, OPEN_MAX = 25, 34

local COLS, ROWS = Grid.COLS, Grid.ROWS

--- dentro da grade e fora do anel de borda
local function inner(cx: number, cy: number): boolean
	return cx > 0 and cy > 0 and cx < COLS - 1 and cy < ROWS - 1
end

-- ------------------------------------------------------------------ geração
--- Gera a grade de materiais. Determinístico (mesma arena toda partida).
function Arena.generate(): Layout
	local layout: Layout = {}

	-- 1. base: campo de grama
	for cy = 0, ROWS - 1 do
		local row = {}
		for cx = 0, COLS - 1 do
			row[cx] = "Grass" :: Grid.Material
		end
		layout[cy] = row
	end

	-- 2. estrada diagonal NW→SE, cortando o campo central (rota de rotação)
	for cy = 0, ROWS - 1 do
		for cx = 0, COLS - 1 do
			if math.abs(cx - cy) <= 1 then
				layout[cy][cx] = "Road"
			end
		end
	end

	-- 3. LAGO + praia de terra
	for cy = 0, ROWS - 1 do
		for cx = 0, COLS - 1 do
			local nx = (cx - LAKE_CX) / LAKE_RX
			local ny = (cy - LAKE_CY) / LAKE_RY
			if nx * nx + ny * ny <= 1 then
				layout[cy][cx] = "Water"
			else
				local mx = (cx - LAKE_CX) / (LAKE_RX + SHORE_PAD_X)
				local my = (cy - LAKE_CY) / (LAKE_RY + SHORE_PAD_Y)
				if mx * mx + my * my <= 1 then
					layout[cy][cx] = "Dirt"
				end
			end
		end
	end

	-- 4. FLORESTA densa: densidade cai do núcleo (0.90) para a borda (0.60)
	local fcx = (FOREST_X0 + FOREST_X1) / 2
	local fcy = (FOREST_Y0 + FOREST_Y1) / 2
	local frx = (FOREST_X1 - FOREST_X0) / 2
	local fry = (FOREST_Y1 - FOREST_Y0) / 2
	for cy = FOREST_Y0, FOREST_Y1 do
		for cx = FOREST_X0, FOREST_X1 do
			if layout[cy][cx] == "Grass" then
				local nx = (cx - fcx) / frx
				local ny = (cy - fcy) / fry
				local d = math.sqrt(nx * nx + ny * ny)
				local density = FOREST_CORE_DENSITY - FOREST_FALLOFF * d
				if hash01(cx, cy, SALT_FOREST) < density then
					layout[cy][cx] = "Tree"
				end
			end
		end
	end

	-- 5. clareiras de terra (o fogo não atravessa chão nu)
	for _, c in ipairs(CLEARINGS) do
		local ri = math.ceil(c.r)
		for dy = -ri, ri do
			for dx = -ri, ri do
				if dx * dx + dy * dy <= c.r * c.r then
					local cx, cy = c.cx + dx, c.cy + dy
					if inner(cx, cy) and layout[cy][cx] ~= "Water" then
						layout[cy][cx] = "Dirt"
					end
				end
			end
		end
	end

	-- 6. manchas de GRAMA ALTA (7×6 com cantos irregulares determinísticos)
	for _, p in ipairs(TALL_GRASS_PATCHES) do
		for dy = -3, 2 do
			for dx = -3, 3 do
				local cx, cy = p.cx + dx, p.cy + dy
				local ragged = math.abs(dx) >= 2
					and math.abs(dy) >= 2
					and hash01(cx, cy, SALT_PATCH) < 0.5
				if inner(cx, cy) and not ragged and layout[cy][cx] == "Grass" then
					layout[cy][cx] = "TallGrass"
				end
			end
		end
	end

	-- 7. ROCHAS em aglomerados de 2–5 células — longe da estrada, do campo
	--    central e de dentro da floresta (não selar corredores nem o incêndio)
	local centerX, centerY = (COLS - 1) / 2, (ROWS - 1) / 2
	for cy = 3, ROWS - 4 do
		for cx = 3, COLS - 4 do
			local eligible = layout[cy][cx] == "Grass"
				and math.abs(cx - cy) > 3
				and math.max(math.abs(cx - centerX), math.abs(cy - centerY)) > 7
				and not (cx >= FOREST_X0 and cx <= FOREST_X1 and cy >= FOREST_Y0 and cy <= FOREST_Y1)
			if eligible and hash32(cx, cy, SALT_ROCK) % ROCK_RARITY == 0 then
				local size = 2 + hash32(cx, cy, SALT_ROCK_SIZE) % 4
				for i = 1, size do
					local off = ROCK_CLUSTER_OFFSETS[i]
					local rx, ry = cx + off[1], cy + off[2]
					if inner(rx, ry) and layout[ry][rx] == "Grass" then
						layout[ry][rx] = "Rock"
					end
				end
			end
		end
	end

	-- 8. CAMPO ABERTO central: garantia dura (sobrescreve qualquer feature)
	for cy = OPEN_MIN, OPEN_MAX do
		for cx = OPEN_MIN, OPEN_MAX do
			local m = layout[cy][cx]
			if m == "Tree" or m == "Rock" or m == "Water" then
				layout[cy][cx] = "Grass"
			end
		end
	end

	-- 8.5 SELA OS BOLSÕES SEM SAÍDA. O ruído da floresta deixa buracos de chão
	-- totalmente cercados por tronco. Ninguém CHEGA neles andando — mas o Match
	-- escolhe spawn por "célula não bloqueia", não por alcançabilidade, e quem
	-- nascesse ali passaria a partida preso. Viram árvore: somem do mapa andável
	-- e ainda engrossam a contiguidade de que o incêndio precisa.
	-- Vizinhança-4 de propósito (conservador): não aposta que o personagem
	-- consiga se espremer na diagonal entre dois troncos.
	local blocked = { Tree = true, Rock = true, Water = true }
	local seen: { [number]: boolean } = {}
	local queue: { number } = { 30 * COLS + 30 } -- campo aberto central, sempre livre
	seen[queue[1]] = true
	local head = 1
	while head <= #queue do
		local k = queue[head]
		head += 1
		local cx = k % COLS
		local cy = (k - cx) / COLS
		for _, d in ipairs(Grid.NEIGHBORS4) do
			local x, y = cx + d[1], cy + d[2]
			local nk = y * COLS + x
			if inner(x, y) and not seen[nk] and not blocked[layout[y][x]] then
				seen[nk] = true
				table.insert(queue, nk)
			end
		end
	end
	for cy = 1, ROWS - 2 do
		for cx = 1, COLS - 2 do
			if not blocked[layout[cy][cx]] and not seen[cy * COLS + cx] then
				layout[cy][cx] = "Tree"
			end
		end
	end

	-- 9. paredão natural: anel de rocha na borda (por último, sem exceção)
	for cx = 0, COLS - 1 do
		layout[0][cx] = "Rock"
		layout[ROWS - 1][cx] = "Rock"
	end
	for cy = 0, ROWS - 1 do
		layout[cy][0] = "Rock"
		layout[cy][COLS - 1] = "Rock"
	end

	return layout
end

return Arena
