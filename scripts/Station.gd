extends StaticBody3D
class_name Station
## Eine interaktive Station (Laptop, Regal, Packtisch ...). Der Spieler zielt mit dem
## Fadenkreuz darauf: get_prompt() liefert den Hinweistext, interact() führt die Aktion aus,
## set_highlighted() lässt die Station aufleuchten. Dynamische Inhalte (Kisten im Regal,
## Pakete auf dem Tisch) werden bei jeder Wirtschaftsänderung aktualisiert.

enum StationType { PC, DOCK, REGAL, PACK, LABEL, SHIP, FOLD, CONVEYOR, DINER_PASS, DINER_TABLE, NPC_TALK, END_DAY }

var station_type: int = StationType.PC
var product_index: int = 0
var table_id: int = 0
var stage: int = 0
var kit: Dictionary = {}
var content: Node3D = null
var _meshes: Array[MeshInstance3D] = []
var _sig: String = "-"
var _highlighted: bool = false
var _name_label: Label3D = null

func setup(type: int, opts: Dictionary) -> void:
	station_type = type
	product_index = int(opts.get("product_index", 0))
	table_id = int(opts.get("table_id", 0))
	stage = int(opts.get("stage", 0))
	var visual := Node3D.new()
	add_child(visual)
	kit = StationKit.build(visual, type, opts)
	content = kit.get("content", null)
	var col := CollisionShape3D.new()
	var shape := BoxShape3D.new()
	shape.size = kit["size"]
	col.shape = shape
	col.position = kit["center"]
	add_child(col)
	_collect(visual)
	_name_label = Props.label(display_name(), 34, Color(1, 1, 1, 0.92), Vector3(0, float(kit["label_h"]), 0), true, 8)
	_name_label.visibility_range_end = 9.0
	_name_label.visibility_range_end_margin = 2.0
	_name_label.visibility_range_fade_mode = GeometryInstance3D.VISIBILITY_RANGE_FADE_SELF
	add_child(_name_label)
	add_to_group("stations")
	GameManager.economy_changed.connect(_refresh)
	_refresh()

func _collect(node: Node) -> void:
	if node == content:
		return
	if node is MeshInstance3D:
		_meshes.append(node)
	for c in node.get_children():
		_collect(c)

func product_id() -> String:
	return GameData.PRODUCTS[product_index]["id"]

func display_name() -> String:
	match station_type:
		StationType.PC: return "Laptop" if stage == 0 else "Büro-PC"
		StationType.DOCK: return "Wareneingang"
		StationType.REGAL: return "Regal: " + GameData.PRODUCTS[product_index]["name"]
		StationType.PACK: return "Packtisch"
		StationType.LABEL: return "Labeldrucker"
		StationType.SHIP: return "PaketBlitz-Abgabe" if stage == 0 else "Versandkäfig"
		StationType.FOLD: return "Falttisch"
		StationType.CONVEYOR: return "Förderband"
		StationType.DINER_PASS: return "Durchreiche"
		StationType.DINER_TABLE: return "Tisch %d" % table_id
		StationType.NPC_TALK: return "Kalle (Chef)"
		StationType.END_DAY: return "Matratze" if stage == 0 else "Stempeluhr"
	return ""

func set_highlighted(on: bool) -> void:
	if on == _highlighted:
		return
	_highlighted = on
	var mat: Material = Mats.highlight() if on else null
	for m in _meshes:
		if is_instance_valid(m):
			m.material_overlay = mat

func _is_business() -> bool:
	return station_type <= StationType.CONVEYOR or station_type == StationType.END_DAY

# ---- Hinweistext ------------------------------------------------------------------------
func get_prompt(player: Node) -> String:
	var gm := GameManager
	var kind: int = player.held_kind
	var held: Dictionary = player.held
	if _is_business() and gm.story_stage == "diner":
		return "Erst die Schicht bei Kalle beenden"
	match station_type:
		StationType.PC:
			return "Laptop öffnen" if stage == 0 else "PC benutzen"
		StationType.DOCK:
			if kind == GameManager.ItemKind.CRATE:
				return "Kiste zurückstellen"
			if kind != GameManager.ItemKind.NONE:
				return "Hände voll"
			if gm.dock_crates.is_empty():
				return "Keine Lieferung da" if gm.traveling_deliveries.is_empty() else "Lieferung ist unterwegs ..."
			var c: Dictionary = gm.dock_crates[0]
			return "Kiste aufnehmen (%d× %s)" % [int(c["quantity"]), GameData.product(c["product"])["name"]]
		StationType.REGAL:
			var pid := product_id()
			var pname: String = GameData.PRODUCTS[product_index]["name"]
			if not gm.product_unlocked(pid):
				return "Gesperrt – ab Firmenlevel %d" % int(GameData.PRODUCTS[product_index]["unlock_level"])
			if kind == GameManager.ItemKind.CRATE:
				if held.get("product", "") != pid:
					return "Falsches Regal (Kiste: %s)" % GameData.product(held.get("product", ""))["name"]
				return "Kiste einräumen (+%d)" % int(held.get("quantity", 0))
			if kind == GameManager.ItemKind.ITEM:
				return "Zurücklegen" if held.get("product", "") == pid else "Falsches Regal"
			if kind != GameManager.ItemKind.NONE:
				return "Hände voll"
			var pending := gm.pending_count_for(pid)
			if gm.stock_qty(pid) <= 0:
				return "Regal leer – im Laptop nachbestellen"
			if pending <= 0:
				return "%s: %d auf Lager · keine Bestellung" % [pname, gm.stock_qty(pid)]
			return "%s entnehmen (%d offen)" % [pname, pending]
		StationType.PACK:
			if kind == GameManager.ItemKind.ITEM:
				var size := int(GameData.product(held.get("product", ""))["size"])
				return "Verpacken (Karton %s · %d übrig)" % [GameData.size_name(size), int(gm.packaging[size])]
			if kind == GameManager.ItemKind.PACKAGE:
				return "Paket zurücklegen"
			if kind != GameManager.ItemKind.NONE:
				return "Hände voll"
			return "Paket nehmen (%d bereit)" % gm.packed_count() if gm.packed_count() > 0 else "Bring einen Artikel zum Verpacken"
		StationType.LABEL:
			if kind == GameManager.ItemKind.PACKAGE:
				return "Versandlabel drucken"
			if kind == GameManager.ItemKind.LABELED:
				return "Hat schon ein Label"
			return "Bring ein verpacktes Paket hierher"
		StationType.SHIP:
			if kind == GameManager.ItemKind.LABELED:
				return "Paket abgeben (+%s)" % UITheme.money(int(held.get("price", 0)))
			if kind == GameManager.ItemKind.PACKAGE:
				return "Erst ein Versandlabel drucken!"
			return "Etikettierte Pakete hier abgeben"
		StationType.FOLD:
			if kind != GameManager.ItemKind.NONE:
				return "Hände frei machen zum Falten"
			return "Karton falten (%d ungefaltet)" % gm.flat_total() if gm.flat_total() > 0 else "Keine ungefalteten Kartons"
		StationType.CONVEYOR:
			if kind == GameManager.ItemKind.LABELED:
				return "Aufs Förderband legen"
			if kind == GameManager.ItemKind.PACKAGE:
				return "Erst etikettieren"
			return "Etikettierte Pakete aufs Band legen"
		StationType.DINER_PASS:
			if gm.story_stage != "diner":
				return "Die Küche ist nicht mehr dein Problem"
			if kind == GameManager.ItemKind.PLATE:
				return "Bring den Teller zu Tisch %d" % int(held.get("table", 0))
			if gm.intro_step != 1:
				return "Sprich zuerst mit Kalle"
			return "Teller nehmen" if gm.diner_plates_waiting() > 0 else "Keine Teller mehr"
		StationType.DINER_TABLE:
			if kind == GameManager.ItemKind.PLATE:
				return "Teller servieren" if int(held.get("table", 0)) == table_id else "Falscher Tisch (Teller für Tisch %d)" % int(held.get("table", 0))
			return "Tisch %d" % table_id
		StationType.NPC_TALK:
			return "Mit Kalle sprechen"
		StationType.END_DAY:
			return "Feierabend machen (Tag beenden)"
	return ""

# ---- Aktion -------------------------------------------------------------------------------
func interact(player: Node) -> void:
	var gm := GameManager
	var kind: int = player.held_kind
	var held: Dictionary = player.held
	if _is_business() and gm.story_stage == "diner":
		gm.notify("Die Schicht ist noch nicht vorbei. Kalle wartet!", "bad")
		return
	match station_type:
		StationType.PC:
			gm.open_pc()
		StationType.DOCK:
			if kind == GameManager.ItemKind.CRATE:
				gm.return_crate(held)
				player.clear_hands()
				Audio.play("place")
			elif kind != GameManager.ItemKind.NONE:
				gm.notify("Hände sind voll.", "bad")
			else:
				var crate := gm.pickup_crate()
				if not crate.is_empty():
					player.hold(GameManager.ItemKind.CRATE, crate)
					Audio.play("pickup")
		StationType.REGAL:
			_interact_regal(player, kind, held)
		StationType.PACK:
			if kind == GameManager.ItemKind.ITEM:
				if gm.wrap_item(held):
					player.clear_hands()
					Audio.play("tape")
			elif kind == GameManager.ItemKind.PACKAGE:
				gm.return_package(held)
				player.clear_hands()
				Audio.play("place")
			elif kind == GameManager.ItemKind.NONE:
				var pkg := gm.pickup_package()
				if not pkg.is_empty():
					player.hold(GameManager.ItemKind.PACKAGE, pkg)
					Audio.play("pickup")
			else:
				gm.notify("Damit kannst du hier nichts anfangen.", "info")
		StationType.LABEL:
			if kind == GameManager.ItemKind.PACKAGE:
				player.hold(GameManager.ItemKind.LABELED, held)
				gm.on_labeled()
				Audio.play("printer")
			else:
				gm.notify("Bring ein verpacktes Paket zum Labeldrucker.", "info")
		StationType.SHIP:
			if kind == GameManager.ItemKind.LABELED:
				gm.ship_package(held, global_position + Vector3(0, 1.8, 0))
				player.clear_hands()
			elif kind == GameManager.ItemKind.PACKAGE:
				gm.notify("Ohne Versandlabel nimmt PaketBlitz nichts an!", "bad")
				Audio.play("error")
			else:
				gm.notify("Hier gibst du etikettierte Pakete ab.", "info")
		StationType.FOLD:
			if kind != GameManager.ItemKind.NONE:
				gm.notify("Zum Falten brauchst du freie Hände.", "info")
			else:
				gm.fold_carton()
		StationType.CONVEYOR:
			if kind == GameManager.ItemKind.LABELED:
				if gm.conveyor_insert(held):
					player.clear_hands()
					Audio.play("place")
			else:
				gm.notify("Nur etikettierte Pakete aufs Band legen.", "info")
		StationType.DINER_PASS:
			if gm.story_stage == "diner" and kind == GameManager.ItemKind.NONE:
				var plate := gm.diner_take_plate()
				if not plate.is_empty():
					player.hold(GameManager.ItemKind.PLATE, plate)
					Audio.play("plate")
		StationType.DINER_TABLE:
			if kind == GameManager.ItemKind.PLATE:
				if gm.diner_serve(table_id, held):
					player.clear_hands()
					Audio.play("plate")
				else:
					gm.notify("Das ist Tisch %d – der Teller gehört an Tisch %d." % [table_id, int(held.get("table", 0))], "bad")
		StationType.NPC_TALK:
			gm.dialogue_requested.emit("kalle")
		StationType.END_DAY:
			gm.end_day_requested.emit()

func _interact_regal(player: Node, kind: int, held: Dictionary) -> void:
	var gm := GameManager
	var pid := product_id()
	if not gm.product_unlocked(pid):
		gm.notify("Dieses Produkt wird erst ab Firmenlevel %d freigeschaltet." % int(GameData.PRODUCTS[product_index]["unlock_level"]), "info")
		return
	match kind:
		GameManager.ItemKind.CRATE:
			if held.get("product", "") != pid:
				gm.notify("Falsches Regal! Die Kiste gehört zu '%s'." % GameData.product(held.get("product", ""))["name"], "bad")
				Audio.play("error")
				return
			if gm.unbox_crate(pid, int(held["quantity"]), float(held["quality"])):
				player.clear_hands()
				Audio.play("place")
		GameManager.ItemKind.ITEM:
			if held.get("product", "") == pid:
				gm.return_item(held)
				player.clear_hands()
				Audio.play("place")
			else:
				gm.notify("Das gehört in ein anderes Regal.", "bad")
		GameManager.ItemKind.NONE:
			var item := gm.pick_item(pid)
			if not item.is_empty():
				player.hold(GameManager.ItemKind.ITEM, item)
				Audio.play("pickup")
		_:
			gm.notify("Damit kannst du hier nichts anfangen.", "info")

# ---- Dynamische Inhalte ------------------------------------------------------------------------
func _signature() -> String:
	var gm := GameManager
	match station_type:
		StationType.DOCK:
			var s := ""
			for c in gm.dock_crates:
				s += String(c["product"]) + ","
			return s
		StationType.REGAL:
			return str(_regal_visible())
		StationType.PACK:
			var s2 := ""
			for p in gm.packed_packages:
				s2 += str(p["color"]) + String(p["product"])
			return s2
		StationType.FOLD:
			return str(mini(gm.flat_total(), 12))
		StationType.SHIP:
			return str(mini(int(gm.daily.get("shipped", 0)), 10))
		StationType.DINER_PASS:
			return str(gm.diner_plates_waiting()) + gm.story_stage
		StationType.DINER_TABLE:
			return str(gm.diner_table_state(table_id))
	return ""

func _regal_visible() -> int:
	var qty := GameManager.stock_qty(product_id())
	if qty <= 0:
		return 0
	if stage == 0:
		return clampi(ceili(qty / 10.0), 1, 20)
	return clampi(ceili(qty / 25.0), 1, 36)

func _refresh() -> void:
	if content == null:
		return
	var sig := _signature()
	if sig == _sig:
		return
	_sig = sig
	for c in content.get_children():
		c.queue_free()
	match station_type:
		StationType.DOCK:
			_fill_dock()
		StationType.REGAL:
			_fill_regal()
		StationType.PACK:
			_fill_packed()
		StationType.FOLD:
			_fill_flat()
		StationType.SHIP:
			_fill_ship()
		StationType.DINER_PASS:
			_fill_plates()
		StationType.DINER_TABLE:
			_fill_table()

func _fill_dock() -> void:
	var crates: Array = GameManager.dock_crates
	var slots: Array = []
	if stage == 0:
		for layer in 3:
			for sx in [-0.3, 0.3]:
				slots.append(Vector3(sx, layer * 0.46, 0))
	else:
		for layer in 2:
			for px in [-0.65, 0.65]:
				for sz in [-0.24, 0.24]:
					slots.append(Vector3(px, layer * 0.46, sz))
	for i in mini(crates.size(), slots.size()):
		var col: Color = GameData.product(crates[i]["product"])["color"]
		var cr := Props.crate(col)
		cr.position = slots[i]
		content.add_child(cr)

func _fill_regal() -> void:
	var n := _regal_visible()
	var col: Color = GameData.PRODUCTS[product_index]["color"]
	var body := Mats.std(col, 0.55)
	var band := Mats.std(Color(0.97, 0.97, 0.96), 0.5)
	if stage == 0:
		for i in n:
			var level := i / 5
			var slot := i % 5
			var pos := Vector3(-0.72 + slot * 0.36, 0.115 + level * 0.5 + 0.11, 0)
			content.add_child(Props.box(Vector3(0.3, 0.22, 0.34), body, pos))
			content.add_child(Props.box(Vector3(0.302, 0.05, 0.342), band, pos + Vector3(0, 0.04, 0)))
	else:
		for i in n:
			var level := i / 12
			var rest := i % 12
			var pal := rest / 6
			var k := rest % 6
			var x := (-1.0 if pal == 0 else 1.0) + (-0.42 + (k % 3) * 0.42)
			var z := -0.22 + (k / 3) * 0.44
			var pos := Vector3(x, 0.15 + level * 1.15 + 0.05 + 0.18, z)
			content.add_child(Props.box(Vector3(0.4, 0.36, 0.4), body, pos))
			content.add_child(Props.box(Vector3(0.402, 0.07, 0.402), band, pos + Vector3(0, 0.06, 0)))

func _fill_packed() -> void:
	var pkgs: Array = GameManager.packed_packages
	var shown := mini(pkgs.size(), 6)
	for i in shown:
		var pkg: Dictionary = pkgs[pkgs.size() - shown + i]
		var vis := ItemKit.build(GameManager.ItemKind.PACKAGE, pkg)
		var b := ItemKit.bounds(GameManager.ItemKind.PACKAGE, pkg)
		var col := i % 2
		var layer := i / 2
		vis.position = Vector3(-0.12 + col * 0.26, b.y / 2.0 + layer * 0.3, 0)
		vis.rotation_degrees.y = (i * 17) % 20 - 10
		content.add_child(vis)

func _fill_flat() -> void:
	var n := mini(GameManager.flat_total(), 12)
	for i in n:
		content.add_child(Props.box(Vector3(0.62, 0.012, 0.5), Mats.cardboard(), Vector3(0, 0.008 + i * 0.014, 0), Vector3(0, (i * 7) % 9 - 4, 0)))

func _fill_ship() -> void:
	if stage == 0:
		return
	var n := mini(int(GameManager.daily.get("shipped", 0)), 10)
	for i in n:
		var pos := Vector3(-0.35 + (i % 3) * 0.35, 0.15 + (i / 6) * 0.3, -0.18 + ((i / 3) % 2) * 0.36)
		content.add_child(Props.box(Vector3(0.3, 0.26, 0.3), Mats.cardboard(), pos, Vector3(0, i * 23 % 30, 0)))

func _fill_plates() -> void:
	var n := GameManager.diner_plates_waiting()
	for i in n:
		var plate := ItemKit.build(GameManager.ItemKind.PLATE, {})
		plate.position = Vector3(-0.36 + i * 0.36, 0.06, 0)
		content.add_child(plate)

func _fill_table() -> void:
	var state := GameManager.diner_table_state(table_id)
	if state == 1:
		var l := Props.label("🍔 Wartet auf Essen", 44, Color(1.0, 0.75, 0.3), Vector3(0, 1.0, 0), true, 10)
		content.add_child(l)
	elif state == 2:
		var plate := ItemKit.build(GameManager.ItemKind.PLATE, {})
		plate.position = Vector3(0, 0.07, 0.1)
		content.add_child(plate)
