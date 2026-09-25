extends SceneTree
## Lädt und kompiliert JEDES GDScript im Projekt und meldet Fehler.
## godot --headless --path . -s tests/compile_all.gd

var failed: int = 0
var total: int = 0

func _initialize() -> void:
	_scan("res://scripts")
	print("\n=== COMPILE: %d Skripte, %d mit Fehlern ===" % [total, failed])
	quit(1 if failed > 0 else 0)

func _scan(dir_path: String) -> void:
	var dir := DirAccess.open(dir_path)
	if dir == null:
		return
	dir.list_dir_begin()
	var f := dir.get_next()
	while f != "":
		var p := dir_path + "/" + f
		if dir.current_is_dir():
			_scan(p)
		elif f.ends_with(".gd"):
			total += 1
			var s: GDScript = load(p)
			if s == null or not s.can_instantiate():
				failed += 1
				print("  FEHLER  " + p)
		f = dir.get_next()
