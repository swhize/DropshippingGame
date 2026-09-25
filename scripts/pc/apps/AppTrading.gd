extends PCApp
## Trading: fiktive Krypto, Meme-Aktie und ETF kaufen/verkaufen, Live-Kurs.

static var sel: String = "DROP"
var chart: LineChart = null
var price_label: Label = null
var hold_label: Label = null
var pl_label: Label = null

func build() -> void:
	chart = null
	var gm := GameManager
	var p := page()
	header(p, "📈 Trading", "Riskant! Kurse schwanken ständig, 1 % Gebühr pro Kauf und Verkauf. Nur Geld einsetzen, das du nicht fürs Geschäft brauchst.")
	var top := hbox(p, 12)
	stat(top, "Portfolio-Wert", money(Market.portfolio_value()))
	stat(top, "Kontostand", money(gm.money))
	var total_pl := 0.0
	for a in Market.ASSETS:
		total_pl += Market.profit(a["id"])
	stat(top, "Gewinn/Verlust", money(int(round(total_pl))), UITheme.GOOD if total_pl >= 0.0 else UITheme.BAD)
	var h := hbox(p, 14)
	var list := vbox(h, 8)
	list.custom_minimum_size = Vector2(250, 0)
	for a in Market.ASSETS:
		var id: String = a["id"]
		var ch := Market.change_pct(id)
		var b := Button.new()
		b.alignment = HORIZONTAL_ALIGNMENT_LEFT
		b.custom_minimum_size = Vector2(250, 58)
		b.text = "%s %s\n%s   %s" % [a["icon"], a["name"], _eur(float(Market.prices[id])), UITheme.pct(ch)]
		b.theme_type_variation = "SideActive" if id == sel else ""
		b.pressed.connect(_select.bind(id))
		list.add_child(b)
	var right := card(h)
	var asset := Market.asset(sel)
	label(right, "%s %s" % [asset["icon"], asset["name"]], "H2")
	wrap_label(right, String(asset["desc"]))
	price_label = label(right, "", "Big")
	chart = LineChart.new()
	chart.decimals = 2
	chart.value_suffix = " €"
	right.add_child(chart)
	hold_label = label(right, "")
	pl_label = label(right, "")
	var br := hbox(right, 6)
	label(br, "Kaufen:", "Muted")
	for amt in [50, 200, 1000]:
		button(br, money(amt), _buy.bind(amt), "", gm.money < amt)
	button(br, "25 % vom Konto", _buy_pct.bind(0.25), "", gm.money < 8)
	var sr := hbox(right, 6)
	label(sr, "Verkaufen:", "Muted")
	var has := float(Market.holdings[sel]) > 0.0
	for f in [0.25, 0.5, 1.0]:
		button(sr, "Alles" if f == 1.0 else "%d %%" % int(f * 100.0), _sell.bind(f), "DangerButton" if f == 1.0 else "", not has)
	live_update()

func live_update() -> void:
	if chart == null or not is_instance_valid(chart):
		return
	var hist: Array = Market.history[sel]
	var ch := Market.change_pct(sel)
	chart.set_series([{"values": hist, "color": UITheme.GOOD if ch >= 0.0 else UITheme.BAD}])
	price_label.text = "%s   %s" % [_eur(float(Market.prices[sel])), UITheme.pct(ch)]
	price_label.add_theme_color_override("font_color", UITheme.GOOD if ch >= 0.0 else UITheme.BAD)
	hold_label.text = "Bestand: %s Anteile · Wert %s" % [("%.3f" % float(Market.holdings[sel])).replace(".", ","), money(int(round(Market.holding_value(sel))))]
	var pl := Market.profit(sel)
	pl_label.text = "Gewinn/Verlust: %s" % money(int(round(pl)))
	pl_label.add_theme_color_override("font_color", UITheme.GOOD if pl >= 0.0 else UITheme.BAD)

func _eur(v: float) -> String:
	return ("%.2f €" % v).replace(".", ",")

func _select(id: String) -> void:
	sel = id
	rebuild()

func _buy(amount: int) -> void:
	Market.buy(sel, amount)

func _buy_pct(f: float) -> void:
	Market.buy(sel, int(GameManager.money * f))

func _sell(f: float) -> void:
	var v := Market.sell(sel, f)
	if v > 0:
		GameManager.notify("Verkauft für %s." % money(v), "good")
