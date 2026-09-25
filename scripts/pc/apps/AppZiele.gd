extends PCApp
## Ziele & Lifestyle: Firmenlevel, Meilensteine mit Belohnung, Statussymbole.

func build() -> void:
	var gm := GameManager
	var p := page()
	header(p, "🏆 Ziele & Lifestyle", "Deine Meilensteine auf dem Weg zum Imperium – und was du dir gönnst.")
	var lc := card(p, "", "CardHi")
	var lh := hbox(lc)
	label(lh, "Firmenlevel %d" % gm.level, "H2", UITheme.ACCENT)
	spacer(lh)
	if gm.level < GameData.MAX_LEVEL:
		label(lh, "%d / %d XP" % [gm.xp, GameData.level_threshold(gm.level + 1)], "Muted")
	bar(lc, gm.level_progress(), 1.0, UITheme.ACCENT, 10)
	if gm.level < GameData.MAX_LEVEL:
		wrap_label(lc, "Nächstes Level: " + String(GameData.LEVEL_UNLOCKS.get(gm.level + 1, "")))
	else:
		wrap_label(lc, "Maximales Level erreicht. Legende.")
	var gc := card(p, "Ziele")
	for g in GameData.GOALS:
		var done: bool = gm.goals_done.get(g["id"], false)
		var gh := hbox(gc, 10)
		label(gh, "✅" if done else "⬜", "", Color(0, 0, 0, 0), 18)
		var gv := vbox(gh, 2)
		gv.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		label(gv, g["title"], "H3", UITheme.MUTED if done else Color(0, 0, 0, 0))
		label(gv, g["desc"], "Muted")
		if not done:
			bar(gv, minf(gm.goal_value(g) / maxf(float(g["target"]), 0.001), 1.0), 1.0, UITheme.TEAL, 6)
		label(gh, "+" + money(int(g["reward"])), "AccentLabel")
	label(p, "Lifestyle", "H3")
	wrap_label(p, "Statussymbole ohne Nutzen, aber mit Stil. Die meisten tauchen in der Welt auf.")
	var f := flow(p, 10)
	for l in GameData.LIFESTYLE:
		var c := card(f)
		(c.get_parent() as Control).custom_minimum_size = Vector2(290, 0)
		label(c, l["name"], "H3")
		wrap_label(c, String(l["desc"]))
		if gm.lifestyle_owned.get(l["id"], false):
			label(c, "✓ Gegönnt", "AccentLabel")
		else:
			button(c, "Kaufen · %s" % money(int(l["cost"])), gm.buy_lifestyle.bind(String(l["id"])), "AccentButton", gm.money < int(l["cost"]))
