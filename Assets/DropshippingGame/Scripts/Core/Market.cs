using System;
using System.Collections.Generic;

namespace DropshippingGame.Core
{
    public sealed class CompetitorDef
    {
        public string Name, Desc;
        public float Factor;
    }

    public sealed class AssetDef
    {
        public string Id, Name, Icon, Desc;
        public float Start, Vol, Drift;
    }

    /// <summary>
    /// Markt: Konkurrenz-Shops mit eigenen Preisen je Produkt (bestimmen die Nachfrage mit)
    /// und ein Trading-System mit drei fiktiven Anlagen (Krypto, Meme-Aktie, ETF).
    /// </summary>
    public sealed class Market
    {
        public static readonly CompetitorDef[] Competitors =
        {
            new CompetitorDef { Name = "BilligBoy24", Factor = 0.86f, Desc = "Billig, schnell, fragwürdig." },
            new CompetitorDef { Name = "TrendHaus", Factor = 1.06f, Desc = "Hip, teuer, schöne Fotos." },
            new CompetitorDef { Name = "AliExpresso", Factor = 0.94f, Desc = "Lieferzeit: ja." },
        };

        public static readonly AssetDef[] Assets =
        {
            new AssetDef { Id = "DROP", Name = "DROPCOIN", Start = 12f, Vol = 0.009f, Drift = -0.00008f, Icon = "coin", Desc = "Hochriskante Krypto. Kann alles – vor allem abstürzen." },
            new AssetDef { Id = "GAME", Name = "GameShop AG", Start = 35f, Vol = 0.0035f, Drift = 0.00001f, Icon = "gamepad", Desc = "Meme-Aktie. Das Internet liebt sie. Meistens." },
            new AssetDef { Id = "ETF", Name = "Welt-ETF", Start = 100f, Vol = 0.0006f, Drift = 0.00002f, Icon = "globe", Desc = "Langweilig. Und genau deshalb solide." },
        };

        public const float TickSeconds = 2f;
        public const float Fee = 0.01f;
        public const int HistoryLength = 90;

        public sealed class PriceWar
        {
            public float Mult;
            public int Until;
        }

        private readonly Sim _sim;
        public List<Dictionary<string, int>> CompPrices = new List<Dictionary<string, int>>();
        public Dictionary<string, PriceWar> CompMods = new Dictionary<string, PriceWar>();
        public Dictionary<string, float> Prices = new Dictionary<string, float>();
        public Dictionary<string, List<float>> History = new Dictionary<string, List<float>>();
        public Dictionary<string, float> Holdings = new Dictionary<string, float>();
        public Dictionary<string, float> Invested = new Dictionary<string, float>();
        private float _acc;

        public event Action TradingChanged;

        public Market(Sim sim)
        {
            _sim = sim;
            Reset();
        }

        private Rng Rng => _sim.Rng;

        public void Reset()
        {
            CompPrices.Clear();
            foreach (var c in Competitors)
            {
                var d = new Dictionary<string, int>();
                foreach (var p in GameData.Products)
                    d[p.Id] = Mathx.RoundToInt(p.RefPrice * c.Factor * Rng.Range(0.95f, 1.05f));
                CompPrices.Add(d);
            }
            CompMods.Clear();
            Prices.Clear();
            History.Clear();
            Holdings.Clear();
            Invested.Clear();
            foreach (var a in Assets)
            {
                Prices[a.Id] = a.Start;
                History[a.Id] = new List<float> { a.Start };
                Holdings[a.Id] = 0f;
                Invested[a.Id] = 0f;
            }
            for (int i = 0; i < 60; i++) Tick(false);
            _acc = 0f;
        }

        /// <summary>Echtzeit-Fortschritt (Kurse bewegen sich alle 2 Sekunden).</summary>
        public void Process(float dt)
        {
            _acc += dt;
            if (_acc >= TickSeconds)
            {
                _acc = 0f;
                Tick(true);
            }
        }

        public void Tick(bool emit)
        {
            foreach (var a in Assets)
            {
                float p = Prices[a.Id];
                float shock = Rng.Normal(0f, a.Vol);
                if (a.Id == "DROP" && Rng.Value() < 0.004f) shock += Rng.Range(-0.25f, 0.25f);
                p = Math.Max(p * (float)Math.Exp(a.Drift + shock), 0.05f);
                Prices[a.Id] = p;
                var h = History[a.Id];
                h.Add(p);
                if (h.Count > HistoryLength) h.RemoveAt(0);
            }
            if (emit) TradingChanged?.Invoke();
        }

        // ---- Konkurrenz -----------------------------------------------------------------------
        public float CompetitorPrice(int ci, string pid)
        {
            float price = CompPrices[ci].TryGetValue(pid, out int v) ? v : 50;
            if (ci == 0 && CompMods.TryGetValue(pid, out var mod)) price *= mod.Mult;
            return price;
        }

        public float MarketPrice(string pid)
        {
            float best = 999999f;
            for (int i = 0; i < CompPrices.Count; i++) best = Math.Min(best, CompetitorPrice(i, pid));
            return best;
        }

        public void NewDay()
        {
            for (int i = 0; i < CompPrices.Count; i++)
            {
                var c = Competitors[i];
                foreach (var p in GameData.Products)
                {
                    float target = p.RefPrice * c.Factor;
                    float cur = CompPrices[i].TryGetValue(p.Id, out int v) ? v : target;
                    cur = Mathx.Lerp(cur, target, 0.3f) * Rng.Range(0.93f, 1.07f);
                    CompPrices[i][p.Id] = Mathx.RoundToInt(Math.Max(cur, 2f));
                }
            }
            var expired = new List<string>();
            foreach (var kv in CompMods)
                if (kv.Value.Until < _sim.Day) expired.Add(kv.Key);
            foreach (var k in expired) CompMods.Remove(k);
        }

        public void ApplyPriceWar(string pid, float mult, int days)
        {
            CompMods[pid] = new PriceWar { Mult = mult, Until = _sim.Day + days - 1 };
        }

        // ---- Trading ------------------------------------------------------------------------------
        public static AssetDef Asset(string id)
        {
            foreach (var a in Assets)
                if (a.Id == id) return a;
            return Assets[0];
        }

        public bool Buy(string id, int amount)
        {
            if (amount <= 0) return false;
            if (_sim.Money < amount)
            {
                _sim.Notify("Nicht genug Geld für diesen Kauf.", "bad");
                _sim.Sound("error");
                return false;
            }
            float units = amount * (1f - Fee) / Prices[id];
            Holdings[id] += units;
            Invested[id] += amount;
            _sim.AdjustMoney(-amount, "trading");
            _sim.Sound("click");
            TradingChanged?.Invoke();
            return true;
        }

        public int Sell(string id, float fraction)
        {
            float f = Mathx.Clamp01(fraction);
            float units = Holdings[id] * f;
            if (units <= 0f) return 0;
            int value = Mathx.RoundToInt(units * Prices[id] * (1f - Fee));
            if (f >= 0.999f)
            {
                Holdings[id] = 0f;
                Invested[id] = 0f;
            }
            else
            {
                Holdings[id] -= units;
                Invested[id] *= 1f - f;
            }
            _sim.AdjustMoney(value, "trading");
            _sim.Sound("cash", 0.02f, -6f);
            TradingChanged?.Invoke();
            return value;
        }

        public float HoldingValue(string id) => Holdings[id] * Prices[id];

        public int PortfolioValue()
        {
            float v = 0f;
            foreach (var a in Assets) v += HoldingValue(a.Id);
            return Mathx.RoundToInt(v);
        }

        public float Profit(string id) => HoldingValue(id) - Invested[id];

        public float ChangePct(string id, int ticks = 30)
        {
            var h = History[id];
            if (h.Count < 2) return 0f;
            float old = h[Math.Max(0, h.Count - 1 - ticks)];
            return h[h.Count - 1] / Math.Max(old, 0.0001f) - 1f;
        }

        public void Shock(string id, float mult)
        {
            Prices[id] = Math.Max(Prices[id] * mult, 0.05f);
            History[id].Add(Prices[id]);
            TradingChanged?.Invoke();
        }

        // ---- Speichern ----------------------------------------------------------------------------
        public Dictionary<string, object> ToJson()
        {
            var cp = new List<object>();
            foreach (var d in CompPrices)
            {
                var o = new Dictionary<string, object>();
                foreach (var kv in d) o[kv.Key] = kv.Value;
                cp.Add(o);
            }
            var mods = new Dictionary<string, object>();
            foreach (var kv in CompMods) mods[kv.Key] = new Dictionary<string, object> { { "mult", (double)kv.Value.Mult }, { "until", kv.Value.Until } };
            var prices = new Dictionary<string, object>();
            var hist = new Dictionary<string, object>();
            var hold = new Dictionary<string, object>();
            var inv = new Dictionary<string, object>();
            foreach (var a in Assets)
            {
                prices[a.Id] = (double)Prices[a.Id];
                var hl = new List<object>();
                foreach (var v in History[a.Id]) hl.Add((double)v);
                hist[a.Id] = hl;
                hold[a.Id] = (double)Holdings[a.Id];
                inv[a.Id] = (double)Invested[a.Id];
            }
            return new Dictionary<string, object>
            {
                { "comp_prices", cp }, { "comp_mods", mods }, { "prices", prices }, { "history", hist }, { "holdings", hold }, { "invested", inv },
            };
        }

        public void FromJson(Dictionary<string, object> d)
        {
            if (d == null || d.Count == 0) return;
            var cp = J.A(d, "comp_prices");
            if (cp.Count == Competitors.Length)
            {
                for (int i = 0; i < cp.Count; i++)
                {
                    var o = J.Obj(cp[i]);
                    foreach (var p in GameData.Products)
                        if (o.ContainsKey(p.Id)) CompPrices[i][p.Id] = J.I(o[p.Id]);
                }
            }
            CompMods.Clear();
            foreach (var kv in J.O(d, "comp_mods"))
            {
                var m = J.Obj(kv.Value);
                CompMods[kv.Key] = new PriceWar { Mult = J.F(m, "mult", 1f), Until = J.I(m, "until") };
            }
            var prices = J.O(d, "prices");
            var hist = J.O(d, "history");
            var hold = J.O(d, "holdings");
            var inv = J.O(d, "invested");
            foreach (var a in Assets)
            {
                if (prices.ContainsKey(a.Id)) Prices[a.Id] = J.F(prices[a.Id], a.Start);
                if (hist.TryGetValue(a.Id, out object ho) && ho is List<object> hl && hl.Count > 0)
                {
                    var h = new List<float>();
                    foreach (var v in hl) h.Add(J.F(v));
                    History[a.Id] = h;
                }
                if (hold.ContainsKey(a.Id)) Holdings[a.Id] = J.F(hold[a.Id]);
                if (inv.ContainsKey(a.Id)) Invested[a.Id] = J.F(inv[a.Id]);
            }
        }
    }
}
