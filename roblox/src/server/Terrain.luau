--!strict
-- RAIA A (Arena/Terreno) — TERRENO REATIVO: constrói o mundo em blocos e roda
-- a máquina de estados da química (GDD §14 + as 3 regras de BotW, GDD §16.1).
-- Este módulo é a AUTORIDADE do estado do mapa. Só o servidor o executa.
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local Shared = ReplicatedStorage:WaitForChild("Shared")
local Grid = require(Shared:WaitForChild("Grid"))
local Arena = nil :: any -- tipo do layout vem por parâmetro

local Terrain = {}

--- Constrói as partes do mundo a partir do layout (chamado 1x no boot).
function Terrain.build(_layout: any) end

--- Tick da simulação: propagação de fogo, expiração de estados, dano por tick.
function Terrain.step(_dt: number) end

--- Célula lógica (ou nil fora dos limites).
function Terrain.getCell(_cx: number, _cy: number): Grid.Cell?
	return nil
end

--- Bloqueia movimento? (água só é atravessável quando congelada — GDD §14)
function Terrain.isBlocking(_cx: number, _cy: number): boolean
	return false
end

--- ENTRADA DA QUÍMICA: uma magia do elemento `element` atinge a área.
--- `strong` = magia tática (efeitos maiores: muro de pedra, congelar lago...).
function Terrain.applyElement(_cx: number, _cy: number, _element: string, _radiusCells: number, _strong: boolean) end

--- Multiplicador de velocidade no ponto (lama desacelera, grama alta um pouco).
function Terrain.speedMultAt(_pos: Vector3): number
	return 1
end

--- Dano em muro/árvore atingidos por projétil.
function Terrain.damageWall(_cx: number, _cy: number, _dmg: number) end

return Terrain
