## Barramento de sinais do jogo — o "Net/Events" desta frente.
## Dono: COORDENADOR. Raia nova precisa de sinal novo? Pede, nao cria local.
## REGRA HERDADA DO PROJETO: telemetria/UI OBSERVAM por aqui e nunca decidem
## jogo. Quem aplica dano e' um lugar so' (gameplay/Combat.gd).
extends Node

signal damage_dealt(target: Node, amount: int, element: String)
signal entity_died(entity: Node)
signal match_started
signal match_over(victory: bool)
signal mana_changed(current: float, max: float)
signal health_changed(current: float, max: float)
signal element_changed(element: String)
signal game_start_requested  # o menu pede a partida; quem troca de cena e' o Main
