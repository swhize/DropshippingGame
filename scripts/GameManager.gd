extends Node
## Zentrale Spiellogik (Autoload): Wirtschaft, Tagesablauf, Story-Fortschritt, Tutorial,
## Bewertungen, Level, Ziele, Personal, Ausbau und Speichern/Laden.
## Die Uhr ist eine "Geschäftsuhr" (bclock): nur die offenen Stunden 08-20 Uhr zählen,
## die Nacht wird übersprungen. Alle Zeitangaben (Lieferung, Kampagnen) laufen darauf.

enum ItemKind { NONE, CRATE, ITEM, PACKAGE, LABELED, PLATE }

const SAVE_PATH := "user://savegame.json"
const SAVE_VERSION := 2
const AUTOSAVE_SECONDS := 90.0

signal economy_changed
signal message(text: String)
signal toast(text: String, kind: String)
signal pc_toggled(is_open: bool)
signal held_item_changed(kind: int, data: Dictionary)
signal day_ended(summary: Dictionary)
signal day_started(day: int)
signal level_up(level: int)
signal goal_completed(goal: Dictionary)
signal story_changed(stage: String)
signal objective_changed
signal location_changed(stage: int)
signal delivery_incoming(product_id: String)
signal delivery_arrived(product_id: String)
signal package_shipped(pkg: Dictionary, reward: int, where: Vector3)
signal input_lock_changed(locked: bool)
signal dialogue_requested(id: String)
signal end_day_requested
signal game_over(reason: String)
signal game_won
signal world_changed
signal staff_changed
signal conveyor_changed

var start_mode: String = ""
var in_game: bool = false
var pc_open: bool = false
var _locks: Dictionary = {}
var _autosave_acc: float = 0.0

# ---- Spielzustand -------------------------------------------------------------------
var money: int = 0
var day: int = 1
var time_minutes: float = GameData.DAY_START
var day_over: bool = false
var story_stage: String = "business"
var intro_step: int = 0
var intro_served: Array = []
var intro_plate_out: int = 0
var tutorial_step: int = -1
var tut_flags: Dictionary = {}
var location_stage: int = 0
var upgrades: Dictionary = {}
var decor_owned: Dictionary = {}
var lifestyle_owned: Dictionary = {}
var reputation: float = 3.0
var review_count: int = 0
var reviews: Array = []
var xp: int = 0
var level: int = 1
var awareness: float = 0.0
var goals_done: Dictionary = {}
var brand_named: bool = false
var ending_seen: bool = false

var brand_name: String = "MeinShop"
var brand_logo_index: int = 0
var brand_color: Color = GameData.BRAND_PALETTE[0]

var traveling_deliveries: Array = []   # [{product, quantity, quality, arrive_at, van_sent}]
var dock_crates: Array = []            # [{product, quantity, quality}]
var stock: Dictionary = {}             # pid -> {qty, quality}
var express_delivery: bool = false
var blocked_suppliers: Dictionary = {} # "index" -> bis einschließlich Tag
var damaged_next_crate: float = 0.0

var order_queue: Array = []            # [{product, price, created}]
var packed_packages: Array = []        # [{product, price, quality, created, color, logo}]
var packaging: Array = [20, 0, 0]
var flat_packaging: Array = [0, 0, 0]
var packaging_brand: Array = []        # je Größe {color, logo}

var listed: Dictionary = {}
var shop_prices: Dictionary = {}
var promo_active: bool = false
var shop_offline_until: float = -1.0
var boosts: Array = []                 # [{source, name, mult, ends_at, product}]
var tiktok_ready_at: float = 0.0

var staff: Array = []                  # [{role, name, progress}]
var social_posted_day: int = 0
var conveyor_queue: Array = []         # [{pkg, done_at}]
var world_items: Array = []            # nur beim Speichern/Laden befüllt
var player_state: Dictionary = {}      # nur beim Speichern/Laden befüllt

var total_shipped: int = 0
var total_earned: int = 0
var total_lost_orders: int = 0
var shipped_per_product: Dictionary = {}
var daily: Dictionary = {}
var history: Array = []                # [{day, revenue, profit, shipped}]
var last_saved_unix: int = 0

var _order_progress: float = 0.0
var _order_jitter: float = 1.0
var _tutorial_order_at: float = -1.0
var _conveyor_id: int = 0
var _last_lost_toast: int = -100000

func _ready() -> void:
	reset_state()

func _notification(what: int) -> void:
	if what == NOTIFICATION_WM_CLOSE_REQUEST and in_game:
		save_game()

func _exit_tree() -> void:
	Mats.clear_cache()
	UITheme.clear_cache()

func _process(delta: float) -> void:
	if not in_game:
		return
	_autosave_acc += delta
	if _autosave_acc >= AUTOSAVE_SECONDS:
		_autosave_acc = 0.0
		if story_stage == "business" and not day_over:
			save_game(true)
	if story_stage != "business" or day_over:
		return
	advance_minutes(delta * GameData.MINUTES_PER_SECOND)

# =====================================================================================
# Neues Spiel / Reset
# =====================================================================================
func reset_state() -> void:
	money = GameData.START_CAPITAL
	day = 1
	time_minutes = GameData.DAY_START
	day_over = false
	story_stage = "business"
	intro_step = 0
	intro_served = []
	intro_plate_out = 0
	_assigned_plates = []
	tutorial_step = -1
	tut_flags = {}
	location_stage = 0
	upgrades = {}
	decor_owned = {}
	lifestyle_owned = {}
	reputation = 3.0
	review_count = 0
	reviews = []
	xp = 0
	level = 1
	awareness = 0.0
	goals_done = {}
	brand_named = false
	ending_seen = false
	brand_name = "MeinShop"
	brand_logo_index = 0
	brand_color = GameData.BRAND_PALETTE[0]
	traveling_deliveries = []
	dock_crates = []
	stock = {}
	express_delivery = false
	blocked_suppliers = {}
	damaged_next_crate = 0.0
	order_queue = []
	packed_packages = []
	packaging = [20, 0, 0]
	flat_packaging = [0, 0, 0]
	packaging_brand = []
	for i in GameData.PACKAGING_SIZES.size():
		packaging_brand.append({"color": brand_color, "logo": brand_logo_index})
	listed = {}
	shop_prices = {}
	promo_active = false
	shop_offline_until = -1.0
	boosts = []
	tiktok_ready_at = 0.0
	staff = []
	social_posted_day = 0
	conveyor_queue = []
	world_items = []
	player_state = {}
	total_shipped = 0
	total_earned = 0
	total_lost_orders = 0
	shipped_per_product = {}
	history = []
	last_saved_unix = 0
	_order_progress = 0.0
	_order_jitter = 1.0
	_tutorial_order_at = -1.0
	for p in GameData.PRODUCTS:
		var id: String = p["id"]
		stock[id] = {"qty": 0, "quality": 1.0}
		listed[id] = false
		shop_prices[id] = int(round(float(p["ref_price"]) * 0.8))
		shipped_per_product[id] = 0
	_reset_daily()
	Market.reset_state()
	Events.reset_state()

## mode: "intro" (Imbiss-Story + Tutorial), "tutorial" (nur Tutorial), "skip" (alles überspringen)
func new_game(mode: String) -> void:
	reset_state()
	match mode:
		"intro":
			story_stage = "diner"
			money = 0
			time_minutes = GameData.INTRO_TIME
			intro_step = 0
		"tutorial":
			tutorial_step = 0
		_:
			tutorial_step = -1
			listed["huelle"] = true
	if mode != "intro":
		Events.add_mail("Mama", "Viel Erfolg, Schatz!", "Ich hab gehört, du machst jetzt was mit Internet. Iss was Ordentliches und vergiss nicht zu schlafen. Hab dich lieb! 💛")
	economy_changed.emit()

func finish_intro() -> void:
	story_stage = "business"
	money = GameData.START_CAPITAL
	day = 1
	time_minutes = GameData.DAY_START
	intro_step = 3
	tutorial_step = 0
	_reset_daily()
	Events.add_mail("Mama", "Viel Erfolg, Schatz!", "Kalle hat angerufen. Du hast gekündigt?! Naja... ich glaub an dich. Iss was Ordentliches! 💛")
	story_changed.emit(story_stage)
	objective_changed.emit()
	economy_changed.emit()

# =====================================================================================
# Eingabe-Sperren (PC, Dialoge, Menüs) & Hinweise
# =====================================================================================
func lock_input(key: String) -> void:
	var was := is_input_locked()
	_locks[key] = true
	if not was:
		input_lock_changed.emit(true)

func unlock_input(key: String) -> void:
	var was := is_input_locked()
	_locks.erase(key)
	if was and not is_input_locked():
		input_lock_changed.emit(false)

func is_input_locked() -> bool:
	return not _locks.is_empty()

func clear_locks() -> void:
	_locks.clear()
	pc_open = false
	_pausers.clear()
	if is_inside_tree():
		get_tree().paused = false
	Audio.set_muffled(false)

var _pausers: Dictionary = {}

## Pausiert das Spiel, solange mindestens ein Fenster (Pausemenü, Tagesabschluss ...) offen ist.
func set_paused(key: String, on: bool) -> void:
	if on:
		_pausers[key] = true
	else:
		_pausers.erase(key)
	if is_inside_tree():
		get_tree().paused = not _pausers.is_empty()
	Audio.set_muffled(not _pausers.is_empty())

func is_paused_by_ui() -> bool:
	return not _pausers.is_empty()

func notify(text: String, kind: String = "info") -> void:
	toast.emit(text, kind)
	message.emit(text)

func open_pc() -> void:
	if pc_open:
		return
	pc_open = true
	lock_input("pc")
	tut_flags["pc_opened"] = true
	_tutorial_check()
	pc_toggled.emit(true)

func close_pc() -> void:
	if not pc_open:
		return
	pc_open = false
	unlock_input("pc")
	pc_toggled.emit(false)

func toggle_pc() -> void:
	if pc_open:
		close_pc()
	else:
		open_pc()

# =====================================================================================
# Zeit
# =====================================================================================
func bclock() -> float:
	return float(day - 1) * GameData.BUSINESS_MINUTES + (time_minutes - GameData.DAY_START)

func is_open() -> bool:
	return story_stage == "business" and not day_over

func advance_minutes(m: float) -> void:
	time_minutes += m
	_update_deliveries()
	_update_orders(m)
	_update_boosts()
	_update_staff(m)
	_update_conveyor()
	_update_expiry()
	if time_minutes >= GameData.DAY_END:
		time_minutes = GameData.DAY_END
		_end_day()

func _reset_daily() -> void:
	daily = {"revenue": 0, "purchases": 0, "packaging": 0, "marketing": 0, "other": 0, "income_other": 0,
		"shipped": 0, "lost": 0, "orders": 0, "trading": 0, "rep_start": reputation, "xp_start": xp}

func _spend(amount: int, category: String) -> void:
	money -= amount
	daily[category] = int(daily.get(category, 0)) + amount

func adjust_money(amount: int, category: String) -> void:
	money += amount
	daily[category] = int(daily.get(category, 0)) + amount
	economy_changed.emit()

func rent_per_day() -> int:
	return int(GameData.STAGE_RENT[location_stage])

func wages_per_day() -> int:
	var w := 0
	for s in staff:
		w += int(GameData.staff_role(s["role"])["wage"])
	return w

func upkeep_per_day() -> int:
	return 10 if upgrades.get("conveyor", false) else 0

func request_end_day() -> void:
	if not is_open():
		return
	time_minutes = GameData.DAY_END
	_end_day()

func _end_day() -> void:
	if day_over:
		return
	day_over = true
	Events.resolve_pending_choices()
	var s := daily.duplicate()
	s["day"] = day
	s["rent"] = rent_per_day()
	s["wages"] = wages_per_day()
	s["upkeep"] = upkeep_per_day()
	var costs := int(s["purchases"]) + int(s["packaging"]) + int(s["marketing"]) + int(s["other"]) + int(s["rent"]) + int(s["wages"]) + int(s["upkeep"])
	s["profit"] = int(s["revenue"]) + int(s["income_other"]) - costs
	s["money_after"] = money - int(s["rent"]) - int(s["wages"]) - int(s["upkeep"])
	s["rep_end"] = reputation
	s["xp_gained"] = xp - int(daily.get("xp_start", xp))
	s["bankrupt"] = int(s["money_after"]) < GameData.BANKRUPT_LIMIT
	s["portfolio"] = Market.portfolio_value()
	day_ended.emit(s)

## Nach der Tagesübersicht: Kosten abbuchen, nächsten Tag starten.
func start_next_day() -> void:
	var rent := rent_per_day()
	var wages := wages_per_day()
	var upkeep := upkeep_per_day()
	money -= rent + wages + upkeep
	var profit := int(daily["revenue"]) + int(daily["income_other"]) - int(daily["purchases"]) - int(daily["packaging"]) \
		- int(daily["marketing"]) - int(daily["other"]) - rent - wages - upkeep
	history.append({"day": day, "revenue": int(daily["revenue"]), "profit": profit, "shipped": int(daily["shipped"])})
	if history.size() > 30:
		history.pop_front()
	if money < GameData.BANKRUPT_LIMIT:
		game_over.emit("pleite")
		return
	day += 1
	time_minutes = GameData.DAY_START
	day_over = false
	awareness *= 0.92
	for k in blocked_suppliers.keys():
		if int(blocked_suppliers[k]) < day:
			blocked_suppliers.erase(k)
	_reset_daily()
	Market.new_day()
	Events.new_day()
	_check_goals()
	day_started.emit(day)
	economy_changed.emit()
	save_game(true)

# =====================================================================================
# Produkte & Nachfrage
# =====================================================================================
func product_unlocked(id: String) -> bool:
	return level >= int(GameData.product(id)["unlock_level"])

func product_has_shelf(id: String) -> bool:
	return GameData.product_index(id) < GameData.GARAGE_PRODUCTS or location_stage >= 1

func product_available(id: String) -> bool:
	return product_unlocked(id) and product_has_shelf(id)

func stock_qty(id: String) -> int:
	return int(stock.get(id, {"qty": 0})["qty"])

func stock_quality(id: String) -> float:
	return float(stock.get(id, {"quality": 1.0})["quality"])

func stock_total() -> int:
	var n := 0
	for id in stock:
		n += int(stock[id]["qty"])
	return n

func capacity() -> int:
	var c: int = GameData.STAGE_CAPACITY[location_stage]
	if upgrades.get("highrack", false):
		c += 3000
	return c

func queue_capacity() -> int:
	var q: int = GameData.STAGE_QUEUE[location_stage]
	if upgrades.get("server", false):
		q += 6
	return q

func pending_count() -> int:
	return order_queue.size()

func pending_count_for(id: String) -> int:
	var n := 0
	for o in order_queue:
		if o["product"] == id:
			n += 1
	return n

func traveling_count_for(id: String) -> int:
	var n := 0
	for d in traveling_deliveries:
		if d["product"] == id:
			n += 1
	return n

func dock_count_for(id: String) -> int:
	var n := 0
	for c in dock_crates:
		if c["product"] == id:
			n += 1
	return n

func packed_count() -> int:
	return packed_packages.size()

func flat_total() -> int:
	return int(flat_packaging[0]) + int(flat_packaging[1]) + int(flat_packaging[2])

func current_sale_price(id: String) -> int:
	var price := int(shop_prices.get(id, 10))
	if promo_active:
		price = int(round(price * (1.0 - GameData.PROMO_DISCOUNT)))
	return maxi(1, price)

func boost_mult(id: String) -> float:
	var m := 1.0
	for b in boosts:
		var bp: String = b.get("product", "")
		if bp == "" or bp == id:
			m *= float(b["mult"])
	return m

func active_boost(source: String) -> Dictionary:
	for b in boosts:
		if b["source"] == source:
			return b
	return {}

func demand_rate(id: String) -> float:
	if not listed.get(id, false) or not product_available(id):
		return 0.0
	var p := GameData.product(id)
	var market := Market.market_price(id)
	var price := float(current_sale_price(id))
	var price_factor := clampf(pow(market / maxf(price, 1.0), 1.6), 0.08, 2.6)
	var rep_mult := lerpf(0.35, 1.6, clampf(reputation / 5.0, 0.0, 1.0))
	return float(p["popularity"]) * price_factor * rep_mult * (0.6 + awareness) * boost_mult(id)

func demand_label(id: String) -> String:
	var r := demand_rate(id)
	if r <= 0.0:
		return "offline"
	if r < 0.35:
		return "sehr niedrig"
	if r < 0.7:
		return "niedrig"
	if r < 1.2:
		return "mittel"
	if r < 2.0:
		return "hoch"
	return "sehr hoch"

func order_interval_minutes() -> float:
	var total := 0.0
	for p in GameData.PRODUCTS:
		total += demand_rate(p["id"])
	if total <= 0.0:
		return 0.0
	var interval := GameData.BASE_ORDER_MINUTES / total
	if promo_active:
		interval *= 0.85
	return maxf(interval, 2.5)

func _pick_order_product() -> String:
	var total := 0.0
	for p in GameData.PRODUCTS:
		total += demand_rate(p["id"])
	if total <= 0.0:
		return ""
	var r := randf() * total
	for p in GameData.PRODUCTS:
		r -= demand_rate(p["id"])
		if r <= 0.0:
			return p["id"]
	return GameData.PRODUCTS[0]["id"]

func _update_orders(m: float) -> void:
	if _tutorial_order_at >= 0.0 and bclock() >= _tutorial_order_at:
		_tutorial_order_at = -1.0
		spawn_order("huelle")
	if bclock() < shop_offline_until:
		return
	var interval := order_interval_minutes()
	if interval <= 0.0:
		return
	_order_progress += m / (interval * _order_jitter)
	if _order_progress >= 1.0:
		_order_progress = 0.0
		_order_jitter = randf_range(0.65, 1.35)
		var id := _pick_order_product()
		if id != "":
			spawn_order(id)

func spawn_order(id: String) -> void:
	if order_queue.size() >= queue_capacity():
		total_lost_orders += 1
		daily["lost"] = int(daily["lost"]) + 1
		if randf() < 0.6:
			_add_review(id, 1, "Shop überlastet – Bestellung ging nicht durch")
		var now_ms := Time.get_ticks_msec()
		if now_ms - _last_lost_toast > 20000:
			_last_lost_toast = now_ms
			notify("Bestellung verloren – deine Warteschlange ist voll! (Preis erhöhen oder Personal holen)", "bad")
	else:
		order_queue.append({"product": id, "price": current_sale_price(id), "created": bclock()})
		daily["orders"] = int(daily["orders"]) + 1
		Audio.play("order", 0.02, -4.0)
		_tutorial_check()
	economy_changed.emit()

func _update_expiry() -> void:
	var now := bclock()
	var i := 0
	var changed := false
	while i < order_queue.size():
		if now - float(order_queue[i]["created"]) > GameData.ORDER_EXPIRE_MINUTES:
			var o: Dictionary = order_queue[i]
			order_queue.remove_at(i)
			total_lost_orders += 1
			daily["lost"] = int(daily["lost"]) + 1
			_add_review(o["product"], 1, "")
			notify("Eine Bestellung (%s) wurde storniert – zu lange gewartet." % GameData.product(o["product"])["name"], "bad")
			changed = true
		else:
			i += 1
	if changed:
		economy_changed.emit()

# =====================================================================================
# Einkauf & Lieferungen
# =====================================================================================
func supplier_available(index: int) -> bool:
	return level >= int(GameData.SUPPLIERS[index]["level"]) and not blocked_suppliers.has(str(index))

func bulk_available(bulk_index: int) -> bool:
	var b: Dictionary = GameData.BULK_OPTIONS[bulk_index]
	return level >= int(b["level"]) and location_stage >= int(b["stage"])

func bulk_cost(product_index: int, bulk_index: int, supplier_index: int) -> int:
	var p: Dictionary = GameData.PRODUCTS[product_index]
	var b: Dictionary = GameData.BULK_OPTIONS[bulk_index]
	var s: Dictionary = GameData.SUPPLIERS[supplier_index]
	var cost := int(round(float(p["unit_cost"]) * int(b["quantity"]) * float(b["discount"]) * float(s["price_mult"])))
	if express_delivery:
		cost += GameData.EXPRESS_SURCHARGE
	return cost

func lead_minutes(supplier_index: int) -> float:
	var t := GameData.BASE_LEAD_MINUTES * float(GameData.SUPPLIERS[supplier_index]["leadtime_mult"])
	if express_delivery:
		t *= 0.5
	if upgrades.get("van", false):
		t *= 0.75
	return maxf(t, 8.0)

func buy_bulk(product_index: int, bulk_index: int, supplier_index: int) -> bool:
	var p: Dictionary = GameData.PRODUCTS[product_index]
	if not product_unlocked(p["id"]):
		notify("Dieses Produkt ist noch gesperrt.", "bad")
		return false
	if not product_has_shelf(p["id"]):
		notify("Dafür brauchst du erst die Lagerhalle (kein Regal in der Garage).", "bad")
		return false
	if not supplier_available(supplier_index):
		notify("Dieser Anbieter ist gerade nicht verfügbar.", "bad")
		return false
	if not bulk_available(bulk_index):
		notify("Diese Bestellmenge ist noch nicht freigeschaltet.", "bad")
		return false
	var cost := bulk_cost(product_index, bulk_index, supplier_index)
	if money < cost:
		notify("Nicht genug Geld für diese Bestellung!", "bad")
		Audio.play("error")
		return false
	var b: Dictionary = GameData.BULK_OPTIONS[bulk_index]
	var s: Dictionary = GameData.SUPPLIERS[supplier_index]
	_spend(cost, "purchases")
	traveling_deliveries.append({"product": p["id"], "quantity": int(b["quantity"]), "quality": float(s["quality"]),
		"arrive_at": bclock() + lead_minutes(supplier_index), "van_sent": false})
	notify("%d× %s bestellt bei %s." % [int(b["quantity"]), p["name"], s["name"]], "good")
	Audio.play("cash", 0.02, -8.0)
	_tutorial_check()
	economy_changed.emit()
	return true

func _update_deliveries() -> void:
	var now := bclock()
	var i := 0
	var changed := false
	while i < traveling_deliveries.size():
		var d: Dictionary = traveling_deliveries[i]
		if not bool(d.get("van_sent", false)) and now >= float(d["arrive_at"]) - 3.0:
			d["van_sent"] = true
			delivery_incoming.emit(d["product"])
		if now >= float(d["arrive_at"]):
			var qty := int(d["quantity"])
			if damaged_next_crate > 0.0:
				var lost := int(round(qty * damaged_next_crate))
				qty -= lost
				damaged_next_crate = 0.0
				notify("Beschädigte Lieferung: %d Stück unbrauchbar." % lost, "bad")
			dock_crates.append({"product": d["product"], "quantity": qty, "quality": d["quality"]})
			traveling_deliveries.remove_at(i)
			delivery_arrived.emit(d["product"])
			notify("Lieferung angekommen: %d× %s am Wareneingang." % [qty, GameData.product(d["product"])["name"]], "info")
			changed = true
		else:
			i += 1
	if changed:
		economy_changed.emit()

func eta_text(d: Dictionary) -> String:
	var left := maxf(0.0, float(d["arrive_at"]) - bclock())
	return "%d min" % int(ceil(left))

func pickup_crate() -> Dictionary:
	if dock_crates.is_empty():
		notify("Nichts am Wareneingang abzuholen.", "info")
		return {}
	var c: Dictionary = dock_crates.pop_front()
	tut_flags["crate_picked"] = true
	_tutorial_check()
	economy_changed.emit()
	return c

func return_crate(crate: Dictionary) -> void:
	dock_crates.push_front(crate)
	economy_changed.emit()

func unbox_crate(id: String, quantity: int, quality: float) -> bool:
	if stock_total() + quantity > capacity():
		notify("Lager voll! (%d/%d) – verkaufe erst etwas oder baue aus." % [stock_total(), capacity()], "bad")
		Audio.play("error")
		return false
	var entry: Dictionary = stock[id]
	var before: int = int(entry["qty"])
	entry["quality"] = (float(entry["quality"]) * before + quality * quantity) / float(maxi(1, before + quantity))
	entry["qty"] = before + quantity
	_tutorial_check()
	economy_changed.emit()
	return true

func pick_item(id: String) -> Dictionary:
	var idx := -1
	for i in order_queue.size():
		if order_queue[i]["product"] == id:
			idx = i
			break
	if idx < 0:
		notify("Keine offene Bestellung für %s." % GameData.product(id)["name"], "info")
		return {}
	if stock_qty(id) <= 0:
		notify("Kein %s mehr auf Lager! Im Laptop nachbestellen." % GameData.product(id)["name"], "bad")
		return {}
	var order: Dictionary = order_queue[idx]
	order_queue.remove_at(idx)
	stock[id]["qty"] = stock_qty(id) - 1
	tut_flags["item_picked"] = true
	_tutorial_check()
	economy_changed.emit()
	return {"product": id, "price": int(order["price"]), "quality": stock_quality(id), "created": float(order["created"])}

func return_item(item: Dictionary) -> void:
	var id: String = item["product"]
	stock[id]["qty"] = stock_qty(id) + 1
	order_queue.push_front({"product": id, "price": int(item["price"]), "created": float(item.get("created", bclock()))})
	economy_changed.emit()

# =====================================================================================
# Verpacken, Etikettieren, Versand
# =====================================================================================
func wrap_item(item: Dictionary) -> bool:
	var id: String = item["product"]
	var size := int(GameData.product(id)["size"])
	if int(packaging[size]) <= 0:
		if int(flat_packaging[size]) > 0:
			notify("Kein gefalteter Karton %s – erst am Falttisch falten!" % GameData.size_name(size), "bad")
		else:
			notify("Keine Kartons in Größe %s! Im Laptop unter 'Verpackung' kaufen." % GameData.size_name(size), "bad")
		Audio.play("error")
		return false
	packaging[size] = int(packaging[size]) - 1
	var brand: Dictionary = packaging_brand[size]
	packed_packages.append({"product": id, "price": int(item["price"]), "quality": float(item["quality"]),
		"created": float(item.get("created", bclock())), "color": brand["color"], "logo": int(brand["logo"])})
	tut_flags["wrapped"] = true
	_tutorial_check()
	economy_changed.emit()
	return true

func pickup_package() -> Dictionary:
	if packed_packages.is_empty():
		notify("Hier liegt kein fertiges Paket.", "info")
		return {}
	var pkg: Dictionary = packed_packages.pop_back()
	economy_changed.emit()
	return pkg

func return_package(pkg: Dictionary) -> void:
	packed_packages.append(pkg)
	economy_changed.emit()

func on_labeled() -> void:
	tut_flags["labeled"] = true
	_tutorial_check()

func fold_carton() -> int:
	var best := -1
	for size in 3:
		if int(flat_packaging[size]) <= 0:
			continue
		if best < 0 or int(packaging[size]) < int(packaging[best]):
			best = size
	if best < 0:
		notify("Keine ungefalteten Kartons da.", "info")
		return -1
	flat_packaging[best] = int(flat_packaging[best]) - 1
	packaging[best] = int(packaging[best]) + 1
	Audio.play("fold")
	economy_changed.emit()
	return best

## Versand: der Kunde zahlt den Preis vom Bestellzeitpunkt. Qualität + Tempo -> Bewertung.
func ship_package(pkg: Dictionary, where: Vector3 = Vector3.ZERO) -> int:
	var reward := int(pkg["price"])
	money += reward
	daily["revenue"] = int(daily["revenue"]) + reward
	daily["shipped"] = int(daily["shipped"]) + 1
	total_shipped += 1
	total_earned += reward
	var id: String = pkg["product"]
	shipped_per_product[id] = int(shipped_per_product.get(id, 0)) + 1
	_add_xp(maxi(1, int(reward / 2)))
	var q := float(pkg.get("quality", 1.0))
	var age := bclock() - float(pkg.get("created", bclock()))
	if randf() < GameData.REVIEW_CHANCE:
		var stars := 4
		if age < 90.0:
			stars += 1
		elif age > 360.0:
			stars -= 2
		elif age > 200.0:
			stars -= 1
		if q < 0.8:
			stars -= 1
		elif q >= 1.3:
			stars += 1
		if randf() < 0.25:
			stars += randi_range(-1, 1)
		_add_review(id, clampi(stars, 1, 5), "")
	if q < 0.8 and randf() < 0.08:
		money -= reward
		daily["other"] = int(daily["other"]) + reward
		notify("Rücksendung! Ein Kunde fand die Billig-Qualität unzumutbar (−%s)." % UITheme.money(reward), "bad")
	Audio.play("cash")
	package_shipped.emit(pkg, reward, where)
	_tutorial_check()
	_check_goals()
	economy_changed.emit()
	return reward

func conveyor_insert(pkg: Dictionary) -> bool:
	if not upgrades.get("conveyor", false):
		return false
	_conveyor_id += 1
	conveyor_queue.append({"id": _conveyor_id, "pkg": pkg, "done_at": bclock() + 6.0})
	conveyor_changed.emit()
	return true

func _update_conveyor() -> void:
	if conveyor_queue.is_empty():
		return
	var now := bclock()
	var i := 0
	while i < conveyor_queue.size():
		if now >= float(conveyor_queue[i]["done_at"]):
			var entry: Dictionary = conveyor_queue[i]
			conveyor_queue.remove_at(i)
			ship_package(entry["pkg"], Vector3(5.8, 2.0, -13.5))
			conveyor_changed.emit()
		else:
			i += 1

# =====================================================================================
# Bewertungen, XP, Ziele
# =====================================================================================
func _add_review(id: String, stars: int, custom: String) -> void:
	var texts: Array = GameData.REVIEW_TEXTS[stars]
	var text := custom if custom != "" else String(texts[randi() % texts.size()])
	reviews.push_front({"stars": stars, "text": text, "name": GameData.REVIEW_NAMES[randi() % GameData.REVIEW_NAMES.size()],
		"product": id, "day": day})
	if reviews.size() > 25:
		reviews.pop_back()
	review_count += 1
	var weight := 0.12 if review_count < 10 else 0.05
	reputation = clampf(reputation + (float(stars) - reputation) * weight, 0.5, 5.0)
	_check_goals()

func change_reputation(delta: float) -> void:
	reputation = clampf(reputation + delta, 0.5, 5.0)
	_check_goals()
	economy_changed.emit()

func add_awareness(v: float) -> void:
	awareness = clampf(awareness + v, 0.0, 1.5)

func _add_xp(n: int) -> void:
	xp += n
	while level < GameData.MAX_LEVEL and xp >= GameData.level_threshold(level + 1):
		level += 1
		level_up.emit(level)
		Audio.play("levelup")
		var unlock: String = GameData.LEVEL_UNLOCKS.get(level, "")
		notify("Firmenlevel %d! Neu: %s" % [level, unlock], "good")
		_check_goals()

func level_progress() -> float:
	if level >= GameData.MAX_LEVEL:
		return 1.0
	var a := GameData.level_threshold(level)
	var b := GameData.level_threshold(level + 1)
	return clampf(float(xp - a) / float(maxi(1, b - a)), 0.0, 1.0)

func goal_value(g: Dictionary) -> float:
	match String(g["type"]):
		"shipped": return float(total_shipped)
		"revenue": return float(total_earned)
		"rating": return reputation if review_count >= 5 else 0.0
		"stage": return float(location_stage)
		"staff": return float(staff.size())
		"brand": return 1.0 if brand_named else 0.0
		"level": return float(level)
	return 0.0

func _check_goals() -> void:
	if story_stage != "business":
		return
	for g in GameData.GOALS:
		var id: String = g["id"]
		if goals_done.get(id, false):
			continue
		if goal_value(g) >= float(g["target"]):
			goals_done[id] = true
			money += int(g["reward"])
			daily["income_other"] = int(daily.get("income_other", 0)) + int(g["reward"])
			goal_completed.emit(g)
			notify("🏆 Ziel erreicht: %s (+%s)" % [g["title"], UITheme.money(int(g["reward"]))], "good")
			Audio.play("levelup", 0.0, -3.0)
			if g.get("final", false) and not ending_seen:
				ending_seen = true
				game_won.emit()
			objective_changed.emit()

func next_goal() -> Dictionary:
	for g in GameData.GOALS:
		if not goals_done.get(g["id"], false):
			return g
	return {}

# =====================================================================================
# Tutorial & Ziele-Anzeige
# =====================================================================================
func _tutorial_check() -> void:
	if tutorial_step < 0:
		return
	var advanced := false
	while tutorial_step >= 0 and tutorial_step < GameData.TUTORIAL.size() and _tutorial_done(tutorial_step):
		tutorial_step += 1
		advanced = true
		if tutorial_step == 5 and order_queue.is_empty():
			_tutorial_order_at = bclock() + 4.0
	if tutorial_step >= GameData.TUTORIAL.size():
		tutorial_step = -1
		notify("Tutorial geschafft! Ab jetzt läuft dein Shop. Ziele findest du im Laptop.", "good")
		Audio.play("levelup")
	if advanced:
		objective_changed.emit()

func _tutorial_done(step: int) -> bool:
	match step:
		0: return tut_flags.get("pc_opened", false)
		1: return not traveling_deliveries.is_empty() or not dock_crates.is_empty() or stock_total() > 0 or tut_flags.get("crate_picked", false)
		2: return tut_flags.get("crate_picked", false) or stock_total() > 0
		3: return stock_total() > 0 or total_shipped > 0
		4: return listed.get("huelle", false)
		5: return not order_queue.is_empty() or tut_flags.get("item_picked", false) or total_shipped > 0
		6: return tut_flags.get("item_picked", false) or total_shipped > 0
		7: return tut_flags.get("wrapped", false) or total_shipped > 0
		8: return tut_flags.get("labeled", false) or total_shipped > 0
		9: return total_shipped > 0
	return true

func current_objective() -> Dictionary:
	if story_stage == "diner":
		match intro_step:
			0:
				return {"title": "Schicht im Imbiss", "text": "Sprich mit Kalle an der Theke.", "progress": -1.0}
			1:
				return {"title": "Teller austragen (%d/3)" % intro_served.size(),
					"text": "Hol an der Durchreiche einen Teller und bring ihn an den richtigen Tisch.", "progress": intro_served.size() / 3.0}
			_:
				return {"title": "Feierabend?", "text": "Sprich mit Kalle. Es ist Zeit für eine Entscheidung.", "progress": -1.0}
	if tutorial_step >= 0 and tutorial_step < GameData.TUTORIAL.size():
		var t: Dictionary = GameData.TUTORIAL[tutorial_step]
		return {"title": "Tutorial %d/%d: %s" % [tutorial_step + 1, GameData.TUTORIAL.size(), t["title"]], "text": t["text"],
			"progress": float(tutorial_step) / GameData.TUTORIAL.size()}
	var g := next_goal()
	if g.is_empty():
		return {"title": "Imperium aufgebaut", "text": "Alle Ziele erreicht. Genieß den Ruhm – oder mach einfach weiter.", "progress": 1.0}
	var v := goal_value(g)
	return {"title": "Ziel: " + String(g["title"]), "text": String(g["desc"]),
		"progress": clampf(v / maxf(float(g["target"]), 0.001), 0.0, 1.0)}

# =====================================================================================
# Imbiss-Intro
# =====================================================================================
const INTRO_TABLES := [2, 3, 4]

func start_shift() -> void:
	intro_step = 1
	objective_changed.emit()
	economy_changed.emit()

func diner_plates_waiting() -> int:
	if story_stage != "diner" or intro_step != 1:
		return 0
	return maxi(0, INTRO_TABLES.size() - intro_served.size() - intro_plate_out)

func diner_table_state(table_id: int) -> int:
	if story_stage != "diner":
		return 0
	if intro_served.has(table_id):
		return 2
	if INTRO_TABLES.has(table_id) and intro_step >= 1:
		return 1
	return 0

func diner_take_plate() -> Dictionary:
	if diner_plates_waiting() <= 0:
		return {}
	for t in INTRO_TABLES:
		if not intro_served.has(t) and not _plate_assigned(t):
			intro_plate_out += 1
			_assigned_plates.append(t)
			economy_changed.emit()
			objective_changed.emit()
			return {"table": t}
	return {}

var _assigned_plates: Array = []

func _plate_assigned(t: int) -> bool:
	return _assigned_plates.has(t)

func diner_serve(table_id: int, plate: Dictionary) -> bool:
	if int(plate.get("table", 0)) != table_id:
		return false
	intro_served.append(table_id)
	intro_plate_out = maxi(0, intro_plate_out - 1)
	if intro_served.size() >= INTRO_TABLES.size():
		intro_step = 2
		notify("📱 Neues Video: 'Mit 19 Millionär – dank Dropshipping!' ... interessant.", "info")
	objective_changed.emit()
	economy_changed.emit()
	return true

# =====================================================================================
# Branding, Webshop, Verpackung, Logistik
# =====================================================================================
func set_brand_name(new_name: String) -> void:
	var n := new_name.strip_edges()
	if n == "" or n == brand_name:
		return
	brand_name = n.substr(0, 22)
	brand_named = true
	_check_goals()
	world_changed.emit()
	economy_changed.emit()

func set_brand_logo(index: int) -> void:
	brand_logo_index = index
	economy_changed.emit()

func set_brand_color(color: Color) -> void:
	brand_color = color
	world_changed.emit()
	economy_changed.emit()

func set_listed(id: String, active: bool) -> void:
	if active and not product_available(id):
		notify("Dieses Produkt ist noch nicht verfügbar.", "bad")
		return
	listed[id] = active
	_tutorial_check()
	economy_changed.emit()

func set_shop_price(id: String, price: int) -> void:
	shop_prices[id] = clampi(price, 1, 999)
	economy_changed.emit()

func set_promo_active(active: bool) -> void:
	promo_active = active
	economy_changed.emit()

func packaging_cost(size: int, flat: bool, batch: int = 0) -> int:
	var cost := float(GameData.PACKAGING_SIZES[size]["cost"]) * float(GameData.PACKAGING_BATCHES[batch]["mult"])
	if flat:
		cost *= GameData.PACKAGING_FLAT_DISCOUNT
	return int(round(cost))

func buy_packaging(size: int, flat: bool = false, batch: int = 0) -> bool:
	var cost := packaging_cost(size, flat, batch)
	if money < cost:
		notify("Nicht genug Geld für Verpackungsmaterial!", "bad")
		Audio.play("error")
		return false
	_spend(cost, "packaging")
	var qty: int = GameData.PACKAGING_BATCHES[batch]["qty"]
	if flat:
		flat_packaging[size] = int(flat_packaging[size]) + qty
	else:
		packaging[size] = int(packaging[size]) + qty
	packaging_brand[size] = {"color": brand_color, "logo": brand_logo_index}
	notify("%d Kartons %s%s gekauft – mit deinem Logo bedruckt." % [qty, GameData.size_name(size), " (ungefaltet)" if flat else ""], "good")
	economy_changed.emit()
	return true

func set_express_delivery(enabled: bool) -> void:
	express_delivery = enabled
	economy_changed.emit()

# =====================================================================================
# Marketing
# =====================================================================================
func start_ad_campaign(tier_index: int) -> bool:
	var t: Dictionary = GameData.AD_TIERS[tier_index]
	if level < int(t["level"]):
		notify("Diese Werbeform ist erst ab Level %d verfügbar." % int(t["level"]), "bad")
		return false
	if not active_boost("ad").is_empty():
		notify("Es läuft schon eine Werbekampagne.", "info")
		return false
	if money < int(t["cost"]):
		notify("Nicht genug Geld für diese Kampagne!", "bad")
		Audio.play("error")
		return false
	_spend(int(t["cost"]), "marketing")
	boosts.append({"source": "ad", "name": t["name"], "mult": float(t["mult"]), "ends_at": bclock() + float(t["minutes"]), "product": ""})
	add_awareness(float(t["awareness"]))
	notify("%s gestartet! Mehr Bestellungen für %d Minuten." % [t["name"], int(t["minutes"])], "good")
	economy_changed.emit()
	return true

func tiktok_available() -> bool:
	return level >= GameData.TIKTOK_LEVEL and bclock() >= tiktok_ready_at and active_boost("tiktok").is_empty()

func trigger_tiktok(score: float, silent: bool = false) -> void:
	var s := clampf(score, 0.05, 1.0)
	var mult := lerpf(GameData.TIKTOK_MIN_MULT, GameData.TIKTOK_MAX_MULT, s)
	var minutes := lerpf(GameData.TIKTOK_MIN_MINUTES, GameData.TIKTOK_MAX_MINUTES, s)
	for i in range(boosts.size() - 1, -1, -1):
		if boosts[i]["source"] == "tiktok":
			boosts.remove_at(i)
	boosts.append({"source": "tiktok", "name": "TikTok-Trend", "mult": mult, "ends_at": bclock() + minutes, "product": ""})
	tiktok_ready_at = bclock() + GameData.TIKTOK_COOLDOWN
	add_awareness(0.05 * s)
	if not silent:
		notify("TikTok gepostet! %d %% Treffer → ×%.1f Nachfrage für %d min." % [int(s * 100), mult, int(minutes)], "good")
	economy_changed.emit()

func add_boost(source: String, boost_name: String, mult: float, minutes: float, product: String = "") -> void:
	boosts.append({"source": source, "name": boost_name, "mult": mult, "ends_at": bclock() + minutes, "product": product})
	economy_changed.emit()

func _update_boosts() -> void:
	var now := bclock()
	var i := 0
	var changed := false
	while i < boosts.size():
		if now >= float(boosts[i]["ends_at"]):
			var b: Dictionary = boosts[i]
			boosts.remove_at(i)
			notify("%s ist ausgelaufen." % b["name"], "info")
			changed = true
		else:
			i += 1
	if changed:
		economy_changed.emit()

# =====================================================================================
# Personal
# =====================================================================================
func staff_allowed() -> bool:
	return location_stage >= 1 and level >= int(GameData.APP_LEVELS["Personal"])

func staff_count(role_id: String) -> int:
	var n := 0
	for s in staff:
		if s["role"] == role_id:
			n += 1
	return n

func hire(role_id: String) -> bool:
	var role := GameData.staff_role(role_id)
	if not staff_allowed():
		notify("Personal gibt es erst mit Lagerhalle und Level 5.", "bad")
		return false
	if staff_count(role_id) >= int(role["max"]):
		notify("Mehr %s brauchst du nicht." % role["name"], "info")
		return false
	var used := []
	for s in staff:
		used.append(s["name"])
	var free_names := []
	for n in GameData.STAFF_NAMES:
		if not used.has(n):
			free_names.append(n)
	var pname: String = free_names[randi() % free_names.size()] if not free_names.is_empty() else "Aushilfe"
	staff.append({"role": role_id, "name": pname, "progress": 0.0})
	notify("%s fängt als %s an (%s/Tag)." % [pname, role["name"], UITheme.money(int(role["wage"]))], "good")
	staff_changed.emit()
	_check_goals()
	economy_changed.emit()
	return true

func fire(index: int) -> void:
	if index < 0 or index >= staff.size():
		return
	var s: Dictionary = staff[index]
	staff.remove_at(index)
	notify("%s wurde entlassen." % s["name"], "info")
	staff_changed.emit()
	economy_changed.emit()

func _update_staff(m: float) -> void:
	if staff.is_empty():
		return
	for s in staff:
		var role := GameData.staff_role(s["role"])
		if s["role"] == "social":
			if social_posted_day != day and time_minutes >= 600.0:
				social_posted_day = day
				var score := randf_range(0.25, 0.85) if randf() > 0.2 else 0.08
				trigger_tiktok(score, true)
				notify("🤳 %s hat ein TikTok gepostet (%d %% Treffer)." % [s["name"], int(score * 100)], "info" if score > 0.2 else "bad")
			continue
		var interval := float(role["interval"])
		s["progress"] = float(s["progress"]) + m / interval
		if float(s["progress"]) >= 1.0:
			if _staff_work(String(s["role"])):
				s["progress"] = 0.0
			else:
				s["progress"] = 1.0

func _staff_work(role_id: String) -> bool:
	match role_id:
		"lager":
			for i in dock_crates.size():
				var c: Dictionary = dock_crates[i]
				if stock_total() + int(c["quantity"]) <= capacity():
					dock_crates.remove_at(i)
					var entry: Dictionary = stock[c["product"]]
					var before: int = int(entry["qty"])
					entry["quality"] = (float(entry["quality"]) * before + float(c["quality"]) * int(c["quantity"])) / float(maxi(1, before + int(c["quantity"])))
					entry["qty"] = before + int(c["quantity"])
					economy_changed.emit()
					return true
			return false
		"packer":
			for i in order_queue.size():
				var o: Dictionary = order_queue[i]
				var id: String = o["product"]
				var size := int(GameData.product(id)["size"])
				if stock_qty(id) <= 0:
					continue
				if int(packaging[size]) <= 0 and int(flat_packaging[size]) > 0:
					flat_packaging[size] = int(flat_packaging[size]) - 1
					packaging[size] = int(packaging[size]) + 1
				if int(packaging[size]) <= 0:
					continue
				order_queue.remove_at(i)
				stock[id]["qty"] = stock_qty(id) - 1
				packaging[size] = int(packaging[size]) - 1
				var brand: Dictionary = packaging_brand[size]
				packed_packages.push_front({"product": id, "price": int(o["price"]), "quality": stock_quality(id),
					"created": float(o["created"]), "color": brand["color"], "logo": int(brand["logo"])})
				economy_changed.emit()
				return true
			return false
		"versand":
			if packed_packages.is_empty():
				return false
			var pkg: Dictionary = packed_packages.pop_front()
			ship_package(pkg, Vector3(5.8, 2.0, -13.5))
			return true
	return false

# =====================================================================================
# Ausbau, Einrichtung, Lifestyle
# =====================================================================================
func upgrade_state(id: String) -> String:
	var u := GameData.upgrade(id)
	if upgrades.get(id, false):
		return "owned"
	if level < int(u["level"]):
		return "level"
	var req: String = u.get("requires", "")
	if req != "" and not upgrades.get(req, false):
		return "requires"
	return "available"

func buy_upgrade(id: String) -> bool:
	var u := GameData.upgrade(id)
	if upgrade_state(id) != "available":
		return false
	if money < int(u["cost"]):
		notify("Nicht genug Geld!", "bad")
		Audio.play("error")
		return false
	_spend(int(u["cost"]), "other")
	upgrades[id] = true
	notify("%s gekauft!" % u["name"], "good")
	Audio.play("levelup")
	if id == "warehouse":
		location_stage = 1
		location_changed.emit(1)
	world_changed.emit()
	_check_goals()
	economy_changed.emit()
	return true

func buy_decor(id: String) -> bool:
	var d := GameData.find_by_id(GameData.DECOR, id)
	if decor_owned.get(id, false):
		return false
	if money < int(d["cost"]):
		notify("Nicht genug Geld!", "bad")
		Audio.play("error")
		return false
	_spend(int(d["cost"]), "other")
	decor_owned[id] = true
	notify("%s aufgestellt." % d["name"], "good")
	world_changed.emit()
	economy_changed.emit()
	return true

func buy_lifestyle(id: String) -> bool:
	var d := GameData.find_by_id(GameData.LIFESTYLE, id)
	if lifestyle_owned.get(id, false):
		return false
	if money < int(d["cost"]):
		notify("Nicht genug Geld!", "bad")
		Audio.play("error")
		return false
	_spend(int(d["cost"]), "other")
	lifestyle_owned[id] = true
	notify("Gönn dir: %s!" % d["name"], "good")
	Audio.play("levelup")
	world_changed.emit()
	economy_changed.emit()
	return true

# =====================================================================================
# Speichern / Laden
# =====================================================================================
func has_save() -> bool:
	return FileAccess.file_exists(SAVE_PATH)

func save_summary() -> Dictionary:
	if not has_save():
		return {}
	var f := FileAccess.open(SAVE_PATH, FileAccess.READ)
	if f == null:
		return {}
	var parsed = JSON.parse_string(f.get_as_text())
	f.close()
	if typeof(parsed) != TYPE_DICTIONARY:
		return {}
	var s: Dictionary = parsed
	return {"day": int(s.get("day", 1)), "money": int(s.get("money", 0)), "brand": String(s.get("brand_name", "")),
		"level": int(s.get("level", 1)), "story": String(s.get("story_stage", "business"))}

static func to_json_value(v):
	match typeof(v):
		TYPE_COLOR:
			return {"__c": [v.r, v.g, v.b, v.a]}
		TYPE_VECTOR3:
			return {"__v3": [v.x, v.y, v.z]}
		TYPE_DICTIONARY:
			var out := {}
			for k in v:
				out[str(k)] = to_json_value(v[k])
			return out
		TYPE_ARRAY:
			var arr := []
			for e in v:
				arr.append(to_json_value(e))
			return arr
	return v

static func from_json_value(v):
	match typeof(v):
		TYPE_DICTIONARY:
			if v.has("__c"):
				var c: Array = v["__c"]
				return Color(c[0], c[1], c[2], c[3])
			if v.has("__v3"):
				var p: Array = v["__v3"]
				return Vector3(p[0], p[1], p[2])
			var out := {}
			for k in v:
				out[k] = from_json_value(v[k])
			return out
		TYPE_ARRAY:
			var arr := []
			for e in v:
				arr.append(from_json_value(e))
			return arr
		TYPE_FLOAT:
			if v == floorf(v) and absf(v) < 1.0e15:
				return int(v)
	return v

func _collect_world_state() -> void:
	world_items = []
	if not is_inside_tree():
		return
	for n in get_tree().get_nodes_in_group("dropped_items"):
		if n.has_method("to_save"):
			world_items.append(n.to_save())
	var players := get_tree().get_nodes_in_group("player")
	if not players.is_empty():
		var p: Node = players[0]
		player_state = {"pos": p.global_position, "rot": p.rotation.y, "held_kind": p.held_kind, "held": p.held}

func save_game(silent: bool = false) -> void:
	if story_stage == "diner":
		return  # Das Intro wird nicht gespeichert
	_collect_world_state()
	var state := {
		"version": SAVE_VERSION, "money": money, "day": day, "time_minutes": time_minutes, "story_stage": story_stage,
		"intro_step": intro_step, "tutorial_step": tutorial_step, "tut_flags": tut_flags, "location_stage": location_stage,
		"upgrades": upgrades, "decor_owned": decor_owned, "lifestyle_owned": lifestyle_owned, "reputation": reputation,
		"review_count": review_count, "reviews": reviews, "xp": xp, "level": level, "awareness": awareness,
		"goals_done": goals_done, "brand_named": brand_named, "ending_seen": ending_seen, "brand_name": brand_name,
		"brand_logo_index": brand_logo_index, "brand_color": brand_color, "traveling_deliveries": traveling_deliveries,
		"dock_crates": dock_crates, "stock": stock, "express_delivery": express_delivery,
		"blocked_suppliers": blocked_suppliers, "damaged_next_crate": damaged_next_crate, "order_queue": order_queue,
		"packed_packages": packed_packages, "packaging": packaging, "flat_packaging": flat_packaging,
		"packaging_brand": packaging_brand, "listed": listed, "shop_prices": shop_prices, "promo_active": promo_active,
		"shop_offline_until": shop_offline_until, "boosts": boosts, "tiktok_ready_at": tiktok_ready_at, "staff": staff,
		"social_posted_day": social_posted_day, "conveyor_queue": conveyor_queue, "world_items": world_items,
		"player_state": player_state, "total_shipped": total_shipped, "total_earned": total_earned,
		"total_lost_orders": total_lost_orders, "shipped_per_product": shipped_per_product, "daily": daily,
		"history": history, "market": Market.to_dict(), "events": Events.to_dict(),
		"saved_unix": int(Time.get_unix_time_from_system()),
	}
	var f := FileAccess.open(SAVE_PATH, FileAccess.WRITE)
	if f == null:
		notify("Speichern fehlgeschlagen!", "bad")
		return
	f.store_string(JSON.stringify(to_json_value(state)))
	f.close()
	last_saved_unix = int(state["saved_unix"])
	if not silent:
		notify("Spiel gespeichert.", "good")

func load_game() -> bool:
	if not has_save():
		return false
	var f := FileAccess.open(SAVE_PATH, FileAccess.READ)
	if f == null:
		return false
	var parsed = JSON.parse_string(f.get_as_text())
	f.close()
	if typeof(parsed) != TYPE_DICTIONARY:
		return false
	var s: Dictionary = from_json_value(parsed)
	reset_state()
	var version := int(s.get("version", 1))
	money = int(s.get("money", money))
	brand_name = String(s.get("brand_name", brand_name))
	brand_logo_index = int(s.get("brand_logo_index", 0))
	var bc = s.get("brand_color", brand_color)
	brand_color = bc if bc is Color else brand_color
	total_shipped = int(s.get("total_shipped", 0))
	total_earned = int(s.get("total_earned", 0))
	total_lost_orders = int(s.get("total_lost_orders", 0))
	express_delivery = bool(s.get("express_delivery", false))
	promo_active = bool(s.get("promo_active", false))
	for id in stock.keys():
		var st: Dictionary = s.get("stock", {})
		if st.has(id):
			stock[id] = {"qty": int(st[id]["qty"]), "quality": float(st[id]["quality"])}
		var ls: Dictionary = s.get("listed", {})
		if ls.has(id):
			listed[id] = bool(ls[id])
		var pr: Dictionary = s.get("shop_prices", {})
		if pr.has(id):
			shop_prices[id] = int(pr[id])
		var sp: Dictionary = s.get("shipped_per_product", {})
		if sp.has(id):
			shipped_per_product[id] = int(sp[id])
	if version < 2:
		# Alter Spielstand (vor dem großen Umbau): Geld, Marke und Lager übernehmen, Rest neu
		story_stage = "business"
		tutorial_step = -1
		economy_changed.emit()
		return true
	day = int(s.get("day", 1))
	time_minutes = float(s.get("time_minutes", GameData.DAY_START))
	story_stage = String(s.get("story_stage", "business"))
	intro_step = int(s.get("intro_step", 3))
	tutorial_step = int(s.get("tutorial_step", -1))
	tut_flags = s.get("tut_flags", {})
	location_stage = int(s.get("location_stage", 0))
	upgrades = s.get("upgrades", {})
	decor_owned = s.get("decor_owned", {})
	lifestyle_owned = s.get("lifestyle_owned", {})
	reputation = float(s.get("reputation", 3.0))
	review_count = int(s.get("review_count", 0))
	reviews = s.get("reviews", [])
	xp = int(s.get("xp", 0))
	level = int(s.get("level", 1))
	awareness = float(s.get("awareness", 0.0))
	goals_done = s.get("goals_done", {})
	brand_named = bool(s.get("brand_named", false))
	ending_seen = bool(s.get("ending_seen", false))
	traveling_deliveries = s.get("traveling_deliveries", [])
	dock_crates = s.get("dock_crates", [])
	blocked_suppliers = s.get("blocked_suppliers", {})
	damaged_next_crate = float(s.get("damaged_next_crate", 0.0))
	order_queue = s.get("order_queue", [])
	packed_packages = s.get("packed_packages", [])
	packaging = s.get("packaging", [20, 0, 0])
	flat_packaging = s.get("flat_packaging", [0, 0, 0])
	var pb: Array = s.get("packaging_brand", [])
	if pb.size() == 3:
		packaging_brand = pb
	shop_offline_until = float(s.get("shop_offline_until", -1.0))
	boosts = s.get("boosts", [])
	tiktok_ready_at = float(s.get("tiktok_ready_at", 0.0))
	staff = s.get("staff", [])
	social_posted_day = int(s.get("social_posted_day", 0))
	conveyor_queue = s.get("conveyor_queue", [])
	world_items = s.get("world_items", [])
	player_state = s.get("player_state", {})
	history = s.get("history", [])
	var d: Dictionary = s.get("daily", {})
	if not d.is_empty():
		for k in daily.keys():
			if d.has(k):
				daily[k] = d[k]
	last_saved_unix = int(s.get("saved_unix", 0))
	Market.from_dict(s.get("market", {}))
	Events.from_dict(s.get("events", {}))
	day_over = false
	if time_minutes >= GameData.DAY_END:
		time_minutes = GameData.DAY_END - 1.0
	economy_changed.emit()
	return true

func delete_save() -> void:
	if has_save():
		DirAccess.remove_absolute(ProjectSettings.globalize_path(SAVE_PATH))
