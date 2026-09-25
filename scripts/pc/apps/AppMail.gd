extends PCApp
## Postfach: Nachrichten, Angebote, Ereignisse mit Entscheidungen.

static var selected_id: int = -1

func build() -> void:
	var p := page()
	header(p, "📧 Postfach", "Nachrichten, Angebote und Entscheidungen. Offene Entscheidungen verfallen um 20 Uhr.")
	var mails: Array = Events.mails
	if mails.is_empty():
		wrap_label(p, "Noch keine Nachrichten. Das ändert sich, sobald dein Shop läuft.", "")
		return
	if selected_id == -1 or Events.find_mail(selected_id).is_empty():
		selected_id = int(mails[0]["id"])
		for m in mails:
			if m["pending"]:
				selected_id = int(m["id"])
				break
	var h := hbox(p, 14)
	var list := vbox(h, 6)
	list.custom_minimum_size = Vector2(320, 0)
	var shown := 0
	for m in mails:
		shown += 1
		if shown > 14:
			break
		var b := Button.new()
		b.alignment = HORIZONTAL_ALIGNMENT_LEFT
		b.custom_minimum_size = Vector2(320, 54)
		var mark := "🔴 " if m["pending"] else ("● " if not m["read"] else "")
		b.text = "%s%s %s\n      %s · Tag %d" % [mark, m["icon"], m["title"], m["sender"], int(m["day"])]
		b.theme_type_variation = "SideActive" if int(m["id"]) == selected_id else ""
		b.pressed.connect(_select.bind(int(m["id"])))
		list.add_child(b)
	var detail := card(h)
	var m := Events.find_mail(selected_id)
	Events.mark_read(selected_id)
	label(detail, "%s  %s" % [m["icon"], m["title"]], "H2")
	label(detail, "Von: %s · Tag %d, %s Uhr" % [m["sender"], int(m["day"]), UITheme.clock(float(m["time"]))], "Muted")
	separator(detail)
	var body := wrap_label(detail, String(m["text"]), "")
	body.custom_minimum_size = Vector2(420, 0)
	if m["pending"]:
		label(detail, "Deine Entscheidung:", "H3")
		var choices: Array = m.get("choices", [])
		for i in choices.size():
			var c: Dictionary = choices[i]
			var cost := int(c.get("cost", 0))
			var txt := String(c["label"]) + ("  ·  %s" % money(cost) if cost > 0 else "")
			button(detail, txt, _choose.bind(i), "AccentButton" if i == 0 else "", cost > GameManager.money)
	elif String(m.get("result", "")) != "":
		var rc := card(detail, "Ergebnis", "CardHi")
		if m.has("chosen"):
			label(rc, "Du hast gewählt: " + String(m["chosen"]), "Muted")
		wrap_label(rc, String(m["result"]), "")

func _select(id: int) -> void:
	selected_id = id
	rebuild()

func _choose(i: int) -> void:
	Events.choose(selected_id, i)
	rebuild()
