extends Control
## Zeichnet eine Vorschau des bedruckten Versandkartons mit Logo, Farbe und Markennamen.

var logo_index: int = 0
var color: Color = Color.RED
var brand: String = "MeinShop"
var small: bool = false

func _init() -> void:
	custom_minimum_size = Vector2(300, 220)

func setup(p_logo: int, p_color: Color, p_brand: String, p_small: bool = false) -> void:
	logo_index = p_logo
	color = p_color
	brand = p_brand
	small = p_small
	custom_minimum_size = Vector2(46, 46) if small else Vector2(300, 220)
	queue_redraw()

func _draw() -> void:
	if small:
		draw_logo(self, logo_index, size / 2.0, minf(size.x, size.y) * 0.36, color)
		return
	var box := Rect2(Vector2(30, 40), Vector2(240, 160))
	var top := PackedVector2Array([box.position, box.position + Vector2(40, -30), box.position + Vector2(box.size.x + 40, -30), box.position + Vector2(box.size.x, 0)])
	draw_colored_polygon(top, Color(0.8, 0.64, 0.44))
	var side := PackedVector2Array([box.position + Vector2(box.size.x, 0), box.position + Vector2(box.size.x + 40, -30), box.end + Vector2(40, -30), box.end])
	draw_colored_polygon(side, Color(0.58, 0.43, 0.27))
	draw_rect(box, Color(0.7, 0.53, 0.34))
	draw_rect(Rect2(box.position + Vector2(0, box.size.y - 30), Vector2(box.size.x, 22)), color)
	draw_rect(Rect2(box.position + Vector2(box.size.x * 0.44, 0), Vector2(box.size.x * 0.12, box.size.y)), Color(0.85, 0.75, 0.52, 0.8))
	draw_logo(self, logo_index, box.position + Vector2(64, 62), 32.0, color)
	var font := UITheme.font(true)
	draw_string(font, box.position + Vector2(112, 72), brand, HORIZONTAL_ALIGNMENT_LEFT, 124, 20, Color(0.12, 0.1, 0.08))
	draw_string(UITheme.font(false), box.position + Vector2(112, 94), "Mit Liebe verpackt ♥", HORIZONTAL_ALIGNMENT_LEFT, 124, 11, Color(0.2, 0.16, 0.12))

static func draw_logo(ci: CanvasItem, index: int, c: Vector2, r: float, col: Color) -> void:
	match index:
		0:
			ci.draw_circle(c, r, col)
		1:
			ci.draw_rect(Rect2(c - Vector2(r, r) * 0.85, Vector2(r, r) * 1.7), col)
		2:
			ci.draw_colored_polygon(PackedVector2Array([c + Vector2(0, -r), c + Vector2(r, 0), c + Vector2(0, r), c + Vector2(-r, 0)]), col)
		3:
			var pts := PackedVector2Array()
			for i in 10:
				var ang := -PI / 2.0 + i * PI / 5.0
				var rr := r if i % 2 == 0 else r * 0.45
				pts.append(c + Vector2(cos(ang), sin(ang)) * rr)
			ci.draw_colored_polygon(pts, col)
		4:
			ci.draw_colored_polygon(PackedVector2Array([c + Vector2(0.2, -1.0) * r, c + Vector2(-0.55, 0.15) * r, c + Vector2(-0.05, 0.15) * r,
				c + Vector2(-0.2, 1.0) * r, c + Vector2(0.55, -0.15) * r, c + Vector2(0.05, -0.15) * r]), col)
		_:
			ci.draw_circle(c + Vector2(-0.45, -0.25) * r, r * 0.5, col)
			ci.draw_circle(c + Vector2(0.45, -0.25) * r, r * 0.5, col)
			ci.draw_colored_polygon(PackedVector2Array([c + Vector2(-0.93, -0.05) * r, c + Vector2(0.93, -0.05) * r, c + Vector2(0, 0.95) * r]), col)
