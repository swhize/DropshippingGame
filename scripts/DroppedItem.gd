extends RigidBody3D
class_name DroppedItem
## Ein abgelegter Gegenstand mit echter Physik (fällt, kippt, lässt sich anstoßen).
## Behält alle Werte und kann mit E wieder aufgenommen werden. Wird mitgespeichert.

var kind: int = 0
var data: Dictionary = {}
var _meshes: Array[MeshInstance3D] = []
var _highlighted: bool = false

func setup(p_kind: int, p_data: Dictionary) -> void:
	kind = p_kind
	data = p_data.duplicate(true)
	var size := ItemKit.bounds(kind, data)
	var visual := ItemKit.build(kind, data)
	add_child(visual)
	_collect(visual)
	var col := CollisionShape3D.new()
	var shape := BoxShape3D.new()
	shape.size = size
	col.shape = shape
	add_child(col)
	mass = 8.0 if kind == GameManager.ItemKind.CRATE else 1.5
	var pm := PhysicsMaterial.new()
	pm.friction = 0.9
	pm.bounce = 0.05
	physics_material_override = pm
	angular_damp = 2.0
	linear_damp = 0.4
	add_to_group("dropped_items")

func _collect(node: Node) -> void:
	if node is MeshInstance3D:
		_meshes.append(node)
	for c in node.get_children():
		_collect(c)

func set_highlighted(on: bool) -> void:
	if on == _highlighted:
		return
	_highlighted = on
	var mat: Material = Mats.highlight() if on else null
	for m in _meshes:
		m.material_overlay = mat

func get_prompt(player: Node) -> String:
	if not player.is_empty():
		return "Hände voll"
	return "Aufheben: " + describe()

func describe() -> String:
	var pid: String = data.get("product", "")
	var pname: String = GameData.product(pid)["name"] if pid != "" else ""
	match kind:
		GameManager.ItemKind.CRATE:
			return "Kiste %d× %s" % [int(data.get("quantity", 0)), pname]
		GameManager.ItemKind.ITEM:
			return pname
		GameManager.ItemKind.PACKAGE:
			return "Paket (%s)" % pname
		GameManager.ItemKind.LABELED:
			return "Versandfertiges Paket (%s)" % pname
		GameManager.ItemKind.PLATE:
			return "Teller"
	return "Gegenstand"

func interact(player: Node) -> void:
	if not player.is_empty():
		GameManager.notify("Hände sind schon voll.", "info")
		return
	player.hold(kind, data)
	Audio.play("pickup")
	queue_free()

func to_save() -> Dictionary:
	return {"kind": kind, "data": data, "pos": global_position, "rot": global_rotation.y}
