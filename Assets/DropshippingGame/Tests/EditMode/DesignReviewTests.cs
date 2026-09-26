using System;
using System.Collections.Generic;
using DropshippingGame.Core;
using NUnit.Framework;

namespace DropshippingGame.Tests
{
    /// <summary>Balance-Paket „Echte Entscheidungen“ (GAME_IDEAS 5.1): Trading, Premium, Werbung, TikTok, Express, Wochenziel-Tausch.</summary>
    public class DesignReviewTests
    {
        private const int TicksPerDay = 270;

        [Test]
        public void Etf_IsNoMoneyMachineAnymore()
        {
            var gm = TestUtil.Fresh(17);
            double sum = 0;
            for (int d = 0; d < 300; d++)
            {
                float start = gm.Market.Prices["ETF"];
                for (int t = 0; t < TicksPerDay; t++) gm.Market.Tick(false);
                sum += gm.Market.Prices["ETF"] / start - 1f;
            }
            double avg = sum / 300;
            Assert.IsTrue(avg > 0 && avg < 0.01, "Mittlere Tagesrendite 0-1 % (" + avg + ")");
            Assert.IsTrue(avg < GameData.LoanDailyInterest, "Kredit + ETF lohnt sich nicht");
            int doubled = 0;
            var drops = new List<float>();
            for (int run = 0; run < 100; run++)
            {
                var m = TestUtil.Fresh(1000 + run);
                float e0 = m.Market.Prices["ETF"], c0 = m.Market.Prices["DROP"];
                for (int t = 0; t < TicksPerDay * 10; t++) m.Market.Tick(false);
                if (m.Market.Prices["ETF"] / e0 >= 1.5f) doubled++;
                drops.Add(m.Market.Prices["DROP"] / c0);
            }
            drops.Sort();
            Assert.AreEqual(0, doubled, "ETF ×1,5 in 10 Tagen praktisch unmöglich");
            float median = drops[drops.Count / 2];
            Assert.IsTrue(median > 0.8f && median < 1.2f, "DROPCOIN-Median ≈ ×1 (" + median + ")");
        }

        [Test]
        public void Premium_OnlyOneOrderPerProductAndDay()
        {
            var gm = TestUtil.Fresh();
            gm.Level = 2;
            gm.Money = 5000;
            Assert.IsTrue(gm.PremiumQuotaLeft("huelle"));
            Assert.IsTrue(gm.BuyBulk(0, 0, 2), "Erste Premium-Bestellung");
            Assert.IsFalse(gm.PremiumQuotaLeft("huelle"));
            Assert.IsFalse(gm.BuyBulk(0, 0, 2), "Zweite am selben Tag nicht");
            Assert.IsTrue(gm.BuyBulk(1, 0, 2), "Anderes Produkt geht");
            Assert.IsTrue(gm.BuyBulk(0, 0, 1), "Standard geht immer");
            gm.EndDayNow();
            gm.StartNextDay();
            Assert.IsTrue(gm.PremiumQuotaLeft("huelle"), "Am nächsten Tag wieder");
            Assert.AreEqual(2f, GameData.Suppliers[2].PriceMult, 0.001f, "Premium kostet das Doppelte");
        }

        [Test]
        public void Ads_RunUntilClosingTime()
        {
            var gm = TestUtil.Fresh();
            gm.Money = 1000;
            gm.AdvanceMinutes(120f);
            Assert.IsTrue(gm.StartAdCampaign(0));
            Assert.AreEqual(gm.BClock() + (GameData.DayEnd - gm.TimeMinutes), gm.ActiveBoost("ad").EndsAt, 0.01f, "Bis 20 Uhr");
            gm.AddBoost("x", "X", 3f, 60f);
            gm.AddBoost("y", "Y", 3f, 60f);
            Assert.AreEqual(GameData.MaxBoostMult, gm.BoostMult("huelle"), 0.001f, "Deckel ×4");
        }

        [Test]
        public void TikTok_AtMostThreePerDay()
        {
            var gm = TestUtil.Fresh();
            gm.Level = 2;
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(gm.TikTokAvailable(), "Video " + (i + 1));
                gm.TriggerTikTok(0.5f);
                gm.AdvanceMinutes(GameData.TikTokCooldown + 1f);
            }
            Assert.AreEqual(0, gm.TikToksLeftToday());
            Assert.IsFalse(gm.TikTokAvailable(), "Das 4. Video geht nicht");
            gm.EndDayNow();
            gm.StartNextDay();
            Assert.IsTrue(gm.TikTokAvailable(), "Zähler morgens zurückgesetzt");
        }

        [Test]
        public void TikTok_StrongerForHypedProduct()
        {
            var a = TestUtil.Fresh();
            var b = TestUtil.Fresh();
            b.Trends.State("led").Start = b.Trends.State("led").Hype = 1.6f;
            a.TriggerTikTok(0.5f, true, "led");
            b.TriggerTikTok(0.5f, true, "led");
            Assert.IsTrue(b.ActiveBoost("tiktok").Mult > a.ActiveBoost("tiktok").Mult, "Hype-Bonus");
        }

        [Test]
        public void Express_NotWhenQueueAlmostFull()
        {
            var gm = TestUtil.Fresh(5);
            gm.Level = 10;
            for (int i = 0; i < 300; i++)
            {
                while (gm.OrderQueue.Count < gm.QueueCapacity() - 1) gm.SpawnOrder("huelle", false);
                gm.SpawnOrder("huelle");
                Assert.IsFalse(gm.OrderQueue[gm.OrderQueue.Count - 1].Express, "Kein Express bei (fast) voller Schlange");
                gm.OrderQueue.Clear();
            }
        }

        [Test]
        public void Challenge_CanBeRerolledOncePerWeek()
        {
            var gm = new Sim(8);
            gm.NewGame("skip");
            var first = gm.Challenges[0];
            Assert.IsTrue(gm.CanRerollChallenge(first.Id));
            Assert.IsTrue(gm.RerollChallenge(first.Id), "Getauscht");
            Assert.AreNotSame(first, gm.Challenges[0]);
            var types = new HashSet<string>();
            foreach (var c in gm.Challenges) Assert.IsTrue(types.Add(c.Type), "Keine doppelten Typen");
            Assert.IsFalse(gm.RerollChallenge(gm.Challenges[1].Id), "Nur einmal pro Woche");
            for (int d = 0; d < 7; d++)
            {
                gm.EndDayNow();
                gm.StartNextDay();
            }
            Assert.IsTrue(gm.CanRerollChallenge(gm.Challenges[0].Id), "Neue Woche, neuer Tausch");
        }
    }
}
