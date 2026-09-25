class_name WorldBuilder
extends Node3D
## Baut die komplette Spielwelt zur Laufzeit: Straße, Kalles Imbiss, Garage, Lagerhalle,
## Park, Skyline, Laternen, Verkehr, Passanten. Verwaltet außerdem Tageslicht, Stationen je
## Ausbaustufe, Deko/Lifestyle-Objekte, Personal-Figuren, Lieferwagen und das Förderband.

const DINER_RECT := Rect2(-44, -19, 16, 12)
const GARAGE_RECT := Rect2(-22, -17, 12, 10)
const WAREHOUSE_RECT := Rect2(-2, -31, 36, 24)

const SPAWN_DINER := Vector3(-35.2, 0.05, -9.2)
const SPAWN_GARAGE := Vector3(-16.0, 0.05, -8.2)
const SPAWN_WAREHOUSE := Vector3(17.0, 0.05, -9.0)
const VAN_STOP := [Vector3(-16.0, 0, -2.2), Vector3(26.5, 0, -2.2)]
const BELT_START := Vector3(21.8, 0.9, -15.5)
const BELT_END := Vector3(7.2, 0.9, -15.5)

const DINER_TABLES := [
	{"id": 1, "pos": Vector3(-42.0, 0, -9.8)},
	{"id": 2, "pos": Vector3(-38.8, 0, -9.8)},
	{"id": 3, "pos": Vector3(-31.3, 0, -9.8)},
	{"id": 4, "pos": Vector3(-31.3, 0, -12.6)},
]

# Deko/Lifestyle-Plätze je Standort: id -> [[stage, pos, rot_y], ...]
const DECOR_SLOTS := {
	"pflanze": [[0, Vector3(-21.4, 0, -16.4), 0.0], [1, Vector3(-1.4, 0, -15.5), 0.0]],
	"poster": [[0, Vector3(-21.83, 1.8, -14.6), 90.0], [1, Vector3(-1.8, 1.8, -11.5), 90.0]],
	"stehlampe": [[0, Vector3(-13.6, 0, -16.4), 0.0], [1, Vector3(4.3, 0, -15.4), 0.0]],
	"teppich": [[0, Vector3(-16.0, 0, -11.2), 0.0], [1, Vector3(1.8, 0, -10.2), 0.0]],
	"billy": [[0, Vector3(-21.62, 0, -10.8), 90.0], [1, Vector3(-1.62, 0, -8.6), 90.0]],
	"whiteboard": [[0, Vector3(-14.4, 1.6, -16.83), 0.0], [1, Vector3(1.6, 1.8, -15.84), 0.0]],
	"kaffee": [[0, Vector3(-16.3, 0, -16.45), 0.0], [1, Vector3(33.3, 0, -22.0), -90.0]],
	"sofa": [[1, Vector3(30.0, 0, -13.2), 180.0]],
	"sneaker": [[0, Vector3(-18.62, 0, -16.5), 0.0], [1, Vector3(-1.4, 0, -12.6), 90.0]],
	"gamingstuhl": [[0, Vector3(-19.9, 0, -15.5), 180.0]],
	"neon": [[0, Vector3(-12.9, 2.4, -16.83), 0.0], [1, Vector3(15.3, 5.8, -30.75), 0.0]],
	"auto": [[-1, Vector3(-6.0, 0, -10.5), 90.0]],
	"sportwagen": [[-1, Vector3(-6.0, 0, -16.0), 90.0]],
}

var sun: DirectionalLight3D
var env: Environment
var sky_mat: ProceduralSkyMaterial
var street_lights: Array[OmniLight3D] = []
var stations_root: Node3D
var decor_root: Node3D
var dynamic_root: Node3D
var customers_root: Node3D
var staff_root: Node3D
var belt_root: Node3D
var brand_labels: Array[Label3D] = []
var gate: Node3D
var gate_col: CollisionShape3D
var sale_sign: Node3D
var penthouse: MeshInstance3D
var menu_mode: bool = false
var _traffic_acc: float = 0.0
var _belt_boxes: Dictionary = {}
var _rng := RandomNumberGenerator.new()

func build(p_menu_mode: bool = false) -> void:
	menu_mode = p_menu_mode
	_rng.seed = 4242
	_environment()
	_ground_and_street()
	_diner()
	_garage()
	_warehouse()
	_park()
	_background()
	_bounds()
	stations_root = _node("Stations")
	decor_root = _node("Decor")
	dynamic_root = _node("Dynamic")
	customers_root = _node("Customers")
	staff_root = _node("Staff")
	belt_root = _node("Belt")
	_pedestrians()
	rebuild_stations()
	refresh_world(false)
	set_time_of_day(12.0)

func _node(n: String) -> Node3D:
	var node := Node3D.new()
	node.name = n
	add_child(node)
	return node

# =====================================================================================
# Umgebung & Licht
# =====================================================================================
func _environment() -> void:
	sun = DirectionalLight3D.new()
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 80.0
	sun.light_angular_distance = 0.6
	sun.shadow_blur = 1.2
	add_child(sun)
	var we := WorldEnvironment.new()
	env = Environment.new()
	env.background_mode = Environment.BG_SKY
	var sky := Sky.new()
	sky_mat = ProceduralSkyMaterial.new()
	sky_mat.sky_curve = 0.12
	sky_mat.ground_bottom_color = Color(0.22, 0.24, 0.26)
	sky_mat.ground_horizon_color = Color(0.62, 0.66, 0.7)
	sky.sky_material = sky_mat
	env.sky = sky
	env.ambient_light_source = Environment.AMBIENT_SOURCE_SKY
	env.ambient_light_color = Color(0.66, 0.63, 0.6)
	env.ambient_light_sky_contribution = 0.55
	env.ambient_light_energy = 0.85
	env.reflected_light_source = Environment.REFLECTION_SOURCE_SKY
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	env.tonemap_white = 6.0
	env.ssao_enabled = true
	env.ssao_radius = 1.3
	env.ssao_intensity = 1.8
	env.glow_enabled = true
	env.glow_intensity = 0.7
	env.glow_bloom = 0.04
	env.glow_hdr_threshold = 1.0
	env.fog_enabled = true
	env.fog_light_color = Color(0.72, 0.77, 0.84)
	env.fog_density = 0.0028
	env.fog_sky_affect = 0.25
	env.fog_aerial_perspective = 0.35
	env.adjustment_enabled = true
	env.adjustment_saturation = 1.1
	env.adjustment_contrast = 1.06
	we.environment = env
	add_child(we)

## Tageslicht: Sonnenstand, Farben, Straßenlaternen und Fensterlicht nach Uhrzeit.
func set_time_of_day(hours: float) -> void:
	var t := clampf((hours - 6.0) / 15.0, 0.0, 1.0)
	var elevation := sin(t * PI) * 62.0 + 3.0
	var azimuth := lerpf(-110.0, 110.0, t)
	sun.rotation_degrees = Vector3(-elevation, azimuth, 0)
	var warm := 1.0 - clampf((elevation - 6.0) / 30.0, 0.0, 1.0)
	sun.light_color = Color(1.0, 0.96, 0.9).lerp(Color(1.0, 0.58, 0.32), warm)
	sun.light_energy = lerpf(0.55, 1.3, clampf(elevation / 40.0, 0.0, 1.0))
	sky_mat.sky_top_color = Color(0.28, 0.5, 0.82).lerp(Color(0.26, 0.3, 0.55), warm)
	sky_mat.sky_horizon_color = Color(0.7, 0.8, 0.92).lerp(Color(0.98, 0.64, 0.42), warm)
	sky_mat.ground_horizon_color = sky_mat.sky_horizon_color.darkened(0.2)
	env.ambient_light_energy = lerpf(0.9, 0.55, warm)
	env.fog_light_color = Color(0.72, 0.77, 0.84).lerp(Color(0.9, 0.62, 0.5), warm)
	var night := clampf((hours - 18.6) / 1.4, 0.0, 1.0)
	Mats.set_night(night)
	for l in street_lights:
		l.visible = night > 0.02
		l.light_energy = night * 2.6

# =====================================================================================
# Boden, Straße
# =====================================================================================
func _ground_and_street() -> void:
	var ground := StaticBody3D.new()
	var col := CollisionShape3D.new()
	var shape := BoxShape3D.new()
	shape.size = Vector3(400, 1, 400)
	col.shape = shape
	col.position = Vector3(0, -0.5, 0)
	ground.add_child(col)
	add_child(ground)
	add_child(Props.box(Vector3(400, 0.02, 400), Mats.shader("grass"), Vector3(0, -0.012, 0)))
	var asphalt := Mats.shader("asphalt")
	add_child(Props.box(Vector3(400, 0.02, 8), asphalt, Vector3(0, 0.004, 0)))
	var walk := Mats.shader("concrete", {"tile": 1.0, "scale": 0.5, "color_a": Color(0.66, 0.65, 0.63), "color_b": Color(0.56, 0.55, 0.53), "stain": 0.1})
	add_child(Props.box(Vector3(400, 0.03, 3.0), walk, Vector3(0, 0.012, -5.5)))
	add_child(Props.box(Vector3(400, 0.03, 3.0), walk, Vector3(0, 0.012, 5.5)))
	var curb := Mats.std(Color(0.7, 0.7, 0.68), 0.8)
	add_child(Props.box(Vector3(400, 0.1, 0.18), curb, Vector3(0, 0.05, -4.0)))
	add_child(Props.box(Vector3(400, 0.1, 0.18), curb, Vector3(0, 0.05, 4.0)))
	var lot := Mats.shader("concrete", {"scale": 0.25, "color_a": Color(0.5, 0.5, 0.49), "color_b": Color(0.4, 0.4, 0.4), "stain": 0.4})
	add_child(Props.box(Vector3(120, 0.02, 30), lot, Vector3(-4, 0.008, -22)))
	var white := Mats.std(Color(0.92, 0.92, 0.9), 0.6)
	for i in 34:
		add_child(Props.box(Vector3(3.0, 0.01, 0.14), white, Vector3(-100 + i * 6.0, 0.017, 0)))
	add_child(Props.box(Vector3(400, 0.01, 0.12), white, Vector3(0, 0.017, -3.65)))
	add_child(Props.box(Vector3(400, 0.01, 0.12), white, Vector3(0, 0.017, 3.65)))
	for i in 7:
		add_child(Props.box(Vector3(0.5, 0.012, 3.6), white, Vector3(-38.0 + i * 1.0, 0.018, -1.9)))
		add_child(Props.box(Vector3(0.5, 0.012, 3.6), white, Vector3(-38.0 + i * 1.0, 0.018, 1.9)))
	for x in [-48.0, -32.0, -16.0, 0.0, 16.0, 32.0, 44.0]:
		var lp := Props.lamp_post()
		var n: Node3D = lp["node"]
		n.position = Vector3(x, 0, 6.8)
		n.rotation_degrees.y = 180
		add_child(n)
		street_lights.append(lp["light"])
	for x in [-25.0, -4.0, 36.0]:
		var lp2 := Props.lamp_post()
		var n2: Node3D = lp2["node"]
		n2.position = Vector3(x, 0, -6.85)
		add_child(n2)
		street_lights.append(lp2["light"])

# =====================================================================================
# Gebäude-Helfer
# =====================================================================================
## Achsenparallele Wand von a nach b (gleiches z ODER gleiches x) mit Öffnungen [von, bis, höhe].
func _wall(a: Vector3, b: Vector3, height: float, thick: float, mat: Material, openings: Array = []) -> void:
	var along_x := absf(a.z - b.z) < 0.01
	var start := minf(a.x, b.x) if along_x else minf(a.z, b.z)
	var stop := maxf(a.x, b.x) if along_x else maxf(a.z, b.z)
	var cuts: Array = []
	for o in openings:
		cuts.append(o)
	cuts.sort_custom(func(p, q): return p[0] < q[0])
	var cursor := start
	var segs: Array = []
	for o in cuts:
		if float(o[0]) > cursor:
			segs.append([cursor, float(o[0]), 0.0, height])
		segs.append([float(o[0]), float(o[1]), float(o[2]), height])
		cursor = float(o[1])
	if cursor < stop:
		segs.append([cursor, stop, 0.0, height])
	for s in segs:
		var seg_len := float(s[1]) - float(s[0])
		var h := float(s[3]) - float(s[2])
		if seg_len <= 0.01 or h <= 0.01:
			continue
		var mid := (float(s[0]) + float(s[1])) / 2.0
		var y := float(s[2]) + h / 2.0
		if along_x:
			Props.solid(self, Vector3(seg_len, h, thick), mat, Vector3(mid, y, a.z))
		else:
			Props.solid(self, Vector3(thick, h, seg_len), mat, Vector3(a.x, y, mid))

## Fenster in einer Wand: Außen- und Innenscheibe + Rahmen. normal zeigt nach außen.
func _window(center: Vector3, size: Vector2, normal: Vector3) -> void:
	var rot := Vector3(0, 90, 0) if absf(normal.x) > 0.5 else Vector3.ZERO
	var ext := Props.box(Vector3(size.x, size.y, 0.02), Mats.shader("window"), center + normal * 0.17, rot)
	var inte := Props.box(Vector3(size.x, size.y, 0.02), Mats.shader("window", {"interior": 1.0}), center - normal * 0.17, rot)
	ext.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	inte.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(ext)
	add_child(inte)
	var frame := Mats.std(Color(0.9, 0.9, 0.88), 0.5)
	add_child(Props.box(Vector3(size.x + 0.12, 0.08, 0.1) if rot == Vector3.ZERO else Vector3(0.1, 0.08, size.x + 0.12), frame, center + normal * 0.2 - Vector3(0, size.y / 2.0 + 0.04, 0)))

func _roof(rect: Rect2, height: float, mat: Material) -> void:
	var r := Props.box(Vector3(rect.size.x + 0.5, 0.3, rect.size.y + 0.5), mat,
		Vector3(rect.position.x + rect.size.x / 2.0, height + 0.15, rect.position.y + rect.size.y / 2.0))
	add_child(r)
	var trim := Mats.std(Color(0.2, 0.21, 0.23), 0.6)
	add_child(Props.box(Vector3(rect.size.x + 0.6, 0.35, 0.12), trim, Vector3(rect.position.x + rect.size.x / 2.0, height + 0.3, rect.position.y + rect.size.y + 0.25)))

func _floor(rect: Rect2, mat: Material) -> void:
	add_child(Props.box(Vector3(rect.size.x - 0.2, 0.025, rect.size.y - 0.2), mat,
		Vector3(rect.position.x + rect.size.x / 2.0, 0.02, rect.position.y + rect.size.y / 2.0)))

func _ceiling_lamp(pos: Vector3, length: float, color: Color, energy: float, rng: float) -> void:
	var fix := Props.ceiling_light(length)
	fix.position = pos
	add_child(fix)
	var l := OmniLight3D.new()
	l.position = pos - Vector3(0, 1.0, 0)
	l.omni_range = rng
	l.light_energy = energy
	l.light_color = color
	l.omni_attenuation = 0.8
	add_child(l)

# =====================================================================================
# Kalles Imbiss
# =====================================================================================
func _diner() -> void:
	var r := DINER_RECT
	var x0 := r.position.x
	var x1 := r.position.x + r.size.x
	var z0 := r.position.y
	var z1 := r.position.y + r.size.y
	var h := 4.2
	var brick := Mats.shader("brick", {"brick": Color(0.64, 0.3, 0.22), "mortar": Color(0.8, 0.77, 0.7)})
	_wall(Vector3(x0, 0, z1 - 0.15), Vector3(x1, 0, z1 - 0.15), h, 0.3, brick, [[-36.2, -34.2, 2.5]])
	_wall(Vector3(x0, 0, z0 + 0.15), Vector3(x1, 0, z0 + 0.15), h, 0.3, brick)
	_wall(Vector3(x0 + 0.15, 0, z0), Vector3(x0 + 0.15, 0, z1), h, 0.3, brick)
	_wall(Vector3(x1 - 0.15, 0, z0), Vector3(x1 - 0.15, 0, z1), h, 0.3, brick)
	_roof(r, h, Mats.shader("metal_sheet", {"base": Color(0.35, 0.37, 0.4)}))
	_floor(r, Mats.shader("tiles", {"tile_size": 0.45}))
	for wx in [-40.5, -31.0]:
		_window(Vector3(wx, 1.8, z1 - 0.15), Vector2(4.2, 1.5), Vector3(0, 0, 1))
	# Markise + Neonschild
	add_child(Props.box(Vector3(16.4, 0.12, 1.4), Mats.std(Color(0.78, 0.14, 0.16), 0.6), Vector3(-36, 3.1, z1 + 0.6), Vector3(-12, 0, 0)))
	for i in 8:
		add_child(Props.box(Vector3(1.0, 0.13, 1.41), Mats.std(Color(0.95, 0.95, 0.93), 0.6), Vector3(x0 + 0.5 + i * 2.0, 3.101, z1 + 0.6), Vector3(-12, 0, 0)))
	var neon := Props.label("KALLES IMBISS", 220, Color(3.2, 0.7, 0.45), Vector3(-36, 3.75, z1 + 0.05), false, 0)
	add_child(neon)
	var neon_light := OmniLight3D.new()
	neon_light.position = Vector3(-36, 3.6, z1 + 1.2)
	neon_light.light_color = Color(1.0, 0.35, 0.25)
	neon_light.light_energy = 1.5
	neon_light.omni_range = 7.0
	add_child(neon_light)
	var aboard := Node3D.new()
	aboard.position = Vector3(-33.0, 0, -5.8)
	aboard.rotation_degrees.y = 20
	aboard.add_child(Props.box(Vector3(0.7, 1.0, 0.05), Mats.std(Color(0.12, 0.12, 0.12), 0.7), Vector3(0, 0.5, 0.18), Vector3(-10, 0, 0)))
	aboard.add_child(Props.label("DÖNER 5,50\nPOMMES 3,-\nCURRYWURST 4,-", 40, Color(1, 1, 1), Vector3(0, 0.55, 0.24), false, 0))
	add_child(aboard)
	# Theke
	var counter_body := Mats.std(Color(0.75, 0.16, 0.18), 0.5)
	Props.solid(self, Vector3(10.5, 1.0, 0.8), counter_body, Vector3(-38.25, 0.5, -15.2))
	add_child(Props.box(Vector3(10.7, 0.06, 0.95), Mats.std(Color(0.85, 0.86, 0.88), 0.25, 0.8), Vector3(-38.25, 1.03, -15.2)))
	for sx in [-42.0, -40.4, -36.8]:
		var st := Props.stool()
		st.position = Vector3(sx, 0, -14.3)
		add_child(st)
	# Küche
	var steel := Mats.std(Color(0.75, 0.77, 0.8), 0.3, 0.85)
	Props.solid(self, Vector3(10.0, 0.95, 0.9), steel, Vector3(-38.5, 0.475, -18.4))
	add_child(Props.box(Vector3(1.6, 0.05, 0.7), Mats.std(Color(0.1, 0.1, 0.1), 0.4), Vector3(-41.0, 0.98, -18.4)))
	for fx in [-38.2, -37.2]:
		add_child(Props.box(Vector3(0.7, 0.2, 0.6), steel, Vector3(fx, 1.05, -18.4)))
		add_child(Props.box(Vector3(0.55, 0.05, 0.45), Mats.std(Color(0.85, 0.6, 0.2), 0.3), Vector3(fx, 1.14, -18.4)))
	add_child(Props.box(Vector3(6.0, 0.6, 1.0), steel, Vector3(-39.0, 3.2, -18.4)))
	Props.solid(self, Vector3(1.0, 2.1, 0.8), Mats.std(Color(0.9, 0.9, 0.92), 0.3, 0.3), Vector3(-43.3, 1.05, -17.6))
	var menu := Props.sign_board("KALLES KARTE", Color(0.1, 0.1, 0.1), Color(1.0, 0.85, 0.3), Vector2(3.4, 0.5))
	menu.position = Vector3(-38, 2.6, z0 + 0.32)
	add_child(menu)
	add_child(Props.label("Döner ....... 5,50\nCurrywurst .. 4,00\nPommes ...... 3,00\nSchnitzel ... 8,90", 48, Color(0.95, 0.95, 0.9), Vector3(-38, 1.95, z0 + 0.33), false, 0))
	for lx in [-41.0, -36.0, -31.0]:
		_ceiling_lamp(Vector3(lx, h - 0.1, -12.0), 1.2, Color(1.0, 0.85, 0.65), 1.4, 8.0)
	_ceiling_lamp(Vector3(-38.5, h - 0.1, -17.2), 2.0, Color(1.0, 0.95, 0.85), 1.1, 6.0)

# =====================================================================================
# Garage
# =====================================================================================
func _garage() -> void:
	var r := GARAGE_RECT
	var x0 := r.position.x
	var x1 := r.position.x + r.size.x
	var z0 := r.position.y
	var z1 := r.position.y + r.size.y
	var h := 3.6
	var block := Mats.shader("brick", {"brick": Color(0.62, 0.62, 0.6), "mortar": Color(0.5, 0.5, 0.48), "size": Vector2(0.4, 0.2)})
	_wall(Vector3(x0, 0, z1 - 0.15), Vector3(x1, 0, z1 - 0.15), h, 0.3, block, [[-19.5, -12.5, 3.0]])
	_wall(Vector3(x0, 0, z0 + 0.15), Vector3(x1, 0, z0 + 0.15), h, 0.3, block)
	_wall(Vector3(x0 + 0.15, 0, z0), Vector3(x0 + 0.15, 0, z1), h, 0.3, block)
	_wall(Vector3(x1 - 0.15, 0, z0), Vector3(x1 - 0.15, 0, z1), h, 0.3, block)
	_roof(r, h, Mats.shader("metal_sheet", {"base": Color(0.42, 0.44, 0.47)}))
	_floor(r, Mats.shader("concrete", {"scale": 0.5, "stain": 0.5}))
	# aufgerolltes Tor
	add_child(Props.cyl(0.25, 0.25, 7.2, Mats.std(Color(0.55, 0.57, 0.6), 0.5, 0.6), Vector3(-16, 3.25, z1 - 0.45), Vector3(0, 0, 90)))
	add_child(Props.box(Vector3(7.2, 0.35, 0.05), Mats.shader("metal_sheet", {"base": Color(0.7, 0.72, 0.75), "ribs": 40.0}), Vector3(-16, 2.85, z1 - 0.2)))
	_window(Vector3(x0 + 0.15, 1.9, -13.0), Vector2(1.6, 1.0), Vector3(-1, 0, 0))
	for lx in [-18.0, -13.8]:
		_ceiling_lamp(Vector3(lx, h - 0.08, -12.0), 1.4, Color(0.92, 0.96, 1.0), 1.5, 8.0)
	var brand := Props.label("GARAGE", 170, Color(1, 1, 1), Vector3(-16, 3.3, z1 + 0.03), false, 10)
	add_child(brand)
	brand_labels.append(brand)
	var spot := SpotLight3D.new()
	spot.position = Vector3(-16, 3.9, z1 + 0.8)
	spot.rotation_degrees = Vector3(-60, 0, 0)
	spot.light_energy = 2.0
	spot.spot_range = 5.0
	spot.light_color = Color(1.0, 0.9, 0.75)
	add_child(spot)
	# Werkbank-Verlängerung und Kram
	var pal := Props.pallet()
	pal.position = Vector3(-12.3, 0, -16.2)
	add_child(pal)

# =====================================================================================
# Lagerhalle
# =====================================================================================
func _warehouse() -> void:
	var r := WAREHOUSE_RECT
	var x0 := r.position.x
	var x1 := r.position.x + r.size.x
	var z0 := r.position.y
	var z1 := r.position.y + r.size.y
	var h := 7.5
	var plinth := Mats.shader("concrete", {"scale": 0.6, "color_a": Color(0.62, 0.62, 0.6), "color_b": Color(0.52, 0.52, 0.5)})
	var sheet := Mats.shader("metal_sheet", {"base": Color(0.36, 0.45, 0.56), "ribs": 18.0})
	var gate_open := [[14.0, 20.0, 4.6]]
	_wall(Vector3(x0, 0, z1 - 0.15), Vector3(x1, 0, z1 - 0.15), 1.2, 0.35, plinth, gate_open)
	_wall(Vector3(x0, 0, z0 + 0.15), Vector3(x1, 0, z0 + 0.15), 1.2, 0.35, plinth)
	_wall(Vector3(x0 + 0.15, 0, z0), Vector3(x0 + 0.15, 0, z1), 1.2, 0.35, plinth)
	_wall(Vector3(x1 - 0.15, 0, z0), Vector3(x1 - 0.15, 0, z1), 1.2, 0.35, plinth)
	# obere Blechverkleidung (über dem Sockel)
	_wall_band(Vector3(x0, 0, z1 - 0.15), Vector3(x1, 0, z1 - 0.15), 1.2, h, 0.3, sheet, [[14.0, 20.0, 4.6]])
	_wall_band(Vector3(x0, 0, z0 + 0.15), Vector3(x1, 0, z0 + 0.15), 1.2, h, 0.3, sheet, [])
	_wall_band(Vector3(x0 + 0.15, 0, z0), Vector3(x0 + 0.15, 0, z1), 1.2, h, 0.3, sheet, [])
	_wall_band(Vector3(x1 - 0.15, 0, z0), Vector3(x1 - 0.15, 0, z1), 1.2, h, 0.3, sheet, [])
	_roof(r, h, Mats.shader("metal_sheet", {"base": Color(0.4, 0.42, 0.45)}))
	_floor(r, Mats.shader("concrete", {"scale": 0.3, "color_a": Color(0.6, 0.6, 0.58), "color_b": Color(0.52, 0.52, 0.5), "stain": 0.2}))
	for wx in [6.0, 27.0]:
		_window(Vector3(wx, 5.4, z1 - 0.15), Vector2(10.0, 1.0), Vector3(0, 0, 1))
	for wz in [-24.0, -14.0]:
		_window(Vector3(x1 - 0.15, 5.4, wz), Vector2(7.0, 1.0), Vector3(1, 0, 0))
		_window(Vector3(x0 + 0.15, 5.4, wz), Vector2(7.0, 1.0), Vector3(-1, 0, 0))
	# Sicherheitslinien am Boden
	var yellow := Mats.std(Color(0.95, 0.78, 0.12), 0.6)
	for lz in [-17.8, -22.4, -26.8]:
		add_child(Props.box(Vector3(30.0, 0.012, 0.1), yellow, Vector3(17.5, 0.035, lz)))
	add_child(Props.box(Vector3(0.1, 0.012, 16.0), yellow, Vector3(12.0, 0.035, -15.0)))
	for lx in [4.0, 16.0, 28.0]:
		for lz2 in [-12.0, -24.0]:
			_ceiling_lamp(Vector3(lx, h - 0.2, lz2), 2.4, Color(0.95, 0.97, 1.0), 1.7, 15.0)
	# Tor (Rolltor)
	gate = Node3D.new()
	gate.position = Vector3(17.0, 2.3, z1 - 0.15)
	add_child(gate)
	gate.add_child(Props.box(Vector3(6.0, 4.6, 0.12), Mats.shader("metal_sheet", {"base": Color(0.72, 0.74, 0.76), "ribs": 30.0}), Vector3.ZERO, Vector3(0, 0, 0)))
	var gbody := StaticBody3D.new()
	gate_col = CollisionShape3D.new()
	var gshape := BoxShape3D.new()
	gshape.size = Vector3(6.0, 4.6, 0.4)
	gate_col.shape = gshape
	gbody.add_child(gate_col)
	gate.add_child(gbody)
	sale_sign = Props.sign_board("ZU VERKAUFEN · 4.500 €", Color(0.9, 0.15, 0.15), Color(1, 1, 1), Vector2(5.0, 0.8))
	sale_sign.position = Vector3(6.5, 2.6, z1 + 0.05)
	add_child(sale_sign)
	var brand := Props.label("LAGERHALLE", 300, Color(1, 1, 1), Vector3(17.0, 6.3, z1 + 0.05), false, 14)
	add_child(brand)
	brand_labels.append(brand)
	# Büro-Trennwand
	var low := Mats.std(Color(0.85, 0.85, 0.82), 0.6)
	Props.solid(self, Vector3(6.85, 1.1, 0.12), low, Vector3(1.575, 0.55, -16.0))
	Props.solid(self, Vector3(0.12, 1.1, 5.5), low, Vector3(5.0, 0.55, -13.25))
	add_child(Props.box(Vector3(6.85, 1.3, 0.04), Mats.glass(), Vector3(1.575, 1.75, -16.0)))
	add_child(Props.box(Vector3(0.04, 1.3, 5.5), Mats.glass(), Vector3(5.0, 1.75, -13.25)))
	# Pausenecke
	var vm := Props.vending_machine()
	vm.position = Vector3(33.2, 0, -18.0)
	vm.rotation_degrees.y = -90
	add_child(vm)
	Props.collider(self, Vector3(0.8, 1.9, 0.9), Vector3(33.2, 0.95, -18.0))
	var bt := Props.table(1.2, 0.8, 0.76, Mats.std(Color(0.92, 0.92, 0.9), 0.4), Mats.dark_metal())
	bt.position = Vector3(29.8, 0, -21.0)
	add_child(bt)
	for cz in [-21.7, -20.3]:
		var ch := Props.chair(Mats.std(Color(0.2, 0.45, 0.7), 0.6))
		ch.position = Vector3(29.8, 0, cz)
		ch.rotation_degrees.y = 0.0 if cz < -21.0 else 180.0
		add_child(ch)
	# Kisten und Paletten als Atmosphäre
	for i in 3:
		var pl := Props.pallet()
		pl.position = Vector3(31.5, 0, -28.5 + i * 1.3)
		add_child(pl)
	var extin := Props.cyl(0.1, 0.1, 0.5, Mats.std(Color(0.85, 0.1, 0.1), 0.4), Vector3(-1.72, 1.0, -20.0))
	add_child(extin)

## Wandband von y0 bis y1 (für die Blechverkleidung über dem Sockel)
func _wall_band(a: Vector3, b: Vector3, y0: float, y1: float, thick: float, mat: Material, openings: Array) -> void:
	var along_x := absf(a.z - b.z) < 0.01
	var start := minf(a.x, b.x) if along_x else minf(a.z, b.z)
	var stop := maxf(a.x, b.x) if along_x else maxf(a.z, b.z)
	var segs: Array = []
	var cursor := start
	for o in openings:
		if float(o[0]) > cursor:
			segs.append([cursor, float(o[0]), y0])
		segs.append([float(o[0]), float(o[1]), float(o[2])])
		cursor = float(o[1])
	if cursor < stop:
		segs.append([cursor, stop, y0])
	for s in segs:
		var seg_len := float(s[1]) - float(s[0])
		var bottom := float(s[2])
		var hh := y1 - bottom
		if seg_len <= 0.01 or hh <= 0.01:
			continue
		var mid := (float(s[0]) + float(s[1])) / 2.0
		if along_x:
			Props.solid(self, Vector3(seg_len, hh, thick), mat, Vector3(mid, bottom + hh / 2.0, a.z))
		else:
			Props.solid(self, Vector3(thick, hh, seg_len), mat, Vector3(a.x, bottom + hh / 2.0, mid))

# =====================================================================================
# Park, Hintergrund, Grenzen
# =====================================================================================
func _park() -> void:
	var path := Mats.shader("concrete", {"tile": 0.8, "scale": 0.4, "color_a": Color(0.72, 0.68, 0.6), "color_b": Color(0.62, 0.58, 0.52), "stain": 0.1})
	add_child(Props.box(Vector3(106, 0.025, 2.4), path, Vector3(-4, 0.01, 12.5)))
	for i in 4:
		add_child(Props.box(Vector3(2.0, 0.025, 5.0), path, Vector3(-40.0 + i * 26.0, 0.01, 8.8)))
	for bx in [-44.0, -20.0, 4.0, 28.0]:
		var b := Props.bench()
		b.position = Vector3(bx, 0, 14.4)
		b.rotation_degrees.y = 180
		add_child(b)
		Props.collider(self, Vector3(1.8, 0.9, 0.5), Vector3(bx, 0.45, 14.4))
		var bin := Props.trash_bin()
		bin.position = Vector3(bx + 1.5, 0, 14.4)
		add_child(bin)
	var placed := 0
	var tries := 0
	while placed < 30 and tries < 400:
		tries += 1
		var x := _rng.randf_range(-55.0, 47.0)
		var z := _rng.randf_range(8.2, 28.5)
		if absf(z - 12.5) < 2.2 or absf(z - 14.4) < 1.0:
			continue
		if x > -46.0 and x < 8.0 and z < 11.3:
			continue  # Sichtachse vom Park auf die Straße freihalten
		var tree := Props.tree(_rng.randf_range(0.85, 1.3), _rng.randi_range(0, 5))
		tree.position = Vector3(x, 0, z)
		tree.rotation_degrees.y = _rng.randf_range(0, 360)
		add_child(tree)
		Props.collider(self, Vector3(0.4, 3.0, 0.4), Vector3(x, 1.5, z))
		placed += 1
	for i in 16:
		var bush := Props.bush(_rng.randf_range(0.7, 1.1))
		bush.position = Vector3(-54.0 + i * 6.5 + _rng.randf_range(-1, 1), 0, 7.6)
		add_child(bush)
	var f := Props.fence(106.0)
	f.position = Vector3(-4, 0, 29.8)
	add_child(f)
	# Deko hinter den Gebäuden
	var dump := Props.trash_bin(Color(0.2, 0.3, 0.5))
	dump.position = Vector3(-30.0, 0, -21.0)
	dump.scale = Vector3(2.2, 1.5, 1.6)
	add_child(dump)
	for i in 2:
		var pl := Props.pallet()
		pl.position = Vector3(-24.5, i * 0.15, -20.0)
		add_child(pl)

func _background() -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = 99
	var walls := [Color(0.72, 0.66, 0.58), Color(0.6, 0.62, 0.66), Color(0.75, 0.55, 0.45), Color(0.55, 0.58, 0.52), Color(0.82, 0.8, 0.74)]
	var x := -120.0
	while x < 120.0:
		var w := rng.randf_range(10.0, 18.0)
		var hgt := rng.randf_range(9.0, 30.0)
		var cx := x + w / 2.0
		if absf(cx - 18.0) < 9.0:
			hgt = 46.0
		var mat := Mats.shader("facade", {"wall": walls[rng.randi_range(0, walls.size() - 1)], "seed": rng.randf() * 50.0, "lit_ratio": 0.4})
		add_child(Props.box(Vector3(w - 1.0, hgt, 12.0), mat, Vector3(cx, hgt / 2.0, 42.0)))
		add_child(Props.box(Vector3(w - 0.6, 0.4, 12.4), Mats.std(Color(0.25, 0.25, 0.27), 0.7), Vector3(cx, hgt + 0.2, 42.0)))
		if hgt >= 46.0:
			penthouse = Props.box(Vector3(w - 0.9, 3.0, 12.1), Mats.emit(Color(1.0, 0.78, 0.3), 3.0), Vector3(cx, hgt - 2.0, 42.0))
			penthouse.visible = false
			add_child(penthouse)
		var h2 := rng.randf_range(8.0, 22.0)
		var mat2 := Mats.shader("facade", {"wall": walls[rng.randi_range(0, walls.size() - 1)], "seed": rng.randf() * 50.0, "lit_ratio": 0.35})
		add_child(Props.box(Vector3(w - 1.0, h2, 12.0), mat2, Vector3(cx, h2 / 2.0, -44.0)))
		x += w
	for side in [-1.0, 1.0]:
		for zz in [-20.0, 20.0]:
			var hh := rng.randf_range(10.0, 20.0)
			var mat3 := Mats.shader("facade", {"wall": walls[rng.randi_range(0, walls.size() - 1)], "seed": rng.randf() * 50.0})
			add_child(Props.box(Vector3(20.0, hh, 22.0), mat3, Vector3(side * 75.0 if side > 0 else -78.0, hh / 2.0, zz)))

func _bounds() -> void:
	var walls := [
		[Vector3(0.5, 6, 70), Vector3(-57.0, 3, -2.0)],
		[Vector3(0.5, 6, 70), Vector3(49.0, 3, -2.0)],
		[Vector3(110, 6, 0.5), Vector3(-4.0, 3, -34.0)],
		[Vector3(110, 6, 0.5), Vector3(-4.0, 3, 30.0)],
	]
	for w in walls:
		Props.collider(self, w[0], w[1])
	for bx in [-56.2, 48.2]:
		for bz in [-5.5, -2.0, 2.0, 5.5]:
			var b := Props.barrier()
			b.position = Vector3(bx, 0, bz)
			b.rotation_degrees.y = 90
			add_child(b)
	var fence_n := Props.fence(110.0)
	fence_n.position = Vector3(-4, 0, -33.8)
	add_child(fence_n)

# =====================================================================================
# Stationen, Deko, Kunden
# =====================================================================================
func station_defs() -> Array:
	var defs: Array = []
	defs.append({"type": Station.StationType.NPC_TALK, "pos": Vector3(-39.5, 0, -16.6), "rot": 0.0})
	defs.append({"type": Station.StationType.DINER_PASS, "pos": Vector3(-34.2, 0, -15.2), "rot": 0.0})
	for t in DINER_TABLES:
		defs.append({"type": Station.StationType.DINER_TABLE, "pos": t["pos"], "rot": 0.0, "table_id": t["id"]})
	if GameManager.location_stage == 0:
		defs.append({"type": Station.StationType.PC, "pos": Vector3(-20.0, 0, -16.35), "rot": 0.0, "stage": 0})
		defs.append({"type": Station.StationType.LABEL, "pos": Vector3(-17.6, 0, -16.45), "rot": 0.0})
		for i in 3:
			defs.append({"type": Station.StationType.REGAL, "pos": Vector3(-10.75, 0, -14.2 + i * 2.3), "rot": -90.0, "product_index": i, "stage": 0})
		defs.append({"type": Station.StationType.PACK, "pos": Vector3(-15.2, 0, -12.6), "rot": 0.0})
		defs.append({"type": Station.StationType.FOLD, "pos": Vector3(-21.3, 0, -12.6), "rot": 90.0})
		defs.append({"type": Station.StationType.DOCK, "pos": Vector3(-20.4, 0, -9.0), "rot": 90.0, "stage": 0})
		defs.append({"type": Station.StationType.SHIP, "pos": Vector3(-11.2, 0, -5.4), "rot": -90.0, "stage": 0})
		defs.append({"type": Station.StationType.END_DAY, "pos": Vector3(-12.4, 0, -15.8), "rot": 0.0, "stage": 0})
	else:
		defs.append({"type": Station.StationType.PC, "pos": Vector3(1.5, 0, -13.2), "rot": 90.0, "stage": 1})
		defs.append({"type": Station.StationType.END_DAY, "pos": Vector3(12.6, 0, -7.7), "rot": 180.0, "stage": 1})
		for i in GameData.PRODUCTS.size():
			defs.append({"type": Station.StationType.REGAL, "pos": Vector3(2.8 + i * 5.0, 0, -29.3), "rot": 0.0, "product_index": i, "stage": 1})
		defs.append({"type": Station.StationType.FOLD, "pos": Vector3(8.0, 0, -20.0), "rot": 0.0})
		defs.append({"type": Station.StationType.PACK, "pos": Vector3(13.0, 0, -20.0), "rot": 0.0})
		defs.append({"type": Station.StationType.LABEL, "pos": Vector3(17.5, 0, -20.0), "rot": 0.0})
		defs.append({"type": Station.StationType.DOCK, "pos": Vector3(26.5, 0, -10.2), "rot": 0.0, "stage": 1})
		defs.append({"type": Station.StationType.SHIP, "pos": Vector3(6.0, 0, -13.2), "rot": 90.0, "stage": 1})
		if GameManager.upgrades.get("conveyor", false):
			defs.append({"type": Station.StationType.CONVEYOR, "pos": Vector3(22.5, 0, -15.5), "rot": -90.0})
	return defs

func rebuild_stations() -> void:
	for c in stations_root.get_children():
		c.queue_free()
	for d in station_defs():
		var st := Station.new()
		st.position = d["pos"]
		st.rotation_degrees.y = float(d.get("rot", 0.0))
		stations_root.add_child(st)
		st.setup(int(d["type"]), d)
		if int(d["type"]) == Station.StationType.NPC_TALK:
			for child in st.get_children():
				if child.has_meta("npc"):
					var npc: NPC = child.get_meta("npc")
					var players := get_tree().get_nodes_in_group("player")
					if not players.is_empty():
						npc.look_at_target = players[0]
	_refresh_belt()

func refresh_world(animate: bool = true) -> void:
	_refresh_brand()
	_refresh_decor()
	_refresh_gate(animate)
	_refresh_customers()
	_refresh_belt()
	if penthouse:
		penthouse.visible = GameManager.lifestyle_owned.get("penthouse", false)

func _refresh_brand() -> void:
	for l in brand_labels:
		l.text = GameManager.brand_name.to_upper()
		l.modulate = Color(1, 1, 1).lerp(GameManager.brand_color, 0.35)
	if brand_labels.size() > 1:
		brand_labels[1].visible = GameManager.location_stage >= 1

func _refresh_gate(animate: bool) -> void:
	var open := GameManager.location_stage >= 1
	sale_sign.visible = not open
	var target_y := 6.3 if open else 2.3
	var target_scale := 0.15 if open else 1.0
	gate_col.disabled = open
	if animate and open and absf(gate.position.y - target_y) > 0.1:
		Audio.play("door")
		var tw := create_tween().set_parallel(true)
		tw.tween_property(gate, "position:y", target_y, 2.4).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
		tw.tween_property(gate, "scale:y", target_scale, 2.4).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	else:
		gate.position.y = target_y
		gate.scale.y = target_scale

func _refresh_decor() -> void:
	for c in decor_root.get_children():
		c.queue_free()
	var owned := {}
	for k in GameManager.decor_owned:
		owned[k] = true
	for k in GameManager.lifestyle_owned:
		owned[k] = true
	for id in owned:
		if not DECOR_SLOTS.has(id):
			continue
		for slot in DECOR_SLOTS[id]:
			var stage := int(slot[0])
			if stage == 1 and GameManager.location_stage < 1:
				continue
			var node := _decor_node(String(id))
			if node == null:
				continue
			node.position = slot[1]
			node.rotation_degrees.y = float(slot[2])
			decor_root.add_child(node)

func _decor_node(id: String) -> Node3D:
	match id:
		"pflanze": return Props.plant(1.4)
		"poster": return Props.poster("HUSTLE\nHARDER", Color(0.12, 0.12, 0.14))
		"stehlampe": return Props.floor_lamp()
		"teppich": return Props.rug(Color(0.55, 0.2, 0.25))
		"billy": return Props.billy()
		"whiteboard": return Props.whiteboard()
		"kaffee": return Props.coffee_machine()
		"sofa": return Props.sofa(Color(0.35, 0.22, 0.15))
		"sneaker":
			var n := Node3D.new()
			n.add_child(Props.box(Vector3(0.5, 0.9, 0.4), Mats.std(Color(0.95, 0.95, 0.95), 0.4), Vector3(0, 0.45, 0)))
			var d := Props.shoe_display()
			d.position = Vector3(0, 0.92, 0)
			n.add_child(d)
			return n
		"gamingstuhl": return Props.gaming_chair()
		"neon": return Props.neon_sign("HUSTLE", Color(1.0, 0.3, 0.8))
		"auto": return Props.car(Color(0.3, 0.45, 0.35))
		"sportwagen": return Props.car(Color(0.9, 0.12, 0.1), true)
	return null

func _refresh_customers() -> void:
	for c in customers_root.get_children():
		c.queue_free()
	var rng := RandomNumberGenerator.new()
	rng.seed = 77 + GameManager.day
	var tables: Array = [2, 3, 4] if GameManager.story_stage == "diner" else [1, 4]
	for t in DINER_TABLES:
		if not tables.has(int(t["id"])):
			continue
		var npc := NPC.new()
		var look := CharacterKit.random_look(rng)
		look["sitting"] = true
		npc.setup(look)
		npc.position = Vector3(t["pos"]) + Vector3(0, 0, 0.62)
		npc.rotation.y = PI
		customers_root.add_child(npc)

# =====================================================================================
# Förderband
# =====================================================================================
func _refresh_belt() -> void:
	for c in belt_root.get_children():
		c.queue_free()
	_belt_boxes.clear()
	if GameManager.location_stage < 1 or not GameManager.upgrades.get("conveyor", false):
		return
	var length := BELT_START.x - BELT_END.x
	var cx := (BELT_START.x + BELT_END.x) / 2.0
	var frame := Mats.std(Color(0.2, 0.45, 0.25), 0.5, 0.4)
	var body := StaticBody3D.new()
	belt_root.add_child(body)
	var col := CollisionShape3D.new()
	var shape := BoxShape3D.new()
	shape.size = Vector3(length, 0.9, 0.8)
	col.shape = shape
	col.position = Vector3(cx, 0.45, BELT_START.z)
	body.add_child(col)
	belt_root.add_child(Props.box(Vector3(length, 0.1, 0.8), frame, Vector3(cx, 0.75, BELT_START.z)))
	belt_root.add_child(Props.box(Vector3(length, 0.04, 0.66), Mats.shader("conveyor", {"speed": -0.9}), Vector3(cx, 0.82, BELT_START.z)))
	for sz in [-0.4, 0.4]:
		belt_root.add_child(Props.box(Vector3(length, 0.1, 0.04), Mats.std(Color(0.95, 0.8, 0.1), 0.5), Vector3(cx, 0.88, BELT_START.z + sz)))
	var legs := int(length / 2.0)
	for i in legs + 1:
		var lx := BELT_END.x + i * (length / legs)
		for sz in [-0.32, 0.32]:
			belt_root.add_child(Props.box(Vector3(0.08, 0.72, 0.08), Mats.dark_metal(), Vector3(lx, 0.36, BELT_START.z + sz)))
	sync_conveyor()

func sync_conveyor() -> void:
	if belt_root == null:
		return
	var alive := {}
	for e in GameManager.conveyor_queue:
		var id := int(e.get("id", 0))
		alive[id] = true
		if _belt_boxes.has(id):
			continue
		var box := ItemKit.build(GameManager.ItemKind.LABELED, e["pkg"])
		box.position = BELT_START + Vector3(0, 0.15, 0)
		belt_root.add_child(box)
		_belt_boxes[id] = box
		var secs := maxf(0.5, (float(e["done_at"]) - GameManager.bclock()) / GameData.MINUTES_PER_SECOND)
		var tw := box.create_tween()
		tw.tween_property(box, "position", BELT_END + Vector3(0, 0.15, 0), secs)
	for id in _belt_boxes.keys():
		if not alive.has(id):
			var b: Node3D = _belt_boxes[id]
			if is_instance_valid(b):
				b.queue_free()
			_belt_boxes.erase(id)

# =====================================================================================
# Personal-Figuren
# =====================================================================================
func refresh_staff() -> void:
	for c in staff_root.get_children():
		c.queue_free()
	if GameManager.location_stage < 1:
		return
	var rng := RandomNumberGenerator.new()
	rng.seed = 555
	var role_seen := {}
	for s in GameManager.staff:
		var role := GameData.staff_role(s["role"])
		var n := int(role_seen.get(s["role"], 0))
		role_seen[s["role"]] = n + 1
		var off := Vector3(n * 0.8, 0, 0)
		var pts: Array = []
		match String(s["role"]):
			"lager":
				pts = [Vector3(26.5, 0, -12.0), Vector3(26.0, 0, -18.5), Vector3(22.8, 0, -27.4), Vector3(17.8, 0, -27.4), Vector3(26.0, 0, -18.5)]
			"packer":
				pts = [Vector3(12.8, 0, -27.4), Vector3(13.0, 0, -21.3), Vector3(7.8, 0, -27.4), Vector3(13.0, 0, -21.3)]
			"versand":
				pts = [Vector3(13.0, 0, -18.9), Vector3(17.5, 0, -18.9), Vector3(6.6, 0, -17.2), Vector3(6.8, 0, -14.2), Vector3(6.6, 0, -17.2)]
			_:
				pts = [Vector3(2.9, 0, -14.6)]
		var shifted: Array = []
		for p in pts:
			shifted.append(p + off)
		var look := CharacterKit.random_look(rng)
		look["shirt"] = role["shirt"]
		look["vest"] = s["role"] != "social"
		var npc := NPC.new()
		npc.setup(look, shifted, 1.6)
		npc.pause_at_points = 0.8
		staff_root.add_child(npc)
		var tag := Props.label("%s · %s" % [s["name"], role["name"]], 30, Color(1, 1, 1, 0.9), Vector3(0, 2.1, 0), true, 6)
		tag.visibility_range_end = 12.0
		npc.add_child(tag)

# =====================================================================================
# Leben auf der Straße
# =====================================================================================
func _pedestrians() -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = 314
	var paths := [
		[Vector3(-50, 0, -6.3), Vector3(40, 0, -6.3)],
		[Vector3(-8, 0, -4.6), Vector3(45, 0, -4.6)],
		[Vector3(-54, 0, 5.3), Vector3(44, 0, 5.3)],
		[Vector3(40, 0, 6.2), Vector3(-50, 0, 6.2)],
		[Vector3(-40, 0, 13.6), Vector3(30, 0, 13.6)],
		[Vector3(20, 0, 13.95), Vector3(-48, 0, 13.95)],
	]
	for p in paths:
		var npc := NPC.new()
		npc.setup(CharacterKit.random_look(rng), p, rng.randf_range(1.0, 1.5))
		npc.ping_pong = true
		npc.pause_at_points = 2.0
		dynamic_root.add_child(npc)
		npc.position = p[0].lerp(p[1], rng.randf())

func _process(delta: float) -> void:
	_traffic_acc -= delta
	if _traffic_acc <= 0.0:
		_traffic_acc = randf_range(5.0, 11.0)
		_spawn_car()

func _spawn_car() -> void:
	var colors := [Color(0.8, 0.1, 0.1), Color(0.15, 0.3, 0.6), Color(0.9, 0.9, 0.9), Color(0.1, 0.1, 0.12),
		Color(0.55, 0.57, 0.6), Color(0.9, 0.7, 0.1), Color(0.2, 0.5, 0.3)]
	var car := Props.car(colors[randi() % colors.size()])
	var east := randf() < 0.5
	var z := 2.0 if east else -2.0
	var from_x := -110.0 if east else 110.0
	car.position = Vector3(from_x, 0, z)
	car.rotation_degrees.y = 0.0 if east else 180.0
	dynamic_root.add_child(car)
	var tw := car.create_tween()
	tw.tween_property(car, "position:x", -from_x, randf_range(14.0, 18.0))
	tw.tween_callback(car.queue_free)

## Lieferwagen fährt vor die aktuelle Anlieferung, hält kurz und fährt weiter.
func send_van() -> void:
	var stop: Vector3 = VAN_STOP[clampi(GameManager.location_stage, 0, 1)]
	var van := Props.van(Color(0.95, 0.78, 0.1), "PaketBlitz")
	van.position = Vector3(stop.x + 60.0, 0, stop.z)
	van.rotation_degrees.y = 180
	dynamic_root.add_child(van)
	var tw := van.create_tween()
	tw.tween_property(van, "position:x", stop.x, 3.2).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tw.tween_callback(func(): Audio.play("truck", 0.05, -4.0))
	tw.tween_interval(2.2)
	tw.tween_property(van, "position:x", stop.x - 80.0, 4.5).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN)
	tw.tween_callback(van.queue_free)

func spawn_float_text(pos: Vector3, text: String, color: Color) -> void:
	var l := Props.label(text, 64, color, pos, true, 12)
	l.no_depth_test = true
	dynamic_root.add_child(l)
	var tw := l.create_tween().set_parallel(true)
	tw.tween_property(l, "position:y", pos.y + 1.2, 1.4).set_ease(Tween.EASE_OUT)
	tw.tween_property(l, "modulate:a", 0.0, 1.4).set_delay(0.4)
	tw.chain().tween_callback(l.queue_free)

func spawn_point() -> Vector3:
	if GameManager.story_stage == "diner":
		return SPAWN_DINER
	return SPAWN_GARAGE if GameManager.location_stage == 0 else SPAWN_WAREHOUSE
