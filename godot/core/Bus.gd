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
## A COSTURA DO TERRENO REATIVO (R19): o gameplay EMITE o impacto; o terreno
## REAGE. E' o mesmo contrato que segurou 8 fases no projeto-mae — quem muda o
## mundo nunca aplica dano, e quem aplica dano nunca muda o mundo.
signal terrain_hit(element: String, pos: Vector3, strong: bool)
signal terrain_changed(kind: String, pos: Vector3)  # p/ audio/UI observarem
signal player_killed_bot(bot_name: String)          # p/ kill feed e audio
signal dodge_performed
## O som do disparo toca no INSTANTE do cast (o impacto ja' tem o proprio som
## via terrain_hit/damage_dealt). So' o PLAYER emite por ora — 6 bots na
## cadencia deles viraria cacofonia sem atenuacao por distancia; quando o Sfx
## ganhar posicionamento 3D, os bots entram.
signal spell_cast(element: String)
