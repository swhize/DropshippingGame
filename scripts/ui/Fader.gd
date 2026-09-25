extends CanvasLayer
## Schwarzblende für Szenenwechsel und Story-Übergänge (mit optionalem Text).

var rect: ColorRect
var label: Label
var busy: bool = false

func _ready() -> void:
	layer = 100
	process_mode = Node.PROCESS_MODE_ALWAYS
	rect = ColorRect.new()
	rect.color = Color(0, 0, 0, 1)
	rect.set_anchors_preset(Control.PRESET_FULL_RECT)
	rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(rect)
	label = Label.new()
	label.theme = UITheme.get_theme()
	label.theme_type_variation = "H1"
	label.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.modulate.a = 0.0
	add_child(label)

## Von Schwarz aufblenden (Szenenstart)
func fade_in(duration: float = 0.8) -> void:
	rect.color.a = 1.0
	rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var tw := create_tween()
	tw.tween_property(rect, "color:a", 0.0, duration)

## Abblenden, Text zeigen, mid aufrufen, wieder aufblenden.
func transition(text: String, mid: Callable, hold: float = 1.2) -> void:
	if busy:
		return
	busy = true
	rect.mouse_filter = Control.MOUSE_FILTER_STOP
	label.text = text
	var tw := create_tween()
	tw.tween_property(rect, "color:a", 1.0, 0.7)
	if text != "":
		tw.tween_property(label, "modulate:a", 1.0, 0.4)
	tw.tween_callback(mid)
	tw.tween_interval(hold)
	if text != "":
		tw.tween_property(label, "modulate:a", 0.0, 0.4)
	tw.tween_property(rect, "color:a", 0.0, 0.8)
	tw.tween_callback(_done)

## Nur abblenden, dann Callback (z.B. Szenenwechsel).
func fade_out(then: Callable, duration: float = 0.6) -> void:
	rect.mouse_filter = Control.MOUSE_FILTER_STOP
	var tw := create_tween()
	tw.tween_property(rect, "color:a", 1.0, duration)
	tw.tween_callback(then)

func _done() -> void:
	busy = false
	rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
