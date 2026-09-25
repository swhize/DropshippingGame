class_name UITheme
extends RefCounted
## Einheitliches Aussehen für alle Oberflächen (HUD, PC, Menüs) + Formatierungshelfer.

const BG := Color(0.07, 0.08, 0.11, 0.94)
const BG_SOFT := Color(0.1, 0.11, 0.15, 0.96)
const CARD := Color(0.13, 0.145, 0.19, 1.0)
const CARD_HI := Color(0.17, 0.19, 0.25, 1.0)
const ACCENT := Color(1.0, 0.74, 0.26)
const ACCENT_DARK := Color(0.86, 0.58, 0.12)
const TEAL := Color(0.32, 0.86, 0.78)
const GOOD := Color(0.42, 0.88, 0.52)
const BAD := Color(1.0, 0.44, 0.44)
const TEXT := Color(0.94, 0.95, 0.97)
const MUTED := Color(0.62, 0.66, 0.74)
const BORDER := Color(1, 1, 1, 0.08)

static var _theme: Theme = null
static var _font: Font = null
static var _font_bold: Font = null

static func font(bold: bool = false) -> Font:
	if bold:
		if _font_bold == null:
			var fb := SystemFont.new()
			fb.font_names = PackedStringArray(["Segoe UI", "Inter", "Roboto", "Arial"])
			fb.font_weight = 700
			_font_bold = fb
		return _font_bold
	if _font == null:
		var f := SystemFont.new()
		f.font_names = PackedStringArray(["Segoe UI", "Inter", "Roboto", "Arial"])
		f.font_weight = 500
		_font = f
	return _font

static func style(color: Color, radius: int = 10, border_color: Color = Color(0, 0, 0, 0), border: int = 0,
		margin: float = 12.0, shadow: int = 0) -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = color
	s.set_corner_radius_all(radius)
	if border > 0:
		s.border_color = border_color
		s.set_border_width_all(border)
	s.content_margin_left = margin
	s.content_margin_right = margin
	s.content_margin_top = margin * 0.6
	s.content_margin_bottom = margin * 0.6
	if shadow > 0:
		s.shadow_color = Color(0, 0, 0, 0.35)
		s.shadow_size = shadow
		s.shadow_offset = Vector2(0, 3)
	s.anti_aliasing = true
	return s

static func get_theme() -> Theme:
	if _theme != null:
		return _theme
	var t := Theme.new()
	t.default_font = font(false)
	t.default_font_size = 16

	t.set_color("font_color", "Label", TEXT)
	t.set_color("font_outline_color", "Label", Color(0, 0, 0, 0.6))

	# Buttons
	_button_styles(t, "Button", Color(0.18, 0.2, 0.26), Color(0.24, 0.27, 0.35), ACCENT_DARK, TEXT, Color(0.1, 0.08, 0.02))
	t.set_type_variation("AccentButton", "Button")
	_button_styles(t, "AccentButton", ACCENT, Color(1.0, 0.82, 0.42), ACCENT_DARK, Color(0.12, 0.09, 0.02), Color(0.12, 0.09, 0.02))
	t.set_font("font", "AccentButton", font(true))
	t.set_type_variation("DangerButton", "Button")
	_button_styles(t, "DangerButton", Color(0.45, 0.16, 0.18), Color(0.6, 0.2, 0.22), Color(0.35, 0.1, 0.12), TEXT, TEXT)
	t.set_type_variation("GhostButton", "Button")
	_button_styles(t, "GhostButton", Color(0, 0, 0, 0), Color(1, 1, 1, 0.07), Color(1, 1, 1, 0.12), TEXT, TEXT)
	t.set_constant("h_separation", "GhostButton", 10)
	t.set_type_variation("SideActive", "Button")
	_button_styles(t, "SideActive", Color(1.0, 0.74, 0.26, 0.16), Color(1.0, 0.74, 0.26, 0.22), Color(1.0, 0.74, 0.26, 0.3), ACCENT, ACCENT)
	t.set_font("font", "SideActive", font(true))

	# Panels
	t.set_stylebox("panel", "PanelContainer", style(BG, 14, BORDER, 1, 18, 8))
	t.set_stylebox("panel", "Panel", style(BG, 14, BORDER, 1, 18))
	t.set_type_variation("Card", "PanelContainer")
	t.set_stylebox("panel", "Card", style(CARD, 10, BORDER, 1, 14))
	t.set_type_variation("CardHi", "PanelContainer")
	t.set_stylebox("panel", "CardHi", style(CARD_HI, 10, Color(1.0, 0.74, 0.26, 0.35), 1, 14))
	t.set_type_variation("Chip", "PanelContainer")
	t.set_stylebox("panel", "Chip", style(Color(1, 1, 1, 0.08), 8, Color(0, 0, 0, 0), 0, 8))
	t.set_type_variation("HudPanel", "PanelContainer")
	t.set_stylebox("panel", "HudPanel", style(Color(0.05, 0.06, 0.09, 0.72), 12, BORDER, 1, 14))

	# Labels
	for v in [["H1", 30, true, TEXT], ["H2", 22, true, TEXT], ["H3", 18, true, TEXT],
			["Muted", 14, false, MUTED], ["Small", 13, false, MUTED], ["Big", 34, true, TEXT],
			["AccentLabel", 15, true, ACCENT], ["Title", 64, true, TEXT]]:
		t.set_type_variation(v[0], "Label")
		t.set_font_size("font_size", v[0], v[1])
		t.set_font("font", v[0], font(v[2]))
		t.set_color("font_color", v[0], v[3])

	# Eingabe
	var le := style(Color(0.06, 0.07, 0.1), 8, Color(1, 1, 1, 0.14), 1, 10)
	t.set_stylebox("normal", "LineEdit", le)
	t.set_stylebox("focus", "LineEdit", style(Color(0.06, 0.07, 0.1), 8, ACCENT, 2, 10))
	t.set_color("font_color", "LineEdit", TEXT)
	t.set_color("caret_color", "LineEdit", ACCENT)

	# Fortschrittsbalken
	t.set_stylebox("background", "ProgressBar", style(Color(1, 1, 1, 0.08), 6, Color(0, 0, 0, 0), 0, 0))
	t.set_stylebox("fill", "ProgressBar", style(ACCENT, 6, Color(0, 0, 0, 0), 0, 0))
	t.set_color("font_color", "ProgressBar", TEXT)

	# Slider
	var track := style(Color(1, 1, 1, 0.12), 4, Color(0, 0, 0, 0), 0, 0)
	track.content_margin_top = 3
	track.content_margin_bottom = 3
	t.set_stylebox("slider", "HSlider", track)
	var fill := style(ACCENT, 4, Color(0, 0, 0, 0), 0, 0)
	fill.content_margin_top = 3
	fill.content_margin_bottom = 3
	t.set_stylebox("grabber_area", "HSlider", fill)
	t.set_stylebox("grabber_area_highlight", "HSlider", fill)

	# Scrollbalken (schmal)
	var sb := style(Color(1, 1, 1, 0.03), 4, Color(0, 0, 0, 0), 0, 0)
	var gr := style(Color(1, 1, 1, 0.18), 4, Color(0, 0, 0, 0), 0, 0)
	var grh := style(Color(1, 1, 1, 0.3), 4, Color(0, 0, 0, 0), 0, 0)
	for sc in ["VScrollBar", "HScrollBar"]:
		t.set_stylebox("scroll", sc, sb)
		t.set_stylebox("grabber", sc, gr)
		t.set_stylebox("grabber_highlight", sc, grh)
		t.set_stylebox("grabber_pressed", sc, grh)

	var sep := StyleBoxLine.new()
	sep.color = Color(1, 1, 1, 0.08)
	sep.thickness = 1
	t.set_stylebox("separator", "HSeparator", sep)
	t.set_constant("separation", "HSeparator", 10)

	t.set_stylebox("panel", "TooltipPanel", style(Color(0.05, 0.06, 0.08, 0.98), 6, BORDER, 1, 8))
	t.set_color("font_color", "TooltipLabel", TEXT)

	t.set_color("font_color", "CheckButton", TEXT)
	t.set_color("font_hover_color", "CheckButton", ACCENT)
	t.set_color("font_color", "CheckBox", TEXT)

	t.set_color("default_color", "RichTextLabel", TEXT)
	t.set_font("bold_font", "RichTextLabel", font(true))
	_theme = t
	return t

static func _button_styles(t: Theme, type: String, normal: Color, hover: Color, pressed: Color, fc: Color, fpc: Color) -> void:
	t.set_stylebox("normal", type, style(normal, 8, BORDER, 1 if normal.a > 0.5 else 0, 12))
	t.set_stylebox("hover", type, style(hover, 8, Color(1, 1, 1, 0.14), 1, 12))
	t.set_stylebox("pressed", type, style(pressed, 8, Color(0, 0, 0, 0), 0, 12))
	t.set_stylebox("disabled", type, style(Color(0.12, 0.13, 0.17, 0.7), 8, Color(0, 0, 0, 0), 0, 12))
	t.set_stylebox("focus", type, StyleBoxEmpty.new())
	t.set_color("font_color", type, fc)
	t.set_color("font_hover_color", type, fc)
	t.set_color("font_pressed_color", type, fpc)
	t.set_color("font_focus_color", type, fc)
	t.set_color("font_disabled_color", type, Color(0.5, 0.53, 0.6))

# ---- Formatierung -----------------------------------------------------------------------
static func money(v: int) -> String:
	var neg := v < 0
	var s := str(absi(v))
	var out := ""
	var count := 0
	for i in range(s.length() - 1, -1, -1):
		out = s[i] + out
		count += 1
		if count % 3 == 0 and i > 0:
			out = "." + out
	return ("−" if neg else "") + out + " €"

static func stars(r: float) -> String:
	var full := int(round(r))
	var s := ""
	for i in 5:
		s += "★" if i < full else "☆"
	return s

static func rating_text(r: float) -> String:
	return ("%.1f" % r).replace(".", ",")

static func clock(minutes: float) -> String:
	var m := int(minutes)
	return "%02d:%02d" % [int(m / 60) % 24, m % 60]

static func pct(v: float) -> String:
	return ("%+.1f %%" % (v * 100.0)).replace(".", ",")

static func clear_cache() -> void:
	_theme = null
	_font = null
	_font_bold = null
