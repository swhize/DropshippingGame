extends PCApp
## Ausbau & Einrichtung: Lagerhalle, Upgrades, Deko.

func build() -> void:
	var gm := GameManager
	var p := page()
	header(p, "🏗 Ausbau & Einrichtung", "Investiere in größere Räume, bessere Technik und ein bisschen Gemütlichkeit.")
	var top := hbox(p, 12)
	stat(top, "Standort", String(GameData.STAGE_NAMES[gm.location_stage]))
	stat(top, "Lagerplatz", "%d / %d" % [gm.stock_total(), gm.capacity()])
	stat(top, "Miete", money(gm.rent_per_day()) + "/Tag")
	stat(top, "Warteschlange", "%d Plätze" % gm.queue_capacity())
	label(p, "Upgrades", "H3")
	for u in GameData.UPGRADES:
		var id: String = u["id"]
		var st := gm.upgrade_state(id)
		var c := card(p, "", "CardHi" if st == "available" else "Card")
		var h := hbox(c, 12)
		swatch(h, Color(0.2, 0.24, 0.33), Vector2(46, 46), u["icon"])
		var v := vbox(h, 2)
		v.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		label(v, u["name"], "H3")
		wrap_label(v, String(u["desc"]))
		match st:
			"owned":
				label(h, "✓ Gekauft", "AccentLabel")
			"level":
				button(h, "🔒 Level %d" % int(u["level"]), func(): pass, "", true)
			"requires":
				button(h, "🔒 Braucht %s" % GameData.upgrade(String(u["requires"]))["name"], func(): pass, "", true)
			_:
				button(h, "Kaufen · %s" % money(int(u["cost"])), gm.buy_upgrade.bind(id), "AccentButton", gm.money < int(u["cost"]))
	label(p, "Einrichtung", "H3")
	var f := flow(p, 10)
	for d in GameData.DECOR:
		var c := card(f)
		(c.get_parent() as Control).custom_minimum_size = Vector2(290, 0)
		label(c, d["name"], "H3")
		if gm.decor_owned.get(d["id"], false):
			label(c, "✓ Aufgestellt", "AccentLabel")
		else:
			button(c, "Kaufen · %s" % money(int(d["cost"])), gm.buy_decor.bind(String(d["id"])), "", gm.money < int(d["cost"]))
