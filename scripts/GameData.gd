class_name GameData
extends RefCounted
## Alle festen Spieldaten an einem Ort: Katalog, Balancing, Freischaltungen und Texte.
## Reine Konstanten - der veränderliche Spielzustand liegt im GameManager.

# ---- Zeit -------------------------------------------------------------------------
const DAY_START := 480.0            # 08:00
const DAY_END := 1200.0             # 20:00
const BUSINESS_MINUTES := 720.0     # ein Geschäftstag = 12 Spielstunden
const DAY_LENGTH_SECONDS := 540.0   # ... dauert 9 echte Minuten
const MINUTES_PER_SECOND := BUSINESS_MINUTES / DAY_LENGTH_SECONDS
const INTRO_TIME := 1050.0          # 17:30 - Schicht im Imbiss

# ---- Start, Standorte, Kosten -------------------------------------------------------
const START_CAPITAL := 150
const BANKRUPT_LIMIT := -500
const STAGE_NAMES := ["Garage", "Lagerhalle"]
const STAGE_RENT := [15, 140]
const STAGE_CAPACITY := [400, 3000]
const STAGE_QUEUE := [6, 14]
const GARAGE_PRODUCTS := 3

# ---- Nachfrage & Lieferung -----------------------------------------------------------
const BASE_ORDER_MINUTES := 72.0
const ORDER_EXPIRE_MINUTES := 720.0
const PROMO_DISCOUNT := 0.2
const BASE_LEAD_MINUTES := 45.0
const EXPRESS_SURCHARGE := 8
const REVIEW_CHANCE := 0.4

# ---- Produkte -------------------------------------------------------------------------
# size: Kartongröße (0 = S, 1 = M, 2 = L), popularity: Grundnachfrage,
# unit_cost: Stückpreis beim Standard-Anbieter, ref_price: typischer Marktpreis
const PRODUCTS := [
	{"id": "huelle", "name": "Handyhülle", "short": "Hülle", "unit_cost": 2.0, "ref_price": 25,
		"size": 0, "popularity": 1.2, "unlock_level": 1, "color": Color(0.93, 0.42, 0.62), "icon": "📱"},
	{"id": "led", "name": "LED-Lichterkette", "short": "LED", "unit_cost": 4.0, "ref_price": 45,
		"size": 1, "popularity": 1.0, "unlock_level": 2, "color": Color(0.98, 0.8, 0.24), "icon": "💡"},
	{"id": "massage", "name": "Massagepistole", "short": "Massage", "unit_cost": 12.0, "ref_price": 110,
		"size": 2, "popularity": 0.55, "unlock_level": 3, "color": Color(0.32, 0.34, 0.42), "icon": "💪"},
	{"id": "ringlicht", "name": "Ringlicht", "short": "Ringlicht", "unit_cost": 6.0, "ref_price": 60,
		"size": 1, "popularity": 0.85, "unlock_level": 5, "color": Color(0.82, 0.88, 1.0), "icon": "⭕"},
	{"id": "katzenbrunnen", "name": "Katzen-Trinkbrunnen", "short": "Katzenbr.", "unit_cost": 9.0, "ref_price": 79,
		"size": 2, "popularity": 0.7, "unlock_level": 6, "color": Color(0.36, 0.78, 0.84), "icon": "🐱"},
	{"id": "haltung", "name": "Haltungskorrektor", "short": "Haltung", "unit_cost": 3.0, "ref_price": 35,
		"size": 0, "popularity": 1.1, "unlock_level": 7, "color": Color(0.58, 0.46, 0.9), "icon": "🧍"},
]

const BULK_OPTIONS := [
	{"name": "Klein", "quantity": 20, "discount": 1.0, "level": 1, "stage": 0},
	{"name": "Mittel", "quantity": 50, "discount": 0.85, "level": 1, "stage": 0},
	{"name": "Groß", "quantity": 200, "discount": 0.68, "level": 2, "stage": 0},
	{"name": "Palette", "quantity": 500, "discount": 0.55, "level": 5, "stage": 1},
]

const SUPPLIERS := [
	{"name": "Billig-Fabrik", "quality": 0.6, "price_mult": 0.65, "leadtime_mult": 1.8, "level": 1,
		"desc": "Spottbillig, aber langsam und die Ware ist... naja."},
	{"name": "Standard-Großhändler", "quality": 1.0, "price_mult": 1.0, "leadtime_mult": 1.0, "level": 1,
		"desc": "Solide Qualität, normale Lieferzeit."},
	{"name": "Premium-Hersteller", "quality": 1.6, "price_mult": 1.75, "leadtime_mult": 0.55, "level": 2,
		"desc": "Teuer, blitzschnell, Kunden lieben es."},
]

# ---- Verpackung -------------------------------------------------------------------------
const PACKAGING_SIZES := [
	{"name": "S", "cost": 15},
	{"name": "M", "cost": 22},
	{"name": "L", "cost": 32},
]
const PACKAGING_BATCHES := [
	{"qty": 10, "mult": 1.0},
	{"qty": 50, "mult": 4.0},
]
const PACKAGING_FLAT_DISCOUNT := 0.5

# ---- Marketing -----------------------------------------------------------------------------
const AD_TIERS := [
	{"id": "flyer", "name": "Flyer & Plakate", "cost": 15, "mult": 1.25, "minutes": 90.0, "awareness": 0.02, "level": 1, "icon": "📄"},
	{"id": "facebook", "name": "Facebook-Ads", "cost": 60, "mult": 1.6, "minutes": 120.0, "awareness": 0.05, "level": 2, "icon": "👍"},
	{"id": "google", "name": "Google-Ads", "cost": 160, "mult": 2.0, "minutes": 180.0, "awareness": 0.08, "level": 6, "icon": "🔎"},
	{"id": "influencer", "name": "Influencer-Kampagne", "cost": 650, "mult": 2.8, "minutes": 240.0, "awareness": 0.15, "level": 7, "icon": "🤳"},
]
const TIKTOK_LEVEL := 2
const TIKTOK_COOLDOWN := 120.0
const TIKTOK_MIN_MULT := 1.2
const TIKTOK_MAX_MULT := 2.4
const TIKTOK_MIN_MINUTES := 60.0
const TIKTOK_MAX_MINUTES := 180.0

# ---- Branding --------------------------------------------------------------------------------
const BRAND_PALETTE := [
	Color(0.9, 0.26, 0.26), Color(0.95, 0.52, 0.18), Color(0.96, 0.78, 0.2), Color(0.3, 0.78, 0.42),
	Color(0.2, 0.7, 0.72), Color(0.22, 0.5, 0.9), Color(0.5, 0.36, 0.88), Color(0.9, 0.36, 0.7),
	Color(0.14, 0.14, 0.16), Color(0.95, 0.95, 0.95),
]
const LOGO_NAMES := ["Kreis", "Quadrat", "Raute", "Stern", "Blitz", "Herz"]

# ---- Fortschritt ------------------------------------------------------------------------------
const LEVEL_XP := [0, 150, 500, 1200, 2500, 4500, 7500, 12000, 18000, 26000]
const MAX_LEVEL := 10
const LEVEL_UNLOCKS := {
	2: "LED-Lichterkette · Premium-Hersteller · Facebook-Ads · TikTok · Großbestellung",
	3: "Massagepistole · Trading-App · Shop-Server-Upgrade",
	4: "Lagerhalle kaufbar",
	5: "Ringlicht · Personal · Palettenbestellung · eigener Lieferwagen",
	6: "Katzen-Trinkbrunnen · Google-Ads · Förderband",
	7: "Haltungskorrektor · Influencer-Kampagnen",
	8: "Hochregal-Erweiterung",
	9: "Ruhm und Ehre",
	10: "Legendenstatus",
}
const APP_LEVELS := {"Trading": 3, "Personal": 5}

const UPGRADES := [
	{"id": "server", "name": "Shop-Server-Upgrade", "icon": "🖥", "cost": 800, "level": 3, "requires": "",
		"desc": "+6 Plätze in der Bestell-Warteschlange. Weniger verlorene Bestellungen."},
	{"id": "warehouse", "name": "Lagerhalle kaufen", "icon": "🏭", "cost": 4500, "level": 4, "requires": "",
		"desc": "Endlich raus aus der Garage: 3.000 Lagerplätze, 6 Regale, Platz für Personal. Miete 140 €/Tag."},
	{"id": "van", "name": "Eigener Lieferwagen", "icon": "🚐", "cost": 3500, "level": 5, "requires": "",
		"desc": "Alle Einkäufe kommen 25 % schneller an."},
	{"id": "conveyor", "name": "Förderband", "icon": "⚙", "cost": 1500, "level": 6, "requires": "warehouse",
		"desc": "Etikettierte Pakete aufs Band legen - sie werden automatisch verschickt. 10 €/Tag Strom."},
	{"id": "highrack", "name": "Hochregal-Erweiterung", "icon": "🏗", "cost": 2500, "level": 8, "requires": "warehouse",
		"desc": "+3.000 Lagerplätze."},
]

const DECOR := [
	{"id": "pflanze", "name": "Zimmerpflanze FLÖRP", "cost": 15},
	{"id": "poster", "name": "Motivationsposter", "cost": 20},
	{"id": "stehlampe", "name": "Stehlampe GLÜMP", "cost": 30},
	{"id": "teppich", "name": "Teppich LÅNGSAM", "cost": 35},
	{"id": "billy", "name": "BILLY-Regal", "cost": 40},
	{"id": "whiteboard", "name": "Whiteboard mit Businessplan", "cost": 60},
	{"id": "kaffee", "name": "Siebträgermaschine", "cost": 180},
	{"id": "sofa", "name": "Ledersofa (nur Lagerhalle)", "cost": 250},
]

const LIFESTYLE := [
	{"id": "sneaker", "name": "Designer-Sneaker", "cost": 150, "desc": "Stehen in der Vitrine. Zum Anschauen, nicht zum Tragen."},
	{"id": "gamingstuhl", "name": "Gaming-Stuhl", "cost": 300, "desc": "RGB macht 20 % produktiver. Angeblich."},
	{"id": "neon", "name": "Neon-Schild 'HUSTLE'", "cost": 400, "desc": "Leuchtet dich jeden Morgen motivierend an."},
	{"id": "auto", "name": "Gebrauchter Kombi", "cost": 1500, "desc": "Parkt vor der Garage. TÜV bis nächsten Monat."},
	{"id": "uhr", "name": "Protzige Uhr", "cost": 5000, "desc": "Deine Uhrzeit-Anzeige wird golden."},
	{"id": "sportwagen", "name": "Sportwagen", "cost": 25000, "desc": "Parkt vor der Lagerhalle. Nachbarn gucken."},
	{"id": "penthouse", "name": "Penthouse mit Stadtblick", "cost": 80000, "desc": "Das oberste Stockwerk im Hochhaus gegenüber leuchtet golden."},
]

const GOALS := [
	{"id": "first_sale", "title": "Der erste Verkauf", "desc": "Verschicke dein erstes Paket.", "type": "shipped", "target": 1, "reward": 50},
	{"id": "brand", "title": "Eine echte Marke", "desc": "Gib deinem Shop in der Branding-App einen eigenen Namen.", "type": "brand", "target": 1, "reward": 25},
	{"id": "ship10", "title": "Läuft bei dir", "desc": "Verschicke 10 Pakete.", "type": "shipped", "target": 10, "reward": 100},
	{"id": "rev1k", "title": "Vierstellig", "desc": "Erreiche 1.000 € Umsatz.", "type": "revenue", "target": 1000, "reward": 150},
	{"id": "rating4", "title": "Kundenliebling", "desc": "Erreiche eine Bewertung von 4,0 Sternen.", "type": "rating", "target": 4.0, "reward": 200},
	{"id": "ship100", "title": "Paketprofi", "desc": "Verschicke 100 Pakete.", "type": "shipped", "target": 100, "reward": 300},
	{"id": "warehouse", "title": "Raus aus der Garage", "desc": "Kaufe die Lagerhalle (Ausbau-App).", "type": "stage", "target": 1, "reward": 500},
	{"id": "staff1", "title": "Chef sein", "desc": "Stelle deinen ersten Mitarbeiter ein.", "type": "staff", "target": 1, "reward": 200},
	{"id": "rev10k", "title": "Fünfstellig", "desc": "Erreiche 10.000 € Umsatz.", "type": "revenue", "target": 10000, "reward": 1000},
	{"id": "level7", "title": "Volles Sortiment", "desc": "Erreiche Firmenlevel 7.", "type": "level", "target": 7, "reward": 1500},
	{"id": "staff4", "title": "Kleines Team", "desc": "Beschäftige 4 Mitarbeiter gleichzeitig.", "type": "staff", "target": 4, "reward": 1000},
	{"id": "rev100k", "title": "Imperium", "desc": "Erreiche 100.000 € Umsatz. Kalle wird staunen.", "type": "revenue", "target": 100000, "reward": 10000, "final": true},
]

const STAFF_ROLES := [
	{"id": "lager", "name": "Lagerist:in", "icon": "📦", "wage": 60, "interval": 16.0, "max": 2,
		"desc": "Holt Kisten vom Wareneingang und räumt sie ins passende Regal.", "shirt": Color(0.25, 0.45, 0.8)},
	{"id": "packer", "name": "Packer:in", "icon": "🧰", "wage": 70, "interval": 12.0, "max": 2,
		"desc": "Kommissioniert offene Bestellungen und verpackt sie (braucht Kartons!).", "shirt": Color(0.3, 0.65, 0.4)},
	{"id": "versand", "name": "Versandkraft", "icon": "🚚", "wage": 65, "interval": 10.0, "max": 2,
		"desc": "Etikettiert fertige Pakete und verschickt sie.", "shirt": Color(0.85, 0.5, 0.2)},
	{"id": "social", "name": "Social-Media-Praktikant:in", "icon": "🤳", "wage": 45, "interval": 0.0, "max": 1,
		"desc": "Postet jeden Vormittag ein TikTok. Mal viral, mal peinlich.", "shirt": Color(0.75, 0.35, 0.75)},
]
const STAFF_NAMES := ["Jonas", "Leonie", "Mehmet", "Sophie", "Luca", "Aylin", "Finn", "Mia", "Kevin",
	"Chantal", "Emre", "Lena", "Tim", "Sara", "Nico", "Paula"]

# ---- Bewertungen ---------------------------------------------------------------------------
const REVIEW_TEXTS := {
	5: ["Top! Kam super schnell an.", "Genau wie beschrieben, gerne wieder.", "Meine Oma liebt es. 10/10.",
		"Die Verpackung allein ist schon ein Erlebnis.", "Schneller als der Pizzadienst!"],
	4: ["Gutes Produkt, Versand okay.", "Macht, was es soll.", "Solide. Karton war etwas zerdrückt.", "Würde wieder kaufen."],
	3: ["Naja. Hatte es mir größer vorgestellt.", "Ganz okay für den Preis.", "Hat gedauert, ist aber angekommen."],
	2: ["Riecht irgendwie nach Plastik.", "Hat ewig gedauert.", "Anleitung nur auf Chinesisch."],
	1: ["Nie angekommen. Nie wieder!", "Nach fünf Minuten kaputt.", "Ich will mein Geld zurück!!!", "Sieht nicht aus wie auf dem Foto."],
}
const REVIEW_NAMES := ["Sabine K.", "Dennis", "Oma Gerda", "xX_Gamer_Xx", "Jürgen aus Bottrop", "Laura M.",
	"Tobi", "Frau Schmidt", "Ben", "Anonym", "Kevin H.", "Mareike"]

# ---- Tutorial --------------------------------------------------------------------------------
const TUTORIAL := [
	{"title": "Willkommen in deiner Garage", "text": "Geh zum Laptop auf der Werkbank und öffne ihn."},
	{"title": "Ware einkaufen", "text": "Öffne im Laptop die App 'Einkauf' und bestelle 20 Handyhüllen."},
	{"title": "Lieferung annehmen", "text": "Der Lieferwagen kommt gleich. Nimm die Kiste an der Lieferpalette neben dem Tor."},
	{"title": "Einlagern", "text": "Bring die Kiste zum Regal 'Handyhülle' und räume sie ein."},
	{"title": "Online gehen", "text": "Öffne am Laptop den 'Webshop' und stell die Handyhülle online."},
	{"title": "Erste Bestellung", "text": "Warte auf deine erste Bestellung. Sie erscheint oben links."},
	{"title": "Kommissionieren", "text": "Nimm eine Handyhülle für die Bestellung aus dem Regal."},
	{"title": "Verpacken", "text": "Verpacke die Hülle am Packtisch in einen Karton."},
	{"title": "Etikettieren", "text": "Nimm das Paket und druck am Labeldrucker ein Versandlabel."},
	{"title": "Versenden", "text": "Bring das Paket zur gelben PaketBlitz-Box vor der Garage."},
]

const KALLE_IDLE := [
	"Na, Herr Unternehmer? Schon Millionär?",
	"Die Fritteuse vermisst dich. Ich nicht.",
	"Willst du 'nen Döner? Geht aufs Haus. Ausnahmsweise.",
	"Mein Neffe macht auch Internet. Der verkauft Socken.",
	"Früher hattest du Fett an den Fingern. Heute Kartonstaub.",
]

# ---- Hilfsfunktionen ------------------------------------------------------------------------
static func product(id: String) -> Dictionary:
	for p in PRODUCTS:
		if p["id"] == id:
			return p
	return PRODUCTS[0]

static func product_index(id: String) -> int:
	for i in PRODUCTS.size():
		if PRODUCTS[i]["id"] == id:
			return i
	return 0

static func staff_role(id: String) -> Dictionary:
	for r in STAFF_ROLES:
		if r["id"] == id:
			return r
	return STAFF_ROLES[0]

static func upgrade(id: String) -> Dictionary:
	for u in UPGRADES:
		if u["id"] == id:
			return u
	return {}

static func find_by_id(list: Array, id: String) -> Dictionary:
	for e in list:
		if e["id"] == id:
			return e
	return {}

static func level_threshold(lvl: int) -> int:
	return LEVEL_XP[clampi(lvl - 1, 0, LEVEL_XP.size() - 1)]

static func size_name(size: int) -> String:
	return PACKAGING_SIZES[clampi(size, 0, 2)]["name"]
