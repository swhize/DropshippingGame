class_name ItemKit
extends RefCounted
## Baut die Darstellung eines tragbaren Gegenstands (Kiste, Einzelteil, Paket, etikettiertes
## Paket, Teller). Ursprung = Objektmitte. Genutzt in der Hand (Player) und am Boden (DroppedItem).

const PACKAGE_SIZES := [Vector3(0.26, 0.18, 0.22), Vector3(0.34, 0.24, 0.3), Vector3(0.46, 0.32, 0.38)]
const CRATE_SIZE := Vector3(0.62, 0.46, 0.46)
const ITEM_SIZE := Vector3(0.2, 0.13, 0.15)

static func bounds(kind: int, data: Dictionary) -> Vector3:
	match kind:
		GameManager.ItemKind.CRATE:
			return CRATE_SIZE
		GameManager.ItemKind.ITEM:
			return ITEM_SIZE
		GameManager.ItemKind.PACKAGE, GameManager.ItemKind.LABELED:
			return PACKAGE_SIZES[_size_index(data)]
		GameManager.ItemKind.PLATE:
			return Vector3(0.3, 0.12, 0.3)
	return Vector3(0.2, 0.2, 0.2)

static func _size_index(data: Dictionary) -> int:
	var pid: String = data.get("product", "")
	if pid == "":
		return 1
	return int(GameData.product(pid)["size"])

static func build(kind: int, data: Dictionary, show_label: bool = true) -> Node3D:
	var root := Node3D.new()
	var pid: String = data.get("product", "")
	var product: Dictionary = GameData.product(pid) if pid != "" else {}
	match kind:
		GameManager.ItemKind.CRATE:
			var s := CRATE_SIZE
			root.add_child(Props.box(s, Mats.cardboard(), Vector3.ZERO))
			root.add_child(Props.box(Vector3(s.x * 0.2, 0.004, s.z + 0.004), Mats.std(Color(0.82, 0.72, 0.48), 0.4), Vector3(0, s.y / 2.0, 0)))
			root.add_child(Props.box(Vector3(s.x + 0.004, s.y * 0.3, s.z * 0.5), Mats.std(product.get("color", Color.WHITE), 0.6), Vector3(0, 0, 0)))
			var q: float = float(data.get("quality", 1.0))
			var qcol := Color(0.9, 0.3, 0.25) if q < 0.8 else (Color(0.95, 0.8, 0.2) if q >= 1.3 else Color(0.85, 0.85, 0.85))
			root.add_child(Props.box(Vector3(0.12, 0.08, 0.004), Mats.std(qcol, 0.5), Vector3(-s.x * 0.3, s.y * 0.25, s.z / 2.0)))
			if show_label:
				var l := Props.label("%d× %s" % [int(data.get("quantity", 0)), product.get("short", "")], 36, Color(1, 1, 1), Vector3(0, s.y / 2.0 + 0.12, 0), true, 8)
				l.visibility_range_end = 8.0
				root.add_child(l)
		GameManager.ItemKind.ITEM:
			var s := ITEM_SIZE
			var col: Color = product.get("color", Color(0.9, 0.8, 0.3))
			root.add_child(Props.box(s, Mats.std(Color(0.97, 0.97, 0.96), 0.5), Vector3.ZERO))
			root.add_child(Props.box(Vector3(s.x + 0.003, s.y * 0.55, s.z + 0.003), Mats.std(col, 0.45), Vector3(0, s.y * 0.1, 0)))
			root.add_child(Props.box(Vector3(0.05, 0.05, 0.004), Mats.std(col.darkened(0.4), 0.5), Vector3(s.x * 0.25, 0, s.z / 2.0 + 0.002)))
		GameManager.ItemKind.PACKAGE, GameManager.ItemKind.LABELED:
			var s: Vector3 = PACKAGE_SIZES[_size_index(data)]
			root.add_child(Props.box(s, Mats.cardboard(), Vector3.ZERO))
			root.add_child(Props.box(Vector3(s.x + 0.003, 0.004, s.z * 0.22), Mats.std(Color(0.8, 0.7, 0.45), 0.35), Vector3(0, s.y / 2.0, 0)))
			var brand_col: Color = data.get("color", Color(0.9, 0.3, 0.3))
			root.add_child(Props.box(Vector3(s.x + 0.004, s.y * 0.14, s.z + 0.004), Mats.std(brand_col, 0.5), Vector3(0, -s.y * 0.32, 0)))
			var logo := logo_mesh(int(data.get("logo", 0)), brand_col, minf(s.x, s.z) * 0.32)
			logo.position = Vector3(s.x * 0.22, s.y / 2.0 + 0.004, -s.z * 0.22)
			root.add_child(logo)
			if kind == GameManager.ItemKind.LABELED:
				var lw := s.x * 0.5
				var ld := s.z * 0.5
				root.add_child(Props.box(Vector3(lw, 0.004, ld), Mats.std(Color(1, 1, 1), 0.6), Vector3(-s.x * 0.12, s.y / 2.0 + 0.006, s.z * 0.12)))
				for i in 7:
					var bw := 0.004 + (i % 3) * 0.003
					root.add_child(Props.box(Vector3(bw, 0.005, ld * 0.45), Mats.std(Color(0.05, 0.05, 0.05)),
						Vector3(-s.x * 0.12 - lw * 0.35 + i * lw * 0.11, s.y / 2.0 + 0.008, s.z * 0.12 + ld * 0.18)))
		GameManager.ItemKind.PLATE:
			root.add_child(Props.cyl(0.15, 0.12, 0.02, Mats.std(Color(0.97, 0.97, 0.97), 0.25), Vector3(0, -0.05, 0)))
			root.add_child(Props.cyl(0.08, 0.08, 0.03, Mats.std(Color(0.85, 0.6, 0.3), 0.7), Vector3(0, -0.025, 0)))
			root.add_child(Props.cyl(0.085, 0.085, 0.025, Mats.std(Color(0.4, 0.22, 0.12), 0.7), Vector3(0, 0.0, 0)))
			root.add_child(Props.cyl(0.088, 0.088, 0.008, Mats.std(Color(0.95, 0.8, 0.2), 0.6), Vector3(0, 0.017, 0)))
			root.add_child(Props.cyl(0.09, 0.09, 0.008, Mats.std(Color(0.3, 0.7, 0.25), 0.8), Vector3(0, 0.025, 0)))
			root.add_child(Props.sphere(0.082, Mats.std(Color(0.88, 0.62, 0.3), 0.7), Vector3(0, 0.035, 0), 12, 6))
			for i in 5:
				root.add_child(Props.box(Vector3(0.012, 0.012, 0.08), Mats.std(Color(0.98, 0.82, 0.3), 0.7), Vector3(0.11, -0.03, -0.06 + i * 0.03), Vector3(0, i * 12, 0)))
	return root

## Markenlogo als flache 3D-Form (liegt auf einer Oberfläche, Oberseite = +Y)
static func logo_mesh(index: int, color: Color, size: float) -> Node3D:
	var n := Node3D.new()
	var m := Mats.std(color, 0.4)
	var t := 0.006
	match index:
		0:
			n.add_child(Props.cyl(size * 0.5, size * 0.5, t, m, Vector3.ZERO, Vector3.ZERO, 20))
		1:
			n.add_child(Props.box(Vector3(size * 0.85, t, size * 0.85), m))
		2:
			n.add_child(Props.box(Vector3(size * 0.7, t, size * 0.7), m, Vector3.ZERO, Vector3(0, 45, 0)))
		3:
			n.add_child(Props.box(Vector3(size * 0.62, t, size * 0.62), m))
			n.add_child(Props.box(Vector3(size * 0.62, t, size * 0.62), m, Vector3.ZERO, Vector3(0, 45, 0)))
		4:
			n.add_child(Props.box(Vector3(size * 0.22, t, size * 0.6), m, Vector3(size * 0.08, 0, -size * 0.18), Vector3(0, 25, 0)))
			n.add_child(Props.box(Vector3(size * 0.22, t, size * 0.6), m, Vector3(-size * 0.08, 0, size * 0.18), Vector3(0, 25, 0)))
		_:
			n.add_child(Props.cyl(size * 0.25, size * 0.25, t, m, Vector3(-size * 0.17, 0, -size * 0.1), Vector3.ZERO, 16))
			n.add_child(Props.cyl(size * 0.25, size * 0.25, t, m, Vector3(size * 0.17, 0, -size * 0.1), Vector3.ZERO, 16))
			n.add_child(Props.box(Vector3(size * 0.5, t, size * 0.5), m, Vector3(0, 0, size * 0.1), Vector3(0, 45, 0)))
	return n
