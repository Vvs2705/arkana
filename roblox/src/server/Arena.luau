--!strict
-- RAIA A (Arena/Terreno) — LAYOUT da arena: decide QUAL material vai em cada
-- célula (lago, floresta, grama alta, rochas, campo aberto, estrada — GDD §14).
-- Materiais são IMUTÁVEIS; quem muda estado é o Terrain. Level design vive aqui.
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local Grid = require(ReplicatedStorage:WaitForChild("Shared"):WaitForChild("Grid"))

local Arena = {}

export type Layout = { { Grid.Material } } -- [cy][cx]

--- Gera a grade de materiais. Determinístico (mesma arena toda partida).
function Arena.generate(): Layout
	local layout: Layout = {}
	for cy = 0, Grid.ROWS - 1 do
		local row = {}
		for cx = 0, Grid.COLS - 1 do
			row[cx] = "Grass" :: Grid.Material
		end
		layout[cy] = row
	end
	return layout
end

return Arena
