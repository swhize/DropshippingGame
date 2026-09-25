extends SceneTree
## Balance-Simulation (godot --headless --path . -s tests/balance_sim.gd): ein "perfekter" Spieler erfüllt jede Bestellung sofort und kauft nach.
func _initialize() -> void:
	var gm: Node = root.get_node("GameManager")
	gm.reset_state()
	gm.new_game("skip")
	gm.in_game = true
	var warehouse_day := -1
	var hand_budget := 0.0
	for d in 25:
		while not gm.day_over:
			gm.advance_minutes(2.0)
			# Nachschub: für jedes verfügbare Produkt Lager auffüllen
			for i in GameData.PRODUCTS.size():
				var id: String = GameData.PRODUCTS[i]["id"]
				if not gm.product_available(id):
					continue
				gm.listed[id] = true
				var bulk := 1
				if gm.stock_qty(id) + gm.traveling_count_for(id) * 50 < 30 and gm.money >= gm.bulk_cost(i, bulk, 1):
					gm.buy_bulk(i, bulk, 1)
			for c in gm.dock_crates.duplicate():
				gm.pickup_crate()
				gm.unbox_crate(c["product"], int(c["quantity"]), float(c["quality"]))
			for i2 in GameData.PRODUCTS.size():
				var pid: String = GameData.PRODUCTS[i2]["id"]
				var sz := int(GameData.PRODUCTS[i2]["size"])
				if gm.product_available(pid) and int(gm.packaging[sz]) < 5 and gm.money >= gm.packaging_cost(sz, false, 0) + 40:
					gm.buy_packaging(sz, false, 0)
			hand_budget += 2.0 / 40.0
			while not gm.order_queue.is_empty() and hand_budget >= 1.0:
				hand_budget -= 1.0
				var o: Dictionary = gm.order_queue[0]
				var it: Dictionary = gm.pick_item(o["product"])
				if it.is_empty():
					break
				if not gm.wrap_item(it):
					gm.return_item(it)
					break
				gm.ship_package(gm.pickup_package())
		var h: Dictionary = {}
		gm.start_next_day()
		h = gm.history[gm.history.size() - 1]
		if warehouse_day < 0 and gm.level >= 4 and gm.money >= 5000:
			gm.buy_upgrade("warehouse")
			warehouse_day = gm.day
		if gm.location_stage >= 1 and gm.level >= 5 and gm.money > 800 and gm.staff.size() < 3:
			for r in ["packer", "versand", "lager"]:
				if gm.staff_count(r) == 0 and gm.money > 600:
					gm.hire(r)
		hand_budget = minf(hand_budget, 1.0)
		print("Tag %2d: Umsatz %5d  Gewinn %5d  Pakete %3d  Konto %6d  Level %d  Bewertung %.2f  %s" % [
			int(h["day"]), int(h["revenue"]), int(h["profit"]), int(h["shipped"]), gm.money, gm.level, gm.reputation,
			"LAGERHALLE" if gm.day == warehouse_day else ""])
	quit()
