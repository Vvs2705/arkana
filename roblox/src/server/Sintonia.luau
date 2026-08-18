--!strict
-- RAIA (R2) — CONJURAÇÃO COMBINADA "SINTONIA": o pilar de inovação (GDD §9).
-- Dois magos conjurando no mesmo alvo dentro de 1,5s fundem as magias numa
-- Magia Combinada (matriz de 10 pares em Shared/Elements.Combos).
-- Limitadores obrigatórios do GDD §9: consome a magia dos DOIS, canalização
-- visível/sonora de ~1s (interrompível) e cooldown compartilhado longo.
local Sintonia = {}

function Sintonia.init(_terrain: any, _combat: any) end

--- Chamado pelo Combat a cada conjuração que atinge algo: alimenta a janela.
function Sintonia.registerCast(_player: Player, _elementId: string, _pos: Vector3) end

--- Ping de Sintonia (GDD §18.7): propõe combo a quem estiver por perto.
function Sintonia.propose(_player: Player, _pos: any) end
function Sintonia.accept(_player: Player, _fromUserId: any) end

function Sintonia.step(_dt: number) end
function Sintonia.onPlayerRemoving(_player: Player) end

return Sintonia
