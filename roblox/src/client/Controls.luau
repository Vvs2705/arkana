--!strict
-- RAIA C (Controles/Câmera) — input de PC e de TOQUE.
--
-- REQUISITO Nº 1, vindo do teste do Diretor em aparelho real (18/08):
-- MIRAR E ATIRAR SÃO UM ÚNICO GESTO. Arrastar a partir do botão da magia mira
-- e dispara ao soltar; toque curto (< Balance.controls.tapMaxDuration) dispara
-- na direção da câmera. Dois gestos separados foi reprovado — não repetir.
--
-- REQUISITO Nº 2: a troca de elemento precisa responder ao toque (o carrossel
-- do protótipo não respondia). Alvos ≥ 48dp, feedback visual imediato.
--
-- O cliente só ENVIA INTENÇÃO (Net.CastSpell/SwitchElement/Dodge).
local Controls = {}

function Controls.init(_player: Player) end
function Controls.update(_dt: number) end

--- Elemento ativo (o HUD lê para destacar o botão correspondente).
function Controls.getElement(): string
	return "Fire"
end

return Controls
