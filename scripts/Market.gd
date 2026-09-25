extends Node
## Markt (Autoload): Konkurrenz-Shops mit eigenen Preisen je Produkt (bestimmen die
## Nachfrage mit) + Trading-System mit drei fiktiven Anlagen (Krypto, Meme-Aktie, ETF).

signal trading_changed

const COMPETITORS := [
	{"name": "BilligBoy24", "factor": 0.86, "desc": "Billig, schnell, fragwürdig."},
	{"name": "TrendHaus", "factor": 1.06, "desc": "Hip, teuer, schöne Fotos."},
	{"name": "AliExpresso", "factor": 0.94, "desc": "Lieferzeit: ja."},
]
const ASSETS := [
	{"id": "DROP", "name": "DROPCOIN", "start": 12.0, "vol": 0.05, "drift": 0.0004, "icon": "🪙",
		"desc": "Hochriskante Krypto. Kann alles – vor allem abstürzen."},
	{"id": "GAME", "name": "GameShop AG", "start": 35.0, "vol": 0.025, "drift": 0.0004, "icon": "🎮",
		"desc": "Meme-Aktie. Das Internet liebt sie. Meistens."},
	{"id": "ETF", "name": "Welt-ETF", "start": 100.0, "vol": 0.006, "drift": 0.0008, "icon": "🌍",
		"desc": "Langweilig. Und genau deshalb solide."},
]
const TICK_SECONDS := 2.0
const FEE := 0.01
const HISTORY := 90

var comp_prices: Array = []    # je Konkurrent: {pid: preis}
var comp_mods: Dictionary = {} # pid -> {mult, until}
var prices: Dictionary = {}
var history: Dictionary = {}
var holdings: Dictionary = {}
var invested: Dictionary = {}
var _acc: float = 0.0

func _ready() -> void:
	reset_state()

func reset_state() -> void:
	comp_prices = []
	for c in COMPETITORS:
		var d := {}
		for p in GameData.PRODUCTS:
			d[p["id"]] = int(round(float(p["ref_price"]) * float(c["factor"]) * randf_range(0.95, 1.05)))
		comp_prices.append(d)
	comp_mods = {}
	prices = {}
	history = {}
	holdings = {}
	invested = {}
	for a in ASSETS:
		var id: String = a["id"]
		prices[id] = float(a["start"])
		history[id] = [float(a["start"])]
		holdings[id] = 0.0
		invested[id] = 0.0
	for i in 60:
		_tick(false)

func _process(delta: float) -> void:
	if not GameManager.in_game or not GameManager.is_open():
		return
	_acc += delta
	if _acc >= TICK_SECONDS:
		_acc = 0.0
		_tick(true)

func _tick(emit: bool) -> void:
	for a in ASSETS:
		var id: String = a["id"]
		var p: float = float(prices[id])
		var shock := randfn(0.0, float(a["vol"]))
		if id == "DROP" and randf() < 0.012:
			shock += randf_range(-0.22, 0.26)
		p = maxf(p * exp(float(a["drift"]) + shock), 0.05)
		prices[id] = p
		var h: Array = history[id]
		h.append(p)
		if h.size() > HISTORY:
			h.pop_front()
	if emit:
		trading_changed.emit()

# ---- Konkurrenz -----------------------------------------------------------------------
func competitor_price(ci: int, pid: String) -> float:
	var price := float(comp_prices[ci].get(pid, 50))
	if ci == 0 and comp_mods.has(pid):
		price *= float(comp_mods[pid]["mult"])
	return price

func market_price(pid: String) -> float:
	var best := 999999.0
	for i in comp_prices.size():
		best = minf(best, competitor_price(i, pid))
	return best

func new_day() -> void:
	for i in comp_prices.size():
		var c: Dictionary = COMPETITORS[i]
		for p in GameData.PRODUCTS:
			var target := float(p["ref_price"]) * float(c["factor"])
			var cur := float(comp_prices[i][p["id"]])
			cur = lerpf(cur, target, 0.3) * randf_range(0.93, 1.07)
			comp_prices[i][p["id"]] = int(round(maxf(cur, 2.0)))
	for pid in comp_mods.keys():
		if int(comp_mods[pid]["until"]) < GameManager.day:
			comp_mods.erase(pid)

func apply_price_war(pid: String, mult: float, days: int) -> void:
	comp_mods[pid] = {"mult": mult, "until": GameManager.day + days - 1}

# ---- Trading ------------------------------------------------------------------------------
func asset(id: String) -> Dictionary:
	for a in ASSETS:
		if a["id"] == id:
			return a
	return ASSETS[0]

func buy(id: String, amount: int) -> bool:
	if amount <= 0:
		return false
	if GameManager.money < amount:
		GameManager.notify("Nicht genug Geld für diesen Kauf.", "bad")
		Audio.play("error")
		return false
	var units := float(amount) * (1.0 - FEE) / float(prices[id])
	holdings[id] = float(holdings[id]) + units
	invested[id] = float(invested[id]) + amount
	GameManager.adjust_money(-amount, "trading")
	Audio.play("click")
	trading_changed.emit()
	return true

func sell(id: String, fraction: float) -> int:
	var f := clampf(fraction, 0.0, 1.0)
	var units := float(holdings[id]) * f
	if units <= 0.0:
		return 0
	var value := int(round(units * float(prices[id]) * (1.0 - FEE)))
	if f >= 0.999:
		holdings[id] = 0.0
		invested[id] = 0.0
	else:
		holdings[id] = float(holdings[id]) - units
		invested[id] = float(invested[id]) * (1.0 - f)
	GameManager.adjust_money(value, "trading")
	Audio.play("cash", 0.02, -6.0)
	trading_changed.emit()
	return value

func holding_value(id: String) -> float:
	return float(holdings[id]) * float(prices[id])

func portfolio_value() -> int:
	var v := 0.0
	for a in ASSETS:
		v += holding_value(a["id"])
	return int(round(v))

func profit(id: String) -> float:
	return holding_value(id) - float(invested[id])

func change_pct(id: String, ticks: int = 30) -> float:
	var h: Array = history[id]
	if h.size() < 2:
		return 0.0
	var old := float(h[maxi(0, h.size() - 1 - ticks)])
	return float(h[h.size() - 1]) / maxf(old, 0.0001) - 1.0

func shock(id: String, mult: float) -> void:
	prices[id] = maxf(float(prices[id]) * mult, 0.05)
	var h: Array = history[id]
	h.append(prices[id])
	trading_changed.emit()

func to_dict() -> Dictionary:
	return {"comp_prices": comp_prices, "comp_mods": comp_mods, "prices": prices, "history": history,
		"holdings": holdings, "invested": invested}

func from_dict(d: Dictionary) -> void:
	if d.is_empty():
		return
	var cp: Array = d.get("comp_prices", [])
	if cp.size() == COMPETITORS.size():
		for i in cp.size():
			for p in GameData.PRODUCTS:
				if cp[i].has(p["id"]):
					comp_prices[i][p["id"]] = int(cp[i][p["id"]])
	comp_mods = d.get("comp_mods", {})
	for a in ASSETS:
		var id: String = a["id"]
		if d.get("prices", {}).has(id):
			prices[id] = float(d["prices"][id])
		if d.get("history", {}).has(id):
			var h: Array = []
			for v in d["history"][id]:
				h.append(float(v))
			history[id] = h
		if d.get("holdings", {}).has(id):
			holdings[id] = float(d["holdings"][id])
		if d.get("invested", {}).has(id):
			invested[id] = float(d["invested"][id])
