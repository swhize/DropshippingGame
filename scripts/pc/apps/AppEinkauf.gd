extends PCApp
## Einkauf: Produkt wählen, Lieferant wählen, Menge bestellen.

static var sel_product: int = 0
static var sel_supplier: int = 1

func _eur(v: float) -> String:
	return ("%.2f €" % v).replace(".", ",")

func build() -> void:
	var gm := GameManager
	var p := page()
	header(p, "📦 Einkauf", "Bestell Ware bei Lieferanten. Jede Bestellung kommt als Kiste am Wareneingang an – der Lieferwagen hält vor der Tür.")
	if not gm.product_available(GameData.PRODUCTS[sel_product]["id"]):
		sel_product = 0
	if not gm.supplier_available(sel_supplier):
		sel_supplier = 1 if gm.supplier_available(1) else 0
	var prod_row := flow(p, 8)
	for i in GameData.PRODUCTS.size():
		var pr: Dictionary = GameData.PRODUCTS[i]
		var id: String = pr["id"]
		var b := Button.new()
		b.custom_minimum_size = Vector2(158, 62)
		if gm.product_available(id):
			b.text = "%s %s\nLager: %d" % [pr["icon"], pr["name"], gm.stock_qty(id)]
		elif not gm.product_unlocked(id):
			b.text = "%s %s\n🔒 ab Level %d" % [pr["icon"], pr["name"], int(pr["unlock_level"])]
			b.disabled = true
		else:
			b.text = "%s %s\n🔒 braucht Lagerhalle" % [pr["icon"], pr["name"]]
			b.disabled = true
		b.theme_type_variation = "SideActive" if i == sel_product else ""
		b.pressed.connect(_sel_product.bind(i))
		prod_row.add_child(b)

	var pr: Dictionary = GameData.PRODUCTS[sel_product]
	var id: String = pr["id"]
	var info := card(p)
	var ih := hbox(info, 14)
	swatch(ih, pr["color"], Vector2(52, 52), pr["icon"])
	var iv := vbox(ih, 2)
	label(iv, pr["name"], "H2")
	label(iv, "Einkauf ab %s/Stk · Marktpreis ~%s · Karton %s" % [_eur(float(pr["unit_cost"])), money(int(Market.market_price(id))), GameData.size_name(int(pr["size"]))], "Muted")
	spacer(ih)
	var sv := vbox(ih, 2)
	var q := gm.stock_quality(id)
	label(sv, "Lager: %d Stück (%s)" % [gm.stock_qty(id), "Premium" if q >= 1.3 else ("Billig" if q < 0.8 else "Standard")])
	label(sv, "Unterwegs: %d · Am Eingang: %d" % [gm.traveling_count_for(id), gm.dock_count_for(id)], "Muted")
	label(sv, "Lagerplatz gesamt: %d / %d" % [gm.stock_total(), gm.capacity()], "Muted")

	label(p, "Lieferant", "H3")
	var sh := hbox(p, 10)
	for si in GameData.SUPPLIERS.size():
		var s: Dictionary = GameData.SUPPLIERS[si]
		var b := Button.new()
		b.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		b.custom_minimum_size = Vector2(0, 84)
		var qstars := "★" if float(s["quality"]) < 0.8 else ("★★" if float(s["quality"]) < 1.3 else "★★★")
		var lead := int(gm.lead_minutes(si))
		var status := ""
		if gm.level < int(s["level"]):
			status = "\n🔒 ab Level %d" % int(s["level"])
		elif gm.blocked_suppliers.has(str(si)):
			status = "\n⛔ gerade geschlossen"
		b.text = "%s\nQualität %s · Preis ×%.2f\nLieferzeit ~%d min%s" % [s["name"], qstars, float(s["price_mult"]), lead, status]
		b.disabled = not gm.supplier_available(si)
		b.theme_type_variation = "SideActive" if si == sel_supplier else ""
		b.tooltip_text = String(s["desc"])
		b.pressed.connect(_sel_supplier.bind(si))
		sh.add_child(b)

	label(p, "Menge", "H3")
	var qh := hbox(p, 10)
	for bi in GameData.BULK_OPTIONS.size():
		var bo: Dictionary = GameData.BULK_OPTIONS[bi]
		var cost := gm.bulk_cost(sel_product, bi, sel_supplier)
		var unit := float(cost) / float(bo["quantity"])
		var txt := "%s · %d Stück\n%s  (%s/Stk)" % [bo["name"], int(bo["quantity"]), money(cost), _eur(unit)]
		if not gm.bulk_available(bi):
			txt = "%s · %d Stück\n🔒 %s" % [bo["name"], int(bo["quantity"]), ("Level %d" % int(bo["level"])) if gm.level < int(bo["level"]) else "Lagerhalle"]
		var b := button(qh, txt, _buy.bind(bi), "AccentButton" if bi == 0 else "", not gm.bulk_available(bi) or gm.money < cost)
		b.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		b.custom_minimum_size = Vector2(0, 58)

	var ex := card(p)
	var cb := CheckButton.new()
	cb.text = "Express-Lieferung: halbe Lieferzeit, +%s pro Bestellung" % money(GameData.EXPRESS_SURCHARGE)
	cb.button_pressed = gm.express_delivery
	cb.toggled.connect(func(on: bool): GameManager.set_express_delivery(on))
	ex.add_child(cb)
	if gm.upgrades.get("van", false):
		label(ex, "🚐 Eigener Lieferwagen: alle Lieferungen 25 % schneller", "Muted")

	if not gm.traveling_deliveries.is_empty():
		var tc := card(p, "Unterwegs")
		for d in gm.traveling_deliveries:
			kv(tc, "%d× %s" % [int(d["quantity"]), GameData.product(d["product"])["name"]], "Ankunft in " + gm.eta_text(d))

func _sel_product(i: int) -> void:
	sel_product = i
	rebuild()

func _sel_supplier(i: int) -> void:
	sel_supplier = i
	rebuild()

func _buy(bi: int) -> void:
	GameManager.buy_bulk(sel_product, bi, sel_supplier)
