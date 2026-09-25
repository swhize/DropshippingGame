extends Node
## Debug-/Verifikationswerkzeug - NUR aktiv, wenn nach "--" eigene Argumente übergeben werden.
## Beispiele:
##   godot --path . -- --screenshot=menu.png                       (Hauptmenü)
##   godot --path . -- --start=intro --screenshot=diner.png        (Imbiss-Intro)
##   godot --path . -- --start=skip --demo --stage=1 --staff --time=14 --cam=17,1,-9,0,-5 --screenshot=wh.png
## Weitere: --app=Webshop --summary --event=shitstorm --pause --help --dialogue --hold=crate
##          --decor --conveyor --advance=MIN --simulate=MIN --print --frames=N --savedemo --drop

var mode: String = "game"
var args: Dictionary = {}

func _ready() -> void:
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--"):
			var kv := a.substr(2).split("=", true, 1)
			args[kv[0]] = kv[1] if kv.size() > 1 else "1"
	if mode == "menu":
		if args.has("start"):
			GameManager.start_mode = String(args["start"])
			get_tree().change_scene_to_file.call_deferred("res://scenes/Main.tscn")
			return
		if args.has("screenshot"):
			_shot()
		return
	await get_tree().process_frame
	await get_tree().process_frame
	if args.has("selftest"):
		var t := Node.new()
		t.set_script(load("res://tests/SelfTest.gd"))
		add_child(t)
		t.call("run")
		return
	_apply()
	if args.has("screenshot"):
		_shot()
	elif args.has("print"):
		_print_state()
		get_tree().quit()

func _apply() -> void:
	var main: Node = get_parent()
	var gm := GameManager
	var world: Node = main.get("world")
	var player: Node = main.get("player")
	if args.has("demo"):
		_demo()
	if args.has("stage") and int(args["stage"]) >= 1 and gm.location_stage < 1:
		gm.upgrades["warehouse"] = true
		gm.location_stage = 1
		gm.location_changed.emit(1)
		player.global_position = world.spawn_point()
	if args.has("conveyor"):
		gm.upgrades["conveyor"] = true
		world.rebuild_stations()
		world.refresh_world(false)
		for i in 3:
			gm.conveyor_insert({"product": "led", "price": 40, "quality": 1.0, "created": gm.bclock(), "color": gm.brand_color, "logo": gm.brand_logo_index})
			gm.conveyor_queue[gm.conveyor_queue.size() - 1]["id"] = 900 + i
			gm.conveyor_queue[gm.conveyor_queue.size() - 1]["done_at"] = gm.bclock() + 4.0 + i * 3.0
		world.sync_conveyor()
	if args.has("staff"):
		gm.staff = [{"role": "lager", "name": "Jonas", "progress": 0.0}, {"role": "packer", "name": "Mia", "progress": 0.0},
			{"role": "versand", "name": "Emre", "progress": 0.0}, {"role": "social", "name": "Chantal", "progress": 0.0}]
		gm.staff_changed.emit()
	if args.has("decor"):
		for d in GameData.DECOR:
			gm.decor_owned[d["id"]] = true
		for l in GameData.LIFESTYLE:
			gm.lifestyle_owned[l["id"]] = true
		gm.world_changed.emit()
	if args.has("time"):
		gm.time_minutes = float(args["time"]) * 60.0
		world.set_time_of_day(float(args["time"]))
	if args.has("advance"):
		gm.advance_minutes(float(args["advance"]))
	if args.has("simulate"):
		var total := float(args["simulate"])
		var done := 0.0
		while done < total and not gm.day_over:
			gm.advance_minutes(2.0)
			done += 2.0
	if args.has("hold"):
		var kinds := {"crate": GameManager.ItemKind.CRATE, "item": GameManager.ItemKind.ITEM, "package": GameManager.ItemKind.PACKAGE,
			"labeled": GameManager.ItemKind.LABELED, "plate": GameManager.ItemKind.PLATE}
		var k: int = kinds.get(String(args["hold"]), GameManager.ItemKind.CRATE)
		player.hold(k, {"product": "led", "quantity": 50, "quality": 1.6, "price": 38, "color": gm.brand_color, "logo": gm.brand_logo_index, "table": 3})
	if args.has("drop"):
		player.hold(GameManager.ItemKind.CRATE, {"product": "massage", "quantity": 200, "quality": 1.6})
		player.drop_held_item()
	if args.has("cam"):
		var c := String(args["cam"]).split(",")
		if not args.has("live"):
			player.set_physics_process(false)
		player.set_process_unhandled_input(false)
		player.global_position = Vector3(float(c[0]), float(c[1]), float(c[2]))
		player.rotation.y = deg_to_rad(float(c[3]))
		player.get("camera").rotation.x = deg_to_rad(float(c[4]))
	if args.has("app"):
		gm.open_pc()
		var pc: Node = get_tree().get_first_node_in_group("pc_screen")
		pc.open_app(String(args["app"]))
	if args.has("summary"):
		gm.request_end_day()
	if args.has("event"):
		Events.trigger(String(args["event"]))
	if args.has("pause"):
		main.get("pause_menu").open()
	if args.has("help"):
		main.call("_show_help")
	if args.has("dialogue"):
		gm.dialogue_requested.emit("kalle")
	if args.has("savedemo"):
		gm.save_game(true)


func _demo() -> void:
	var gm := GameManager
	gm.day = 12
	gm.tutorial_step = -1
	gm.money = 8420
	gm.level = 6
	gm.xp = 5200
	gm.reputation = 4.3
	gm.review_count = 38
	gm.brand_name = "NovaGoods"
	gm.brand_color = GameData.BRAND_PALETTE[5]
	gm.brand_logo_index = 3
	gm.brand_named = true
	for i in 3:
		gm.packaging_brand[i] = {"color": gm.brand_color, "logo": 3}
	gm.packaging = [40, 30, 20]
	gm.flat_packaging = [10, 0, 0]
	for i in GameData.PRODUCTS.size():
		var id: String = GameData.PRODUCTS[i]["id"]
		gm.stock[id] = {"qty": 80 + i * 30, "quality": 1.0 + (0.6 if i % 2 == 0 else 0.0)}
		gm.listed[id] = true
		gm.shipped_per_product[id] = 120 - i * 15
	for id in ["huelle", "led", "massage", "huelle"]:
		gm.order_queue.append({"product": id, "price": gm.current_sale_price(id), "created": gm.bclock()})
	for i in 3:
		gm.packed_packages.append({"product": "led", "price": 38, "quality": 1.0, "created": gm.bclock(), "color": gm.brand_color, "logo": 3})
	gm.dock_crates.append({"product": "huelle", "quantity": 50, "quality": 1.0})
	gm.dock_crates.append({"product": "led", "quantity": 20, "quality": 1.6})
	gm.traveling_deliveries.append({"product": "massage", "quantity": 50, "quality": 1.6, "arrive_at": gm.bclock() + 40.0, "van_sent": false})
	gm.total_shipped = 412
	gm.total_earned = 18450
	var rev := 300
	for d in 11:
		rev = int(rev * randf_range(1.05, 1.25))
		gm.history.append({"day": d + 1, "revenue": rev, "profit": int(rev * randf_range(0.2, 0.45)) - 60, "shipped": int(rev / 30)})
	var texts := [[5, "Top! Kam super schnell an."], [4, "Macht, was es soll."], [5, "Meine Oma liebt es. 10/10."], [2, "Riecht irgendwie nach Plastik."], [5, "Schneller als der Pizzadienst!"]]
	for t in texts:
		gm.reviews.append({"stars": t[0], "text": t[1], "name": "Sabine K.", "product": "led", "day": 11})
	gm.boosts.append({"source": "ad", "name": "Facebook-Ads", "mult": 1.6, "ends_at": gm.bclock() + 80.0, "product": ""})
	for g in ["first_sale", "brand", "ship10", "rev1k", "rating4", "ship100", "rev10k"]:
		gm.goals_done[g] = true
	gm.awareness = 0.45
	gm.daily["revenue"] = 1240
	gm.daily["purchases"] = 310
	gm.daily["shipped"] = 31
	Market.holdings["DROP"] = 40.0
	Market.invested["DROP"] = 400.0
	Events.add_mail("Oma Gerda", "Ein Brief von Oma", "Mein Enkel, der Geschäftsmann! Ich bin so stolz.", "💌")
	gm.economy_changed.emit()
	gm.world_changed.emit()

func _print_state() -> void:
	var gm := GameManager
	var player: Node = get_parent().get("player")
	print("STATE day=%d time=%.0f money=%d shipped=%d stock=%d orders=%d packed=%d level=%d rep=%.2f staff=%d stage=%d dropped=%d held=%d brand=%s" % [
		gm.day, gm.time_minutes, gm.money, gm.total_shipped, gm.stock_total(), gm.pending_count(), gm.packed_count(), gm.level,
		gm.reputation, gm.staff.size(), gm.location_stage, get_tree().get_nodes_in_group("dropped_items").size(),
		int(player.get("held_kind")) if player else -1, gm.brand_name])

func _shot() -> void:
	var wait := float(args.get("wait", "3.0"))
	await get_tree().create_timer(wait, true, false, true).timeout
	for i in 3:
		await get_tree().process_frame
	var img := get_viewport().get_texture().get_image()
	var err := img.save_png(String(args["screenshot"]))
	print("Screenshot gespeichert (%d): %s" % [err, args["screenshot"]])
	if args.has("print"):
		_print_state()
	get_tree().quit()
