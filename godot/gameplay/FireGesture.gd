## Maquina PURA do gesto unico do botao de Fogo (GDD §19.3 — e' LEI).
## Sem nos, sem cena, sem Input: testavel headless. A UI converte dp->px
## e alimenta press/drag/release; a maquina so' classifica.
##   dedo desce -> arrastar mira -> soltar dispara
##   toque curto (<= tap_max_ms, sem sair da deadzone) -> TAP (dispara na camera)
##   voltar ao centro (ou segurar parado alem do tap) -> CANCEL
class_name FireGesture
extends RefCounted

enum { IDLE, AIMING }
enum Result { NONE, TAP, FIRE, CANCEL }
enum Visual { NONE, RING, CROSS }  # anel = soltar dispara · X = soltar cancela

var deadzone_px := 20.0  # a UI calcula de Balance.TOUCH.aim_deadzone_dp
var tap_max_ms := 220    # a UI copia de Balance.TOUCH.tap_max_ms

var state := IDLE
var _origin := Vector2.ZERO
var _offset := Vector2.ZERO
var _press_ms := 0
var _ever_aimed := false


func press(pos: Vector2, now_ms: int) -> void:
	state = AIMING
	_origin = pos
	_offset = Vector2.ZERO
	_press_ms = now_ms
	_ever_aimed = false


func drag(pos: Vector2) -> void:
	if state != AIMING:
		return
	_offset = pos - _origin
	if _offset.length() > deadzone_px:
		_ever_aimed = true


func release(now_ms: int) -> Result:
	if state != AIMING:
		return Result.NONE
	state = IDLE
	if _offset.length() > deadzone_px:
		return Result.FIRE
	if not _ever_aimed and now_ms - _press_ms <= tap_max_ms:
		return Result.TAP
	return Result.CANCEL  # voltou ao centro (ou segurou parado): NAO dispara


func has_aimed() -> bool:
	return state == AIMING and _ever_aimed


## O botao MOSTRA o que soltar faz AGORA (cancelar e' estado de 1a classe;
## cor + FORMA, regra do projeto).
func visual(now_ms: int) -> Visual:
	if state != AIMING:
		return Visual.NONE
	if _offset.length() > deadzone_px:
		return Visual.RING
	if not _ever_aimed and now_ms - _press_ms <= tap_max_ms:
		return Visual.RING  # soltar agora seria TAP: ainda dispara
	return Visual.CROSS
