extends Node3D
class_name NPC
## Einfache Nicht-Spieler-Figur: läuft Wegpunkte ab (oder steht), animiert Beine/Arme,
## kann Sprechblasen zeigen. Keine Physik - reine Deko/Atmosphäre.

var model: Node3D
var waypoints: Array[Vector3] = []
var speed: float = 1.4
var pause_at_points: float = 0.0
var ping_pong: bool = false
var look_at_target: Node3D = null

var _idx: int = 0
var _dir: int = 1
var _wait: float = 0.0
var _phase: float = 0.0
var _t: float = 0.0
var _walk_amount: float = 0.0
var _bubble: Label3D
var _bubble_time: float = 0.0

func setup(look: Dictionary, points: Array = [], p_speed: float = 1.4) -> void:
	model = CharacterKit.build(look)
	add_child(model)
	for p in points:
		waypoints.append(p)
	speed = p_speed
	_t = randf() * 10.0
	_bubble = Props.label("", 44, Color(1, 1, 1), Vector3(0, 2.25, 0), true, 10)
	_bubble.visible = false
	_bubble.width = 420
	_bubble.autowrap_mode = TextServer.AUTOWRAP_WORD
	add_child(_bubble)
	if not waypoints.is_empty():
		position = waypoints[0]

func say(text: String, duration: float = 4.0) -> void:
	_bubble.text = text
	_bubble.visible = true
	_bubble_time = duration

func _process(delta: float) -> void:
	_t += delta
	if _bubble_time > 0.0:
		_bubble_time -= delta
		if _bubble_time <= 0.0:
			_bubble.visible = false
	var moving := false
	if waypoints.size() > 1:
		if _wait > 0.0:
			_wait -= delta
		else:
			var target := waypoints[_idx]
			var to := target - position
			to.y = 0.0
			var dist := to.length()
			if dist < 0.08:
				_next_point()
				_wait = pause_at_points
			else:
				var step := minf(dist, speed * delta)
				position += to / dist * step
				rotation.y = lerp_angle(rotation.y, atan2(to.x, to.z), minf(1.0, delta * 8.0))
				moving = true
	elif look_at_target != null and is_instance_valid(look_at_target):
		var d := look_at_target.global_position - global_position
		if d.length() < 6.0:
			rotation.y = lerp_angle(rotation.y, atan2(d.x, d.z), minf(1.0, delta * 3.0))
	_walk_amount = move_toward(_walk_amount, 1.0 if moving else 0.0, delta * 4.0)
	_phase += delta * speed * 4.2 * _walk_amount
	CharacterKit.animate(model, _phase, _walk_amount, _t)

func _next_point() -> void:
	if ping_pong:
		if _idx + _dir >= waypoints.size() or _idx + _dir < 0:
			_dir = -_dir
		_idx += _dir
	else:
		_idx = (_idx + 1) % waypoints.size()
