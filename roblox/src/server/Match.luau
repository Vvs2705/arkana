--!strict
-- RAIA (R3) — LOOP DE BATTLE ROYALE: lobby, início, zona arcana fechando,
-- eliminações, bots de preenchimento (GDD §19.6) e Selo do Campeão (GDD §18.6).
-- Escopo do GDD §19.2: 20 jogadores, partidas curtas.
local Match = {}

function Match.init(_terrain: any, _combat: any) end
function Match.step(_dt: number) end
function Match.onPlayerRemoving(_player: Player) end

return Match
