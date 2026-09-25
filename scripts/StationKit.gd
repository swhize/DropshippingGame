class_name StationKit
extends RefCounted
## Baut das 3D-Modell jeder Station aus Grundformen. Lokaler Ursprung = Bodenmitte,
## Vorderseite zeigt nach +Z. Rückgabe: {size, center, label_h, content}
## "content" ist ein leerer Knoten, in den die Station dynamische Dinge legt
## (Kisten im Regal, Pakete auf dem Tisch ...).
##
## Typ-Nummern entsprechen Station.StationType:
## 0 PC, 1 DOCK, 2 REGAL, 3 PACK, 4 LABEL, 5 SHIP, 6 FOLD, 7 CONVEYOR,
## 8 DINER_PASS, 9 DINER_TABLE, 10 NPC_TALK, 11 END_DAY

static func build(root: Node3D, type: int, opts: Dictionary) -> Dictionary:
	var stage: int = opts.get("stage", 0)
	match type:
		0: return _pc(root, stage)
		1: return _dock(root, stage)
		2: return _regal(root, stage, opts)
		3: return _pack(root)
		4: return _label(root)
		5: return _ship(root, stage)
		6: return _fold(root)
		7: return _conveyor_in(root)
		8: return _diner_pass(root)
		9: return _diner_table(root, opts)
		10: return _kalle(root)
		11: return _end_day(root, stage)
	root.add_child(Props.box(Vector3.ONE, Mats.std(Color.MAGENTA), Vector3(0, 0.5, 0)))
	return {"size": Vector3.ONE, "center": Vector3(0, 0.5, 0), "label_h": 1.3, "content": null}

static func _content(root: Node3D, pos: Vector3) -> Node3D:
	var c := Node3D.new()
	c.position = pos
	root.add_child(c)
	return c

# ---- PC ---------------------------------------------------------------------------------
static func _pc(root: Node3D, stage: int) -> Dictionary:
	if stage == 0:
		root.add_child(Props.table(1.7, 0.75, 0.92, Mats.wood(), Mats.dark_metal()))
		root.add_child(Props.box(Vector3(1.7, 0.9, 0.03), Mats.std(Color(0.55, 0.45, 0.32), 0.9), Vector3(0, 1.5, -0.36)))
		for i in 6:
			root.add_child(Props.box(Vector3(0.04, 0.22, 0.03), Mats.std(Color(0.7, 0.2, 0.2) if i % 2 == 0 else Color(0.25, 0.25, 0.28), 0.5), Vector3(-0.6 + i * 0.2, 1.55, -0.33)))
		_laptop(root, Vector3(-0.15, 0.93, 0.02))
		root.add_child(Props.cyl(0.07, 0.09, 0.03, Mats.dark_metal(), Vector3(0.55, 0.935, -0.15)))
		root.add_child(Props.box(Vector3(0.03, 0.4, 0.03), Mats.dark_metal(), Vector3(0.55, 1.13, -0.15), Vector3(0, 0, 10)))
		root.add_child(Props.cyl(0.05, 0.1, 0.1, Mats.std(Color(0.9, 0.3, 0.2), 0.5), Vector3(0.5, 1.33, -0.08), Vector3(35, 0, 0)))
		root.add_child(Props.cyl(0.045, 0.04, 0.1, Mats.std(Color(0.95, 0.95, 0.95), 0.4), Vector3(0.3, 0.97, 0.15)))
		return {"size": Vector3(1.7, 1.0, 0.75), "center": Vector3(0, 0.5, 0), "label_h": 1.55, "content": null}
	root.add_child(Props.table(1.8, 0.85, 0.76, Mats.std(Color(0.92, 0.92, 0.9), 0.5), Mats.dark_metal()))
	root.add_child(Props.box(Vector3(0.05, 0.14, 0.05), Mats.dark_metal(), Vector3(0, 0.83, -0.2)))
	root.add_child(Props.box(Vector3(0.8, 0.48, 0.04), Mats.std(Color(0.08, 0.08, 0.09), 0.3), Vector3(0, 1.12, -0.22)))
	root.add_child(Props.box(Vector3(0.74, 0.42, 0.01), Mats.emit(Color(0.3, 0.6, 0.95), 1.3), Vector3(0, 1.12, -0.195)))
	root.add_child(Props.box(Vector3(0.44, 0.02, 0.15), Mats.std(Color(0.15, 0.15, 0.17), 0.5), Vector3(0, 0.77, 0.12)))
	var chair := Props.chair(Mats.std(Color(0.15, 0.15, 0.18), 0.6))
	chair.position = Vector3(0.1, 0, 0.75)
	chair.rotation_degrees.y = 180
	root.add_child(chair)
	return {"size": Vector3(1.8, 1.0, 0.85), "center": Vector3(0, 0.5, 0), "label_h": 1.55, "content": null}

static func _laptop(root: Node3D, pos: Vector3) -> void:
	var body := Mats.std(Color(0.7, 0.72, 0.75), 0.3, 0.7)
	root.add_child(Props.box(Vector3(0.38, 0.02, 0.26), body, pos + Vector3(0, 0.01, 0)))
	var lid := Node3D.new()
	lid.position = pos + Vector3(0, 0.02, -0.13)
	lid.rotation_degrees.x = -12
	root.add_child(lid)
	lid.add_child(Props.box(Vector3(0.38, 0.25, 0.012), body, Vector3(0, 0.125, 0)))
	lid.add_child(Props.box(Vector3(0.34, 0.21, 0.004), Mats.emit(Color(0.35, 0.65, 1.0), 1.5), Vector3(0, 0.125, 0.008)))
	root.add_child(Props.box(Vector3(0.3, 0.004, 0.11), Mats.std(Color(0.2, 0.2, 0.22), 0.6), pos + Vector3(0, 0.022, 0.03)))

# ---- Wareneingang --------------------------------------------------------------------------
static func _dock(root: Node3D, stage: int) -> Dictionary:
	var w := 1.5 if stage == 0 else 2.8
	var d := 1.4 if stage == 0 else 2.2
	var stripe_a := Mats.std(Color(0.95, 0.78, 0.1), 0.6)
	var stripe_b := Mats.std(Color(0.1, 0.1, 0.1), 0.6)
	for i in int(w / 0.25):
		root.add_child(Props.box(Vector3(0.25, 0.01, 0.08), stripe_a if i % 2 == 0 else stripe_b, Vector3(-w / 2.0 + 0.125 + i * 0.25, 0.006, d / 2.0)))
		root.add_child(Props.box(Vector3(0.25, 0.01, 0.08), stripe_a if i % 2 == 0 else stripe_b, Vector3(-w / 2.0 + 0.125 + i * 0.25, 0.006, -d / 2.0)))
	var pal_positions := [Vector3.ZERO] if stage == 0 else [Vector3(-0.65, 0, 0), Vector3(0.65, 0, 0)]
	for pp in pal_positions:
		var pal := Props.pallet()
		pal.position = pp
		root.add_child(pal)
	var sign := Props.sign_board("WARENEINGANG", Color(0.95, 0.78, 0.1), Color(0.1, 0.1, 0.1), Vector2(1.0, 0.22))
	sign.position = Vector3(0, 1.6 if stage == 0 else 2.2, -d / 2.0 + 0.05)
	sign.scale = Vector3.ONE * (0.8 if stage == 0 else 1.2)
	root.add_child(sign)
	root.add_child(Props.box(Vector3(0.05, 1.6 if stage == 0 else 2.2, 0.05), Mats.dark_metal(), Vector3(w / 2.0 - 0.05, 0.8 if stage == 0 else 1.1, -d / 2.0)))
	var c := _content(root, Vector3(0, 0.15, 0))
	return {"size": Vector3(w, 0.7, d), "center": Vector3(0, 0.35, 0), "label_h": 1.3, "content": c}

# ---- Regal ------------------------------------------------------------------------------------
static func _regal(root: Node3D, stage: int, opts: Dictionary) -> Dictionary:
	var p: Dictionary = GameData.PRODUCTS[opts.get("product_index", 0)]
	var pc: Color = p["color"]
	if stage == 0:
		var up := Mats.std(Color(0.3, 0.32, 0.35), 0.45, 0.6)
		var shelf := Mats.std(Color(0.62, 0.64, 0.66), 0.4, 0.7)
		for sx in [-0.88, 0.88]:
			for sz in [-0.24, 0.24]:
				root.add_child(Props.box(Vector3(0.04, 2.0, 0.04), up, Vector3(sx, 1.0, sz)))
		for i in 4:
			root.add_child(Props.box(Vector3(1.8, 0.03, 0.54), shelf, Vector3(0, 0.1 + i * 0.5, 0)))
		root.add_child(Props.box(Vector3(0.5, 0.12, 0.01), Mats.std(pc, 0.5), Vector3(0, 0.02 + 0.5, 0.275)))
		var tag := Props.label(p["name"], 36, Color(1, 1, 1), Vector3(0, 0.52, 0.285), false, 6)
		root.add_child(tag)
		var c := _content(root, Vector3.ZERO)
		c.set_meta("layout", "small")
		return {"size": Vector3(1.8, 2.0, 0.55), "center": Vector3(0, 1.0, 0), "label_h": 2.35, "content": c}
	var blue := Mats.std(Color(0.16, 0.32, 0.62), 0.5, 0.5)
	var orange := Mats.std(Color(0.95, 0.45, 0.1), 0.5, 0.4)
	for sx in [-1.98, 1.98]:
		for sz in [-0.5, 0.5]:
			root.add_child(Props.box(Vector3(0.08, 3.6, 0.08), blue, Vector3(sx, 1.8, sz)))
		for i in 6:
			root.add_child(Props.box(Vector3(0.03, 0.03, 1.0), blue, Vector3(sx, 0.3 + i * 0.6, 0), Vector3(35 if i % 2 == 0 else -35, 0, 0)))
	for lvl in 3:
		var y := 0.15 + lvl * 1.15
		for sz in [-0.5, 0.5]:
			root.add_child(Props.box(Vector3(4.0, 0.1, 0.06), orange, Vector3(0, y, sz)))
		for sx in [-1.0, 1.0]:
			var pal := Props.pallet()
			pal.position = Vector3(sx, y + 0.05 - 0.15, 0)
			pal.scale = Vector3(1.3, 1.0, 0.95)
			root.add_child(pal)
	var sign := Props.sign_board(p["name"].to_upper(), pc.darkened(0.2), Color(1, 1, 1), Vector2(2.4, 0.4))
	sign.position = Vector3(0, 3.85, 0.45)
	root.add_child(sign)
	var c2 := _content(root, Vector3.ZERO)
	c2.set_meta("layout", "rack")
	return {"size": Vector3(4.0, 3.6, 1.1), "center": Vector3(0, 1.8, 0), "label_h": 4.4, "content": c2}

# ---- Packtisch --------------------------------------------------------------------------------
static func _pack(root: Node3D) -> Dictionary:
	root.add_child(Props.table(1.6, 0.8, 0.92, Mats.std(Color(0.85, 0.83, 0.78), 0.6), Mats.dark_metal()))
	root.add_child(Props.box(Vector3(0.8, 0.005, 0.55), Mats.std(Color(0.2, 0.45, 0.3), 0.8), Vector3(-0.3, 0.925, 0.05)))
	root.add_child(Props.box(Vector3(0.18, 0.06, 0.08), Mats.std(Color(0.8, 0.15, 0.15), 0.5), Vector3(-0.62, 0.95, -0.25)))
	root.add_child(Props.cyl(0.06, 0.06, 0.05, Mats.std(Color(0.8, 0.7, 0.45), 0.4), Vector3(-0.62, 1.01, -0.25), Vector3(90, 0, 0)))
	root.add_child(Props.cyl(0.14, 0.14, 0.7, Mats.std(Color(0.8, 0.9, 1.0, 0.6), 0.2), Vector3(0.0, 1.05, -0.28), Vector3(0, 0, 90)))
	root.add_child(Props.box(Vector3(0.12, 0.015, 0.03), Mats.std(Color(0.95, 0.7, 0.1), 0.4), Vector3(-0.15, 0.935, 0.2)))
	var c := _content(root, Vector3(0.45, 0.925, 0.05))
	return {"size": Vector3(1.6, 1.0, 0.8), "center": Vector3(0, 0.5, 0), "label_h": 1.6, "content": c}

# ---- Labeldrucker -------------------------------------------------------------------------------
static func _label(root: Node3D) -> Dictionary:
	root.add_child(Props.table(1.0, 0.65, 0.92, Mats.std(Color(0.3, 0.32, 0.36), 0.5, 0.4), Mats.dark_metal()))
	var body := Mats.std(Color(0.92, 0.92, 0.9), 0.4)
	root.add_child(Props.box(Vector3(0.42, 0.24, 0.36), body, Vector3(-0.1, 1.04, 0)))
	root.add_child(Props.box(Vector3(0.44, 0.05, 0.3), Mats.std(Color(0.2, 0.2, 0.22), 0.5), Vector3(-0.1, 1.18, -0.01)))
	root.add_child(Props.box(Vector3(0.32, 0.01, 0.12), Mats.std(Color(1, 1, 1), 0.6), Vector3(-0.1, 0.99, 0.23), Vector3(-20, 0, 0)))
	root.add_child(Props.box(Vector3(0.06, 0.03, 0.01), Mats.emit(Color(0.2, 1.0, 0.3), 3.0), Vector3(0.05, 1.1, 0.181)))
	root.add_child(Props.cyl(0.07, 0.07, 0.2, Mats.std(Color(1, 1, 1), 0.6), Vector3(0.3, 1.0, 0), Vector3(0, 0, 90)))
	root.add_child(Props.box(Vector3(0.14, 0.1, 0.02), Mats.emit(Color(0.4, 0.75, 1.0), 1.0), Vector3(-0.22, 1.1, 0.181)))
	return {"size": Vector3(1.0, 1.2, 0.65), "center": Vector3(0, 0.6, 0), "label_h": 1.65, "content": null}

# ---- Versand ------------------------------------------------------------------------------------
static func _ship(root: Node3D, stage: int) -> Dictionary:
	if stage == 0:
		var yellow := Mats.std(Color(0.98, 0.78, 0.1), 0.45)
		root.add_child(Props.box(Vector3(1.0, 1.35, 0.75), yellow, Vector3(0, 0.75, 0)))
		root.add_child(Props.box(Vector3(1.06, 0.08, 0.8), Mats.std(Color(0.2, 0.2, 0.22), 0.5), Vector3(0, 1.46, 0)))
		root.add_child(Props.box(Vector3(0.7, 0.12, 0.02), Mats.std(Color(0.1, 0.1, 0.1), 0.4), Vector3(0, 1.15, 0.38)))
		root.add_child(Props.box(Vector3(1.0, 0.16, 0.01), Mats.std(Color(0.85, 0.12, 0.12), 0.5), Vector3(0, 0.75, 0.38)))
		var l := Props.label("PaketBlitz", 72, Color(0.12, 0.12, 0.12), Vector3(0, 0.45, 0.385), false, 0)
		root.add_child(l)
		root.add_child(Props.box(Vector3(0.9, 0.08, 0.65), Mats.dark_metal(), Vector3(0, 0.04, 0)))
		return {"size": Vector3(1.0, 1.5, 0.75), "center": Vector3(0, 0.75, 0), "label_h": 1.9, "content": null}
	var wire := Mats.std(Color(0.7, 0.72, 0.75), 0.35, 0.8)
	root.add_child(Props.box(Vector3(1.2, 0.06, 0.8), wire, Vector3(0, 0.18, 0)))
	for fx in [-0.5, 0.5]:
		for fz in [-0.32, 0.32]:
			root.add_child(Props.cyl(0.07, 0.07, 0.05, Mats.std(Color(0.1, 0.1, 0.1)), Vector3(fx, 0.07, fz), Vector3(0, 0, 90)))
	for i in 7:
		root.add_child(Props.box(Vector3(0.02, 1.5, 0.02), wire, Vector3(-0.6 + i * 0.2, 0.95, -0.4)))
		root.add_child(Props.box(Vector3(0.02, 1.5, 0.02), wire, Vector3(-0.6 + i * 0.2, 0.95, 0.4)))
	for i in 5:
		root.add_child(Props.box(Vector3(0.02, 1.5, 0.02), wire, Vector3(-0.6, 0.95, -0.4 + i * 0.2)))
		root.add_child(Props.box(Vector3(0.02, 1.5, 0.02), wire, Vector3(0.6, 0.95, -0.4 + i * 0.2)))
	for j in 4:
		root.add_child(Props.box(Vector3(1.2, 0.02, 0.02), wire, Vector3(0, 0.3 + j * 0.45, 0.4)))
		root.add_child(Props.box(Vector3(1.2, 0.02, 0.02), wire, Vector3(0, 0.3 + j * 0.45, -0.4)))
	var sign := Props.sign_board("VERSAND", Color(0.2, 0.7, 0.35), Color(1, 1, 1), Vector2(1.0, 0.25))
	sign.position = Vector3(0, 1.9, 0)
	root.add_child(sign)
	var c := _content(root, Vector3(0, 0.21, 0))
	return {"size": Vector3(1.2, 1.7, 0.8), "center": Vector3(0, 0.85, 0), "label_h": 2.35, "content": c}

# ---- Falttisch -----------------------------------------------------------------------------------
static func _fold(root: Node3D) -> Dictionary:
	root.add_child(Props.table(1.4, 0.75, 0.9, Mats.shader("planks", {"wood": Color(0.78, 0.64, 0.46), "plank_w": 0.15}), Mats.dark_metal()))
	var half := Node3D.new()
	half.position = Vector3(0.38, 0.9, 0)
	root.add_child(half)
	half.add_child(Props.box(Vector3(0.36, 0.26, 0.3), Mats.cardboard(), Vector3(0, 0.13, 0)))
	half.add_child(Props.box(Vector3(0.36, 0.01, 0.16), Mats.cardboard(), Vector3(0, 0.3, 0.2), Vector3(-55, 0, 0)))
	var c := _content(root, Vector3(-0.22, 0.9, 0))
	return {"size": Vector3(1.4, 1.0, 0.75), "center": Vector3(0, 0.5, 0), "label_h": 1.55, "content": c}

# ---- Förderband-Einlauf -----------------------------------------------------------------------------
static func _conveyor_in(root: Node3D) -> Dictionary:
	var frame := Mats.std(Color(0.2, 0.45, 0.25), 0.5, 0.4)
	root.add_child(Props.box(Vector3(1.0, 0.8, 0.9), frame, Vector3(0, 0.4, 0)))
	root.add_child(Props.box(Vector3(0.9, 0.04, 0.8), Mats.shader("conveyor"), Vector3(0, 0.82, 0)))
	for sz in [-0.43, 0.43]:
		root.add_child(Props.box(Vector3(1.0, 0.12, 0.04), Mats.std(Color(0.95, 0.8, 0.1), 0.5), Vector3(0, 0.88, sz)))
	root.add_child(Props.box(Vector3(0.12, 0.06, 0.02), Mats.emit(Color(0.2, 1.0, 0.3), 3.0), Vector3(0.35, 0.65, 0.451)))
	return {"size": Vector3(1.0, 0.95, 0.9), "center": Vector3(0, 0.47, 0), "label_h": 1.5, "content": null}

# ---- Imbiss --------------------------------------------------------------------------------------------
static func _diner_pass(root: Node3D) -> Dictionary:
	var steel := Mats.std(Color(0.78, 0.8, 0.82), 0.25, 0.9)
	for sx in [-0.55, 0.55]:
		root.add_child(Props.box(Vector3(0.04, 0.55, 0.04), steel, Vector3(sx, 1.28, 0)))
	root.add_child(Props.box(Vector3(1.2, 0.03, 0.5), steel, Vector3(0, 1.03, 0)))
	root.add_child(Props.box(Vector3(1.2, 0.06, 0.18), steel, Vector3(0, 1.56, 0)))
	root.add_child(Props.box(Vector3(1.0, 0.02, 0.1), Mats.emit(Color(1.0, 0.45, 0.15), 3.0), Vector3(0, 1.52, 0)))
	var bell := Props.sphere(0.05, Mats.std(Color(0.9, 0.75, 0.3), 0.2, 0.9), Vector3(0.45, 1.07, 0.15))
	root.add_child(bell)
	var c := _content(root, Vector3(0, 1.045, 0))
	return {"size": Vector3(1.2, 1.7, 0.6), "center": Vector3(0, 0.85, 0), "label_h": 1.95, "content": c}

static func _diner_table(root: Node3D, opts: Dictionary) -> Dictionary:
	var red := Mats.std(Color(0.78, 0.14, 0.16), 0.45)
	root.add_child(Props.table(1.0, 0.75, 0.76, Mats.std(Color(0.95, 0.94, 0.9), 0.35), Mats.metal()))
	for sz in [-0.62, 0.62]:
		root.add_child(Props.box(Vector3(1.0, 0.45, 0.45), red, Vector3(0, 0.225, sz)))
		root.add_child(Props.box(Vector3(1.0, 0.55, 0.12), red, Vector3(0, 0.72, sz + (0.2 if sz > 0 else -0.2))))
	root.add_child(Props.cyl(0.03, 0.03, 0.12, Mats.std(Color(0.9, 0.9, 0.9), 0.3), Vector3(0.3, 0.83, 0.1)))
	root.add_child(Props.cyl(0.03, 0.03, 0.12, Mats.std(Color(0.85, 0.2, 0.15), 0.3), Vector3(0.36, 0.83, 0.1)))
	var num := Props.label(str(opts.get("table_id", 0)), 80, Color(0.15, 0.15, 0.15), Vector3(-0.3, 0.9, 0), true, 6)
	root.add_child(num)
	var c := _content(root, Vector3(0, 0.77, 0))
	return {"size": Vector3(1.0, 0.8, 0.8), "center": Vector3(0, 0.4, 0), "label_h": 1.4, "content": c}

static func _kalle(root: Node3D) -> Dictionary:
	var npc := NPC.new()
	npc.setup({"skin": Color(0.93, 0.75, 0.6), "hair": Color(0.2, 0.15, 0.1), "shirt": Color(0.92, 0.92, 0.9),
		"pants": Color(0.2, 0.2, 0.22), "apron": true, "mustache": true})
	root.add_child(npc)
	var head: Node3D = npc.model.get_meta("head")
	head.add_child(Props.box(Vector3(0.26, 0.22, 0.26), Mats.std(Color(0.98, 0.98, 0.97), 0.8), Vector3(0, 0.48, 0)))
	head.add_child(Props.box(Vector3(0.3, 0.06, 0.3), Mats.std(Color(0.98, 0.98, 0.97), 0.8), Vector3(0, 0.37, 0)))
	root.set_meta("npc", npc)
	return {"size": Vector3(0.7, 1.9, 0.7), "center": Vector3(0, 0.95, 0), "label_h": 2.55, "content": null}

# ---- Feierabend ---------------------------------------------------------------------------------------
static func _end_day(root: Node3D, stage: int) -> Dictionary:
	if stage == 0:
		root.add_child(Props.box(Vector3(1.4, 0.22, 2.0), Mats.std(Color(0.9, 0.9, 0.88), 0.9), Vector3(0, 0.11, 0)))
		root.add_child(Props.box(Vector3(1.42, 0.06, 1.3), Mats.std(Color(0.25, 0.35, 0.6), 0.9), Vector3(0, 0.25, 0.32)))
		root.add_child(Props.box(Vector3(0.6, 0.12, 0.35), Mats.std(Color(0.97, 0.97, 0.97), 0.9), Vector3(0, 0.28, -0.7)))
		root.add_child(Props.box(Vector3(0.16, 0.1, 0.07), Mats.std(Color(0.8, 0.15, 0.15), 0.4), Vector3(0.6, 0.05, -1.1)))
		root.add_child(Props.box(Vector3(0.1, 0.04, 0.01), Mats.emit(Color(1.0, 0.2, 0.2), 3.0), Vector3(0.6, 0.06, -1.064)))
		return {"size": Vector3(1.4, 0.45, 2.0), "center": Vector3(0, 0.22, 0), "label_h": 0.9, "content": null}
	root.add_child(Props.box(Vector3(0.12, 1.25, 0.12), Mats.dark_metal(), Vector3(0, 0.625, 0)))
	root.add_child(Props.box(Vector3(0.45, 0.55, 0.25), Mats.std(Color(0.85, 0.83, 0.78), 0.5), Vector3(0, 1.45, 0)))
	root.add_child(Props.box(Vector3(0.3, 0.12, 0.01), Mats.emit(Color(0.3, 1.0, 0.45), 2.0), Vector3(0, 1.58, 0.126)))
	root.add_child(Props.box(Vector3(0.12, 0.02, 0.05), Mats.std(Color(0.15, 0.15, 0.15)), Vector3(0, 1.32, 0.13)))
	return {"size": Vector3(0.5, 1.75, 0.4), "center": Vector3(0, 0.87, 0), "label_h": 2.1, "content": null}
