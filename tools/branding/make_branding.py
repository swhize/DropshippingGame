# Erzeugt App-Icon (1024²) und Splash-Logo für Dropshipping Simulator (braucht Pillow: pip install pillow).
# Aufruf: python3 tools/branding/make_branding.py Assets/DropshippingGame/Branding Assets/DropshippingGame/Resources/Fonts [vorschau.png]
import sys, math
from PIL import Image, ImageDraw, ImageFilter, ImageFont
out = sys.argv[1]; fonts = sys.argv[2]
S = 4  # Supersampling
N = 1024 * S

def lerp(a, b, t): return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(len(a)))
def mix(p, q, t): return (p[0] + (q[0] - p[0]) * t, p[1] + (q[1] - p[1]) * t)

# ---------- Icon ----------
img = Image.new("RGBA", (N, N), (0, 0, 0, 0))
bg = Image.new("RGBA", (N, N))
top, bot = (40, 44, 62), (14, 16, 24)
d = ImageDraw.Draw(bg)
for y in range(0, N, S):
    d.rectangle([0, y, N, y + S], fill=lerp(top, bot, y / N) + (255,))
glow = Image.new("RGBA", (N, N), (0, 0, 0, 0))
gd = ImageDraw.Draw(glow)
r = int(N * 0.36)
cx, cy = N // 2, int(N * 0.56)
gd.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(255, 176, 60, 110))
glow = glow.filter(ImageFilter.GaussianBlur(N * 0.08))
bg = Image.alpha_composite(bg, glow)
mask = Image.new("L", (N, N), 0)
m = int(N * 0.035)
ImageDraw.Draw(mask).rounded_rectangle([m, m, N - m, N - m], radius=int(N * 0.2), fill=255)
img.paste(bg, (0, 0), mask)

u = N * 0.25
h = N * 0.25
c = (N * 0.5, N * 0.50)
dy = u * 0.5
T = (c[0], c[1] - dy); R = (c[0] + u, c[1]); B = (c[0], c[1] + dy); L = (c[0] - u, c[1])
def down(p): return (p[0], p[1] + h)
ink = (20, 18, 16, 255)
ow = int(N * 0.018)
left = [L, B, down(B), down(L)]
right = [B, R, down(R), down(B)]
topf = [T, R, B, L]
sh = Image.new("RGBA", (N, N), (0, 0, 0, 0))
ImageDraw.Draw(sh).ellipse([c[0] - u * 1.15, down(B)[1] - dy * 0.45, c[0] + u * 1.15, down(B)[1] + dy * 0.45], fill=(0, 0, 0, 140))
sh = sh.filter(ImageFilter.GaussianBlur(N * 0.02))
img = Image.alpha_composite(img, sh)
d = ImageDraw.Draw(img)
d.polygon(left, fill=(232, 158, 46, 255))
d.polygon(right, fill=(190, 116, 26, 255))
d.polygon(topf, fill=(255, 205, 110, 255))
tw = 0.16
a1, a2 = mix(L, T, 0.5 - tw / 2), mix(L, T, 0.5 + tw / 2)
b1, b2 = mix(B, R, 0.5 - tw / 2), mix(B, R, 0.5 + tw / 2)
d.polygon([a1, a2, b2, b1], fill=(255, 236, 190, 255))
d.polygon([b1, b2, down(b2), down(b1)], fill=(235, 196, 140, 255))
def onleft(s, t):
    p = mix(L, B, s); return (p[0], p[1] + h * t)
lab = [onleft(0.18, 0.30), onleft(0.66, 0.30), onleft(0.66, 0.80), onleft(0.18, 0.80)]
d.polygon(lab, fill=(250, 248, 241, 255))
for i, t in enumerate([0.44, 0.55, 0.66]):
    s2 = 0.58 if i < 2 else 0.42
    d.line([onleft(0.25, t), onleft(s2, t)], fill=(60, 56, 50, 255), width=int(N * 0.013))
for poly in (left, right, topf):
    d.line(poly + [poly[0]], fill=ink, width=ow, joint="curve")

pts = [(N * 0.17, N * 0.43), (N * 0.36, N * 0.29), (N * 0.52, N * 0.35), (N * 0.75, N * 0.15)]
for wcol, wid in ((ink, int(N * 0.078)), ((255, 255, 255, 255), int(N * 0.046))):
    d.line(pts, fill=wcol, width=wid, joint="curve")
    for p in pts[:-1]:
        rr = wid / 2
        d.ellipse([p[0] - rr, p[1] - rr, p[0] + rr, p[1] + rr], fill=wcol)
tip = pts[-1]; prev = pts[-2]
ang = math.atan2(tip[1] - prev[1], tip[0] - prev[0])
def head(scale):
    L0 = N * 0.13 * scale; W0 = N * 0.085 * scale
    t = (tip[0] + math.cos(ang) * L0 * 0.35, tip[1] + math.sin(ang) * L0 * 0.35)
    bx, by = t[0] - math.cos(ang) * L0, t[1] - math.sin(ang) * L0
    nx, ny = -math.sin(ang), math.cos(ang)
    return [t, (bx + nx * W0, by + ny * W0), (bx - nx * W0, by - ny * W0)]
d.polygon(head(1.32), fill=ink)
d.polygon(head(1.0), fill=(255, 255, 255, 255))

icon = img.resize((1024, 1024), Image.LANCZOS)
icon.save(out + "/AppIcon.png", optimize=True)

# ---------- Splash-Logo (Wortmarke) ----------
W, H = 3200, 1040
logo = Image.new("RGBA", (W, H), (0, 0, 0, 0))
ld = ImageDraw.Draw(logo)
f_big = ImageFont.truetype(fonts + "/Display-Bold.ttf", 300)
f_pill = ImageFont.truetype(fonts + "/UI-Bold.ttf", 104)
f_eye = ImageFont.truetype(fonts + "/UI-Bold.ttf", 52)
text = "DROPSHIPPING"
bb = ld.textbbox((0, 0), text, font=f_big)
tw_ = bb[2] - bb[0]
x = (W - tw_) // 2 - bb[0]; y = 170 - bb[1]
eye = "H U S T L E   G A M E S"
eb = ld.textbbox((0, 0), eye, font=f_eye)
ld.text(((W - (eb[2] - eb[0])) // 2 - eb[0], 60), eye, font=f_eye, fill=(160, 168, 188, 255))
ld.text((x + 8, y + 10), text, font=f_big, fill=(0, 0, 0, 110))
ld.text((x, y), text, font=f_big, fill=(255, 255, 255, 255))
pt = "S I M U L A T O R"
pb = ld.textbbox((0, 0), pt, font=f_pill)
pw, ph = pb[2] - pb[0], pb[3] - pb[1]
py = 170 + (bb[3] - bb[1]) + 50
px0 = (W - pw) // 2 - 48
ld.rounded_rectangle([px0, py, px0 + pw + 96, py + ph + 56], radius=26, fill=(255, 189, 66, 255))
ld.text((px0 + 48 - pb[0], py + 28 - pb[1]), pt, font=f_pill, fill=(33, 24, 8, 255))
logo = logo.crop(logo.getbbox())
pad = 24
canvas = Image.new("RGBA", (logo.width + pad * 2, logo.height + pad * 2), (0, 0, 0, 0))
canvas.paste(logo, (pad, pad))
canvas = canvas.resize((canvas.width // 2, canvas.height // 2), Image.LANCZOS)
canvas.save(out + "/SplashLogo.png", optimize=True)

# Vorschau auf dunklem Grund (nur zur Sichtprüfung)
if len(sys.argv) > 3:
    pv = Image.new("RGBA", (1024 + canvas.width + 60, max(1024, canvas.height) + 40), (22, 24, 32, 255))
    pv.alpha_composite(icon, (20, 20))
    pv.alpha_composite(canvas, (1044, 20))
    pv.convert("RGB").resize((pv.width // 2, pv.height // 2)).save(sys.argv[3])
print("ok", icon.size, canvas.size)
