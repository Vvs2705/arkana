## Auditoria headless de personagem externo:
##   Godot --headless --path godot --script res://characters/model_audit.gd
extends SceneTree

const MAGE_SCENE := "res://characters/Mage.tscn"
const DEFAULT_SLUG := "01-pyra"
const MAX_VERTICES := 20000
const MAX_SURFACES := 8
const MAX_MATERIALS := 8
const MAX_BONES := 90

var _fails := 0
var _mage: Node3D
var _slug := DEFAULT_SLUG
var _frames := 0


func _initialize() -> void:
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--mage="):
			_slug = arg.trim_prefix("--mage=")
	_mage = load(MAGE_SCENE).instantiate()
	_mage.mage_id = _slug
	root.add_child(_mage)


func _process(_delta: float) -> bool:
	_frames += 1
	if _frames < 2:
		return false

	var report: Dictionary = _mage.get_model_report()
	print("--- auditoria de modelo: %s ---" % _slug)
	for k in report.keys():
		print("%s: %s" % [k, report[k]])

	_check(String(report["source"]).begins_with("external:"),
		"%s usa modelo externo" % _slug)
	_check(int(report["vertices"]) > 0 and int(report["vertices"]) <= MAX_VERTICES,
		"vertices <= %d" % MAX_VERTICES)
	_check(int(report["surface_count"]) > 0 and int(report["surface_count"]) <= MAX_SURFACES,
		"superficies/draw calls aprox <= %d" % MAX_SURFACES)
	_check(int(report["material_slots"]) > 0 and int(report["material_slots"]) <= MAX_MATERIALS,
		"materiais <= %d" % MAX_MATERIALS)
	_check(int(report["bones"]) <= MAX_BONES, "ossos <= %d" % MAX_BONES)
	var anims: Dictionary = report["animations"]
	for required in ["idle", "run", "cast"]:
		_check(anims.has(required), "animacao obrigatoria '%s' resolvida" % required)

	print("RESULTADO: %s" % ("OK" if _fails == 0 else "%d FALHAS" % _fails))
	quit(0 if _fails == 0 else 1)
	return true


func _check(ok: bool, msg: String) -> void:
	print(("PASS   " if ok else "FALHOU ") + msg)
	if not ok:
		_fails += 1
