extends PCApp
## Marktanalyse: Preise der Konkurrenz je Produkt, Marktpreis, Preiskämpfe.

func build() -> void:
	var gm := GameManager
	var p := page()
	header(p, "🏪 Marktanalyse", "Der günstigste Konkurrenzpreis ist der Marktpreis. Liegst du darunter, steigt deine Nachfrage deutlich.")
	var cr := hbox(p, 10)
	for c in Market.COMPETITORS:
		var cc := card(cr)
		label(cc, c["name"], "H3")
		label(cc, c["desc"], "Muted")
	if not Market.comp_mods.is_empty():
		var wc := card(p, "", "CardHi")
		for pid in Market.comp_mods:
			label(wc, "⚔ Preiskampf: BilligBoy24 unterbietet bei %s bis Tag %d" % [GameData.product(pid)["name"], int(Market.comp_mods[pid]["until"])], "", UITheme.BAD)
	var tc := card(p)
	var grid := GridContainer.new()
	grid.columns = 7
	grid.add_theme_constant_override("h_separation", 18)
	grid.add_theme_constant_override("v_separation", 10)
	tc.add_child(grid)
	for head in ["Produkt", "Dein Preis", "BilligBoy24", "TrendHaus", "AliExpresso", "Marktpreis", "Nachfrage"]:
		label(grid, head, "Small")
	for pr in GameData.PRODUCTS:
		var id: String = pr["id"]
		if not gm.product_unlocked(id):
			continue
		label(grid, "%s %s" % [pr["icon"], pr["name"]])
		var mine := gm.current_sale_price(id)
		var market := Market.market_price(id)
		label(grid, money(mine), "", UITheme.GOOD if mine <= market else UITheme.BAD)
		for ci in Market.COMPETITORS.size():
			label(grid, money(int(round(Market.competitor_price(ci, id)))), "Muted")
		label(grid, money(int(round(market))), "AccentLabel")
		label(grid, gm.demand_label(id) if gm.listed.get(id, false) else "offline", "Muted")
	var tip := card(p)
	wrap_label(tip, "💡 Faustregel: Knapp unter dem Marktpreis verkaufst du viel mit guter Marge. Premium-Ware bringt bessere Bewertungen – und gute Bewertungen bringen mehr Kunden.", "")
