extends Node
## Prozedurale Audio-Engine: erzeugt ALLE Soundeffekte und Musikstücke per Code
## (keine Audiodateien im Projekt). Läuft in einem Hintergrund-Thread und cached das
## Ergebnis nach dem ersten Start unter user://audio_cache, damit spätere Starts sofort klingen.

const RATE := 22050
const CACHE_DIR := "user://audio_cache"
const CACHE_VERSION := 4
const TABLE_SIZE := 2048

const SFX_NAMES := ["click", "hover", "pickup", "drop", "place", "cash", "notify", "order", "error",
	"tape", "printer", "levelup", "step", "fold", "truck", "door", "whoosh", "plate", "bad"]
const MUSIC_NAMES := ["menu", "work", "diner"]

var _sfx: Dictionary = {}
var _music: Dictionary = {}
var _players: Array[AudioStreamPlayer] = []
var _music_players: Array[AudioStreamPlayer] = []
var _active_music: int = 0
var _current_track: String = ""
var _wanted_track: String = ""
var _thread: Thread = null
var _lowpass: AudioEffectLowPassFilter = null
var _mutex := Mutex.new()
var _pending: Array = []
var _abort: bool = false

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	_setup_buses()
	for i in 14:
		var p := AudioStreamPlayer.new()
		p.bus = "SFX"
		add_child(p)
		_players.append(p)
	for i in 2:
		var m := AudioStreamPlayer.new()
		m.bus = "Music"
		m.volume_db = -80.0
		add_child(m)
		_music_players.append(m)
	Settings.apply_audio()
	if DisplayServer.get_name() == "headless":
		return  # in Testläufen keine Klangerzeugung
	_thread = Thread.new()
	_thread.start(_generate_all)

func _exit_tree() -> void:
	_abort = true
	if _thread != null and _thread.is_started():
		_thread.wait_to_finish()

## Fertig erzeugte Klänge aus dem Hintergrund-Thread übernehmen (bewusst ohne
## call_deferred aus dem Thread - das kann beim Beenden zu einem Deadlock führen).
func _process(_delta: float) -> void:
	if _pending.is_empty():
		return
	_mutex.lock()
	var items := _pending.duplicate()
	_pending.clear()
	_mutex.unlock()
	for it in items:
		_register(it[0], it[1], it[2])

func _setup_buses() -> void:
	for bus_name in ["Music", "SFX"]:
		if AudioServer.get_bus_index(bus_name) == -1:
			AudioServer.add_bus()
			var idx := AudioServer.bus_count - 1
			AudioServer.set_bus_name(idx, bus_name)
			AudioServer.set_bus_send(idx, "Master")
	var music_idx := AudioServer.get_bus_index("Music")
	_lowpass = AudioEffectLowPassFilter.new()
	_lowpass.cutoff_hz = 20000.0
	AudioServer.add_bus_effect(music_idx, _lowpass)

# ---- Öffentliche API ---------------------------------------------------------------------
func play(sfx_name: String, pitch_var: float = 0.06, volume_db: float = 0.0) -> void:
	if not _sfx.has(sfx_name):
		return
	var p := _free_player()
	if p == null:
		return
	p.stream = _sfx[sfx_name]
	p.pitch_scale = 1.0 + randf_range(-pitch_var, pitch_var)
	p.volume_db = volume_db
	p.play()

func play_music(track: String) -> void:
	_wanted_track = track
	if _music.has(track):
		_crossfade_to(track)

func stop_music() -> void:
	_wanted_track = ""
	_current_track = ""
	for m in _music_players:
		var tw := create_tween()
		tw.tween_property(m, "volume_db", -60.0, 1.0)
		tw.tween_callback(m.stop)

## Dämpft die Musik (z.B. im Pausemenü), klingt wie "durch die Wand".
func set_muffled(on: bool) -> void:
	if _lowpass == null:
		return
	var tw := create_tween()
	tw.tween_property(_lowpass, "cutoff_hz", 750.0 if on else 20000.0, 0.4)

func is_ready() -> bool:
	return _sfx.size() >= SFX_NAMES.size()

func _free_player() -> AudioStreamPlayer:
	for p in _players:
		if not p.playing:
			return p
	return _players[0]

func _crossfade_to(track: String) -> void:
	if track == _current_track:
		return
	_current_track = track
	var old_player := _music_players[_active_music]
	_active_music = 1 - _active_music
	var new_player := _music_players[_active_music]
	new_player.stream = _music[track]
	new_player.volume_db = -40.0
	new_player.play()
	var tw := create_tween().set_parallel(true)
	tw.tween_property(new_player, "volume_db", 0.0, 2.0)
	tw.tween_property(old_player, "volume_db", -60.0, 2.0)
	tw.chain().tween_callback(old_player.stop)

# ---- Erzeugung (Hintergrund-Thread) -----------------------------------------------------
func _generate_all() -> void:
	for n in SFX_NAMES:
		if _abort:
			return
		var bytes := _cached_or_generate(n, false)
		_mutex.lock()
		_pending.append([n, bytes, false])
		_mutex.unlock()
	for t in MUSIC_NAMES:
		if _abort:
			return
		var mbytes := _cached_or_generate(t, true)
		_mutex.lock()
		_pending.append([t, mbytes, true])
		_mutex.unlock()

func _cached_or_generate(sound_name: String, is_music: bool) -> PackedByteArray:
	var path := "%s/%s_v%d.pcm" % [CACHE_DIR, sound_name, CACHE_VERSION]
	if FileAccess.file_exists(path):
		var f := FileAccess.open(path, FileAccess.READ)
		if f:
			var cached := f.get_buffer(f.get_length())
			f.close()
			if cached.size() > 0:
				return cached
	var bytes := render(sound_name, is_music)
	DirAccess.make_dir_recursive_absolute(CACHE_DIR)
	var out := FileAccess.open(path, FileAccess.WRITE)
	if out:
		out.store_buffer(bytes)
		out.close()
	return bytes

## Rendert einen Klang als 16-Bit-PCM (öffentlich, damit Tests die Synthese prüfen können).
func render(sound_name: String, is_music: bool) -> PackedByteArray:
	if is_music:
		var buf := _render_music(sound_name)
		return _to_pcm16(buf, 0.82 / maxf(_peak(buf), 0.0001))
	var sfx := _render_sfx(sound_name)
	return _to_pcm16(sfx, 1.0)

func _register(sound_name: String, bytes: PackedByteArray, is_music: bool) -> void:
	var s := AudioStreamWAV.new()
	s.format = AudioStreamWAV.FORMAT_16_BITS
	s.mix_rate = RATE
	s.stereo = false
	s.data = bytes
	if is_music:
		s.loop_mode = AudioStreamWAV.LOOP_FORWARD
		s.loop_begin = 0
		s.loop_end = bytes.size() / 2
		_music[sound_name] = s
		if _wanted_track == sound_name:
			_crossfade_to(sound_name)
	else:
		_sfx[sound_name] = s

# ---- Hilfen --------------------------------------------------------------------------------
func _buf(seconds: float) -> PackedFloat32Array:
	var b := PackedFloat32Array()
	b.resize(maxi(1, int(seconds * RATE)))
	b.fill(0.0)
	return b

func _to_pcm16(buf: PackedFloat32Array, gain: float) -> PackedByteArray:
	var bytes := PackedByteArray()
	bytes.resize(buf.size() * 2)
	for i in buf.size():
		bytes.encode_s16(i * 2, int(clampf(buf[i] * gain, -1.0, 1.0) * 32767.0))
	return bytes

func _peak(buf: PackedFloat32Array) -> float:
	var p := 0.0
	for v in buf:
		p = maxf(p, absf(v))
	return p

func _midi(m: float) -> float:
	return 440.0 * pow(2.0, (m - 69.0) / 12.0)

func _table(harmonics: Array) -> PackedFloat32Array:
	var t := PackedFloat32Array()
	t.resize(TABLE_SIZE)
	var norm := 0.0
	for h in harmonics:
		norm += float(h)
	for i in TABLE_SIZE:
		var ph := TAU * float(i) / TABLE_SIZE
		var v := 0.0
		for k in harmonics.size():
			v += float(harmonics[k]) * sin(ph * (k + 1))
		t[i] = v / norm
	return t

func _tone(table: PackedFloat32Array, midi: float, seconds: float, attack: float, decay: float, wobble: float) -> PackedFloat32Array:
	var n := maxi(1, int(seconds * RATE))
	var b := PackedFloat32Array()
	b.resize(n)
	var inc := _midi(midi) * TABLE_SIZE / RATE
	var phase := 0.0
	var inv_rate := 1.0 / RATE
	for i in n:
		var t := i * inv_rate
		var env := minf(t / attack, 1.0) * exp(-t * decay)
		phase += inc * (1.0 + wobble * sin(2.2 * t))
		if phase >= TABLE_SIZE:
			phase -= TABLE_SIZE
		b[i] = table[int(phase)] * env
	var fade := mini(n, int(0.04 * RATE))
	for i in fade:
		b[n - 1 - i] *= float(i) / fade
	return b

func _mix(out: PackedFloat32Array, src: PackedFloat32Array, start: int, gain: float) -> void:
	var total := out.size()
	for i in src.size():
		var idx := (start + i) % total
		out[idx] += src[i] * gain

func _noise_buf(seconds: float, rng: RandomNumberGenerator) -> PackedFloat32Array:
	var b := _buf(seconds)
	for i in b.size():
		b[i] = rng.randf_range(-1.0, 1.0)
	return b

func _lowpass_buf(b: PackedFloat32Array, cutoff: float) -> void:
	var a := 1.0 - exp(-TAU * cutoff / RATE)
	var y := 0.0
	for i in b.size():
		y += a * (b[i] - y)
		b[i] = y

# ---- Soundeffekte --------------------------------------------------------------------------
func _render_sfx(sound_name: String) -> PackedFloat32Array:
	var rng := RandomNumberGenerator.new()
	rng.seed = hash(sound_name)
	var inv := 1.0 / RATE
	var b: PackedFloat32Array
	match sound_name:
		"click":
			b = _buf(0.05)
			for i in b.size():
				var t := i * inv
				b[i] = sin(TAU * 1900.0 * t) * exp(-t * 90.0) * 0.45
		"hover":
			b = _buf(0.03)
			for i in b.size():
				var t := i * inv
				b[i] = sin(TAU * 2700.0 * t) * exp(-t * 130.0) * 0.15
		"pickup", "place", "drop", "step":
			var dur := {"pickup": 0.2, "place": 0.16, "drop": 0.35, "step": 0.1}[sound_name] as float
			var f0 := {"pickup": 180.0, "place": 150.0, "drop": 110.0, "step": 90.0}[sound_name] as float
			var dec := {"pickup": 22.0, "place": 28.0, "drop": 12.0, "step": 45.0}[sound_name] as float
			var noise_amt := {"pickup": 0.25, "place": 0.15, "drop": 0.4, "step": 0.55}[sound_name] as float
			b = _buf(dur)
			var nz := _noise_buf(dur, rng)
			_lowpass_buf(nz, 1400.0 if sound_name != "step" else 900.0)
			var phase := 0.0
			for i in b.size():
				var t := i * inv
				var f := f0 * exp(-t * 5.0) + 40.0
				phase += TAU * f * inv
				b[i] = sin(phase) * exp(-t * dec) * 0.7 + nz[i] * exp(-t * dec * 2.0) * noise_amt
		"cash":
			b = _buf(0.8)
			for i in b.size():
				var t := i * inv
				var v := 0.0
				if t < 0.03:
					v += rng.randf_range(-1.0, 1.0) * 0.3 * (1.0 - t / 0.03)
				v += sin(TAU * 90.0 * t) * exp(-t * 30.0) * 0.3
				v += (sin(TAU * 1568.0 * t) + 0.35 * sin(TAU * 1568.0 * 2.76 * t)) * exp(-t * 5.0) * 0.26
				if t > 0.08:
					var t2 := t - 0.08
					v += (sin(TAU * 2093.0 * t2) + 0.35 * sin(TAU * 2093.0 * 2.76 * t2)) * exp(-t2 * 4.5) * 0.24
				b[i] = v
		"notify", "order":
			var f1 := 880.0 if sound_name == "notify" else 1318.5
			var f2 := 1318.5 if sound_name == "notify" else 1760.0
			var gap := 0.11 if sound_name == "notify" else 0.07
			b = _buf(0.55)
			for i in b.size():
				var t := i * inv
				var v := (sin(TAU * f1 * t) + 0.3 * sin(TAU * f1 * 2.0 * t)) * exp(-t * 9.0)
				if t > gap:
					var t2 := t - gap
					v += (sin(TAU * f2 * t2) + 0.3 * sin(TAU * f2 * 2.0 * t2)) * exp(-t2 * 7.0)
				b[i] = v * 0.22 * minf(t / 0.004, 1.0)
		"error", "bad":
			b = _buf(0.45 if sound_name == "bad" else 0.28)
			for i in b.size():
				var t := i * inv
				var f := 150.0
				if sound_name == "bad":
					f = 440.0 if t < 0.18 else 311.0
				var sq := signf(sin(TAU * f * t)) + signf(sin(TAU * f * 1.01 * t))
				b[i] = sq * exp(-t * (9.0 if sound_name == "error" else 5.0)) * 0.09
			_lowpass_buf(b, 2500.0)
		"tape":
			b = _noise_buf(0.5, rng)
			_lowpass_buf(b, 4200.0)
			for i in b.size():
				var t := i * inv
				var am := 0.5 + 0.5 * sin(TAU * 65.0 * t)
				b[i] *= am * am * minf(t / 0.03, 1.0) * clampf((0.5 - t) / 0.08, 0.0, 1.0) * (0.3 + t) * 0.8
		"printer":
			b = _noise_buf(0.65, rng)
			_lowpass_buf(b, 600.0)
			for i in b.size():
				var t := i * inv
				var v := b[i] * 0.35 + (fposmod(80.0 * t, 1.0) * 2.0 - 1.0) * 0.05
				v *= minf(t / 0.05, 1.0) * clampf((0.65 - t) / 0.1, 0.0, 1.0)
				for start in [0.02, 0.14, 0.52]:
					if t >= start and t < start + 0.045:
						v += sin(TAU * 2400.0 * t) * 0.22
				b[i] = v
		"levelup":
			b = _buf(1.1)
			var notes := [523.25, 659.25, 783.99, 1046.5, 2093.0]
			var starts := [0.0, 0.09, 0.18, 0.27, 0.45]
			for n in notes.size():
				var st := int(float(starts[n]) * RATE)
				for i in range(st, b.size()):
					var t := (i - st) * inv
					var f := float(notes[n])
					b[i] += (sin(TAU * f * t) + 0.3 * sin(TAU * f * 2.0 * t)) * exp(-t * 4.0) * 0.16
		"fold":
			b = _noise_buf(0.32, rng)
			_lowpass_buf(b, 1800.0)
			for i in b.size():
				var t := i * inv
				var env := exp(-t * 30.0)
				if t > 0.13:
					env += exp(-(t - 0.13) * 26.0)
				b[i] *= env * 0.5
		"truck":
			b = _buf(1.7)
			var nz := _noise_buf(1.7, rng)
			_lowpass_buf(nz, 300.0)
			for i in b.size():
				var t := i * inv
				var env := minf(t / 0.35, 1.0) * clampf((1.7 - t) / 0.6, 0.0, 1.0)
				var saw := fposmod(42.0 * t, 1.0) * 2.0 - 1.0
				var saw2 := fposmod(84.5 * t, 1.0) * 2.0 - 1.0
				b[i] = (saw * 0.35 + saw2 * 0.15 + nz[i] * 1.2) * env * 0.4
			_lowpass_buf(b, 900.0)
		"door":
			b = _noise_buf(1.3, rng)
			_lowpass_buf(b, 1200.0)
			for i in b.size():
				var t := i * inv
				var rattle := 0.55 + 0.45 * signf(sin(TAU * 26.0 * t))
				var env := minf(t / 0.08, 1.0) * clampf((1.3 - t) / 0.3, 0.0, 1.0)
				b[i] = (b[i] * rattle + sin(TAU * 58.0 * t) * 0.15) * env * 0.55
		"whoosh":
			b = _noise_buf(0.45, rng)
			var y := 0.0
			for i in b.size():
				var t := i * inv
				var a := 0.02 + 0.25 * sin(PI * t / 0.45)
				y += a * (b[i] - y)
				b[i] = y * sin(PI * t / 0.45) * 0.6
		"plate":
			b = _buf(0.3)
			for i in b.size():
				var t := i * inv
				var v := sin(TAU * 2650.0 * t) * 0.5 + sin(TAU * 3980.0 * t) * 0.3 + sin(TAU * 5210.0 * t) * 0.2
				b[i] = v * exp(-t * 16.0) * 0.28
		_:
			b = _buf(0.05)
	return b

# ---- Musik ------------------------------------------------------------------------------------
func _track_cfg(track: String) -> Dictionary:
	match track:
		"menu":
			return {"bpm": 68.0, "style": "pad", "seed": 11, "repeats": 2, "cutoff": 5200.0,
				"chords": [[48, 55, 59, 62, 64], [45, 52, 55, 60, 64], [41, 48, 52, 57, 60], [43, 50, 55, 59, 62]],
				"scale": [72, 74, 76, 79, 81, 84]}
		"diner":
			return {"bpm": 112.0, "style": "jazz", "seed": 23, "repeats": 2, "cutoff": 2600.0,
				"chords": [[50, 53, 57, 60], [43, 47, 50, 53], [48, 52, 55, 59], [45, 49, 52, 55]],
				"scale": [74, 76, 77, 79, 81, 84]}
		_:
			return {"bpm": 84.0, "style": "lofi", "seed": 7, "repeats": 2, "cutoff": 4600.0,
				"chords": [[53, 57, 60, 64], [52, 55, 59, 62], [50, 53, 57, 60], [48, 52, 55, 59]],
				"scale": [72, 74, 76, 79, 81, 84]}

func _render_music(track: String) -> PackedFloat32Array:
	var cfg := _track_cfg(track)
	var rng := RandomNumberGenerator.new()
	rng.seed = int(cfg["seed"])
	var style: String = cfg["style"]
	var beat := 60.0 / float(cfg["bpm"])
	var bar := beat * 4.0
	var chords: Array = cfg["chords"]
	var bars: int = chords.size() * int(cfg["repeats"])
	var out := _buf(bar * bars)
	var ep := _table([1.0, 0.42, 0.14, 0.06, 0.03])
	var pad := _table([1.0, 0.55, 0.36, 0.24, 0.16, 0.1, 0.06])
	var bass := _table([1.0, 0.28, 0.06])

	# Akkord-Anschläge nur einmal pro Akkord rendern, danach an alle Positionen mischen.
	var hit_long: Array = []
	var hit_short: Array = []
	for c in chords:
		var long_buf := _buf(bar * 1.15 if style == "pad" else beat * 1.8)
		var short_buf := _buf(beat * 1.3)
		for m in c:
			if style == "pad":
				_mix(long_buf, _tone(pad, float(m), bar * 1.15, 0.6, 0.35, 0.002), 0, 0.12)
			else:
				_mix(long_buf, _tone(ep, float(m), beat * 1.8, 0.008, 1.6, 0.003), 0, 0.14)
				_mix(short_buf, _tone(ep, float(m), beat * 1.3, 0.008, 2.4, 0.003), 0, 0.11)
		hit_long.append(long_buf)
		hit_short.append(short_buf)

	var kick := _drum_kick()
	var snare := _drum_snare(rng)
	var hat := _drum_hat(rng)
	var scale: Array = cfg["scale"]

	for bi in bars:
		var ci := bi % chords.size()
		var chord: Array = chords[ci]
		var bar_start := int(bi * bar * RATE)
		var root := float(chord[0]) - 12.0
		match style:
			"pad":
				_mix(out, hit_long[ci], bar_start, 1.0)
				_mix(out, _tone(bass, root, bar * 0.95, 0.2, 0.6, 0.0), bar_start, 0.28)
				_mix(out, kick, bar_start, 0.25)
				for s in 8:
					if s % 2 == 1:
						_mix(out, hat, bar_start + int((s * 0.5 + 0.08) * beat * RATE), 0.05)
			"jazz":
				_mix(out, hit_short[ci], bar_start + int(beat * 0.02 * RATE), 1.0)
				_mix(out, hit_short[ci], bar_start + int(beat * 2.5 * RATE), 0.8)
				var walk := [root, root + 4.0, root + 7.0, root + 10.0]
				for q in 4:
					_mix(out, _tone(bass, float(walk[q]), beat * 0.95, 0.01, 3.0, 0.0), bar_start + int(q * beat * RATE), 0.34)
				for s in 8:
					var swing := 0.16 if s % 2 == 1 else 0.0
					_mix(out, snare if s % 4 == 2 else hat, bar_start + int((s * 0.5 + swing) * beat * RATE), 0.12 if s % 4 == 2 else 0.07)
			_:
				_mix(out, hit_long[ci], bar_start, 1.0)
				_mix(out, hit_short[ci], bar_start + int(beat * 2.5 * RATE), 0.75)
				_mix(out, _tone(bass, root, beat * 1.6, 0.01, 1.2, 0.0), bar_start, 0.36)
				_mix(out, _tone(bass, root + 7.0, beat * 0.9, 0.01, 2.0, 0.0), bar_start + int(beat * 2.5 * RATE), 0.26)
				_mix(out, kick, bar_start, 0.62)
				_mix(out, kick, bar_start + int(beat * 2.5 * RATE), 0.45)
				if bi % 2 == 1:
					_mix(out, kick, bar_start + int(beat * 1.75 * RATE), 0.3)
				_mix(out, snare, bar_start + int(beat * RATE), 0.34)
				_mix(out, snare, bar_start + int(beat * 3.0 * RATE), 0.34)
				for s in 8:
					var swing := 0.17 if s % 2 == 1 else 0.0
					_mix(out, hat, bar_start + int((s * 0.5 + swing) * beat * RATE), rng.randf_range(0.06, 0.11))
		# Kleine Melodie aus der Pentatonik (seed-basiert, also bei jedem Start gleich)
		for s in 8:
			if rng.randf() < (0.3 if style != "pad" else 0.22):
				var note := float(scale[rng.randi_range(0, scale.size() - 1)])
				var note_len := beat * rng.randf_range(0.4, 0.9)
				_mix(out, _tone(ep, note, note_len, 0.01, 2.8, 0.004), bar_start + int(s * 0.5 * beat * RATE), 0.09)

	# Vinyl-Knistern + Rauschteppich + warmer Tiefpass über das ganze Stück
	var hiss := _noise_buf(1.0, rng)
	_lowpass_buf(hiss, 3000.0)
	var crackles := int(out.size() * 0.0003)
	for k in crackles:
		var pos := rng.randi_range(0, out.size() - 2)
		out[pos] += rng.randf_range(-0.2, 0.2)
		out[pos + 1] -= rng.randf_range(0.0, 0.1)
	var a := 1.0 - exp(-TAU * float(cfg["cutoff"]) / RATE)
	var hp_a := 1.0 - exp(-TAU * 280.0 / RATE)
	var y := 0.0
	var low := 0.0
	var radio := style == "jazz"
	var hiss_n := hiss.size()
	for i in out.size():
		var x := out[i] + hiss[i % hiss_n] * 0.012
		y += a * (x - y)
		if radio:
			low += hp_a * (y - low)
			out[i] = (y - low) * 1.4
		else:
			out[i] = y
	return out

func _drum_kick() -> PackedFloat32Array:
	var b := _buf(0.35)
	var phase := 0.0
	var inv := 1.0 / RATE
	for i in b.size():
		var t := i * inv
		phase += TAU * (48.0 + 70.0 * exp(-t * 28.0)) * inv
		b[i] = sin(phase) * exp(-t * 9.0)
	return b

func _drum_snare(rng: RandomNumberGenerator) -> PackedFloat32Array:
	var b := _noise_buf(0.24, rng)
	_lowpass_buf(b, 5200.0)
	var inv := 1.0 / RATE
	for i in b.size():
		var t := i * inv
		b[i] = b[i] * exp(-t * 17.0) * 0.8 + sin(TAU * 190.0 * t) * exp(-t * 28.0) * 0.45
	return b

func _drum_hat(rng: RandomNumberGenerator) -> PackedFloat32Array:
	var b := _noise_buf(0.06, rng)
	var inv := 1.0 / RATE
	var y := 0.0
	var a := 1.0 - exp(-TAU * 6000.0 / RATE)
	for i in b.size():
		var t := i * inv
		y += a * (b[i] - y)
		b[i] = (b[i] - y) * exp(-t * 75.0)
	return b
