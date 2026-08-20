## Maquina PURA do botao de Fogo — REVISADA NA R17 pelo veredito do Diretor no
## aparelho: "clicar dispara; SEGURAR mantem disparando enquanto houver mana;
## arrastar direciona". O disparo saiu do RELEASE e foi para o PRESS + repeticao.
## Sem nos, sem cena, sem Input: testavel headless. A UI converte dp->px e
## alimenta press/drag/poll/release; a maquina so' classifica.
##   dedo desce  -> DISPARA ja' (e comeca o auto-fogo)
##   segurar     -> poll() pede um disparo por cadencia (mana quem barra e' o Player)
##   arrastar    -> direciona a mira (continua disparando)
##   soltar      -> para
## O CANCEL de "voltar ao centro" morreu COM o disparo-no-soltar: nao ha' mais
## "soltar dispara" para cancelar. O visual RING agora significa "disparando".
class_name FireGesture
extends RefCounted

enum { IDLE, FIRING }
enum Result { NONE, FIRE }
enum Visual { NONE, RING }

var deadzone_px := 20.0  # a UI calcula de Balance.TOUCH.aim_deadzone_dp
var repeat_ms := 270     # a UI copia de Balance.FIRE.fire_rate (em ms)

var state := IDLE
var _origin := Vector2.ZERO
var _offset := Vector2.ZERO
var _next_ms := 0


## Devolve FIRE imediatamente: clicar ja' dispara (pedido do Diretor).
func press(pos: Vector2, now_ms: int) -> Result:
	state = FIRING
	_origin = pos
	_offset = Vector2.ZERO
	_next_ms = now_ms + repeat_ms
	return Result.FIRE


func drag(pos: Vector2) -> void:
	if state != FIRING:
		return
	_offset = pos - _origin


## Chamado todo frame pela UI enquanto o dedo esta' no botao: um FIRE por
## cadencia. A MANA nao mora aqui de proposito — quem decide se o disparo sai
## e' o Player (autoridade unica de custo), esta maquina so' pede.
func poll(now_ms: int) -> Result:
	if state != FIRING or now_ms < _next_ms:
		return Result.NONE
	_next_ms = now_ms + repeat_ms
	return Result.FIRE


func release(_now_ms: int) -> Result:
	state = IDLE
	return Result.NONE


func has_aimed() -> bool:
	return state == FIRING and _offset.length() > deadzone_px


## O anel aceso = disparando. (O X de cancelamento morreu junto com o
## disparo-no-soltar; se um dia voltar um gesto cancelavel, ele volta.)
func visual(_now_ms: int) -> Visual:
	return Visual.RING if state == FIRING else Visual.NONE
