class_name LineChart
extends Control
## Einfaches Liniendiagramm mit Gitter, Füllung unter der ersten Linie und Endwert-Label.

var series: Array = []      # [{values: Array, color: Color, label: String}]
var value_suffix: String = ""
var include_zero: bool = false
var decimals: int = 0

func _init() -> void:
	custom_minimum_size = Vector2(300, 190)
	size_flags_horizontal = Control.SIZE_EXPAND_FILL

func set_series(s: Array) -> void:
	series = s
	queue_redraw()

func _fmt(v: float) -> String:
	if decimals > 0:
		return ("%." + str(decimals) + "f") % v + value_suffix
	return str(int(round(v))) + value_suffix

func _draw() -> void:
	var font := UITheme.font(false)
	var r := Rect2(Vector2.ZERO, size)
	draw_rect(r, Color(0.05, 0.06, 0.08, 0.8))
	var pad_l := 54.0
	var pad_r := 60.0
	var pad_t := 12.0
	var pad_b := 22.0
	var area := Rect2(pad_l, pad_t, size.x - pad_l - pad_r, size.y - pad_t - pad_b)
	var lo := INF
	var hi := -INF
	var count := 0
	for s in series:
		var vals: Array = s["values"]
		count = maxi(count, vals.size())
		for v in vals:
			lo = minf(lo, float(v))
			hi = maxf(hi, float(v))
	if count < 2:
		draw_string(font, Vector2(pad_l, size.y / 2.0), "Noch keine Daten", HORIZONTAL_ALIGNMENT_LEFT, -1, 14, UITheme.MUTED)
		return
	if include_zero:
		lo = minf(lo, 0.0)
		hi = maxf(hi, 0.0)
	if absf(hi - lo) < 0.0001:
		hi += 1.0
		lo -= 1.0
	var span := hi - lo
	lo -= span * 0.06
	hi += span * 0.06
	for i in 5:
		var y := area.position.y + area.size.y * i / 4.0
		draw_line(Vector2(area.position.x, y), Vector2(area.end.x, y), Color(1, 1, 1, 0.06), 1.0)
		var val := hi - (hi - lo) * i / 4.0
		draw_string(font, Vector2(4, y + 5), _fmt(val), HORIZONTAL_ALIGNMENT_LEFT, pad_l - 8, 12, UITheme.MUTED)
	if include_zero and lo < 0.0 and hi > 0.0:
		var zy := area.position.y + area.size.y * (hi / (hi - lo))
		draw_line(Vector2(area.position.x, zy), Vector2(area.end.x, zy), Color(1, 1, 1, 0.2), 1.0)
	for si in series.size():
		var s: Dictionary = series[si]
		var vals: Array = s["values"]
		if vals.size() < 2:
			continue
		var col: Color = s["color"]
		var pts := PackedVector2Array()
		for i in vals.size():
			var x := area.position.x + area.size.x * float(i) / float(vals.size() - 1)
			var y := area.position.y + area.size.y * (1.0 - (float(vals[i]) - lo) / (hi - lo))
			pts.append(Vector2(x, y))
		if si == 0:
			var poly := PackedVector2Array(pts)
			poly.append(Vector2(area.end.x, area.end.y))
			poly.append(Vector2(area.position.x, area.end.y))
			draw_colored_polygon(poly, Color(col.r, col.g, col.b, 0.12))
		draw_polyline(pts, col, 2.2, true)
		var last := pts[pts.size() - 1]
		draw_circle(last, 4.0, col)
		draw_string(font, last + Vector2(8, 5), _fmt(float(vals[vals.size() - 1])), HORIZONTAL_ALIGNMENT_LEFT, pad_r - 8, 13, col)
		if s.has("label"):
			draw_string(font, Vector2(area.position.x + 8 + si * 130, size.y - 5), "● " + String(s["label"]), HORIZONTAL_ALIGNMENT_LEFT, -1, 12, col)
