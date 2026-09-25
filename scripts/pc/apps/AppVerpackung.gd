extends PCApp
## Verpackung: Kartons in S/M/L kaufen (fertig oder ungefaltet), mit Marken-Aufdruck.

func build() -> void:
	var gm := GameManager
	var p := page()
	header(p, "🧰 Verpackung", "Kartons in drei Größen. Beim Kauf wird dein aktuelles Branding aufgedruckt.")
	var ph := hbox(p, 16)
	var prev: Control = preload("res://scripts/pc/BrandPreview.gd").new()
	prev.setup(gm.brand_logo_index, gm.brand_color, gm.brand_name)
	ph.add_child(prev)
	var pv := vbox(ph, 6)
	pv.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	label(pv, "Dein aktueller Druck", "H3")
	wrap_label(pv, "Neue Kartons bekommen Logo und Farbe deiner Marke. Schon gekaufte Kartons behalten ihren alten Druck.")
	wrap_label(pv, "Ungefaltete Kartons kosten nur die Hälfte, müssen aber erst am Falttisch gefaltet werden.")
	for size in 3:
		var c := card(p)
		var h := hbox(c, 14)
		var brand: Dictionary = gm.packaging_brand[size]
		swatch(h, brand["color"], Vector2(50, 50), GameData.size_name(size))
		var v := vbox(h, 2)
		v.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		label(v, "Karton %s" % GameData.size_name(size), "H3")
		var fits: Array = []
		for pr in GameData.PRODUCTS:
			if int(pr["size"]) == size:
				fits.append(pr["name"])
		label(v, "Passt für: " + ", ".join(fits), "Muted")
		label(v, "Vorrat: %d gefaltet · %d ungefaltet" % [int(gm.packaging[size]), int(gm.flat_packaging[size])])
		var bv := vbox(h, 6)
		var r1 := hbox(bv, 6)
		button(r1, "10 Stk · %s" % money(gm.packaging_cost(size, false, 0)), gm.buy_packaging.bind(size, false, 0), "AccentButton", gm.money < gm.packaging_cost(size, false, 0))
		button(r1, "50 Stk · %s" % money(gm.packaging_cost(size, false, 1)), gm.buy_packaging.bind(size, false, 1), "", gm.money < gm.packaging_cost(size, false, 1))
		var r2 := hbox(bv, 6)
		button(r2, "10 ungefaltet · %s" % money(gm.packaging_cost(size, true, 0)), gm.buy_packaging.bind(size, true, 0), "", gm.money < gm.packaging_cost(size, true, 0))
		button(r2, "50 ungefaltet · %s" % money(gm.packaging_cost(size, true, 1)), gm.buy_packaging.bind(size, true, 1), "", gm.money < gm.packaging_cost(size, true, 1))
