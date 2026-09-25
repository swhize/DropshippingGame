extends CanvasLayer
## Pausemenü (Esc): Weiter, Speichern, Einstellungen, Hilfe, Hauptmenü, Beenden.

signal main_menu_requested
signal quit_requested
signal help_requested

var card: PanelContainer
var menu_box: VBoxContainer
var settings: Control
var is_open: bool = false

func _ready() -> void:
	layer = 50
	process_mode = Node.PROCESS_MODE_ALWAYS
	var root := Control.new()
	root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	root.theme = UITheme.get_theme()
	add_child(root)
	var dim := ColorRect.new()
	dim.color = Color(0.02, 0.03, 0.05, 0.7)
	dim.set_anchors_preset(Control.PRESET_FULL_RECT)
	root.add_child(dim)
	var center := CenterContainer.new()
	center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	root.add_child(center)
	card = PanelContainer.new()
	center.add_child(card)
	var stack := VBoxContainer.new()
	card.add_child(stack)
	menu_box = VBoxContainer.new()
	menu_box.add_theme_constant_override("separation", 10)
	menu_box.custom_minimum_size = Vector2(340, 0)
	stack.add_child(menu_box)
	var title := Label.new()
	title.text = "Pause"
	title.theme_type_variation = "H1"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	menu_box.add_child(title)
	_btn("Weiterspielen", close, "AccentButton")
	_btn("Spiel speichern", _save)
	_btn("Einstellungen", _show_settings)
	_btn("Steuerung & Hilfe", func(): help_requested.emit())
	_btn("Zum Hauptmenü", func(): main_menu_requested.emit())
	_btn("Spiel beenden", func(): quit_requested.emit(), "DangerButton")
	var settings_panel := preload("res://scripts/ui/SettingsPanel.gd").new()
	settings = settings_panel
	settings.visible = false
	settings_panel.back.connect(_show_menu)
	stack.add_child(settings)
	visible = false

func _btn(text: String, cb: Callable, variation: String = "") -> void:
	var b := Button.new()
	b.text = text
	b.custom_minimum_size = Vector2(0, 42)
	if variation != "":
		b.theme_type_variation = variation
	b.pressed.connect(func():
		Audio.play("click")
		cb.call())
	menu_box.add_child(b)

func open() -> void:
	if is_open:
		return
	is_open = true
	visible = true
	_show_menu()
	GameManager.lock_input("pause")
	GameManager.set_paused("pause", true)
	Audio.play("whoosh", 0.05, -8.0)

func close() -> void:
	if not is_open:
		return
	is_open = false
	visible = false
	GameManager.unlock_input("pause")
	GameManager.set_paused("pause", false)

func _save() -> void:
	if GameManager.story_stage == "diner":
		GameManager.notify("Während der Imbiss-Schicht kann nicht gespeichert werden.", "info")
		return
	GameManager.save_game()

func _show_settings() -> void:
	menu_box.visible = false
	settings.visible = true

func _show_menu() -> void:
	menu_box.visible = true
	settings.visible = false

func _unhandled_input(event: InputEvent) -> void:
	if is_open and event.is_action_pressed("ui_cancel"):
		get_viewport().set_input_as_handled()
		if settings.visible:
			_show_menu()
		else:
			close()
