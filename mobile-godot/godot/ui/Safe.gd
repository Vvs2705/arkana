## CONTRATO DE AREA SEGURA — dono: COORDENADOR. Raias usam, nao editam.
##
## O bug do APK R19: nenhuma tela consultava a area segura do Android, entao
## vida/timer sumiam sob o notch e o joystick/Fogo caiam sob a barra de gestos.
## Toda margem de borda no jogo passa por aqui.
##
## POR QUE canvas() e nao a base do project.godot (achado da raia UI, R20):
## com stretch/aspect="expand" o Godot NAO estica o canvas — ele o AUMENTA no
## eixo que sobra. Base 1600x720 num 16:9 vira 1600x900 de canvas real. Medir
## pela base do project.godot errava a margem em ate' 25% JUSTO no eixo do
## notch. get_visible_rect() devolve o canvas de verdade: erro zero em qualquer
## aspecto (20:9, 21:9, tablet 16:9, dobravel).
##
## Uso:
##   ctrl.offset_left  =  Safe.left()  + Dp.px(12)
##   ctrl.offset_right = -Safe.right() - Dp.px(12)
## ou, para encostar um Control de tela cheia dentro da area util:
##   Safe.apply(ctrl)
class_name Safe


## Tamanho REAL do canvas em px. Fonte unica de verdade para Safe e Dp.
static func canvas() -> Vector2:
	var loop := Engine.get_main_loop()
	if loop is SceneTree and (loop as SceneTree).root != null:
		var v := (loop as SceneTree).root.get_visible_rect().size
		if v.x > 0.0 and v.y > 0.0:
			return v
	# Headless/selftest: cai na base declarada, que ali e' o canvas mesmo.
	return Vector2(
			float(ProjectSettings.get_setting("display/window/size/viewport_width", 1600)),
			float(ProjectSettings.get_setting("display/window/size/viewport_height", 720)))


## Recorte seguro em px de CANVAS (convertido do fisico pela razao canvas/janela).
static func rect() -> Rect2:
	var cv := canvas()
	var win := Vector2(DisplayServer.window_get_size())
	if win.x <= 0.0 or win.y <= 0.0:
		return Rect2(Vector2.ZERO, cv)
	var sa := DisplayServer.get_display_safe_area()
	# Desktop/editor devolve a tela inteira: vira margem zero, sem ramo especial.
	var k := cv / win
	return Rect2(Vector2(sa.position) * k, Vector2(sa.size) * k)


static func left() -> float:
	return maxf(rect().position.x, 0.0)


static func top() -> float:
	return maxf(rect().position.y, 0.0)


## Quanto sobra a DIREITA da area util ate a borda do canvas (sempre >= 0).
static func right() -> float:
	var r := rect()
	return maxf(canvas().x - (r.position.x + r.size.x), 0.0)


static func bottom() -> float:
	var r := rect()
	return maxf(canvas().y - (r.position.y + r.size.y), 0.0)


## Encosta um Control ancorado em tela cheia dentro da area util.
## extra_dp e' a folga de respiro por cima da area segura.
static func apply(c: Control, extra_dp: float = 0.0) -> void:
	var e := Dp.px(extra_dp)
	c.offset_left = left() + e
	c.offset_top = top() + e
	c.offset_right = -right() - e
	c.offset_bottom = -bottom() - e
