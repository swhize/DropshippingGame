class_name PCApp
extends VBoxContainer
## Basisklasse für alle HustleOS-Apps: Bauhelfer für Karten, Zeilen, Buttons, Balken.
## Apps überschreiben build() (und optional tick()/live_update()/can_rebuild()).

var screen: Node = null
var auto_refresh: bool = true

func _init() -> void:
	size_flags_horizontal = Control.SIZE_EXPAND_FILL
	add_theme_constant_override("separation", 14)

func build() -> void:
	pass

func can_rebuild() -> bool:
	return auto_refresh

func tick(_delta: float) -> void:
	pass

func live_update() -> void:
	pass

func rebuild() -> void:
	for c in get_children():
		remove_child(c)
		c.queue_free()
	build()

# ---- Bauhelfer ----------------------------------------------------------------------------
func page() -> VBoxContainer:
	## Innenabstand für den Inhalt
	var margin := MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 24)
	margin.add_theme_constant_override("margin_right", 24)
	margin.add_theme_constant_override("margin_top", 20)
	margin.add_theme_constant_override("margin_bottom", 24)
	margin.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	add_child(margin)
	var v := VBoxContainer.new()
	v.add_theme_constant_override("separation", 14)
	v.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	margin.add_child(v)
	return v

func header(parent: Node, title: String, subtitle: String = "") -> void:
	var l := Label.new()
	l.text = title
	l.theme_type_variation = "H1"
	parent.add_child(l)
	if subtitle != "":
		var s := Label.new()
		s.text = subtitle
		s.theme_type_variation = "Muted"
		s.autowrap_mode = TextServer.AUTOWRAP_WORD
		parent.add_child(s)

func card(parent: Node, title: String = "", variation: String = "Card") -> VBoxContainer:
	var p := PanelContainer.new()
	p.theme_type_variation = variation
	p.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	parent.add_child(p)
	var v := VBoxContainer.new()
	v.add_theme_constant_override("separation", 8)
	p.add_child(v)
	if title != "":
		var l := Label.new()
		l.text = title
		l.theme_type_variation = "H3"
		v.add_child(l)
	return v

func hbox(parent: Node, sep: int = 10) -> HBoxContainer:
	var h := HBoxContainer.new()
	h.add_theme_constant_override("separation", sep)
	parent.add_child(h)
	return h

func vbox(parent: Node, sep: int = 6) -> VBoxContainer:
	var v := VBoxContainer.new()
	v.add_theme_constant_override("separation", sep)
	parent.add_child(v)
	return v

func flow(parent: Node, sep: int = 10) -> HFlowContainer:
	var f := HFlowContainer.new()
	f.add_theme_constant_override("h_separation", sep)
	f.add_theme_constant_override("v_separation", sep)
	f.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	parent.add_child(f)
	return f

func label(parent: Node, text: String, variation: String = "", color: Color = Color(0, 0, 0, 0), size: int = 0) -> Label:
	var l := Label.new()
	l.text = text
	if variation != "":
		l.theme_type_variation = variation
	if color.a > 0.0:
		l.add_theme_color_override("font_color", color)
	if size > 0:
		l.add_theme_font_size_override("font_size", size)
	parent.add_child(l)
	return l

func wrap_label(parent: Node, text: String, variation: String = "Muted") -> Label:
	var l := label(parent, text, variation)
	l.autowrap_mode = TextServer.AUTOWRAP_WORD
	l.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	return l

func rich(parent: Node, bbcode: String) -> RichTextLabel:
	var r := RichTextLabel.new()
	r.bbcode_enabled = true
	r.fit_content = true
	r.scroll_active = false
	r.text = bbcode
	r.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	parent.add_child(r)
	return r

func button(parent: Node, text: String, cb: Callable, variation: String = "", disabled: bool = false, tip: String = "") -> Button:
	var b := Button.new()
	b.text = text
	b.custom_minimum_size = Vector2(0, 36)
	if variation != "":
		b.theme_type_variation = variation
	b.disabled = disabled
	b.tooltip_text = tip
	b.pressed.connect(func():
		Audio.play("click", 0.05, -4.0)
		cb.call())
	parent.add_child(b)
	return b

func spacer(parent: Node) -> Control:
	var c := Control.new()
	c.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	parent.add_child(c)
	return c

func swatch(parent: Node, color: Color, size: Vector2 = Vector2(30, 30), text: String = "") -> PanelContainer:
	var p := PanelContainer.new()
	p.custom_minimum_size = size
	p.add_theme_stylebox_override("panel", UITheme.style(color, 7, Color(0, 0, 0, 0), 0, 0))
	if text != "":
		var l := Label.new()
		l.text = text
		l.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		l.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		l.add_theme_font_size_override("font_size", int(size.y * 0.55))
		p.add_child(l)
	parent.add_child(p)
	return p

func bar(parent: Node, value: float, max_value: float, color: Color = UITheme.ACCENT, height: int = 8, width: int = 0) -> ProgressBar:
	var b := ProgressBar.new()
	b.max_value = maxf(max_value, 0.0001)
	b.value = value
	b.show_percentage = false
	b.custom_minimum_size = Vector2(width, height)
	b.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	if width == 0:
		b.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	b.add_theme_stylebox_override("fill", UITheme.style(color, 5, Color(0, 0, 0, 0), 0, 0))
	parent.add_child(b)
	return b

func stat(parent: Node, title: String, value: String, color: Color = UITheme.TEXT) -> void:
	var p := PanelContainer.new()
	p.theme_type_variation = "Card"
	p.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	parent.add_child(p)
	var v := VBoxContainer.new()
	p.add_child(v)
	label(v, title, "Small")
	label(v, value, "H2", color)

func kv(parent: Node, key: String, value: String, color: Color = Color(0, 0, 0, 0)) -> void:
	var h := hbox(parent, 8)
	var k := label(h, key, "Muted")
	k.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	var v := label(h, value, "", color)
	v.add_theme_font_override("font", UITheme.font(true))

func separator(parent: Node) -> void:
	parent.add_child(HSeparator.new())

func locked_page(def: Dictionary, reason: String) -> void:
	var p := page()
	header(p, "%s  %s" % [def["icon"], def["title"]])
	var c := card(p, "", "CardHi")
	label(c, "🔒  Noch gesperrt", "H2", UITheme.ACCENT)
	wrap_label(c, reason + ". Wachse weiter, dann schaltet sich diese App frei.", "")

func money(v: int) -> String:
	return UITheme.money(v)
