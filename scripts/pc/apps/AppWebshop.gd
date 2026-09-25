extends PCApp
## Webshop: Produkte online stellen, Preise setzen, Rabattaktion, Bewertungen.

func build() -> void:
	var gm := GameManager
	var p := page()
	header(p, "🌐 %s – Webshop" % gm.brand_name, "Stell Produkte online, setz Preise und behalte deine Bewertungen im Blick.")
	var top := hbox(p, 12)
	stat(top, "Bewertung", "%s %s" % [UITheme.stars(gm.reputation), UITheme.rating_text(gm.reputation)], UITheme.ACCENT)
	stat(top, "Bewertungen", str(gm.review_count))
	stat(top, "Offene Bestellungen", "%d / %d" % [gm.pending_count(), gm.queue_capacity()])
	var interval := gm.order_interval_minutes()
	stat(top, "Bestellungen/Stunde", ("~%.1f" % (60.0 / interval)).replace(".", ",") if interval > 0.0 else "—")

	var pc := card(p)
	var chk := CheckButton.new()
	chk.text = "Rabattaktion: −20 % auf alle Preise (lockt mehr Kunden an)"
	chk.button_pressed = gm.promo_active
	chk.toggled.connect(func(on: bool): GameManager.set_promo_active(on))
	pc.add_child(chk)

	for pr in GameData.PRODUCTS:
		var id: String = pr["id"]
		var c := card(p)
		var h := hbox(c, 14)
		swatch(h, pr["color"], Vector2(44, 44), pr["icon"])
		var v := vbox(h, 2)
		v.custom_minimum_size = Vector2(200, 0)
		label(v, pr["name"], "H3")
		if not gm.product_available(id):
			var why := "🔒 ab Firmenlevel %d" % int(pr["unlock_level"]) if not gm.product_unlocked(id) else "🔒 braucht die Lagerhalle"
			label(v, why, "Muted")
			(c.get_parent() as Control).modulate.a = 0.55
			continue
		var on := CheckButton.new()
		on.text = "Online"
		on.button_pressed = gm.listed.get(id, false)
		on.toggled.connect(func(x: bool): GameManager.set_listed(id, x))
		v.add_child(on)
		var price := int(gm.shop_prices[id])
		var pv := vbox(h, 4)
		var prow := hbox(pv, 4)
		for d in [-5, -1]:
			button(prow, "%d" % d, gm.set_shop_price.bind(id, price + d))
		var pl := label(prow, money(price), "H2", UITheme.ACCENT)
		pl.custom_minimum_size = Vector2(90, 0)
		pl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		for d in [1, 5]:
			button(prow, "+%d" % d, gm.set_shop_price.bind(id, price + d))
		var market := Market.market_price(id)
		var margin := float(price) - float(pr["unit_cost"])
		label(pv, "Markt: %s · Marge ~%s/Stk" % [money(int(market)), money(int(round(margin)))], "Muted")
		spacer(h)
		var dv := vbox(h, 4)
		dv.custom_minimum_size = Vector2(190, 0)
		label(dv, "Nachfrage: " + gm.demand_label(id))
		bar(dv, minf(gm.demand_rate(id), 3.0), 3.0, UITheme.TEAL, 8, 180)
		label(dv, "%d offen · %d verkauft" % [gm.pending_count_for(id), int(gm.shipped_per_product.get(id, 0))], "Muted")

	var rc := card(p, "Neueste Bewertungen")
	if gm.reviews.is_empty():
		wrap_label(rc, "Noch keine Bewertungen – verschick deine ersten Pakete!")
	var n := 0
	for r in gm.reviews:
		n += 1
		if n > 8:
			break
		var rh := hbox(rc, 12)
		var st := label(rh, UITheme.stars(float(r["stars"])), "", UITheme.ACCENT)
		st.custom_minimum_size = Vector2(90, 0)
		var rv := vbox(rh, 0)
		label(rv, "„%s“" % r["text"])
		label(rv, "%s · %s · Tag %d" % [r["name"], GameData.product(r["product"])["name"], int(r["day"])], "Small")
