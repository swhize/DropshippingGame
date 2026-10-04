using System;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>Finanzen › Trading: fiktive Krypto, Meme-Aktie und ETF kaufen/verkaufen, mit Live-Kurs.</summary>
    public sealed class AppTrading : LaptopApp
    {
        private static string _sel = "DROP";
        private LineChart _chart;
        private Label _price, _hold, _pl, _portfolio, _total;

        public override string Lead => "Riskant! Kurse schwanken ständig, " + Mathf.RoundToInt(Market.Fee * 100f) + " % Gebühr pro Kauf und Verkauf. Nur Geld einsetzen, das du nicht fürs Geschäft brauchst.";

        public override void Build()
        {
            var s = S;
            var m = s.Market;
            if (!m.Prices.ContainsKey(_sel)) _sel = Market.Assets[0].Id;
            var top = Columns(Root);
            _portfolio = Stat(top, "PORTFOLIO-WERT", Fmt.Money(m.PortfolioValue())).Q<Label>(className: "stat-value");
            Stat(top, "KONTOSTAND", Fmt.Money(s.Money));
            _total = Stat(top, "GEWINN / VERLUST", "").Q<Label>(className: "stat-value");

            var h = UIX.Row(Root, 14f);
            h.style.alignItems = Align.FlexStart;
            var list = UIX.Col(h, 8f);
            list.style.width = 260;
            list.style.flexShrink = 0;
            foreach (var a in Market.Assets)
            {
                string id = a.Id;
                float ch = m.ChangePct(id);
                var t = Tile(list, id == _sel, false, () =>
                {
                    _sel = id;
                    Rebuild();
                });
                var tr = UIX.Row(t, 10f);
                UIX.Round(UIX.Swatch(tr, Theme.LaptopAccent, 36f, a.Icon), 12f);
                var tv = Grow(UIX.Col(tr, 0f));
                UIX.Text(tv, a.Name, "tile-title");
                UIX.Num(tv, Fmt.Eur(m.Prices[id]), false, "tile-sub");
                UIX.Num(tr, Fmt.Pct(ch), true, ch >= 0f ? "good-text" : "bad-text");
                if (m.Holdings[id] > 0f) UIX.Text(t, "Im Depot: " + Fmt.Money(m.HoldingValue(id)), "tile-sub");
                UIX.PassThrough(t);
            }

            var asset = Market.Asset(_sel);
            var right = Grow(UIX.Card(h));
            var rh = UIX.Row(right, 10f);
            UIX.Icon(rh, asset.Icon, 24f);
            UIX.Text(rh, asset.Name, "h2");
            P(right, asset.Desc);
            _price = UIX.Num(right, "", true, "big");
            _chart = new LineChart { Decimals = 2, Suffix = " €" };
            right.Add(_chart);
            _hold = UIX.Text(right, "");
            _pl = UIX.Text(right, "", "bold");

            var br = UIX.Row(right, 6f);
            br.style.flexWrap = Wrap.Wrap;
            UIX.Text(br, "Kaufen", "muted").style.width = 84;
            foreach (int amt in new[] { 50, 200, 1000 })
            {
                int a = amt;
                Btn(br, Fmt.Money(a), () => S.Market.Buy(_sel, a), "soft", s.Money < a);
            }
            Btn(br, "25 % vom Konto", () => S.Market.Buy(_sel, (int)(S.Money * 0.25f)), "soft", s.Money < 8);
            var sr = UIX.Row(right, 6f);
            sr.style.flexWrap = Wrap.Wrap;
            UIX.Text(sr, "Verkaufen", "muted").style.width = 84;
            bool has = m.Holdings[_sel] > 0f;
            foreach (float f in new[] { 0.25f, 0.5f, 1f })
            {
                float fr = f;
                Btn(sr, fr >= 1f ? "Alles" : Mathf.RoundToInt(fr * 100f) + " %", () =>
                {
                    int v = S.Market.Sell(_sel, fr);
                    if (v > 0) S.Notify("Verkauft für " + Fmt.Money(v) + ".", "good");
                }, fr >= 1f ? "danger" : "soft", !has);
            }
            LiveUpdate();
        }

        /// <summary>Nur Kurs, Chart und Werte aktualisieren (alle 2 Sekunden), ohne Neuaufbau.</summary>
        public void LiveUpdate()
        {
            if (_chart == null || S == null) return;
            var m = S.Market;
            if (!m.Prices.ContainsKey(_sel)) return;
            float ch = m.ChangePct(_sel);
            var col = ch >= 0f ? Theme.LaptopGood : Theme.LaptopBad;
            var series = new LineChart.Series { Color = col };
            series.Values.AddRange(m.History[_sel]);
            _chart.Set(series);
            _price.text = Fmt.Eur(m.Prices[_sel]) + "   " + Fmt.Pct(ch);
            _price.style.color = col;
            _hold.text = "Bestand: " + Fmt.Dec(m.Holdings[_sel], 3) + " Anteile · Wert " + Fmt.Money(m.HoldingValue(_sel));
            float pl = m.Profit(_sel);
            _pl.text = "Gewinn/Verlust: " + Fmt.SignedMoney(Mathf.RoundToInt(pl));
            _pl.style.color = pl >= 0f ? Theme.LaptopGood : Theme.LaptopBad;
            float total = 0f;
            foreach (var a in Market.Assets) total += m.Profit(a.Id);
            _total.text = Fmt.SignedMoney(Mathf.RoundToInt(total));
            _total.style.color = total >= 0f ? Theme.LaptopGood : Theme.LaptopBad;
            _portfolio.text = Fmt.Money(m.PortfolioValue());
        }
    }

    /// <summary>
    /// Finanzen › Bank: Kredite aufnehmen und tilgen. Der Kreditrahmen wächst mit dem
    /// Firmenlevel, Zinsen werden jeden Abend fällig. Dazu die Fixkosten pro Abend.
    /// </summary>
    public sealed class AppBank : LaptopApp
    {
        public override string Lead => "Ein Kredit hilft über Engpässe oder finanziert den nächsten großen Schritt. Zinsen: " +
                                       Fmt.Dec(GameData.LoanDailyInterest * 100f, 0) + " % pro Tag auf die offene Summe.";

        public override void Build()
        {
            var s = S;
            var top = Columns(Root);
            Stat(top, "KONTOSTAND", Fmt.Money(s.Money), s.Money < 0 ? "bad" : "good");
            Stat(top, "SCHULDEN", Fmt.Money(s.Debt), s.Debt > 0 ? "bad" : null);
            Stat(top, "KREDITRAHMEN", Fmt.Money(s.CreditAvailable()), null, "von " + Fmt.Money(s.CreditLimit()) + " (Level " + s.Level + ")");
            Stat(top, "FIXKOSTEN 20 UHR", Fmt.Money(-s.FixedCostsPerDay()), "bad", "Miete, Löhne, Strom, Zinsen");

            var h = UIX.Row(Root, 16f);
            h.style.alignItems = Align.FlexStart;
            var left = Grow(UIX.Col(h, 10f));
            Section(left, "Kredit aufnehmen");
            for (int i = 0; i < GameData.Loans.Length; i++)
            {
                var o = GameData.Loans[i];
                var c = UIX.Card(left);
                var ch = UIX.Row(c, 12f);
                UIX.Round(UIX.Swatch(ch, Theme.LaptopAccent, 42f, "bank"), 14f);
                var v = Grow(UIX.Col(ch, 2f));
                UIX.Num(v, Fmt.Money(o.Amount), true, "h3");
                int interest = Mathf.Max(1, Mathf.RoundToInt(o.Amount * GameData.LoanDailyInterest));
                UIX.Text(v, "Zinsen ca. " + Fmt.Money(interest) + " pro Tag, jederzeit tilgbar", "small");
                bool locked = s.Level < o.Level;
                int amount = o.Amount;
                string txt = locked ? LockText(o.Level) : (o.Amount > s.CreditAvailable() ? "Rahmen zu klein" : "Aufnehmen");
                Btn(ch, txt, () => S.TakeLoan(amount), locked ? "" : "accent", !s.LoanAvailable(i), locked ? "lock" : null);
            }

            var right = Grow(UIX.Col(h, 10f));
            Section(right, "Tilgen");
            var rc = UIX.Card(right);
            if (s.Debt <= 0)
            {
                UIX.Empty(rc, "check_circle", "Du bist schuldenfrei.", "Ein Kredit lohnt sich, wenn er mehr einbringt als er kostet – z. B. für Ware, die sicher verkauft wird.");
            }
            else
            {
                KV(rc, "Offene Schuld", Fmt.Money(s.Debt), "bad");
                KV(rc, "Zinsen pro Tag", Fmt.Money(s.InterestPerDay()), "bad");
                var r = UIX.Wrap(rc);
                foreach (int amt in new[] { 100, 500, 2000 })
                {
                    if (amt >= s.Debt) continue;
                    int a = amt;
                    Btn(r, Fmt.Money(a) + " tilgen", () => S.RepayLoan(a), "soft", s.Money < a);
                }
                Btn(r, "Alles tilgen", () => S.RepayLoan(S.Debt), "accent", s.Money <= 0);
            }

            Section(right, "Fixkosten pro Abend");
            var fc = UIX.Card(right);
            KV(fc, "Miete (" + GameData.StageNames[Mathf.Clamp(s.LocationStage, 0, GameData.StageNames.Length - 1)] + ")", Fmt.Money(-s.RentPerDay()), "bad");
            if (s.WagesPerDay() > 0) KV(fc, "Löhne", Fmt.Money(-s.WagesPerDay()), "bad");
            if (s.UpkeepPerDay() > 0) KV(fc, "Strom Förderband", Fmt.Money(-s.UpkeepPerDay()), "bad");
            if (s.InterestPerDay() > 0) KV(fc, "Kreditzinsen", Fmt.Money(-s.InterestPerDay()), "bad");
            UIX.Separator(fc);
            KV(fc, "Summe", Fmt.Money(-s.FixedCostsPerDay()), "bad");
            var warn = UIX.Row(right, 8f, "card");
            UIX.Icon(warn, "warning", 18f, Theme.LaptopBad);
            Grow(P(warn, "Fällt dein Kontostand am Abend unter " + Fmt.Money(GameData.BankruptLimit) + ", ist die Firma pleite.", ""));
        }
    }

    /// <summary>Finanzen › Analytics: Kennzahlen, Umsatz-/Cashflow-Verlauf, Verkäufe je Produkt, heutige Kosten.</summary>
    public sealed class AppAnalytics : LaptopApp
    {
        public override string Lead => "Zahlen, Daten, Fakten – dein Business auf einen Blick.";

        public override void Build()
        {
            var s = S;
            var r1 = Columns(Root);
            Stat(r1, "UMSATZ GESAMT", Fmt.Money(s.TotalEarned), "good");
            Stat(r1, "UMSATZ HEUTE", Fmt.Money(s.Daily.Revenue));
            Stat(r1, "PAKETE GESAMT", Fmt.Thousands(s.TotalShipped));
            Stat(r1, "VERLOREN", s.TotalLostOrders.ToString(), s.TotalLostOrders > 0 ? "bad" : null);
            var r2 = Columns(Root);
            Stat(r2, "BEWERTUNG", Fmt.Rating(s.Reputation) + " / 5", "accent");
            Stat(r2, "FIRMENLEVEL", s.Level.ToString());
            Stat(r2, "PORTFOLIO", Fmt.Money(s.Market.PortfolioValue()));
            float stockValue = 0f;
            foreach (var p in GameData.Products) stockValue += p.UnitCost * s.StockQty(p.Id);
            Stat(r2, "LAGERWERT", Fmt.Money(stockValue));

            var cc = Card(Root, "Umsatz & Cashflow pro Tag", s.History.Count + " Tage");
            var chart = new LineChart { IncludeZero = true, Suffix = " €" };
            chart.style.height = 220;
            var rev = new LineChart.Series { Color = Theme.LaptopAccent, Label = "Umsatz" };
            var prof = new LineChart.Series { Color = Theme.LaptopTeal, Label = "Cashflow (nach allen Kosten)" };
            foreach (var hh in s.History)
            {
                rev.Values.Add(hh.Revenue);
                prof.Values.Add(hh.Profit);
            }
            chart.Set(rev, prof);
            cc.Add(chart);
            if (s.History.Count < 2) P(cc, "Nach ein paar Tagen siehst du hier deinen Verlauf.", "small");

            var h = UIX.Row(Root, 14f);
            h.style.alignItems = Align.FlexStart;
            var pc = Grow(Card(h, "Verkäufe nach Produkt"));
            int maxv = 1;
            foreach (var p in GameData.Products) maxv = Math.Max(maxv, s.ShippedPerProduct.TryGetValue(p.Id, out int n) ? n : 0);
            foreach (var p in GameData.Products)
            {
                if (!s.ProductUnlocked(p.Id)) continue;
                int n = s.ShippedPerProduct.TryGetValue(p.Id, out int v) ? v : 0;
                var row = UIX.Row(pc, 10f);
                UIX.Swatch(row, p.Color.ToColor(), 20f);
                UIX.Ellipsis(UIX.Text(row, p.Short)).style.width = 90;
                UIX.Bar(row, n / (float)maxv, p.Color.ToColor(), 10f);
                var cl = UIX.Num(row, n.ToString(), false, "muted");
                cl.style.width = 44;
                cl.style.unityTextAlign = TextAnchor.MiddleRight;
            }

            var d = s.Daily;
            var tc = Grow(Card(h, "Heute: Einnahmen & Ausgaben"));
            KV(tc, "Umsatz (Verkäufe)", Fmt.Money(d.Revenue), "good");
            if (d.Stand > 0) KV(tc, "  davon Verkaufsstand", Fmt.Money(d.Stand), "good");
            KV(tc, "Sonstige Einnahmen", Fmt.Money(d.IncomeOther), "good");
            KV(tc, "Wareneinkauf", Fmt.Money(-d.Purchases), "bad");
            KV(tc, "Verpackung", Fmt.Money(-d.Packaging), "bad");
            KV(tc, "Marketing", Fmt.Money(-d.Marketing), "bad");
            KV(tc, "Sonstiges (Ausbau, Deko, Events)", Fmt.Money(-d.Other), "bad");
            if (d.Trading != 0) KV(tc, "Trading (Käufe/Verkäufe)", Fmt.SignedMoney(d.Trading), d.Trading >= 0 ? "good" : "bad");
            UIX.Separator(tc);
            KV(tc, "Fixkosten heute Abend", Fmt.Money(-s.FixedCostsPerDay()), "bad");
        }
    }
}
