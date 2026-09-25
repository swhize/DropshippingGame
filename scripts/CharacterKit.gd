class_name CharacterKit
extends RefCounted
## Low-Poly-Figuren (Schedule-I-Stil) aus Boxen: Beine mit Knie, Arme, Kopf, Haare,
## optional Schürze / Warnweste / Kappe / Schnurrbart. Die Gelenk-Knoten werden als
## Metadaten gespeichert, damit NPC.gd sie animieren kann. Blickrichtung ist +Z.

const SKIN_TONES := [Color(0.95, 0.8, 0.66), Color(0.87, 0.68, 0.52), Color(0.72, 0.52, 0.38), Color(0.5, 0.35, 0.25)]
const HAIR := [Color(0.12, 0.09, 0.07), Color(0.35, 0.22, 0.12), Color(0.75, 0.6, 0.35), Color(0.55, 0.2, 0.1), Color(0.6, 0.6, 0.62)]
const SHIRTS := [Color(0.2, 0.35, 0.65), Color(0.7, 0.2, 0.22), Color(0.25, 0.55, 0.35), Color(0.85, 0.75, 0.3),
	Color(0.4, 0.4, 0.45), Color(0.6, 0.35, 0.65), Color(0.9, 0.9, 0.9), Color(0.15, 0.15, 0.17)]
const PANTS := [Color(0.18, 0.22, 0.35), Color(0.2, 0.2, 0.22), Color(0.4, 0.33, 0.25), Color(0.3, 0.32, 0.38)]

static func random_look(rng: RandomNumberGenerator) -> Dictionary:
	return {
		"skin": SKIN_TONES[rng.randi_range(0, SKIN_TONES.size() - 1)],
		"hair": HAIR[rng.randi_range(0, HAIR.size() - 1)],
		"shirt": SHIRTS[rng.randi_range(0, SHIRTS.size() - 1)],
		"pants": PANTS[rng.randi_range(0, PANTS.size() - 1)],
		"long_hair": rng.randf() < 0.4,
	}

static func build(o: Dictionary) -> Node3D:
	var root := Node3D.new()
	var skin := Mats.std(o.get("skin", SKIN_TONES[0]), 0.7)
	var shirt := Mats.std(o.get("shirt", SHIRTS[0]), 0.8)
	var pants := Mats.std(o.get("pants", PANTS[0]), 0.85)
	var hair := Mats.std(o.get("hair", HAIR[0]), 0.9)
	var shoe := Mats.std(Color(0.1, 0.1, 0.11), 0.6)

	for side in [-1.0, 1.0]:
		var hip := Node3D.new()
		hip.position = Vector3(0.1 * side, 0.92, 0)
		root.add_child(hip)
		hip.add_child(Props.box(Vector3(0.16, 0.46, 0.18), pants, Vector3(0, -0.23, 0)))
		var knee := Node3D.new()
		knee.position = Vector3(0, -0.46, 0)
		hip.add_child(knee)
		knee.add_child(Props.box(Vector3(0.15, 0.42, 0.16), pants, Vector3(0, -0.21, 0)))
		knee.add_child(Props.box(Vector3(0.16, 0.08, 0.27), shoe, Vector3(0, -0.43, 0.04)))
		root.set_meta("hip_l" if side < 0 else "hip_r", hip)
		root.set_meta("knee_l" if side < 0 else "knee_r", knee)

	var torso := Node3D.new()
	torso.position = Vector3(0, 0.92, 0)
	root.add_child(torso)
	root.set_meta("torso", torso)
	torso.add_child(Props.box(Vector3(0.44, 0.62, 0.25), shirt, Vector3(0, 0.31, 0)))
	if o.get("apron", false):
		torso.add_child(Props.box(Vector3(0.42, 0.72, 0.02), Mats.std(Color(0.95, 0.95, 0.93), 0.8), Vector3(0, 0.18, 0.135)))
	if o.get("vest", false):
		torso.add_child(Props.box(Vector3(0.46, 0.5, 0.27), Mats.std(Color(1.0, 0.55, 0.1), 0.6), Vector3(0, 0.36, 0)))
		torso.add_child(Props.box(Vector3(0.47, 0.05, 0.28), Mats.emit(Color(0.9, 0.95, 0.9), 0.6), Vector3(0, 0.3, 0)))

	for side in [-1.0, 1.0]:
		var shoulder := Node3D.new()
		shoulder.position = Vector3(0.29 * side, 0.58, 0)
		torso.add_child(shoulder)
		shoulder.add_child(Props.box(Vector3(0.12, 0.58, 0.14), shirt, Vector3(0, -0.29, 0)))
		shoulder.add_child(Props.box(Vector3(0.11, 0.11, 0.12), skin, Vector3(0, -0.63, 0)))
		root.set_meta("arm_l" if side < 0 else "arm_r", shoulder)

	var head := Node3D.new()
	head.position = Vector3(0, 0.7, 0)
	torso.add_child(head)
	root.set_meta("head", head)
	head.add_child(Props.box(Vector3(0.1, 0.08, 0.1), skin, Vector3(0, 0.02, 0)))
	head.add_child(Props.box(Vector3(0.27, 0.29, 0.26), skin, Vector3(0, 0.2, 0)))
	var eye := Mats.std(Color(0.08, 0.08, 0.1), 0.4)
	head.add_child(Props.box(Vector3(0.04, 0.045, 0.01), eye, Vector3(-0.065, 0.23, 0.131)))
	head.add_child(Props.box(Vector3(0.04, 0.045, 0.01), eye, Vector3(0.065, 0.23, 0.131)))
	head.add_child(Props.box(Vector3(0.28, 0.08, 0.28), hair, Vector3(0, 0.37, -0.005)))
	head.add_child(Props.box(Vector3(0.28, 0.16, 0.06), hair, Vector3(0, 0.28, -0.12)))
	if o.get("long_hair", false):
		head.add_child(Props.box(Vector3(0.3, 0.32, 0.08), hair, Vector3(0, 0.16, -0.13)))
	if o.get("mustache", false):
		head.add_child(Props.box(Vector3(0.14, 0.035, 0.02), hair, Vector3(0, 0.14, 0.135)))
	if o.get("cap", false):
		var cap := Mats.std(o.get("cap_color", Color(0.85, 0.2, 0.2)), 0.7)
		head.add_child(Props.box(Vector3(0.29, 0.09, 0.29), cap, Vector3(0, 0.39, 0)))
		head.add_child(Props.box(Vector3(0.24, 0.02, 0.14), cap, Vector3(0, 0.35, 0.19)))

	if o.get("sitting", false):
		set_sitting(root, true)
	return root

static func set_sitting(root: Node3D, sitting: bool) -> void:
	for side in ["l", "r"]:
		var hip: Node3D = root.get_meta("hip_" + side)
		var knee: Node3D = root.get_meta("knee_" + side)
		hip.rotation.x = -PI / 2.0 if sitting else 0.0
		knee.rotation.x = PI / 2.0 if sitting else 0.0
	root.position.y = -0.46 if sitting else 0.0

## Lauf-/Idle-Animation. phase läuft mit der Zeit, amount 0 = stehen, 1 = voll laufen.
static func animate(root: Node3D, phase: float, amount: float, t: float) -> void:
	if not root.has_meta("hip_l"):
		return
	var swing := sin(phase) * 0.55 * amount
	var hip_l: Node3D = root.get_meta("hip_l")
	var hip_r: Node3D = root.get_meta("hip_r")
	var knee_l: Node3D = root.get_meta("knee_l")
	var knee_r: Node3D = root.get_meta("knee_r")
	var arm_l: Node3D = root.get_meta("arm_l")
	var arm_r: Node3D = root.get_meta("arm_r")
	var torso: Node3D = root.get_meta("torso")
	if absf(hip_l.rotation.x) < 1.2:  # nicht im Sitzen animieren
		hip_l.rotation.x = swing
		hip_r.rotation.x = -swing
		knee_l.rotation.x = maxf(0.0, -sin(phase)) * 0.8 * amount
		knee_r.rotation.x = maxf(0.0, sin(phase)) * 0.8 * amount
	arm_l.rotation.x = -swing * 0.8 + sin(t * 1.3) * 0.03
	arm_r.rotation.x = swing * 0.8 - sin(t * 1.3) * 0.03
	torso.position.y = 0.92 + absf(sin(phase)) * 0.04 * amount + sin(t * 2.0) * 0.004
