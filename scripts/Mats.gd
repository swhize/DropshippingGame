class_name Mats
extends RefCounted
## Material-Bibliothek: einfache PBR-Materialien und prozedurale Shader (Beton, Asphalt,
## Ziegel, Holzdielen, Fliesen, Gras, Fassaden mit Fenstern, ...). Alles wird per Code
## erzeugt und gecached - es gibt keine Texturdateien im Projekt.

static var _cache: Dictionary = {}
static var _shaders: Dictionary = {}

const COMMON := """
varying vec3 w_pos;
varying vec3 w_nrm;
float hash21(vec2 p) { p = fract(p * vec2(123.34, 456.21)); p += dot(p, p + 45.32); return fract(p.x * p.y); }
float vnoise(vec2 p) {
	vec2 i = floor(p); vec2 f = fract(p); vec2 u = f * f * (3.0 - 2.0 * f);
	float a = hash21(i); float b = hash21(i + vec2(1.0, 0.0));
	float c = hash21(i + vec2(0.0, 1.0)); float d = hash21(i + vec2(1.0, 1.0));
	return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}
float fbm(vec2 p) {
	float v = 0.0; float a = 0.5;
	for (int i = 0; i < 4; i++) { v += a * vnoise(p); p = p * 2.07 + vec2(1.7, 9.2); a *= 0.5; }
	return v;
}
vec2 tri_uv() {
	vec3 n = abs(w_nrm);
	if (n.y >= n.x && n.y >= n.z) { return w_pos.xz; }
	if (n.x >= n.z) { return vec2(w_pos.z, w_pos.y); }
	return vec2(w_pos.x, w_pos.y);
}
void vertex() {
	w_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	w_nrm = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
}
"""

const FRAGMENTS := {
	"concrete": """
uniform vec3 color_a : source_color = vec3(0.56, 0.56, 0.54);
uniform vec3 color_b : source_color = vec3(0.44, 0.44, 0.43);
uniform float scale = 0.35;
uniform float tile = 0.0;
uniform float stain = 0.25;
void fragment() {
	vec2 uv = tri_uv();
	float n = fbm(uv * scale);
	float fine = vnoise(uv * 9.0) * 0.07;
	float st = smoothstep(0.62, 0.8, fbm(uv * 0.12 + 3.0)) * stain;
	vec3 col = mix(color_b, color_a, n) - fine - st * 0.18;
	if (tile > 0.0) {
		vec2 g = abs(fract(uv / tile) - 0.5);
		col *= 1.0 - step(0.475, max(g.x, g.y)) * 0.3;
	}
	ALBEDO = col;
	ROUGHNESS = 0.88 - st * 0.3;
}
""",
	"asphalt": """
uniform vec3 base : source_color = vec3(0.17, 0.17, 0.18);
void fragment() {
	vec2 uv = tri_uv();
	float n = fbm(uv * 0.5);
	float speck = step(0.83, vnoise(uv * 36.0));
	ALBEDO = base * (0.82 + n * 0.35) + speck * 0.05;
	ROUGHNESS = 0.93;
}
""",
	"brick": """
uniform vec3 brick : source_color = vec3(0.62, 0.3, 0.22);
uniform vec3 mortar : source_color = vec3(0.76, 0.73, 0.67);
uniform vec2 size = vec2(0.5, 0.2);
void fragment() {
	vec2 uv = tri_uv() / size;
	float row = floor(uv.y);
	uv.x += mod(row, 2.0) * 0.5;
	vec2 cell = floor(uv);
	vec2 f = fract(uv);
	float m = clamp(step(f.x, 0.035) + step(f.y, 0.08), 0.0, 1.0);
	float h = hash21(cell);
	vec3 bcol = brick * (0.78 + 0.36 * h) * (0.9 + 0.2 * vnoise(tri_uv() * 7.0));
	ALBEDO = mix(bcol, mortar, m);
	ROUGHNESS = 0.9;
}
""",
	"planks": """
uniform vec3 wood : source_color = vec3(0.55, 0.37, 0.23);
uniform float plank_w = 0.2;
void fragment() {
	vec2 uv = tri_uv();
	float idx = floor(uv.x / plank_w);
	float h = hash21(vec2(idx, 3.1));
	float along = uv.y + h * 7.0;
	float seam = clamp(step(0.95, fract(uv.x / plank_w)) + step(0.988, fract(along / 2.4)), 0.0, 1.0);
	float grain = vnoise(vec2(uv.x * 40.0, along * 2.0)) * 0.22 + fbm(vec2(uv.x * 3.0, along * 0.5)) * 0.2;
	vec3 col = wood * (0.72 + 0.3 * h + grain);
	ALBEDO = col * (1.0 - seam * 0.45);
	ROUGHNESS = 0.62;
}
""",
	"tiles": """
uniform vec3 tile_a : source_color = vec3(0.92, 0.92, 0.9);
uniform vec3 tile_b : source_color = vec3(0.13, 0.13, 0.15);
uniform float tile_size = 0.5;
void fragment() {
	vec2 uv = tri_uv() / tile_size;
	vec2 c = floor(uv);
	float chk = mod(c.x + c.y, 2.0);
	vec2 g = abs(fract(uv) - 0.5);
	float grout = step(0.47, max(g.x, g.y));
	vec3 col = mix(tile_a, tile_b, chk);
	col = mix(col, vec3(0.45), grout * 0.6);
	col *= 0.93 + 0.07 * vnoise(uv * 3.0);
	ALBEDO = col;
	ROUGHNESS = mix(0.22, 0.8, grout);
}
""",
	"grass": """
uniform vec3 g1 : source_color = vec3(0.27, 0.48, 0.2);
uniform vec3 g2 : source_color = vec3(0.42, 0.6, 0.27);
void fragment() {
	vec2 uv = tri_uv();
	float n = fbm(uv * 0.35);
	float d = vnoise(uv * 14.0);
	ALBEDO = mix(g1, g2, n) * (0.88 + d * 0.22);
	ROUGHNESS = 0.96;
}
""",
	"metal_sheet": """
uniform vec3 base : source_color = vec3(0.47, 0.49, 0.52);
uniform float ribs = 22.0;
void fragment() {
	vec2 uv = tri_uv();
	vec3 n = abs(w_nrm);
	float coord = n.y > 0.5 ? uv.x : uv.x;
	float r = sin(coord * ribs) * 0.5 + 0.5;
	float grime = fbm(uv * 0.4);
	float streak = vnoise(vec2(uv.x * 6.0, uv.y * 0.3));
	ALBEDO = base * (0.78 + 0.2 * r) * (0.82 + 0.3 * grime) * (0.92 + 0.1 * streak);
	METALLIC = 0.55;
	ROUGHNESS = 0.5 + grime * 0.2;
}
""",
	"facade": """
global uniform float night_amount;
uniform vec3 wall : source_color = vec3(0.7, 0.66, 0.6);
uniform vec2 win = vec2(3.0, 3.2);
uniform float seed = 0.0;
uniform float lit_ratio = 0.45;
void fragment() {
	vec2 uv = tri_uv();
	vec3 an = abs(w_nrm);
	vec2 g = uv / win;
	vec2 cell = floor(g);
	vec2 f = fract(g);
	float is_win = step(0.24, f.x) * step(f.x, 0.76) * step(0.3, f.y) * step(f.y, 0.82);
	if (an.y > 0.5 || w_pos.y < 1.2) { is_win = 0.0; }
	float lit = step(1.0 - lit_ratio, hash21(cell + seed));
	vec3 glass = mix(vec3(0.16, 0.21, 0.27), vec3(0.34, 0.43, 0.52), f.y);
	vec3 col = mix(wall * (0.85 + 0.15 * vnoise(uv * 2.0)), glass, is_win);
	ALBEDO = col;
	ROUGHNESS = mix(0.9, 0.15, is_win);
	EMISSION = vec3(1.0, 0.8, 0.5) * is_win * lit * night_amount * 2.4;
}
""",
	"window": """
global uniform float night_amount;
uniform vec3 glow : source_color = vec3(1.0, 0.8, 0.52);
uniform float interior = 0.0;
void fragment() {
	float fres = pow(1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0), 3.0);
	vec2 uv = tri_uv();
	float frame = step(0.94, fract(uv.x / 1.2 + 0.03)) + step(0.94, fract(uv.y / 1.1 + 0.03));
	vec3 day = mix(vec3(0.22, 0.3, 0.38) + fres * 0.45, vec3(0.75, 0.87, 1.0), interior);
	vec3 col = mix(day, vec3(0.06, 0.07, 0.09), night_amount * (1.0 - interior * 0.3));
	ALBEDO = mix(col, vec3(0.2), clamp(frame, 0.0, 1.0));
	ROUGHNESS = 0.06;
	METALLIC = 0.2;
	vec3 e_night = glow * (1.0 - interior) * 1.9;
	vec3 e_day = vec3(0.55, 0.72, 0.9) * interior * 0.9;
	EMISSION = mix(e_day, e_night, night_amount) * (1.0 - clamp(frame, 0.0, 1.0));
}
""",
	"lamp": """
global uniform float night_amount;
uniform vec3 glow : source_color = vec3(1.0, 0.85, 0.6);
uniform float always_on = 0.0;
void fragment() {
	ALBEDO = vec3(0.9);
	EMISSION = glow * (0.15 + max(night_amount * 4.0, always_on * 1.5));
}
""",
	"conveyor": """
uniform float speed = 0.9;
void fragment() {
	vec2 uv = tri_uv();
	float s = step(0.5, fract(w_pos.x * 3.0 - TIME * speed));
	ALBEDO = vec3(0.07) + s * 0.05;
	ROUGHNESS = 0.8;
}
""",
	"flat": """
uniform vec3 col : source_color = vec3(0.3, 0.55, 0.25);
uniform float variation = 0.12;
void fragment() {
	vec3 n = normalize(cross(dFdx(VERTEX), dFdy(VERTEX)));
	if (dot(n, VIEW) < 0.0) { n = -n; }
	NORMAL = n;
	ALBEDO = col * (1.0 - variation + variation * 2.0 * hash21(floor(w_pos.xz * 2.0) + floor(w_pos.y * 2.0)));
	ROUGHNESS = 0.9;
}
""",
	"cardboard": """
uniform vec3 base : source_color = vec3(0.68, 0.51, 0.33);
void fragment() {
	vec2 uv = tri_uv();
	float n = vnoise(uv * 18.0) * 0.08 + fbm(uv * 2.0) * 0.1;
	float corr = sin(uv.x * 140.0) * 0.015;
	ALBEDO = base * (0.9 + n + corr);
	ROUGHNESS = 0.92;
}
""",
}

const HIGHLIGHT_CODE := """
shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_back;
uniform vec4 rim : source_color = vec4(1.0, 0.72, 0.25, 1.0);
void fragment() {
	float f = pow(1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0), 2.2);
	float pulse = 0.75 + 0.25 * sin(TIME * 5.0);
	ALBEDO = rim.rgb * (f * 0.7 + 0.07) * pulse;
}
"""

static func _get_shader(kind: String) -> Shader:
	if _shaders.has(kind):
		return _shaders[kind]
	var sh := Shader.new()
	if kind == "highlight":
		sh.code = HIGHLIGHT_CODE
	else:
		sh.code = "shader_type spatial;\nrender_mode diffuse_burley, specular_schlick_ggx;\n" + COMMON + FRAGMENTS[kind]
	_shaders[kind] = sh
	return sh

## Prozedurales Shader-Material (gecached nach Art + Parametern).
static func shader(kind: String, params: Dictionary = {}) -> ShaderMaterial:
	var key := "sh:%s:%s" % [kind, str(params)]
	if _cache.has(key):
		return _cache[key]
	var m := ShaderMaterial.new()
	m.shader = _get_shader(kind)
	for k in params:
		m.set_shader_parameter(k, params[k])
	_cache[key] = m
	return m

## Einfaches PBR-Material.
static func std(color: Color, roughness: float = 0.8, metallic: float = 0.0) -> StandardMaterial3D:
	var key := "std:%s:%.2f:%.2f" % [str(color), roughness, metallic]
	if _cache.has(key):
		return _cache[key]
	var m := StandardMaterial3D.new()
	m.albedo_color = color
	m.roughness = roughness
	m.metallic = metallic
	if color.a < 0.99:
		m.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	_cache[key] = m
	return m

## Selbstleuchtendes Material (Neon, Displays, Lampen).
static func emit(color: Color, energy: float = 2.0) -> StandardMaterial3D:
	var key := "emit:%s:%.2f" % [str(color), energy]
	if _cache.has(key):
		return _cache[key]
	var m := StandardMaterial3D.new()
	m.albedo_color = color
	m.emission_enabled = true
	m.emission = color
	m.emission_energy_multiplier = energy
	_cache[key] = m
	return m

static func highlight() -> ShaderMaterial:
	return shader("highlight")

static func glass(tint: Color = Color(0.7, 0.85, 0.95, 0.25)) -> StandardMaterial3D:
	var key := "glass:%s" % str(tint)
	if _cache.has(key):
		return _cache[key]
	var m := StandardMaterial3D.new()
	m.albedo_color = tint
	m.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	m.roughness = 0.05
	m.metallic = 0.3
	_cache[key] = m
	return m

## Häufig genutzte Materialien mit sprechendem Namen
static func metal() -> StandardMaterial3D:
	return std(Color(0.6, 0.62, 0.65), 0.35, 0.8)

static func dark_metal() -> StandardMaterial3D:
	return std(Color(0.18, 0.19, 0.21), 0.45, 0.7)

static func wood() -> ShaderMaterial:
	return shader("planks", {"wood": Color(0.6, 0.42, 0.26), "plank_w": 0.12})

static func cardboard() -> ShaderMaterial:
	return shader("cardboard")

static func set_night(v: float) -> void:
	RenderingServer.global_shader_parameter_set("night_amount", v)

## Beim Beenden aufräumen, damit keine Render-Ressourcen hängen bleiben.
static func clear_cache() -> void:
	_cache.clear()
	_shaders.clear()
