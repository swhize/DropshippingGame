extends PCApp
## Branding: Markenname, Logo, Farbe - mit Live-Vorschau des Kartons.

var name_edit: LineEdit = null

func can_rebuild() -> bool:
	return name_edit == null or not is_instance_valid(name_edit) or not name_edit.has_focus()

func build() -> void:
	name_edit = null
	var gm := GameManager
	var p := page()
	header(p, "🏷 Branding", "Deine Marke: Name, Logo und Farbe. Erscheint auf neuen Kartons, im Webshop und groß an deinem Gebäude.")
	var h := hbox(p, 18)
	var left := vbox(h, 10)
	left.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	var nc := card(left, "Markenname")
	var nh := hbox(nc)
	name_edit = LineEdit.new()
	name_edit.text = gm.brand_name
	name_edit.max_length = 22
	name_edit.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	name_edit.text_submitted.connect(func(_t: String): _save_name())
	nh.add_child(name_edit)
	button(nh, "Speichern", _save_name, "AccentButton")
	var lc := card(left, "Logo")
	var lf := flow(lc, 8)
	for i in GameData.LOGO_NAMES.size():
		var box := vbox(lf, 4)
		var holder := CenterContainer.new()
		holder.custom_minimum_size = Vector2(78, 50)
		box.add_child(holder)
		var icon: Control = preload("res://scripts/pc/BrandPreview.gd").new()
		icon.setup(i, gm.brand_color, "", true)
		holder.add_child(icon)
		var b := button(box, GameData.LOGO_NAMES[i], gm.set_brand_logo.bind(i), "SideActive" if i == gm.brand_logo_index else "")
		b.custom_minimum_size = Vector2(78, 32)
	var cc := card(left, "Farbe")
	var cf := flow(cc, 8)
	for col in GameData.BRAND_PALETTE:
		var b := Button.new()
		b.custom_minimum_size = Vector2(40, 40)
		var selected: bool = (col as Color).is_equal_approx(gm.brand_color)
		var st := UITheme.style(col, 8, Color(1, 1, 1), 3 if selected else 0, 0)
		for s in ["normal", "hover", "pressed", "focus"]:
			b.add_theme_stylebox_override(s, st)
		b.pressed.connect(func():
			Audio.play("click")
			GameManager.set_brand_color(col))
		cf.add_child(b)
	var right := vbox(h, 10)
	label(right, "Vorschau", "H3")
	var prev: Control = preload("res://scripts/pc/BrandPreview.gd").new()
	prev.setup(gm.brand_logo_index, gm.brand_color, gm.brand_name)
	right.add_child(prev)
	wrap_label(right, "Tipp: Nur neu gekaufte Kartons (App 'Verpackung') bekommen dieses Design.")

func _save_name() -> void:
	if name_edit == null:
		return
	GameManager.set_brand_name(name_edit.text)
	name_edit.release_focus()
	GameManager.notify("Marke gespeichert: " + GameManager.brand_name, "good")
