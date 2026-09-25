extends Control
## TikTok-Minispiel als gezeichnetes Handy: Aufnahme starten, Markierung pendelt über den
## Balken, im richtigen Moment stoppen. Die Trefferquote bestimmt Stärke und Dauer des Trends.

signal posted(score: float)

const IDEAS := ["POV: Du packst um 3 Uhr nachts Pakete 📦", "UNBOXING GONE WRONG 😱",
	"Life-Hack: Das hier braucht JEDER 🔥", "Mein Chef (ich) gibt mir frei 😎",
	"Wie ich mit 19 ... okay, noch nicht 😅", "Kunde bestellt 1x – ich liefere mit Liebe 💌"]

var state: String = "idle"
var marker: float = 0.0
var dir: float = 1.0
var speed: float = 0.9
var zone_a: float = 0.4
var zone_b: float = 0.6
var score: float = 0.0
var idea: String = ""
var _t: float = 0.0
var button: Button

func _init() -> void:
	custom_minimum_size = Vector2(250, 430)

func _ready() -> void:
	idea = IDEAS[randi() % IDEAS.size()]
	button = Button.new()
	button.theme_type_variation = "AccentButton"
	button.position = Vector2(35, 360)
	button.size = Vector2(180, 42)
	button.pressed.connect(_on_button)
	add_child(button)
	_update_button()

func is_busy() -> bool:
	return state == "rec" or state == "done"

func _update_button() -> void:
	match state:
		"idle":
			button.text = "● Aufnahme starten"
		"rec":
			button.text = "■ STOPP!"
		"done":
			button.text = "Posten 🚀"

func _on_button() -> void:
	match state:
		"idle":
			state = "rec"
			marker = 0.0
			dir = 1.0
			speed = randf_range(0.8, 1.15)
			zone_a = randf_range(0.2, 0.62)
			zone_b = zone_a + randf_range(0.14, 0.2)
			Audio.play("click")
		"rec":
			state = "done"
			var mid := (zone_a + zone_b) / 2.0
			var half := (zone_b - zone_a) / 2.0
			var dist := absf(marker - mid)
			score = 1.0 if dist <= half * 0.35 else clampf(1.0 - (dist - half * 0.35) / (half + 0.28), 0.05, 1.0)
			Audio.play("notify" if score > 0.6 else "click")
		"done":
			state = "posted"
			posted.emit(score)
			Audio.play("levelup", 0.0, -6.0)
	_update_button()
	queue_redraw()

func _process(delta: float) -> void:
	_t += delta
	if state == "rec":
		marker += dir * speed * delta
		speed += delta * 0.08
		if marker >= 1.0:
			marker = 1.0
			dir = -1.0
		elif marker <= 0.0:
			marker = 0.0
			dir = 1.0
	queue_redraw()

func _draw() -> void:
	var font := UITheme.font(true)
	var body := Rect2(Vector2(10, 0), Vector2(230, 420))
	draw_style_box(UITheme.style(Color(0.06, 0.06, 0.07), 28, Color(0.35, 0.36, 0.4), 3, 0), body)
	var scr := Rect2(Vector2(22, 14), Vector2(206, 330))
	var sb := StyleBoxFlat.new()
	sb.set_corner_radius_all(18)
	sb.bg_color = Color(0.1, 0.05, 0.16)
	draw_style_box(sb, scr)
	for i in 14:
		var y := scr.position.y + 20 + i * 22
		var a := 0.04 + 0.03 * sin(_t * 2.0 + i)
		draw_rect(Rect2(scr.position.x + 8, y, scr.size.x - 16, 12), Color(0.9, 0.2, 0.6, a))
	draw_string(font, Vector2(34, 42), "♪ TikTok", HORIZONTAL_ALIGNMENT_LEFT, -1, 18, Color(1, 1, 1))
	if state == "rec":
		var blink := 0.5 + 0.5 * sin(_t * 8.0)
		draw_circle(Vector2(208, 36), 6, Color(1, 0.15, 0.2, blink))
		draw_string(font, Vector2(150, 42), "REC", HORIZONTAL_ALIGNMENT_LEFT, -1, 14, Color(1, 0.3, 0.3))
	draw_string(UITheme.font(false), Vector2(34, 90), idea, HORIZONTAL_ALIGNMENT_LEFT, 185, 15, Color(1, 1, 1, 0.9))
	var bar := Rect2(Vector2(36, 250), Vector2(178, 26))
	draw_rect(bar, Color(1, 1, 1, 0.1))
	draw_rect(Rect2(bar.position.x + bar.size.x * zone_a, bar.position.y, bar.size.x * (zone_b - zone_a), bar.size.y), Color(0.3, 0.95, 0.6, 0.55))
	var mx := bar.position.x + bar.size.x * marker
	draw_rect(Rect2(mx - 3, bar.position.y - 6, 6, bar.size.y + 12), Color(1, 1, 1))
	match state:
		"idle":
			draw_string(font, Vector2(34, 230), "Stopp im grünen Bereich!", HORIZONTAL_ALIGNMENT_LEFT, 190, 14, Color(1, 1, 1, 0.85))
		"rec":
			draw_string(font, Vector2(34, 230), "Jetzt! ... oder jetzt?", HORIZONTAL_ALIGNMENT_LEFT, 190, 14, Color(1, 1, 1, 0.85))
		"done", "posted":
			var views := int(pow(score, 2.0) * 1800000.0 + 1200.0)
			draw_string(font, Vector2(34, 205), "Treffer: %d %%" % int(score * 100), HORIZONTAL_ALIGNMENT_LEFT, -1, 20, UITheme.ACCENT)
			draw_string(UITheme.font(false), Vector2(34, 230), "Prognose: %s Views" % _num(views), HORIZONTAL_ALIGNMENT_LEFT, -1, 14, Color(1, 1, 1, 0.85))
	if state == "posted":
		draw_string(font, Vector2(34, 320), "Gepostet! ✓", HORIZONTAL_ALIGNMENT_LEFT, -1, 18, UITheme.GOOD)

func _num(v: int) -> String:
	if v >= 1000000:
		return ("%.1f Mio." % (v / 1000000.0)).replace(".", ",")
	if v >= 1000:
		return "%d Tsd." % int(v / 1000)
	return str(v)
