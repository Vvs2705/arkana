--!strict
-- RAIA B (Combate) — magias, projéteis com tempo de viagem (GDD §4.1: NADA de
-- hitscan), mana, cooldowns, dano, esquiva com i-frames e Escudo Evolutivo
-- (GDD §5). AUTORIDADE: valida toda intenção do cliente antes de agir.
local Combat = {}

--- Recebe o módulo Terrain (raia A) para aplicar química no impacto.
function Combat.init(_terrain: any) end

function Combat.onPlayerAdded(_player: Player) end
function Combat.onPlayerRemoving(_player: Player) end
function Combat.onCharacterAdded(_player: Player, _character: Model, _humanoid: Humanoid) end

--- Intenção validada pelo Main: conjurar básico (M1) ou tático (M2).
function Combat.handleCast(_player: Player, _kind: string, _dir: Vector3) end

--- Troca de elemento (Q / carrossel de toque). Valida o id.
function Combat.handleSwitch(_player: Player, _elementId: any) end

--- Esquiva com i-frames curtos (GDD §15 item 5).
function Combat.handleDodge(_player: Player, _dir: Vector3?) end

--- Tick: move projéteis, aplica dano de terreno, regenera mana, expira i-frames.
function Combat.step(_dt: number) end

--- Dano roteado (usado por Combat, Sintonia e Match/zona).
function Combat.applyDamage(_targetHumanoid: Humanoid, _amount: number, _fromPlayer: Player?) end

--- Elemento ativo do jogador (a Sintonia precisa saber o que ele conjurou).
function Combat.getElement(_player: Player): string?
	return nil
end

return Combat
