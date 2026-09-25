extends CanvasLayer
## Gesprächsfenster unten im Bild: Sprecher, Text mit Schreibmaschinen-Effekt, optional
## Antwortmöglichkeiten. start(lines, choices, on_done) - on_done(choice_index) am Ende.

var panel: PanelContainer
var speaker_label: Label
var text_label: Label
var choice_box: HBoxContainer
var hint: Label

var _lines: Array = []
var _choices: Array = []
var _on_done: Callable
var _idx: int = 0
var _chars: float = 0.0
var active: bool = false

func _ready() -> void:
	layer = 20
	process_mode = Node.PROCESS_MODE_ALWAYS
	var root := Control.new()
	root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	root.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.theme = UITheme.get_theme()
	add_child(root)
	panel = PanelContainer.new()
	panel.anchor_left = 0.5
	panel.anchor_right = 0.5
	panel.anchor_top = 1.0
	panel.anchor_bottom = 1.0
	panel.offset_left = -440
	panel.offset_right = 440
	panel.offset_top = -210
	panel.offset_bottom = -30
	root.add_child(panel)
	var v := VBoxContainer.new()
	v.add_theme_constant_override("separation", 10)
	panel.add_child(v)
	speaker_label = Label.new()
	speaker_label.theme_type_variation = "AccentLabel"
	speaker_label.add_theme_font_size_override("font_size", 18)
	v.add_child(speaker_label)
	text_label = Label.new()
	text_label.autowrap_mode = TextServer.AUTOWRAP_WORD
	text_label.add_theme_font_size_override("font_size", 19)
	text_label.size_flags_vertical = Control.SIZE_EXPAND_FILL
	v.add_child(text_label)
	choice_box = HBoxContainer.new()
	choice_box.add_theme_constant_override("separation", 10)
	choice_box.alignment = BoxContainer.ALIGNMENT_END
	v.add_child(choice_box)
	hint = Label.new()
	hint.theme_type_variation = "Small"
	hint.text = "[E] / [Leertaste] / Klick – weiter"
	hint.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	v.add_child(hint)
	panel.visible = false

## lines: Array von {speaker, text}
func start(lines: Array, choices: Array = [], on_done: Callable = Callable()) -> void:
	_lines = lines
	_choices = choices
	_on_done = on_done
	_idx = 0
	_choices_shown = false
	active = true
	panel.visible = true
	GameManager.lock_input("dialogue")
	_show_line()

func _show_line() -> void:
	var line: Dictionary = _lines[_idx]
	speaker_label.text = String(line.get("speaker", ""))
	text_label.text = String(line.get("text", ""))
	text_label.visible_characters = 0
	_chars = 0.0
	for c in choice_box.get_children():
		c.queue_free()
	hint.visible = true

var _choices_shown: bool = false

func _process(delta: float) -> void:
	if not active:
		return
	if text_label.visible_characters != -1:
		_chars += delta * 55.0
		if _chars >= text_label.text.length():
			text_label.visible_characters = -1
		else:
			text_label.visible_characters = int(_chars)
	if text_label.visible_characters == -1 and _idx == _lines.size() - 1 and not _choices.is_empty() and not _choices_shown:
		_choices_shown = true
		hint.visible = false
		for i in _choices.size():
			var b := Button.new()
			b.text = String(_choices[i])
			b.theme_type_variation = "AccentButton" if i == 0 else ""
			b.pressed.connect(_finish.bind(i))
			choice_box.add_child(b)

func _unhandled_input(event: InputEvent) -> void:
	if not active:
		return
	var advance: bool = event.is_action_pressed("interact") or event.is_action_pressed("jump") \
		or (event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT)
	if not advance:
		return
	get_viewport().set_input_as_handled()
	if text_label.visible_characters != -1:
		text_label.visible_characters = -1
		return
	if _idx < _lines.size() - 1:
		_idx += 1
		Audio.play("click", 0.05, -8.0)
		_show_line()
	elif _choices.is_empty():
		_finish(-1)

func _finish(choice: int) -> void:
	active = false
	panel.visible = false
	GameManager.unlock_input("dialogue")
	Audio.play("click")
	if _on_done.is_valid():
		_on_done.call(choice)
