extends VBoxContainer
## Einstellungen: Lautstärken, Maus, Sichtfeld, Vollbild. Wird im Hauptmenü und Pausemenü genutzt.

signal back

func _ready() -> void:
	add_theme_constant_override("separation", 10)
	custom_minimum_size = Vector2(460, 0)
	var t := Label.new()
	t.text = "Einstellungen"
	t.theme_type_variation = "H2"
	add_child(t)
	_slider("Gesamtlautstärke", "master_volume", 0.0, 1.0, 0.05, true)
	_slider("Musik", "music_volume", 0.0, 1.0, 0.05, true)
	_slider("Effekte", "sfx_volume", 0.0, 1.0, 0.05, true)
	_slider("Mausempfindlichkeit", "mouse_sensitivity", 0.2, 3.0, 0.1, false)
	_slider("Sichtfeld (FOV)", "fov", 60.0, 100.0, 1.0, false)
	_check("Vollbild", "fullscreen")
	_check("Kopfwippen beim Laufen", "head_bob")
	var b := Button.new()
	b.text = "Zurück"
	b.theme_type_variation = "AccentButton"
	b.custom_minimum_size = Vector2(0, 40)
	b.pressed.connect(func():
		Audio.play("click")
		back.emit())
	add_child(b)

func _slider(label_text: String, key: String, min_v: float, max_v: float, step: float, percent: bool) -> void:
	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 12)
	var l := Label.new()
	l.text = label_text
	l.custom_minimum_size = Vector2(190, 0)
	row.add_child(l)
	var s := HSlider.new()
	s.min_value = min_v
	s.max_value = max_v
	s.step = step
	s.value = float(Settings.get(key))
	s.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	s.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	s.custom_minimum_size = Vector2(170, 20)
	row.add_child(s)
	var v := Label.new()
	v.custom_minimum_size = Vector2(56, 0)
	v.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	v.theme_type_variation = "Muted"
	row.add_child(v)
	var fmt := func(val: float) -> String:
		return "%d %%" % int(round(val * 100.0)) if percent else ("%.1f" % val if step < 1.0 else "%d" % int(val))
	v.text = fmt.call(s.value)
	s.value_changed.connect(func(val: float):
		v.text = fmt.call(val)
		Settings.set_value(key, val))
	add_child(row)

func _check(label_text: String, key: String) -> void:
	var c := CheckButton.new()
	c.text = label_text
	c.button_pressed = bool(Settings.get(key))
	c.toggled.connect(func(on: bool):
		Audio.play("click")
		Settings.set_value(key, on))
	add_child(c)
