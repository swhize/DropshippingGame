using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace DropshippingGame.UI
{
    /// <summary>
    /// Oberflächen des Finanzviertels: Bank-Fenster (Konten, Kredit, Sparen), Börsen-Terminal
    /// (alle Anlagen, Chart, Kaufen/Verkaufen, Nachrichten – live, Spiel läuft weiter) und die
    /// Handy-App „Wallet“ (registriert sich selbst über <see cref="PhoneView.ExtraApps"/>).
    /// </summary>
    public static class FinanceUI
    {
        private static int _bankTab;
        private static string _sel = "HSTL";

        private static Sim S => Game.Sim;
        private static Color Good => new Color(0.2f, 0.75f, 0.4f);
        private static Color Bad => new Color(0.9f, 0.3f, 0.3f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _bankTab = 0;
            _sel = "HSTL";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterWallet()
        {
            if (PhoneView.ExtraApps.Exists(a => a != null && a.Title == "Wallet")) return;
            PhoneView.ExtraApps.Add(new PhoneView.PhoneApp
            {
                Title = "Wallet", TabLabel = "Wallet", Icon = "wallet",
                Build = (view, content) => BuildWallet(view, content),
            });
        }

        // =====================================================================================
        // Bank
        // =====================================================================================
        public static void OpenBank(bool atm)
        {
            var ui = Game.UI;
            if (ui == null || S == null) return;
            if (atm) _bankTab = 0;
            var body = ui.Modal.Open(atm ? "Geldautomat" : "Kiezbank", atm ? FinanceText.AtmLines[UnityEngine.Random.Range(0, FinanceText.AtmLines.Length)]
                : FinanceText.BankerLines[UnityEngine.Random.Range(0, FinanceText.BankerLines.Length)], 720,
                new[] { new ModalButton("Tschüss", null, "accent") }, true, "bank", null, "bank");
            var host = UIX.Col(body, 10f);
            BuildBank(host, atm);
        }

        private static void RebuildBank(VisualElement host, bool atm)
        {
            host.Clear();
            BuildBank(host, atm);
        }

        private static void BuildBank(VisualElement host, bool atm)
        {
            var s = S;
            if (s == null) return;
            var m = s.Market;
            Action refresh = () => RebuildBank(host, atm);
            var stats = UIX.Row(host, 8f);
            UIX.Stat(stats, "GIROKONTO", Fmt.Money(s.Money), s.Money < 0 ? Bad : (Color?)null);
            UIX.Stat(stats, "SPARKONTO", Fmt.Money(m.Savings));
            UIX.Stat(stats, "DEPOT", Fmt.Money(m.PortfolioValue()));
            UIX.Stat(stats, "KREDIT", s.Debt > 0 ? "−" + Fmt.Money(s.Debt) : "0 €", s.Debt > 0 ? Bad : (Color?)null);
            int score = m.CreditScore();
            UIX.KV(host, "Vermögen (alles minus Schulden)", Fmt.Money(m.NetWorth()));
            UIX.KV(host, "Schufi-Score", score + " / 1000 · " + Market.CreditScoreLabel(score));
            if (!atm)
            {
                UIX.Segmented(host, new[] { "Konten", "Kredit", "Sparen" }, _bankTab, i =>
                {
                    _bankTab = i;
                    refresh();
                });
            }
            var box = UIX.Col(host, 8f);
            if (_bankTab == 1 && !atm) BankLoans(box, s, refresh);
            else if (_bankTab == 2 || atm) BankSavings(box, s, refresh, atm);
            else BankAccounts(box, s);
        }

        private static void BankAccounts(VisualElement box, Sim s)
        {
            var m = s.Market;
            UIX.KV(box, "Dividende heute Nacht (geschätzt)", "+" + Fmt.Money(m.DividendForecast()));
            UIX.KV(box, "Sparzinsen heute Nacht", "+" + Fmt.Money(m.SavingsInterestForecast()));
            UIX.KV(box, "Kreditzinsen heute Abend", s.InterestPerDay() > 0 ? "−" + Fmt.Money(s.InterestPerDay()) : "0 €");
            UIX.KV(box, "Dividenden bisher", Fmt.Money(m.DividendsTotal));
            UIX.KV(box, "Sparzinsen bisher", Fmt.Money(m.SavingsInterestTotal));
            UIX.KV(box, "Depot: Aktien / ETFs / Krypto", Fmt.Money(m.KindValue(AssetKind.Stock)) + " / " + Fmt.Money(m.KindValue(AssetKind.Etf)) + " / " + Fmt.Money(m.KindValue(AssetKind.Crypto)));
            Note(box, "Kontoführung: 0 €. Kontoatmung: auch 0 €. Noch.");
        }

        private static void BankLoans(VisualElement box, Sim s, Action refresh)
        {
            UIX.KV(box, "Kreditrahmen (Level " + s.Level + ")", Fmt.Money(s.CreditLimit()));
            UIX.KV(box, "Noch verfügbar", Fmt.Money(s.CreditAvailable()));
            UIX.KV(box, "Zinsen", Fmt.Dec(GameData.LoanDailyInterest * 100f, 0) + " % pro Tag");
            var r = UIX.Row(box, 6f);
            r.style.flexWrap = Wrap.Wrap;
            for (int i = 0; i < GameData.Loans.Length; i++)
            {
                var o = GameData.Loans[i];
                int amt = o.Amount;
                bool ok = s.LoanAvailable(i);
                UIX.Button(r, (s.Level < o.Level ? "Ab Lv " + o.Level + ": " : "+") + Fmt.Money(amt), () =>
                {
                    S?.TakeLoan(amt);
                    refresh();
                }, "soft", !ok);
            }
            var t = UIX.Row(box, 6f);
            t.style.flexWrap = Wrap.Wrap;
            foreach (int a in new[] { 100, 500, 2000 })
            {
                int amt = a;
                UIX.Button(t, "Tilgen " + Fmt.Money(amt), () =>
                {
                    S?.RepayLoan(amt);
                    refresh();
                }, "", s.Debt <= 0 || s.Money <= 0);
            }
            UIX.Button(t, "Alles tilgen", () =>
            {
                S?.RepayLoan(int.MaxValue);
                refresh();
            }, "accent", s.Debt <= 0 || s.Money <= 0);
            Note(box, "Kredit + Aktien kaufen? Die Zinsen fressen jede Rendite. Fragen Sie Gary.");
        }

        private static void BankSavings(VisualElement box, Sim s, Action refresh, bool atm)
        {
            var m = s.Market;
            UIX.KV(box, "Sparzins", Fmt.Dec(Market.SavingsRate * 100f, 1) + " % pro Tag (bis " + Fmt.Money(Market.SavingsCap) + ")");
            if (!m.SavingsEarnInterest) UIX.KV(box, "Status", "Keine Zinsen – erst Kredit tilgen!", Bad);
            var d = UIX.Row(box, 6f);
            d.style.flexWrap = Wrap.Wrap;
            foreach (int a in new[] { 100, 500, 2000, 10000 })
            {
                int amt = a;
                UIX.Button(d, "Einzahlen " + Fmt.Money(amt), () =>
                {
                    S?.Market.Deposit(amt);
                    refresh();
                }, "soft", s.Money < amt);
            }
            var w = UIX.Row(box, 6f);
            w.style.flexWrap = Wrap.Wrap;
            foreach (int a in new[] { 100, 500, 2000 })
            {
                int amt = a;
                UIX.Button(w, (atm ? "Abheben " : "Auszahlen ") + Fmt.Money(amt), () =>
                {
                    S?.Market.Withdraw(amt);
                    refresh();
                }, "", m.Savings <= 0);
            }
            UIX.Button(w, "Alles auszahlen", () =>
            {
                S?.Market.Withdraw(int.MaxValue);
                refresh();
            }, "accent", m.Savings <= 0);
            foreach (var tip in FinanceText.SavingsTips) Note(box, tip);
        }

        private static void Note(VisualElement parent, string text)
        {
            var l = UIX.Text(parent, text, "modal-text");
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.opacity = 0.75f;
        }

        // =====================================================================================
        // Börsen-Terminal
        // =====================================================================================
        public static void OpenExchange()
        {
            var ui = Game.UI;
            if (ui == null || S == null) return;
            if (!Market.IsAsset(_sel)) _sel = "HSTL";
            var body = ui.Modal.Open("Börsen-Terminal", "Kurse live. Keine Anlageberatung. Eher Anlageunterhaltung.", 940,
                new[] { new ModalButton("Fertig", null, "accent") }, false, "boerse", null, "trend");
            var host = UIX.Col(body, 10f);
            BuildExchange(host);
        }

        private sealed class Live
        {
            public readonly Dictionary<string, Label[]> Rows = new Dictionary<string, Label[]>();
            public Label Price, Hold, Depot;
            public PriceChart Chart;
            public string Sig = "";
        }

        private static void BuildExchange(VisualElement host)
        {
            var s = S;
            if (s == null) return;
            var m = s.Market;
            var live = new Live();
            var cols = UIX.Row(host, 14f);
            cols.style.alignItems = Align.FlexStart;
            var list = UIX.Col(cols, 2f);
            list.style.width = 330;
            foreach (AssetKind k in new[] { AssetKind.Etf, AssetKind.Stock, AssetKind.Crypto })
            {
                UIX.Text(list, k == AssetKind.Etf ? "ETFs (ruhig)" : (k == AssetKind.Stock ? "Aktien" : "Krypto (wild)"), "kv-key");
                foreach (var a in Market.OfKind(k))
                {
                    string id = a.Id;
                    var r = UIX.PressRow(list, 6f, () =>
                    {
                        _sel = id;
                        host.Clear();
                        BuildExchange(host);
                    }, "exch-row");
                    r.style.paddingLeft = 6;
                    r.style.paddingRight = 6;
                    if (id == _sel) r.style.backgroundColor = new Color(0.3f, 0.5f, 1f, 0.2f);
                    UIX.Icon(r, a.Icon, 15f);
                    var n = UIX.Text(r, id + "  " + a.Name, "kv-key");
                    n.style.flexGrow = 1;
                    var p = UIX.Text(r, "", "kv-value");
                    p.style.width = 70;
                    var c = UIX.Text(r, "", "kv-value");
                    c.style.width = 62;
                    if (m.Holdings.TryGetValue(id, out float h) && h > 0f) UIX.Icon(r, "wallet", 12f);
                    UIX.PassThrough(r);
                    live.Rows[id] = new[] { p, c };
                }
            }
            var right = UIX.Col(cols, 8f);
            right.style.flexGrow = 1;
            right.style.flexShrink = 1;
            var asset = Market.Asset(_sel);
            UIX.Text(right, asset.Name + " (" + asset.Id + ") · " + Market.KindName(asset.Kind) + " · " + asset.Sector, "modal-title");
            live.Price = UIX.Num(right, "", true, "status-money");
            live.Chart = new PriceChart();
            live.Chart.style.height = 150;
            right.Add(live.Chart);
            var desc = UIX.Text(right, asset.Desc + (asset.Dividend > 0f ? " Dividende: " + Fmt.Dec(asset.Dividend * 100f, 2) + " % pro Tag." : "") +
                " Gebühr: " + Fmt.Dec(Market.FeeFor(asset.Id) * 100f, 1) + " % je Kauf/Verkauf." +
                (asset.Components != null ? " Enthält: " + string.Join(", ", asset.Components) + "." : ""), "modal-text");
            desc.style.whiteSpace = WhiteSpace.Normal;
            live.Hold = UIX.Text(right, "", "kv-key");
            live.Depot = UIX.Text(right, "", "kv-key");
            var buy = UIX.Row(right, 6f);
            buy.style.flexWrap = Wrap.Wrap;
            foreach (int a in new[] { 50, 200, 1000, 5000 })
            {
                int amt = a;
                UIX.Button(buy, "Kaufen " + Fmt.Money(amt), () =>
                {
                    S?.Market.Buy(_sel, amt);
                    host.Clear();
                    BuildExchange(host);
                }, "soft", s.Money < amt);
            }
            var sell = UIX.Row(right, 6f);
            sell.style.flexWrap = Wrap.Wrap;
            bool has = m.Holdings.TryGetValue(_sel, out float hq) && hq > 0f;
            foreach (float f in new[] { 0.25f, 0.5f, 1f })
            {
                float fr = f;
                UIX.Button(sell, fr >= 1f ? "Alles verkaufen" : "Verkaufen " + Mathf.RoundToInt(fr * 100f) + " %", () =>
                {
                    int v = S != null ? S.Market.Sell(_sel, fr) : 0;
                    if (v > 0) Game.Notify("Verkauft für " + Fmt.Money(v) + ".", "good");
                    host.Clear();
                    BuildExchange(host);
                }, fr >= 1f ? "danger" : "", !has);
            }
            UIX.Text(host, "NACHRICHTEN", "kv-key");
            var news = UIX.Col(host, 3f);
            FillNews(news, m, 5);
            host.schedule.Execute(() => UpdateExchange(live, news)).Every(500);
            UpdateExchange(live, news);
        }

        private static void FillNews(VisualElement news, Market m, int max)
        {
            news.Clear();
            if (m.News.Count == 0) Note(news, "Noch keine Schlagzeilen. Die Makler langweilen sich.");
            for (int i = 0; i < m.News.Count && i < max; i++)
            {
                var n = m.News[i];
                var r = UIX.Row(news, 6f);
                UIX.Icon(r, n.Good ? "trend" : "trend_down", 14f, n.Good ? Good : Bad);
                var t = UIX.Text(r, Fmt.Clock(n.Minute) + "  " + n.Text + " (" + Fmt.Pct(n.Pct) + ")", "modal-text");
                t.style.whiteSpace = WhiteSpace.Normal;
                t.style.flexShrink = 1;
            }
        }

        private static void UpdateExchange(Live live, VisualElement news)
        {
            var s = S;
            if (s == null) return;
            var m = s.Market;
            foreach (var kv in live.Rows)
            {
                if (!m.Prices.ContainsKey(kv.Key)) continue;
                float c = m.ChangePct(kv.Key);
                kv.Value[0].text = PriceText(m.Prices[kv.Key]);
                kv.Value[1].text = Fmt.Pct(c);
                kv.Value[1].style.color = c >= 0 ? Good : Bad;
            }
            if (m.Prices.TryGetValue(_sel, out float p))
            {
                float c = m.ChangePct(_sel);
                live.Price.text = Fmt.Eur(p) + "  " + Fmt.Pct(c);
                live.Price.style.color = c >= 0 ? Good : Bad;
                if (m.History.TryGetValue(_sel, out var h)) live.Chart.Set(h, c >= 0 ? Good : Bad);
                float hold = m.Holdings.TryGetValue(_sel, out float hq) ? hq : 0f;
                float pl = m.Profit(_sel);
                live.Hold.text = "Bestand: " + Fmt.Dec(hold, 3) + " Anteile · " + Fmt.Money(m.HoldingValue(_sel)) + " · G/V " + Fmt.SignedMoney(Mathf.RoundToInt(pl));
            }
            live.Depot.text = "Konto " + Fmt.Money(s.Money) + " · Depot " + Fmt.Money(m.PortfolioValue()) + " · Hektik " + Mathf.RoundToInt(m.Frenzy * 100f) + " %";
            string sig = m.News.Count > 0 ? m.News[0].Text + m.News.Count : "";
            if (sig != live.Sig)
            {
                live.Sig = sig;
                FillNews(news, m, 5);
            }
        }

        private static string PriceText(float p) => p >= 100f ? Fmt.Dec(p, 1) : Fmt.Dec(p, p < 1f ? 3 : 2);

        // =====================================================================================
        // Handy-App Wallet
        // =====================================================================================
        private static void BuildWallet(PhoneView view, VisualElement content)
        {
            var s = S;
            if (s == null) return;
            var m = s.Market;
            var hero = UIX.Col(content, 2f, "status-hero");
            UIX.Text(hero, "DEPOT", "status-tile-key");
            var depot = UIX.Num(hero, Fmt.Money(m.PortfolioValue()), true, "status-money");
            var pl = UIX.Text(hero, "", "phone-item-sub");
            UIX.Text(hero, "Konto " + Fmt.Money(s.Money) + " · Sparkonto " + Fmt.Money(m.Savings), "phone-item-sub");
            var labels = new Dictionary<string, Label>();
            foreach (AssetKind k in new[] { AssetKind.Crypto, AssetKind.Etf, AssetKind.Stock })
            {
                UIX.Text(content, k == AssetKind.Crypto ? "KRYPTO" : (k == AssetKind.Etf ? "ETFs" : "AKTIEN (Bestand)"), "status-tile-key");
                foreach (var a in Market.OfKind(k))
                {
                    string id = a.Id;
                    bool has = m.Holdings.TryGetValue(id, out float h) && h > 0.0001f;
                    if (k == AssetKind.Stock && !has) continue;
                    var item = UIX.Col(content, 4f, "phone-item");
                    item.style.alignItems = Align.Stretch;
                    var top = UIX.Row(item, 8f);
                    UIX.Icon(top, a.Icon, 16f);
                    var name = UIX.Text(top, a.Name, "phone-item-title");
                    name.style.flexGrow = 1;
                    name.style.flexShrink = 1;
                    labels[id] = UIX.Text(top, "", "phone-item-sub");
                    var act = UIX.Row(item, 6f);
                    UIX.Text(act, has ? Fmt.Money(m.HoldingValue(id)) : "kein Bestand", "phone-item-sub").style.flexGrow = 1;
                    var b1 = UIX.Button(act, "+50 €", () =>
                    {
                        S?.Market.Buy(id, 50);
                        view.MarkDirty();
                    }, "soft", s.Money < 50);
                    b1.AddToClassList("btn-sm");
                    var b2 = UIX.Button(act, "Verkaufen", () =>
                    {
                        int v = S != null ? S.Market.Sell(id, 1f) : 0;
                        if (v > 0) Game.Notify("Verkauft für " + Fmt.Money(v) + ".", "good");
                        view.MarkDirty();
                    }, "ghost", !has);
                    b2.AddToClassList("btn-sm");
                }
            }
            if (m.News.Count > 0)
            {
                var n = UIX.Text(content, "News: " + m.News[0].Text, "phone-item-sub");
                n.style.whiteSpace = WhiteSpace.Normal;
            }
            UIX.Text(content, "Mehr Auswahl im Börsen-Terminal (Finanzviertel).", "phone-item-sub");
            Action upd = () =>
            {
                var s2 = S;
                if (s2 == null) return;
                var m2 = s2.Market;
                depot.text = Fmt.Money(m2.PortfolioValue());
                float t = m2.TotalProfit();
                pl.text = "G/V gesamt " + Fmt.SignedMoney(Mathf.RoundToInt(t));
                pl.style.color = t >= 0 ? Good : Bad;
                foreach (var kv in labels)
                {
                    if (!m2.Prices.ContainsKey(kv.Key)) continue;
                    float c = m2.ChangePct(kv.Key);
                    kv.Value.text = PriceText(m2.Prices[kv.Key]) + " " + Fmt.Pct(c);
                    kv.Value.style.color = c >= 0 ? Good : Bad;
                }
            };
            hero.schedule.Execute(upd).Every(1000);
            upd();
        }
    }

    /// <summary>Einfacher Kurs-Chart (Linie + Fläche), skaliert auf Min/Max des Verlaufs.</summary>
    public sealed class PriceChart : VisualElement
    {
        private readonly List<float> _v = new List<float>();
        private Color _c = Color.green;

        public PriceChart()
        {
            pickingMode = PickingMode.Ignore;
            style.backgroundColor = new Color(0f, 0f, 0f, 0.15f);
            generateVisualContent += Draw;
        }

        public void Set(IList<float> values, Color color)
        {
            _v.Clear();
            if (values != null) _v.AddRange(values);
            _c = color;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect;
            if (_v.Count < 2 || r.width < 8f || r.height < 8f) return;
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var v in _v)
            {
                lo = Mathf.Min(lo, v);
                hi = Mathf.Max(hi, v);
            }
            if (hi - lo < hi * 0.002f + 0.0001f)
            {
                hi += hi * 0.001f + 0.0001f;
                lo -= lo * 0.001f + 0.0001f;
            }
            var m = new UIDraw.MeshBuilder();
            var pts = new List<Vector2>();
            for (int i = 0; i < _v.Count; i++)
            {
                float x = 4f + (r.width - 8f) * i / (_v.Count - 1);
                float y = 6f + (r.height - 12f) * (1f - (_v[i] - lo) / (hi - lo));
                pts.Add(new Vector2(x, y));
            }
            var fill = new Color(_c.r, _c.g, _c.b, 0.15f);
            for (int i = 0; i + 1 < pts.Count; i++)
                m.Quad(pts[i], pts[i + 1], new Vector2(pts[i + 1].x, r.height), new Vector2(pts[i].x, r.height), fill);
            m.Polyline(pts, 2.5f, _c);
            m.Circle(pts[pts.Count - 1], 4f, _c, 12);
            m.Flush(ctx);
        }
    }
}
