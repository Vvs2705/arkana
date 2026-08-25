## Auditoria headless de personagem externo:
##   Godot --headless --path godot --script res://characters/model_audit.gd
extends SceneTree

const MAGE_SCENE := "res://characters/Mage.tscn"
const DEFAULT_SLUG := "01-pyra"
## O ORCAMENTO GEOMETRICO E' EM TRIANGULO, e sempre foi (TECH_ART: LOD0 de
## personagem hero). Ate' 25/08 este auditor cobrava VERTICE num teto de 20.000
## e reprovava os DOIS modelos reais do jogo — Pyra com 22.561 e Brok com 29.212
## — estando ambos por volta de 15,4 mil triangulos, ou seja, dentro do alvo.
## Costura de UV duplica vertice sem criar um unico triangulo: era o teste
## medindo uma unidade e o orcamento outra.
const MIN_TRIANGLES := 12000
const MAX_TRIANGLES := 20000

## Mantido apenas como DIAGNOSTICO no relatorio. Nao e' mais portao.
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
	var tris := int(report.get("triangles", 0))
	_check(tris >= MIN_TRIANGLES and tris <= MAX_TRIANGLES,
		"triangulos dentro de %d..%d (medido: %d)" % [MIN_TRIANGLES, MAX_TRIANGLES, tris])
	print("  (diagnostico, sem portao) vertices: %d" % int(report["vertices"]))
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
