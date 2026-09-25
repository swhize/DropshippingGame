extends Node
## Integrationstest in der echten Spielszene: spielt Intro und kompletten Geschäftskreislauf
## über die 3D-Stationen (Station.interact) durch, prüft Hinweistexte, Ausbau, Ablegen,
## Speichern und Laden. Start: godot --headless --path . res://scenes/Main.tscn -- --selftest

var main: Node
var player: Node
var world: Node
var fails: int = 0
var checks: int = 0

func check(cond: bool, what: String) -> void:
	checks += 1
	if cond:
		print("  ok   " + what)
	else:
		fails += 1
		print("  FAIL " + what)

func station(type: int, product_index: int = -1, table_id: int = -1) -> Node:
	for s in get_tree().get_nodes_in_group("stations"):
		if s.is_queued_for_deletion():
			continue
		if s.station_type != type:
			continue
		if product_index >= 0 and s.product_index != product_index:
			continue
		if table_id >= 0 and s.table_id != table_id:
			continue
		return s
	return null

func use(type: int, product_index: int = -1, table_id: int = -1) -> String:
	var s := station(type, product_index, table_id)
	if s == null:
		return "<keine Station>"
	var p: String = s.get_prompt(player)
	s.interact(player)
	return p

func _all_prompts() -> bool:
	for s in get_tree().get_nodes_in_group("stations"):
		if s.is_queued_for_deletion():
			continue
		var p: String = s.get_prompt(player)
		if p == "":
			return false
	return true

func run() -> void:
	main = get_parent().get_parent()
	player = main.get("player")
	world = main.get("world")
	var gm := GameManager
	var S := Station.StationType
	print("== Selbsttest: Imbiss-Intro ==")
	gm.new_game("intro")
	world.rebuild_stations()
	world.refresh_world(false)
	await get_tree().process_frame
	check(station(S.NPC_TALK) != null and station(S.DINER_PASS) != null and station(S.DINER_TABLE, -1, 3) != null, "Imbiss-Stationen vorhanden")
	check(_all_prompts(), "Alle Stationen liefern Hinweistexte (Intro)")
	var p0 := use(S.PC)
	check(p0.begins_with("Erst die Schicht"), "Business-Stationen während der Schicht gesperrt")
	gm.start_shift()
	for i in 3:
		use(S.DINER_PASS)
		check(player.held_kind == GameManager.ItemKind.PLATE, "Teller %d aufgenommen" % (i + 1))
		var t := int(player.held["table"])
		var wrong := 2 if t != 2 else 3
		use(S.DINER_TABLE, -1, wrong)
		check(player.held_kind == GameManager.ItemKind.PLATE, "Falscher Tisch behält Teller")
		use(S.DINER_TABLE, -1, t)
		check(player.is_empty(), "Teller an Tisch %d serviert" % t)
	check(gm.intro_step == 2, "Intro: zurück zu Kalle")
	main.call("_do_finish_intro")
	await get_tree().process_frame
	await get_tree().process_frame
	check(gm.story_stage == "business" and gm.money == GameData.START_CAPITAL, "Kündigung → Garage mit Startkapital")
	check(station(S.REGAL, 0) != null and station(S.SHIP) != null, "Garagen-Stationen gebaut")
	check(player.global_position.distance_to(WorldBuilder.SPAWN_GARAGE) < 1.0, "Spieler steht in der Garage")

	print("== Selbsttest: Geschäftskreislauf über Stationen ==")
	use(S.PC)
	check(gm.pc_open and gm.tutorial_step == 1, "Laptop öffnet sich, Tutorial weiter")
	gm.close_pc()
	check(not gm.is_input_locked(), "Eingabe nach dem Schließen wieder frei")
	gm.buy_bulk(0, 1, 1)
	gm.advance_minutes(gm.lead_minutes(1) + 1.0)
	await get_tree().process_frame
	var dock := station(S.DOCK)
	check(dock.content.get_child_count() > 0, "Kiste liegt sichtbar auf der Lieferpalette")
	check(use(S.DOCK).begins_with("Kiste aufnehmen"), "Hinweis 'Kiste aufnehmen'")
	check(player.held_kind == GameManager.ItemKind.CRATE, "Kiste in der Hand")
	check(use(S.REGAL, 1).begins_with("Gesperrt"), "Falsches (gesperrtes) Regal lehnt ab")
	check(player.held_kind == GameManager.ItemKind.CRATE, "Kiste noch in der Hand")
	use(S.REGAL, 0)
	await get_tree().process_frame
	check(player.is_empty() and gm.stock_qty("huelle") == 50, "Kiste eingeräumt (50 Stück)")
	check(station(S.REGAL, 0).content.get_child_count() > 0, "Regal zeigt Ware")
	gm.set_listed("huelle", true)
	gm.advance_minutes(5.0)
	check(gm.pending_count() >= 1, "Bestellung eingegangen")
	check(use(S.REGAL, 0).contains("entnehmen"), "Hinweis 'entnehmen'")
	check(player.held_kind == GameManager.ItemKind.ITEM, "Artikel in der Hand")
	check(use(S.PACK).begins_with("Verpacken"), "Hinweis 'Verpacken'")
	await get_tree().process_frame
	check(player.is_empty() and gm.packed_count() == 1, "Verpackt")
	check(station(S.PACK).content.get_child_count() > 0, "Paket liegt sichtbar auf dem Packtisch")
	use(S.PACK)
	check(player.held_kind == GameManager.ItemKind.PACKAGE, "Paket aufgenommen")
	check(use(S.SHIP).begins_with("Erst ein Versandlabel"), "Versand ohne Label abgelehnt")
	use(S.LABEL)
	check(player.held_kind == GameManager.ItemKind.LABELED, "Label gedruckt")
	var money_before: int = gm.money
	check(use(S.SHIP).begins_with("Paket abgeben"), "Hinweis 'Paket abgeben'")
	check(player.is_empty() and gm.total_shipped == 1 and gm.money > money_before, "Paket verschickt, Geld erhalten")
	check(gm.tutorial_step == -1, "Tutorial über die Stationen abgeschlossen")
	check(_all_prompts(), "Alle Stationen liefern Hinweistexte (Garage)")

	print("== Selbsttest: Ablegen & Aufheben ==")
	player.hold(GameManager.ItemKind.CRATE, {"product": "huelle", "quantity": 20, "quality": 1.0})
	player.drop_held_item()
	await get_tree().process_frame
	var dropped := get_tree().get_nodes_in_group("dropped_items")
	check(dropped.size() == 1 and player.is_empty(), "Kiste abgelegt (Physik-Objekt)")
	for i in 30:
		await get_tree().physics_frame
	dropped[0].interact(player)
	check(player.held_kind == GameManager.ItemKind.CRATE and int(player.held["quantity"]) == 20, "Kiste wieder aufgehoben, Menge erhalten")
	player.drop_held_item()
	await get_tree().process_frame

	print("== Selbsttest: Lagerhalle ==")
	gm.level = 6
	gm.money = 20000
	gm.buy_upgrade("warehouse")
	await get_tree().process_frame
	await get_tree().process_frame
	var regals := 0
	for s in get_tree().get_nodes_in_group("stations"):
		if not s.is_queued_for_deletion() and s.station_type == S.REGAL:
			regals += 1
	check(regals == 6, "Lagerhalle: 6 Regale (%d)" % regals)
	gm.buy_upgrade("conveyor")
	main.call("_on_location_changed", 1)
	await get_tree().process_frame
	check(station(S.CONVEYOR) != null, "Förderband-Station gebaut")
	player.hold(GameManager.ItemKind.LABELED, {"product": "led", "price": 40, "quality": 1.0, "created": gm.bclock(), "color": Color.RED, "logo": 1})
	use(S.CONVEYOR)
	check(player.is_empty() and gm.conveyor_queue.size() == 1, "Paket liegt auf dem Förderband")
	gm.level = 5
	gm.hire("packer")
	await get_tree().process_frame
	check(world.get("staff_root").get_child_count() == 1, "Mitarbeiter-Figur erscheint")
	check(_all_prompts(), "Alle Stationen liefern Hinweistexte (Lagerhalle)")
	gm.buy_decor("pflanze")
	gm.buy_lifestyle("auto")
	await get_tree().process_frame
	check(world.get("decor_root").get_child_count() >= 2, "Deko & Lifestyle sichtbar in der Welt")

	print("== Selbsttest: Speichern mit Welt-Zustand ==")
	player.hold(GameManager.ItemKind.ITEM, {"product": "led", "price": 38, "quality": 1.0, "created": gm.bclock()})
	gm.save_game(true)
	var saved := gm.save_summary()
	check(not saved.is_empty(), "Spielstand geschrieben")
	var f := FileAccess.open(GameManager.SAVE_PATH, FileAccess.READ)
	var raw: Dictionary = GameManager.from_json_value(JSON.parse_string(f.get_as_text()))
	f.close()
	check(raw["world_items"].size() >= 1, "Abgelegte Kiste im Spielstand")
	check(int(raw["player_state"]["held_kind"]) == GameManager.ItemKind.ITEM, "Gehaltener Gegenstand im Spielstand")
	gm.delete_save()

	print("\n=== SELBSTTEST: %d Checks, %d Fehler ===" % [checks, fails])
	get_tree().quit(1 if fails > 0 else 0)
