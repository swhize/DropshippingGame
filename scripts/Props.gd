class_name Props
extends RefCounted
## Baukasten für alle 3D-Objekte der Welt (Möbel, Fahrzeuge, Bäume, Deko ...).
## Alles aus Grundformen + prozeduralen Materialien, keine Modelldateien.

# ---- Grundformen -------------------------------------------------------------------------
static func box(size: Vector3, mat: Material, pos: Vector3 = Vector3.ZERO, rot_deg: Vector3 = Vector3.ZERO) -> MeshInstance3D:
	var mi := MeshInstance3D.new()
	var mesh := BoxMesh.new()
	mesh.size = size
	mi.mesh = mesh
	mi.material_override = mat
	mi.position = pos
	mi.rotation_degrees = rot_deg
	return mi

static func cyl(r_top: float, r_bottom: float, h: float, mat: Material, pos: Vector3 = Vector3.ZERO,
		rot_deg: Vector3 = Vector3.ZERO, segments: int = 16) -> MeshInstance3D:
	var mi := MeshInstance3D.new()
	var mesh := CylinderMesh.new()
	mesh.top_radius = r_top
	mesh.bottom_radius = r_bottom
	mesh.height = h
	mesh.radial_segments = segments
	mesh.rings = 1
	mi.mesh = mesh
	mi.material_override = mat
	mi.position = pos
	mi.rotation_degrees = rot_deg
	return mi

static func sphere(r: float, mat: Material, pos: Vector3 = Vector3.ZERO, segments: int = 16, rings: int = 8) -> MeshInstance3D:
	var mi := MeshInstance3D.new()
	var mesh := SphereMesh.new()
	mesh.radius = r
	mesh.height = r * 2.0
	mesh.radial_segments = segments
	mesh.rings = rings
	mi.mesh = mesh
	mi.material_override = mat
	mi.position = pos
	return mi

static func label(text: String, font_size: int, color: Color, pos: Vector3, billboard: bool = true,
		outline: int = 12) -> Label3D:
	var l := Label3D.new()
	l.text = text
	l.font = UITheme.font(true)
	l.font_size = font_size
	l.pixel_size = 0.004
	l.outline_size = outline
	l.modulate = color
	l.outline_modulate = Color(0, 0, 0, 0.85)
	l.position = pos
	if billboard:
		l.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	else:
		l.double_sided = false  # sonst erscheint der Text von hinten gespiegelt
	l.texture_filter = BaseMaterial3D.TEXTURE_FILTER_LINEAR_WITH_MIPMAPS
	return l

## Statischer Kollisionskörper (unsichtbar) - für Wände, Möbel usw.
static func collider(parent: Node3D, size: Vector3, pos: Vector3, rot_deg: Vector3 = Vector3.ZERO) -> StaticBody3D:
	var body := StaticBody3D.new()
	body.position = pos
	body.rotation_degrees = rot_deg
	var col := CollisionShape3D.new()
	var shape := BoxShape3D.new()
	shape.size = size
	col.shape = shape
	body.add_child(col)
	parent.add_child(body)
	return body

## Box mit Kollision in einem Rutsch
static func solid(parent: Node3D, size: Vector3, mat: Material, pos: Vector3, rot_deg: Vector3 = Vector3.ZERO) -> MeshInstance3D:
	var mi := box(size, mat, pos, rot_deg)
	parent.add_child(mi)
	collider(parent, size, pos, rot_deg)
	return mi

# ---- Möbel -----------------------------------------------------------------------------------
static func table(w: float, d: float, h: float, top_mat: Material, leg_mat: Material) -> Node3D:
	var n := Node3D.new()
	n.add_child(box(Vector3(w, 0.05, d), top_mat, Vector3(0, h - 0.025, 0)))
	var ix := w / 2.0 - 0.07
	var iz := d / 2.0 - 0.07
	for sx in [-1.0, 1.0]:
		for sz in [-1.0, 1.0]:
			n.add_child(box(Vector3(0.05, h - 0.05, 0.05), leg_mat, Vector3(sx * ix, (h - 0.05) / 2.0, sz * iz)))
	return n

static func chair(mat: Material) -> Node3D:
	var n := Node3D.new()
	n.add_child(box(Vector3(0.44, 0.05, 0.44), mat, Vector3(0, 0.46, 0)))
	n.add_child(box(Vector3(0.44, 0.48, 0.05), mat, Vector3(0, 0.72, -0.2)))
	for sx in [-0.19, 0.19]:
		for sz in [-0.19, 0.19]:
			n.add_child(box(Vector3(0.04, 0.44, 0.04), Mats.dark_metal(), Vector3(sx, 0.22, sz)))
	return n

static func stool() -> Node3D:
	var n := Node3D.new()
	n.add_child(cyl(0.2, 0.2, 0.06, Mats.std(Color(0.8, 0.15, 0.15), 0.5), Vector3(0, 0.74, 0)))
	n.add_child(cyl(0.03, 0.03, 0.72, Mats.metal(), Vector3(0, 0.36, 0)))
	n.add_child(cyl(0.18, 0.2, 0.03, Mats.metal(), Vector3(0, 0.015, 0)))
	return n

static func pallet() -> Node3D:
	var n := Node3D.new()
	var w := Mats.shader("planks", {"wood": Color(0.72, 0.56, 0.36), "plank_w": 0.14})
	for i in 5:
		n.add_child(box(Vector3(1.2, 0.025, 0.14), w, Vector3(0, 0.137, -0.4 + i * 0.2)))
	for sx in [-0.52, 0.0, 0.52]:
		n.add_child(box(Vector3(0.1, 0.1, 1.0), w, Vector3(sx, 0.07, 0)))
	return n

static func crate(color: Color, size: Vector3 = Vector3(0.6, 0.45, 0.45)) -> Node3D:
	var n := Node3D.new()
	n.add_child(box(size, Mats.cardboard(), Vector3(0, size.y / 2.0, 0)))
	n.add_child(box(Vector3(size.x + 0.005, 0.06, size.z * 0.35), Mats.std(color, 0.6), Vector3(0, size.y * 0.55, 0)))
	n.add_child(box(Vector3(size.x * 0.18, 0.005, size.z + 0.005), Mats.std(Color(0.8, 0.72, 0.5), 0.4), Vector3(0, size.y + 0.002, 0)))
	return n

static func plant(scale: float = 1.0) -> Node3D:
	var n := Node3D.new()
	n.add_child(cyl(0.16 * scale, 0.12 * scale, 0.3 * scale, Mats.std(Color(0.85, 0.82, 0.78), 0.6), Vector3(0, 0.15 * scale, 0)))
	var leaf := Mats.shader("flat", {"col": Color(0.25, 0.55, 0.28)})
	n.add_child(sphere(0.28 * scale, leaf, Vector3(0, 0.55 * scale, 0), 7, 4))
	n.add_child(sphere(0.2 * scale, leaf, Vector3(0.12 * scale, 0.75 * scale, 0.05 * scale), 6, 3))
	n.add_child(sphere(0.18 * scale, leaf, Vector3(-0.1 * scale, 0.8 * scale, -0.06 * scale), 6, 3))
	return n

static func tree(scale: float = 1.0, variant: int = 0) -> Node3D:
	var n := Node3D.new()
	n.add_child(cyl(0.12 * scale, 0.18 * scale, 2.2 * scale, Mats.std(Color(0.36, 0.25, 0.16), 0.9), Vector3(0, 1.1 * scale, 0), Vector3.ZERO, 7))
	var greens := [Color(0.24, 0.48, 0.22), Color(0.3, 0.52, 0.2), Color(0.2, 0.42, 0.26)]
	var leaf := Mats.shader("flat", {"col": greens[variant % 3]})
	if variant % 2 == 0:
		n.add_child(sphere(1.25 * scale, leaf, Vector3(0, 2.9 * scale, 0), 7, 4))
		n.add_child(sphere(0.9 * scale, leaf, Vector3(0.55 * scale, 3.5 * scale, 0.2 * scale), 6, 4))
		n.add_child(sphere(0.8 * scale, leaf, Vector3(-0.5 * scale, 3.3 * scale, -0.3 * scale), 6, 4))
	else:
		n.add_child(cyl(0.0, 1.3 * scale, 2.2 * scale, leaf, Vector3(0, 2.6 * scale, 0), Vector3.ZERO, 7))
		n.add_child(cyl(0.0, 0.95 * scale, 1.8 * scale, leaf, Vector3(0, 3.6 * scale, 0), Vector3.ZERO, 7))
	return n

static func bush(scale: float = 1.0) -> Node3D:
	var n := Node3D.new()
	var leaf := Mats.shader("flat", {"col": Color(0.26, 0.5, 0.24)})
	n.add_child(sphere(0.6 * scale, leaf, Vector3(0, 0.35 * scale, 0), 7, 4))
	n.add_child(sphere(0.45 * scale, leaf, Vector3(0.45 * scale, 0.3 * scale, 0.1), 6, 3))
	return n

## Straßenlaterne - gibt {node, light} zurück
static func lamp_post() -> Dictionary:
	var n := Node3D.new()
	var pole := Mats.std(Color(0.16, 0.17, 0.19), 0.4, 0.6)
	n.add_child(cyl(0.06, 0.09, 4.2, pole, Vector3(0, 2.1, 0), Vector3.ZERO, 8))
	n.add_child(box(Vector3(0.08, 0.08, 1.1), pole, Vector3(0, 4.15, 0.5)))
	n.add_child(box(Vector3(0.34, 0.12, 0.5), pole, Vector3(0, 4.08, 1.0)))
	n.add_child(box(Vector3(0.28, 0.03, 0.42), Mats.shader("lamp"), Vector3(0, 4.01, 1.0)))
	var light := OmniLight3D.new()
	light.position = Vector3(0, 3.8, 1.0)
	light.omni_range = 11.0
	light.light_color = Color(1.0, 0.82, 0.6)
	light.light_energy = 0.0
	light.visible = false
	n.add_child(light)
	return {"node": n, "light": light}

static func bench() -> Node3D:
	var n := Node3D.new()
	var w := Mats.shader("planks", {"wood": Color(0.55, 0.36, 0.22), "plank_w": 0.1})
	for i in 3:
		n.add_child(box(Vector3(1.8, 0.04, 0.12), w, Vector3(0, 0.45, -0.14 + i * 0.14)))
	for i in 2:
		n.add_child(box(Vector3(1.8, 0.12, 0.04), w, Vector3(0, 0.66 + i * 0.16, -0.26)))
	for sx in [-0.8, 0.8]:
		n.add_child(box(Vector3(0.06, 0.45, 0.5), Mats.dark_metal(), Vector3(sx, 0.225, -0.05)))
	return n

static func trash_bin(color: Color = Color(0.2, 0.4, 0.28)) -> Node3D:
	var n := Node3D.new()
	n.add_child(box(Vector3(0.62, 0.9, 0.62), Mats.std(color, 0.6), Vector3(0, 0.45, 0)))
	n.add_child(box(Vector3(0.66, 0.06, 0.66), Mats.std(color.darkened(0.3), 0.6), Vector3(0, 0.93, 0)))
	return n

static func fence(length: float, color: Color = Color(0.3, 0.3, 0.32)) -> Node3D:
	var n := Node3D.new()
	var m := Mats.std(color, 0.5, 0.5)
	var posts := int(length / 2.0) + 1
	for i in posts:
		n.add_child(box(Vector3(0.06, 1.3, 0.06), m, Vector3(-length / 2.0 + i * (length / maxf(posts - 1, 1)), 0.65, 0)))
	n.add_child(box(Vector3(length, 0.05, 0.05), m, Vector3(0, 1.2, 0)))
	n.add_child(box(Vector3(length, 0.05, 0.05), m, Vector3(0, 0.5, 0)))
	n.add_child(box(Vector3(length, 0.9, 0.01), Mats.std(Color(0.35, 0.36, 0.38, 0.35), 0.4, 0.6), Vector3(0, 0.75, 0)))
	return n

static func barrier() -> Node3D:
	var n := Node3D.new()
	for i in 5:
		var c := Color(0.9, 0.15, 0.12) if i % 2 == 0 else Color(0.95, 0.95, 0.95)
		n.add_child(box(Vector3(0.5, 0.25, 0.08), Mats.std(c, 0.5), Vector3(-1.0 + i * 0.5, 0.85, 0)))
	for sx in [-1.1, 1.1]:
		n.add_child(box(Vector3(0.08, 1.0, 0.4), Mats.std(Color(0.95, 0.95, 0.95), 0.6), Vector3(sx, 0.5, 0)))
	return n

static func wheel(radius: float, width: float) -> MeshInstance3D:
	return cyl(radius, radius, width, Mats.std(Color(0.06, 0.06, 0.07), 0.8), Vector3.ZERO, Vector3(0, 0, 90), 14)

## Auto (Front zeigt in +X)
static func car(color: Color, sporty: bool = false) -> Node3D:
	var n := Node3D.new()
	var paint := Mats.std(color, 0.25, 0.4)
	var glass := Mats.std(Color(0.12, 0.16, 0.22), 0.1, 0.4)
	var h := 0.55 if sporty else 0.7
	n.add_child(box(Vector3(4.2 if sporty else 4.0, h, 1.8), paint, Vector3(0, 0.35 + h / 2.0, 0)))
	var cab_len := 1.8 if sporty else 2.2
	var cab_h := 0.45 if sporty else 0.6
	n.add_child(box(Vector3(cab_len, cab_h, 1.6), glass, Vector3(-0.2, 0.35 + h + cab_h / 2.0, 0)))
	n.add_child(box(Vector3(cab_len - 0.3, 0.06, 1.62), paint, Vector3(-0.2, 0.35 + h + cab_h, 0)))
	for fx in [1.3, -1.3]:
		for fz in [0.85, -0.85]:
			var wh := wheel(0.36, 0.26)
			wh.position = Vector3(fx, 0.36, fz)
			n.add_child(wh)
	n.add_child(box(Vector3(0.05, 0.14, 0.4), Mats.emit(Color(1.0, 0.95, 0.8), 3.0), Vector3(2.02 if not sporty else 2.12, 0.62, 0.6)))
	n.add_child(box(Vector3(0.05, 0.14, 0.4), Mats.emit(Color(1.0, 0.95, 0.8), 3.0), Vector3(2.02 if not sporty else 2.12, 0.62, -0.6)))
	n.add_child(box(Vector3(0.05, 0.12, 0.4), Mats.emit(Color(1.0, 0.1, 0.1), 2.0), Vector3(-2.02 if not sporty else -2.12, 0.65, 0.6)))
	n.add_child(box(Vector3(0.05, 0.12, 0.4), Mats.emit(Color(1.0, 0.1, 0.1), 2.0), Vector3(-2.02 if not sporty else -2.12, 0.65, -0.6)))
	if sporty:
		n.add_child(box(Vector3(0.25, 0.05, 1.7), paint, Vector3(-2.0, 1.15, 0)))
		n.add_child(box(Vector3(0.06, 0.25, 0.06), paint, Vector3(-2.0, 1.0, 0.6)))
		n.add_child(box(Vector3(0.06, 0.25, 0.06), paint, Vector3(-2.0, 1.0, -0.6)))
	return n

## Lieferwagen (Front zeigt in +X)
static func van(color: Color, text: String) -> Node3D:
	var n := Node3D.new()
	var paint := Mats.std(color, 0.35, 0.3)
	var glass := Mats.std(Color(0.12, 0.16, 0.22), 0.1, 0.4)
	n.add_child(box(Vector3(3.6, 2.2, 2.0), paint, Vector3(-0.6, 1.5, 0)))
	n.add_child(box(Vector3(1.4, 1.5, 1.95), paint, Vector3(1.9, 1.15, 0)))
	n.add_child(box(Vector3(0.05, 0.7, 1.8), glass, Vector3(2.62, 1.45, 0)))
	n.add_child(box(Vector3(0.9, 0.6, 0.02), glass, Vector3(1.9, 1.45, 0.99)))
	n.add_child(box(Vector3(0.9, 0.6, 0.02), glass, Vector3(1.9, 1.45, -0.99)))
	for fx in [1.8, -1.6]:
		for fz in [0.92, -0.92]:
			var wh := wheel(0.42, 0.3)
			wh.position = Vector3(fx, 0.42, fz)
			n.add_child(wh)
	var stripe := Mats.std(Color(1, 1, 1), 0.4)
	n.add_child(box(Vector3(3.6, 0.2, 2.02), stripe, Vector3(-0.6, 1.0, 0)))
	for side in [1.0, -1.0]:
		var l := label(text, 150, Color(1, 1, 1), Vector3(-0.6, 1.8, side * 1.02), false, 16)
		l.rotation_degrees = Vector3(0, 0 if side > 0 else 180, 0)
		n.add_child(l)
	n.add_child(box(Vector3(0.05, 0.16, 0.4), Mats.emit(Color(1.0, 0.95, 0.8), 3.0), Vector3(2.62, 0.75, 0.7)))
	n.add_child(box(Vector3(0.05, 0.16, 0.4), Mats.emit(Color(1.0, 0.95, 0.8), 3.0), Vector3(2.62, 0.75, -0.7)))
	return n

# ---- Deko & Lifestyle ------------------------------------------------------------------------
static func poster(text: String, color: Color) -> Node3D:
	var n := Node3D.new()
	n.add_child(box(Vector3(0.8, 1.1, 0.02), Mats.std(color, 0.7), Vector3.ZERO))
	var l := label(text, 70, Color(1, 1, 1), Vector3(0, 0.1, 0.02), false, 8)
	l.width = 180
	l.autowrap_mode = TextServer.AUTOWRAP_WORD
	n.add_child(l)
	return n

static func rug(color: Color, size: Vector2 = Vector2(2.4, 1.6)) -> Node3D:
	var n := Node3D.new()
	n.add_child(box(Vector3(size.x, 0.015, size.y), Mats.std(color, 0.95), Vector3(0, 0.008, 0)))
	n.add_child(box(Vector3(size.x - 0.3, 0.017, size.y - 0.3), Mats.std(color.lightened(0.2), 0.95), Vector3(0, 0.009, 0)))
	return n

static func floor_lamp() -> Node3D:
	var n := Node3D.new()
	n.add_child(cyl(0.18, 0.2, 0.04, Mats.dark_metal(), Vector3(0, 0.02, 0)))
	n.add_child(cyl(0.02, 0.02, 1.6, Mats.dark_metal(), Vector3(0, 0.8, 0)))
	n.add_child(cyl(0.12, 0.25, 0.3, Mats.emit(Color(1.0, 0.9, 0.72), 1.4), Vector3(0, 1.65, 0)))
	var light := OmniLight3D.new()
	light.position = Vector3(0, 1.5, 0)
	light.omni_range = 4.0
	light.light_energy = 0.8
	light.light_color = Color(1.0, 0.85, 0.65)
	n.add_child(light)
	return n

static func billy() -> Node3D:
	var n := Node3D.new()
	var w := Mats.std(Color(0.93, 0.92, 0.88), 0.7)
	for sx in [-0.4, 0.4]:
		n.add_child(box(Vector3(0.03, 2.0, 0.3), w, Vector3(sx, 1.0, 0)))
	for i in 5:
		n.add_child(box(Vector3(0.8, 0.03, 0.3), w, Vector3(0, 0.05 + i * 0.47, 0)))
	var cols := [Color(0.7, 0.2, 0.2), Color(0.2, 0.4, 0.7), Color(0.85, 0.7, 0.2), Color(0.3, 0.6, 0.35)]
	for i in 7:
		n.add_child(box(Vector3(0.05, 0.28, 0.22), Mats.std(cols[i % 4], 0.7), Vector3(-0.3 + i * 0.07, 0.62, 0)))
	var p := plant(0.6)
	p.position = Vector3(0.2, 1.46, 0)
	n.add_child(p)
	return n

static func whiteboard() -> Node3D:
	var n := Node3D.new()
	n.add_child(box(Vector3(1.6, 1.0, 0.04), Mats.std(Color(0.97, 0.97, 0.97), 0.3), Vector3.ZERO))
	n.add_child(box(Vector3(1.66, 0.04, 0.06), Mats.metal(), Vector3(0, -0.52, 0.02)))
	var l := label("BUSINESSPLAN\n1. Kaufen\n2. Verkaufen\n3. ???\n4. PROFIT", 42, Color(0.15, 0.25, 0.6), Vector3(-0.2, 0, 0.03), false, 0)
	n.add_child(l)
	n.add_child(box(Vector3(0.5, 0.02, 0.01), Mats.std(Color(0.8, 0.2, 0.2)), Vector3(0.45, 0.2, 0.03), Vector3(0, 0, 25)))
	n.add_child(box(Vector3(0.5, 0.02, 0.01), Mats.std(Color(0.8, 0.2, 0.2)), Vector3(0.5, -0.05, 0.03), Vector3(0, 0, 40)))
	return n

static func coffee_machine() -> Node3D:
	var n := Node3D.new()
	n.add_child(Props.table(0.7, 0.5, 0.9, Mats.wood(), Mats.dark_metal()))
	n.add_child(box(Vector3(0.36, 0.36, 0.34), Mats.std(Color(0.75, 0.76, 0.78), 0.2, 0.9), Vector3(0, 1.08, 0)))
	n.add_child(box(Vector3(0.3, 0.05, 0.3), Mats.dark_metal(), Vector3(0, 1.28, 0)))
	n.add_child(cyl(0.05, 0.04, 0.08, Mats.std(Color(0.95, 0.95, 0.95), 0.3), Vector3(0.05, 0.95, 0.12)))
	n.add_child(box(Vector3(0.06, 0.04, 0.02), Mats.emit(Color(0.3, 1.0, 0.4), 2.0), Vector3(-0.1, 1.15, 0.18)))
	return n

static func sofa(color: Color) -> Node3D:
	var n := Node3D.new()
	var m := Mats.std(color, 0.55)
	n.add_child(box(Vector3(2.0, 0.42, 0.9), m, Vector3(0, 0.3, 0)))
	n.add_child(box(Vector3(2.0, 0.55, 0.22), m, Vector3(0, 0.72, -0.34)))
	for sx in [-0.93, 0.93]:
		n.add_child(box(Vector3(0.2, 0.3, 0.9), m, Vector3(sx, 0.6, 0)))
	for sx in [-0.45, 0.45]:
		n.add_child(box(Vector3(0.82, 0.12, 0.62), Mats.std(color.lightened(0.12), 0.6), Vector3(sx, 0.57, 0.06)))
	return n

static func shoe_display() -> Node3D:
	var n := Node3D.new()
	n.add_child(box(Vector3(0.6, 0.04, 0.3), Mats.wood(), Vector3.ZERO))
	n.add_child(box(Vector3(0.62, 0.34, 0.32), Mats.glass(), Vector3(0, 0.19, 0)))
	for sx in [-0.12, 0.12]:
		n.add_child(box(Vector3(0.1, 0.07, 0.25), Mats.std(Color(0.95, 0.95, 0.95), 0.4), Vector3(sx, 0.06, 0)))
		n.add_child(box(Vector3(0.1, 0.02, 0.25), Mats.std(Color(0.9, 0.2, 0.2), 0.4), Vector3(sx, 0.025, 0)))
	return n

static func gaming_chair() -> Node3D:
	var n := Node3D.new()
	var black := Mats.std(Color(0.08, 0.08, 0.09), 0.5)
	var accent := Mats.emit(Color(0.2, 0.9, 1.0), 1.5)
	n.add_child(cyl(0.3, 0.3, 0.05, black, Vector3(0, 0.08, 0), Vector3.ZERO, 5))
	n.add_child(cyl(0.04, 0.04, 0.35, Mats.metal(), Vector3(0, 0.28, 0)))
	n.add_child(box(Vector3(0.55, 0.1, 0.52), black, Vector3(0, 0.5, 0)))
	n.add_child(box(Vector3(0.52, 0.8, 0.1), black, Vector3(0, 0.95, -0.24), Vector3(-8, 0, 0)))
	n.add_child(box(Vector3(0.04, 0.72, 0.02), accent, Vector3(-0.2, 0.95, -0.18), Vector3(-8, 0, 0)))
	n.add_child(box(Vector3(0.04, 0.72, 0.02), accent, Vector3(0.2, 0.95, -0.18), Vector3(-8, 0, 0)))
	return n

static func neon_sign(text: String, color: Color) -> Node3D:
	var n := Node3D.new()
	n.add_child(box(Vector3(2.0, 0.6, 0.04), Mats.std(Color(0.05, 0.05, 0.06), 0.3), Vector3.ZERO))
	var l := label(text, 120, Color(color.r * 3.0, color.g * 3.0, color.b * 3.0), Vector3(0, 0, 0.03), false, 0)
	n.add_child(l)
	var light := OmniLight3D.new()
	light.position = Vector3(0, 0, 0.5)
	light.omni_range = 3.5
	light.light_energy = 1.2
	light.light_color = color
	n.add_child(light)
	return n

static func vending_machine() -> Node3D:
	var n := Node3D.new()
	n.add_child(box(Vector3(0.9, 1.9, 0.8), Mats.std(Color(0.75, 0.12, 0.12), 0.4), Vector3(0, 0.95, 0)))
	n.add_child(box(Vector3(0.6, 1.2, 0.02), Mats.emit(Color(0.7, 0.85, 1.0), 0.8), Vector3(-0.08, 1.1, 0.41)))
	for i in 4:
		n.add_child(box(Vector3(0.5, 0.02, 0.03), Mats.std(Color(0.2, 0.2, 0.2)), Vector3(-0.08, 0.65 + i * 0.28, 0.42)))
	return n

static func ceiling_light(length: float) -> Node3D:
	var n := Node3D.new()
	n.add_child(box(Vector3(length, 0.08, 0.2), Mats.dark_metal(), Vector3.ZERO))
	n.add_child(box(Vector3(length - 0.1, 0.03, 0.14), Mats.shader("lamp", {"always_on": 1.0, "glow": Color(0.9, 0.95, 1.0)}), Vector3(0, -0.05, 0)))
	return n

static func sign_board(text: String, bg: Color, fg: Color, size: Vector2) -> Node3D:
	var n := Node3D.new()
	n.add_child(box(Vector3(size.x, size.y, 0.06), Mats.std(bg, 0.5), Vector3.ZERO))
	n.add_child(label(text, 110, fg, Vector3(0, 0, 0.04), false, 0))
	var back := label(text, 110, fg, Vector3(0, 0, -0.04), false, 0)
	back.rotation_degrees.y = 180
	n.add_child(back)
	return n
