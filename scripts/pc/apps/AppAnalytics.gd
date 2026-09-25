extends PCApp
## Analytics: Kennzahlen, Umsatz-/Gewinnverlauf, Verkäufe je Produkt, heutige Kosten.

func build() -> void:
	var gm := GameManager
	var p := page()
	header(p, "📊 Analytics", "Zahlen, Daten, Fakten – dein Business auf einen Blick.")
	var r1 := hbox(p, 12)
	stat(r1, "Umsatz gesamt", money(gm.total_earned), UITheme.GOOD)
	stat(r1, "Umsatz heute", money(int(gm.daily["revenue"])))
	stat(r1, "Pakete gesamt", str(gm.total_shipped))
	stat(r1, "Verloren", str(gm.total_lost_orders), UITheme.BAD if gm.total_lost_orders > 0 else UITheme.TEXT)
	var r2 := hbox(p, 12)
	stat(r2, "Bewertung", "%s %s" % [UITheme.stars(gm.reputation), UITheme.rating_text(gm.reputation)], UITheme.ACCENT)
	stat(r2, "Firmenlevel", str(gm.level))
	stat(r2, "Portfolio", money(Market.portfolio_value()))
	var stock_value := 0.0
	for pr in GameData.PRODUCTS:
		stock_value += float(pr["unit_cost"]) * gm.stock_qty(pr["id"])
	stat(r2, "Lagerwert", money(int(round(stock_value))))
	var cc := card(p, "Umsatz & Cashflow pro Tag")
	var chart := LineChart.new()
	chart.include_zero = true
	chart.value_suffix = " €"
	chart.custom_minimum_size = Vector2(300, 220)
	cc.add_child(chart)
	var rev: Array = []
	var prof: Array = []
	for hh in gm.history:
		rev.append(float(hh["revenue"]))
		prof.append(float(hh["profit"]))
	chart.set_series([{"values": rev, "color": UITheme.ACCENT, "label": "Umsatz"}, {"values": prof, "color": UITheme.TEAL, "label": "Cashflow"}])
	var pc := card(p, "Verkäufe nach Produkt")
	var maxv := 1
	for pr in GameData.PRODUCTS:
		maxv = maxi(maxv, int(gm.shipped_per_product.get(pr["id"], 0)))
	for pr in GameData.PRODUCTS:
		var n := int(gm.shipped_per_product.get(pr["id"], 0))
		var row := hbox(pc, 10)
		swatch(row, pr["color"], Vector2(22, 22))
		var nl := label(row, pr["name"])
		nl.custom_minimum_size = Vector2(190, 0)
		bar(row, float(n), float(maxv), pr["color"], 10)
		var cl := label(row, str(n), "Muted")
		cl.custom_minimum_size = Vector2(50, 0)
		cl.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	var tc := card(p, "Heute: Einnahmen & Ausgaben")
	var d: Dictionary = gm.daily
	kv(tc, "Umsatz (Verkäufe)", money(int(d["revenue"])), UITheme.GOOD)
	kv(tc, "Sonstige Einnahmen", money(int(d["income_other"])), UITheme.GOOD)
	kv(tc, "Wareneinkauf", money(-int(d["purchases"])), UITheme.BAD)
	kv(tc, "Verpackung", money(-int(d["packaging"])), UITheme.BAD)
	kv(tc, "Marketing", money(-int(d["marketing"])), UITheme.BAD)
	kv(tc, "Sonstiges (Ausbau, Deko, Events)", money(-int(d["other"])), UITheme.BAD)
	kv(tc, "Fixkosten heute Abend (Miete, Löhne, Strom)", money(-(gm.rent_per_day() + gm.wages_per_day() + gm.upkeep_per_day())), UITheme.BAD)
