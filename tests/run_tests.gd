extends SceneTree
## Headless Logik-Tests: godot --headless --path . -s tests/run_tests.gd
## Prüft die komplette Spiellogik direkt über die Autoloads (ohne Welt/Eingaben).

var failures: int = 0
var checks: int = 0
var gm: Node
var events: Node
var market: Node

func _initialize() -> void:
	gm = root.get_node_or_null("GameManager")
	events = root.get_node_or_null("Events")
	market = root.get_node_or_null("Market")
	if gm == null or events == null or market == null:
		push_error("Autoloads fehlen")
		quit(1)
		return
	_run_all()
	gm.delete_save()
	print("\n=== TESTS: %d Checks, %d Fehler ===" % [checks, failures])
	quit(1 if failures > 0 else 0)

func _check(cond: bool, what: String) -> void:
	checks += 1
	if cond:
		print("  ok   " + what)
	else:
		failures += 1
		print("  FAIL " + what)

func _fresh() -> void:
	gm.reset_state()
	gm.tutorial_step = -1

func _run_all() -> void:
	_test_basics()
	_test_orders()
	_test_packaging()
	_test_demand()
	_test_day_cycle()
	_test_progression()
	_test_staff_and_conveyor()
	_test_events_and_market()
	_test_intro_and_tutorial()
	_test_save_load()
	_test_audio()

func _test_basics() -> void:
	print("== Grundlagen & Einkauf ==")
	_fresh()
	_check(gm.money == GameData.START_CAPITAL, "Startkapital %d" % GameData.START_CAPITAL)
	_check(GameData.PRODUCTS.size() == 6, "6 Produkte im Katalog")
	_check(gm.level == 1 and gm.location_stage == 0, "Level 1, Garage")
	_check(gm.product_available("huelle") and not gm.product_available("led"), "Nur Handyhülle zu Beginn verfügbar")
	var cost: int = gm.bulk_cost(0, 0, 1)
	_check(cost == 40, "20 Hüllen beim Standard-Anbieter kosten 40 € (ist %d)" % cost)
	_check(gm.buy_bulk(0, 0, 1), "Einkauf klappt")
	_check(gm.money == GameData.START_CAPITAL - 40, "Geld abgezogen")
	_check(not gm.buy_bulk(1, 0, 1), "Gesperrtes Produkt (LED) kann nicht gekauft werden")
	_check(not gm.buy_bulk(0, 0, 2), "Premium-Anbieter ist auf Level 1 gesperrt")
	var lead: float = gm.lead_minutes(1)
	gm.advance_minutes(lead + 0.5)
	_check(gm.traveling_deliveries.is_empty() and gm.dock_crates.size() == 1, "Lieferung nach %d min am Wareneingang" % int(lead))
	var crate: Dictionary = gm.pickup_crate()
	_check(int(crate["quantity"]) == 20, "Kiste enthält 20 Stück")
	_check(gm.unbox_crate("huelle", 20, 1.0), "Kiste eingeräumt")
	_check(gm.stock_qty("huelle") == 20, "Lagerbestand 20")
	_check(not gm.unbox_crate("huelle", 1000, 1.0), "Lagerkapazität der Garage wird eingehalten")

func _test_orders() -> void:
	print("== Bestellungen, Verpacken, Versand ==")
	_fresh()
	gm.stock["huelle"] = {"qty": 10, "quality": 1.0}
	gm.set_listed("huelle", true)
	gm.spawn_order("huelle")
	_check(gm.pending_count() == 1, "Eine Bestellung eingegangen")
	var price: int = gm.order_queue[0]["price"]
	_check(price == gm.current_sale_price("huelle"), "Preis wird bei Bestellung festgeschrieben")
	var item: Dictionary = gm.pick_item("huelle")
	_check(not item.is_empty() and gm.pending_count() == 0 and gm.stock_qty("huelle") == 9, "Kommissioniert")
	gm.return_item(item)
	_check(gm.pending_count() == 1 and gm.stock_qty("huelle") == 10, "Zurücklegen stellt Bestellung + Bestand wieder her")
	item = gm.pick_item("huelle")
	var cartons_before: int = int(gm.packaging[0])
	_check(gm.wrap_item(item), "Verpackt")
	_check(int(gm.packaging[0]) == cartons_before - 1, "Karton S verbraucht")
	var pkg: Dictionary = gm.pickup_package()
	_check(pkg.has("color") and pkg.has("logo"), "Paket trägt Branding")
	var money_before: int = gm.money
	var xp_before: int = gm.xp
	gm.ship_package(pkg)
	_check(gm.money >= money_before + price - price, "Versand bringt Geld (%d → %d)" % [money_before, gm.money])
	_check(gm.total_shipped == 1 and gm.xp > xp_before, "Versand zählt + gibt XP")
	_check(gm.goals_done.get("first_sale", false), "Ziel 'Erster Verkauf' erreicht")
	print("-- Warteschlange voll --")
	gm.order_queue.clear()
	for i in gm.queue_capacity():
		gm.spawn_order("huelle")
	var lost: int = gm.total_lost_orders
	var reviews: int = gm.review_count
	for i in 10:
		gm.spawn_order("huelle")
	_check(gm.total_lost_orders == lost + 10, "Verlorene Bestellungen gezählt")
	var new_reviews: int = gm.review_count - reviews
	_check(new_reviews >= 1 and new_reviews <= 10 and int(gm.reviews[0]["stars"]) == 1, "Verlorene Bestellungen erzeugen teils 1-Stern-Bewertungen (%d von 10)" % new_reviews)
	print("-- Stornierung --")
	gm.order_queue.clear()
	gm.spawn_order("huelle")
	gm.order_queue[0]["created"] = gm.bclock() - GameData.ORDER_EXPIRE_MINUTES - 1.0
	gm._update_expiry()
	_check(gm.order_queue.is_empty(), "Zu alte Bestellung wird storniert")

func _test_packaging() -> void:
	print("== Verpackung & Falttisch ==")
	_fresh()
	_check(gm.buy_packaging(1, true, 0), "Ungefaltete M-Kartons gekauft")
	_check(int(gm.flat_packaging[1]) == 10, "10 ungefaltet")
	var size: int = gm.fold_carton()
	_check(size == 1 and int(gm.packaging[1]) == 1 and int(gm.flat_packaging[1]) == 9, "Falten wandelt 1 Karton um")
	_check(gm.packaging_cost(0, false, 1) == 60, "50er-Pack S kostet 60 €")
	gm.set_brand_color(GameData.BRAND_PALETTE[3])
	gm.buy_packaging(2, false, 0)
	_check((gm.packaging_brand[2]["color"] as Color).is_equal_approx(GameData.BRAND_PALETTE[3]), "Markenfarbe wird beim Kauf aufgedruckt")
	_check(not (gm.packaging_brand[0]["color"] as Color).is_equal_approx(GameData.BRAND_PALETTE[3]), "Alte Kartons behalten ihren Druck")

func _test_demand() -> void:
	print("== Nachfrage ==")
	_fresh()
	gm.set_listed("huelle", true)
	var r1: float = gm.demand_rate("huelle")
	gm.set_shop_price("huelle", 60)
	var r2: float = gm.demand_rate("huelle")
	_check(r1 > 0.0 and r2 < r1, "Höherer Preis → weniger Nachfrage")
	gm.set_listed("huelle", false)
	_check(gm.demand_rate("huelle") == 0.0, "Offline → keine Nachfrage")
	gm.listed["massage"] = true
	_check(gm.demand_rate("massage") == 0.0, "Gesperrtes Produkt hat keine Nachfrage")
	gm.set_listed("huelle", true)
	gm.set_shop_price("huelle", 20)
	var base: float = gm.demand_rate("huelle")
	gm.reputation = 5.0
	_check(gm.demand_rate("huelle") > base, "Gute Bewertung → mehr Nachfrage")
	gm.add_boost("test", "Test", 2.0, 30.0)
	_check(gm.boost_mult("huelle") >= 2.0, "Boost multipliziert Nachfrage")
	gm.advance_minutes(31.0)
	_check(gm.boosts.is_empty(), "Boost läuft aus")

func _test_day_cycle() -> void:
	print("== Tagesablauf ==")
	_fresh()
	var got: Array = []
	var cb := func(s: Dictionary): got.append(s)
	gm.day_ended.connect(cb)
	gm.request_end_day()
	_check(gm.day_over and got.size() == 1, "Tagesende löst Abrechnung aus")
	_check(int(got[0]["rent"]) == GameData.STAGE_RENT[0], "Miete in der Abrechnung")
	var money_before: int = gm.money
	gm.start_next_day()
	_check(gm.day == 2 and not gm.day_over and gm.time_minutes == GameData.DAY_START, "Tag 2 beginnt um 08:00")
	_check(gm.money == money_before - GameData.STAGE_RENT[0], "Miete abgebucht")
	_check(gm.history.size() == 1, "Tageshistorie gespeichert")
	gm.day_ended.disconnect(cb)
	print("-- Pleite --")
	var over: Array = []
	var cb2 := func(r: String): over.append(r)
	gm.game_over.connect(cb2)
	gm.money = -600
	gm.request_end_day()
	gm.start_next_day()
	_check(over.size() == 1 and gm.day == 2, "Unter −500 € → Insolvenz statt neuem Tag")
	gm.game_over.disconnect(cb2)
	print("-- Zeit läuft --")
	_fresh()
	gm.advance_minutes(100.0)
	_check(absf(gm.time_minutes - 580.0) < 0.01, "Uhr läuft (08:00 + 100 min)")
	gm.advance_minutes(1000.0)
	_check(gm.day_over and gm.time_minutes == GameData.DAY_END, "Um 20 Uhr ist Feierabend")

func _test_progression() -> void:
	print("== Level, Ziele, Ausbau ==")
	_fresh()
	var ups: Array = []
	var cb := func(l: int): ups.append(l)
	gm.level_up.connect(cb)
	gm._add_xp(160)
	_check(gm.level == 2 and ups.size() == 1, "150 XP → Level 2")
	_check(gm.product_available("led"), "Level 2 schaltet LED frei")
	gm.level_up.disconnect(cb)
	_check(gm.upgrade_state("warehouse") == "level", "Lagerhalle erst ab Level 4")
	gm.level = 4
	gm.money = 6000
	_check(gm.buy_upgrade("warehouse"), "Lagerhalle gekauft")
	_check(gm.location_stage == 1 and gm.capacity() == 3000, "Lagerhalle: 3.000 Lagerplätze")
	_check(gm.goals_done.get("warehouse", false), "Ziel 'Raus aus der Garage'")
	_check(gm.product_has_shelf("ringlicht"), "Lagerhalle hat Regale für alle Produkte")
	_check(gm.upgrade_state("conveyor") == "level", "Förderband braucht Level 6")
	gm.set_brand_name("  NovaGoods  ")
	_check(gm.brand_name == "NovaGoods" and gm.goals_done.get("brand", false), "Markenname gesetzt + Ziel")
	gm.money = 500
	_check(gm.buy_decor("pflanze") and gm.decor_owned.get("pflanze", false), "Deko kaufen")
	_check(gm.buy_lifestyle("sneaker") and gm.lifestyle_owned.get("sneaker", false), "Lifestyle kaufen")

func _test_staff_and_conveyor() -> void:
	print("== Personal & Förderband ==")
	_fresh()
	_check(not gm.hire("packer"), "Kein Personal in der Garage")
	gm.location_stage = 1
	gm.upgrades["warehouse"] = true
	gm.level = 5
	_check(gm.hire("packer"), "Packer:in eingestellt")
	_check(gm.wages_per_day() == 70, "Lohn 70 €/Tag")
	gm.stock["huelle"] = {"qty": 5, "quality": 1.0}
	gm.listed["huelle"] = true
	gm.spawn_order("huelle")
	gm._update_staff(13.0)
	_check(gm.packed_count() == 1 and gm.pending_count() == 0, "Packer:in verpackt automatisch")
	_check(gm.hire("versand"), "Versandkraft eingestellt")
	var shipped: int = gm.total_shipped
	gm._update_staff(11.0)
	_check(gm.total_shipped == shipped + 1, "Versandkraft verschickt automatisch")
	_check(gm.hire("lager"), "Lagerist:in eingestellt")
	gm.dock_crates.append({"product": "huelle", "quantity": 20, "quality": 1.0})
	gm._update_staff(17.0)
	_check(gm.dock_crates.is_empty() and gm.stock_qty("huelle") >= 20, "Lagerist:in räumt Kisten ein")
	gm.fire(0)
	_check(gm.staff.size() == 2, "Entlassen")
	gm.upgrades["conveyor"] = true
	var before: int = gm.total_shipped
	_check(gm.conveyor_insert({"product": "huelle", "price": 20, "quality": 1.0, "created": gm.bclock(), "color": Color.RED, "logo": 0}), "Paket aufs Band")
	gm.advance_minutes(7.0)
	_check(gm.total_shipped == before + 1 and gm.conveyor_queue.is_empty(), "Förderband verschickt automatisch")

func _test_events_and_market() -> void:
	print("== Ereignisse ==")
	_fresh()
	var m0: int = gm.money
	events.trigger("oma")
	_check(gm.money == m0 + 100 and events.mails.size() == 1, "Oma schickt 100 €")
	var mail: Dictionary = events.trigger("shitstorm")
	_check(mail["pending"] and events.pending_count() == 1, "Shitstorm wartet auf Entscheidung")
	var rep: float = gm.reputation
	var res: String = events.choose(int(mail["id"]), 1)
	_check(res != "" and gm.reputation < rep and events.pending_count() == 0, "Ignorieren kostet Bewertung")
	var m2: Dictionary = events.trigger("crypto_bro")
	gm.money = 10
	_check(events.choose(int(m2["id"]), 0) == "", "Ohne Geld keine Investition")
	events.resolve_pending_choices()
	_check(events.pending_count() == 0, "Offene Entscheidungen werden um 20 Uhr aufgelöst")
	gm.money = 1000
	gm.traveling_deliveries.append({"product": "huelle", "quantity": 20, "quality": 1.0, "arrive_at": gm.bclock() + 10.0, "van_sent": false})
	events.trigger("customs")
	_check(float(gm.traveling_deliveries[0]["arrive_at"]) > gm.bclock() + 100.0, "Zoll verzögert Lieferung")
	var ok := true
	for ev in events.EVENTS:
		if ev.has("choices"):
			for c in ev["choices"]:
				var probs: Array = events._probabilities(c["outcomes"])
				var s := 0.0
				for p in probs:
					s += float(p)
				if absf(s - 1.0) > 0.001:
					ok = false
					print("     Wahrscheinlichkeiten ≠ 1 bei ", ev["id"])
	_check(ok, "Alle Entscheidungs-Wahrscheinlichkeiten summieren sich zu 1")
	events.trigger("to_the_moon")
	_check(events.mails.size() >= 4, "Postfach füllt sich")
	print("== Markt & Trading ==")
	_check(market.market_price("huelle") > 5.0, "Marktpreis vorhanden (%.0f)" % market.market_price("huelle"))
	var mp: float = market.market_price("huelle")
	market.apply_price_war("huelle", 0.75, 2)
	_check(market.market_price("huelle") < mp, "Preiskampf senkt Marktpreis")
	gm.money = 1000
	_check(market.buy("ETF", 500), "ETF gekauft")
	_check(gm.money == 500 and float(market.holdings["ETF"]) > 0.0, "Geld investiert")
	var back: int = market.sell("ETF", 1.0)
	_check(back > 480 and back < 500, "Verkauf bringt Geld minus Gebühren (%d)" % back)
	_check(float(market.holdings["ETF"]) == 0.0, "Bestand leer")
	gm.level = 2
	gm.tiktok_ready_at = 0.0
	_check(gm.tiktok_available(), "TikTok ab Level 2 verfügbar")
	gm.trigger_tiktok(0.9)
	_check(not gm.active_boost("tiktok").is_empty() and not gm.tiktok_available(), "TikTok-Boost aktiv + Abklingzeit")

func _test_intro_and_tutorial() -> void:
	print("== Imbiss-Intro ==")
	gm.new_game("intro")
	_check(gm.story_stage == "diner" and gm.money == 0, "Intro startet im Imbiss ohne Geld")
	_check(gm.diner_plates_waiting() == 0, "Vor dem Gespräch mit Kalle keine Teller")
	gm.start_shift()
	_check(gm.diner_plates_waiting() == 3, "Drei Teller warten")
	var plate: Dictionary = gm.diner_take_plate()
	_check(int(plate["table"]) == 2, "Erster Teller für Tisch 2")
	_check(not gm.diner_serve(3, plate), "Falscher Tisch wird abgelehnt")
	_check(gm.diner_serve(2, plate), "Richtiger Tisch")
	for t in [3, 4]:
		var pl: Dictionary = gm.diner_take_plate()
		gm.diner_serve(int(pl["table"]), pl)
	_check(gm.intro_step == 2, "Alle Teller serviert → zurück zu Kalle")
	gm.finish_intro()
	_check(gm.story_stage == "business" and gm.money == GameData.START_CAPITAL and gm.tutorial_step == 0, "Kündigung: 150 € und Tutorial startet")
	print("== Tutorial ==")
	gm.new_game("tutorial")
	_check(gm.tutorial_step == 0, "Tutorial bei Schritt 1")
	gm.open_pc()
	gm.close_pc()
	_check(gm.tutorial_step == 1, "Laptop geöffnet → Schritt 2")
	gm.buy_bulk(0, 0, 1)
	_check(gm.tutorial_step == 2, "Bestellt → Schritt 3")
	gm.advance_minutes(gm.lead_minutes(1) + 1.0)
	var c: Dictionary = gm.pickup_crate()
	_check(gm.tutorial_step == 3, "Kiste genommen → Schritt 4")
	gm.unbox_crate("huelle", int(c["quantity"]), float(c["quality"]))
	_check(gm.tutorial_step == 4, "Eingeräumt → Schritt 5")
	gm.set_listed("huelle", true)
	_check(gm.tutorial_step == 5, "Online → Schritt 6")
	gm.advance_minutes(5.0)
	_check(gm.pending_count() >= 1 and gm.tutorial_step == 6, "Erste Bestellung kommt garantiert")
	var it: Dictionary = gm.pick_item("huelle")
	gm.wrap_item(it)
	var pk: Dictionary = gm.pickup_package()
	gm.on_labeled()
	gm.ship_package(pk)
	_check(gm.tutorial_step == -1, "Tutorial abgeschlossen")
	gm.clear_locks()

func _test_save_load() -> void:
	print("== Speichern / Laden ==")
	_fresh()
	gm.money = 4242
	gm.day = 7
	gm.level = 5
	gm.xp = 3000
	gm.location_stage = 1
	gm.upgrades["warehouse"] = true
	gm.brand_name = "SaveTest"
	gm.brand_color = Color(0.1, 0.8, 0.3, 1.0)
	gm.stock["led"] = {"qty": 42, "quality": 1.6}
	gm.packaging = [3, 4, 5]
	gm.packed_packages.append({"product": "led", "price": 33, "quality": 1.0, "created": 12.5, "color": Color(0.9, 0.1, 0.1, 1.0), "logo": 2})
	gm.staff.append({"role": "packer", "name": "Mia", "progress": 0.5})
	gm.boosts.append({"source": "ad", "name": "Facebook-Ads", "mult": 1.6, "ends_at": gm.bclock() + 50.0, "product": ""})
	gm.reviews.append({"stars": 5, "text": "Top", "name": "Ben", "product": "led", "day": 7})
	gm.goals_done["first_sale"] = true
	market.holdings["DROP"] = 12.5
	events.add_mail("Test", "Hallo", "Text")
	gm.save_game(true)
	_check(gm.has_save(), "Spielstand geschrieben")
	var summary: Dictionary = gm.save_summary()
	_check(int(summary["day"]) == 7 and String(summary["brand"]) == "SaveTest", "Menü-Zusammenfassung lesbar")
	gm.reset_state()
	_check(gm.money == GameData.START_CAPITAL, "Zurückgesetzt")
	_check(gm.load_game(), "Geladen")
	_check(gm.money == 4242 and gm.day == 7 and gm.level == 5, "Geld/Tag/Level korrekt")
	_check(gm.location_stage == 1 and gm.upgrades.get("warehouse", false), "Standort + Upgrades korrekt")
	_check(gm.brand_color.is_equal_approx(Color(0.1, 0.8, 0.3, 1.0)), "Farbe korrekt (JSON-Konvertierung)")
	_check(gm.stock_qty("led") == 42 and absf(gm.stock_quality("led") - 1.6) < 0.001, "Lager korrekt")
	_check(int(gm.packaging[2]) == 5, "Kartons korrekt")
	_check(gm.packed_packages.size() == 1 and (gm.packed_packages[0]["color"] as Color).is_equal_approx(Color(0.9, 0.1, 0.1, 1.0)), "Pakete mit Farbe korrekt")
	_check(gm.staff.size() == 1 and String(gm.staff[0]["name"]) == "Mia", "Personal korrekt")
	_check(gm.boosts.size() == 1 and gm.reviews.size() == 1 and gm.goals_done.get("first_sale", false), "Boosts/Bewertungen/Ziele korrekt")
	_check(absf(float(market.holdings["DROP"]) - 12.5) < 0.001, "Trading-Bestand korrekt")
	_check(events.mails.size() == 1, "Postfach korrekt")
	var conv = gm.from_json_value(JSON.parse_string(JSON.stringify(gm.to_json_value({"v": Vector3(1, 2, 3), "n": 5}))))
	_check(conv["v"] is Vector3 and conv["n"] is int, "JSON-Konverter: Vector3 + Ganzzahlen")
	gm.delete_save()
	_check(not gm.has_save(), "Test-Spielstand wieder gelöscht")
	gm.reset_state()

func _test_audio() -> void:
	print("== Prozedurale Klänge ==")
	var audio: Node = root.get_node_or_null("Audio")
	var all_ok := true
	for n in ["click", "cash", "tape", "printer", "levelup", "truck", "door", "whoosh", "plate", "error", "notify", "step"]:
		var b: PackedByteArray = audio.render(n, false)
		if b.size() < 200:
			all_ok = false
			print("     zu kurz: ", n)
	_check(all_ok, "Alle Soundeffekte werden synthetisiert")
	var t0 := Time.get_ticks_msec()
	for t in ["menu", "work", "diner"]:
		var mb: PackedByteArray = audio.render(t, true)
		_check(mb.size() > 22050 * 2 * 10, "Musikstück '%s' erzeugt (%.1f s)" % [t, mb.size() / 2.0 / 22050.0])
	print("     Musik-Synthese dauerte %d ms" % (Time.get_ticks_msec() - t0))
