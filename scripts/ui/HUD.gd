extends CanvasLayer
## Spieler-HUD: Geld, Tag/Uhrzeit, Bewertung, Firmenlevel, Bestellungen, aktuelles Ziel,
## aktive Boosts, Fadenkreuz mit Aktionshinweis, gehaltener Gegenstand, Benachrichtigungen.

var player: Node = null

var root: Control
var money_label: Label
var money_pop: Label
var day_label: Label
var clock_label: Label
var rating_label: Label
var level_label: Label
var xp_bar: ProgressBar
var status_label: RichTextLabel
var obj_title: Label
var obj_text: Label
var obj_bar: ProgressBar
var boosts_label: RichTextLabel
var crosshair: Control
var prompt_panel: PanelContainer
var prompt_label: Label
var hands_panel: PanelContainer
var hands_label: Label
var hands_swatch: ColorRect
var toast_box: VBoxContainer
var banner: Label
var hint_label: Label

var _last_money: int = 0
var _left_panel: PanelContainer
var _right_panel: PanelContainer

func _ready() -> void:
	layer = 1
	root = Control.new()
	root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	root.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.theme = UITheme.get_theme()
	add_child(root)
	_build_left()
	_build_right()
	_build_center()
	_build_bottom()
	GameManager.economy_changed.connect(_refresh)
	GameManager.toast.connect(add_toast)
	GameManager.held_item_changed.connect(_on_held)
	GameManager.level_up.connect(_on_level_up)
	GameManager.objective_changed.connect(_refresh)
	GameManager.input_lock_changed.connect(_on_lock)
	_last_money = GameManager.money
	_refresh()
	_on_held(GameManager.ItemKind.NONE, {})

func _panel(pos: Vector2, anchor_right: bool) -> PanelContainer:
	var p := PanelContainer.new()
	p.theme_type_variation = "HudPanel"
	p.mouse_filter = Control.MOUSE_FILTER_IGNORE
	if anchor_right:
		p.anchor_left = 1.0
		p.anchor_right = 1.0
		p.offset_left = -pos.x
		p.offset_right = -16
		p.grow_horizontal = Control.GROW_DIRECTION_BEGIN
	else:
		p.position = pos
	p.offset_top = pos.y
	root.add_child(p)
	return p

func _label(parent: Node, text: String, variation: String = "", size: int = 0) -> Label:
	var l := Label.new()
	l.text = text
	if variation != "":
		l.theme_type_variation = variation
	if size > 0:
		l.add_theme_font_size_override("font_size", size)
	l.mouse_filter = Control.MOUSE_FILTER_IGNORE
	parent.add_child(l)
	return l

func _build_left() -> void:
	_left_panel = _panel(Vector2(16, 16), false)
	var v := VBoxContainer.new()
	v.add_theme_constant_override("separation", 4)
	_left_panel.add_child(v)
	var mrow := HBoxContainer.new()
	v.add_child(mrow)
	money_label = _label(mrow, "0 €", "Big")
	money_pop = _label(mrow, "", "AccentLabel", 20)
	money_pop.modulate.a = 0.0
	var trow := HBoxContainer.new()
	trow.add_theme_constant_override("separation", 12)
	v.add_child(trow)
	day_label = _label(trow, "Tag 1", "H3")
	clock_label = _label(trow, "08:00", "H3")
	rating_label = _label(trow, "★★★☆☆", "", 17)
	rating_label.add_theme_color_override("font_color", UITheme.ACCENT)
	var lrow := HBoxContainer.new()
	lrow.add_theme_constant_override("separation", 8)
	v.add_child(lrow)
	level_label = _label(lrow, "Level 1", "Small")
	xp_bar = ProgressBar.new()
	xp_bar.custom_minimum_size = Vector2(170, 8)
	xp_bar.show_percentage = false
	xp_bar.max_value = 1.0
	xp_bar.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	xp_bar.mouse_filter = Control.MOUSE_FILTER_IGNORE
	lrow.add_child(xp_bar)
	status_label = RichTextLabel.new()
	status_label.bbcode_enabled = true
	status_label.fit_content = true
	status_label.scroll_active = false
	status_label.custom_minimum_size = Vector2(360, 0)
	status_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	status_label.add_theme_font_size_override("normal_font_size", 15)
	v.add_child(status_label)

func _build_right() -> void:
	_right_panel = _panel(Vector2(380, 16), true)
	_right_panel.custom_minimum_size = Vector2(360, 0)
	var v := VBoxContainer.new()
	v.add_theme_constant_override("separation", 5)
	_right_panel.add_child(v)
	obj_title = _label(v, "", "AccentLabel")
	obj_text = _label(v, "", "", 15)
	obj_text.autowrap_mode = TextServer.AUTOWRAP_WORD
	obj_text.custom_minimum_size = Vector2(330, 0)
	obj_bar = ProgressBar.new()
	obj_bar.custom_minimum_size = Vector2(0, 6)
	obj_bar.show_percentage = false
	obj_bar.max_value = 1.0
	obj_bar.mouse_filter = Control.MOUSE_FILTER_IGNORE
	v.add_child(obj_bar)
	boosts_label = RichTextLabel.new()
	boosts_label.bbcode_enabled = true
	boosts_label.fit_content = true
	boosts_label.scroll_active = false
	boosts_label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	boosts_label.add_theme_font_size_override("normal_font_size", 14)
	v.add_child(boosts_label)
	toast_box = VBoxContainer.new()
	toast_box.anchor_left = 1.0
	toast_box.anchor_right = 1.0
	toast_box.offset_left = -380
	toast_box.offset_right = -16
	toast_box.offset_top = 250
	toast_box.add_theme_constant_override("separation", 6)
	toast_box.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(toast_box)

func _build_center() -> void:
	crosshair = Control.new()
	crosshair.set_anchors_preset(Control.PRESET_CENTER)
	crosshair.mouse_filter = Control.MOUSE_FILTER_IGNORE
	crosshair.draw.connect(_draw_crosshair)
	root.add_child(crosshair)
	prompt_panel = PanelContainer.new()
	prompt_panel.theme_type_variation = "HudPanel"
	prompt_panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	prompt_panel.anchor_left = 0.5
	prompt_panel.anchor_right = 0.5
	prompt_panel.anchor_top = 0.5
	prompt_panel.anchor_bottom = 0.5
	prompt_panel.offset_top = 34
	prompt_panel.grow_horizontal = Control.GROW_DIRECTION_BOTH
	root.add_child(prompt_panel)
	var h := HBoxContainer.new()
	h.add_theme_constant_override("separation", 10)
	prompt_panel.add_child(h)
	var key := PanelContainer.new()
	key.add_theme_stylebox_override("panel", UITheme.style(UITheme.ACCENT, 5, Color(0, 0, 0, 0), 0, 8))
	h.add_child(key)
	var kl := _label(key, "E", "", 15)
	kl.add_theme_color_override("font_color", Color(0.1, 0.08, 0.02))
	kl.add_theme_font_override("font", UITheme.font(true))
	prompt_label = _label(h, "", "", 16)
	prompt_panel.visible = false
	banner = _label(root, "", "Title")
	banner.set_anchors_preset(Control.PRESET_CENTER_TOP)
	banner.offset_top = 120
	banner.grow_horizontal = Control.GROW_DIRECTION_BOTH
	banner.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	banner.add_theme_color_override("font_color", UITheme.ACCENT)
	banner.add_theme_constant_override("outline_size", 12)
	banner.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.7))
	banner.modulate.a = 0.0

func _build_bottom() -> void:
	hands_panel = PanelContainer.new()
	hands_panel.theme_type_variation = "HudPanel"
	hands_panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	hands_panel.anchor_top = 1.0
	hands_panel.anchor_bottom = 1.0
	hands_panel.offset_left = 16
	hands_panel.offset_top = -64
	hands_panel.offset_bottom = -16
	hands_panel.grow_vertical = Control.GROW_DIRECTION_BEGIN
	root.add_child(hands_panel)
	var h := HBoxContainer.new()
	h.add_theme_constant_override("separation", 10)
	hands_panel.add_child(h)
	hands_swatch = ColorRect.new()
	hands_swatch.custom_minimum_size = Vector2(18, 18)
	hands_swatch.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	hands_swatch.mouse_filter = Control.MOUSE_FILTER_IGNORE
	h.add_child(hands_swatch)
	hands_label = _label(h, "", "", 16)
	hint_label = _label(root, "E Interagieren · G Ablegen · Tab Laptop · Esc Menü · F1 Hilfe", "Small")
	hint_label.anchor_left = 1.0
	hint_label.anchor_right = 1.0
	hint_label.anchor_top = 1.0
	hint_label.anchor_bottom = 1.0
	hint_label.offset_left = -520
	hint_label.offset_right = -16
	hint_label.offset_top = -34
	hint_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT

func _draw_crosshair() -> void:
	var active := player != null and String(player.prompt) != ""
	var col := UITheme.ACCENT if active else Color(1, 1, 1, 0.85)
	if active:
		crosshair.draw_arc(Vector2.ZERO, 9.0, 0.0, TAU, 28, col, 2.0, true)
	crosshair.draw_circle(Vector2.ZERO, 2.4, col)

func _process(_delta: float) -> void:
	var gm := GameManager
	clock_label.text = UITheme.clock(gm.time_minutes)
	if gm.lifestyle_owned.get("uhr", false):
		clock_label.add_theme_color_override("font_color", UITheme.ACCENT)
	var locked := gm.is_input_locked()
	crosshair.visible = not locked
	crosshair.queue_redraw()
	var p: String = String(player.prompt) if player != null else ""
	prompt_panel.visible = p != "" and not locked
	if prompt_label.text != p:
		prompt_label.text = p
		prompt_panel.reset_size()
		prompt_panel.offset_left = -prompt_panel.size.x / 2.0
		prompt_panel.offset_right = prompt_panel.size.x / 2.0
	var btxt := ""
	for b in gm.boosts:
		var left := maxf(0.0, float(b["ends_at"]) - gm.bclock())
		btxt += "[color=#9fe3d9]⚡ %s · ×%.1f · %d min[/color]\n" % [b["name"], float(b["mult"]), int(left)]
	if gm.bclock() < gm.shop_offline_until:
		btxt += "[color=#ff8a80]💥 Shop offline[/color]\n"
	boosts_label.text = btxt.strip_edges()

func _on_lock(locked: bool) -> void:
	_left_panel.modulate.a = 0.35 if locked and GameManager.pc_open else 1.0

func _refresh() -> void:
	var gm := GameManager
	if gm.money != _last_money:
		_pop_money(gm.money - _last_money)
		_last_money = gm.money
	money_label.text = UITheme.money(gm.money)
	money_label.add_theme_color_override("font_color", UITheme.BAD if gm.money < 0 else UITheme.TEXT)
	day_label.text = "Tag %d" % gm.day if gm.story_stage == "business" else "Imbiss"
	rating_label.text = "%s %s" % [UITheme.stars(gm.reputation), UITheme.rating_text(gm.reputation)]
	level_label.text = "Level %d" % gm.level
	xp_bar.value = gm.level_progress()
	if gm.story_stage == "business":
		var chips := ""
		for o in gm.order_queue:
			chips += GameData.product(o["product"])["icon"]
		status_label.text = "🛒 Bestellungen [b]%d/%d[/b]  %s\n📦 Eingang [b]%d[/b]   🚚 Unterwegs [b]%d[/b]   📤 Verpackt [b]%d[/b]" % [
			gm.pending_count(), gm.queue_capacity(), chips, gm.dock_crates.size(), gm.traveling_deliveries.size(), gm.packed_count()]
	else:
		status_label.text = "🍔 Schicht bei Kalle"
	var obj := gm.current_objective()
	obj_title.text = String(obj["title"])
	obj_text.text = String(obj["text"])
	var prog := float(obj["progress"])
	obj_bar.visible = prog >= 0.0
	obj_bar.value = maxf(prog, 0.0)

func _pop_money(delta: int) -> void:
	if delta == 0:
		return
	money_pop.text = ("  +" if delta > 0 else "  ") + UITheme.money(delta)
	money_pop.add_theme_color_override("font_color", UITheme.GOOD if delta > 0 else UITheme.BAD)
	money_pop.modulate.a = 1.0
	var tw := create_tween()
	tw.tween_interval(1.2)
	tw.tween_property(money_pop, "modulate:a", 0.0, 0.8)

func _on_held(kind: int, data: Dictionary) -> void:
	var pid: String = data.get("product", "")
	var pname: String = GameData.product(pid)["name"] if pid != "" else ""
	hands_swatch.color = GameData.product(pid)["color"] if pid != "" else Color(0.4, 0.42, 0.48)
	var q := float(data.get("quality", 1.0))
	var qt := "Premium" if q >= 1.3 else ("Billig" if q < 0.8 else "Standard")
	match kind:
		GameManager.ItemKind.CRATE:
			hands_label.text = "Kiste: %d× %s (%s)   [G] ablegen" % [int(data.get("quantity", 0)), pname, qt]
		GameManager.ItemKind.ITEM:
			hands_label.text = "%s für Bestellung (%s)   [G] ablegen" % [pname, UITheme.money(int(data.get("price", 0)))]
		GameManager.ItemKind.PACKAGE:
			hands_label.text = "Paket: %s – noch ohne Label   [G] ablegen" % pname
		GameManager.ItemKind.LABELED:
			hands_label.text = "Versandfertig: %s   [G] ablegen" % pname
		GameManager.ItemKind.PLATE:
			hands_label.text = "Teller für Tisch %d" % int(data.get("table", 0))
			hands_swatch.color = Color(0.95, 0.8, 0.4)
		_:
			hands_label.text = "Hände frei"
	hands_panel.reset_size()

func _on_level_up(lvl: int) -> void:
	banner.text = "LEVEL %d" % lvl
	banner.modulate.a = 0.0
	banner.scale = Vector2(0.8, 0.8)
	var tw := create_tween()
	tw.tween_property(banner, "modulate:a", 1.0, 0.25)
	tw.parallel().tween_property(banner, "scale", Vector2.ONE, 0.25).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	tw.tween_interval(1.8)
	tw.tween_property(banner, "modulate:a", 0.0, 0.6)

func add_toast(text: String, kind: String = "info") -> void:
	var colors := {"info": UITheme.TEAL, "good": UITheme.GOOD, "bad": UITheme.BAD}
	var col: Color = colors.get(kind, UITheme.TEAL)
	var p := PanelContainer.new()
	var st := UITheme.style(Color(0.06, 0.07, 0.1, 0.9), 8, col, 0, 12)
	st.border_width_left = 4
	st.border_color = col
	p.add_theme_stylebox_override("panel", st)
	p.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var l := Label.new()
	l.text = text
	l.autowrap_mode = TextServer.AUTOWRAP_WORD
	l.custom_minimum_size = Vector2(330, 0)
	l.add_theme_font_size_override("font_size", 15)
	p.add_child(l)
	toast_box.add_child(p)
	while toast_box.get_child_count() > 5:
		var old := toast_box.get_child(0)
		toast_box.remove_child(old)
		old.queue_free()
	p.modulate.a = 0.0
	var tw := p.create_tween()
	tw.tween_property(p, "modulate:a", 1.0, 0.2)
	tw.tween_interval(4.5)
	tw.tween_property(p, "modulate:a", 0.0, 0.5)
	tw.tween_callback(p.queue_free)
