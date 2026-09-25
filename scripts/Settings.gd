extends Node
## Spieler-Einstellungen (Lautstärke, Maus, Sichtfeld, Vollbild) + Tastenbelegung.
## Wird als erstes Autoload geladen und in user://settings.cfg gespeichert.

signal changed

const PATH := "user://settings.cfg"

var master_volume: float = 0.85
var music_volume: float = 0.5
var sfx_volume: float = 0.8
var mouse_sensitivity: float = 1.0
var fov: float = 75.0
var fullscreen: bool = false
var head_bob: bool = true

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	_setup_input_map()
	load_settings()
	apply_display()

func _setup_input_map() -> void:
	_add_keys("move_forward", [KEY_W, KEY_UP])
	_add_keys("move_back", [KEY_S, KEY_DOWN])
	_add_keys("move_left", [KEY_A, KEY_LEFT])
	_add_keys("move_right", [KEY_D, KEY_RIGHT])
	_add_keys("interact", [KEY_E])
	_add_keys("drop", [KEY_G])
	_add_keys("jump", [KEY_SPACE])
	_add_keys("sprint", [KEY_SHIFT])
	_add_keys("crouch", [KEY_CTRL, KEY_C])
	_add_keys("help", [KEY_F1])
	_add_keys("open_pc", [KEY_TAB])

func _add_keys(action: String, keys: Array) -> void:
	if not InputMap.has_action(action):
		InputMap.add_action(action)
	for k in keys:
		var ev := InputEventKey.new()
		ev.physical_keycode = k
		InputMap.action_add_event(action, ev)

func load_settings() -> void:
	var cfg := ConfigFile.new()
	if cfg.load(PATH) != OK:
		return
	master_volume = float(cfg.get_value("audio", "master", master_volume))
	music_volume = float(cfg.get_value("audio", "music", music_volume))
	sfx_volume = float(cfg.get_value("audio", "sfx", sfx_volume))
	mouse_sensitivity = float(cfg.get_value("input", "mouse", mouse_sensitivity))
	fov = float(cfg.get_value("video", "fov", fov))
	fullscreen = bool(cfg.get_value("video", "fullscreen", fullscreen))
	head_bob = bool(cfg.get_value("video", "head_bob", head_bob))

func save_settings() -> void:
	var cfg := ConfigFile.new()
	cfg.set_value("audio", "master", master_volume)
	cfg.set_value("audio", "music", music_volume)
	cfg.set_value("audio", "sfx", sfx_volume)
	cfg.set_value("input", "mouse", mouse_sensitivity)
	cfg.set_value("video", "fov", fov)
	cfg.set_value("video", "fullscreen", fullscreen)
	cfg.set_value("video", "head_bob", head_bob)
	cfg.save(PATH)

## Setzt eine Einstellung, wendet sie sofort an und speichert.
func set_value(key: String, value) -> void:
	set(key, value)
	apply_audio()
	if key == "fullscreen":
		apply_display()
	save_settings()
	changed.emit()

func apply_audio() -> void:
	_set_bus("Master", master_volume)
	_set_bus("Music", music_volume)
	_set_bus("SFX", sfx_volume)

func _set_bus(bus_name: String, value: float) -> void:
	var idx := AudioServer.get_bus_index(bus_name)
	if idx < 0:
		return
	AudioServer.set_bus_volume_db(idx, linear_to_db(maxf(value, 0.0001)))
	AudioServer.set_bus_mute(idx, value <= 0.001)

func apply_display() -> void:
	if DisplayServer.get_name() == "headless":
		return
	if fullscreen:
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)
	elif DisplayServer.window_get_mode() == DisplayServer.WINDOW_MODE_FULLSCREEN:
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
