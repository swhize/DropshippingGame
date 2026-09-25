extends CanvasLayer
## Wiederverwendbares zentriertes Fenster (Karte) mit Titel, Inhalt und Buttons.
## Genutzt für Ereignis-Entscheidungen, Tagesabschluss, Bestätigungen, Hilfe, Ende.

var dim: ColorRect
var card: PanelContainer
var title_label: Label
var subtitle_label: Label
var body_box: VBoxContainer
var button_row: HBoxContainer
var is_open: bool = false
var _pause: bool = false
var _key: String = ""

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	var root := Control.new()
	root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	root.theme = UITheme.get_theme()
	root.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(root)
	dim = ColorRect.new()
	dim.color = Color(0, 0, 0, 0.55)
	dim.set_anchors_preset(Control.PRESET_FULL_RECT)
	root.add_child(dim)
	var center := CenterContainer.new()
	center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	root.add_child(center)
	card = PanelContainer.new()
	center.add_child(card)
	var v := VBoxContainer.new()
	v.add_theme_constant_override("separation", 12)
	card.add_child(v)
	title_label = Label.new()
	title_label.theme_type_variation = "H2"
	v.add_child(title_label)
	subtitle_label = Label.new()
	subtitle_label.theme_type_variation = "Muted"
	v.add_child(subtitle_label)
	body_box = VBoxContainer.new()
	body_box.add_theme_constant_override("separation", 8)
	v.add_child(body_box)
	button_row = HBoxContainer.new()
	button_row.add_theme_constant_override("separation", 10)
	button_row.alignment = BoxContainer.ALIGNMENT_END
	v.add_child(button_row)
	visible = false

## buttons: Array von [text, callable, variation]. Callable darf leer sein (= nur schließen).
func open(title: String, subtitle: String, width: float, buttons: Array, pause: bool = true, key: String = "modal") -> VBoxContainer:
	for c in body_box.get_children():
		c.queue_free()
	for c in button_row.get_children():
		c.queue_free()
	title_label.text = title
	subtitle_label.text = subtitle
	subtitle_label.visible = subtitle != ""
	card.custom_minimum_size = Vector2(width, 0)
	for b in buttons:
		add_button(String(b[0]), b[1] if b.size() > 1 else Callable(), String(b[2]) if b.size() > 2 else "")
	_pause = pause
	_key = key
	visible = true
	is_open = true
	GameManager.lock_input(key)
	if pause:
		GameManager.set_paused(key, true)
	card.modulate.a = 0.0
	card.scale = Vector2(0.96, 0.96)
	card.pivot_offset = card.size / 2.0
	var tw := create_tween()
	tw.tween_property(card, "modulate:a", 1.0, 0.18)
	tw.parallel().tween_property(card, "scale", Vector2.ONE, 0.18)
	Audio.play("whoosh", 0.05, -10.0)
	return body_box

func add_button(text: String, cb: Callable, variation: String = "") -> Button:
	var btn := Button.new()
	btn.text = text
	btn.custom_minimum_size = Vector2(120, 38)
	if variation != "":
		btn.theme_type_variation = variation
	btn.pressed.connect(_on_button.bind(cb))
	button_row.add_child(btn)
	return btn

func _on_button(cb: Callable) -> void:
	Audio.play("click")
	close()
	if cb.is_valid():
		cb.call()

func close() -> void:
	if not is_open:
		return
	is_open = false
	visible = false
	GameManager.unlock_input(_key)
	if _pause:
		GameManager.set_paused(_key, false)

func text(parent: Node, t: String, variation: String = "", width: float = 0.0) -> Label:
	var l := Label.new()
	l.text = t
	if variation != "":
		l.theme_type_variation = variation
	l.autowrap_mode = TextServer.AUTOWRAP_WORD
	if width > 0.0:
		l.custom_minimum_size = Vector2(width, 0)
	parent.add_child(l)
	return l

func kv_row(parent: Node, key: String, value: String, color: Color = Color(0, 0, 0, 0)) -> void:
	var h := HBoxContainer.new()
	var k := Label.new()
	k.text = key
	k.theme_type_variation = "Muted"
	k.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	h.add_child(k)
	var v := Label.new()
	v.text = value
	v.add_theme_font_override("font", UITheme.font(true))
	if color.a > 0.0:
		v.add_theme_color_override("font_color", color)
	h.add_child(v)
	parent.add_child(h)
