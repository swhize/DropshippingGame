extends Node3D
## Hauptmenü: die Spielwelt in der Abenddämmerung als Kulisse mit langsamer Kamerafahrt,
## links das Menü (Fortsetzen, Neues Spiel, Einstellungen, Credits, Beenden).

var world: WorldBuilder
var cam: Camera3D
var fader: CanvasLayer
var content: VBoxContainer
var _t: float = 0.0

func _ready() -> void:
	get_tree().paused = false
	GameManager.in_game = false
	GameManager.clear_locks()
	GameManager.reset_state()
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
	world = WorldBuilder.new()
	add_child(world)
	world.build(true)
	world.set_time_of_day(19.35)
	cam = Camera3D.new()
	cam.fov = 58.0
	add_child(cam)
	cam.current = true
	_update_camera()
	_build_ui()
	fader = CanvasLayer.new()
	fader.set_script(load("res://scripts/ui/Fader.gd"))
	add_child(fader)
	fader.fade_in(1.4)
	Audio.play_music("menu")
	if not OS.get_cmdline_user_args().is_empty():
		var h := Node.new()
		h.set_script(load("res://scripts/DebugHarness.gd"))
		h.set("mode", "menu")
		add_child(h)

func _process(delta: float) -> void:
	_t += delta
	_update_camera()

func _update_camera() -> void:
	var x := -18.0 + sin(_t * 0.045) * 20.0
	cam.position = Vector3(x, 2.9 + sin(_t * 0.11) * 0.25, 12.3)
	cam.look_at(Vector3(x * 0.7 - 6.0, 2.7, -9.0))

# ---- Oberfläche ------------------------------------------------------------------------------
func _build_ui() -> void:
	var layer := CanvasLayer.new()
	add_child(layer)
	var root := Control.new()
	root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	root.theme = UITheme.get_theme()
	layer.add_child(root)
	var shade := TextureRect.new()
	var grad := Gradient.new()
	grad.set_color(0, Color(0.02, 0.03, 0.05, 0.92))
	grad.set_color(1, Color(0.02, 0.03, 0.05, 0.0))
	var gt := GradientTexture2D.new()
	gt.gradient = grad
	gt.fill_from = Vector2(0, 0)
	gt.fill_to = Vector2(1, 0)
	shade.texture = gt
	shade.anchor_bottom = 1.0
	shade.offset_right = 760
	shade.stretch_mode = TextureRect.STRETCH_SCALE
	shade.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(shade)
	var margin := MarginContainer.new()
	margin.anchor_bottom = 1.0
	margin.offset_right = 560
	margin.add_theme_constant_override("margin_left", 70)
	margin.add_theme_constant_override("margin_top", 70)
	margin.add_theme_constant_override("margin_bottom", 50)
	root.add_child(margin)
	var v := VBoxContainer.new()
	v.add_theme_constant_override("separation", 14)
	margin.add_child(v)
	var title := Label.new()
	title.text = "DROPSHIPPING\nSIMULATOR"
	title.theme_type_variation = "Title"
	title.add_theme_constant_override("line_spacing", -14)
	title.add_theme_constant_override("outline_size", 10)
	title.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.5))
	v.add_child(title)
	var sub := Label.new()
	sub.text = "Vom Imbiss zum Imperium"
	sub.theme_type_variation = "AccentLabel"
	sub.add_theme_font_size_override("font_size", 22)
	v.add_child(sub)
	var gap := Control.new()
	gap.custom_minimum_size = Vector2(0, 26)
	v.add_child(gap)
	content = VBoxContainer.new()
	content.add_theme_constant_override("separation", 10)
	content.custom_minimum_size = Vector2(400, 0)
	v.add_child(content)
	var ver := Label.new()
	ver.text = "v1.0 · Alle Modelle, Texturen, Sounds & Musik werden prozedural erzeugt · Godot 4.7"
	ver.theme_type_variation = "Small"
	ver.anchor_left = 1.0
	ver.anchor_right = 1.0
	ver.anchor_top = 1.0
	ver.anchor_bottom = 1.0
	ver.offset_left = -700
	ver.offset_right = -20
	ver.offset_top = -34
	ver.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	root.add_child(ver)
	_show_main()

func _clear() -> void:
	for c in content.get_children():
		content.remove_child(c)
		c.queue_free()

func _btn(text: String, cb: Callable, variation: String = "", sub: String = "") -> Button:
	var b := Button.new()
	b.text = text if sub == "" else text + "\n" + sub
	b.custom_minimum_size = Vector2(400, 58 if sub != "" else 50)
	b.alignment = HORIZONTAL_ALIGNMENT_LEFT
	b.add_theme_font_size_override("font_size", 19)
	if variation != "":
		b.theme_type_variation = variation
	b.pressed.connect(func():
		Audio.play("click")
		cb.call())
	b.mouse_entered.connect(func(): Audio.play("hover", 0.05, -6.0))
	content.add_child(b)
	return b

func _show_main() -> void:
	_clear()
	var summary := GameManager.save_summary()
	if not summary.is_empty():
		_btn("▶  Fortsetzen", _start.bind("continue"), "AccentButton",
			"      %s · Tag %d · %s · Level %d" % [summary["brand"], int(summary["day"]), UITheme.money(int(summary["money"])), int(summary["level"])])
	_btn("✦  Neues Spiel", _show_new_game, "" if not summary.is_empty() else "AccentButton")
	_btn("⚙  Einstellungen", _show_settings)
	_btn("♥  Credits", _show_credits)
	_btn("⏻  Beenden", func(): get_tree().quit())

func _show_new_game() -> void:
	_clear()
	var h := Label.new()
	h.text = "Neues Spiel"
	h.theme_type_variation = "H2"
	content.add_child(h)
	if GameManager.has_save():
		var w := Label.new()
		w.text = "⚠ Dein bisheriger Spielstand wird beim ersten Speichern überschrieben."
		w.theme_type_variation = "AccentLabel"
		w.autowrap_mode = TextServer.AUTOWRAP_WORD
		w.custom_minimum_size = Vector2(400, 0)
		content.add_child(w)
	_btn("🍔  Mit Story starten (empfohlen)", _start.bind("intro"), "AccentButton", "      Beginne als Aushilfe in Kalles Imbiss. Inkl. Tutorial.")
	_btn("🏠  Direkt in die Garage", _start.bind("tutorial"), "", "      Ohne Imbiss-Intro, mit Tutorial.")
	_btn("⚡  Profi-Start", _start.bind("skip"), "", "      Kein Intro, kein Tutorial. Du weißt, was du tust.")
	_btn("←  Zurück", _show_main, "GhostButton")

func _show_settings() -> void:
	_clear()
	var panel := PanelContainer.new()
	content.add_child(panel)
	var s: Node = preload("res://scripts/ui/SettingsPanel.gd").new()
	s.connect("back", _show_main)
	panel.add_child(s)

func _show_credits() -> void:
	_clear()
	var panel := PanelContainer.new()
	content.add_child(panel)
	var v := VBoxContainer.new()
	v.add_theme_constant_override("separation", 10)
	panel.add_child(v)
	var h := Label.new()
	h.text = "Credits"
	h.theme_type_variation = "H2"
	v.add_child(h)
	var t := Label.new()
	t.text = "Idee & Game Design: du\nCode, 3D-Welt, Shader, Sounds & Musik: Claude\nEngine: Godot 4.7\n\nKein einziges Asset wurde importiert. Jedes Modell, jede Textur und jeder Ton entsteht beim Start aus Code."
	t.autowrap_mode = TextServer.AUTOWRAP_WORD
	t.custom_minimum_size = Vector2(380, 0)
	v.add_child(t)
	var b := Button.new()
	b.text = "Zurück"
	b.theme_type_variation = "AccentButton"
	b.pressed.connect(_show_main)
	v.add_child(b)

func _start(mode: String) -> void:
	GameManager.start_mode = mode
	Audio.play("whoosh")
	fader.fade_out(func(): get_tree().change_scene_to_file("res://scenes/Main.tscn"), 0.7)
