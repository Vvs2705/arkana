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
-- PASSE DE IDENTIDADE (18/08, auditoria P2: "o jogo fica clicável em thumbnail
-- e legível em luta?"). O mapa funcionava mas era GENÉRICO — nenhum ponto
-- reconhecível de longe. Agora são QUATRO macro-POIs, um por quadrante, cada um
-- amarrado a um elemento e com um MARCO VERTICAL (`Arena.MARKS`) que orienta
-- sem minimapa e dá assunto à screenshot:
--   · NE  LAGO / TEMPLO INUNDADO .. Água congela e vira rota; o Raio conduz;
--         a torre e as 4 colunas são PÍERES de rocha DENTRO do lago — em cima
--         do gelo viram cobertura. Margens secas dos dois lados = sem counter
--         absoluto.                      Marco: torre do templo (~47 studs)
--   · SW  FLORESTA RITUAL ......... cobertura alta e CONSUMÍVEL, cortada pela
--         trilha-eixo e por clareiras (aceiro). Marco: Árvore-Mãe (~45 studs)
--   · NW  RUÍNAS MINERAIS ......... praça de laje + reticulado de muros: salas
--         e corredores onde a Terra ergue e rompe cobertura.
--                                       Marco: obelisco rachado (~39 studs)
--   · SE  BAIXADA / ALAGADO ....... bacia de lodo (Água aqui = lamaçal, §14)
--         com canal em anel que o Raio eletriza; a estrada atravessa SECA e é
--         a rota de fuga.               Marco: menir tombado (~24 studs)
-- A estrada diagonal costura RUÍNAS ↔ centro ↔ BAIXADA. Vista de cima: quatro
-- massas de cor e textura distintas em volta do campo aberto — a thumbnail tem
-- assunto em qualquer ângulo de câmera.
--
-- ACHADO QUE ESTE PASSE CONSERTA: a clareira (14,44) nascia CERCADA de tronco;
-- ninguém chegava nela andando e o passo 8.5 a convertia de volta em árvore.
-- No mapa CONSTRUÍDO a floresta era um bloco maciço de 380 células, sem aceiro
-- e sem entrada — o pilar "clareiras impedem incêndio infinito" não existia.
-- A trilha-eixo liga as clareiras aos dois lados e devolve o aceiro ao mapa.
--
-- NÚMEROS DE LEVEL DESIGN (medidos no preview ASCII, grade 60×60):
--   · LAGO NE ..... 200 células de água + 5 píeres de marco → congelar segue
--                   criando a rota real (a lâmina encosta rente aos píeres)
--   · FLORESTA SW . ~330 árvores em DUAS manchas (~180 cada, split da trilha).
--                   Densidade do núcleo (0.90) segue acima do limiar de
--                   percolação de sítio em vizinhança-4 (~0.593); abaixo dele o
--                   incêndio morre em bolsões e o pilar do GDD §14 não acontece.
--   · RUÍNAS NW ... ~50 células de muro em salas de ~4×4, portas FIXAS
--   · BAIXADA SE .. ~300 células de lodo, canal de ~30 células de água RASA
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
local SALT_RUIN = 71
local SALT_MOSS = 103
local SALT_REED = 97

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
-- A do coração da floresta cresceu (2.6 → 3.4) para virar o ADRO da Árvore-Mãe:
-- o marco ocupa 2×2 células no centro e tem que sobrar volta pelos dois lados.
local CLEARINGS = {
	{ cx = 14, cy = 44, r = 3.4 }, -- adro da Árvore-Mãe (coração da floresta)
	{ cx = 15, cy = 51, r = 2.6 }, -- segundo aceiro, no sul da floresta
	{ cx = 40, cy = 40, r = 3.0 }, -- boca noroeste da baixada
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

-- TRILHA-EIXO da floresta ritual: 2 células de largura, norte→sul, de ponta a
-- ponta da mata. Existe por três motivos, nesta ordem:
--   1. sem ela a clareira central fica inalcançável e o passo 8.5 a APAGA
--      (era o bug: floresta maciça de 380 células sem entrada);
--   2. mata maciça não é cobertura, é parede — trilha dá entrada e flanco;
--   3. corta a mancha combustível em DUAS (~180 cada), ambas ainda muito acima
--      do limiar de percolação: o incêndio percola, mas agora tem onde parar.
local TRAIL_X0, TRAIL_X1 = 14, 15
local TRAIL_Y0, TRAIL_Y1 = 33, 56

-- RUÍNAS MINERAIS (noroeste) — praça de laje com reticulado de muros de rocha.
-- O vão de porta no MEIO de cada tramo é FIXO (nunca sorteado): toda sala é
-- alcançável POR CONSTRUÇÃO, então o passo 8.5 não tem bolsão para selar aqui.
-- A erosão do hash só ABRE mais buraco — nunca fecha um.
local RUIN_X0, RUIN_X1 = 3, 22
local RUIN_Y0, RUIN_Y1 = 2, 13
local RUIN_PITCH = 5
local RUIN_DOOR = 2 -- porta = células com resto RUIN_DOOR e RUIN_DOOR+1 no tramo
local RUIN_EROSION = 0.30
local RUIN_MOSS = 0.22 -- fração da laje que o mato retomou (quebra a chapa)

-- BAIXADA / ALAGADO (sudeste) — bacia de lodo com canal de água em ANEL.
-- O canal nunca invade a faixa da estrada: o calçamento atravessa a bacia SECO
-- por construção — rota de fuga do POI e garantia de que a ilha central (com o
-- menir) continua alcançável a pé.
local MARSH_CX, MARSH_CY = 46, 46
local MARSH_RX, MARSH_RY = 10.5, 9.5
--- canal estreito de propósito (~1,5 célula de largura radial): TODA célula de
--- água dele faz fronteira com terra e é RASA (`isShoreWater` do Terrain) —
--- ninguém cai num poço fundo do qual não sai andando.
local MARSH_CHANNEL0, MARSH_CHANNEL1 = 0.36, 0.50
local MARSH_REED0, MARSH_REED1 = 0.58, 0.97
--- 0.42 < limiar de percolação (~0.593): fogo em junco morre em bolsões — na
--- baixada isso é FEATURE (o incêndio infinito não pertence a este POI)
local MARSH_REED_DENSITY = 0.42

-- CAMPO ABERTO central: bloco garantidamente sem bloqueio (spawn e combate).
local OPEN_MIN, OPEN_MAX = 25, 34

local COLS, ROWS = Grid.COLS, Grid.ROWS

-- ------------------------------------------------------------------- marcos
--- MARCOS VERTICAIS — um por POI. São o que se enxerga do outro lado do mapa e
--- o que faz a screenshot ter assunto. CONTRATO: toda célula de pegada vira
--- ROCHA no layout. Rocha já bloqueia andar e tiro, não queima e não aceita
--- muro de Terra — o marco entra no jogo com regras que JÁ existem, sem
--- material novo e sem caso especial na química (GDD §14).
--- Por que rocha até na Árvore-Mãe: marco que o fogo derruba é referência que
--- some no meio da partida. A mata em volta continua queimando normalmente.
--- `w` = lado do quadrado de células da pegada (padrão 1).
export type Mark = { kind: string, cx: number, cy: number, w: number? }

Arena.MARKS = {
	-- NE — TEMPLO INUNDADO: torre no miolo do lago, 4 colunas em retângulo.
	-- Congelou, a lâmina de gelo encosta rente nos píeres: viram cobertura.
	{ kind = "TempleTower", cx = 37, cy = 15 },
	{ kind = "TempleColumn", cx = 34, cy = 13 },
	{ kind = "TempleColumn", cx = 34, cy = 17 },
	{ kind = "TempleColumn", cx = 40, cy = 13 },
	{ kind = "TempleColumn", cx = 40, cy = 17 },
	-- SW — ÁRVORE-MÃE no adro da clareira, pegada 2×2 (tronco de 6 studs: de
	-- longe lê como tronco-monumento, não como poste). A trilha desemboca nela
	-- e contorna pelo adro.
	{ kind = "MotherTree", cx = 14, cy = 44, w = 2 },
	-- NW — OBELISCO RACHADO, no miolo de uma sala das ruínas (fora das linhas
	-- de muro: 11 e 8 não são múltiplos do passo 5 a partir de 3/2)
	{ kind = "Obelisk", cx = 11, cy = 8 },
	-- SE — MENIR TOMBADO na ilha do canal; a laje caída aponta para leste e
	-- morre DENTRO do canal (ponte improvisada de 1.9 studs — sobe andando)
	{ kind = "Menhir", cx = 48, cy = 45 },
} :: { Mark }

--- Percorre as células da pegada de um marco. Fonte ÚNICA da pegada: o layout
--- carimba rocha por aqui e o Terrain pula a rocha padrão por aqui — as duas
--- contas não têm como divergir.
function Arena.forMarkCells(m: Mark, fn: (cx: number, cy: number) -> ())
	local w = m.w or 1
	for dy = 0, w - 1 do
		for dx = 0, w - 1 do
			fn(m.cx + dx, m.cy + dy)
		end
	end
end

-- ------------------------------------------------------------------ helpers
--- dentro da grade e fora do anel de borda
local function inner(cx: number, cy: number): boolean
	return cx > 0 and cy > 0 and cx < COLS - 1 and cy < ROWS - 1
end

--- faixa da estrada diagonal (3 células de largura). Ruínas e baixada NUNCA
--- escrevem nela: é o que mantém a avenida e o calçamento abertos sem depender
--- de sorteio nenhum.
local function onRoad(cx: number, cy: number): boolean
	return math.abs(cx - cy) <= 1
end

--- distância normalizada ao centro da baixada (1 = borda da elipse)
local function marshD(cx: number, cy: number): number
	local nx = (cx - MARSH_CX) / MARSH_RX
	local ny = (cy - MARSH_CY) / MARSH_RY
	return math.sqrt(nx * nx + ny * ny)
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

	-- 4.5 TRILHA-EIXO da floresta (ver constantes: sem ela a clareira central
	-- era inalcançável e o passo 8.5 a apagava — a floresta virava um paredão)
	for cy = TRAIL_Y0, TRAIL_Y1 do
		for cx = TRAIL_X0, TRAIL_X1 do
			if inner(cx, cy) then
				local m = layout[cy][cx]
				if m == "Tree" or m == "Grass" then
					layout[cy][cx] = "Dirt"
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

	-- 7.3 RUÍNAS MINERAIS (NW). Primeiro a praça APAGA o que havia — inclusive
	-- aglomerados soltos do passo 7: rocha aleatória encostada em muro é como
	-- nasce bolsão. Depois os muros, com porta FIXA no meio de cada tramo.
	for cy = RUIN_Y0, RUIN_Y1 do
		for cx = RUIN_X0, RUIN_X1 do
			if inner(cx, cy) and not onRoad(cx, cy) then
				local mx = (cx - RUIN_X0) % RUIN_PITCH
				local my = (cy - RUIN_Y0) % RUIN_PITCH
				local wall = false
				if mx == 0 and my == 0 then
					wall = true -- pilar de canto: âncora da silhueta, nunca erode
				elseif mx == 0 then
					wall = not (my == RUIN_DOOR or my == RUIN_DOOR + 1)
						and hash01(cx, cy, SALT_RUIN) >= RUIN_EROSION
				elseif my == 0 then
					wall = not (mx == RUIN_DOOR or mx == RUIN_DOOR + 1)
						and hash01(cx, cy, SALT_RUIN) >= RUIN_EROSION
				end
				if wall then
					layout[cy][cx] = "Rock"
				else
					-- laje da praça, com o mato retomando parte (quebra a chapa).
					-- `Slab` e não `Road`: mesmas regras, cor própria (R9) — a
					-- praça deixou de se pintar com a cor da estrada.
					layout[cy][cx] = if hash01(cx, cy, SALT_MOSS) < RUIN_MOSS
						then "Grass"
						else "Slab"
				end
			end
		end
	end

	-- 7.6 BAIXADA / ALAGADO (SE): bacia de lodo + canal em anel + juncos.
	-- Sobrescreve rocha solta de propósito (rocha + canal = risco de bolsão);
	-- a cobertura deste POI são os juncos, o canal e o menir.
	for cy = 0, ROWS - 1 do
		for cx = 0, COLS - 1 do
			if inner(cx, cy) and not onRoad(cx, cy) then
				local d = marshD(cx, cy)
				if d <= 1 then
					local m = layout[cy][cx]
					if m == "Grass" or m == "TallGrass" or m == "Rock" or m == "Dirt" then
						if d >= MARSH_CHANNEL0 and d <= MARSH_CHANNEL1 then
							layout[cy][cx] = "Water" -- canal raso: toda célula faz margem
						elseif
							d >= MARSH_REED0
							and d <= MARSH_REED1
							and hash01(cx, cy, SALT_REED) < MARSH_REED_DENSITY
						then
							layout[cy][cx] = "TallGrass" -- juncos: emboscada no lodo
						else
							-- lodo: Água aqui vira lamaçal (§14). `Silt` e não
							-- `Dirt`: mesmas regras (o lamaçal continua valendo,
							-- ver Terrain.applyWater), cor própria (R9).
							layout[cy][cx] = "Silt"
						end
					end
				end
			end
		end
	end

	-- 7.9 MARCOS VERTICAIS: a pegada vira ROCHA (regras prontas — bloqueia
	-- andar e tiro, não queima, não aceita muro; §14 intocado). A silhueta em
	-- si é do Terrain. No lago a rocha vira PÍER e as águas vizinhas passam a
	-- fazer margem (rasas): dá para subir nelas andando.
	for _, m in ipairs(Arena.MARKS) do
		Arena.forMarkCells(m, function(cx, cy)
			if inner(cx, cy) then
				layout[cy][cx] = "Rock"
			end
		end)
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
