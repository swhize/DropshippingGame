extends CanvasLayer
## "HustleOS" - die Laptop-/Handy-Oberfläche. Seitenleiste mit Apps, Kopfzeile mit Uhr und
## Kontostand, Inhaltsbereich lädt die jeweilige App (scripts/pc/apps/*.gd).

const APPS := [
	{"id": "Mail", "icon": "📧", "title": "Postfach"},
	{"id": "Einkauf", "icon": "📦", "title": "Einkauf"},
	{"id": "Verpackung", "icon": "🧰", "title": "Verpackung"},
	{"id": "Webshop", "icon": "🌐", "title": "Webshop"},
	{"id": "Marketing", "icon": "📣", "title": "Marketing"},
	{"id": "Branding", "icon": "🏷", "title": "Branding"},
	{"id": "Markt", "icon": "🏪", "title": "Marktanalyse"},
	{"id": "Trading", "icon": "📈", "title": "Trading"},
	{"id": "Personal", "icon": "👷", "title": "Personal"},
	{"id": "Ausbau", "icon": "🏗", "title": "Ausbau"},
	{"id": "Analytics", "icon": "📊", "title": "Analytics"},
	{"id": "Ziele", "icon": "🏆", "title": "Ziele & Lifestyle"},
]

var current: String = "Einkauf"
var app: PCApp = null
var sidebar_buttons: Dictionary = {}
var content_scroll: ScrollContainer
var clock_label: Label
var money_label: Label
var title_label: Label
var brand_label: Label
var frame: PanelContainer
var _dirty: bool = false
var _dirty_t: float = 0.0
var _restore_scroll: int = -1

func _ready() -> void:
	layer = 10
	add_to_group("pc_screen")
	var root := Control.new()
	root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	root.theme = UITheme.get_theme()
	add_child(root)
	var dim := ColorRect.new()
	dim.color = Color(0.01, 0.02, 0.04, 0.72)
	dim.set_anchors_preset(Control.PRESET_FULL_RECT)
	root.add_child(dim)
	frame = PanelContainer.new()
	frame.anchor_left = 0.5
	frame.anchor_right = 0.5
	frame.anchor_top = 0.5
	frame.anchor_bottom = 0.5
	frame.offset_left = -600
	frame.offset_right = 600
	frame.offset_top = -335
	frame.offset_bottom = 335
	frame.add_theme_stylebox_override("panel", UITheme.style(Color(0.035, 0.04, 0.055), 18, Color(1, 1, 1, 0.12), 2, 10, 24))
	root.add_child(frame)
	var outer := VBoxContainer.new()
	outer.add_theme_constant_override("separation", 0)
	frame.add_child(outer)
	outer.add_child(_top_bar())
	var body := HBoxContainer.new()
	body.size_flags_vertical = Control.SIZE_EXPAND_FILL
	body.add_theme_constant_override("separation", 0)
	outer.add_child(body)
	body.add_child(_sidebar())
	var content_bg := PanelContainer.new()
	content_bg.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	var cbs := UITheme.style(Color(0.075, 0.085, 0.115), 12, Color(0, 0, 0, 0), 0, 0)
	content_bg.add_theme_stylebox_override("panel", cbs)
	body.add_child(content_bg)
	content_scroll = ScrollContainer.new()
	content_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	content_bg.add_child(content_scroll)
	visible = false
	GameManager.pc_toggled.connect(_on_pc_toggled)
	GameManager.economy_changed.connect(_mark_dirty)
	Events.mail_changed.connect(_update_badges)
	Market.trading_changed.connect(_on_trading)

func _top_bar() -> Control:
	var bar := PanelContainer.new()
	bar.add_theme_stylebox_override("panel", UITheme.style(Color(0.055, 0.062, 0.085), 12, Color(0, 0, 0, 0), 0, 14))
	var h := HBoxContainer.new()
	h.add_theme_constant_override("separation", 16)
	bar.add_child(h)
	var logo := Label.new()
	logo.text = "◆ HustleOS"
	logo.add_theme_font_override("font", UITheme.font(true))
	logo.add_theme_color_override("font_color", UITheme.ACCENT)
	logo.add_theme_font_size_override("font_size", 18)
	h.add_child(logo)
	brand_label = Label.new()
	brand_label.theme_type_variation = "Muted"
	h.add_child(brand_label)
	title_label = Label.new()
	title_label.theme_type_variation = "H3"
	title_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	h.add_child(title_label)
	money_label = Label.new()
	money_label.add_theme_font_override("font", UITheme.font(true))
	h.add_child(money_label)
	clock_label = Label.new()
	clock_label.theme_type_variation = "Muted"
	h.add_child(clock_label)
	var close := Button.new()
	close.text = "✕  Schließen"
	close.theme_type_variation = "GhostButton"
	close.pressed.connect(func(): GameManager.close_pc())
	h.add_child(close)
	return bar

func _sidebar() -> Control:
	var side := PanelContainer.new()
	side.custom_minimum_size = Vector2(212, 0)
	side.add_theme_stylebox_override("panel", UITheme.style(Color(0.045, 0.05, 0.07), 0, Color(0, 0, 0, 0), 0, 10))
	var scroll := ScrollContainer.new()
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	side.add_child(scroll)
	var v := VBoxContainer.new()
	v.add_theme_constant_override("separation", 3)
	v.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(v)
	for a in APPS:
		var b := Button.new()
		b.alignment = HORIZONTAL_ALIGNMENT_LEFT
		b.custom_minimum_size = Vector2(0, 40)
		b.theme_type_variation = "GhostButton"
		b.pressed.connect(open_app.bind(String(a["id"])))
		v.add_child(b)
		sidebar_buttons[a["id"]] = b
	return side

func _app_def(id: String) -> Dictionary:
	for a in APPS:
		if a["id"] == id:
			return a
	return APPS[0]

func app_locked_reason(id: String) -> String:
	var gm := GameManager
	match id:
		"Trading":
			if gm.level < int(GameData.APP_LEVELS["Trading"]):
				return "Ab Firmenlevel %d" % int(GameData.APP_LEVELS["Trading"])
		"Personal":
			if gm.location_stage < 1:
				return "Braucht die Lagerhalle"
			if gm.level < int(GameData.APP_LEVELS["Personal"]):
				return "Ab Firmenlevel %d" % int(GameData.APP_LEVELS["Personal"])
	return ""

func _update_badges() -> void:
	for a in APPS:
		var id: String = a["id"]
		var b: Button = sidebar_buttons[id]
		var badge := ""
		if id == "Mail" and Events.unread_count() > 0:
			badge = "  (%d)" % Events.unread_count()
		if id == "Marketing" and GameManager.tiktok_available():
			badge = "  •"
		var lock := "  🔒" if app_locked_reason(id) != "" else ""
		b.text = "%s   %s%s%s" % [a["icon"], a["title"], badge, lock]
		b.theme_type_variation = "SideActive" if id == current else "GhostButton"
		b.modulate = Color(1, 1, 1, 0.55) if lock != "" else Color(1, 1, 1, 1)

func _on_pc_toggled(is_open: bool) -> void:
	visible = is_open
	if not is_open:
		return
	Audio.play("whoosh", 0.05, -6.0)
	var gm := GameManager
	var target := current
	if gm.tutorial_step == 1:
		target = "Einkauf"
	elif gm.tutorial_step == 4:
		target = "Webshop"
	elif Events.pending_count() > 0:
		target = "Mail"
	open_app(target)
	frame.modulate.a = 0.0
	frame.scale = Vector2(0.97, 0.97)
	frame.pivot_offset = frame.size / 2.0
	var tw := create_tween().set_parallel(true)
	tw.tween_property(frame, "modulate:a", 1.0, 0.16)
	tw.tween_property(frame, "scale", Vector2.ONE, 0.16)

func open_app(id: String) -> void:
	current = id
	_restore_scroll = -1
	_dirty = false
	Audio.play("click", 0.05, -6.0)
	if app != null:
		app.queue_free()
		app = null
	var reason := app_locked_reason(id)
	var script_path := "res://scripts/pc/apps/App%s.gd" % id
	if reason != "":
		app = PCApp.new()
		app.screen = self
		content_scroll.add_child(app)
		app.locked_page(_app_def(id), reason)
	else:
		app = load(script_path).new()
		app.screen = self
		content_scroll.add_child(app)
		app.build()
	content_scroll.scroll_vertical = 0
	title_label.text = "%s  %s" % [_app_def(id)["icon"], _app_def(id)["title"]]
	_update_badges()

func _mark_dirty() -> void:
	_dirty = true

func _on_trading() -> void:
	if visible and app != null and current == "Trading":
		app.live_update()

func _process(delta: float) -> void:
	if not visible:
		return
	var gm := GameManager
	clock_label.text = "Tag %d · %s" % [gm.day, UITheme.clock(gm.time_minutes)]
	money_label.text = UITheme.money(gm.money)
	money_label.add_theme_color_override("font_color", UITheme.BAD if gm.money < 0 else UITheme.GOOD)
	brand_label.text = gm.brand_name
	if _restore_scroll >= 0:
		content_scroll.scroll_vertical = _restore_scroll
		_restore_scroll = -1
	_dirty_t += delta
	if _dirty and _dirty_t > 0.25 and app != null and app.can_rebuild():
		_dirty = false
		_dirty_t = 0.0
		_restore_scroll = content_scroll.scroll_vertical
		app.rebuild()
		_update_badges()
	elif app != null:
		app.tick(delta)

func _unhandled_input(event: InputEvent) -> void:
	if not visible:
		return
	if event.is_action_pressed("ui_cancel") or event.is_action_pressed("open_pc"):
		get_viewport().set_input_as_handled()
		GameManager.close_pc()
