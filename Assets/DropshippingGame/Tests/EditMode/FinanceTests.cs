using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>Finanzviertel: Aktien, Index-ETFs, Kryptos, Nachrichten, Dividenden, Sparkonto (Market.Exchange.cs).</summary>
    public class FinanceTests
    {
        private const int TicksPerDay = 270;

        private static Sim FreshSeeded(int seed)
        {
            var gm = TestUtil.Fresh(seed);
            gm.Market.SeedExchange(seed * 31 + 7);
            gm.Market.Reset();
            return gm;
        }

        [Test]
        public void Assets_LegacyStaysCompatible()
        {
            Assert.AreEqual(3, Market.Assets.Length, "TradingViech sieht weiter genau drei Anlagen");
            Assert.AreEqual("DROP", Market.Assets[0].Id);
            Assert.AreEqual("GAME", Market.Assets[1].Id);
            Assert.AreEqual("ETF", Market.Assets[2].Id);
            Assert.AreEqual(Market.Assets.Length + Market.Extra.Length, Market.AllAssets.Length);
            for (int i = 0; i < 3; i++) Assert.AreSame(Market.Assets[i], Market.AllAssets[i], "Original-Anlagen zuerst");
            var ids = new HashSet<string>();
            foreach (var a in Market.AllAssets)
            {
                Assert.IsTrue(ids.Add(a.Id), "Id eindeutig: " + a.Id);
                Assert.IsFalse(string.IsNullOrEmpty(a.Name) || string.IsNullOrEmpty(a.Desc) || string.IsNullOrEmpty(a.Icon), "Texte: " + a.Id);
                if (a.Components != null)
                    foreach (var c in a.Components)
                        Assert.AreEqual(AssetKind.Stock, Market.Asset(c).Kind, "ETF-Bestandteil ist Aktie: " + c);
            }
            Assert.IsTrue(Market.OfKind(AssetKind.Stock).Count >= 6 && Market.OfKind(AssetKind.Etf).Count >= 3 && Market.OfKind(AssetKind.Crypto).Count >= 4);
            Assert.AreEqual(Market.Fee, Market.FeeFor("DROP"), 1e-6f, "Original-Gebühr bleibt");
            Assert.AreEqual(Market.FeeEtf, Market.FeeFor("HSTL"), 1e-6f);
            Assert.AreEqual(Market.FeeCrypto, Market.FeeFor("KEKS"), 1e-6f);
            Assert.AreEqual(Market.Fee, Market.FeeFor("GIBTSNICHT"), 1e-6f);
        }

        [Test]
        public void Exchange_DoesNotChangeLegacyPrices()
        {
            var a = TestUtil.Fresh(5);
            var b = TestUtil.Fresh(5);
            b.Market.SeedExchange(999);
            for (int t = 0; t < 500; t++)
            {
                a.Market.Tick(false);
                b.Market.Tick(false);
            }
            foreach (var x in Market.Assets)
                Assert.AreEqual(a.Market.Prices[x.Id], b.Market.Prices[x.Id], 1e-6f, "Original-Kurs unabhängig vom Finanzviertel: " + x.Id);
        }

        [Test]
        public void Prices_StayPositiveAndBounded()
        {
            var gm = FreshSeeded(3);
            for (int t = 0; t < TicksPerDay * 40; t++) gm.Market.Tick(false);
            foreach (var a in Market.AllAssets)
            {
                float p = gm.Market.Prices[a.Id];
                Assert.IsTrue(p >= Market.MinPrice && !float.IsNaN(p) && !float.IsInfinity(p), "Kurs gültig: " + a.Id);
                Assert.IsTrue(gm.Market.History[a.Id].Count <= Market.HistoryLength, "Verlauf begrenzt: " + a.Id);
                if (!a.Legacy) Assert.IsTrue(p < a.Start * 20f, "Kein Davonlaufen: " + a.Id + " " + p);
            }
        }

        /// <summary>Kernregel: Kredit aufnehmen und anlegen lohnt sich bei keiner Anlage (inkl. Dividende).</summary>
        [Test]
        public void Balance_NoAssetBeatsLoanInterest()
        {
            foreach (var a in Market.Extra)
            {
                double sum = 0;
                int n = 0;
                for (int seed = 0; seed < 6; seed++)
                {
                    var gm = FreshSeeded(100 + seed);
                    for (int d = 0; d < 80; d++)
                    {
                        float start = gm.Market.Prices[a.Id];
                        for (int t = 0; t < TicksPerDay; t++) gm.Market.Tick(false);
                        sum += gm.Market.Prices[a.Id] / start - 1f + a.Dividend;
                        n++;
                    }
                }
                double avg = sum / n;
                TestContext.WriteLine(a.Id + " Ø Tagesrendite inkl. Dividende: " + (avg * 100).ToString("0.00") + " %");
                Assert.IsTrue(avg < GameData.LoanDailyInterest * 0.75, "Kredit + " + a.Id + " lohnt nicht (" + avg + ")");
                if (a.Kind != AssetKind.Crypto) Assert.IsTrue(avg < 0.01, a.Id + " unter 1 % pro Tag (" + avg + ")");
            }
        }

        [Test]
        public void Balance_EtfCalmerThanStocksCalmerThanCrypto()
        {
            var sd = new Dictionary<string, double>();
            foreach (var a in Market.Extra)
            {
                var gm = FreshSeeded(42);
                var rets = new List<double>();
                for (int d = 0; d < 120; d++)
                {
                    float start = gm.Market.Prices[a.Id];
                    for (int t = 0; t < TicksPerDay; t++) gm.Market.Tick(false);
                    rets.Add(Math.Log(gm.Market.Prices[a.Id] / start));
                }
                double mean = 0;
                foreach (var r in rets) mean += r;
                mean /= rets.Count;
                double v = 0;
                foreach (var r in rets) v += (r - mean) * (r - mean);
                sd[a.Id] = Math.Sqrt(v / rets.Count);
                TestContext.WriteLine(a.Id + " Tages-Schwankung: " + (sd[a.Id] * 100).ToString("0.0") + " %");
            }
            double stockAvg = (sd["KART"] + sd["PAKT"] + sd["HYPE"] + sd["FRIT"] + sd["BOTX"] + sd["ROLL"]) / 6;
            Assert.IsTrue(sd["HSTL"] < stockAvg * 0.8, "Breiter ETF schwankt weniger als Aktien");
            Assert.IsTrue(sd["KEKS"] > stockAvg * 2 && sd["MOON"] > stockAvg * 2 && sd["BLOK"] > stockAvg, "Krypto schwankt mehr");
        }

        [Test]
        public void News_MovePricesAndRaiseFrenzy()
        {
            var gm = FreshSeeded(8);
            float before = gm.Market.Prices["BOTX"];
            float frenzy = gm.Market.Frenzy;
            MarketNews posted = null;
            gm.Market.NewsPosted += n => posted = n;
            // Sicher eine gute Aktien-Nachricht; Ziel kann jede Aktie sein – daher bis BOTX getroffen wird.
            MarketNews news = null;
            for (int i = 0; i < 200 && (news == null || news.Target != "BOTX"); i++) news = gm.Market.PostNews(0, 1);
            Assert.IsNotNull(news);
            Assert.AreEqual("BOTX", news.Target);
            Assert.AreSame(news, posted, "Ereignis gefeuert");
            Assert.IsTrue(news.Good && news.Pct > 0f && !string.IsNullOrEmpty(news.Text));
            Assert.IsTrue(gm.Market.Frenzy > frenzy, "Hektik steigt");
            Assert.AreSame(news, gm.Market.News[0], "Neueste Nachricht vorne");
            Assert.IsTrue(gm.Market.News.Count <= Market.NewsKeep);
            gm.Market.Tick(false);
            Assert.IsTrue(gm.Market.Prices["BOTX"] > before * 1.02f, "Kurs springt nach guter Nachricht");
            var bad = gm.Market.PostNews(3, -1);
            Assert.AreEqual("CRYPTO", bad.Target);
            float k = gm.Market.Prices["KEKS"];
            gm.Market.Tick(false);
            Assert.IsTrue(gm.Market.Prices["KEKS"] < k, "Krypto fällt bei schlechter Krypto-Nachricht");
        }

        /// <summary>Nach einer Nachricht einsteigen und den Nachlauf mitnehmen bringt nach Gebühren nichts.</summary>
        [Test]
        public void News_ChasingIsNotProfitableAfterFees()
        {
            double sum = 0;
            int n = 0;
            var gm = FreshSeeded(77);
            for (int i = 0; i < 600; i++)
            {
                var news = gm.Market.PostNews(0, 1);
                gm.Market.Tick(false); // Sprung ist im Kurs – jetzt sieht der Spieler die Nachricht
                string id = news.Target;
                float buy = gm.Market.Prices[id];
                for (int t = 0; t < 30; t++) gm.Market.Tick(false);
                float sell = gm.Market.Prices[id];
                float fee = Market.FeeFor(id);
                sum += sell / buy * (1f - fee) * (1f - fee) - 1f;
                n++;
                for (int t = 0; t < 40; t++) gm.Market.Tick(false);
            }
            double avg = sum / n;
            TestContext.WriteLine("Nachrichten-Jagd Ø: " + (avg * 100).ToString("0.00") + " %");
            Assert.IsTrue(avg < 0, "Nachrichten jagen lohnt nach Gebühren nicht (" + avg + ")");
        }

        [Test]
        public void Trading_BuySellNewAssets()
        {
            var gm = FreshSeeded(2);
            gm.Money = 2000;
            Assert.IsTrue(gm.Market.Buy("HSTL", 1000));
            Assert.AreEqual(1000, gm.Money);
            Assert.AreEqual(1000f * (1f - Market.FeeEtf) / gm.Market.Prices["HSTL"], gm.Market.Holdings["HSTL"], 1e-3f, "ETF-Gebühr 0,5 %");
            Assert.IsTrue(gm.Market.PortfolioValue() > 980 && gm.Market.KindValue(AssetKind.Etf) > 980, "Depot zählt neue Anlagen mit");
            Assert.IsFalse(gm.Market.Buy("NOPE", 100), "Unbekannte Anlage");
            Assert.IsFalse(gm.Market.Buy("KEKS", 5000), "Nicht genug Geld");
            int back = gm.Market.Sell("HSTL", 1f);
            Assert.IsTrue(back > 980 && back <= 1000, "Rückkauf minus Gebühren: " + back);
            Assert.AreEqual(0, gm.Market.Sell("NOPE", 1f));
            Assert.AreEqual(0f, gm.Market.HoldingValue("NOPE"));
        }

        [Test]
        public void Dividends_PaidOvernight()
        {
            var gm = FreshSeeded(4);
            gm.Money = 5000;
            Assert.IsTrue(gm.Market.Buy("KART", 4000));
            int expected = gm.Market.DividendForecast();
            Assert.IsTrue(expected >= 8, "≈ 0,25 % von 4.000 €: " + expected);
            int before = gm.Money;
            gm.Market.NewDay();
            Assert.AreEqual(before + expected, gm.Money, "Dividende gutgeschrieben");
            Assert.AreEqual(expected, gm.Market.LastDividends);
            Assert.AreEqual(expected, gm.Market.DividendsTotal);
            Assert.AreEqual(0f, gm.Market.DividendPerDay("KEKS"), "Krypto zahlt nichts");
        }

        [Test]
        public void Savings_InterestOnlyWithoutDebtAndCapped()
        {
            var gm = FreshSeeded(6);
            gm.Money = 100000;
            Assert.IsFalse(gm.Market.Deposit(200000), "Nicht genug Geld");
            Assert.IsFalse(gm.Market.Deposit(0));
            Assert.IsTrue(gm.Market.Deposit(10000));
            Assert.AreEqual(90000, gm.Money);
            Assert.AreEqual(10000, gm.Market.Savings);
            gm.Market.NewDay();
            Assert.AreEqual(10030, gm.Market.Savings, "0,3 % Zinsen");
            gm.Debt = 500;
            Assert.AreEqual(0, gm.Market.SavingsInterestForecast(), "Mit Schulden keine Sparzinsen");
            gm.Market.NewDay();
            Assert.AreEqual(10030, gm.Market.Savings);
            gm.Debt = 0;
            gm.Market.Deposit(80000);
            Assert.AreEqual((int)(Market.SavingsCap * Market.SavingsRate), gm.Market.SavingsInterestForecast(), "Zinsen gedeckelt");
            Assert.IsTrue(Market.SavingsRate < GameData.LoanDailyInterest / 4f, "Sparzins weit unter Kreditzins");
            int got = gm.Market.Withdraw(1000000);
            Assert.AreEqual(90030, got, "Alles abgehoben");
            Assert.AreEqual(0, gm.Market.Savings);
            Assert.AreEqual(100030, gm.Money);
            Assert.AreEqual(0, gm.Market.Withdraw(10));
        }

        [Test]
        public void Savings_CoverFixedCostsAtEndOfDay()
        {
            var gm = TestUtil.Fresh(9);
            gm.NewGame("skip");
            gm.Money = 1000;
            gm.Market.Deposit(1000);
            gm.Money = -300;
            DaySummary summary = null;
            gm.DayEnded += s => summary = s;
            gm.EndDayNow();
            Assert.IsNotNull(summary);
            Assert.IsFalse(summary.Bankrupt, "Dispo-Schutz verhindert Pleite");
            Assert.IsTrue(gm.Money >= gm.FixedCostsPerDay(), "Fixkosten gedeckt");
            Assert.IsTrue(gm.Market.Savings < 1000 && gm.Market.Savings >= 0);
            Assert.IsTrue(summary.Notes.Exists(n => n.Contains("Sparkonto")), "Hinweis auf dem Kassenbon");
        }

        [Test]
        public void CreditScore_ReactsToDebt()
        {
            var gm = FreshSeeded(1);
            int clean = gm.Market.CreditScore();
            gm.Debt = gm.CreditLimit();
            int indebted = gm.Market.CreditScore();
            Assert.IsTrue(indebted < clean - 200, "Schulden drücken die Bonität");
            Assert.IsTrue(clean >= 0 && clean <= 1000);
            Assert.IsFalse(string.IsNullOrEmpty(Market.CreditScoreLabel(indebted)));
            Assert.AreEqual(gm.Money + gm.Market.Savings + gm.Market.PortfolioValue() - gm.Debt, gm.Market.NetWorth());
        }

        [Test]
        public void Save_RoundTripAndOldSavesLoad()
        {
            var gm = FreshSeeded(11);
            gm.Money = 5000;
            gm.Market.Buy("KEKS", 500);
            gm.Market.Buy("DROP", 300);
            gm.Market.Deposit(1234);
            gm.Market.PostNews(0, -1);
            gm.Market.Tick(false);
            float keks = gm.Market.Holdings["KEKS"];
            float price = gm.Market.Prices["KEKS"];
            string text = gm.Market.News[0].Text;
            var json = Json.Parse(Json.Write(gm.ToJson())) as Dictionary<string, object>;

            var gm2 = TestUtil.Fresh(12);
            gm2.FromJson(json);
            Assert.AreEqual(keks, gm2.Market.Holdings["KEKS"], 1e-4f, "Krypto-Bestand");
            Assert.AreEqual(price, gm2.Market.Prices["KEKS"], 1e-3f, "Kurs");
            Assert.AreEqual(1234, gm2.Market.Savings, "Sparkonto");
            Assert.AreEqual(text, gm2.Market.News[0].Text, "Nachrichten");
            Assert.IsTrue(gm2.Market.Holdings["DROP"] > 0f, "Original-Bestand");

            // Alter Spielstand (v4): Markt ohne neue Schlüssel und ohne neue Anlagen
            var market = J.O(json, "market");
            foreach (var key in new[] { "savings", "news", "dividends_total", "savings_interest_total", "last_dividends", "last_savings_interest" }) market.Remove(key);
            foreach (var key in new[] { "prices", "history", "holdings", "invested" })
                foreach (var a in Market.Extra) J.O(market, key).Remove(a.Id);
            var gm3 = TestUtil.Fresh(13);
            gm3.FromJson(json);
            Assert.AreEqual(0, gm3.Market.Savings);
            Assert.AreEqual(0, gm3.Market.News.Count);
            Assert.IsTrue(gm3.Market.Holdings["DROP"] > 0f, "Alter Bestand bleibt");
            foreach (var a in Market.Extra)
            {
                Assert.AreEqual(0f, gm3.Market.Holdings[a.Id], "Neue Anlage leer: " + a.Id);
                Assert.IsTrue(gm3.Market.Prices[a.Id] > 0f && gm3.Market.History[a.Id].Count > 0, "Neue Anlage hat Kurs: " + a.Id);
            }
            gm3.Market.Tick(false);
            Assert.IsTrue(gm3.Market.PortfolioValue() > 0);
        }

        [Test]
        public void Texts_AreFilled()
        {
            var rng = new Rng(1);
            foreach (var a in Market.OfKind(AssetKind.Stock))
            {
                if (a.Legacy) continue;
                Assert.IsFalse(string.IsNullOrEmpty(FinanceText.StockHeadline(a.Id, true, rng)));
                Assert.IsFalse(string.IsNullOrEmpty(FinanceText.StockHeadline(a.Id, false, rng)));
            }
            Assert.IsTrue(FinanceText.BrokerBabble.Length >= 15 && FinanceText.BrokerPanic.Length >= 5 && FinanceText.BankerLines.Length >= 4);
            Assert.AreEqual("KeksCoin geht ab!", string.Format(FinanceText.BrokerUp, "KeksCoin"));
        }
    }
}
