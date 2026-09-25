extends PCApp
## Personal: Mitarbeiter einstellen/entlassen. Sie arbeiten automatisch in der Lagerhalle.

func build() -> void:
	var gm := GameManager
	var p := page()
	header(p, "👷 Personal", "Mitarbeiter übernehmen Arbeit automatisch – solange Nachschub da ist. Löhne werden jeden Abend abgerechnet.")
	var top := hbox(p, 12)
	stat(top, "Mitarbeiter", str(gm.staff.size()))
	stat(top, "Löhne pro Tag", money(gm.wages_per_day()), UITheme.BAD)
	stat(top, "Umsatz heute", money(int(gm.daily["revenue"])), UITheme.GOOD)
	label(p, "Einstellen", "H3")
	var grid := flow(p, 10)
	for r in GameData.STAFF_ROLES:
		var c := card(grid)
		(c.get_parent() as Control).custom_minimum_size = Vector2(390, 0)
		var ch := hbox(c, 12)
		swatch(ch, r["shirt"], Vector2(42, 42), r["icon"])
		var v := vbox(ch, 2)
		label(v, r["name"], "H3")
		var count := gm.staff_count(r["id"])
		label(v, "%s pro Tag · %d/%d besetzt" % [money(int(r["wage"])), count, int(r["max"])], "Muted")
		wrap_label(c, String(r["desc"]))
		button(c, "Einstellen", gm.hire.bind(String(r["id"])), "AccentButton", count >= int(r["max"]))
	if not gm.staff.is_empty():
		var lc := card(p, "Dein Team")
		for i in gm.staff.size():
			var s: Dictionary = gm.staff[i]
			var role := GameData.staff_role(s["role"])
			var sh := hbox(lc, 12)
			var nl := label(sh, "%s  %s" % [role["icon"], s["name"]], "H3")
			nl.custom_minimum_size = Vector2(170, 0)
			label(sh, role["name"], "Muted")
			spacer(sh)
			if s["role"] != "social":
				bar(sh, float(s["progress"]), 1.0, UITheme.TEAL, 6, 120)
			button(sh, "Entlassen", gm.fire.bind(i), "DangerButton")
	var tip := card(p)
	wrap_label(tip, "💡 Packer:innen brauchen Lagerbestand und Kartons, Versandkräfte verpackte Pakete, Lagerist:innen Kisten am Wareneingang. Fehlt etwas, warten sie.", "")
