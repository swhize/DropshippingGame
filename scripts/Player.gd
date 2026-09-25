extends CharacterBody3D
## Ego-Perspektive: Bewegung, Blick, Zielen per Fadenkreuz (Raycast) mit Hervorhebung,
## Tragen mit sichtbaren Armen, Ablegen mit Physik, Kopfwippen und Schrittgeräusche.

const SPEED := 4.6
const SPRINT_SPEED := 7.2
const CROUCH_SPEED := 2.2
const JUMP_VELOCITY := 5.2
const GRAVITY := 16.0
const MOUSE_SENSITIVITY := 0.0026
const PITCH_LIMIT := deg_to_rad(86)
const STAND_HEIGHT := 1.8
const CROUCH_HEIGHT := 1.1
const STAND_CAM_Y := 1.62
const CROUCH_CAM_Y := 0.95
const REACH := 3.2

var camera: Camera3D
var ray: RayCast3D
var hand_anchor: Node3D
var collision_shape: CollisionShape3D
var capsule_shape: CapsuleShape3D

var held_kind: int = GameManager.ItemKind.NONE
var held: Dictionary = {}
var held_visual: Node3D = null

var focus: Node = null
var prompt: String = ""

var _bob_t: float = 0.0
var _step_acc: float = 0.0
var _cam_base_y: float = STAND_CAM_Y
var _sway: Vector2 = Vector2.ZERO
var _was_on_floor: bool = true

func _ready() -> void:
	add_to_group("player")
	collision_shape = CollisionShape3D.new()
	capsule_shape = CapsuleShape3D.new()
	capsule_shape.radius = 0.32
	capsule_shape.height = STAND_HEIGHT
	collision_shape.shape = capsule_shape
	collision_shape.position.y = STAND_HEIGHT / 2.0
	add_child(collision_shape)
	camera = Camera3D.new()
	camera.position = Vector3(0, STAND_CAM_Y, 0)
	camera.current = true
	camera.near = 0.05
	add_child(camera)
	ray = RayCast3D.new()
	ray.target_position = Vector3(0, 0, -REACH)
	ray.collide_with_areas = true
	ray.collide_with_bodies = true
	ray.add_exception(self)
	camera.add_child(ray)
	hand_anchor = Node3D.new()
	camera.add_child(hand_anchor)
	_apply_settings()
	Settings.changed.connect(_apply_settings)

func _apply_settings() -> void:
	camera.fov = Settings.fov

func is_empty() -> bool:
	return held_kind == GameManager.ItemKind.NONE

func hold(kind: int, data: Dictionary = {}) -> void:
	held_kind = kind
	held = data.duplicate(true)
	_rebuild_held_visual()
	GameManager.held_item_changed.emit(held_kind, held)

func clear_hands() -> void:
	held_kind = GameManager.ItemKind.NONE
	held = {}
	_rebuild_held_visual()
	GameManager.held_item_changed.emit(held_kind, held)

# ---- Gehaltener Gegenstand mit Armen ------------------------------------------------------
func _rebuild_held_visual() -> void:
	if held_visual:
		held_visual.queue_free()
		held_visual = null
	if held_kind == GameManager.ItemKind.NONE:
		return
	held_visual = Node3D.new()
	hand_anchor.add_child(held_visual)
	var size := ItemKit.bounds(held_kind, held)
	var two_hands := held_kind == GameManager.ItemKind.CRATE or held_kind == GameManager.ItemKind.PACKAGE or held_kind == GameManager.ItemKind.LABELED
	var item_pos := Vector3(0.0, -0.36, -0.72) if two_hands else Vector3(0.26, -0.3, -0.55)
	if held_kind == GameManager.ItemKind.CRATE:
		item_pos = Vector3(0.0, -0.5, -0.8)
	var item := ItemKit.build(held_kind, held, false)
	item.position = item_pos
	item.rotation_degrees = Vector3(0, -8 if two_hands else -20, 0)
	for m in _meshes_in(item):
		m.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	held_visual.add_child(item)
	var sleeve := Mats.std(Color(0.2, 0.22, 0.28), 0.8)
	var skin := Mats.std(Color(0.93, 0.76, 0.62), 0.7)
	if two_hands:
		for side in [-1.0, 1.0]:
			var hand := item_pos + Vector3(side * (size.x / 2.0 + 0.03), -size.y * 0.15, 0.02)
			var shoulder := Vector3(side * 0.34, -0.72, -0.08)
			_arm(shoulder, hand, sleeve, skin)
	else:
		_arm(Vector3(0.36, -0.7, -0.05), item_pos + Vector3(0.04, -size.y * 0.5 - 0.03, 0.03), sleeve, skin)

func _arm(from: Vector3, to: Vector3, sleeve: Material, skin: Material) -> void:
	var dir := to - from
	var length := dir.length()
	var arm := Node3D.new()
	arm.position = (from + to) / 2.0
	held_visual.add_child(arm)
	arm.basis = Basis.looking_at(dir.normalized(), Vector3.UP)
	var fore := Props.box(Vector3(0.1, 0.1, length), sleeve, Vector3.ZERO)
	fore.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	arm.add_child(fore)
	var hand := Props.box(Vector3(0.09, 0.07, 0.11), skin, Vector3(0, 0, -length / 2.0 - 0.03))
	hand.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	arm.add_child(hand)

func _meshes_in(n: Node) -> Array:
	var out: Array = []
	if n is MeshInstance3D:
		out.append(n)
	for c in n.get_children():
		out.append_array(_meshes_in(c))
	return out

# ---- Ablegen -------------------------------------------------------------------------------
func drop_held_item() -> void:
	if is_empty():
		return
	var item := DroppedItem.new()
	var fwd := -camera.global_transform.basis.z
	var pos := camera.global_position + fwd * 0.7 + Vector3(0, -0.35, 0)
	get_parent().add_child(item)
	item.global_position = pos
	item.rotation.y = rotation.y
	item.setup(held_kind, held)
	item.linear_velocity = fwd * 2.2 + velocity * 0.5 + Vector3(0, 0.8, 0)
	Audio.play("drop")
	clear_hands()

func spawn_dropped(kind: int, data: Dictionary, pos: Vector3, rot_y: float) -> void:
	var item := DroppedItem.new()
	get_parent().add_child(item)
	item.global_position = pos + Vector3(0, 0.05, 0)
	item.rotation.y = rot_y
	item.setup(kind, data)

# ---- Eingabe & Bewegung --------------------------------------------------------------------
func _unhandled_input(event: InputEvent) -> void:
	if GameManager.is_input_locked():
		return
	if event is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		var sens := MOUSE_SENSITIVITY * Settings.mouse_sensitivity
		rotate_y(-event.relative.x * sens)
		camera.rotation.x = clampf(camera.rotation.x - event.relative.y * sens, -PITCH_LIMIT, PITCH_LIMIT)
		_sway += Vector2(event.relative.x, event.relative.y) * 0.0006

func _physics_process(delta: float) -> void:
	var locked := GameManager.is_input_locked()
	var input_dir := Vector3.ZERO
	var crouching := false
	var sprinting := false
	if not locked:
		if Input.is_action_pressed("move_forward"):
			input_dir -= transform.basis.z
		if Input.is_action_pressed("move_back"):
			input_dir += transform.basis.z
		if Input.is_action_pressed("move_left"):
			input_dir -= transform.basis.x
		if Input.is_action_pressed("move_right"):
			input_dir += transform.basis.x
		crouching = Input.is_action_pressed("crouch")
		sprinting = Input.is_action_pressed("sprint") and not crouching
	input_dir.y = 0.0
	input_dir = input_dir.normalized()

	capsule_shape.height = CROUCH_HEIGHT if crouching else STAND_HEIGHT
	collision_shape.position.y = capsule_shape.height / 2.0
	_cam_base_y = lerpf(_cam_base_y, CROUCH_CAM_Y if crouching else STAND_CAM_Y, 10.0 * delta)

	var speed := SPEED
	if crouching:
		speed = CROUCH_SPEED
	elif sprinting:
		speed = SPRINT_SPEED
	if held_kind == GameManager.ItemKind.CRATE:
		speed *= 0.85
	var target := input_dir * speed
	var accel := 12.0 if is_on_floor() else 3.0
	velocity.x = move_toward(velocity.x, target.x, accel * speed * delta)
	velocity.z = move_toward(velocity.z, target.z, accel * speed * delta)
	if not is_on_floor():
		velocity.y -= GRAVITY * delta
	elif not locked and Input.is_action_just_pressed("jump"):
		velocity.y = JUMP_VELOCITY
	move_and_slide()
	_push_bodies()

	# Landung + Kopfwippen + Schritte
	if is_on_floor() and not _was_on_floor:
		Audio.play("step", 0.1, -6.0)
	_was_on_floor = is_on_floor()
	var hspeed := Vector2(velocity.x, velocity.z).length()
	var moving := is_on_floor() and hspeed > 0.6
	if moving:
		_bob_t += delta * hspeed * 1.9
		_step_acc += delta * hspeed
		if _step_acc >= 1.55:
			_step_acc = 0.0
			Audio.play("step", 0.15, -15.0)
	var bob := sin(_bob_t) * 0.035 if (moving and Settings.head_bob) else 0.0
	camera.position.y = _cam_base_y + bob
	camera.position.x = cos(_bob_t * 0.5) * 0.02 if (moving and Settings.head_bob) else 0.0
	_sway = _sway.lerp(Vector2.ZERO, minf(1.0, delta * 8.0))
	hand_anchor.position = Vector3(-_sway.x * 0.6, _sway.y * 0.6 + bob * 0.5, 0)

	_update_focus()
	if locked:
		return
	if Input.is_action_just_pressed("interact") and focus != null and is_instance_valid(focus):
		focus.interact(self)
	if Input.is_action_just_pressed("drop"):
		drop_held_item()

func _push_bodies() -> void:
	for i in get_slide_collision_count():
		var c := get_slide_collision(i)
		var body := c.get_collider()
		if body is RigidBody3D:
			body.apply_central_impulse(-c.get_normal() * 0.6)

func _update_focus() -> void:
	var target: Node = null
	if not GameManager.is_input_locked() and ray.is_colliding():
		var n: Node = ray.get_collider()
		for i in 3:
			if n == null:
				break
			if n.has_method("interact"):
				target = n
				break
			n = n.get_parent()
	if target != focus:
		if focus != null and is_instance_valid(focus) and focus.has_method("set_highlighted"):
			focus.set_highlighted(false)
		focus = target
		if focus != null and focus.has_method("set_highlighted"):
			focus.set_highlighted(true)
	if focus != null and is_instance_valid(focus) and focus.has_method("get_prompt"):
		prompt = focus.get_prompt(self)
	else:
		focus = null
		prompt = ""
