extends Node3D
## Spielsitzung: baut Welt, Spieler und alle Oberflächen und verbindet Story, Tagesablauf,
## Ereignisse und Menüs. Der Startmodus kommt aus GameManager.start_mode (vom Hauptmenü).

var world: WorldBuilder
var player: CharacterBody3D
var hud: CanvasLayer
var pc: CanvasLayer
var dialogue: CanvasLayer
var modal: CanvasLayer
var big_modal: CanvasLayer
var pause_menu: CanvasLayer
var fader: CanvasLayer
var _light_acc: float = 1.0

func _ready() -> void:
	GameManager.clear_locks()
	_apply_start_mode()
	GameManager.in_game = true
	world = WorldBuilder.new()
	world.name = "World"
	add_child(world)
	player = CharacterBody3D.new()
	player.set_script(load("res://scripts/Player.gd"))
	add_child(player)
	world.build(false)
	world.refresh_staff()
	_place_player(true)
	_restore_world_items()
	_setup_ui()
	_connect_signals()
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
	world.set_time_of_day(GameManager.time_minutes / 60.0)
	Audio.play_music("diner" if GameManager.story_stage == "diner" else "work")
	fader.fade_in(1.0)
	if GameManager.story_stage == "diner":
		GameManager.notify("Kalles Imbiss, 17:30 Uhr. Deine Schicht beginnt.", "info")
	if not OS.get_cmdline_user_args().is_empty():
		var h := Node.new()
		h.set_script(load("res://scripts/DebugHarness.gd"))
		h.set("mode", "game")
		add_child(h)

func _apply_start_mode() -> void:
	match GameManager.start_mode:
		"continue":
			if not GameManager.load_game():
				GameManager.new_game("tutorial")
		"intro":
			GameManager.new_game("intro")
		"tutorial":
			GameManager.new_game("tutorial")
		"skip":
			GameManager.new_game("skip")
		_:
			GameManager.new_game("skip")
	GameManager.start_mode = ""

func _place_player(use_saved: bool) -> void:
	var ps: Dictionary = GameManager.player_state
	if use_saved and ps.has("pos") and GameManager.story_stage == "business":
		player.global_position = ps["pos"]
		player.rotation.y = float(ps.get("rot", 0.0))
		var hk := int(ps.get("held_kind", 0))
		if hk != GameManager.ItemKind.NONE:
			player.hold(hk, ps.get("held", {}))
		GameManager.player_state = {}
	else:
		player.global_position = world.spawn_point()
		player.rotation.y = 0.0
	player.velocity = Vector3.ZERO

func _restore_world_items() -> void:
	for w in GameManager.world_items:
		var pos = w.get("pos", Vector3.ZERO)
		if pos is Vector3:
			player.spawn_dropped(int(w["kind"]), w["data"], pos, float(w.get("rot", 0.0)))
	GameManager.world_items = []

func _setup_ui() -> void:
	hud = CanvasLayer.new()
	hud.set_script(load("res://scripts/ui/HUD.gd"))
	hud.set("player", player)
	add_child(hud)
	pc = CanvasLayer.new()
	pc.set_script(load("res://scripts/pc/PCScreen.gd"))
	add_child(pc)
	dialogue = CanvasLayer.new()
	dialogue.set_script(load("res://scripts/ui/Dialogue.gd"))
	add_child(dialogue)
	modal = CanvasLayer.new()
	modal.set_script(load("res://scripts/ui/Modal.gd"))
	modal.layer = 30
	add_child(modal)
	big_modal = CanvasLayer.new()
	big_modal.set_script(load("res://scripts/ui/Modal.gd"))
	big_modal.layer = 40
	add_child(big_modal)
	pause_menu = CanvasLayer.new()
	pause_menu.set_script(load("res://scripts/ui/PauseMenu.gd"))
	add_child(pause_menu)
	fader = CanvasLayer.new()
	fader.set_script(load("res://scripts/ui/Fader.gd"))
	add_child(fader)

func _connect_signals() -> void:
	var gm := GameManager
	gm.dialogue_requested.connect(_on_dialogue)
	gm.end_day_requested.connect(_confirm_end_day)
	gm.day_ended.connect(_show_summary)
	gm.game_over.connect(_show_game_over)
	gm.game_won.connect(_show_ending)
	gm.location_changed.connect(_on_location_changed)
	gm.world_changed.connect(func(): world.refresh_world(true))
	gm.staff_changed.connect(world.refresh_staff)
	gm.conveyor_changed.connect(world.sync_conveyor)
	gm.delivery_incoming.connect(func(_p): world.send_van())
	gm.package_shipped.connect(_on_shipped)
	gm.input_lock_changed.connect(_on_lock_changed)
	Events.choice_event.connect(_show_event_choice)
	pause_menu.connect("main_menu_requested", _to_menu)
	pause_menu.connect("quit_requested", _quit)
	pause_menu.connect("help_requested", _show_help)

# ---- Eingabe ---------------------------------------------------------------------------------
func _unhandled_input(event: InputEvent) -> void:
	var gm := GameManager
	if event.is_action_pressed("ui_cancel") and not gm.is_input_locked():
		get_viewport().set_input_as_handled()
		pause_menu.open()
	elif event.is_action_pressed("help") and not gm.is_input_locked():
		_show_help()
	elif event.is_action_pressed("open_pc") and not gm.is_input_locked():
		if gm.story_stage == "business":
			get_viewport().set_input_as_handled()
			gm.open_pc()
		else:
			gm.notify("Dein Handy hat gerade keinen Empfang. Kalle guckt.", "info")

func _on_lock_changed(locked: bool) -> void:
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE if locked else Input.MOUSE_MODE_CAPTURED

func _process(delta: float) -> void:
	_light_acc += delta
	if _light_acc >= 0.2:
		_light_acc = 0.0
		world.set_time_of_day(GameManager.time_minutes / 60.0)
	if player.global_position.y < -20.0:
		player.global_position = world.spawn_point()
		player.velocity = Vector3.ZERO

# ---- Welt-Ereignisse -------------------------------------------------------------------------
func _on_location_changed(_stage: int) -> void:
	world.rebuild_stations()
	world.refresh_world(true)
	world.refresh_staff()
	Events.add_mail("Immobilien Schulze", "Willkommen in der Lagerhalle!", "Die Schlüssel liegen unter der Fußmatte. Das Tor ist offen. Viel Erfolg mit dem Laden! PS: Das Förderband gibt's gegen Aufpreis.", "🏭")

func _on_shipped(_pkg: Dictionary, reward: int, where: Vector3) -> void:
	if where != Vector3.ZERO:
		world.spawn_float_text(where, "+" + UITheme.money(reward), UITheme.GOOD)

# ---- Story: Kalle ----------------------------------------------------------------------------
func _on_dialogue(id: String) -> void:
	if id != "kalle":
		return
	var gm := GameManager
	if gm.story_stage == "diner":
		match gm.intro_step:
			0:
				dialogue.start([
					{"speaker": "Kalle", "text": "Da bist du ja endlich! Fünf Minuten zu spät. Wie immer."},
					{"speaker": "Kalle", "text": "Tisch 2, 3 und 4 warten auf ihr Essen. Die Teller stehen da vorne an der Durchreiche."},
					{"speaker": "Kalle", "text": "Zack zack! Und Handy weg, sonst zieh ich's dir vom Lohn ab."},
				], [], func(_c): GameManager.start_shift())
			1:
				dialogue.start([{"speaker": "Kalle", "text": "Was stehst du hier rum?! Die Teller werden kalt!"}])
			_:
				dialogue.start([
					{"speaker": "Kalle", "text": "Na endlich. Hat ja ewig gedauert."},
					{"speaker": "Kalle", "text": "Und glaub nicht, ich hätte nicht gesehen, wie du vorhin wieder auf dein Handy gestarrt hast."},
					{"speaker": "Du", "text": "(Dieses Video ... 'Mit 19 Millionär – dank Dropshipping'. Und die Garage gegenüber steht leer ...)"},
				], ["Ich kündige.", "Ich mach mein eigenes Ding. Online-Shop."], _on_quit_choice)
	else:
		var text: String = GameData.KALLE_IDLE[randi() % GameData.KALLE_IDLE.size()]
		if gm.ending_seen:
			text = "Hunderttausend Umsatz?! Kleiner ... hast du vielleicht 'nen Job für mich?"
		elif gm.location_stage >= 1 and randf() < 0.5:
			text = "Ich hab gehört, du hast jetzt 'ne ganze Lagerhalle. Respekt. Sag's aber keinem, dass ich das gesagt hab."
		dialogue.start([{"speaker": "Kalle", "text": text}])

func _on_quit_choice(choice: int) -> void:
	var reply := "Du ... WAS? Nach allem, was ich für dich getan hab? Pff. Na dann geh doch!"
	if choice == 1:
		reply = "Online-Shop? DU? Hahaha! ... Na gut. Viel Glück mit deinem Internet-Kram."
	dialogue.start([
		{"speaker": "Kalle", "text": reply},
		{"speaker": "Kalle", "text": "Hier. 150 Euro, dein letzter Lohn. Mach was draus – und komm nicht angekrochen."},
	], [], func(_c): fader.transition("Am nächsten Morgen ...", _do_finish_intro, 1.6))

func _do_finish_intro() -> void:
	GameManager.finish_intro()
	world.rebuild_stations()
	world.refresh_world(false)
	if player.is_empty() == false:
		player.clear_hands()
	_place_player(false)
	world.set_time_of_day(GameManager.time_minutes / 60.0)
	Audio.play_music("work")
	GameManager.notify("Tag 1. Deine Garage. Dein Business. Los geht's!", "good")
	GameManager.save_game(true)

# ---- Tagesablauf -----------------------------------------------------------------------------
func _confirm_end_day() -> void:
	if not GameManager.is_open():
		return
	var body: VBoxContainer = modal.open("Feierabend machen?", "Es ist %s Uhr." % UITheme.clock(GameManager.time_minutes), 470,
		[["Ja, Tag beenden", func(): GameManager.request_end_day(), "AccentButton"], ["Weiterarbeiten", Callable()]], true, "confirm")
	modal.text(body, "Offene Bestellungen bleiben bis morgen liegen. Miete und Löhne werden abgerechnet.", "Muted", 430)

func _show_summary(s: Dictionary) -> void:
	if GameManager.pc_open:
		GameManager.close_pc()
	var title := "Feierabend – Tag %d" % int(s["day"])
	var body: VBoxContainer = big_modal.open(title, "Zeit für die Abrechnung.", 540, [["Nächster Tag  ▶", _next_day, "AccentButton"]], true, "summary")
	Audio.play("notify")
	var m: Node = big_modal
	m.kv_row(body, "Verschickte Pakete", str(int(s["shipped"])))
	m.kv_row(body, "Verlorene Bestellungen", str(int(s["lost"])), UITheme.BAD if int(s["lost"]) > 0 else Color(0, 0, 0, 0))
	body.add_child(HSeparator.new())
	m.kv_row(body, "Umsatz", UITheme.money(int(s["revenue"])), UITheme.GOOD)
	if int(s["income_other"]) != 0:
		m.kv_row(body, "Sonstige Einnahmen", UITheme.money(int(s["income_other"])), UITheme.GOOD)
	for pair in [["Wareneinkauf", "purchases"], ["Verpackung", "packaging"], ["Marketing", "marketing"], ["Sonstiges", "other"],
			["Miete", "rent"], ["Löhne", "wages"], ["Strom Förderband", "upkeep"]]:
		if int(s[pair[1]]) != 0:
			m.kv_row(body, pair[0], UITheme.money(-int(s[pair[1]])), UITheme.BAD)
	body.add_child(HSeparator.new())
	var profit := int(s["profit"])
	m.kv_row(body, "Tagesergebnis (Cashflow)", UITheme.money(profit), UITheme.GOOD if profit >= 0 else UITheme.BAD)
	if int(s["trading"]) != 0:
		m.kv_row(body, "Trading (Käufe/Verkäufe)", UITheme.money(int(s["trading"])))
	m.kv_row(body, "Kontostand danach", UITheme.money(int(s["money_after"])), UITheme.BAD if int(s["money_after"]) < 0 else UITheme.TEXT)
	var dr := float(s["rep_end"]) - float(s["rep_start"])
	m.kv_row(body, "Bewertung", "%s %s (%s)" % [UITheme.stars(float(s["rep_end"])), UITheme.rating_text(float(s["rep_end"])), ("%+.2f" % dr).replace(".", ",")])
	m.kv_row(body, "Erfahrung", "+%d XP" % int(s["xp_gained"]), UITheme.ACCENT)
	if bool(s["bankrupt"]):
		modal.text(body, "⚠ Dein Konto fällt unter %s – das ist die Insolvenz!" % UITheme.money(GameData.BANKRUPT_LIMIT), "AccentLabel", 500)
	elif int(s["money_after"]) < 0:
		modal.text(body, "⚠ Du bist im Minus. Ab %s ist Schluss – verkauf mehr oder spar Kosten!" % UITheme.money(GameData.BANKRUPT_LIMIT), "AccentLabel", 500)

func _next_day() -> void:
	fader.transition("Tag %d" % (GameManager.day + 1), _do_next_day, 0.9)

func _do_next_day() -> void:
	GameManager.start_next_day()
	if GameManager.day_over:
		return
	world.refresh_world(false)
	world.rebuild_stations()
	_place_player(false)
	world.set_time_of_day(GameManager.time_minutes / 60.0)

func _show_game_over(_reason: String) -> void:
	var body: VBoxContainer = big_modal.open("Pleite!", "Dein Konto ist tief im Minus. Die Bank zieht den Stecker.", 520,
		[["Letzten Spielstand laden", _reload_save, "AccentButton"], ["Hauptmenü", _to_menu]], true, "gameover")
	Audio.play("bad")
	big_modal.text(body, "Du hast %d Tage durchgehalten, %d Pakete verschickt und %s umgesetzt. Nicht schlecht – aber das Geld ist weg." % [
		GameManager.day, GameManager.total_shipped, UITheme.money(GameManager.total_earned)], "", 480)

func _show_ending() -> void:
	var body: VBoxContainer = big_modal.open("Du hast es geschafft! 🏆", "100.000 € Umsatz. Vom Imbiss zum Imperium.", 560,
		[["Weiterspielen", Callable(), "AccentButton"], ["Credits", _show_credits]], true, "ending")
	Audio.play("levelup")
	big_modal.text(body, "Vor %d Tagen hast du noch Teller bei Kalle getragen. Heute gehört dir eine Marke mit %s Bewertung, %d verschickten Paketen und einem Team, das für dich arbeitet." % [
		GameManager.day, UITheme.rating_text(GameManager.reputation), GameManager.total_shipped], "", 520)
	big_modal.text(body, "Das Spiel ist hier nicht zu Ende – bau dein Imperium weiter aus, so lange du willst.", "Muted", 520)

func _show_credits() -> void:
	var body: VBoxContainer = modal.open("Credits", "Dropshipping Simulator – Vom Imbiss zum Imperium", 520, [["Schließen", Callable(), "AccentButton"]], true, "credits")
	modal.text(body, "Idee & Game Design: du\nProgrammierung, 3D-Welt, Shader, Sounds & Musik: prozedural per Code erzeugt mit Claude\nEngine: Godot 4.7\n\nKein einziges Asset wurde importiert – jedes Modell, jede Textur, jeder Ton entsteht beim Start aus Code.", "", 480)

func _show_help() -> void:
	var body: VBoxContainer = modal.open("Steuerung & Spielablauf", "", 660, [["Verstanden", Callable(), "AccentButton"]], true, "help")
	var r := RichTextLabel.new()
	r.bbcode_enabled = true
	r.fit_content = true
	r.custom_minimum_size = Vector2(620, 0)
	r.text = "[b]Steuerung[/b]\nWASD laufen · Shift sprinten · Strg ducken · Leertaste springen\nMaus umsehen · [color=#ffbd42]E[/color] mit dem Anvisierten interagieren · [color=#ffbd42]G[/color] Gegenstand ablegen\n[color=#ffbd42]Tab[/color] Laptop/Handy · Esc Pause · F1 diese Hilfe\n\n[b]Der Kreislauf[/b]\n1. Im Laptop unter [b]Einkauf[/b] Ware bestellen\n2. Kiste am [b]Wareneingang[/b] holen und ins passende [b]Regal[/b] räumen\n3. Produkt im [b]Webshop[/b] online stellen – Bestellungen erscheinen oben links\n4. Artikel aus dem Regal nehmen, am [b]Packtisch[/b] verpacken\n5. Am [b]Labeldrucker[/b] ein Versandlabel drucken\n6. Paket zur [b]Versand-Abgabe[/b] bringen – Geld kassieren\n\n[b]Wachsen[/b]\nGute Preise, schnelle Lieferung und Qualität bringen gute Bewertungen und mehr Kunden. Marketing erhöht die Reichweite. Mit Erfahrung steigt dein Firmenlevel und schaltet Produkte, Lagerhalle, Personal und mehr frei. Um 20 Uhr ist Feierabend – dann werden Miete und Löhne fällig."
	body.add_child(r)

# ---- Ereignisse ------------------------------------------------------------------------------
func _show_event_choice(mail: Dictionary) -> void:
	if mail.is_empty() or not mail.get("pending", false):
		return
	if modal.is_open or big_modal.is_open or dialogue.active or pause_menu.is_open:
		return
	var buttons: Array = []
	var choices: Array = mail.get("choices", [])
	for i in choices.size():
		var c: Dictionary = choices[i]
		var cost := int(c.get("cost", 0))
		var txt := String(c["label"]) + ("  ·  %s" % UITheme.money(cost) if cost > 0 else "")
		buttons.append([txt, _decide.bind(int(mail["id"]), i), "AccentButton" if i == 0 else ""])
	buttons.append(["Später (Postfach)", Callable(), "GhostButton"])
	var body: VBoxContainer = modal.open("%s  %s" % [mail["icon"], mail["title"]], "Von: " + String(mail["sender"]), 640, buttons, true, "event")
	modal.text(body, String(mail["text"]), "", 600)

func _decide(mail_id: int, choice: int) -> void:
	var res := Events.choose(mail_id, choice)
	if res == "":
		call_deferred("_show_event_choice", Events.find_mail(mail_id))
		return
	var body: VBoxContainer = modal.open("Ergebnis", "", 520, [["OK", Callable(), "AccentButton"]], true, "event_result")
	modal.text(body, res, "", 480)

# ---- Menü / Beenden --------------------------------------------------------------------------
func _to_menu() -> void:
	if GameManager.story_stage == "business" and not GameManager.day_over:
		GameManager.save_game(true)
	GameManager.in_game = false
	GameManager.clear_locks()
	fader.fade_out(func(): get_tree().change_scene_to_file("res://scenes/MainMenu.tscn"))

func _reload_save() -> void:
	GameManager.in_game = false
	GameManager.clear_locks()
	GameManager.start_mode = "continue"
	get_tree().reload_current_scene()

func _quit() -> void:
	if GameManager.story_stage == "business" and not GameManager.day_over:
		GameManager.save_game(true)
	get_tree().quit()
