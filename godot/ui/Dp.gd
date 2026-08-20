## dp -> px de CANVAS. Numero que o dedo sente vive em dp (regra do projeto):
## fisico = dp * dpi/160; com stretch canvas_items o toque chega em px de
## canvas, entao converte pela razao base/janela.
class_name Dp


static func px(dp: float) -> float:
	var dpi := DisplayServer.screen_get_dpi()
	if dpi <= 0:
		dpi = 160
	var v := dp * float(dpi) / 160.0
	var win := DisplayServer.window_get_size().x
	if win > 0:
		var base := float(ProjectSettings.get_setting("display/window/size/viewport_width", 1280))
		v *= base / float(win)
	return v
