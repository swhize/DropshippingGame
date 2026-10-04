using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    /// <summary>Anlageklasse im Finanzviertel.</summary>
    public enum AssetKind
    {
        Crypto,
        Stock,
        Etf,
    }

    /// <summary>Börsen-Nachricht (Laufband, Börsen-Terminal, Wallet-App).</summary>
    public sealed class MarketNews
    {
        public string Text = "";
        /// <summary>Betroffene Anlage-Id, "*" = alle Aktien, "CRYPTO" = alle Kryptos.</summary>
        public string Target = "";
        /// <summary>Sofortige Kursbewegung (+0,05 = +5 %).</summary>
        public float Pct;
        public bool Good;
        public int Day;
        public float Minute;
    }

    /// <summary>
    /// Finanzviertel-Erweiterung des Markts: sechs Aktien mit Branche und Dividende, zwei Index-ETFs,
    /// drei Kryptos, Börsen-Nachrichten, Hektik-Wert für die Makler, Dividenden und Sparkonto.
    ///
    /// Balance (Nebenspiel, keine Gelddruckmaschine):
    /// * Kurse laufen leicht zum Startkurs zurück (kein Davonlaufen nach oben oder unten).
    /// * Erwartete Tagesrendite inkl. Dividende jeder Anlage liegt deutlich unter den Kreditzinsen
    ///   (<see cref="GameData.LoanDailyInterest"/>) – Kredit + Anlage lohnt sich nicht.
    /// * Nachrichten bewegen den Kurs sofort; was danach kommt, ist unsicher ("Sell the news").
    ///   Der Nachlauf ist im Mittel kleiner als die Gebühren für Kauf und Verkauf.
    /// * Sparzinsen gibt es nur ohne Schulden und nur bis <see cref="SavingsCap"/>.
    /// Die drei Original-Anlagen (DROP, GAME, ETF) behalten ihren Zufallsgenerator und ihr Verhalten.
    /// </summary>
    public sealed partial class Market
    {
        // =====================================================================================
        // Anlagen
        // =====================================================================================
        public static readonly AssetDef[] Extra =
        {
            // ---- Aktien -----------------------------------------------------------------------
            new AssetDef { Id = "KART", Name = "Kartonwerk Müller", Kind = AssetKind.Stock, Sector = "Verpackung", Start = 24f, Vol = 0.0018f, Drift = 0.000004f, Dividend = 0.0025f, Icon = "box",
                Desc = "Macht Kartons. Langweilig, aber jeder Shop braucht sie. Zahlt brav Dividende." },
            new AssetDef { Id = "PAKT", Name = "PaketPiraten SE", Kind = AssetKind.Stock, Sector = "Logistik", Start = 48f, Vol = 0.0022f, Drift = 0.000005f, Dividend = 0.0015f, Icon = "truck",
                Desc = "Liefert schnell. Manchmal sogar an die richtige Adresse." },
            new AssetDef { Id = "HYPE", Name = "HypeHaus Media", Kind = AssetKind.Stock, Sector = "Medien", Start = 18f, Vol = 0.0032f, Drift = 0.000002f, Icon = "mega",
                Desc = "Influencer-Agentur. Umsatz: Likes. Gewinn: auch Likes." },
            new AssetDef { Id = "FRIT", Name = "Frittenwerk AG", Kind = AssetKind.Stock, Sector = "Food", Start = 31f, Vol = 0.0016f, Drift = 0.000004f, Dividend = 0.002f, Icon = "plate",
                Desc = "Pommes für ganz Europa. Krisensicher, weil Hunger immer geht." },
            new AssetDef { Id = "BOTX", Name = "Botomat KI", Kind = AssetKind.Stock, Sector = "Tech", Start = 120f, Vol = 0.003f, Drift = 0.000006f, Icon = "bolt",
                Desc = "KI für alles. Auch für Toaster. Vor allem für Toaster." },
            new AssetDef { Id = "ROLL", Name = "Voltwagen E-Roller", Kind = AssetKind.Stock, Sector = "Mobilität", Start = 9f, Vol = 0.0035f, Drift = -0.000004f, Icon = "rocket",
                Desc = "E-Roller-Verleih. Die Roller liegen im Fluss, die Aktie manchmal auch." },
            // ---- Index-ETFs (folgen ihren Aktien, gleich gewichtet) ------------------------------
            new AssetDef { Id = "HSTL", Name = "Hustle-30-ETF", Kind = AssetKind.Etf, Sector = "Breiter Markt", Start = 80f, Dividend = 0.0008f, Icon = "grid",
                Components = new[] { "KART", "PAKT", "HYPE", "FRIT", "BOTX", "ROLL", "GAME" },
                Desc = "Der ganze Kiez-Markt in einem Klick. Breit gestreut, wenig Drama." },
            new AssetDef { Id = "TECX", Name = "Tech-Hype-ETF", Kind = AssetKind.Etf, Sector = "Tech", Start = 50f, Icon = "screen",
                Components = new[] { "BOTX", "HYPE", "GAME" },
                Desc = "Alles mit KI, Likes und Pixeln in einem Korb." },
            // ---- Kryptos -----------------------------------------------------------------------
            new AssetDef { Id = "KEKS", Name = "KeksCoin", Kind = AssetKind.Crypto, Sector = "Meme", Start = 0.8f, Vol = 0.011f, Drift = -0.000045f, JumpChance = 0.005f, JumpSize = 0.3f, Icon = "coin",
                Desc = "Ein Hund auf einem Keks. Marktkapitalisierung: unerklärlich." },
            new AssetDef { Id = "BLOK", Name = "BlockKette", Kind = AssetKind.Crypto, Sector = "Krypto", Start = 260f, Vol = 0.006f, Drift = -0.000015f, JumpChance = 0.002f, JumpSize = 0.15f, Icon = "link",
                Desc = "Die seriöse Krypto. Hat ein Whitepaper. Niemand hat es gelesen." },
            new AssetDef { Id = "MOON", Name = "MondMuffin", Kind = AssetKind.Crypto, Sector = "Meme", Start = 3.5f, Vol = 0.009f, Drift = -0.00004f, JumpChance = 0.006f, JumpSize = 0.35f, Icon = "rocket",
                Desc = "To the moon! Oder in den Keller. Keine Fahrstuhlmusik." },
        };

        private static AssetDef[] _all;

        /// <summary>Alle handelbaren Anlagen: die drei Original-Anlagen zuerst, dann das Finanzviertel.</summary>
        public static AssetDef[] AllAssets
        {
            get
            {
                if (_all != null) return _all;
                var list = new List<AssetDef>(Assets);
                list.AddRange(Extra);
                _all = list.ToArray();
                return _all;
            }
        }

        /// <summary>Anlagen einer Klasse in fester Reihenfolge (für Listen in Börse, Bank, Wallet).</summary>
        public static List<AssetDef> OfKind(AssetKind kind)
        {
            var r = new List<AssetDef>();
            foreach (var a in AllAssets)
                if (a.Kind == kind) r.Add(a);
            return r;
        }

        public static bool IsAsset(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            foreach (var a in AllAssets)
                if (a.Id == id) return true;
            return false;
        }

        public static string KindName(AssetKind k) => k == AssetKind.Crypto ? "Krypto" : (k == AssetKind.Stock ? "Aktie" : "ETF");

        // =====================================================================================
        // Konstanten
        // =====================================================================================
        public const float FeeCrypto = 0.015f;
        public const float FeeStock = 0.01f;
        public const float FeeEtf = 0.005f;
        /// <summary>Gemeinsamer Marktfaktor der Aktien pro Tick.</summary>
        public const float MarketVol = 0.0005f;
        /// <summary>Gemeinsamer Krypto-Faktor pro Tick.</summary>
        public const float CryptoVol = 0.003f;
        /// <summary>Rückzug zum Startkurs (log) pro Tick – hält die Kurse in einem Band.</summary>
        public const float Reversion = 0.00003f;
        /// <summary>ETF-Verwaltungsgebühr pro Tick.</summary>
        public const float EtfTer = 0.000002f;
        /// <summary>Chance pro Tick auf eine Börsen-Nachricht (≈ 4 pro Geschäftstag).</summary>
        public const float NewsChance = 0.015f;
        public const int NewsKeep = 12;
        public const float MinPrice = 0.01f;
        /// <summary>Sparzins pro Tag (nur ohne Schulden).</summary>
        public const float SavingsRate = 0.003f;
        /// <summary>Verzinst wird höchstens dieser Betrag.</summary>
        public const int SavingsCap = 50000;

        /// <summary>Gebühr je Kauf und je Verkauf. Original-Anlagen behalten <see cref="Fee"/>.</summary>
        public static float FeeFor(string id)
        {
            var a = Asset(id);
            if (a == null || a.Legacy || a.Id != id) return Fee;
            switch (a.Kind)
            {
                case AssetKind.Crypto: return FeeCrypto;
                case AssetKind.Etf: return FeeEtf;
                default: return FeeStock;
            }
        }

        // =====================================================================================
        // Zustand
        // =====================================================================================
        /// <summary>Sparkonto (Tagesgeld) in €.</summary>
        public int Savings;
        public int SavingsInterestTotal, DividendsTotal;
        /// <summary>Letzte Nacht ausgezahlt (für Bank-UI / Kassenbon).</summary>
        public int LastDividends, LastSavingsInterest;
        /// <summary>Neueste zuerst.</summary>
        public readonly List<MarketNews> News = new List<MarketNews>();
        /// <summary>Hektik an der Börse 0..1 (Kursbewegungen + frische Nachrichten). Für die Makler.</summary>
        public float Frenzy;
        /// <summary>Anzahl Ticks seit Spielbeginn (nur Info).</summary>
        public int TickCount;

        /// <summary>Neue Börsen-Nachricht (UI-Thread: aus <see cref="Tick"/>).</summary>
        public event Action<MarketNews> NewsPosted;

        private Rng _xr = new Rng();
        private readonly Dictionary<string, float> _mom = new Dictionary<string, float>();
        private readonly Dictionary<string, int> _momTicks = new Dictionary<string, int>();
        private readonly Dictionary<string, float> _jump = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _ret = new Dictionary<string, float>();

        /// <summary>Eigener Zufall für das Finanzviertel (Tests: reproduzierbar). Danach <see cref="Reset"/> aufrufen.</summary>
        public void SeedExchange(int seed) => _xr = new Rng(seed);

        private void ResetExchange()
        {
            Savings = 0;
            SavingsInterestTotal = 0;
            DividendsTotal = 0;
            LastDividends = 0;
            LastSavingsInterest = 0;
            News.Clear();
            Frenzy = 0f;
            TickCount = 0;
            _mom.Clear();
            _momTicks.Clear();
            _jump.Clear();
            _ret.Clear();
        }

        private void ResetExchangeAfterWarmup()
        {
            News.Clear();
            Frenzy = 0.2f;
            TickCount = 0;
            _mom.Clear();
            _momTicks.Clear();
            _jump.Clear();
        }

        // =====================================================================================
        // Kurse
        // =====================================================================================
        private void TickExchange()
        {
            TickCount++;
            _ret.Clear();
            float mkt = _xr.Normal(0f, MarketVol);
            float cry = _xr.Normal(0f, CryptoVol);
            float absSum = 0f;
            int absN = 0;
            // Aktien und Kryptos
            foreach (var a in Extra)
            {
                if (a.Kind == AssetKind.Etf) continue;
                float p = Prices.TryGetValue(a.Id, out float pp) ? pp : a.Start;
                float logDev = (float)Math.Log(Math.Max(p, MinPrice) / a.Start);
                float shock = _xr.Normal(0f, a.Vol) + (a.Kind == AssetKind.Crypto ? cry : mkt) + MomentumStep(a.Id);
                float np = p * (float)Math.Exp(a.Drift - Reversion * logDev + shock);
                if (a.JumpChance > 0f && _xr.Value() < a.JumpChance) np *= 1f + _xr.Range(-a.JumpSize, a.JumpSize);
                if (_jump.TryGetValue(a.Id, out float j))
                {
                    np *= 1f + j;
                    _jump.Remove(a.Id);
                }
                np = Math.Max(np, MinPrice);
                float r = np / Math.Max(p, MinPrice) - 1f;
                _ret[a.Id] = r;
                if (a.Kind == AssetKind.Stock)
                {
                    absSum += Math.Abs(r);
                    absN++;
                }
                else
                {
                    absSum += Math.Abs(r) * 0.3f;
                    absN++;
                }
                SetPrice(a.Id, np);
            }
            // Original-Aktie GAME als ETF-Bestandteil
            _ret["GAME"] = ChangePct("GAME", 1);
            // Index-ETFs: mittlere Rendite der Bestandteile minus Gebühr
            foreach (var a in Extra)
            {
                if (a.Kind != AssetKind.Etf || a.Components == null || a.Components.Length == 0) continue;
                float sum = 0f;
                int n = 0;
                foreach (var c in a.Components)
                {
                    if (!_ret.TryGetValue(c, out float rc)) continue;
                    sum += rc;
                    n++;
                }
                float r = (n > 0 ? sum / n : 0f) - EtfTer;
                if (_jump.TryGetValue(a.Id, out float j))
                {
                    r += j;
                    _jump.Remove(a.Id);
                }
                float p = Prices.TryGetValue(a.Id, out float pp) ? pp : a.Start;
                SetPrice(a.Id, Math.Max(p * (1f + r), MinPrice));
                _ret[a.Id] = r;
            }
            // Hektik: Ø-Bewegung der Aktien (Kryptos gedämpft), langsam geglättet
            float move = absN > 0 ? absSum / absN : 0f;
            float target = Mathx.Clamp01(move / 0.0065f);
            Frenzy = Mathx.Clamp01(Frenzy * 0.93f + target * 0.07f);
            if (_xr.Value() < NewsChance) PostNews(-1, 0);
        }

        private void SetPrice(string id, float p)
        {
            Prices[id] = p;
            if (!History.TryGetValue(id, out var h))
            {
                h = new List<float>();
                History[id] = h;
            }
            h.Add(p);
            if (h.Count > HistoryLength) h.RemoveAt(0);
        }

        private float MomentumStep(string id)
        {
            if (!_momTicks.TryGetValue(id, out int left) || left <= 0) return 0f;
            _momTicks[id] = left - 1;
            return _mom.TryGetValue(id, out float m) ? m : 0f;
        }

        /// <summary>Größte Bewegung der letzten <paramref name="ticks"/> Ticks über alle Anlagen (für Laufband / Makler-Rufe).</summary>
        public AssetDef BiggestMover(int ticks, out float change)
        {
            AssetDef best = null;
            change = 0f;
            foreach (var a in AllAssets)
            {
                float c = ChangePct(a.Id, ticks);
                if (best == null || Math.Abs(c) > Math.Abs(change))
                {
                    best = a;
                    change = c;
                }
            }
            return best;
        }

        // =====================================================================================
        // Nachrichten
        // =====================================================================================
        /// <summary>
        /// Erzeugt eine Börsen-Nachricht und bewegt die Kurse. type: -1 zufällig, 0 Aktie, 1 alle Aktien,
        /// 2 eine Krypto, 3 alle Kryptos. dir: 0 zufällig, 1 gut, -1 schlecht.
        /// </summary>
        public MarketNews PostNews(int type, int dir)
        {
            if (type < 0)
            {
                float t = _xr.Value();
                type = t < 0.58f ? 0 : (t < 0.72f ? 1 : (t < 0.92f ? 2 : 3));
            }
            bool crypto = type >= 2;
            bool good = dir > 0 || (dir == 0 && _xr.Value() < (crypto ? 0.47f : 0.5f));
            var news = new MarketNews { Good = good, Day = _sim.Day, Minute = _sim.TimeMinutes };
            float mag;
            var targets = new List<string>();
            switch (type)
            {
                case 0:
                {
                    var stocks = new List<AssetDef>();
                    foreach (var a in Extra)
                        if (a.Kind == AssetKind.Stock) stocks.Add(a);
                    var s = stocks[_xr.Index(stocks.Count)];
                    mag = _xr.Range(0.03f, 0.08f);
                    news.Target = s.Id;
                    news.Text = FinanceText.StockHeadline(s.Id, good, _xr);
                    targets.Add(s.Id);
                    break;
                }
                case 1:
                    mag = _xr.Range(0.01f, 0.03f);
                    news.Target = "*";
                    news.Text = _xr.Pick(good ? FinanceText.MarketGood : FinanceText.MarketBad);
                    foreach (var a in Extra)
                        if (a.Kind == AssetKind.Stock) targets.Add(a.Id);
                    break;
                case 2:
                {
                    var coins = new List<AssetDef>();
                    foreach (var a in Extra)
                        if (a.Kind == AssetKind.Crypto) coins.Add(a);
                    var c = coins[_xr.Index(coins.Count)];
                    mag = _xr.Range(0.1f, 0.35f);
                    news.Target = c.Id;
                    news.Text = string.Format(_xr.Pick(good ? FinanceText.CoinGood : FinanceText.CoinBad), c.Name);
                    targets.Add(c.Id);
                    break;
                }
                default:
                    mag = _xr.Range(0.06f, 0.15f);
                    news.Target = "CRYPTO";
                    news.Text = _xr.Pick(good ? FinanceText.CryptoGood : FinanceText.CryptoBad);
                    foreach (var a in Extra)
                        if (a.Kind == AssetKind.Crypto) targets.Add(a.Id);
                    break;
            }
            float signed = good ? mag : -mag;
            news.Pct = signed;
            // Nachlauf: 25 % der Bewegung über 30 Ticks – in 35 % der Fälle genau andersherum ("Sell the news").
            bool reverse = _xr.Value() < 0.35f;
            float follow = (reverse ? -signed : signed) * 0.25f / 30f;
            foreach (var id in targets)
            {
                _jump[id] = (_jump.TryGetValue(id, out float j) ? j : 0f) + signed;
                _mom[id] = follow;
                _momTicks[id] = 30;
            }
            News.Insert(0, news);
            while (News.Count > NewsKeep) News.RemoveAt(News.Count - 1);
            Frenzy = Mathx.Clamp01(Frenzy + 0.3f + mag * 2f);
            NewsPosted?.Invoke(news);
            return news;
        }

        // =====================================================================================
        // Dividenden, Sparkonto (über Nacht)
        // =====================================================================================
        /// <summary>Erwartete Dividende heute Nacht für eine Anlage (€).</summary>
        public float DividendPerDay(string id)
        {
            var a = Asset(id);
            if (a == null || a.Id != id || a.Dividend <= 0f) return 0f;
            return HoldingValue(id) * a.Dividend;
        }

        public int DividendForecast()
        {
            float v = 0f;
            foreach (var a in AllAssets) v += DividendPerDay(a.Id);
            return (int)Math.Floor(v);
        }

        public bool SavingsEarnInterest => _sim.Debt <= 0;

        public int SavingsInterestForecast() => SavingsEarnInterest ? (int)Math.Floor(Math.Min(Savings, SavingsCap) * SavingsRate) : 0;

        private void ExchangeNewDay()
        {
            LastDividends = DividendForecast();
            if (LastDividends > 0)
            {
                DividendsTotal += LastDividends;
                _sim.AdjustMoney(LastDividends, "trading");
                _sim.Notify("Dividende über Nacht: +" + Fmt.Money(LastDividends) + ".", "good");
            }
            LastSavingsInterest = SavingsInterestForecast();
            if (LastSavingsInterest > 0)
            {
                Savings += LastSavingsInterest;
                SavingsInterestTotal += LastSavingsInterest;
            }
            Frenzy = Math.Max(Frenzy * 0.5f, 0.15f);
        }

        /// <summary>Geld aufs Sparkonto legen. false = nicht genug auf dem Girokonto.</summary>
        public bool Deposit(int amount)
        {
            if (amount <= 0) return false;
            if (_sim.Money < amount)
            {
                _sim.Notify("So viel ist nicht auf dem Girokonto.", "bad");
                _sim.Sound("error");
                return false;
            }
            _sim.Money -= amount;
            Savings += amount;
            _sim.Sound("coin");
            _sim.RaiseEconomyChanged();
            TradingChanged?.Invoke();
            return true;
        }

        /// <summary>Vom Sparkonto abheben. Gibt den abgehobenen Betrag zurück.</summary>
        public int Withdraw(int amount)
        {
            int take = Math.Min(amount, Savings);
            if (take <= 0) return 0;
            Savings -= take;
            _sim.Money += take;
            _sim.Sound("cash", 0.02f, -6f);
            _sim.RaiseEconomyChanged();
            TradingChanged?.Invoke();
            return take;
        }

        /// <summary>
        /// Dispo-Schutz am Feierabend: Reicht das Girokonto nicht für die Fixkosten, bucht die Bank vom
        /// Sparkonto um (verhindert eine Pleite trotz Erspartem). Gibt den umgebuchten Betrag zurück.
        /// </summary>
        public int CoverFromSavings(int needed)
        {
            int gap = needed - _sim.Money;
            if (gap <= 0 || Savings <= 0) return 0;
            int take = Math.Min(gap, Savings);
            Savings -= take;
            _sim.Money += take;
            _sim.Daily.Notes.Add("Bank: " + Fmt.Money(take) + " vom Sparkonto aufs Girokonto umgebucht (Dispo-Schutz).");
            _sim.RaiseEconomyChanged();
            return take;
        }

        /// <summary>Girokonto + Sparkonto + Depot − Kredit.</summary>
        public int NetWorth() => _sim.Money + Savings + PortfolioValue() - _sim.Debt;

        /// <summary>Gesamtgewinn/-verlust aller offenen Positionen (€).</summary>
        public float TotalProfit()
        {
            float v = 0f;
            foreach (var a in AllAssets) v += Profit(a.Id);
            return v;
        }

        /// <summary>Depotwert einer Anlageklasse.</summary>
        public int KindValue(AssetKind kind)
        {
            float v = 0f;
            foreach (var a in AllAssets)
                if (a.Kind == kind) v += HoldingValue(a.Id);
            return Mathx.RoundToInt(v);
        }

        /// <summary>Satirischer Bonitäts-Wert 0..1000 ("Schufi-Score").</summary>
        public int CreditScore()
        {
            float limit = Math.Max(1, _sim.CreditLimit());
            float s = 380f + _sim.Level * 40f + _sim.Reputation * 30f + Math.Min(200f, Savings / 100f) + Math.Min(80f, PortfolioValue() / 250f);
            s -= Math.Min(1f, _sim.Debt / limit) * 300f;
            if (_sim.Money < 0) s -= 150f;
            return Mathx.Clamp(Mathx.RoundToInt(s), 0, 1000);
        }

        public static string CreditScoreLabel(int score)
        {
            if (score >= 850) return "Goldkunde";
            if (score >= 650) return "Solide";
            if (score >= 450) return "Joa";
            if (score >= 250) return "Wackelig";
            return "Bitte gehen Sie";
        }

        // =====================================================================================
        // Admin
        // =====================================================================================
        /// <summary>Alle Aktien und ETFs des Finanzviertels sofort um den Faktor bewegen (Crash/Rallye).</summary>
        public void AdminMoveAll(float mult, bool crypto)
        {
            foreach (var a in Extra)
            {
                if ((a.Kind == AssetKind.Crypto) != crypto) continue;
                SetPrice(a.Id, Math.Max(MinPrice, Prices[a.Id] * mult));
            }
            Frenzy = 1f;
            TradingChanged?.Invoke();
        }

        // =====================================================================================
        // Speichern
        // =====================================================================================
        private void ExchangeToJson(Dictionary<string, object> d)
        {
            d["savings"] = Savings;
            d["savings_interest_total"] = SavingsInterestTotal;
            d["dividends_total"] = DividendsTotal;
            d["last_dividends"] = LastDividends;
            d["last_savings_interest"] = LastSavingsInterest;
            var news = new List<object>();
            foreach (var n in News)
            {
                news.Add(new Dictionary<string, object>
                {
                    { "text", n.Text }, { "target", n.Target }, { "pct", (double)n.Pct }, { "good", n.Good }, { "day", n.Day }, { "minute", (double)n.Minute },
                });
            }
            d["news"] = news;
        }

        private void ExchangeFromJson(Dictionary<string, object> d)
        {
            Savings = Math.Max(0, J.I(d, "savings"));
            SavingsInterestTotal = J.I(d, "savings_interest_total");
            DividendsTotal = J.I(d, "dividends_total");
            LastDividends = J.I(d, "last_dividends");
            LastSavingsInterest = J.I(d, "last_savings_interest");
            News.Clear();
            foreach (var o in J.A(d, "news"))
            {
                var m = J.Obj(o);
                string text = J.S(m, "text");
                if (string.IsNullOrEmpty(text)) continue;
                News.Add(new MarketNews
                {
                    Text = text, Target = J.S(m, "target"), Pct = J.F(m, "pct"), Good = J.B(m, "good"), Day = J.I(m, "day"), Minute = J.F(m, "minute"),
                });
                if (News.Count >= NewsKeep) break;
            }
        }
    }
}
