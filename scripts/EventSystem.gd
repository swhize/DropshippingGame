extends Node
## Ereignisse & Postfach (Autoload "Events"). Jeden Geschäftstag werden 0-2 zufällige
## Ereignisse eingeplant. Automatische Ereignisse wirken sofort, Entscheidungs-Ereignisse
## öffnen ein Popup (oder warten im Postfach bis Feierabend, dann gilt die Standardwahl).

signal mail_received(mail: Dictionary)
signal choice_event(mail: Dictionary)
signal mail_changed

const EVENTS := [
	{"id": "black_friday", "title": "Black Friday!", "sender": "Shop-System", "icon": "🛍",
		"text": "Heute drehen alle durch. Die Leute kaufen alles, was nicht festgeschraubt ist – drei Stunden lang mehr als doppelt so viele Bestellungen!",
		"min_level": 2, "cooldown": 6, "weight": 1.0,
		"effects": {"boost": {"mult": 2.4, "minutes": 180.0, "name": "Black Friday"}}},
	{"id": "viral", "title": "Du gehst viral!", "sender": "TikTok", "icon": "🔥",
		"text": "Ein Video mit deinem Produkt hat über Nacht 2 Millionen Aufrufe. Die Nachfrage explodiert!",
		"needs": "listed", "cooldown": 4, "weight": 1.0,
		"effects": {"boost": {"mult": 3.0, "minutes": 120.0, "name": "Viraler Hit", "product": "random_listed"}, "awareness": 0.1}},
	{"id": "supplier_down", "title": "Billig-Fabrik geschlossen", "sender": "Billig-Fabrik", "icon": "🏭",
		"text": "Sehr geehrter Kunde, wegen 'Inspektion' (die Polizei war da) liefern wir 2 Tage nicht. Danke für Verständnis.",
		"cooldown": 5, "weight": 0.7, "effects": {"block_supplier": {"index": 0, "days": 2}}},
	{"id": "customs", "title": "Zollkontrolle", "sender": "Zollamt", "icon": "🛃",
		"text": "Eine deiner Lieferungen wird vom Zoll geprüft. Sie kommt zwei Stunden später an.",
		"needs": "deliveries", "cooldown": 3, "weight": 1.0, "effects": {"delay_delivery": 120.0}},
	{"id": "damaged", "title": "Transportschaden", "sender": "Spedition Rumpel & Söhne", "icon": "📦",
		"text": "Beim Verladen ist ein Gabelstapler 'leicht' gegen deine nächste Lieferung gefahren. Ein Viertel ist hin.",
		"needs": "deliveries", "cooldown": 4, "weight": 0.8, "effects": {"damage_next": 0.25}},
	{"id": "shitstorm", "title": "Shitstorm!", "sender": "Social Media", "icon": "😡",
		"text": "Ein Kunde behauptet auf TikTok, dein Produkt hätte sein Handy 'geschmolzen'. 40.000 Kommentare. Was tust du?",
		"min_level": 2, "cooldown": 5, "weight": 0.9, "default": 1,
		"choices": [
			{"label": "Entschuldigen + Gutscheine verteilen", "cost": 100, "outcomes": [
				{"p": 1.0, "text": "Die Community findet's fair. Der Sturm legt sich.", "effects": {"rep": 0.1}}]},
			{"label": "Ignorieren", "outcomes": [
				{"p": 1.0, "text": "Der Sturm tobt weiter. Deine Bewertung leidet.", "effects": {"rep": -0.35}}]},
			{"label": "Mit einem Meme antworten", "outcomes": [
				{"p": 0.5, "text": "Legendär! Dein Meme geht viral – alle lieben dich.", "effects": {"rep": 0.2, "awareness": 0.12, "boost": {"mult": 1.8, "minutes": 90.0, "name": "Meme-Hype"}}},
				{"p": 0.5, "text": "Cringe. Das Internet vergisst nicht.", "effects": {"rep": -0.5}}]}]},
	{"id": "crypto_bro", "title": "Bro, hör mir kurz zu", "sender": "Maximilian (Krypto-Bro)", "icon": "🚀",
		"text": "Bro. $HUSTLE-Coin. 100x garantiert. Nur heute. Investier 500 € und wir sehen uns auf den Malediven. 🚀🚀🚀",
		"min_level": 2, "min_money": 600, "cooldown": 7, "weight": 0.7, "default": 1,
		"choices": [
			{"label": "All in, Bro", "cost": 500, "outcomes": [
				{"p": 0.2, "text": "UNFASSBAR. Der Coin ist wirklich explodiert: +3.000 €!", "effects": {"money": 3000}},
				{"p": 0.8, "text": "Maximilians Profil ist gelöscht. Die 500 € sind weg.", "effects": {}}]},
			{"label": "Nein danke", "outcomes": [
				{"p": 1.0, "text": "Maximilian: 'Have fun staying poor.' Du schläfst trotzdem gut.", "effects": {}}]}]},
	{"id": "influencer", "title": "Kooperationsanfrage", "sender": "@lara.lifestyle (1,2 Mio.)", "icon": "🤳",
		"text": "Hey Babe! 💕 Ich würde dein Produkt in meiner Story zeigen. Kostet dich nur 300 €. Deal?",
		"min_level": 3, "min_money": 350, "cooldown": 5, "weight": 0.9, "default": 1,
		"choices": [
			{"label": "Deal!", "cost": 300, "outcomes": [
				{"p": 0.65, "text": "Die Story ballert! Die Bestellungen fliegen nur so rein.", "effects": {"boost": {"mult": 2.3, "minutes": 240.0, "name": "Influencer-Story"}, "awareness": 0.15}},
				{"p": 0.35, "text": "Sie hat aus Versehen den Link der Konkurrenz gepostet. Ups.", "effects": {}}]},
			{"label": "Ablehnen", "outcomes": [{"p": 1.0, "text": "'Okay, dann halt nicht 🙄'", "effects": {}}]}]},
	{"id": "pitch_day", "title": "Einladung zum Pitch Day", "sender": "MegaMarkt Einkauf", "icon": "🎤",
		"text": "Die Handelskette MegaMarkt sucht neue Marken für ihre Filialen. Du hast fünf Minuten, um sie zu überzeugen. Wie pitchst du?",
		"min_level": 3, "cooldown": 6, "weight": 0.8, "default": 2,
		"choices": [
			{"label": "Mit Leidenschaft und großen Visionen", "outcomes": [
				{"p": "rep", "text": "Standing Ovations! MegaMarkt gibt dir einen Großauftrag: +1.500 €", "effects": {"money": 1500, "rep": 0.1, "awareness": 0.08}},
				{"p": "rest", "text": "'Und die Zahlen?' – Stille. Kein Deal.", "effects": {}}]},
			{"label": "Mit harten Zahlen und Grafiken", "outcomes": [
				{"p": "level", "text": "Deine Excel-Tabelle überzeugt. Deal über 1.200 €!", "effects": {"money": 1200, "awareness": 0.05}},
				{"p": "rest", "text": "Zu wenig Umsatz für MegaMarkt. Vielleicht nächstes Jahr.", "effects": {}}]},
			{"label": "Absagen", "outcomes": [{"p": 1.0, "text": "Du konzentrierst dich lieber auf deinen Shop.", "effects": {}}]}]},
	{"id": "fake_reviews", "title": "Unmoralisches Angebot", "sender": "ReviewBoost Agentur", "icon": "⭐",
		"text": "50 Fünf-Sterne-Bewertungen für nur 200 €. Sehen garantiert echt aus. Niemand merkt was. 😉",
		"min_level": 2, "cooldown": 8, "weight": 0.6, "default": 1,
		"choices": [
			{"label": "Kaufen", "cost": 200, "outcomes": [
				{"p": 0.5, "text": "Deine Bewertung schießt nach oben. Niemand merkt was ... noch.", "effects": {"rep": 0.6}},
				{"p": 0.5, "text": "Aufgeflogen! Ein Verbraucherportal berichtet über dich. Katastrophe.", "effects": {"rep": -1.0}}]},
			{"label": "Ablehnen", "outcomes": [{"p": 1.0, "text": "Ehrlich währt am längsten. Deine Kunden merken das.", "effects": {"rep": 0.05}}]}]},
	{"id": "copycat", "title": "Die Konkurrenz kopiert dich", "sender": "Marktbeobachtung", "icon": "🕵",
		"text": "BilligBoy24 verkauft plötzlich exakt dein Produkt – 25 % billiger. Die nächsten zwei Tage wird's hart.",
		"needs": "listed", "min_level": 2, "cooldown": 6, "weight": 0.8, "effects": {"price_war": {"mult": 0.75, "days": 2}}},
	{"id": "server_down", "title": "Webshop offline!", "sender": "Hosting-Anbieter", "icon": "💥",
		"text": "Unser Server hat eine Kaffeepause gemacht. Dein Shop ist eine Stunde lang offline. Sorry!",
		"cooldown": 5, "weight": 0.6, "effects": {"offline": 60.0}},
	{"id": "tax", "title": "Post vom Finanzamt", "sender": "Finanzamt", "icon": "📨",
		"text": "Wir haben Fragen zu Ihren Einnahmen. Bitte reichen Sie sämtliche Unterlagen ein.",
		"min_level": 3, "min_money": 800, "cooldown": 8, "weight": 0.6, "default": 1,
		"choices": [
			{"label": "Steuerberater beauftragen", "cost": 150, "outcomes": [
				{"p": 1.0, "text": "Alles sauber. Der Berater hat sogar was zurückgeholt: +80 €.", "effects": {"money": 80}}]},
			{"label": "Selbst machen", "outcomes": [
				{"p": 0.5, "text": "Glück gehabt. Alles in Ordnung.", "effects": {}},
				{"p": 0.5, "text": "Formfehler! Nachzahlung: 10 % deines Kontostands.", "effects": {"money_pct": -0.1}}]}]},
	{"id": "oma", "title": "Ein Brief von Oma", "sender": "Oma Gerda", "icon": "💌",
		"text": "Mein Enkel, der Geschäftsmann! Ich hab dir 100 Euro beigelegt. Kauf dir was Warmes.",
		"max_money": 300, "cooldown": 10, "weight": 1.2, "effects": {"money": 100}},
	{"id": "kalle_calls", "title": "Kalle ruft an", "sender": "Kalle", "icon": "🍔",
		"text": "Ey. Mein Aushilfskellner ist krank. Kannst du heute zwei Stunden aushelfen? 80 Euro. Bar auf die Hand.",
		"min_day": 3, "cooldown": 7, "weight": 0.6, "default": 1,
		"choices": [
			{"label": "Aushelfen (2 Stunden weg)", "outcomes": [
				{"p": 1.0, "text": "Alte Zeiten. Du riechst nach Fritteuse, aber 80 € sind 80 €.", "effects": {"money": 80, "time_skip": 120.0}}]},
			{"label": "Nie wieder", "outcomes": [{"p": 1.0, "text": "Kalle: 'Pff. Undankbar.' *legt auf*", "effects": {}}]}]},
	{"id": "street_fest", "title": "Straßenfest vor der Tür", "sender": "Stadtverwaltung", "icon": "🎪",
		"text": "Heute ist Straßenfest! Ein Verkaufsstand kostet 50 € – du verkaufst direkt aus deinem Lager an Passanten.",
		"needs": "stock", "min_level": 2, "cooldown": 6, "weight": 0.8, "default": 1,
		"choices": [
			{"label": "Stand aufbauen", "cost": 50, "outcomes": [
				{"p": 1.0, "text": "Die Leute lieben deinen Stand!", "effects": {"sell_stock": {"units": 18, "price_mult": 1.15}}}]},
			{"label": "Keine Zeit", "outcomes": [{"p": 1.0, "text": "Du hörst die Musik nur von drinnen.", "effects": {}}]}]},
	{"id": "water_damage", "title": "Wasserschaden!", "sender": "Vermieter", "icon": "💧",
		"text": "Das Garagendach ist undicht. Ein Teil deiner Ware ist nass geworden.",
		"stage": 0, "needs": "stock", "cooldown": 8, "weight": 0.5, "effects": {"stock_loss": 0.15}},
	{"id": "tiktok_algo", "title": "Der Algorithmus liebt dich", "sender": "TikTok", "icon": "🎵",
		"text": "Eines deiner alten Videos wird plötzlich wieder ausgespielt. Gratis-Reichweite!",
		"min_level": 2, "cooldown": 5, "weight": 0.8, "effects": {"tiktok": 0.6}},
	{"id": "crypto_crash", "title": "Krypto-Crash!", "sender": "Finanznews", "icon": "📉",
		"text": "Ein Tech-Milliardär hat einen kritischen Post abgesetzt. DROPCOIN stürzt ab.",
		"min_level": 3, "cooldown": 6, "weight": 0.6, "effects": {"asset": {"id": "DROP", "mult": 0.45}}},
	{"id": "to_the_moon", "title": "DROPCOIN to the moon 🚀", "sender": "Finanznews", "icon": "📈",
		"text": "Ein Tech-Milliardär hat ein Hunde-Meme mit DROPCOIN gepostet. Der Kurs explodiert.",
		"min_level": 3, "cooldown": 6, "weight": 0.6, "effects": {"asset": {"id": "DROP", "mult": 2.2}}},
]

var mails: Array = []
var scheduled: Array = []
var next_id: int = 1
var last_seen: Dictionary = {}

func _ready() -> void:
	reset_state()

func reset_state() -> void:
	mails = []
	scheduled = []
	next_id = 1
	last_seen = {}

func _process(_delta: float) -> void:
	if scheduled.is_empty() or not GameManager.in_game or not GameManager.is_open():
		return
	var t := GameManager.time_minutes
	for i in range(scheduled.size() - 1, -1, -1):
		if t >= float(scheduled[i]["at"]):
			var id: String = scheduled[i]["id"]
			scheduled.remove_at(i)
			trigger(id)

func event_def(id: String) -> Dictionary:
	for e in EVENTS:
		if e["id"] == id:
			return e
	return {}

func new_day() -> void:
	scheduled = []
	var gm := GameManager
	if gm.tutorial_step >= 0 or gm.day < 2:
		return
	var count := 1
	var r := randf()
	if r < 0.15:
		count = 0
	elif r > 0.7:
		count = 2
	var pool: Array = []
	for ev in EVENTS:
		if _eligible(ev):
			pool.append(ev)
	for n in count:
		if pool.is_empty():
			break
		var idx := _weighted_index(pool)
		var ev: Dictionary = pool[idx]
		pool.remove_at(idx)
		scheduled.append({"id": ev["id"], "at": randf_range(570.0, 1080.0)})

func _eligible(ev: Dictionary) -> bool:
	var gm := GameManager
	if gm.level < int(ev.get("min_level", 1)):
		return false
	if gm.day < int(ev.get("min_day", 2)):
		return false
	if ev.has("min_money") and gm.money < int(ev["min_money"]):
		return false
	if ev.has("max_money") and gm.money > int(ev["max_money"]):
		return false
	if ev.has("stage") and gm.location_stage != int(ev["stage"]):
		return false
	if last_seen.has(ev["id"]) and gm.day - int(last_seen[ev["id"]]) < int(ev.get("cooldown", 3)):
		return false
	match String(ev.get("needs", "")):
		"deliveries":
			return not gm.traveling_deliveries.is_empty()
		"stock":
			return gm.stock_total() > 10
		"listed":
			return not _listed_products().is_empty()
	return true

func _weighted_index(pool: Array) -> int:
	var total := 0.0
	for e in pool:
		total += float(e.get("weight", 1.0))
	var r := randf() * total
	for i in pool.size():
		r -= float(pool[i].get("weight", 1.0))
		if r <= 0.0:
			return i
	return pool.size() - 1

func _listed_products() -> Array:
	var out: Array = []
	for p in GameData.PRODUCTS:
		if GameManager.listed.get(p["id"], false) and GameManager.product_available(p["id"]):
			out.append(p["id"])
	return out

## Löst ein Ereignis sofort aus (auch für Tests/Debug nutzbar).
func trigger(id: String) -> Dictionary:
	var ev := event_def(id)
	if ev.is_empty():
		return {}
	last_seen[id] = GameManager.day
	var mail := {"id": next_id, "event": id, "title": ev["title"], "sender": ev["sender"], "icon": ev.get("icon", "📧"),
		"text": ev["text"], "day": GameManager.day, "time": GameManager.time_minutes, "read": false,
		"pending": ev.has("choices"), "result": ""}
	next_id += 1
	if ev.has("choices"):
		var labels: Array = []
		for c in ev["choices"]:
			labels.append({"label": c["label"], "cost": int(c.get("cost", 0))})
		mail["choices"] = labels
	else:
		mail["result"] = "\n".join(_apply(ev.get("effects", {})))
	mails.push_front(mail)
	if mails.size() > 40:
		mails.pop_back()
	Audio.play("notify")
	GameManager.notify("%s %s" % [mail["icon"], mail["title"]], "info")
	mail_received.emit(mail)
	mail_changed.emit()
	if mail["pending"]:
		choice_event.emit(mail)
	GameManager.economy_changed.emit()
	return mail

func add_mail(sender: String, title: String, text: String, icon: String = "💬") -> void:
	var mail := {"id": next_id, "event": "", "title": title, "sender": sender, "icon": icon, "text": text,
		"day": GameManager.day, "time": GameManager.time_minutes, "read": false, "pending": false, "result": ""}
	next_id += 1
	mails.push_front(mail)
	mail_received.emit(mail)
	mail_changed.emit()

func find_mail(mail_id: int) -> Dictionary:
	for m in mails:
		if int(m["id"]) == mail_id:
			return m
	return {}

func mark_read(mail_id: int) -> void:
	var m := find_mail(mail_id)
	if not m.is_empty() and not m["read"]:
		m["read"] = true
		mail_changed.emit()

func unread_count() -> int:
	var n := 0
	for m in mails:
		if not m["read"] or m["pending"]:
			n += 1
	return n

func pending_count() -> int:
	var n := 0
	for m in mails:
		if m["pending"]:
			n += 1
	return n

## Entscheidung treffen. Gibt den Ergebnistext zurück ("" wenn nicht möglich).
func choose(mail_id: int, choice_index: int) -> String:
	var m := find_mail(mail_id)
	if m.is_empty() or not m["pending"]:
		return ""
	var ev := event_def(m["event"])
	var choice: Dictionary = ev["choices"][choice_index]
	var cost := int(choice.get("cost", 0))
	if cost > 0 and GameManager.money < cost:
		GameManager.notify("Dafür fehlt dir das Geld.", "bad")
		Audio.play("error")
		return ""
	if cost > 0:
		GameManager.adjust_money(-cost, "other")
	var outcomes: Array = choice["outcomes"]
	var probs := _probabilities(outcomes)
	var r := randf()
	var picked: Dictionary = outcomes[outcomes.size() - 1]
	for i in outcomes.size():
		r -= probs[i]
		if r <= 0.0:
			picked = outcomes[i]
			break
	var lines: Array = [String(picked["text"])]
	lines.append_array(_apply(picked.get("effects", {})))
	m["pending"] = false
	m["read"] = true
	m["chosen"] = choice["label"]
	m["result"] = "\n".join(lines)
	mail_changed.emit()
	GameManager.economy_changed.emit()
	return m["result"]

func _probabilities(outcomes: Array) -> Array:
	var gm := GameManager
	var probs: Array = []
	var fixed := 0.0
	for o in outcomes:
		var p = o["p"]
		var v := 0.0
		if p is String:
			match String(p):
				"rep": v = clampf(0.15 + gm.reputation * 0.12, 0.15, 0.85)
				"level": v = clampf(0.1 + gm.level * 0.09, 0.15, 0.9)
				_: v = -1.0
		else:
			v = float(p)
		probs.append(v)
		if v >= 0.0:
			fixed += v
	for i in probs.size():
		if float(probs[i]) < 0.0:
			probs[i] = maxf(0.0, 1.0 - fixed)
	return probs

func resolve_pending_choices() -> void:
	for m in mails:
		if m["pending"]:
			var ev := event_def(m["event"])
			var result := choose(int(m["id"]), int(ev.get("default", ev["choices"].size() - 1)))
			if result == "":
				m["pending"] = false
				m["result"] = "Keine Entscheidung getroffen."

# ---- Wirkungen ----------------------------------------------------------------------------
func _apply(effects: Dictionary) -> Array:
	var gm := GameManager
	var out: Array = []
	for key in effects:
		var v = effects[key]
		match String(key):
			"money":
				gm.adjust_money(int(v), "income_other" if int(v) > 0 else "other")
				out.append("%s %s" % ["+" if int(v) > 0 else "", UITheme.money(int(v))])
			"money_pct":
				var amount := int(round(maxf(gm.money, 0) * float(v)))
				gm.adjust_money(amount, "other")
				out.append(UITheme.money(amount))
			"rep":
				gm.change_reputation(float(v))
				out.append("Bewertung %+.1f ★" % float(v))
			"awareness":
				gm.add_awareness(float(v))
				out.append("Bekanntheit steigt")
			"boost":
				var pid: String = v.get("product", "")
				if pid == "random_listed":
					var lp := _listed_products()
					pid = lp[randi() % lp.size()] if not lp.is_empty() else ""
				gm.add_boost("event", String(v["name"]), float(v["mult"]), float(v["minutes"]), pid)
				out.append("×%.1f Nachfrage für %d min%s" % [float(v["mult"]), int(v["minutes"]),
					(" (" + GameData.product(pid)["name"] + ")") if pid != "" else ""])
			"block_supplier":
				gm.blocked_suppliers[str(int(v["index"]))] = gm.day + int(v["days"]) - 1
				out.append("%s für %d Tage gesperrt" % [GameData.SUPPLIERS[int(v["index"])]["name"], int(v["days"])])
			"delay_delivery":
				if not gm.traveling_deliveries.is_empty():
					var d: Dictionary = gm.traveling_deliveries[0]
					d["arrive_at"] = float(d["arrive_at"]) + float(v)
					d["van_sent"] = false
					out.append("Lieferung +%d min verspätet" % int(v))
			"damage_next":
				gm.damaged_next_crate = float(v)
				out.append("Nächste Lieferung beschädigt")
			"price_war":
				var lp2 := _listed_products()
				if not lp2.is_empty():
					var target: String = lp2[randi() % lp2.size()]
					Market.apply_price_war(target, float(v["mult"]), int(v["days"]))
					out.append("Preiskampf bei %s" % GameData.product(target)["name"])
			"offline":
				gm.shop_offline_until = gm.bclock() + float(v)
				out.append("Shop %d min offline" % int(v))
			"time_skip":
				gm.advance_minutes(float(v))
				out.append("%d Stunden vergangen" % int(float(v) / 60.0))
			"sell_stock":
				var sold := 0
				var earned := 0
				var units := int(v["units"])
				for p in GameData.PRODUCTS:
					var id: String = p["id"]
					while units > 0 and gm.stock_qty(id) > 0:
						gm.stock[id]["qty"] = gm.stock_qty(id) - 1
						units -= 1
						sold += 1
						earned += int(round(float(p["ref_price"]) * float(v["price_mult"])))
				if sold > 0:
					gm.adjust_money(earned, "income_other")
					gm.total_earned += earned
				out.append("%d Artikel verkauft: +%s" % [sold, UITheme.money(earned)])
			"stock_loss":
				var best_id := ""
				for p in GameData.PRODUCTS:
					if best_id == "" or gm.stock_qty(p["id"]) > gm.stock_qty(best_id):
						best_id = p["id"]
				var lost := int(round(gm.stock_qty(best_id) * float(v)))
				gm.stock[best_id]["qty"] = gm.stock_qty(best_id) - lost
				out.append("%d× %s verloren" % [lost, GameData.product(best_id)["name"]])
			"tiktok":
				gm.trigger_tiktok(float(v), true)
				out.append("Gratis-TikTok-Boost")
			"asset":
				Market.shock(String(v["id"]), float(v["mult"]))
				out.append("%s %s" % [Market.asset(String(v["id"]))["name"], UITheme.pct(float(v["mult"]) - 1.0)])
	gm.economy_changed.emit()
	return out

func to_dict() -> Dictionary:
	return {"mails": mails, "scheduled": scheduled, "next_id": next_id, "last_seen": last_seen}

func from_dict(d: Dictionary) -> void:
	if d.is_empty():
		return
	mails = d.get("mails", [])
	scheduled = d.get("scheduled", [])
	next_id = int(d.get("next_id", 1))
	last_seen = d.get("last_seen", {})
