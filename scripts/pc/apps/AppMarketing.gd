extends PCApp
## Marketing: Werbekampagnen (Flyer bis Influencer) + TikTok-Minispiel + Bekanntheit.

var tiktok: Control = null

func can_rebuild() -> bool:
	if tiktok != null and is_instance_valid(tiktok) and tiktok.is_busy():
		return false
	return true

func build() -> void:
	tiktok = null
	var gm := GameManager
	var p := page()
	header(p, "📣 Marketing", "Mehr Reichweite = mehr Bestellungen. Kampagnen wirken zeitlich begrenzt, die Bekanntheit deiner Marke bleibt teilweise.")
	var aw := card(p)
	var ah := hbox(aw)
	label(ah, "Bekanntheit deiner Marke", "H3")
	spacer(ah)
	label(ah, "%d %%" % int(gm.awareness / 1.5 * 100.0), "AccentLabel")
	bar(aw, gm.awareness, 1.5, UITheme.TEAL, 10)
	if not gm.boosts.is_empty():
		var bc := card(p, "Gerade aktiv", "CardHi")
		for b in gm.boosts:
			kv(bc, "⚡ " + String(b["name"]), "×%.1f · noch %d min" % [float(b["mult"]), int(maxf(0.0, float(b["ends_at"]) - gm.bclock()))], UITheme.TEAL)

	var h := hbox(p, 16)
	var left := vbox(h, 10)
	left.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	label(left, "Werbekampagnen", "H3")
	var running := not gm.active_boost("ad").is_empty()
	for i in GameData.AD_TIERS.size():
		var t: Dictionary = GameData.AD_TIERS[i]
		var c := card(left)
		var ch := hbox(c, 12)
		swatch(ch, Color(0.2, 0.24, 0.34), Vector2(42, 42), t["icon"])
		var v := vbox(ch, 2)
		v.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		label(v, t["name"], "H3")
		label(v, "×%.1f Nachfrage · %d min · +Bekanntheit" % [float(t["mult"]), int(t["minutes"])], "Muted")
		var locked := gm.level < int(t["level"])
		var txt := "🔒 Level %d" % int(t["level"]) if locked else ("Läuft …" if running else "Starten · %s" % money(int(t["cost"])))
		button(ch, txt, gm.start_ad_campaign.bind(i), "" if locked or running else "AccentButton", locked or running or gm.money < int(t["cost"]))

	var right := vbox(h, 10)
	right.custom_minimum_size = Vector2(270, 0)
	label(right, "TikTok", "H3")
	if gm.level < GameData.TIKTOK_LEVEL:
		var lc := card(right)
		wrap_label(lc, "🔒 TikTok schaltet sich ab Firmenlevel %d frei." % GameData.TIKTOK_LEVEL, "")
	elif not gm.tiktok_available():
		var lc2 := card(right)
		if not gm.active_boost("tiktok").is_empty():
			wrap_label(lc2, "🔥 Dein Trend läuft gerade! Genieß die Bestellungen.", "")
		else:
			wrap_label(lc2, "Nächstes Video in %d Minuten möglich – der Algorithmus braucht Pause." % int(ceil(gm.tiktok_ready_at - gm.bclock())), "")
	else:
		tiktok = preload("res://scripts/pc/TikTokGame.gd").new()
		right.add_child(tiktok)
		tiktok.connect("posted", _on_posted)

func _on_posted(score: float) -> void:
	GameManager.trigger_tiktok(score)
